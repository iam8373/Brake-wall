using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct VoxelMaterialDef
{
    public string layerName;
    public byte id;
    public float maxHardness;
    public float cashValue;
    public Color rubbleColor;
    public bool isTreasure;
    public bool emitsCrystalLight;
}

public class VoxelWorldManager : MonoBehaviour
{
    public static VoxelWorldManager Instance { get; private set; }

    [Header("World Dimensions (0.5m Cubes, 16^3 Chunks = 8m per chunk)")]
    public int chunksX = 6;
    public int chunksY = 2;
    public int chunksZ = 6;
    public float voxelSize = 0.5f; // Exact 0.5m cube width from GDD Section 5 & Economy Sheet

    [Header("Rendering & Collision")]
    public Material voxelVertexColorMaterial;
    public int voxelWallLayer = 6;

    [Header("References")]
    public FirstPersonToolController toolController;

    [Header("Greywater Deep: 6 Rock Bands + 3 Treasure Types (Matches Economy Sheet)")]
    public VoxelMaterialDef[] materialLayers = new VoxelMaterialDef[]
    {
        new VoxelMaterialDef { layerName = "Air",                   id = 0, maxHardness = 0f,   cashValue = 0f,   rubbleColor = Color.clear },
        new VoxelMaterialDef { layerName = "B1: Concrete lining",   id = 1, maxHardness = 10f,  cashValue = 2.0f, rubbleColor = new Color(0.68f, 0.70f, 0.72f) },
        new VoxelMaterialDef { layerName = "B2: Packed earth",      id = 2, maxHardness = 25f,  cashValue = 3.5f, rubbleColor = new Color(0.48f, 0.32f, 0.19f) },
        new VoxelMaterialDef { layerName = "B3: Sandstone gallery", id = 3, maxHardness = 60f,  cashValue = 6.0f, rubbleColor = new Color(0.82f, 0.60f, 0.32f) },
        new VoxelMaterialDef { layerName = "B4: Granite core",      id = 4, maxHardness = 140f, cashValue = 10f,  rubbleColor = new Color(0.42f, 0.40f, 0.44f) },
        new VoxelMaterialDef { layerName = "B5: Ore veins",         id = 5, maxHardness = 260f, cashValue = 30f,  rubbleColor = new Color(0.72f, 0.42f, 0.21f) },
        new VoxelMaterialDef { layerName = "B6: Crystal vault",     id = 6, maxHardness = 420f, cashValue = 70f,  rubbleColor = new Color(0.31f, 0.84f, 0.91f), emitsCrystalLight = true },
        // Buried Treasure Cubes (Destroyed by Dynamite per GDD Section 5!)
        new VoxelMaterialDef { layerName = "Treasure: Copper Seam", id = 7, maxHardness = 45f,  cashValue = 45f,  rubbleColor = new Color(0.85f, 0.48f, 0.22f), isTreasure = true },
        new VoxelMaterialDef { layerName = "Treasure: Gold Seam",   id = 8, maxHardness = 110f, cashValue = 180f, rubbleColor = new Color(1.00f, 0.84f, 0.15f), isTreasure = true },
        new VoxelMaterialDef { layerName = "Treasure: Geode Gem",   id = 9, maxHardness = 220f, cashValue = 450f, rubbleColor = new Color(0.55f, 0.95f, 1.00f), isTreasure = true, emitsCrystalLight = true }
    };

    public const int CHUNK_SIZE = 16;

    public int totalCubesBroken { get; private set; } = 0;

    private byte[,,] voxelIds;
    private float[,,] voxelDamage;
    private VoxelChunk[,,] chunks;
    private readonly HashSet<VoxelChunk> dirtyChunks = new HashSet<VoxelChunk>();

    private int totalVoxelsX, totalVoxelsY, totalVoxelsZ;

    void Awake()
    {
        Instance = this;
        totalVoxelsX = chunksX * CHUNK_SIZE;
        totalVoxelsY = chunksY * CHUNK_SIZE;
        totalVoxelsZ = chunksZ * CHUNK_SIZE;

        voxelIds = new byte[totalVoxelsX, totalVoxelsY, totalVoxelsZ];
        voxelDamage = new float[totalVoxelsX, totalVoxelsY, totalVoxelsZ];
        chunks = new VoxelChunk[chunksX, chunksY, chunksZ];

        GenerateGreywaterDeepMineV1();
        BuildAllChunks();
    }

    void OnEnable()
    {
        if (toolController) toolController.OnVoxelStrike += HandlePunchStrike;
        StickyDynamite.OnDynamiteDetonated += HandleDynamiteBlast;
    }

    void OnDisable()
    {
        if (toolController) toolController.OnVoxelStrike -= HandlePunchStrike;
        StickyDynamite.OnDynamiteDetonated -= HandleDynamiteBlast;
    }

    // Builds the V1 Hand-Built Greywater Deep Mine (GDD Section 7 & Wireframe Screen 3)
    private void GenerateGreywaterDeepMineV1()
    {
        int centerX = totalVoxelsX / 2;
        int centerZ = totalVoxelsZ / 2;

        for (int x = 0; x < totalVoxelsX; x++)
        {
            for (int y = 0; y < totalVoxelsY; y++)
            {
                for (int z = 0; z < totalVoxelsZ; z++)
                {
                    // Radial / directional depth bands outward from the Collapse Chamber
                    float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                    byte bandId = 1; // B1: Concrete lining
                    if (distFromCenter > 10f) bandId = 2; // B2: Packed earth & timber
                    if (distFromCenter > 18f) bandId = 3; // B3: Sandstone gallery
                    if (distFromCenter > 25f) bandId = 4; // B4: Granite core
                    if (distFromCenter > 32f) bandId = 5; // B5: Ore veins
                    if (z > centerZ + 34)     bandId = 6; // B6: Crystal vault leading North to Main Shaft

                    // Seed deterministic treasure cubes (Gold, Copper, Glowing Crystal)
                    int hash = (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
                    if ((hash & 0x7F) == 0 && distFromCenter > 8f)
                    {
                        bandId = (bandId <= 2) ? (byte)7 : (bandId <= 4 ? (byte)8 : (byte)9);
                    }

                    voxelIds[x, y, z] = bandId;
                }
            }
        }

        // 1. Centre: Collapse Chamber (Start, Crusher 07, Company Store)
        CarveBox(centerX - 6, 2, centerZ - 6, centerX + 6, 10, centerZ + 6);

        // 2. West: Old Gallery (Band 2 - Foreman Quill's locker, Logbook 1, Work Gauntlets)
        CarveBox(centerX - 22, 2, centerZ - 4, centerX - 14, 9, centerZ + 4);

        // 3. East: Equipment Bay (Band 3 - Surveyor Pike's Logbook 2, Pneumatic Hammer, Conveyor)
        CarveBox(centerX + 16, 2, centerZ - 5, centerX + 24, 9, centerZ + 5);

        // 4. South: Pump Room (Band 4 - Cook Bellamy's Logbook 3, Generator, Fuel Cell, Rock Drill)
        CarveBox(centerX - 5, 2, centerZ - 26, centerX + 5, 9, centerZ - 18);

        // 5. North: Main Shaft (End of Band 6 - Main Shaft Lift Gate)
        CarveBox(centerX - 6, 2, totalVoxelsZ - 10, centerX + 6, 12, totalVoxelsZ - 2);
    }

    private void CarveBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int x = Mathf.Max(1, minX); x < Mathf.Min(totalVoxelsX - 1, maxX); x++)
            for (int y = Mathf.Max(1, minY); y < Mathf.Min(totalVoxelsY - 1, maxY); y++)
                for (int z = Mathf.Max(1, minZ); z < Mathf.Min(totalVoxelsZ - 1, maxZ); z++)
                    voxelIds[x, y, z] = 0;
    }

    private void BuildAllChunks()
    {
        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    GameObject chunkObj = new GameObject($"Chunk_{cx}_{cy}_{cz}");
                    chunkObj.transform.SetParent(transform, false);
                    chunkObj.transform.localPosition = new Vector3(cx * CHUNK_SIZE * voxelSize, cy * CHUNK_SIZE * voxelSize, cz * CHUNK_SIZE * voxelSize);
                    chunkObj.layer = voxelWallLayer;

                    VoxelChunk chunk = chunkObj.AddComponent<VoxelChunk>();
                    chunk.Initialize(this, cx, cy, cz, voxelVertexColorMaterial);
                    chunks[cx, cy, cz] = chunk;
                    chunk.RebuildMesh();
                }
            }
        }
    }

    // =========================================================================
    // PUNCH & DYNAMITE VOXEL DAMAGE (Supports 5 to 97 Cube Patterns & Rock Drill)
    // =========================================================================

    private void HandlePunchStrike(Vector3 hitPoint, Vector3 hitNormal, float damagePower, int targetCubeCount, bool isCharged)
    {
        Vector3 insidePos = hitPoint - hitNormal * (voxelSize * 0.45f) - transform.position;
        int centerX = Mathf.FloorToInt(insidePos.x / voxelSize);
        int centerY = Mathf.FloorToInt(insidePos.y / voxelSize);
        int centerZ = Mathf.FloorToInt(insidePos.z / voxelSize);

        GetGridAxesFromNormal(hitNormal, out Vector3Int rightAxis, out Vector3Int upAxis, out Vector3Int depthAxis);

        dirtyChunks.Clear();

        // Convert target cube count (5, 9, 13, 21, 29, 37, 49, 61, 77, 97) into diamond radius
        int radius = 1;
        if (targetCubeCount >= 9)  radius = 2;
        if (targetCubeCount >= 21) radius = 3;
        if (targetCubeCount >= 37) radius = 4;
        if (targetCubeCount >= 61) radius = 5;
        if (isCharged) radius += 1;

        int maxDepth = (CompanyStoreEconomyManager.Instance && CompanyStoreEconomyManager.Instance.rockDrillRestored) ? 3 : 1;
        int hitsApplied = 0;

        for (int d = 0; d < maxDepth; d++)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) != r) continue;
                        if (hitsApplied >= targetCubeCount * (isCharged ? 2 : 1)) break;

                        Vector3Int coord = new Vector3Int(centerX, centerY, centerZ) + rightAxis * dx + upAxis * dy + depthAxis * d;
                        ApplyDamageToVoxel(coord.x, coord.y, coord.z, damagePower, hitNormal, false);
                        hitsApplied++;
                    }
                }
            }
        }

        RebuildDirtyChunks();
    }

    private void HandleDynamiteBlast(Vector3 blastWorldPos, float blastRadius, float blastDamage)
    {
        Vector3 localPos = blastWorldPos - transform.position;
        int cx = Mathf.FloorToInt(localPos.x / voxelSize);
        int cy = Mathf.FloorToInt(localPos.y / voxelSize);
        int cz = Mathf.FloorToInt(localPos.z / voxelSize);
        int voxRadius = Mathf.CeilToInt(blastRadius / voxelSize);

        dirtyChunks.Clear();

        for (int x = cx - voxRadius; x <= cx + voxRadius; x++)
        {
            for (int y = cy - voxRadius; y <= cy + voxRadius; y++)
            {
                for (int z = cz - voxRadius; z <= cz + voxRadius; z++)
                {
                    float dist = Vector3.Distance(new Vector3(cx, cy, cz), new Vector3(x, y, z));
                    if (dist <= voxRadius)
                    {
                        Vector3 popDir = (VoxelToWorldCenter(x, y, z) - blastWorldPos).normalized;
                        ApplyDamageToVoxel(x, y, z, blastDamage, popDir, true);
                    }
                }
            }
        }

        RebuildDirtyChunks();
    }

    private void ApplyDamageToVoxel(int x, int y, int z, float damage, Vector3 popNormal, bool isDynamiteBlast)
    {
        if (x <= 0 || x >= totalVoxelsX - 1 || y <= 0 || y >= totalVoxelsY - 1 || z <= 0 || z >= totalVoxelsZ - 1)
            return;

        byte id = voxelIds[x, y, z];
        if (id == 0 || id >= materialLayers.Length) return;

        VoxelMaterialDef mat = materialLayers[id];
        voxelDamage[x, y, z] += damage;
        MarkChunkDirtyAtVoxel(x, y, z);

        if (voxelDamage[x, y, z] >= mat.maxHardness)
        {
            voxelIds[x, y, z] = 0;
            voxelDamage[x, y, z] = 0f;
            totalCubesBroken++;

            if (SaveManager.Instance != null)
                SaveManager.Instance.RecordVoxelEdit(x, y, z, totalVoxelsY, totalVoxelsZ, 0);

            if (UIScreenAndAccessibilityManager.Instance != null)
                UIScreenAndAccessibilityManager.Instance.NotifyFirstWallBroken();

            // GDD Section 5 Rule: Dynamite destroys treasure in its blast radius!
            if (isDynamiteBlast && mat.isTreasure)
                return;

            if (RubbleSuctionManager.Instance != null)
            {
                float oreMultiplier = CompanyStoreEconomyManager.Instance ? CompanyStoreEconomyManager.Instance.GetCurrentOreSenseMultiplier() : 1f;
                int finalCashValue = Mathf.Max(1, Mathf.RoundToInt(mat.cashValue * oreMultiplier));
                Vector3 spawnPos = VoxelToWorldCenter(x, y, z);
                RubbleSuctionManager.Instance.SpawnRubbleCube(spawnPos, popNormal, finalCashValue, mat.rubbleColor);
            }
        }
    }

    private void MarkChunkDirtyAtVoxel(int vx, int vy, int vz)
    {
        int cx = vx / CHUNK_SIZE;
        int cy = vy / CHUNK_SIZE;
        int cz = vz / CHUNK_SIZE;

        if (cx >= 0 && cx < chunksX && cy >= 0 && cy < chunksY && cz >= 0 && cz < chunksZ)
            dirtyChunks.Add(chunks[cx, cy, cz]);

        if (vx % CHUNK_SIZE == 0 && cx > 0) dirtyChunks.Add(chunks[cx - 1, cy, cz]);
        if (vx % CHUNK_SIZE == CHUNK_SIZE - 1 && cx < chunksX - 1) dirtyChunks.Add(chunks[cx + 1, cy, cz]);
        if (vy % CHUNK_SIZE == 0 && cy > 0) dirtyChunks.Add(chunks[cx, cy - 1, cz]);
        if (vy % CHUNK_SIZE == CHUNK_SIZE - 1 && cy < chunksY - 1) dirtyChunks.Add(chunks[cx, cy + 1, cz]);
        if (vz % CHUNK_SIZE == 0 && cz > 0) dirtyChunks.Add(chunks[cx, cy, cz - 1]);
        if (vz % CHUNK_SIZE == CHUNK_SIZE - 1 && cz < chunksZ - 1) dirtyChunks.Add(chunks[cx, cy, cz + 1]);
    }

    private void RebuildDirtyChunks()
    {
        foreach (VoxelChunk chunk in dirtyChunks)
        {
            if (chunk != null) chunk.RebuildMesh();
        }
    }

    public byte GetVoxelId(int x, int y, int z)
    {
        if (x < 0 || x >= totalVoxelsX || y < 0 || y >= totalVoxelsY || z < 0 || z >= totalVoxelsZ)
            return 1;
        return voxelIds[x, y, z];
    }

    public Color GetVoxelDisplayColor(int x, int y, int z)
    {
        byte id = GetVoxelId(x, y, z);
        if (id == 0 || id >= materialLayers.Length) return Color.white;

        VoxelMaterialDef mat = materialLayers[id];
        float dmgRatio = Mathf.Clamp01(voxelDamage[x, y, z] / Mathf.Max(0.01f, mat.maxHardness));
        return Color.Lerp(mat.rubbleColor, Color.black, dmgRatio * 0.45f);
    }

    public Vector3 VoxelToWorldCenter(int x, int y, int z)
    {
        return transform.position + new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
    }

    private void GetGridAxesFromNormal(Vector3 normal, out Vector3Int rightAxis, out Vector3Int upAxis, out Vector3Int depthAxis)
    {
        Vector3Int n = new Vector3Int(Mathf.RoundToInt(normal.x), Mathf.RoundToInt(normal.y), Mathf.RoundToInt(normal.z));
        depthAxis = -n;
        if (Mathf.Abs(n.y) > 0) { rightAxis = Vector3Int.right; upAxis = new Vector3Int(0, 0, 1); }
        else if (Mathf.Abs(n.x) > 0) { rightAxis = new Vector3Int(0, 0, 1); upAxis = Vector3Int.up; }
        else { rightAxis = Vector3Int.right; upAxis = Vector3Int.up; }
    }
}
