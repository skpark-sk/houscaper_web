// Run inside Rhino via MCP execute_rhinocommon_csharp_code:
//   var code = System.IO.File.ReadAllText(@"...\export-ffmm-modules.cs");
// but locals need wrapping — use export-ffmm-modules-run.cs snippet instead.

var mp = MeshingParameters.FastRenderMesh;
var outDir = @"C:\Users\sk928\Documents\GitHub\houscaper_web\assets\ffmm-modules";
System.IO.Directory.CreateDirectory(outDir);

Mesh MeshGeo(GeometryBase g) {
  if (g is Mesh m) return m.DuplicateMesh();
  Brep b = g as Brep;
  if (b == null && g is Extrusion ex) b = ex.ToBrep();
  if (b == null) return null;
  var ms = Mesh.CreateFromBrep(b, mp);
  if (ms == null || ms.Length == 0) return null;
  var j = new Mesh();
  foreach (var x in ms) j.Append(x);
  return j;
}

void WriteObj(Mesh mesh, string path) {
  var ci = System.Globalization.CultureInfo.InvariantCulture;
  var sb = new System.Text.StringBuilder();
  for (int i = 0; i < mesh.Vertices.Count; i++) {
    var v = mesh.Vertices[i];
    sb.AppendLine(string.Format(ci, "v {0} {1} {2}", v.X, v.Y, v.Z));
  }
  mesh.Normals.ComputeNormals();
  for (int i = 0; i < mesh.Normals.Count; i++) {
    var n = mesh.Normals[i];
    sb.AppendLine(string.Format(ci, "vn {0} {1} {2}", n.X, n.Y, n.Z));
  }
  bool hasN = mesh.Normals.Count == mesh.Vertices.Count;
  for (int i = 0; i < mesh.Faces.Count; i++) {
    var f = mesh.Faces[i];
    if (hasN) {
      if (f.IsQuad) sb.AppendLine(string.Format("f {0}//{0} {1}//{1} {2}//{2} {3}//{3}", f.A+1,f.B+1,f.C+1,f.D+1));
      else sb.AppendLine(string.Format("f {0}//{0} {1}//{1} {2}//{2}", f.A+1,f.B+1,f.C+1));
    } else {
      if (f.IsQuad) sb.AppendLine(string.Format("f {0} {1} {2} {3}", f.A+1,f.B+1,f.C+1,f.D+1));
      else sb.AppendLine(string.Format("f {0} {1} {2}", f.A+1,f.B+1,f.C+1));
    }
  }
  System.IO.File.WriteAllText(path, sb.ToString());
}

var bags = new Dictionary<string, List<(RhinoObject o, BoundingBox bb)>> {
  {"arc", new List<(RhinoObject, BoundingBox)>()},
  {"floor", new List<(RhinoObject, BoundingBox)>()},
  {"window", new List<(RhinoObject, BoundingBox)>()},
  {"blank", new List<(RhinoObject, BoundingBox)>()},
};
var layerMap = new Dictionary<string,string> {
  {"Material difference::Arc_Frame", "arc"},
  {"Material difference::Floor", "floor"},
  {"Material difference::Window Frame", "window"},
  {"Material difference::Blank", "blank"},
  {"Modules::Window_select1", "window"},
  {"Modules::WC", "blank"},
};
foreach (var kv in layerMap) {
  var li = doc.Layers.FindByFullPath(kv.Key, -1);
  if (li < 0) continue;
  foreach (var o in doc.Objects.FindByLayer(doc.Layers[li])) {
    if (!(o.Geometry is Brep) && !(o.Geometry is Extrusion)) continue;
    bags[kv.Value].Add((o, o.Geometry.GetBoundingBox(true)));
  }
}
output.AppendLine(string.Join(" ", bags.Select(kv => kv.Key+"="+kv.Value.Count)));

var cellLi = doc.Layers.FindByFullPath("Modules::octant box", -1);
var cells = new List<(RhinoObject o, BoundingBox bb)>();
if (cellLi >= 0) {
  foreach (var o in doc.Objects.FindByLayer(doc.Layers[cellLi])) {
    if (!(o.Geometry is Brep)) continue;
    var bb = o.Geometry.GetBoundingBox(true);
    var s = bb.Max - bb.Min;
    if (s.X > 200 && s.X < 700 && s.Y > 200 && s.Y < 700 && s.Z > 100 && s.Z < 400)
      cells.Add((o, bb));
  }
}
output.AppendLine("cells=" + cells.Count);

var typed = new Dictionary<string, List<(BoundingBox bb, int arc, int floor, int window, int blank)>> {
  {"base", new List<(BoundingBox,int,int,int,int)>()},
  {"floor", new List<(BoundingBox,int,int,int,int)>()},
  {"window", new List<(BoundingBox,int,int,int,int)>()},
  {"opening", new List<(BoundingBox,int,int,int,int)>()},
};
foreach (var c in cells) {
  var box = c.bb; box.Inflate(5);
  int a=0,f=0,w=0,b=0;
  foreach (var it in bags["arc"]) if (box.Contains(it.bb.Center)) a++;
  foreach (var it in bags["floor"]) if (box.Contains(it.bb.Center)) f++;
  foreach (var it in bags["window"]) if (box.Contains(it.bb.Center)) w++;
  foreach (var it in bags["blank"]) if (box.Contains(it.bb.Center)) b++;
  if (a+f+w+b == 0) continue;
  string type = w > 0 ? "window" : (b > 0 && a == 0) ? "opening" : (f >= a) ? "floor" : "base";
  typed[type].Add((c.bb, a, f, w, b));
}
foreach (var kv in typed) output.AppendLine(kv.Key + " cells=" + kv.Value.Count);

double S = 0.001; // mm -> m
var man = new System.Text.StringBuilder();
man.AppendLine("{");
man.AppendLine("  \"source\": \"22.3dm (Arc_Frame clusters; ffmm MODULE layers are 6-face boxes only)\",");
man.AppendLine("  \"units\": \"Meters\",");
man.AppendLine("  \"moduleSize\": {\"x\":0.9,\"y\":0.9,\"z\":0.54},");
man.AppendLine("  \"modules\": {");
bool firstT = true;
foreach (var type in new[] {"base","floor","opening","window"}) {
  var list = typed[type].OrderByDescending(t => t.arc + t.floor + t.window + t.blank).Take(2).ToList();
  if (list.Count == 0) {
    string bagKey = type == "base" ? "arc" : type == "opening" ? "blank" : type;
    if (!bags.ContainsKey(bagKey) || bags[bagKey].Count == 0) bagKey = "arc";
    foreach (var it in bags[bagKey].OrderByDescending(x => {
      var s = x.bb.Max - x.bb.Min; return s.X * s.Y * s.Z;
    }).Take(2))
      list.Add((it.bb, bagKey=="arc"?1:0, bagKey=="floor"?1:0, bagKey=="window"?1:0, bagKey=="blank"?1:0));
  }
  if (!firstT) man.AppendLine(",");
  firstT = false;
  man.AppendLine("    \"" + type + "\": [");
  for (int vi = 0; vi < list.Count; vi++) {
    var cell = list[vi];
    var box = cell.bb; box.Inflate(30);
    var joined = new Mesh();
    int added = 0;
    foreach (var bag in bags.Values) {
      foreach (var it in bag) {
        if (!box.Contains(it.bb.Center)) continue;
        var mm = MeshGeo(it.o.Geometry);
        if (mm == null) continue;
        joined.Append(mm);
        added++;
      }
    }
    if (joined.Vertices.Count == 0) { output.AppendLine("skip empty " + type); continue; }
    joined.Transform(Transform.Scale(Point3d.Origin, S));
    joined.Vertices.CombineIdentical(true, true);
    joined.Faces.CullDegenerateFaces();
    joined.Normals.ComputeNormals();
    joined.Compact();
    var bb = joined.GetBoundingBox(true);
    joined.Transform(Transform.Translation(-0.5*(bb.Min.X+bb.Max.X), -0.5*(bb.Min.Y+bb.Max.Y), -bb.Min.Z));
    var bb2 = joined.GetBoundingBox(true);
    var fname = type + "_" + vi + ".obj";
    WriteObj(joined, System.IO.Path.Combine(outDir, fname));
    if (vi > 0) man.AppendLine(",");
    man.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
      "      {{\"file\":\"{0}\",\"materialHits\":{1},\"verts\":{2},\"faces\":{3},\"bbox\":[{4:F4},{5:F4},{6:F4}]}}",
      fname, added, joined.Vertices.Count, joined.Faces.Count,
      bb2.Max.X-bb2.Min.X, bb2.Max.Y-bb2.Min.Y, bb2.Max.Z-bb2.Min.Z);
    man.AppendLine();
    output.AppendLine("wrote " + fname + " hits=" + added + " v=" + joined.Vertices.Count + " f=" + joined.Faces.Count);
  }
  man.Append("    ]");
}
man.AppendLine();
man.AppendLine("  }");
man.AppendLine("}");
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "manifest.json"), man.ToString());
output.AppendLine("done");
