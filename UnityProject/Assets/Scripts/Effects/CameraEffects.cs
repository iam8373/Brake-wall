using UnityEngine;

/// <summary>
/// Camera shake (decaying noise), FOV kick on charged punches and a walking bob, applied as an
/// offset on the camera's local transform. Each has an intensity; the shared effects intensity from
/// Settings scales all three, and Reduce Flashing also halves shake.
/// </summary>
public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance { get; private set; }

    public Camera targetCamera;
    public MobileFirstPersonController player;

    [Header("Shake")]
    [Range(0f, 2f)] public float shakeIntensity = 1f;
    public float shakeDecay = 4f;
    public float shakeFrequency = 22f;
    public float maxShakeMeters = 0.05f;

    [Header("FOV kick")]
    [Range(0f, 2f)] public float fovKickIntensity = 1f;
    public float fovReturnSpeed = 6f;

    [Header("Walking bob")]
    [Range(0f, 2f)] public float bobIntensity = 1f;
    public float bobAmplitude = 0.035f;
    public float bobFrequency = 1.9f;

    private Vector3 restLocalPos;
    private float baseFov;
    private float trauma;
    private float fovOffset;
    private float bobPhase;

    void Awake()
    {
        Instance = this;
        if (!targetCamera) targetCamera = GetComponent<Camera>();
        restLocalPos = transform.localPosition;
        if (targetCamera) baseFov = targetCamera.fieldOfView;
    }

    private float Global => UIScreenAndAccessibilityManager.Instance ? UIScreenAndAccessibilityManager.Instance.EffectsIntensity : 1f;
    private bool Reduced => UIScreenAndAccessibilityManager.Instance && UIScreenAndAccessibilityManager.Instance.ReduceFlashing;

    /// <summary>Adds shake (0..1 trauma).</summary>
    public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    /// <summary>Widens the field of view briefly by up to <paramref name="degrees"/>.</summary>
    public void KickFov(float degrees) => fovOffset = Mathf.Max(fovOffset, degrees);

    void LateUpdate()
    {
        float g = Global;
        Vector3 offset = Vector3.zero;

        if (trauma > 0f)
        {
            float s = trauma * trauma * maxShakeMeters * shakeIntensity * g * (Reduced ? 0.5f : 1f);
            float t = Time.time * shakeFrequency;
            offset += new Vector3((Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f * s, (Mathf.PerlinNoise(0.2f, t) - 0.5f) * 2f * s, 0f);
            trauma = Mathf.Max(0f, trauma - shakeDecay * Time.deltaTime);
        }

        if (player && player.IsGrounded)
        {
            float speed = player.HorizontalVelocity.magnitude;
            if (speed > 0.1f)
            {
                bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f * Mathf.Clamp01(speed / Mathf.Max(0.1f, player.moveSpeed));
                float a = bobAmplitude * bobIntensity * g;
                offset += new Vector3(Mathf.Cos(bobPhase * 0.5f) * a * 0.5f, Mathf.Sin(bobPhase) * a, 0f);
            }
        }
        transform.localPosition = restLocalPos + offset;

        if (targetCamera)
        {
            fovOffset = Mathf.MoveTowards(fovOffset, 0f, fovReturnSpeed * Time.deltaTime);
            targetCamera.fieldOfView = baseFov + fovOffset * fovKickIntensity * g;
        }
    }
}
