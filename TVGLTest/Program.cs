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
            //var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            var fileNames = dir.GetFiles("*SquareSupport*").ToArray();
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
                using (fileStream = File.OpenRead(filename))
                    IO.Open(fileStream, filename, out ts);
                if (ts.Errors != null) continue;
                Color color = new Color(KnownColors.AliceBlue);
                ts.SolidColor = new Color(KnownColors.MediumSeaGreen)
                {
                    Af = 0.25f
                };
                //Presenter.ShowAndHang(ts);
                TestVoxelization(ts);
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
        public static void TestVoxelization(TessellatedSolid ts)
        {
            var res = 600;

            stopwatch.Restart();
            var vsa = new VoxelizedSolid(ts, res);
            stopwatch.Stop();
            stopwatch.Restart();
            var vsb = new VoxelizedSolidByte(ts, res);
            stopwatch.Stop();
            Console.WriteLine("voxelization    : {0}", stopwatch.Elapsed);
            VoxelizedSolid resultsA = null;
            VoxelizedSolidByte resultB = null;

            #region test cartesian slicing
            for (int i = -3; i < 4; i++)
            {
                if (i == 0) continue;
                stopwatch.Restart();
                var testResults = vsa.SliceOnFlat((CartesianDirections)i, vsa.VoxelsPerSide[Math.Abs(i) - 1] / 3);
                stopwatch.Stop();
                testResults.Item1.SolidColor = new Color(KnownColors.Magenta);
                Console.WriteLine("Slicing in {0}   : {1}", (CartesianDirections)i,
                    stopwatch.Elapsed);
                stopwatch.Restart();
                var testresultB = vsb.SliceOnFlat((CartesianDirections)i, vsa.VoxelsPerSide[Math.Abs(i) - 1] / 3);
                stopwatch.Stop();
                testResults.Item1.SolidColor = new Color(KnownColors.Magenta);
                Console.WriteLine("Slicing in {0}   : {1}", (CartesianDirections)i,
                    stopwatch.Elapsed);
                // Presenter.ShowAndHang(testResults.Item1, testResults.Item2);

            }
            #endregion
            #region test arbitrary slicing
            var r = new Random(1);
            for (int i = 0; i < 10; i++)
            {
                var normal = new[] { r.NextDouble() - .5, r.NextDouble() - .5, r.NextDouble() - .5 };
                normal.normalizeInPlace();
                stopwatch.Restart();
                var testResults = vsa.SliceOnFlat(new Flat(vsa.Center, normal));
                stopwatch.Stop();
                testResults.Item1.SolidColor = new Color(KnownColors.Magenta);
                Console.WriteLine("Slicing in {0}   : {1}", i, stopwatch.Elapsed);
                normal = new[] { r.NextDouble() - .5, r.NextDouble() - .5, r.NextDouble() - .5 };
                normal.normalizeInPlace();
                stopwatch.Restart();
                var testResultsB = vsb.SliceOnFlat(new Flat(vsa.Center, normal));
                stopwatch.Stop();
                testResults.Item1.SolidColor = new Color(KnownColors.Magenta);
                Console.WriteLine("Slicing in {0}   : {1}", i, stopwatch.Elapsed);
                //Presenter.ShowAndHang(testResults.Item1, testResults.Item2);
            }
            #endregion
            #region test cartesian drafting
            for (int i = -3; i < 4; i++)
            {
                if (i == 0) continue;

                vsa.UpdateToAllSparse();
                stopwatch.Restart();
                resultsA = vsa.DraftToNewSolid((CartesianDirections)i);
                stopwatch.Stop();
                Console.WriteLine("Sparse Drafting in {0}   : {1}", (CartesianDirections)i,
                    stopwatch.Elapsed);
                
                // Presenter.ShowAndHang(testResult);

                vsa.UpdateToAllDense();
                stopwatch.Restart();
                resultB = vsb.DraftToNewSolid((CartesianDirections)i);
                stopwatch.Stop();
                Console.WriteLine("Dense Drafting in {0}   : {1}", (CartesianDirections)i,
                    stopwatch.Elapsed);
                // Presenter.ShowAndHang(testResult);
            }
            #endregion
            vsa.UpdateToAllSparse();


            var newBounds = new[]{ ts.Bounds[0].subtract(ts.Center.multiply(0.5)),
                ts.Bounds[1].subtract(ts.Center.multiply(0.5)) };
            var offsetVsA = new VoxelizedSolid(ts, res, newBounds);
            Console.WriteLine("Created offset solid");
            var offsetVsB = new VoxelizedSolidByte(ts, res, newBounds);
            Console.WriteLine("Created offset solid");
            // Presenter.ShowAndHang(offsetVs);

            #region Boolean testing
            stopwatch.Restart();
            resultsA = vsa.UnionToNewSolid(offsetVsA);
            stopwatch.Stop();
            Console.WriteLine("union with offset      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultsA = vsa.IntersectToNewSolid(offsetVsA);
            stopwatch.Stop();
            Console.WriteLine("intersect with offset      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultsA = vsa.SubtractToNewSolid(offsetVsA);
            stopwatch.Stop();
            Console.WriteLine("subtract with offset      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultsA = vsa.InvertToNewSolid();
            stopwatch.Stop();
            Console.WriteLine("invert original      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultsA.Union(vsa);
            stopwatch.Stop();
            Console.WriteLine("union invert with original      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultsA.Invert();
            stopwatch.Stop();
            Console.WriteLine("invert previous \"all\"      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);


            stopwatch.Restart();
            resultB = vsb.UnionToNewSolid(offsetVsB);
            stopwatch.Stop();
            Console.WriteLine("union with offset      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultB = vsb.IntersectToNewSolid(offsetVsB);
            stopwatch.Stop();
            Console.WriteLine("intersect with offset      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultB = vsb.SubtractToNewSolid(offsetVsB);
            stopwatch.Stop();
            Console.WriteLine("subtract with offset      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultB = vsb.InvertToNewSolid();
            stopwatch.Stop();
            Console.WriteLine("invert original      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultB.Union(vsb);
            stopwatch.Stop();
            Console.WriteLine("union invert with original      : {0}", stopwatch.Elapsed);
            //Presenter.ShowAndHang(testResult);
            stopwatch.Restart();
            resultB = resultB.InvertToNewSolid();
            stopwatch.Stop();
            Console.WriteLine("invert previous \"all\"      : {0}", stopwatch.Elapsed);
            // Presenter.ShowAndHang(testResult);
            #endregion


            stopwatch.Restart();
            var fullBlock = VoxelizedSolid.CreateFullBlock(vsa);
            stopwatch.Stop();
            Console.WriteLine("Creating Full block   : {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            var fullBlockB = new VoxelizedSolidByte(vsb.VoxelsPerSide, res, vsb.VoxelSideLength, vsb.Bounds, 255);
            stopwatch.Stop();
            Console.WriteLine("Creating Full block   : {0}", stopwatch.Elapsed);

            #region Erode testing
            stopwatch.Restart();
            resultsA = fullBlock.DirectionalErodeToConstraintToNewSolid(vsa, new[] { 0, .471, .882 }, 0, 0, "flat");
            stopwatch.Stop();
            Console.WriteLine("sparse eroding full block to constraint 0-flat       : {0}", stopwatch.Elapsed);
            Presenter.ShowAndHang(resultsA);


            stopwatch.Restart();
            resultB = fullBlockB.ErodeToNewSolid(vsb, new[] { 0, .471, .882 }, 0, 0, "flat");
            stopwatch.Stop();
            Console.WriteLine("sparse eroding full block to constraint 0-flat       : {0}", stopwatch.Elapsed);
            Presenter.ShowAndHang(resultsA);





            vsa.UpdateToAllDense();
            stopwatch.Restart();
            resultsA = fullBlock.DirectionalErodeToConstraintToNewSolid(vsa, new[] { 0, .471, .882 }, 0, 0, "flat");
            stopwatch.Stop();
            Console.WriteLine("dense eroding full block to constraint 0-flat       : {0}", stopwatch.Elapsed);
            Presenter.ShowAndHang(resultsA);


            stopwatch.Restart();
            resultsA = fullBlock.DirectionalErodeToConstraintToNewSolid(vsa, new[] { 0, .471, .882 }, 10, 2, "flat");
            stopwatch.Stop();
            Console.WriteLine("eroding full block to constraint        : {0}", stopwatch.Elapsed);
            Presenter.ShowAndHang(resultsA);
            #endregion
        }

        public static void TestSegmentation(TessellatedSolid ts)
        {
            var obb = MinimumEnclosure.OrientedBoundingBox(ts);
            var startTime = DateTime.Now;

            var averageNumberOfSteps = 500;

            //Do the average # of slices slices for each direction on a box (l == w == h).
            //Else, weight the average # of slices based on the average obb distance
            var obbAverageLength = (obb.Dimensions[0] + obb.Dimensions[1] + obb.Dimensions[2]) / 3;
            //Set step size to an even increment over the entire length of the solid
            var stepSize = obbAverageLength / averageNumberOfSteps;

            foreach (var direction in obb.Directions)
            {
                Dictionary<int, double> stepDistances;
                Dictionary<int, double> sortedVertexDistanceLookup;
                //var segments = DirectionalDecomposition.UniformDirectionalSegmentation(ts, direction,
                //    stepSize, out stepDistances, out sortedVertexDistanceLookup);
                //foreach (var segment in segments)
                //{
                //    var vertexLists = segment.DisplaySetup(ts);
                //    Presenter.ShowVertexPathsWithSolid(vertexLists, new List<TessellatedSolid>() { ts });
                //}
            }

            // var segments = AreaDecomposition.UniformDirectionalSegmentation(ts, obb.Directions[2].multiply(-1), stepSize);
            var totalTime = DateTime.Now - startTime;
            Debug.WriteLine(totalTime.TotalMilliseconds + " Milliseconds");
            //CheckAllObjectTypes(ts, segments);
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