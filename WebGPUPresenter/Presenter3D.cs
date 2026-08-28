using BugViewer;
using TVGL;
using Color = TVGL.Color;

namespace WebGPUPresenter;

public sealed class Presenter3D : IPresenter3D
{
    private static readonly Lazy<LocalPresenterHost> Shared = new(() => new LocalPresenterHost());
    internal static LocalPresenterHost SharedHost => Shared.Value;
    private readonly LocalPresenterHost host;

    public Presenter3D()
    {
        host = Shared.Value;
        host.WaitReady();
    }

    public void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "")
        => ShowAndHang([solid], heading, title, subtitle);

    public void ShowAndHang(Solid solid, Action<(TriangleFace face, Vector3 point)> onSelection, string heading = "", string title = "", string subtitle = "")
    {
        ArgumentNullException.ThrowIfNull(onSelection);
        var scene = Scene(heading, title, subtitle, onSelection);
        AddSolid(scene, solid);
        host.Show(scene);
    }

    public void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
    {
        var s = Scene(heading, title, subtitle);
        foreach (var x in solids)
            AddSolid(s, x);
        host.Show(s);
    }

    public void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
    {
        var s = Scene(heading, title, subtitle);
        s.Meshes.Add(Mesh(faces, ColorRgba.LightGray, false, "faces"));
        host.Show(s);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null,
        IEnumerable<Color>? colors = null,
        bool otherwiseRandomPathColors = false,
        params Solid[] solids)
    {
        var s = Scene();
        var pathGroups = paths.Select(path => path.ToList()).ToList();
        Paths(s, pathGroups, closePaths, lineThicknesses,
            ExpandPathColors(pathGroups.Count, colors));
        foreach (var x in solids)
            AddSolid(s, x);
        host.Show(s);
    }

    public void ShowAndHang(
        IEnumerable<Vector3> path,
        bool closePaths = false,
        double lineThickness = -1,
        Color? color = null,
        params Solid[] solids)
    {
        ShowAndHang(
            [path],
            [closePaths],
            [lineThickness],
            [color ?? new Color(KnownColors.Black)],
            false,
            solids);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths,
        IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null,
        IEnumerable<Color>? colors = null,
        params Solid[] solids)
    {
        var pathGroups = paths.Select(pathSet => pathSet.ToList()).ToList();
        var flattenedPaths = pathGroups.SelectMany(pathSet => pathSet).ToList();
        IList<Color> colorList = colors?.ToList();
        if (colorList is null)
            colorList = Color.Distinct64Colors;
        var expandedColors = 
             pathGroups.SelectMany((pathSet, setIndex) =>
                Enumerable.Repeat(
                    setIndex < colorList.Count ? colorList[setIndex]
                    : Color.GetRandomColors().First(),  // new Color(KnownColors.Black),
                    pathSet.Count))
                .ToList();

        ShowAndHang(flattenedPaths, closePaths, lineThicknesses, expandedColors, true, solids);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths,
        IEnumerable<double>? lineThicknesses,
        IEnumerable<Color>? colors,
        IEnumerable<TriangleFace>? faces)
    {
        var s = Scene();
        Paths(s, paths, closePaths, lineThicknesses, colors);
        if (faces is not null)
            s.Meshes.Add(Mesh(faces, ColorRgba.LightGray, false, "faces"));
        host.Show(s);
    }

    public void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color? color = null)
    {
        var scene = Scene();
        scene.PointSets.Add(Points(points, radius, color ?? new Color(KnownColors.Red)));
        host.Show(scene);
    }

    public void ShowPointsAndHang(
        IEnumerable<IEnumerable<Vector3>> pointSets,
        double radius = 0,
        IEnumerable<Color>? colors = null)
    {
        var scene = Scene();
        var palette = colors?.ToList() ?? [];
        var i = 0;
        foreach (var set in pointSets)
        {
            var color = i < palette.Count ? palette[i] : RandomColor(i++);
            scene.PointSets.Add(Points(set, radius, color));
        }
        host.Show(scene);
    }

    public void ShowAndHangTransparentsAndSolids(
        IEnumerable<TessellatedSolid> a,
        IEnumerable<TessellatedSolid> b)
    {
        var scene = Scene();
        foreach (var solid in b)
            AddSolid(scene, solid);
        foreach (var solid in a)
        {
            scene.Meshes.Add(Mesh(
                solid.Faces,
                new ColorRgba(solid.SolidColor.R, solid.SolidColor.G, solid.SolidColor.B, 89),
                solid.HasUniformColor,
                "transparent"));
        }
        host.Show(scene);
    }

    public void ShowGaussSphereWithIntensity(IEnumerable<Vertex> v, IList<Color> c, Solid s)
    {
        var scene = Scene();
        AddSolid(scene, s);
        var radius = Math.Max(
            s.XMax - s.XMin,
            Math.Max(s.YMax - s.YMin, s.ZMax - s.ZMin)) / 2;
        var center = s.Center;
        var verts = v.ToList();
        for (var i = 0; i < verts.Count; i++)
        {
            var path = new[] { center, center + verts[i].Coordinates * radius };
            var color = i < c.Count ? c[i] : new Color(KnownColors.Red);
            Paths(scene, [path], [false], [5.0], [color]);
        }
        host.Show(scene);
    }

    public void Show(
        Solid s,
        string title = "",
        HoldType h = HoldType.Immediate,
        int t = -1,
        int id = -1)
    {
        Show([s], title, h, t, id);
    }

    public void Show(
        ICollection<Solid> s,
        string title = "",
        HoldType h = HoldType.Immediate,
        int t = -1,
        int id = -1)
    {
        var scene = Scene(t: title);
        foreach (var solid in s)
            AddSolid(scene, solid);
        Publish(scene, h, t, id);
    }

    public void Show(
        IEnumerable<IEnumerable<Vector3>> p,
        IEnumerable<bool>? c = null,
        IEnumerable<double>? t = null,
        IEnumerable<Color>? co = null,
        string title = "",
        HoldType h = HoldType.Immediate,
        int time = -1,
        int id = -1,
        params Solid[] s)
    {
        var scene = Scene(t: title);
        var pathGroups = p.Select(path => path.ToList()).ToList();
        Paths(scene, pathGroups, c, t, ExpandPathColors(pathGroups.Count, co));
        foreach (var solid in s)
            AddSolid(scene, solid);
        Publish(scene, h, time, id);
    }

    public void ShowStepsAndHang(
        IEnumerable<IEnumerable<IEnumerable<Vector3>>> p,
        IEnumerable<IEnumerable<Matrix4x4>> pt,
        IEnumerable<IEnumerable<Solid>> s,
        IEnumerable<IEnumerable<Matrix4x4>> st,
        IEnumerable<bool>? c = null,
        IEnumerable<double>? t = null,
        IEnumerable<Color>? co = null)
    {
        var faceGroups = s?.Select(g => g.Select(x =>
            x is TessellatedSolid ts
                ? ts.Faces
                : Enumerable.Empty<TriangleFace>()));
        ShowStepsAndHang(p, pt, faceGroups, st, c, t, co);
    }

    public void ShowStepsAndHang(
        IEnumerable<IEnumerable<IEnumerable<Vector3>>> p,
        IEnumerable<IEnumerable<Matrix4x4>> pt,
        IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> f,
        IEnumerable<IEnumerable<Matrix4x4>> ft,
        IEnumerable<bool>? c = null,
        IEnumerable<double>? t = null,
        IEnumerable<Color>? co = null)
    {
        var pathGroups = p?.ToList() ?? [];
        var faceGroups = f?.ToList() ?? [];
        var pathColors = co?.ToList() ?? Color.Distinct64Colors.ToList();
        var count = Math.Max(
            pathGroups.SelectMany(g => g).Count(),
            faceGroups.SelectMany(g => g).Count());
        var request = Scene();

        for (var index = 0; index < count; index++)
        {
            var step = Scene();
            for (var groupIndex = 0; groupIndex < pathGroups.Count; groupIndex++)
            {
                var group = pathGroups[groupIndex];
                if (index < group.Count())
                    Paths(step, [group.ElementAt(index)], c, t,
                        [groupIndex < pathColors.Count ? pathColors[groupIndex] : Color.GetRandomColors().First()]);
            }
            foreach (var group in faceGroups)
            {
                if (index < group.Count())
                {
                    step.Meshes.Add(Mesh(
                        group.ElementAt(index),
                        ColorRgba.LightGray,
                        false,
                        "step"));
                }
            }
            request.Steps.Add(step);
        }
        host.Show(request);
    }

    private static SceneRequest Scene(string h = "", string t = "", string st = "",
        Action<(TriangleFace face, Vector3 point)>? onSelection = null)
        => new()
        {
            RequestId = Guid.NewGuid(),
            Heading = h,
            Title = t,
            Subtitle = st,
            OnSelection = onSelection
        };

    private static void AddSolid(SceneRequest s, Solid x)
    {
        if (x is TessellatedSolid ts)
        {
            var primitives = ts.Primitives?.Where(primitive => primitive.Faces?.Count > 0).ToList() ?? [];
            if (primitives.Count == 0)
            {
                s.Meshes.Add(Mesh(ts.Faces,
                    new ColorRgba(ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B, ts.SolidColor.A),
                    ts.HasUniformColor, "solid"));
                return;
            }

            var primitiveFaces = primitives.SelectMany(primitive => primitive.Faces).ToHashSet();
            foreach (var primitive in primitives)
                s.Meshes.Add(Mesh(primitive.Faces,
                    new ColorRgba(ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B, ts.SolidColor.A),
                    ts.HasUniformColor, "primitive"));

            var unassignedFaces = ts.Faces.Where(face => !primitiveFaces.Contains(face)).ToList();
            if (unassignedFaces.Count > 0)
                s.Meshes.Add(Mesh(unassignedFaces,
                    new ColorRgba(ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B, ts.SolidColor.A),
                    ts.HasUniformColor, "unclassified"));
        }
        else if (x is CrossSectionSolid cs)
        {
            Paths(s, cs.GetCrossSectionsAs3DLoops().SelectMany(v => v), null, null, null);
        }
        else if (x is VoxelizedSolid vs)
        {
            var points = vs.GetExposedVoxels().Select(v => new[]
            {
                (float)(v.xIndex * vs.VoxelSideLength + vs.Offset.X),
                (float)(v.yIndex * vs.VoxelSideLength + vs.Offset.Y),
                (float)(v.zIndex * vs.VoxelSideLength + vs.Offset.Z)
            }).ToList();
            s.PointSets.Add(new ScenePointSet
            {
                Id = $"voxels-{Guid.NewGuid():N}",
                Radius = Math.Max(1, vs.VoxelSideLength),
                Color = new ColorRgba(vs.SolidColor.R, vs.SolidColor.G, vs.SolidColor.B, vs.SolidColor.A),
                Points = points
            });
        }
    }

    private static SceneMesh Mesh(IEnumerable<TriangleFace> faces, ColorRgba def, bool uniform, string prefix)
    {
        var f = faces.ToList();
        var vertices = f.SelectMany(face => face.Vertices).Distinct().ToList();
        var indicesByVertex = vertices.Select((vertex, index) => (vertex, index))
            .ToDictionary(item => item.vertex, item => item.index);
        return new SceneMesh
        {
            Id = $"{prefix}-{Guid.NewGuid():N}",
            Vertices = vertices
                .Select(v => new[] { (float)v.X, (float)v.Y, (float)v.Z })
                .ToList(),
            Triangles = f.Select(face => face.Vertices
                .Select(vertex => indicesByVertex[vertex]).ToArray())
                .ToList(),
            Colors = uniform ? [def] : f.Select(x => new ColorRgba(x.Color.R, x.Color.G, x.Color.B, x.Color.A)).ToList(),
            HasUniformColor = uniform,
            SourceFaces = f
        };
    }

    private static void Paths(
        SceneRequest s,
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closed,
        IEnumerable<double>? thick,
        IEnumerable<Color>? colors)
    {
        var c = closed?.ToList() ?? [];
        var t = thick?.ToList() ?? [];
        var co = colors?.ToList() ?? [];
        var i = 0;

        foreach (var p in paths)
        {
            var v = p.Where(x => !x.IsNull())
                .Select(x => new[] { (float)x.X, (float)x.Y, (float)x.Z })
                .ToList();
            if (v.Count >= 2)
            {
                if (i < c.Count && c[i])
                    v.Add(v[0]);
                s.Paths.Add(new ScenePath
                {
                    Id = $"path-{Guid.NewGuid():N}",
                    Vertices = v,
                    Thickness = i < t.Count ? t[i] : -1,
                    Color = i < co.Count ? new ColorRgba(co[i].R, co[i].G, co[i].B, co[i].A)
                    : ColorRgba.Black
                });
            }
            i++;
        }
    }

    private static ScenePointSet Points(IEnumerable<Vector3> points, double radius, Color color)
        => new()
        {
            Id = $"points-{Guid.NewGuid():N}",
            Radius = radius <= 0 ? -1 : radius,
            Color = new ColorRgba(color.R, color.G, color.B, color.A),
            Points = points.Select(p => new[] { (float)p.X, (float)p.Y, (float)p.Z }).ToList()
        };

    private void Publish(SceneRequest scene, HoldType hold, int time, int id)
    {
        host.Publish(new SceneRequest
        {
            RequestId = scene.RequestId,
            Title = scene.Title,
            Meshes = scene.Meshes,
            Paths = scene.Paths,
            PointSets = scene.PointSets,
            IsBlocking = false,
            PersistentId = id,
            HoldType = hold,
            DisplayIntervalMilliseconds = time
        });
    }

    private static Color RandomColor(int i)
        => new(
            (byte)((i * 97 + 40) % 220),
            (byte)((i * 57 + 90) % 220),
            (byte)((i * 131 + 20) % 220));

    private static IList<Color> ExpandPathColors(int pathCount, IEnumerable<Color>? colors)
    {
        var colorList = colors?.ToList() ?? Color.Distinct64Colors.ToList();
        return Enumerable.Range(0, pathCount)
            .Select(index => index < colorList.Count
                ? colorList[index]
                : Color.GetRandomColors().First())
            .ToList();
    }
}
