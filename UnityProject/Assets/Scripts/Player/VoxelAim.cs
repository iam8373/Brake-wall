using BreakToEscape.Core;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Shared aim for punches, the preview and dynamite: one grid ray (plus a small cone of grid
/// rays for aim assist) from the camera centre, once per frame. No physics queries.
/// </summary>
[DefaultExecutionOrder(-50)]
public class VoxelAim : MonoBehaviour
{
    public Camera playerCamera;
    [Tooltip("Punch reach in metres (economy.json punch.reachMeters overrides this at start).")]
    public float reachMeters = 2.4f;
    [Tooltip("Aim assist cone: tangent of the half angle. 0.09 is about 5 degrees.")]
    [Range(0f, 0.3f)] public float assistSpread = 0.09f;
    [Tooltip("Number of assist rays around the centre ray (0 to 8).")]
    [Range(0, 8)] public int assistRays = 6;

    public bool HasTarget { get; private set; }
    public GridHit CurrentHit { get; private set; }
    /// <summary>World point on the struck face (centre of the face).</summary>
    public Vector3 FacePoint { get; private set; }
    public Vector3 FaceNormal { get; private set; }

    private int lastFrame = -1;

    void Awake()
    {
        if (!playerCamera) playerCamera = GetComponent<Camera>();
        if (!playerCamera) Debug.LogError("VoxelAim: playerCamera is not assigned.", this);
    }

    void Update() => Refresh();

    /// <summary>Recomputes the aim once per frame; later calls in the same frame reuse the result.</summary>
    public void Refresh()
    {
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        HasTarget = false;
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!playerCamera || !world || !world.IsReady) return;

        Transform t = playerCamera.transform;
        GridHit hit = GridRaycast.CastWithAssist(world.Grid, world.WorldToVoxelUnits(t.position),
            VoxelWorldManager.ToFloat3(t.forward), VoxelWorldManager.ToFloat3(t.right), VoxelWorldManager.ToFloat3(t.up),
            reachMeters / world.VoxelSize, assistSpread, assistRays);
        if (!hit.hit || hit.startedInside || world.Grid.IsBoundary(hit.voxel.x, hit.voxel.y, hit.voxel.z)) return;

        CurrentHit = hit;
        HasTarget = true;
        FaceNormal = VoxelWorldManager.ToVector3(hit.normal);
        FacePoint = world.FaceCenterToWorld(hit.voxel, hit.normal);
    }

    /// <summary>Longer ray for placing dynamite (no assist). Returns false if nothing is in range.</summary>
    public bool CastForPlacement(float maxMeters, out GridHit hit, out Vector3 facePoint)
    {
        hit = default;
        facePoint = Vector3.zero;
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!playerCamera || !world || !world.IsReady) return false;
        Transform t = playerCamera.transform;
        hit = world.RaycastWorld(t.position, t.forward, maxMeters);
        if (!hit.hit || hit.startedInside) return false;
        facePoint = world.FaceCenterToWorld(hit.voxel, hit.normal);
        return true;
    }
}
