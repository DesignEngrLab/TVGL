using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
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
            //var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            var fileNames = dir.GetFiles("*.ply").ToArray();
            //Casing = 18
            //SquareSupport = 75
            for (var i = 0; i < fileNames.Count(); i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                TessellatedSolid ts;
                if (!File.Exists(filename)) continue;
                IO.Open(filename, out ts);
                Console.WriteLine("v={0}, f={1}", ts.NumberOfVertices, ts.NumberOfFaces);
                IO.Save(ts, filename + ".tvgl");
                IO.Open(filename + ".tvgl", out TessellatedSolid ts2);
                Console.WriteLine("v={0}, f={1}", ts2.NumberOfVertices, ts2.NumberOfFaces);


            }

            Console.WriteLine("Completed.");
            Console.ReadKey();
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
        public static void TestVoxelization(TessellatedSolid ts)
        {
            var res = 8;

            //stopwatch.Restart();
            //var vs = new VoxelizedSolid(ts, res);
            //stopwatch.Stop();
            Console.WriteLine("Original voxelization: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            var vs_dense = new VoxelizedSolid(ts, res);
            stopwatch.Stop();
            Console.WriteLine("Dense voxelization    : {0}", stopwatch.Elapsed);
            //var vs_cut = vs_dense.CutSolid(VoxelDirections.XNegative, 100);
            //vs_cut.Item1.SolidColor = new Color(KnownColors.Magenta);
            //Presenter.ShowAndHang(vs_cut.Item1, vs_cut.Item2);

            var vs_cut2 = vs_dense.SliceOnFlat(CartesianDirections.XNegative, 87);
            vs_cut2.Item1.SolidColor = new Color(KnownColors.Magenta);
            Presenter.ShowAndHang(vs_cut2.Item1, vs_cut2.Item2);

            //Console.WriteLine(ts.SurfaceArea);
            //Console.WriteLine(vs_dense.SurfaceArea);
            //Console.WriteLine();

            //Console.WriteLine(ts.Volume);
            //Console.WriteLine(vs.Volume);
            //Console.WriteLine(vs_dense.Volume);

            //Presenter.ShowAndHang(vs_dense);


            //stopwatch.Restart();
            //var bb = vs.CreateBoundingSolid();
            //stopwatch.Stop();
            //Console.WriteLine("Original bounding solid: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            var bb_dense = vs_dense.CreateBoundingSolid();
            stopwatch.Stop();
            Console.WriteLine("Dense bounding solid   : {0}", stopwatch.Elapsed);


            //stopwatch.Restart();
            //var erd = bb.ErodeToNewSolid(vs, new[] { -1, -2, -3.0 }, 0D, 20, false, true, "ball");
            //stopwatch.Stop();
            //Console.WriteLine("Original erosion: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            var erd_dense = bb_dense.ErodeToNewSolid(vs_dense, new[] { 0, .471, -.882 }, 0, 0, "flat");
            stopwatch.Stop();
            Console.WriteLine("Dense erosion   : {0}", stopwatch.Elapsed);
            erd_dense.SolidColor = new Color(KnownColors.Magenta);

            //Presenter.ShowAndHang(erd);
            Presenter.ShowAndHang(erd_dense, vs_dense);


            //stopwatch.Restart();
            //var neg = vs.InvertToNewSolid();
            //stopwatch.Stop();
            //Console.WriteLine("Original inversion: {0}", stopwatch.Elapsed);

            //stopwatch.Restart();
            //var neg_dense = vs_dense.InvertToNewSolid();
            //stopwatch.Stop();
            //Console.WriteLine("Dense inversion   : {0}", stopwatch.Elapsed);


            //stopwatch.Restart();
            //var draft = vs.ExtrudeToNewSolid(VoxelDirections.ZPositive);
            //stopwatch.Stop();
            //Console.WriteLine("Original draft: {0}", stopwatch.Elapsed);

            //stopwatch.Restart();
            //var draft_dense = vs_dense.DraftToNewSolid(VoxelDirections.ZPositive);
            //stopwatch.Stop();
            //Console.WriteLine("Dense draft   : {0}", stopwatch.Elapsed);


            //stopwatch.Restart();
            //var intersect = neg.IntersectToNewSolid(draft);
            //stopwatch.Stop();
            //Console.WriteLine("Original intersect: {0}", stopwatch.Elapsed);

            //stopwatch.Restart();
            //var intersect_dense = neg_dense.IntersectToNewSolid(draft_dense);
            //stopwatch.Stop();
            //Console.WriteLine("Dense intersect   : {0}", stopwatch.Elapsed);


            //var draft1 = vs.ExtrudeToNewSolid(VoxelDirections.YNegative);
            //stopwatch.Restart();
            //var union = draft.UnionToNewSolid(draft1);
            //stopwatch.Stop();
            //Console.WriteLine("Original union: {0}", stopwatch.Elapsed);

            //var draft1_dense = vs_dense.DraftToNewSolid(VoxelDirections.YNegative);
            //stopwatch.Restart();
            //var union_dense = draft_dense.UnionToNewSolid(draft1_dense);
            //stopwatch.Stop();
            //Console.WriteLine("Dense union   : {0}", stopwatch.Elapsed);


            //stopwatch.Restart();
            //var subtract = draft.SubtractToNewSolid(draft1);
            //stopwatch.Stop();
            //Console.WriteLine("Original subtract: {0}", stopwatch.Elapsed);

            //stopwatch.Restart();
            //var subtract_dense = draft_dense.SubtractToNewSolid(draft1_dense);
            //stopwatch.Stop();
            //Console.WriteLine("Dense subtract   : {0}", stopwatch.Elapsed);
        }


        private static void CheckAllObjectTypes(TessellatedSolid ts, IEnumerable<DirectionalDecomposition.DirectionalSegment> segments)
        {
            var faces = new HashSet<PolygonalFace>(ts.Faces);
            var vertices = new HashSet<Vertex>(ts.Vertices);
            var edges = new HashSet<Edge>(ts.Edges);


            foreach (var face in faces)
            {
                face.Color = new Color(KnownColors.Gray);
            }

            foreach (var segment in segments)
            {
                foreach (var face in segment.ReferenceFaces)
                {
                    faces.Remove(face);
                }
                foreach (var edge in segment.ReferenceEdges)
                {
                    edges.Remove(edge);
                }
                foreach (var vertex in segment.ReferenceVertices)
                {
                    vertices.Remove(vertex);
                }
            }

            ts.HasUniformColor = false;
            //Turn the remaining faces red
            foreach (var face in faces)
            {
                face.Color = new Color(KnownColors.Red);
            }
            Presenter.ShowAndHang(ts);

            //Make sure that every face, edge, and vertex is accounted for
            //Assert.That(!edges.Any(), "edges missed");
            //Assert.That(!faces.Any(), "faces missed");
            //Assert.That(!vertices.Any(), "vertices missed");
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