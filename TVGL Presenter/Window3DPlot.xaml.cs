// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Window3DPlot.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for Window3DPlot.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Windows;

namespace TVGL
{


    /// <summary>
    /// Interaction logic for Window3DPlot.xaml
    /// </summary>
    public partial class Window3DPlot : Window
    {
        public Window3DPlot(Window3DPlotViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = viewModel;
            Closed += (s, e) =>
            {
                if (DataContext is IDisposable)
                {
                    (DataContext as IDisposable).Dispose();
                }
            };
            ((Window3DPlotViewModel)DataContext).SetUpCamera(this.view);
        }

        private void ResetCameraButtonClick(object sender, RoutedEventArgs e)
        {
            ((Window3DPlotViewModel)DataContext).ResetCameraCommand();
        }
    }

}