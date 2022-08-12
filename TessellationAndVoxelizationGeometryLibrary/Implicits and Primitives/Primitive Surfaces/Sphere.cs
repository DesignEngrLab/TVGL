// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace TVGL
{
    /// <summary>
    ///     Class Sphere.
    /// </summary>
    public class Sphere : PrimitiveSurface
    {

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Center = Center.Transform(transformMatrix);
            var rVector1 = Radius * Vector3.UnitX;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            var rVector2 = Radius * Vector3.UnitY;
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            var rVector3 = Radius * Vector3.UnitZ;
            rVector3 = rVector3.TransformNoTranslate(transformMatrix);
            rVector1 *= Radius;
            Radius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared() + rVector3.LengthSquared()) / 3);
        }

        /// <summary>
        /// Calculates the error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }

            var maxError = 0.0;
            foreach (var c in vertices)
            {
                var d = Math.Abs((c - Center).Length() - Radius);
                if (d > maxError)
                    maxError = d;
            }
            return maxError;
        }

        /// <summary>
        /// Defines the sphere from points.
        /// Sumith YD, "Fast Geometric Fit Algorithm for Sphere Using Exact Solution", arXiv: 1506.02776
        /// </summary>
        /// <param name="verts">The verts.</param>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DefineSphereFromVertices(IEnumerable<Vector3> verts, out Vector3 center, out double radius)
        {
            var numVerts = 0;
            double Sx = 0.0, Sy = 0.0, Sz = 0.0;
            double Sxx = 0.0;
            double Sxy = 0.0, Syy = 0.0;
            double Sxz = 0.0, Syz = 0.0, Szz = 0.0;
            double Sxxx = 0.0, Sxxy = 0.0, Sxyy = 0.0, Syyy = 0.0;
            double Sxxz = 0.0, Sxzz = 0.0, Szzz = 0.0;
            double Syyz = 0.0, Syzz = 0.0;
            foreach (var vertex in verts)
            {
                var x = vertex.X;
                var y = vertex.Y;
                var z = vertex.Z;
                Sx += x;
                Sy += y;
                Sz += z;
                Sxx += x * x;
                Syy += y * y;
                Szz += z * z;
                Sxy += x * y;
                Sxz += x * z;
                Syz += y * z;
                Sxxx += x * x * x;
                Sxxy += x * x * y;
                Sxyy += x * y * y;
                Syyy += y * y * y;
                Sxxz += x * x * z;
                Sxzz += x * z * z;
                Szzz += z * z * z;
                Syyz += y * y * z;
                Syzz += y * z * z;
                numVerts++;
            }
            if (numVerts < 3)
            {
                radius = double.NaN;
                center = Vector3.Null;
                return false;
            }
            var A1 = Sxx + Syy + Szz;
            var a = 2 * Sx * Sx - 2 * numVerts * Sxx;
            var b = 2 * Sx * Sy - 2 * numVerts * Sxy;
            var c = 2 * Sx * Sz - 2 * numVerts * Sxz;
            var d = -numVerts * (Sxxx + Sxyy + Sxzz) + A1 * Sx;
            var e = b; // 2 * Sx * Sy - 2 * N * Sxy;
            var f = 2 * Sy * Sy - 2 * numVerts * Syy;
            var g = 2 * Sy * Sz - 2 * numVerts * Syz;
            var h = -numVerts * (Sxxy + Syyy + Syzz) + A1 * Sy;
            var j = c; // 2 * Sx * Sz - 2 * N * Sxz;
            var k = g; // 2 * Sy * Sz - 2 * N * Syz;
            var l = 2 * Sz * Sz - 2 * numVerts * Szz;
            var m = -numVerts * (Sxxz + Syyz + Szzz) + A1 * Sz;
            var delta = a * (f * l - g * k) - e * (b * l - c * k) + j * (b * g - c * f);
            var xc = (d * (f * l - g * k) - h * (b * l - c * k) + m * (b * g - c * f)) / delta;
            var yc = (a * (h * l - m * g) - e * (d * l - m * c) + j * (d * g - h * c)) / delta;
            var zc = (a * (f * m - h * k) - e * (b * m - d * k) + j * (b * h - d * f)) / delta;
            radius = Math.Sqrt(xc * xc + yc * yc + zc * zc + (A1 - 2 * (xc * Sx + yc * Sy + zc * Sz)) / numVerts);
            center = new Vector3(xc, yc, zc);
            return true;
        }

        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;
        private Vector3 faceZDir = Vector3.Null;

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = point - Center;
            if (faceXDir.IsNull())
            {
                faceXDir = Vector3.UnitX;
                faceYDir = Vector3.UnitY;
                faceZDir = Vector3.UnitZ;
            }
            var x = faceXDir.Dot(v) / Radius;
            var y = faceYDir.Dot(v) / Radius;
            var z = faceZDir.Dot(v) / Radius;
            var (polarAngle, azimuthAngle) = Sphere.ConvertToSphericalAngles(x, y, z);

            return new Vector2(azimuthAngle * Radius, polarAngle * Radius);
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            if (faceXDir.IsNull())
            {
                faceXDir = Vector3.UnitX;
                faceYDir = Vector3.UnitY;
                faceZDir = Vector3.UnitZ;
            }
            var azimuthAngle = point.X / Radius;
            var polarAngle = point.Y / Radius;
            var baseCoord = ConvertSphericalToCartesian(Radius, polarAngle, azimuthAngle);
            var rotateMatrix = new Matrix4x4(faceXDir, faceYDir, faceZDir, Vector3.Zero);
            baseCoord = baseCoord.Transform(rotateMatrix);
            return baseCoord + Center;
        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            var pointsList = points as IList<Vector3> ?? points.ToList();
            var pointcenter = pointsList.Aggregate((sum, v) => sum + v) / pointsList.Count;
            faceZDir = (pointcenter - Center).Normalize();
            faceYDir = faceZDir.GetPerpendicularDirection();
            faceXDir = faceYDir.Cross(faceZDir);
            foreach (var p in pointsList)
            {
                yield return TransformFrom3DTo2D(p);
            }
        }

        /// <summary>
        /// Converts a cartesian coordinate (with a provided center) to spherical angles.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="center">The center.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static (double, double) ConvertToSphericalAngles(Vector3 point, Vector3 center)
        {
            return ConvertToSphericalAngles(point - center);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static (double, double) ConvertToSphericalAngles(Vector3 p)
        {
            return ConvertToSphericalAngles(p.X, p.Y, p.Z);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static (double, double) ConvertToSphericalAngles(double x, double y, double z)
        {
            // this follow the description and first figure at https://wikipedia.org/wiki/Spherical_coordinate_system
            // ISO physics convention, where polar angle is measured as deviation from z axis
            var polarAngle = Math.Acos(z);
            var azimuthalX = Math.Acos(x);
            var azimuthalY = Math.Acos(y);
            var azimuthAngle = Math.Atan2(azimuthalY, azimuthalX);
            return (polarAngle, azimuthAngle);
        }


        /// <summary>
        /// Converts the spherical coordinate to cartesian coordinates.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="polarAngle">The polar angle.</param>
        /// <param name="azimuthAngle">The azimuth angle.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 ConvertSphericalToCartesian(double radius, double polarAngle, double azimuthAngle)
        {
            var sinPolar = Math.Sin(polarAngle);
            var cosPolar = Math.Cos(polarAngle);
            var sinAzimuth = Math.Sin(azimuthAngle);
            var cosAzimuth = Math.Cos(azimuthAngle);

            return new Vector3(radius * cosAzimuth * sinPolar, radius * sinAzimuth * sinPolar, radius * cosPolar);
        }


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Sphere(Vector3 center, double radius, bool isPositive, IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Center = center;
            IsPositive = isPositive;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Sphere(Vector3 center, double radius, bool isPositive)
        {
            Center = center;
            IsPositive = isPositive;
            Radius = radius;
        }

        public Sphere() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Sphere(Sphere originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Center = originalToBeCopied.Center;
            Radius = originalToBeCopied.Radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Sphere(Sphere originalToBeCopied, int[] newFaceIndices, TessellatedSolid copiedTessellatedSolid)
            : base(newFaceIndices, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Center = originalToBeCopied.Center;
            Radius = originalToBeCopied.Radius;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Is the sphere positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center { get; set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }

        #endregion
    }
}