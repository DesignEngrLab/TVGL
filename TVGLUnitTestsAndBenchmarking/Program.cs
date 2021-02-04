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
            //TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

#if PRESENT

            // 1. bubble up from the bin directories to find the TestFiles directory
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "TestFiles"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "TestFiles");
            var dirName = dir.FullName;
            var fileNames = dir.GetFiles("union*.json").ToList();
            while (fileNames.Any())
            {
                var filename = fileNames[0].Name;
                var nameSegments = filename.Split('.');
                var preName = nameSegments[0] + "." + nameSegments[1];
                var polygons = new List<Polygon>();
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(ff => item.FullName.Equals(ff.FullName));
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                //if (polygons.Count < 2) continue;
                Debug.WriteLine("Attempting: " + filename);
                //var polygon1 = polygons[0];
                //var polygon2 = polygons[2];

                //91282315972, 91362112269, 9212769213

                Presenter.ShowAndHang(polygons);
                //polygons.RemoveAt(1);
                //var polysValue = TestCases.MakeBumpyRings(50, 28, 1.8);
                //var polysValue = TestCases.EdgeCases["tinyOffsetProb"];
                //var polygon1 = new Polygon(polysValue.Item1);
                //var polygon2 = new Polygon(polysValue.Item2);
                //Presenter.ShowAndHang(new[] { polygon1, polygon2 });
                polygons = polygons[0].Union(polygons[1]);
                //polygons = polygon1.Union(polygon2);
                continue;
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
            //#endif
            //PolygonBooleanTester.FullComparison();
            //var stats = new List<(string, int, long, long)>();

            //foreach (var testCase in TestCases.GetAllTwoArgumentEdgeCases())
            //{
            //    var polys = testCase.Value;
            //    PolygonBooleanTester.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2),
            //        TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
            //}

                //var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
                //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
                //PolygonOperationsTesting.TestRemoveSelfIntersect();
                //PolygonOperationsTesting.DebugEdgeCases();
                //PolygonOperationsTesting.DebugOctagons();
                //PolygonOperationsTesting.TestUnionSimple();
            }
#endif
        }

    }
}