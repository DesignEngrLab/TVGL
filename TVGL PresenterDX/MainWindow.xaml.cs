// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TVGL.IOFunctions;
using TVGL.Voxelization;

namespace TVGLPresenterDX
{
    using System.Windows;
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


        private void main(object sender, RoutedEventArgs e)
        {
            viewModel.Test();
        }
    }
}