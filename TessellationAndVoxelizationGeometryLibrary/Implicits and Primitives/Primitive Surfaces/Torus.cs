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
        internal Torus() { }
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public Torus(Vector3 center, Vector3 axis, double majorRadius, double minorRadius, bool isPositive,
            IEnumerable<PolygonalFace> faces) : base(faces)
        {
            Center = center;
            Axis = axis;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Torus"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Torus(Torus originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null) 
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Center = originalToBeCopied.Center;
            Axis = originalToBeCopied.Axis;
            MajorRadius = originalToBeCopied.MajorRadius;
            MinorRadius = originalToBeCopied.MinorRadius;
        }

        /// <summary>
        ///     Is the torus positive? (false is negative)
        /// </summary>
        public bool IsPositive;


        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center { get; set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        ///     Gets the major radius.
        /// </summary>
        /// <value>The major radius.</value>
        public double MajorRadius { get; set; }

        /// <summary>
        ///     Gets the minor radius.
        /// </summary>
        /// <value>The minor radius.</value>
        public double MinorRadius { get; set; }


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
            var planeDist = Center.Dot(Axis);
            var sqDistanceSum = 0.0;
            foreach (var v in vertices)
            {
                var coords = new Vector3(v.X, v.Y, v.Z);
                Vector3 ptOnCircle = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, coords, planeDist);
                var d = (coords - ptOnCircle).Length() - MinorRadius;
                sqDistanceSum += d * d;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }
        public static Vector3 ClosestPointOnCenterRingToPoint(Vector3 axis, Vector3 center, double majorRadius, Vector3 vertexCoord, double planeDist = double.NaN)
        {
            if (double.IsNaN(planeDist)) planeDist = center.Dot(axis);
            var d = planeDist - vertexCoord.Dot(axis);
            var ptInPlane = vertexCoord + d * axis;
            var dirToCircle = (ptInPlane - center).Normalize();
            return center + majorRadius * dirToCircle;
        }

    }
}