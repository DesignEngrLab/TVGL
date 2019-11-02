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
        public GroupElement3D ModelGeometry { get; private set; }
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

       // public DefaultRenderTechniquesManager RenderTechniquesManager { get; private set; }
        public MainViewModel()
        {
            this.ModelTransform = new TranslateTransform3D(0, 0, 0);

            this.ModelGeometry = new GroupModel3D();
        //    RenderTechniquesManager = new DefaultRenderTechniquesManager();
            EffectsManager = new DefaultEffectsManager();
        }

        
        public void AttachModelList(IList<MeshGeometryModel3D> model3Ds)
        {
            this.ModelTransform = new TranslateTransform3D(0, 0, 0);
            this.ModelGeometry = new GroupModel3D();
            foreach (var model3D in model3Ds)
            {
                this.ModelGeometry.ItemsSource.Add(model3D);
                //  model3D.Attach(modelView.RenderHost);
            }
            this.OnPropertyChanged("ModelGeometry");
        }

    }

}