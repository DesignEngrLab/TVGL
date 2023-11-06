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

    }
}