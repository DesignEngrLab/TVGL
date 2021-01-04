// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using TVGL.Numerics;
using StarMathLib;

namespace TVGL // COMMENTEDCHANGE namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating a 3D Plane
    /// </summary>
    public class Plane : PrimitiveSurface // IEquatable<Plane> commenting this since sometimes two planes at same "location"
                                          // but represent different patches
    {
        /// <summary>
        /// The normal vector of the Plane.
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// The distance of the Plane along its normal from the origin.
        /// </summary>
        public double DistanceToOrigin;


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
        Matrix4x4 _asTransformFromXYPlane = Matrix4x4.Null;
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
        Matrix4x4 _asTransformToXYPlane = Matrix4x4.Null;
        /// <summary>
        /// Gets the closest point on the plane to the origin.
        /// </summary>
        /// <value>The closest point to origin.</value>
        public Vector3 ClosestPointToOrigin => Normal * DistanceToOrigin;


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public Plane(IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Vertices = new HashSet<Vertex>(faces.SelectMany(f => f.Vertices).Distinct());
            DefineNormalAndDistanceFromVertices(Vertices, out var dto, out var normal);
            if (normal.Dot(faces.First().Normal) < 0)
            {
                normal *= -1;
                dto *= -1;
            }
            DistanceToOrigin = dto;
            Normal = normal;
        }


        internal static bool DefineNormalAndDistanceFromVertices(IEnumerable<Vertex> vertices, out double distanceToPlane, out Vector3 normal)
        {
            return DefineNormalAndDistanceFromVertices(vertices.Select(v => v.Coordinates), out distanceToPlane, out normal);
        }
        internal static bool DefineNormalAndDistanceFromVertices(IEnumerable<Vector3> vertices, out double distanceToPlane, out Vector3 normal)
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
            foreach (var vertex in pointList)
            {
                var x = vertex.X;
                var y = vertex.Y;
                var z = vertex.Z;
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
            var matrix = new double[,] { { xSq, xy, xz }, { xy, ySq, yz }, { xz, yz, zSq } };
            var rhs = new[] { xSum, ySum, zSum };
            if (matrix.solve(rhs, out var normalArray, true))
            {
                normal = (new Vector3(normalArray)).Normalize();
                distanceToPlane = normal.Dot(new Vector3(xSum / numVertices, ySum / numVertices, zSum / numVertices));
                if (distanceToPlane < 0)
                {
                    distanceToPlane = -distanceToPlane;
                    normal = -normal;
                }
                return true;
            }
            var absoluteDiff = Vector3.Zero;
            for (int i = 0, j = numVertices - 1; i < numVertices; j = i++)
                absoluteDiff += new Vector3(Math.Abs(pointList[i].X - pointList[j].X), Math.Abs(pointList[i].Y - pointList[j].Y),
                    Math.Abs(pointList[i].Z - pointList[j].Z));
            if (absoluteDiff.X.IsNegligible()) normal = Vector3.UnitX;
            else if (absoluteDiff.Y.IsNegligible()) normal = Vector3.UnitY;
            else if (absoluteDiff.Z.IsNegligible()) normal = Vector3.UnitZ;
            else
            {
                normal = Vector3.Null;
                distanceToPlane = double.NaN;
                return false;
            }
            distanceToPlane = pointList[0].Dot(normal);
            return true;
            //throw new Exception("Some how all vertices on faces passed into Plane constructor are collinear.");
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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                Vector3 a = point2 - point1;
                Vector3 b = point3 - point1;

                // N = Cross(a, b)
                Vector3 n = Vector3.Cross(a, b);
                Vector3 normal = Vector3.Normalize(n);

                // D = - Dot(N, point1)
                double d = -Vector3.Dot(normal, point1);

                return new Plane(d, normal);
            }
            else
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
                    -(normal.X * point1.X + normal.Y * point1.Y + normal.Z * point1.Z), normal);
            }
        }

        /// <summary>
        /// Creates a new Plane whose normal vector is the source Plane's normal vector normalized.
        /// </summary>
        /// <param name="value">The source Plane.</param>
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
        public Plane TransformToNewPlane(Matrix4x4 matrix)
        {
            var copy = this.Copy(true);
            copy.Transform(matrix);
            return copy;
        }
        /// <summary>
        /// Transforms a normalized Plane by a Matrix.
        /// </summary>
        /// <param name="plane"> The normalized Plane to transform.
        /// This Plane must already be normalized, so that its Normal vector is of unit length, before this method is called.</param>
        /// <param name="matrix">The transformation matrix to apply to the Plane.</param>
        /// <returns>The transformed Plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Transform(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var invMatrix);

            Normal = new Vector3(
                Normal.X * invMatrix.M11 + Normal.Y * invMatrix.M12 + Normal.Z * invMatrix.M13 + DistanceToOrigin * invMatrix.M14,
                Normal.X * invMatrix.M21 + Normal.Y * invMatrix.M22 + Normal.Z * invMatrix.M23 + DistanceToOrigin * invMatrix.M24,
                Normal.X * invMatrix.M31 + Normal.Y * invMatrix.M32 + Normal.Z * invMatrix.M33 + DistanceToOrigin * invMatrix.M34);

            this.DistanceToOrigin = Normal.X * invMatrix.M41 + Normal.Y * invMatrix.M42 + Normal.Z * invMatrix.M43 + DistanceToOrigin * invMatrix.M44;
        }


        /// <summary>
        /// Transforms to new plane.
        /// </summary>
        /// <param name="rotation">The rotation.</param>
        /// <returns>Plane.</returns>
        public Plane TransformToNewPlane(Quaternion rotation)
        {
            var copy = this.Copy(true);
            copy.Transform(rotation);
            return copy;
        }

        /// <summary>
        ///  Transforms a normalized Plane by a Quaternion rotation.
        /// </summary>
        /// <param name="plane"> The normalized Plane to transform.
        /// This Plane must already be normalized, so that its Normal vector is of unit length, before this method is called.</param>
        /// <param name="rotation">The Quaternion rotation to apply to the Plane.</param>
        /// <returns>A new Plane that results from applying the rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transform(Quaternion rotation)
        {
            // Compute rotation matrix.
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
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static double Dot(Plane plane, Vector4 value)
        //{
        //    return plane.Normal.X * value.X +
        //           plane.Normal.Y * value.Y +
        //           plane.Normal.Z * value.Z +
        //           plane.D * value.W;
        //}

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the normal vector of this Plane plus the distance (D) value of the Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The Vector3.</param>
        /// <returns>The resulting value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DotCoordinate(Plane plane, Vector3 value)
        {
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                return Vector3.Dot(plane.Normal, value) + plane.DistanceToOrigin;
            }
            else
            {
                return plane.Normal.X * value.X +
                       plane.Normal.Y * value.Y +
                       plane.Normal.Z * value.Z +
                       plane.DistanceToOrigin;
            }
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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                return Vector3.Dot(plane.Normal, value);
            }
            else
            {
                return plane.Normal.X * value.X +
                       plane.Normal.Y * value.Y +
                       plane.Normal.Z * value.Z;
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Plane is equal to this Plane instance.
        /// </summary>
        /// <param name="other">The Plane to compare this instance to.</param>
        /// <returns>True if the other Plane is equal to this instance; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SameLocation(Plane other)
        {
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                return this.Normal.Equals(other.Normal) && this.DistanceToOrigin == other.DistanceToOrigin;
            }
            else
            {
                return (Normal.X == other.Normal.X &&
                        Normal.Y == other.Normal.Y &&
                        Normal.Z == other.Normal.Z &&
                        DistanceToOrigin == other.DistanceToOrigin);
            }
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
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        /*
        public override int GetHashCode()
        {
            return Normal.GetHashCode() + DistanceToOrigin.GetHashCode();
        }
        */

        /// <summary>
        /// Copies the specified copy members.
        /// </summary>
        /// <param name="copyMembers">The copy members.</param>
        /// <returns>TVGL.Plane.</returns>
        public Plane Copy(bool copyMembers = false)
        {
            var copy = new Plane();
            copy.Area = Area;
            copy.BoundsHaveBeenSet = BoundsHaveBeenSet;
            copy.DistanceToOrigin = DistanceToOrigin;
            copy.MaxX = MaxX;
            copy.MaxY = MaxY;
            copy.MaxZ = MaxZ;
            copy.MinX = MinX;
            copy.MinY = MinY;
            copy.MinZ = MinZ;
            copy.Normal = Normal;
            if (copyMembers)
            {
                copy.Vertices = new HashSet<Vertex>(Vertices);
                copy.Faces = new HashSet<PolygonalFace>(Faces);
                copy.InnerEdges = new HashSet<Edge>(InnerEdges);
                copy.OuterEdges = new HashSet<Edge>(OuterEdges);
            }
            return copy;
        }

        public override double CalculateError(IEnumerable<Vertex> vertices = null)
        {
            if (vertices == null) vertices = Vertices;
            var numVerts = 0;
            var sqDistanceSum = 0.0;
            foreach (var v in vertices)
            {
                var d = v.Dot(Normal) - DistanceToOrigin;
                sqDistanceSum += d * d;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }
    }
}
