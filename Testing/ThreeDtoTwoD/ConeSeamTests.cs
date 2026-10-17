using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
using Xunit;

namespace TVGLUnitTestsAndBenchmarking
{
    public class ConeSeamTests
    {
        private const double Tolerance = 1e-9;
        private static readonly double ApertureScale = 1.0 / Math.Sqrt(2.0);

        [Fact]
        public void SeamAwareTransformPreservesBranchCrossingsAndMultipleRevolutions()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var seamCrossingPath = new[] { -0.2, 0.0, 0.2 }
                .Select(angle => PointOnCone(angle)).ToList();
            var developed = cone.TransformFrom3DTo2D(seamCrossingPath, Vector3.UnitX).ToList();

            Assert.InRange(Math.Abs(SignedAngle(developed[0], developed[1]) - 0.2 * ApertureScale), 0,
                Tolerance);
            Assert.InRange(Math.Abs(SignedAngle(developed[1], developed[2]) - 0.2 * ApertureScale), 0,
                Tolerance);

            var azimuths = Enumerable.Range(0, 167).Select(i => 0.2 * i).ToList();
            var multiTurnPath = azimuths.Select(angle => PointOnCone(angle)).ToList();
            var multiTurnDeveloped = cone.TransformFrom3DTo2D(multiTurnPath, Vector3.UnitX).ToList();
            var accumulatedAngle = 0.0;
            for (var i = 1; i < multiTurnDeveloped.Count; i++)
                accumulatedAngle += SignedAngle(multiTurnDeveloped[i - 1], multiTurnDeveloped[i]);

            var expectedAngle = (azimuths[^1] - azimuths[0]) * ApertureScale;
            Assert.InRange(Math.Abs(accumulatedAngle - expectedAngle), 0, Tolerance);
        }

        [Fact]
        public void BestSeamUsesLargestGapWhenCutCountsAreEqual()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var paths = new List<IList<Vector3>>
            {
                new[] { -0.1, 0.0, 0.1 }.Select(angle => PointOnCone(angle)).ToList(),
                new[] { 0.3, 0.4, 0.5 }.Select(angle => PointOnCone(angle)).ToList()
            };

            var polygons = cone.GetUnrolledPolygons(paths, new[] { false, false }, out var pointToVertexMap)
                .Select(result => result.polygon).ToList();
            var developedAngles = polygons.SelectMany(polygon => polygon.Path)
                .Select(point => Math.Atan2(point.Y, point.X)).ToList();
            var returnedVertices = polygons.SelectMany(polygon => polygon.AllPolygons)
                .SelectMany(polygon => polygon.Vertices).ToHashSet();

            Assert.Equal(2, polygons.Count);
            Assert.InRange(Math.Abs(developedAngles.Average()), 0, Tolerance);
            Assert.Equal(paths.Sum(path => path.Count), pointToVertexMap.Count);
            foreach (var point3D in paths.SelectMany(path => path))
                Assert.Contains(pointToVertexMap[point3D], returnedVertices);
        }

        [Fact]
        public void BestSeamMinimizesCutPathsBeforeMaximizingGapWidth()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var paths = new List<IList<Vector3>>
            {
                new List<Vector3> { PointOnCone(0.0, 10), PointOnCone(1.5, 11), PointOnCone(3.0, 12) },
                new[] { 3.9, 4.1 }.Select(angle => PointOnCone(angle)).ToList(),
                new[] { 4.9, 5.1 }.Select(angle => PointOnCone(angle)).ToList(),
                new[] { 5.9, 6.1 }.Select(angle => PointOnCone(angle)).ToList()
            };

            var polygons = cone.GetUnrolledPolygons(paths, new[] { false, false, false, false })
                .Select(result => result.polygon).ToList();
            var pointAtAzimuthZero = polygons[0].Path.MinBy(point => point.LengthSquared());
            var selectedSectorCenter = -Math.Atan2(pointAtAzimuthZero.Y, pointAtAzimuthZero.X) / ApertureScale;
            var expectedSectorCenter = 3.45 - Math.PI;

            // The widest global vertex gap lies inside the first path. The selected 0.9-radian gap cuts no paths.
            Assert.InRange(Math.Abs(selectedSectorCenter - expectedSectorCenter), 0, 1e-6);
        }

        [Fact]
        public void SelectedSeamInverseRestoresSourcesAndMatchesBothSectorBorders()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var ring = Enumerable.Range(0, 32)
                .Select(i => PointOnCone(i * Math.Tau / 32.0, 10.0)).ToList();

            var result = cone.GetUnrolledPolygons(new[] { (IList<Vector3>)ring }, new[] { true },
                out var pointToVertexMap, out var seamDirection, 2.0).Single();

            foreach (var (sourcePoint, vertex2D) in pointToVertexMap)
            {
                var restoredPoint = cone.TransformFrom2DTo3D(vertex2D.Coordinates, seamDirection);
                Assert.InRange(Vector3.Distance(sourcePoint, restoredPoint), 0, 1e-6);
            }

            var halfSectorAngle = 0.5 * Math.Tau * ApertureScale;
            var positiveBorder = result.polygon.Vertices
                .OrderBy(vertex => AngularDistance(Math.Atan2(vertex.Y, vertex.X), halfSectorAngle))
                .Take(result.numRepeats).OrderBy(vertex => vertex.Coordinates.Length()).ToList();
            var negativeBorder = result.polygon.Vertices
                .OrderBy(vertex => AngularDistance(Math.Atan2(vertex.Y, vertex.X), -halfSectorAngle))
                .Take(result.numRepeats).OrderBy(vertex => vertex.Coordinates.Length()).ToList();
            Assert.Equal(result.numRepeats, positiveBorder.Count);
            Assert.Equal(result.numRepeats, negativeBorder.Count);
            for (var i = 0; i < result.numRepeats; i++)
            {
                var first = cone.TransformFrom2DTo3D(positiveBorder[i].Coordinates, seamDirection);
                var second = cone.TransformFrom2DTo3D(negativeBorder[i].Coordinates, seamDirection);
                Assert.InRange(Vector3.Distance(first, second), 0, 1e-6);
            }
        }

        [Fact]
        public void EncirclingPolygonLeavesItsOnlyAngularGapAtNegativeX()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var ring = Enumerable.Range(0, 32)
                .Select(i => PointOnCone(0.37 + i * Math.Tau / 32.0, 10.0 + 0.4 * Math.Sin(3 * i)))
                .ToList();

            var result = cone.GetUnrolledPolygons(new[] { (IList<Vector3>)ring }, new[] { true },
                out var pointToVertexMap, out var seamDirection, 2.0).Single();
            var halfSectorAngle = 0.5 * Math.Tau * ApertureScale;
            var angles = result.polygon.Path.Select(point => Math.Atan2(point.Y, point.X)).ToList();

            Assert.All(angles, angle => Assert.InRange(angle, -halfSectorAngle - 1e-6,
                halfSectorAngle + 1e-6));

            var normalizedAngles = angles.Select(NormalizeAngle).OrderBy(angle => angle).ToList();
            var largestGap = double.NegativeInfinity;
            var largestGapMidpoint = double.NaN;
            for (var i = 0; i < normalizedAngles.Count; i++)
            {
                var start = normalizedAngles[i];
                var end = i + 1 < normalizedAngles.Count ? normalizedAngles[i + 1] : normalizedAngles[0] + Math.Tau;
                if (end - start <= largestGap)
                    continue;

                largestGap = end - start;
                largestGapMidpoint = NormalizeAngle(start + 0.5 * largestGap);
            }

            Assert.InRange(AngularDistance(largestGapMidpoint, Math.PI), 0, 1e-6);
            foreach (var (sourcePoint, vertex2D) in pointToVertexMap)
            {
                var restoredPoint = cone.TransformFrom2DTo3D(vertex2D.Coordinates, seamDirection);
                Assert.InRange(Vector3.Distance(sourcePoint, restoredPoint), 0, 1e-6);
            }
        }

        private static Vector3 PointOnCone(double azimuth, double height = 10.0)
            => new(height * Math.Cos(azimuth), height * Math.Sin(azimuth), height);

        private static double SignedAngle(Vector2 from, Vector2 to)
            => Math.Atan2(from.Cross(to), from.Dot(to));

        private static double AngularDistance(double first, double second)
            => Math.Abs(Math.Atan2(Math.Sin(first - second), Math.Cos(first - second)));

        private static double NormalizeAngle(double angle)
        {
            angle %= Math.Tau;
            return angle < 0 ? angle + Math.Tau : angle;
        }
    }
}
