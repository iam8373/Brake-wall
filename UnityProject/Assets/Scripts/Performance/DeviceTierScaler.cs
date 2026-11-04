using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Performance;
using BreakToEscape.Core.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Device tier detection and quality settings. This is the single owner of
/// Application.targetFrameRate. Per tier (values from economy.json "tiers"):
///  - rubble cap (100 / 200 / 400) and pool size,
///  - frame target and physics step,
///  - URP render scale and shadow distance on the active URP asset, the main light's shadows,
///  - camera far clip.
/// If the project has quality levels named "Low", "Mid" and "High" (each with its own URP asset,
/// see README_UNITY_SETUP.md), the matching level is selected first.
/// The thermal guard lowers the rubble cap first and only then drops 60 fps to 30 fps.
/// </summary>
[DefaultExecutionOrder(-400)]
public class DeviceTierScaler : MonoBehaviour
{
    public static DeviceTierScaler Instance { get; private set; }

    [Header("References")]
    public RubbleSuctionManager rubbleManager;
    public Camera mainCamera;
    public Light mainLight;

    [Header("Thermal and frame-rate guard")]
    public bool enableThermalGuard = true;

    public QualityTier DetectedTier { get; private set; } = QualityTier.Mid;
    public QualityTier CurrentTier { get; private set; } = QualityTier.Mid;
    public int TargetFps { get; private set; } = 60;
    public int RubbleCap { get; private set; } = 200;

    private EconomyData data;
    private int frames;
    private float elapsed;
    private bool manualOverride;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Detects the tier and applies it. Returns the rubble cap to size the pool with.</summary>
    public int Initialize(EconomyData economyData, int savedOverride)
    {
        data = economyData;
        int ram = SystemInfo.systemMemorySize, cores = SystemInfo.processorCount, gpu = SystemInfo.graphicsMemorySize;
        DetectedTier = DeviceTierPolicy.Classify(ram, cores, gpu, data.tierDetection);
        Debug.Log("DeviceTierScaler: RAM " + ram + " MB, " + cores + " cores, GPU memory " + gpu + " MB (" + SystemInfo.graphicsDeviceName + ") → " + DetectedTier);
        manualOverride = savedOverride >= 0;
        Apply(manualOverride ? (QualityTier)savedOverride : DetectedTier);
        if (GameBootstrap.Instance)
            GameBootstrap.Instance.Analytics(AnalyticsEvents.QualityTier, "tier", CurrentTier.ToString(), "ramMb", ram, "cores", cores, "graphicsMb", gpu, "manualOverride", manualOverride);
        return RubbleCap;
    }

    /// <summary>Settings screen: -1 = automatic, 0..2 = Low/Mid/High. A higher rubble cap than the pool size takes effect after a restart.</summary>
    public void SetManualOverride(int tier)
    {
        if (data == null) return;
        manualOverride = tier >= 0;
        Apply(manualOverride ? (QualityTier)Mathf.Clamp(tier, 0, 2) : DetectedTier);
    }

    public void Apply(QualityTier tier)
    {
        CurrentTier = tier;
        TierSettings t = data.Tier(DeviceTierPolicy.Key(tier));

        SelectQualityLevel(tier);
        TargetFps = t.targetFps;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;
        Time.fixedDeltaTime = 1f / Mathf.Max(20, t.physicsHz);
        RubbleCap = t.rubbleCap;
        if (rubbleManager) rubbleManager.SetRubbleCap(RubbleCap);
        if (mainCamera) mainCamera.farClipPlane = t.farClip;
        if (mainLight) mainLight.shadows = t.mainLightShadows ? LightShadows.Soft : LightShadows.None;

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp)
        {
            urp.renderScale = t.renderScale;
            urp.shadowDistance = t.shadowDistance;
        }
        else
        {
            Debug.LogWarning("DeviceTierScaler: no URP asset is active; render scale and shadow distance were not applied.");
        }
        frames = 0;
        elapsed = 0f;
    }

    private static void SelectQualityLevel(QualityTier tier)
    {
        string wanted = tier.ToString();
        string[] names = QualitySettings.names;
        if (names == null) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                if (QualitySettings.GetQualityLevel() != i) QualitySettings.SetQualityLevel(i, true);
                return;
            }
        }
    }

    void Update()
    {
        if (!enableThermalGuard || data == null) return;
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < data.thermalGuard.checkIntervalSeconds) return;
        float average = frames / elapsed;
        frames = 0;
        elapsed = 0f;
        if (Time.timeScale == 0f) return;

        GuardAction action = DeviceTierPolicy.Evaluate(average, TargetFps, RubbleCap, data.thermalGuard, out int newCap);
        if (action == GuardAction.LowerRubbleCap)
        {
            RubbleCap = newCap;
            if (rubbleManager) rubbleManager.SetRubbleCap(RubbleCap);
            Debug.Log("DeviceTierScaler: average " + average.ToString("0") + " fps, rubble cap lowered to " + RubbleCap);
        }
        else if (action == GuardAction.DropFrameTarget)
        {
            TargetFps = 30;
            Application.targetFrameRate = 30;
            Debug.Log("DeviceTierScaler: average " + average.ToString("0") + " fps, frame target dropped to 30");
        }
    }
}
