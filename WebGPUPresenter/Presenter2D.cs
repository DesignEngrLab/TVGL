using TVGL;
using Color = TVGL.Color;

namespace WebGPUPresenter;

public sealed class Presenter2D : IPresenter2D
{
    public void SaveToPng(IEnumerable<Polygon> polygon, string fileName, int width, int height, string title = "", MarkerType markerType = MarkerType.None, Color lineColor = null)
    {
        throw new NotImplementedException();
    }

    public void SaveToPng(IEnumerable<Polygon> polygons, string fileName, int width, int height, Color lineColor, Color fillColor, Color backgroundColor, Polygon outerBorder = null, string title = "", MarkerType markerType = MarkerType.None)
    {
        throw new NotImplementedException();
    }

    public void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
    {
        throw new NotImplementedException();
    }

    public void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "", Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
    {
        throw new NotImplementedException();
    }

    public void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
    {
        throw new NotImplementedException();
    }

    public void Show(IEnumerable<Polygon> polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(double[,] data, string title = "")
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1, IEnumerable<IEnumerable<Vector2>> points2, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker1 = MarkerType.Circle, MarkerType marker2 = MarkerType.Cross)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
    {
        throw new NotImplementedException();
    }

    public void ShowHeatmap(double[,] values, bool normalizeValues = false)
    {
        throw new NotImplementedException();
    }

    public void ShowStepsAndHang(ICollection<double[,]> data, string title = "")
    {
        throw new NotImplementedException();
    }

    public void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<Vector2>> points, bool connectPointsInLine, string title = "")
    {
        throw new NotImplementedException();
    }

    public void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>> points, IEnumerable<bool> connectPointsInLine, string title = "")
    {
        throw new NotImplementedException();
    }
}
