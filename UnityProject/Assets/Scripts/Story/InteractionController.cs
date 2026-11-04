using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Finds the interactable the player is looking at (range + view cone + grid line of sight, no
/// physics) a few times per second, shows the USE prompt button, and runs it on tap.
/// </summary>
public class InteractionController : MonoBehaviour
{
    public Camera playerCamera;
    public Button useButton;
    public TextMeshProUGUI useButtonLabel;
    public float reachMeters = 2.6f;
    [Range(5f, 60f)] public float viewConeDegrees = 32f;
    public float scanInterval = 0.1f;

    public IInteractable Current { get; private set; }

    private readonly List<IInteractable> interactables = new List<IInteractable>(64);
    private float timer;
    private bool shown;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!useButton) Debug.LogError("InteractionController: useButton is not assigned.", this);
        if (useButton) useButton.gameObject.SetActive(false);
    }

    public void Register(IInteractable item)
    {
        if (item != null && !interactables.Contains(item)) interactables.Add(item);
    }

    void Update()
    {
        if (PlayerInputSource.DebugKeyDown(PlayerInputSource.DebugKey.Interact)) UseCurrent();
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = scanInterval;
        Current = FindBest();
        bool show = Current != null;
        if (useButton && show != shown)
        {
            useButton.gameObject.SetActive(show);
            shown = show;
        }
        if (show && useButtonLabel) useButtonLabel.text = Current.Prompt;
    }

    private IInteractable FindBest()
    {
        if (!playerCamera) return null;
        Transform cam = playerCamera.transform;
        VoxelWorldManager world = VoxelWorldManager.Instance;
        IInteractable best = null;
        float bestScore = float.MaxValue;
        float cosCone = Mathf.Cos(viewConeDegrees * Mathf.Deg2Rad);
        for (int i = 0; i < interactables.Count; i++)
        {
            IInteractable item = interactables[i];
            if (item == null || !item.CanInteract) continue;
            Vector3 to = item.InteractPoint - cam.position;
            float dist = to.magnitude;
            if (dist > reachMeters || dist < 0.01f) continue;
            float cos = Vector3.Dot(cam.forward, to / dist);
            if (cos < cosCone) continue;
            if (world && world.IsReady)
            {
                GridHit hit = world.RaycastWorld(cam.position, to / dist, dist);
                if (hit.hit && hit.distance * world.VoxelSize < dist - world.VoxelSize * 0.75f) continue;
            }
            float score = dist * (2f - cos);
            if (score < bestScore) { bestScore = score; best = item; }
        }
        return best;
    }

    /// <summary>USE button.</summary>
    public void UseCurrent()
    {
        if (Current == null || !Current.CanInteract) return;
        Current.Interact();
        timer = 0f;
    }
}
