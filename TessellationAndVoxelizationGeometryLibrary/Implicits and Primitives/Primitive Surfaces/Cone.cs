// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    ///     The class for Cone primitives.
    /// </summary>
    public class Cone : PrimitiveSurface
    {
        /// <summary>
        ///     Is the cone positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        internal Cone() { }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="facesAll">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive, IEnumerable<PolygonalFace> facesAll = null)
            : base(facesAll)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            IsPositive = IsPositive;
        }

        /// <summary>
        ///     Gets the aperture.
        /// </summary>
        /// <value>The aperture.</value>
        public double Aperture { get; set; }

        /// <summary>
        ///     Gets the apex.
        /// </summary>
        /// <value>The apex.</value>
        public Vector3 Apex { get; set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override double CalculateError(IEnumerable<IVertex3D> vertices = null)
        {
            if (vertices == null) vertices = Vertices;
            var numVerts = 0;
            var aper = Aperture;
            var sqDistanceSum = 0.0;
            foreach (var v in vertices)
            {
                var coords = new Vector3(v.X, v.Y, v.Z);
                var d = (coords - Apex).Cross(Axis).Length()
                    - Math.Abs(aper * (coords - Apex).Dot(Axis));
                sqDistanceSum += d * d;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }
    }
}