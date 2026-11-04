using System;
using System.Collections.Generic;
using BreakToEscape.Core;
using BreakToEscape.Core.Economy;
using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Owns the voxel grid, the generated mine layout and the chunk objects. Converts between
/// world metres and voxel units, applies punches and blasts, spawns rubble for broken cubes,
/// and rebuilds chunk meshes and colliders on a per-frame budget.
/// Initialised by <see cref="GameBootstrap"/>; it does no work in Awake.
/// </summary>
[DefaultExecutionOrder(-500)]
public class VoxelWorldManager : MonoBehaviour
{
    public static VoxelWorldManager Instance { get; private set; }

    [Header("Rendering")]
    [Tooltip("Material using the BreakToEscape/VoxelLit shader (vertex colours).")]
    public Material voxelVertexColorMaterial;
    [Tooltip("Physics layer for wall chunks (rubble and the player collide with it).")]
    public int voxelWallLayer = 6;

    [Header("Mine generation")]
    [Tooltip("Off = the hand-built version 1 mine. On = seeded procedural mine (New Shift, experimental).")]
    public bool useProceduralMine = false;
    public int proceduralSeed = 12345;

    [Header("Rebuild budget (per frame)")]
    [Tooltip("Chunk render meshes rebuilt per frame. A punch normally touches 1 to 4 chunks.")]
    public int maxMeshRebuildsPerFrame = 6;
    [Tooltip("Chunk collision meshes cooked per frame. Colliders follow the visible mesh one or more frames later.")]
    public int maxColliderBakesPerFrame = 2;
    [Tooltip("Chunks within this many chunks of the spawn are built during loading; the rest stream in.")]
    public int initialChunkRadius = 1;

    public VoxelGrid Grid { get; private set; }
    public MineLayout Layout { get; private set; }
    public float VoxelSize { get; private set; } = 0.5f;
    public bool IsReady { get; private set; }

    /// <summary>Raised after cubes break: (broken voxels, popDirection world, isBlast). The list is reused; copy if needed.</summary>
    public event Action<List<BrokenVoxel>, Vector3, bool> CubesBroken;

    private VoxelChunk[] chunks;
    private readonly List<int> drained = new List<int>(32);
    private readonly Queue<int> urgentMeshQueue = new Queue<int>(16);
    private readonly Queue<int> backgroundMeshQueue = new Queue<int>(128);
    private readonly HashSet<int> urgentQueued = new HashSet<int>();
    private readonly HashSet<int> meshDone = new HashSet<int>();
    private readonly Queue<int> colliderQueue = new Queue<int>();
    private readonly HashSet<int> colliderQueued = new HashSet<int>();
    private readonly List<Int3> scratchCells = new List<Int3>(512);
    private readonly List<BrokenVoxel> scratchBroken = new List<BrokenVoxel>(512);

    private EconomyData data;
    private EconomyState economy;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable() => StickyDynamite.Detonated += OnDynamiteDetonated;
    void OnDisable() => StickyDynamite.Detonated -= OnDynamiteDetonated;

    private void OnDynamiteDetonated(Vector3 position, float radiusMeters, float damage) => ApplyBlast(position, radiusMeters, damage);

    /// <summary>Generates the mine. Call once, before applying saved edits and before building meshes.</summary>
    public void Initialize(EconomyData economyData, EconomyState economyState)
    {
        data = economyData;
        economy = economyState;
        VoxelSize = data.voxelSize;
        var table = new VoxelMaterialTable(data);
        Int3 size = data.worldSizeVoxels;
        Grid = new VoxelGrid(size.x, size.y, size.z, table);
        Layout = useProceduralMine
            ? ProceduralMineGenerator.Generate(Grid, proceduralSeed, data.treasureOneInVoxels, data.nookTreasureClusterSize)
            : HandBuiltMineGenerator.Generate(Grid, data.treasureOneInVoxels, data.nookTreasureClusterSize);

        if (!voxelVertexColorMaterial)
            Debug.LogError("VoxelWorldManager: voxelVertexColorMaterial is not assigned; walls will not render. Run the scene builder again.", this);
    }

    /// <summary>Creates the chunk objects and builds the meshes near the spawn now; the rest stream in over frames.</summary>
    public void BuildChunks(Vector3 focusWorld)
    {
        if (Grid == null) { Debug.LogError("VoxelWorldManager.BuildChunks called before Initialize.", this); return; }
        chunks = new VoxelChunk[Grid.ChunkCount];
        float chunkMeters = VoxelGrid.ChunkSize * VoxelSize;
        Int3 focus = WorldToVoxel(focusWorld);
        int fx = focus.x / VoxelGrid.ChunkSize, fy = focus.y / VoxelGrid.ChunkSize, fz = focus.z / VoxelGrid.ChunkSize;

        var order = new List<int>(Grid.ChunkCount);
        for (int cx = 0; cx < Grid.chunksX; cx++)
            for (int cy = 0; cy < Grid.chunksY; cy++)
                for (int cz = 0; cz < Grid.chunksZ; cz++)
                {
                    int index = Grid.ChunkIndex(cx, cy, cz);
                    var go = new GameObject("Chunk_" + cx + "_" + cy + "_" + cz);
                    go.layer = voxelWallLayer;
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(cx * chunkMeters, cy * chunkMeters, cz * chunkMeters);
                    VoxelChunk chunk = go.AddComponent<VoxelChunk>();
                    chunk.Initialize(this, cx, cy, cz, voxelVertexColorMaterial);
                    chunks[index] = chunk;
                    order.Add(index);
                }

        order.Sort((a, b) => ChunkDistance(a, fx, fy, fz).CompareTo(ChunkDistance(b, fx, fy, fz)));
        foreach (int index in order)
        {
            Int3 c = Grid.ChunkCoord(index);
            bool near = Math.Abs(c.x - fx) <= initialChunkRadius && Math.Abs(c.y - fy) <= initialChunkRadius && Math.Abs(c.z - fz) <= initialChunkRadius;
            if (near)
            {
                chunks[index].RebuildRenderMesh();
                chunks[index].RebuildCollider();
                meshDone.Add(index);
            }
            else
            {
                EnqueueMesh(index, false);
            }
        }
        IsReady = true;
    }

    private int ChunkDistance(int index, int fx, int fy, int fz)
    {
        Int3 c = Grid.ChunkCoord(index);
        return Math.Abs(c.x - fx) + Math.Abs(c.y - fy) + Math.Abs(c.z - fz);
    }

    void Update()
    {
        if (!IsReady) return;

        if (Grid.HasDirtyChunks)
        {
            drained.Clear();
            Grid.DrainDirtyChunks(drained);
            for (int i = 0; i < drained.Count; i++) EnqueueMesh(drained[i], true);
        }

        int meshes = 0;
        while (meshes < maxMeshRebuildsPerFrame && (urgentMeshQueue.Count > 0 || backgroundMeshQueue.Count > 0))
        {
            int index;
            if (urgentMeshQueue.Count > 0)
            {
                index = urgentMeshQueue.Dequeue();
                urgentQueued.Remove(index);
            }
            else
            {
                index = backgroundMeshQueue.Dequeue();
                if (meshDone.Contains(index)) continue;
            }
            chunks[index].RebuildRenderMesh();
            meshDone.Add(index);
            if (colliderQueued.Add(index)) colliderQueue.Enqueue(index);
            meshes++;
        }

        int bakes = 0;
        while (colliderQueue.Count > 0 && bakes < maxColliderBakesPerFrame)
        {
            int index = colliderQueue.Dequeue();
            colliderQueued.Remove(index);
            chunks[index].RebuildCollider();
            bakes++;
        }
    }

    /// <summary>
    /// Queues a chunk for a render-mesh rebuild. Urgent chunks (just damaged) are rebuilt before
    /// chunks that are still streaming in after loading. Queues are reused, so this does not allocate.
    /// </summary>
    private void EnqueueMesh(int index, bool urgent)
    {
        if (urgent)
        {
            if (urgentQueued.Add(index)) urgentMeshQueue.Enqueue(index);
        }
        else
        {
            backgroundMeshQueue.Enqueue(index);
        }
    }

    // ------------------------------------------------------------------ coordinates

    public Vector3 Origin => transform.position;

    /// <summary>World metres → voxel units (floating point, relative to the grid origin).</summary>
    public Float3 WorldToVoxelUnits(Vector3 world)
    {
        Vector3 local = (world - Origin) / VoxelSize;
        return new Float3(local.x, local.y, local.z);
    }

    public Int3 WorldToVoxel(Vector3 world)
    {
        Float3 v = WorldToVoxelUnits(world);
        return new Int3(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
    }

    /// <summary>Voxel units → world metres.</summary>
    public Vector3 VoxelUnitsToWorld(Float3 v) => Origin + new Vector3(v.x, v.y, v.z) * VoxelSize;

    public Vector3 VoxelCenterToWorld(Int3 v) => Origin + new Vector3(v.x + 0.5f, v.y + 0.5f, v.z + 0.5f) * VoxelSize;

    public Vector3 FaceCenterToWorld(Int3 voxel, Int3 normal) =>
        VoxelCenterToWorld(voxel) + new Vector3(normal.x, normal.y, normal.z) * (VoxelSize * 0.5f);

    public static Vector3 ToVector3(Int3 v) => new Vector3(v.x, v.y, v.z);
    public static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);

    /// <summary>Grid ray from a world-space ray (metres). Max distance in metres.</summary>
    public GridHit RaycastWorld(Vector3 origin, Vector3 direction, float maxDistanceMeters)
    {
        if (Grid == null) return default;
        return GridRaycast.Cast(Grid, WorldToVoxelUnits(origin), ToFloat3(direction), maxDistanceMeters / VoxelSize);
    }

    // ------------------------------------------------------------------ damage

    /// <summary>
    /// Applies a punch at a struck voxel face. Returns the number of cubes broken. Spawns rubble
    /// through <see cref="RubbleSuctionManager"/> with Ore Sense applied.
    /// </summary>
    public int ApplyPunch(Int3 voxel, Int3 faceNormal, int patternLevel, int depthLayers, float damage)
    {
        if (Grid == null) return 0;
        scratchBroken.Clear();
        PunchResolver.ApplyPunch(Grid, voxel, faceNormal, patternLevel, depthLayers, damage, scratchCells, scratchBroken);
        Vector3 pop = ToVector3(faceNormal);
        SpawnRubbleFor(scratchBroken, pop, false);
        return scratchBroken.Count;
    }

    /// <summary>Applies a dynamite blast centred at a world position. Treasure caught in the blast is destroyed (design rule).</summary>
    public int ApplyBlast(Vector3 worldCenter, float radiusMeters, float damage)
    {
        if (Grid == null) return 0;
        scratchBroken.Clear();
        PunchResolver.ApplyBlast(Grid, WorldToVoxelUnits(worldCenter), radiusMeters / VoxelSize, damage, scratchBroken);
        SpawnRubbleFor(scratchBroken, Vector3.up, true);
        return scratchBroken.Count;
    }

    private void SpawnRubbleFor(List<BrokenVoxel> broken, Vector3 popDirection, bool isBlast)
    {
        if (broken.Count == 0) return;
        RubbleSuctionManager rubble = RubbleSuctionManager.Instance;
        for (int i = 0; i < broken.Count; i++)
        {
            VoxelMaterial mat = Grid.materials.Get(broken[i].material);
            if (isBlast && mat.isTreasure) continue;
            if (!rubble) continue;
            int value = economy != null ? economy.CubeCashValue(mat.value) : Mathf.Max(1, Mathf.RoundToInt(mat.value));
            Vector3 pos = VoxelCenterToWorld(broken[i].position);
            Vector3 dir = isBlast ? (pos - VoxelCenterToWorld(broken[0].position) + Vector3.up).normalized : popDirection;
            rubble.SpawnRubbleCube(pos, dir, value, broken[i].material, MaterialColor(mat));
        }
        CubesBroken?.Invoke(broken, popDirection, isBlast);
    }

    public static Color MaterialColor(VoxelMaterial m) => new Color(m.r, m.g, m.b, 1f);

    /// <summary>Band (1..6) at a world position, used for band-entered captions and audio.</summary>
    public int BandAtWorld(Vector3 world)
    {
        if (Grid == null || Layout == null) return 1;
        Int3 v = WorldToVoxel(world);
        Int3 c = Layout.chamberCenter;
        return Layout.isProcedural ? ProceduralMineGenerator.BandAt(v.x, v.z, c.x, c.z, Grid.sizeX, Grid.sizeZ) : HandBuiltMineGenerator.BandAt(v.x, v.z, c.x, c.z);
    }
}
