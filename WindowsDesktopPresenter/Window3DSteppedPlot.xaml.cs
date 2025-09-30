using OxyPlot;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WindowsDesktopPresenter
{
    /// <summary>
    /// Interaction logic for PlotWindow.xaml
    /// </summary>
    public partial class Window3DSteppedPlot : Window
    {
        Stepped3DViewModel stepViewModel;
        ObservableCollection<PlotModel> Models = new ObservableCollection<PlotModel>();
        public PlotModel SelectedModel { get; set; }
        public int SelectedIndex { get; private set; }
        public Window3DSteppedPlot()
        {
            DataContext = stepViewModel = new Stepped3DViewModel(this);
            InitializeComponent();
            Closed += (s, e) =>
            {
                if (DataContext is IDisposable)
                {
                    (DataContext as IDisposable).Dispose();
                }
            };
            stepViewModel.SetUpCamera(this.view);
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Calculate the index based on the value of the ScrollBar
            int selectedIndex = (int)e.NewValue;

            // Update the SelectedIndex property
            SelectedIndex = selectedIndex;

            // Update the PlotView by setting the Model property to the selected PlotModel
            if (SelectedIndex >= 0 && SelectedIndex < Models.Count)
            {
                SelectedModel = Models[SelectedIndex];
                group1. = SelectedModel;
                view.Model.InvalidatePlot(true);
            }
        }


        private void ResetCameraButtonClick(object sender, RoutedEventArgs e) => stepViewModel.ResetCameraCommand();

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => stepViewModel.OnClosing(sender, e);
    }
}