using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonToolController : MonoBehaviour
{
    public enum HotbarSlot { Fists = 0, Vacuum = 1, Dynamite = 2, Headlamp = 3 }

    [Header("1. Viewmodel Transforms (Landscape Layout)")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform vacuumRoot;
    public Transform dynamiteInHand;
    public Light headlampLight;

    [Header("2. Upgradeable Mesh Tiers")]
    public GameObject[] leftGloveTiers;   // 0 = Basic Glove, 1 = Work Gauntlet
    public GameObject[] rightGloveTiers;
    public GameObject[] vacuumTiers;      // 0 = Dust-Buster, 1 = Mid Vac, 2 = Suction Cannon

    [Header("3. Idle Breathing")]
    public float breatheSpeed = 2.6f;
    public float breatheAmplitude = 0.014f;

    [Header("4. Single-Button Tap vs. Hold-to-Charge Punch")]
    public float tapThreshold = 0.18f;      // Below this = fast tap punch
    public float minChargeTime = 0.30f;     // Minimum hold to count as charged
    public float maxChargeTime = 1.15f;     // Full charge duration
    public Vector3 leftJabOffset = new Vector3(0.22f, 0.04f, 0.58f);
    public Vector3 rightJabOffset = new Vector3(-0.22f, 0.04f, 0.58f);
    public Vector3 chargeWindupOffset = new Vector3(0.12f, -0.08f, -0.26f);
    public Vector3 chargeWindupEuler = new Vector3(-22f, 10f, -14f);
    public Vector3 chargeThrustEuler = new Vector3(12f, -8f, 5f);
    public float jabThrustTime = 0.055f;
    public float jabReturnTime = 0.11f;
    public float chargeShakeAmount = 0.014f;

    [Header("5. Voxel Aim Assist & Damage")]
    public Camera playerCamera;
    public LayerMask voxelWallMask;
    public float punchReach = 2.4f;
    public float aimAssistRadius = 0.22f;   // Snaps to nearest voxel under crosshair
    public float basePunchPower = 1f;
    public float maxChargeMultiplier = 3.5f;

    // Hook your VoxelWorldManager to this event: (worldHitPoint, hitNormal, damagePower, patternTier, isCharged)
    public event Action<Vector3, Vector3, float, int, bool> OnVoxelStrike;

    [Header("6. Vacuum (Suction & Blow Mode)")]
    public float vacuumJitter = 0.006f;
    public int currentTankCount = 0;
    public int maxTankCapacity = 50;
    public bool isBlowMode = false;

    [Header("7. Pooled Sticky Dynamite")]
    public StickyDynamite dynamitePrefab;
    public Transform throwOrigin;
    public GameObject dynamiteGhostPreview; // White wireframe preview on wall
    public int dynamiteStock = 3;
    public int poolSize = 6;
    public float throwSpeed = 16f;
    public float fuseSeconds = 3.0f;

    // Internal state
    private Vector3 leftRestPos, rightRestPos, vacRestPos, dynRestPos;
    private Quaternion leftRestRot, rightRestRot, vacRestRot, dynRestRot;

    private HotbarSlot currentSlot = HotbarSlot.Fists;
    private bool isLeftBusy = false;
    private bool isRightBusy = false;
    private bool isSwapping = false;
    private bool nextJabIsLeft = true;

    // Single-button punch state
    private bool punchButtonHeld = false;
    private float punchHoldTimer = 0f;
    private bool isWindingUp = false;

    // Vacuum state
    private bool isVacuumHeld = false;

    // Object Pool for Dynamite
    private readonly Queue<StickyDynamite> dynamitePool = new Queue<StickyDynamite>();

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;

        if (leftArm) { leftRestPos = leftArm.localPosition; leftRestRot = leftArm.localRotation; }
        if (rightArm) { rightRestPos = rightArm.localPosition; rightRestRot = rightArm.localRotation; }
        if (vacuumRoot) { vacRestPos = vacuumRoot.localPosition; vacRestRot = vacuumRoot.localRotation; }
        if (dynamiteInHand) { dynRestPos = dynamiteInHand.localPosition; dynRestRot = dynamiteInHand.localRotation; }

        InitializeDynamitePool();
        ApplySlotVisibility(HotbarSlot.Fists);
    }

    private void InitializeDynamitePool()
    {
        if (!dynamitePrefab) return;
        for (int i = 0; i < poolSize; i++)
        {
            StickyDynamite stick = Instantiate(dynamitePrefab);
            stick.gameObject.SetActive(false);
            stick.InitializePool(ReturnDynamiteToPool);
            dynamitePool.Enqueue(stick);
        }
    }

    private void ReturnDynamiteToPool(StickyDynamite stick)
    {
        stick.gameObject.SetActive(false);
        dynamitePool.Enqueue(stick);
    }

    void Update()
    {
        // 1. Handle Single-Button Tap vs. Hold-to-Charge Timer
        if (punchButtonHeld && currentSlot == HotbarSlot.Fists && !isSwapping && !isRightBusy)
        {
            punchHoldTimer += Time.deltaTime;
            if (punchHoldTimer >= tapThreshold)
            {
                isWindingUp = true;
                float chargeRatio = Mathf.Clamp01((punchHoldTimer - tapThreshold) / (maxChargeTime - tapThreshold));

                Vector3 windPos = rightRestPos + chargeWindupOffset * chargeRatio;
                Vector3 shake = UnityEngine.Random.insideUnitSphere * (chargeShakeAmount * chargeRatio);
                Quaternion windRot = rightRestRot * Quaternion.Euler(chargeWindupEuler * chargeRatio);

                rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, windPos + shake, Time.deltaTime * 18f);
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, windRot, Time.deltaTime * 18f);
            }
        }

        // 2. Independent Idle Breathing (Non-busy arm keeps bobbing while the other punches)
        if (!isSwapping)
        {
            if (currentSlot == HotbarSlot.Fists || currentSlot == HotbarSlot.Dynamite)
            {
                if (!isLeftBusy && leftArm)
                {
                    ApplyBreathing(leftArm, leftRestPos, leftRestRot, 0f);
                }
                if (!isRightBusy && !isWindingUp && rightArm)
                {
                    ApplyBreathing(rightArm, rightRestPos, rightRestRot, Mathf.PI * 0.5f);
                }
            }

            if (currentSlot == HotbarSlot.Dynamite && dynamiteInHand && !isRightBusy)
            {
                ApplyBreathing(dynamiteInHand, dynRestPos, dynRestRot, Mathf.PI * 0.5f);
                UpdateDynamiteWallGhost();
            }

            // 3. Vacuum Suction / Blow Recoil
            if (currentSlot == HotbarSlot.Vacuum && vacuumRoot)
            {
                if (isVacuumHeld)
                {
                    float kickZ = isBlowMode ? -0.045f : -0.025f;
                    Vector3 jitter = UnityEngine.Random.insideUnitSphere * vacuumJitter;
                    vacuumRoot.localPosition = Vector3.Lerp(vacuumRoot.localPosition, vacRestPos + new Vector3(0f, 0f, kickZ) + jitter, Time.deltaTime * 25f);
                }
                else
                {
                    ApplyBreathing(vacuumRoot, vacRestPos, vacRestRot, 0f);
                }
            }
        }
    }

    private void ApplyBreathing(Transform target, Vector3 restPos, Quaternion restRot, float phaseOffset)
    {
        float y = Mathf.Sin(Time.time * breatheSpeed + phaseOffset) * breatheAmplitude;
        float x = Mathf.Cos(Time.time * breatheSpeed * 0.5f + phaseOffset) * (breatheAmplitude * 0.5f);
        target.localPosition = Vector3.Lerp(target.localPosition, restPos + new Vector3(x, y, 0f), Time.deltaTime * 10f);
        target.localRotation = Quaternion.Slerp(target.localRotation, restRot, Time.deltaTime * 10f);
    }

    // =========================================================================
    // SINGLE PUNCH BUTTON (Bind PointerDown & PointerUp on 1 Landscape Button)
    // =========================================================================

    public void OnPunchButtonDown()
    {
        if (isSwapping) return;

        if (currentSlot == HotbarSlot.Dynamite)
        {
            TryThrowDynamite();
            return;
        }

        if (currentSlot != HotbarSlot.Fists) return;

        punchButtonHeld = true;
        punchHoldTimer = 0f;
        isWindingUp = false;
    }

    public void OnPunchButtonUp()
    {
        if (!punchButtonHeld) return;
        punchButtonHeld = false;

        if (currentSlot != HotbarSlot.Fists || isSwapping) return;

        // Case A: Quick Tap -> Alternating Left / Right Fast Jab
        if (punchHoldTimer < minChargeTime)
        {
            isWindingUp = false;
            FireAlternatingJab();
        }
        // Case B: Held past minimum charge -> Scaled Charged Release
        else
        {
            float chargeRatio = Mathf.Clamp01((punchHoldTimer - tapThreshold) / (maxChargeTime - tapThreshold));
            isWindingUp = false;
            StartCoroutine(ChargedReleaseRoutine(chargeRatio));
        }
    }

    private void FireAlternatingJab()
    {
        if (nextJabIsLeft && !isLeftBusy)
        {
            nextJabIsLeft = false;
            StartCoroutine(JabRoutine(leftArm, true, leftRestPos, leftRestPos + leftJabOffset, leftRestRot));
        }
        else if (!isRightBusy)
        {
            nextJabIsLeft = true;
            StartCoroutine(JabRoutine(rightArm, false, rightRestPos, rightRestPos + rightJabOffset, rightRestRot));
        }
        else if (!isLeftBusy)
        {
            nextJabIsLeft = false;
            StartCoroutine(JabRoutine(leftArm, true, leftRestPos, leftRestPos + leftJabOffset, leftRestRot));
        }
    }

    private IEnumerator JabRoutine(Transform arm, bool isLeft, Vector3 restPos, Vector3 apexPos, Quaternion restRot)
    {
        if (isLeft) isLeftBusy = true; else isRightBusy = true;

        Vector3 startPos = arm.localPosition;
        Quaternion startRot = arm.localRotation;
        Quaternion apexRot = restRot * Quaternion.Euler(6f, isLeft ? 8f : -8f, 0f);

        float t = 0f;
        while (t < jabThrustTime)
        {
            t += Time.deltaTime;
            float lerp = t / jabThrustTime;
            arm.localPosition = Vector3.Lerp(startPos, apexPos, lerp);
            arm.localRotation = Quaternion.Slerp(startRot, apexRot, lerp);
            yield return null;
        }

        ExecuteAimAssistedVoxelHit(basePunchPower, 1, false);

        t = 0f;
        while (t < jabReturnTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.SmoothStep(0f, 1f, t / jabReturnTime);
            arm.localPosition = Vector3.Lerp(apexPos, restPos, lerp);
            arm.localRotation = Quaternion.Slerp(apexRot, restRot, lerp);
            yield return null;
        }

        arm.localPosition = restPos;
        arm.localRotation = restRot;

        if (isLeft) isLeftBusy = false; else isRightBusy = false;
    }

    private IEnumerator ChargedReleaseRoutine(float chargeRatio)
    {
        isRightBusy = true;

        Vector3 startPos = rightArm.localPosition;
        Quaternion startRot = rightArm.localRotation;

        Vector3 scaledApexPos = rightRestPos + Vector3.Lerp(rightJabOffset, new Vector3(-0.28f, 0.08f, 0.78f), chargeRatio);
        Quaternion apexRot = rightRestRot * Quaternion.Euler(chargeThrustEuler * chargeRatio);

        float thrustDuration = 0.05f;
        float t = 0f;
        while (t < thrustDuration)
        {
            t += Time.deltaTime;
            float lerp = t / thrustDuration;
            rightArm.localPosition = Vector3.Lerp(startPos, scaledApexPos, lerp);
            rightArm.localRotation = Quaternion.Slerp(startRot, apexRot, lerp);
            yield return null;
        }

        float scaledDamage = basePunchPower * Mathf.Lerp(1.25f, maxChargeMultiplier, chargeRatio);
        int patternRadius = chargeRatio > 0.65f ? 3 : 2;
        ExecuteAimAssistedVoxelHit(scaledDamage, patternRadius, true);

        yield return new WaitForSeconds(0.055f);

        float returnDuration = 0.22f;
        t = 0f;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.SmoothStep(0f, 1f, t / returnDuration);
            rightArm.localPosition = Vector3.Lerp(scaledApexPos, rightRestPos, lerp);
            rightArm.localRotation = Quaternion.Slerp(apexRot, rightRestRot, lerp);
            yield return null;
        }

        rightArm.localPosition = rightRestPos;
        rightArm.localRotation = rightRestRot;
        isRightBusy = false;
    }

    // =========================================================================
    // AIM ASSIST & VOXEL WALL IMPACT CONNECTION
    // =========================================================================

    private void ExecuteAimAssistedVoxelHit(float damagePower, int patternTier, bool isCharged)
    {
        if (!playerCamera) return;

        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        bool foundTarget = Physics.Raycast(centerRay, out hit, punchReach, voxelWallMask) ||
                           Physics.SphereCast(centerRay, aimAssistRadius, out hit, punchReach, voxelWallMask);

        if (foundTarget)
        {
            OnVoxelStrike?.Invoke(hit.point, hit.normal, damagePower, patternTier, isCharged);
        }
    }

    // =========================================================================
    // 4-SLOT HOTBAR (0 = Fists, 1 = Vacuum, 2 = Dynamite, 3 = Headlamp)
    // =========================================================================

    public void SelectHotbarSlot(int slotIndex)
    {
        HotbarSlot targetSlot = (HotbarSlot)Mathf.Clamp(slotIndex, 0, 3);

        if (targetSlot == HotbarSlot.Headlamp)
        {
            if (!isLeftBusy && !isSwapping) StartCoroutine(HeadlampToggleGestureRoutine());
            return;
        }

        if (targetSlot == currentSlot || isSwapping) return;
        StartCoroutine(HotbarSwapRoutine(targetSlot));
    }

    private IEnumerator HotbarSwapRoutine(HotbarSlot nextSlot)
    {
        isSwapping = true;
        isVacuumHeld = false;
        punchButtonHeld = false;
        isWindingUp = false;
        if (dynamiteGhostPreview) dynamiteGhostPreview.SetActive(false);

        float dropDist = 0.45f;
        float swapTime = 0.13f;

        float t = 0f;
        while (t < swapTime)
        {
            t += Time.deltaTime;
            float lerp = t / swapTime;
            OffsetCurrentSlotVertical(-dropDist * lerp);
            yield return null;
        }

        currentSlot = nextSlot;
        ApplySlotVisibility(currentSlot);

        t = 0f;
        while (t < swapTime)
        {
            t += Time.deltaTime;
            float lerp = 1f - (t / swapTime);
            OffsetCurrentSlotVertical(-dropDist * lerp);
            yield return null;
        }

        OffsetCurrentSlotVertical(0f);
        isSwapping = false;
    }

    private void OffsetCurrentSlotVertical(float yOffset)
    {
        Vector3 delta = new Vector3(0f, yOffset, 0f);
        if (currentSlot == HotbarSlot.Fists)
        {
            if (leftArm) leftArm.localPosition = leftRestPos + delta;
            if (rightArm) rightArm.localPosition = rightRestPos + delta;
        }
        else if (currentSlot == HotbarSlot.Vacuum && vacuumRoot)
        {
            vacuumRoot.localPosition = vacRestPos + delta;
        }
        else if (currentSlot == HotbarSlot.Dynamite)
        {
            if (leftArm) leftArm.localPosition = leftRestPos + delta;
            if (rightArm) rightArm.localPosition = rightRestPos + delta;
            if (dynamiteInHand) dynamiteInHand.localPosition = dynRestPos + delta;
        }
    }

    private void ApplySlotVisibility(HotbarSlot slot)
    {
        bool showArms = (slot == HotbarSlot.Fists || slot == HotbarSlot.Dynamite);
        if (leftArm) leftArm.gameObject.SetActive(showArms);
        if (rightArm) rightArm.gameObject.SetActive(showArms);
        if (vacuumRoot) vacuumRoot.gameObject.SetActive(slot == HotbarSlot.Vacuum);
        if (dynamiteInHand) dynamiteInHand.gameObject.SetActive(slot == HotbarSlot.Dynamite && dynamiteStock > 0);
        if (dynamiteGhostPreview) dynamiteGhostPreview.SetActive(false);
    }

    private IEnumerator HeadlampToggleGestureRoutine()
    {
        isLeftBusy = true;
        bool wasArmHidden = (leftArm && !leftArm.gameObject.activeSelf);
        if (wasArmHidden) leftArm.gameObject.SetActive(true);

        Vector3 helmetTapPos = leftRestPos + new Vector3(0.08f, 0.35f, -0.1f);
        Quaternion helmetTapRot = leftRestRot * Quaternion.Euler(-40f, 20f, 0f);

        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float lerp = t / 0.1f;
            leftArm.localPosition = Vector3.Lerp(leftRestPos, helmetTapPos, lerp);
            leftArm.localRotation = Quaternion.Slerp(leftRestRot, helmetTapRot, lerp);
            yield return null;
        }

        if (headlampLight) headlampLight.enabled = !headlampLight.enabled;

        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float lerp = t / 0.12f;
            leftArm.localPosition = Vector3.Lerp(helmetTapPos, leftRestPos, lerp);
            leftArm.localRotation = Quaternion.Slerp(helmetTapRot, leftRestRot, lerp);
            yield return null;
        }

        leftArm.localPosition = leftRestPos;
        leftArm.localRotation = leftRestRot;
        if (wasArmHidden) leftArm.gameObject.SetActive(false);
        isLeftBusy = false;
    }

    // =========================================================================
    // POOLED STICKY DYNAMITE & WALL GHOST PREVIEW
    // =========================================================================

    private void UpdateDynamiteWallGhost()
    {
        if (!dynamiteGhostPreview || dynamiteStock <= 0 || !playerCamera)
        {
            if (dynamiteGhostPreview) dynamiteGhostPreview.SetActive(false);
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, punchReach * 1.8f, voxelWallMask))
        {
            dynamiteGhostPreview.SetActive(true);
            dynamiteGhostPreview.transform.position = hit.point + hit.normal * 0.03f;
            dynamiteGhostPreview.transform.rotation = Quaternion.LookRotation(hit.normal);
        }
        else
        {
            dynamiteGhostPreview.SetActive(false);
        }
    }

    public void TryThrowDynamite()
    {
        if (isRightBusy || isSwapping || dynamiteStock <= 0 || dynamitePool.Count == 0) return;
        StartCoroutine(DynamiteThrowRoutine());
    }

    private IEnumerator DynamiteThrowRoutine()
    {
        isRightBusy = true;

        Vector3 windPos = rightRestPos + new Vector3(0.12f, 0.18f, -0.25f);
        Quaternion windRot = rightRestRot * Quaternion.Euler(-30f, 0f, 0f);

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float lerp = t / 0.12f;
            rightArm.localPosition = Vector3.Lerp(rightRestPos, windPos, lerp);
            rightArm.localRotation = Quaternion.Slerp(rightRestRot, windRot, lerp);
            yield return null;
        }

        Vector3 releasePos = rightRestPos + new Vector3(-0.15f, -0.05f, 0.45f);
        Quaternion releaseRot = rightRestRot * Quaternion.Euler(20f, 0f, 0f);
        t = 0f;
        while (t < 0.07f)
        {
            t += Time.deltaTime;
            float lerp = t / 0.07f;
            rightArm.localPosition = Vector3.Lerp(windPos, releasePos, lerp);
            rightArm.localRotation = Quaternion.Slerp(windRot, releaseRot, lerp);
            yield return null;
        }

        dynamiteStock--;
        if (dynamiteInHand && dynamiteStock <= 0) dynamiteInHand.gameObject.SetActive(false);

        StickyDynamite stick = dynamitePool.Dequeue();
        Transform origin = throwOrigin ? throwOrigin : playerCamera.transform;
        stick.transform.SetPositionAndRotation(origin.position + origin.forward * 0.35f, origin.rotation);
        stick.gameObject.SetActive(true);
        stick.Launch(origin.forward * throwSpeed, fuseSeconds);

        t = 0f;
        while (t < 0.14f)
        {
            t += Time.deltaTime;
            float lerp = t / 0.14f;
            rightArm.localPosition = Vector3.Lerp(releasePos, rightRestPos, lerp);
            rightArm.localRotation = Quaternion.Slerp(releaseRot, rightRestRot, lerp);
            yield return null;
        }

        rightArm.localPosition = rightRestPos;
        rightArm.localRotation = rightRestRot;
        isRightBusy = false;
    }

    // =========================================================================
    // VACUUM & UPGRADE TIER SWAPPING
    // =========================================================================

    public void SetVacuumButtonHeld(bool held) => isVacuumHeld = held;
    public void ToggleVacuumBlowMode() => isBlowMode = !isBlowMode;

    public void SetGloveUpgradeTier(int tierIndex)
    {
        for (int i = 0; i < leftGloveTiers.Length; i++)
            if (leftGloveTiers[i]) leftGloveTiers[i].SetActive(i == tierIndex);
        for (int i = 0; i < rightGloveTiers.Length; i++)
            if (rightGloveTiers[i]) rightGloveTiers[i].SetActive(i == tierIndex);
    }

    public void SetVacuumUpgradeTier(int tierIndex)
    {
        for (int i = 0; i < vacuumTiers.Length; i++)
            if (vacuumTiers[i]) vacuumTiers[i].SetActive(i == tierIndex);
    }
}
