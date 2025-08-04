using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WindowsDesktopPresenter
{
    /// <summary>
    /// Interaction logic for PlotWindow.xaml
    /// </summary>
    public partial class Window2DHeldPlot : Window
    {
        public Window2DHeldPlot()
        {
            DataContext = new Held2DViewModel();
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ((Held2DViewModel)DataContext).OnLoaded(sender, e);

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => ((Held2DViewModel)DataContext).OnClosing(sender, e);
    }
}