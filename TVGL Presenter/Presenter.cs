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

using TVGL.Voxelization;
using System;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static class Presenter
    {
        #region 2D Plots via OxyPlot

        #region Plotting 2D coordinates both scatter and polygons


        /// <summary>
        /// Shows the and hang.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        /// <summary>
        /// Shows the and hang.
        /// </summary>
        /// <param name="pointsList">The points list.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsList, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        /// <summary>
        /// Shows the and hang.
        /// </summary>
        /// <param name="pointsLists">The points lists.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsLists, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<PolygonLight> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var points = polygons.Select(polygon => polygon.Path).ToList();
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
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
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector2>> points1,
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
        /// Shows the and hang.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IEnumerable<Vertex> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(vertices.ProjectVerticesTo2DCoordinates(direction, out _), title, plot2DType, closeShape,
                marker);
        }

        /// <summary>
        /// Shows the and hang.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="title">The title.</param>
        /// <param name="plot2DType">Type of the plot2 d.</param>
        /// <param name="closeShape">if set to <c>true</c> [close shape].</param>
        /// <param name="marker">The marker.</param>
        public static void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(vertices.Select(listsOfVerts => listsOfVerts.ProjectVerticesTo2DCoordinates(direction, out _)),
                title, plot2DType, closeShape, marker);
        }

        #endregion

        #endregion

        #region 3D Plots via Helix.Toolkit

        /// <summary>
        /// Shows the vertex paths with solid.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="closePaths"></param>
        /// <param name="solid"></param>
        public static void ShowVertexPaths(IEnumerable<List<double[]>> paths, bool closePaths = true, TessellatedSolid solid = null)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

            if (solid != null)
            {
                var model = MakeModelVisual3D(solid);
                models.Add(model);
                window.view1.Children.Add(model);
            }

            foreach (var path in paths)
            {
                var contour = path.Select(point => new Point3D(point[0], point[1], point[2])).ToList();

                //Now create a line collection by doubling up the points
                var lineCollection = new List<Point3D>();
                foreach (var t in contour)
                {
                    lineCollection.Add(t);
                    lineCollection.Add(t);
                }
                lineCollection.RemoveAt(0);

                if (closePaths)
                {
                    lineCollection.Add(lineCollection.First());
                }

                var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection) };
                window.view1.Children.Add(lines);
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }

        /// <summary>
        /// Shows the vertex paths with solid.
        /// </summary>
        /// <param name="segments">The segments.</param>
        /// <param name="solids">The solids.</param>
        public static void ShowVertexPathsWithSolid(IEnumerable<double[]> segments, IEnumerable<TessellatedSolid> solids)
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
                var lines = new LinesVisual3D { Points = new Point3DCollection(lineCollection) };
                window.view1.Children.Add(lines);
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }
        /// <summary>
        /// Shows the vertex paths with solid.
        /// </summary>
        /// <param name="vertexPaths">The vertex paths.</param>
        /// <param name="solids">The solids.</param>
        public static void ShowVertexPathsWithSolid(IEnumerable<List<List<double[]>>> vertexPaths, IEnumerable<TessellatedSolid> solids)
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

                    //Now create a line collection by doubling up the points
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

        /// <summary>
        /// Shows the vertex paths with solid.
        /// </summary>
        /// <param name="vertexPaths">The vertex paths.</param>
        /// <param name="solids">The solids.</param>
        public static void ShowVertexPathsWithSolid(IEnumerable<List<List<Vertex>>> vertexPaths, IEnumerable<TessellatedSolid> solids)
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

                    //Now create a line collection by doubling up the points
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
        /// Shows the vertex paths.
        /// </summary>
        /// <param name="vertexPaths">The vertex paths.</param>
        public static void ShowVertexPaths(IEnumerable<List<List<double[]>>> vertexPaths)
        {
            var window = new Window3DPlot();

            foreach (var crossSection in vertexPaths)
            {
                foreach (var path in crossSection)
                {
                    var contour = path.Select(point => new Point3D(point[0], point[1], point[2])).ToList();

                    //Now create a line collection by doubling up the points
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
        /// Shows the vertex paths.
        /// </summary>
        /// <param name="segments">The segments.</param>
        public static void ShowVertexPaths(IEnumerable<double[]> segments)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();

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

        /// <summary>
        /// Shows vertex paths. Assumes paths are closed.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="ts">The ts.</param>
        public static void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IEnumerable<Color> colors, TessellatedSolid ts)
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
            var colorEnumerator = colors.GetEnumerator();
            foreach (var point in vertices)
            {
                colorEnumerator.MoveNext();
                var color = colorEnumerator.Current;
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
        /// Shows vertex paths. Assumes paths are closed.
        /// </summary>
        /// <param name="vertexPaths">The vertex paths.</param>
        public static void ShowVertexPaths(IEnumerable<List<List<Vertex>>> vertexPaths)
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
        /// Shows the specified tessellated solid in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        /// <param name="ShowWithSolid">if set to <c>true</c> [show with solid].</param>
        public static void ShowWire(TessellatedSolid tessellatedSolid, bool ShowWithSolid = true)
        {
            if (ShowWithSolid)
                ShowVertexPathsWithSolid(tessellatedSolid.Edges.Select(e => new[]{ e.From.X, e.From.Y, e.From.Z,
                e.To.X, e.To.Y, e.To.Z }).ToList(), new[] { tessellatedSolid });
            else
                ShowVertexPaths(tessellatedSolid.Edges.Select(e => new[]{ e.From.X, e.From.Y, e.From.Z,
                    e.To.X, e.To.Y, e.To.Z }).ToList());
        }
        /// <summary>
        /// Shows the specified tessellated solid in a Helix toolkit window.
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
        /// Shows the and hang.
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
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds">The seconds.</param>
        public static void Show(IEnumerable<TessellatedSolid> tessellatedSolids, int seconds = 0)
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



        public static void ShowAndHang(params Solid[] solids)
        {
            ShowAndHang(solids.ToList());
        }


        public static void ShowAndHang(VoxelizedSolid solid)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();
            Visual3D model = MakeModelVisual3D(solid);
            models.Add(model);
            window.view1.Children.Add(model);
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }


        public static void ShowAndHang(IEnumerable<Solid> solids)
        {
            var window = new Window3DPlot();
            var models = new List<Visual3D>();
            foreach (var s in solids)
            {
                if (s is CrossSectionSolid)
                {
                    var contours = MakeLinesVisual3D(((CrossSectionSolid)s).Layer2D.Values, ((CrossSectionSolid)s).StepDistances.Values, true);
                    models.AddRange(contours);
                    foreach (var contour in contours)
                        window.view1.Children.Add(contour);
                }
                else
                {
                    Visual3D model = null;
                    if (s is TessellatedSolid)
                        model = MakeModelVisual3D((TessellatedSolid)s);
                    else if (s is VoxelizedSolid)
                        model = MakeModelVisual3D((VoxelizedSolid)s);
                    else if (s is ImplicitSolid)
                    {
                        model = MakeModelVisual3D(((ImplicitSolid)s).ConvertToTessellatedSolid());
                    }
                    models.Add(model);
                    window.view1.Children.Add(model);
                }
            }
            window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }


        public static void ShowAndHang(IEnumerable<TessellatedSolid> tessellatedSolids)
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
        /// Shows the and hang transparents and solids.
        /// </summary>
        /// <param name="transparents">The transparents.</param>
        /// <param name="solids">The solids.</param>
        public static void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparents, IEnumerable<TessellatedSolid> solids)
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
                 f => f.Vertices.Select(v => new Point3D(v.Coordinates[0], v.Coordinates[1], v.Coordinates[2])));
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
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds">The seconds.</param>
        public static void ShowSequentially(IEnumerable<TessellatedSolid> tessellatedSolids, int seconds = 1)
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
                //window.Arrange(new Rect(new System.Windows.Vector2(0, 0), size));
            }
            window.Close();
        }


        /// <summary>
        /// Shows the with convex hull.
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void ShowWithConvexHull(TessellatedSolid ts)
        {
            var window = new Window3DPlot();
            window.view1.Children.Add(MakeModelVisual3D(ts));
            var positions =
         ts.ConvexHull.Faces.SelectMany(
             f => f.Vertices.Select(v => new Point3D(v.Coordinates[0], v.Coordinates[1], v.Coordinates[2])));
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


        private static Visual3D MakeModelVisual3D(VoxelizedSolid vs)
        {
            var positions = new Point3DCollection();
            var normals = new Vector3DCollection();
            var s = vs.VoxelSideLength;

            for (var i = 0; i < vs.numVoxelsX; i++)
            {
                for (var j = 0; j < vs.numVoxelsY; j++)
                {
                    for (var k = 0; k < vs.numVoxelsZ; k++)
                    {
                        if (!vs[i, j, k]) continue;

                        var neighbors = vs.GetNeighbors(i, j, k).ToList();
                        if (neighbors.All(n => n != null)) continue;

                        var x = i * s + vs.Offset[0];
                        var y = j * s + vs.Offset[1];
                        var z = k * s + vs.Offset[2];
                        for (var m = 0; m < 12; m++)
                        {
                            if (neighbors[m / 2] != null) continue;
                            for (var n = 0; n < 3; n++)
                            {
                                positions.Add(new Point3D(x + coordOffsets[m][n][0] * s, y + coordOffsets[m][n][1] * s,
                                    z + coordOffsets[m][n][2] * s));
                                normals.Add(new Vector3D(normalsTemplate[m][0], normalsTemplate[m][1],
                                    normalsTemplate[m][2]));
                            }
                        }
                    }
                }
            }

            return new ModelVisual3D
            {
                Content =
                         new GeometryModel3D
                         {
                             Geometry = new MeshGeometry3D
                             {
                                 Positions = positions,
                                 Normals = normals
                             },
                             Material = MaterialHelper.CreateMaterial(
                                 new System.Windows.Media.Color
                                 {
                                     A = 255, //vs.SolidColor.A,
                                     B = vs.SolidColor.B,
                                     G = vs.SolidColor.G,
                                     R = vs.SolidColor.R
                                 })
                         }
            };
        }


        private static IEnumerable<LinesVisual3D> MakeLinesVisual3D(IEnumerable<IEnumerable<double[]>> paths, bool closePaths = true)
        {
            var lineVisuals = new List<LinesVisual3D>();
            foreach (var path in paths)
            {
                var contour = path.Select(point => new Point3D(point[0], point[1], point[2])).ToList();
                //Now create a line collection by doubling up the points
                var lineCollection = new List<Point3D>();
                foreach (var t in contour)
                {
                    lineCollection.Add(t);
                    lineCollection.Add(t);
                }
                lineCollection.RemoveAt(0);
                if (closePaths)
                    lineCollection.Add(lineCollection.First());
                lineVisuals.Add(new LinesVisual3D { Points = new Point3DCollection(lineCollection) });
            }
            return lineVisuals;
        }
        private static IEnumerable<LinesVisual3D> MakeLinesVisual3D(IEnumerable<IEnumerable<Vertex>> paths, bool closePaths = true)
        {
            var lineVisuals = new List<LinesVisual3D>();
            foreach (var path in paths)
            {
                var contour = path.Select(point => new Point3D(point.X, point.Y, point.Z)).ToList();
                //Now create a line collection by doubling up the points
                var lineCollection = new List<Point3D>();
                foreach (var t in contour)
                {
                    lineCollection.Add(t);
                    lineCollection.Add(t);
                }
                lineCollection.RemoveAt(0);
                if (closePaths)
                    lineCollection.Add(lineCollection.First());
                lineVisuals.Add(new LinesVisual3D { Points = new Point3DCollection(lineCollection) });
            }
            return lineVisuals;
        }


        private static IEnumerable<LinesVisual3D> MakeLinesVisual3D(IEnumerable<IEnumerable<PolygonLight>> xYPaths,
            IEnumerable<double> zValues, bool closePaths = true)
        {
            var lineVisuals = new List<LinesVisual3D>();
            var zEnumerator = zValues.GetEnumerator();
            foreach (var layer in xYPaths)
            {
                zEnumerator.MoveNext();
                var z = zEnumerator.Current;
                foreach (var polygon in layer)
                {
                    var contour = polygon.Path.Select(point => new Point3D(point.X, point.Y, z)).ToList();
                    //Now create a line collection by doubling up the points
                    var lineCollection = new List<Point3D>();
                    foreach (var t in contour)
                    {
                        lineCollection.Add(t);
                        lineCollection.Add(t);
                    }
                    lineCollection.RemoveAt(0);
                    if (closePaths)
                        lineCollection.Add(lineCollection.First());
                    lineVisuals.Add(new LinesVisual3D { Points = new Point3DCollection(lineCollection) });
                }
            }
            return lineVisuals;
        }

        /// <summary>
        /// Makes the model visual3 d.
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
                        f => f.Vertices.Select(v => new Point3D(v.Coordinates[0], v.Coordinates[1], v.Coordinates[2])));
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
        /// <summary>
        /// Colors the palette.
        /// </summary>
        /// <returns>System.String[].</returns>
        public static string[] ColorPalette()
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

        static readonly float[][] normalsTemplate =
        {
            new float[] {-1, 0, 0}, new float[] {-1, 0, 0},
            new float[] {1, 0, 0}, new float[] {1, 0, 0},
            new float[] {0, -1, 0}, new float[] {0, -1, 0},
            new float[] {0, 1, 0}, new float[] {0, 1, 0},
            new float[] {0, 0, -1}, new float[] {0, 0, -1},
            new float[] {0, 0, 1}, new float[] {0, 0, 1}
        };

        static readonly float[][][] coordOffsets =
        {
            new[]{ new float[] {0, 0, 0}, new float[] { 0, 0, 1}, new float[] {0, 1, 0}},
            new[]{ new float[] {0, 1, 0}, new float[] {0, 0, 1}, new float[] {0, 1, 1}}, //x-neg
            new[]{ new float[] {1, 0, 0}, new float[] {1, 1, 0}, new float[] {1, 0, 1}},
            new[]{ new float[] {1, 1, 0}, new float[] {1, 1, 1}, new float[] {1, 0, 1}}, //x-pos
            new[]{ new float[] {0, 0, 0}, new float[] { 1, 0, 0}, new float[] {0, 0, 1}},
            new[]{ new float[] {1, 0, 0}, new float[] {1, 0, 1}, new float[] {0, 0, 1}}, //y-neg
            new[]{ new float[] {0, 1, 0}, new float[] {0, 1, 1}, new float[] {1, 1, 0}},
            new[]{ new float[] {1, 1, 0}, new float[] {0, 1, 1}, new float[] {1, 1, 1}}, //y-pos
            new[]{ new float[] {0, 0, 0}, new float[] {0, 1, 0}, new float[] {1, 0, 0}},
            new[]{new float[] {1, 0, 0}, new float[] {0, 1, 0}, new float[] {1, 1, 0}}, //z-neg
            new[]{ new float[] {0, 0, 1}, new float[] {1, 0, 1}, new float[] {0, 1, 1}},
            new[]{ new float[] {1, 0, 1}, new float[] {1, 1, 1}, new float[] {0, 1, 1}}, //z-pos
        };
    }
}