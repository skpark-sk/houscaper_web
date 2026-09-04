using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Houscaper
{
    public enum BuildMode { Build, Erase, Paint }

    /// <summary>
    /// One click, one block. Owns the grid, drives the assembler, and keeps an undo history.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        const string SaveKey = "houscaper.town.v1";
        const string EmptyMarker = "-";

        public CameraRig Rig;
        public MeshFilter HouseFilter;
        public Transform Ghost;

        public BuildMode Mode { get; private set; } = BuildMode.Build;
        public byte Swatch { get; private set; }
        public int BlockCount => _world.Count;
        public bool GridVisible { get; private set; } = true;

        public System.Action StateChanged;
        public Transform GridTransform;

        readonly VoxelWorld _world = new VoxelWorld();
        readonly List<Edit> _undo = new List<Edit>();

        TileLibrary _tiles;
        HouseAssembler _assembler;
        Mesh _houseMesh;
        bool _dirty;
        bool _ready;
        float _saveAt = -1f;

        struct Edit
        {
            public Vector3Int Cell;
            public bool HadBefore;
            public byte Before;
            public bool HasAfter;
            public byte After;
        }

        /// <summary>
        /// Called by <see cref="Bootstrap"/> once its references exist. Deliberately not Awake:
        /// AddComponent runs Awake before the caller can assign anything.
        /// </summary>
        public void Initialize(CameraRig rig, MeshFilter houseFilter, Transform ghost, Transform grid)
        {
            Rig = rig;
            HouseFilter = houseFilter;
            Ghost = ghost;
            GridTransform = grid;

            _tiles = new TileLibrary();
            _assembler = new HouseAssembler(_world, _tiles);
            _houseMesh = new Mesh { name = "House" };
            HouseFilter.sharedMesh = _houseMesh;

            _world.Changed += () => _dirty = true;
            Rig.Clicked += OnClick;

            if (!Load()) SeedStarterHouse();
            Rebuild();

            _ready = true;
            StateChanged?.Invoke();
        }

        void OnDestroy()
        {
            if (Rig != null) Rig.Clicked -= OnClick;
        }

        void Update()
        {
            if (!_ready) return;

            UpdateGhost();
            HandleShortcuts();

            if (_dirty)
            {
                Rebuild();
                _dirty = false;
                _saveAt = Time.unscaledTime + 0.75f;
                StateChanged?.Invoke();
            }

            if (_saveAt > 0f && Time.unscaledTime >= _saveAt)
            {
                Save();
                _saveAt = -1f;
            }
        }

        void Rebuild()
        {
            _assembler.Rebuild(_houseMesh);
        }

        // ── Interaction ─────────────────────────────────────────────────────────

        void OnClick(int button)
        {
            if (!VoxelRaycaster.Raycast(_world, Rig.PointerRay(), out var hit)) return;

            // Right-click always erases, whatever the current mode is.
            if (button == 1)
            {
                if (!hit.IsGround) Apply(hit.Cell, false, 0);
                return;
            }

            if (button != 0) return;

            switch (Mode)
            {
                case BuildMode.Build:
                    Apply(hit.PlacementCell, true, Swatch);
                    break;

                case BuildMode.Erase:
                    if (!hit.IsGround) Apply(hit.Cell, false, 0);
                    break;

                case BuildMode.Paint:
                    if (!hit.IsGround) Apply(hit.Cell, true, Swatch);
                    break;
            }
        }

        void Apply(Vector3Int cell, bool solid, byte swatch)
        {
            if (!VoxelWorld.InBounds(cell)) return;

            bool had = _world.TryGet(cell, out var before);
            if (solid && had && before == swatch) return;
            if (!solid && !had) return;

            // Blocks need something underneath: the island, or another block.
            if (solid && cell.y > 0 && !_world.IsSolid(cell + Vector3Int.down) && !HasNeighbourSupport(cell)) return;

            bool changed = solid ? _world.Set(cell, swatch) : _world.Remove(cell);
            if (!changed) return;

            _undo.Add(new Edit { Cell = cell, HadBefore = had, Before = before, HasAfter = solid, After = swatch });
            if (_undo.Count > 400) _undo.RemoveAt(0);
        }

        /// <summary>Allows balconies and bridges to reach one corner out from a wall.</summary>
        bool HasNeighbourSupport(Vector3Int corner)
        {
            return _world.IsSolid(corner + new Vector3Int(1, 0, 0))
                || _world.IsSolid(corner + new Vector3Int(-1, 0, 0))
                || _world.IsSolid(corner + new Vector3Int(0, 0, 1))
                || _world.IsSolid(corner + new Vector3Int(0, 0, -1));
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;

            var edit = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);

            if (edit.HadBefore) _world.Set(edit.Cell, edit.Before);
            else _world.Remove(edit.Cell);
        }

        public void SetMode(BuildMode mode)
        {
            Mode = mode;
            StateChanged?.Invoke();
        }

        public void SetSwatch(int index)
        {
            Swatch = (byte)Mathf.Clamp(index, 0, Palette.Swatches.Length - 1);
            StateChanged?.Invoke();
        }

        public void ToggleGrid()
        {
            GridVisible = !GridVisible;
            if (GridTransform != null) GridTransform.gameObject.SetActive(GridVisible);
            StateChanged?.Invoke();
        }

        public void ClearAll()
        {
            _undo.Clear();
            _world.Clear();
        }

        void HandleShortcuts()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) && Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }

            if (Input.GetKeyDown(KeyCode.G)) ToggleGrid();
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(BuildMode.Build);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(BuildMode.Erase);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(BuildMode.Paint);
        }

        void UpdateGhost()
        {
            if (Ghost == null || Rig == null) return;

            if (Mode == BuildMode.Paint || !VoxelRaycaster.Raycast(_world, Rig.PointerRay(), out var hit))
            {
                Ghost.gameObject.SetActive(false);
                return;
            }

            var cell = Mode == BuildMode.Erase
                ? hit.Cell
                : hit.PlacementCell;

            if ((Mode == BuildMode.Erase && hit.IsGround) || !VoxelWorld.InBounds(cell))
            {
                Ghost.gameObject.SetActive(false);
                return;
            }

            Ghost.gameObject.SetActive(true);
            Ghost.position = VoxelWorld.CornerPosition(cell);
        }

        // ── Persistence ─────────────────────────────────────────────────────────

        void Save()
        {
            var sb = new StringBuilder();
            foreach (var pair in _world.Cells)
            {
                sb.Append(pair.Key.x).Append(',')
                  .Append(pair.Key.y).Append(',')
                  .Append(pair.Key.z).Append(',')
                  .Append(pair.Value).Append(';');
            }

            // A lone marker records "cleared on purpose" so a reload does not reseed.
            PlayerPrefs.SetString(SaveKey, sb.Length == 0 ? EmptyMarker : sb.ToString());
            PlayerPrefs.Save();
        }

        bool Load()
        {
            var raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return false;
            if (raw == EmptyMarker) return true;

            var cells = new List<KeyValuePair<Vector3Int, byte>>();
            foreach (var entry in raw.Split(';'))
            {
                if (entry.Length == 0) continue;

                var parts = entry.Split(',');
                if (parts.Length != 4) continue;
                if (!int.TryParse(parts[0], out int x)) continue;
                if (!int.TryParse(parts[1], out int y)) continue;
                if (!int.TryParse(parts[2], out int z)) continue;
                if (!byte.TryParse(parts[3], out byte s)) continue;

                cells.Add(new KeyValuePair<Vector3Int, byte>(new Vector3Int(x, y, z), s));
            }

            _world.Load(cells);
            return true;
        }

        /// <summary>
        /// A small cottage so the island is never empty on a first visit. These are lattice
        /// corners, so a 3x3 patch is a two-bay house rather than nine separate blocks.
        /// </summary>
        void SeedStarterHouse()
        {
            var cells = new List<KeyValuePair<Vector3Int, byte>>();

            void Put(int x, int y, int z, byte s) =>
                cells.Add(new KeyValuePair<Vector3Int, byte>(new Vector3Int(x, y, z), s));

            // Two-storey main house.
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Put(x, 0, z, 0);
                    Put(x, 1, z, 0);
                }
            }

            // A single-storey wing in a second colour.
            for (int x = -1; x <= 0; x++)
            {
                Put(x, 0, 2, 4);
                Put(x, 0, 3, 4);
            }

            _world.Load(cells);
        }
    }
}
