#if !PRESENT
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)

        {
            //TestVoxelization();
            //TS_Testing_Functions.TestModify();
            //TVGL3Dto2DTests.TestSilhouette();
            // Polygon_Testing_Functions.TestSimplify();
            //TS_Testing_Functions.TestClassify();
            TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

#if PRESENT

            // 1. bubble up from the bin directories to find the TestFiles directory
            var polysValue = TestCases.MakeBumpyRings(50, 28, 1.8);
            //var polysValue = TestCases.EdgeCases["tinyOffsetProb"];
            var polygon1 = new Polygon(polysValue.Item1);
            var polygon2 = new Polygon(polysValue.Item2);
            //Presenter.ShowAndHang(new[] { polygon1, polygon2 });
            var polygons = polygon1.Union(polygon2);
            var polygon = polygons.LargestPolygon();
            Presenter.ShowAndHang(polygon);
            polygon.Transform(Matrix3x3.CreateRotation(1));
            Presenter.ShowAndHang(polygon);

            //var polygons = new List<Polygon> { polygon1 };
            //var coords1 = TestCases.MakeStarryCircularPolygon(50, 28, 1.8).ToList();
            //var hole1 = TestCases.MakeStarryCircularPolygon(80, 14, 1.5).ToList();
            //hole1.Reverse();
            //var polygon1 = new Polygon(coords1);
            //polygon1 = polygon1.Intersect(new Polygon(hole1))[0];
            //Presenter.ShowAndHang(polygons);
            //var polygon1 = new Polygon(coords1, true);
            // Presenter.ShowAndHang(polygon1);
            //var polygons3 = polygon1.OffsetRound(88);
            var offsetBase = Math.Sqrt(polygons.LargestPolygon().Area);
            var factors = new[] { -.03, 0.03, -.1, 0.1, -.3, 0.3, -1, 1, -3, 3, -10 };
            foreach (var factor in factors)
            {
                var offset = factor * offsetBase;
                var polygons3 = polygons[0].OffsetRound(offset, 0.00254);
                Presenter.ShowAndHang(polygons3);
}

#else
            //#endif
            //PolygonBooleanTester.FullComparison();
            //var stats = new List<(string, int, long, long)>();

            //foreach (var testCase in TestCases.GetAllTwoArgumentEdgeCases())
            //{
            //    var polys = testCase.Value;
            //    PolygonBooleanTester.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2),
            //        TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
            //}
#endif
            //var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
            //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
            //PolygonOperationsTesting.TestRemoveSelfIntersect();
            //PolygonOperationsTesting.DebugEdgeCases();
            //PolygonOperationsTesting.DebugOctagons();
            //PolygonOperationsTesting.TestUnionSimple();
        }

        private static void TestVoxelization()
        {
            var iS = new ImplicitSolid();
            Presenter.ShowAndHang(iS.ConvertToTessellatedSolid());

            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "TestFiles"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "TestFiles");

            //var fileName = dir.FullName + Path.DirectorySeparatorChar + "test.json";
            var fileNames = dir.GetFiles("*ath*").OrderBy(x => r.NextDouble()).ToArray();
            for (var i = 0; i < fileNames.Length; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                var p = solid.CreateSilhouette(Vector3.UnitY);
                Presenter.ShowAndHang(p);
                //var css = CrossSectionSolid.CreateConstantCrossSectionSolid(Vector3.UnitZ, 0, 20, p, solid.SameTolerance, solid.Units);
                //Presenter.ShowAndHang(css.ConvertToTessellatedExtrusions(false, false));
                var faces = Extrude.ExtrusionFacesFrom2DPolygons(p, Vector3.UnitY, 0, 20);
                Presenter.ShowAndHang(faces);
               // solid.Transform(Matrix4x4.CreateRotationY(Math.PI/2));
                Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                Console.WriteLine("voxelizing...");
                var voxsol = new VoxelizedSolid(solid, 1000);
                Console.WriteLine("now presenting " + name);
                //Presenter.ShowAndHang(voxsol);
                Presenter.ShowAndHang(voxsol.ConvertToTessellatedSolidMarchingCubes(12));
                Console.WriteLine("draft in pos y");
                var yposVoxSol = voxsol.DraftToNewSolid(CartesianDirections.YPositive);
                Console.WriteLine("presenting");
                Presenter.ShowAndHang(yposVoxSol.ConvertToTessellatedSolidMarchingCubes(5));

                Console.WriteLine("draft in neg y");
                var ynegVoxSol = voxsol.DraftToNewSolid(CartesianDirections.YNegative);
                Console.WriteLine("presenting");
                Presenter.ShowAndHang(ynegVoxSol.ConvertToTessellatedSolidMarchingCubes(5));

                Console.WriteLine("union of y solids");
                var yUnion = yposVoxSol.UnionToNewSolid(ynegVoxSol);
                Console.WriteLine("presenting");
                Presenter.ShowAndHang(yUnion.ConvertToTessellatedSolidMarchingCubes(5));

                Console.WriteLine("draft in neg z");
                var znegVoxSol = voxsol.DraftToNewSolid(CartesianDirections.ZNegative);
                Console.WriteLine("intersecting");
                var intersect = znegVoxSol.IntersectToNewSolid(yUnion);
                Console.WriteLine("presenting");
                Presenter.ShowAndHang(intersect.ConvertToTessellatedSolidMarchingCubes(5));

            }
        }
    }
}