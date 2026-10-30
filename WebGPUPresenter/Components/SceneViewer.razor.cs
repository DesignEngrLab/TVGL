using BugViewer;
using TVGL;
using Microsoft.AspNetCore.Components;
using NumericsVector3 = System.Numerics.Vector3;

namespace WebGPUPresenter.Components;

public partial class SceneViewer
{
    [Parameter] public int ViewerId { get; set; }
    [Parameter] public bool IsActive { get; set; }
    [Parameter] public IReadOnlyList<SceneRequest> Scenes { get; set; } = [];
    [Parameter] public EventCallback<int> OnClose { get; set; }
    [Parameter] public EventCallback OnReady { get; set; }
    [Parameter] public EventCallback<SceneTriangleSelection> OnTriangleSelected { get; set; }
    private global::BugViewer.SceneViewer? presentation;
    public Task ShowSaveAsAsync() => presentation?.ShowSaveAsAsync() ?? Task.CompletedTask;
    private global::BugViewer.BugViewer? viewer => presentation?.Viewer;
    private string? activeInspectionKey;
    private string? coloredMeshId;
    private LineData? inspectionLine;
    private readonly IReadOnlyList<string> inspectionKeys = ["g", "v", "f", "p"];
    private IEnumerable<(SceneRequest Scene, SceneMesh Mesh)> VisibleSceneMeshes() =>
        presentation?.VisibleSceneMeshes() ?? [];

    private async Task TriangleSelectedAsync(SceneTriangleSelection selection)
    {
        var mesh = VisibleSceneMeshes().LastOrDefault(item => item.Scene.RequestId == selection.RequestId
            && item.Mesh.Id == selection.MeshId).Mesh;
        var face = mesh?.SourceFaces().ElementAtOrDefault(selection.TriangleIndex);
        if (mesh is not null && face is not null)
            await ApplyInspectionCommandAsync(face, new TVGL.Vector3(selection.Point.X, selection.Point.Y, selection.Point.Z),
                mesh, selection.TriangleIndex);
        await OnTriangleSelected.InvokeAsync(selection);
    }

    private async Task ApplyInspectionCommandAsync(
    TriangleFace face, TVGL.Vector3 point, SceneMesh mesh, int triangleIndex)
    {
        if (viewer is null)
            return;

        if (activeInspectionKey == "g")
        {
            var edge = face.NonNullEdges.MinBy(edge =>
            MiscFunctions.ClosestPointOnLineSegmentToPoint(edge.From.Coordinates, edge.To.Coordinates, point)
            .DistanceSquared(point));
            if (edge is not null)
            {
                await ShowInspectionBillboardAsync(
                $"Edge {edge.IndexInList}; Vector: {edge.Vector}; Length: {edge.Length:G6}",
                edge.Center() + 2 * edge.Normal * viewer.PathThickness);
                await ShowInspectionLineAsync(CreateEdgeHighlight(edge, edge.Normal, viewer.PathThickness));
            }
        }
        else if (activeInspectionKey == "v")
        {
            var vertex = face.Vertices.MinBy(vertex => vertex.Coordinates.DistanceSquared(point));
            if (vertex is not null)
            {
                await ShowInspectionBillboardAsync($"Vertex {vertex.IndexInList}; {vertex.Coordinates}",
                vertex.Coordinates + vertex.Normal * viewer.PathThickness);
                await ShowInspectionLineAsync(
                CreateVertexHighlight(vertex.Coordinates + vertex.Normal * viewer.PathThickness));
            }
        }
        else if (activeInspectionKey == "f")
        {
            await ShowInspectionBillboardAsync(
            $"Face {face.IndexInList}; Normal: {face.Normal}", 
            face.Center + 2 * face.Normal * viewer.PathThickness);
            var colors = SceneGeometry.ExpandedColors(mesh);
            colors[triangleIndex] = FaceHighlightColor;
            await ApplyMeshColorsAsync(mesh, colors);
        }
        else if (activeInspectionKey == "p")
        {
            var primitive = face.BelongsToPrimitive;
            if (primitive is null)
                return;

            await ShowInspectionBillboardAsync(primitive.KeyString, primitive.CenterOfBoundingBox
            + 2 * face.Normal * viewer.PathThickness);
            var highlight = primitive switch
            {
                Plane => new ColorRgba(0, 255, 0),
                Cylinder => new ColorRgba(255, 0, 0),
                Cone => new ColorRgba(255, 165, 0),
                Sphere => new ColorRgba(255, 255, 0),
                Torus => new ColorRgba(255, 192, 203),
                Prismatic => new ColorRgba(128, 0, 128),
                _ => new ColorRgba(128, 128, 128)
            };
            var primitiveFaceIndices = primitive.Faces.Select(primitiveFace => primitiveFace.IndexInList).ToHashSet();
            var baseColors = SceneGeometry.ExpandedColors(mesh);
            var colors = mesh.SourceFaces().Select((sourceFace, index) =>
            ReferenceEquals(sourceFace.BelongsToPrimitive, primitive) ||
            primitiveFaceIndices.Contains(sourceFace.IndexInList)
            ? highlight
            : baseColors[index])
            .ToList();
            await ApplyMeshColorsAsync(mesh, colors);
        }
    }

    private static readonly ColorRgba InspectionHighlightColor = new(255, 255, 0, 160);
    private static readonly ColorRgba FaceHighlightColor = new(255, 255, 0, 255);
    private static readonly ColorRgba TransparentColor = new(0, 0, 0, 0);

    private async Task InspectionKeyChangedAsync(string? key)
    {
        activeInspectionKey = key;
        await ClearInspectionVisualsAsync();
    }

    private async Task ClearInspectionVisualsAsync()
    {
        if (viewer is null)
            return;

        await viewer.RemoveTextBillboardAsync(new TextBillboard { Id = "InspectionInfo", Text = "", Vertices = [], BackgroundColor = default, TextColor = default });
        if (inspectionLine is not null)
        {
            await viewer.RemoveLinesAsync(inspectionLine);
            inspectionLine = null;
        }
        await RestoreColoredMeshAsync();
    }

    private async Task ApplyMeshColorsAsync(SceneMesh mesh, List<ColorRgba>
    colors)
    {
        if (viewer is null)
            return;
        if (coloredMeshId is not null && coloredMeshId != mesh.Id)
            await RestoreColoredMeshAsync();

        await viewer.ChangeMeshColorsAsync(mesh.Id, colors);
        coloredMeshId = mesh.Id;
    }

    private async Task RestoreColoredMeshAsync()
    {
        if (viewer is null || coloredMeshId is null)
            return;

        var meshId = coloredMeshId;
        coloredMeshId = null;
        var mesh = VisibleSceneMeshes().Select(item => item.Mesh)
            .LastOrDefault(candidate => candidate.Id == meshId);
        if (mesh is not null)
            await viewer.ChangeMeshColorsAsync(mesh.Id, SceneGeometry.ExpandedColors(mesh));
    }

    private async Task ShowInspectionLineAsync(LineData line)
    {
        if (viewer is null)
            return;

        if (inspectionLine is not null)
            await viewer.RemoveLinesAsync(inspectionLine);
        await viewer.AddLinesAsync(line);
        inspectionLine = line;
    }

    private LineData CreateEdgeHighlight(Edge edge, TVGL.Vector3 normal, double outset) => new()
    {
        Id = "InspectionHighlight",
        Vertices =
        [ ToNumerics(edge.From.Coordinates + normal * outset),
            ToNumerics(edge.To.Coordinates + normal * outset)
        ],
        Colors = [InspectionHighlightColor, TransparentColor],
        Thicknesses = [2 * viewer.PathThickness, 0f],
        FadeFactors = [0f, 0f]
    };

    private LineData CreateVertexHighlight(TVGL.Vector3 coordinate)
    {
        var radius = viewer.PathThickness;
        var vertices = new List<NumericsVector3>
        {
            ToNumerics(coordinate + new TVGL.Vector3(-radius, 0, 0)),
            ToNumerics(coordinate + new TVGL.Vector3(radius, 0, 0)),
            ToNumerics(coordinate + new TVGL.Vector3(0, -radius, 0)),
            ToNumerics(coordinate + new TVGL.Vector3(0, radius, 0)),
            ToNumerics(coordinate + new TVGL.Vector3(0, 0, -radius)),
            ToNumerics(coordinate + new TVGL.Vector3(0, 0, radius))
        };
        radius *= 2;
        return new LineData
        {
            Id = "InspectionHighlight",
            Vertices = vertices,
            Colors = [InspectionHighlightColor, TransparentColor,
                InspectionHighlightColor, TransparentColor,
                InspectionHighlightColor, TransparentColor],
            Thicknesses = [radius, 0f, radius, 0f, radius, 0f],
            FadeFactors = Enumerable.Repeat(0f, vertices.Count)
        };
    }

    private static NumericsVector3 ToNumerics(TVGL.Vector3 position) =>
    new((float)position.X, (float)position.Y, (float)position.Z);

    private async Task ShowInspectionBillboardAsync(string text, TVGL.Vector3 position)
    {
        if (viewer is null)
            return;

        await viewer.RemoveTextBillboardAsync(new TextBillboard { Id = "InspectionInfo", Text = "", Vertices = [], BackgroundColor = default, TextColor = default });
        await viewer.AddTextBillboardAsync("InspectionInfo", text, ToNumerics(position),
              new ColorRgba(255, 255, 224, 230), new ColorRgba(0, 0, 0), 10 * viewer.PathThickness, 0f, 1f);
    }


}
