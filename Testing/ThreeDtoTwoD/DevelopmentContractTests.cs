using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TVGL;
using Xunit;

namespace TVGLUnitTestsAndBenchmarking
{
    public class DevelopmentContractTests
    {
        [Fact]
        public void PlaneCylinderAndConeReturnOwnedSourceVertexMappings()
        {
            var plane = new Plane(2.0, Vector3.UnitZ) { PerpXDir = Vector3.UnitX };
            AssertMappedRoundTrip(plane,
                new List<Vector3>
                {
                    new(-2, -1, 2), new(2, -1, 2), new(2, 1, 2), new(-2, 1, 2)
                }, true, false);

            var cylinder = new Cylinder { Anchor = Vector3.Zero, Axis = Vector3.UnitZ, Radius = 2.0 };
            AssertMappedRoundTrip(cylinder,
                new[] { -0.6, -0.2, 0.2, 0.6 }
                    .Select((angle, index) => new Vector3(2 * Math.Cos(angle), 2 * Math.Sin(angle), index))
                    .ToList(), false, true);

            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            AssertMappedRoundTrip(cone,
                new[] { -0.6, -0.2, 0.2, 0.6 }
                    .Select((angle, index) => PointOnCone(angle, 4.0 + index)).ToList(), false, true);
        }

        [Fact]
        public void ConeStoresSelectedSeamAndDefaultInverseUsesIt()
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, 1.0, true);
            var ring = Enumerable.Range(0, 32)
                .Select(i => PointOnCone(0.21 + i * Math.Tau / 32.0, 10.0)).ToList();

            var result = cone.DevelopPolygons(new[] { (IList<Vector3>)ring }, new[] { true },
                new DevelopmentOptions { SelectBestSeam = true, NewVertexSpacing = 2.0 }).Single();
            var selectedSeam = cone.PerpXDir;

            Assert.InRange(Vector3.Distance(cone.PerpXDir, selectedSeam), 0, 1e-10);
            foreach (var (sourcePoint, vertex2D) in result.PointToVertexMap)
                Assert.InRange(Vector3.Distance(sourcePoint,
                    cone.TransformFrom2DTo3D(vertex2D.Coordinates)), 0, 1e-6);

            AssertSeamPairs(cone, result, 2e-6);
        }

        [Fact]
        public void CylinderDuplicateMetadataPairsTheTwoPhysicalSeamBoundaries()
        {
            var cylinder = new Cylinder { Anchor = Vector3.Zero, Axis = Vector3.UnitZ, Radius = 3.0 };
            var bottom = Enumerable.Range(0, 32)
                .Select(i => PointOnCylinder(i * Math.Tau / 32.0, 0.0)).ToList();
            var top = Enumerable.Range(0, 32)
                .Select(i => PointOnCylinder(-i * Math.Tau / 32.0, 5.0)).ToList();

            var result = cylinder.DevelopPolygons(
                new IList<Vector3>[] { bottom, top }, new[] { true, true }, new DevelopmentOptions
                {
                    SelectBestSeam = true,
                    NewVertexSpacing = double.NaN
                }).Single();

            AssertSeamPairs(cylinder, result);
        }

        [Theory]
        [InlineData(0.2)]
        [InlineData(7.4)]
        public void ConeTipUsesOneApexAndTriangulatesWithoutAHighValenceFan(double aperture)
        {
            var cone = new Cone(Vector3.Zero, Vector3.UnitZ, aperture, true);
            const double slantRadius = 20.0;
            var height = slantRadius / Math.Sqrt(1.0 + aperture * aperture);
            var ring = Enumerable.Range(0, 96)
                .Select(i => new Vector3(aperture * height * Math.Cos(i * Math.Tau / 96.0),
                    aperture * height * Math.Sin(i * Math.Tau / 96.0), height)).ToList();

            var result = cone.DevelopPolygons(new[] { (IList<Vector3>)ring }, new[] { true },
                new DevelopmentOptions { NewVertexSpacing = 0.8 }).Single();

            Assert.Single(result.Polygon.Vertices, vertex =>
                vertex.Coordinates.LengthSquared().IsNegligible());
            AssertSeamPairs(cone, result, 2e-6);

            var triangulation = result.Polygon.TriangulateDelaunay(false, true, targetSideLength: 0.8);
            var apex = Assert.Single(triangulation.Vertices, vertex =>
                vertex.Coordinates.LengthSquared().IsNegligible());
            var apexFaces = triangulation.Faces.Where(face => face.Vertices.Contains(apex)).ToList();

            Assert.InRange(apexFaces.Count, 1, 24);
            Assert.All(triangulation.Faces, face =>
            {
                Assert.Equal(3, face.Vertices.Distinct().Count());
                Assert.True(face.Area > 1e-10, $"Triangle {face.IndexInList} has area {face.Area}.");
            });
            Assert.All(apexFaces, face =>
            {
                var otherVertices = face.Vertices.Where(vertex => !ReferenceEquals(vertex, apex)).ToArray();
                var first = (otherVertices[0].Coordinates - apex.Coordinates).Normalize();
                var second = (otherVertices[1].Coordinates - apex.Coordinates).Normalize();
                var angle = Math.Acos(Math.Clamp(first.Dot(second), -1.0, 1.0));
                Assert.True(angle > Math.PI / 180.0,
                    $"Apex triangle {face.IndexInList} has only {angle * 180.0 / Math.PI} degrees at the tip.");
            });
        }

        [Fact]
        public void DevelopmentDirectionsSurviveJsonRoundTrip()
        {
            AssertDirectionRoundTrip(new Plane(3.0, Vector3.UnitZ) { PerpXDir = Vector3.UnitY });
            AssertDirectionRoundTrip(new Cylinder
            {
                Anchor = new Vector3(1, 2, 3), Axis = Vector3.UnitZ, Radius = 4, PerpXDir = Vector3.UnitY
            });
            AssertDirectionRoundTrip(new Cone(Vector3.Zero, Vector3.UnitZ, 0.75, true)
            {
                PerpXDir = Vector3.UnitY
            });
        }

        private static void AssertMappedRoundTrip(PrimitiveSurface surface, IList<Vector3> path, bool isClosed,
            bool selectBestSeam)
        {
            var result = surface.DevelopPolygons(new[] { path }, new[] { isClosed }, new DevelopmentOptions
            {
                SelectBestSeam = selectBestSeam,
                NewVertexSpacing = double.NaN
            }).Single();
            var ownedVertices = result.Polygon.AllPolygons.SelectMany(polygon => polygon.Vertices).ToHashSet();

            Assert.Equal(path.Count, result.PointToVertexMap.Count);
            foreach (var sourcePoint in path)
            {
                var vertex = result.PointToVertexMap[sourcePoint];
                Assert.Contains(vertex, ownedVertices);
                Assert.InRange(Vector3.Distance(sourcePoint,
                    surface.TransformFrom2DTo3D(vertex.Coordinates)), 0, 1e-6);
            }
        }

        private static void AssertDirectionRoundTrip<TSurface>(TSurface surface)
            where TSurface : PrimitiveSurface
        {
            var json = JsonConvert.SerializeObject(surface);
            var copy = JsonConvert.DeserializeObject<TSurface>(json);
            var originalDirection = surface switch
            {
                Plane plane => plane.PerpXDir,
                Cylinder cylinder => cylinder.PerpXDir,
                Cone cone => cone.PerpXDir,
                _ => throw new NotSupportedException()
            };
            var copiedDirection = copy switch
            {
                Plane plane => plane.PerpXDir,
                Cylinder cylinder => cylinder.PerpXDir,
                Cone cone => cone.PerpXDir,
                _ => throw new NotSupportedException()
            };
            Assert.InRange(Vector3.Distance(originalDirection, copiedDirection), 0, 1e-10);
        }

        private static void AssertSeamPairs(PrimitiveSurface surface, DevelopedPolygon result,
            double tolerance = 1e-6)
        {
            Assert.True(result.NumDuplicates > 0);
            var vertices = result.Polygon.Vertices;
            var secondSeamStart = result.IndexWhereMatchingStarts - 1;
            for (var i = 0; i < result.NumDuplicates; i++)
            {
                var matchingIndex = secondSeamStart + result.NumDuplicates - 1 - i;
                var first = surface.TransformFrom2DTo3D(vertices[i].Coordinates);
                var second = surface.TransformFrom2DTo3D(vertices[matchingIndex].Coordinates);
                var distance = Vector3.Distance(first, second);
                Assert.True(distance <= tolerance,
                    $"Pair {i}/{matchingIndex} ({vertices[i].Coordinates}, {vertices[matchingIndex].Coordinates}) restored {distance} apart.");
            }
        }

        private static Vector3 PointOnCone(double azimuth, double height)
            => new(height * Math.Cos(azimuth), height * Math.Sin(azimuth), height);

        private static Vector3 PointOnCylinder(double azimuth, double height)
            => new(3.0 * Math.Cos(azimuth), 3.0 * Math.Sin(azimuth), height);
    }
}
