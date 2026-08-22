namespace WebGPUPresenter;

public sealed class SceneRequest
{
    public required Guid RequestId { get; init; }
    public string Heading { get; init; } = "";
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public List<SceneMesh> Meshes { get; init; } = [];
    public List<ScenePath> Paths { get; init; } = [];
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
public readonly record struct Rgba32(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public static readonly Rgba32 Black = new(0, 0, 0);
    public static readonly Rgba32 LightGray = new(211, 211, 211);
}
