using System;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Media3D;
using OxyPlot.Axes;
using OxyPlot.Series;
using TVGL;

namespace TVGL_Presenter
{
    /// <summary>
    /// The Class HelixPresenter is the only class within the TVGL Helix Presenter
    /// project (TVGL_Presenter.dll). It is a simple static class with one main
    /// function, "Show".
    /// </summary>
    public static class Presenter
    {
        const double bufferRatio2D = 0.75;
        public static void Show(IList<Point> points, string title)
        {
            var window = new Window2DPlot();
            window.Title = title;
            var series = new ScatterSeries();
            series.Points.AddRange(points.Select(p => new ScatterPoint(p.X, p.Y, 1, 1)));
            var xMin = points.Min(pt => pt.X);
            var xMax = points.Max(pt => pt.X);
            var width = xMax - xMin;
            var yMin = points.Min(pt => pt.Y);
            var yMax = points.Max(pt => pt.Y);
            var height = yMax - yMin;
            var buffer = bufferRatio2D * Math.Min(width, height);
            xMin -= buffer;
            xMax += buffer;
            yMin -= buffer;
            yMax += buffer;
            window.Plot.Series.Add(series);
            window.Plot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = xMin, Maximum = xMax });
            window.Plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = yMin, Maximum = yMax });
            window.ShowDialog();
        }
        public static void Show(IList<Vertex> vertices, double[] direction, string title)
        {
            Show(MiscFunctions.Get2DProjectionPoints(vertices, direction, false), title);
        }
        /// <summary>
        /// Shows the specified tessellated solid in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        /// <param name="seconds"></param>
        public static void Show(TessellatedSolid tessellatedSolid, int seconds = 0)
        {
            var window = new Window3DPlot();
            window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.ZoomExtentsWhenLoaded = true;
            if (seconds > 0)
            {
                window.Show();
                System.Threading.Thread.Sleep(seconds * 1000);
                window.Close();
            }
            else window.Show();
        }

        /// <summary>
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds"></param>
        public static void Show(IList<TessellatedSolid> tessellatedSolids, int seconds = 0)
        {
            var window = new Window3DPlot();
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
            else window.Show();
        }

        /// <summary>
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <param name="seconds"></param>

        public static void ShowSequentially(IList<TessellatedSolid> tessellatedSolids, int seconds = 1)
        {
            //var models = new List<Visual3D>();
            var window = new Window3DPlot();
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