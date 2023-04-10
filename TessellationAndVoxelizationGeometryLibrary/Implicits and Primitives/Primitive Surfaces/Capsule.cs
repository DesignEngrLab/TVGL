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
            base.Transform(transformMatrix);
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


        public Vector3 Anchor1
        {
            get => anchor1;
            set
            {
                if (anchor1 != value)
                {
                    anchor1 = value;
                    CalculatePrivateGeometryFields();
                }
            }
        }
        public Vector3 Anchor2
        {
            get => anchor2;
            set
            {
                if (anchor2 != value)
                {
                    anchor2 = value;
                    CalculatePrivateGeometryFields();
                }
            }
        }

        public double Radius1
        {
            get => radius1;
            set
            {
                if (radius1 != value)
                {
                    radius1 = value;
                    CalculatePrivateGeometryFields();
                }
            }
        }
        public double Radius2
        {
            get => radius2;
            set
            {
                if (radius2 != value)
                {
                    radius2 = value;
                    CalculatePrivateGeometryFields();
                }
            }
        }

        Vector3 anchor1;
        Vector3 anchor2;
        double radius1;
        double radius2;
        Vector3 directionVector;
        double directionVectorLength;
        double plane1Dx;
        double plane2Dx;
        Vector3 coneAnchor1;
        Vector3 coneAnchor2;
        double coneLength;
        double coneRadius1;
        double coneRadius2;
        #endregion

        #region Constructors

        public Capsule(Vector3 anchor1, double radius1, Vector3 anchor2, double radius2, bool isPositive)
        {
            this.anchor1 = anchor1;
            this.radius1 = radius1;
            this.anchor2 = anchor2;
            this.radius2 = radius2;
            IsPositive = isPositive;
            CalculatePrivateGeometryFields();

        }

        private void CalculatePrivateGeometryFields()
        {
            directionVector = Anchor2 - Anchor1;
            directionVectorLength = directionVector.Length();
            directionVector /= directionVectorLength;
            var sinPhi = (Radius1 - Radius2) / directionVectorLength;
            //sinPhi = 0;
            var deltaR1 = Radius1 * sinPhi;
            coneAnchor1 = Anchor1 + deltaR1 * directionVector;
            var deltaR2 = Radius2 * sinPhi;
            coneAnchor2 = Anchor2 + deltaR2 * directionVector;
            plane1Dx = directionVector.Dot(coneAnchor1);
            plane2Dx = directionVector.Dot(coneAnchor2);
            coneLength = directionVectorLength + (deltaR2 - deltaR1);
            var cosPhi = Math.Sqrt(1 - sinPhi * sinPhi);
            coneRadius1 = radius1 * cosPhi;
            coneRadius2 = radius2 * cosPhi;
        }

        #endregion
        /// <summary>
        /// Returns where the given point is inside the Capsule.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool PointIsInside(Vector3 x)
        {
            return PointMembership(x) < 0 == IsPositive;
        }

        public override double PointMembership(Vector3 point)
        {
            var dxAlong = point.Dot(directionVector);
            if (dxAlong < plane1Dx) return (point - Anchor1).Length() - Radius1;
            if (dxAlong > plane2Dx) return (point - Anchor2).Length() - Radius2;
            var t = (dxAlong - plane1Dx) / coneLength;
            var thisRadius = (1 - t) * coneRadius1 + t * coneRadius2;
            return (point - coneAnchor1).Cross(directionVector).Length() - thisRadius;
        }
    }
}