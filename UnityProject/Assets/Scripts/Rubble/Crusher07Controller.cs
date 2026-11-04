using System;
using System.Collections;
using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.Limits;
using TMPro;
using UnityEngine;

/// <summary>
/// Crusher 07, the floor hatch in the Collapse Chamber. Cubes that fall into its trigger are
/// claimed from the rubble pool (so they can never be swept or paid twice), processed at the
/// Crusher Speed table rate, paid exactly once, and returned. A restored Conveyor pulls nearby
/// cubes in.
/// </summary>
public class Crusher07Controller : MonoBehaviour
{
    public static Crusher07Controller Instance { get; private set; }

    [Header("Roller transforms")]
    public Transform leftRoller;
    public Transform rightRoller;
    public Transform crusherFrame;

    [Header("Roller animation")]
    [Tooltip("Revolutions per minute (15 rpm = 90 degrees per second, the first draft's idle speed).")]
    public float idleRollerRPM = 15f;
    public float activeRollerRPM = 70f;
    public float grindShakeAmount = 0.005f;
    public float grindSeconds = 0.14f;

    [Header("Conveyor (restored equipment)")]
    public float conveyorPullRadius = 2.2f;
    public float conveyorPullSpeed = 2.8f;
    public LayerMask rubbleLayerMask;

    [Header("Floating +$ popups")]
    public TextMeshPro floatingCashPrefab;
    public int popupPoolSize = 15;
    public float popupRiseSpeed = 0.85f;
    public float popupLifetime = 0.9f;

    /// <summary>Base payout of one crushed cube and where it was crushed. Raised exactly once per cube.</summary>
    public static event Action<int, Vector3> CubeCrushed;

    public RateMeter CrushRate { get; } = new RateMeter();

    private struct QueuedCube
    {
        public RubbleCube cube;
        public int generation;
    }

    private readonly Queue<QueuedCube> queue = new Queue<QueuedCube>();
    private readonly Queue<TextMeshPro> popupPool = new Queue<TextMeshPro>();
    private readonly Collider[] conveyorHits = new Collider[32];
    private EconomyState economy;
    private Vector3 frameRestPos;
    private float cooldown;
    private float grindVisualTimer;
    private float conveyorScanTimer;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        if (crusherFrame) frameRestPos = crusherFrame.localPosition;
    }

    public void Initialize(EconomyState economyState)
    {
        economy = economyState;
        mainCam = Camera.main;
        if (floatingCashPrefab)
        {
            for (int i = 0; i < popupPoolSize; i++)
            {
                TextMeshPro popup = Instantiate(floatingCashPrefab, transform.position, Quaternion.identity);
                popup.gameObject.SetActive(false);
                popupPool.Enqueue(popup);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        RubbleSuctionManager rubble = RubbleSuctionManager.Instance;
        if (!rubble || !other.TryGetComponent(out RubbleCube cube)) return;
        if (!rubble.TryClaim(cube)) return;
        queue.Enqueue(new QueuedCube { cube = cube, generation = cube.Generation });
    }

    void Update()
    {
        float rate = economy != null ? economy.CrusherCubesPerSecond : 8f;
        bool grinding = queue.Count > 0 || grindVisualTimer > 0f;
        if (grindVisualTimer > 0f) grindVisualTimer -= Time.deltaTime;
        CrushRate.Tick(Time.time);

        float rpm = grinding ? activeRollerRPM : idleRollerRPM;
        float degrees = rpm * 6f * Time.deltaTime;
        if (leftRoller) leftRoller.Rotate(Vector3.right * degrees, Space.Self);
        if (rightRoller) rightRoller.Rotate(Vector3.left * degrees, Space.Self);
        if (crusherFrame)
        {
            if (grinding)
            {
                Vector3 jitter = UnityEngine.Random.insideUnitSphere * grindShakeAmount;
                jitter.y = 0f;
                crusherFrame.localPosition = frameRestPos + jitter;
            }
            else
            {
                crusherFrame.localPosition = Vector3.Lerp(crusherFrame.localPosition, frameRestPos, Time.deltaTime * 15f);
            }
        }

        cooldown -= Time.deltaTime;
        while (queue.Count > 0 && cooldown <= 0f)
        {
            QueuedCube next = queue.Dequeue();
            if (!next.cube || next.cube.Generation != next.generation) continue;
            cooldown += 1f / Mathf.Max(0.5f, rate);
            StartCoroutine(Grind(next.cube, next.generation));
        }
        if (queue.Count == 0 && cooldown < 0f) cooldown = 0f;

        if (economy != null && economy.ConveyorActive) RunConveyor();
    }

    private void RunConveyor()
    {
        conveyorScanTimer -= Time.deltaTime;
        if (conveyorScanTimer > 0f) return;
        conveyorScanTimer = 0.1f;
        RubbleSuctionManager rubble = RubbleSuctionManager.Instance;
        int count = Physics.OverlapSphereNonAlloc(transform.position, conveyorPullRadius, conveyorHits, rubbleLayerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!conveyorHits[i].TryGetComponent(out RubbleCube cube) || !rubble || !rubble.IsActive(cube)) continue;
            Vector3 dir = (transform.position + Vector3.up * 0.1f - cube.transform.position).normalized;
            cube.Body.WakeUp();
            cube.Body.linearVelocity = dir * conveyorPullSpeed;
        }
    }

    private IEnumerator Grind(RubbleCube cube, int generation)
    {
        grindVisualTimer = 0.25f;
        Vector3 start = cube.transform.position;
        Vector3 target = transform.position + Vector3.down * 0.15f;
        Vector3 startScale = cube.transform.localScale;
        for (float t = 0f; t < grindSeconds; t += Time.deltaTime)
        {
            if (cube.Generation != generation) yield break;
            float k = t / grindSeconds;
            cube.transform.position = Vector3.Lerp(start, target, k) + UnityEngine.Random.insideUnitSphere * 0.015f;
            cube.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, k);
            yield return null;
        }
        if (cube.Generation != generation) yield break;
        int payout = cube.CashValue;
        RubbleSuctionManager rubble = RubbleSuctionManager.Instance;
        if (rubble && rubble.ReturnCube(cube))
        {
            CrushRate.Record(Time.time);
            CubeCrushed?.Invoke(payout, start);
            SpawnPopup(payout, start);
        }
    }

    public void SpawnPopup(long amount, Vector3 worldPos)
    {
        if (popupPool.Count == 0) return;
        TextMeshPro popup = popupPool.Dequeue();
        popup.transform.position = worldPos + new Vector3(UnityEngine.Random.Range(-0.18f, 0.18f), 0.15f, UnityEngine.Random.Range(-0.18f, 0.18f));
        popup.text = "+$" + amount.ToString("N0");
        popup.alpha = 1f;
        popup.gameObject.SetActive(true);
        StartCoroutine(AnimatePopup(popup));
    }

    private IEnumerator AnimatePopup(TextMeshPro popup)
    {
        Vector3 small = Vector3.one * 0.6f;
        for (float elapsed = 0f; elapsed < popupLifetime; elapsed += Time.deltaTime)
        {
            float t = elapsed / popupLifetime;
            popup.transform.position += Vector3.up * (popupRiseSpeed * Time.deltaTime);
            if (mainCam) popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - mainCam.transform.position);
            popup.transform.localScale = Vector3.Lerp(small, Vector3.one, Mathf.Min(1f, t * 4f));
            if (t > 0.5f) popup.alpha = 1f - (t - 0.5f) * 2f;
            yield return null;
        }
        popup.gameObject.SetActive(false);
        popupPool.Enqueue(popup);
    }
}
