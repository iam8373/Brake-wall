using System.Collections.Generic;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Prototype effects built in code (no assets needed): dust in the material's colour on punches,
/// dynamite explosion and smoke, crusher sparks, a coin burst on payouts and slow cave dust motes
/// around the player. One particle system per effect, emitted with EmitParams (pooled by design,
/// nothing is instantiated during play). Intensity scales particle counts; Reduce Flashing removes
/// the explosion flash.
/// </summary>
public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    public PunchAction punch;
    public Transform player;
    [Tooltip("Particle material (URP Particles/Unlit or the game's unlit vertex-colour material).")]
    public Material particleMaterial;
    public Light flashLight;

    public float Intensity { get; set; } = 1f;
    public bool ReduceFlashing { get; set; }

    private ParticleSystem dust, smoke, sparks, coins, motes;
    private float flashTimer;

    void Awake()
    {
        Instance = this;
        dust = Create("PunchDust", 600, 0.6f, 0.08f, 0.5f);
        smoke = Create("DynamiteSmoke", 200, 2.2f, 0.6f, -0.05f);
        sparks = Create("CrusherSparks", 200, 0.35f, 0.04f, 1.0f);
        coins = Create("CoinBurst", 150, 0.7f, 0.07f, 0.8f);
        motes = Create("CaveDustMotes", 120, 6f, 0.03f, 0f);
        if (flashLight) flashLight.enabled = false;
        if (!particleMaterial) Debug.LogError("EffectsManager: particleMaterial is not assigned; effects will render with the default material.", this);
    }

    void OnEnable()
    {
        if (punch) punch.Struck += OnStruck;
        StickyDynamite.Detonated += OnBlast;
        Crusher07Controller.CubeCrushed += OnCrushed;
    }

    void OnDisable()
    {
        if (punch) punch.Struck -= OnStruck;
        StickyDynamite.Detonated -= OnBlast;
        Crusher07Controller.CubeCrushed -= OnCrushed;
    }

    private ParticleSystem Create(string name, int max, float lifetime, float size, float gravity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.maxParticles = max;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.gravityModifier = gravity;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        if (r && particleMaterial) r.sharedMaterial = particleMaterial;
        return ps;
    }

    private int Count(int baseCount) => Mathf.Max(0, Mathf.RoundToInt(baseCount * Intensity));

    private void Burst(ParticleSystem ps, Vector3 position, Vector3 direction, float speed, float spread, Color color, int count, float size)
    {
        var p = new ParticleSystem.EmitParams { startColor = color, startSize = size, applyShapeToPosition = false };
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = (direction + Random.insideUnitSphere * spread).normalized;
            p.position = position + Random.insideUnitSphere * 0.1f;
            p.velocity = dir * speed * Random.Range(0.5f, 1f);
            ps.Emit(p, 1);
        }
    }

    private void OnStruck(PunchStrike strike)
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!world) return;
        Color c = VoxelWorldManager.MaterialColor(world.Grid.materials.Get(strike.material));
        int count = Count(6 + strike.cubesBroken / 2 + (strike.charged ? 12 : 0));
        Burst(dust, strike.worldPoint, VoxelWorldManager.ToVector3(strike.normal), strike.charged ? 3f : 1.8f, 0.8f, c, count, 0.08f);
        if (CameraEffects.Instance)
        {
            CameraEffects.Instance.Shake(strike.charged ? 0.12f + 0.1f * strike.chargeRatio : 0.04f);
            if (strike.charged) CameraEffects.Instance.KickFov(4f * strike.chargeRatio);
        }
    }

    private void OnBlast(Vector3 position, float radius, float damage)
    {
        Burst(smoke, position, Vector3.up, 1.2f, 1.2f, new Color(0.35f, 0.33f, 0.3f, 0.8f), Count(40), 0.6f);
        Burst(dust, position, Vector3.up, 5f, 1.5f, new Color(1f, 0.7f, 0.3f), Count(60), 0.1f);
        if (flashLight && !ReduceFlashing)
        {
            flashLight.transform.position = position;
            flashLight.enabled = true;
            flashTimer = 0.12f;
        }
        if (CameraEffects.Instance) CameraEffects.Instance.Shake(ReduceFlashing ? 0.2f : 0.45f);
    }

    private void OnCrushed(int payout, Vector3 position)
    {
        Burst(sparks, position, Vector3.up, 2.5f, 0.9f, new Color(1f, 0.8f, 0.3f), Count(5), 0.04f);
        Burst(coins, position + Vector3.up * 0.2f, Vector3.up, 1.6f, 0.6f, new Color(1f, 0.85f, 0.2f), Count(Mathf.Clamp(payout / 5, 1, 12)), 0.07f);
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && flashLight) flashLight.enabled = false;
        }
        if (player && motes && Random.value < 0.3f * Intensity)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = player.position + new Vector3(Random.Range(-4f, 4f), Random.Range(0f, 3f), Random.Range(-4f, 4f)),
                velocity = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.03f, 0.02f), Random.Range(-0.05f, 0.05f)),
                startColor = new Color(0.9f, 0.85f, 0.7f, 0.35f),
                startSize = 0.03f,
                applyShapeToPosition = false,
            };
            motes.Emit(p, 1);
        }
    }
}
