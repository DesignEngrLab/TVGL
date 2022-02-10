// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Window3DPlot.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for Window3DPlot.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TVGLPresenter
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using HelixToolkit.Wpf.SharpDX;
    using SharpDX;
    using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
    using Transform3D = System.Windows.Media.Media3D.Transform3D;
    using Transform3DGroup = System.Windows.Media.Media3D.Transform3DGroup;
    using Vector3D = System.Windows.Media.Media3D.Vector3D;
    using Point3D = System.Windows.Media.Media3D.Point3D;
    using System;

    /// <summary>
    /// Interaction logic for Window3DPlot.xaml
    /// </summary>
    public partial class Window3DPlot : Window
    {
        public Window3DPlot(MainViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = viewModel;
            Closed += (s, e) => {
                if (DataContext is IDisposable)
                {
                    (DataContext as IDisposable).Dispose();
                }
            };
        }
    }

}