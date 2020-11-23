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
        public Vector3 Center { get;  set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get;  set; }

        /// <summary>
        ///     Gets the major radius.
        /// </summary>
        /// <value>The major radius.</value>
        public double MajorRadius { get;  set; }

        /// <summary>
        ///     Gets the minor radius.
        /// </summary>
        /// <value>The minor radius.</value>
        public double MinorRadius { get;  set; }

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
        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        internal Torus() { }
    }
}