using System;
using UnityEngine;

/// <summary>
/// A pooled stick of dynamite pressed onto a wall face. It does not fly or use physics: the
/// owner places it at a grid-ray hit, the fuse runs, and the blast is reported once. The pool
/// owner (<see cref="DynamiteAction"/>) is the only code that activates or returns it.
/// </summary>
public class StickyDynamite : MonoBehaviour
{
    public Transform fuseSparkVisual;

    /// <summary>World position, blast radius (m), damage.</summary>
    public static event Action<Vector3, float, float> Detonated;

    public bool IsLit { get; private set; }

    private Action<StickyDynamite> returnToPool;
    private float fuseRemaining;
    private float blastRadius;
    private float blastDamage;

    public void InitializePool(Action<StickyDynamite> onReturn) => returnToPool = onReturn;

    /// <summary>Places the stick on a face and lights the fuse.</summary>
    public void PlaceAndLight(Vector3 facePoint, Vector3 faceNormal, float fuseSeconds, float radius, float damage)
    {
        transform.SetPositionAndRotation(facePoint + faceNormal * 0.04f, Quaternion.LookRotation(faceNormal) * Quaternion.Euler(90f, 0f, 0f));
        fuseRemaining = fuseSeconds;
        blastRadius = radius;
        blastDamage = damage;
        IsLit = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsLit) return;
        fuseRemaining -= Time.deltaTime;
        if (fuseSparkVisual) fuseSparkVisual.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 45f) * 0.3f);
        if (fuseRemaining > 0f) return;
        IsLit = false;
        Vector3 position = transform.position;
        returnToPool?.Invoke(this);
        Detonated?.Invoke(position, blastRadius, blastDamage);
    }
}
