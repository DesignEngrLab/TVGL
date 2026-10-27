using BugViewer;
using System.Runtime.CompilerServices;
using TVGL;

namespace WebGPUPresenter;

/// <summary>In-process delivery settings. Serialize as SceneRequest to save presentation data only.</summary>
public sealed record PresenterRequest : SceneRequest
{
    public bool IsBlocking { get; init; } = true;
    public int PersistentId { get; init; } = -1;
    public HoldType HoldType { get; init; } = HoldType.Immediate;
    public int DisplayIntervalMilliseconds { get; init; } = -1;
    internal Action<(TriangleFace face, Vector3 point)>? OnSelection { get; init; }
}

internal static class SceneMeshSources
{
    private static readonly ConditionalWeakTable<SceneMesh, Source> Sources = new();
    private sealed record Source(IReadOnlyList<TriangleFace> Faces);
    internal static SceneMesh WithSourceFaces(this SceneMesh mesh, IReadOnlyList<TriangleFace> faces)
    {
        Sources.Add(mesh, new Source(faces));
        return mesh;
    }
    internal static IReadOnlyList<TriangleFace> SourceFaces(this SceneMesh mesh) =>
        Sources.TryGetValue(mesh, out var source) ? source.Faces : [];
}
