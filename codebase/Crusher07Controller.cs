using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Crusher07Controller : MonoBehaviour
{
    [Header("Roller Transforms")]
    public Transform leftRoller;
    public Transform rightRoller;
    public Transform crusherFrame;

    [Header("Roller Animation Settings")]
    public float idleRollerRPM = 90f;
    public float activeRollerRPM = 420f;
    public float grindShakeAmount = 0.005f;

    [Header("Upgradeable Stats")]
    public float crushesPerSecond = 6f;
    public bool autoConveyorUnlocked = false;
    public float conveyorPullRadius = 2.2f;
    public float conveyorPullSpeed = 2.8f;
    public LayerMask rubbleLayerMask;

    [Header("Floating +$ Popup Pool")]
    public TextMeshPro floatingCashPrefab;
    public int popupPoolSize = 15;
    public float popupRiseSpeed = 0.85f;
    public float popupLifetime = 0.9f;

    public static event Action<int> OnCashEarned;

    private readonly Queue<RubbleCube> crushQueue = new Queue<RubbleCube>();
    private readonly Queue<TextMeshPro> popupPool = new Queue<TextMeshPro>();
    private readonly Collider[] conveyorHits = new Collider[32];

    private Vector3 frameStartLocalPos;
    private float crushCooldown = 0f;
    private float activeGrindTimer = 0f;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        if (crusherFrame) frameStartLocalPos = crusherFrame.localPosition;

        if (floatingCashPrefab)
        {
            for (int i = 0; i < popupPoolSize; i++)
            {
                TextMeshPro tmp = Instantiate(floatingCashPrefab, transform.position, Quaternion.identity);
                tmp.gameObject.SetActive(false);
                popupPool.Enqueue(tmp);
            }
        }
    }

    void Update()
    {
        bool isGrinding = crushQueue.Count > 0 || activeGrindTimer > 0f;
        if (activeGrindTimer > 0f) activeGrindTimer -= Time.deltaTime;

        float currentSpeed = isGrinding ? activeRollerRPM : idleRollerRPM;
        if (leftRoller) leftRoller.Rotate(Vector3.right * (currentSpeed * Time.deltaTime), Space.Self);
        if (rightRoller) rightRoller.Rotate(Vector3.left * (currentSpeed * Time.deltaTime), Space.Self);

        if (crusherFrame)
        {
            if (isGrinding)
            {
                Vector3 jitter = UnityEngine.Random.insideUnitSphere * grindShakeAmount;
                jitter.y = 0f;
                crusherFrame.localPosition = frameStartLocalPos + jitter;
            }
            else
            {
                crusherFrame.localPosition = Vector3.Lerp(crusherFrame.localPosition, frameStartLocalPos, Time.deltaTime * 15f);
            }
        }

        crushCooldown -= Time.deltaTime;
        if (crushQueue.Count > 0 && crushCooldown <= 0f)
        {
            crushCooldown = 1f / Mathf.Max(0.5f, crushesPerSecond);
            RubbleCube cube = crushQueue.Dequeue();
            if (cube != null && cube.gameObject.activeInHierarchy)
            {
                StartCoroutine(GrindSingleCubeRoutine(cube));
            }
        }

        if (autoConveyorUnlocked)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, conveyorPullRadius, conveyorHits, rubbleLayerMask);
            for (int i = 0; i < count; i++)
            {
                if (conveyorHits[i].TryGetComponent<RubbleCube>(out RubbleCube rubble) && !rubble.isBeingProcessed)
                {
                    Vector3 pullDir = (transform.position + Vector3.up * 0.1f - rubble.transform.position).normalized;
                    rubble.transform.position += pullDir * (conveyorPullSpeed * Time.deltaTime);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<RubbleCube>(out RubbleCube rubble) && !rubble.isBeingProcessed)
        {
            rubble.PrepareForCrusher();
            crushQueue.Enqueue(rubble);
        }
    }

    private IEnumerator GrindSingleCubeRoutine(RubbleCube cube)
    {
        activeGrindTimer = 0.25f;
        Vector3 startPos = cube.transform.position;
        Vector3 targetPos = transform.position + Vector3.down * 0.15f;
        Vector3 startScale = cube.transform.localScale;

        float elapsed = 0f;
        float grindDuration = 0.14f;

        while (elapsed < grindDuration && cube != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / grindDuration;
            cube.transform.position = Vector3.Lerp(startPos, targetPos, t) + UnityEngine.Random.insideUnitSphere * 0.015f;
            cube.transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x * 0.2f, startScale.y * 0.2f, startScale.z * 0.2f), t);
            yield return null;
        }

        if (cube != null)
        {
            int payout = cube.cashValue;
            OnCashEarned?.Invoke(payout);
            SpawnFloatingCashPopup(payout, startPos);
            cube.ReturnToPool();
        }
    }

    private void SpawnFloatingCashPopup(int amount, Vector3 worldPos)
    {
        if (popupPool.Count == 0) return;

        TextMeshPro popup = popupPool.Dequeue();
        Vector3 randomSpread = new Vector3(UnityEngine.Random.Range(-0.18f, 0.18f), 0.15f, UnityEngine.Random.Range(-0.18f, 0.18f));
        popup.transform.position = worldPos + randomSpread;
        popup.text = "+$" + amount.ToString("N0");
        popup.alpha = 1f;
        popup.gameObject.SetActive(true);

        StartCoroutine(AnimateCashPopupRoutine(popup));
    }

    private IEnumerator AnimateCashPopupRoutine(TextMeshPro popup)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.6f;
        Vector3 peakScale = Vector3.one * 1.0f;

        while (elapsed < popupLifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupLifetime;

            popup.transform.position += Vector3.up * (popupRiseSpeed * Time.deltaTime);
            if (mainCam)
            {
                popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - mainCam.transform.position);
            }

            popup.transform.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Min(1f, t * 4f));
            if (t > 0.5f)
            {
                popup.alpha = 1f - ((t - 0.5f) * 2f);
            }
            yield return null;
        }

        popup.gameObject.SetActive(false);
        popupPool.Enqueue(popup);
    }
}
