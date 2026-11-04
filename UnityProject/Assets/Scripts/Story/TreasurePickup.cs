using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>A named showcase treasure buried in a rock voxel. It can be collected once the voxel is dug out.</summary>
public class TreasurePickup : MonoBehaviour, IInteractable
{
    public TreasureSpot Spot { get; private set; }
    public bool Uncovered { get; private set; }
    public bool Collected { get; private set; }

    private TreasureAndMapManager owner;

    public void Setup(TreasureSpot spot, TreasureAndMapManager manager)
    {
        Spot = spot;
        owner = manager;
    }

    public void SetUncovered(bool uncovered) => Uncovered = uncovered;

    public void MarkCollected()
    {
        Collected = true;
        gameObject.SetActive(false);
    }

    public Vector3 InteractPoint => transform.position;
    public string Prompt => "COLLECT " + Spot.name.ToUpperInvariant() + " (+$" + Spot.cashValue.ToString("N0") + ")";
    public bool CanInteract => Uncovered && !Collected;
    public void Interact() => owner.Collect(this);
}
