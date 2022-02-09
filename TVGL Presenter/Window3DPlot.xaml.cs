// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System;
using SharpDX;
using TVGL;
using TVGL.Voxelization;
using Color = TVGL.Color;

namespace TVGLPresenter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using HelixToolkit.SharpDX.Core;
    using HelixToolkit.Wpf.SharpDX;
    using TVGL;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window3DPlot : Window
    {
        private MainViewModel viewModel;

        public Window3DPlot()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            viewModel.modelView = this.view1;
            this.DataContext = viewModel;

        }



    }
}