// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Sphere.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace TVGL
{
    /// <summary>
    /// Class Sphere.
    /// </summary>
    public class Sphere : PrimitiveSurface
    {

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix, bool transformFacesAndVertices)
        {
            base.Transform(transformMatrix, transformFacesAndVertices);
            var rVector1 = Radius * Vector3.UnitX;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            var rVector2 = Radius * Vector3.UnitY;
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            var rVector3 = Radius * Vector3.UnitZ;
            rVector3 = rVector3.TransformNoTranslate(transformMatrix);
            Radius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared() + rVector3.LengthSquared()) / 3);
            Center = Center.Transform(transformMatrix);
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

        public static Sphere CreateFrom4Points(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            /* see comment at :https://math.stackexchange.com/questions/2585392/equation-of-the-sphere-that-passes-through-4-points 
             * take the general equation of a sphere: (x_i − a)^2 + (y_i − b)^2 + (z_i − c)^2 = r^2
             * we have 4 unknowns: a, b, c, r where (a, b, c) is the center of the sphere and r is the radius.
             * x_i, y_i, z_i are the four points p1, p2, p3, p4 
             * we can make 3 linear equations where the unknowns are a,b & c by subtracting this equation
             * for example the equation for p_1 (x_1, y_1, z_1) is subtracted from p2 (x_2, y_2, z_2)
             * to create: x_1^2−x_2^2 + 2a(x2−x1) + y_1^2 − y_2^2 + 2b(y_2−y_1) + z_1^2 − z_2^2 + 2c(z2−z1)=0
             * do two more such subtraction, say: p_2 - p_3 and p_3 - p_4.
             * then solve for a, b, & c
             * Solve for these, then easily obtain r.
             * rearranging the difference equation to get coefficients for the matrix
             * 2(x_j - x_i)a + 2(y_j -y_i)b + 2(z_j - z_i)c = x_j^2 − x_i^2 + y_j^2 - y_i^2 + zj^2 - z_i^2 */
            var matrix = new Matrix3x3(
                2 * (p2.X - p1.X), 2 * (p2.Y - p1.Y), 2 * (p2.Z - p1.Z),
                2 * (p3.X - p2.X), 2 * (p3.Y - p2.Y), 2 * (p3.Z - p2.Z),
                2 * (p4.X - p3.X), 2 * (p4.Y - p3.Y), 2 * (p4.Z - p3.Z));
            var b = new Vector3(
                p2.X * p2.X - p1.X * p1.X + p2.Y * p2.Y - p1.Y * p1.Y + p2.Z * p2.Z - p1.Z * p1.Z,
                p3.X * p3.X - p2.X * p2.X + p3.Y * p3.Y - p2.Y * p2.Y + p3.Z * p3.Z - p2.Z * p2.Z,
                p4.X * p4.X - p3.X * p3.X + p4.Y * p4.Y - p3.Y * p3.Y + p4.Z * p4.Z - p3.Z * p3.Z);
            var center = matrix.Solve(b);
            var radius = (p1 - center).Length();
            return new Sphere(center, radius, null);
        }

        public static Sphere CreateFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var planeNormal = (p2 - p1).Cross(p3 - p1).Normalize();
            var midPoint1 = 0.5 * (p1 + p2);
            var planeDist = planeNormal.Dot(midPoint1);
            var normal1 = (p2 - p1).Normalize();
            var dist1 = normal1.Dot(midPoint1);
            var midPoint2 = 0.5 * (p1 + p3);
            var normal2 = (p3 - p1).Normalize();
            var dist2 = normal2.Dot(midPoint2);
            var center = MiscFunctions.PointCommonToThreePlanes(planeNormal, planeDist, normal1, dist1, normal2,
                dist2);
            return new Sphere(center, (p1 - center).Length(), null);
        }

        public static Sphere CreateFrom2Points(Vector3 p1, Vector3 p2)
        {
            var center = 0.5 * (p1 + p2);
            return new Sphere(center, (p1 - center).Length(), null);
        }

        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 faceXDir = Vector3.Null;
        /// <summary>
        /// The face y dir
        /// </summary>
        private Vector3 faceYDir = Vector3.Null;
        /// <summary>
        /// The face z dir
        /// </summary>
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
            var sphericalAngles = TVGL.SphericalAnglePair.ConvertToSphericalAngles(x, y, z);

            return new Vector2(sphericalAngles.AzimuthAngle * Radius, sphericalAngles.PolarAngle * Radius);
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
            var baseCoord = TVGL.SphericalAnglePair.ConvertSphericalToCartesian(Radius, polarAngle, azimuthAngle);
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


        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var d = (point - Center).Normalize();
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var d = (point - Center).Length() - Radius;
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }
        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="gradient">The gradient.</param>
        /// <returns>System.Double.</returns>
        public double PointMembership(Vector3 point, out Vector3 gradient)
        {
            var v = point - Center;
            var vLength = v.Length();
            var distance = vLength - Radius;
            gradient = distance * v / vLength;
            return distance;
        }
        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="gradient">The gradient.</param>
        /// <param name="curvatureDiagonalTerms">The curvature diagonal terms.</param>
        /// <param name="curvatureCrossTerms">The curvature cross terms.</param>
        /// <returns>System.Double.</returns>
        public double PointMembership(Vector3 point, out Vector3 gradient, out Vector3 curvatureDiagonalTerms,
            out Vector3 curvatureCrossTerms)
        {
            var v = point - Center;
            var vLength = v.Length();
            gradient = (point - Center) / vLength;
            var denominator = vLength * v.LengthSquared();
            curvatureDiagonalTerms = new Vector3((v.Y * v.Y + v.Z * v.Z) / denominator,
                (v.X * v.X + v.Z * v.Z) / denominator,
            (v.X * v.X + v.Y * v.Y) / denominator);
            curvatureCrossTerms = new Vector3(-v.X * v.Y / denominator,
                -v.X * v.Z / denominator,
                -v.Y * v.Z / denominator);
            return vLength - Radius;
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any() || Area.IsNegligible()) return;
            isPositive = (LargestFace.Center - Center).Dot(LargestFace.Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            MinX = Center.X - Radius;
            MaxX = Center.X + Radius;
            MinY = Center.Y - Radius;
            MaxY = Center.Y + Radius;
            MinZ = Center.Z - Radius;
            MaxZ = Center.Z + Radius;
        }

        /// <summary>
        /// Returns the intersections between the line specified by the arguments and this sphere.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            return LineIntersection(Center, Radius, anchor, direction);
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
        public static IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 center, double radius, Vector3 anchor, Vector3 direction)
        {
            // make a triangle from the center of the sphere to the anchor and the anchor plus the direction to the closest point on the line
            var toCenter = center - anchor;
            direction = direction.Normalize();
            var tCenter = toCenter.Dot(direction); // parametric distance from anchor to closest point on line
            var chordCenter = anchor + tCenter * direction; // the point on the line closest to the center of the sphere
            var chordLengthSqd = (chordCenter - center).LengthSquared(); // squared distance from chordCenter to center of sphere
            if (chordLengthSqd.IsPracticallySame(radius * radius)) // one intersection
                yield return (chordCenter, tCenter);
            if (chordLengthSqd > radius * radius) // no intersection
                yield break;
            var halfChordLength = Math.Sqrt(radius * radius - chordLengthSqd);
            yield return (chordCenter - halfChordLength * direction, tCenter - halfChordLength);
            yield return (chordCenter + halfChordLength * direction, tCenter + halfChordLength);
        }

        public static Sphere FitToVertices(double maxRadius, IEnumerable<Vector3> points, Vector3 firstNormal
            , out double maxError)
        {
            maxError = double.MaxValue;
            if (DefineSphereFromVertices(points, out var sphereCenter, out var sphereRadius)
                && sphereRadius < maxRadius && !SphereIsTooFlat(sphereCenter, points))
            {
                var primitiveSurface = new Sphere(sphereCenter, sphereRadius, (points.First() - sphereCenter).Dot(firstNormal) > 0);
                maxError = primitiveSurface.CalculateMaxError(points);
                return primitiveSurface;
            }
            return null;
        }

        public static bool SphereIsTooFlat(Vector3 center, IEnumerable<Vector3> vertices)
        {
            var normals = new List<Vector3>();
            foreach (var vertex in vertices)
                normals.Add((vertex - center).Normalize());
            for (int i = 0; i < normals.Count - 1; i++)
                for (int j = i + 1; j < normals.Count; j++)
                {
                    if (!normals[i].IsAligned(normals[j]))
                        return false;
                }
            return true;
        }

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere" /> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Sphere(Vector3 center, double radius, bool isPositive, IEnumerable<TriangleFace> faces)
        {
            Center = center;
            this.isPositive = isPositive;
            Radius = radius;

            SetFacesAndVertices(faces);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere" /> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Sphere(Vector3 center, double radius, bool? isPositive)
        {
            Center = center;
            this.isPositive = isPositive;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        public Sphere() { }

        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center { get; set; }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }

        public override string KeyString => "Sphere|" + Center.ToString() + "|" + Radius.ToString("F5") + GetCommonKeyDetails();

        #endregion
    }
}