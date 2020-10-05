// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using TVGL.Numerics;

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
        internal DenseRegion()
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
        public override void Transform(Matrix4x4 transformMatrix)
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