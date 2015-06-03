using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using TVGL.Tessellation;

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
        public static void Show(TessellatedSolid tessellatedSolid)
        {
            var window = new MainWindow();
            window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.ZoomExtentsWhenLoaded = true;
            window.ShowDialog();
        }
        /// <summary>
        /// Shows the specified tessellated solids in a Helix toolkit window.
        /// </summary>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        public static void Show(IList<TessellatedSolid> tessellatedSolids)
        {
            var window = new MainWindow();

            foreach (var tessellatedSolid in tessellatedSolids)
                window.view1.Children.Add(MakeModelVisual3D(tessellatedSolid));
            window.view1.ZoomExtentsWhenLoaded = true;
            window.ShowDialog();
        }

        private static Visual3D MakeModelVisual3D(TessellatedSolid ts)
        {
            var defaultMaterial = MaterialHelper.CreateMaterial(
                    new Color
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

                var c = (f.color == null)
                    ? defaultMaterial
                    : MaterialHelper.CreateMaterial(new Color { A = f.color.A, B = f.color.B, G = f.color.G, R = f.color.R });
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