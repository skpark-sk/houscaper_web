# Grasshopper BMC — how it works

Sources inspected via Rhino MCP + on-disk files in `houscaper_web`:

| File | Role |
|------|------|
| `ffmm.3dm` | Architectural **module tileset** (Meters) — prioritized |
| `202203.24_aftertiral_BMC_SK.gh` | Tile-assembly / frame GH (711 objects) — on disk |
| `revised tile2.gh` | Full colored-voxel → cube-id pipeline (was open in GH) |
| `Houscaper_P5_thesis.pdf` | Design rules for 26 surface tiles + architectural heximal IDs |

---

## Purpose

Townscaper-like placement of modular architecture: the user (or a baked massing) supplies **colored voxels**; Boolean Marching Cubes (BMC) turns every imaginary unit cube’s 8 corner labels into a **cube ID**, then picks a pre-modeled tile from a tileset. Grasshopper does the polygonization / placement; Rhino stores the tile geometry libraries.

Thesis mapping:

- **Surface tilesets** — binary 0/1 corners → 26 unique tiles (roof / wall / floor / extra).
- **Architectural tilesets** — heximal corner labels `{0,1,2,3,4,5}` → many more IDs; voxel types are Base / Floor / Foundation / Opening / Window.

---

## `ffmm.3dm` tileset inventory (primary)

Document: `C:\Users\sk928\Documents\GitHub\houscaper_web\ffmm.3dm`  
Units: **Meters** · Objects: **5405** · Modified: 2022-05-09

### Layer tree

```
MODULE/
  Base_Modules      516   ← thesis “base” (label 1)
  Floor_Modules     154   ← thesis “floor frame” (label 2)
  Window_Modules     39   ← thesis “window” (label 5)
  Opening_Modules    24   ← thesis “opening” (label 4)
OCTANT BOX/
  Base_OCT          168   ← half-module octants 0.45×0.45×0.27
  Floor_OCT          49
  Window_OCT         20
CUBE FRAME         1680   ← unit-cube wire helpers
VETRICES/
  Floor_vertices    100
  Opening_vertices   40
  window_vertices    40
MATERIAL/
  Arc_Frame        1238   ← curved structural frames
  Support wire      414
  Floor             324
  Bottom Frame      148
  Window Frame       25
```

### Geometry facts (measured)

| Layer | Typical bbox (m) | Notes |
|-------|------------------|-------|
| `*_Modules` | **0.9 × 0.9 × 0.54** | Matches thesis module (900×900×540 mm) |
| `*_OCT` | **0.45 × 0.45 × 0.27** | One octant of a module (= half XY, half Z) |

Module solids are closed BREPs (6 faces). Objects are mostly unnamed; identity is **layer + position** in the atlas grid.

### How `ffmm` differs from other Rhino tilesets seen

| | **ffmm.3dm** (this set) | **ffsmAAg.3dm** | **ffbig.3dm** (earlier) |
|--|-------------------------|-----------------|-------------------------|
| Units | Meters | Centimeters | Centimeters |
| Object count | ~5.4k | ~3.8k | ~16.2k |
| Parent for massing | `MODULE` (`*_Modules`) | `Voxel` (`*_Voxel`) | `Voxel` (`*_Voxel`) |
| Foundation | **None** | Foundation_Voxel / OCT / vertices | Full foundation + baked building |
| Opening octants | No `Open_OCT` | `Open_OCT` present | `Open_OCT` present |
| Material density | Heavy Arc_Frame (1238) + Support wire (414) | Lighter materials | Very large baked Arc_Frame / Support wire |
| Role | Compact **tileset atlas** for base/floor/window/opening | Smaller voxel/octant library with foundation | Full **assembled building** bake + voxel inputs |

`ffmm` is the architectural tileset library oriented around finished **modules**, not foundation voxels. Naming (`Modules` vs `Voxel`) matches the thesis “architectural tilesets” chapter more closely than the foundation-heavy `ffbig` bake.

---

## Definition A — `revised tile2.gh` (colored cube-ID pipeline)

Active canvas when inspected: `…\revised tile2.gh`  
~615 objects, 172 components, 14 groups. Categories: Params, Sets, Maths (GhPython), Anemone loop, Display.

### Named groups

- `Get cube array and cube vertices`
- `points to compare from geometry`
- `All in one box`
- `Cube list of ALL`
- `LOOP START`

### Data flow

```
Rhino layers (Voxel::*)
    │  Filter By Layer  ×5
    ▼
[GhPython] centroids per type
    base / floor / foundation / window / opening
    → base_ctp, fl_ctp, fo_ctp, wi_ctp, op_ctp
    │
    │  labels: base=1, floor=2, foundation=3, window=4, opening=5
    ▼
[GhPython] interpolate cube corners
    for each cube’s 8 vertices: MemberIndex against centroid lists
    pack with byeight() → base_ / floor_ / found_ / wi_ / op_
    │
    ▼
[Addition A+B] merge typed digit streams into one cube list
    │
[GhPython] fiftodec()  — interpret 8 digits as base-6 → decimal cube ID
    │
    ▼
Anemone Loop Start/End + Move/Merge
    walk cube list, place geometry
    │
    ▼
Cluster “Tile var” (× many)  inputs: CRV, PTS, GEO, Cube, Cube2
    → Selected Obj (R / RO2)
Custom Preview
```

### Key GhPython pieces (quoted nicknames / logic)

1. **`from voxel to cube array`**  
   Input `voxel` (list of boxes) → `cube_array`, `cube_vertices`, `first_vertices`.  
   Builds a `BoxArray` covering the voxel bounding volume; each cell’s `BoxCorners` become the 8 BMC vertices.

2. **Centroid script** (inputs `base`, `floor`, `foundation`, `opening`, `window`)  
   `centpt(list)` via `Volume()['centroid']` → typed centroid lists.

3. **Label / pack script**  
   `interpolate(pts, id)` marks cube corners that coincide with a typed centroid as that `id`, else `0`.  
   IDs: **1 base, 2 floor, 3 foundation, 4 window, 5 opening**.  
   `byeight` chunks flat lists into groups of 8 (one cube).

4. **`fiftodec` / `bintodec` / `tritodec`**  
   Join 8 digits and parse as base **6** (architectural), 2 (binary surface), or 3 — matching thesis heximal cube IDs.

### Inputs / outputs

| Stage | Input | Output |
|-------|--------|--------|
| Layer filter | Rhino geometry by layer | Typed voxel lists |
| Cube array | Voxel boxes | Imaginary cube grid + corners |
| Label | Centroids + cube corners | Per-cube 8-digit typed codes |
| ID | Digit lists | Decimal cube IDs (`fiftodec`) |
| Tile var clusters | Cage curves, guide points, GEO, cube codes | Oriented tile instances |
| Preview | Geometry + materials | Viewport display (bake optional) |

---

## Definition B — `202203.24_aftertiral_BMC_SK.gh`

Path: `houscaper_web/202203.24_aftertiral_BMC_SK.gh`  
Opened via `GH_DocumentIO` (711 objects). Author tag in script: `sk928` / `2022.03.21`.

This definition is **tile-geometry assembly**, not the full colored massing pipeline of `revised tile2.gh`.

### Shape of the canvas

Heavy use of:

- **Equality** (69), **Pick'n'Choose** (37), **Clean Tree** (37), **Mass Addition** (40)
- **Clusters**: nickname `Tile var`, `Tile var - mirror`, generic `Cluster` (37 total)
- **Filter By Layer** (3), Anemone **ClassicLoopStart/End**
- **Sweep1**, explode/join curves — building frame profiles
- Boolean toggles + Python frame classifier

### Named groups / scribbles (actual canvas labels)

Groups: `IMAGINARY CUBES`, `LOOP START`, `Ground level`, `Getting bottom verices`, `Getting GREEN (SLAB) vertices`.

Scribbles describing tile families:

- `Bottom wall with edge`, `Top wall with edge`, `Middle wall`
- `Bottom tiles`, `top tiles`, `Top connect`, `Cantilever`
- `Window..`, `starting point`, `All `

### Frame-class Python (representative)

```python
# nickname: Python  — maps wall strip role → integer
if BottomFrame == False: a = 0
if BottomFrame == True:  a = 1
if TopFrame == True:     a = 2
if MiddleFrame == True:  a = 3
```

Other scripts find list **intersection indices** between two trees (shared corner / break points) used when stitching tile variants.

### Relation to thesis / Townscaper placement

1. User places voxels (game or Rhino massing).  
2. Imaginary cubes read 8 corner labels (binary surface or heximal architectural).  
3. Invalid point/edge-only configs are excluded (surface-to-surface rule → 26 canonical surface tiles).  
4. Matching tile geometry from a library (`ffmm` modules or surface atlas) is transformed into the cube pose (`Tile var` clusters: cage curve + guide points + GEO).  
5. Materials (Arc_Frame, Floor, Window Frame, …) ride along as the architectural reading of the same BMC ID.

---

## Web demo mapping

The browser demo reuses `bmc-data.js` (26 canonical tiles + 256 lookup) for **surface** placement rules, and adds an **ffmm / architectural** brush mode (base / floor / window / opening) aligned with the module layers in `ffmm.3dm` and the label IDs in `revised tile2.gh`.

See `townscaper.html` for the playable loop.

## Unity WebGL port

`unity/Houscaper` reimplements this corner/octant model in Unity for the WebGL build. It keeps
the BMC premise — clicks set lattice corners, and geometry is generated per octant rather than
per voxel — but generates its tileset procedurally in C# instead of loading the Rhino atlas, so
the modules are authored for a Brick Block-style pastel look. See `unity/README.md`.
