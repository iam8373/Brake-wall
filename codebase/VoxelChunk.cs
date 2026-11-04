using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelChunk : MonoBehaviour
{
    private VoxelWorldManager world;
    private int chunkX, chunkY, chunkZ;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;

    private readonly List<Vector3> vertices = new List<Vector3>(2048);
    private readonly List<int> triangles = new List<int>(3072);
    private readonly List<Color> colors = new List<Color>(2048);

    public void Initialize(VoxelWorldManager manager, int cx, int cy, int cz, Material mat)
    {
        world = manager;
        chunkX = cx;
        chunkY = cy;
        chunkZ = cz;

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        GetComponent<MeshRenderer>().sharedMaterial = mat;

        mesh = new Mesh { name = $"ChunkMesh_{cx}_{cy}_{cz}" };
        meshFilter.sharedMesh = mesh;
    }

    public void RebuildMesh()
    {
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        int s = VoxelWorldManager.CHUNK_SIZE;
        float vSize = world.voxelSize;
        int startX = chunkX * s;
        int startY = chunkY * s;
        int startZ = chunkZ * s;

        for (int x = 0; x < s; x++)
        {
            for (int y = 0; y < s; y++)
            {
                for (int z = 0; z < s; z++)
                {
                    int gx = startX + x;
                    int gy = startY + y;
                    int gz = startZ + z;

                    if (world.GetVoxelId(gx, gy, gz) == 0) continue;

                    Color col = world.GetVoxelDisplayColor(gx, gy, gz);
                    Vector3 basePos = new Vector3(x * vSize, y * vSize, z * vSize);

                    if (world.GetVoxelId(gx, gy + 1, gz) == 0) AddQuad(basePos, new Vector3(0, vSize, 0), new Vector3(0, vSize, vSize), new Vector3(vSize, vSize, vSize), new Vector3(vSize, vSize, 0), col);
                    if (world.GetVoxelId(gx, gy - 1, gz) == 0) AddQuad(basePos, new Vector3(0, 0, 0), new Vector3(vSize, 0, 0), new Vector3(vSize, 0, vSize), new Vector3(0, 0, vSize), col);
                    if (world.GetVoxelId(gx - 1, gy, gz) == 0) AddQuad(basePos, new Vector3(0, 0, vSize), new Vector3(0, vSize, vSize), new Vector3(0, vSize, 0), new Vector3(0, 0, 0), col);
                    if (world.GetVoxelId(gx + 1, gy, gz) == 0) AddQuad(basePos, new Vector3(vSize, 0, 0), new Vector3(vSize, vSize, 0), new Vector3(vSize, vSize, vSize), new Vector3(vSize, 0, vSize), col);
                    if (world.GetVoxelId(gx, gy, gz + 1) == 0) AddQuad(basePos, new Vector3(vSize, 0, vSize), new Vector3(vSize, vSize, vSize), new Vector3(0, vSize, vSize), new Vector3(0, 0, vSize), col);
                    if (world.GetVoxelId(gx, gy, gz - 1) == 0) AddQuad(basePos, new Vector3(0, 0, 0), new Vector3(0, vSize, 0), new Vector3(vSize, vSize, 0), new Vector3(vSize, 0, 0), col);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        if (vertices.Count > 0) meshCollider.sharedMesh = mesh;
    }

    private void AddQuad(Vector3 offset, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color c)
    {
        int idx = vertices.Count;
        vertices.Add(offset + v0);
        vertices.Add(offset + v1);
        vertices.Add(offset + v2);
        vertices.Add(offset + v3);

        colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);

        triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
        triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
    }
}
