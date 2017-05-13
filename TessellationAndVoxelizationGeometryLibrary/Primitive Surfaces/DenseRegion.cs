// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-18-2015
// ***********************************************************************
// <copyright file="DenseRegion.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    ///     Class DenseRegion.
    /// </summary>
    public class DenseRegion : PrimitiveSurface
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DenseRegion" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public DenseRegion(List<PolygonalFace> faces) : base(faces)
        {
            Type = PrimitiveSurfaceType.Dense;
        }

        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsNewMemberOf(PolygonalFace face)
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

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void UpdateWith(PolygonalFace face)
        {
            throw new NotImplementedException();
        }
    }
}