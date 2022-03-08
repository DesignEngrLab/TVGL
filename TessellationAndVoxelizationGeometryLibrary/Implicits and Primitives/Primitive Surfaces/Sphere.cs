// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.Primitives
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
            Center = Center.Multiply(transformMatrix);
        }

        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var sqDistanceSum = 0.0;
            var numVerts = 0;
            foreach (var c in vertices)
            {
                var d = (c - Center).Length() - Radius;
                sqDistanceSum += d * d;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }

        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;
        private Vector3 faceZDir = Vector3.Null;
        public override Vector2 TransformFrom3DTo2D(IVertex3D point)
        {
            var v = new Vector3(point.X, point.Y, point.Z) - Center;
            if (faceXDir.IsNull())
            {
                faceXDir = Vector3.UnitX;
                faceYDir = Vector3.UnitY;
                faceZDir = Vector3.UnitZ;
            }
            var x = faceXDir.Dot(v)/Radius;
            var y = faceYDir.Dot(v)/Radius;
            var z = faceZDir.Dot(v)/Radius;
            var polarAngle = Math.Acos(z);
            var azimuthalX = Math.Acos(x);
            var azimuthalY = Math.Acos(y);
            var azimuthAngle = Math.Atan2(azimuthalY, azimuthalX);

            return new Vector2(azimuthAngle * Radius, polarAngle * Radius);
        }

        public override Vector3 TransformFrom2DTo3D(IVertex2D point)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<IVertex3D> points)
        {
            throw new NotImplementedException();
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