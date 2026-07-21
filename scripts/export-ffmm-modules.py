#! python3
# Rhino Python: export typed module meshes from open 22.3dm / similar thesis docs
import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino
import System
import System.IO
from System.Globalization import CultureInfo

doc = sc.doc
out_dir = r"C:\Users\sk928\Documents\GitHub\houscaper_web\assets\ffmm-modules"
System.IO.Directory.CreateDirectory(out_dir)

mp = Rhino.Geometry.MeshingParameters.FastRenderMesh
ci = CultureInfo.InvariantCulture

def mesh_geo(g):
    if isinstance(g, Rhino.Geometry.Mesh):
        return g.DuplicateMesh()
    brep = None
    if isinstance(g, Rhino.Geometry.Brep):
        brep = g
    elif isinstance(g, Rhino.Geometry.Extrusion):
        brep = g.ToBrep()
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

bags = {
    "arc": layer_objs("Material difference::Arc_Frame"),
    "floor": layer_objs("Material difference::Floor"),
    "window": layer_objs("Material difference::Window Frame") + layer_objs("Modules::Window_select1"),
    "blank": layer_objs("Material difference::Blank") + layer_objs("Modules::WC"),
}
print("bags", {k: len(v) for k, v in bags.items()})

cells = []
for o, bb in layer_objs("Modules::octant box"):
    s = bb.Max - bb.Min
    if 200 < s.X < 700 and 200 < s.Y < 700 and 100 < s.Z < 400:
        cells.append((o, bb))
print("cells", len(cells))

typed = {"base": [], "floor": [], "window": [], "opening": []}
for o, bb in cells:
    box = Rhino.Geometry.BoundingBox(bb.Min, bb.Max)
    box.Inflate(5)
    a = sum(1 for _, ib in bags["arc"] if box.Contains(ib.Center))
    f = sum(1 for _, ib in bags["floor"] if box.Contains(ib.Center))
    w = sum(1 for _, ib in bags["window"] if box.Contains(ib.Center))
    b = sum(1 for _, ib in bags["blank"] if box.Contains(ib.Center))
    if a + f + w + b == 0:
        continue
    if w > 0:
        t = "window"
    elif b > 0 and a == 0:
        t = "opening"
    elif f >= a:
        t = "floor"
    else:
        t = "base"
    typed[t].append((bb, a, f, w, b))

for k, v in typed.items():
    print(k, "cells", len(v))

S = 0.001  # mm -> m
manifest = {
    "source": "22.3dm Arc_Frame clusters (ffmm MODULE shells are boxes only)",
    "units": "Meters",
    "moduleSize": {"x": 0.9, "y": 0.9, "z": 0.54},
    "modules": {}
}

for typ in ("base", "floor", "opening", "window"):
    lst = sorted(typed[typ], key=lambda t: t[1] + t[2] + t[3] + t[4], reverse=True)[:2]
    if not lst:
        bag_key = "arc" if typ == "base" else ("blank" if typ == "opening" else typ)
        src = bags.get(bag_key) or bags["arc"]
        src = sorted(src, key=lambda it: (lambda s: s.X * s.Y * s.Z)(it[1].Max - it[1].Min), reverse=True)[:2]
        lst = [(bb, 1, 0, 0, 0) for _, bb in src]
    entries = []
    for vi, (bb, a, f, w, b) in enumerate(lst):
        box = Rhino.Geometry.BoundingBox(bb.Min, bb.Max)
        box.Inflate(30)
        joined = Rhino.Geometry.Mesh()
        added = 0
        for bag in bags.values():
            for o, ib in bag:
                if not box.Contains(ib.Center):
                    continue
                mm = mesh_geo(o.Geometry)
                if mm is None:
                    continue
                joined.Append(mm)
                added += 1
        if joined.Vertices.Count == 0:
            print("skip empty", typ, vi)
            continue
        joined.Transform(Rhino.Geometry.Transform.Scale(Rhino.Geometry.Point3d.Origin, S))
        joined.Vertices.CombineIdentical(True, True)
        joined.Faces.CullDegenerateFaces()
        joined.Normals.ComputeNormals()
        joined.Compact()
        bb2 = joined.GetBoundingBox(True)
        joined.Transform(Rhino.Geometry.Transform.Translation(
            -0.5 * (bb2.Min.X + bb2.Max.X),
            -0.5 * (bb2.Min.Y + bb2.Max.Y),
            -bb2.Min.Z
        ))
        bb3 = joined.GetBoundingBox(True)
        fname = "{}_{}.obj".format(typ, vi)
        write_obj(joined, System.IO.Path.Combine(out_dir, fname))
        size = [bb3.Max.X - bb3.Min.X, bb3.Max.Y - bb3.Min.Y, bb3.Max.Z - bb3.Min.Z]
        entries.append({
            "file": fname,
            "materialHits": added,
            "verts": joined.Vertices.Count,
            "faces": joined.Faces.Count,
            "bbox": [round(size[0], 4), round(size[1], 4), round(size[2], 4)],
        })
        print("wrote", fname, "hits", added, "v", joined.Vertices.Count, "f", joined.Faces.Count, "size", size)
    manifest["modules"][typ] = entries

import json
System.IO.File.WriteAllText(
    System.IO.Path.Combine(out_dir, "manifest.json"),
    json.dumps(manifest, indent=2)
)
print("done")
