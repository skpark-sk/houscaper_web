using UnityEngine;

namespace Houscaper
{
    public struct VoxelHit
    {
        /// <summary>The solid cell that was hit, or the ground cell under the cursor.</summary>
        public Vector3Int Cell;

        /// <summary>Face that was entered, pointing out of <see cref="Cell"/>.</summary>
        public Vector3Int Normal;

        /// <summary>True when the ray landed on the island rather than on a block.</summary>
        public bool IsGround;

        /// <summary>Cell a new block would occupy for this hit.</summary>
        public Vector3Int PlacementCell => IsGround ? Cell : Cell + Normal;
    }

    /// <summary>
    /// Amanatides–Woo grid traversal over the sparse voxel set. Cheaper and more precise than
    /// colliders, and it hands back the exact face that was crossed.
    /// </summary>
    public static class VoxelRaycaster
    {
        const int MaxSteps = 256;

        public static bool Raycast(VoxelWorld world, Ray ray, out VoxelHit hit)
        {
            hit = default;

            // Grid space: one unit per cell on every axis.
            var origin = new Vector3(
                ray.origin.x / VoxelWorld.CellSize + 0.5f,
                (ray.origin.y - VoxelWorld.GroundOffset) / VoxelWorld.LevelHeight + 0.5f,
                ray.origin.z / VoxelWorld.CellSize + 0.5f);

            var dir = new Vector3(
                ray.direction.x / VoxelWorld.CellSize,
                ray.direction.y / VoxelWorld.LevelHeight,
                ray.direction.z / VoxelWorld.CellSize);

            if (dir.sqrMagnitude < 1e-12f) return false;

            int x = Mathf.FloorToInt(origin.x);
            int y = Mathf.FloorToInt(origin.y);
            int z = Mathf.FloorToInt(origin.z);

            int stepX = dir.x > 0f ? 1 : dir.x < 0f ? -1 : 0;
            int stepY = dir.y > 0f ? 1 : dir.y < 0f ? -1 : 0;
            int stepZ = dir.z > 0f ? 1 : dir.z < 0f ? -1 : 0;

            float tDeltaX = stepX == 0 ? float.PositiveInfinity : Mathf.Abs(1f / dir.x);
            float tDeltaY = stepY == 0 ? float.PositiveInfinity : Mathf.Abs(1f / dir.y);
            float tDeltaZ = stepZ == 0 ? float.PositiveInfinity : Mathf.Abs(1f / dir.z);

            float tMaxX = Boundary(origin.x, x, stepX, tDeltaX);
            float tMaxY = Boundary(origin.y, y, stepY, tDeltaY);
            float tMaxZ = Boundary(origin.z, z, stepZ, tDeltaZ);

            var normal = Vector3Int.zero;

            for (int i = 0; i < MaxSteps; i++)
            {
                var cell = new Vector3Int(x, y, z);

                if (world.IsSolid(cell))
                {
                    hit = new VoxelHit { Cell = cell, Normal = normal, IsGround = false };
                    return true;
                }

                // Below the grid there is nothing left to hit but the island itself.
                if (y < 0 && stepY <= 0) break;
                if (y >= VoxelWorld.MaxHeight && stepY >= 0) break;

                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    x += stepX;
                    tMaxX += tDeltaX;
                    normal = new Vector3Int(-stepX, 0, 0);
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    tMaxY += tDeltaY;
                    normal = new Vector3Int(0, -stepY, 0);
                }
                else
                {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    normal = new Vector3Int(0, 0, -stepZ);
                }
            }

            return RaycastGround(ray, out hit);
        }

        /// <summary>Falls back to the island surface at y = 0 so the first block has somewhere to go.</summary>
        public static bool RaycastGround(Ray ray, out VoxelHit hit)
        {
            hit = default;
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return false;

            float t = -ray.origin.y / ray.direction.y;
            if (t <= 0f) return false;

            var point = ray.origin + ray.direction * t;
            var cell = new Vector3Int(
                Mathf.RoundToInt(point.x / VoxelWorld.CellSize),
                0,
                Mathf.RoundToInt(point.z / VoxelWorld.CellSize));

            if (!VoxelWorld.InBounds(cell)) return false;

            hit = new VoxelHit { Cell = cell, Normal = Vector3Int.up, IsGround = true };
            return true;
        }

        static float Boundary(float origin, int cell, int step, float delta)
        {
            if (step == 0) return float.PositiveInfinity;
            float next = step > 0 ? cell + 1 - origin : origin - cell;
            return next * delta;
        }
    }
}
