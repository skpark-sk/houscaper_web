/**
 * Thesis / Rhino tileset palettes for the Townscaper demo.
 * Surface kinds match bmc-data.js (roof/wall/floor/extra).
 * Architectural types match ffmm.3dm MODULE layers + revised tile2.gh labels.
 */
window.TILESETS = {
  surface1: {
    id: "surface1",
    label: "Surface 1 (offset)",
    thesis: "§3.2.2 simple isosurface offset",
    mode: "surface",
    colors: {
      roof:  "#b84830",
      wall:  "#c8b890",
      floor: "#5a8aaa",
      extra: "#6050a0",
      none:  "#b0bfc8",
    },
  },
  surface2: {
    id: "surface2",
    label: "Surface 2 (roof/floor)",
    thesis: "§3.2.3 slanted roof + top-shifted floor",
    mode: "surface",
    // Stronger roof/floor split (thesis surface tileset 2)
    colors: {
      roof:  "#c1554d",
      wall:  "#ede5d8",
      floor: "#3d6a7a",
      extra: "#7d5fa0",
      none:  "#b8c8d0",
    },
    floorLift: 0.18, // hint: floor tiles sit toward cube top
    roofPitch: true,
  },
  ffmm: {
    id: "ffmm",
    label: "ffmm architectural",
    thesis: "§3.3 + ffmm.3dm MODULE::*",
    mode: "architectural",
    // Labels from revised tile2.gh interpolate(..., id)
    types: {
      1: { key: "base",    layer: "Base_Modules",    color: "#8a8070" },
      2: { key: "floor",   layer: "Floor_Modules",   color: "#3d8f4a" },
      4: { key: "opening", layer: "Opening_Modules", color: "#c8c064" },
      5: { key: "window",  layer: "Window_Modules",  color: "#4050c8" },
    },
    // Foundation (3) exists in other .3dm / GH but not in ffmm.3dm
    moduleSize: { x: 0.9, y: 0.9, z: 0.54 },
  },
};
