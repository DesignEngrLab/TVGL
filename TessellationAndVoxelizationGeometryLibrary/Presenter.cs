using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TVGL
{
    public static class Presenter
    {
        public static void Show(Solid solid, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
  => Global.Presenter3D.Show(solid, title, holdType, timetoShow, id);
        public static void Show(ICollection<Solid> solids, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
       => Global.Presenter3D.Show(solids, title, holdType, timetoShow, id);
        public static void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids)
          => Global.Presenter3D.Show(paths, closePaths, lineThicknesses, colors, title, holdType, timetoShow, id, solids);

        public static void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
=> Global.Presenter2D.Show(path, title, plot2DType, closeShape, marker, holdType, timetoShow, id);
        public static void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "", Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
       => Global.Presenter2D.Show(paths, title, plot2DType, closePaths, marker, holdType, timetoShow, id);
        public static void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
       => Global.Presenter2D.Show(polygon, title, plot2DType, marker, holdType, timetoShow, id);
        public static void Show(IEnumerable<Polygon> polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
       => Global.Presenter2D.Show(polygon, title, plot2DType, marker, holdType, timetoShow, id);
        public static void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "")
        => Global.Presenter3D.ShowAndHang(solid, heading, title, subtitle);
        public static void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
        => Global.Presenter3D.ShowAndHang(solids, heading, title, subtitle);
        public static void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        => Global.Presenter3D.ShowAndHang(paths, closePaths, lineThicknesses, colors, solids);
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, bool otherwiseRandomColors = false, params Solid[] solids)
       => Global.Presenter3D.ShowAndHang(paths, closePaths, lineThicknesses, colors, otherwiseRandomColors, solids);
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, IEnumerable<TriangleFace> faces = null)
        => Global.Presenter3D.ShowAndHang(paths, closePaths, lineThicknesses, colors, faces);
        public static void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids)
        => Global.Presenter3D.ShowAndHang(path, closePaths, lineThickness, color, solids);
        public static void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
       => Global.Presenter3D.ShowAndHang(faces, heading, title, subtitle);
        public static void ShowAndHang(double[,] data, string title = "")
    => Global.Presenter2D.ShowAndHang(data, title);
        public static void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false)
        => Global.Presenter2D.ShowAndHang(grid, converter, normalizeValues);
        public static void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => Global.Presenter2D.ShowAndHang(points, title, plot2DType, closeShape, marker);
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
       => Global.Presenter2D.ShowAndHang(pointsList, title, plot2DType, closeShape, marker);
        public static void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => Global.Presenter2D.ShowAndHang(pointsLists, title, plot2DType, closeShape, marker);
        public static void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
       => Global.Presenter2D.ShowAndHang(polygons, title, plot2DType, marker);
        public static void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
        => Global.Presenter2D.ShowAndHang(polygon, title, plot2DType, marker);
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1, IEnumerable<IEnumerable<Vector2>> points2, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker1 = MarkerType.Circle, MarkerType marker2 = MarkerType.Cross)
       => Global.Presenter2D.ShowAndHang(points1, points2, title, plot2DType, closeShape, marker1, marker2);
        public static void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        => Global.Presenter2D.ShowAndHang(vertices, direction, title, plot2DType, closeShape, marker);
        public static void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
       => Global.Presenter2D.ShowAndHang(vertices, direction, title, plot2DType, closeShape, marker);
        public static void ShowStepsAndHang(ICollection<double[,]> data, string title = "")
            => Global.Presenter2D.ShowStepsAndHang(data, title);
        public static void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<Vector2>> points,
            bool connectPointsInLine, string title = "")
             => Global.Presenter2D.ShowStepsAndHang(data, points, connectPointsInLine, title);
        public static void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>> points,
           IEnumerable<bool> connectPointsInLine, string title = "")
            => Global.Presenter2D.ShowStepsAndHang(data, points, connectPointsInLine, title);

        public static void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparentSolids, IEnumerable<TessellatedSolid> solids)
       => Global.Presenter3D.ShowAndHangTransparentsAndSolids(transparentSolids, solids);
        public static void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid)
        => Global.Presenter3D.ShowGaussSphereWithIntensity(vertices, colors, solid);
        public static void ShowHeatmap(double[,] values, bool normalizeValues = false)
      => Global.Presenter2D.ShowHeatmap(values, normalizeValues);
        public static void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null)
        => Global.Presenter3D.ShowPointsAndHang(points, radius, color);
        public static void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null)
        => Global.Presenter3D.ShowPointsAndHang(pointSets, radius, colors);
        public static void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IList<IEnumerable<Solid>> solids, IEnumerable<IEnumerable<Matrix4x4>> solidTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null)
            => Global.Presenter3D.ShowStepsAndHang(paths, pathTransforms, solids, solidTransforms, closePaths,
                lineThicknesses, colors);
        public static void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
       IList<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IEnumerable<IEnumerable<Matrix4x4>> fGTransforms, IEnumerable<bool> closePaths = null,
       IEnumerable<double> lineThicknesses = null, IEnumerable<Color> pathColors = null)
                => Global.Presenter3D.ShowStepsAndHang(paths, pathTransforms, faceGroups, fGTransforms, closePaths,
                    lineThicknesses, pathColors);
    }



    internal class EmptyPresenter3D : IPresenter3D
    {
        public void Show(Solid solid, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void Show(ICollection<Solid> solids, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "", HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids)
        {
            // do nothing
        }

        public void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "")
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, IEnumerable<TriangleFace> faces = null)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, bool otherwiseRandomPathColors = false, params Solid[] solids)
        {
            // do nothing
        }

        public void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparentSolids, IEnumerable<TessellatedSolid> solids)
        {
            // do nothing
        }

        public void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid)
        {
            // do nothing
        }

        public void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null)
        {
            // do nothing
        }

        public void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null)
        {
            // do nothing
        }

        public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms, IEnumerable<IEnumerable<Solid>> solids, IEnumerable<IEnumerable<Matrix4x4>> solidTransforms, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null)
        {
            // do nothing
        }

        public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms, IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IEnumerable<IEnumerable<Matrix4x4>> fGTransforms, IEnumerable<bool> closePaths = null, IEnumerable<double> lineThicknesses = null, IEnumerable<Color> pathColors = null)
        {
            // do nothing
        }
    }


    internal class EmptyPresenter2D : IPresenter2D
    {
        public void SaveToPng(IEnumerable<Polygon> polygon, string fileName, int width, int height, string title = "", MarkerType markerType = MarkerType.None)
        {
            throw new NotImplementedException();
        }

        public void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "", Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void Show(IEnumerable<Polygon> polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            // do nothing
        }

        public void ShowAndHang(double[,] data, string title = "")
        {
            // do nothing
        }

        public void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1, IEnumerable<IEnumerable<Vector2>> points2, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker1 = MarkerType.Circle, MarkerType marker2 = MarkerType.Cross)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "", Plot2DType plot2DType = Plot2DType.Line, bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            // do nothing
        }

        public void ShowHeatmap(double[,] values, bool normalizeValues = false)
        {
            // do nothing
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
}
