using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// A story object in the mine (locker, logbook, note, part, fuel cell, generator, cable, crate,
/// store terminal, lift gate). Created at runtime from the mine layout by
/// <see cref="StoryDirector"/>; its behaviour is data-driven by <see cref="Spot"/>.
/// </summary>
public class MineInteractable : MonoBehaviour, IInteractable
{
    public InteractableSpot Spot { get; private set; }
    public bool Consumed { get; private set; }

    private StoryDirector director;
    private string prompt;

    public void Setup(InteractableSpot spot, StoryDirector owner, string promptText)
    {
        Spot = spot;
        director = owner;
        prompt = promptText;
    }

    public Vector3 InteractPoint => transform.position + Vector3.up * 0.4f;
    public string Prompt => prompt;
    public bool CanInteract => !Consumed && gameObject.activeInHierarchy;
    public void Interact() => director.Handle(this);

    /// <summary>Picked-up items disappear; fixtures (locker, generator, gate) stay.</summary>
    public void Consume(bool hide)
    {
        Consumed = hide;
        if (hide) gameObject.SetActive(false);
    }
}
