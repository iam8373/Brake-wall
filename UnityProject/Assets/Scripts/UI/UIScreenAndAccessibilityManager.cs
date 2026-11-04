using System.Collections.Generic;
using BreakToEscape.Core.Performance;
using BreakToEscape.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen navigation (Play HUD, Company Store, Map, Pause, Rewarded-ad offer, Settings, plus the
/// text, code-card and ending overlays) and accessibility settings: left-handed mode (mirrors every
/// thumb control and the look-drag zone), vibration, look sensitivity, reduce flashing, effects
/// intensity, graphics quality override, auto-punch and the debug rate overlay.
/// </summary>
public class UIScreenAndAccessibilityManager : MonoBehaviour
{
    public static UIScreenAndAccessibilityManager Instance { get; private set; }

    [Header("Screens (wireframes 1 to 5, plus Settings)")]
    public GameObject playScreenHUD;
    public GameObject companyStoreScreen;
    public GameObject mapScreen;
    public GameObject pauseScreen;
    public GameObject adOfferPopupScreen;
    public GameObject settingsScreen;
    [Tooltip("Dimmed full-screen backdrop shown behind any open screen or overlay.")]
    public GameObject dimmer;

    [Header("Left-handed mode: every rectangle here is mirrored left/right")]
    public List<RectTransform> mirroredControls = new List<RectTransform>();

    [Header("Settings widgets (optional; kept in sync with the values)")]
    public Toggle leftHandedToggle;
    public Toggle vibrationToggle;
    public Toggle reduceFlashingToggle;
    public Toggle autoPunchToggle;
    public Toggle debugOverlayToggle;
    public Slider lookSensitivitySlider;
    public Slider effectsIntensitySlider;
    public TextMeshProUGUI qualityLabel;
    public TextMeshProUGUI adOfferTitle;
    public TextMeshProUGUI adOfferSubtitle;

    [Header("System references")]
    public MobileFirstPersonController playerController;
    public AudioHapticsManager audioHaptics;
    public MonetizationServicesManager monetization;
    public DeviceTierScaler tierScaler;
    public PunchAction punch;
    public HeadlampController headlamp;
    public DebugRateOverlay debugOverlay;
    public EffectsManager effects;

    public bool LeftHanded { get; private set; }
    public bool VibrationEnabled { get; private set; } = true;
    public bool ReduceFlashing { get; private set; }
    public float LookSensitivity { get; private set; } = 0.16f;
    public float EffectsIntensity { get; private set; } = 1f;
    public int QualityOverride { get; private set; } = -1;
    public bool StoreEverOpened { get; private set; }

    private readonly List<GameObject> openOverlays = new List<GameObject>();
    private readonly List<RectState> defaults = new List<RectState>();
    private string pendingAdPlacement;
    private bool paused;

    private struct RectState
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition;
    }

    void Awake()
    {
        Instance = this;
        foreach (RectTransform rt in mirroredControls)
            defaults.Add(rt ? new RectState { anchorMin = rt.anchorMin, anchorMax = rt.anchorMax, pivot = rt.pivot, anchoredPosition = rt.anchoredPosition } : default);
        foreach (GameObject screen in new[] { companyStoreScreen, mapScreen, pauseScreen, adOfferPopupScreen, settingsScreen, dimmer })
            if (screen) screen.SetActive(false);
        if (playScreenHUD) playScreenHUD.SetActive(true);
    }

    void Update()
    {
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Map)) ToggleMapScreen();
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Pause))
        {
            if (openOverlays.Count > 0) CloseTopOverlay();
            else SetPaused(!paused);
        }
    }

    /// <summary>Applies loaded settings. Called by the bootstrap after the save is read.</summary>
    public void ApplySettings(bool leftHanded, bool vibration, bool reduceFlashing, float lookSensitivity, float effectsIntensity, int qualityOverride)
    {
        SetLeftHandedMode(leftHanded);
        SetVibration(vibration);
        SetReduceFlashing(reduceFlashing);
        SetLookSensitivity(lookSensitivity);
        SetEffectsIntensity(effectsIntensity);
        SetQualityOverride(qualityOverride);
    }

    // ------------------------------------------------------------------ overlays

    public void ShowOverlay(GameObject overlay)
    {
        if (!overlay) return;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (!openOverlays.Contains(overlay)) openOverlays.Add(overlay);
        if (dimmer)
        {
            dimmer.SetActive(true);
            dimmer.transform.SetSiblingIndexBelow(overlay.transform);
        }
        if (playerController) playerController.InputLocked = true;
    }

    public void HideOverlay(GameObject overlay)
    {
        if (!overlay) return;
        overlay.SetActive(false);
        openOverlays.Remove(overlay);
        if (openOverlays.Count == 0)
        {
            if (dimmer) dimmer.SetActive(false);
            if (playerController) playerController.InputLocked = false;
        }
        else if (dimmer)
        {
            dimmer.transform.SetSiblingIndexBelow(openOverlays[openOverlays.Count - 1].transform);
        }
    }

    public void CloseTopOverlay()
    {
        if (openOverlays.Count == 0) return;
        GameObject top = openOverlays[openOverlays.Count - 1];
        if (top == pauseScreen) SetPaused(false);
        else HideOverlay(top);
    }

    public bool AnyOverlayOpen => openOverlays.Count > 0;

    // ------------------------------------------------------------------ screens

    public void OpenCompanyStore()
    {
        StoreEverOpened = true;
        ShowOverlay(companyStoreScreen);
    }

    public void CloseCompanyStore() => HideOverlay(companyStoreScreen);

    public void ToggleMapScreen()
    {
        if (!mapScreen) return;
        if (mapScreen.activeSelf) HideOverlay(mapScreen);
        else ShowOverlay(mapScreen);
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (value) ShowOverlay(pauseScreen);
        else
        {
            HideOverlay(settingsScreen);
            HideOverlay(pauseScreen);
        }
        Time.timeScale = value ? 0f : 1f;
        if (value && GameBootstrap.Instance) GameBootstrap.Instance.SaveNow();
    }

    public void Pause() => SetPaused(true);
    public void Resume() => SetPaused(false);

    public void OpenSettings() => ShowOverlay(settingsScreen);
    public void CloseSettings() => HideOverlay(settingsScreen);

    /// <summary>QUIT TO MENU: there is no separate menu scene yet, so this saves and closes the app on a phone.</summary>
    public void QuitToMenu()
    {
        if (GameBootstrap.Instance) GameBootstrap.Instance.SaveNow();
        Time.timeScale = 1f;
        Application.Quit();
    }

    // ------------------------------------------------------------------ rewarded ad offer (screen 5)

    /// <summary>Opens the equal-weight WATCH AD / NO THANKS card for a placement.</summary>
    public void OpenAdOffer(string placement)
    {
        if (!monetization || !monetization.CanOffer(placement)) return;
        pendingAdPlacement = placement;
        bool dynamite = placement == MonetizationRules.PlacementFreeDynamite;
        if (adOfferTitle) adOfferTitle.text = dynamite ? "Free dynamite crate?" : "Double cash for " + Mathf.RoundToInt(monetization.DoubleCashMinutes) + " minutes?";
        if (adOfferSubtitle) adOfferSubtitle.text = monetization.AdsRemoved ? "Ads are removed. Enjoy, on the house." : "Watch a short ad. Totally optional.";
        ShowOverlay(adOfferPopupScreen);
        if (GameBootstrap.Instance) GameBootstrap.Instance.Analytics(AnalyticsEvents.AdOffered, "placement", placement);
    }

    public void OpenDoubleCashOffer() => OpenAdOffer(MonetizationRules.PlacementDoubleCash);
    public void OpenFreeDynamiteOffer() => OpenAdOffer(MonetizationRules.PlacementFreeDynamite);

    public void ConfirmWatchAdFromPopup()
    {
        HideOverlay(adOfferPopupScreen);
        if (monetization && pendingAdPlacement != null) monetization.RequestReward(pendingAdPlacement);
        pendingAdPlacement = null;
    }

    public void DeclineAdFromPopup()
    {
        HideOverlay(adOfferPopupScreen);
        if (GameBootstrap.Instance && pendingAdPlacement != null) GameBootstrap.Instance.Analytics(AnalyticsEvents.AdDeclined, "placement", pendingAdPlacement);
        pendingAdPlacement = null;
    }

    // ------------------------------------------------------------------ accessibility

    public void ToggleLeftHandedMode() => SetLeftHandedMode(!LeftHanded);

    public void SetLeftHandedMode(bool enabled)
    {
        LeftHanded = enabled;
        for (int i = 0; i < mirroredControls.Count; i++)
        {
            RectTransform rt = mirroredControls[i];
            if (!rt) continue;
            RectState d = defaults[i];
            if (enabled)
            {
                rt.anchorMin = new Vector2(1f - d.anchorMax.x, d.anchorMin.y);
                rt.anchorMax = new Vector2(1f - d.anchorMin.x, d.anchorMax.y);
                rt.pivot = new Vector2(1f - d.pivot.x, d.pivot.y);
                rt.anchoredPosition = new Vector2(-d.anchoredPosition.x, d.anchoredPosition.y);
            }
            else
            {
                rt.anchorMin = d.anchorMin;
                rt.anchorMax = d.anchorMax;
                rt.pivot = d.pivot;
                rt.anchoredPosition = d.anchoredPosition;
            }
        }
        if (playerController) playerController.SetLeftHanded(enabled);
        if (leftHandedToggle) leftHandedToggle.SetIsOnWithoutNotify(enabled);
    }

    public void SetVibration(bool enabled)
    {
        VibrationEnabled = enabled;
        if (audioHaptics) audioHaptics.hapticsEnabled = enabled;
        if (vibrationToggle) vibrationToggle.SetIsOnWithoutNotify(enabled);
    }

    public void ToggleVibration() => SetVibration(!VibrationEnabled);

    public void SetReduceFlashing(bool enabled)
    {
        ReduceFlashing = enabled;
        if (headlamp) headlamp.ReduceFlashing = enabled;
        if (effects) effects.ReduceFlashing = enabled;
        if (reduceFlashingToggle) reduceFlashingToggle.SetIsOnWithoutNotify(enabled);
    }

    public void ToggleReduceFlashing() => SetReduceFlashing(!ReduceFlashing);

    public void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, 0.05f, 0.45f);
        if (playerController) playerController.SetLookSensitivity(LookSensitivity);
        if (lookSensitivitySlider) lookSensitivitySlider.SetValueWithoutNotify(LookSensitivity);
    }

    public void SetEffectsIntensity(float value)
    {
        EffectsIntensity = Mathf.Clamp01(value);
        if (effects) effects.Intensity = EffectsIntensity;
        if (effectsIntensitySlider) effectsIntensitySlider.SetValueWithoutNotify(EffectsIntensity);
    }

    /// <summary>-1 = automatic, 0 = Low, 1 = Mid, 2 = High.</summary>
    public void SetQualityOverride(int tier)
    {
        QualityOverride = Mathf.Clamp(tier, -1, 2);
        if (tierScaler) tierScaler.SetManualOverride(QualityOverride);
        if (qualityLabel) qualityLabel.text = "GRAPHICS: " + (QualityOverride < 0 ? "AUTO (" + (tierScaler ? tierScaler.CurrentTier.ToString().ToUpperInvariant() : "?") + ")" : ((QualityTier)QualityOverride).ToString().ToUpperInvariant());
    }

    /// <summary>Settings button: cycles Auto → Low → Mid → High.</summary>
    public void CycleQuality() => SetQualityOverride(QualityOverride >= 2 ? -1 : QualityOverride + 1);

    public void SetAutoPunch(bool enabled)
    {
        if (punch) punch.AutoPunchEnabled = enabled;
        if (autoPunchToggle) autoPunchToggle.SetIsOnWithoutNotify(enabled);
    }

    public void SetDebugOverlay(bool visible)
    {
        if (debugOverlay) debugOverlay.gameObject.SetActive(visible);
        if (debugOverlayToggle) debugOverlayToggle.SetIsOnWithoutNotify(visible);
    }
}

/// <summary>Small helper to keep the dimmer directly under the top overlay.</summary>
public static class TransformSiblingExtensions
{
    public static void SetSiblingIndexBelow(this Transform t, Transform above)
    {
        if (!t || !above || t.parent != above.parent) return;
        int index = above.GetSiblingIndex();
        int mine = t.GetSiblingIndex();
        t.SetSiblingIndex(mine < index ? index - 1 : index);
    }
}
