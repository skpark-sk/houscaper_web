#!/usr/bin/env node
// Drives a headless Unity WebGL build into public/unity.
//
//   npm run build:unity
//
// Unity Hub install locations are scanned automatically. Set UNITY_PATH to override:
//   UNITY_PATH="C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.23f1\\Editor\\Unity.exe" npm run build:unity

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const projectPath = path.resolve("unity/Houscaper");
const output = path.resolve("public/unity");

// Editors installed through Unity Hub live under a per-version directory, so the hub root
// is scanned rather than any one version being hard-coded. Newest version wins.
const hubRoots = [
  "C:\\Program Files\\Unity\\Hub\\Editor",
  "C:\\Program Files (x86)\\Unity\\Hub\\Editor",
  "/Applications/Unity/Hub/Editor",
  path.join(process.env.HOME ?? "", "Applications/Unity/Hub/Editor"),
  path.join(process.env.HOME ?? "", "Unity/Hub/Editor"),
];

// Relative to a version directory, where the binary sits on each platform.
const binarySuffixes = [
  ["Editor", "Unity.exe"],
  ["Unity.app", "Contents", "MacOS", "Unity"],
  ["Editor", "Unity"],
];

function hubEditors() {
  const found = [];

  for (const root of hubRoots) {
    let versions;
    try {
      versions = fs.readdirSync(root);
    } catch {
      continue;
    }

    for (const version of versions) {
      for (const suffix of binarySuffixes) {
        const binary = path.join(root, version, ...suffix);
        if (fs.existsSync(binary)) {
          found.push({ version, binary });
          break;
        }
      }
    }
  }

  // Sort newest-first the way Unity versions compare: numerically, segment by segment.
  return found.sort((a, b) => compareVersions(b.version, a.version));
}

function compareVersions(a, b) {
  const parts = (v) => v.split(/[^0-9]+/).filter(Boolean).map(Number);
  const left = parts(a);
  const right = parts(b);

  for (let i = 0; i < Math.max(left.length, right.length); i++) {
    const diff = (left[i] ?? 0) - (right[i] ?? 0);
    if (diff !== 0) return diff;
  }
  return 0;
}

const explicit = process.env.UNITY_PATH;
if (explicit && !fs.existsSync(explicit)) {
  console.error(`UNITY_PATH is set but does not exist: ${explicit}`);
  process.exit(1);
}

const discovered = hubEditors();
const unity = explicit ?? discovered[0]?.binary ?? ["/opt/unity/Editor/Unity"].find((p) => fs.existsSync(p));

if (!unity) {
  console.error(
    [
      "No Unity editor found.",
      "",
      "Looked under:",
      ...hubRoots.map((root) => `  ${root}`),
      "",
      "Install one (Unity 6.3 LTS or newer, with the WebGL Build Support module):",
      "  unity install 6000.3.23f1",
      "",
      "Or point at it directly:",
      '  UNITY_PATH="C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.23f1\\Editor\\Unity.exe" npm run build:unity',
      "",
      "Or build from the editor: open unity/Houscaper, then Houscaper > Build WebGL.",
    ].join("\n"),
  );
  process.exit(1);
}

if (!explicit && discovered.length > 1) {
  console.log(`found ${discovered.length} editors, using ${discovered[0].version}`);
}

fs.mkdirSync(output, { recursive: true });

const args = [
  "-quit",
  "-batchmode",
  "-nographics",
  "-projectPath", projectPath,
  "-executeMethod", "Houscaper.EditorTools.HouscaperBuild.BuildWebGL",
  "-houscaperOutput", output,
  "-logFile", "-",
];

console.log(`unity: ${unity}`);
console.log(`output: ${output}`);

const result = spawnSync(unity, args, { stdio: "inherit" });
process.exit(result.status ?? 1);
