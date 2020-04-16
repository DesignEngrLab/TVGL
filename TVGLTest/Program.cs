using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.Voxelization;


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
            Trace.Listeners.Add(writer);
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
            //var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            var fileNames = dir.GetFiles("*").ToArray();
            //Casing = 18
            //SquareSupport = 75
            for (var i = 0; i < fileNames.Count() - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i+1].FullName;
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                var solid = (TessellatedSolid)IO.Open(filename);
                TestSlice(solid);
            }

            //Console.WriteLine("Completed.");
            //  Console.ReadKey();
        }

        public static void TestSlice(TessellatedSolid ts, Flat flat = null)
        {
            Presenter.ShowAndHang(ts);
            if (!(flat is null))
                Slice.SliceOnInfiniteFlat(ts, flat, out var solids, out var contactData);
            else
            {
                Slice.SliceOnInfiniteFlat(ts, new Flat((ts.XMax + ts.XMin) / 2, Vector3.UnitX), out var solidsX,
                    out var contactDataX);
                Slice.SliceOnInfiniteFlat(ts, new Flat((ts.YMax + ts.YMin) / 2, Vector3.UnitY), out var solidsY,
    out var contactDataY);
                Slice.SliceOnInfiniteFlat(ts, new Flat((ts.ZMax + ts.ZMin) / 2, Vector3.UnitZ), out var solidsZ,
    out var contactDataZ);
            }
        }
        public static void TestCrossSectionSolidToTessellated(TessellatedSolid ts)
        {
            //Presenter.ShowAndHang(new ImplicitSolid());
            var xs = CrossSectionSolid.CreateFromTessellatedSolid(ts, CartesianDirections.ZPositive, 10);
            //Presenter.ShowAndHang(ts);
            Presenter.ShowAndHang(xs);
            stopwatch.Restart();
            TessellatedSolid ts1 = xs.ConvertToTessellatedSolidMarchingCubes();
            //ts1.SimplifyFlatPatches();
            Console.WriteLine("time elapsed = {0}", stopwatch.Elapsed);
            //ts1.SimplifyFlatPatches();
            Presenter.ShowAndHang(ts1);
            IO.Save(xs, ts.FileName + "XSections", FileType.TVGL);
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
            var silhouette = TVGL.TwoDimensional.Silhouette.CreateSilhouette(ts, new Vector3(0.5, 0, 0.5));
            Presenter.ShowAndHang(silhouette);
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