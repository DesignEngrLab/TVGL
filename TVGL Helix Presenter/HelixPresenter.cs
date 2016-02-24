using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using TVGL;
using MIConvexHull;

namespace TVGL_Helix_Presenter
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Helix_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static class HelixPresenter
    {
        /// <summary>
        /// Shows the specified tessellated solid in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        /// <param name="seconds"></param>
        public static void Show(TessellatedSolid tessellatedSolid, int seconds = 0)
        {
            var window = new MainWindow();
            window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.ZoomExtentsWhenLoaded = true;
            if (seconds > 0)
            {
                window.Show();
                System.Threading.Thread.Sleep(seconds*1000);
                window.Close();
            }
            else window.ShowDialog();
        }

        /// <summary>
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds"></param>
        public static void Show(IList<TessellatedSolid> tessellatedSolids, int seconds = 0)
        {
            var window = new MainWindow();
            var models = new List<Visual3D>();
           
            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                models.Add(model);
                window.view1.Children.Add(model);
            }
            window.view1.ZoomExtentsWhenLoaded = true;
            if (seconds > 0)
            {
                window.Show();
                System.Threading.Thread.Sleep(seconds * 1000);
                window.Close();
            }
            else window.ShowDialog();
        }

        /// <summary>
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds"></param>
        
        public static void ShowSequentially(IList<TessellatedSolid> tessellatedSolids, int seconds = 1)
        {
            //var models = new List<Visual3D>();
            var window = new MainWindow();
            var models = new ObservableCollection<Visual3D>();
            var startLocation = window.WindowStartupLocation;

            foreach (var tessellatedSolid in tessellatedSolids)
            {
                var model = MakeModelVisual3D(tessellatedSolid);
                window.view1.Children.Add(model);
                window.view1.ZoomExtentsWhenLoaded = true;
                window.WindowStartupLocation = startLocation;
                window.Show();
                System.Threading.Thread.Sleep(seconds * 1000);
                window.Hide();
                //var size = new Size(400, 400);
                window.InvalidateVisual();
                //window.Top = window.Top;
                //window.Left = window.Left;
                //window.UpdateLayout();
                //window.view1.Items.Refresh();
                //window.view1.UpdateLayout();              
                //window.Measure(size);
                //window.Arrange(new Rect(new System.Windows.Point(0, 0), size));
            }
            window.Close();
            
        }

        private static Visual3D MakeModelVisual3D(TessellatedSolid ts)
        {
            var defaultMaterial = MaterialHelper.CreateMaterial(
                    new System.Windows.Media.Color
                    {
                        A = ts.SolidColor.A,
                        B = ts.SolidColor.B,
                        G = ts.SolidColor.G,
                        R = ts.SolidColor.R
                    });
            if (ts.HasUniformColor)
            {
                var positions = ts.Faces.SelectMany(f => f.Vertices.Select(v => new Point3D(v.Position[0], v.Position[1], v.Position[2])));
                var normals = ts.Faces.SelectMany(f => f.Vertices.Select(v => new Vector3D(f.Normal[0], f.Normal[1], f.Normal[2])));
                return new ModelVisual3D
                {
                    Content =
                        new GeometryModel3D
                        {
                            Geometry = new MeshGeometry3D
                            {
                                Positions = new Point3DCollection(positions),
                                // TriangleIndices = new Int32Collection(triIndices),
                                Normals = new Vector3DCollection(normals)
                            },
                            Material = defaultMaterial
                        }
                };
            }
            var result = new ModelVisual3D();
            foreach (var f in ts.Faces)
            {
                var vOrder = new Point3DCollection();
                for (var i = 0; i < 3; i++)
                    vOrder.Add(new Point3D(f.Vertices[i].X, f.Vertices[i].Y, f.Vertices[i].Z));

                var c = (f.Color == null)
                    ? defaultMaterial
                    : MaterialHelper.CreateMaterial(new System.Windows.Media.Color { A = f.Color.A, B = f.Color.B, G = f.Color.G, R = f.Color.R });
                result.Children.Add(new ModelVisual3D
                {
                    Content =
                        new GeometryModel3D
                        {
                            Geometry = new MeshGeometry3D { Positions = vOrder },
                            Material = c
                        }
                });
            }
            return result;
        }
    }
}