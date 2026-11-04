using UnityEngine;

public class DeviceTierScaler : MonoBehaviour
{
    public enum MobileQualityTier { Low = 0, Mid = 1, High = 2 }

    public static DeviceTierScaler Instance { get; private set; }

    [Header("Detected Hardware Tier")]
    public MobileQualityTier currentTier = MobileQualityTier.Mid;

    [Header("References")]
    public RubbleSuctionManager rubbleManager;
    public Camera mainCamera;

    [Header("Thermal & Frame Rate Auto-Guard")]
    public bool enableThermalFrameDropGuard = true;
    public float fpsCheckInterval = 5.0f;
    public float minAcceptable60FpsAverage = 48.0f;

    private int frameCounter = 0;
    private float fpsTimer = 0f;
    private bool lockedTo30ByThermalGuard = false;

    void Awake()
    {
        Instance = this;
        DetectHardwareTier();
        ApplyQualityTierSettings(currentTier);
    }

    private void DetectHardwareTier()
    {
        int ramMB = SystemInfo.systemMemorySize;
        int cores = SystemInfo.processorCount;
        int vramMB = SystemInfo.graphicsMemorySize;

        // Classify Android hardware into Low, Mid (Snapdragon 6-series class), or High tier
        if (ramMB < 4200 || cores <= 4 || vramMB < 1024)
        {
            currentTier = MobileQualityTier.Low;
        }
        else if (ramMB >= 7500 && cores >= 8)
        {
            currentTier = MobileQualityTier.High;
        }
        else
        {
            currentTier = MobileQualityTier.Mid;
        }
    }

    public void ApplyQualityTierSettings(MobileQualityTier tier)
    {
        currentTier = tier;

        switch (tier)
        {
            case MobileQualityTier.Low:
                Application.targetFrameRate = 30;
                Time.fixedDeltaTime = 1f / 30f; // 30 Hz physics step on low-end phones
                if (rubbleManager) rubbleManager.maxActiveRubble = 200;
                if (mainCamera) mainCamera.farClipPlane = 22f;
                QualitySettings.shadows = ShadowQuality.Disable;
                break;

            case MobileQualityTier.Mid:
                Application.targetFrameRate = 60;
                Time.fixedDeltaTime = 1f / 60f;
                if (rubbleManager) rubbleManager.maxActiveRubble = 350;
                if (mainCamera) mainCamera.farClipPlane = 32f;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                break;

            case MobileQualityTier.High:
                Application.targetFrameRate = 60;
                Time.fixedDeltaTime = 1f / 60f;
                if (rubbleManager) rubbleManager.maxActiveRubble = 500;
                if (mainCamera) mainCamera.farClipPlane = 45f;
                QualitySettings.shadows = ShadowQuality.All;
                break;
        }
    }

    void Update()
    {
        if (!enableThermalFrameDropGuard || currentTier == MobileQualityTier.Low || lockedTo30ByThermalGuard)
            return;

        frameCounter++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= fpsCheckInterval)
        {
            float avgFps = frameCounter / fpsTimer;
            frameCounter = 0;
            fpsTimer = 0f;

            // As noted in Section 3 of the Architecture Doc: if a Mid-tier phone cannot hold 60 fps due to thermals, lock to 30 fps
            if (avgFps < minAcceptable60FpsAverage)
            {
                lockedTo30ByThermalGuard = true;
                Application.targetFrameRate = 30;
                Time.fixedDeltaTime = 1f / 45f;
            }
        }
    }
}
