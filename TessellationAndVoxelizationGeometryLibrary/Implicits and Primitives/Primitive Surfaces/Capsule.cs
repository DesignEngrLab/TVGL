// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-10-2023
// ***********************************************************************
// <copyright file="Capsule.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    /// The class for Capsule primitives.
    /// </summary>
    public class Capsule : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// <exception cref="System.NotImplementedException"></exception>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// <exception cref="System.NotImplementedException"></exception>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            throw new NotImplementedException();

        }

        #region Properties

        /// <summary>
        /// Is the Capsule positive? (false is negative)
        /// </summary>
        /// <value><c>true</c> if this instance is positive; otherwise, <c>false</c>.</value>
        public bool IsPositive { get; set; }


        /// <summary>
        /// Gets or sets the anchor1.
        /// </summary>
        /// <value>The anchor1.</value>
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
        /// <summary>
        /// Gets or sets the anchor2.
        /// </summary>
        /// <value>The anchor2.</value>
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

        /// <summary>
        /// Gets or sets the radius1.
        /// </summary>
        /// <value>The radius1.</value>
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
        /// <summary>
        /// Gets or sets the radius2.
        /// </summary>
        /// <value>The radius2.</value>
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

        /// <summary>
        /// The anchor1
        /// </summary>
        Vector3 anchor1;
        /// <summary>
        /// The anchor2
        /// </summary>
        Vector3 anchor2;
        /// <summary>
        /// The radius1
        /// </summary>
        double radius1;
        /// <summary>
        /// The radius2
        /// </summary>
        double radius2;
        /// <summary>
        /// The direction vector
        /// </summary>
        Vector3 directionVector;
        /// <summary>
        /// The direction vector length
        /// </summary>
        double directionVectorLength;
        /// <summary>
        /// The plane1 dx
        /// </summary>
        double plane1Dx;
        /// <summary>
        /// The plane2 dx
        /// </summary>
        double plane2Dx;
        /// <summary>
        /// The cone anchor1
        /// </summary>
        Vector3 coneAnchor1;
        /// <summary>
        /// The cone anchor2
        /// </summary>
        Vector3 coneAnchor2;
        /// <summary>
        /// The cone length
        /// </summary>
        double coneLength;
        /// <summary>
        /// The cone radius1
        /// </summary>
        double coneRadius1;
        /// <summary>
        /// The cone radius2
        /// </summary>
        double coneRadius2;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Capsule"/> class.
        /// </summary>
        /// <param name="anchor1">The anchor1.</param>
        /// <param name="radius1">The radius1.</param>
        /// <param name="anchor2">The anchor2.</param>
        /// <param name="radius2">The radius2.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Capsule(Vector3 anchor1, double radius1, Vector3 anchor2, double radius2, bool isPositive)
        {
            this.anchor1 = anchor1;
            this.radius1 = radius1;
            this.anchor2 = anchor2;
            this.radius2 = radius2;
            IsPositive = isPositive;
            CalculatePrivateGeometryFields();

        }

        /// <summary>
        /// Calculates the private geometry fields.
        /// </summary>
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

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
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