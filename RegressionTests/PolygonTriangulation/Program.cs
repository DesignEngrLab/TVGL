using TVGL;

foreach (var sweepAngle in new[] { 0.0, 0.73 })
{
    var profile = new Polygon(new[] { new Vector2(0, 0), new Vector2(200, 0),
        new Vector2(200, 300), new Vector2(120, 300), new Vector2(120, 280),
        new Vector2(80, 280), new Vector2(80, 300), new Vector2(0, 300) });
    foreach (var x in new[] { 40.0, 160.0 })
        foreach (var y in new[] { 30.0, 150.0, 270.0 })
        {
            var hole = new Polygon(Enumerable.Range(0, 24).Select(i =>
                new Vector2(x + 8.5 * Math.Cos(i * Math.PI / 12),
                    y + 8.5 * Math.Sin(i * Math.PI / 12))));
            if (hole.IsPositive) hole.Reverse();
            profile.AddInnerPolygon(hole);
        }

    var original = profile.AllPolygons.SelectMany(p => p.Vertices).Select(v => v.Coordinates).ToArray();
    var triangles = profile.TriangulateToCoordinatesSweepLine(suggestedAngle: sweepAngle).ToList();
    Check(original.SequenceEqual(profile.AllPolygons.SelectMany(p => p.Vertices).Select(v => v.Coordinates)),
        "Triangulation changed source coordinates.");
    Check(triangles.SelectMany(t => t).All(original.Contains), "Triangles must use exact source coordinates.");
    var areas = triangles.Select(t => Math.Abs((t[1] - t[0]).Cross(t[2] - t[0])) / 2).ToList();
    Check(areas.All(a => a > 1e-10), "Triangulation emitted a zero-area face.");
    Check(Math.Abs(areas.Sum() - profile.Area) < 1e-6, "Cap area differs from the profile.");
    var faces = profile.ExtrusionFaceVectorsFrom2DPolygons(Vector3.UnitZ, 0, 4);
    var solid = new TessellatedSolid(faces, faces.Count, null, TessellatedSolidBuildOptions.Minimal);
    solid.MakeEdgesIfNonExistent();
    Check(solid.Edges.All(e => e.OtherFace != null), "Extrusion has single-sided edges.");
    Check(solid.Faces.All(f => f.Area > 1e-10), "Extrusion has degenerate faces.");
    foreach (var normal in new[] { Vector3.UnitZ, -Vector3.UnitZ })
    {
        var silhouette = solid.CreateSilhouette(normal);
        Check(silhouette.NumberOfInnerPolygons == 6, "Silhouette lost a hole.");
        Check(Math.Abs(silhouette.Area - profile.Area) < 1e-6, "Silhouette area differs from the profile.");
    }
}
Console.WriteLine("PASS: six aligned holes, positive cap area, exact source coordinates, watertight extrusion and both silhouettes.");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
