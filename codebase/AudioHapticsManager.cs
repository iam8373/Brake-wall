using UnityEngine;

public class AudioHapticsManager : MonoBehaviour
{
    public static AudioHapticsManager Instance { get; private set; }

    [Header("References")]
    public FirstPersonToolController toolController;
    public VoxelWorldManager voxelWorld;

    [Header("Audio Sources")]
    public AudioSource sfxOneShotSource;
    public AudioSource vacuumLoopSource;
    public AudioSource crusherLoopSource;
    public AudioSource ambienceLoopSource;

    [Header("Material-Layered Punch Impact Clips")]
    public AudioClip[] concretePunchClips;
    public AudioClip[] brickPunchClips;
    public AudioClip[] sandstonePunchClips;
    public AudioClip[] granitePunchClips;
    public AudioClip[] steelClangClips;
    public AudioClip chargedPunchSweetenerClip;

    [Header("Tool, Economy & Explosion Clips")]
    public AudioClip dynamiteFuseClip;
    public AudioClip dynamiteBlastClip;
    public AudioClip coinChimeClip;
    public AudioClip treasureRadarPingClip;

    [Header("Vacuum Pitch Scaling (Follows Tank Fill)")]
    public float vacuumBasePitch = 0.85f;
    public float vacuumFullTankPitch = 1.45f;
    public float vacuumBlowModePitch = 0.72f;

    [Header("Android Haptics Settings")]
    public bool hapticsEnabled = true;

    private bool isVacuumSoundActive = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject androidVibrator;
    private AndroidJavaClass vibrationEffectClass;
    private int sdkVersion;
#endif

    void Awake()
    {
        Instance = this;
        InitializeAndroidVibrator();
    }

    void OnEnable()
    {
        if (toolController) toolController.OnVoxelStrike += HandleVoxelStrikeAudioAndHaptics;
        Crusher07Controller.OnCashEarned += HandleCashEarnedAudio;
        StickyDynamite.OnDynamiteDetonated += HandleDynamiteBlastAudioAndHaptics;
    }

    void OnDisable()
    {
        if (toolController) toolController.OnVoxelStrike -= HandleVoxelStrikeAudioAndHaptics;
        Crusher07Controller.OnCashEarned -= HandleCashEarnedAudio;
        StickyDynamite.OnDynamiteDetonated -= HandleDynamiteBlastAudioAndHaptics;
    }

    void Start()
    {
        if (ambienceLoopSource && !ambienceLoopSource.isPlaying)
        {
            ambienceLoopSource.loop = true;
            ambienceLoopSource.Play();
        }
    }

    void Update()
    {
        UpdateVacuumLoopPitch();
    }

    // Bind to your Vacuum UI button PointerDown / PointerUp alongside FirstPersonToolController
    public void SetVacuumAudioActive(bool active)
    {
        isVacuumSoundActive = active;
        if (!vacuumLoopSource) return;

        if (active && !vacuumLoopSource.isPlaying)
            vacuumLoopSource.Play();
        else if (!active && vacuumLoopSource.isPlaying)
            vacuumLoopSource.Stop();
    }

    private void UpdateVacuumLoopPitch()
    {
        if (!isVacuumSoundActive || !vacuumLoopSource || !toolController) return;

        if (toolController.isBlowMode)
        {
            vacuumLoopSource.pitch = Mathf.Lerp(vacuumLoopSource.pitch, vacuumBlowModePitch, Time.deltaTime * 10f);
        }
        else
        {
            float fillRatio = (float)toolController.currentTankCount / Mathf.Max(1, toolController.maxTankCapacity);
            float targetPitch = Mathf.Lerp(vacuumBasePitch, vacuumFullTankPitch, fillRatio);
            vacuumLoopSource.pitch = Mathf.Lerp(vacuumLoopSource.pitch, targetPitch, Time.deltaTime * 10f);
        }
    }

    private void HandleVoxelStrikeAudioAndHaptics(Vector3 hitPoint, Vector3 hitNormal, float damagePower, int patternTier, bool isCharged)
    {
        byte matId = 1;
        if (voxelWorld != null)
        {
            Vector3 localPos = hitPoint - hitNormal * (voxelWorld.voxelSize * 0.45f) - voxelWorld.transform.position;
            int vx = Mathf.FloorToInt(localPos.x / voxelWorld.voxelSize);
            int vy = Mathf.FloorToInt(localPos.y / voxelWorld.voxelSize);
            int vz = Mathf.FloorToInt(localPos.z / voxelWorld.voxelSize);
            matId = voxelWorld.GetVoxelId(vx, vy, vz);
        }

        AudioClip[] pool = GetClipPoolForMaterial(matId);
        if (pool != null && pool.Length > 0 && sfxOneShotSource)
        {
            AudioClip clip = pool[Random.Range(0, pool.Length)];
            sfxOneShotSource.pitch = Random.Range(0.92f, 1.08f);
            sfxOneShotSource.PlayOneShot(clip, isCharged ? 1.0f : 0.78f);

            if (isCharged && chargedPunchSweetenerClip)
            {
                sfxOneShotSource.PlayOneShot(chargedPunchSweetenerClip, 0.9f);
            }
        }

        // Trigger light haptic pulse on tap punch, heavier pulse on charged punch
        if (isCharged)
            TriggerAndroidHaptic(45, 220);
        else
            TriggerAndroidHaptic(18, 110);
    }

    private AudioClip[] GetClipPoolForMaterial(byte matId)
    {
        switch (matId)
        {
            case 1: return concretePunchClips;
            case 2: return brickPunchClips;
            case 3: return sandstonePunchClips;
            case 4: return granitePunchClips;
            case 5: return steelClangClips;
            default: return concretePunchClips;
        }
    }

    private void HandleCashEarnedAudio(int payoutAmount)
    {
        if (!sfxOneShotSource || !coinChimeClip) return;

        // Scale coin chime pitch with payout value ($2 -> 0.95x, $100+ -> 1.45x)
        float normalizedPayout = Mathf.Clamp01(Mathf.Log10(Mathf.Max(1, payoutAmount)) / 2.5f);
        sfxOneShotSource.pitch = Mathf.Lerp(0.95f, 1.45f, normalizedPayout);
        sfxOneShotSource.PlayOneShot(coinChimeClip, 0.75f);
        TriggerAndroidHaptic(10, 80);
    }

    private void HandleDynamiteBlastAudioAndHaptics(Vector3 blastPos, float radius, float dmg)
    {
        if (sfxOneShotSource && dynamiteBlastClip)
        {
            sfxOneShotSource.pitch = Random.Range(0.9f, 1.02f);
            sfxOneShotSource.PlayOneShot(dynamiteBlastClip, 1.0f);
        }
        TriggerAndroidHaptic(90, 255);
    }

    public void PlayRadarPing(float proximityRatio)
    {
        if (!sfxOneShotSource || !treasureRadarPingClip) return;
        sfxOneShotSource.pitch = Mathf.Lerp(0.85f, 1.35f, proximityRatio);
        sfxOneShotSource.PlayOneShot(treasureRadarPingClip, 0.45f);
    }

    // =========================================================================
    // NATIVE ANDROID HAPTICS (API 26+ VibrationEffect)
    // =========================================================================

    private void InitializeAndroidVibrator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                sdkVersion = buildVersion.GetStatic<int>("SDK_INT");
                androidVibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (sdkVersion >= 26)
                {
                    vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
        }
        catch (System.Exception)
        {
            androidVibrator = null;
        }
#endif
    }

    public void TriggerAndroidHaptic(long milliseconds, int amplitude)
    {
        if (!hapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (androidVibrator != null)
        {
            try
            {
                if (sdkVersion >= 26 && vibrationEffectClass != null)
                {
                    int clampedAmp = Mathf.Clamp(amplitude, 1, 255);
                    using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, clampedAmp))
                    {
                        androidVibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    androidVibrator.Call("vibrate", milliseconds);
                }
                return;
            }
            catch (System.Exception) { }
        }
        Handheld.Vibrate();
#endif
    }
}
