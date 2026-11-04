using System.Collections.Generic;
using BreakToEscape.Core.World;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One 16 × 16 × 16 chunk of the mine. Builds a face-culled mesh (only faces next to air) with
/// flat normals, per-face UVs (0..1 per face, ready for a texture atlas later) and vertex colours
/// (band colour, darkened by damage; alpha = 1 marks glowing crystal for the shader).
/// The collision mesh is a separate, double-buffered mesh so the collider is re-cooked by
/// assigning a different mesh instead of clearing it to null on every rebuild.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelChunk : MonoBehaviour
{
    private VoxelWorldManager world;
    private int chunkX, chunkY, chunkZ;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh renderMesh;
    private readonly Mesh[] colliderMeshes = new Mesh[2];
    private int colliderBuffer;

    // Shared build buffers: chunks are rebuilt one at a time on the main thread.
    private static readonly List<Vector3> Vertices = new List<Vector3>(8192);
    private static readonly List<Vector3> Normals = new List<Vector3>(8192);
    private static readonly List<Vector2> Uvs = new List<Vector2>(8192);
    private static readonly List<Color32> Colors = new List<Color32>(8192);
    private static readonly List<int> Triangles = new List<int>(12288);
    private static readonly Color32[] MaterialColors = new Color32[VoxelMaterialTable.MaxMaterials];
    private static bool colorsCached;

    public int FaceCount { get; private set; }

    public void Initialize(VoxelWorldManager manager, int cx, int cy, int cz, Material material)
    {
        world = manager;
        chunkX = cx;
        chunkY = cy;
        chunkZ = cz;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;

        renderMesh = new Mesh { name = "ChunkMesh_" + cx + "_" + cy + "_" + cz, indexFormat = IndexFormat.UInt16 };
        renderMesh.MarkDynamic();
        meshFilter.sharedMesh = renderMesh;
        for (int i = 0; i < 2; i++)
            colliderMeshes[i] = new Mesh { name = "ChunkCollider_" + cx + "_" + cy + "_" + cz + "_" + i, indexFormat = IndexFormat.UInt16 };
        meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning |
                                      MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase;

        if (!colorsCached) CacheColors(manager.Grid.materials);
    }

    private static void CacheColors(VoxelMaterialTable table)
    {
        foreach (VoxelMaterial m in table.All())
            MaterialColors[m.id] = new Color32((byte)(m.r * 255f), (byte)(m.g * 255f), (byte)(m.b * 255f), (byte)(m.glows ? 255 : 0));
        colorsCached = true;
    }

    /// <summary>Rebuilds the visible mesh from the grid.</summary>
    public void RebuildRenderMesh()
    {
        BuildGeometry(true);
        renderMesh.Clear();
        renderMesh.SetVertices(Vertices);
        renderMesh.SetNormals(Normals);
        renderMesh.SetUVs(0, Uvs);
        renderMesh.SetColors(Colors);
        renderMesh.SetTriangles(Triangles, 0, false);
        renderMesh.bounds = ChunkBounds();
    }

    /// <summary>
    /// Rebuilds the collision mesh into the spare buffer and assigns it. Assigning a different
    /// mesh instance makes the collider re-cook without the null-then-reassign reset.
    /// </summary>
    public void RebuildCollider()
    {
        BuildGeometry(false);
        colliderBuffer ^= 1;
        Mesh target = colliderMeshes[colliderBuffer];
        target.Clear();
        if (Triangles.Count == 0)
        {
            meshCollider.enabled = false;
            return;
        }
        target.SetVertices(Vertices);
        target.SetTriangles(Triangles, 0, true);
        meshCollider.sharedMesh = target;
        meshCollider.enabled = true;
    }

    private Bounds ChunkBounds()
    {
        float size = VoxelGrid.ChunkSize * world.VoxelSize;
        return new Bounds(new Vector3(size, size, size) * 0.5f, new Vector3(size, size, size));
    }

    /// <summary>
    /// Hot loop. Reads the grid's raw id array with direct strides; neighbours outside the grid
    /// count as solid (the mine is a closed block), so no bounds-checked lookup is needed per face.
    /// </summary>
    private void BuildGeometry(bool full)
    {
        Vertices.Clear();
        Normals.Clear();
        Uvs.Clear();
        Colors.Clear();
        Triangles.Clear();

        VoxelGrid grid = world.Grid;
        byte[] ids = grid.RawIds;
        int sx = grid.sizeX, sy = grid.sizeY, sz = grid.sizeZ;
        int strideX = sy * sz, strideY = sz;
        int s = VoxelGrid.ChunkSize;
        float v = world.VoxelSize;
        int x0 = chunkX * s, y0 = chunkY * s, z0 = chunkZ * s;
        int faces = 0;

        for (int lx = 0; lx < s; lx++)
        {
            int gx = x0 + lx;
            for (int ly = 0; ly < s; ly++)
            {
                int gy = y0 + ly;
                int index = gx * strideX + gy * strideY + z0;
                for (int lz = 0; lz < s; lz++, index++)
                {
                    byte id = ids[index];
                    if (id == 0) continue;
                    int gz = z0 + lz;

                    bool airPX = gx + 1 < sx && ids[index + strideX] == 0;
                    bool airNX = gx > 0 && ids[index - strideX] == 0;
                    bool airPY = gy + 1 < sy && ids[index + strideY] == 0;
                    bool airNY = gy > 0 && ids[index - strideY] == 0;
                    bool airPZ = gz + 1 < sz && ids[index + 1] == 0;
                    bool airNZ = gz > 0 && ids[index - 1] == 0;
                    if (!(airPX || airNX || airPY || airNY || airPZ || airNZ)) continue;

                    Color32 c = default;
                    if (full) c = ShadeColor(MaterialColors[id], grid.DamageRatio(gx, gy, gz));
                    float px = lx * v, py = ly * v, pz = lz * v;

                    if (airPY) { AddFace(full, px, py + v, pz, 0, 0, v, v, 0, 0, Vector3.up, c, 1.08f); faces++; }
                    if (airNY) { AddFace(full, px, py, pz, v, 0, 0, 0, 0, v, Vector3.down, c, 0.70f); faces++; }
                    if (airNX) { AddFace(full, px, py, pz, 0, 0, v, 0, v, 0, Vector3.left, c, 0.86f); faces++; }
                    if (airPX) { AddFace(full, px + v, py, pz, 0, v, 0, 0, 0, v, Vector3.right, c, 0.90f); faces++; }
                    if (airNZ) { AddFace(full, px, py, pz, 0, v, 0, v, 0, 0, Vector3.back, c, 0.94f); faces++; }
                    if (airPZ) { AddFace(full, px, py, pz + v, v, 0, 0, 0, v, 0, Vector3.forward, c, 0.82f); faces++; }
                }
            }
        }
        FaceCount = faces;
    }

    private static Color32 ShadeColor(Color32 baseColor, float damageRatio)
    {
        float k = 1f - damageRatio * 0.45f;
        return new Color32((byte)(baseColor.r * k), (byte)(baseColor.g * k), (byte)(baseColor.b * k), baseColor.a);
    }

    /// <summary>
    /// Adds one quad. (ox, oy, oz) is the first corner; (ax, ay, az) and (bx, by, bz) are the two
    /// edge vectors, ordered so the winding faces <paramref name="normal"/>. A per-face tint gives
    /// cheap directional shading that reads well even with weak lighting.
    /// </summary>
    private static void AddFace(bool full, float ox, float oy, float oz, float ax, float ay, float az, float bx, float by, float bz,
        Vector3 normal, Color32 color, float tint)
    {
        int start = Vertices.Count;
        Vertices.Add(new Vector3(ox, oy, oz));
        Vertices.Add(new Vector3(ox + ax, oy + ay, oz + az));
        Vertices.Add(new Vector3(ox + ax + bx, oy + ay + by, oz + az + bz));
        Vertices.Add(new Vector3(ox + bx, oy + by, oz + bz));
        Triangles.Add(start); Triangles.Add(start + 1); Triangles.Add(start + 2);
        Triangles.Add(start); Triangles.Add(start + 2); Triangles.Add(start + 3);
        if (!full) return;

        for (int i = 0; i < 4; i++) Normals.Add(normal);
        Uvs.Add(new Vector2(0f, 0f));
        Uvs.Add(new Vector2(0f, 1f));
        Uvs.Add(new Vector2(1f, 1f));
        Uvs.Add(new Vector2(1f, 0f));
        var tinted = new Color32(
            (byte)Mathf.Min(255f, color.r * tint), (byte)Mathf.Min(255f, color.g * tint), (byte)Mathf.Min(255f, color.b * tint), color.a);
        for (int i = 0; i < 4; i++) Colors.Add(tinted);
    }
}
