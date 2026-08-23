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
}

public sealed class SceneMesh
{
    public required string Id { get; init; }
    public List<float[]> Vertices { get; init; } = [];
    public List<int[]> Triangles { get; init; } = [];
    public List<Rgba32> Colors { get; init; } = [];
    public bool HasUniformColor { get; init; }
}

public sealed class ScenePath
{
    public required string Id { get; init; }
    public List<float[]> Vertices { get; init; } = [];
    public double Thickness { get; init; } = 1;
    public Rgba32 Color { get; init; } = Rgba32.Black;
}

public sealed class ScenePointSet
{
    public required string Id { get; init; }
    public List<float[]> Points { get; init; } = [];
    public double Radius { get; init; } = 1;
    public Rgba32 Color { get; init; } = Rgba32.Red;
}

public sealed class PlotRequest
{
    public List<PlotTrace> Traces { get; init; } = [];
    public double[,]? Heatmap { get; init; }
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
    public Rgba32 Color { get; init; } = Rgba32.Black;
}

public readonly record struct Rgba32(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public static readonly Rgba32 Black = new(0, 0, 0);
    public static readonly Rgba32 Red = new(220, 50, 47);
    public static readonly Rgba32 LightGray = new(211, 211, 211);
}
