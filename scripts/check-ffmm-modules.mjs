/**
 * Sanity: OBJ assets exist, fill ~module envelope, not shard debris.
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const dir = path.join(root, "assets", "ffmm-modules");
const man = JSON.parse(fs.readFileSync(path.join(dir, "manifest.json"), "utf8"));
const MOD = man.moduleSize || { x: 0.9, y: 0.9, z: 0.54 };

function measureObj(text) {
  const vs = [];
  let faces = 0;
  for (const line of text.split(/\r?\n/)) {
    if (line.startsWith("v ")) {
      const p = line.trim().split(/\s+/);
      vs.push([+p[1], +p[2], +p[3]]);
    } else if (line.startsWith("f ")) faces++;
  }
  if (!vs.length) return { verts: 0, faces, sx: 0, sy: 0, sz: 0 };
  const xs = vs.map((v) => v[0]), ys = vs.map((v) => v[1]), zs = vs.map((v) => v[2]);
  return {
    verts: vs.length,
    faces,
    sx: Math.max(...xs) - Math.min(...xs),
    sy: Math.max(...ys) - Math.min(...ys),
    sz: Math.max(...zs) - Math.min(...zs),
  };
}

let ok = true;
for (const [key, list] of Object.entries(man.modules)) {
  for (const entry of list) {
    const p = path.join(dir, entry.file);
    if (!fs.existsSync(p)) {
      console.error("MISSING", p);
      ok = false;
      continue;
    }
    const m = measureObj(fs.readFileSync(p, "utf8"));
    console.log(
      `${entry.file}: verts=${m.verts} faces=${m.faces} size=(${m.sx.toFixed(3)}x${m.sy.toFixed(3)}x${m.sz.toFixed(3)})`,
    );
    if (m.verts < 24 || m.faces < 12) {
      console.error("  too sparse — looks like a shard, not a unit module");
      ok = false;
    }
    // Must span most of the thesis footprint (reject L-fragments / half slabs)
    if (m.sx < MOD.x * 0.85 || m.sy < MOD.y * 0.85) {
      console.error(`  footprint too small for module (need ~${MOD.x}x${MOD.y})`);
      ok = false;
    }
    if (m.sz < MOD.z * 0.35) {
      console.error("  height too flat");
      ok = false;
    }
  }
}

// tilesets.js references
const ts = fs.readFileSync(path.join(root, "tilesets.js"), "utf8");
for (const f of ["base_0.obj", "floor_0.obj", "opening_0.obj", "window_0.obj"]) {
  if (!ts.includes(f)) {
    console.error("tilesets.js missing", f);
    ok = false;
  }
}
const html = fs.readFileSync(path.join(root, "townscaper.html"), "utf8");
if (!html.includes("loadArchMeshes") || !html.includes("parseObj")) {
  console.error("townscaper.html missing mesh loader");
  ok = false;
}
if (!html.includes("archMeshesReady")) {
  console.error("townscaper.html missing archMeshesReady");
  ok = false;
}

if (!ok) process.exit(1);
console.log("ok — Rhino module assets + tileset wiring look sane");
