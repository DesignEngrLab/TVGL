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

                        var points = new PointLight[n];
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
                        var ouelletConvexHull = new OuelletConvexHull.OuelletConvexHull(windowsPoints);
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
    }
}