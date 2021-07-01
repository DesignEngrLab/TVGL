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
        internal Cone() { }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive, IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cone"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cone(Cone originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Aperture = originalToBeCopied.Aperture;
            Apex = originalToBeCopied.Apex;
            Axis = originalToBeCopied.Axis;
        }


        /// <summary>
        ///     Is the cone positive? (false is negative)
        /// </summary>
        public bool IsPositive;

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
            Axis = Axis.Transform(transformMatrix);
            Apex = Apex.Transform(transformMatrix);
        }

        public override double CalculateError(IEnumerable<IVertex3D> vertices = null)
        {
            List<Vector3> coords;
            if (vertices == null)
            {
                coords = Vertices.Select(v => v.Coordinates).ToList();
                coords.AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                coords.AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            else if (vertices is List<Vector3>)
                coords = (List<Vector3>)vertices;
            else coords = vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToList();

            var sqDistanceSum = 0.0;
            foreach (var c in coords)
            {
                var d = (c - Apex).Cross(Axis).Length()
                    - Math.Abs(Aperture * (c - Apex).Dot(Axis));
                sqDistanceSum += d * d;
            }
            return sqDistanceSum / coords.Count;
        }
    }
}