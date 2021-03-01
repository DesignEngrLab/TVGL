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
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "repos"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "repos\\medemaMVP\\MedemaUI\\bin\\Debug\\netcoreapp3.1");
            var dirName = dir.FullName;
            //subtractFail44254.55676414352
            // subtractFail44254.53607401621.sub.json
            //subtractFail44254.55658121528
            // subtractFail44254.5640559838
            //subtractFail44254.57326221065
            // subtractFail44254.56914480324
            //subtractFail44254.57365136574
            var fileNames = dir.GetFiles("subtractFail44254.57369567129*.json").ToList();
            while (fileNames.Any())
            {
                var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = nameSegments[0] + "." + nameSegments[1];
                var polygons = new List<Polygon>();
                Polygon min = null;
                Polygon sub = null;
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(ff => item.FullName.Equals(ff.FullName));
                    IO.Open(item.FullName, out Polygon p);
                    if (item.FullName.Contains("min.json"))
                        min = p;
                    else sub = p;
                    //polygons.Add(p);
                }
                //if (polygons.Count < 2) continue;
                Debug.WriteLine("Attempting: " + filename);
                //var polygon1 = polygons[0];
                //var polygon2 = polygons[2];

                //91282315972, 91362112269, 9212769213
                //Presenter.ShowAndHang(new[] { min, sub });
                min.Subtract(sub);
                //polygons.RemoveAt(1);
                //var polysValue = TestCases.MakeBumpyRings(50, 28, 1.8);
                //var polysValue = TestCases.EdgeCases["tinyOffsetProb"];
                //var polygon1 = new Polygon(polysValue.Item1);
                //var polygon2 = new Polygon(polysValue.Item2);
                //Presenter.ShowAndHang(polygons);
                //polygons = polygons.IntersectPolygons();
                //polygons = polygons.UnionPolygons(new Polygon[0]);
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