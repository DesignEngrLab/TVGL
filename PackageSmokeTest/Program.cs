using TVGL;

var points = new List<Vector3>
{
    new(0, 0, 0),
    new(1, 0, 0),
    new(0, 1, 0),
    new(0, 0, 1),
    new(0.2, 0.2, 0.2)
};

if (!ConvexHull3D.Create(points, out var hull, out var returnedVertexIndices))
    throw new InvalidOperationException("Convex-hull construction failed.");

if (hull.Vertices.Count != 4 || hull.Edges.Count != 6 || hull.Faces.Count != 4)
{
    throw new InvalidOperationException(
        $"Unexpected hull topology: {hull.Vertices.Count} vertices, " +
        $"{hull.Edges.Count} edges, and {hull.Faces.Count} faces.");
}

Console.WriteLine($"Hull vertices: {hull.Vertices.Count}");
Console.WriteLine($"Hull edges: {hull.Edges.Count}");
Console.WriteLine($"Hull faces: {hull.Faces.Count}");
Console.WriteLine($"Returned input indices: {string.Join(", ", returnedVertexIndices)}");
Console.WriteLine("TVGL package smoke test passed.");
