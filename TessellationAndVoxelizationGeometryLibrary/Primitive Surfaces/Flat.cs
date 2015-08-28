// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-15-2015
// ***********************************************************************
// <copyright file="Flat.cs" company="">
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
    /// Class Flat.
    /// </summary>
    public class Flat : PrimitiveSurface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Flat"/> class.
        /// </summary>
        /// <param name="Faces">The faces.</param>
        public Flat(IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            var normalSum = new double[3];
            normalSum = Faces.Aggregate(normalSum, (current, face) => current.add(face.Normal));
            Normal = normalSum.divide(Faces.Count);
            //Round to eliminate small deviations 
            Normal[0] = Math.Round(Normal[0], 10);
            Normal[1] = Math.Round(Normal[1], 10);
            Normal[2] = Math.Round(Normal[2], 10);
            Normal = Normal.normalize();
            DistanceToOrigin = Math.Round(Faces.Average(f => Normal.dotProduct(f.Vertices[0].Position)), 10);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Flat"/> class.
        /// </summary>
        public Flat()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Flat"/> class.
        /// </summary>
        /// <param name="distanceToOrigin">The distance to origin.</param>
        /// <param name="normal">The normal.</param>
        public Flat(double distanceToOrigin, double[] normal)
        {
            Normal = normal.normalize();
            DistanceToOrigin = distanceToOrigin;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Flat"/> class.
        /// </summary>
        /// <param name="pointOnPlane">a point on plane.</param>
        /// <param name="normal">The normal.</param>
        public Flat(double[] pointOnPlane, double[] normal)
        {
            Normal = normal.normalize();
            DistanceToOrigin = Normal.dotProduct(pointOnPlane);
        }

        /// <summary>
        /// Gets or sets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public double DistanceToOrigin { get; set; }
        /// <summary>
        /// Gets or sets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public double[] Normal { get; set; }

        /// <summary>
        /// Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            var tolerance = 0.00001;
            if (Faces.Contains(face)) return false;
            if (Math.Abs(face.Normal.dotProduct(Normal) - 1.0) > tolerance) return false;
            foreach (var v in face.Vertices)
                if (Math.Abs(v.Position.dotProduct(Normal) - DistanceToOrigin) > Constants.ErrorForFaceInSurface * Math.Abs(DistanceToOrigin))
                    return false;
            return true;
        }

        /// <summary>
        /// Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            Normal = Normal.multiply(Faces.Count).add(face.Normal).divide(Faces.Count + 1);
            Normal.normalizeInPlace();
            var newVerts = new List<Vertex>();
            var newDistanceToPlane = 0.0;
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                {
                    newVerts.Add(v);
                    newDistanceToPlane += v.Position.dotProduct(Normal);
                }
            DistanceToOrigin = (Vertices.Count * DistanceToOrigin + newDistanceToPlane) / (Vertices.Count + newVerts.Count);
            base.UpdateWith(face);
        }
    }
}
