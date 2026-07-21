export const ARCH_CORNER_OFFSETS = [
  [0, 0, 0],
  [1, 0, 0],
  [1, 1, 0],
  [0, 1, 0],
  [0, 0, 1],
  [1, 0, 1],
  [1, 1, 1],
  [0, 1, 1],
];

const voxelKey = (x, y, z) => `${x},${y},${z}`;

export function signatureForCube(voxels, cx, cy, cz) {
  return ARCH_CORNER_OFFSETS.map(([dx, dy, dz]) =>
    voxels.get(voxelKey(cx + dx, cy + dy, cz + dz)) ?? 0
  ).join("");
}

export function indexTilesBySignature(manifest) {
  const index = new Map();
  for (const tile of manifest.tiles ?? []) {
    const variants = index.get(tile.signature) ?? [];
    variants.push(tile);
    index.set(tile.signature, variants);
  }
  for (const variants of index.values()) {
    variants.sort((a, b) => a.variant - b.variant || a.id.localeCompare(b.id));
  }
  return index;
}

export function pickTileVariant(index, signature, cx, cy, cz) {
  const variants = index.get(signature);
  if (!variants?.length) return null;
  return pickFromVariants(variants, cx, cy, cz);
}

function pickFromVariants(variants, cx, cy, cz) {
  const hash = Math.abs(
    Math.imul(cx, 73856093) ^ Math.imul(cy, 19349663) ^ Math.imul(cz, 83492791)
  );
  return variants[hash % variants.length];
}

export function pickArchitecturalTile(index, signature, cx, cy, cz) {
  const exact = index.get(signature);
  if (exact?.length) return pickFromVariants(exact, cx, cy, cz);

  const role = signature.includes("4")
    ? "blank"
    : signature.includes("3")
      ? "foundation"
      : null;
  if (!role) return null;

  const structuralSignature = signature.replace(/[34]/g, "1");
  const variants = (index.get(structuralSignature) ?? []).filter(
    (tile) => tile.roles?.includes(role),
  );
  return variants.length ? pickFromVariants(variants, cx, cy, cz) : null;
}

const ROTATE_ONCE = [3, 0, 1, 2, 7, 4, 5, 6];
const MIRROR_X = [1, 0, 3, 2, 5, 4, 7, 6];

function permuteSignature(signature, permutation) {
  return permutation.map((sourceIndex) => signature[sourceIndex]).join("");
}

export function transformSignature(signature, rotation = 0, mirrored = false) {
  let transformed = mirrored
    ? permuteSignature(signature, MIRROR_X)
    : signature;
  for (let turn = 0; turn < rotation; turn++) {
    transformed = permuteSignature(transformed, ROTATE_ONCE);
  }
  return transformed;
}

export function buildTileOrbitIndex(manifest) {
  const orbitIndex = new Map();
  for (const tile of manifest.tiles ?? []) {
    for (const mirrored of [false, true]) {
      for (let rotation = 0; rotation < 4; rotation++) {
        const signature = transformSignature(tile.signature, rotation, mirrored);
        const candidates = orbitIndex.get(signature) ?? [];
        if (!candidates.some((candidate) =>
          candidate.tile.id === tile.id &&
          candidate.rotation === rotation &&
          candidate.mirrored === mirrored
        )) {
          candidates.push({ tile, rotation, mirrored });
        }
        orbitIndex.set(signature, candidates);
      }
    }
  }
  return orbitIndex;
}

export function resolveArchitecturalTile(orbitIndex, signature, cx, cy, cz) {
  const role = signature.includes("4")
    ? "blank"
    : signature.includes("3")
      ? "foundation"
      : signature.includes("5")
        ? "window_frame"
      : null;
  const exact = orbitIndex.get(signature) ?? [];
  const structuralSignature = signature.replace(/[345]/g, "1");
  const structural = orbitIndex.get(structuralSignature) ?? [];
  const preferred = structural.filter(
    (candidate) => role && candidate.tile.roles?.includes(role),
  );
  const candidates = exact.length ? exact : preferred.length ? preferred : structural;
  if (!candidates.length) return null;
  const hash = Math.abs(
    Math.imul(cx, 73856093) ^ Math.imul(cy, 19349663) ^ Math.imul(cz, 83492791)
  );
  return candidates[hash % candidates.length];
}
