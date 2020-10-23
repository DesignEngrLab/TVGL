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
using TVGLPresenterDX;
using HelixToolkit.SharpDX.Core;
using TVGL;

namespace TVGLPresenterDX
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static class Presenter
    {
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
            var window = new MainWindow();
            var models = new List<Element3D>();

            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.group.Children.Add(model);
            }
            // window.view1.FitView(window.view1.Camera.LookDirection, window.view1.Camera.UpDirection);
            window.ShowDialog();
        }


        /// <summary>
        /// Makes the model visual3 d.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>Visual3D.</returns>
        private static MeshGeometryModel3D MakeModelVisual3D(TessellatedSolid ts)
        {
            var defaultMaterial = new PhongMaterial()
            {
                DiffuseColor = new SharpDX.Color4(
                    ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf, ts.SolidColor.Af)
            };
            //if (ts.HasUniformColor)
            {
                var positions =
                    ts.Faces.SelectMany(
                        f => f.Vertices.Select(v =>
                            new Vector3((float)v.Coordinates[0], (float)v.Coordinates[1], (float)v.Coordinates[2])));
                var normals =
                    ts.Faces.SelectMany(f =>
                        f.Vertices.Select(v =>
                            new Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])));
                return new MeshGeometryModel3D
                {
                    Geometry = new MeshGeometry3D
                    {
                        Positions = new Vector3Collection(positions),
                        // TriangleIndices = new Int32Collection(triIndices),
                        Normals = new Vector3Collection(normals)
                    },
                    Material = defaultMaterial
                };
            }
        }

        #endregion

        #region 3D Voxelization Plots

        #endregion

    }
}