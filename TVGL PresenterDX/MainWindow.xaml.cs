// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Linq;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using StarMathLib;
using TVGL;
using TVGL.Voxelization;
using Color = TVGL.Color;

namespace TVGLPresenterDX
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using HelixToolkit.Wpf.SharpDX;
    using TVGL;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            viewModel.modelView = this.view1;
            this.DataContext = viewModel;

        }



        public void AddSolids(IList<Solid> solids)
        {
            viewModel.AttachModelList(solids.Select(ConvertToObject3D).ToList());
        }

        private MeshGeometryModel3D ConvertToObject3D(Solid solid)
        {
            if (solid is TessellatedSolid) return ConvertTessellatedSolidtoObject3D((TessellatedSolid)solid);
            if (solid is VoxelizedSolid) return ConvertVoxelizedSolidtoObject3D((VoxelizedSolid)solid);
            throw new ArgumentException("Solid must be TessellatedSolid or VoxelizedSolid");
        }
        private MeshGeometryModel3D ConvertTessellatedSolidtoObject3D(TessellatedSolid ts)
        {
            var result = new MeshGeometryModel3D
            {
                Material = new PhongMaterial()
                {
                    DiffuseColor = new SharpDX.Color4(ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf,
                    ts.SolidColor.Af)
                },
                Geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
                {
                    Positions = new Vector3Collection(ts.Faces.SelectMany(f => f.Vertices.Select(v =>
                          new Vector3((float)v.X, (float)v.Y, (float)v.Z)))),
                    Indices = new IntCollection(Enumerable.Range(0, 3 * ts.NumberOfFaces)),
                    Normals = new Vector3Collection(ts.Faces.SelectMany(f => f.Vertices.Select(v =>
                        new Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])))),

                }
            };
            return result;
        }


        private MeshGeometryModel3D ConvertVoxelizedSolidtoObject3D(VoxelizedSolid vs)
        {
            if (false)
            {
                var ts = vs.ConvertToTessellatedSolidMarchingCubes(20);
                ts.SolidColor = new Color(KnownColors.MediumSeaGreen)
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
                                positions.Add(new Vector3(x + (coordOffsets[m][n][0] * s), y + coordOffsets[m][n][1] * s,
                                    z + coordOffsets[m][n][2] * s));
                                normals.Add(new Vector3(normalsTemplate[m][0], normalsTemplate[m][1],
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
                Geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
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