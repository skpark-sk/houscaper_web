import assert from "node:assert/strict";
import { parseRhinoObj } from "./rhino-obj.js";

const groups = parseRhinoObj(`
v 0 0 0
v 0.9 0 0
v 0.9 0.9 0.54
v 0 0.9 0.54
g arc_frame_1
f 1 2 3 4
`);

assert.deepEqual([...groups.keys()], ["arc_frame"]);
const positions = [...groups.get("arc_frame")];
assert.equal(positions.length, 18, "quad should triangulate to two faces");
const expected = [0, 0, 0, 1, 0, 0, 1, 0.6, 1];
positions.slice(0, 9).forEach((value, index) => {
  assert.ok(
    Math.abs(value - expected[index]) < 1e-9,
    "Rhino metres/Z-up must become unit-grid Three.js Y-up coordinates",
  );
});

console.log("ok — Rhino OBJ groups, triangulation, units, and axes");
