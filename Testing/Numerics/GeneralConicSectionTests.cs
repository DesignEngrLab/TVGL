using System;
using System.Collections.Generic;
using Xunit;

using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public static class GeneralConicSectionTests
    {
        [Fact]
        public static void ClassificationPreservesCoefficients()
        {
            const double a = -0.00020452714650700629;
            const double b = -0.000046026608781365904;
            const double c = -0.000011890855537091593;
            const double d = -0.011314302635329046;
            const double e = 0.096040959529793787;

            var conic = new GeneralConicSection(a, b, c, d, e, false);

            Assert.Equal(PrimitiveCurveType.Ellipse, conic.CurveType);
            Assert.Equal(a, conic.A);
            Assert.Equal(b, conic.B);
            Assert.Equal(c, conic.C);
            Assert.Equal(d, conic.D);
            Assert.Equal(e, conic.E);
        }

        [Fact]
        public static void RotatedParabolaUsesFullXYCoefficientDiscriminant()
        {
            // (x + y)^2 - x + y = 0 has A=1, B=2, C=1 and B^2 - 4AC = 0.
            var conic = new GeneralConicSection(1.0, 2.0, 1.0, -1.0, 1.0, true);

            Assert.Equal(PrimitiveCurveType.Parabola, conic.CurveType);
        }

        [Fact]
        public static void ClassificationIsInvariantToEquationScale()
        {
            var normalScale = new GeneralConicSection(3.0, 2.0, 2.0, 0.0, 0.0, true);
            var tinyScale = new GeneralConicSection(3e-12, 2e-12, 2e-12, 0.0, 0.0, true);

            Assert.Equal(PrimitiveCurveType.Ellipse, normalScale.CurveType);
            Assert.Equal(normalScale.CurveType, tinyScale.CurveType);
        }

        [Fact]
        public static void QuadricPlaneIntersectionsRemainOnOriginalQuadric()
        {
            var quadric = new GeneralQuadric(
                0.0021224868936546206, 0.0021224868936546206, 0.0,
                0.0, 0.0, 0.0,
                0.0, 0.0, 1.0, -7.5877985186850241);
            var trianglePoints = new[]
            {
                new Vector3(-20.000, -15.898, 6.326),
                new Vector3(-20.776, -15.807, 5.996),
                new Vector3(-20.000, -15.826, 5.996)
            };
            var plane = Plane.CreateFromVertices(trianglePoints[0], trianglePoints[1], trianglePoints[2]);
            var conic = GeneralConicSection.CreateFromQuadric(quadric, plane);
            var intersections = new List<Vector2>();

            for (var i = 0; i < trianglePoints.Length; i++)
            {
                var start = plane.TransformFrom3DTo2D(trianglePoints[i]);
                var end = plane.TransformFrom3DTo2D(trianglePoints[(i + 1) % trianglePoints.Length]);
                intersections.AddRange(GetIntersectionsOnSegment(conic, start, end));
            }

            Assert.Equal(PrimitiveCurveType.Ellipse, conic.CurveType);
            Assert.True(intersections.Count >= 2);
            foreach (var intersection in intersections)
            {
                var point = plane.TransformFrom2DTo3D(intersection);
                Assert.True(Math.Abs(Evaluate(quadric, point)) < 1e-10,
                    $"Projected conic point misses the source quadric: {point}");
            }
        }

        private static IEnumerable<Vector2> GetIntersectionsOnSegment(
            GeneralConicSection conic, Vector2 start, Vector2 end)
        {
            foreach (var (intersection, lineT) in conic.LineIntersection(start, end - start))
                if (lineT >= -1e-10 && lineT <= 1.0 + 1e-10)
                    yield return intersection;
        }

        private static double Evaluate(GeneralQuadric quadric, Vector3 point)
        {
            return quadric.XSqdCoeff * point.X * point.X
                   + quadric.YSqdCoeff * point.Y * point.Y
                   + quadric.ZSqdCoeff * point.Z * point.Z
                   + quadric.XYCoeff * point.X * point.Y
                   + quadric.XZCoeff * point.X * point.Z
                   + quadric.YZCoeff * point.Y * point.Z
                   + quadric.XCoeff * point.X
                   + quadric.YCoeff * point.Y
                   + quadric.ZCoeff * point.Z
                   + quadric.W;
        }
    }
}
