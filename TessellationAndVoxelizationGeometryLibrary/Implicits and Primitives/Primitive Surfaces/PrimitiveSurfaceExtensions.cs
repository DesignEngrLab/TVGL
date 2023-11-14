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
            return border.GetVectors().BorderEncirclesAxis(axis, anchor);
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
            return border.GetVectors().BorderEncirclesAxis(transform, anchor);
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
            var vectors = surface.Borders[0].GetVectors().Concat(new[] { surface.Borders[0].GetVectors().First() });
            FindWindingAroundAxis(vectors, transform, surface.GetAnchor(), out var globalMinAngle, out var globalMaxAngle);

            foreach (var path in surface.Borders.Skip(1))
            {
                vectors = path.GetVectors().Concat(new[] { path.GetVectors().First() });
                FindWindingAroundAxis(vectors, transform, surface.GetAnchor(), out var minAngle, out var maxAngle);
                if (globalMinAngle > minAngle) globalMinAngle = minAngle;
                if (globalMaxAngle < maxAngle) globalMaxAngle = maxAngle;
            }
            vectorAtMinAngle = new Vector3(Math.Cos(globalMinAngle), Math.Sin(globalMinAngle), 0).TransformNoTranslate(backTransform);
            vectorAtMaxAngle = new Vector3(Math.Cos(globalMaxAngle), Math.Sin(globalMaxAngle), 0).TransformNoTranslate(backTransform);
            return globalMaxAngle - globalMinAngle;
        }


        /// <summary>
        /// Finds the total winding angle around the axis and provides the minimum and maximum angle.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="minAngle">The min angle.</param>
        /// <param name="maxAngle">The max angle.</param>
        /// <returns>A double.</returns>
        public static double FindWindingAroundAxis(this IEnumerable<Vector3> path, Matrix4x4 transform, Vector3 anchor,
            out double minAngle, out double maxAngle)
        {
            var coords = path.Select(v => v.ConvertTo2DCoordinates(transform));
            var center = anchor.ConvertTo2DCoordinates(transform);
            var startPoint = coords.First();
            var prevVector = startPoint - center;
            var angleSum = 0.0;
            var startingAngle = Math.Atan2(prevVector.Y, prevVector.X);
            minAngle = startingAngle;
            maxAngle = startingAngle;
            foreach (var coord in coords.Skip(1))
            {
                var nextVector = coord - center;
                var angleDelta = Math.Atan2(prevVector.Cross(nextVector), prevVector.Dot(nextVector));
                angleSum += angleDelta;
                startingAngle += angleDelta;
                if (minAngle > startingAngle) minAngle = startingAngle;
                if (maxAngle < startingAngle) maxAngle = startingAngle;
                prevVector = nextVector;
            }
            while (minAngle < -Math.PI)
            {
                minAngle += Math.Tau;
                maxAngle += Math.Tau;
            }
            while (minAngle > Math.PI)
            {
                minAngle -= Math.Tau;
                maxAngle -= Math.Tau;
            }
            return Math.Abs(angleSum);
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
        /// Finds the intersection between a sphere and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <param name="anchor">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public static bool SphereLineIntersection(this Sphere sphere, Vector3 anchor, Vector3 direction, out Vector3 point1, out Vector3 point2, out double t1, out double t2)
        {
            return SphereLineIntersection(sphere.Center, sphere.Radius, anchor, direction, out point1, out point2, out t1, out t2);
        }


        /// <summary>
        /// Finds the intersection between a sphere and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="center">The center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="anchor">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public static bool SphereLineIntersection(Vector3 center, double radius, Vector3 anchor, Vector3 direction, out Vector3 point1, out Vector3 point2, out double t1, out double t2)
        {
            // make a triangle from the center of the sphere to the anchor and the anchor plus the direction to the closest point on the line
            var toCenter = center - anchor;
            direction = direction.Normalize();
            var tCenter = toCenter.Dot(direction); // parametric distance from anchor to closest point on line
            var chordCenter = center + tCenter * direction; // the point on the line closest to the center of the sphere
            var chordLengthSqd = (chordCenter - center).LengthSquared(); // squared distance from chordCenter to center of sphere
            if (chordLengthSqd.IsPracticallySame(radius * radius)) // one intersection
            {
                point1 = point2 = chordCenter;
                t1 = t2 = tCenter;
                return true;
            }
            if (chordLengthSqd > radius * radius) // no intersection
            {
                point1 = point2 = Vector3.Null;
                t1 = t2 = double.NaN;
                return false;
            }
            var halfChordLength = Math.Sqrt(radius * radius - chordLengthSqd);
            point1 = chordCenter - halfChordLength * direction;
            point2 = chordCenter + halfChordLength * direction;
            t1 = tCenter - halfChordLength;
            t2 = tCenter + halfChordLength;
            return true;
        }

        /// <summary>
        /// Finds the intersection between a cylinder and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="cylinder">The cylinder.</param>
        /// <param name="anchor">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        public static bool CylinderLineIntersection(this Cylinder cylinder, Vector3 anchor, Vector3 direction, out Vector3 point1, out Vector3 point2, out double t1, out double t2)
        {
            return CylinderLineIntersection(cylinder.Axis, cylinder.Radius, cylinder.Anchor, anchor, direction, out point1, out point2, out t1, out t2);
        }

        /// <summary>
        /// Finds the intersection between a cylinder and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="axis">The axis of the cylinder.</param>
        /// <param name="radius">The radius of the cylinder.</param>
        /// <param name="anchorCyl">The anchor of the cylinder..</param>
        /// <param name="anchorLine">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public static bool CylinderLineIntersection(Vector3 axis, double radius, Vector3 anchorCyl, Vector3 anchorLine, Vector3 direction, out Vector3 point1, out Vector3 point2, out double t1, out double t2)
        {
            direction = direction.Normalize();
            var minDistance = MiscFunctions.SkewedLineIntersection(anchorCyl, axis, anchorLine, direction, out _, out var cylAxisPoint, out var linePoint, out _,
                out var tChordCenter);

            if (minDistance.IsPracticallySame(radius)) // one intersection
            {
                point1 = point2 = linePoint;
                t1 = t2 = tChordCenter;
                return true;
            }
            if (minDistance > radius) // no intersection
            {
                point1 = point2 = Vector3.Null;
                t1 = t2 = double.NaN;
                return false;
            }
            // here, the halfChoordLength is the distance from the chordCenter to where it would intersect the circle of the cylinder
            var halfChordLength = Math.Sqrt(radius * radius - minDistance * minDistance);
            var sinAngleLineCylinder = axis.Cross(direction).Length();
            var distanceToCylinder = halfChordLength / sinAngleLineCylinder;
            t1 = tChordCenter - distanceToCylinder;
            t2 = tChordCenter + distanceToCylinder;
            point1 = linePoint - distanceToCylinder * direction;
            point2 = linePoint + distanceToCylinder * direction;
            return true;
        }
    }
}