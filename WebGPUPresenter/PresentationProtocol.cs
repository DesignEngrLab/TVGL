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
    internal List<LineData> PathBatches { get; init; } = [];
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
    public SceneStepPathSource? PathSource { get; init; }
    public IReadOnlyList<Matrix4x4>? Transforms { get; init; }
    public int Count => Math.Max(Math.Max(Meshes.Count, PathSource?.Count ?? 0), Transforms?.Count ?? 0);

    private readonly List<int> nullTransformIndices = [];
    private int transformsInspectedThrough = -1;

    internal int LastNullTransformAtOrBefore(int stepIndex)
    {
        if (Transforms is null)
            return -1;
        var inspectionEnd = Math.Min(stepIndex, Transforms.Count - 1);
        for (var index = transformsInspectedThrough + 1; index <= inspectionEnd; index++)
            if (Transforms[index].IsNull())
                nullTransformIndices.Add(index);
        transformsInspectedThrough = Math.Max(transformsInspectedThrough, inspectionEnd);
        var location = nullTransformIndices.BinarySearch(stepIndex);
        if (location >= 0)
            return nullTransformIndices[location];
        location = ~location;
        return location == 0 ? -1 : nullTransformIndices[location - 1];
    }

    internal IEnumerable<int> ResolvePathIndices(int stepIndex)
    {
        if (PathSource is not { } source || source.HistoryStepLimit == 0
            || source.Count == 0 || stepIndex < 0)
            yield break;

        if (Transforms is null)
        {
            var endIndex = source.HistoryStepLimit.HasValue
                ? Math.Min(stepIndex, source.Count - 1)
                : source.Count - 1;
            var startIndex = source.HistoryStepLimit.HasValue
                ? Math.Max(0, endIndex - source.HistoryStepLimit.Value + 1)
                : 0;
            for (var index = startIndex; index <= endIndex; index++)
                yield return index;
            yield break;
        }

        if (stepIndex >= Transforms.Count || Transforms[stepIndex].IsNull())
        {
            if (stepIndex < source.Count)
                yield return stepIndex;
            yield break;
        }

        var lowerBound = source.HistoryStepLimit.HasValue
            ? Math.Max(0, stepIndex - source.HistoryStepLimit.Value + 1)
            : LastNullTransformAtOrBefore(stepIndex) + 1;
        if (source.HistoryStepLimit.HasValue)
        {
            for (var index = stepIndex; index >= lowerBound; index--)
                if (Transforms[index].IsNull())
                {
                    lowerBound = index + 1;
                    break;
                }
        }
        for (var index = Math.Max(0, lowerBound); index <= Math.Min(stepIndex, source.Count - 1); index++)
            yield return index;
    }
}

internal sealed class SceneStepPathSource
{
    public required string Id { get; init; }
    public required IReadOnlyList<IEnumerable<Vector3>> Paths { get; init; }
    public IReadOnlyList<bool> ClosePaths { get; init; } = [];
    public IReadOnlyList<double> Thicknesses { get; init; } = [];
    public IReadOnlyList<Color> Colors { get; init; } = [];
    public Color DefaultColor { get; init; } = new(KnownColors.Black);
    public int? HistoryStepLimit { get; init; }
    public int Count => Paths.Count;

    public bool CloseAt(int index) => ValueAt(ClosePaths, index, false);
    public double ThicknessAt(int index) => ValueAt(Thicknesses, index, -1.0);
    public Color ColorAt(int index) => ValueAt(Colors, index, DefaultColor);

    private static T ValueAt<T>(IReadOnlyList<T> values, int index, T defaultValue)
        => values.Count == 0 ? defaultValue : values[Math.Min(index, values.Count - 1)];
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
