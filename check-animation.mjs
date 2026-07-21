/**
 * Minimal check for the tile transition curve embedded in index.html.
 * Run: node check-animation.mjs
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import vm from "node:vm";

const html = readFileSync(new URL("./index.html", import.meta.url), "utf8");
const moduleStart = '<script type="module">';
const moduleSource = html.slice(
  html.indexOf(moduleStart) + moduleStart.length,
  html.indexOf("</script>", html.indexOf(moduleStart)),
);
if (vm.SourceTextModule) new vm.SourceTextModule(moduleSource);

const source = html.match(/function tileMotion\([\s\S]*?\n\}/)?.[0];
assert(source, "index.html exposes the tileMotion curve");

const context = { reducedMotion: { matches: false } };
vm.runInNewContext(`${source}; this.tileMotion = tileMotion;`, context);
const { tileMotion } = context;

assert.deepEqual(
  Array.from(tileMotion(0)),
  [0.72, 0.28, 1],
  "enter starts above a practical minimum scale",
);
assert.deepEqual(Array.from(tileMotion(1)), [1, 0, 1], "enter settles exactly");
assert.deepEqual(Array.from(tileMotion(1, true)), [0.8, -0.08, 0], "exit shrinks and fades");
assert.deepEqual(
  Array.from(tileMotion(0.5, false, true)),
  [1, 0, 1],
  "reduced motion removes spatial movement",
);
assert.deepEqual(
  Array.from(tileMotion(0.5, true, true)),
  [1, 0, 0.5],
  "reduced motion keeps only a crossfade",
);

console.log("all animation checks passed");
