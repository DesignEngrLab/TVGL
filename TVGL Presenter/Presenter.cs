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

namespace TVGLPresenter
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
        public static void ShowAndHang(TessellatedSolid ts)
        {
            ShowAndHang(new[] { ts });
        }

        public static void ShowAndHang(IList<TessellatedSolid> tessellatedSolids)
        {            
            var window = new Window3DPlot(new MainViewModel(tessellatedSolids));
            window.ShowDialog();
        }

        #endregion

        #region 3D Voxelization Plots

        #endregion

        #region Method to Transition over
        internal static void ShowVertexPathsWithSolid(IEnumerable<Vector3[]> enumerable, TessellatedSolid[] tessellatedSolids)
        {
            throw new NotImplementedException();
        }

        public static void ShowAndHang(VoxelizedSolid correctVoxels, List<Polygon> shallowTree)
        {
            throw new NotImplementedException();
        }
        #endregion





        public static void AddSolids(IList<Solid> solids, MainViewModel viewModel)
        {
        //    viewModel.AttachModelList(solids.Select(ConvertToObject3D).ToList());
        }

        private static MeshGeometryModel3D ConvertToObject3D(Solid solid)
        {
            if (solid is TessellatedSolid) return ConvertTessellatedSolidtoObject3D((TessellatedSolid)solid);
            if (solid is VoxelizedSolid) return ConvertVoxelizedSolidtoObject3D((VoxelizedSolid)solid);
            throw new ArgumentException("Solid must be TessellatedSolid or VoxelizedSolid");
        }
        private static MeshGeometryModel3D ConvertTessellatedSolidtoObject3D(TessellatedSolid ts)
        {
            var result = new MeshGeometryModel3D
            {
                Material = new PhongMaterial()
                {
                    DiffuseColor = new SharpDX.Color4(ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf,
                    ts.SolidColor.Af)
                },
                Geometry = new MeshGeometry3D
                {
                    Positions = new Vector3Collection(ts.Faces.SelectMany(f => f.Vertices.Select(v =>
                          new SharpDX.Vector3((float)v.X, (float)v.Y, (float)v.Z)))),
                    Indices = new IntCollection(Enumerable.Range(0, 3 * ts.NumberOfFaces)),
                    Normals = new Vector3Collection(ts.Faces.SelectMany(f => f.Vertices.Select(v =>
                        new SharpDX.Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])))),

                }
            };
            return result;
        }


        private static MeshGeometryModel3D ConvertVoxelizedSolidtoObject3D(VoxelizedSolid vs)
        {
            if (false)
            {
                var ts = vs.ConvertToTessellatedSolidMarchingCubes(20);
                ts.SolidColor = new TVGL.Color(KnownColors.MediumSeaGreen)
                {
                    Af = 0.80f
                };
                return ConvertTessellatedSolidtoObject3D(ts);
            }

            var normalsTemplate = new[]
            {
                new float[] {-1, 0, 0}, new float[] {-1, 0, 0},
                new float[] {1, 0, 0}, new float[] {1, 0, 0},
                new float[] {0, -1, 0}, new float[] {0, -1, 0},
                new float[] {0, 1, 0}, new float[] {0, 1, 0},
                new float[] {0, 0, -1}, new float[] {0, 0, -1},
                new float[] {0, 0, 1}, new float[] {0, 0, 1}
            };

            var coordOffsets = new[]
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
            var positions = new Vector3Collection();
            var normals = new Vector3Collection();

            var s = (float)vs.VoxelSideLength;

            for (var i = 0; i < vs.numVoxelsX; i++)
                for (var j = 0; j < vs.numVoxelsY; j++)
                    for (var k = 0; k < vs.numVoxelsZ; k++)
                    {
                        if (!vs[i, j, k]) continue;

                        if (!vs.GetNeighbors(i, j, k, out var neighbors)) continue;

                        var x = i * s + (float)vs.Offset[0];
                        var y = j * s + (float)vs.Offset[1];
                        var z = k * s + (float)vs.Offset[2];
                        for (var m = 0; m < 12; m++)
                        {
                            if (neighbors[m / 2] != null) continue;
                            for (var n = 0; n < 3; n++)
                            {
                                positions.Add(new SharpDX.Vector3(x + (coordOffsets[m][n][0] * s), y + coordOffsets[m][n][1] * s,
                                    z + coordOffsets[m][n][2] * s));
                                normals.Add(new SharpDX.Vector3(normalsTemplate[m][0], normalsTemplate[m][1],
                                    normalsTemplate[m][2]));
                            }
                        }
                    }
            return new MeshGeometryModel3D
            {
                Material = new PhongMaterial()
                {
                    DiffuseColor = new SharpDX.Color4(vs.SolidColor.Rf, vs.SolidColor.Gf, vs.SolidColor.Bf,
                    vs.SolidColor.Af)
                    //(float)0.75 * vs.SolidColor.Af)
                },
                Geometry = new MeshGeometry3D
                {
                    Positions = positions,
                    Indices = new IntCollection(Enumerable.Range(0, positions.Count)),
                    Normals = normals
                }
            };
        }


        /// <summary>
        ///     Handles the OnChecked event of the GridLines control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        //private void GridLines_OnChecked(object sender, RoutedEventArgs e)
        //{
        //    GridLines.Visible = true;
        //}

        ///// <summary>
        /////     Handles the OnUnChecked event of the GridLines control.
        ///// </summary>
        ///// <param name="sender">The source of the event.</param>
        ///// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        //private void GridLines_OnUnChecked(object sender, RoutedEventArgs e)
        //{
        //    GridLines.Visible = false;
        //}
    }
}