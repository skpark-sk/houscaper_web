import fs from "node:fs";
import path from "node:path";
import { buildFamilyCatalog } from "./tile-families.js";

const sourceRoot = "./assets/architectural-tiles";
const publicRoot = "./public/assets";
const meshRoot = path.join(publicRoot, "architectural-tiles");
const tilesetRoot = path.join(publicRoot, "tilesets");
const manifest = JSON.parse(
  fs.readFileSync(path.join(sourceRoot, "manifest.json"), "utf8"),
);
const cageData = JSON.parse(
  fs.readFileSync("./assets/rhino-atlas-cages.json", "utf8"),
);
const catalog = buildFamilyCatalog(manifest, cageData);

fs.rmSync(meshRoot, { recursive: true, force: true });
fs.rmSync(tilesetRoot, { recursive: true, force: true });
fs.cpSync(sourceRoot, meshRoot, { recursive: true });
fs.mkdirSync(tilesetRoot, { recursive: true });
const bmcSource = fs.readFileSync("./bmc-data.js", "utf8").trim();
const bmcData = JSON.parse(
  bmcSource.replace(/^window\.BMC\s*=\s*/, "").replace(/;$/, ""),
);
fs.writeFileSync(
  path.join(publicRoot, "bmc-data.json"),
  `${JSON.stringify(bmcData)}\n`,
);

for (const family of catalog.families) {
  const familyRoot = path.join(tilesetRoot, family.id);
  fs.mkdirSync(familyRoot, { recursive: true });
  fs.writeFileSync(
    path.join(familyRoot, "manifest.json"),
    `${JSON.stringify(
      {
        schema: "houscaper-family-v1",
        source: catalog.source,
        familyId: family.id,
        label: family.label,
        meshBase: "/assets/architectural-tiles/",
        tiles: family.tiles,
      },
      null,
      2,
    )}\n`,
  );
}

fs.writeFileSync(
  path.join(tilesetRoot, "catalog.json"),
  `${JSON.stringify(
    {
      schema: "houscaper-catalog-v1",
      source: catalog.source,
      defaultFamilyId: catalog.defaultFamilyId,
      families: catalog.families.map(({ id, label, tiles }) => ({
        id,
        label,
        tileCount: tiles.length,
        manifest: `/assets/tilesets/${id}/manifest.json`,
      })),
    },
    null,
    2,
  )}\n`,
);

console.log(`built ${catalog.families.length} isolated Rhino tile families`);
