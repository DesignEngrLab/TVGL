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
    public class UnknownRegion : PrimitiveSurface
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UnknownRegion" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public UnknownRegion(List<PolygonalFace> faces) : base(faces) { }
        public UnknownRegion() { }

        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            return true;
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void UpdateWith(PolygonalFace face)
        {
            base.UpdateWith(face);
        }
    }
}