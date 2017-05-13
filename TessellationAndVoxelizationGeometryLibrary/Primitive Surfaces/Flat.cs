// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-15-2015
// ***********************************************************************
// <copyright file="Flat.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Class Flat.
    /// </summary>
    public class Flat : PrimitiveSurface
    {
        /// <summary>
        ///     Gets or sets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public double DistanceToOrigin { get; set; }

        /// <summary>
        ///     Gets or sets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public double[] Normal { get; set; }

        /// <summary>
        ///     Tolerance used to determine whether faces shoud be part of this flat
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            if (Tolerance.IsPracticallySame(0.0)) Tolerance = Constants.ErrorForFaceInSurface;
            if (Faces.Contains(face)) return false;
            if (!face.Normal.dotProduct(Normal).IsPracticallySame(1.0, Tolerance)) return false;
            //Return true if all the vertices are within the tolerance 
            //Note that the dotProduct term and distance to origin, must have the same sign, 
            //so there is no additional need moth absolute value methods.
            return face.Vertices.All(v => Normal.dotProduct(v.Position).IsPracticallySame(DistanceToOrigin, Tolerance));
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            Normal = Normal.multiply(Faces.Count).add(face.Normal).divide(Faces.Count + 1);
            Normal.normalizeInPlace();
            var newVerts = new List<Vertex>();
            var newDistanceToPlane = 0.0;
            foreach (var v in face.Vertices.Where(v => !Vertices.Contains(v)))
            {
                newVerts.Add(v);
                newDistanceToPlane += v.Position.dotProduct(Normal);
            }
            DistanceToOrigin = (Vertices.Count * DistanceToOrigin + newDistanceToPlane) / (Vertices.Count + newVerts.Count);
            base.UpdateWith(face);
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Flat" /> class.
        /// </summary>
        /// <param name="faces"></param>
        public Flat(IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Type = PrimitiveSurfaceType.Flat;

            //Set the normal by weighting each face's normal with its area
            //This makes small faces have less effect at shifting the normal
            var normalSum = new double[3];
            foreach(var face in faces)
            {
                var weightedNormal = face.Normal.multiply(face.Area);
                normalSum[0] += weightedNormal[0];
                normalSum[1] += weightedNormal[1];
                normalSum[2] += weightedNormal[2];
            }
            Normal = normalSum.normalize();

            DistanceToOrigin = Faces.Average(f => Normal.dotProduct(f.Vertices[0].Position));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Flat" /> class.
        /// </summary>
        public Flat()
        {
            Type = PrimitiveSurfaceType.Flat;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Flat" /> class.
        /// </summary>
        /// <param name="distanceToOrigin">The distance to origin.</param>
        /// <param name="normal">The normal.</param>
        public Flat(double distanceToOrigin, double[] normal)
        {
            Type = PrimitiveSurfaceType.Flat;
            Normal = normal.normalize();
            DistanceToOrigin = distanceToOrigin;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Flat" /> class.
        /// </summary>
        /// <param name="pointOnPlane">a point on plane.</param>
        /// <param name="normal">The normal.</param>
        public Flat(double[] pointOnPlane, double[] normal)
        {
            Type = PrimitiveSurfaceType.Flat;
            Normal = normal.normalize();
            DistanceToOrigin = Normal.dotProduct(pointOnPlane);
        }

        #endregion
    }
}