using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Selects the style used to render two-dimensional data.
    /// </summary>
    public enum Plot2DType
    {
        /// <summary>Renders connected line segments.</summary>
        Line,
        /// <summary>Renders independent data points.</summary>
        Scatter,
        /// <summary>Renders values as bars.</summary>
        Bar,
        /// <summary>Renders a two-dimensional scalar field as a heatmap.</summary>
        Heatmap,
        /// <summary>Renders the area beneath a curve.</summary>
        Area
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

    /// <summary>
    /// Controls whether a plot is added to an existing presentation or shown immediately.
    /// </summary>
    public enum HoldType
    {
        /// <summary>Adds the content to the current presentation queue.</summary>
        AddToQueue,
        /// <summary>Displays the content immediately.</summary>
        Immediate
    };

    /// <summary>
    /// Interface for the Presenter class containing all public methods and properties
    /// </summary>
    public interface IPresenter2D
    {
        /// <summary>
        /// Show the matrix of data as a 2D plot (heatmap)
        /// </summary>
        /// <param name="data">The rectangular data matrix to render.</param>
        /// <param name="title">The optional plot title.</param>
        void ShowAndHang(double[,] data, string title = "");

        /// <summary>Displays a grid as a heatmap and waits until the presentation is closed.</summary>
        /// <typeparam name="T">The type stored in the grid.</typeparam>
        /// <param name="grid">The grid to render.</param>
        /// <param name="converter">Converts each grid value to a numeric intensity.</param>
        /// <param name="normalizeValues">Whether to normalize intensities before rendering.</param>
        void ShowAndHang<T>(Grid<T> grid, Func<T, double> converter, bool normalizeValues = false);

        /// <summary>Displays a rectangular data matrix as a heatmap.</summary>
        /// <param name="values">The values to render.</param>
        /// <param name="normalizeValues">Whether to normalize values before rendering.</param>
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

        /// <summary>Displays a collection of polygons and waits for the presentation to close.</summary>
        void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.Circle);

        /// <summary>Displays a polygon and waits for the presentation to close.</summary>
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


        /// <summary>Displays one two-dimensional path.</summary>
        void Show(IEnumerable<Vector2> path, string title = "", Plot2DType plot2DType = Plot2DType.Line,
           bool closeShape = true, MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        /// <summary>Displays multiple two-dimensional paths.</summary>
        void Show(IEnumerable<IEnumerable<Vector2>> paths, string title = "",
           Plot2DType plot2DType = Plot2DType.Line, IEnumerable<bool> closePaths = null,
           MarkerType marker = MarkerType.Circle, HoldType holdType = HoldType.Immediate,
           int timetoShow = -1, int id = -1);
        /// <summary>Displays one polygon without blocking the calling code.</summary>
        void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        /// <summary>Displays multiple polygons without blocking the calling code.</summary>
        void Show(IEnumerable<Polygon> polygon, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            MarkerType marker = MarkerType.None, HoldType holdType = HoldType.Immediate,
            int timetoShow = -1, int id = -1);

        /// <summary>Displays a sequence of data matrices and waits for the user.</summary>
        void ShowStepsAndHang(ICollection<double[,]> data, string title = "");
        /// <summary>Displays data matrices with associated point collections.</summary>
        void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<Vector2>> points,
            bool connectPointsInLine, string title = "");
        /// <summary>Displays data matrices with multiple associated point collections.</summary>
        void ShowStepsAndHang(ICollection<double[,]> data, IEnumerable<IEnumerable<IEnumerable<Vector2>>> points,
           IEnumerable<bool> connectPointsInLine, string title = "");

    }
}
