// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Plane.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;


namespace TVGL
{
    /// <summary>
    /// A structure encapsulating a 3D Plane
    /// </summary>
    public class Plane : PrimitiveSurface
    {
        /// <summary>
        /// The normal vector of the Plane.
        /// </summary>
        /// <value>The normal.</value>
        public Vector3 Normal { get; set; }
        /// <summary>
        /// The distance of the Plane along its normal from the origin.
        /// </summary>
        /// <value>The distance to origin.</value>
        public double DistanceToOrigin { get; set; }


        public override string KeyString => "Plane|" + Normal.ToString() + "|" + DistanceToOrigin.ToString("F5")
             + GetCommonKeyDetails();

        /// <summary>
        /// Gets as transform from xy plane.
        /// </summary>
        /// <value>As transform from xy plane.</value>
        [JsonIgnore]
        public Matrix4x4 AsTransformFromXYPlane
        {
            get
            {
                if (_asTransformFromXYPlane.IsNull())
                {
                    Normal.TransformToXYPlane(out var rotMatrix);
                    _asTransformFromXYPlane = Matrix4x4.CreateTranslation(0, 0, DistanceToOrigin) * rotMatrix;
                }
                return _asTransformFromXYPlane;
            }
        }
        /// <summary>
        /// As transform from xy plane
        /// </summary>
        Matrix4x4 _asTransformFromXYPlane = Matrix4x4.Null;
        /// <summary>
        /// Gets as transform to xy plane.
        /// </summary>
        /// <value>As transform to xy plane.</value>
        [JsonIgnore]
        public Matrix4x4 AsTransformToXYPlane
        {
            get
            {
                if (_asTransformToXYPlane.IsNull())
                {
                    var rotMatrix = Normal.TransformToXYPlane(out _);
                    _asTransformToXYPlane = rotMatrix * Matrix4x4.CreateTranslation(0, 0, -DistanceToOrigin);
                }
                return _asTransformToXYPlane;
            }
        }
        /// <summary>
        /// As transform to xy plane
        /// </summary>
        Matrix4x4 _asTransformToXYPlane = Matrix4x4.Null;
        /// <summary>
        /// Gets the closest point on the plane to the origin.
        /// </summary>
        /// <value>The closest point to origin.</value>
        [JsonIgnore]
        public Vector3 ClosestPointToOrigin => Normal * DistanceToOrigin;

        public Vector4 AsVector4 => new Vector4(Normal.X, Normal.Y, Normal.Z, -DistanceToOrigin);


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="connectFacesToPrimitive">if set to <c>true</c> [connect faces to primitive].</param>
        public Plane(IEnumerable<TriangleFace> faces, bool connectFacesToPrimitive = true)
        {
            Vertices = new HashSet<Vertex>(faces.SelectMany(f => f.Vertices).Distinct());
            DefineNormalAndDistanceFromVertices(Vertices, out var dto, out var normal);

            LargestFace = faces.MaxBy(f => f.Area);
            if (normal.Dot(LargestFace.Normal) < 0)
            {
                normal *= -1;
                dto *= -1;
            }
            DistanceToOrigin = dto;
            Normal = normal;

            SetFacesAndVertices(faces, connectFacesToPrimitive);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normalGuess">The normal guess.</param>
        //public Plane(IEnumerable<Vector3> vertices, Vector3 normalGuess)
        //{
        //    DefineNormalAndDistanceFromVertices(vertices, out var dto, out var normal);
        //    if (normal.Dot(normalGuess) < 0)
        //    {
        //        normal *= -1;
        //        dto *= -1;
        //    }
        //    DistanceToOrigin = dto;
        //    Normal = normal;
        //}


        /// <summary>
        /// Defines the normal and distance from vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="distanceToPlane">The distance to plane.</param>
        /// <param name="normal">The normal.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool DefineNormalAndDistanceFromVertices(IEnumerable<Vertex> vertices, out double distanceToPlane, out Vector3 normal)
        {
            return DefineNormalAndDistanceFromVertices(vertices.Select(v => v.Coordinates), out distanceToPlane, out normal);
        }

        /// <summary>
        /// Defines the normal and distance from vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="distanceToPlane">The distance to plane.</param>
        /// <param name="normal">The normal.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DefineNormalAndDistanceFromVertices(IEnumerable<Vector3> vertices, out double distanceToPlane,
            out Vector3 normal)
        {
            var pointList = vertices as IList<Vector3> ?? vertices.ToList();
            var numVertices = pointList.Count;
            if (numVertices < 3)
            {
                distanceToPlane = double.NaN;
                normal = Vector3.Null;
                return false;
            }
            if (numVertices == 3)
            {
                var cross = (pointList[1] - pointList[0]).Cross(pointList[2] - pointList[1]);
                var crossLength = cross.Length();
                if (crossLength.IsNegligible())
                {
                    distanceToPlane = double.NaN;
                    normal = Vector3.Null;
                    return false;
                }
                normal = cross / crossLength;
                distanceToPlane = normal.Dot((pointList[0] + pointList[1] + pointList[2]) / 3);
                if (distanceToPlane < 0)
                {
                    distanceToPlane = -distanceToPlane;
                    normal = -normal;
                }
                return true;
            }
            double xSum = 0.0, ySum = 0.0, zSum = 0.0;
            double xSq = 0.0;
            double xy = 0.0, ySq = 0.0;
            double xz = 0.0, yz = 0.0, zSq = 0.0;
            var x = pointList.First().X;
            var y = pointList.First().Y;
            var z = pointList.First().Z;
            var xIsConstant = true;
            var yIsConstant = true;
            var zIsConstant = true;
            foreach (var vertex in pointList)
            {
                if (vertex.IsNull()) continue;
                xIsConstant &= vertex.X.IsPracticallySame(x);
                x = vertex.X;
                yIsConstant &= vertex.Y.IsPracticallySame(y);
                y = vertex.Y;
                zIsConstant &= vertex.Z.IsPracticallySame(z);
                z = vertex.Z;
                xSum += x;
                ySum += y;
                zSum += z;
                xSq += x * x;
                ySq += y * y;
                zSq += z * z;
                xy += x * y;
                xz += x * z;
                yz += y * z;
            }
            if ((xIsConstant && yIsConstant) || (xIsConstant && zIsConstant) || (yIsConstant && zIsConstant))
            {
                distanceToPlane = double.NaN;
                normal = Vector3.Null;
                return false;
            }
            if (xIsConstant)
            {
                if (x < 0)
                {
                    normal = -Vector3.UnitX;
                    distanceToPlane = -x;
                }
                else
                {
                    normal = Vector3.UnitX;
                    distanceToPlane = x;
                }
                return true;
            }
            if (yIsConstant)
            {
                if (y < 0)
                {
                    normal = -Vector3.UnitY;
                    distanceToPlane = -y;
                }
                else
                {
                    normal = Vector3.UnitY;
                    distanceToPlane = y;
                }
                return true;
            }
            if (zIsConstant)
            {
                if (z < 0)
                {
                    normal = -Vector3.UnitZ;
                    distanceToPlane = -z;
                }
                else
                {
                    normal = Vector3.UnitZ;
                    distanceToPlane = z;
                }
                return true;
            }
            var matrix = new double[,] { { xSq, xy, xz }, { xy, ySq, yz }, { xz, yz, zSq } };
            var rhs = new[] { xSum, ySum, zSum };
            if (matrix.solve(rhs, out var normalArray, true))
            {
                normal = new Vector3(normalArray);
                //Check for negligible normal before trying to normalize.
                if (normal.IsNegligible())
                {
                    normal = Vector3.Null;
                    distanceToPlane = double.NaN;
                    return false;
                }
                normal = normal.Normalize();
                distanceToPlane = normal.Dot(new Vector3(xSum / numVertices, ySum / numVertices, zSum / numVertices));
                if (distanceToPlane < 0)
                {
                    distanceToPlane = -distanceToPlane;
                    normal = -normal;
                }
                return true;
            }
            else
            {
                normal = Vector3.Null;
                distanceToPlane = double.NaN;
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        public Plane() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="distanceToOrigin">The distance to origin.</param>
        /// <param name="normal">The normal.</param>
        public Plane(double distanceToOrigin, Vector3 normal) : this()
        {
            Normal = normal.Normalize();
            DistanceToOrigin = distanceToOrigin;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="pointOnPlane">a point on plane.</param>
        /// <param name="normal">The normal.</param>
        public Plane(Vector3 pointOnPlane, Vector3 normal) : this()
        {
            Normal = normal.Normalize();
            DistanceToOrigin = Normal.Dot(pointOnPlane);
            if (DistanceToOrigin < 0)
            {
                DistanceToOrigin = -DistanceToOrigin;
                Normal = -Normal;
            }
        }

        /// <summary>
        /// Gets the adjacent flats.
        /// </summary>
        /// <param name="allFlats">All flats.</param>
        /// <returns>HashSet&lt;Plane&gt;.</returns>
        public HashSet<Plane> GetAdjacentFlats(ICollection<Plane> allFlats)
        {
            var adjacentFlats = new HashSet<Plane>(); //use a hash to avoid duplicates
            var adjacentFaces = GetAdjacentFaces();
            foreach (var flat in allFlats)
            {
                foreach (var face in adjacentFaces)
                {
                    if (flat.Faces.Contains(face))
                    {
                        adjacentFlats.Add(flat);
                        break;
                    }
                }
            }
            return adjacentFlats;
        }


        #endregion
        /// <summary>
        /// Creates a Plane that contains the three given points.
        /// </summary>
        /// <param name="point1">The first point defining the Plane.</param>
        /// <param name="point2">The second point defining the Plane.</param>
        /// <param name="point3">The third point defining the Plane.</param>
        /// <returns>The Plane containing the three points.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane CreateFromVertices(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            double ax = point2.X - point1.X;
            double ay = point2.Y - point1.Y;
            double az = point2.Z - point1.Z;

            double bx = point3.X - point1.X;
            double by = point3.Y - point1.Y;
            double bz = point3.Z - point1.Z;

            // N=Cross(a,b)
            double nx = ay * bz - az * by;
            double ny = az * bx - ax * bz;
            double nz = ax * by - ay * bx;

            // Normalize(N)
            double ls = nx * nx + ny * ny + nz * nz;
            double invNorm = 1.0 / Math.Sqrt(ls);

            Vector3 normal = new Vector3(
                nx * invNorm,
                ny * invNorm,
                nz * invNorm);

            return new Plane(
                normal.X * point1.X + normal.Y * point1.Y + normal.Z * point1.Z, normal);
        }

        /// <summary>
        /// Creates a new Plane whose normal vector is the source Plane's normal vector normalized.
        /// </summary>
        /// <returns>The normalized Plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            double ls = Normal.LengthSquared();
            if (ls.IsPracticallySame(1.0)) return;
            double lengthfactor = 1 / Math.Sqrt(ls);
            Normal *= lengthfactor;
            DistanceToOrigin *= lengthfactor;
        }


        /// <summary>
        /// Transforms to new plane.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>Plane.</returns>
        public Plane TransformToNewPlane(Matrix4x4 matrix, bool transformFacesAndVertices)
        {
            var copy = (Plane)this.Clone();
            copy.Transform(matrix, transformFacesAndVertices);
            return copy;
        }
        /// <summary>
        /// Transforms a normalized Plane by a Matrix.
        /// </summary>
        /// <param name="matrix">The transformation matrix to apply to the Plane.</param>
        /// <returns>The transformed Plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Transform(Matrix4x4 matrix, bool transformFacesAndVertices)
        {
            base.Transform(matrix, transformFacesAndVertices);
            var pointOnPlane = DistanceToOrigin * Normal;
            pointOnPlane = pointOnPlane.Transform(matrix);
            Normal = Normal.TransformNoTranslate(matrix);
            DistanceToOrigin = Normal.Dot(pointOnPlane);
            // it seems like there is a quicker way to do this, but I run into problems when DistanceToOrigin = 0
        }


        /// <summary>
        /// Transforms to new plane.
        /// </summary>
        /// <param name="rotation">The rotation.</param>
        /// <returns>Plane.</returns>
        public Plane TransformToNewPlane(Quaternion rotation,bool transformFacesAndVertices)
        {
            var copy = (Plane)this.Clone();
            copy.Transform(rotation, transformFacesAndVertices);
            return copy;
        }

        /// <summary>
        /// Transforms a normalized Plane by a Quaternion rotation.
        /// </summary>
        /// <param name="rotation">The Quaternion rotation to apply to the Plane.</param>
        /// <returns>A new Plane that results from applying the rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transform(Quaternion rotation, bool transformFacesAndVertices)
        {
            // Compute rotation matrix.
            base.Transform(Matrix4x4.Identity, transformFacesAndVertices);
            double x2 = rotation.X + rotation.X;
            double y2 = rotation.Y + rotation.Y;
            double z2 = rotation.Z + rotation.Z;

            double wx2 = rotation.W * x2;
            double wy2 = rotation.W * y2;
            double wz2 = rotation.W * z2;
            double xx2 = rotation.X * x2;
            double xy2 = rotation.X * y2;
            double xz2 = rotation.X * z2;
            double yy2 = rotation.Y * y2;
            double yz2 = rotation.Y * z2;
            double zz2 = rotation.Z * z2;

            double m11 = 1.0 - yy2 - zz2;
            double m21 = xy2 - wz2;
            double m31 = xz2 + wy2;

            double m12 = xy2 + wz2;
            double m22 = 1.0 - xx2 - zz2;
            double m32 = yz2 - wx2;

            double m13 = xz2 - wy2;
            double m23 = yz2 + wx2;
            double m33 = 1.0 - xx2 - yy2;


            Normal = new Vector3(
                Normal.X * m11 + Normal.Y * m21 + Normal.Z * m31,
                Normal.X * m12 + Normal.Y * m22 + Normal.Z * m32,
                Normal.X * m13 + Normal.Y * m23 + Normal.Z * m33);
        }

        /// <summary>
        /// Calculates the dot product of a Plane and Vector4.
        /// </summary>
        /// <param name="plane">The Plane.</param>
        /// <param name="value">The Vector4.</param>
        /// <returns>The dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DotCoordinate(Plane plane, Vector3 value)
        {
            return plane.Normal.X * value.X +
                   plane.Normal.Y * value.Y +
                   plane.Normal.Z * value.Z +
                   plane.DistanceToOrigin;
        }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The Vector3.</param>
        /// <returns>The resulting dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DotNormal(Plane plane, Vector3 value)
        {
            return plane.Normal.X * value.X +
                   plane.Normal.Y * value.Y +
                   plane.Normal.Z * value.Z;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Plane is equal to this Plane instance.
        /// </summary>
        /// <param name="other">The Plane to compare this instance to.</param>
        /// <returns>True if the other Plane is equal to this instance; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SameLocation(Plane other)
        {
            return (Normal.X == other.Normal.X &&
                    Normal.Y == other.Normal.Y &&
                    Normal.Z == other.Normal.Z &&
                    DistanceToOrigin == other.DistanceToOrigin);
        }

        /// <summary>
        /// Returns a String representing this Plane instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;

            return string.Format(ci, "{{Normal:{0} D:{1}}}", Normal.ToString(), DistanceToOrigin.ToString(ci));
        }

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            return point.Transform(AsTransformToXYPlane).ToVector2();
        }

        /// <summary>
        /// Transforms the from2 d to3 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var v = new Vector3(point.X, point.Y, 0);
            return v.Transform(AsTransformFromXYPlane);
        }
        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            foreach (var point in points)
            {
                yield return TransformFrom3DTo2D(point);
            }
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var d = point.Dot(Normal) - DistanceToOrigin;
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }
        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            return Normal;
        }
        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any() || Area.IsNegligible()) return;
            isPositive = LargestFace.Normal.Dot(Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            MinX = MinY = MinZ = double.NegativeInfinity;
            MaxX = MaxY = MaxZ = double.PositiveInfinity;
        }

        /// <summary>
        /// Finds where the line intersects the plane. Returns null if the line is parallel to the plane.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            yield return LineIntersection(Normal, DistanceToOrigin, anchor, direction);
        }

        /// <summary>
        /// Finds where the line intersects the plane. Returns null if the line is parallel to the plane.
        /// </summary>
        /// <param name="planeNormal"></param>
        /// <param name="planeDistanceToOrigin"></param>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static (Vector3 intersection, double lineT) LineIntersection(Vector3 planeNormal,
            double planeDistanceToOrigin, Vector3 anchor, Vector3 direction)
        {
            var intersectPoint = MiscFunctions.PointOnPlaneFromLine(planeNormal, planeDistanceToOrigin, anchor,
                direction, out var t);
            if (intersectPoint.IsNull())
                return (Vector3.Null, double.NaN);
            return (intersectPoint, t);
        }

        /// <summary>
        /// Creates a plane from fitting the given vertices. The normal is guessed from the given normalGuess.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="normalGuess"></param>
        /// <returns></returns>
        public static Plane FitToVertices(IEnumerable<Vector3> points, Vector3 normalGuess, out double maxError)
        {
            maxError = double.MaxValue;
            if (!DefineNormalAndDistanceFromVertices(points, out var distanceToPlane, out var planeNormal))
                return null;

            if (!normalGuess.IsNull() && normalGuess.Dot(planeNormal) < 0)
            {
                distanceToPlane = -distanceToPlane;
                planeNormal = -planeNormal;
            }
            var primitiveSurface = new Plane(distanceToPlane, planeNormal);
            maxError = primitiveSurface.CalculateMaxError(points);
            return primitiveSurface;
        }
    }
}
