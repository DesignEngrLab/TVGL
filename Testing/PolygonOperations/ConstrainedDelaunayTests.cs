using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TVGL;
using Xunit;

namespace TVGLUnitTestsAndBenchmarking
{
    public class ConstrainedDelaunayTests
    {
        [Fact]
        public void MultiStepConstraintInsertionDoesNotRetainReplacedFaces()
        {
            // This fixed point cloud needs multiple rounds of edge flips to insert the
            // 0--1 constraint. Earlier implementations also kept intermediate faces,
            // producing overlapping triangles and more than two faces on an edge.
            var random = new Random(2);
            var points = Enumerable.Range(0, 20)
                .Select(i => new Vertex2D(new Vector2(10 * random.NextDouble(), 10 * random.NextDouble()), i, -1))
                .ToList();
            var constraints = new List<(int From, int To)> { (0, 1) };
            var method = typeof(PolygonOperations).GetMethod("RunConstrainedDelaunay",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            object[] arguments = [points, constraints, null];
            Assert.True((bool)method.Invoke(null, arguments));
            var triangulation = (Delaunay2D)arguments[2];

            var facesByVertices = triangulation.Faces.Select(f =>
                new[] { f.A.IndexInList, f.B.IndexInList, f.C.IndexInList }.Order().ToArray()).ToArray();
            Assert.Equal(facesByVertices.Length, facesByVertices.Select(f => string.Join(",", f)).Distinct().Count());
            var edgeCounts = triangulation.Faces
                .SelectMany(f => new[] { (f.A, f.B), (f.B, f.C), (f.C, f.A) })
                .GroupBy(e => (Math.Min(e.Item1.IndexInList, e.Item2.IndexInList),
                               Math.Max(e.Item1.IndexInList, e.Item2.IndexInList)))
                .ToDictionary(g => g.Key, g => g.Count());
            Assert.All(edgeCounts.Values, count => Assert.InRange(count, 1, 2));
            Assert.True(edgeCounts.ContainsKey((0, 1)));
        }
    }
}
