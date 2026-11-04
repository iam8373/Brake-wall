using BreakToEscape.Core.Economy;
using UnityEngine;

/// <summary>
/// Headlamp: three levels (range, intensity, cone) bought in the Company Store, read from
/// economy.json. A small flicker keeps it feeling like an old mine lamp; Reduce Flashing turns
/// the flicker off.
/// </summary>
public class HeadlampController : MonoBehaviour
{
    public Light lamp;
    [Range(0f, 0.3f)] public float flickerAmount = 0.06f;
    public float flickerSpeed = 7f;

    public bool IsOn => lamp && lamp.enabled;
    public bool ReduceFlashing { get; set; }

    private float baseIntensity = 1.6f;

    void Awake()
    {
        if (!lamp) Debug.LogError("HeadlampController: lamp is not assigned.", this);
    }

    public void ApplyLevel(HeadlampLevel level)
    {
        if (!lamp || level == null) return;
        lamp.range = level.rangeMeters;
        lamp.spotAngle = level.spotAngle;
        baseIntensity = level.intensity;
        lamp.intensity = baseIntensity;
    }

    public void Toggle()
    {
        if (lamp) lamp.enabled = !lamp.enabled;
    }

    public void SetOn(bool on)
    {
        if (lamp) lamp.enabled = on;
    }

    void Update()
    {
        if (!lamp || !lamp.enabled) return;
        float flicker = ReduceFlashing ? 0f : (Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f) - 0.5f) * 2f * flickerAmount;
        lamp.intensity = baseIntensity * (1f + flicker);
    }
}
