using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;
using StarMathLib;

namespace TVGLPresenterDX
{
    internal class Program
    {

        static readonly Stopwatch stopwatch = new Stopwatch();

        private static readonly string[] FileNames = {
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
        "../../../TestFiles/drillparts.amf",  //Edge/face relationship contains errors
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
        "../../../TestFiles/brace.stl",  //Convex hull fails in MIconvexHull
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
            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            DirectoryInfo dir;
            if (Directory.Exists("../../../../TestFiles"))
            {
                //x64
                dir = new DirectoryInfo("../../../../TestFiles");
            }
            else
            {
                //x86
                dir = new DirectoryInfo("../../../TestFiles");
            }
            var random = new Random();
            var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*").ToArray();
            //Casing = 18
            //SquareSupport = 75
            for (var i = 77; i < fileNames.Count(); i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                TessellatedSolid ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    IO.Open(fileStream, filename, out ts);
                if (ts.Errors != null) continue;
                Color color = new Color(KnownColors.AliceBlue);
                ts.SolidColor = new Color(KnownColors.MediumSeaGreen)
                {
                    Af = 0.25f
                };
                //Presenter.ShowAndHang(ts);
                TestCrossSectionSolidToTessellated(ts);
                //TestSlice(ts);
                // var stopWatch = new Stopwatch();
                // Color color = new Color(KnownColors.AliceBlue);
                //ts[0].SetToOriginAndSquare(out var backTransform);
                //ts[0].Transform(new double[,]
                //  {
                //{1,0,0,-(ts[0].XMax + ts[0].XMin)/2},
                //{0,1,0,-(ts[0].YMax+ts[0].YMin)/2},
                //{0,0,1,-(ts[0].ZMax+ts[0].ZMin)/2},
                //  });
                // stopWatch.Restart();
                //PresenterShowAndHang(ts);
                // Console.WriteLine("Voxelizing Tesselated File " + filename);
                //  var vs1 = new VoxelizedSolid(ts[0], VoxelDiscretization.Coarse, false);//, bounds);
                // Presenter.ShowAndHang(vs1);
                //TestVoxelization(ts[0]);
                //bounds = vs1.Bounds;
                // return;
            }

            Console.WriteLine("Completed.");
            //  Console.ReadKey();
        }

        public static void TestSlice(TessellatedSolid ts, Flat flat = null)
        {
            if (!(flat is null))
                Slice.OnInfiniteFlat(ts, flat, out var solids, out var contactData);
            else
            {
                Slice.OnInfiniteFlat(ts, new Flat((ts.XMax + ts.XMin) / 2, new[] { 1.0, 0, 0 }), out var solidsX,
                    out var contactDataX);
                Slice.OnInfiniteFlat(ts, new Flat((ts.YMax + ts.YMin) / 2, new[] { 0, 1.0, 0 }), out var solidsY,
    out var contactDataY);
                Slice.OnInfiniteFlat(ts, new Flat((ts.ZMax + ts.ZMin) / 2, new[] { 0, 0, 1.0 }), out var solidsZ,
    out var contactDataZ);
            }
        }
        public static void TestCrossSectionSolidToTessellated(TessellatedSolid ts)
        {
            //Presenter.ShowAndHang(new ImplicitSolid());
            var xs = CrossSectionSolid.CreateFromTessellatedSolid(ts, CartesianDirections.ZPositive,100);
            //Presenter.ShowAndHang(ts);
            Presenter.ShowAndHang(xs);
            stopwatch.Restart();
            TessellatedSolid ts1 = xs.ConvertToTessellatedSolidMarchingCubes();
            Console.WriteLine("time elapsed = {0}", stopwatch.Elapsed);
            //ts1.SimplifyFlatPatches();
            Presenter.ShowAndHang(ts1);
            return;
            //var res = 600;
            //stopwatch.Restart();
            //var vsa = new VoxelizedSolid(ts, res);
            //stopwatch.Stop();
            //stopwatch.Restart();
            //var ts2 = vsa.ConvertToTessellatedSolidMarchingCubes(100);
            //Presenter.ShowAndHang(vsa, ts2);
        }

        public static void TestSilhouette(TessellatedSolid ts)
        {
            var silhouette = TVGL.Silhouette.Run(ts, new[] { 0.5, 0.0, 0.5 });
            Presenter.ShowAndHang(silhouette);
        }

        private static void TestOBB(string InputDir)
        {
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles();
            var numVertices = new List<int>();
            var data = new List<double[]>();
            foreach (var fileInfo in fis)
            {
                try
                {
                    IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name, out TessellatedSolid tessellatedSolid);
                    List<double> times, volumes;
                    MinimumEnclosure.OrientedBoundingBox_Test(tessellatedSolid, out times, out volumes);//, out VolumeData2);
                    data.Add(new[] { tessellatedSolid.ConvexHull.Vertices.Count(), tessellatedSolid.Volume,
                            times[0], times[1],times[2], volumes[0],  volumes[1], volumes[2] });
                }
                catch { }
            }
            // TVGLTest.ExcelInterface.PlotEachSeriesSeperately(VolumeData1, "Edge", "Angle", "Volume");
            TVGLTest.ExcelInterface.CreateNewGraph(new[] { data }, "", "Methods", "Volume", new[] { "PCA", "ChanTan" });
        }

        private static void TestSimplify(TessellatedSolid ts)
        {
            ts.Simplify(.9);
            Debug.WriteLine("number of vertices = " + ts.NumberOfVertices);
            Debug.WriteLine("number of edges = " + ts.NumberOfEdges);
            Debug.WriteLine("number of faces = " + ts.NumberOfFaces);
            TVGL.Presenter.ShowAndHang(ts);
        }
    }
}