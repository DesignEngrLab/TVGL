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
using Newtonsoft.Json;
using StarMathLib;


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
        public Vector3 Normal { get; set; }
        /// <summary>
        /// The distance of the Plane along its normal from the origin.
        /// </summary>
        public double DistanceToOrigin { get; set; }

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
        Matrix4x4 _asTransformFromXYPlane = Matrix4x4.Null;
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
        Matrix4x4 _asTransformToXYPlane = Matrix4x4.Null;
        /// <summary>
        /// Gets the closest point on the plane to the origin.
        /// </summary>
        /// <value>The closest point to origin.</value>
        [JsonIgnore]
        public Vector3 ClosestPointToOrigin => Normal * DistanceToOrigin;


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public Plane(IEnumerable<PolygonalFace> faces, bool connectFacesToPrimitive = true)
            : base(faces, connectFacesToPrimitive)
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

        public Plane(IEnumerable<Vector3> vertices, Vector3 normalGuess)
        {
            DefineNormalAndDistanceFromVertices(vertices, out var dto, out var normal);
            if (normal.Dot(normalGuess) < 0)
            {
                normal *= -1;
                dto *= -1;
            }
            DistanceToOrigin = dto;
            Normal = normal;
        }


        public static bool DefineNormalAndDistanceFromVertices(IEnumerable<Vertex> vertices, out double distanceToPlane, out Vector3 normal)
        {
            return DefineNormalAndDistanceFromVertices(vertices.Select(v => v.Coordinates), out distanceToPlane, out normal);
        }

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
            foreach (var vertex in pointList)
            {
                if (vertex.IsNull()) continue;
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
            if (absoluteDiff.X.IsNegligible(Constants.ErrorForFaceInSurface)) normal = Vector3.UnitX;
            else if (absoluteDiff.Y.IsNegligible(Constants.ErrorForFaceInSurface)) normal = Vector3.UnitY;
            else if (absoluteDiff.Z.IsNegligible(Constants.ErrorForFaceInSurface)) normal = Vector3.UnitZ;
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
            var copy = new Plane(this);
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
        public Plane TransformToNewPlane(Quaternion rotation)
        {
            var copy = new Plane(this);
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
        /// Initializes a new instance of the <see cref="Plane"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Plane(Plane originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            DistanceToOrigin = originalToBeCopied.DistanceToOrigin;
            Normal = originalToBeCopied.Normal;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Plane(Plane originalToBeCopied, int[] newFaceIndices, TessellatedSolid copiedTessellatedSolid)
            : base(newFaceIndices, copiedTessellatedSolid)
        {
            DistanceToOrigin = originalToBeCopied.DistanceToOrigin;
            Normal = originalToBeCopied.Normal;
        }

        public static double CalculateError(IEnumerable<Vector3> vertices, out Vector3 normal, out double dto)
        {
            DefineNormalAndDistanceFromVertices(vertices, out dto, out normal);
            var maxError = 0.0;
            foreach (var c in vertices)
            {
                var d = Math.Abs(c.Dot(normal) - dto);
                if (d > maxError)
                    maxError = d;
            }
            return maxError;
        }

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
                var d = Math.Abs(c.Dot(Normal) - DistanceToOrigin);
                if (d > maxError)
                    maxError = d;
            }
            return maxError;
        }

        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = new Vector3(point.X, point.Y, point.Z);
            var result = v.Transform(AsTransformToXYPlane);
            return new Vector2(result.X, result.Y);
        }

        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var v = new Vector3(point.X, point.Y, 0);
            return v.Transform(AsTransformFromXYPlane);
        }
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            foreach (var point in points)
            {
                yield return TransformFrom3DTo2D(point);
            }
        }

        public override double PointMembership(Vector3 point)
        {
            var d = point.Dot(Normal);
            return d - DistanceToOrigin;
        }
    }
}
