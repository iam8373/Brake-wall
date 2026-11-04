using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIScreenAndAccessibilityManager : MonoBehaviour
{
    public static UIScreenAndAccessibilityManager Instance { get; private set; }

    [Header("5 Wireframe Screens (From Touch Screen Wireframes PDF)")]
    public GameObject playScreenHUD;
    public GameObject companyStoreScreen;
    public GameObject mapScreen;
    public GameObject pauseScreen;
    public GameObject adOfferPopupScreen;

    [Header("Left-Handed Mode Transforms (Mirrors Thumb Controls)")]
    public RectTransform moveJoystickRect;
    public RectTransform punchButtonRect;
    public RectTransform vacuumButtonRect;
    public RectTransform blowToggleButtonRect;

    [Header("Accessibility Settings (Section 13 of GDD)")]
    public bool leftHandedMode = false;
    public bool vibrationEnabled = true;
    public bool reduceExplosionFlashing = false;
    [Range(0.05f, 0.45f)] public float lookSensitivity = 0.16f;

    [Header("System References")]
    public MobileFirstPersonController playerController;
    public AudioHapticsManager audioHaptics;
    public CompanyStoreEconomyManager economyManager;
    public MonetizationServicesManager monetizationManager;

    // First-3-Minutes Contextual Dispatch Tutorial (Section 11 of GDD)
    private bool step1PunchDone = false;
    private bool step2VacuumDone = false;
    private bool step3CrusherDone = false;
    private bool step4StoreOpenedDone = false;
    private bool step5UpgradeBoughtDone = false;

    private Vector2 joyDefaultPos, punchDefaultPos, vacDefaultPos, blowDefaultPos;

    void Awake()
    {
        Instance = this;
        if (moveJoystickRect) joyDefaultPos = moveJoystickRect.anchoredPosition;
        if (punchButtonRect) punchDefaultPos = punchButtonRect.anchoredPosition;
        if (vacuumButtonRect) vacDefaultPos = vacuumButtonRect.anchoredPosition;
        if (blowToggleButtonRect) blowDefaultPos = blowToggleButtonRect.anchoredPosition;
    }

    void Start()
    {
        // Step 1 caption on wake-up in Collapse Chamber
        if (economyManager)
        {
            economyManager.ShowTutorialCaption("DISPATCH: "Punch the wall. It has it coming."");
        }
    }

    // =========================================================================
    // CONTEXTUAL FIRST-3-MINUTES TUTORIAL TRIGGERS (Section 11 of GDD)
    // =========================================================================

    public void NotifyFirstWallBroken()
    {
        if (step1PunchDone) return;
        step1PunchDone = true;
        if (economyManager)
            economyManager.ShowTutorialCaption("DISPATCH: "Cubes are cash. Hold VACUUM."");
    }

    public void NotifyFirstCubeVacuumed()
    {
        if (!step1PunchDone || step2VacuumDone) return;
        step2VacuumDone = true;
        if (economyManager)
            economyManager.ShowTutorialCaption("DISPATCH: "Crusher 07 is hungry. Feed it."");
    }

    public void NotifyFirstCubeCrushed()
    {
        if (!step2VacuumDone || step3CrusherDone) return;
        step3CrusherDone = true;
        if (economyManager)
            economyManager.ShowTutorialCaption("DISPATCH: "The Company Store is open. Of course it is."");
    }

    public void NotifyFirstUpgradePurchased()
    {
        if (step5UpgradeBoughtDone) return;
        step5UpgradeBoughtDone = true;
        if (economyManager)
            economyManager.ShowTutorialCaption("DISPATCH: "Dig deeper."");
    }

    // =========================================================================
    // 5 WIREFRAME SCREEN NAVIGATION
    // =========================================================================

    public void OpenCompanyStore()
    {
        step4StoreOpenedDone = true;
        if (companyStoreScreen) companyStoreScreen.SetActive(true);
    }

    public void CloseCompanyStore()
    {
        if (companyStoreScreen) companyStoreScreen.SetActive(false);
    }

    public void ToggleMapScreen()
    {
        if (!mapScreen) return;
        mapScreen.SetActive(!mapScreen.activeSelf);
    }

    public void SetPaused(bool paused)
    {
        if (pauseScreen) pauseScreen.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    // Opens Screen 5 (Equal-weight WATCH AD / NO THANKS popup)
    public void OpenOptionalAdPopup()
    {
        if (!adOfferPopupScreen) return;
        adOfferPopupScreen.SetActive(true);
    }

    public void CloseOptionalAdPopup()
    {
        if (adOfferPopupScreen) adOfferPopupScreen.SetActive(false);
    }

    public void ConfirmWatchAdFromPopup()
    {
        CloseOptionalAdPopup();
        if (monetizationManager)
            monetizationManager.OnTapRewardedAdDoubleCash();
    }

    // =========================================================================
    // ACCESSIBILITY CONTROLS (Left-Handed Mode, Haptics, Sensitivity, Flashing)
    // =========================================================================

    public void ToggleLeftHandedMode()
    {
        SetLeftHandedMode(!leftHandedMode);
    }

    public void SetLeftHandedMode(bool enabled)
    {
        leftHandedMode = enabled;

        // Mirror anchors and X positions between Left and Right thumbs
        MirrorRectHorizontally(moveJoystickRect, joyDefaultPos, enabled, true);
        MirrorRectHorizontally(punchButtonRect, punchDefaultPos, enabled, false);
        MirrorRectHorizontally(vacuumButtonRect, vacDefaultPos, enabled, false);
        MirrorRectHorizontally(blowToggleButtonRect, blowDefaultPos, enabled, false);
    }

    private void MirrorRectHorizontally(RectTransform rt, Vector2 defaultPos, bool mirrored, bool defaultIsLeft)
    {
        if (!rt) return;
        float anchorX = mirrored ? (defaultIsLeft ? 1f : 0f) : (defaultIsLeft ? 0f : 1f);
        rt.anchorMin = new Vector2(anchorX, rt.anchorMin.y);
        rt.anchorMax = new Vector2(anchorX, rt.anchorMax.y);
        rt.anchoredPosition = new Vector2(mirrored ? -defaultPos.x : defaultPos.x, defaultPos.y);
    }

    public void ToggleVibration()
    {
        vibrationEnabled = !vibrationEnabled;
        if (audioHaptics) audioHaptics.hapticsEnabled = vibrationEnabled;
    }

    public void ToggleReduceFlashing()
    {
        reduceExplosionFlashing = !reduceExplosionFlashing;
    }

    public void SetLookSensitivity(float value)
    {
        lookSensitivity = Mathf.Clamp(value, 0.05f, 0.45f);
        if (playerController) playerController.lookSensitivity = lookSensitivity;
    }
}
