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



        public List<Solid> Solids
        {
            set
            {
                viewModel.AttachModelList(value.Select(solid => ConvertToObject3D(solid)).ToList());
            }
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
                    DiffuseColor = new SharpDX.Color4(ts.SolidColor.Rf, ts.SolidColor.Gf, ts.SolidColor.Bf, ts.SolidColor.Af)
                },
                Geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
                {
                    Positions = new Vector3Collection(ts.Vertices.Select(v =>
                        new Vector3((float)v.X, (float)v.Y, (float)v.Z))),
                    Indices = new IntCollection(ts.Faces.SelectMany(f => f.Vertices.Select(v => v.IndexInList))),
                    Normals = new Vector3Collection(ts.Faces.Select(f =>
                        new Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])))
                }
            };
            return result;
        }

        private MeshGeometryModel3D ConvertVoxelizedSolidtoObject3D(VoxelizedSolid vs)
        {
            var boxFaceIndices = new[]
            {
                0, 1, 2,  2, 1, 4,  1, 6, 4,  4, 6, 7,  2, 4, 5,  4, 7, 5,  0, 2, 3, 3, 2, 5,
                5, 7, 3, 7, 6, 3, 3, 1, 0, 1, 3, 6
            };
            var positions = new Vector3Collection();
            var indices = new IntCollection();
            var normals = new Vector3Collection();
            var boxVoxels = vs.GetVoxelsAsAABBDoubles(VoxelRoleTypes.Partial, 0);
            foreach (var boxVoxel in boxVoxels)
            {
                var i = positions.Count;
                var x = (float)boxVoxel[0];
                var y = (float)boxVoxel[1];
                var z = (float)boxVoxel[2];
                var s = (float)boxVoxel[3];
                positions.Add(new Vector3(x, y, z));//0
                positions.Add(new Vector3(x + s, y, z));//1
                positions.Add(new Vector3(x, y + s, z));//2
                positions.Add(new Vector3(x, y, z + s));//3
                positions.Add(new Vector3(x + s, y + s, z));//4
                positions.Add(new Vector3(x, y + s, z + s));//5
                positions.Add(new Vector3(x + s, y, z + s));//6
                positions.Add(new Vector3(x + s, y + s, z + s));//7
                foreach (var boxFaceIndex in boxFaceIndices)
                    indices.Add(i + boxFaceIndex);
                normals.Add(new Vector3(0f, 0f, -1f));
                normals.Add(new Vector3(0f, 0f, -1f));
                normals.Add(new Vector3(1f, 0f, 0f));
                normals.Add(new Vector3(1f, 0f, 0f));
                normals.Add(new Vector3(0f, 1f, 0f));
                normals.Add(new Vector3(0f, 1f, 0f));
                normals.Add(new Vector3(-1f, 0f, 0f));
                normals.Add(new Vector3(-1f, 0f, 0f));
                normals.Add(new Vector3(0f, 0f, 1f));
                normals.Add(new Vector3(0f, 0f, 1f));
                normals.Add(new Vector3(0f, -1f, 0f));
                normals.Add(new Vector3(0f, -1f, 0f));
            }

            return new MeshGeometryModel3D
            {
                Material = new PhongMaterial()
                {
                    DiffuseColor = new SharpDX.Color4(vs.SolidColor.Rf, vs.SolidColor.Gf, vs.SolidColor.Bf, (float)0.75 * vs.SolidColor.Af)
                },
                Geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
                {
                    Positions = positions,
                    Indices = indices,
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