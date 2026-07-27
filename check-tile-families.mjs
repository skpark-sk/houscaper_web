import assert from "node:assert/strict";
import fs from "node:fs";
import {
  buildFamilyCatalog,
  clusterAtlasFamilies,
} from "./tile-families.js";

const cageData = JSON.parse(fs.readFileSync("./assets/rhino-atlas-cages.json", "utf8"));
const manifest = JSON.parse(
  fs.readFileSync("./assets/architectural-tiles/manifest.json", "utf8"),
);

const clusters = clusterAtlasFamilies(cageData.cages);
assert.deepEqual(
  clusters.map((cluster) => cluster.cages.length),
  [31, 60, 66, 55, 26],
  "22.3dm must remain split into its five spatial atlas families",
);

const catalog = buildFamilyCatalog(manifest, cageData);
assert.deepEqual(
  catalog.families.map(({ id, tiles }) => [id, tiles.length]),
  [
    ["surface-floor", 8],
    ["frame-light", 66],
    ["frame-structural", 25],
    ["blank-openings", 26],
  ],
);
assert.equal(catalog.defaultFamilyId, "frame-structural");

const structural = catalog.families.find(({ id }) => id === "frame-structural");
const structuralCages = structural.tiles.map((tile) => tile.sourceCage).sort((a, b) => a - b);
assert.deepEqual(
  structuralCages,
  [124, 125, 126, 127, 130, 131, 132, 133, 134, 135, 136, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 178],
  "default family must stay on the left coherent Rhino study (drop right half + same-origin alts)",
);

// Same-origin alternates that used to hash-mix inside frame-structural.
for (const dropped of [128, 129, 137]) {
  assert.ok(
    !structuralCages.includes(dropped),
    `same-origin alternate cage ${dropped} must not stay in the default family`,
  );
}
for (const rightHalf of [152, 158, 161, 166, 177]) {
  assert.ok(
    !structuralCages.includes(rightHalf),
    `right-half study cage ${rightHalf} must not stay in the default family`,
  );
}

for (const family of catalog.families) {
  const sourceCages = new Set(family.tiles.map((tile) => tile.sourceCage));
  assert.equal(
    sourceCages.size,
    family.tiles.length,
    `${family.id} must not duplicate or borrow cages from another family`,
  );
}

console.log("ok — Rhino atlas families stay geometrically isolated");
