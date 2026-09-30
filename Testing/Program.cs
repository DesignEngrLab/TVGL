using System;
using TVGL;
using WebGPUPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            OutputServices.Presenter2D = new Presenter2D();
            OutputServices.Presenter3D = new Presenter3D();
            var p3d = new Polygon3D(
                [new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(6, 2, 1)], true,
                [[new Vector3(2, 3, 4), new Vector3(3, 4, 5), new Vector3(5, 3, 2)]]);
            IO.Save(p3d, "polygon3D.json");
            IO.Open("polygon3D.json", out Polygon3D p3dLoaded);
            var s = new Sphere(new Vector3(10, 10, 10), 15, true);
            var sTessellated = s.Tessellate();
            Presenter.ShowAndHang(sTessellated);
            var hyperboloid = new GeneralQuadric(-.1, .1, .1, 0, 0, 0, 0, 0, 0, 2);
            var hTessellated = hyperboloid.Tessellate(-100, 100, -100, 100, -100, 100, 5);
            Presenter.ShowAndHang(hTessellated);
        }
    }
}