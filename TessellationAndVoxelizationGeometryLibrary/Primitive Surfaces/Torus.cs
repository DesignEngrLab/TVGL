// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-18-2015
// ***********************************************************************
// <copyright file="Torus.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    ///     Class Torus.
    /// </summary>
    public class Torus : PrimitiveSurface
    {
        /// <summary>
        ///     Is the sphere positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public Torus(IEnumerable<PolygonalFace> faces) : base(faces)
        {
        }


        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center { get; internal set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public double[] Axis { get; internal set; }

        /// <summary>
        ///     Gets the major radius.
        /// </summary>
        /// <value>The major radius.</value>
        public double MajorRadius { get; internal set; }

        /// <summary>
        ///     Gets the minor radius.
        /// </summary>
        /// <value>The minor radius.</value>
        public double MinorRadius { get; internal set; }

        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>Boolean.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void UpdateWith(PolygonalFace face)
        {
            base.UpdateWith(face);
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Builds from multiple faces.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>PrimitiveSurface.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static PrimitiveSurface BuildFromMultipleFaces(List<PolygonalFace> faces)
        {
            throw new NotImplementedException();
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
    }
}