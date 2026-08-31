using BugViewer;
using TVGL;

namespace WebGPUPresenter;

public enum PresentationKind { ThreeDimensional, TwoDimensional }

public sealed class SceneRequest
{
    public required Guid RequestId { get; init; }
    public PresentationKind Kind { get; init; } = PresentationKind.ThreeDimensional;
    public bool IsBlocking { get; init; } = true;
    public int PersistentId { get; init; } = -1;
    public HoldType HoldType { get; init; } = HoldType.Immediate;
    public int DisplayIntervalMilliseconds { get; init; } = -1;
    public string Heading { get; init; } = "";
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public List<SceneMesh> Meshes { get; init; } = [];
    public List<ScenePath> Paths { get; init; } = [];
    public List<ScenePointSet> PointSets { get; init; } = [];
    public PlotRequest? Plot { get; init; }
    public List<SceneRequest> Steps { get; init; } = [];
    internal List<SceneStepGroup> StepGroups { get; init; } = [];
    internal Action<(TriangleFace face, Vector3 point)>? OnSelection { get; init; }
    public UpdateTypes AutoResetCamera { get; init; } = UpdateTypes.SphereChange;
    public bool ShowMeshBorders { get; init; } = true;
    public MeshFaceDisplay ShowSurfacesAs { get; init; } = MeshFaceDisplay.Surfaces;
}

internal sealed class SceneStepGroup
{
    public List<SceneMesh?> Meshes { get; init; } = [];
    public List<ScenePath?> Paths { get; init; } = [];
    public List<Matrix4x4?>? Transforms { get; init; }
    public int Count => Math.Max(Math.Max(Meshes.Count, Paths.Count), Transforms?.Count ?? 0);
}

public sealed class SceneMesh
{
    public required string Id { get; init; }
    public List<float[]> Vertices { get; init; } = [];
    public List<int[]> Triangles { get; init; } = [];
    public List<float[]> PrimitiveSurfaceNormals { get; init; } = [];
    public bool HasPrimitiveSurfaces { get; init; }
    public List<ColorRgba> Colors { get; init; } = [];
    public bool HasUniformColor { get; init; }
    // This stays in-process only; vertices/indices are sent to BugViewer, while this preserves the reverse pick map.
    internal IReadOnlyList<TriangleFace> SourceFaces { get; init; } = [];
}

public readonly record struct SceneTriangleSelection(Guid RequestId, string MeshId, int TriangleIndex, System.Numerics.Vector3 Point);

public sealed class ScenePath
{
    public required string Id { get; init; }
    public List<float[]> Vertices { get; init; } = [];
    public double Thickness { get; init; } = -1;
    public ColorRgba Color { get; init; } = ColorRgba.Black;
}

public sealed class ScenePointSet
{
    public required string Id { get; init; }
    public List<float[]> Points { get; init; } = [];
    public double Radius { get; init; } = 1;
    public ColorRgba Color { get; init; } = ColorRgba.Red;
}

public sealed class PlotRequest
{
    public List<PlotTrace> Traces { get; init; } = [];
    public double[][]? Heatmap { get; init; }
    public bool NormalizeHeatmap { get; init; }
}

public sealed class PlotTrace
{
    public required string Name { get; init; }
    public List<double> X { get; init; } = [];
    public List<double> Y { get; init; } = [];
    public Plot2DType Type { get; init; } = Plot2DType.Line;
    public bool Closed { get; init; }
    public MarkerType Marker { get; init; } = MarkerType.Circle;
    public ColorRgba Color { get; init; } = ColorRgba.Black;
}
