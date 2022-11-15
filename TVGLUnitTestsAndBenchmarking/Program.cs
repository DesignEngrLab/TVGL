using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public class SegIntersectClass
    {
        Random r = new Random();
        public void CheckAccuracy()
        {
            foreach (var d in Data())
            {
                var conv = TestConventional(d);
                if (conv.Item1)
                    Console.WriteLine(conv.Item1 + ", " + conv.Item2.X + ", " + conv.Item2.Y + ", " + conv.Item3 + ", " + conv.Item4);
                else Console.WriteLine("conv found no intersection");
                var pga = TestPGA(d);
                if (pga.Item1)
                    Console.WriteLine(pga.Item1 + ", " + pga.Item2.X + ", " + pga.Item2.Y + ", " + pga.Item3 + ", " + pga.Item4);
                else Console.WriteLine("pga found no intersection");
                Console.WriteLine();
                //if (!pga.Item1 && pga.Item1 == conv.Item1)
                //    Console.WriteLine(String.Join(", ", d));
            }
        }
        public IEnumerable<double[]> Data()
        {
            yield return new[] { 0.4385146521963749, 10.364880796295385, 10.200722586116735, 2.1927047120180854, 2.0762985717358977, 11.127089620942188, 10.178747709813473, 0.9295354822357275 };
            yield return new[] { 0.8069868504128048, 9.989662824461375, 6.168424908277015, -5.237284209535103, 5.24473967371708, -6.675423901559528, 8.217904161315563, 3.785469181852359 };
            yield return new[] { 1.7027330429091632, 9.643531708337651, -6.538366624959455, -10.11749937895817, 6.776649766418461, 4.368753648421272, 1.1178163186282493, 8.332176313331848 };
            yield return new[] { 8.252530099705776, 6.226387182086324, -7.962663951628316, -2.9012903180615615, 7.064339890425189, 6.142908667444132, 2.3012944238899675, -7.216453057222747 };
            yield return new[] { 3.011335866526794, 7.985948635668391, -1.5181477399361067, -7.541110034257271, 7.776630392892776, 3.5956243432900727, -10.043555057554594, -0.9591923664562562 };
            yield return new[] { 8.327760084933988, 1.222120433183249, -7.894973954286644, -1.4051358226697612, 9.324954084723696, -6.735560423255866, 2.7499552148702637, -7.223306755416853 };
            yield return new[] { 7.293300190782637, 2.277323858241992, 7.821205085695791, -9.19135932563374, 10.980017722174205, 0.961892943524647, 9.86714120126616, -0.24057037112886756 };
            yield return new[] { 0.09234723887342591, 7.583350371612793, -7.44520776481961, -5.493343116672661, 5.6097034528097955, -9.128895758198361, 3.28161533605255, 8.285406478068806 };
            yield return new[] { 4.155895592072373, 6.670879304776244, 3.6601717237744906, -6.631298047716252, 11.317116772850778, -2.8677523177365174, 10.532551455897964, 5.4295864161356935 };
            yield return new[] { 8.555003631542315, 1.0911637215981507, -5.0070430684418366, -9.024285310395731, 9.116351628824349, -3.918961099331923, 7.223205497353764, -2.9920278602698835 };


            yield return new[] { 20.8069868504128048, 9.989662824461375, 26.168424908277015, -5.237284209535103, 5.24473967371708, -6.675423901559528, 8.217904161315563, 3.785469181852359 };
            yield return new[] { -21.7027330429091632, 9.643531708337651, 26.538366624959455, -10.11749937895817, 6.776649766418461, 4.368753648421272, 1.1178163186282493, 8.332176313331848 };
            yield return new[] { 8.252530099705776, 26.226387182086324, -7.962663951628316, -18.9012903180615615, 7.064339890425189, 6.142908667444132, 2.3012944238899675, -7.216453057222747 };
            yield return new[] { 3.011335866526794, -27.985948635668391, -1.5181477399361067, -27.541110034257271, 7.776630392892776, 3.5956243432900727, -10.043555057554594, -0.9591923664562562 };
            yield return new[] { 28.327760084933988, 21.222120433183249, 13.894973954286644, 19.4051358226697612, 9.324954084723696, -6.735560423255866, 2.7499552148702637, -7.223306755416853 };
            yield return new[] { 27.293300190782637, 22.277323858241992, -13.821205085695791, -19.19135932563374, 10.980017722174205, 0.961892943524647, 9.86714120126616, -0.24057037112886756 };
            yield return new[] { 20.09234723887342591, -13.583350371612793, 13.44520776481961, 15.493343116672661, 5.6097034528097955, -9.128895758198361, 3.28161533605255, 8.285406478068806 };
            yield return new[] { -16.155895592072373, -14.670879304776244, 23.6601717237744906, -26.631298047716252, 11.317116772850778, -2.8677523177365174, 10.532551455897964, 5.4295864161356935 };

        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (bool, Vector2, double, double) TestConventionalPre(double[] B)
        {
            var success = TVGL.MiscFunctions.SegmentSegment2DIntersectionPre(new Vector2(B[0], B[1]), new Vector2(B[2], B[3]),
                new Vector2(B[4], B[5]), new Vector2(B[6], B[7]),
                out var intersectionPt, out var t_a, out var t_b);
            return (success, intersectionPt, t_a, t_b);
        }


        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (bool, Vector2, double, double) TestConventional(double[] B)
        {
            var success = TVGL.MiscFunctions.SegmentSegment2DIntersection(new Vector2(B[0], B[1]), new Vector2(B[2], B[3]),
                new Vector2(B[4], B[5]), new Vector2(B[6], B[7]),
                out var intersectionPt, out var t_a, out var t_b);
            return (success, intersectionPt, t_a, t_b);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]

        public (bool, Vector2, double, double) TestPGA(double[] B)
        {
            var success = TVGL.MiscFunctions.SegmentSegment2DIntersectionPGA(new Vector2(B[0], B[1]), new Vector2(B[2], B[3]),
                new Vector2(B[4], B[5]), new Vector2(B[6], B[7]),
                out var intersectionPt, out var t_a, out var t_b);
            return (success, intersectionPt, t_a, t_b);
        }
    }
    internal class Program
    {
        static Random r = new Random();
        //static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)
        {
            //var seggy = new SegIntersectClass();
            //seggy.CheckAccuracy();
            var summary = BenchmarkRunner.Run(typeof(SegIntersectClass).Assembly);
            //PGA2DTest();
            //TS_Testing_Functions.TestSilhouette();
            //JustShowMeThePolygons(BackoutToFolder("TestFiles\\polygons"));
            //PolygonOperationsTesting.DebugEdgeCases();
            //DebugIntersectCases(BackoutToFolder("TestFiles\\polygons"));
            //DebugOffsetCases(BackoutToFolder("TestFiles\\polygons"));
            //DebugUnionCases(BackoutToFolder("TestFiles\\polygons"));
        }




        private static void PGA2DTest()
        {
            var f1 = new Vector2(2, 3);
            var t1 = new Vector2(12, 10);
            var f2 = new Vector2(50, 5);
            var t2 = new Vector2(3, 5);
            TVGL.MiscFunctions.SegmentSegment2DIntersection(f1, t1, f2, t2, out var intersectionPt, out var t_a, out var t_b);
            //var fg1 = new PGA2D(); fg1[5] = f1.X; fg1[4] = f1.Y; fg1[6] = 1;
            //var tg1 = new PGA2D(); tg1[5] = t1.X; tg1[4] = t1.Y; tg1[6] = 1;
            //var fg2 = new PGA2D(); fg2[5] = f2.X; fg2[4] = f2.Y; fg2[6] = 1;
            //var tg2 = new PGA2D(); tg2[5] = t2.X; tg2[4] = t2.Y; tg2[6] = 1;
            //var lg1 = fg1 & tg1;
            //var lg2 = fg2 & tg2;
            //var ip = lg1 ^ lg2;

            //var iVfg1 = fg1 & ip;
            //var iVtg1 = ip & tg1;
            //var dotf1 = iVfg1 | lg1;
            //var dott1 = iVtg1 | lg1;
            //var alpha1 = dotf1[0] / (dotf1[0] + dott1[0]);
            //var iVfg2 = fg2 & ip;
            //var iVtg2 = ip & tg2;
            //var dotf2 = iVfg2 | lg2;
            //var dott2 = iVtg2 | lg2;
            //var alpha2 = dotf2[0] / (dotf2[0] + dott2[0]);
            TVGL.MiscFunctions.SegmentSegment2DIntersectionPGA(f1, t1, f2, t2, out intersectionPt, out t_a, out t_b);

        }

        public static DirectoryInfo BackoutToFolder(string folderName = "")
        {
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(Path.Combine(dir.FullName, folderName)))
            {
                if (dir == null) throw new FileNotFoundException("Folder not found", folderName);
                dir = dir.Parent;
            }
            return new DirectoryInfo(Path.Combine(dir.FullName, folderName));
        }

        public static void DebugOffsetCases(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("offsetFail*.json").Skip(0).ToList();
            //var offset = -0.2;
            while (fileNames.Any())
            {
                var polygons = new List<Polygon>();
                var filename = fileNames[0].Name;
                //var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = string.Join('.', nameSegments.Take(2).ToArray());
                var offset = double.Parse(nameSegments[^4] + "." + nameSegments[^3]);
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                if (polygons.All(p => p == null)) continue;
                Debug.WriteLine("Attempting: " + filename);
                Presenter.ShowAndHang(polygons);
                var result = polygons.OffsetRound(offset, 0.02, polygonSimplify: PolygonSimplify.DoNotSimplify);
                Presenter.ShowAndHang(result);
            }
        }

        public static void DebugIntersectCases(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("intersect*.json").ToList();
            while (fileNames.Any())
            {
                var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = string.Join('.', nameSegments.Take(nameSegments.Length - 2).ToArray());

                var polygonsA = new List<Polygon>();
                var polygonsB = new List<Polygon>();
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    if (item.Name.Contains("B"))
                        polygonsB.Add(p);
                    else polygonsA.Add(p);
                }
                Debug.WriteLine("Attempting: " + filename);
                Presenter.ShowAndHang(polygonsA);
                Presenter.ShowAndHang(polygonsB);
                Presenter.ShowAndHang(new[] { polygonsA, polygonsB }.SelectMany(p => p));
                var result = polygonsA.IntersectPolygons(polygonsB);
                Presenter.ShowAndHang(result);
            }
        }
        public static void DebugUnionCases(DirectoryInfo dir)
        {
            var polygonsA = new List<Polygon>();
            var polygonsB = new List<Polygon>();

            foreach (var item in dir.GetFiles("union*.json"))
            {
                IO.Open(item.FullName, out Polygon p);
                if (item.Name.Contains("B", StringComparison.InvariantCulture))
                    polygonsB.Add(p);
                else polygonsA.Add(p);
            }

            Presenter.ShowAndHang(polygonsA);
            Presenter.ShowAndHang(polygonsB);
            Presenter.ShowAndHang(new[] { polygonsA, polygonsB }.SelectMany(p => p));
            var result = polygonsA.UnionPolygons(polygonsB);
            Presenter.ShowAndHang(result);
        }
        public static void JustShowMeThePolygons(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("endles*.json").ToList();
            var silhouetteBeforeFace = new List<Polygon>();
            foreach (var fileName in fileNames.Take(1))
            {
                //Debug.WriteLine("Attempting: " + fileName);
                IO.Open(fileName.FullName, out Polygon p);
                silhouetteBeforeFace.Add(p);
            }
            Presenter.ShowAndHang(silhouetteBeforeFace);

            var poly1 = silhouetteBeforeFace.OffsetMiter(15.557500000000001, tolerance: 0.08);
            var showe = new List<Polygon>();
            showe.AddRange(silhouetteBeforeFace);
            showe.AddRange(poly1);
            Presenter.ShowAndHang(showe);

            var poly2 = poly1.OffsetRound(-15.557500000000001, tolerance: 0.08);

            showe.AddRange(poly2);
            Presenter.ShowAndHang(showe);
            //p.RemoveSelfIntersections(ResultType.BothPermitted);
            //p.TriangulateToCoordinates();
        }
    }
}
