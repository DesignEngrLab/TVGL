// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="CappedCylinder.cs" company="Design Engineering Lab">
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
    /// The class for CappedCylinder primitives.
    /// </summary>
    public class CappedCylinder : Cylinder
    {

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var pointDot = point.Dot(Axis);
            var a = (point - Anchor);
            var b = Axis.Cross(a);
            Vector3 outwardVector;
            // if you're within half a percent of the min or max distance along the axis, 
            // and you're not closer to the radius than the end-caps, then you're on the end-cap
            if (Math.Abs(pointDot - MinDistanceAlongAxis) < Math.Abs(b.Length() - Radius))
                outwardVector = -Axis;
            else if (Math.Abs(pointDot - MaxDistanceAlongAxis) < Math.Abs(b.Length() - Radius))
                outwardVector = Axis;
            else outwardVector = b.Cross(Axis).Normalize();
            if (IsPositive.HasValue && !IsPositive.Value) outwardVector *= -1;
            return outwardVector;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var dxAlong = point.Dot(Axis);
            if (dxAlong < MinDistanceAlongAxis)
            {
                dxAlong = MinDistanceAlongAxis - dxAlong;
                var outward = (point - Anchor).Cross(Axis).Length() - Radius;
                return Math.Sqrt(dxAlong * dxAlong + outward * outward);
            }
            if (dxAlong > MaxDistanceAlongAxis)
            {
                dxAlong = MaxDistanceAlongAxis - dxAlong;
                var outward = (point - Anchor).Cross(Axis).Length() - Radius;
                return Math.Sqrt(dxAlong * dxAlong + outward * outward);
            }
            var d = (point - Anchor).Cross(Axis).Length() - Radius;
            // if d is positive, then the point is outside the CappedCylinder
            // if d is negative, then the point is inside the CappedCylinder
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        protected override void SetPrimitiveLimits()
        {
            var offset = Anchor.Dot(Axis);
            var top = Anchor + (MaxDistanceAlongAxis - offset) * Axis;
            var bottom = Anchor + (MinDistanceAlongAxis - offset) * Axis;
            var xFactor = Math.Sqrt(1 - Axis.X * Axis.X);
            if (double.IsNaN(xFactor)) xFactor = 0; // occasionally get NaN due to rounding errors when Axis.X is 1 or -1
            var yFactor = Math.Sqrt(1 - Axis.Y * Axis.Y);
            if (double.IsNaN(yFactor)) yFactor = 0; // occasionally get NaN due to rounding errors ""
            var zFactor = Math.Sqrt(1 - Axis.Z * Axis.Z);
            if (double.IsNaN(zFactor)) zFactor = 0; // occasionally get NaN due to rounding errors ::
            MinX = Math.Min(top.X, bottom.X) - xFactor * Radius;
            MaxX = Math.Max(top.X, bottom.X) + xFactor * Radius;
            MinY = Math.Min(top.Y, bottom.Y) - yFactor * Radius;
            MaxY = Math.Max(top.Y, bottom.Y) + yFactor * Radius;
            MinZ = Math.Min(top.Z, bottom.Z) - zFactor * Radius;
            MaxZ = Math.Max(top.Z, bottom.Z) + zFactor * Radius;
        }

        /// <summary>
        /// Finds the intersection between this CappedCylinder and the specified line.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchorLine, Vector3 direction)
        {
            var firstNeedMaxCapPoint = false;
            var firstNeedMinCapPoint = false;
            var cylIntersects = LineIntersection(Axis, Radius, Anchor, anchorLine, direction).ToList();
            if (cylIntersects.Count > 0)
            {
                var dot = cylIntersects[0].intersection.Dot(Axis);
                firstNeedMinCapPoint = double.IsFinite(MinDistanceAlongAxis) && MinDistanceAlongAxis > dot;
                firstNeedMaxCapPoint = double.IsFinite(MaxDistanceAlongAxis) && MaxDistanceAlongAxis < dot;
                if (!firstNeedMinCapPoint && !firstNeedMaxCapPoint)
                    yield return cylIntersects[0];
            }
            var secondNeedMaxCapPoint = false;
            var secondNeedMinCapPoint = false;
            if (cylIntersects.Count > 1)
            {
                var dot = cylIntersects[1].intersection.Dot(Axis);
                secondNeedMinCapPoint = double.IsFinite(MinDistanceAlongAxis) && MinDistanceAlongAxis > dot;
                secondNeedMaxCapPoint = double.IsFinite(MaxDistanceAlongAxis) && MaxDistanceAlongAxis < dot;
                if (!secondNeedMinCapPoint && !secondNeedMaxCapPoint)
                    yield return cylIntersects[1];
            }
            if ((firstNeedMinCapPoint && secondNeedMinCapPoint) || (firstNeedMaxCapPoint && secondNeedMaxCapPoint))
                // if both intersections are below the min plane or above the max plane then no intersection
                yield break;
            if (firstNeedMinCapPoint || secondNeedMinCapPoint)
            {
                var minPlaneIntersect = Plane.LineIntersection(Axis, MinDistanceAlongAxis, anchorLine, direction);
                if (!minPlaneIntersect.intersection.IsNull()) yield return minPlaneIntersect;
            }
            if (firstNeedMaxCapPoint || secondNeedMaxCapPoint)
            {
                var maxPlaneIntersect = Plane.LineIntersection(Axis, MaxDistanceAlongAxis, anchorLine, direction);
                if (!maxPlaneIntersect.intersection.IsNull()) yield return maxPlaneIntersect;
            }
        }


        public override string KeyString => "Capped"+base.KeyString;
    }
}