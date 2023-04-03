// ***********************************************************************
// Assembly         : TVGL Presenter
// Author           : Matt
// Created          : 05-20-2016
//
// Last Modified By : Matt
// Last Modified On : 05-24-2016
// ***********************************************************************
// <copyright file="Presenter.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.SharpDX.Core;
using OxyPlot;
using System;
using System.Windows;
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static partial class Presenter
    {
        static List<Window> openWindows = new List<Window>();
        #region 2D Plots via OxyPlot

        #region Plotting 2D coordinates both scatter and polygons


        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static async Task Show(IEnumerable<Vector2> points, bool makeNew = false, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            Window currentWindow;
            if (makeNew || !openWindows.Any(w => w is Window2DPlot))
            {
                currentWindow = new Window2DPlot(points, title, plot2DType, closeShape, marker);
                openWindows.Insert(0, currentWindow);
                 currentWindow.Show();
            }
            else
            {
                currentWindow = openWindows.First(w => w is Window2DPlot);
                currentWindow.Activate();
                ((Window2DPlot)currentWindow).PlotData(points, plot2DType, closeShape, marker);
            }
        }    

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsList">The points list.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(pointsList, title, plot2DType, closeShape, marker);
            window.Show();

        }

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="pointsLists">The points lists.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsLists, title, plot2DType, closeShape, marker);
            window.Show();
        }

        public static void Show(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var points = polygons.SelectMany(polygon => polygon.AllPolygons.Select(p => p.Path)).ToList();
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.Show();
        }

        public static void Show(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
             Show(new[] { polygon }, title, plot2DType, closeShape, marker);
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
        public static void Show(IEnumerable<IEnumerable<Vector2>> points1,
            IEnumerable<IEnumerable<Vector2>> points2, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker1 = MarkerType.Circle,
            MarkerType marker2 = MarkerType.Cross)
        {
            var window = new Window2DPlot(points1, points2, title, plot2DType, closeShape, marker1, marker2);
            window.Show(); 
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
        //public static void Show(IEnumerable<Vertex> vertices, Vector3 direction, string title = "",
        //    Plot2DType plot2DType = Plot2DType.Line,
        //    bool closeShape = true, MarkerType marker = MarkerType.Circle)
        //{
        //    return Show(vertices.ProjectTo2DCoordinates(direction, out _), title, plot2DType, closeShape,
        //          marker);
        //}

        /// <summary>
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            Show(vertices.Select(listsOfVerts => listsOfVerts.ProjectTo2DCoordinates(direction, out _)),
                  title, plot2DType, closeShape, marker);
        }

        #endregion

        #endregion


        #region Show and Hang Solids
        public static void Show(TessellatedSolid ts, string heading = "", string title = "",
            string subtitle = "")
        {
             Show(new[] { ts });
        }
        public static void Show(VoxelizedSolid vs, string heading = "", string title = "",
            string subtitle = "")
        {
             Show(new[] { vs });
        }

        public static void Show(VoxelizedSolid correctVoxels, IEnumerable<Polygon> shallowTree)
        {
            throw new NotImplementedException();
        }


        public static void Show(IEnumerable<Solid> solids, string heading = "", string title = "",
            string subtitle = "")
        {
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(ConvertSolidsToModel3D(solids));
            var window = new Window3DPlot(vm);
            window.Show(); 
        }

        #endregion

    }
}