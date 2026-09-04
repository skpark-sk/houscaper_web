# Houscaper — Unity WebGL

A Brick Block–style one-click house builder, built on the BMC corner/octant model this
repository already documents in `docs/grasshopper-how-it-works.md`.

Reference feel: <https://oskarstalberg.com/game/house/index.html>

## The core idea: octants, not voxels

Clicking does **not** place a filled voxel. It sets a **lattice corner** — the same corners
`ARCH_CORNER_OFFSETS` walks in `architectural-tiles.js`, where a cube's eight corners spell out
one of the 256 BMC configurations.

Geometry is then generated **per octant**. Every set corner owns the eighth of each adjoining
cube nearest to it, so one corner contributes eight octants and one cube receives one octant
from each of its set corners. Each octant picks its module from four bits:

| bit | meaning |
| --- | --- |
| `A` | corner one step along the octant's local +X is set |
| `B` | corner one step along the octant's local +Z is set |
| `Y` | corner one step along the octant's vertical direction is set |
| `D` | the diagonal corner `A + B` is set — decides whether a roof plateau continues |

Because modules are authored once in the canonical `+X/+Z` quadrant and placed at one of four
yaws, the four horizontal octants tile a corner with no mirrored geometry. The vertical pair is
authored separately, since an upper octant grows a roof where a lower one grows a soffit.

This is what makes ridges, hips and eaves appear on their own: a roof corner rises to the ridge
only where the run of corners continues, and neighbouring octants always agree on the height of
a shared corner, so the surfaces meet without seams.

## Layout

```
unity/Houscaper/Assets/
  Scripts/
    VoxelWorld.cs        sparse lattice of corners + the corner<->world mapping
    TileLibrary.cs       the octant tileset, generated procedurally and cached
    HouseAssembler.cs    resolves modules per octant and bakes one mesh (with AO)
    MeshData.cs          CPU mesh buffer, role-to-palette resolution, mirrored stamping
    VoxelRaycaster.cs    Amanatides-Woo traversal over the corner pick cubes
    BuildController.cs   one click = one corner; undo, palette, persistence
    CameraRig.cs         orbit camera that tells a click apart from a drag
    SceneryBuilder.cs    island, sea, sky dome, build grid, ghost cube
    HouscaperUI.cs       the pastel HUD, assembled in code
    Bootstrap.cs         the only component in the scene
  Editor/
    HouscaperSceneSetup.cs  generates Scenes/Main.unity on first import
    HouscaperBuild.cs       WebGL build entry point
  Resources/Shaders/        flat pastel lighting, ghost, sky gradient
  WebGLTemplates/Houscaper/ loading screen
```

The scene asset is **generated, not committed** — it holds nothing but a `Bootstrap` object, so
there is no hand-authored scene YAML to drift between Unity versions. Open the project and it
appears; `Houscaper ▸ Regenerate Main Scene` rebuilds it.

## Building

Editor: open `unity/Houscaper`, then **Houscaper ▸ Build WebGL**.

Headless, from the repository root:

```sh
npm run build:unity
# or point at a specific editor
UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity" npm run build:unity
```

Output lands in `public/unity/`, which `.gitignore` excludes and which
[`/unity`](../app/unity/page.jsx) serves through an iframe. Until a build exists that page says
so rather than failing.

Builds are written **uncompressed** so any plain static host serves them without
`Content-Encoding` headers. Switch `compressionFormat` in `HouscaperBuild.cs` to Brotli once the
host sets them.

## Controls

| input | action |
| --- | --- |
| left click | place a corner |
| right click | remove a corner |
| drag | orbit |
| middle drag | pan |
| wheel | zoom |
| `1` `2` `3` | build / erase / paint |
| `G` | toggle the grid |
| `Ctrl`+`Z` | undo |

The town is saved to `PlayerPrefs` (IndexedDB in the browser) shortly after every edit.
