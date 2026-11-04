using UnityEngine;

/// <summary>
/// The scene object for one pooled rubble cube. State lives in RubblePoolBook (owned by
/// <see cref="RubbleSuctionManager"/>); this component only moves, colours and reports settling.
/// <see cref="Generation"/> changes every time the cube is reused, so a coroutine that started on
/// an earlier life of the cube can detect that and stop.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class RubbleCube : MonoBehaviour
{
    [Tooltip("Speed below which the cube counts as resting (m/s).")]
    public float settleSpeed = 0.15f;
    [Tooltip("Seconds below the settle speed before the cube sleeps and may be swept.")]
    public float settleSeconds = 0.6f;

    public int Slot { get; private set; } = -1;
    public int Generation { get; private set; }
    public int CashValue { get; private set; }
    public byte Material { get; private set; }
    public Rigidbody Body { get; private set; }

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;
    private RubbleSuctionManager owner;
    private float settleTimer;
    private bool reportedSettled;
    private bool kinematicHeld;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
    }

    public void Bind(RubbleSuctionManager manager, int slot)
    {
        owner = manager;
        Slot = slot;
    }

    public void Activate(Vector3 position, Vector3 velocity, int cashValue, byte material, Color color, float scale)
    {
        Generation++;
        CashValue = cashValue;
        Material = material;
        settleTimer = 0f;
        reportedSettled = false;
        kinematicHeld = false;

        meshRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        meshRenderer.SetPropertyBlock(block);

        transform.SetPositionAndRotation(position, Random.rotation);
        transform.localScale = Vector3.one * scale;
        gameObject.SetActive(true);
        Body.isKinematic = false;
        Body.WakeUp();
        Body.linearVelocity = velocity;
        Body.angularVelocity = Random.insideUnitSphere * 10f;
    }

    /// <summary>Freezes physics while the cube is scripted (vacuum curve, crusher).</summary>
    public void HoldKinematic()
    {
        kinematicHeld = true;
        Body.isKinematic = true;
    }

    public void ReleaseToPhysics()
    {
        kinematicHeld = false;
        Body.isKinematic = false;
        Body.WakeUp();
    }

    public void Deactivate()
    {
        Generation++;
        kinematicHeld = false;
        gameObject.SetActive(false);
    }

    public void PushWithAir(Vector3 velocity)
    {
        if (kinematicHeld || Body.isKinematic) return;
        Body.WakeUp();
        Body.linearVelocity = Vector3.Lerp(Body.linearVelocity, velocity, Time.deltaTime * 12f);
        if (reportedSettled) { reportedSettled = false; owner.ReportSettled(this, false); }
    }

    void Update()
    {
        if (kinematicHeld || owner == null) return;
        bool slow = Body.IsSleeping() || Body.linearVelocity.sqrMagnitude < settleSpeed * settleSpeed;
        if (slow)
        {
            settleTimer += Time.deltaTime;
            if (!reportedSettled && settleTimer >= settleSeconds)
            {
                reportedSettled = true;
                Body.Sleep();
                owner.ReportSettled(this, true);
            }
        }
        else
        {
            settleTimer = 0f;
            if (reportedSettled)
            {
                reportedSettled = false;
                owner.ReportSettled(this, false);
            }
        }
    }
}
