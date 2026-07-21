import assert from "node:assert/strict";
import fs from "node:fs";

const html = fs.readFileSync(new URL("./index.html", import.meta.url), "utf8");

assert.match(html, /value="architectural" selected/);
assert.match(html, /await loadArchitecturalTiles\(\)/);
assert.match(html, /rebuildArchitectural\(changeCenter/);
assert.match(html, /updateArchitecturalAnimations\(dt\)/);
assert.match(html, /22\.3dm architectural/);

console.log("ok — index defaults to the Rhino atlas and animates local tile changes");
