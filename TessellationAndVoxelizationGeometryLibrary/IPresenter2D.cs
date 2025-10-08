using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public enum Plot2DType
    {
        Line,
        Scatter,
        Bar,
        Heatmap
    }

    /// <summary>
    /// Defines the type of marker to use when rendering points in plots.
    /// Note that this current set of names and the order is based on the OxyPlot library.
    /// </summary>
    public enum MarkerType
    {
        /// <summary>
        /// Do not render markers.
        /// </summary>
        None,

        /// <summary>
        /// Render markers as circles.
        /// </summary>
        Circle,

        /// <summary>
        /// Render markers as squares.
        /// </summary>
        Square,

        /// <summary>
        /// Render markers as diamonds.
        /// </summary>
        Diamond,

        /// <summary>
        /// Render markers as triangles.
        /// </summary>
        Triangle,

        /// <summary>
        /// Render markers as crosses 
        /// </summary>
        Cross,

        /// <summary>
        /// Renders markers as plus signs 
        /// </summary>
        Plus,

        /// <summary>
        /// Renders markers as stars 
        /// </summary>
        Star
    }

    public enum HoldType
    {
        AddToQueue,
        Immediate
    };

    /// <summary>
    /// Interface for the Presenter class containing all public methods and properties
    /// </summary>
    public interface IPresenter2D
    {

        void SaveToPng(IEnumerable<Polygon> polygon, string fileName, int width, int height,
           string title = "", MarkerType markerType = MarkerType.None);

        /// <summary>
        /// Show the matrix of data as a 2D plot (heatmap)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="title"></param>
        void ShowAndHang(double[,] data, string title = "");


        void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false);

        void ShowHeatmap(double[,] values, bool normalizeValues = false);
        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle);

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsList">The points list.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle);

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsLists">The points lists.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle);

        void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.Circle);

        void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.Circle);

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
        void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1,
           IEnumerable<IEnumerable<Vector2>> points2, string title = "",
           Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker1 = MarkerType.Circle,
           MarkerType marker2 = MarkerType.Cross);

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "",
           Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle);

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "",
           Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle);


        void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "",
           Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null,
           MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate,
           int timetoShow = -1, int id = -1);
        void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        void Show(IEnumerable<Polygon> polygon, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate,
            int timetoShow = -1, int id = -1);

        void ShowStepsAndHang(ICollection<double[,]> data, string title = "");
        void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<Vector2>> points,
            bool connectPointsInLine, string title = "");
        void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>> points,
           IEnumerable<bool> connectPointsInLine, string title = "");

    }
}
