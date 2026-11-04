using System;
using System.Collections.Generic;

namespace BreakToEscape.Core.World
{
    /// <summary>
    /// Explicit, symmetric punch pattern shapes for Punch Pattern levels 1..10.
    /// Cells are (x, y) offsets on the struck wall face. Each level reproduces the
    /// designed cube count exactly: 5, 9, 13, 21, 29, 37, 49, 61, 77, 97.
    /// The wireframe preview and the punch both read these same cells.
    /// </summary>
    public static class PunchPatterns
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        /// <summary>Designed cube counts per level, from the economy spreadsheet.</summary>
        public static readonly int[] DesignedCounts = { 5, 9, 13, 21, 29, 37, 49, 61, 77, 97 };

        private static readonly Int2[][] Shapes = BuildAll();

        /// <summary>Cells for a pattern level (clamped to 1..10). The array is shared; do not modify it.</summary>
        public static Int2[] Cells(int level) => Shapes[ClampLevel(level) - 1];

        public static int ClampLevel(int level) => level < MinLevel ? MinLevel : (level > MaxLevel ? MaxLevel : level);

        /// <summary>
        /// Rule for one level, written exactly as the design defines it.
        /// a = |x|, b = |y|, m = max(a, b), s = a + b.
        /// </summary>
        public static bool Contains(int level, int x, int y)
        {
            int a = Math.Abs(x), b = Math.Abs(y);
            int m = Math.Max(a, b), s = a + b;
            switch (ClampLevel(level))
            {
                case 1: return s <= 1;
                case 2: return m <= 1;
                case 3: return s <= 2;
                case 4: return m <= 2 && !(a == 2 && b == 2);
                case 5: return m <= 2 || (a == 3 && b == 0) || (a == 0 && b == 3);
                case 6: return m <= 3 && s <= 4;
                case 7: return m <= 3;
                case 8: return s <= 5;
                case 9: return m <= 4 && !(a == 4 && b == 4);
                default: return m <= 5 && s <= 7;
            }
        }

        private static Int2[][] BuildAll()
        {
            var all = new Int2[MaxLevel][];
            var cells = new List<Int2>(128);
            for (int level = MinLevel; level <= MaxLevel; level++)
            {
                cells.Clear();
                // Centre first, then rings outward, so the first cell is always the aimed cube.
                for (int ring = 0; ring <= 7; ring++)
                    for (int x = -ring; x <= ring; x++)
                        for (int y = -ring; y <= ring; y++)
                            if (Math.Max(Math.Abs(x), Math.Abs(y)) == ring && Contains(level, x, y))
                                cells.Add(new Int2(x, y));
                all[level - 1] = cells.ToArray();
            }
            return all;
        }

        /// <summary>
        /// Charged punch rule: the charge ratio (0..1) adds up to <paramref name="maxBonusLevels"/>
        /// pattern levels (floor of ratio × bonus), capped at level 10.
        /// </summary>
        public static int ChargedLevel(int baseLevel, float chargeRatio, int maxBonusLevels)
        {
            float c = chargeRatio < 0f ? 0f : (chargeRatio > 1f ? 1f : chargeRatio);
            int bonus = (int)Math.Floor(c * maxBonusLevels + 1e-4f);
            return ClampLevel(ClampLevel(baseLevel) + bonus);
        }

        /// <summary>Charged punch damage multiplier: 1 at no charge up to <paramref name="maxMultiplier"/> at full charge.</summary>
        public static float ChargedDamageMultiplier(float chargeRatio, float maxMultiplier)
        {
            float c = chargeRatio < 0f ? 0f : (chargeRatio > 1f ? 1f : chargeRatio);
            return 1f + (maxMultiplier - 1f) * c;
        }
    }

    /// <summary>Wall-face axes for a struck face normal.</summary>
    public struct FaceAxes
    {
        public Int3 right, up, depth;

        /// <summary>
        /// Axes for a face normal pointing out of the struck voxel (towards the player).
        /// depth points into the wall (−normal).
        /// </summary>
        public static FaceAxes FromNormal(Int3 normal)
        {
            var axes = new FaceAxes { depth = -normal };
            if (normal.y != 0) { axes.right = Int3.UnitX; axes.up = Int3.UnitZ; }
            else if (normal.x != 0) { axes.right = Int3.UnitZ; axes.up = Int3.UnitY; }
            else { axes.right = Int3.UnitX; axes.up = Int3.UnitY; }
            return axes;
        }
    }

    /// <summary>One voxel broken by a punch or blast.</summary>
    public struct BrokenVoxel
    {
        public Int3 position;
        public byte material;
    }

    /// <summary>Applies punch patterns and blasts to a <see cref="VoxelGrid"/>.</summary>
    public static class PunchResolver
    {
        /// <summary>
        /// Lists the voxels a punch will target: the pattern cells on the face, repeated for each
        /// depth layer (Rock Drill). Solid-or-not filtering is left to the caller so the preview
        /// can show exactly the same cells the punch uses.
        /// </summary>
        public static void TargetCells(Int3 center, Int3 faceNormal, int patternLevel, int depthLayers, List<Int3> output)
        {
            FaceAxes axes = FaceAxes.FromNormal(faceNormal);
            Int2[] cells = PunchPatterns.Cells(patternLevel);
            int layers = Math.Max(1, depthLayers);
            for (int d = 0; d < layers; d++)
            {
                Int3 layerOrigin = center + axes.depth * d;
                for (int i = 0; i < cells.Length; i++)
                    output.Add(layerOrigin + axes.right * cells[i].x + axes.up * cells[i].y);
            }
        }

        /// <summary>
        /// Applies one punch. Every pattern cell in every depth layer receives the full damage
        /// independently (each layer has its own cell loop, so later layers are never skipped).
        /// Returns the number of voxels that received damage.
        /// </summary>
        public static int ApplyPunch(VoxelGrid grid, Int3 center, Int3 faceNormal, int patternLevel, int depthLayers,
            float damage, List<Int3> scratch, List<BrokenVoxel> broken)
        {
            scratch.Clear();
            TargetCells(center, faceNormal, patternLevel, depthLayers, scratch);
            int touched = 0;
            for (int i = 0; i < scratch.Count; i++)
            {
                Int3 p = scratch[i];
                DamageOutcome outcome = grid.ApplyDamage(p.x, p.y, p.z, damage, out byte mat);
                if (outcome == DamageOutcome.Ignored) continue;
                touched++;
                if (outcome == DamageOutcome.Broken) broken.Add(new BrokenVoxel { position = p, material = mat });
            }
            return touched;
        }

        /// <summary>Spherical blast in voxel units. Returns the number of voxels damaged.</summary>
        public static int ApplyBlast(VoxelGrid grid, Float3 centerVoxelUnits, float radiusVoxels, float damage, List<BrokenVoxel> broken)
        {
            int r = (int)Math.Ceiling(radiusVoxels);
            int cx = (int)Math.Floor(centerVoxelUnits.x), cy = (int)Math.Floor(centerVoxelUnits.y), cz = (int)Math.Floor(centerVoxelUnits.z);
            float r2 = radiusVoxels * radiusVoxels;
            int touched = 0;
            for (int x = cx - r; x <= cx + r; x++)
                for (int y = cy - r; y <= cy + r; y++)
                    for (int z = cz - r; z <= cz + r; z++)
                    {
                        float dx = x + 0.5f - centerVoxelUnits.x, dy = y + 0.5f - centerVoxelUnits.y, dz = z + 0.5f - centerVoxelUnits.z;
                        if (dx * dx + dy * dy + dz * dz > r2) continue;
                        DamageOutcome outcome = grid.ApplyDamage(x, y, z, damage, out byte mat);
                        if (outcome == DamageOutcome.Ignored) continue;
                        touched++;
                        if (outcome == DamageOutcome.Broken) broken.Add(new BrokenVoxel { position = new Int3(x, y, z), material = mat });
                    }
            return touched;
        }
    }
}
