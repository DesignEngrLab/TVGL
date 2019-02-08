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
            var points = new []
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
            var nums = new[] { 3, 10 };
            var repeat = 10;
            for (int k = 0; k < 6; k++) 
            {
                foreach (var n in nums)
                {
                    averageTimes[n] = new List<(string MethodName, double AverageTimeInMilliseconds)>();
                    var campbellTotalTime = TimeSpan.Zero;
                    var ouelletTotalTime = TimeSpan.Zero;
                    var monotoneChainTotalTime = TimeSpan.Zero;
                    for (int i = 0; i < repeat; i++)
                    {
                        var random = new Random();

                        var points = Issues(3); 
                        //var points = new PointLight[n];
                        //for (int j = 0; j < n; j++)
                        //    points[j] = new PointLight(100 * random.NextDouble(), 100 * random.NextDouble());
                        stopwatch.Restart();
                        var convexHull = MinimumEnclosure.ConvexHull2D(points, out var hullCands);
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

                        var pointsAsList = points.ToList();
                        stopwatch.Restart();
                        var monotoneChainConvexHull = MinimumEnclosure.MonotoneChain(pointsAsList);
                        stopwatch.Stop();
                        monotoneChainTotalTime += stopwatch.Elapsed;
                        //Presenter.ShowAndHang(new[] {points.ToList(), convexHull});
                        //Console.WriteLine("{0}:{1} in {2}", n, monotoneChainConvexHull.Count(),
                        //    stopwatch.Elapsed);

                        var miConvexHull = MinimumEnclosure.MIConvexHull2D(points);
                        var p0 = new PolygonLight(miConvexHull);
                        var p1 = new PolygonLight(convexHull);
                        var p2 = new PolygonLight(ouelletConvexHull.GetResultsAsArrayOfPoint()
                            .Select(p => new PointLight(p.X, p.Y)));
                        monotoneChainConvexHull.Reverse();
                        var p3 = new PolygonLight(monotoneChainConvexHull);
                        if (!p1.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                        {
                            Presenter.ShowAndHang(new List<PolygonLight>{p0, p1});
                            
                            foreach (var chain in hullCands)
                            {
                                var polyLine = new PolygonLight(chain.Values);
                                Presenter.ShowAndHang(new List<PolygonLight> { p1, polyLine });
                            }
                        }
                        if (!p2.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                        {
                            Presenter.ShowAndHang(new List<PolygonLight> { p0, p2 });
                        }
                        if (!p3.Area.IsPracticallySame(p0.Area, p0.Area * (1 - Constants.HighConfidence)))
                        {
                            Presenter.ShowAndHang(new List<PolygonLight> { p0, p3 });
                        }
                    }
                    var campbellAverage = campbellTotalTime.TotalMilliseconds / repeat;
                    var ouelletAverage = ouelletTotalTime.TotalMilliseconds / repeat;
                    var monotoneChainAverage = monotoneChainTotalTime.TotalMilliseconds / repeat;
                    Console.WriteLine("N = {0}", n);
                    Console.WriteLine("1) {0} in {1} ", "Campbell", campbellAverage);
                    Console.WriteLine("2) {0} in {1} ", "Ouellet", ouelletAverage);
                    Console.WriteLine("3) {0} in {1} ", "Monotone Chain", monotoneChainAverage);

                    averageTimes[n].Add(("Campbell", campbellAverage));
                    averageTimes[n].Add(("Ouellet", ouelletAverage));
                    averageTimes[n].Add(("Monotone Chain", monotoneChainAverage));
                }

                for (var n = 0; n < nums.Length; n++)
                {
                    nums[n] *= 10;
                }
            }
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

            return null;
        }
    }
}