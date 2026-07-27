import assert from "node:assert/strict";
import {
  buildTileOrbitIndex,
  indexTilesBySignature,
  pickArchitecturalTile,
  pickTileVariant,
  resolveArchitecturalTile,
  signatureForCube,
} from "./architectural-tiles.js";

const typed = new Map([
  ["0,0,0", 1],
  ["1,0,0", 2],
  ["1,1,0", 5],
]);
assert.equal(
  signatureForCube(typed, 0, 0, 0),
  "12500000",
  "typed corners must follow the BMC corner order",
);

const index = indexTilesBySignature({
  tiles: [
    { id: "a", signature: "12500000", variant: 0 },
    { id: "b", signature: "12500000", variant: 1 },
    { id: "c", signature: "00000001", variant: 0 },
    { id: "opening", signature: "10000000", variant: 0, roles: ["blank"] },
  ],
});
assert.deepEqual(index.get("12500000").map((tile) => tile.id), ["a", "b"]);
assert.equal(pickTileVariant(index, "12500000", 0, 0, 0).id, "a");
assert.equal(pickTileVariant(index, "12500000", 1, 0, 0).id, "b");
assert.equal(pickTileVariant(index, "99999999", 0, 0, 0), null);
assert.equal(
  pickArchitecturalTile(index, "40000000", 0, 0, 0).id,
  "opening",
  "opening corners use the matching Blank variant of the base topology",
);

const orbitIndex = buildTileOrbitIndex({
  tiles: [
    { id: "asymmetric", signature: "12000000", variant: 0, roles: ["arc_frame"] },
    { id: "opening", signature: "10000000", variant: 0, roles: ["blank"] },
  ],
});
const rotated = resolveArchitecturalTile(orbitIndex, "20010000", 0, 0, 0);
assert.equal(rotated.tile.id, "asymmetric");
assert.equal(rotated.rotation, 1);
assert.equal(rotated.mirrored, false);
const rotatedOpening = resolveArchitecturalTile(orbitIndex, "00040000", 0, 0, 0);
assert.equal(rotatedOpening.tile.id, "opening");
assert.equal(rotatedOpening.rotation, 1);
const fallbackWindow = resolveArchitecturalTile(orbitIndex, "50000000", 0, 0, 0);
assert.equal(
  fallbackWindow.tile.id,
  "opening",
  "typed edits retain structural topology when a specialized variant is absent",
);

// World sig 00000010 is occupancy-symmetric under (rot1) ↔ (rot0+mirror)
// of base 00000001. Prefer unmirrored so hash cannot flip chiral elbows.
const symmetricOrbit = buildTileOrbitIndex({
  tiles: [{ id: "rafter", signature: "00000001", variant: 1, roles: ["arc_frame"] }],
});
assert.deepEqual(
  (symmetricOrbit.get("00000010") ?? []).map((c) => [c.rotation, c.mirrored]),
  [
    [1, false],
    [0, true],
  ],
);
const canonical = resolveArchitecturalTile(symmetricOrbit, "00000010", -1, -1, -1);
assert.equal(canonical.rotation, 1);
assert.equal(canonical.mirrored, false);

console.log("ok — typed cube signatures and deterministic variants");
