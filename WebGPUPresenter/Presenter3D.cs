using TVGL;
using Color = TVGL.Color;

namespace WebGPUPresenter;

public sealed class Presenter3D : IPresenter3D
{
    private static readonly Lazy<LocalPresenterHost> Shared = new(() => new LocalPresenterHost());
    private readonly LocalPresenterHost host;
    public Presenter3D() { host = Shared.Value; host.WaitReady(); }
    public void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "")
        => ShowAndHang([solid], heading, title, subtitle);
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
        s.Meshes.Add(Mesh(faces, Rgba32.LightGray, false, "faces"));
        host.Show(s);
    }
    public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null, IEnumerable<Color>? colors = null,
        bool otherwiseRandomPathColors = false, params Solid[] solids)
    {
        var s = Scene();
        Paths(s, paths, closePaths, lineThicknesses, colors);
        foreach (var x in solids)
            AddSolid(s, x);
        host.Show(s);
    }
    public void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1,
        Color? color = null, params Solid[] solids)
    {
        ShowAndHang([path], [closePaths], [lineThickness < 0 ? 1 : lineThickness],
            [color ?? new Color(KnownColors.Black)], false, solids);
    }
    public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null, IEnumerable<Color>? colors = null, params Solid[] solids)
    {
        ShowAndHang(paths.SelectMany(x => x), closePaths, lineThicknesses, colors, true, solids);
    }
    public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool>? closePaths,
        IEnumerable<double>? lineThicknesses, IEnumerable<Color>? colors, IEnumerable<TriangleFace>? faces)
    {
        var s = Scene();
        Paths(s, paths, closePaths, lineThicknesses, colors);
        if (faces is not null)
            s.Meshes.Add(Mesh(faces, Rgba32.LightGray, false, "faces"));
        host.Show(s);
    }
    public void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color? color = null)
    {
        throw new NotImplementedException();
    }
    public void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0,
        IEnumerable<Color>? colors = null)
    {
        throw new NotImplementedException();
    }
    public void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> a, IEnumerable<TessellatedSolid> b)
    {
        throw new NotImplementedException();
    }
    public void ShowGaussSphereWithIntensity(IEnumerable<Vertex> v, IList<Color> c, Solid s)
    {
        throw new NotImplementedException();
    }
    public void Show(Solid s, string title = "", HoldType h = HoldType.Immediate, int t = -1, int id = -1)
    {
        throw new NotImplementedException();
    }
    public void Show(ICollection<Solid> s, string title = "", HoldType h = HoldType.Immediate, int t = -1, int id = -1)
    {
        throw new NotImplementedException();
    }
    public void Show(IEnumerable<IEnumerable<Vector3>> p, IEnumerable<bool>? c = null, IEnumerable<double>? t = null,
        IEnumerable<Color>? co = null, string title = "", HoldType h = HoldType.Immediate, int time = -1, int id = -1, params Solid[] s)

    {
        throw new NotImplementedException();
    }
    public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> p, IEnumerable<IEnumerable<Matrix4x4>> pt, 
        IEnumerable<IEnumerable<Solid>> s, IEnumerable<IEnumerable<Matrix4x4>> st, IEnumerable<bool>? c = null,
        IEnumerable<double>? t = null, IEnumerable<Color>? co = null)
    {
        throw new NotImplementedException();
    }
    public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> p, IEnumerable<IEnumerable<Matrix4x4>> pt,
        IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> f, IEnumerable<IEnumerable<Matrix4x4>> ft, IEnumerable<bool>? c = null,
        IEnumerable<double>? t = null, IEnumerable<Color>? co = null)
    {
        throw new NotImplementedException();
    }

    private static SceneRequest Scene(string h = "", string t = "", string st = "") 
        => new() { RequestId = Guid.NewGuid(), Heading = h, Title = t, Subtitle = st };
    private static void AddSolid(SceneRequest s, Solid x)
    {
        if (x is TessellatedSolid ts)
            s.Meshes.Add(Mesh(ts.Faces, Rgba(ts.SolidColor), ts.HasUniformColor, "solid"));
        else if (x is CrossSectionSolid cs)
            Paths(s, cs.GetCrossSectionsAs3DLoops().SelectMany(v => v), null, null, null);
    }
    private static SceneMesh Mesh(IEnumerable<TriangleFace> faces, Rgba32 def, bool uniform, string prefix)
    {
        var f = faces.ToList(); 
        return new() { Id = $"{prefix}-{Guid.NewGuid():N}", 
            Vertices = f.SelectMany(x => x.Vertices).Select(v => new[] { (float)v.X, (float)v.Y, (float)v.Z }).ToList(),
            Triangles = Enumerable.Range(0, f.Count).Select(i => new[] { 3 * i, 3 * i + 1, 3 * i + 2 }).ToList(),
            Colors = uniform ? [def] : f.Select(x => Rgba(x.Color)).ToList(), HasUniformColor = uniform }; 
    }
    private static void Paths(SceneRequest s, IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool>? closed,
        IEnumerable<double>? thick, IEnumerable<Color>? colors) 
    {
        var c = closed?.ToList() ?? [];
        var t = thick?.ToList() ?? [];
        var co = colors?.ToList() ?? [];
        var i = 0;
        foreach (var p in paths) 
        {
            var v = p.Where(x => !x.IsNull()).Select(x => new[] { (float)x.X, (float)x.Y, (float)x.Z }).ToList();
            if (v.Count >= 2) 
            {
                if (i < c.Count && c[i])
                    v.Add(v[0]);
                s.Paths.Add(new() { Id = $"path-{Guid.NewGuid():N}", Vertices = v,
                    Thickness = i < t.Count ? t[i] : 1, Color = i < co.Count ? Rgba(co[i]) : Rgba32.Black });
            }
            i++;
        } 
    }
    private static Rgba32 Rgba(Color? c) => c is null ? Rgba32.LightGray : new(c.R, c.G, c.B, c.A);
}
