using System.Collections.Generic;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Dynamite tool: shows a ghost on the wall where the stick will go, plays a short reach-and-place
/// motion, and lights a pooled stick (3 s fuse, radius and damage from economy.json). The stock
/// lives in the economy state. Owns the dynamite pool.
/// </summary>
public class DynamiteAction : MonoBehaviour
{
    public VoxelAim aim;
    public ToolViewmodelAnimator animator;
    public StickyDynamite dynamitePrefab;
    public GameObject ghostPreview;
    [Tooltip("Placement reach as a multiple of the punch reach.")]
    public float reachMultiplier = 1.5f;
    public int poolSize = 6;

    private readonly Queue<StickyDynamite> pool = new Queue<StickyDynamite>();
    private EconomyData data;
    private EconomyState economy;
    private bool ghostVisible;

    public void Initialize(EconomyData economyData, EconomyState economyState)
    {
        data = economyData;
        economy = economyState;
        if (!dynamitePrefab) { Debug.LogError("DynamiteAction: dynamitePrefab is not assigned.", this); return; }
        for (int i = 0; i < poolSize; i++)
        {
            StickyDynamite stick = Instantiate(dynamitePrefab);
            stick.name = "Dynamite_" + i;
            stick.gameObject.SetActive(false);
            stick.InitializePool(ReturnToPool);
            pool.Enqueue(stick);
        }
    }

    private void ReturnToPool(StickyDynamite stick)
    {
        stick.gameObject.SetActive(false);
        if (!pool.Contains(stick)) pool.Enqueue(stick);
    }

    private float Reach => (aim ? aim.reachMeters : 2.4f) * reachMultiplier;

    void Update()
    {
        bool active = animator && animator.CurrentSlot == ToolViewmodelAnimator.Slot.Dynamite && !animator.IsSwapping;
        bool show = false;
        if (active && economy != null && economy.DynamiteStock > 0 && ghostPreview && aim.CastForPlacement(Reach, out GridHit hit, out Vector3 point))
        {
            Vector3 n = VoxelWorldManager.ToVector3(hit.normal);
            ghostPreview.transform.SetPositionAndRotation(point + n * 0.03f, Quaternion.LookRotation(n) * Quaternion.Euler(90f, 0f, 0f));
            show = true;
        }
        if (ghostPreview && show != ghostVisible)
        {
            ghostPreview.SetActive(show);
            ghostVisible = show;
        }
        if (animator && active) animator.SetDynamiteInHandVisible(economy != null && economy.DynamiteStock > 0);
    }

    /// <summary>PUNCH button while the dynamite slot is selected.</summary>
    public void TryPlace()
    {
        if (economy == null || economy.DynamiteStock <= 0 || pool.Count == 0 || !animator) return;
        if (!aim.CastForPlacement(Reach, out GridHit hit, out Vector3 point)) return;
        Vector3 normal = VoxelWorldManager.ToVector3(hit.normal);
        animator.PlayPlaceDynamite(() =>
        {
            if (!economy.TryUseDynamite() || pool.Count == 0) return;
            StickyDynamite stick = pool.Dequeue();
            stick.PlaceAndLight(point, normal, data.dynamiteFuseSeconds, data.dynamiteBlastRadiusMeters, data.dynamiteBlastDamage);
        });
    }

    void OnDisable()
    {
        if (ghostPreview) ghostPreview.SetActive(false);
        ghostVisible = false;
    }
}
