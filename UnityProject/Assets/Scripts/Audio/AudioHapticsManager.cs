using System.Collections.Generic;
using BreakToEscape.Core.Json;
using UnityEngine;

/// <summary>
/// Sound and haptics. Clips are loaded by name from Resources/Audio using the mapping in
/// Resources/Data/audio.json, so sounds can be added without code changes. Every clip slot may be
/// empty: a missing clip is skipped silently after one warning (no sound files ship yet; see
/// docs/ASSET_REQUIREMENTS.md). The punch sound uses the material that was struck, read before the
/// damage is applied. Haptics use the Android vibrator service (VibratorManager on Android 12+).
/// </summary>
public class AudioHapticsManager : MonoBehaviour
{
    public static AudioHapticsManager Instance { get; private set; }

    [Header("References")]
    public FirstPersonToolController toolController;
    public PunchAction punch;
    public RubbleSuctionManager suction;

    [Header("Audio sources")]
    public AudioSource sfxOneShotSource;
    public AudioSource vacuumLoopSource;
    public AudioSource crusherLoopSource;
    public AudioSource ambienceLoopSource;
    public AudioSource musicSource;

    [Header("Vacuum pitch (follows tank fill)")]
    public float vacuumBasePitch = 0.85f;
    public float vacuumFullTankPitch = 1.45f;
    public float vacuumBlowModePitch = 0.72f;

    [Header("Haptics")]
    public bool hapticsEnabled = true;

    private readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    private readonly Dictionary<int, string> materialPunchKeys = new Dictionary<int, string>();
    private readonly HashSet<string> warned = new HashSet<string>();
    private float crusherActiveTimer;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
    private AndroidJavaClass vibrationEffect;
    private int sdkInt;
#endif

    void Awake()
    {
        Instance = this;
        LoadClipTable();
        InitVibrator();
    }

    void OnEnable()
    {
        if (punch) punch.Struck += OnStruck;
        Crusher07Controller.CubeCrushed += OnCrushed;
        StickyDynamite.Detonated += OnDetonated;
        if (toolController) toolController.VacuumHeldChanged += SetVacuumAudioActive;
    }

    void OnDisable()
    {
        if (punch) punch.Struck -= OnStruck;
        Crusher07Controller.CubeCrushed -= OnCrushed;
        StickyDynamite.Detonated -= OnDetonated;
        if (toolController) toolController.VacuumHeldChanged -= SetVacuumAudioActive;
    }

    void Start()
    {
        StartLoop(ambienceLoopSource, "ambience");
        StartLoop(musicSource, "music");
    }

    private void LoadClipTable()
    {
        TextAsset table = Resources.Load<TextAsset>("Data/audio");
        if (!table)
        {
            Debug.LogWarning("AudioHapticsManager: Resources/Data/audio.json not found; the game will be silent.");
            return;
        }
        var root = JsonRead.Obj(MiniJson.Parse(table.text), "audio.json");
        var events = JsonRead.OptObj(root, "events");
        if (events != null)
        {
            foreach (var kv in events)
            {
                var names = kv.Value as List<object>;
                if (names == null) continue;
                var loaded = new List<AudioClip>();
                foreach (object n in names)
                {
                    if (!(n is string clipName)) continue;
                    AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
                    if (clip) loaded.Add(clip);
                }
                clips[kv.Key] = loaded.ToArray();
            }
        }
        var materials = JsonRead.OptObj(root, "punchByMaterial");
        if (materials != null)
            foreach (var kv in materials)
                if (int.TryParse(kv.Key, out int id) && kv.Value is string key) materialPunchKeys[id] = key;
    }

    private AudioClip Pick(string eventKey)
    {
        if (clips.TryGetValue(eventKey, out AudioClip[] list) && list.Length > 0) return list[Random.Range(0, list.Length)];
        if (warned.Add(eventKey)) Debug.Log("AudioHapticsManager: no clip for '" + eventKey + "' yet (placeholder silence).");
        return null;
    }

    private void PlayOneShot(string eventKey, float volume, float pitch)
    {
        AudioClip clip = Pick(eventKey);
        if (!clip || !sfxOneShotSource) return;
        sfxOneShotSource.pitch = pitch;
        sfxOneShotSource.PlayOneShot(clip, volume);
    }

    private void StartLoop(AudioSource source, string eventKey)
    {
        if (!source) return;
        AudioClip clip = Pick(eventKey);
        if (!clip) return;
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    void Update()
    {
        UpdateVacuumPitch();
        if (crusherActiveTimer > 0f)
        {
            crusherActiveTimer -= Time.deltaTime;
            if (crusherActiveTimer <= 0f && crusherLoopSource && crusherLoopSource.isPlaying) crusherLoopSource.Stop();
        }
    }

    public void SetVacuumAudioActive(bool active)
    {
        if (!vacuumLoopSource) return;
        if (active && !vacuumLoopSource.isPlaying)
        {
            AudioClip clip = Pick("vacuum_loop");
            if (!clip) return;
            vacuumLoopSource.clip = clip;
            vacuumLoopSource.loop = true;
            vacuumLoopSource.Play();
        }
        else if (!active && vacuumLoopSource.isPlaying)
        {
            vacuumLoopSource.Stop();
        }
    }

    private void UpdateVacuumPitch()
    {
        if (!vacuumLoopSource || !vacuumLoopSource.isPlaying || !toolController) return;
        float target = vacuumBasePitch;
        if (toolController.IsBlowMode) target = vacuumBlowModePitch;
        else if (suction && suction.Tank != null) target = Mathf.Lerp(vacuumBasePitch, vacuumFullTankPitch, (float)suction.Tank.Count / Mathf.Max(1, suction.Tank.Capacity));
        vacuumLoopSource.pitch = Mathf.Lerp(vacuumLoopSource.pitch, target, Time.deltaTime * 10f);
    }

    private void OnStruck(PunchStrike strike)
    {
        string key = materialPunchKeys.TryGetValue(strike.material, out string k) ? k : "punch_concrete";
        PlayOneShot(key, strike.charged ? 1f : 0.78f, Random.Range(0.92f, 1.08f));
        if (strike.charged) PlayOneShot("punch_charged", 0.9f, 1f);
        if (strike.charged || strike.cubesBroken >= 10) Vibrate(45, 220);
        else Vibrate(18, 110);
    }

    private void OnCrushed(int payout, Vector3 position)
    {
        float normalized = Mathf.Clamp01(Mathf.Log10(Mathf.Max(1, payout)) / 2.5f);
        PlayOneShot("coin", 0.75f, Mathf.Lerp(0.95f, 1.45f, normalized));
        if (crusherLoopSource && !crusherLoopSource.isPlaying)
        {
            AudioClip clip = Pick("crusher_loop");
            if (clip) { crusherLoopSource.clip = clip; crusherLoopSource.loop = true; crusherLoopSource.Play(); }
        }
        crusherActiveTimer = 0.6f;
        Vibrate(10, 80);
    }

    private void OnDetonated(Vector3 position, float radius, float damage)
    {
        PlayOneShot("dynamite_blast", 1f, Random.Range(0.9f, 1.02f));
        Vibrate(90, 255);
    }

    public void PlayRadarPing(float proximity) => PlayOneShot("radar_ping", 0.45f, Mathf.Lerp(0.85f, 1.35f, proximity));
    public void PlayUiClick() => PlayOneShot("ui_click", 0.6f, 1f);

    // ------------------------------------------------------------------ haptics

    private void InitVibrator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdkInt = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (sdkInt >= 31)
                {
                    using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                        vibrator = manager != null ? manager.Call<AndroidJavaObject>("getDefaultVibrator") : null;
                }
                else
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            if (sdkInt >= 26) vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("AudioHapticsManager: vibrator unavailable: " + e.Message);
            vibrator = null;
        }
#endif
    }

    /// <summary>Short vibration. Amplitude 1..255 (ignored on devices without amplitude control).</summary>
    public void Vibrate(long milliseconds, int amplitude)
    {
        if (!hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null) return;
        try
        {
            if (vibrationEffect != null)
            {
                using (AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255)))
                    vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception)
        {
            hapticsEnabled = false;
        }
#endif
    }
}
