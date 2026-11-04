using System;

namespace BreakToEscape.Core
{
    /// <summary>Integer 3D coordinate (voxel index). Unity-independent.</summary>
    [Serializable]
    public struct Int3 : IEquatable<Int3>
    {
        public int x, y, z;

        public Int3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }

        public static readonly Int3 Zero = new Int3(0, 0, 0);
        public static readonly Int3 UnitX = new Int3(1, 0, 0);
        public static readonly Int3 UnitY = new Int3(0, 1, 0);
        public static readonly Int3 UnitZ = new Int3(0, 0, 1);

        public static Int3 operator +(Int3 a, Int3 b) => new Int3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Int3 operator -(Int3 a, Int3 b) => new Int3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Int3 operator -(Int3 a) => new Int3(-a.x, -a.y, -a.z);
        public static Int3 operator *(Int3 a, int s) => new Int3(a.x * s, a.y * s, a.z * s);
        public static bool operator ==(Int3 a, Int3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Int3 a, Int3 b) => !(a == b);

        public bool Equals(Int3 other) => this == other;
        public override bool Equals(object obj) => obj is Int3 other && this == other;
        public override int GetHashCode() => (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
        public override string ToString() => "(" + x + ", " + y + ", " + z + ")";
    }

    /// <summary>Integer 2D offset (for punch pattern cells on a wall face).</summary>
    public struct Int2 : IEquatable<Int2>
    {
        public int x, y;
        public Int2(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(Int2 other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Int2 other && Equals(other);
        public override int GetHashCode() => (x * 397) ^ y;
        public override string ToString() => "(" + x + ", " + y + ")";
    }

    /// <summary>Float 3D vector for Unity-independent geometry (grid rays, layout points).</summary>
    [Serializable]
    public struct Float3
    {
        public float x, y, z;

        public Float3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Float3 operator +(Float3 a, Float3 b) => new Float3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Float3 operator -(Float3 a, Float3 b) => new Float3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Float3 operator *(Float3 a, float s) => new Float3(a.x * s, a.y * s, a.z * s);
        public static Float3 operator /(Float3 a, float s) => new Float3(a.x / s, a.y / s, a.z / s);

        public float Length => (float)Math.Sqrt(x * x + y * y + z * z);

        public Float3 Normalized
        {
            get
            {
                float len = Length;
                return len > 1e-6f ? this / len : new Float3(0f, 0f, 0f);
            }
        }

        public static float Dot(Float3 a, Float3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public override string ToString() => "(" + x + ", " + y + ", " + z + ")";
    }

    /// <summary>Axis-aligned integer box in voxel coordinates, max is exclusive.</summary>
    [Serializable]
    public struct VoxelBox
    {
        public Int3 min;
        public Int3 max;

        public VoxelBox(Int3 min, Int3 max) { this.min = min; this.max = max; }

        public VoxelBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            min = new Int3(minX, minY, minZ);
            max = new Int3(maxX, maxY, maxZ);
        }

        public Int3 Size => max - min;

        public bool Contains(Int3 p) =>
            p.x >= min.x && p.x < max.x && p.y >= min.y && p.y < max.y && p.z >= min.z && p.z < max.z;

        /// <summary>Centre of the box in voxel units (floating point).</summary>
        public Float3 Center => new Float3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f);

        /// <summary>Centre of the floor cell row: x/z centre, y = min.y (the first air layer).</summary>
        public Int3 FloorCenter => new Int3((min.x + max.x) / 2, min.y, (min.z + max.z) / 2);

        public bool Intersects(VoxelBox o) =>
            min.x < o.max.x && max.x > o.min.x && min.y < o.max.y && max.y > o.min.y && min.z < o.max.z && max.z > o.min.z;

        public VoxelBox Expanded(int amount) =>
            new VoxelBox(min - new Int3(amount, amount, amount), max + new Int3(amount, amount, amount));

        public override string ToString() => "[" + min + " .. " + max + ")";
    }

    /// <summary>Small deterministic pseudo-random generator (xorshift32). Same seed, same sequence, on every platform.</summary>
    public sealed class DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = (uint)seed;
            if (state == 0) state = 0x9E3779B9u;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>Integer in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        /// <summary>Float in [0, 1).</summary>
        public float NextFloat() => (NextUInt() & 0xFFFFFF) / 16777216f;

        /// <summary>Stateless hash of a voxel coordinate and seed, for density decisions that must not depend on visit order.</summary>
        public static uint Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x27d4eb2du;
                h ^= (uint)x * 0x85ebca6bu;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xc2b2ae35u;
                h = (h << 11) | (h >> 21);
                h ^= (uint)z * 0x165667b1u;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
