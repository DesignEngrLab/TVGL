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
using System.Text.RegularExpressions;



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
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var dxAlong = (point - coneAnchor1).Dot(directionVector);
            double d;
            if (dxAlong < 0) d = DistanceFromSphere1(point);
            else if (dxAlong > coneLength) d = DistanceFromSphere2(point);
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
            if (isPositive.HasValue && !isPositive.Value) d = -d;
            return d;
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var loc = point - Anchor1;
            var dxAlong = loc.Dot(directionVector.Normalize());
            Vector3 d;
            if (dxAlong < 0) d = loc.Normalize();
            else if (dxAlong > directionVectorLength) d = (point - Anchor2).Normalize();
            else
            {
                var b = directionVector.Cross(loc);
                var c = b.Cross(directionVector).Normalize();
                var cosAngle = directionVectorLength /
                    Math.Sqrt(directionVectorLength * directionVectorLength +
                    (coneRadius1 - coneRadius2) * (coneRadius1 - coneRadius2));
                var sinAngle = Math.Sqrt(1 - cosAngle * cosAngle);
                if (coneRadius1 < coneRadius2) sinAngle = -sinAngle;
                d = (c * cosAngle + directionVector * sinAngle).Normalize();
            }
            if (isPositive.HasValue && !isPositive.Value) d *= -1;
            return d;
        }

        public double DistanceFromSphere2(Vector3 point)
        {
            return (point - Anchor2).Length() - Radius2;
        }

        public double DistanceFromSphere1(Vector3 point)
        {
            return (point - Anchor1).Length() - Radius1;
        }
        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any()) return;
            var firstFace = Faces.First();
            isPositive = (firstFace.Center - Anchor1).Dot(firstFace.Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            MinX = Math.Min(Anchor1.X - Radius1, Anchor2.X - Radius2);
            MaxX = Math.Max(Anchor1.X + Radius1, Anchor2.X + Radius2);
            MinY = Math.Min(Anchor1.Y - Radius1, Anchor2.Y - Radius2);
            MaxY = Math.Max(Anchor1.Y + Radius1, Anchor2.Y + Radius2);
            MinZ = Math.Min(Anchor1.Z - Radius1, Anchor2.Z - Radius2);
            MaxZ = Math.Max(Anchor1.Z + Radius1, Anchor2.Z + Radius2);
        }

        /// <summary>
        /// Finds the intersection between this capsule and this specified line
        /// 
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            if (Radius1 != Radius2) throw new NotImplementedException("Capsule must have equal radii to use this method.");
            var a1ToA2Distance = (Anchor2 - Anchor1).Length();
            var cDir = (Anchor2 - Anchor1) / a1ToA2Distance;
            var pointsFound = 0;
            var sphereIntersects = Sphere.LineIntersection(Anchor1, Radius1, anchor, direction).ToList();
            if (sphereIntersects.Count > 0 && (sphereIntersects[0].intersection - Anchor1).Dot(cDir) <= 0)
            {
                yield return sphereIntersects[0];
                pointsFound++;
            }
            if (sphereIntersects.Count > 1 && (sphereIntersects[1].intersection - Anchor1).Dot(cDir) <= 0)
            {
                yield return sphereIntersects[1];
                pointsFound++;
            }

            if (pointsFound >= 2) yield break;
            sphereIntersects = Sphere.LineIntersection(Anchor2, Radius2, anchor, direction).ToList();
            if (sphereIntersects.Count > 0 && (sphereIntersects[0].intersection - Anchor2).Dot(cDir) >= 0)
            {
                yield return sphereIntersects[0];
                pointsFound++;
            }
            if (pointsFound >= 2) yield break;
            if (sphereIntersects.Count > 1 && (sphereIntersects[1].intersection - Anchor1).Dot(cDir) >= 0)
            {
                yield return sphereIntersects[1];
                pointsFound++;
            }
            if (pointsFound >= 2) yield break;
            var cylIntersects = Cylinder.LineIntersection(cDir, Radius1, Anchor1, anchor, direction).ToList();
            if (cylIntersects.Count > 0)  // && (cylIntersects[0].intersection - Anchor1).Dot(cDir) >= 0)
            {
                var dot = (cylIntersects[0].intersection - Anchor1).Dot(cDir);
                if (dot < a1ToA2Distance && dot >= 0)
                    yield return cylIntersects[0];
            }
            if (pointsFound >= 2) yield break;
            if (cylIntersects.Count > 1)
            {
                var dot = (cylIntersects[1].intersection - Anchor1).Dot(cDir);
                if (dot < a1ToA2Distance && dot >= 0)
                    yield return cylIntersects[1];
            }
        }

    }
}