import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.dirname(fileURLToPath(import.meta.url));
const assetDir = path.join(root, "assets", "architectural-tiles");
const manifestPath = path.join(assetDir, "manifest.json");

if (!fs.existsSync(manifestPath)) {
  throw new Error("missing Rhino architectural tile manifest");
}

const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
if (manifest.schema !== "houscaper.architectural-tiles/1") {
  throw new Error(`unexpected schema: ${manifest.schema}`);
}
if (manifest.source?.file !== "22.3dm" || manifest.source?.unit !== "mm") {
  throw new Error("manifest must identify 22.3dm millimetres as the source");
}
if (manifest.web?.unit !== "m" || manifest.web?.upAxis !== "Y") {
  throw new Error("manifest must declare metre/Y-up web coordinates");
}
if (manifest.cornerOrder !== "000,100,110,010,001,101,111,011") {
  throw new Error("corner order does not match the BMC engine");
}
if (!Array.isArray(manifest.tiles) || manifest.tiles.length < 150) {
  throw new Error(`expected a substantial Rhino tile atlas, got ${manifest.tiles?.length ?? 0}`);
}

const signatures = new Set();
for (const tile of manifest.tiles) {
  if (!/^[0-5]{8}$/.test(tile.signature)) {
    throw new Error(`invalid typed-corner signature: ${tile.signature}`);
  }
  if (!tile.file || !fs.existsSync(path.join(assetDir, tile.file))) {
    throw new Error(`missing tile asset: ${tile.file}`);
  }
  if (tile.pivot?.join(",") !== "0,0,0") {
    throw new Error(`${tile.id}: pivot must be the Rhino cage minimum`);
  }
  if (tile.size.some((value, axis) => value < 0 || value > [0.9, 0.9, 0.54][axis] + 0.002)) {
    throw new Error(`${tile.id}: geometry escapes its 0.9×0.9×0.54 m cage`);
  }
  signatures.add(tile.signature);
}

if (signatures.size < 80) {
  throw new Error(`expected at least 80 typed configurations, got ${signatures.size}`);
}

console.log(
  `ok — ${manifest.tiles.length} Rhino tiles, ${signatures.size} typed signatures`,
);
