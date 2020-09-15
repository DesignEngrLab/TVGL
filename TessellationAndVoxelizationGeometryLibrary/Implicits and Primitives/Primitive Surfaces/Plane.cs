// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using TVGL.Numerics;

namespace TVGL // COMMENTEDCHANGE namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating a 3D Plane
    /// </summary>
    public class Plane : PrimitiveSurface, IEquatable<Plane>
    {
        /// <summary>
        /// Tolerance used to determine whether faces should be part of this flat
        /// </summary>
        /// <value>The tolerance.</value>
        public double Tolerance { get; set; }

        /// <summary>
        /// The normal vector of the Plane.
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// The distance of the Plane along its normal from the origin.
        /// </summary>
        public double DistanceToOrigin;

        /// <summary>
        /// Gets the closest point on the plane to the origin.
        /// </summary>
        /// <value>The closest point to origin.</value>
        public Vector3 ClosestPointToOrigin => Normal * DistanceToOrigin;

        /// <summary>
        /// Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            if (Tolerance.IsPracticallySame(0.0)) Tolerance = Constants.ErrorForFaceInSurface;
            if (Faces.Contains(face)) return false;
            if (!face.Normal.Dot(Normal).IsPracticallySame(1.0, Tolerance)) return false;
            //Return true if all the vertices are within the tolerance 
            //Note that the Dot term and distance to origin, must have the same sign, 
            //so there is no additional need moth absolute value methods.
            return face.Vertices.All(v => Normal.Dot(v.Coordinates).IsPracticallySame(DistanceToOrigin, Tolerance));
        }

        /// <summary>
        /// Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            Normal = (Faces.Count * Normal) + face.Normal;
            Normal = Vector3.Normalize(Normal);
            var newVerts = new List<Vertex>();
            var newDistanceToPlane = 0.0;
            foreach (var v in face.Vertices.Where(v => !Vertices.Contains(v)))
            {
                newVerts.Add(v);
                newDistanceToPlane += v.Coordinates.Dot(Normal);
            }
            DistanceToOrigin = (Vertices.Count * DistanceToOrigin + newDistanceToPlane) / (Vertices.Count + newVerts.Count);
            base.UpdateWith(face);
        }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public Plane(IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Type = PrimitiveSurfaceType.Plane;

            //Set the normal by weighting each face's normal with its area
            //This makes small faces have less effect at shifting the normal
            var normalSumX = 0.0;
            var normalSumY = 0.0;
            var normalSumZ = 0.0;
            foreach (var face in faces)
            {
                var weightedNormal = face.Normal * face.Area;
                normalSumX += weightedNormal.X;
                normalSumY += weightedNormal.Y;
                normalSumZ += weightedNormal.Z;
            }
            Normal = new Vector3(normalSumX, normalSumY, normalSumZ).Normalize();

            DistanceToOrigin = Faces.Average(f => Normal.Dot(f.Vertices[0].Coordinates));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane" /> class.
        /// </summary>
        public Plane() : base()
        {
            Type = PrimitiveSurfaceType.Plane;
        }

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
        }

        public HashSet<Plane> GetAdjacentFlats(List<Plane> allFlats)
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
            if (true) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
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
            if (true) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
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
            if (true) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
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
        /// Returns a boolean indicating whether the two given Planes are equal.
        /// </summary>
        /// <param name="value1">The first Plane to compare.</param>
        /// <param name="value2">The second Plane to compare.</param>
        /// <returns>True if the Planes are equal; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Plane value1, Plane value2)
        {
            return (value1.Normal.X == value2.Normal.X &&
                    value1.Normal.Y == value2.Normal.Y &&
                    value1.Normal.Z == value2.Normal.Z &&
                    value1.DistanceToOrigin == value2.DistanceToOrigin);
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given Planes are not equal.
        /// </summary>
        /// <param name="value1">The first Plane to compare.</param>
        /// <param name="value2">The second Plane to compare.</param>
        /// <returns>True if the Planes are not equal; False if they are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Plane value1, Plane value2)
        {
            return (value1.Normal.X != value2.Normal.X ||
                    value1.Normal.Y != value2.Normal.Y ||
                    value1.Normal.Z != value2.Normal.Z ||
                    value1.DistanceToOrigin != value2.DistanceToOrigin);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Plane is equal to this Plane instance.
        /// </summary>
        /// <param name="other">The Plane to compare this instance to.</param>
        /// <returns>True if the other Plane is equal to this instance; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Plane other)
        {
            if (true) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
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
        /// Returns a boolean indicating whether the given Object is equal to this Plane instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this Plane; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is Plane)
            {
                return Equals((Plane)obj);
            }

            return false;
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
        public override int GetHashCode()
        {
            return Normal.GetHashCode() + DistanceToOrigin.GetHashCode();
        }

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
            copy.Type = Type;
            copy.Tolerance = Tolerance;
            if (copyMembers)
            {
                copy.Vertices = new HashSet<Vertex>(Vertices);
                copy.Faces = new HashSet<PolygonalFace>(Faces);
                copy.InnerEdges = new HashSet<Edge>(InnerEdges);
                copy.OuterEdges = new HashSet<Edge>(OuterEdges);
            }
            return copy;
        }
    }
}
