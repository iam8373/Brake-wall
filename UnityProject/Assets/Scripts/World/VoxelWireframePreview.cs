using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Wireframe preview of exactly the cubes the next punch will hit. It uses the same aim result
/// (grid ray) and the same pattern cell lists (<see cref="PunchResolver.TargetCells"/>) as the
/// punch itself, and the cube size comes from the world (0.5 m). No physics queries.
/// The preview is one combined line mesh, rebuilt only when the target changes, drawn with one call.
/// </summary>
public class VoxelWireframePreview : MonoBehaviour
{
    [Header("References")]
    public VoxelAim aim;
    public PunchAction punch;
    [Tooltip("Unlit vertex-colour material (BreakToEscape/UnlitVertexColor).")]
    public Material lineMaterial;

    [Header("Colours")]
    public Color centerVoxelColor = new Color(1f, 0.62f, 0.1f, 1f);
    public Color outerVoxelColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Lines are drawn slightly larger than a cube so they sit on top of the faces.")]
    public float lineInflate = 1.02f;

    private Mesh mesh;
    private bool previewEnabled = true;
    private Int3 lastVoxel;
    private Int3 lastNormal;
    private int lastLevel = -1;
    private int lastDepth = -1;
    private int lastBrokenCount = -1;
    private bool hasMesh;

    private readonly List<Int3> cells = new List<Int3>(512);
    private readonly List<Vector3> verts = new List<Vector3>(4096);
    private readonly List<Color32> colors = new List<Color32>(4096);
    private readonly List<int> indices = new List<int>(12288);

    private static readonly int[] EdgeIndices = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

    void Awake()
    {
        mesh = new Mesh { name = "PunchPreviewLines" };
        mesh.MarkDynamic();
        if (!lineMaterial) Debug.LogError("VoxelWireframePreview: lineMaterial is not assigned; the punch preview is hidden.", this);
    }

    public void SetPreviewActive(bool active) => previewEnabled = active;

    void LateUpdate()
    {
        VoxelWorldManager world = VoxelWorldManager.Instance;
        if (!previewEnabled || !aim || !punch || !lineMaterial || !world || !world.IsReady || !punch.PreviewVisible) return;
        if (!aim.HasTarget) return;

        int level = punch.PreviewPatternLevel;
        int depth = punch.DepthLayers;
        GridHit hit = aim.CurrentHit;
        if (!hasMesh || hit.voxel != lastVoxel || hit.normal != lastNormal || level != lastLevel || depth != lastDepth || world.Grid.BrokenCount != lastBrokenCount)
        {
            Rebuild(world, hit, level, depth);
            lastVoxel = hit.voxel;
            lastNormal = hit.normal;
            lastLevel = level;
            lastDepth = depth;
            lastBrokenCount = world.Grid.BrokenCount;
            hasMesh = true;
        }
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(world.Origin, Quaternion.identity, Vector3.one), lineMaterial, 0);
    }

    private void Rebuild(VoxelWorldManager world, GridHit hit, int level, int depth)
    {
        cells.Clear();
        PunchResolver.TargetCells(hit.voxel, hit.normal, level, depth, cells);
        verts.Clear();
        colors.Clear();
        indices.Clear();
        float v = world.VoxelSize;
        float h = v * 0.5f * lineInflate;
        Color32 center = centerVoxelColor;
        Color32 outer = outerVoxelColor;
        VoxelGrid grid = world.Grid;

        for (int i = 0; i < cells.Count; i++)
        {
            Int3 c = cells[i];
            if (!grid.IsSolid(c.x, c.y, c.z) || grid.IsBoundary(c.x, c.y, c.z)) continue;
            var mid = new Vector3((c.x + 0.5f) * v, (c.y + 0.5f) * v, (c.z + 0.5f) * v);
            int start = verts.Count;
            verts.Add(mid + new Vector3(-h, -h, -h));
            verts.Add(mid + new Vector3(h, -h, -h));
            verts.Add(mid + new Vector3(h, h, -h));
            verts.Add(mid + new Vector3(-h, h, -h));
            verts.Add(mid + new Vector3(-h, -h, h));
            verts.Add(mid + new Vector3(h, -h, h));
            verts.Add(mid + new Vector3(h, h, h));
            verts.Add(mid + new Vector3(-h, h, h));
            Color32 col = i == 0 ? center : outer;
            for (int k = 0; k < 8; k++) colors.Add(col);
            for (int k = 0; k < EdgeIndices.Length; k++) indices.Add(start + EdgeIndices[k]);
        }

        mesh.Clear();
        mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }
}
