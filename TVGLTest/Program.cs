using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarMathLib;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;


namespace TVGL_Test
{
    internal class Program
    {
        private static readonly string[] FileNames = {
           "../../../TestFiles/Binary.stl",
         //   "../../../TestFiles/ABF.ply",
        //     "../../../TestFiles/Beam_Boss.STL",
        //     "../../../TestFiles/Beam_Clean.STL",

        //"../../../TestFiles/bigmotor.amf",
        //"../../../TestFiles/DxTopLevelPart2.shell",
        //"../../../TestFiles/Candy.shell",
        //"../../../TestFiles/amf_Cube.amf",
        //"../../../TestFiles/train.3mf",
        //"../../../TestFiles/Castle.3mf",
        //"../../../TestFiles/Raspberry Pi Case.3mf",
       //"../../../TestFiles/shark.ply",
       //"../../../TestFiles/bunnySmall.ply",
       // "../../../TestFiles/cube.ply",
       // "../../../TestFiles/airplane.ply",
       // "../../../TestFiles/TXT - G5 support de carrosserie-1.STL.ply",
        "../../../TestFiles/Tetrahedron.STL",
       // "../../../TestFiles/off_axis_box.STL",
       //    "../../../TestFiles/Wedge.STL",
       // "../../../TestFiles/Mic_Holder_SW.stl",
       // "../../../TestFiles/Mic_Holder_JR.stl",
       // "../../../TestFiles/3_bananas.amf",
       // "../../../TestFiles/drillparts.amf",  //Edge/face relationship contains errors
       // "../../../TestFiles/wrenchsns.amf", //convex hull edge contains a concave edge outside of tolerance
       // "../../../TestFiles/hdodec.off",
       // "../../../TestFiles/tref.off",
       // "../../../TestFiles/mushroom.off",
       // "../../../TestFiles/vertcube.off",
       // "../../../TestFiles/trapezoid.4d.off",
       // "../../../TestFiles/ABF.STL",
       // "../../../TestFiles/Pump-1repair.STL",
       // "../../../TestFiles/Pump-1.STL",
       // "../../../TestFiles/SquareSupportWithAdditionsForSegmentationTesting.STL",
       // "../../../TestFiles/Beam_Clean.STL",
       // "../../../TestFiles/Square_Support.STL",
       // "../../../TestFiles/Aerospace_Beam.STL",
       // "../../../TestFiles/Rook.amf",
       //"../../../TestFiles/bunny.ply",

       // "../../../TestFiles/piston.stl",
       // "../../../TestFiles/Z682.stl",
       // "../../../TestFiles/sth2.stl",
       // "../../../TestFiles/Cuboide.stl", //Note that this is an assembly 
       // "../../../TestFiles/new/5.STL",
       //"../../../TestFiles/new/2.stl", //Note that this is an assembly 
       // "../../../TestFiles/new/6.stl", //Note that this is an assembly  //breaks in slice at 1/2 y direction
       //"../../../TestFiles/new/4.stl", //breaks because one of its faces has no normal
       // "../../../TestFiles/radiobox.stl",
       // "../../../TestFiles/brace.stl",  //Convex hull fails in MIconvexHull
       // "../../../TestFiles/G0.stl",
       // "../../../TestFiles/GKJ0.stl",
       // "../../../TestFiles/testblock2.stl",
       // "../../../TestFiles/Z665.stl",
       // "../../../TestFiles/Casing.stl", //breaks because one of its faces has no normal
       // "../../../TestFiles/mendel_extruder.stl",

       //"../../../TestFiles/MV-Test files/holding-device.STL",
       //"../../../TestFiles/MV-Test files/gear.STL"
        };

        [STAThread]
        private static void Main(string[] args)
        {
            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            var dir = new DirectoryInfo("../../../TestFiles");
            var fileNames = dir.GetFiles("*.stl");
            for (var i = 0; i < FileNames.Count(); i++) //fileNames.Count(); i++)
            {
                var filename = FileNames[i]; //fileNames[i].FullName;
                // Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                List<TessellatedSolid> ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    ts = IO.Open(fileStream, filename);
                //filename += "1.ply";
                //using (fileStream = File.OpenWrite(filename))
                //    IO.Save(fileStream, ts, FileType.PLY_Binary);
                //using (fileStream = File.OpenRead(filename))
                //    ts = IO.Open(fileStream, filename);


                //TestPolygon(ts[0]);
                //TestSegmentation(ts[0]);
                //Presenter.ShowAndHang(ts);
                TestVoxelization(ts[0]);
                //TestOctreeVoxelization(ts[0]);

                //TestSilhouette(ts[0]);
                //TestAdditiveVolumeEstimate(ts[0]);
            }

            Console.WriteLine("Completed.");
            // Console.ReadKey();
        }


        public static void TestVoxelization(TessellatedSolid ts)
        {
            var startTime = DateTime.Now;
            var voxelSpace1 = new VoxelizedSolid(ts, 8, true);
            var totalTime1 = DateTime.Now - startTime;
            //Console.WriteLine("{0}\t\t|  {1} verts  |  {2} ms  |  {3} voxels", ts.FileName, ts.NumberOfVertices,
            //    totalTime1.TotalSeconds,
            //    voxelSpace1.VoxelIDHashSet.Count);
            Presenter.ShowAndHangVoxelization(ts, voxelSpace1);
            //return;
            //startTime = DateTime.Now;
            //var voxelSpace2 = new VoxelSpace();
            //voxelSpace2.VoxelizeSolid(ts, 100);
            //var totalTime2 = DateTime.Now - startTime;
            //Console.WriteLine("{0}\t\t|  {1} verts  |  {2} ms  |  {3} voxels", ts.FileName, ts.NumberOfVertices,
            //    totalTime2.TotalSeconds,
            //    voxelSpace2.Voxels.Count);
            //Presenter.ShowAndHangVoxelization(ts, voxelSpace2);

            //int counter1 = 0;
            //foreach (var key in voxelSpace1.VoxelIDHashSet)
            //{
            //    if (voxelSpace2.VoxelIDHashSet.Contains(key))
            //    {
            //        voxelSpace1.Voxels.Remove(key);
            //    }
            //    else counter1++;
            //}
            //Console.WriteLine(counter1+" voxels in new that are not in old.");
            // Presenter.ShowAndHangVoxelization(ts, voxelSpace1);
            /*
             Voxel voxelOfInterest = null;
            int maxYalongMinZ = Int32.MinValue;
            int minZ = Int32.MaxValue;
            foreach (var voxel in voxelSpace1.Voxels)
            {
                if (voxel.Value.Index[2] < minZ)
                {
                    minZ = voxel.Value.Index[2];
                    maxYalongMinZ = voxel.Value.Index[1];
                }
                else if (voxel.Value.Index[2] == minZ)
                {
                    if (voxel.Value.Index[1] > maxYalongMinZ)
                    {
                        maxYalongMinZ = voxel.Value.Index[1];
                        voxelOfInterest = voxel.Value;
                    }
                }
            }
            Presenter.ShowAndHangVoxelization(ts, new List<Voxel>() {voxelOfInterest});

            var counter2 = 0;
            foreach (var key in voxelSpace2.VoxelIDHashSet)
            {
                if (voxelSpace1.VoxelIDHashSet.Contains(key))
                {
                    voxelSpace2.Voxels.Remove(key);
                }
                else counter2++;
            }
            if (counter2 > 0)
            {
                Console.WriteLine(counter2 + " voxels in old that are not in new.");
                Presenter.ShowAndHangVoxelization(ts, voxelSpace2);
            }*/
        }

        public static void TestOctreeVoxelization(TessellatedSolid ts)
        {
            //Level 0 has one voxel. 
            //Level 1 is the outermost octree, with 8 voxels.
            //Level 2 has 64 voxels and so on...
            //Settings max Level to 3, with set level 0, 1, and 2.
            const int maxLevel = 6;
            var octree = new VoxelizingOctree(maxLevel);
            octree.GenerateOctree(ts);
            Presenter.ShowVoxelization(ts, octree, maxLevel - 1, CellStatus.Intersecting);
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
                var segments = DirectionalDecomposition.UniformDirectionalSegmentation(ts, direction,
                    stepSize, out stepDistances, out sortedVertexDistanceLookup);
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
            Assert.That(!edges.Any(), "edges missed");
            Assert.That(!faces.Any(), "faces missed");
            Assert.That(!vertices.Any(), "vertices missed");
        }

        public static void TestSilhouette(TessellatedSolid ts)
        {
            var silhouette = TVGL.Silhouette.Run(ts, new[] { 0.5, 0.0, 0.5 });
            Presenter.ShowAndHang(silhouette);
        }

        private static void TestPolygon(TessellatedSolid ts)
        {
            ContactData contactData;
            Slice.GetContactData(ts, new Flat(10, new[] { 1.0, 0, 0 }),
                out contactData);
            throw new NotImplementedException();
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
                    var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                    foreach (var tessellatedSolid in ts)
                    {
                        List<double> times, volumes;
                        MinimumEnclosure.OrientedBoundingBox_Test(tessellatedSolid, out times, out volumes);//, out VolumeData2);
                        data.Add(new[] { tessellatedSolid.ConvexHull.Vertices.Count(), tessellatedSolid.Volume,
                            times[0], times[1],times[2], volumes[0],  volumes[1], volumes[2] });
                    }
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

        //private static void TestClassification(TessellatedSolid ts)
        //{
        //    TesselationToPrimitives.Run(ts);
        //}

        //private static void TestOBB(TessellatedSolid ts, string filename)
        //{
        //    //var obb = MinimumEnclosure.Find_via_PCA_Approach(ts);
        //    //var obb = MinimumEnclosure.Find_via_ChanTan_AABB_Approach(ts);
        //    //var obb = MinimumEnclosure.Find_via_MC_ApproachOne(ts);\
        //    //MiscFunctions.IsConvexHullBroken(ts);
        //    List<List<double[]>> VolumeData1;
        //  //  List<List<double[]>> VolumeData2;
        //    var obb = MinimumEnclosure.OrientedBoundingBox_Test(ts, out VolumeData1);//, out VolumeData2);
        //    //var obb = MinimumEnclosure.Find_via_BM_ApproachOne(ts);
        //    //TVGLTest.ExcelInterface.PlotEachSeriesSeperately(VolumeData1, "Edge", "Angle", "Volume");
        ////   TVGLTest.ExcelInterface.CreateNewGraph(VolumeData1, "", "Methods", "Volume", new []{"PCA", "ChanTan"});
        //}

        private static void TestInsideSolid(TessellatedSolid ts1, TessellatedSolid ts2)
        {
            var now = DateTime.Now;
            Console.WriteLine("start...");
            List<Vertex> insideVertices1;
            List<Vertex> outsideVertices1;
            List<Vertex> insideVertices2;
            List<Vertex> outsideVertices2;
            MiscFunctions.FindSolidIntersections(ts2, ts1, out insideVertices1, out outsideVertices1, out insideVertices2, out outsideVertices2, true);
            //var vertexInQuestion = new Vertex(new[] {0.0, 0.0, 0.0});
            //var isVertexInside = MinimumEnclosure.IsVertexInsideSolid(ts, vertexInQuestion);
            //ToDo: Run test multiple times and look for vertices that change. Get visual and determine cause of error.
            //ToDo: Also, check if boundary function works 
            Console.WriteLine("Is the Solid inside the Solid?");
            Console.WriteLine();
            Console.WriteLine("end...Time Elapsed = " + (DateTime.Now - now));
            Console.ReadLine();
        }


        private static void TestXSections(TessellatedSolid ts)
        {
            var now = DateTime.Now;
            Debug.WriteLine("start...");
            var crossAreas = new double[3][,];
            var maxSlices = 100;
            var delta = Math.Max((ts.Bounds[1][0] - ts.Bounds[0][0]) / maxSlices,
                Math.Max((ts.Bounds[1][1] - ts.Bounds[0][1]) / maxSlices,
                    (ts.Bounds[1][2] - ts.Bounds[0][2]) / maxSlices));
            //Parallel.For(0, 3, i =>
            var greatestDeltas = new List<double>();
            var greatestDeltaLocations = new List<double>();
            var areaData = new List<List<double[]>>();
            for (int i = 0; i < 3; i++)
            {
                //var max = ts.Bounds[1][i];
                //var min = ts.Bounds[0][i];
                //var numSteps = (int)Math.Ceiling((max - min) / delta);
                var coordValues = ts.Vertices.Select(v => v.Position[i]).Distinct().OrderBy(x => x).ToList();
                var numValues = new List<double>();
                var offset = 0.000000001;
                foreach (var coordValue in coordValues)
                {
                    if (coordValues[0] == coordValue)
                    {
                        //Only Add increment forward
                        numValues.Add(coordValue + offset);
                    }
                    else if (coordValues.Last() == coordValue)
                    {
                        //Only Add increment back
                        numValues.Add(coordValue - offset);
                    }
                    else
                    {
                        //Add increment forward and back
                        numValues.Add(coordValue + offset);
                        numValues.Add(coordValue - offset);
                    }
                }
                coordValues = numValues.OrderBy(x => x).ToList();
                var numSteps = coordValues.Count;
                var direction = new double[3];
                direction[i] = 1.0;
                crossAreas[i] = new double[numSteps, 2];
                var greatestDelta = 0.0;
                var previousArea = 0.0;
                var greatestDeltaLocation = 0.0;
                var dataPoints = new List<double[]>();
                for (var j = 0; j < numSteps; j++)
                {
                    var dist = crossAreas[i][j, 0] = coordValues[j];
                    //Console.WriteLine("slice at Coord " + i + " at " + coordValues[j]);
                    var newArea = 0.0;// Slice.DefineContact(ts, new Flat(dist, direction), false).Area;
                    crossAreas[i][j, 1] = newArea;
                    if (j > 0 && Math.Abs(newArea - previousArea) > greatestDelta)
                    {
                        greatestDelta = Math.Abs(newArea - previousArea);
                        greatestDeltaLocation = dist;
                    }
                    var dataPoint = new double[] { dist, newArea };
                    dataPoints.Add(dataPoint);
                    previousArea = newArea;
                }
                areaData.Add(dataPoints);
                greatestDeltas.Add(greatestDelta);
                greatestDeltaLocations.Add(greatestDeltaLocation);
            }//);
            TVGLTest.ExcelInterface.CreateNewGraph(areaData, "Area Decomposition", "Distance From Origin", "Area");
            Debug.WriteLine("end...Time Elapsed = " + (DateTime.Now - now));

            //Console.ReadKey();
            //for (var i = 0; i < 3; i++)
            //{
            //    Debug.WriteLine("\nfor direction " + i);
            //    for (var j = 0; j < crossAreas[i].GetLength(0); j++)
            //    {
            //        Debug.WriteLine(crossAreas[i][j, 0] + ", " + crossAreas[i][j, 1]);
            //    }
            //}
        }
    }
}