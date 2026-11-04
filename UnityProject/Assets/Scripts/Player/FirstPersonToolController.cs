using System;
using UnityEngine;

/// <summary>
/// Tool coordinator: hotbar selection and routing of the on-screen buttons to the tool that is
/// in hand. The work itself is split by responsibility:
///  - <see cref="PunchAction"/>: punch rules, cooldown, charge, pattern level
///  - <see cref="DynamiteAction"/>: placement and the dynamite pool
///  - <see cref="HeadlampController"/>: lamp levels and flicker
///  - <see cref="ToolViewmodelAnimator"/>: all viewmodel motion
///  - <see cref="VoxelAim"/>: grid-ray aiming
///  - <see cref="RubbleSuctionManager"/>: vacuum suction and blow-out
/// Public methods keep the first draft's names so UI wiring stays recognisable.
/// </summary>
public class FirstPersonToolController : MonoBehaviour
{
    public enum HotbarSlot { Fists = 0, Vacuum = 1, Dynamite = 2, Headlamp = 3 }

    public ToolViewmodelAnimator animator;
    public PunchAction punch;
    public DynamiteAction dynamite;
    public HeadlampController headlamp;
    public VoxelAim aim;
    public Camera playerCamera;

    public HotbarSlot CurrentSlot => (HotbarSlot)(int)(animator ? animator.CurrentSlot : ToolViewmodelAnimator.Slot.Fists);
    public bool IsVacuumHeld { get; private set; }
    public bool IsBlowMode { get; private set; }

    /// <summary>Raised when the selected slot changes (for the hotbar highlight).</summary>
    public event Action<HotbarSlot> SlotChanged;
    public event Action<bool> VacuumHeldChanged;

    private bool vacuumRequested;

    void Awake()
    {
        if (!animator) Debug.LogError("FirstPersonToolController: animator is not assigned.", this);
        if (!punch) Debug.LogError("FirstPersonToolController: punch is not assigned.", this);
        if (!playerCamera) playerCamera = Camera.main;
    }

    void Update()
    {
        // Editor and desktop testing keys (read through the single input abstraction).
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Punch)) OnPunchButtonDown();
        if (PlayerInputSource.DebugKeyUp(PlayerInputSource.DebugKey.Punch)) OnPunchButtonUp();
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Vacuum)) SetVacuumButtonHeld(true);
        if (PlayerInputSource.DebugKeyUp(PlayerInputSource.DebugKey.Vacuum)) SetVacuumButtonHeld(false);
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.BlowToggle)) ToggleVacuumBlowMode();
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Slot1)) SelectHotbarSlot(0);
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Slot2)) SelectHotbarSlot(1);
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Slot3)) SelectHotbarSlot(2);
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Slot4)) SelectHotbarSlot(3);
    }

    // ------------------------------------------------------------------ PUNCH button (tap / hold)

    public void OnPunchButtonDown()
    {
        if (!animator || animator.IsSwapping) return;
        switch (CurrentSlot)
        {
            case HotbarSlot.Fists: if (punch) punch.OnButtonDown(); break;
            case HotbarSlot.Dynamite: if (dynamite) dynamite.TryPlace(); break;
            case HotbarSlot.Vacuum: SetVacuumButtonHeld(true); break;
        }
    }

    public void OnPunchButtonUp()
    {
        if (CurrentSlot == HotbarSlot.Fists && punch) punch.OnButtonUp();
        else if (CurrentSlot == HotbarSlot.Vacuum) SetVacuumButtonHeld(false);
    }

    // ------------------------------------------------------------------ VACUUM button

    /// <summary>Holding VACUUM switches to the vacuum if needed and runs it.</summary>
    public void SetVacuumButtonHeld(bool held)
    {
        vacuumRequested = held;
        if (held && CurrentSlot != HotbarSlot.Vacuum)
        {
            // Swap to the vacuum first; it starts when the swap finishes if the button is still held.
            SelectHotbarSlot((int)HotbarSlot.Vacuum);
            return;
        }
        if (IsVacuumHeld == held) return;
        IsVacuumHeld = held;
        if (animator) animator.SetVacuumState(held, IsBlowMode);
        VacuumHeldChanged?.Invoke(held);
    }

    public void ToggleVacuumBlowMode()
    {
        IsBlowMode = !IsBlowMode;
        if (animator) animator.SetVacuumState(IsVacuumHeld, IsBlowMode);
    }

    // ------------------------------------------------------------------ hotbar

    public void SelectHotbarSlot(int slotIndex)
    {
        if (!animator) return;
        var slot = (HotbarSlot)Mathf.Clamp(slotIndex, 0, 3);
        if (slot == HotbarSlot.Headlamp)
        {
            if (headlamp) animator.PlayHeadlampGesture(headlamp.Toggle);
            return;
        }
        if (slot == CurrentSlot) return;
        if (punch) punch.CancelHold();
        if (IsVacuumHeld)
        {
            IsVacuumHeld = false;
            animator.SetVacuumState(false, IsBlowMode);
            VacuumHeldChanged?.Invoke(false);
        }
        animator.SwapTo((ToolViewmodelAnimator.Slot)(int)slot, () =>
        {
            SlotChanged?.Invoke(slot);
            if (slot == HotbarSlot.Vacuum && vacuumRequested) SetVacuumButtonHeld(true);
        });
    }

    public void SetGloveUpgradeTier(int tier) { if (animator) animator.SetGloveTier(tier); }
    public void SetVacuumUpgradeTier(int tier) { if (animator) animator.SetVacuumTier(tier); }
}
