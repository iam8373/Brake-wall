using System;
using System.Collections;
using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Limits;
using BreakToEscape.Core.Rubble;
using UnityEngine;

/// <summary>
/// Rubble pool, vacuum suction and blow-out. Owns every <see cref="RubbleCube"/> object and the
/// pure bookkeeping (<see cref="RubblePoolBook"/>, <see cref="TankInventory"/>).
///  - The pool is sized to the device tier's rubble cap (100 / 200 / 400 by default).
///  - At the cap, only settled cubes that are not being vacuumed or crushed are swept, paying the
///    60% refund once.
///  - Suction is limited by a token bucket at the Vacuum Suction table rate, and cubes in flight to
///    the nozzle reserve tank space, so a full tank never swallows (and loses) a cube.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RubbleSuctionManager : MonoBehaviour
{
    public static RubbleSuctionManager Instance { get; private set; }

    [Header("References")]
    public FirstPersonToolController toolController;
    public Transform vacuumNozzlePoint;
    public RubbleCube rubblePrefab;
    public LayerMask rubbleLayerMask;

    [Header("Rubble")]
    [Tooltip("Size of a rubble cube in metres (smaller than a 0.5 m wall cube so piles look chunky but compact).")]
    public float rubbleScale = 0.24f;
    public float popSpeedMin = 2.2f;
    public float popSpeedMax = 4.5f;

    [Header("Vacuum")]
    public float suctionRange = 4.2f;
    public float suctionConeAngle = 40f;
    [Tooltip("Seconds a cube takes along the curve into the nozzle (visual only; the rate limit is the token bucket).")]
    public float suctionCurveDuration = 0.18f;
    [Tooltip("How often the vacuum looks for nearby cubes (seconds).")]
    public float scanInterval = 0.08f;
    public float blowEjectForce = 11f;

    /// <summary>Raised when the vacuum pulls in a cube (tutorial, audio).</summary>
    public event Action CubeVacuumed;
    /// <summary>Raised when a sweep (or a refused spawn) pays a refund.</summary>
    public event Action<long> SweepRefundPaid;
    public event Action TankFull;

    public RubblePoolBook Book { get; private set; }
    public TankInventory Tank { get; private set; }
    public RateMeter VacuumRate { get; } = new RateMeter();

    private RubbleCube[] cubes;
    private EconomyState economy;
    private TokenBucket suctionBucket;
    private readonly Collider[] hits = new Collider[64];
    private readonly List<int> sweptScratch = new List<int>(16);
    private float scanTimer;
    private float blowTimer;
    private bool tankFullReported;
    private bool initialized;

    void Awake()
    {
        Instance = this;
    }

    public void Initialize(EconomyData data, EconomyState economyState, int rubbleCap)
    {
        economy = economyState;
        if (!rubblePrefab) { Debug.LogError("RubbleSuctionManager: rubblePrefab is not assigned; no rubble will spawn.", this); return; }
        if (!toolController) Debug.LogError("RubbleSuctionManager: toolController is not assigned.", this);
        if (!vacuumNozzlePoint) Debug.LogError("RubbleSuctionManager: vacuumNozzlePoint is not assigned.", this);

        int capacity = Mathf.Max(1, rubbleCap);
        Book = new RubblePoolBook(capacity, capacity, data.sweepRefundRatio);
        Tank = new TankInventory(economy.TankCapacity);
        cubes = new RubbleCube[capacity];
        for (int i = 0; i < capacity; i++)
        {
            RubbleCube cube = Instantiate(rubblePrefab, transform);
            cube.name = "Rubble_" + i;
            cube.gameObject.SetActive(false);
            cube.Bind(this, i);
            cubes[i] = cube;
        }
        suctionBucket = new TokenBucket(economy.VacuumCubesPerSecond, BurstFor(economy.VacuumCubesPerSecond));
        initialized = true;
    }

    private static float BurstFor(float rate) => Mathf.Max(1f, rate * 0.25f);

    /// <summary>Re-reads vacuum rate and tank size after upgrades.</summary>
    public void ApplyStats()
    {
        if (!initialized) return;
        suctionBucket.Configure(economy.VacuumCubesPerSecond, BurstFor(economy.VacuumCubesPerSecond));
        Tank.SetCapacity(economy.TankCapacity);
        tankFullReported = false;
    }

    /// <summary>Changes the live cap (thermal guard or a quality change). It cannot exceed the pool size chosen at start-up.</summary>
    public void SetRubbleCap(int cap)
    {
        if (!initialized) return;
        Book.SetCap(cap);
        sweptScratch.Clear();
        long paid = Book.SweepExcess(sweptScratch);
        foreach (int slot in sweptScratch) cubes[slot].Deactivate();
        PayRefund(paid);
    }

    public int PoolSize => initialized ? Book.Capacity : 0;

    // ------------------------------------------------------------------ spawning and returning

    public void SpawnRubbleCube(Vector3 worldPos, Vector3 popNormal, int cashValue, byte material, Color color)
    {
        if (!initialized) return;
        int slot = Book.TrySpawn(cashValue, material, out int swept, out long refund);
        if (swept >= 0) cubes[swept].Deactivate();
        PayRefund(refund);
        if (slot < 0) return;
        Vector3 velocity = (popNormal + UnityEngine.Random.insideUnitSphere * 0.45f).normalized * UnityEngine.Random.Range(popSpeedMin, popSpeedMax);
        cubes[slot].Activate(worldPos, velocity, cashValue, material, color, rubbleScale);
    }

    private void PayRefund(long amount)
    {
        if (amount <= 0) return;
        if (CompanyStoreEconomyManager.Instance) CompanyStoreEconomyManager.Instance.Earn(amount);
        else economy.Earn(amount);
        SweepRefundPaid?.Invoke(amount);
    }

    public void ReportSettled(RubbleCube cube, bool settled)
    {
        if (initialized) Book.SetSettled(cube.Slot, settled);
    }

    /// <summary>Claims a cube for processing (crusher). Fails if it is pooled or already claimed.</summary>
    public bool TryClaim(RubbleCube cube)
    {
        if (!initialized || !Book.BeginProcessing(cube.Slot)) return false;
        cube.HoldKinematic();
        return true;
    }

    /// <summary>Returns a claimed cube to the pool. Safe to call twice; only the first call counts.</summary>
    public bool ReturnCube(RubbleCube cube)
    {
        if (!initialized || !Book.Return(cube.Slot)) return false;
        cube.Deactivate();
        return true;
    }

    public bool IsActive(RubbleCube cube) => initialized && Book.State(cube.Slot) == RubbleState.Active;

    // ------------------------------------------------------------------ vacuum

    void Update()
    {
        if (!initialized || !toolController || !vacuumNozzlePoint) return;
        suctionBucket.Refill(Time.deltaTime);
        VacuumRate.Tick(Time.time);
        bool running = toolController.IsVacuumHeld && toolController.CurrentSlot == FirstPersonToolController.HotbarSlot.Vacuum;
        if (!running) return;
        if (toolController.IsBlowMode) RunBlow();
        else RunSuction();
    }

    private void RunSuction()
    {
        if (Tank.IsFull)
        {
            if (!tankFullReported) { tankFullReported = true; TankFull?.Invoke(); }
            return;
        }
        tankFullReported = false;
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        Transform cam = toolController.playerCamera.transform;
        int count = Physics.OverlapSphereNonAlloc(cam.position + cam.forward * (suctionRange * 0.5f), suctionRange, hits, rubbleLayerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (suctionBucket.Tokens < 1f || Tank.IsFull) break;
            if (!hits[i].TryGetComponent(out RubbleCube cube) || !IsActive(cube)) continue;
            Vector3 to = cube.transform.position - cam.position;
            if (Vector3.Angle(cam.forward, to) > suctionConeAngle) continue;
            if (!Tank.TryReserve()) break;
            if (!Book.BeginProcessing(cube.Slot)) { Tank.CancelReservation(); continue; }
            suctionBucket.TryTake();
            cube.HoldKinematic();
            StartCoroutine(SuctionCurve(cube, cube.Generation));
        }
    }

    private IEnumerator SuctionCurve(RubbleCube cube, int generation)
    {
        Transform cam = toolController.playerCamera.transform;
        Vector3 start = cube.transform.position;
        Vector3 startScale = cube.transform.localScale;
        Vector3 control = Vector3.Lerp(start, cam.position + cam.forward * 1.1f, 0.6f) + Vector3.up * 0.25f;
        for (float elapsed = 0f; elapsed < suctionCurveDuration; elapsed += Time.deltaTime)
        {
            if (cube.Generation != generation) { Tank.CancelReservation(); yield break; }
            float t = elapsed / suctionCurveDuration;
            float u = 1f - t;
            cube.transform.position = u * u * start + 2f * u * t * control + t * t * vacuumNozzlePoint.position;
            cube.transform.Rotate(Vector3.one * (720f * Time.deltaTime), Space.Self);
            cube.transform.localScale = Vector3.Lerp(startScale, startScale * 0.25f, t * t);
            yield return null;
        }
        if (cube.Generation != generation) { Tank.CancelReservation(); yield break; }
        Tank.CompleteReservation(cube.Material, cube.CashValue);
        ReturnCube(cube);
        VacuumRate.Record(Time.time);
        CubeVacuumed?.Invoke();
    }

    private void RunBlow()
    {
        Transform cam = toolController.playerCamera.transform;
        int count = Physics.OverlapSphereNonAlloc(cam.position + cam.forward * 1.8f, 2.0f, hits, rubbleLayerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (hits[i].TryGetComponent(out RubbleCube floorCube) && IsActive(floorCube))
                floorCube.PushWithAir(cam.forward * (blowEjectForce * 0.6f));

        blowTimer -= Time.deltaTime;
        if (blowTimer > 0f || Tank.Count == 0) return;
        // Blow-out uses the same rate as suction so emptying the tank is never faster than filling it.
        if (!suctionBucket.TryTake()) return;
        if (!Book.CanSpawn) return;
        blowTimer = 1f / Mathf.Max(1f, economy.VacuumCubesPerSecond);
        if (!Tank.TryPop(out TankItem item)) return;

        int slot = Book.TrySpawn(item.cashValue, item.material, out int swept, out long refund);
        if (swept >= 0) cubes[swept].Deactivate();
        PayRefund(refund);
        if (slot < 0) return;
        Vector3 velocity = (cam.forward + UnityEngine.Random.insideUnitSphere * 0.08f).normalized * blowEjectForce;
        Color color = MaterialColorFor(item.material);
        cubes[slot].Activate(vacuumNozzlePoint.position + vacuumNozzlePoint.forward * 0.25f, velocity, item.cashValue, item.material, color, rubbleScale);
        tankFullReported = false;
    }

    private static Color MaterialColorFor(byte material)
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        return world && world.Grid != null ? VoxelWorldManager.MaterialColor(world.Grid.materials.Get(material)) : Color.grey;
    }
}
