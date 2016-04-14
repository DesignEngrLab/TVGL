using HelixToolkit;
using PropertyTools;
using System.Windows;
namespace TVGL_Presenter
{
    public partial class Window3DPlot : Window
    { 
        public Window3DPlot()
        {
            InitializeComponent();
            ShowGridLinesMenuItem.IsChecked = true;
        }

        private void GridLines_OnChecked(object sender, RoutedEventArgs e)
        {
            GridLines.Visible = true;
        }
        private void GridLines_OnUnChecked(object sender, RoutedEventArgs e)
        {
            GridLines.Visible = false;
        }
    }
}