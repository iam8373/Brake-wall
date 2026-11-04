using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StickyDynamite : MonoBehaviour
{
    public Transform fuseSparkVisual;
    public float blastRadius = 1.75f;
    public float blastDamage = 15f;

    // Hook your voxel explosion and particle pool here
    public static event Action<Vector3, float, float> OnDynamiteDetonated;

    private Rigidbody rb;
    private Action<StickyDynamite> returnToPoolCallback;
    private bool hasStuck = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void InitializePool(Action<StickyDynamite> onReturnToPool)
    {
        returnToPoolCallback = onReturnToPool;
    }

    public void Launch(Vector3 velocity, float fuseDuration)
    {
        hasStuck = false;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        rb.angularVelocity = new Vector3(8f, 0f, 0f);
        StopAllCoroutines();
        StartCoroutine(FuseCountdownRoutine(fuseDuration));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasStuck) return;
        hasStuck = true;

        rb.isKinematic = true;
        ContactPoint contact = collision.GetContact(0);
        transform.position = contact.point + contact.normal * 0.03f;
        transform.rotation = Quaternion.LookRotation(contact.normal);
    }

    private IEnumerator FuseCountdownRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (fuseSparkVisual)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 45f) * 0.3f;
                fuseSparkVisual.localScale = Vector3.one * pulse;
            }
            yield return null;
        }

        OnDynamiteDetonated?.Invoke(transform.position, blastRadius, blastDamage);
        returnToPoolCallback?.Invoke(this);
    }
}
