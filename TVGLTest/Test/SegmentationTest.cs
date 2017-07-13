using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using ClipperLib;
using StarMathLib;
using TVGL;
using TVGL.IOFunctions;

namespace TVGL_Test
{
    [TestFixture]
    class SegmentationTest
    {
        [SetUp]
        public void TestSetup()
        {
        }

        private static void TestSegmentation(TessellatedSolid ts, int[] segmentCounts)
        {
            var obb = MinimumEnclosure.OrientedBoundingBox(ts);
            var averageNumberOfSteps = 500;

            //Do the average # of slices slices for each direction on a box (l == w == h).
            //Else, weight the average # of slices based on the average obb distance
            var obbAverageLength = (obb.Dimensions[0] + obb.Dimensions[1] + obb.Dimensions[2]) / 3;
            //Set step size to an even increment over the entire length of the solid
            var stepSize = obbAverageLength / averageNumberOfSteps;

            //var startTime = DateTime.Now;
            
            for (var i =0; i < 3; i++ )
            {
                var direction = obb.Directions[i];

                //Get the forward and reverse segments. They should be mostly the same.
                Dictionary<int, double> stepDistances;
                var segments = DirectionalDecomposition.UniformDirectionalSegmentation(ts, direction, stepSize, out stepDistances);
                Assert.That(segments.Count == segmentCounts[i], "Incorrect Number of Segments");
               
                //Check to make sure all the faces and edges and vertices are inlcuded into at least one segment.
                CheckAllObjectTypes(ts, segments);

                var reverseSegments = DirectionalDecomposition.UniformDirectionalSegmentation(ts, direction.multiply(-1), stepSize, out stepDistances);
                Assert.That(reverseSegments.Count == segmentCounts[i], "Incorrect Number of Segments");
                CheckAllObjectTypes(ts, reverseSegments);
            }
            //var totalTime = DateTime.Now - startTime;
            //Debug.WriteLine(totalTime.TotalMilliseconds + " Milliseconds");
        }

        private static void CheckAllObjectTypes(TessellatedSolid ts, IEnumerable<DirectionalDecomposition.DirectionalSegment> segments)
        {
            var faces = new HashSet<PolygonalFace>(ts.Faces);
            var vertices = new HashSet<Vertex>(ts.Vertices);
            var edges = new HashSet<Edge>(ts.Edges);

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

            //Make sure that every face, edge, and vertex is accounted for
            Assert.That(!edges.Any(), "edges missed");
            Assert.That(!faces.Any(), "faces missed");
            Assert.That(!vertices.Any(), "vertices missed");
        }

        private static TessellatedSolid LoadSolid(string filename)
        {
            Stream fileStream;
            List<TessellatedSolid> ts;
            using (fileStream = File.OpenRead(filename))
                ts = IO.Open(fileStream, filename);
            return ts[0];
        }

        [Test]
        public void SegmentSquareSupport()
        {
            //This gets the directory of the app. System.Directory functions do not get the correct base directory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            //The @ makes it verbatim, Or you can use double \. 
            var path = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\")) + "TestFiles\\Square_Support.STL";
            var ts = LoadSolid(path);

            var segmentCounts = new[] {15, 33, 17};
            TestSegmentation(ts, segmentCounts);
        }

        [Test]
        public void SegmentBeamClean()
        {
            //This gets the directory of the app. System.Directory functions do not get the correct base directory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            //The @ makes it verbatim, Or you can use double \. 
            var path = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\")) + "TestFiles\\Beam_Clean.STL";

            var ts = LoadSolid(path);
            var segmentCounts = new[] { 12, 5, 13 };
            TestSegmentation(ts, segmentCounts);
        }

    }
}