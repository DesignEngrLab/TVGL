using System.Linq;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public  static partial class PrimitiveSurfaceTessellation
    {
        public static void TestPresent()
        {
            var sphere1 = new Sphere(new Vector3(3, 4, 5), 6, true);
            sphere1.Tessellate(0.8);

            var cone = new Cone(Vector3.UnitZ, Vector3.UnitX, 1, true);
            cone.Length = 10;
            cone.Tessellate();
            foreach (var face in cone.Faces)
                face.Color = new Color(100, 200, 0, 100);

            var cyl = new Cylinder
            {
                Anchor = Vector3.UnitX,
                Axis = Vector3.UnitZ,
                Radius = 2,
                IsPositive = true
            };
            cyl.MinDistanceAlongAxis = -15;  
            cyl.MaxDistanceAlongAxis = 15;
            cyl.Tessellate();
            foreach (var face in cyl.Faces)
                face.Color = new Color(100, 0, 200, 100);

            var quadric = new GeneralQuadric(1, -2, 3, 4, 5, 6, 7, 8, 9, 10);
            quadric.Tessellate(-10,10,-10,10,-10,10,1);
            foreach (var face in quadric.Faces)
                face.Color = new Color(100, 0, 100, 200);

            Presenter.ShowAndHang(sphere1.Faces.Concat(cone.Faces).Concat(cyl.Faces).Concat(quadric.Faces));
        }
    }
}