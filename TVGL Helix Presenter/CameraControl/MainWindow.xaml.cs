// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix 3D Toolkit">
//   http://helixtoolkit.codeplex.com, license: MIT
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
namespace TVGL_Helix_Presenter
{
    public partial class MainWindow : Window
    { 
        public MainWindow()
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