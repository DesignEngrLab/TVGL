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
        /// Gets or sets the anchor1, which is the center of sphere at the first end.
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
        /// Gets or sets the anchor2, which is the center of sphere at the second end.
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
        /// Gets or sets the radius1 - the radius of the first end
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
        /// Gets or sets the radius2 - the radius of the second end
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
        /// The direction vector is the unit vector that goes from Anchor1 to Anchor2
        /// </summary>
        Vector3 directionVector;
        /// <summary>
        /// The direction vector length is the distance from Anchor1 to Anchor2
        /// </summary>
        double directionVectorLength;
        /// <summary>
        /// The plane1 dx is the distance from origin for the plane that separates the cone 
        /// from the sphere
        /// </summary>
        double conePlaneDistance1;
        /// <summary>
        /// The plane2 dx is the distance from origin for the plane that separates the cone 
        /// from the sphere
        /// </summary>
        double conePlaneDistance2;
        /// <summary>
        /// The cone anchor1 is an adjustment to the Anchor1 to account for the difference in radii
        /// when creating the cone section. if the radii are the same, then this is the same as Anchor1
        /// when one end is smaller than the other, then plane at which we switch from cone to sphere is
        /// outside of the anchors
        /// </summary>
        Vector3 coneAnchor1;
        /// <summary>
        /// The cone anchor2 is an adjustment to the Anchor2 (see description for coneAnchor1)
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
            this.isPositive = isPositive;
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
            conePlaneDistance1 = directionVector.Dot(coneAnchor1);
            conePlaneDistance2 = directionVector.Dot(coneAnchor2);
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
            return DistanceToPoint(x) < 0;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var dxAlong = (point - coneAnchor1).Dot(directionVector);
            double d;
            if (dxAlong < 0) d = (point - Anchor1).Length() - Radius1;
            else if (dxAlong > coneLength) d = (point - Anchor2).Length() - Radius2;
            else // in the cone section
            {
                var t = (dxAlong - conePlaneDistance1) / coneLength;
                var thisRadius = (1 - t) * coneRadius1 + t * coneRadius2;
                var distAtCommonDepth = (point - coneAnchor1).Cross(directionVector).Length() - thisRadius;
                // to be more exact, we need the cosine of the aperture angle to get the closest distance
                // instead of finding angle, and then its cosine, we just pythagorean it 
                var cosAngle = directionVectorLength /
                    Math.Sqrt(directionVectorLength * directionVectorLength +
                    (coneRadius1 - coneRadius2) * (coneRadius1 - coneRadius2));
                d = distAtCommonDepth * cosAngle;
            }
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var dxAlong = (point - Anchor1).Dot(directionVector.Normalize());
            Vector3 d;
            if (dxAlong < conePlaneDistance1) d = (point - Anchor1).Normalize();
            else if (dxAlong > conePlaneDistance2) d = (point - Anchor2).Normalize();
            else
            {
                var a = (point - Anchor1);
                var b = directionVector.Cross(a);
                var c =directionVector.Cross(b).Normalize();
                var cosAngle = directionVectorLength /
                    Math.Sqrt(directionVectorLength * directionVectorLength +
                    (coneRadius1 - coneRadius2) * (coneRadius1 - coneRadius2));
                var sinAngle = Math.Sqrt(1 - cosAngle * cosAngle);
                if (coneRadius1 < coneRadius2) sinAngle = -sinAngle;
                d = c*cosAngle + directionVector*sinAngle;
            }
            if (isPositive.HasValue && !isPositive.Value) d *= -1;
            return d;
        }
        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any()) return;
            var firstFace = Faces.First();
            isPositive = (firstFace.Center - Anchor1).Dot(firstFace.Normal) > 0;
        }
    }
}