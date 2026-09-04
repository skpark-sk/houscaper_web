using UnityEngine;

namespace Houscaper
{
    /// <summary>
    /// Bakes the lattice into geometry the BMC way: nothing is emitted per voxel. Each set
    /// corner is walked octant by octant — four quadrants, upper and lower — and every octant
    /// picks its module from the three corners it touches plus the diagonal.
    /// </summary>
    public class HouseAssembler
    {
        readonly VoxelWorld _world;
        readonly TileLibrary _tiles;
        readonly MeshData _buffer = new MeshData();

        /// <summary>Hemisphere directions sampled for contact shading.</summary>
        static readonly Vector3[] AoSamples =
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0), new Vector3(0, -1, 0),
            new Vector3(0, 0, 1), new Vector3(0, 0, -1),
            new Vector3(1, 1, 0).normalized, new Vector3(-1, 1, 0).normalized,
            new Vector3(0, 1, 1).normalized, new Vector3(0, 1, -1).normalized,
            new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, 1).normalized,
            new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized,
        };

        public HouseAssembler(VoxelWorld world, TileLibrary tiles)
        {
            _world = world;
            _tiles = tiles;
        }

        public void Rebuild(Mesh mesh)
        {
            _buffer.Clear();

            foreach (var pair in _world.Cells)
            {
                AppendCorner(pair.Key, pair.Value);
            }

            _buffer.Upload(mesh);
        }

        void AppendCorner(Vector3Int corner, byte swatch)
        {
            var origin = VoxelWorld.CornerPosition(corner);

            bool above = _world.IsSolid(corner + Vector3Int.up);
            bool below = _world.IsSolid(corner + Vector3Int.down);

            for (int q = 0; q < 4; q++)
            {
                var stepA = TileLibrary.AxisA[q];
                var stepB = TileLibrary.AxisB[q];

                bool a = _world.IsSolid(corner + stepA);
                bool b = _world.IsSolid(corner + stepB);
                bool d = _world.IsSolid(corner + stepA + stepB);

                AppendOctant(corner, swatch, origin, q, upper: true, a, b, d, above);
                AppendOctant(corner, swatch, origin, q, upper: false, a, b, d, below);
            }

            // A stack on the apex of the corner's own roof, which all four upper octants share.
            if (!above && Hash(corner, 0, 91) < 0.12f)
            {
                float apex = TileLibrary.HY + TileLibrary.RidgeRise - 0.05f;
                _buffer.Append(_tiles.Chimney, Quaternion.identity, origin + Vector3.up * apex, swatch, Ao);
            }
        }

        void AppendOctant(
            Vector3Int corner, byte swatch, Vector3 origin,
            int quadrant, bool upper, bool a, bool b, bool d, bool vertical)
        {
            var styleA = a ? FaceStyle.Plain : PickStyle(corner, quadrant, 0, upper);
            var styleB = b ? FaceStyle.Plain : PickStyle(corner, quadrant, 1, upper);

            var module = _tiles.Octant(upper, a, b, vertical, d, styleA, styleB);
            _buffer.Append(module, TileLibrary.Yaw[quadrant], origin, swatch, Ao);
        }

        /// <summary>
        /// Windows sit in the upper half of a storey and doors in the lower half at ground level,
        /// so a facade never stacks two openings inside one storey.
        /// </summary>
        FaceStyle PickStyle(Vector3Int corner, int quadrant, int face, bool upper)
        {
            int side = quadrant * 2 + face;

            if (!upper)
            {
                if (corner.y == 0 && Hash(corner, side, 17) < 0.22f) return FaceStyle.Door;
                return FaceStyle.Plain;
            }

            return Hash(corner, side, 3) < 0.62f ? FaceStyle.Window : FaceStyle.Plain;
        }

        // ── Shading ─────────────────────────────────────────────────────────────

        float Ao(Vector3 world, Vector3 normal)
        {
            int hits = 0;
            int total = 0;

            for (int i = 0; i < AoSamples.Length; i++)
            {
                var dir = AoSamples[i];
                if (Vector3.Dot(dir, normal) <= 0.05f) continue;

                total++;
                var probe = world + new Vector3(
                    dir.x * VoxelWorld.CellSize * 0.55f,
                    dir.y * VoxelWorld.LevelHeight * 0.55f,
                    dir.z * VoxelWorld.CellSize * 0.55f);

                if (_world.IsSolid(VoxelWorld.PositionToCorner(probe))) hits++;
            }

            if (total == 0) return 1f;
            return 1f - 0.4f * hits / total;
        }

        /// <summary>Stable per-corner randomness so facades never reshuffle between rebuilds.</summary>
        public static float Hash(Vector3Int cell, int side, int salt)
        {
            unchecked
            {
                int h = cell.x * 73856093 ^ cell.y * 19349663 ^ cell.z * 83492791 ^ side * 1376312589 ^ salt * 6371;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }
    }
}
