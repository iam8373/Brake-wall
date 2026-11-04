using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubbleSuctionManager : MonoBehaviour
{
    public static RubbleSuctionManager Instance { get; private set; }

    [Header("References")]
    public FirstPersonToolController toolController;
    public Transform vacuumNozzlePoint;
    public RubbleCube rubblePrefab;

    [Header("Mobile Device Tier Rubble Cap (100 Low / 150-250 Mid / 400 High - Technical Budget A3)")]
    public int maxActiveRubble = 150;
    public float sweepCashRefundRatio = 0.60f; // GDD Section 5: oldest settled cubes pay 60% automatic cash bonus
    public LayerMask rubbleLayerMask;

    [Header("Vacuum Suction & Blow Stats")]
    public float suctionRange = 4.2f;
    public float suctionConeAngle = 40f;
    public float suctionCurveDuration = 0.18f;
    public float blowInterval = 0.045f;
    public float blowEjectForce = 11f;

    private readonly Queue<RubbleCube> rubblePool = new Queue<RubbleCube>();
    private readonly List<RubbleCube> activeRubble = new List<RubbleCube>();
    private readonly Stack<TankItem> vacuumTank = new Stack<TankItem>();
    private readonly Collider[] suctionHits = new Collider[64];

    private float blowTimer = 0f;
    private bool isVacuumRunning = false;

    private struct TankItem
    {
        public int cashValue;
        public Color materialColor;
    }

    void Awake()
    {
        Instance = this;
        PrewarmRubblePool(400); // Prewarm up to High-tier cap so tier changes never allocate
    }

    private void PrewarmRubblePool(int totalPoolCount)
    {
        if (!rubblePrefab) return;
        for (int i = 0; i < totalPoolCount; i++)
        {
            RubbleCube cube = Instantiate(rubblePrefab, transform);
            cube.gameObject.SetActive(false);
            rubblePool.Enqueue(cube);
        }
    }

    public void SpawnRubbleCube(Vector3 worldPos, Vector3 popNormal, int cashValue, Color matColor)
    {
        // GDD Section 5: If active rubble reaches the phone's tier cap, sweep the oldest settled cube for 60% automatic cash
        if (activeRubble.Count >= maxActiveRubble || rubblePool.Count == 0)
        {
            if (activeRubble.Count > 0)
            {
                RubbleCube oldest = activeRubble[0];
                int autoBonus = Mathf.Max(1, Mathf.RoundToInt(oldest.cashValue * sweepCashRefundRatio));
                if (CompanyStoreEconomyManager.Instance)
                    CompanyStoreEconomyManager.Instance.AddCash(autoBonus);

                oldest.ReturnToPool();
            }
            if (rubblePool.Count == 0) return;
        }

        RubbleCube cube = rubblePool.Dequeue();
        activeRubble.Add(cube);

        cube.transform.SetPositionAndRotation(worldPos, UnityEngine.Random.rotation);
        cube.transform.localScale = Vector3.one * 0.24f;
        cube.gameObject.SetActive(true);

        Vector3 popVelocity = (popNormal + UnityEngine.Random.insideUnitSphere * 0.45f).normalized * UnityEngine.Random.Range(2.2f, 4.5f);
        cube.ActivateCube(cashValue, matColor, popVelocity, ReturnCubeToPool);
    }

    private void ReturnCubeToPool(RubbleCube cube)
    {
        activeRubble.Remove(cube);
        cube.gameObject.SetActive(false);
        rubblePool.Enqueue(cube);
    }

    public void SetVacuumActive(bool active) => isVacuumRunning = active;

    void Update()
    {
        if (!isVacuumRunning || !toolController || !vacuumNozzlePoint) return;

        if (!toolController.isBlowMode)
            RunSuctionStep();
        else
            RunBlowStep();
    }

    private void RunSuctionStep()
    {
        if (toolController.currentTankCount >= toolController.maxTankCapacity) return;

        Transform cam = toolController.playerCamera.transform;
        Vector3 scanCenter = cam.position + cam.forward * (suctionRange * 0.5f);
        int hitCount = Physics.OverlapSphereNonAlloc(scanCenter, suctionRange, suctionHits, rubbleLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            if (toolController.currentTankCount >= toolController.maxTankCapacity) break;

            if (suctionHits[i].TryGetComponent<RubbleCube>(out RubbleCube cube) && !cube.isBeingProcessed)
            {
                Vector3 toCube = (cube.transform.position - cam.position).normalized;
                if (Vector3.Angle(cam.forward, toCube) <= suctionConeAngle)
                {
                    cube.BeginVacuumCurve();
                    if (UIScreenAndAccessibilityManager.Instance)
                        UIScreenAndAccessibilityManager.Instance.NotifyFirstCubeVacuumed();
                    StartCoroutine(BezierSuctionCurveRoutine(cube));
                }
            }
        }
    }

    private IEnumerator BezierSuctionCurveRoutine(RubbleCube cube)
    {
        Vector3 startPos = cube.transform.position;
        Vector3 startScale = cube.transform.localScale;
        Transform cam = toolController.playerCamera.transform;
        Vector3 controlPoint = Vector3.Lerp(startPos, cam.position + cam.forward * 1.1f, 0.6f) + Vector3.up * 0.25f;

        float elapsed = 0f;
        while (elapsed < suctionCurveDuration && cube != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / suctionCurveDuration;
            float oneMinusT = 1f - t;

            Vector3 endPos = vacuumNozzlePoint.position;
            Vector3 bezierPos = (oneMinusT * oneMinusT * startPos) +
                                (2f * oneMinusT * t * controlPoint) +
                                (t * t * endPos);

            cube.transform.position = bezierPos;
            cube.transform.Rotate(Vector3.one * (720f * Time.deltaTime), Space.Self);
            cube.transform.localScale = Vector3.Lerp(startScale, startScale * 0.25f, t * t);
            yield return null;
        }

        if (cube != null)
        {
            if (toolController.currentTankCount < toolController.maxTankCapacity)
            {
                toolController.currentTankCount++;
                vacuumTank.Push(new TankItem { cashValue = cube.cashValue, materialColor = cube.currentColor });
            }
            cube.ReturnToPool();
        }
    }

    private void RunBlowStep()
    {
        blowTimer -= Time.deltaTime;

        Transform cam = toolController.playerCamera.transform;
        int hitCount = Physics.OverlapSphereNonAlloc(cam.position + cam.forward * 1.8f, 2.0f, suctionHits, rubbleLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            if (suctionHits[i].TryGetComponent<RubbleCube>(out RubbleCube floorCube) && !floorCube.isBeingProcessed)
            {
                floorCube.PushWithAir(cam.forward * (blowEjectForce * 0.6f));
            }
        }

        if (blowTimer <= 0f && vacuumTank.Count > 0 && rubblePool.Count > 0)
        {
            blowTimer = blowInterval;
            TankItem item = vacuumTank.Pop();
            toolController.currentTankCount = Mathf.Max(0, toolController.currentTankCount - 1);

            RubbleCube ejected = rubblePool.Dequeue();
            activeRubble.Add(ejected);

            ejected.transform.SetPositionAndRotation(vacuumNozzlePoint.position + vacuumNozzlePoint.forward * 0.25f, UnityEngine.Random.rotation);
            ejected.transform.localScale = Vector3.one * 0.24f;
            ejected.gameObject.SetActive(true);

            Vector3 ejectVel = (toolController.playerCamera.transform.forward + UnityEngine.Random.insideUnitSphere * 0.08f).normalized * blowEjectForce;
            ejected.ActivateCube(item.cashValue, item.materialColor, ejectVel, ReturnCubeToPool);
        }
    }
}
