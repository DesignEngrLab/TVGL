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
    ///     Class Sphere.
    /// </summary>
    public class Sphere : PrimitiveSurface
    {

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
                var d = (c - Center).Length() - Radius;
                sqDistanceSum += d * d;
            }
            return sqDistanceSum / coords.Count;
        }


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Sphere(Vector3 center, double radius, bool isPositive, IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Center = center;
            IsPositive = isPositive;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Sphere(Vector3 center, double radius, bool isPositive)
        {
            Center = center;
            IsPositive = isPositive;
            Radius = radius;
        }

        internal Sphere() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Sphere(Sphere originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Center = originalToBeCopied.Center;
            Radius = originalToBeCopied.Radius;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Is the sphere positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center { get; set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }

        #endregion
    }
}