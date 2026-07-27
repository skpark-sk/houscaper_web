import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const catalogPath = "./public/assets/tilesets/catalog.json";
assert.ok(fs.existsSync(catalogPath), "generated family catalog is missing");
assert.ok(fs.existsSync("./public/assets/bmc-data.json"), "BMC ESM replacement is missing");

const catalog = JSON.parse(fs.readFileSync(catalogPath, "utf8"));
assert.equal(catalog.defaultFamilyId, "frame-structural");
assert.deepEqual(
  catalog.families.map(({ id }) => id),
  ["surface-floor", "frame-light", "frame-structural", "blank-openings"],
);

for (const family of catalog.families) {
  const manifestPath = `./public/assets/tilesets/${family.id}/manifest.json`;
  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  assert.equal(manifest.familyId, family.id);
  assert.ok(manifest.tiles.length > 0);
  for (const tile of manifest.tiles) {
    assert.ok(
      fs.existsSync(path.join("./public/assets/architectural-tiles", tile.file)),
      `${family.id}/${tile.file} is missing`,
    );
  }
}

console.log("ok — family manifests and Rhino meshes are publishable");
