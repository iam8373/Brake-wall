using System;
using BreakToEscape.Core;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Limits;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>Information about one punch that reached the wall.</summary>
public struct PunchStrike
{
    public Int3 voxel;
    public Int3 normal;
    public Vector3 worldPoint;
    /// <summary>Material of the struck cube before damage (so sound never reads air).</summary>
    public byte material;
    public int patternLevel;
    public float damage;
    public bool charged;
    public float chargeRatio;
    public int cubesBroken;
}

/// <summary>
/// Punch rules. Enforces the Punch Speed table as a global cooldown shared by both arms (with a
/// short input buffer so taps feel responsive), passes the player's real pattern level to the
/// world, and applies the charged-punch rule (up to +2 pattern levels, capped at 10, and up to
/// ×2 damage; values from economy.json). Auto-punch uses the same limiter.
/// </summary>
public class PunchAction : MonoBehaviour
{
    public VoxelAim aim;
    public ToolViewmodelAnimator animator;
    [Tooltip("A release this long after the tap threshold still counts as a tap. Tap threshold 0.18 s + 0.12 s = 0.30 s, the first draft's minimum charge time.")]
    public float chargeCommitSeconds = 0.12f;

    /// <summary>Raised when a punch lands on the wall (after damage is applied).</summary>
    public event Action<PunchStrike> Struck;
    /// <summary>Raised when a punch is swung (hit or miss), for rate measurement.</summary>
    public event Action Swung;

    public bool PreviewVisible => enabled && animator && animator.CurrentSlot == ToolViewmodelAnimator.Slot.Fists;
    /// <summary>Pattern level the preview should show now (includes the current charge).</summary>
    public int PreviewPatternLevel => holding && HoldCharge > 0f && !AutoPunchActive ? PunchPatterns.ChargedLevel(BaseLevel, HoldCharge, data.maxPatternBonusLevels) : BaseLevel;
    public int DepthLayers => economy != null ? economy.PunchDepthLayers : 1;
    public bool AutoPunchEnabled { get; set; }
    public bool AutoPunchActive => AutoPunchEnabled && economy != null && economy.AutoPunchUnlocked;

    private EconomyData data;
    private EconomyState economy;
    private CooldownLimiter limiter;
    private bool holding;
    private float holdStart;
    private bool pendingCharged;
    private float pendingChargeRatio;

    private int BaseLevel => economy != null ? economy.PatternLevel : 1;

    /// <summary>0..1 charge of the current hold (0 below the tap threshold).</summary>
    public float HoldCharge
    {
        get
        {
            if (!holding || data == null) return 0f;
            float held = Time.time - holdStart;
            if (held < data.tapThresholdSeconds) return 0f;
            return Mathf.Clamp01((held - data.tapThresholdSeconds) / Mathf.Max(0.01f, data.fullChargeSeconds - data.tapThresholdSeconds));
        }
    }

    public void Initialize(EconomyData economyData, EconomyState economyState)
    {
        data = economyData;
        economy = economyState;
        limiter = new CooldownLimiter(economy.PunchesPerSecond, data.punchInputBufferSeconds);
        if (aim) aim.reachMeters = data.punchReachMeters;
        ApplyStats();
        if (!aim) Debug.LogError("PunchAction: aim is not assigned.", this);
        if (!animator) Debug.LogError("PunchAction: animator is not assigned.", this);
    }

    /// <summary>Re-reads punch speed after upgrades or equipment changes.</summary>
    public void ApplyStats()
    {
        if (economy == null || limiter == null) return;
        limiter.SetRate(economy.PunchesPerSecond);
        if (animator) animator.SetPunchRate(economy.PunchesPerSecond);
    }

    public void OnButtonDown()
    {
        if (limiter == null) return;
        holding = true;
        holdStart = Time.time;
        if (AutoPunchActive) TryTapPunch();
    }

    public void OnButtonUp()
    {
        if (!holding || limiter == null) return;
        holding = false;
        if (animator) animator.SetChargeWindup(-1f);
        if (AutoPunchActive) return;

        float held = Time.time - holdStart;
        if (held < data.tapThresholdSeconds + chargeCommitSeconds)
        {
            TryTapPunch();
        }
        else
        {
            float ratio = Mathf.Clamp01((held - data.tapThresholdSeconds) / Mathf.Max(0.01f, data.fullChargeSeconds - data.tapThresholdSeconds));
            if (limiter.Request(Time.time)) FireCharged(ratio);
            else { pendingCharged = true; pendingChargeRatio = ratio; }
        }
    }

    /// <summary>Cancels a hold (tool swap, pause).</summary>
    public void CancelHold()
    {
        holding = false;
        pendingCharged = false;
        if (animator) animator.SetChargeWindup(-1f);
    }

    void Update()
    {
        if (limiter == null) return;
        float now = Time.time;

        if (holding && !AutoPunchActive && animator) animator.SetChargeWindup(HoldCharge > 0f ? HoldCharge : -1f);

        if (pendingCharged && limiter.IsReady(now))
        {
            pendingCharged = false;
            if (limiter.Request(now)) FireCharged(pendingChargeRatio);
        }
        else if (limiter.Poll(now))
        {
            FireTap();
        }

        if (holding && AutoPunchActive && limiter.IsReady(now)) TryTapPunch();
    }

    private void TryTapPunch()
    {
        if (!animator || !animator.CanJab) return;
        if (limiter.Request(Time.time)) FireTap();
    }

    private void FireTap()
    {
        if (!animator || !animator.PlayJab(() => Strike(1f, false, 0f))) return;
        Swung?.Invoke();
    }

    private void FireCharged(float ratio)
    {
        if (!animator) return;
        float damageMultiplier = PunchPatterns.ChargedDamageMultiplier(ratio, data.maxChargeDamageMultiplier);
        if (animator.PlayChargedRelease(ratio, () => Strike(damageMultiplier, true, ratio))) Swung?.Invoke();
    }

    /// <summary>Runs at the impact frame of the animation. Damage, pattern and depth come from the economy state.</summary>
    private void Strike(float damageMultiplier, bool charged, float ratio)
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!aim || !world || economy == null) return;
        aim.Refresh();
        if (!aim.HasTarget) return;

        GridHit hit = aim.CurrentHit;
        byte material = world.Grid.Get(hit.voxel);
        int level = charged ? PunchPatterns.ChargedLevel(BaseLevel, ratio, data.maxPatternBonusLevels) : BaseLevel;
        float damage = economy.PunchDamage * damageMultiplier;
        int broken = world.ApplyPunch(hit.voxel, hit.normal, level, economy.PunchDepthLayers, damage);

        Struck?.Invoke(new PunchStrike
        {
            voxel = hit.voxel,
            normal = hit.normal,
            worldPoint = aim.FacePoint,
            material = material,
            patternLevel = level,
            damage = damage,
            charged = charged,
            chargeRatio = ratio,
            cubesBroken = broken,
        });
    }
}
