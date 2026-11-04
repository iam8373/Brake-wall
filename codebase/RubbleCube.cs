using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class RubbleCube : MonoBehaviour
{
    public int cashValue = 1;
    public Color currentColor = Color.gray;
    public bool isBeingProcessed { get; private set; }

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Action<RubbleCube> returnToPoolCallback;
    private float settleTimer = 0f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void ActivateCube(int value, Color matColor, Vector3 initialVelocity, Action<RubbleCube> onReturn)
    {
        cashValue = value;
        currentColor = matColor;
        returnToPoolCallback = onReturn;
        isBeingProcessed = false;
        settleTimer = 0f;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(BaseColorId, matColor);
        meshRenderer.SetPropertyBlock(propBlock);

        rb.isKinematic = false;
        rb.WakeUp();
        rb.linearVelocity = initialVelocity;
        rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;
    }

    void Update()
    {
        if (isBeingProcessed || rb.isKinematic || rb.IsSleeping()) return;

        if (rb.linearVelocity.sqrMagnitude < 0.02f)
        {
            settleTimer += Time.deltaTime;
            if (settleTimer > 0.6f) rb.Sleep();
        }
        else
        {
            settleTimer = 0f;
        }
    }

    public void BeginVacuumCurve()
    {
        isBeingProcessed = true;
        rb.isKinematic = true;
    }

    public void PrepareForCrusher()
    {
        isBeingProcessed = true;
        rb.isKinematic = true;
    }

    public void PushWithAir(Vector3 velocity)
    {
        if (rb.isKinematic) return;
        rb.WakeUp();
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velocity, Time.deltaTime * 12f);
    }

    public void ReturnToPool()
    {
        isBeingProcessed = false;
        returnToPoolCallback?.Invoke(this);
    }
}
