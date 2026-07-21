const DEFAULT_SCALE = 1 / 0.9;

export function parseRhinoObj(text, scale = DEFAULT_SCALE) {
  const vertices = [];
  const groups = new Map();
  let role = "default";

  const output = () => {
    if (!groups.has(role)) groups.set(role, []);
    return groups.get(role);
  };

  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#")) continue;
    const parts = line.split(/\s+/);
    if (parts[0] === "v") {
      vertices.push([+parts[1], +parts[2], +parts[3]]);
      continue;
    }
    if (parts[0] === "g") {
      role = (parts[1] || "default").replace(/_\d+$/, "");
      continue;
    }
    if (parts[0] !== "f") continue;

    const face = parts.slice(1).map((part) => {
      const index = Number(part.split("/")[0]);
      return vertices[index < 0 ? vertices.length + index : index - 1];
    });
    const positions = output();
    for (let i = 1; i < face.length - 1; i++) {
      for (const [x, y, z] of [face[0], face[i], face[i + 1]]) {
        positions.push(x * scale, z * scale, -y * scale);
      }
    }
  }

  return groups;
}
