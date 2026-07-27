import assert from "node:assert/strict";
import fs from "node:fs";

for (const file of [
  "app/layout.jsx",
  "app/page.jsx",
  "app/globals.css",
  "components/Houscaper.jsx",
  "houscaper-engine.js",
]) {
  assert.ok(fs.existsSync(file), `${file} is missing`);
}

const component = fs.readFileSync("components/Houscaper.jsx", "utf8");
assert.match(component, /^"use client";/);
assert.match(component, /mountHouscaper/);
assert.match(component, /tileFamily/);

const engine = fs.readFileSync("houscaper-engine.js", "utf8");
assert.match(engine, /export async function mountHouscaper/);
assert.match(engine, /\/assets\/tilesets\/catalog\.json/);
assert.match(engine, /return \(\) =>/);
assert.doesNotMatch(engine, /window\.BMC/);
assert.doesNotMatch(engine, /window\.(voxels|refresh|setVoxel)\b/);

assert.equal(
  (engine.match(/addEventListener\(/g) ?? []).length,
  (engine.match(/,\s*\{ signal \}\)/g) ?? []).length,
  "every engine listener is registered with the AbortController signal",
);
const disposer = engine.slice(engine.lastIndexOf("return () =>"));
for (const released of [
  "listeners.abort()",
  "clearTimeout(flash._t)",
  "disposeArchitecturalGeometries()",
  "renderer.forceContextLoss()",
]) {
  assert.ok(disposer.includes(released), `the disposer must call ${released}`);
}

const css = fs.readFileSync("app/globals.css", "utf8");
assert.doesNotMatch(engine, /\.style\.display/, "layout stays in the stylesheet");
assert.match(css, /#brushes\[hidden\]/);

const packageJson = JSON.parse(fs.readFileSync("package.json", "utf8"));
assert.match(packageJson.scripts.build, /next build/);
assert.ok(packageJson.dependencies.next);
assert.ok(packageJson.dependencies.react);

console.log("ok — Next.js owns a client-only, disposable Houscaper engine");
