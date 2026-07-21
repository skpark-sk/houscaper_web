/**
 * Build clean closed unit-module OBJs for townscaper architectural mode.
 *
 * Why not raw 22.3dm Material difference export?
 * Those layers are fragment atlases (thin walls / L-shards). MODULE boxes are
 * empty 6-face shells. Octant clustering produced jagged debris, not playable
 * modules. These meshes follow the thesis envelope 0.9×0.9×0.54 m and read as
 * base frame / floor / window / opening — one coherent piece per voxel cell.
 *
 * Rhino Z-up metres. townscaper parseObj maps (x,y,z)->(x,z,-y).
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const outDir = path.join(root, "assets", "ffmm-modules");

const SX = 0.9, SY = 0.9, SZ = 0.54;

/** @typedef {{x:number,y:number,z:number}} V */

function box(x0, y0, z0, x1, y1, z1) {
  const v = [
    [x0, y0, z0], [x1, y0, z0], [x1, y1, z0], [x0, y1, z0],
    [x0, y0, z1], [x1, y0, z1], [x1, y1, z1], [x0, y1, z1],
  ];
  const f = [
    [0, 1, 2, 3], // bottom
    [4, 7, 6, 5], // top
    [0, 4, 5, 1], // y-
    [2, 6, 7, 3], // y+
    [0, 3, 7, 4], // x-
    [1, 5, 6, 2], // x+
  ];
  return { v, f };
}

function merge(parts) {
  const v = [];
  const f = [];
  for (const p of parts) {
    const o = v.length;
    for (const q of p.v) v.push(q);
    for (const face of p.f) f.push(face.map((i) => i + o));
  }
  return { v, f };
}

function writeObj(mesh, file) {
  const lines = [];
  for (const [x, y, z] of mesh.v) lines.push(`v ${x} ${y} ${z}`);
  // face normals omitted — townscaper computes them
  for (const face of mesh.f) {
    lines.push("f " + face.map((i) => i + 1).join(" "));
  }
  fs.writeFileSync(path.join(outDir, file), lines.join("\n") + "\n");
}

/** Structural cage: corner posts + lintels + mid rails (Arc_Frame reading). */
function makeBase() {
  const t = 0.08; // post thickness
  const hx = SX / 2, hy = SY / 2;
  const posts = [
    box(-hx, -hy, 0, -hx + t, -hy + t, SZ),
    box(hx - t, -hy, 0, hx, -hy + t, SZ),
    box(-hx, hy - t, 0, -hx + t, hy, SZ),
    box(hx - t, hy - t, 0, hx, hy, SZ),
  ];
  const beams = [];
  for (const z of [0, SZ * 0.48, SZ - t]) {
    beams.push(box(-hx + t, -hy, z, hx - t, -hy + t, z + t));
    beams.push(box(-hx + t, hy - t, z, hx - t, hy, z + t));
    beams.push(box(-hx, -hy + t, z, -hx + t, hy - t, z + t));
    beams.push(box(hx - t, -hy + t, z, hx, hy - t, z + t));
  }
  return merge([...posts, ...beams]);
}

/** Floor plate + edge curb + short corner posts (fills module cell). */
function makeFloor() {
  const slab = 0.1;
  const curb = 0.07;
  const hx = SX / 2, hy = SY / 2;
  const post = 0.07;
  const postH = SZ;
  return merge([
    box(-hx, -hy, 0, hx, hy, slab),
    box(-hx, -hy, slab, hx, -hy + curb, SZ * 0.42),
    box(-hx, hy - curb, slab, hx, hy, SZ * 0.42),
    box(-hx, -hy + curb, slab, -hx + curb, hy - curb, SZ * 0.42),
    box(hx - curb, -hy + curb, slab, hx, hy - curb, SZ * 0.42),
    box(-hx, -hy, 0, -hx + post, -hy + post, postH),
    box(hx - post, -hy, 0, hx, -hy + post, postH),
    box(-hx, hy - post, 0, -hx + post, hy, postH),
    box(hx - post, hy - post, 0, hx, hy, postH),
  ]);
}

/** Window: solid wall face with punched opening + mullion + thin glass. */
function makeWindow() {
  const t = 0.07;
  const hx = SX / 2, hy = SY / 2;
  const wallY0 = -hy;
  const wallY1 = -hy + t;
  // wall with opening: build as frame around hole
  const ox0 = -0.28, ox1 = 0.28, oz0 = 0.12, oz1 = 0.42;
  const parts = [
    // bottom sill
    box(-hx, wallY0, 0, hx, wallY1, oz0),
    // top lintel
    box(-hx, wallY0, oz1, hx, wallY1, SZ),
    // left / right jambs
    box(-hx, wallY0, oz0, ox0, wallY1, oz1),
    box(ox1, wallY0, oz0, hx, wallY1, oz1),
    // mullion cross
    box(-0.025, wallY0, oz0, 0.025, wallY1, oz1),
    box(ox0, wallY0, 0.255, ox1, wallY1, 0.285),
    // side returns so it reads as a module, not a single plane
    box(-hx, -hy + t, 0, -hx + t, hy, SZ),
    box(hx - t, -hy + t, 0, hx, hy, SZ),
    box(-hx + t, hy - t, 0, hx - t, hy, t),
    // glass (thin)
    box(ox0, wallY0 + 0.02, oz0, ox1, wallY0 + 0.03, oz1),
  ];
  return merge(parts);
}

/** Opening / blank: portal frame on one face, open void. */
function makeOpening() {
  const t = 0.08;
  const hx = SX / 2, hy = SY / 2;
  const ox0 = -0.32, ox1 = 0.32, oz0 = 0.08, oz1 = 0.46;
  return merge([
    box(-hx, -hy, 0, hx, -hy + t, oz0), // sill
    box(-hx, -hy, oz1, hx, -hy + t, SZ), // lintel
    box(-hx, -hy, oz0, ox0, -hy + t, oz1),
    box(ox1, -hy, oz0, hx, -hy + t, oz1),
    box(-hx, -hy + t, 0, -hx + t, hy, SZ),
    box(hx - t, -hy + t, 0, hx, hy, SZ),
    box(-hx + t, hy - t, 0, hx - t, hy, t),
    box(-hx + t, -hy + t, 0, hx - t, hy - t, t * 0.6), // floor strip
  ]);
}

fs.mkdirSync(outDir, { recursive: true });

const modules = {
  base: [makeBase(), makeBase()],
  floor: [makeFloor(), makeFloor()],
  window: [makeWindow(), makeWindow()],
  opening: [makeOpening(), makeOpening()],
};

const manifest = {
  source:
    "Unit modules built to thesis envelope 0.9×0.9×0.54 m (22.3dm Material difference are fragment shards; MODULE layers are empty 6-face boxes)",
  units: "Meters",
  moduleSize: { x: SX, y: SY, z: SZ },
  rhinoUp: "Z",
  modules: {},
};

for (const [typ, meshes] of Object.entries(modules)) {
  const entries = [];
  meshes.forEach((mesh, i) => {
    const file = `${typ}_${i}.obj`;
    writeObj(mesh, file);
    const xs = mesh.v.map((p) => p[0]);
    const ys = mesh.v.map((p) => p[1]);
    const zs = mesh.v.map((p) => p[2]);
    entries.push({
      file,
      verts: mesh.v.length,
      faces: mesh.f.length,
      bbox: [
        +(Math.max(...xs) - Math.min(...xs)).toFixed(4),
        +(Math.max(...ys) - Math.min(...ys)).toFixed(4),
        +(Math.max(...zs) - Math.min(...zs)).toFixed(4),
      ],
    });
    console.log(file, "v", mesh.v.length, "f", mesh.f.length);
  });
  manifest.modules[typ] = entries;
}

fs.writeFileSync(path.join(outDir, "manifest.json"), JSON.stringify(manifest, null, 2) + "\n");
console.log("wrote unit modules →", outDir);
