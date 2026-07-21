/**
 * Minimal BMC lookup self-check (no test framework).
 * Run: node check-bmc.mjs
 */
import { readFileSync } from "fs";

// bmc-data.js assigns window.BMC — shim window then eval
globalThis.window = globalThis;
eval(readFileSync(new URL("./bmc-data.js", import.meta.url), "utf8"));
const { tiles, lookup } = window.BMC;

let failed = 0;
function assert(cond, msg) {
  if (!cond) { console.error("FAIL:", msg); failed++; }
  else console.log("ok:", msg);
}

assert(tiles.length === 26, "26 canonical tiles");
assert(lookup.length === 256, "256 cube configs");

// empty / full
assert(lookup[0].kind === "empty", "cfg 0 empty");
assert(lookup[255].kind === "full", "cfg 255 full");

// single corner → floor tile 0 (configId 1)
assert(lookup[1].kind === "tile" && lookup[1].tile === 0, "cfg 1 → tile 0");
assert(tiles[0].kind === "floor", "tile 0 is floor");

// roof single (configId 16)
assert(lookup[16].kind === "tile" && tiles[lookup[16].tile].kind === "roof", "cfg 16 roof");

// wall equal top/bottom (configId 17)
assert(lookup[17].kind === "tile" && tiles[lookup[17].tile].kind === "wall", "cfg 17 wall");

// point connection should be invalid (e.g. opposite corners alone is often invalid)
// cfg 5 = bits for verts that may be invalid — use known invalid from data
const invalids = lookup.filter(e => e.kind === "invalid");
assert(invalids.length > 0, `has invalid configs (${invalids.length})`);

eval(readFileSync(new URL("./tilesets.js", import.meta.url), "utf8"));
assert(window.TILESETS.ffmm.types[1].layer === "Base_Modules", "ffmm base layer name");
assert(window.TILESETS.ffmm.types[5].layer === "Window_Modules", "ffmm window layer");
assert(window.TILESETS.surface1.mode === "surface", "surface1 mode");
assert(window.TILESETS.surface2.roofPitch === true, "surface2 pitch flag");

if (failed) {
  console.error(`\n${failed} check(s) failed`);
  process.exit(1);
}
console.log("\nall checks passed");
