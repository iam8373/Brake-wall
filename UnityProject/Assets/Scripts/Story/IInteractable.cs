using UnityEngine;

/// <summary>Anything the player can use with the USE button (story objects, treasure).</summary>
public interface IInteractable
{
    /// <summary>World position used for range, view angle and line-of-sight checks.</summary>
    Vector3 InteractPoint { get; }
    string Prompt { get; }
    bool CanInteract { get; }
    void Interact();
}
