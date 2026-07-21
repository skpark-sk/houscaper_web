# Tileset `22.3dm` — documentation

Korean primary · layer / geometry names kept in English as in Rhino.

Inspected live via **Rhino MCP** (`user-rhino`) on 2026-07-21. Active document path matches the repo copy.

---

## Document summary

| Field | Value |
|-------|--------|
| **Workspace path** | `C:\Users\sk928\Documents\GitHub\houscaper_web\22.3dm` |
| **Rhino name** | `22.3dm` |
| **Units** | **Millimeters** |
| **Tolerance** | 1.0 (absolute) · angle 1.0° |
| **Created / modified** | 2022-04-26 / 2022-05-05 |
| **Object count** | **8358** |
| **Layer count** | 20 (including empty parents) |
| **Model bbox (mm)** | ≈ `[-11700, -33300, -2160]` → `[155454, 75150, 5495]` |

### Objects by type

| Type | Count |
|------|------:|
| BREP | 2942 |
| CURVE | 2511 |
| LINE | 1083 |
| POINT | 926 |
| POLYLINE | 803 |
| EXTRUSION | 87 |
| ANNOTATION | 6 |

---

## Layer tree (with counts)

Counts from `get_document_summary` (child objects may also appear under parent rollups in Rhino’s UI; table below is the MCP flat + hierarchy view).

```
Default                         826
Material difference             695   (parent rollup; children below)
  └ Support wire                 12
  └ Arc_Frame                  1483
  └ Floor                       588
  └ Window Frame                 22
  └ Blank                        43
Layer 05                          0   (empty)
Material                          0   (empty stub)
Base vertices                   653
  └ Floor_vertices              205   (green)
  └ window_vertices              48   (blue)
  └ foundations                 175   (dark green)
Modules                         217
  └ 레이어 01                     0   (empty)
  └ Window_select1               13   (dark blue)
  └ WC                           32   (blue-violet)
  └ octant box                  490   (terracotta)
Cube Frame                     2856
FRAME                             0   (empty stub)
```

**Role of major groups**

| Group | Role |
|-------|------|
| **Modules** (+ children) | Architectural tile solids / selection sets / half-module helpers |
| **Cube Frame** | Unit-cube wire helpers for BMC placement cages |
| **Base vertices** (+ typed children) | Corner / centroid markers for labeling cube corners (BMC heximal IDs) |
| **Material difference** | Finished architectural reading: frames, floors, window frames, blanks |
| **Default** | Large mixed atlas leftovers / working geometry (not a typed tileset family) |

---

## Module types & sizes

Measured with `analyze_objects` / bbox sampling (closed solid BREPs, 6 faces).

| Layer | Typical size (mm) | Count (layer) | Notes |
|-------|-------------------|--------------:|-------|
| `Modules` | **900 × 900 × 540** | 217 | Full module (= thesis 0.9 × 0.9 × 0.54 m) |
| `Modules::Window_select1` | **900 × 900 × 540** | 13 | Window-candidate subset; same module box |
| `Modules::WC` | **450 × 450 × 270** | 32 | Half-module / octant-scale solids |
| `Modules::octant box` | **450 × 450 × 270** | 490 | One octant of a module (½ XY, ½ Z) |
| `Base vertices::foundations` | **900 × 900 × 540** | 175 | Foundation-scale boxes (see below) |

Volume check (example `Modules` BREP): solid box, volume = 900×900×540 = **4.374×10⁸ mm³**.

### Typing vs `ffmm.3dm`

`22.3dm` does **not** split finished tiles into `Base_Modules` / `Floor_Modules` / `Opening_Modules` / `Window_Modules`.

Instead:

- Generic **`Modules`** atlas (217)
- Explicit **window** selection: `Window_select1` (13)
- **`WC`** half-cells (32) — name suggests wet-core / special half-module, not a thesis label ID
- **`octant box`** under Modules (490) — topology helpers, not finished massing tiles
- **Foundation** present as `Base vertices::foundations` (175) — this is the big difference from `ffmm.3dm`

Typed **vertex** layers (for GH centroid → corner labeling):

| Layer | Color cue | Likely BMC role |
|-------|-----------|-----------------|
| `Base vertices` (parent) | black | base / generic corner points |
| `Floor_vertices` | green | floor (label **2**) |
| `window_vertices` | blue | window (web `tilesets.js`: label **5**) |
| `foundations` | green | foundation (label **3**) |

Opening vertices are **not** a dedicated child layer here (unlike `ffmm`’s `Opening_vertices`).

---

## Materials (architectural reading)

Under `Material difference::*`:

| Layer | Count | Role |
|-------|------:|------|
| `Arc_Frame` | 1483 | Curved / primary structural frame curves |
| `Floor` | 588 | Floor slabs / plates |
| `Blank` | 43 | Neutral / filler material pieces |
| `Window Frame` | 22 | Window frame geometry |
| `Support wire` | 12 | Sparse support wires (much lighter than `ffmm`) |

Empty parents `Material` and `FRAME` look like leftovers from an older layer scheme.

---

## Topology ↔ BMC / Houscaper play

### Thesis / Grasshopper pipeline

Same BMC idea as `docs/grasshopper-how-it-works.md` and `revised tile2.gh`:

1. Massing voxels (or module boxes) sit on a grid.
2. Imaginary unit cubes read **8 corner labels**.
3. Architectural labels are heximal `{0,1,2,3,4,5}` → decimal cube ID (`fiftodec`).
4. Matching tile / material geometry is placed via `Tile var` clusters (cage curve + guide points + GEO).

Canonical GH label IDs (architectural):

| ID | Type | In `22.3dm`? |
|----|------|--------------|
| 1 | base | Yes — `Base vertices` + `Modules` |
| 2 | floor | Yes — `Floor_vertices`, material `Floor` |
| 3 | foundation | **Yes** — `foundations` (absent in `ffmm.3dm`) |
| 4 | opening | Weak / implicit — no dedicated `Opening_*` layer |
| 5 | window | Yes — `window_vertices`, `Window_select1`, `Window Frame` |

> Note: `tilesets.js` maps **4 → opening**, **5 → window**. Some GH comments swap 4/5; trust the web file for the playable demo.

### Web demo mapping (`tilesets.js` / `bmc-data.js`)

| Web asset | Relation to `22.3dm` |
|-----------|----------------------|
| `bmc-data.js` | Surface BMC: 26 tiles + 256 binary lookup — **not** this architectural atlas |
| `tilesets.js` → `surface1` / `surface2` | Surface brush modes only |
| `tilesets.js` → `ffmm` | Architectural mode keyed to **`ffmm.3dm`** layer names (`Base_Modules`, …), `moduleSize` 0.9×0.9×0.54 **meters** |
| **`22.3dm`** | **Not wired** as a `TILESETS` entry today |

Practical takeaway for Houscaper play:

- Shared **module size** with thesis / `ffmm`: 900×900×540 mm (= 0.9×0.9×0.54 m).
- Shared **octant** size: 450×450×270 mm.
- `22.3dm` is a **richer / older atlas** that still carries **foundation** geometry and a flatter `Modules` hierarchy.
- The browser architectural brush currently mirrors **`ffmm`**, which intentionally **omits foundation (3)**.
- To “play” `22` as-is in the web demo would need a new tileset entry (and likely different layer → brush mapping), not a drop-in swap of the `ffmm` key.

---

## What this tileset is / is not

**Is**

- Millimeter architectural module + material library for thesis BMC work
- Includes foundation helpers/solids
- Heavy `Cube Frame` + vertex atlas for GH assembly
- Sibling / precursor style to `ffmm.3dm` (same module dimensions, different layer taxonomy)

**Is not**

- Not the web-wired architectural tileset (`ffmm` is)
- Not a binary surface tileset (no 26-tile surface atlas)
- Not a clean `Base_Modules` / `Floor_Modules` / `Opening_Modules` / `Window_Modules` split
- Not empty of foundation (unlike `ffmm`)
- `Default` (826) is not a semantic tile family — treat as working clutter when scripting

---

## Comparison snapshot: `22.3dm` vs `ffmm.3dm`

| | **22.3dm** (this file) | **ffmm.3dm** |
|--|------------------------|--------------|
| Units | Millimeters | Meters |
| Objects | ~8358 | ~5405 |
| Module parent | `Modules` | `MODULE::*Modules` |
| Typed modules | Weak (generic + Window_select1 + WC) | Base / Floor / Window / Opening |
| Foundation | **Yes** (`foundations`) | **No** |
| Octants | `Modules::octant box` (490) | `OCTANT BOX::*` typed |
| Cube frame | `Cube Frame` 2856 | `CUBE FRAME` 1680 |
| Materials parent | `Material difference` | `MATERIAL` |
| Web `tilesets.js` | Not referenced | `TILESETS.ffmm` |

---

## Viewport captures (Rhino MCP)

Captures were taken with `capture_viewport` while exploring. Images live in the MCP/chat transcript (not exported as repo image files). Captions:

| # | Viewport | Settings | Caption |
|---|----------|----------|---------|
| 1 | **Perspective** | zoom_to_fit, all layers | Full atlas: clusters of cube frames, white corner spheres, green/blue typed markers across a large XY layout |
| 2 | **Top** | zoom_to_fit | Plan catalog: rectangular frames in rows/columns; white / green / blue node grids encode topology families |
| 3 | **Front** | zoom_to_fit | Elevation strip of solid modules (tan / dark / blue accents) along a shared baseline — finished massing vocabulary |
| 4 | **Perspective** | helpers hidden (`Cube Frame`, `Base vertices*`, `octant box`, `Default` off) | Modules + materials focus: wire cages with floor plates and colored slabs; white base spheres still visible on many tiles |

After capture #4, helper layers were **restored** visible.

---

## Inspection notes

- MCP open used: `_-Open` on workspace `22.3dm` (confirmed active via `get_document_summary`).
- Objects are mostly **unnamed**; identity = **layer + position** in the atlas.
- No `docs/tileset-ffmm.md` was produced for the earlier wrong target; this file is the deliverable for **`22.3dm` only**.
- Related reading: `docs/grasshopper-how-it-works.md`, `tilesets.js` (`ffmm` block), `bmc-data.js` (surface only).
