// Regenerates assets/rhino-atlas-cages.json from the open 22.3dm document.
//
// Run through Rhino MCP `execute_rhinocommon_csharp_code`, which supplies `doc`
// (RhinoDoc) and `output` (StringBuilder). 22.3dm must be the active document.
//
// Cage grouping relies on `Cube Frame` holding exactly 12 edge curves per cage in
// document order. Corner types come from marker points on the `Base vertices`
// layers, matched within 2mm of the cage corner:
//   5 = window, 2 = floor, 1 = base
// Emitted signature digits follow ARCH_CORNER_OFFSETS in architectural-tiles.js:
// bottom ring (0,0,0) (1,0,0) (1,1,0) (0,1,0) then the same ring at z+1.

var outPath = @"C:\Users\sk928\Documents\GitHub\houscaper_web\assets\rhino-atlas-cages.json";
var ci = System.Globalization.CultureInfo.InvariantCulture;

var frameLayer = doc.Layers.FirstOrDefault(l => l.FullPath == "Cube Frame");
var frames = doc.Objects.FindByLayer(frameLayer).Where(o => o.Geometry != null).ToList();

var markerSpecs = new[] {
    Tuple.Create(5, "Base vertices::window_vertices"),
    Tuple.Create(2, "Base vertices::Floor_vertices"),
    Tuple.Create(1, "Base vertices"),
};

var markers = new Dictionary<int, List<Point3d>>();
foreach (var spec in markerSpecs) {
    var layer = doc.Layers.FirstOrDefault(l => l.FullPath == spec.Item2);
    var points = new List<Point3d>();
    if (layer != null)
        foreach (var o in doc.Objects.FindByLayer(layer))
            if (o.Geometry is Point) points.Add(((Point)o.Geometry).Location);
    markers[spec.Item1] = points;
}

var paths = new[] {
    "Material difference::Arc_Frame",
    "Material difference::Floor",
    "Material difference::Window Frame",
    "Material difference::Blank",
    "Material difference::Support wire",
};

var materials = new List<Tuple<string, RhinoObject>>();
foreach (var path in paths) {
    var layer = doc.Layers.FirstOrDefault(l => l.FullPath == path);
    if (layer == null) continue;
    var role = path.Split(new[] { "::" }, StringSplitOptions.None).Last()
        .Replace(" ", "_").ToLowerInvariant();
    foreach (var o in doc.Objects.FindByLayer(layer))
        if (o.Geometry != null) materials.Add(Tuple.Create(role, o));
}

var rows = new List<string>();
for (int g = 0; g < frames.Count / 12; g++) {
    var cage = BoundingBox.Empty;
    for (int i = g * 12; i < g * 12 + 12; i++)
        cage.Union(frames[i].Geometry.GetBoundingBox(true));

    var corners = new[] {
        new Point3d(cage.Min.X, cage.Min.Y, cage.Min.Z),
        new Point3d(cage.Max.X, cage.Min.Y, cage.Min.Z),
        new Point3d(cage.Max.X, cage.Max.Y, cage.Min.Z),
        new Point3d(cage.Min.X, cage.Max.Y, cage.Min.Z),
        new Point3d(cage.Min.X, cage.Min.Y, cage.Max.Z),
        new Point3d(cage.Max.X, cage.Min.Y, cage.Max.Z),
        new Point3d(cage.Max.X, cage.Max.Y, cage.Max.Z),
        new Point3d(cage.Min.X, cage.Max.Y, cage.Max.Z),
    };

    var digits = new int[8];
    for (int v = 0; v < 8; v++)
        foreach (var spec in markerSpecs)
            if (markers[spec.Item1].Any(p => p.DistanceTo(corners[v]) < 2.0)) {
                digits[v] = spec.Item1;
                break;
            }

    var pieces = materials
        .Where(x => cage.Contains(x.Item2.Geometry.GetBoundingBox(true).Center))
        .ToList();
    var roles = pieces.GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.Count());
    var rolesJson = string.Join(",", roles.OrderBy(x => x.Key)
        .Select(x => "\"" + x.Key + "\":" + x.Value));

    rows.Add("    {\"sourceCage\":" + g
        + ",\"origin\":[" + cage.Min.X.ToString("0.###", ci)
        + "," + cage.Min.Y.ToString("0.###", ci)
        + "," + cage.Min.Z.ToString("0.###", ci) + "]"
        + ",\"signature\":\"" + string.Join("", digits) + "\""
        + ",\"pieceCount\":" + pieces.Count
        + ",\"roles\":{" + rolesJson + "}}");
}

var json = "{\n  \"source\":\"22.3dm\",\n  \"moduleSizeMm\":[900,900,540],\n  \"cages\":[\n"
    + string.Join(",\n", rows) + "\n  ]\n}\n";
System.IO.File.WriteAllText(outPath, json);
output.AppendLine($"Wrote {rows.Count} cage records to {outPath}");
