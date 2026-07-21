/**
 * Sanity: OBJ assets exist + parse like townscaper.html
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const dir = path.join(root, "assets", "ffmm-modules");
const man = JSON.parse(fs.readFileSync(path.join(dir, "manifest.json"), "utf8"));

function parseObj(text) {
  let verts = 0, faces = 0;
  for (const line of text.split(/\r?\n/)) {
    if (line.startsWith("v ")) verts++;
    else if (line.startsWith("f ")) faces++;
  }
  return { verts, faces };
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
    const { verts, faces } = parseObj(fs.readFileSync(p, "utf8"));
    console.log(`${entry.file}: verts=${verts} faces=${faces}`);
    if (verts < 3 || faces < 1) {
      console.error("  too thin");
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
