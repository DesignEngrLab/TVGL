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

        /// <summary>
        ///     Shows two different lists of polygons using a unique marker for each.
        /// </summary>
        /// <param name="points2"></param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="points1"></param>
        /// <param name="marker1"></param>
        /// <param name="marker2"></param>
        public static void ShowAndHang(IList<List<Point>> points1, IList<List<Point>> points2, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker1 = MarkerType.Circle, MarkerType marker2 = MarkerType.Cross)
        {
            var window = new Window2DPlot(points1, points2, title, plot2DType, closeShape, marker1, marker2);
            window.ShowDialog();
        }


        /// <summary>
        ///     Shows the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IList<List<double[]>> points, IList<Color> colors, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker, colors);
            window.ShowDialog();
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
        public static void ShowVertexPathsWithSolid(IList<double[]> segments, IList<TessellatedSolid> solids)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            foreach (var tessellatedSolid in solids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }

            foreach (var point in segments)
            {
                var lineCollection = new List<Point3D>
                {
                    new Point3D(point[0], point[1], point[2]),
                    new Point3D(point[3], point[4], point[5])
                };
                var color = new System.Windows.Media.Color();
                color.R = 255; //G & B default to 0 to form red
                var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection), Color = color };
                window.view1.Children.Add(lines);
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }
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
                    var color = new System.Windows.Media.Color();
                    color.R = 255; //G & B default to 0 to form red
                    var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection), Color = color };
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

        public static void ShowVertexPaths(IList<List<List<double[]>>> vertexPaths)
        {
            var window = new Window3DPlot();

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

        /// <summary>
        ///     Shows vertex paths. Assumes paths are closed.
        /// </summary>
        /// <param name="vertexPaths"></param>
        public static void ShowGaussSphereWithIntensity(IList<Vertex> vertices, IList<Color> colors, TessellatedSolid ts)
        {
            var window = new Window3DPlot();
            var pt0 = new Point3D(ts.Center[0], ts.Center[1], ts.Center[2]);
            var x = ts.XMax - ts.XMin;
            var y = ts.YMax - ts.YMin;
            var z = ts.ZMax - ts.ZMin;
            var radius = System.Math.Max(System.Math.Max(x, y), z) / 2;

            //Add the solid to the visual
            var model = MakeModelVisual3D(ts);
            window.view1.Children.Add(model);

            //Add a transparent unit sphere to the visual
            var sphere = new SphereVisual3D();
            sphere.Radius = radius;
            sphere.Center = pt0;
            sphere.Material = MaterialHelper.CreateMaterial(new System.Windows.Media.Color { A = 15, R = 200, G = 200, B = 200 });
            //window.view1.Children.Add(sphere);

            var i = 0;
            foreach (var point in vertices)
            {
                var color = colors[i];
                var pt1 = new Point3D(pt0.X + point.X * radius, pt0.Y + point.Y * radius, pt0.Z + point.Z * radius);


                //No create a line collection by doubling up the points
                var lineCollection = new List<Point3D>();
                lineCollection.Add(pt0);
                lineCollection.Add(pt1);

                var systemColor = new System.Windows.Media.Color();
                systemColor.A = 255;
                systemColor.R = color.R;
                systemColor.G = color.G;
                systemColor.B = color.B;


                var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection), Color = systemColor, Thickness = 5 };


                var pointsVisual = new PointsVisual3D { Color = systemColor, Size = 5 };
                pointsVisual.Points = new Point3DCollection() { pt1 };
                window.view1.Children.Add(pointsVisual);
                window.view1.Children.Add(lines);
                i++;
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

        public static void ShowAndHangTransparentsAndSolids(IList<TessellatedSolid> transparents, IList<TessellatedSolid> solids)
        {
            var window = new Window3DPlot();
            foreach (var ts in solids)
            {
                window.view1.Children.Add(MakeModelVisual3D(ts));
            }
            foreach (var ts in transparents)
            {
                var positions =
                ts.Faces.SelectMany(
                 f => f.Vertices.Select(v => new Point3D(v.Position[0], v.Position[1], v.Position[2])));
                var normals =
                    ts.Faces.SelectMany(f => f.Vertices.Select(v => new Vector3D(f.Normal[0], f.Normal[1], f.Normal[2])));
                var r = ts.SolidColor.R;
                var g = ts.SolidColor.G;
                var b = ts.SolidColor.B;
                window.view1.Children.Add(
                new ModelVisual3D
                {
                    Content =
                        new GeometryModel3D
                        {
                            Geometry = new MeshGeometry3D
                            {
                                Positions = new Point3DCollection(positions),
                                Normals = new Vector3DCollection(normals)
                            },
                            Material = MaterialHelper.CreateMaterial(new System.Windows.Media.Color { A = 120, R = r, G = g, B = b })
                        }
                });
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

        //A palet of distinguishable colors
        //http://graphicdesign.stackexchange.com/questions/3682/where-can-i-find-a-large-palette-set-of-contrasting-colors-for-coloring-many-d
        public static string[] ColorPalet()
        {
            return new[]
            {
                "#000000",
                "#1CE6FF",
                "#FF34FF",
                "#FF4A46",
                "#008941",
                "#006FA6",
                "#A30059",
                "#FFDBE5",
                "#7A4900",
                "#0000A6",
                "#63FFAC",
                "#B79762",
                "#004D43",
                "#8FB0FF",
                "#997D87",
                "#5A0007",
                "#809693",
                "#FEFFE6",
                "#1B4400",
                "#4FC601",
                "#3B5DFF",
                "#4A3B53",
                "#FF2F80",
                "#61615A",
                "#BA0900",
                "#6B7900",
                "#00C2A0",
                "#FFAA92",
                "#FF90C9",
                "#B903AA",
                "#D16100",
                "#DDEFFF",
                "#000035",
                "#7B4F4B",
                "#A1C299",
                "#300018",
                "#0AA6D8",
                "#013349",
                "#00846F",
                "#372101",
                "#FFB500",
                "#C2FFED",
                "#A079BF",
                "#CC0744",
                "#C0B9B2",
                "#C2FF99",
                "#001E09",
                "#00489C",
                "#6F0062",
                "#0CBD66",
                "#EEC3FF",
                "#456D75",
                "#B77B68",
                "#7A87A1",
                "#788D66",
                "#885578",
                "#FAD09F",
                "#FF8A9A",
                "#D157A0",
                "#BEC459",
                "#456648",
                "#0086ED",
                "#886F4C",
                "#34362D",
                "#B4A8BD",
                "#00A6AA",
                "#452C2C",
                "#636375",
                "#A3C8C9",
                "#FF913F",
                "#938A81",
                "#575329",
                "#00FECF",
                "#B05B6F",
                "#8CD0FF",
                "#3B9700",
                "#04F757",
                "#C8A1A1",
                "#1E6E00",
                "#7900D7",
                "#A77500",
                "#6367A9",
                "#A05837",
                "#6B002C",
                "#772600",
                "#D790FF",
                "#9B9700",
                "#549E79",
                "#FFF69F",
                "#201625",
                "#72418F",
                "#BC23FF",
                "#99ADC0",
                "#3A2465",
                "#922329",
                "#5B4534",
                "#FDE8DC",
                "#404E55",
                "#0089A3",
                "#CB7E98",
                "#A4E804",
                "#324E72",
                "#6A3A4C"
            };
        }
    }
}