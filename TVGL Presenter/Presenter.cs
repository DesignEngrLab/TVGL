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

using System.Collections;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using OxyPlot;

namespace TVGL
{
    /// <summary>
    ///     The Class HelixPresenter is the only class within the TVGL Helix Presenter
    ///     project (TVGL_Presenter.dll). It is a simple static class with one main
    ///     function, "Show".
    /// </summary>
    public static class Presenter
    {
        #region 2D Plots via OxyPlot

        #region Single Series of Points

        /// <summary>
        ///     Shows the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<Point> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.Show();
        }

        /// <summary>
        ///     Shows the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<Vertex> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            Show(MiscFunctions.Get2DProjectionPoints(vertices, direction, false), title, plot2DType, closeShape, marker);
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<Point> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<List<Point>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsList, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<List<List<Point>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsLists, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<Vertex> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(MiscFunctions.Get2DProjectionPoints(vertices, direction, false), title, plot2DType, closeShape,
                marker);
        }

        #endregion

        #region List of Series of Points

        #region for Points

        /// <summary>
        ///     Shows the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<Point[]> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.Show();
        }

        /// <summary>
        ///     Shows the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<List<Point>> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.Show();
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<List<Point>> points, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<Point[]> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        #endregion

        #region for Vertices and projection vector

        /// <summary>
        ///     Shows the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<List<Vertex>> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            Show(
                vertices.Select(listsOfVerts => MiscFunctions.Get2DProjectionPoints(listsOfVerts, direction, false))
                    .ToList(), title, plot2DType, closeShape, marker);
        }

        /// <summary>
        ///     Shows the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void Show(IList<Vertex[]> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            Show(
                vertices.Select(listsOfVerts => MiscFunctions.Get2DProjectionPoints(listsOfVerts, direction, false))
                    .ToList(), title, plot2DType, closeShape, marker);
        }


        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<List<Vertex>> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(
                vertices.Select(listsOfVerts => MiscFunctions.Get2DProjectionPoints(listsOfVerts, direction, false))
                    .ToList(), title, plot2DType, closeShape, marker);
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<Vertex[]> vertices, double[] direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(
                vertices.Select(listsOfVerts => MiscFunctions.Get2DProjectionPoints(listsOfVerts, direction, false))
                    .ToList(), title, plot2DType, closeShape, marker);
        }

        #endregion

        #endregion

        #endregion

        #region 3D Plots via Helix.Toolkit
        public static void ShowVertexPathsWithSolid(IList<List<List<double[]>>> vertexPaths, IList<TessellatedSolid> solids)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            foreach (var tessellatedSolid in solids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }

            foreach (var crossSection in vertexPaths)
            {
                foreach (var path in crossSection)
                {
                    var contour = path.Select(point => new Point3D(point[0], point[1], point[2])).ToList();

                    //No create a line collection by doubling up the points
                    var lineCollection = new List<Point3D>();
                    foreach (var t in contour)
                    {
                        lineCollection.Add(t);
                        lineCollection.Add(t);
                    }
                    lineCollection.RemoveAt(0);
                    lineCollection.Add(lineCollection.First());

                    var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection) };
                    window.view1.Children.Add(lines);
                }
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        public static void ShowVertexPathsWithSolid(IList<List<List<Vertex>>> vertexPaths, IList<TessellatedSolid> solids)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            foreach (var tessellatedSolid in solids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }

            foreach (var crossSection in vertexPaths)
            {
                foreach (var path in crossSection)
                {
                    var contour = path.Select(point => new Point3D(point.X, point.Y, point.Z)).ToList();

                    //No create a line collection by doubling up the points
                    var lineCollection = new List<Point3D>();
                    foreach (var t in contour)
                    {
                        lineCollection.Add(t);
                        lineCollection.Add(t);
                    }
                    lineCollection.RemoveAt(0);
                    lineCollection.Add(lineCollection.First());

                    var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection) };
                    window.view1.Children.Add(lines);
                }
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows vertex paths. Assumes paths are closed.
        /// </summary>
        /// <param name="vertexPaths"></param>
        public static void ShowVertexPaths(IList<List<List<Vertex>>> vertexPaths)
        {
            var window = new Window3DPlot();

            foreach (var crossSection in vertexPaths)
            {
                foreach (var path in crossSection)
                {
                    var contour = path.Select(point => new Point3D(point.X, point.Y, point.Z)).ToList();

                    //No create a line collection by doubling up the points
                    var lineCollection = new List<Point3D>();
                    foreach (var t in contour)
                    {
                        lineCollection.Add(t);
                        lineCollection.Add(t);
                    }
                    lineCollection.RemoveAt(0);
                    lineCollection.Add(lineCollection.First());

                    var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection) };
                    window.view1.Children.Add(lines);
                }
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows the specified tessellated solid in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        /// <param name="seconds">The seconds.</param>
        public static void Show(TessellatedSolid tessellatedSolid, int seconds = 0)
        {
            var window = new Window3DPlot();
            window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            if (seconds > 0)
            {
                window.Show();
                Thread.Sleep(seconds * 1000);
                window.Close();
            }
            else window.Show();
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        public static void ShowAndHang(TessellatedSolid tessellatedSolid)
        {
            var window = new Window3DPlot();
            window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds">The seconds.</param>
        public static void Show(IList<TessellatedSolid> tessellatedSolids, int seconds = 0)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            if (seconds > 0)
            {
                window.Show();
                Thread.Sleep(seconds * 1000);
                window.Close();
            }
            else window.Show();
        }

        /// <summary>
        ///     Shows the and hang.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        public static void ShowAndHang(IList<TessellatedSolid> tessellatedSolids)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        /// <summary>
        ///     Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds">The seconds.</param>
        public static void ShowSequentially(IList<TessellatedSolid> tessellatedSolids, int seconds = 1)
        {
            //var models = new List<Visual3D>();
            var window = new Window3DPlot();
            var models = new ObservableCollection<Visual3D>();
            var startLocation = window.WindowStartupLocation;

            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                window.view1.Children.Add(model);
                window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
                window.WindowStartupLocation = startLocation;
                window.Show();
                Thread.Sleep(seconds * 1000);
                window.Hide();
                //var size = new Size(400, 400);
                window.InvalidateVisual();
                //window.Top = window.Top;
                //window.Left = window.Left;
                //window.UpdateLayout();
                //window.view1.Items.Refresh();
                //window.view1.UpdateLayout();              
                //window.Measure(size);
                //window.Arrange(new Rect(new System.Windows.Point(0, 0), size));
            }
            window.Close();
        }


        public static void ShowWithConvexHull(TessellatedSolid ts)
        {
            var window = new Window3DPlot();
            window.view1.Children.Add(MakeModelVisual3D(ts));
            var positions =
         ts.ConvexHull.Faces.SelectMany(
             f => f.Vertices.Select(v => new Point3D(v.Position[0], v.Position[1], v.Position[2])));
            var normals =
                ts.ConvexHull.Faces.SelectMany(f => f.Vertices.Select(v => new Vector3D(f.Normal[0], f.Normal[1], f.Normal[2])));
            window.view1.Children.Add(
            new ModelVisual3D
            {
                Content =
                    new GeometryModel3D
                    {
                        Geometry = new MeshGeometry3D
                        {
                            Positions = new Point3DCollection(positions),
                            // TriangleIndices = new Int32Collection(triIndices),
                            Normals = new Vector3DCollection(normals)
                        },
                        Material = MaterialHelper.CreateMaterial(new System.Windows.Media.Color { A = 189, G = 189, B = 189 })
                    }
            });
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            //window.Show();
            window.ShowDialog();
        }


        /// <summary>
        ///     Makes the model visual3 d.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>Visual3D.</returns>
        private static Visual3D MakeModelVisual3D(TessellatedSolid ts)
        {
            var defaultMaterial = MaterialHelper.CreateMaterial(
                new System.Windows.Media.Color
                {
                    A = ts.SolidColor.A,
                    B = ts.SolidColor.B,
                    G = ts.SolidColor.G,
                    R = ts.SolidColor.R
                });
            if (ts.HasUniformColor)
            {
                var positions =
                    ts.Faces.SelectMany(
                        f => f.Vertices.Select(v => new Point3D(v.Position[0], v.Position[1], v.Position[2])));
                var normals =
                    ts.Faces.SelectMany(f => f.Vertices.Select(v => new Vector3D(f.Normal[0], f.Normal[1], f.Normal[2])));
                return new ModelVisual3D
                {
                    Content =
                        new GeometryModel3D
                        {
                            Geometry = new MeshGeometry3D
                            {
                                Positions = new Point3DCollection(positions),
                                // TriangleIndices = new Int32Collection(triIndices),
                                Normals = new Vector3DCollection(normals)
                            },
                            Material = defaultMaterial
                        }
                };
            }
            var result = new ModelVisual3D();
            foreach (var f in ts.Faces)
            {
                var vOrder = new Point3DCollection();
                for (var i = 0; i < 3; i++)
                    vOrder.Add(new Point3D(f.Vertices[i].X, f.Vertices[i].Y, f.Vertices[i].Z));

                var c = f.Color == null
                    ? defaultMaterial
                    : MaterialHelper.CreateMaterial(new System.Windows.Media.Color
                    {
                        A = f.Color.A,
                        B = f.Color.B,
                        G = f.Color.G,
                        R = f.Color.R
                    });
                result.Children.Add(new ModelVisual3D
                {
                    Content =
                        new GeometryModel3D
                        {
                            Geometry = new MeshGeometry3D { Positions = vOrder },
                            Material = c
                        }
                });
            }
            return result;
        }

        #endregion
    }
}