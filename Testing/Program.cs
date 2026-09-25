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
            var s = new Sphere(new Vector3(10, 10, 10), 15, true);
            var sTessellated = s.Tessellate();
            Presenter.ShowAndHang(sTessellated);
            var hyperboloid = new GeneralQuadric(-.1, .1, .1, 0, 0, 0, 0, 0, 0, 2);
            var hTessellated = hyperboloid.Tessellate(-100, 100, -100, 100, -100, 100, 5);
            Presenter.ShowAndHang(hTessellated);
        }
    }
}