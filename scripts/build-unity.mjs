#!/usr/bin/env node
// Drives a headless Unity WebGL build into public/unity.
//
//   npm run build:unity
//
// Point UNITY_PATH at the editor binary if it is not in one of the usual places:
//   UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity" npm run build:unity

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const projectPath = path.resolve("unity/Houscaper");
const output = path.resolve("public/unity");

const candidates = [
  process.env.UNITY_PATH,
  "/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity",
  "/opt/unity/Editor/Unity",
  "C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.62f1\\Editor\\Unity.exe",
].filter(Boolean);

const unity = candidates.find((candidate) => fs.existsSync(candidate));

if (!unity) {
  console.error(
    [
      "Unity editor not found.",
      "",
      "Set UNITY_PATH to the editor binary, for example:",
      '  UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity" npm run build:unity',
      "",
      "Or build from the editor: open unity/Houscaper, then Houscaper ▸ Build WebGL.",
    ].join("\n"),
  );
  process.exit(1);
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
