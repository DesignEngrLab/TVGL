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
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.SharpDX.Core;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using OxyPlot;
using Vector2 = TVGL.Numerics.Vector2;
using Polygon = TVGL.TwoDimensional.Polygon;
using Vector3 = TVGL.Numerics.Vector3;
using System;
using TVGL.Voxelization;
using HelixToolkit.SharpDX.Core.Model.Scene;

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

        #endregion


        #region Show and Hang Solids
        public static void ShowAndHang(TessellatedSolid ts, string heading = "", string title = "",
            string subtitle = "")
        {
            ShowAndHang(new[] { ts });
        }
        public static void ShowAndHang(VoxelizedSolid vs, string heading = "", string title = "",
            string subtitle = "")
        {
            ShowAndHang(new[] { vs });
        }

        public static void ShowAndHang(VoxelizedSolid correctVoxels, IEnumerable<Polygon> shallowTree)
        {
            throw new NotImplementedException();
        }

        public static void ShowAndHang(CrossSectionSolid css)
        {
            ShowVertexPaths(css.GetCrossSectionsAs3DLoops());
        }

        public static void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "",
            string subtitle = "")
        {
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(ConvertSolidsToModel3D(solids));
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        #endregion



        #region ShowPaths with or without Solid(s)
        public static void ShowVertexPaths(IEnumerable<Vector3> vertices, Solid solid = null, double lineThickness = 0,
            Color color = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, lineThickness,
                color == null ? null : new List<Color> { color }, closePaths); ;
        }
        public static void ShowVertexPaths(IEnumerable<IEnumerable<Vector3>> vertices, IEnumerable<bool> closePaths, Solid solid = null, double lineThickness = 0,
   Color color = null)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, closePaths, lineThickness,
                color == null ? null : new List<Color> { color }); ;
        }
        public static void ShowVertexPaths(IEnumerable<Vertex> vertices, Solid solid = null, double lineThickness = 0,
            Color color = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, lineThickness,
                color == null ? null : new List<Color> { color }, closePaths); ;
        }
        public static void ShowVertexPaths(IEnumerable<IEnumerable<Vector3>> vertices, Solid solid = null, double lineThickness = 0,
            IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, lineThickness, colors, closePaths);
        }
        public static void ShowVertexPaths(IEnumerable<IEnumerable<Vertex>> vertices, Solid solid = null, double lineThickness = 0,
            IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, lineThickness, colors, closePaths);
        }

        public static void ShowVertexPaths(IEnumerable<IEnumerable<IEnumerable<Vector3>>> vertices, Solid solid = null,
    double lineThickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices, new List<Solid> { solid }, lineThickness, colors, closePaths);
        }
        public static void ShowVertexPathsWithMultipleSolid(IEnumerable<IEnumerable<IEnumerable<Vector3>>> vertices,
            IEnumerable<Solid> solids, double lineThickness = 1, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices.SelectMany(v => v), solids, lineThickness, colors, closePaths);
        }
        public static void ShowVertexPathsWithSolids(IEnumerable<Vertex> vertices, IEnumerable<Solid> solids,
            double thickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices.Select(v => v.Coordinates), solids, thickness, colors, closePaths);
        }
        public static void ShowVertexPathsWithSolids(IEnumerable<IEnumerable<Vertex>> vertices, IEnumerable<Solid> solids,
            double thickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices.Select(v => v.Select(vv => vv.Coordinates)), solids, thickness, colors, closePaths);
        }
        public static void ShowVertexPathsWithSolids(IEnumerable<Vector3> vertices, IEnumerable<Solid> solids,
            double thickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(new[] { vertices }, solids, thickness, colors, closePaths);
        }
        public static void ShowVertexPathsWithSolids(IEnumerable<Vector3> vertices, IEnumerable<Solid> solids, 
            IEnumerable<bool> closePaths, double thickness = 0, IEnumerable<Color> colors = null)
        {
            ShowVertexPathsWithSolids(new[] { vertices }, solids, closePaths, thickness, colors);
        }
        private static void ShowVertexPathsWithSolids(IEnumerable<IEnumerable<Vector3>> lines, IEnumerable<Solid> solids,
            IEnumerable<bool> closePaths, double thickness = 0, IEnumerable<Color> colors = null)
        {
            var lineVisuals = GetVertexPaths(lines, thickness, colors, closePaths);
            var vm = new Window3DPlotViewModel();
            vm.Add(lineVisuals);
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }
        private static void ShowVertexPathsWithSolids(IEnumerable<IEnumerable<Vector3>> lines, IEnumerable<Solid> solids,
            double thickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            var lineVisuals = GetVertexPaths(lines, thickness, colors, closePaths);
            var vm = new Window3DPlotViewModel();
            vm.Add(lineVisuals);
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public static void ShowVertexPathsWithSolids(IEnumerable<IEnumerable<IEnumerable<Vector3>>> vertices,
            IEnumerable<Solid> solids, double lineThickness = 0, IEnumerable<Color> colors = null, bool closePaths = false)
        {
            ShowVertexPathsWithSolids(vertices.SelectMany(v => v), solids, lineThickness, colors, closePaths);
        }


        public static IEnumerable<GeometryModel3D> GetVertexPaths(IEnumerable<IEnumerable<Vector3>> paths, double thickness = 0,
            IEnumerable<Color> colors = null, bool closePaths = false)
        {
            return GetVertexPaths(paths, thickness, colors, paths.Select(x => closePaths));
        }
        public static IEnumerable<GeometryModel3D> GetVertexPaths(IEnumerable<IEnumerable<Vector3>> paths, double thickness = 0,
            IEnumerable<Color> colors = null, IEnumerable<bool> closePaths = null)
        {
            //set the default color to be the first color in the list. If none was provided, use black.
            colors = colors ?? Color.GetRandomColors();
            var colorEnumerator = colors.GetEnumerator();

            var isClosed = closePaths ?? paths.Select(x => true);
            var closedEnumerator = isClosed.GetEnumerator();

            var linesVisual = new List<LineGeometryModel3D>();
            foreach (var path in paths)
            {
                if (path == null || !path.Any()) continue;
                var contour = path.Select(point => new SharpDX.Vector3((float)point[0], (float)point[1], (float)point[2]));

                //////No create a line collection by doubling up the points
                //var lineCollection = new List<SharpDX.Vector3>();
                //foreach (var t in contour)
                //{
                //    lineCollection.Add(t);
                //    lineCollection.Add(t);
                //}
                //lineCollection.RemoveAt(0);
                //if (closePaths) lineCollection.Add(lineCollection.First());
                colorEnumerator.MoveNext();
                var tvglColor = colorEnumerator.Current;
                var color = new System.Windows.Media.Color { R = tvglColor.R, G = tvglColor.G, B = tvglColor.B, A = tvglColor.A };
                var positions = new Vector3Collection(contour);
                var lineIndices = new IntCollection();
                for (var i = 1; i < positions.Count; i++)
                {
                    lineIndices.Add(i - 1);
                    lineIndices.Add(i);
                }
                closedEnumerator.MoveNext();
                if (closedEnumerator.Current)
                {
                    lineIndices.Add(positions.Count - 1);
                    lineIndices.Add(0);
                }

                yield return new LineGeometryModel3D
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
                    Color = color
                };
            }
        }
        #endregion


        public static IEnumerable<GeometryModel3D> ConvertSolidsToModel3D(IEnumerable<Solid> solids)
        {
            foreach (var ts in solids.Where(ts => ts is TessellatedSolid))
                foreach (var m3d in ConvertTessellatedSolidToMGM3D((TessellatedSolid)ts))
                    yield return m3d;

            foreach (var vs in solids.Where(vs => vs is VoxelizedSolid))
                foreach (var m3d in ConvertVoxelsToPointModel3D((VoxelizedSolid)vs))
                    yield return m3d;
            foreach (var css in solids.Where(cs => cs is CrossSectionSolid))
                foreach (var m3d in GetVertexPaths(((CrossSectionSolid)css).GetCrossSectionsAs3DLoops().SelectMany(v => v),1,null,true))
                    yield return m3d;
        }

        private static IEnumerable<GeometryModel3D> ConvertTessellatedSolidToMGM3D(TessellatedSolid ts)
        {
            var defaultColor = new SharpDX.Color4(ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf, ts.SolidColor.Af);
            var positions =
                ts.Faces.SelectMany(
                    f => f.Vertices.Select(v =>
                        new SharpDX.Vector3((float)v.Coordinates[0], (float)v.Coordinates[1], (float)v.Coordinates[2])));
            var normals =
                           ts.Faces.SelectMany(f =>
                               f.Vertices.Select(v =>
                                   new SharpDX.Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])));
            var indices = Enumerable.Range(0, ts.NumberOfFaces * 3);
            if (ts.HasUniformColor)
            {
                yield return new MeshGeometryModel3D
                {
                    Geometry = new MeshGeometry3D
                    {
                        Positions = new Vector3Collection(positions),
                        TriangleIndices = new IntCollection(indices),
                        Normals = new Vector3Collection(normals),
                    },
                    Material = new PhongMaterial() { DiffuseColor = defaultColor }
                };
            }
            else
            {
                var vertexList = positions.ToList();  //to avoid re-enumeration
                var normalList = normals.ToList();    //these lists are defined
                var colorToFaceDict = new Dictionary<SharpDX.Color4, List<int>>();
                for (int i = 0; i < ts.NumberOfFaces; i++)
                {
                    var f = ts.Faces[i];
                    var faceColor = (f.Color == null) ? defaultColor
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
            var color = new System.Windows.Media.Color { R = vs.SolidColor.R, G = vs.SolidColor.G, B = vs.SolidColor.B, A = vs.SolidColor.A };
            var s = (float)vs.VoxelSideLength;
            var xOffset = (float)vs.Offset[0];
            var yOffset = (float)vs.Offset[1];
            var zOffset = (float)vs.Offset[2];

            yield return new PointGeometryModel3D
            {
                Geometry = new PointGeometry3D
                {
                    Positions = new Vector3Collection(vs.Select(vox => new SharpDX.Vector3(vox[0] * s + xOffset,
                    vox[1] * s + yOffset, vox[2] * s + zOffset)))
                },
                Size = new System.Windows.Size(s, s),
                FixedSize = true,
                Color = color
            };
        }

    }
}