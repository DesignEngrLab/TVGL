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

        #region 3D Plots via Helix.Toolkit

        /// <summary>
        /// Shows the and hang.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
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

        public static void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "",
            string subtitle = "")
        {
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(ConvertSolidsToModel3D(solids));
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        #endregion

        #region 3D Voxelization Plots

        #endregion

        #region Method to Transition over
        internal static void ShowVertexPathsWithSolid(IEnumerable<Vector3[]> paths, IEnumerable<TessellatedSolid> tessellatedSolids)
        {
            throw new NotImplementedException();
        }

        public static void ShowAndHang(VoxelizedSolid correctVoxels, IEnumerable<Polygon> shallowTree)
        {
            throw new NotImplementedException();
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
                var indicesList = indices.ToList();   // they are not needed in the above scenario
                var colorToFaceDict = new Dictionary<SharpDX.Color4, List<int>>();
                for (int i = 0; i < ts.NumberOfFaces; i++)
                {
                    var f = ts.Faces[i];
                    var faceColor = (f.Color == null) ? defaultColor
                        : new SharpDX.Color4(f.Color.Rf, f.Color.Gf, f.Color.Bf, f.Color.Af);
                    if (colorToFaceDict.ContainsKey(faceColor))
                        colorToFaceDict[faceColor].Add(i);
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
                            TriangleIndices = new IntCollection(faceIndices
                        .SelectMany(f => new[] { indicesList[3 * f], indicesList[3 * f + 1], indicesList[3 * f + 2] })),
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
                Size = new System.Windows.Size(10*s, 10*s),
                FixedSize = true,
                Color = color
            };
        }
    }
}