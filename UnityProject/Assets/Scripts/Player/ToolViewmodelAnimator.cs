using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Code-driven first-person viewmodel animation (rigid arms, vacuum, dynamite, headlamp gesture).
/// All timings and curves are inspector fields for tuning. Animation never decides game rules:
/// punch timing comes from <see cref="PunchAction"/> and its rate limiter; the animator only plays
/// the motion and calls back at the impact moment.
/// </summary>
public class ToolViewmodelAnimator : MonoBehaviour
{
    public enum Slot { Fists = 0, Vacuum = 1, Dynamite = 2, Headlamp = 3 }

    [Header("Transforms")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform vacuumRoot;
    public Transform dynamiteInHand;

    [Header("Upgrade looks")]
    public GameObject[] leftGloveTiers;
    public GameObject[] rightGloveTiers;
    public GameObject[] vacuumTiers;

    [Header("Idle breathing")]
    public float breatheSpeed = 2.6f;
    public float breatheAmplitude = 0.014f;

    [Header("Jab (thrust time scales with punch speed, clamped so it stays readable)")]
    public Vector3 leftJabOffset = new Vector3(0.22f, 0.04f, 0.58f);
    public Vector3 rightJabOffset = new Vector3(-0.22f, 0.04f, 0.58f);
    [Tooltip("Share of one arm's cycle spent thrusting.")]
    [Range(0.1f, 0.6f)] public float thrustShare = 0.3f;
    [Tooltip("Shortest thrust, at the highest punch speed (seconds). About 0.07 s or more reads clearly at 60 fps.")]
    public float minThrustTime = 0.07f;
    public float maxThrustTime = 0.12f;
    public float minReturnTime = 0.10f;
    public float maxReturnTime = 0.24f;
    public AnimationCurve thrustCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Charged punch")]
    public Vector3 chargeWindupOffset = new Vector3(0.12f, -0.08f, -0.26f);
    public Vector3 chargeWindupEuler = new Vector3(-22f, 10f, -14f);
    public Vector3 chargeThrustOffset = new Vector3(-0.28f, 0.08f, 0.78f);
    public Vector3 chargeThrustEuler = new Vector3(12f, -8f, 5f);
    public float chargeShakeAmount = 0.014f;
    public float chargedThrustTime = 0.08f;
    public float chargedHoldTime = 0.06f;
    public float chargedReturnTime = 0.22f;

    [Header("Vacuum")]
    public float vacuumJitter = 0.006f;
    public float suctionRecoil = 0.025f;
    public float blowRecoil = 0.045f;

    [Header("Tool swap")]
    public float swapDropDistance = 0.45f;
    public float swapHalfTime = 0.13f;

    [Header("Dynamite place (reach to the wall and press the stick on)")]
    public Vector3 placeReachOffset = new Vector3(-0.12f, 0.06f, 0.62f);
    public float placeReachTime = 0.16f;
    public float placeReturnTime = 0.18f;

    [Header("Headlamp gesture")]
    public Vector3 helmetTapOffset = new Vector3(0.08f, 0.35f, -0.1f);
    public Vector3 helmetTapEuler = new Vector3(-40f, 20f, 0f);
    public float helmetTapTime = 0.12f;

    public bool IsSwapping { get; private set; }
    public bool LeftBusy { get; private set; }
    public bool RightBusy { get; private set; }
    public Slot CurrentSlot { get; private set; } = Slot.Fists;

    private Vector3 leftRestPos, rightRestPos, vacRestPos, dynRestPos;
    private Quaternion leftRestRot, rightRestRot, vacRestRot, dynRestRot;
    private bool nextJabLeft = true;
    private float thrustTime = 0.1f, returnTime = 0.2f;
    private float windupRatio = -1f;
    private bool vacuumHeld, vacuumBlow;

    void Awake()
    {
        if (!leftArm || !rightArm) Debug.LogError("ToolViewmodelAnimator: arm transforms are not assigned.", this);
        if (leftArm) { leftRestPos = leftArm.localPosition; leftRestRot = leftArm.localRotation; }
        if (rightArm) { rightRestPos = rightArm.localPosition; rightRestRot = rightArm.localRotation; }
        if (vacuumRoot) { vacRestPos = vacuumRoot.localPosition; vacRestRot = vacuumRoot.localRotation; }
        if (dynamiteInHand) { dynRestPos = dynamiteInHand.localPosition; dynRestRot = dynamiteInHand.localRotation; }
        ApplySlotVisibility(Slot.Fists, true);
    }

    /// <summary>Sets jab timings from punches per second. Arms alternate, so each arm's cycle is two intervals.</summary>
    public void SetPunchRate(float punchesPerSecond)
    {
        float armCycle = 2f / Mathf.Max(0.1f, punchesPerSecond);
        thrustTime = Mathf.Clamp(armCycle * thrustShare, minThrustTime, maxThrustTime);
        returnTime = Mathf.Clamp(armCycle * (1f - thrustShare) * 0.6f, minReturnTime, maxReturnTime);
    }

    public float CurrentThrustTime => thrustTime;

    public bool CanJab => !IsSwapping && (CurrentSlot == Slot.Fists) && (!LeftBusy || !RightBusy) && windupRatio < 0f;

    void Update()
    {
        if (IsSwapping) return;
        float t = Time.time;
        if (CurrentSlot == Slot.Fists || CurrentSlot == Slot.Dynamite)
        {
            if (!LeftBusy && leftArm) Breathe(leftArm, leftRestPos, leftRestRot, 0f, t);
            if (!RightBusy && windupRatio < 0f && rightArm) Breathe(rightArm, rightRestPos, rightRestRot, Mathf.PI * 0.5f, t);
            if (windupRatio >= 0f && rightArm && !RightBusy)
            {
                Vector3 shake = UnityEngine.Random.insideUnitSphere * (chargeShakeAmount * windupRatio);
                Vector3 pos = rightRestPos + chargeWindupOffset * windupRatio + shake;
                Quaternion rot = rightRestRot * Quaternion.Euler(chargeWindupEuler * windupRatio);
                rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, pos, Time.deltaTime * 18f);
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rot, Time.deltaTime * 18f);
            }
            if (CurrentSlot == Slot.Dynamite && dynamiteInHand && !RightBusy) Breathe(dynamiteInHand, dynRestPos, dynRestRot, Mathf.PI * 0.5f, t);
        }
        else if (CurrentSlot == Slot.Vacuum && vacuumRoot)
        {
            if (vacuumHeld)
            {
                float kick = vacuumBlow ? -blowRecoil : -suctionRecoil;
                Vector3 jitter = UnityEngine.Random.insideUnitSphere * vacuumJitter;
                vacuumRoot.localPosition = Vector3.Lerp(vacuumRoot.localPosition, vacRestPos + new Vector3(0f, 0f, kick) + jitter, Time.deltaTime * 25f);
            }
            else
            {
                Breathe(vacuumRoot, vacRestPos, vacRestRot, 0f, t);
            }
        }
    }

    private void Breathe(Transform target, Vector3 restPos, Quaternion restRot, float phase, float time)
    {
        float y = Mathf.Sin(time * breatheSpeed + phase) * breatheAmplitude;
        float x = Mathf.Cos(time * breatheSpeed * 0.5f + phase) * breatheAmplitude * 0.5f;
        target.localPosition = Vector3.Lerp(target.localPosition, restPos + new Vector3(x, y, 0f), Time.deltaTime * 10f);
        target.localRotation = Quaternion.Slerp(target.localRotation, restRot, Time.deltaTime * 10f);
    }

    // ------------------------------------------------------------------ punches

    /// <summary>Plays an alternating jab. <paramref name="onImpact"/> runs at full extension.</summary>
    public bool PlayJab(Action onImpact)
    {
        if (!CanJab) return false;
        bool useLeft = nextJabLeft ? !LeftBusy : RightBusy;
        nextJabLeft = !useLeft;
        if (useLeft) StartCoroutine(JabRoutine(leftArm, true, leftRestPos, leftRestRot, leftJabOffset, onImpact));
        else StartCoroutine(JabRoutine(rightArm, false, rightRestPos, rightRestRot, rightJabOffset, onImpact));
        return true;
    }

    private IEnumerator JabRoutine(Transform arm, bool isLeft, Vector3 restPos, Quaternion restRot, Vector3 offset, Action onImpact)
    {
        SetBusy(isLeft, true);
        Vector3 startPos = arm.localPosition;
        Quaternion startRot = arm.localRotation;
        Vector3 apexPos = restPos + offset;
        Quaternion apexRot = restRot * Quaternion.Euler(6f, isLeft ? 8f : -8f, 0f);
        float thrust = thrustTime, ret = returnTime;

        for (float t = 0f; t < thrust; t += Time.deltaTime)
        {
            float k = thrustCurve.Evaluate(t / thrust);
            arm.localPosition = Vector3.LerpUnclamped(startPos, apexPos, k);
            arm.localRotation = Quaternion.Slerp(startRot, apexRot, k);
            yield return null;
        }
        arm.localPosition = apexPos;
        onImpact?.Invoke();

        for (float t = 0f; t < ret; t += Time.deltaTime)
        {
            float k = returnCurve.Evaluate(t / ret);
            arm.localPosition = Vector3.LerpUnclamped(apexPos, restPos, k);
            arm.localRotation = Quaternion.Slerp(apexRot, restRot, k);
            yield return null;
        }
        arm.localPosition = restPos;
        arm.localRotation = restRot;
        SetBusy(isLeft, false);
    }

    /// <summary>Wind-up pose while the PUNCH button is held (ratio 0..1); pass a negative value to stop.</summary>
    public void SetChargeWindup(float ratio) => windupRatio = ratio < 0f ? -1f : Mathf.Clamp01(ratio);

    public bool PlayChargedRelease(float chargeRatio, Action onImpact)
    {
        if (IsSwapping || RightBusy || !rightArm) return false;
        windupRatio = -1f;
        StartCoroutine(ChargedRoutine(Mathf.Clamp01(chargeRatio), onImpact));
        return true;
    }

    private IEnumerator ChargedRoutine(float chargeRatio, Action onImpact)
    {
        RightBusy = true;
        Vector3 startPos = rightArm.localPosition;
        Quaternion startRot = rightArm.localRotation;
        Vector3 apexPos = rightRestPos + Vector3.Lerp(rightJabOffset, chargeThrustOffset, chargeRatio);
        Quaternion apexRot = rightRestRot * Quaternion.Euler(chargeThrustEuler * chargeRatio);
        for (float t = 0f; t < chargedThrustTime; t += Time.deltaTime)
        {
            float k = thrustCurve.Evaluate(t / chargedThrustTime);
            rightArm.localPosition = Vector3.LerpUnclamped(startPos, apexPos, k);
            rightArm.localRotation = Quaternion.Slerp(startRot, apexRot, k);
            yield return null;
        }
        rightArm.localPosition = apexPos;
        onImpact?.Invoke();
        yield return new WaitForSeconds(chargedHoldTime);
        for (float t = 0f; t < chargedReturnTime; t += Time.deltaTime)
        {
            float k = returnCurve.Evaluate(t / chargedReturnTime);
            rightArm.localPosition = Vector3.LerpUnclamped(apexPos, rightRestPos, k);
            rightArm.localRotation = Quaternion.Slerp(apexRot, rightRestRot, k);
            yield return null;
        }
        rightArm.localPosition = rightRestPos;
        rightArm.localRotation = rightRestRot;
        RightBusy = false;
    }

    // ------------------------------------------------------------------ dynamite and headlamp

    public bool PlayPlaceDynamite(Action onPlaced)
    {
        if (IsSwapping || RightBusy || !rightArm) return false;
        StartCoroutine(PlaceRoutine(onPlaced));
        return true;
    }

    private IEnumerator PlaceRoutine(Action onPlaced)
    {
        RightBusy = true;
        Vector3 reach = rightRestPos + placeReachOffset;
        Quaternion reachRot = rightRestRot * Quaternion.Euler(10f, -6f, 0f);
        for (float t = 0f; t < placeReachTime; t += Time.deltaTime)
        {
            float k = thrustCurve.Evaluate(t / placeReachTime);
            rightArm.localPosition = Vector3.LerpUnclamped(rightRestPos, reach, k);
            rightArm.localRotation = Quaternion.Slerp(rightRestRot, reachRot, k);
            if (dynamiteInHand) dynamiteInHand.localPosition = Vector3.LerpUnclamped(dynRestPos, dynRestPos + placeReachOffset, k);
            yield return null;
        }
        onPlaced?.Invoke();
        if (dynamiteInHand) dynamiteInHand.gameObject.SetActive(false);
        for (float t = 0f; t < placeReturnTime; t += Time.deltaTime)
        {
            float k = returnCurve.Evaluate(t / placeReturnTime);
            rightArm.localPosition = Vector3.LerpUnclamped(reach, rightRestPos, k);
            rightArm.localRotation = Quaternion.Slerp(reachRot, rightRestRot, k);
            yield return null;
        }
        rightArm.localPosition = rightRestPos;
        rightArm.localRotation = rightRestRot;
        if (dynamiteInHand) dynamiteInHand.localPosition = dynRestPos;
        RightBusy = false;
    }

    public void SetDynamiteInHandVisible(bool visible)
    {
        if (dynamiteInHand) dynamiteInHand.gameObject.SetActive(visible && CurrentSlot == Slot.Dynamite);
    }

    public bool PlayHeadlampGesture(Action onToggle)
    {
        if (IsSwapping || LeftBusy || !leftArm) return false;
        StartCoroutine(HeadlampRoutine(onToggle));
        return true;
    }

    private IEnumerator HeadlampRoutine(Action onToggle)
    {
        LeftBusy = true;
        bool wasHidden = !leftArm.gameObject.activeSelf;
        if (wasHidden) leftArm.gameObject.SetActive(true);
        Vector3 tapPos = leftRestPos + helmetTapOffset;
        Quaternion tapRot = leftRestRot * Quaternion.Euler(helmetTapEuler);
        for (float t = 0f; t < helmetTapTime; t += Time.deltaTime)
        {
            float k = thrustCurve.Evaluate(t / helmetTapTime);
            leftArm.localPosition = Vector3.LerpUnclamped(leftRestPos, tapPos, k);
            leftArm.localRotation = Quaternion.Slerp(leftRestRot, tapRot, k);
            yield return null;
        }
        onToggle?.Invoke();
        for (float t = 0f; t < helmetTapTime; t += Time.deltaTime)
        {
            float k = returnCurve.Evaluate(t / helmetTapTime);
            leftArm.localPosition = Vector3.LerpUnclamped(tapPos, leftRestPos, k);
            leftArm.localRotation = Quaternion.Slerp(tapRot, leftRestRot, k);
            yield return null;
        }
        leftArm.localPosition = leftRestPos;
        leftArm.localRotation = leftRestRot;
        if (wasHidden) leftArm.gameObject.SetActive(false);
        LeftBusy = false;
    }

    // ------------------------------------------------------------------ vacuum

    public void SetVacuumState(bool held, bool blow)
    {
        vacuumHeld = held;
        vacuumBlow = blow;
    }

    // ------------------------------------------------------------------ swapping and looks

    public bool SwapTo(Slot slot, Action onSwapped)
    {
        if (IsSwapping || slot == CurrentSlot || LeftBusy || RightBusy) return false;
        StartCoroutine(SwapRoutine(slot, onSwapped));
        return true;
    }

    private IEnumerator SwapRoutine(Slot next, Action onSwapped)
    {
        IsSwapping = true;
        windupRatio = -1f;
        for (float t = 0f; t < swapHalfTime; t += Time.deltaTime)
        {
            OffsetSlot(-swapDropDistance * (t / swapHalfTime));
            yield return null;
        }
        CurrentSlot = next;
        ApplySlotVisibility(next, false);
        onSwapped?.Invoke();
        for (float t = 0f; t < swapHalfTime; t += Time.deltaTime)
        {
            OffsetSlot(-swapDropDistance * (1f - t / swapHalfTime));
            yield return null;
        }
        OffsetSlot(0f);
        IsSwapping = false;
    }

    private void OffsetSlot(float y)
    {
        Vector3 d = new Vector3(0f, y, 0f);
        if (CurrentSlot == Slot.Fists || CurrentSlot == Slot.Dynamite)
        {
            if (leftArm) leftArm.localPosition = leftRestPos + d;
            if (rightArm) rightArm.localPosition = rightRestPos + d;
            if (CurrentSlot == Slot.Dynamite && dynamiteInHand) dynamiteInHand.localPosition = dynRestPos + d;
        }
        else if (CurrentSlot == Slot.Vacuum && vacuumRoot)
        {
            vacuumRoot.localPosition = vacRestPos + d;
        }
    }

    private void ApplySlotVisibility(Slot slot, bool immediate)
    {
        bool arms = slot == Slot.Fists || slot == Slot.Dynamite;
        if (leftArm) leftArm.gameObject.SetActive(arms);
        if (rightArm) rightArm.gameObject.SetActive(arms);
        if (vacuumRoot) vacuumRoot.gameObject.SetActive(slot == Slot.Vacuum);
        if (dynamiteInHand) dynamiteInHand.gameObject.SetActive(slot == Slot.Dynamite);
        if (immediate) CurrentSlot = slot;
    }

    public void SetGloveTier(int tier)
    {
        SetTier(leftGloveTiers, tier);
        SetTier(rightGloveTiers, tier);
    }

    public void SetVacuumTier(int tier) => SetTier(vacuumTiers, tier);

    private static void SetTier(GameObject[] tiers, int tier)
    {
        if (tiers == null) return;
        for (int i = 0; i < tiers.Length; i++) if (tiers[i]) tiers[i].SetActive(i == tier);
    }

    private void SetBusy(bool left, bool busy)
    {
        if (left) LeftBusy = busy; else RightBusy = busy;
    }
}
