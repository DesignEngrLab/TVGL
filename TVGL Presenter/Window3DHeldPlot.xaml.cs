using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TVGL
{
    /// <summary>
    /// Interaction logic for PlotWindow.xaml
    /// </summary>
    public partial class Window3DHeldPlot : Window
    {
        Held3DViewModel held3DViewModel;
        internal Window3DHeldPlot()
        {
            DataContext = held3DViewModel = new Held3DViewModel(this);
            InitializeComponent();
            Closed += (s, e) =>
            {
                if (DataContext is IDisposable)
                {
                    (DataContext as IDisposable).Dispose();
                }
            };
            held3DViewModel.SetUpCamera(this.view);
        }


        private void ResetCameraButtonClick(object sender, RoutedEventArgs e) => held3DViewModel.ResetCameraCommand();

        private void OnLoaded(object sender, RoutedEventArgs e) => held3DViewModel.OnLoaded(sender, e);

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => held3DViewModel.OnClosing(sender, e);
    }
}