using System;
using System.IO;
using TVGL;
using WebGPUPresenter;

namespace TVGLUnitTestsAndBenchmarking
{

    internal class Program
    {
        public static string inputFolder = "TestFiles\\QuadricLayerData";

        [STAThread]
        private static void Main(string[] args)
        {
            var dirInfo = IO.BackoutToFolder(inputFolder);

            OutputServices.Presenter2D = new Presenter2D();
            OutputServices.Presenter3D = new Presenter3D();
            TestCase1(dirInfo);
            TestCase2(dirInfo);
            TestCase3(dirInfo);
            TestCase4(dirInfo);
            TestCase5(dirInfo);
        }

        private static void TestCase1(System.IO.DirectoryInfo dirInfo)
        {
            // ellipse
            var quadric = new GeneralQuadric(0.2, 0.8, 0.1, 0, 0, 0, 0, 0, 0, -5);
            IO.Open(dirInfo.FullName + "\\ellipseLayerSphereModel.json",
                out Polygon3D loop1);
            Presenter.ShowAndHang(loop1.Vertices, solids: quadric.Tessellate(-10, 10, -10, 10, -10, 10, 0.5));
        }
        private static void TestCase2(DirectoryInfo dirInfo)
        {
            // ellipse
            var quadric = new GeneralQuadric(0.2, 0.8, 0.1, 0, 0, 0, 0, 0, 0, -5);
            IO.Open(dirInfo.FullName + "\\ellipseLayerFlatZModel.json",
                out Polygon3D loop1);
            Presenter.ShowAndHang(loop1.Vertices, solids: quadric.Tessellate(-10, 10, -10, 10, -10, 10, 0.5));
        }
        private static void TestCase3(DirectoryInfo dirInfo)
        {
            // ellipse
            var quadric = new GeneralQuadric(0.2, 0.8, 0.1, 0, 0, 0, 0, 0, 0, -5);
            IO.Open(dirInfo.FullName + "\\ellipseLayerFlatZModelTwoLoops1.json",
                out Polygon3D loop1);
            IO.Open(dirInfo.FullName + "\\ellipseLayerFlatZModelTwoLoops2.json",
                out Polygon3D loop2);
            Presenter.ShowAndHang([loop1.Vertices, loop2.Vertices], solids: quadric.Tessellate(-10, 10, -10, 10, -10, 10, 0.5));
        }
        private static void TestCase4(DirectoryInfo dirInfo)
        {
            // hyperboloid
            var quadric = new GeneralQuadric(-0.6, 1, 0.4, 0, 0, 0, 0, 0, 0, -5);

            IO.Open(dirInfo.FullName + "\\hyperboloidLayerSphereModel.json",
                out Polygon3D loop1);
            Presenter.ShowAndHang(loop1.Vertices, solids: quadric.Tessellate(-10, 10, -10, 10, -10, 10, 0.5));
        }
        private static void TestCase5(DirectoryInfo dirInfo)
        {
            // hyperboloid
            var quadric = new GeneralQuadric(-0.6, 1, 0.4, 0, 0, 0, 0, 0, 0, -5);
            IO.Open(dirInfo.FullName + "\\hyperboloidLayerThreeLoopModel1.json",
                out Polygon3D loop1);
            IO.Open(dirInfo.FullName + "\\hyperboloidLayerThreeLoopModel2.json",
                out Polygon3D loop2);
            IO.Open(dirInfo.FullName + "\\hyperboloidLayerThreeLoopModel3.json",
                out Polygon3D loop3);
            Presenter.ShowAndHang([loop1.Vertices, loop2.Vertices, loop3.Vertices], solids: quadric.Tessellate(-10, 10, -10, 10, -10, 10, 0.5));
        }

    }
}