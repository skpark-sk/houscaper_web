#! python3
# Rhino Python — CORRECT unit-module export from open 22.3dm
#
# DO NOT cluster Material difference shards by Modules::octant box.
# That produced jagged L-fragments (the broken brown debris in townscaper).
#
# Prefer:
#   1) single closed solid near 900×900×540 per type, OR
#   2) join ALL material pieces whose centers fall in a FULL 900×900×540 grid cell
#      with enough pieces to cover the footprint.
#
# If Rhino only has empty MODULE boxes + fragment atlases, use
# scripts/build-ffmm-unit-modules.mjs instead (committed unit meshes).

import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino
import System
import System.IO
from System.Globalization import CultureInfo
import json

doc = sc.doc
out_dir = r"C:\Users\sk928\Documents\GitHub\houscaper_web\assets\ffmm-modules"
System.IO.Directory.CreateDirectory(out_dir)

mp = Rhino.Geometry.MeshingParameters.FastRenderMesh
ci = CultureInfo.InvariantCulture
S = 0.001  # mm → m
MOD = (900.0, 900.0, 540.0)


def mesh_geo(g):
    if isinstance(g, Rhino.Geometry.Mesh):
        return g.DuplicateMesh()
    brep = g if isinstance(g, Rhino.Geometry.Brep) else (g.ToBrep() if isinstance(g, Rhino.Geometry.Extrusion) else None)
    if brep is None:
        return None
    ms = Rhino.Geometry.Mesh.CreateFromBrep(brep, mp)
    if not ms:
        return None
    j = Rhino.Geometry.Mesh()
    for m in ms:
        j.Append(m)
    return j


def write_obj(mesh, path):
    sb = System.Text.StringBuilder()
    for i in range(mesh.Vertices.Count):
        v = mesh.Vertices[i]
        sb.AppendLine(System.String.Format(ci, "v {0} {1} {2}", v.X, v.Y, v.Z))
    mesh.Normals.ComputeNormals()
    has_n = mesh.Normals.Count == mesh.Vertices.Count
    if has_n:
        for i in range(mesh.Normals.Count):
            n = mesh.Normals[i]
            sb.AppendLine(System.String.Format(ci, "vn {0} {1} {2}", n.X, n.Y, n.Z))
    for i in range(mesh.Faces.Count):
        f = mesh.Faces[i]
        if has_n:
            if f.IsQuad:
                sb.AppendLine("f {0}//{0} {1}//{1} {2}//{2} {3}//{3}".format(f.A+1, f.B+1, f.C+1, f.D+1))
            else:
                sb.AppendLine("f {0}//{0} {1}//{1} {2}//{2}".format(f.A+1, f.B+1, f.C+1))
        else:
            if f.IsQuad:
                sb.AppendLine("f {0} {1} {2} {3}".format(f.A+1, f.B+1, f.C+1, f.D+1))
            else:
                sb.AppendLine("f {0} {1} {2}".format(f.A+1, f.B+1, f.C+1))
    System.IO.File.WriteAllText(path, sb.ToString())


def layer_objs(full_path):
    li = doc.Layers.FindByFullPath(full_path, -1)
    if li < 0:
        return []
    out = []
    for o in doc.Objects.FindByLayer(doc.Layers[li]):
        g = o.Geometry
        if isinstance(g, (Rhino.Geometry.Brep, Rhino.Geometry.Extrusion)):
            out.append((o, g.GetBoundingBox(True)))
    return out


def near_module(bb, min_xy=800, min_z=200):
    s = bb.Max - bb.Min
    return s.X >= min_xy and s.Y >= min_xy and s.Z >= min_z


bags = {
    "base": layer_objs("Material difference::Arc_Frame"),
    "floor": layer_objs("Material difference::Floor"),
    "window": layer_objs("Material difference::Window Frame") + layer_objs("Modules::Window_select1"),
    "opening": layer_objs("Material difference::Blank") + layer_objs("Modules::WC"),
}
print("bags", {k: len(v) for k, v in bags.items()})

# Prefer single largest near-module solid per type (not octant shard clusters)
picked = {}
for typ, items in bags.items():
    ranked = sorted(
        [(o, bb) for o, bb in items if near_module(bb, 650 if typ != "window" else 300, 50 if typ == "window" else 200)],
        key=lambda it: (it[1].Max - it[1].Min).X * (it[1].Max - it[1].Min).Y * max((it[1].Max - it[1].Min).Z, 1),
        reverse=True,
    )
    picked[typ] = ranked[:2]
    print(typ, "near-module candidates", len(ranked), "exporting", len(picked[typ]))

manifest = {
    "source": "22.3dm largest near-module solids per Material difference type (not octant shards)",
    "units": "Meters",
    "moduleSize": {"x": 0.9, "y": 0.9, "z": 0.54},
    "rhinoUp": "Z",
    "modules": {},
}

for typ in ("base", "floor", "opening", "window"):
    entries = []
    for vi, (o, bb) in enumerate(picked[typ]):
        mm = mesh_geo(o.Geometry)
        if mm is None or mm.Vertices.Count == 0:
            print("skip empty", typ, vi)
            continue
        mm.Transform(Rhino.Geometry.Transform.Scale(Rhino.Geometry.Point3d.Origin, S))
        mm.Vertices.CombineIdentical(True, True)
        mm.Faces.CullDegenerateFaces()
        mm.Normals.ComputeNormals()
        mm.Compact()
        bb2 = mm.GetBoundingBox(True)
        mm.Transform(Rhino.Geometry.Transform.Translation(
            -0.5 * (bb2.Min.X + bb2.Max.X),
            -0.5 * (bb2.Min.Y + bb2.Max.Y),
            -bb2.Min.Z,
        ))
        bb3 = mm.GetBoundingBox(True)
        size = [bb3.Max.X - bb3.Min.X, bb3.Max.Y - bb3.Min.Y, bb3.Max.Z - bb3.Min.Z]
        # Reject shard exports that don't fill the footprint
        if size[0] < 0.65 or size[1] < 0.65:
            print("reject shard", typ, vi, size)
            continue
        fname = "{}_{}.obj".format(typ, vi)
        write_obj(mm, System.IO.Path.Combine(out_dir, fname))
        entries.append({
            "file": fname,
            "verts": mm.Vertices.Count,
            "faces": mm.Faces.Count,
            "bbox": [round(size[0], 4), round(size[1], 4), round(size[2], 4)],
        })
        print("wrote", fname, "v", mm.Vertices.Count, "size", size)
    if not entries:
        print("WARN: no valid Rhino solid for", typ, "— keep build-ffmm-unit-modules.mjs output")
    manifest["modules"][typ] = entries

System.IO.File.WriteAllText(
    System.IO.Path.Combine(out_dir, "manifest.json"),
    json.dumps(manifest, indent=2),
)
print("done")
