using System;
using System.Collections.Generic;
using BreakToEscape.Core.Economy;

namespace BreakToEscape.Core.World
{
    /// <summary>Data for one voxel material. Id 0 is air.</summary>
    public struct VoxelMaterial
    {
        public byte id;
        public string name;
        public float hp;
        public float value;
        public float r, g, b;
        public bool isTreasure;
        public bool glows;
        /// <summary>Band number 1..6 for rock, or the first band a treasure seam appears in.</summary>
        public int band;
        /// <summary>Bit mask of bands (bit n = band n) a treasure seam appears in.</summary>
        public int bandMask;
    }

    /// <summary>
    /// Material table built from economy.json: id 0 = air, ids 1..6 = the six rock bands,
    /// ids 7..9 = treasure seams. Hit points and values come from the data, never from code.
    /// </summary>
    public sealed class VoxelMaterialTable
    {
        public const byte Air = 0;
        public const int MaxMaterials = 16;

        private readonly VoxelMaterial[] materials = new VoxelMaterial[MaxMaterials];
        private readonly bool[] defined = new bool[MaxMaterials];

        public VoxelMaterialTable(EconomyData data)
        {
            materials[0] = new VoxelMaterial { id = 0, name = "Air" };
            defined[0] = true;
            foreach (BandDefinition band in data.bands)
            {
                EconomyData.ParseHexColor(band.colorHex, out float r, out float g, out float b);
                Define(new VoxelMaterial
                {
                    id = (byte)band.id, name = band.name, hp = band.hp, value = band.value,
                    r = r, g = g, b = b, band = band.id, glows = band.id == 6
                });
            }
            foreach (TreasureMaterialDefinition t in data.treasureMaterials)
            {
                EconomyData.ParseHexColor(t.colorHex, out float r, out float g, out float b);
                int mask = 0;
                if (t.bands != null) foreach (int bandNumber in t.bands) mask |= 1 << bandNumber;
                Define(new VoxelMaterial
                {
                    id = (byte)t.id, name = t.name, hp = t.hp, value = t.value,
                    r = r, g = g, b = b, isTreasure = true, glows = t.glows, bandMask = mask,
                    band = t.bands != null && t.bands.Length > 0 ? t.bands[0] : 1
                });
            }
        }

        private void Define(VoxelMaterial m)
        {
            if (m.id >= MaxMaterials) throw new ArgumentOutOfRangeException(nameof(m), "Material id " + m.id + " exceeds table size");
            materials[m.id] = m;
            defined[m.id] = true;
        }

        public bool IsDefined(byte id) => id < MaxMaterials && defined[id];

        public VoxelMaterial Get(byte id) => IsDefined(id) ? materials[id] : materials[1];

        /// <summary>Treasure material id used in a given band (1..6), or 0 if none is configured.</summary>
        public byte TreasureForBand(int band)
        {
            for (int i = 0; i < MaxMaterials; i++)
            {
                if (!defined[i] || !materials[i].isTreasure) continue;
                if ((materials[i].bandMask & (1 << band)) != 0) return (byte)i;
            }
            return 0;
        }

        public IEnumerable<VoxelMaterial> All()
        {
            for (int i = 0; i < MaxMaterials; i++) if (defined[i]) yield return materials[i];
        }
    }

    public enum DamageOutcome
    {
        Ignored,
        Damaged,
        Broken
    }

    /// <summary>
    /// The mine as a flat array of material ids plus accumulated damage. Unity-independent.
    /// Flat index = (x * sizeY + y) * sizeZ + z, the same layout the first save format used.
    /// The outermost shell of voxels is unbreakable so the player can never leave the grid.
    /// </summary>
    public sealed class VoxelGrid
    {
        public const int ChunkSize = 16;

        public readonly int sizeX, sizeY, sizeZ;
        public readonly int chunksX, chunksY, chunksZ;
        public readonly VoxelMaterialTable materials;

        private readonly byte[] ids;
        private readonly float[] damage;

        /// <summary>Broken voxels since generation: flat index → new id (0). Saved as a sparse delta.</summary>
        private readonly Dictionary<int, byte> edits = new Dictionary<int, byte>(4096);
        /// <summary>Flat indices that hold partial damage (non-zero and below break threshold).</summary>
        private readonly HashSet<int> damaged = new HashSet<int>();
        private readonly HashSet<int> dirtyChunks = new HashSet<int>();

        public int BrokenCount { get; private set; }

        public VoxelGrid(int sizeX, int sizeY, int sizeZ, VoxelMaterialTable materials)
        {
            if (sizeX % ChunkSize != 0 || sizeY % ChunkSize != 0 || sizeZ % ChunkSize != 0)
                throw new ArgumentException("Grid size must be a multiple of " + ChunkSize);
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
            chunksX = sizeX / ChunkSize;
            chunksY = sizeY / ChunkSize;
            chunksZ = sizeZ / ChunkSize;
            this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
            ids = new byte[sizeX * sizeY * sizeZ];
            damage = new float[ids.Length];
        }

        public int VoxelCount => ids.Length;
        public int ChunkCount => chunksX * chunksY * chunksZ;

        /// <summary>Direct read access for the mesh builder hot loop (do not write through this).</summary>
        public byte[] RawIds => ids;

        public int Index(int x, int y, int z) => (x * sizeY + y) * sizeZ + z;

        public Int3 FromIndex(int index)
        {
            int z = index % sizeZ;
            int rest = index / sizeZ;
            int y = rest % sizeY;
            int x = rest / sizeY;
            return new Int3(x, y, z);
        }

        public bool InBounds(int x, int y, int z) =>
            x >= 0 && y >= 0 && z >= 0 && x < sizeX && y < sizeY && z < sizeZ;

        public bool IsBoundary(int x, int y, int z) =>
            x <= 0 || y <= 0 || z <= 0 || x >= sizeX - 1 || y >= sizeY - 1 || z >= sizeZ - 1;

        /// <summary>Material id; outside the grid counts as solid band-1 rock.</summary>
        public byte Get(int x, int y, int z) => InBounds(x, y, z) ? ids[Index(x, y, z)] : (byte)1;

        public byte Get(Int3 p) => Get(p.x, p.y, p.z);

        public bool IsSolid(int x, int y, int z) => Get(x, y, z) != VoxelMaterialTable.Air;

        public float DamageAt(int x, int y, int z) => InBounds(x, y, z) ? damage[Index(x, y, z)] : 0f;

        /// <summary>0..1 damage ratio used to darken damaged cubes.</summary>
        public float DamageRatio(int x, int y, int z)
        {
            byte id = Get(x, y, z);
            if (id == 0) return 0f;
            float hp = materials.Get(id).hp;
            return hp <= 0f ? 0f : Math.Min(1f, DamageAt(x, y, z) / hp);
        }

        /// <summary>Generation-time write. Not recorded as an edit.</summary>
        public void SetGenerated(int x, int y, int z, byte id)
        {
            if (!InBounds(x, y, z)) return;
            ids[Index(x, y, z)] = id;
        }

        public void FillGenerated(byte id)
        {
            for (int i = 0; i < ids.Length; i++) ids[i] = id;
        }

        /// <summary>Carves a box to air at generation time (max exclusive). The boundary shell is kept.</summary>
        public void CarveGenerated(VoxelBox box)
        {
            int x0 = Math.Max(1, box.min.x), x1 = Math.Min(sizeX - 1, box.max.x);
            int y0 = Math.Max(1, box.min.y), y1 = Math.Min(sizeY - 1, box.max.y);
            int z0 = Math.Max(1, box.min.z), z1 = Math.Min(sizeZ - 1, box.max.z);
            for (int x = x0; x < x1; x++)
                for (int y = y0; y < y1; y++)
                    for (int z = z0; z < z1; z++)
                        ids[Index(x, y, z)] = 0;
        }

        /// <summary>
        /// Adds damage to one voxel. Returns Broken when it reaches the material's hit points,
        /// which turns the voxel to air, records the edit and marks the chunk (and touching
        /// neighbour chunks) dirty. <paramref name="brokenMaterial"/> is the material before it broke.
        /// </summary>
        public DamageOutcome ApplyDamage(int x, int y, int z, float amount, out byte brokenMaterial)
        {
            brokenMaterial = 0;
            if (amount <= 0f || !InBounds(x, y, z) || IsBoundary(x, y, z)) return DamageOutcome.Ignored;
            int i = Index(x, y, z);
            byte id = ids[i];
            if (id == 0 || !materials.IsDefined(id)) return DamageOutcome.Ignored;

            float hp = materials.Get(id).hp;
            damage[i] += amount;
            MarkDirty(x, y, z);
            if (damage[i] + 1e-4f >= hp)
            {
                ids[i] = 0;
                damage[i] = 0f;
                damaged.Remove(i);
                edits[i] = 0;
                BrokenCount++;
                brokenMaterial = id;
                return DamageOutcome.Broken;
            }
            damaged.Add(i);
            return DamageOutcome.Damaged;
        }

        private void MarkDirty(int x, int y, int z)
        {
            int cx = x / ChunkSize, cy = y / ChunkSize, cz = z / ChunkSize;
            AddDirty(cx, cy, cz);
            int lx = x % ChunkSize, ly = y % ChunkSize, lz = z % ChunkSize;
            if (lx == 0) AddDirty(cx - 1, cy, cz);
            if (lx == ChunkSize - 1) AddDirty(cx + 1, cy, cz);
            if (ly == 0) AddDirty(cx, cy - 1, cz);
            if (ly == ChunkSize - 1) AddDirty(cx, cy + 1, cz);
            if (lz == 0) AddDirty(cx, cy, cz - 1);
            if (lz == ChunkSize - 1) AddDirty(cx, cy, cz + 1);
        }

        private void AddDirty(int cx, int cy, int cz)
        {
            if (cx < 0 || cy < 0 || cz < 0 || cx >= chunksX || cy >= chunksY || cz >= chunksZ) return;
            dirtyChunks.Add(ChunkIndex(cx, cy, cz));
        }

        public int ChunkIndex(int cx, int cy, int cz) => (cx * chunksY + cy) * chunksZ + cz;

        public Int3 ChunkCoord(int chunkIndex)
        {
            int cz = chunkIndex % chunksZ;
            int rest = chunkIndex / chunksZ;
            return new Int3(rest / chunksY, rest % chunksY, cz);
        }

        /// <summary>Moves the dirty chunk indices into <paramref name="output"/> and clears the set.</summary>
        public void DrainDirtyChunks(List<int> output)
        {
            foreach (int c in dirtyChunks) output.Add(c);
            dirtyChunks.Clear();
        }

        public bool HasDirtyChunks => dirtyChunks.Count > 0;

        // ------------------------------------------------------------------ save support

        /// <summary>Copies the broken-voxel delta (flat index → id) into a list of pairs.</summary>
        public void GetEdits(List<int> indices, List<byte> newIds)
        {
            foreach (var kv in edits)
            {
                indices.Add(kv.Key);
                newIds.Add(kv.Value);
            }
        }

        public int EditCount => edits.Count;

        /// <summary>Partial damage on unbroken voxels: flat index → accumulated damage.</summary>
        public void GetPartialDamage(List<int> indices, List<float> amounts)
        {
            foreach (int i in damaged)
            {
                if (ids[i] == 0 || damage[i] <= 0f) continue;
                indices.Add(i);
                amounts.Add(damage[i]);
            }
        }

        /// <summary>
        /// Applies a saved delta on top of a freshly generated mine. Call before the first mesh
        /// build. Invalid indices are skipped. Returns the number of edits applied.
        /// </summary>
        public int ApplySavedEdits(IList<int> indices, IList<byte> newIds, int brokenCount)
        {
            int applied = 0;
            int n = Math.Min(indices.Count, newIds.Count);
            for (int k = 0; k < n; k++)
            {
                int i = indices[k];
                if (i < 0 || i >= ids.Length) continue;
                Int3 p = FromIndex(i);
                if (IsBoundary(p.x, p.y, p.z)) continue;
                byte id = newIds[k];
                if (id != 0 && !materials.IsDefined(id)) continue;
                ids[i] = id;
                damage[i] = 0f;
                edits[i] = id;
                applied++;
            }
            BrokenCount = Math.Max(brokenCount, applied);
            return applied;
        }

        public int ApplySavedDamage(IList<int> indices, IList<float> amounts)
        {
            int applied = 0;
            int n = Math.Min(indices.Count, amounts.Count);
            for (int k = 0; k < n; k++)
            {
                int i = indices[k];
                if (i < 0 || i >= ids.Length || ids[i] == 0) continue;
                float hp = materials.Get(ids[i]).hp;
                float amount = amounts[k];
                if (amount <= 0f || float.IsNaN(amount)) continue;
                damage[i] = Math.Min(amount, hp * 0.999f);
                damaged.Add(i);
                applied++;
            }
            return applied;
        }
    }
}
