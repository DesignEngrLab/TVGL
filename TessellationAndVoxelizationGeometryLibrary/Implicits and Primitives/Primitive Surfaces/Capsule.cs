// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    ///     The class for Capsule primitives.
    /// </summary>
    public class Capsule : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            throw new NotImplementedException();

        }

        #region Properties

        /// <summary>
        ///     Is the Capsule positive? (false is negative)
        /// </summary>
        public bool IsPositive { get; set; }


        public Vector3 Anchor1 { get; set; }
        public Vector3 Anchor2 { get; set; }

        public double Radius1 { get; set; }
        public double Radius2 { get; set; }

        Vector3 directionVector;
        double directionVectorLength;
        double plane1Dx;
        double plane2Dx;
        #endregion

        #region Constructors

        public Capsule(Vector3 anchor1, double radius1, Vector3 anchor2, double radius2, bool isPositive)
        {
            Anchor1 = anchor1;
            Radius1 = radius1;
            Anchor2 = anchor2;
            Radius2 = radius2;
            IsPositive = isPositive;
            directionVector = Anchor2 - Anchor1;
            directionVectorLength = directionVector.Length();
            directionVector /= directionVectorLength;
            plane1Dx = directionVector.Dot(Anchor1);
            plane2Dx = directionVector.Dot(Anchor2);
        }

        /// <summary>
        /// Returns where the given point is inside the Capsule.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool PointIsInside(Vector3 x)
        {
            return PointMembership(x) < 0;
        }

        public override double PointMembership(Vector3 point)
        {
            var dxAlong = point.Dot(directionVector);
            if (dxAlong < plane1Dx) return (point - Anchor1).Length() - Radius1;
            if (dxAlong > plane2Dx) return (point - Anchor2).Length() - Radius2;
            var t = (dxAlong - plane1Dx) / directionVectorLength;
            var thisRadius = (1 - t) * Radius1 + t * Radius2;
            return (point - Anchor1).Cross(directionVector).Length() - thisRadius;
        }


        #endregion
    }
}