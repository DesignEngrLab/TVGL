using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TVGL
{
    /// <summary>
    /// Interaction logic for PlotWindow.xaml
    /// </summary>
    public partial class Window2DHeldPlot : Window
    {
        public Window2DHeldPlot()
        {
            DataContext = new HeldViewModel();
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ((HeldViewModel)DataContext).OnLoaded(sender, e);

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => ((HeldViewModel)DataContext).OnClosing(sender, e);
    }
}