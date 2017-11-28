// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
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
    using System.Collections.ObjectModel;
    using System.Windows.Media.Media3D;

    using HelixToolkit.Wpf.SharpDX;
    using Microsoft.Win32;
    using System.Windows.Input;
    using System.IO;
    using System.ComponentModel;

    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected bool SetValue<T>(ref T backingField, T value, string propertyName)
        {
            if (object.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }

    public partial class MainViewModel : ObservableObject
    {
        public Element3DCollection ModelGeometry { get; private set; }
        public Transform3D ModelTransform { get; private set; }
        public Viewport3DX modelView
        {
            get;
            set;
        }


        public ICommand OpenFileCommand
        {
            get; set;
        }
        public DefaultEffectsManager EffectsManager { get; private set; }

        public DefaultRenderTechniquesManager RenderTechniquesManager { get; private set; }
        public MainViewModel()
        {
            this.ModelTransform = new TranslateTransform3D(0, 0, 0);

            this.ModelGeometry = new Element3DCollection();
            RenderTechniquesManager = new DefaultRenderTechniquesManager();
            EffectsManager = new DefaultEffectsManager(RenderTechniquesManager);
        }



        private void Present(TessellatedSolid ts, VoxelizedSolid vs)
        {
            AttachModelList
            (new[]
            {
                ConvertTessellatedSolidtoObject3D(ts, new Color(KnownColors.Aquamarine)),
                ConvertVoxelizedSolidtoObject3D(vs)
            });
        }

        private MeshGeometryModel3D ConvertTessellatedSolidtoObject3D(TessellatedSolid ts, Color color)
        {
            var result = new MeshGeometryModel3D();
            result.Material = new PhongMaterial() { DiffuseColor = new SharpDX.Color4(color.Rf, color.Gf, color.Bf, color.Af) };
            result.Geometry = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
           {
                Positions =new Vector3Collection(ts.Vertices.Select(v=>new Vector3((float)v.X, (float)v.Y, (float)v.Z))),
                Indices = new IntCollection(ts.Faces.SelectMany(f=>f.Vertices.Select(v=>v.IndexInList))),
                Normals =new Vector3Collection(ts.Faces.Select(f=>new Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])))
            };
            return result;
        }

        private MeshGeometryModel3D ConvertVoxelizedSolidtoObject3D(VoxelizedSolid vs)
        {
            vs.GetVoxels(VoxelRoleTypes.Partial, 2);
        }

        public void AttachModelList(IList<MeshGeometryModel3D> model3Ds)
        {
            this.ModelTransform = new TranslateTransform3D(0, 0, 0);
            this.ModelGeometry = new Element3DCollection();
            foreach (var model3D in model3Ds)
            {
                this.ModelGeometry.Add(model3D);
              //  model3D.Attach(modelView.RenderHost);
            }
            this.OnPropertyChanged("ModelGeometry");
        }

    }

}