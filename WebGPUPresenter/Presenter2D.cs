using BugViewer;
using TVGL;
using Color = TVGL.Color;

namespace WebGPUPresenter;

public sealed class Presenter2D : IPresenter2D
{
    private readonly LocalPresenterHost host;
    public Presenter2D() { host = Presenter3D.SharedHost; host.WaitReady(); }

    public void ShowAndHang(double[,] data, string title = "") => Block(Heatmap(data, false, title));
    public void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false)
    {
        var values = new double[grid.XCount, grid.YCount];
        for (var x = 0; x < grid.XCount; x++) for (var y = 0; y < grid.YCount; y++) values[x, y] = converter(grid[x, y]);
        Block(Heatmap(values, normalizeValues, "Contour Map"));
    }
    public void ShowHeatmap(double[,] values, bool normalizeValues = false) => Block(Heatmap(values, normalizeValues, "Contour Map"));
    public void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => ShowAndHang([points], title, plot2DType, closeShape, marker);
    public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => Block(Plot(pointsList, title, plot2DType, Repeat(closeShape), Repeat(marker)));
    public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => Block(new SceneRequest { RequestId = Guid.NewGuid(), Kind = PresentationKind.TwoDimensional, Title = title, Steps = pointsLists.Select(p => Plot(p, title, plot2DType, Repeat(closeShape), Repeat(marker))).ToList() });
    public void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
        => Block(Plot(polygons.SelectMany(p => p.AllPolygons).Select(p => p.Path), title, plot2DType, polygons.SelectMany(p => p.AllPolygons).Select(p => p.IsClosed), Repeat(marker)));
    public void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle) => ShowAndHang([polygon], title, plot2DType, marker);
    public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1, IEnumerable<IEnumerable<Vector2>> points2, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker1 = MarkerType.Circle, MarkerType marker2 = MarkerType.Cross)
        => Block(Plot(points1.Concat(points2), title, plot2DType, Repeat(closeShape), Repeat(marker1).Concat(Repeat(marker2))));
    public void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => ShowAndHang(vertices.ProjectTo2DCoordinates(direction, out _), title, plot2DType, closeShape, marker);
    public void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => ShowAndHang(vertices.Select(v => v.ProjectTo2DCoordinates(direction, out _)), title, plot2DType, closeShape, marker);

    public void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        => Show([path], title, plot2DType, [closeShape], marker, holdType, timetoShow, id);
    public void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "", Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        => Publish(Plot(paths, title, plot2DType, closePaths ?? Repeat(true), Repeat(marker), false, id, holdType, timetoShow));
    public void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1) => Show([polygon], title, plot2DType, marker, holdType, timetoShow, id);
    public void Show(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        => Show(polygons.Select(p => p.Path), title, plot2DType, polygons.Select(p => p.IsClosed), marker, holdType, timetoShow, id);

    public void ShowStepsAndHang(ICollection<double[,]> data, string title = "") => Block(Steps(data, null, null, title));
    public void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<Vector2>> points, bool connectPointsInLine, string title = "") => Block(Steps(data, points.Select(p => (IEnumerable<IEnumerable<Vector2>>)[p]), Repeat(connectPointsInLine), title));
    public void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>> points, IEnumerable<bool> connectPointsInLine, string title = "") => Block(Steps(data, points, connectPointsInLine, title));

    private void Block(SceneRequest request) => host.Show(request);
    private void Publish(SceneRequest request) => host.Publish(request);
    private static SceneRequest Heatmap(double[,] data, bool normalize, string title) => new() { RequestId = Guid.NewGuid(), Kind = PresentationKind.TwoDimensional, Title = title, Plot = new PlotRequest { Heatmap = ToJagged(Normalize(data, normalize)), NormalizeHeatmap = normalize } };
    private static SceneRequest Plot(IEnumerable<IEnumerable<Vector2>> paths, string title, Plot2DType type,
        IEnumerable<bool> closed, IEnumerable<MarkerType> markers, bool blocking = true, int id = -1,
        HoldType hold = HoldType.Immediate, int time = -1)
    {
        var closedEnumerator = closed.GetEnumerator();
        bool cs = false;
        var markerEnumerator = markers.GetEnumerator();
        var i = 0;
        var traces = paths.Select(path =>
        {
            var p = path.ToList();
            var ms = markerEnumerator.MoveNext() ? markerEnumerator.Current : MarkerType.None;
            cs = closedEnumerator.MoveNext() ? closedEnumerator.Current : cs;
            var t = new PlotTrace
            {
                Name = $"series {i + 1}",
                X = p.Select(v => v.X).ToList(),
                Y = p.Select(v => v.Y).ToList(),
                Type = type,
                Closed = cs,
                Marker = ms,
                Color = ColorAt(i++)
            };
            return t;
        }).ToList();
        return new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Kind = PresentationKind.TwoDimensional,
            IsBlocking = blocking,
            PersistentId = id,
            HoldType = hold,
            DisplayIntervalMilliseconds = time,
            Title = title,
            Plot = new PlotRequest { Traces = traces }
        };
    }
    private static SceneRequest Steps(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>>? overlays, IEnumerable<bool>? closed, string title)
    {
        var overlayList = overlays?.ToList();
        var closedPaths = closed ?? Repeat(false);
        return new SceneRequest {
            RequestId = Guid.NewGuid(),
            Kind = PresentationKind.TwoDimensional,
            Title = title, 
            Steps = data.Select((d, i) => new SceneRequest {
                RequestId = Guid.NewGuid(), 
                Kind = PresentationKind.TwoDimensional,
                Title = title,
                Plot = new PlotRequest {
                    Heatmap = ToJagged(d),
                    Traces = overlayList is not null && i < overlayList.Count 
                    ? Plot(overlayList[i], title, Plot2DType.Line, closedPaths, Repeat(MarkerType.None)).Plot!.Traces : [] 
                }
            }).ToList() };
    }
    private static double[][] ToJagged(double[,] data)
    {
        var rows = data.GetLength(0);
        var columns = data.GetLength(1);
        var result = new double[rows][];
        for (var row = 0; row < rows; row++)
        {
            result[row] = new double[columns];
            for (var column = 0; column < columns; column++)
                result[row][column] = data[row, column];
        }
        return result;
    }
    private static double[,] Normalize(double[,] values, bool enabled)
    {
        if (!enabled) return values; var min = values.Cast<double>().Min(); var max = values.Cast<double>().Max(); var result = new double[values.GetLength(0), values.GetLength(1)];
        for (var x = 0; x < values.GetLength(0); x++) for (var y = 0; y < values.GetLength(1); y++) result[x, y] = max == min ? 0 : (values[x, y] - min) / (max - min); return result;
    }
    private static IEnumerable<T> Repeat<T>(T value) { while (true) yield return value; }
    private static ColorRgba ColorAt(int index) => new((byte)((index * 97 + 40) % 220), (byte)((index * 57 + 90) % 220), (byte)((index * 131 + 20) % 220));
}
