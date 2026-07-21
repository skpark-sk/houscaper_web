/**
 * Inspect ffmm.3dm MODULE layers and export a small Three.js JSON mesh set.
 * Offline path when Rhino MCP is unavailable.
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import rhino3dm from "rhino3dm";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, "..");
const src = path.join(root, "ffmm.3dm");
const outDir = path.join(root, "assets", "ffmm-modules");

const TARGET = {
  Base_Modules: { key: "base", typeId: 1, max: 2 },
  Floor_Modules: { key: "floor", typeId: 2, max: 2 },
  Opening_Modules: { key: "opening", typeId: 4, max: 2 },
  Window_Modules: { key: "window", typeId: 5, max: 2 },
};

const rhino = await rhino3dm();
const buf = fs.readFileSync(src);
const file = rhino.File3dm.fromByteArray(new Uint8Array(buf));
const layers = file.layers();
const layerByIndex = {};
for (let i = 0; i < layers.count; i++) {
  const L = layers.get(i);
  layerByIndex[i] = { name: L.name, fullPath: L.fullPath || L.name };
}

const objects = file.objects();
const byLayer = {};
const typeCounts = {};

for (let i = 0; i < objects.count; i++) {
  const obj = objects.get(i);
  const attrs = obj.attributes();
  const geo = obj.geometry();
  if (!geo) continue;
  const li = attrs.layerIndex;
  const layer = layerByIndex[li]?.name || `idx${li}`;
  const gtype = geo.constructor?.name || geo.objectType || typeof geo;
  typeCounts[gtype] = (typeCounts[gtype] || 0) + 1;
  if (!(layer in TARGET) && !Object.keys(TARGET).some((k) => (layerByIndex[li]?.fullPath || "").includes(k))) {
    continue;
  }
  // match by name or fullPath ending
  let matched = null;
  for (const k of Object.keys(TARGET)) {
    const fp = layerByIndex[li]?.fullPath || layer;
    if (layer === k || fp.endsWith(k) || fp.includes(k)) {
      matched = k;
      break;
    }
  }
  if (!matched) continue;
  (byLayer[matched] ||= []).push({ i, gtype, id: attrs.id });
}

console.log("geometry type histogram (all objects):", typeCounts);
console.log("MODULE layer hits:");
for (const [k, arr] of Object.entries(byLayer)) {
  const types = {};
  for (const a of arr) types[a.gtype] = (types[a.gtype] || 0) + 1;
  console.log(`  ${k}: ${arr.length}`, types);
}

// Try to get meshes — prefer Mesh; for Brep try getMesh / faces
function geometryToMesh(geo) {
  const name = geo.constructor?.name;
  if (name === "Mesh") return geo;
  if (name === "Extrusion") {
    try {
      const m = geo.getMesh(rhino.MeshType.Any);
      if (m) return m;
    } catch {}
  }
  if (name === "Brep" || name === "InstanceDefinitionGeometry") {
    // try render meshes
    try {
      if (typeof geo.faces === "function" || geo.faces) {
        const faces = geo.faces;
        const count = faces?.count ?? 0;
        // BrepFace getMesh
      }
    } catch {}
    try {
      // Some builds expose meshes()
      if (typeof geo.getMesh === "function") {
        const m = geo.getMesh(rhino.MeshType.Render);
        if (m) return m;
      }
    } catch (e) {
      // ignore
    }
  }
  return null;
}

// Probe first object of each layer for meshability
for (const [layer, arr] of Object.entries(byLayer)) {
  const sample = arr.slice(0, 3);
  for (const s of sample) {
    const obj = objects.get(s.i);
    const geo = obj.geometry();
    const mesh = geometryToMesh(geo);
    const bb = geo.getBoundingBox?.() || geo.boundingBox?.();
    console.log(`sample ${layer} #${s.i} type=${s.gtype} mesh=${!!mesh} bb=${bb ? JSON.stringify([bb.min, bb.max]) : "n/a"}`);
    // list geo methods briefly
    if (!mesh && layer === "Base_Modules" && s === sample[0]) {
      const proto = Object.getOwnPropertyNames(Object.getPrototypeOf(geo));
      console.log("  brep/geo methods sample:", proto.filter((m) => /mesh|Mesh|face|Face|brep|Brep/i.test(m)).slice(0, 40));
    }
  }
}

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(
  path.join(outDir, "inspect.json"),
  JSON.stringify({ typeCounts, byLayer: Object.fromEntries(Object.entries(byLayer).map(([k, v]) => [k, { count: v.length, types: v.reduce((a, x) => ((a[x.gtype] = (a[x.gtype] || 0) + 1), a), {}) }])) }, null, 2)
);
console.log("wrote", path.join(outDir, "inspect.json"));
