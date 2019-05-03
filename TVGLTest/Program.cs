using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using OuelletConvexHull;
using StarMathLib;
using TVGL;

namespace TVGLPresenterDX
{
    internal class Program
    {

        static readonly Stopwatch stopwatch = new Stopwatch();

        private static readonly string[] FileNames =
        {
            //"../../../TestFiles/Binary.stl",
            //   "../../../TestFiles/ABF.ply",
            //   "../../../TestFiles/Beam_Boss.STL",
            "../../../TestFiles/Beam_Clean.STL",

            "../../../TestFiles/bigmotor.amf",
            "../../../TestFiles/DxTopLevelPart2.shell",
            "../../../TestFiles/Candy.shell",
            "../../../TestFiles/amf_Cube.amf",
            "../../../TestFiles/train.3mf",
            "../../../TestFiles/Castle.3mf",
            "../../../TestFiles/Raspberry Pi Case.3mf",
            //"../../../TestFiles/shark.ply",
            "../../../TestFiles/bunnySmall.ply",
            "../../../TestFiles/cube.ply",
            "../../../TestFiles/airplane.ply",
            "../../../TestFiles/TXT - G5 support de carrosserie-1.STL.ply",
            "../../../TestFiles/Tetrahedron.STL",
            "../../../TestFiles/off_axis_box.STL",
            "../../../TestFiles/Wedge.STL",
            "../../../TestFiles/Mic_Holder_SW.stl",
            "../../../TestFiles/Mic_Holder_JR.stl",
            "../../../TestFiles/3_bananas.amf",
            "../../../TestFiles/drillparts.amf", //Edge/face relationship contains errors
            "../../../TestFiles/wrenchsns.amf", //convex hull edge contains a concave edge outside of tolerance
            "../../../TestFiles/hdodec.off",
            "../../../TestFiles/tref.off",
            "../../../TestFiles/mushroom.off",
            "../../../TestFiles/vertcube.off",
            "../../../TestFiles/trapezoid.4d.off",
            "../../../TestFiles/ABF.STL",
            "../../../TestFiles/Pump-1repair.STL",
            "../../../TestFiles/Pump-1.STL",
            "../../../TestFiles/SquareSupportWithAdditionsForSegmentationTesting.STL",
            "../../../TestFiles/Beam_Clean.STL",
            "../../../TestFiles/Square_Support.STL",
            "../../../TestFiles/Aerospace_Beam.STL",
            "../../../TestFiles/Rook.amf",
            "../../../TestFiles/bunny.ply",

            "../../../TestFiles/piston.stl",
            "../../../TestFiles/Z682.stl",
            "../../../TestFiles/sth2.stl",
            "../../../TestFiles/Cuboide.stl", //Note that this is an assembly 
            "../../../TestFiles/new/5.STL",
            "../../../TestFiles/new/2.stl", //Note that this is an assembly 
            "../../../TestFiles/new/6.stl", //Note that this is an assembly  //breaks in slice at 1/2 y direction
            "../../../TestFiles/new/4.stl", //breaks because one of its faces has no normal
            "../../../TestFiles/radiobox.stl",
            "../../../TestFiles/brace.stl", //Convex hull fails in MIconvexHull
            "../../../TestFiles/G0.stl",
            "../../../TestFiles/GKJ0.stl",
            "../../../TestFiles/testblock2.stl",
            "../../../TestFiles/Z665.stl",
            "../../../TestFiles/Casing.stl", //breaks because one of its faces has no normal
            "../../../TestFiles/mendel_extruder.stl",

            "../../../TestFiles/MV-Test files/holding-device.STL",
            "../../../TestFiles/MV-Test files/gear.STL"
        };

        internal static PointLight[] CurrentIssue()
        {
            var points = new[]
            {
                new PointLight(25.3999977, -237.46763546929208),
                new PointLight(25.3999977, -254.00001529999997),
                new PointLight(292.1000061, -254.0000153),
                new PointLight(330.2000122, -254.00001529999997),
                new PointLight(330.2000122, -219.39469697624477),
                new PointLight(330.2000122, 1.1742143626199923E-15),
                new PointLight(63.5000038, 1.1742143626199925E-15),
                new PointLight(25.3999977, 1.1742143626199923E-15),
                new PointLight(25.399997699999997, -17.010983139277897),
                new PointLight(25.3999977, -254.0000153),
                new PointLight(63.5000038, -254.00001530000003),
                new PointLight(292.1000061, -254.00001530000003),
                new PointLight(330.20001220000006, -254.0000153),
                new PointLight(330.20001220000006, -219.22213574366245),
                new PointLight(330.20001220000006, 1.1586618619264479E-15),
                new PointLight(25.3999977, 1.1586618619264479E-15),
            };
            return points;
        }

        [STAThread]
        private static void Main(string[] args)
        {
            var averageTimes = new Dictionary<int, List<(string MethodName, double AverageTimeInMilliseconds)>>();
            var repeat = 10;

            #region random cases
            Console.WriteLine("\n\n\n******* Random CASES ***********");

            var nums = new[] { 4, 10 };
            for (int k = 0; k < 5; k++)
            {
                foreach (var n in nums)
                {
                    averageTimes[n] = new List<(string MethodName, double AverageTimeInMilliseconds)>();
                    var campbellTotalTime = TimeSpan.Zero;
                    var ouelletTotalTime = TimeSpan.Zero;
                    //var monotoneChainTotalTime = TimeSpan.Zero;
                    for (int i = 0; i < repeat; i++)
                    {
                        var random = new Random();

                        PointLight[] points = new PointLight[n];
                        for (int j = 0; j < n; j++)
                            points[j] = new PointLight(100 * random.NextDouble(), 100 * random.NextDouble());

                        stopwatch.Restart();
                        var convexHull = MinimumEnclosure.ConvexHull2D(points);
                        stopwatch.Stop();
                        campbellTotalTime += stopwatch.Elapsed;
                        //Presenter.ShowAndHang(new[] {points.ToList(), convexHull});
                        //Console.WriteLine("{0}:{1} in {2}", n, convexHull.Count(),
                        //    stopwatch.Elapsed);

                        var windowsPoints = points.Select(p => new System.Windows.Point(p.X, p.Y)).ToList();
                        stopwatch.Restart();
                        var ouelletConvexHull = new OuelletConvexHull.ConvexHull(windowsPoints);
                        ouelletConvexHull.CalcConvexHull(ConvexHullThreadUsage.OnlyOne);
                        stopwatch.Stop();
                        ouelletTotalTime += stopwatch.Elapsed;
                        //Console.WriteLine("{0}:{1} in {2}", n, ouelletConvexHull.GetResultsAsArrayOfPoint().Count(),
                        //    stopwatch.Elapsed);

                        //var pointsAsList = points.ToList();
                        //stopwatch.Restart();
                        //var monotoneChainConvexHull = MinimumEnclosure.MonotoneChain(pointsAsList);
                        //stopwatch.Stop();
                        //monotoneChainTotalTime += stopwatch.Elapsed;
                        //Presenter.ShowAndHang(new[] {points.ToList(), convexHull});
                        //Console.WriteLine("{0}:{1} in {2}", n, monotoneChainConvexHull.Count(),
                        //    stopwatch.Elapsed);

                        var miConvexHull = MinimumEnclosure.MIConvexHull2D(points);
                        var p0 = new PolygonLight(miConvexHull);
                        var p1 = new PolygonLight(convexHull);
                        var p2 = new PolygonLight(ouelletConvexHull.GetResultsAsArrayOfPoint()
                            .Select(p => new PointLight(p.X, p.Y)));
                        //monotoneChainConvexHull.Reverse();
                        //var p3 = new PolygonLight(monotoneChainConvexHull);
                        if (!p1.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)) ||
                            !p1.Length.IsPracticallySame(p0.Length, p0.Length * (1 - Constants.HighConfidence)))
                        {
                            Presenter.ShowAndHang(new List<PolygonLight> { p0, p1 });

                            //foreach (var chain in hullCands)
                            //{
                            //    var polyLine = new PolygonLight(chain.Values.Select(c => c.Item1));
                            //    Presenter.ShowAndHang(new List<PolygonLight> { p1, polyLine });
                            //}
                        }

                        if (!p2.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                        {
                            Presenter.ShowAndHang(new List<PolygonLight> { p0, p2 });
                        }

                        //if (!p3.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                        //{
                        //    Presenter.ShowAndHang(new List<PolygonLight> { p0, p3 });
                        //}
                    }

                    var campbellAverage = campbellTotalTime.TotalMilliseconds / repeat;
                    var ouelletAverage = ouelletTotalTime.TotalMilliseconds / repeat;
                    //var monotoneChainAverage = monotoneChainTotalTime.TotalMilliseconds / repeat;
                    Console.WriteLine("N = {0}", n);
                    Console.WriteLine("1) {0} in {1} ", "Campbell", campbellAverage);
                    Console.WriteLine("2) {0} in {1} ", "Ouellet", ouelletAverage);
                    if (campbellAverage / ouelletAverage > 1)
                        Console.WriteLine("*** increased by {0} ", campbellAverage / ouelletAverage);
                    else Console.WriteLine("reduced by {0} ", campbellAverage / ouelletAverage);
                    //Console.WriteLine("3) {0} in {1} ", "Monotone Chain", monotoneChainAverage);

                    averageTimes[n].Add(("Campbell", campbellAverage));
                    averageTimes[n].Add(("Ouellet", ouelletAverage));
                    //averageTimes[n].Add(("Monotone Chain", monotoneChainAverage));
                }
                for (var n = 0; n < nums.Length; n++)
                {
                    nums[n] *= 10;
                }
            }
            #endregion

            #region Edge Cases
            Console.WriteLine("******* EDGE CASES ***********");
            for (int k = 1; k < 8; k++)
            {
                averageTimes[-k] = new List<(string MethodName, double AverageTimeInMilliseconds)>();
                var campbellTotalTime = TimeSpan.Zero;
                var ouelletTotalTime = TimeSpan.Zero;
                //var monotoneChainTotalTime = TimeSpan.Zero;
                for (int i = 0; i < repeat; i++)
                {
                    PointLight[] points = Issues(k);

                    stopwatch.Restart();
                    var convexHull = MinimumEnclosure.ConvexHull2D(points);
                    stopwatch.Stop();
                    campbellTotalTime += stopwatch.Elapsed;
                    //Presenter.ShowAndHang(new[] {points.ToList(), convexHull});
                    //Console.WriteLine("{0}:{1} in {2}", n, convexHull.Count(),
                    //    stopwatch.Elapsed);

                    var windowsPoints = points.Select(p => new System.Windows.Point(p.X, p.Y)).ToList();
                    stopwatch.Restart();
                    var ouelletConvexHull = new OuelletConvexHull.ConvexHull(windowsPoints);
                    ouelletConvexHull.CalcConvexHull(ConvexHullThreadUsage.OnlyOne);
                    stopwatch.Stop();
                    ouelletTotalTime += stopwatch.Elapsed;
                    //Console.WriteLine("{0}:{1} in {2}", n, ouelletConvexHull.GetResultsAsArrayOfPoint().Count(),
                    //    stopwatch.Elapsed);

                    //var pointsAsList = points.ToList();
                    //stopwatch.Restart();
                    //var monotoneChainConvexHull = MinimumEnclosure.MonotoneChain(pointsAsList);
                    //stopwatch.Stop();
                    //monotoneChainTotalTime += stopwatch.Elapsed;
                    //Presenter.ShowAndHang(new[] {points.ToList(), convexHull});
                    //Console.WriteLine("{0}:{1} in {2}", n, monotoneChainConvexHull.Count(),
                    //    stopwatch.Elapsed);

                    var miConvexHull = MinimumEnclosure.MIConvexHull2D(points);
                    var p0 = new PolygonLight(miConvexHull);
                    var p1 = new PolygonLight(convexHull);
                    var p2 = new PolygonLight(ouelletConvexHull.GetResultsAsArrayOfPoint()
                        .Select(p => new PointLight(p.X, p.Y)));
                    //monotoneChainConvexHull.Reverse();
                    //var p3 = new PolygonLight(monotoneChainConvexHull);
                    if (!p1.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)) ||
                        !p1.Length.IsPracticallySame(p0.Length, p0.Length * (1 - Constants.HighConfidence)))
                    {
                        Presenter.ShowAndHang(new List<PolygonLight> { p0, p1 });

                        //foreach (var chain in hullCands)
                        //{
                        //    var polyLine = new PolygonLight(chain.Values.Select(c => c.Item1));
                        //    Presenter.ShowAndHang(new List<PolygonLight> { p1, polyLine });
                        //}
                    }

                    if (!p2.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                    {
                        Presenter.ShowAndHang(new List<PolygonLight> { p0, p2 });
                    }

                    //if (!p3.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                    //{
                    //    Presenter.ShowAndHang(new List<PolygonLight> { p0, p3 });
                    //}
                }

                var campbellAverage = campbellTotalTime.TotalMilliseconds / repeat;
                var ouelletAverage = ouelletTotalTime.TotalMilliseconds / repeat;
                //var monotoneChainAverage = monotoneChainTotalTime.TotalMilliseconds / repeat;
                Console.WriteLine("Case #{0}", k);
                Console.WriteLine("1) {0} in {1} ", "Campbell", campbellAverage);
                Console.WriteLine("2) {0} in {1} ", "Ouellet", ouelletAverage);
                if (campbellAverage / ouelletAverage > 1)
                    Console.WriteLine("*** increased by {0} ", campbellAverage / ouelletAverage);
                else Console.WriteLine("reduced by {0} ", campbellAverage / ouelletAverage);
                //Console.WriteLine("3) {0} in {1} ", "Monotone Chain", monotoneChainAverage);

                averageTimes[-k].Add(("Campbell", campbellAverage));
                averageTimes[-k].Add(("Ouellet", ouelletAverage));
                //averageTimes[n].Add(("Monotone Chain", monotoneChainAverage));
            }
            #endregion

            Console.ReadKey();
        }



        internal static PointLight[] Issues(int index)
        {
            if (index == 1)
            {
                var points = new PointLight[]
                {
                    new PointLight(142.875, 128.549242659264),
                    new PointLight(15.8749971, 128.549242659264),
                    new PointLight(15.8749971, 123.0711471861),
                    new PointLight(15.8749971, 102.044504779806),
                    new PointLight(112.756631954527, 102.044504779806),
                    new PointLight(142.875, 102.044504779806),
                    new PointLight(142.875, 103.211199448627),
                    new PointLight(142.875, 128.549242659264),
                    new PointLight(15.8749971, 107.537039367956),
                    new PointLight(142.875, 107.537039367956),
                    new PointLight(142.875, 123.0711471861),
                    new PointLight(122.567413667611, 123.0711471861),
                    new PointLight(15.8749971, 123.0711471861),
                };
                return points;
            }
            if (index == 2)
            {
                var points = new PointLight[]
                {
                    new PointLight(142.875, 131.997024961786),
                    new PointLight(46.8831210665006, 131.997024961786),
                    new PointLight(15.8749971, 131.997024961786),
                    new PointLight(15.8749971, 118.546749549146),
                    new PointLight(15.8749971, 99.8873378986474),
                    new PointLight(15.8749971, 98.5973931147587),
                    new PointLight(142.875, 98.5973931147587),
                    new PointLight(142.875, 103.211199448627),
                    new PointLight(142.875, 131.997024961786),
                    new PointLight(15.8749971, 107.537039367956),
                    new PointLight(142.875, 107.537039367956),
                    new PointLight(142.875, 123.0711471861),
                    new PointLight(122.567413667611, 123.0711471861),
                    new PointLight(15.8749971, 123.0711471861),
                };
                return points;
            }
            if (index == 3)
            {
                var points = new PointLight[]
                {
                    new PointLight( 10.3022430542896, 21.5417418210693 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 54.6805850243065, -7.8218723162473 ),
                    new PointLight( 63.6185559352488, 5.60456709871792 ),
                    new PointLight( 68.9165840323107, 13.6722306510078 ),
                    new PointLight( 68.9441184177873, 13.7468649902332 ),
                    new PointLight( 68.9660285242253, 13.8062697930064 ),
                    new PointLight( 68.9614187102478, 13.9268515892982 ),
                    new PointLight( 68.9596476226058, 13.9731436885205 ),
                    new PointLight( 68.9201308292732, 14.0977295423655 ),
                    new PointLight( 68.8976081843702, 14.1687349836516 ),
                    new PointLight( 68.7814317154539, 14.3882266743166 ),
                    new PointLight( 68.7180013876881, 14.4783786611837 ),
                    new PointLight( 68.6139742972195, 14.6262205828687 ),
                    new PointLight( 68.3993757843515, 14.8768487625423 ),
                    new PointLight( 68.3928955619614, 14.8833448828499 ),
                    new PointLight( 68.3681792812265, 14.9081217597976 ),
                    new PointLight( 68.0511865256194, 15.1687364931754 ),
                    new PointLight( 67.8898214011948, 15.3014013030284 ),
                    new PointLight( 67.6783233209062, 15.4637490450954 ),
                    new PointLight( 67.3176079203105, 15.7406366594061 ),
                    new PointLight( 67.2593449661162, 15.7791872436254 ),
                    new PointLight( 28.1293415256119, 41.6701572594061 ),
                    new PointLight( 28.0710785661162, 41.7087078436254 ),
                    new PointLight( 27.0475863673268, 42.3859171985308 ),
                    new PointLight( 26.4195807988052, 42.7408253969716 ),
                    new PointLight( 25.8705652410062, 43.0273232468961 ),
                    new PointLight( 25.8295833151296, 43.0438082034231 ),
                    new PointLight( 25.8169648548083, 43.0477999545099 ),
                    new PointLight( 25.5149958033333, 43.1433241748213 ),
                    new PointLight( 25.2304772845461, 43.2043870704097 ),
                    new PointLight( 25.1009739246229, 43.2154334683917 ),
                    new PointLight( 24.983032523747, 43.2254909641465 ),
                    new PointLight( 24.8468525636534, 43.2125734249997 ),
                    new PointLight( 24.7787531477696, 43.2061136411042 ),
                    new PointLight( 24.6226705392973, 43.1467362211156 ),
                    new PointLight( 24.5765630925322, 43.1033420061374 ),
                    new PointLight( 24.5186370096942, 43.0488167683015 ),
                    new PointLight( 19.1648754121725, 35.0180301097642 ),
                    new PointLight( 10.3022430542896, 21.5417418210693 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 54.6805850243065, -7.8218723162473 ),
                    new PointLight( 63.6185559352488, 5.60456709871792 ),
                    new PointLight( 68.9165840323107, 13.6722306510078 ),
                    new PointLight( 68.9441184177873, 13.7468649902332 ),
                    new PointLight( 68.9660285242253, 13.8062697930064 ),
                    new PointLight( 68.9614187102478, 13.9268515892982 ),
                    new PointLight( 68.9596476226058, 13.9731436885205 ),
                    new PointLight( 68.9201308292732, 14.0977295423655 ),
                    new PointLight( 68.8976081843702, 14.1687349836516 ),
                    new PointLight( 68.7814317154539, 14.3882266743166 ),
                    new PointLight( 68.7180013876881, 14.4783786611837 ),
                    new PointLight( 68.6139742972195, 14.6262205828687 ),
                    new PointLight( 68.3993757843515, 14.8768487625423 ),
                    new PointLight( 68.3928955619614, 14.8833448828499 ),
                    new PointLight( 68.3681792812265, 14.9081217597976 ),
                    new PointLight( 68.0511865256194, 15.1687364931754 ),
                    new PointLight( 67.8898214011948, 15.3014013030284 ),
                    new PointLight( 67.6783233209062, 15.4637490450954 ),
                    new PointLight( 67.3176079203105, 15.7406366594061 ),
                    new PointLight( 67.2593449661162, 15.7791872436254 ),
                    new PointLight( 28.1293415256119, 41.6701572594061 ),
                    new PointLight( 28.0710785661162, 41.7087078436254 ),
                    new PointLight( 27.0475863673268, 42.3859171985308 ),
                    new PointLight( 26.4195807988052, 42.7408253969716 ),
                    new PointLight( 25.8705652410062, 43.0273232468961 ),
                    new PointLight( 25.8295833151296, 43.0438082034231 ),
                    new PointLight( 25.8169648548083, 43.0477999545099 ),
                    new PointLight( 25.5149958033333, 43.1433241748213 ),
                    new PointLight( 25.2304772845461, 43.2043870704097 ),
                    new PointLight( 25.1009739246229, 43.2154334683917 ),
                    new PointLight( 24.983032523747, 43.2254909641465 ),
                    new PointLight( 24.8468525636534, 43.2125734249997 ),
                    new PointLight( 24.7787531477696, 43.2061136411042 ),
                    new PointLight( 24.6226705392973, 43.1467362211156 ),
                    new PointLight( 24.5765630925322, 43.1033420061374 ),
                    new PointLight( 24.5186370096942, 43.0488167683015 ),
                    new PointLight( 19.1648754121725, 35.0180301097642 ),
                };
                return points;
            }
            if (index == 4)
            {
                var points = new PointLight[]
                {
                    new PointLight( 73.2883076120985, 11.5333759417026 ),
                    new PointLight( 73.279617943811, 11.5634130483227 ),
                    new PointLight( 73.2494320361919, 11.6677342861392 ),
                    new PointLight( 73.1900114675181, 11.7944488911759 ),
                    new PointLight( 73.1712893004617, 11.8221027873178 ),
                    new PointLight( 73.1114982830667, 11.910413470828 ),
                    new PointLight( 73.0878480560413, 11.9357157318846 ),
                    new PointLight( 73.0158287587745, 12.0127641329552 ),
                    new PointLight( 72.9053595488361, 12.0989810894029 ),
                    new PointLight( 66.8022999, 16.1371646 ),
                    new PointLight( 22.0941825467548, 45.7189796939573 ),
                    new PointLight( 21.5109739511639, 46.1048686105971 ),
                    new PointLight( 21.3884278203004, 46.1728299143281 ),
                    new PointLight( 21.3561705588626, 46.1846024532984 ),
                    new PointLight( 21.2568218434256, 46.2208595367864 ),
                    new PointLight( 21.1193917373618, 46.2477771800216 ),
                    new PointLight( 21.0873439823058, 46.2489565564523 ),
                    new PointLight( 20.9795247102683, 46.2529243162207 ),
                    new PointLight( 20.9486350570963, 46.2491963832899 ),
                    new PointLight( 20.8406666808378, 46.2361660816829 ),
                    new PointLight( 20.8115246470032, 46.2278757957675 ),
                    new PointLight( 20.7062352633284, 46.1979215973773 ),
                    new PointLight( 20.5795393466025, 46.1391300856415 ),
                    new PointLight( 20.4636971114297, 46.0612369590515 ),
                    new PointLight( 20.4426209036215, 46.0416179956696 ),
                    new PointLight( 20.3615645118966, 45.9661627635183 ),
                    new PointLight( 20.2756543066589, 45.8562517122073 ),
                    new PointLight( 18.6319823708994, 43.4516169290509 ),
                    new PointLight( 18.6272167, 43.4444414 ),
                    new PointLight( 6.0128562, 24.3798808 ),
                    new PointLight( 6.0128562, 24.3798808 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 35.0112980632504, -2.42156511211317 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 58.9699742, -10.6600127 ),
                    new PointLight( 71.1136491317534, 7.69318312617239 ),
                    new PointLight( 71.5843346, 8.4045479 ),
                    new PointLight( 73.1595648168057, 10.8647970251525 ),
                    new PointLight( 73.1994144919634, 10.9367627642941 ),
                    new PointLight( 73.2271388930218, 10.9868404793126 ),
                    new PointLight( 73.2369658645687, 11.0139404862566 ),
                    new PointLight( 73.2747061249512, 11.1180186971866 ),
                    new PointLight( 73.2802265981637, 11.1466781630145 ),
                    new PointLight( 73.3011034056027, 11.2550944935809 ),
                    new PointLight( 73.305672971526, 11.3946926532052 ),
                    new PointLight( 73.2883076120985, 11.5333759417026 ),
                    new PointLight( 6.20973220885308, 24.2496147215154 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 34.6141197923865, -2.15876597823326 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 58.7731002019635, -10.5297480411139 ),
                    new PointLight( 62.2586843662291, -5.27563683466277 ),
                    new PointLight( 72.9864711131986, 10.9793271789971 ),
                    new PointLight( 73.0539053927416, 11.1014630649511 ),
                    new PointLight( 73.1010753238373, 11.2329041918674 ),
                    new PointLight( 73.1104733778277, 11.2831037876829 ),
                    new PointLight( 73.1268185819021, 11.3704126976825 ),
                    new PointLight( 73.1304909618419, 11.5106045867891 ),
                    new PointLight( 73.1241706426974, 11.5582983410471 ),
                    new PointLight( 73.1120100507726, 11.6500258345824 ),
                    new PointLight( 73.0718347776597, 11.7852442550023 ),
                    new PointLight( 73.0109467553864, 11.9129298200261 ),
                    new PointLight( 72.9308515615462, 12.0299411340171 ),
                    new PointLight( 72.833516253832, 12.1333939987605 ),
                    new PointLight( 72.8075042735197, 12.1536483944415 ),
                    new PointLight( 72.721341150609, 12.2207396854653 ),
                    new PointLight( 63.0572297151433, 18.6151480269693 ),
                    new PointLight( 23.8689665151433, 44.5446665269693 ),
                    new PointLight( 21.694998649391, 45.9831058145347 ),
                    new PointLight( 21.6689913670982, 45.9975671668151 ),
                    new PointLight( 21.5707443171004, 46.052197395124 ),
                    new PointLight( 21.538960222552, 46.0639151356658 ),
                    new PointLight( 21.4374693384537, 46.1013313659829 ),
                    new PointLight( 21.2984531639914, 46.1292983766322 ),
                    new PointLight( 21.1571264037788, 46.1354114137187 ),
                    new PointLight( 21.1132796190049, 46.1304397288064 ),
                    new PointLight( 21.0169634688924, 46.1195166075692 ),
                    new PointLight( 20.8814169340615, 46.0820099377742 ),
                    new PointLight( 20.8370868964682, 46.0617904527539 ),
                    new PointLight( 20.7538234823251, 46.0238123380904 ),
                    new PointLight( 20.6373253761627, 45.9463531448209 ),
                    new PointLight( 20.5347941989726, 45.8515426529865 ),
                    new PointLight( 20.4704891776366, 45.7694693830149 ),
                    new PointLight( 20.4487486365048, 45.7417211461512 ),
                    new PointLight( 9.68262925522312, 29.5121205439766 ),
                };
                return points;
            }
            if (index == 5)
            {
                var points = new PointLight[]
                {
                    new PointLight( 71.9488478926165, 10.311991078012 ),
                    new PointLight( 71.9397011921, 10.37935578997 ),
                    new PointLight( 71.9369230088454, 10.3914348063282 ),
                    new PointLight( 71.8358203187923, 10.6924647745305 ),
                    new PointLight( 71.766859920807, 10.8685354696664 ),
                    new PointLight( 71.7526416518628, 10.8998987914239 ),
                    new PointLight( 71.6760318128378, 11.056932746588 ),
                    new PointLight( 71.6476672190557, 11.1059824301246 ),
                    new PointLight( 71.6424135316113, 11.1148450719655 ),
                    new PointLight( 71.4479539511623, 11.3832380717307 ),
                    new PointLight( 71.3721898450287, 11.4814354898779 ),
                    new PointLight( 71.357383125943, 11.4984893789466 ),
                    new PointLight( 71.1774444658322, 11.6741641284406 ),
                    new PointLight( 71.0366328498844, 11.7961182832839 ),
                    new PointLight( 71.0102465316816, 11.8173955347901 ),
                    new PointLight( 70.8676454, 11.9244294 ),
                    new PointLight( 69.9983771424799, 12.4995941871905 ),
                    new PointLight( 69.8403283683853, 12.6041696030434 ),
                    new PointLight( 66.475316274149, 14.8306817789378 ),
                    new PointLight( 27.7854729671318, 40.4304130449727 ),
                    new PointLight( 26.9161291194679, 41.0056278006562 ),
                    new PointLight( 22.1470982, 44.1611306 ),
                    new PointLight( 22.0266081750431, 44.2278946887084 ),
                    new PointLight( 21.9851284132721, 44.2508788195149 ),
                    new PointLight( 21.7572024217353, 44.3710671634698 ),
                    new PointLight( 21.7469334390058, 44.376482049431 ),
                    new PointLight( 21.7354809801053, 44.3814097496925 ),
                    new PointLight( 21.600122889704, 44.4306529768538 ),
                    new PointLight( 21.568084489512, 44.4415472363095 ),
                    new PointLight( 21.2770643393656, 44.5221629667043 ),
                    new PointLight( 21.2444315899884, 44.5291833154136 ),
                    new PointLight( 21.0324034767032, 44.5610585492388 ),
                    new PointLight( 20.7427378546548, 44.5703769483841 ),
                    new PointLight( 20.6342484940252, 44.5732572277517 ),
                    new PointLight( 20.5624865589828, 44.5751616646299 ),
                    new PointLight( 20.546523059722, 44.5739950344558 ),
                    new PointLight( 20.3861081805674, 44.5542739230854 ),
                    new PointLight( 20.3593116914652, 44.5501837186245 ),
                    new PointLight( 20.2407253095011, 44.5228908272487 ),
                    new PointLight( 20.1407726045705, 44.499885694942 ),
                    new PointLight( 20.0656183194298, 44.4825878493993 ),
                    new PointLight( 20.0271496096073, 44.4716567484051 ),
                    new PointLight( 19.855639271094, 44.4217830736414 ),
                    new PointLight( 19.8263771773612, 44.4104767916265 ),
                    new PointLight( 19.8198810162633, 44.4079476562332 ),
                    new PointLight( 19.6635671832587, 44.3350277694415 ),
                    new PointLight( 19.632811002503, 44.3201788104046 ),
                    new PointLight( 19.4992315831836, 44.2448347019869 ),
                    new PointLight( 19.4138497934148, 44.1966747709067 ),
                    new PointLight( 19.3715120983046, 44.1727940178166 ),
                    new PointLight( 19.344201851333, 44.1548907981439 ),
                    new PointLight( 19.166360055965, 44.0279001210109 ),
                    new PointLight( 19.0036306256763, 43.8761054405806 ),
                    new PointLight( 18.8778547030295, 43.7532924670821 ),
                    new PointLight( 18.8204752915631, 43.6972628314718 ),
                    new PointLight( 18.8089018443326, 43.6836415929485 ),
                    new PointLight( 18.6272167, 43.4444414 ),
                    new PointLight( 12.4667145737747, 34.1338413016557 ),
                    new PointLight( 6.0128562, 24.3798808 ),
                    new PointLight( 6.0128562, 24.3798808 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 38.4535097440705, -4.69915760573904 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 58.9699742, -10.6600127 ),
                    new PointLight( 58.9699742, -10.6600127 ),
                    new PointLight( 62.594790503742, -5.18169054213951 ),
                    new PointLight( 71.5843346, 8.4045479 ),
                    new PointLight( 71.5866064436728, 8.4079943265195 ),
                    new PointLight( 71.5870195061587, 8.40862094952305 ),
                    new PointLight( 71.7692847807011, 8.75713477765153 ),
                    new PointLight( 71.8221690291518, 8.86544158193592 ),
                    new PointLight( 71.8302884162703, 8.88585310592427 ),
                    new PointLight( 71.9068046266502, 9.12929578311794 ),
                    new PointLight( 71.9471732115869, 9.32575306913269 ),
                    new PointLight( 71.994166219279, 9.56500316652986 ),
                    new PointLight( 71.9949666955115, 9.57336786953529 ),
                    new PointLight( 71.9994291777771, 9.72000658167632 ),
                    new PointLight( 72.0000241535039, 9.91692069876896 ), //The issue
                    new PointLight( 71.9488478926165, 10.311991078012 ),
                    new PointLight( 6.0128562, 24.3798808 ),
                    new PointLight( 34.2831379, -1.9397667 ),
                    new PointLight( 38.1887242301612, -4.52395818315243 ),
                    new PointLight( 39.5788481, -5.4437551 ),
                    new PointLight( 58.9699742, -10.6600127 ),
                    new PointLight( 61.6999477721746, -6.53410025158757 ),
                    new PointLight( 66.7533031516917, 1.10322711271854 ),
                    new PointLight( 71.5843346, 8.4045479 ),
                    new PointLight( 71.5861933811868, 8.40736770351596 ),
                    new PointLight( 71.7707807114827, 8.7564153998967 ),
                    new PointLight( 71.8140496420333, 8.84503005794757 ),
                    new PointLight( 71.9080714581683, 9.1290122770965 ),
                    new PointLight( 71.9411003003893, 9.28975005656311 ),
                    new PointLight( 71.9933657430464, 9.55663846352443 ),
                    new PointLight( 71.9964551538457, 9.65815757192976 ),
                    new PointLight( 72.0006767437759, 9.91699466262915 ), //Part of the isue
                    new PointLight( 71.9499630394135, 10.3121601911007 ),
                    new PointLight( 71.9424793753545, 10.3672767736118 ),
                    new PointLight( 71.8375003335573, 10.693114306434 ),
                    new PointLight( 71.7810781897512, 10.8371721479089 ),
                    new PointLight( 71.6761283014128, 11.0569882290266 ),
                    new PointLight( 71.6529209065001, 11.0971197882838 ),
                    new PointLight( 71.4489853782237, 11.3840382586888 ),
                    new PointLight( 71.3869965641144, 11.4643816008092 ),
                    new PointLight( 71.1782286720445, 11.6750603596332 ),
                    new PointLight( 71.0630191680873, 11.7748410317778 ),
                    new PointLight( 70.8676454, 11.9244294 ),
                    new PointLight( 70.1564259165744, 12.3950187713377 ),
                    new PointLight( 66.8491310205647, 14.5833414777262 ),
                    new PointLight( 28.6577078695528, 39.8532853695965 ),
                    new PointLight( 28.0558544365547, 40.2515109696851 ),
                    new PointLight( 22.1470982, 44.1611306 ),
                    new PointLight( 21.943648651501, 44.2738629503213 ),
                    new PointLight( 21.766372884294, 44.3673427712944 ),
                    new PointLight( 21.7583858979062, 44.3715543491694 ),
                    new PointLight( 21.53604608932, 44.4524414957652 ),
                    new PointLight( 21.3096970887428, 44.515142617995 ),
                    new PointLight( 21.0324584596624, 44.562281716171 ),
                    new PointLight( 20.7186455072789, 44.5726068489187 ),
                    new PointLight( 20.6342648953542, 44.5748470661543 ),
                    new PointLight( 20.5784500582436, 44.576328294804 ),
                    new PointLight( 20.3325152023629, 44.5460935141637 ),
                    new PointLight( 20.2402813517993, 44.5248657102378 ),
                    new PointLight( 20.1625403608244, 44.5069728299574 ),
                    new PointLight( 20.1040870292522, 44.4935189503935 ),
                    new PointLight( 19.8556327441837, 44.4217997017224 ),
                    new PointLight( 19.8328733384591, 44.4130059270198 ),
                    new PointLight( 19.6020548217472, 44.3053298513677 ),
                    new PointLight( 19.4981597201695, 44.2467288794723 ),
                    new PointLight( 19.4317516629545, 44.2092711561415 ),
                    new PointLight( 19.3988223452762, 44.1906972374893 ),
                    new PointLight( 19.1658380657095, 44.0284621163861 ),
                    new PointLight( 18.9745028843436, 43.8499838739503 ),
                    new PointLight( 18.8766771689302, 43.7544626744893 ),
                    new PointLight( 18.8320487387937, 43.710884069995 ),
                    new PointLight( 18.6272167, 43.4444414 ),
                    new PointLight( 10.1198568321178, 30.5869463452765 ),

                };
                return points;
            }

            if (index == 6)
            {
                var points = new PointLight[]
                {
                    new PointLight(-28.3336641378862, 47.1057854611752),
                    new PointLight(-28.3284787131365, 47.1023544298585),
                    new PointLight(-27.9796660064341, 46.8715571014948),
                    new PointLight(-27.2733862743013, 46.4042362222663),
                    new PointLight(6.0128562, 24.3798808),
                    new PointLight(6.0128562, 24.3798808),
                    new PointLight(6.0128562, 24.3798808),
                    new PointLight(34.2831379, -1.9397667),
                    new PointLight(35.5408690910689, -2.77196395728638),
                    new PointLight(36.2028328758419, -3.20996251375289),
                    new PointLight(38.1887242301612, -4.52395818315243),
                    new PointLight(39.5788481, -5.4437551),
                    new PointLight(58.9699742, -10.6600127),
                    new PointLight(58.9699742, -10.6600127),
                    new PointLight(148.5824104, 36.4185083),
                    new PointLight(148.725676761512, 36.5266610857816),
                    new PointLight(149.871808330458, 37.3918838829931),
                    new PointLight(150.58814056105, 37.9326481312503),
                    new PointLight(150.731389383318, 38.0407876765331),
                    new PointLight(150.7314069, 38.0408009),
                    new PointLight(150.854335818374, 38.1716097810905),
                    new PointLight(151.960696737105, 39.3488904061497),
                    new PointLight(152.452412700985, 39.8721262395093),
                    new PointLight(152.5753416, 40.0029351),
                    new PointLight(152.628047871944, 40.0756827054077),
                    new PointLight(153.260523608386, 40.9486546233107),
                    new PointLight(153.3659362, 41.0941499),
                    new PointLight(153.365941866652, 41.0941593090112),
                    new PointLight(153.45862964027, 41.2480597628367),
                    new PointLight(154.0611372, 42.2484742),
                    new PointLight(155.001394827861, 44.3842389202384),
                    new PointLight(155.14604095677, 44.7127979128907),
                    new PointLight(155.1460498, 44.712818),
                    new PointLight(155.450712481485, 45.9318642259421),
                    new PointLight(155.537758969253, 46.2801631770239),
                    new PointLight(155.668328700905, 46.8026116036466),
                    new PointLight(155.7988984, 47.3250599),
                    new PointLight(155.81236399325, 47.5040594302653),
                    new PointLight(156.0008824, 50.0100542),
                    new PointLight(155.746190776033, 52.6905534508082),
                    new PointLight(155.7461887, 52.6905753),
                    new PointLight(155.699252670196, 52.8638363198087),
                    new PointLight(155.136019979934, 54.9429697853482),
                    new PointLight(155.0421479, 55.2894919),
                    new PointLight(154.966606951203, 55.4523278717016),
                    new PointLight(154.891065957795, 55.6151639395662),
                    new PointLight(153.90904233613, 57.7320127906091),
                    new PointLight(153.9090331, 57.7320327),
                    new PointLight(153.807058144455, 57.8797607726087),
                    new PointLight(152.583357955253, 59.6524986908058),
                    new PointLight(152.379408, 59.9479549),
                    new PointLight(152.128459771705, 60.2046934871024),
                    new PointLight(150.748244108536, 61.6167561331132),
                    new PointLight(150.4972959, 61.8734947),
                    new PointLight(148.3168434, 63.4532476),
                    new PointLight(145.9007622, 64.641795),
                    new PointLight(143.3185849, 65.4049125),
                    new PointLight(128.23025811178, 67.8207964146677),
                    new PointLight(127.512579826988, 67.9227278482468),
                    new PointLight(125.036200379656, 68.1813358300569),
                    new PointLight(120.067208004877, 68.7002360536887),
                    new PointLight(119.355251989725, 68.7603683199008),
                    new PointLight(117.912661059628, 68.8113005802573),
                    new PointLight(112.006339378289, 69.0197967417166),
                    new PointLight(111.300107752776, 69.0381302805618),
                    new PointLight(-13.4064290079294, 69.004920724286),
                    new PointLight(-13.5109114762393, 68.9919471398904),
                    new PointLight(-13.5446710480061, 68.9877549099476),
                    new PointLight(-13.6785757096054, 68.9491618775298),
                    new PointLight(-13.8048475928049, 68.8900898155988),
                    new PointLight(-13.920435092173, 68.8120281312236),
                    new PointLight(-13.997057303078, 68.7406883587188),
                    new PointLight(-14.0225565800419, 68.7169465663885),
                    new PointLight(-14.1084613408133, 68.6070318796464),
                    new PointLight(-24.9457418552349, 52.2320538964607),

                };
                return points;
            }

            if (index == 7)
            {
                var points = new[]
                {
                    new PointLight( 5, -173.961356988035 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -0.401681582128174 ),new PointLight( 55, 8.80095195939652E-18 ),new PointLight( 5, 8.80095195939652E-18 ),new PointLight( 5, -173.961356988035 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -0.401681582128174 ),new PointLight( 55, 8.80095195939652E-18 ),new PointLight( 5, 8.80095195939652E-18 ),new PointLight( 5, 2.43534526529407E-17 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -1.11150855464817 ),new PointLight( 55, 2.43534526529407E-17 ),new PointLight( 5, -3.53594679391949 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -169.613345208986 ),new PointLight( 55, 3.99059533464848E-17 ),new PointLight( 5, 3.99059533464848E-17 ),new PointLight( 5, -169.552457624742 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -172.365393094424 ),new PointLight( 55, -2.63460724538329 ),new PointLight( 55, 5.5458454040029E-17 ),new PointLight( 5, 5.5458454040029E-17 ),new PointLight( 5, 7.10109547335733E-17 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 7.10109547335733E-17 ),new PointLight( 5, 8.65634554271174E-17 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 8.65634554271174E-17 ),new PointLight( 5, -4.85111679583696 ),new PointLight( 5, -170.148883829852 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 1.02115956120662E-16 ),new PointLight( 5, 1.02115956120662E-16 ),new PointLight( 5, 1.17668456814206E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -14.9633856827241 ),new PointLight( 55, 1.17668456814206E-16 ),new PointLight( 5, 1.3322095750775E-16 ),new PointLight( 5, -175 ),new PointLight( 15.878676, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -168.671210986804 ),new PointLight( 55, 1.3322095750775E-16 ),new PointLight( 5, 1.48773458201294E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -167.93237456528 ),new PointLight( 55, 1.48773458201294E-16 ),new PointLight( 5, 1.64325958894838E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -167.193538143757 ),new PointLight( 55, -7.80646286310852 ),new PointLight( 55, 1.64325958894838E-16 ),new PointLight( 5, 1.79878459588383E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -12.8458802595348 ),new PointLight( 55, -10.3609961636518 ),new PointLight( 55, 1.79878459588383E-16 ),new PointLight( 5, 1.95430960281927E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 1.95430960281927E-16 ),new PointLight( 5, 2.10983460975471E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -164.977028879185 ),new PointLight( 55, 2.10983460975471E-16 ),new PointLight( 5, 2.26535961669015E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 2.26535961669015E-16 ),new PointLight( 5, 2.42088462362559E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 2.42088462362559E-16 ),new PointLight( 5, 2.57640963056104E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 2.57640963056104E-16 ),new PointLight( 5, 2.73193463749648E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 2.73193463749648E-16 ),new PointLight( 5, 2.88745964443192E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 2.88745964443192E-16 ),new PointLight( 5, 3.04298465136736E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.04298465136736E-16 ),new PointLight( 5, 3.1985096583028E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.1985096583028E-16 ),new PointLight( 5, 3.35403466523824E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.35403466523824E-16 ),new PointLight( 5, 3.50955967217369E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.50955967217369E-16 ),new PointLight( 5, 3.66508467910913E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.66508467910913E-16 ),new PointLight( 5, 3.82060968604457E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.82060968604457E-16 ),new PointLight( 5, 3.97613469298001E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 3.97613469298001E-16 ),new PointLight( 5, 4.13165969991545E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.13165969991545E-16 ),new PointLight( 5, 4.28718470685089E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.28718470685089E-16 ),new PointLight( 5, 4.44270971378634E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.44270971378634E-16 ),new PointLight( 5, 4.59823472072178E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.59823472072178E-16 ),new PointLight( 5, 4.75375972765722E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.75375972765722E-16 ),new PointLight( 5, 4.90928473459266E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 4.90928473459266E-16 ),new PointLight( 5, 5.0648097415281E-16 ),new PointLight( 5, -175 ),new PointLight( 46.358676, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.0648097415281E-16 ),new PointLight( 5, 5.22033474846354E-16 ),new PointLight( 5, -175 ),new PointLight( 47.628676, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.22033474846354E-16 ),new PointLight( 5, 5.37585975539899E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, -149.15995290271 ),new PointLight( 55, 5.37585975539899E-16 ),new PointLight( 5, 5.53138476233443E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.53138476233443E-16 ),new PointLight( 5, 5.68690976926987E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.68690976926987E-16 ),new PointLight( 5, 5.84243477620531E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.84243477620531E-16 ),new PointLight( 5, 5.99795978314075E-16 ),new PointLight( 5, -175 ),new PointLight( 55, -175 ),new PointLight( 55, 5.99795978314075E-16 ),new PointLight( 0, -86.226482423467 ),new PointLight( 0, -87.0795267449547 ),new PointLight( 5, -146.467885707883 ),new PointLight( 55, -146.467885707883 ),new PointLight( 60, -87.0795267449547 ),new PointLight( 60, -86.226482423467 ),new PointLight( 55, -28.5321098720532 ),new PointLight( 5, -28.5321098720532 ),new PointLight( 0, -84.2429584973432 ),new PointLight( 0, -91.1452338857474 ),new PointLight( 5, -144.863142156169 ),new PointLight( 55, -144.863142156169 ),new PointLight( 60, -91.1452338857474 ),new PointLight( 60, -84.2429584973432 ),new PointLight( 55, -30.1368581634177 ),new PointLight( 5, -30.1368581634177 ),new PointLight( 0, -82.2594345712193 ),new PointLight( 0, -92.323795927203 ),new PointLight( 5, -143.960861078778 ),new PointLight( 14.2516539221584, -143.960861078778 ),new PointLight( 54.2516536377869, -143.960861078778 ),new PointLight( 55, -143.960861078778 ),new PointLight( 60, -92.323795927203 ),new PointLight( 60, -82.2594345712193 ),new PointLight( 55, -31.0391342606237 ),new PointLight( 5, -31.0391342606237 ),new PointLight( 0, -82.3096012171783 ),new PointLight( 0, -93.5023579686585 ),new PointLight( 5, -143.396976586535 ),new PointLight( 55, -143.396976586535 ),new PointLight( 60, -93.5023579686585 ),new PointLight( 60, -81.4146651913322 ),new PointLight( 55, -31.6030140793735 ),new PointLight( 5, -31.6030140793735 ),new PointLight( 0, -81.4146651913322 ),new PointLight( 0, -82.3872191896282 ),new PointLight( 0, -94.680920010114 ),new PointLight( 5, -142.833092094293 ),new PointLight( 55, -142.833092094293 ),new PointLight( 60, -94.680920010114 ),new PointLight( 60, -80.5852670401221 ),new PointLight( 55, -32.1668938981232 ),new PointLight( 5, -32.1668938981232 ),new PointLight( 0, -80.5852670401221 ),new PointLight( 0, -82.4648371620782 ),new PointLight( 0, -95.3777711480749 ),new PointLight( 5, -142.40420018272 ),new PointLight( 12.8377711811079, -142.40420018272 ),new PointLight( 52.837770359461, -142.40420018272 ),new PointLight( 55, -142.40420018272 ),new PointLight( 60, -95.3777711480749 ),new PointLight( 60, -79.7558688889121 ),new PointLight( 55, -32.5957886687596 ),new PointLight( 47.162225840539, -32.5957886687596 ),new PointLight( 7.16222881889208, -32.5957886687596 ),new PointLight( 5, -32.5957886687596 ),new PointLight( 0, -79.7558688889121 ),new PointLight( 0, -82.5424551345281 ),new PointLight( 0, -96.009873967945 ),new PointLight( 5, -142.026909149552 ),new PointLight( 55, -142.026909149552 ),new PointLight( 60, -96.009873967945 ),new PointLight( 60, -78.926470737702 ),new PointLight( 55, -32.9730854403083 ),new PointLight( 5, -32.9730854403083 ),new PointLight( 0, -78.926470737702 ),new PointLight( 0, -78.1862422125757 ),new PointLight( 0, -96.641976787815 ),new PointLight( 5, -141.649618116385 ),new PointLight( 55, -141.649618116385 ),new PointLight( 60, -96.641976787815 ),new PointLight( 60, -78.1290727283303 ),new PointLight( 55, -33.350382211857 ),new PointLight( 5, -33.350382211857 ),new PointLight( 0, -78.1290727283303 ),new PointLight( 0, -78.264197527888 ),new PointLight( 0, -97.2740796076851 ),new PointLight( 5, -141.314244041304 ),new PointLight( 55, -141.314244041304 ),new PointLight( 60, -97.2740796076851 ),new PointLight( 60, -77.6252948684851 ),new PointLight( 55, -33.6857602056376 ),new PointLight( 5, -33.6857602056376 ),new PointLight( 0, -77.625294868485 ),new PointLight( 0, -78.3421528432003 ),new PointLight( 0, -97.9061824275552 ),new PointLight( 5, -141.04797162458 ),new PointLight( 55, -141.04797162458 ),new PointLight( 60, -97.9061824275552 ),new PointLight( 60, -77.1215170086398 ),new PointLight( 55, -33.9520335412508 ),new PointLight( 5, -33.9520335412508 ),new PointLight( 0, -77.1215170086398 ),new PointLight( 0, -78.4201081585126 ),new PointLight( 0, -98.5382852474252 ),new PointLight( 5, -140.781699207856 ),new PointLight( 55, -140.781699207856 ),new PointLight( 60, -98.5382852474252 ),new PointLight( 60, -76.6177391487945 ),new PointLight( 55, -34.2183068768639 ),new PointLight( 5, -34.2183068768639 ),new PointLight( 0, -76.6177391487945 ),new PointLight( 0, -78.4980634738249 ),new PointLight( 0, -99.0261215528803 ),new PointLight( 5, -140.515426791132 ),new PointLight( 55, -140.515426791132 ),new PointLight( 60, -99.0261215528803 ),new PointLight( 60, -76.1139612889492 ),new PointLight( 55, -34.484580212477 ),new PointLight( 5, -34.484580212477 ),new PointLight( 0, -76.1139612889492 ),new PointLight( 0, -78.5760187891372 ),new PointLight( 0, -99.4386447159682 ),new PointLight( 5, -140.259212705043 ),new PointLight( 55, -140.259212705043 ),new PointLight( 60, -99.4386447159682 ),new PointLight( 60, -75.6101834291039 ),new PointLight( 55, -34.7407949826661 ),new PointLight( 5, -34.7407949826661 ),new PointLight( 0, -75.6101834291039 ),new PointLight( 0, -78.6539741044495 ),new PointLight( 0, -99.8511678790561 ),new PointLight( 5, -140.034479850181 ),new PointLight( 55, -140.034479850181 ),new PointLight( 60, -99.8511678790561 ),new PointLight( 60, -75.1064055692586 ),new PointLight( 55, -34.9655277867674 ),new PointLight( 5, -34.9655277867674 ),new PointLight( 0, -75.1064055692586 ),new PointLight( 0, -78.7319294197618 ),new PointLight( 0, -100.263691042144 ),new PointLight( 5, -139.809746995319 ),new PointLight( 55, -139.809746995319 ),new PointLight( 60, -100.263691042144 ),new PointLight( 60, -74.6026277094134 ),new PointLight( 55, -35.1902605908686 ),new PointLight( 5, -35.1902605908686 ),new PointLight( 0, -74.6026277094133 ),new PointLight( 0, -74.4670332904994 ),new PointLight( 0, -100.676214205232 ),new PointLight( 5, -139.585014140457 ),new PointLight( 55, -139.585014140457 ),new PointLight( 60, -100.676214205232 ),new PointLight( 60, -74.1922572679287 ),new PointLight( 55, -35.4149933949699 ),new PointLight( 5, -35.4149933949699 ),new PointLight( 0, -74.1922572679287 ),new PointLight( 0, -74.5952907067315 ),new PointLight( 0, -101.08873736832 ),new PointLight( 5, -139.360281285595 ),new PointLight( 15, -139.360281285595 ),new PointLight( 54.7203453127005, -139.360281285595 ),new PointLight( 55, -139.360281285595 ),new PointLight( 60, -101.08873736832 ),new PointLight( 60, -73.8488255206133 ),new PointLight( 55, -35.6397261990712 ),new PointLight( 15, -35.6397261990712 ),new PointLight( 14.7203454189692, -35.6397261990712 ),new PointLight( 5, -35.6397261990712 ),new PointLight( 0, -73.8488255206133 ),new PointLight( 0, -74.7235481229636 ),new PointLight( 0, -101.501260531408 ),new PointLight( 5, -139.135548430733 ),new PointLight( 55, -139.135548430733 ),new PointLight( 60, -101.501260531408 ),new PointLight( 60, -73.505393773298 ),new PointLight( 55, -35.8644590031724 ),new PointLight( 15, -35.8644590031724 ),new PointLight( 14.6544224002888, -35.8644590031724 ),new PointLight( 5, -35.8644590031724 ),new PointLight( 0, -73.505393773298 ),new PointLight( 0, -74.8518055391957 ),new PointLight( 0, -101.913783694496 ),new PointLight( 5, -138.910815575871 ),new PointLight( 55, -138.910815575871 ),new PointLight( 60, -101.913783694496 ),new PointLight( 60, -73.1619620259826 ),new PointLight( 55, -36.0891918072737 ),new PointLight( 5, -36.0891918072737 ),new PointLight( 0, -73.1619620259826 ),new PointLight( 0, -74.9800629554278 ),new PointLight( 0, -102.326306857584 ),new PointLight( 5, -138.686082721009 ),new PointLight( 55, -138.686082721009 ),new PointLight( 60, -102.326306857584 ),new PointLight( 60, -72.8185302786673 ),new PointLight( 55, -36.313924611375 ),new PointLight( 5, -36.313924611375 ),new PointLight( 0, -72.8185302786673 ),new PointLight( 0, -75.1083203716599 ),new PointLight( 0, -102.626779626193 ),new PointLight( 5, -138.461349866147 ),new PointLight( 55, -138.461349866147 ),new PointLight( 60, -102.626779626193 ),new PointLight( 60, -72.4750985313519 ),new PointLight( 55, -36.5386574154762 ),new PointLight( 5, -36.5386574154762 ),new PointLight( 0, -72.4750985313519 ),new PointLight( 0, -75.236577787892 ),new PointLight( 0, -102.915379683027 ),new PointLight( 5, -138.236617011285 ),new PointLight( 55, -138.236617011285 ),new PointLight( 60, -102.915379683027 ),new PointLight( 60, -72.1316667840366 ),new PointLight( 55, -36.7633902195775 ),new PointLight( 5, -36.7633902195775 ),new PointLight( 0, -72.1316667840366 ),new PointLight( 0, -75.3648352041241 ),new PointLight( 0, -103.203979739862 ),new PointLight( 5, -138.011884156423 ),new PointLight( 55, -138.011884156423 ),new PointLight( 60, -103.203979739862 ),new PointLight( 60, -71.7882350367212 ),new PointLight( 55, -36.9881230236788 ),new PointLight( 15, -36.9881230236788 ),new PointLight( 14.3248073068866, -36.9881230236788 ),new PointLight( 5, -36.9881230236788 ),new PointLight( 0, -71.7882350367212 ),new PointLight( 0, -75.4930926203562 ),new PointLight( 0, -103.492579796697 ),new PointLight( 5, -137.787151301561 ),new PointLight( 55, -137.787151301561 ),new PointLight( 60, -103.492579796697 ),new PointLight( 60, -71.4448032894059 ),new PointLight( 55, -37.21285582778 ),new PointLight( 5, -37.21285582778 ),new PointLight( 0, -71.4448032894059 ),new PointLight( 0, -75.6213500365883 ),new PointLight( 0, -103.781179853531 ),new PointLight( 5, -137.562418446699 ),new PointLight( 55, -137.562418446699 ),new PointLight( 60, -103.781179853531 ),new PointLight( 60, -71.1013715420905 ),new PointLight( 55, -37.4375886318813 ),new PointLight( 5, -37.4375886318813 ),new PointLight( 0, -71.1013715420905 ),new PointLight( 0, -71.1064560983703 ),new PointLight( 0, -104.069779910366 ),new PointLight( 5, -137.337685591837 ),new PointLight( 55, -137.337685591837 ),new PointLight( 60, -104.069779910366 ),new PointLight( 60, -70.823504008734 ),new PointLight( 55, -37.6623214359825 ),new PointLight( 5, -37.6623214359825 ),new PointLight( 0, -70.823504008734 ),new PointLight( 0, -71.2945521697917 ),new PointLight( 0, -104.358379967201 ),new PointLight( 5, -137.112952736975 ),new PointLight( 55, -137.112952736975 ),new PointLight( 60, -104.358379967201 ),new PointLight( 60, -70.5800652983963 ),new PointLight( 55, -37.8870542400838 ),new PointLight( 5, -37.8870542400838 ),new PointLight( 0, -70.5800652983963 ),new PointLight( 0, -71.4826482412131 ),new PointLight( 0, -104.646980024035 ),new PointLight( 5, -136.888219882113 ),new PointLight( 55, -136.888219882113 ),new PointLight( 60, -104.646980024035 ),new PointLight( 60, -70.3366265880586 ),new PointLight( 55, -38.1117870441851 ),new PointLight( 15, -38.1117870441851 ),new PointLight( 13.9951922134845, -38.1117870441851 ),new PointLight( 5, -38.1117870441851 ),new PointLight( 0, -70.3366265880586 ),new PointLight( 0, -71.6707443126345 ),new PointLight( 0, -104.93558008087 ),new PointLight( 5, -136.663487027251 ),new PointLight( 55, -136.663487027251 ),new PointLight( 60, -104.93558008087 ),new PointLight( 60, -100.971373585897 ),new PointLight( 60, -70.0931878777209 ),new PointLight( 55, -38.3365198482863 ),new PointLight( 5, -38.3365198482863 ),new PointLight( 0, -70.0931878777209 ),new PointLight( 0, -71.8588403840559 ),new PointLight( 0, -105.224180137705 ),new PointLight( 5, -136.438754172389 ),new PointLight( 55, -136.438754172389 ),new PointLight( 60, -105.224180137705 ),new PointLight( 60, -69.8497491673832 ),new PointLight( 55, -38.5612526523876 ),new PointLight( 5, -38.5612526523876 ),new PointLight( 0, -69.8497491673832 ),new PointLight( 0, -72.0469364554773 ),new PointLight( 0, -105.51278019454 ),new PointLight( 5, -136.214021317527 ),new PointLight( 55, -136.214021317527 ),new PointLight( 60, -105.51278019454 ),new PointLight( 60, -84.4052864733266 ),new PointLight( 60, -69.6063104570455 ),new PointLight( 55, -38.7859854564889 ),new PointLight( 5, -38.7859854564889 ),new PointLight( 0, -69.6063104570455 ),new PointLight( 0, -72.2350325268987 ),new PointLight( 0, -105.728323487669 ),new PointLight( 5, -135.989288462665 ),new PointLight( 55, -135.989288462665 ),new PointLight( 60, -105.728323487669 ),new PointLight( 60, -93.3447056276542 ),new PointLight( 60, -69.3628717467078 ),new PointLight( 55, -39.0107182605901 ),new PointLight( 15, -39.0107182605901 ),new PointLight( 13.7315001387628, -39.0107182605901 ),new PointLight( 5, -39.0107182605901 ),new PointLight( 0, -69.3628717467078 ),new PointLight( 0, -72.4231285983201 ),new PointLight( 0, -105.933413830435 ),new PointLight( 5, -135.764555607803 ),new PointLight( 55, -135.764555607803 ),new PointLight( 60, -105.933413830435 ),new PointLight( 60, -69.1194330363701 ),new PointLight( 55, -39.2354510646914 ),new PointLight( 15, -39.2354510646914 ),new PointLight( 13.6655771200823, -39.2354510646914 ),new PointLight( 5, -39.2354510646914 ),new PointLight( 0, -69.1194330363701 ),new PointLight( 0, -72.6112246697415 ),new PointLight( 0, -106.1385041732 ),new PointLight( 5, -135.539822752941 ),new PointLight( 55, -135.539822752941 ),new PointLight( 60, -106.1385041732 ),new PointLight( 60, -88.9071326198136 ),new PointLight( 60, -68.8759943260324 ),new PointLight( 55, -39.4601838687927 ),new PointLight( 5, -39.4601838687927 ),new PointLight( 0, -68.8759943260324 ),new PointLight( 0, -72.7993207411629 ),new PointLight( 0, -106.343594515966 ),new PointLight( 5, -135.315089898079 ),new PointLight( 55, -135.315089898079 ),new PointLight( 60, -106.343594515966 ),new PointLight( 60, -68.6325556156948 ),new PointLight( 55, -39.6849166728939 ),new PointLight( 5, -39.6849166728939 ),new PointLight( 0, -68.6325556156948 ),new PointLight( 0, -72.9874168125843 ),new PointLight( 0, -106.548684858732 ),new PointLight( 5, -135.090357043217 ),new PointLight( 55, -135.090357043217 ),new PointLight( 60, -106.548684858732 ),new PointLight( 60, -81.771536476053 ),new PointLight( 60, -68.3891169053571 ),new PointLight( 55, -39.9096494769952 ),new PointLight( 15, -39.9096494769952 ),new PointLight( 13.467808064041, -39.9096494769952 ),new PointLight( 5, -39.9096494769952 ),new PointLight( 0, -68.3891169053571 ),new PointLight( 0, -73.1755128840057 ),new PointLight( 0, -106.753775201498 ),new PointLight( 5, -134.865624188355 ),new PointLight( 15, -134.865624188355 ),new PointLight( 53.4018844380769, -134.865624188355 ),new PointLight( 55, -134.865624188355 ),new PointLight( 60, -106.753775201498 ),new PointLight( 60, -68.1456781950194 ),new PointLight( 55, -40.1343822810965 ),new PointLight( 15, -40.1343822810965 ),new PointLight( 13.4018850453606, -40.1343822810965 ),new PointLight( 5, -40.1343822810965 ),new PointLight( 0, -68.1456781950194 )
                };
                return points;
            }

            return null;
        }
    }
}