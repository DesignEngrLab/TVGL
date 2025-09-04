using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
using MarkerType = TVGL.MarkerType;


namespace WindowsDesktopPresenter
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple  class with one main
    /// function, "Show".
    /// </summary>
    public class Presenter2D : IPresenter2D
    {
        List<Window2DHeldPlot> plot2DHeldWindows = new List<Window2DHeldPlot>();


        /// <summary>
        /// Saves the polygons to a PNG of the given width and height.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="fileName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="title"></param>
        /// <param name="polyMarker"></param>
        public void SaveToPng(IEnumerable<Polygon> polygon, string fileName, int width, int height,
            string title = "", MarkerType markerType = MarkerType.None)
        {
            var vectors = polygon.SelectMany(poly => poly.AllPaths);
            var black = new Color(KnownColors.Black);
            var colors = new List<Color>();
            foreach (var vector in vectors)
                colors.Add(black);
            var window = new Window2DPlot(vectors, title, Plot2DType.Line, [true], markerType);
            var pngExporter = new PngExporter { Width = width, Height = height, Resolution = 96 };
            pngExporter.ExportToFile(window.Model, fileName);
        }


        #region 2D Plots via OxyPlot

        #region Plotting 2D coordinates both scatter and polygons
        /// <summary>
        /// Show the matrix of data as a 2D plot (heatmap)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="title"></param>
        public void ShowAndHang(double[,] data, string title = "")
        {
            var window = new Window2DPlot(data, title);
            window.ShowDialog();
        }


        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsList">The points list.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(pointsList, title, plot2DType, [closeShape], marker);
            window.ShowDialog();
        }

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsLists">The points lists.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsLists, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
             MarkerType marker = MarkerType.Circle)
        {
            var points = polygons.SelectMany(polygon => polygon.AllPolygons.Select(p => p.Path)).ToList();
            var closed = polygons.SelectMany(polygon => polygon.AllPolygons.Select(p => p.IsClosed)).ToList();
            var window = new Window2DPlot(points, title, plot2DType, closed, marker);
            window.ShowDialog();
        }

        public void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
             MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang([polygon], title, plot2DType, marker);
        }


        /// <summary>
        /// Shows two different lists of polygons using a unique marker for each.
        /// </summary>
        /// <param name="points1">The points1.</param>
        /// <param name="points2">The points2.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker1">The marker1.</param>
        /// <param name="marker2">The marker2.</param>
        public void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1,
            IEnumerable<IEnumerable<Vector2>> points2, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker1 = MarkerType.Circle,
            MarkerType marker2 = MarkerType.Cross)
        {
            var window = new Window2DPlot(points1, points2, title, plot2DType, closeShape, marker1, marker2);
            window.ShowDialog();
        }

        #endregion



        #region 2D plots projecting vertices to 2D
        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(vertices.ProjectTo2DCoordinates(direction, out _), title, plot2DType, closeShape,
                marker);
        }

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(vertices.Select(listsOfVerts => listsOfVerts.ProjectTo2DCoordinates(direction, out _)),
                title, plot2DType, closeShape, marker);
        }

        #endregion

        public void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false)
        {
            var values = new double[grid.XCount, grid.YCount];
            for (int i = 0; i < grid.XCount; i++)
                for (int j = 0; j < grid.YCount; j++)
                    values[i, j] = converter(grid[i, j]);

            ShowHeatmap(values, normalizeValues);
        }
        public void ShowHeatmap(double[,] values, bool normalizeValues = false)
        {
            var data = values;
            if (normalizeValues)
            {
                var zMax = values.Max2D();
                var zMin = values.Min2D();
                data = new double[values.GetLength(0), values.GetLength(1)];
                for (var i = 0; i < values.GetLength(0); i++)
                {
                    for (var j = 0; j < values.GetLength(1); j++)
                    {
                        data[i, j] = (values[i, j] - zMin) / (zMax - zMin);
                    }
                }
            }

            var contourSeries = new ContourSeries
            {
                Color = OxyColors.Black,
                LabelBackground = OxyColors.White,
                Data = data,
                //ColumnCoordinates = xCoordinates,
                //RowCoordinates = yCoordinates,
            };


            //var xMin = xCoordinates.Min();
            //var xMax = xCoordinates.Max();
            //var yMin = yCoordinates.Min();
            //var yMax = yCoordinates.Max();
            var heatMapSeries = new HeatMapSeries()
            {
                X0 = 0, // xMin,
                X1 = values.GetLength(0), // xMax,
                Y0 = 0, //yMin,
                Y1 = values.GetLength(1), // yMax,
                Data = data,
            };


            var heatmap = new PlotModel();
            heatmap.Axes.Add(new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(500),
                HighColor = OxyColors.Gray,
                LowColor = OxyColors.Black,
            });
            heatmap.Series.Add(heatMapSeries);
            //heatmap.Series.Add(contourSeries);

            var window = new Window2DPlot(heatmap, "Contour Map");
            window.ShowDialog();
        }
        #endregion


        public void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
            => Show([path], title, plot2DType, [closeShape], marker, holdType, timetoShow, id);

        public void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "",
            Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null,
            MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate,
            int timetoShow = -1, int id = -1)
        {
            var window = PresenterAsyncMethods.GetOrCreate3DWindow(id, plot2DHeldWindows);

            window.Dispatcher.Invoke(() =>
            {
                var vm = (Held2DViewModel)window.DataContext;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(paths, plot2DType, closePaths, marker);
                else vm.EnqueueNewSeries(paths, plot2DType, closePaths, marker);

                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;

                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }
        public void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
             MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
            => Show([polygon], title, plot2DType, marker, holdType, timetoShow, id);

        public void Show(IEnumerable<Polygon> polygon, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate,
            int timetoShow = -1, int id = -1)
            => Show(polygon.Select(p => p.Path), title, plot2DType, polygon.Select(p => p.IsClosed), marker, holdType, timetoShow, id);

    }
}