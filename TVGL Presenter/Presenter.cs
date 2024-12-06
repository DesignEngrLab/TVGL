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
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using MediaColor = System.Windows.Media.Color;
using TVGLColor = TVGL.Color;

namespace TVGL
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static partial class Presenter
    {
        #region 2D Plots via OxyPlot

        #region Plotting 2D coordinates both scatter and polygons
        /// <summary>
        /// Show the matrix of data as a 2D plot (heatmap)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="title"></param>
        public static void ShowAndHang(double[,] data, string title = "")
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
        public static void ShowAndHang(IEnumerable<Vector2> points, string title = "", Plot2DType plot2DType = Plot2DType.Line,
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
        public static void ShowAndHang(IEnumerable<IEnumerable<Vector2>> pointsList, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var window = new Window2DPlot(pointsList, title, plot2DType, closeShape, marker);
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
        public static void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector2>>> pointsLists, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {

            var window = new Window2DPlot(pointsLists, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<Polygon> polygons, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            var points = polygons.SelectMany(polygon => polygon.AllPolygons.Select(p => p.Path)).ToList();
            var window = new Window2DPlot(points, title, plot2DType, closeShape, marker);
            window.ShowDialog();
        }

        public static void ShowAndHang(Polygon polygon, string title = "", Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(new[] { polygon }, title, plot2DType, closeShape, marker);
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
        /// Shows the provided objects and "hangs" (halts code until user closes presenter window).
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
        public static void ShowAndHang(IEnumerable<IEnumerable<Vertex>> vertices, Vector3 direction, string title = "",
            Plot2DType plot2DType = Plot2DType.Line,
            bool closeShape = true, MarkerType marker = MarkerType.Circle)
        {
            ShowAndHang(vertices.Select(listsOfVerts => listsOfVerts.ProjectTo2DCoordinates(direction, out _)),
                title, plot2DType, closeShape, marker);
        }

        #endregion

        public static void ShowHeatmap(double[,] values, bool normalizeValues = false)
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


        #region Show and Hang Solids
        public static void ShowAndHang(Solid solid, string heading = "", string title = "",
            string subtitle = "")
        {
            if (solid is CrossSectionSolid css)
                ShowAndHang(css.GetCrossSectionsAs3DLoops());
            else
                ShowAndHang(new[] { solid }, heading, title, subtitle);
        }

        public static void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
        {
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(ConvertSolidsToModel3D(solids));
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        #endregion


        /// <summary>
        /// Shows the gauss sphere with intensity.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="solid">The ts.</param>
        public static void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid)
        {

            var vm = new Window3DPlotViewModel();
            var window = new Window3DPlot(vm);
            var pt0 = new System.Windows.Media.Media3D.Point3D(solid.Center[0], solid.Center[1], solid.Center[2]);
            var x = solid.XMax - solid.XMin;
            var y = solid.YMax - solid.YMin;
            var z = solid.ZMax - solid.ZMin;
            var radius = System.Math.Max(System.Math.Max(x, y), z) / 2;

            //Add the solid to the visual
            var model = ConvertSolidsToModel3D(new[] { solid });
            vm.Add(model);

            //Add a transparent unit sphere to the visual...doesn't seem to be one in SharpDX
            //var sphere = new HelixToolkit.Wpf.SharpDX.
            //sphere.Radius = radius;
            //sphere.Center = pt0;
            //sphere.Material = MaterialHelper.CreateMaterial(new MediaColor { A = 15, R = 200, G = 200, B = 200 });
            //window.view1.Children.Add(sphere);

            var i = 0;
            foreach (var point in vertices)
            {
                var positions = new Vector3Collection(new[] {
                    new SharpDX.Vector3((float)solid.Center.X, (float)solid.Center.Y, (float)solid.Center.Z),
                    new SharpDX.Vector3((float)(pt0.X + point.X * radius),
                                        (float)(pt0.Y + point.Y * radius), (float)(pt0.Z + point.Z * radius))
                });
                var lineIndices = new IntCollection(new[] { 0, 1 });

                var color = colors[i];
                var lines = new LineGeometryModel3D
                {
                    Geometry = new LineGeometry3D
                    {
                        Positions = positions,
                        Indices = lineIndices
                    },
                    IsRendering = true,
                    Smoothness = 2,
                    Thickness = 5,
                    Color = new MediaColor { A = 255, R = color.R, G = color.G, B = color.B }
                };
            }
            window.ShowDialog();
        }


        #region ShowPaths with or without Solid(s)
        public static void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null)
        {
            if (radius == 0) radius = 1;
            color ??= new Color(KnownColors.Red);
            var pointVisuals = GetPointModels(points, radius, color);
            var vm = new Window3DPlotViewModel();
            vm.Add(pointVisuals);
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public static void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null)
        {
            if (radius == 0) radius = 1;
            //set the default color to be the first color in the list. If none was provided, use black.
            colors = colors ?? Color.GetRandomColors();
            var colorEnumerator = colors.GetEnumerator();

            var vm = new Window3DPlotViewModel();
            foreach (var points in pointSets)
            {
                colorEnumerator.MoveNext();
                var color = colorEnumerator.Current;
                var pointVisuals = GetPointModels(points, radius, color);
                vm.Add(pointVisuals);
            }
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public static IEnumerable<GeometryModel3D> GetPointModels(IEnumerable<Vector3> points, double radius = 0, Color tvglColor = null)
        {
            var color = new MediaColor { R = tvglColor.R, G = tvglColor.G, B = tvglColor.B, A = tvglColor.A };
            yield return new PointGeometryModel3D
            {
                Geometry = new PointGeometry3D
                {
                    Positions = new Vector3Collection(points.Select(p => new SharpDX.Vector3((float)p.X, (float)p.Y, (float)p.Z)))
                },
                Size = new System.Windows.Size(radius, radius),
                FixedSize = true,
                Color = color
            };
        }



        #endregion


        #region ShowPaths with or without Solid(s)
        public static void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        {
            var vm = new Window3DPlotViewModel();
            vm.Add(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors));
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        {
            var vm = new Window3DPlotViewModel();
            vm.Add(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors));
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public static void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids)
            => ShowAndHang([path], [closePaths], [lineThickness == -1 ? 1 : lineThickness], [color == null ? new Color(KnownColors.Black) : color], solids);


        private static List<LineGeometryModel3D> ConvertPathsToLineModels(IEnumerable<IEnumerable<Vector3>> paths,
            IEnumerable<bool> closePaths, IEnumerable<double> lineThicknesses, IEnumerable<TVGLColor> colors)
        {
            var lineVisuals = new List<LineGeometryModel3D>();
            closePaths = closePaths ?? paths.Select(x => true);
            var closedEnumerator = closePaths.GetEnumerator();
            lineThicknesses = lineThicknesses ?? paths.Select(x => 1.0);
            var lineThickEnumerator = lineThicknesses.GetEnumerator();
            //set the default mediaColor to be the first mediaColor in the list. If none was provided, use black.
            colors = colors ?? Color.GetRandomColors();
            var colorEnumerator = colors.GetEnumerator();

            var linesVisual = new List<LineGeometryModel3D>();
            foreach (var path in paths)
            {
                while (!closedEnumerator.MoveNext())
                    closedEnumerator = closePaths.GetEnumerator();
                var isClosed = closedEnumerator.Current;
                while (!lineThickEnumerator.MoveNext())
                    lineThickEnumerator = lineThicknesses.GetEnumerator();
                var lineThick = lineThickEnumerator.Current;
                while (!colorEnumerator.MoveNext())
                    colorEnumerator = colors.GetEnumerator();
                var color = colorEnumerator.Current;

                if (path == null || !path.Any() || path.Any(p => p.IsNull())) continue;
                lineVisuals.Add(GetVertexPath(path, lineThick, color, isClosed));
            }
            return lineVisuals;
        }

        private static List<LineGeometryModel3D> ConvertPathsToLineModels(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths,
            IEnumerable<bool> closePaths, IEnumerable<double> lineThicknesses, IEnumerable<TVGLColor> colors)
        {
            var lineVisuals = new List<LineGeometryModel3D>();
            closePaths = closePaths ?? paths.Select(x => true);
            var closedEnumerator = closePaths.GetEnumerator();
            lineThicknesses = lineThicknesses ?? paths.Select(x => 1.0);
            var lineThickEnumerator = lineThicknesses.GetEnumerator();
            //set the default mediaColor to be the first mediaColor in the list. If none was provided, use black.
            colors = colors ?? TVGLColor.GetRandomColors();
            var colorEnumerator = colors.GetEnumerator();

            var linesVisual = new List<LineGeometryModel3D>();
            foreach (var path in paths)
            {
                while (!closedEnumerator.MoveNext())
                    closedEnumerator = closePaths.GetEnumerator();
                var isClosed = closedEnumerator.Current;
                while (!lineThickEnumerator.MoveNext())
                    lineThickEnumerator = lineThicknesses.GetEnumerator();
                var lineThick = lineThickEnumerator.Current;
                while (!colorEnumerator.MoveNext())
                    colorEnumerator = colors.GetEnumerator();
                var color = colorEnumerator.Current;

                if (path == null || !path.Any()) continue;
                foreach (var p in path)
                    lineVisuals.Add(GetVertexPath(p, lineThick, color, isClosed));
            }

            return lineVisuals;
        }

        private static LineGeometryModel3D GetVertexPath(IEnumerable<Vector3> path, double thickness, TVGLColor color, bool closePath)
        {
            var contour = path.Select(point => new SharpDX.Vector3((float)point[0], (float)point[1], (float)point[2]));

            MediaColor mediaColor = new MediaColor { R = color.R, G = color.G, B = color.B, A = color.A };
            if (color == null)
            {
                color = Color.GetRandomColors().First();
                mediaColor = new MediaColor { R = color.R, G = color.G, B = color.B, A = color.A };
            }
            else mediaColor = new MediaColor { R = color.R, G = color.G, B = color.B, A = color.A };
            var positions = new Vector3Collection(contour);
            var lineIndices = new IntCollection();
            for (var i = 1; i < positions.Count; i++)
            {
                lineIndices.Add(i - 1);
                lineIndices.Add(i);
            }
            if (closePath)
            {
                lineIndices.Add(positions.Count - 1);
                lineIndices.Add(0);
            }

            return new LineGeometryModel3D
            {
                Geometry = new LineGeometry3D
                {
                    Positions = positions,
                    Indices = lineIndices
                },
                IsRendering = true,
                Smoothness = 2,
                FillMode = thickness == 0 ? SharpDX.Direct3D11.FillMode.Wireframe : SharpDX.Direct3D11.FillMode.Solid,
                Thickness = thickness,
                Color = mediaColor
            };
        }

        #endregion



        public static void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
        {
            var geomModels = ConvertTessellatedSolidToMGM3D(faces, new Color(KnownColors.LightGray), false);
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(geomModels);
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public static IEnumerable<GeometryModel3D> ConvertSolidsToModel3D(IEnumerable<Solid> solids)
        {
            foreach (var ts in solids.Where(ts => ts is TessellatedSolid))
                foreach (var m3d in ConvertTessellatedSolidToMGM3D((TessellatedSolid)ts))
                    yield return m3d;

            foreach (var vs in solids.Where(vs => vs is VoxelizedSolid))
                foreach (var m3d in ConvertVoxelsToPointModel3D((VoxelizedSolid)vs))
                    yield return m3d;
            foreach (var css in solids.Where(cs => cs is CrossSectionSolid))
                foreach (var layer in ((CrossSectionSolid)css).GetCrossSectionsAs3DLoops().SelectMany(v => v))
                    yield return GetVertexPath(layer, 1, null, true);
        }

        private static IEnumerable<GeometryModel3D> ConvertTessellatedSolidToMGM3D(TessellatedSolid ts)
        { return ConvertTessellatedSolidToMGM3D(ts.Faces, ts.SolidColor, ts.HasUniformColor); }
        private static IEnumerable<GeometryModel3D> ConvertTessellatedSolidToMGM3D(IEnumerable<TriangleFace> faces, Color defaultColor, bool hasUniformColor)
        {
            var faceList = faces as IList<TriangleFace> ?? faces.ToList();
            var numFaces = faceList.Count;
            var defaultSharpDXColor = new SharpDX.Color4(defaultColor.Rf, defaultColor.Gf, defaultColor.Bf, defaultColor.Af);
            var positions =
                faceList.SelectMany(
                    f => f.Vertices.Select(v =>
                        new SharpDX.Vector3((float)v.Coordinates[0], (float)v.Coordinates[1], (float)v.Coordinates[2])));
            var normals =
                           faceList.SelectMany(f =>
                               f.Vertices.Select(v =>
                                   new SharpDX.Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])));
            var indices = Enumerable.Range(0, numFaces * 3);
            if (hasUniformColor)
            {
                yield return new MeshGeometryModel3D
                {
                    Geometry = new MeshGeometry3D
                    {
                        Positions = new Vector3Collection(positions),
                        TriangleIndices = new IntCollection(indices),
                        Normals = new Vector3Collection(normals),
                    },
                    Material = new PhongMaterial() { DiffuseColor = defaultSharpDXColor }
                };
            }
            else
            {
                var vertexList = positions.ToList();  //to avoid re-enumeration
                var normalList = normals.ToList();    //these lists are defined
                var colorToFaceDict = new Dictionary<SharpDX.Color4, List<int>>();
                for (int i = 0; i < numFaces; i++)
                {
                    var f = faceList[i];
                    var faceColor = (f.Color == null) ? defaultSharpDXColor
                        : new SharpDX.Color4(f.Color.Rf, f.Color.Gf, f.Color.Bf, f.Color.Af);
                    if (colorToFaceDict.TryGetValue(faceColor, out var ints))
                        ints.Add(i);
                    else
                        colorToFaceDict.Add(faceColor, new List<int> { i });
                }
                foreach (var entry in colorToFaceDict)
                {
                    var material = new PhongMaterial { DiffuseColor = entry.Key };
                    var faceIndices = entry.Value;
                    yield return new MeshGeometryModel3D
                    {
                        Geometry = new MeshGeometry3D
                        {
                            Positions = new Vector3Collection(faceIndices
                        .SelectMany(f => new[] { vertexList[3 * f], vertexList[3 * f + 1], vertexList[3 * f + 2] })),
                            TriangleIndices = new IntCollection(Enumerable.Range(0, 3 * faceIndices.Count)),
                            Normals = new Vector3Collection(faceIndices
                        .SelectMany(f => new[] { normalList[3 * f], normalList[3 * f + 1], normalList[3 * f + 2] }))
                        },
                        Material = material
                    };
                }
            }
        }

        private static IEnumerable<GeometryModel3D> ConvertVoxelsToPointModel3D(VoxelizedSolid vs)
        {
            var sw = Stopwatch.StartNew();
            var s = (float)vs.VoxelSideLength;
            var xOffset = (float)vs.Offset[0];
            var yOffset = (float)vs.Offset[1];
            var zOffset = (float)vs.Offset[2];
            var radius = s;

            yield return new PointGeometryModel3D
            {
                FigureRatio = .2,
                Geometry = new PointGeometry3D
                {
                    Positions = new Vector3Collection(vs.GetExposedVoxels().Select(vox => new SharpDX.Vector3(vox.xIndex * s + xOffset,
                    vox.yIndex * s + yOffset, vox.zIndex * s + zOffset))),

                },
                Size = new System.Windows.Size(3 * Math.Sqrt(s), 3 * Math.Sqrt(s)),
                Color = new MediaColor { R = vs.SolidColor.R, G = vs.SolidColor.G, B = vs.SolidColor.B, A = vs.SolidColor.A }
            };
            Console.WriteLine(sw.Elapsed.ToString());
        }
    }
}