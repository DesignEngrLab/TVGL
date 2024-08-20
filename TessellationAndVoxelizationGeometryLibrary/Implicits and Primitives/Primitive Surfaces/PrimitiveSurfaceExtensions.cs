// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="PrimitiveSurface.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public static class PrimitiveSurfaceExtensions
    {

        /// <summary>
        /// Returns the Borders of the surface that encircle the surface's axis (e.g. the endcaps of a cylinder).
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A list of PrimitiveBorders.</returns>
        public static IEnumerable<BorderLoop> BordersEncirclingAxis(this PrimitiveSurface surface)
        {
            var axis = surface.GetAxis();
            if (axis.IsNull()) yield break;
            var anchor = surface.GetAnchor();
            if (anchor.IsNull()) yield break;
            foreach (var result in surface.BordersEncirclingAxis(axis, anchor))
                yield return result;
        }

        /// <summary>
        /// Returns the Borders of the surface that encircle the given axis and anchor point.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A list of PrimitiveBorders.</returns>
        public static IEnumerable<BorderLoop> BordersEncirclingAxis(this PrimitiveSurface surface, Vector3 axis, Vector3 anchor)
        {
            foreach (var border in surface.Borders)
                if (border.BorderEncirclesAxis(axis, anchor))
                    yield return border;
        }

        /// <summary>
        /// Reports if the given border the encircles axis of the given primitive surface.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="border">The border.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this PrimitiveSurface surface, BorderLoop border)
        {
            var axis = surface.GetAxis();
            if (axis.IsNull()) return false;
            var anchor = surface.GetAnchor();
            if (anchor.IsNull()) return false;
            return border.BorderEncirclesAxis(axis, anchor);
        }


        /// <summary>
        /// Reports if the given border the encircles the given axis and anchor.
        /// </summary>
        /// <param name="border">The border.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this BorderLoop border, Vector3 axis, Vector3 anchor)
        {
            return border.GetCoordinates().BorderEncirclesAxis(axis, anchor);
        }


        /// <summary>
        /// Reports if the given border the encircles the given axis and anchor.
        /// </summary>
        /// <param name="border">The border.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this BorderLoop border, Matrix4x4 transform, Vector3 anchor)
        {
            return border.GetCoordinates().BorderEncirclesAxis(transform, anchor);
        }
        /// <param name="transform">The transform.</param>


        /// <summary>
        /// Returns true if the given path encircles the axis (by more than 5/6 around).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(this IEnumerable<Vector3> path, Vector3 axis, Vector3 anchor)
        {
            var angle = Math.Abs(FindWindingAroundAxis(path, axis, anchor, out _, out _));
            return angle > 1.67 * Math.PI;
            // 1.67 is 5/3, which is 5/6 the way around. so the border would be at least a hexagon.
        }

        /// <summary>
        /// Returns true if the given path encircles the axis (by more than 5/6 around).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(this IEnumerable<Vector3> path, Matrix4x4 transform, Vector3 anchor)
        {
            var angle = Math.Abs(FindWindingAroundAxis(path, transform, anchor, out _, out _));
            return angle > 1.67 * Math.PI;
            // 1.67 is 5/3, which is 5/6 the way around. so the border would be at least a hexagon.
        }



        /// <summary>
        /// Finds the largest encompassing angle for primitive about its axis. The resulting vectors
        /// (<paramref name="vectorAtMinAngle"/> & <paramref name="vectorAtMaxAngle"/>) emanate from the axis outward
        /// (they are orthogonal to the axis).
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="vectorAtMinAngle">The vector at min angle.</param>
        /// <param name="vectorAtMaxAngle">The vector at max angle.</param>
        /// <returns>A double.</returns>
        public static double FindLargestEncompassingAngleForAxis(this PrimitiveSurface surface, out Vector3 vectorAtMinAngle, out Vector3 vectorAtMaxAngle)
        {
            var axis = surface.GetAxis();
            var transform = axis.TransformToXYPlane(out var backTransform);
            var globalMinAngle = double.PositiveInfinity;
            var globalMaxAngle = double.NegativeInfinity;

            foreach (var path in surface.Borders)
            {
                FindWindingAroundAxis(path.GetCoordinates(), transform, surface.GetAnchor(), out var minAngle, out var maxAngle);
                if (globalMaxAngle < minAngle)
                {
                    minAngle += Math.Tau;
                    maxAngle += Math.Tau;
                }
                if (globalMinAngle > maxAngle)
                {
                    minAngle -= Math.Tau;
                    maxAngle -= Math.Tau;
                }
                if (globalMinAngle > minAngle) globalMinAngle = minAngle;
                if (globalMaxAngle < maxAngle) globalMaxAngle = maxAngle;

                if (Math.Abs(globalMaxAngle - globalMinAngle) > Math.Tau)
                {
                    globalMinAngle = -Math.PI;
                    globalMaxAngle = Math.PI;
                    break;
                }
            }
            vectorAtMinAngle = new Vector3(Math.Cos(globalMinAngle), Math.Sin(globalMinAngle), 0).TransformNoTranslate(backTransform);
            vectorAtMaxAngle = new Vector3(Math.Cos(globalMaxAngle), Math.Sin(globalMaxAngle), 0).TransformNoTranslate(backTransform);
            return globalMaxAngle - globalMinAngle;
        }


        /// <summary>
        /// Finds the total winding angle around the axis and provides the minimum and maximum angle.
        /// T
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="minAngle">The min angle.</param>
        /// <param name="maxAngle">The max angle.</param>
        /// <returns>A magnitude of the angle.</returns>
        public static double FindWindingAroundAxis(this IEnumerable<Vector3> path, Matrix4x4 transform,
            Vector3 anchor, out double minAngle, out double maxAngle)
        {
            var coords = path.Select(v => v.ConvertTo2DCoordinates(transform));
            var center = anchor.ConvertTo2DCoordinates(transform);
            return coords.GetWindingAngles(center, true, out minAngle, out maxAngle);
        }

        /// <summary>
        /// Finds the total winding angle around the axis and provides the minimum and maximum angle.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="startingAngle">The starting angle.</param>
        /// <returns>A double.</returns>
        public static double FindWindingAroundAxis(this IEnumerable<Vector3> path, Vector3 axis, Vector3 anchor,
            out double minAngle, out double maxAngle)
        {
            var transform = axis.TransformToXYPlane(out _);
            return FindWindingAroundAxis(path, transform, anchor, out minAngle, out maxAngle);
        }


        /// <summary>
        /// Gets the center of mass for primitive surface. This is a weighted sum using the face area,
        /// which would be the proper way to find the center of mass for a collection of triangles in a 2D plane
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetCenterOfMass(this PrimitiveSurface surface)
        { return surface.Faces.GetCenterOfMass(); }
        /// <summary>
        /// Gets the center of mass for the collection of faces. This is a weighted sum using the face area,
        /// which would be the proper way to find the center of mass for a collection of triangles in a 2D plane
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetCenterOfMass(this IEnumerable<TriangleFace> faces)
        {
            var totalArea = 0.0;
            var totalCenter = Vector3.Zero;
            foreach (var face in faces)
            {
                var area = face.Area;
                totalArea += area;
                totalCenter += face.Center * area;
            }
            return totalCenter / totalArea;
        }


        /// <summary>
        /// Gets the axis for the primitive surface. This is straightforward for cylinders, cones, tori,
        /// and prismatic surfaces. For planes, it returns the normal, and for spheres, it returns null.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetAxis(this PrimitiveSurface surface)
        {
            if (surface is Plane plane) return plane.Normal;
            else if (surface is Cylinder cylinder) return cylinder.Axis;
            else if (surface is Cone cone) return cone.Axis;
            else if (surface is Torus torus) return torus.Axis;
            else if (surface is Prismatic prismatic) return prismatic.Axis;
            else if (surface is Capsule capsule) return (capsule.Anchor2 - capsule.Anchor1).Normalize();
            else if (surface is GeneralQuadric gq) return gq.Axis1;
            else return Vector3.Null;
        }

        /// <summary>
        /// Gets the anchor for the primitive surface. This is straightforward for cylinders, cones, tori,
        /// and spheres. For all others, it returns the center of mass.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetAnchor(this PrimitiveSurface s)
        {
            if (s is Cylinder cylinder) return cylinder.Anchor;
            else if (s is Cone cone) return cone.Apex;
            else if (s is Torus torus) return torus.Center;
            else if (s is Sphere sphere) return sphere.Center;
            else if (s is Capsule capsule) return 0.5 * (capsule.Anchor1 + capsule.Anchor2);
            else if (s is GeneralQuadric gq) return gq.Center;
            else return GetCenterOfMass(s.Faces);
        }

        /// <summary>
        /// Gets the radius for the primitive surface. This is straightforward for cylinders, & spheres.
        /// For tori, it returns the major radius unless max is true. In which case it returns the larger
        /// of the minor and major radii. For all others, it returns the average radius of borders that
        /// are circle shaped. Unless max is true. In which case it returns the max of the radii of circles.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="max">If true, max.</param>
        /// <returns>A double.</returns>
        public static double GetRadius(this PrimitiveSurface s, bool max = false)
        {
            if (s is Cylinder cylinder) return cylinder.Radius;
            else if (s is Capsule capsule) return 0.5 * (capsule.Radius1 + capsule.Radius2);
            if (s is Sphere sphere) return sphere.Radius;
            if (s is Torus torus)
            {
                if (max) return Math.Max(torus.MajorRadius, torus.MinorRadius);
                return torus.MajorRadius;
            }
            if (s.Borders == null) return double.NaN;

            var circleBorders = s.Borders.Where(b => b.Curve is Circle);
            if (!circleBorders.Any()) return 0.0;
            else if (max) return circleBorders.Max(b => ((Circle)b.Curve).Radius);
            else return circleBorders.Average(b => ((Circle)b.Curve).Radius);
        }

        /// <summary>
        /// Given a set of 2D points in arbitrary order that are known to be on a curve, this function
        /// finds the extremes
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static (Vector2 start, Vector2 startDirj, Vector2 end, Vector2 endDir) FindExtremesAlong2DCurve(this PrimitiveSurface surface,
            IEnumerable<Vector2> points)
        {
            var int2PointDict = new Dictionary<int, (Vector2, Vertex)>();
            var pointsEnumerator = points.GetEnumerator();
            foreach (var v in surface.Vertices)
            {
                pointsEnumerator.MoveNext();
                var point = pointsEnumerator.Current;
                int2PointDict.Add(v.IndexInList, (point, v));
            }
            var possibleExtremes = new List<(Vector2, Vector2, Vector2)>();
            foreach ((Vector2 location, Vertex vertex) in int2PointDict.Values)
            {
                var outVectors = new List<Vector2>();
                var outer1 = Vector2.Null;
                var outer2 = Vector2.Null;
                foreach (var edge in vertex.Edges)
                {
                    if (!surface.OuterEdges.Contains(edge)) continue;
                    var otherVertex = edge.OtherVertex(vertex);
                    var otherLocation = int2PointDict[otherVertex.IndexInList].Item1;
                    var vector = otherLocation - location;
                    if (vector.LengthSquared().IsNegligible()) continue;
                    vector = vector.Normalize();
                    if (outer1.IsNull())
                        outer1 = vector;
                    else
                    {
                        outer2 = vector;
                        break;
                    }
                }
                if (outer1.IsNull() || (!outer2.IsNull() && outer1.Dot(outer2) < 0))
                    continue;
                possibleExtremes.Add((location, outer1, outer2));
            }
            if (possibleExtremes.Count == 2) return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                    possibleExtremes[1].Item1, (possibleExtremes[1].Item2 + possibleExtremes[1].Item3).Normalize());
            while (possibleExtremes.Count > 2)
            {
                for (int i = possibleExtremes.Count - 1; i > 0; i--)
                {
                    (var point, var v1, var v2) = possibleExtremes[i];
                    for (int j = i - 1; j >= 0; j--)
                    {
                        (var otherPoint, var w1, var w2) = possibleExtremes[j];
                        if (point.IsPracticallySame(otherPoint))
                        {
                            possibleExtremes.RemoveAt(i);
                            break;
                        }
                        var iTrapsJ = PointASeesPointB(point, v1, v2, otherPoint);
                        var jTrapsI = PointASeesPointB(otherPoint, w1, w2, point);
                        if (iTrapsJ == jTrapsI) continue;
                        if (jTrapsI)
                        {
                            possibleExtremes.RemoveAt(i);
                            break;
                        }
                        else // if (iTrapsJ)
                        {
                            i--;
                            possibleExtremes.RemoveAt(j);
                        }
                        //if (possibleExtremes.Count == 2) break;
                    }
                    //if (possibleExtremes.Count == 2) break;
                }
                if (possibleExtremes.Count <= 2) break;
                // more than 2 - then rework the possibleExtremes with connections to the nearest neighbors
                // (here, were are assuming the number is super small like 3 to 6)
                for (int i = possibleExtremes.Count - 1; i > 0; i--)
                {
                    (var point, _, _) = possibleExtremes[i];
                    var closestVector1 = Vector2.Null;
                    var closestVector2 = Vector2.Null;
                    var closestDistance1 = double.PositiveInfinity;
                    var closestDistance2 = double.PositiveInfinity;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        (var otherPoint, _, _) = possibleExtremes[j];
                        var v = otherPoint - point;
                        var distance = v.LengthSquared();
                        if (distance < closestDistance1)
                        {
                            closestDistance2 = closestDistance1;
                            closestVector2 = closestVector1;
                            closestDistance1 = distance;
                            closestVector1 = v;
                        }
                        else if (distance < closestDistance2)
                        {
                            closestDistance2 = distance;
                            closestVector2 = v;
                        }
                    }
                    if (closestVector1.Dot(closestVector2) > 0)
                        possibleExtremes[i] = (point, closestVector1, closestVector2);
                    else possibleExtremes.RemoveAt(i);
                }
            }
            if (possibleExtremes.Count == 0)
                throw new Exception("No points found at the extremes of the curve.");
            if (possibleExtremes.Count == 1)
                return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                   Vector2.Null, Vector2.Null);
            else
                return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                       possibleExtremes[1].Item1, (possibleExtremes[1].Item2 + possibleExtremes[1].Item3).Normalize());
        }

        private static bool PointASeesPointB(Vector2 pointA, Vector2 vA1, Vector2 vA2, Vector2 pointB)
        {
            var vNew = pointB - pointA;
            if (vNew.LengthSquared() < Constants.BaseTolerance * Constants.BaseTolerance) return false;
            return (vA1.IsNull() || vNew.Dot(vA1) >= 0) && (vA2.IsNull() || vNew.Dot(vA2) >= 0);
        }


        #region Tessellation of PrimitiveSurfaces
        public static void Tessellate(this PrimitiveSurface surface, double xMin, double xMax, double yMin, double yMax, double zMin, double zMax, double maxEdgeLength)
        {
            if (surface.Vertices != null && surface.Vertices.Count > 0) return;
            var meshSize = maxEdgeLength / Math.Sqrt(3);
            var solid = new ImplicitSolid(surface);
            solid.Bounds = new[] { new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax) };
            var tessellatedSolid = solid.ConvertToTessellatedSolid(meshSize);
            surface.SetFacesAndVertices(tessellatedSolid.Faces, true);
        }
        public static void Tessellate(this PrimitiveSurface surface, double maxEdgeLength = double.NaN)
        {
            if (surface.Vertices != null && surface.Vertices.Count > 0) return;
            surface.SetBounds();
            if (double.IsFinite(surface.MaxX) && double.IsFinite(surface.MaxY) && double.IsFinite(surface.MaxZ) &&
                 double.IsFinite(surface.MinX) && double.IsFinite(surface.MinY) && double.IsFinite(surface.MinZ))
            {
                if (double.IsNaN(maxEdgeLength))
                {
                    var diagonal = new Vector3(surface.MaxX - surface.MinX, surface.MaxY - surface.MinY, surface.MaxZ - surface.MinZ);
                    maxEdgeLength = 0.033 * diagonal.Length();
                }
                Tessellate(surface, surface.MinX, surface.MaxX, surface.MinY, surface.MaxY, surface.MinZ, surface.MaxZ, maxEdgeLength);
            }
            else throw new ArgumentOutOfRangeException("The provided primitive is" +
                "unbounded in size. Please invoke the overload of this method that accepts coordinate limits");
        }


        public static VoxelizedSolid Voxelize(this PrimitiveSurface surface, VoxelizedSolid environment, bool treatAsSolid = false)
        {
            var result = VoxelizedSolid.CreateEmpty(environment);
            var minIndices = result.ConvertCoordinatesToIndices(new Vector3(surface.MinX, surface.MinY, surface.MinZ));
            var maxIndices = result.ConvertCoordinatesToIndices(new Vector3(surface.MaxX, surface.MaxY, surface.MaxZ));
            var minJ = minIndices[1];
            var maxJ = Math.Min(result.numVoxelsY, maxIndices[1]);
            var minK = minIndices[2];
            var maxK = Math.Min(result.numVoxelsZ, maxIndices[2]);


            //Parallel.For(minK, maxK, k =>
            for (var k = minK; k < maxK; k++)
            {
                var zCoord = result.ConvertZIndexToCoord(k);
                for (int j = minJ; j < maxJ; j++)
                {
                    var yCoord = result.ConvertYIndexToCoord(j);
                    foreach (var intersection in GetPrimitiveAndLineIntersections(surface, result.XMin, yCoord, zCoord))
                    {
                        if (intersection.lineT < 0 || intersection.lineT > result.XMax) continue;
                        //if (treatAsSolid)
                        var indices = result.ConvertCoordinatesToIndices(intersection.intersection);
                        result[indices] = true;
                    }
                }
            } //);
            return result;
        }

        /// <summary>
        /// Creates a the faces and vertices for a circle positioned in 3D space at circleCenter with normalDirection as the outward normal
        /// and the given radius. The number of points in the circle is determined by numPoints.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faces"></param>
        /// <param name="circleCenter"></param>
        /// <param name="normalDirection"></param>
        /// <param name="radius"></param>
        /// <param name="numPoints"></param>
        internal static void GetCircleTessellation(out Vertex[] vertices, out TriangleFace[] faces,
            Vector3 circleCenter, Vector3 normalDirection, double radius, int numPoints)
        {
            var cosAxis = normalDirection.GetPerpendicularDirection();
            var sinAxis = normalDirection.Cross(cosAxis);
            var rotationMatrix = Matrix4x4.CreateRotationZ(2 * Math.PI / numPoints);
            vertices = new Vertex[numPoints + 1];
            var centerVertex = new Vertex(circleCenter);
            vertices[numPoints] = centerVertex;

            for (int i = 0; i < numPoints; i++)
            {
                var angle = i * 2 * Math.PI / numPoints;
                var coord = circleCenter + radius * (cosAxis * Math.Cos(angle) + sinAxis * Math.Sin(angle));
                vertices[i] = new Vertex(coord);
            }

            faces = new TriangleFace[numPoints];
            var j = numPoints - 1;
            for (int i = 0; i < numPoints; i++)
            {
                faces[i] = new TriangleFace(centerVertex, vertices[j], vertices[i]);
                j = i;
            }
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cone cone, double maxEdgeLength, bool keepOpen = false)
        {
            if (double.IsInfinity(cone.Length))
                throw new ArgumentException("The cone must have finite a finite length.");
            var baseCircleRadius = cone.Length * cone.Aperture;
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * baseCircleRadius * baseCircleRadius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return Tessellate(cone, numPoints, keepOpen);
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="numPoints"></param>
        /// <param name="keepOpen">if true then no triangles are added for the base circle</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cone cone, int numPoints = 30, bool keepOpen = false)
        {
            if (double.IsInfinity(cone.Length))
                throw new ArgumentException("The cone must have finite a finite length.");
            var axis = cone.Axis.Normalize();
            var baseCircleRadius = cone.Length * cone.Aperture;
            var baseCircleCenter = cone.Apex + cone.Length * axis;

            GetCircleTessellation(out var btmVertices, out var btmFaces, cone.Apex, axis, baseCircleRadius, numPoints);
            List<Vertex> vertices;
            List<TriangleFace> faces;
            var apexVertex = new Vertex(cone.Apex);
            if (keepOpen)
            {
                vertices = btmVertices.SkipLast(1).Concat([apexVertex]).ToList();
                faces = new List<TriangleFace>();
            }
            else
            {
                vertices = btmVertices.Concat([apexVertex]).ToList();
                faces = btmFaces.ToList();
            }

            var sideFaces = new List<TriangleFace>();
            var j = numPoints - 1;
            for (int i = 0; i < numPoints; i++)
            {
                sideFaces.Add(new TriangleFace(apexVertex, btmVertices[i], btmVertices[j]));
                j = i;
            }
            cone.SetFacesAndVertices(sideFaces);
            var btmPlane = new Plane(cone.Apex + axis * cone.Length, axis);
            btmPlane.SetFacesAndVertices(btmFaces, true, true);
            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor = false };
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [btmPlane, cone]
            };
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cylinder cylinder, double maxEdgeLength, bool keepOpen = false)
        {
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * cylinder.Radius * cylinder.Radius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return Tessellate(cylinder, numPoints, keepOpen);
        }

        /// <summary>
        /// Create a tessellated solid for a cylinder. Long skinny triangles are created from one end to the other.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <param name="numPoints"></param>
        /// <param name="keepOpen">if true then no triangles are added for the circles on the ends</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cylinder cylinder, int numPoints = 30, bool keepOpen = false)
        {
            if (double.IsInfinity(cylinder.MinDistanceAlongAxis) || double.IsInfinity(cylinder.MaxDistanceAlongAxis))
                throw new ArgumentException("The cylinder must have finite min and max distances along the axis.");
            var axis = cylinder.Axis.Normalize();
            var anchorDist = cylinder.Anchor.Dot(axis);
            var cylinderMinAxisPoint = cylinder.Anchor + (cylinder.MinDistanceAlongAxis - anchorDist) * axis;
            var cylinderMaxAxisPoint = cylinderMinAxisPoint + (cylinder.MaxDistanceAlongAxis - cylinder.MinDistanceAlongAxis) * axis;

            GetCircleTessellation(out var btmVertices, out var btmFaces, cylinderMinAxisPoint, -axis, cylinder.Radius, numPoints);
            GetCircleTessellation(out var topVertices, out var topFaces, cylinderMaxAxisPoint, axis, cylinder.Radius, numPoints);
            List<Vertex> vertices;
            List<TriangleFace> faces;
            if (keepOpen)
            {
                vertices = btmVertices.SkipLast(1).Concat(topVertices.SkipLast(1)).ToList();
                faces = new List<TriangleFace>();
            }
            else
            {
                vertices = btmVertices.Concat(topVertices).ToList();
                faces = btmFaces.Concat(topFaces).ToList();
            }

            var sideFaces = new List<TriangleFace>();
            var j = numPoints - 1;
            for (int i = 1; i <= numPoints; i++)
            {
                sideFaces.Add(new TriangleFace(btmVertices[j], topVertices[j], topVertices[i]));
                sideFaces.Add(new TriangleFace(btmVertices[j], topVertices[i], btmVertices[i]));
                j = i;
            }
            faces.AddRange(sideFaces);


            var btmPlane = new Plane(cylinderMinAxisPoint, -axis);
            btmPlane.SetFacesAndVertices(btmFaces, true, true);
            var topPlane = new Plane(cylinderMaxAxisPoint, axis);
            topPlane.SetFacesAndVertices(topFaces, true, true);
            cylinder.SetFacesAndVertices(sideFaces, true, true);

            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor = false };

            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [btmPlane, topPlane, cylinder]
            };
        }


        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid TessellateHollowCylinder(this Cylinder cylinder, double innerRadius, double maxEdgeLength)
        {
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * innerRadius * innerRadius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return Tessellate(cylinder, innerRadius, numPoints);
        }

        /// <summary>
        /// Creates a tessellated solid for a tube or hollow cylinder.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <param name="innerRadius"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid TessellateHollowCylinder(this Cylinder cylinder, double innerRadius, int numPoints = 30)
        {
            if (double.IsInfinity(cylinder.MinDistanceAlongAxis) || double.IsInfinity(cylinder.MaxDistanceAlongAxis))
                throw new ArgumentException("The cylinder must have finite min and max distances along the axis.");
            if (innerRadius >= cylinder.Radius)
                throw new ArgumentException("The inner radius must be smaller than the cylinder's given (outer) radius.");
            var centerDot = cylinder.Axis.Dot(cylinder.Anchor);
            var bottomCenter = cylinder.Anchor + (centerDot - cylinder.MinDistanceAlongAxis) * cylinder.Axis;
            var topCenter = cylinder.Anchor + (centerDot - cylinder.MaxDistanceAlongAxis) * cylinder.Axis;
            var cosAxis = cylinder.Axis.GetPerpendicularDirection();
            var sinAxis = cylinder.Axis.Cross(cosAxis);
            var outerRadius = cylinder.Radius;
            var btmInnerVertices = new Vertex[numPoints];
            var btmOuterVertices = new Vertex[numPoints];
            var topInnerVertices = new Vertex[numPoints];
            var topOuterVertices = new Vertex[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                var angle = i * 2 * Math.PI / numPoints;
                var cosAngle = Math.Cos(angle);
                var sinAngle = Math.Sin(angle);
                var radialVector = cosAxis * cosAngle + sinAxis * sinAngle;
                var innerVector = innerRadius * radialVector;
                var outerVector = outerRadius * radialVector;
                btmInnerVertices[i] = new Vertex(bottomCenter + innerVector);
                btmOuterVertices[i] = new Vertex(bottomCenter + outerVector);
                topInnerVertices[i] = new Vertex(topCenter + innerVector);
                topOuterVertices[i] = new Vertex(topCenter + outerVector);
            }
            var bottomFaces = new List<TriangleFace>();
            var topFaces = new List<TriangleFace>();
            var innerFaces = new List<TriangleFace>();
            var outerFaces = new List<TriangleFace>();
            var j = numPoints - 1;
            for (int i = 0; i < numPoints; i++)
            {
                bottomFaces.Add(new TriangleFace(btmInnerVertices[j], btmOuterVertices[j], btmOuterVertices[i]));
                bottomFaces.Add(new TriangleFace(btmInnerVertices[j], btmOuterVertices[i], btmInnerVertices[i]));
                topFaces.Add(new TriangleFace(topOuterVertices[j], topInnerVertices[j], topInnerVertices[i]));
                topFaces.Add(new TriangleFace(topOuterVertices[j], topInnerVertices[i], topOuterVertices[i]));
                outerFaces.Add(new TriangleFace(btmOuterVertices[j], topOuterVertices[j], topOuterVertices[i]));
                outerFaces.Add(new TriangleFace(btmOuterVertices[j], topOuterVertices[i], btmOuterVertices[i]));
                innerFaces.Add(new TriangleFace(btmInnerVertices[i], topInnerVertices[i], topInnerVertices[j]));
                innerFaces.Add(new TriangleFace(btmInnerVertices[i], topInnerVertices[j], btmInnerVertices[j]));
                j = i;
            }

            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions();
            tessellatedSolidBuildOptions.CopyElementsPassedToConstructor = false;
            var faces = bottomFaces.Concat(topFaces).Concat(outerFaces).ToArray();
            var vertices = btmInnerVertices.Concat(btmOuterVertices).Concat(
                topInnerVertices).Concat(topOuterVertices).ToArray();
            var btmPlane = new Plane(cylinder.MinDistanceAlongAxis, cylinder.Axis);
            btmPlane.SetFacesAndVertices(bottomFaces, true, true);
            var topPlane = new Plane(cylinder.MaxDistanceAlongAxis, cylinder.Axis);
            topPlane.SetFacesAndVertices(topFaces, true, true);
            cylinder.SetFacesAndVertices(outerFaces, true, true);
            var innerCylinder = new Cylinder
            {
                Axis = cylinder.Axis,
                Anchor = cylinder.Anchor,
                Radius = innerRadius,
                MinDistanceAlongAxis = cylinder.MinDistanceAlongAxis,
                MaxDistanceAlongAxis = cylinder.MaxDistanceAlongAxis,
                IsPositive = false
            };
            innerCylinder.SetFacesAndVertices(innerFaces, true, true);

            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [btmPlane, topPlane, cylinder, innerCylinder]
            };
        }
        #endregion

        private static IEnumerable<(Vector3 intersection, double lineT)> GetPrimitiveAndLineIntersections(PrimitiveSurface surface, double xCoord,
             double yCoord, double zCoord)
        {
            if (surface.Faces == null || surface.Faces.Count == 0)
                foreach (var result in surface.LineIntersection(new Vector3(xCoord, yCoord, zCoord), Vector3.UnitX))
                    yield return result;
            else
            {
                foreach (var face in surface.Faces)
                {
                    var intersectPoint = MiscFunctions.PointOnTriangleFromRay(face, new Vector3(0, yCoord, zCoord),
                        Vector3.UnitX, out var t);
                    if (!intersectPoint.IsNull())
                        yield return (intersectPoint, t);
                }
            }
        }
    }
}