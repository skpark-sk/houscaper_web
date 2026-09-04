using System;
using System.Collections.Generic;
using UnityEngine;

namespace Houscaper
{
    /// <summary>
    /// The build, stored the way BMC defines it: a sparse set of *lattice corners*, not filled
    /// voxels. Geometry is generated per octant — each set corner owns the eighth of every
    /// adjoining cube nearest to it — so the eight corners of a cube spell out its
    /// configuration exactly as ARCH_CORNER_OFFSETS does in architectural-tiles.js.
    ///
    /// The unit cube centred on a corner is also its pick volume, which is why the raycaster
    /// can treat corners and cells with the same arithmetic.
    /// </summary>
    public class VoxelWorld
    {
        /// <summary>Horizontal spacing between lattice corners.</summary>
        public const float CellSize = 1.2f;

        /// <summary>Vertical spacing between lattice corners: one storey.</summary>
        public const float LevelHeight = 1f;

        /// <summary>Buildable footprint is a square of cells centred on the origin.</summary>
        public const int Radius = 9;
        public const int MaxHeight = 12;

        readonly Dictionary<Vector3Int, byte> _cells = new Dictionary<Vector3Int, byte>();

        public event Action Changed;

        public int Count => _cells.Count;
        public IEnumerable<KeyValuePair<Vector3Int, byte>> Cells => _cells;

        public bool IsSolid(Vector3Int cell) => _cells.ContainsKey(cell);

        public bool IsSolid(int x, int y, int z) => _cells.ContainsKey(new Vector3Int(x, y, z));

        public bool TryGet(Vector3Int cell, out byte swatch) => _cells.TryGetValue(cell, out swatch);

        public static bool InBounds(Vector3Int cell)
        {
            return cell.y >= 0 && cell.y < MaxHeight
                && cell.x >= -Radius && cell.x <= Radius
                && cell.z >= -Radius && cell.z <= Radius;
        }

        /// <summary>
        /// Lifts the lattice by half a storey so the lower octants of the y = 0 row rest on the
        /// island instead of being buried in it.
        /// </summary>
        public const float GroundOffset = LevelHeight * 0.5f;

        /// <summary>World position of a lattice corner: the centre of its pick cube.</summary>
        public static Vector3 CornerPosition(Vector3Int corner)
        {
            return new Vector3(
                corner.x * CellSize,
                corner.y * LevelHeight + GroundOffset,
                corner.z * CellSize);
        }

        /// <summary>Nearest lattice corner to a world position.</summary>
        public static Vector3Int PositionToCorner(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / CellSize),
                Mathf.RoundToInt((position.y - GroundOffset) / LevelHeight),
                Mathf.RoundToInt(position.z / CellSize));
        }

        public bool Set(Vector3Int cell, byte swatch)
        {
            if (!InBounds(cell)) return false;
            if (_cells.TryGetValue(cell, out var existing) && existing == swatch) return false;
            _cells[cell] = swatch;
            Changed?.Invoke();
            return true;
        }

        public bool Remove(Vector3Int cell)
        {
            if (!_cells.Remove(cell)) return false;
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_cells.Count == 0) return;
            _cells.Clear();
            Changed?.Invoke();
        }

        /// <summary>Restores a snapshot in one shot, firing a single change.</summary>
        public void Load(IEnumerable<KeyValuePair<Vector3Int, byte>> cells)
        {
            _cells.Clear();
            foreach (var pair in cells)
            {
                if (InBounds(pair.Key)) _cells[pair.Key] = pair.Value;
            }
            Changed?.Invoke();
        }
    }
}
