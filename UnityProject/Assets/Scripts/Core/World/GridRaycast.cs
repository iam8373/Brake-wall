using System;

namespace BreakToEscape.Core.World
{
    /// <summary>Result of a voxel grid ray query.</summary>
    public struct GridHit
    {
        public bool hit;
        /// <summary>The first solid voxel along the ray.</summary>
        public Int3 voxel;
        /// <summary>Face normal of the entered face, pointing back towards the ray origin. Zero when started inside.</summary>
        public Int3 normal;
        /// <summary>Distance along the ray in voxel units.</summary>
        public float distance;
        /// <summary>True if the ray origin was already inside a solid voxel.</summary>
        public bool startedInside;
    }

    /// <summary>
    /// Voxel grid ray traversal (Amanatides and Woo, "A Fast Voxel Traversal Algorithm").
    /// Used for aiming, the punch preview, punches, dynamite placement and pickups, so none
    /// of them need per-frame physics queries. All inputs are in voxel units
    /// (world position relative to the grid origin, divided by the voxel size).
    /// </summary>
    public static class GridRaycast
    {
        private const int MaxSteps = 512;

        public static GridHit Cast(VoxelGrid grid, Float3 origin, Float3 direction, float maxDistance)
        {
            var result = new GridHit();
            Float3 dir = direction.Normalized;
            if (dir.x == 0f && dir.y == 0f && dir.z == 0f) return result;

            int x = (int)Math.Floor(origin.x);
            int y = (int)Math.Floor(origin.y);
            int z = (int)Math.Floor(origin.z);

            if (grid.IsSolid(x, y, z))
            {
                result.hit = true;
                result.voxel = new Int3(x, y, z);
                result.startedInside = true;
                result.distance = 0f;
                result.normal = DominantAxisNormal(dir);
                return result;
            }

            int stepX = Math.Sign(dir.x), stepY = Math.Sign(dir.y), stepZ = Math.Sign(dir.z);
            float tMaxX = Boundary(origin.x, x, stepX, dir.x);
            float tMaxY = Boundary(origin.y, y, stepY, dir.y);
            float tMaxZ = Boundary(origin.z, z, stepZ, dir.z);
            float tDeltaX = stepX != 0 ? Math.Abs(1f / dir.x) : float.PositiveInfinity;
            float tDeltaY = stepY != 0 ? Math.Abs(1f / dir.y) : float.PositiveInfinity;
            float tDeltaZ = stepZ != 0 ? Math.Abs(1f / dir.z) : float.PositiveInfinity;

            for (int i = 0; i < MaxSteps; i++)
            {
                float t;
                Int3 normal;
                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    x += stepX; t = tMaxX; tMaxX += tDeltaX; normal = new Int3(-stepX, 0, 0);
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY; t = tMaxY; tMaxY += tDeltaY; normal = new Int3(0, -stepY, 0);
                }
                else
                {
                    z += stepZ; t = tMaxZ; tMaxZ += tDeltaZ; normal = new Int3(0, 0, -stepZ);
                }

                if (t > maxDistance) break;
                if (grid.IsSolid(x, y, z))
                {
                    result.hit = true;
                    result.voxel = new Int3(x, y, z);
                    result.normal = normal;
                    result.distance = t;
                    return result;
                }
            }
            return result;
        }

        private static float Boundary(float origin, int cell, int step, float dir)
        {
            if (step == 0) return float.PositiveInfinity;
            float next = step > 0 ? cell + 1 : cell;
            return (next - origin) / dir;
        }

        private static Int3 DominantAxisNormal(Float3 dir)
        {
            float ax = Math.Abs(dir.x), ay = Math.Abs(dir.y), az = Math.Abs(dir.z);
            if (ax >= ay && ax >= az) return new Int3(-Math.Sign(dir.x), 0, 0);
            if (ay >= az) return new Int3(0, -Math.Sign(dir.y), 0);
            return new Int3(0, 0, -Math.Sign(dir.z));
        }

        /// <summary>
        /// Aim assist: casts the centre ray first; if it misses, casts a small ring of rays
        /// (<paramref name="ringRays"/>, at most 8) offset by <paramref name="spread"/> (tangent of the
        /// cone half-angle) and returns the closest hit. Grid rays only, no physics.
        /// </summary>
        public static GridHit CastWithAssist(VoxelGrid grid, Float3 origin, Float3 forward, Float3 right, Float3 up,
            float maxDistance, float spread, int ringRays)
        {
            GridHit center = Cast(grid, origin, forward, maxDistance);
            if (center.hit || spread <= 0f || ringRays <= 0) return center;

            GridHit best = center;
            int count = Math.Min(8, ringRays);
            for (int i = 0; i < count; i++)
            {
                double angle = i * (2.0 * Math.PI / count);
                float c = (float)Math.Cos(angle) * spread, s = (float)Math.Sin(angle) * spread;
                Float3 dir = (forward + right * c + up * s).Normalized;
                GridHit h = Cast(grid, origin, dir, maxDistance);
                if (h.hit && (!best.hit || h.distance < best.distance)) best = h;
            }
            return best;
        }
    }
}
