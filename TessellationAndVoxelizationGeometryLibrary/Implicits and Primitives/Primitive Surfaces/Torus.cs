// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Torus.cs" company="Design Engineering Lab">
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
    /// Class Torus.
    /// </summary>
    public class Torus : PrimitiveSurface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Torus"/> class.
        /// </summary>
        public Torus() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Torus" /> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="minorRadius">The minor radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Torus(Vector3 center, Vector3 axis, double majorRadius, double minorRadius,
            IEnumerable<TriangleFace> faces) : base(faces)
        {
            Center = center;
            Axis = axis;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Torus" /> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="minorRadius">The minor radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Torus(Vector3 center, Vector3 axis, double majorRadius, double minorRadius, bool isPositive)
        {
            Center = center;
            Axis = axis;
            this.isPositive = isPositive;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
        }

        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center
        {
            get => center;
            set
            {
                center = value;
                if (!axis.IsNull())
                    distanceFromOriginToBisectingPlane = center.Dot(axis);
            }
        }
        /// <summary>
        /// Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis
        {
            get => axis;
            set
            {
                axis = value;
                if (!center.IsNull())
                    distanceFromOriginToBisectingPlane = center.Dot(axis);
            }
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector3 center = Vector3.Null;
        /// <summary>
        /// The axis
        /// </summary>
        private Vector3 axis = Vector3.Null;
        /// <summary>
        /// The distance to bisecting plane
        /// </summary>
        private double distanceFromOriginToBisectingPlane;
        /// <summary>
        /// Gets the major radius, which is the distance from the center of the tube to the center of the torus
        /// </summary>
        /// <value>The major radius.</value>
        public double MajorRadius { get; set; }

        /// <summary>
        /// Gets the minor radius, which the radius of the tube.
        /// </summary>
        /// <value>The minor radius.</value>
        public double MinorRadius { get; set; }

        public override string KeyString => "Torus|" + Center.ToString() + "|" + Axis.ToString() + "|" +
            "|" + MajorRadius.ToString("F5") + "|" + MinorRadius.ToString("F5") + GetCommonKeyDetails();

        public Matrix4x4 TransformToXYPlane
        {
            get
            {
                if (transformToXYPlane.IsNull())
                    CreateEquationTransforms();
                return transformToXYPlane;
            }
        }
        Matrix4x4 transformToXYPlane = Matrix4x4.Null;
        public Matrix4x4 TransformFromYPlane
        {
            get
            {
                if (transformFromXYPlane.IsNull())
                    CreateEquationTransforms();
                return transformFromXYPlane;
            }
        }

        private void CreateEquationTransforms()
        {
            var translate = Matrix4x4.CreateTranslation(-Center);
            var rotate = Axis.TransformToXYPlane(out var backTransform);
            transformToXYPlane = translate * rotate;
            transformFromXYPlane = backTransform * -translate;
        }

        Matrix4x4 transformFromXYPlane = Matrix4x4.Null;
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
            Center = Center.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();

            var unitVector1 = Axis.GetPerpendicularDirection();
            var rVector1 = MajorRadius * unitVector1;
            var rVector2 = MajorRadius * Axis.Cross(unitVector1);
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            MajorRadius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);

            rVector1 = MinorRadius * unitVector1;
            rVector2 = MinorRadius * Axis;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            MinorRadius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
        }


        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 faceXDir = Vector3.Null;
        /// <summary>
        /// The face y dir
        /// </summary>
        private Vector3 faceYDir = Vector3.Null;
        /// <summary>
        /// The face z dir
        /// </summary>
        private Vector3 faceZDir = Vector3.Null;

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            if (faceXDir.IsNull())
            {
                faceZDir = Axis;
                faceYDir = Axis.GetPerpendicularDirection();
                faceXDir = faceYDir.Cross(faceZDir);
            }
            var planeDist = Center.Dot(Axis);
            var d = planeDist - point.Dot(Axis);
            var ptInPlane = point + d * Axis;  // project the point back to the plane cutting through the torus
            var vectorToPiP = ptInPlane - Center;
            var deltaRing = vectorToPiP.Length() - MajorRadius;
            var hoopAngle = Math.Atan2(-d, deltaRing);
            var polarAngle = Math.Atan2(vectorToPiP.Dot(faceYDir), vectorToPiP.Dot(faceXDir));

            return new Vector2(polarAngle * MajorRadius, hoopAngle * MinorRadius);
        }

        /// <summary>
        /// Transforms the from2 d to3 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            if (faceXDir.IsNull())
            {
                faceZDir = Axis;
                faceYDir = Axis.GetPerpendicularDirection();
                faceXDir = faceYDir.Cross(faceZDir);
            }
            // reverse final set of 3D to 2D to get angles
            var polarAngle = point.X / MajorRadius;
            var hoopAngle = point.Y / MinorRadius;
            // create the transformation to rotate about the torus axis by the polarAngle amount
            var rotToSlicePlane = Matrix4x4.CreateFromAxisAngle(faceZDir, polarAngle);
            // apply this transfrom to rotate the X-dir to the location of the point on the torus
            // think of this as a slice through the torus (like you're cutting a donut in half)
            // it is called dirToTubeCenter
            var dirToTubeCenter = faceXDir.TransformNoTranslate(rotToSlicePlane);
            // now lets start the return point. starting from the center. move along this vector
            // by the amount of the majorRadius to get to the center of the tube at the correct slice location
            var result = Center + dirToTubeCenter * MajorRadius;
            // next we create a new vector to rotate to a point on the tube.
            // this is done by further rotating the ditToTubeCenter by the angle hoopAngle 
            // the axis of rotate though is the initial Y-axis rotated by the same previous transform
            var rotYAxis = faceYDir.TransformNoTranslate(rotToSlicePlane);
            // this next rotYAxis is in the negative direction, so we put a negative in front of the angle
            var rotToHoopDir = Matrix4x4.CreateFromAxisAngle(rotYAxis, -hoopAngle);
            result += MinorRadius * dirToTubeCenter.TransformNoTranslate(rotToHoopDir);
            return result;
        }

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            if (pathIsClosed && points.BorderEncirclesAxis(Axis, Center))
            {
                var transform = Axis.TransformToXYPlane(out _);
                foreach (var point in points)
                    yield return point.ConvertTo2DCoordinates(transform);
                yield break;
            }
            foreach (var p in points)
            {
                yield return TransformFrom3DTo2D(p);
            }
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            Vector3 ptOnCircle = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, point, distanceFromOriginToBisectingPlane);
            var d = (point - ptOnCircle).Length() - MinorRadius;
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            Vector3 ptOnCircle = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, point, distanceFromOriginToBisectingPlane);
            var d = (point - ptOnCircle).Normalize();
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        /// <summary>
        /// Closests the point on center ring to point.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="center">The center.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="vertexCoord">The vertex coord.</param>
        /// <param name="planeDist">The plane dist.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 ClosestPointOnCenterRingToPoint(Vector3 axis, Vector3 center, double majorRadius, Vector3 vertexCoord,
            double planeDist = double.NaN)
        {
            if (double.IsNaN(planeDist)) planeDist = center.Dot(axis);
            var d = planeDist - vertexCoord.Dot(axis);
            var ptInPlane = vertexCoord + d * axis;
            var dirToCircle = (ptInPlane - center).Normalize();
            return center + majorRadius * dirToCircle;
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any()) return;
            var firstFace = Faces.First();
            var anchor = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, firstFace.Center);
            isPositive = (firstFace.Center - anchor).Dot(firstFace.Normal) > 0;
        }

        public double FindLargestEncompassingAnglesInTubeCrossSection()
        {
            var borderEnumerator = Borders.GetEnumerator();
            if (!borderEnumerator.MoveNext()) return Math.Tau; // if there are no borders then I guess you have a 
                                                               //complete torus
            var border = borderEnumerator.Current;
            FindWindingAroundTubeCrossSection(border.GetCoordinates(), true, out var globalMinAngle, out var globalMaxAngle);
            while (borderEnumerator.MoveNext())
            {
                border = borderEnumerator.Current;
                FindWindingAroundTubeCrossSection(border.GetCoordinates(), true, out var minAngle, out var maxAngle);
                if (globalMaxAngle < minAngle)
                {
                    minAngle -= Math.Tau;
                    maxAngle -= Math.Tau;
                }
                if (globalMinAngle > maxAngle)
                {
                    minAngle += Math.Tau;
                    maxAngle += Math.Tau;
                }
                var disconnectedRegions = globalMaxAngle < minAngle || globalMinAngle > maxAngle;
                // an interesting thing about tori is that they can have borders that wrap
                // around the hoop but go nowhere in terms of the the tube's cross section
                // so this is manifests as two (and only two?) regions of angle that do not overlap

                if (globalMinAngle > minAngle) globalMinAngle = minAngle;
                if (globalMaxAngle < maxAngle) globalMaxAngle = maxAngle;

                if (disconnectedRegions)
                {
                    if (globalMinAngle < -Math.PI)
                    {
                        globalMinAngle += Math.Tau;
                        globalMaxAngle += Math.Tau;

                    }
                    else if (globalMinAngle > Math.PI)
                    {
                        globalMinAngle -= Math.Tau;
                        globalMaxAngle -= Math.Tau;

                    }
                    var c = Faces.First().Center;
                    FindWindingAroundTubeCrossSection([c], true, out var angle, out _);
                    if (angle < globalMinAngle || angle > globalMaxAngle)
                    {
                        (globalMinAngle, globalMaxAngle) = (globalMaxAngle, globalMinAngle);
                        if (globalMinAngle > Math.PI) globalMinAngle -= Math.Tau;
                        else globalMaxAngle += Math.Tau;
                    }
                    break;
                }
                if (Math.Abs(globalMaxAngle - globalMinAngle) > Math.Tau)
                {
                    globalMinAngle = -Math.PI;
                    globalMaxAngle = Math.PI;
                    break;
                }
            }
            return globalMaxAngle - globalMinAngle;
        }

        private double FindWindingAroundTubeCrossSection(IEnumerable<Vector3> points, bool closedPath, out double minAngle, out double maxAngle)
        {
            return
                PolygonOperations.GetWindingAngles(
                    points.Select(p =>
                    {
                        var ringPoint = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, p);
                        var xAxis = (ringPoint - Center).Normalize();
                        var localCoord = p - ringPoint;
                        return new Vector2(xAxis.Dot(localCoord), Axis.Dot(localCoord));
                    }), Vector2.Zero, closedPath, out minAngle, out maxAngle);
        }

        protected override void SetPrimitiveLimits()
        {
            var xFactor = Math.Sqrt(1 - Axis.X * Axis.X);
            var yFactor = Math.Sqrt(1 - Axis.Y * Axis.Y);
            var zFactor = Math.Sqrt(1 - Axis.Z * Axis.Z);
            MinX = Center.X - xFactor * MajorRadius - MinorRadius;
            MaxX = Center.X + xFactor * MajorRadius + MinorRadius;
            MinY = Center.Y - yFactor * MajorRadius - MinorRadius;
            MaxY = Center.Y + yFactor * MajorRadius + MinorRadius;
            MinZ = Center.Z - zFactor * MajorRadius - MinorRadius;
            MaxZ = Center.Z + zFactor * MajorRadius + MinorRadius;
        }

        public IEnumerable<BorderSegment> MinorRadiusSegments()
        {
            var minorRadiusSegments = GetSegmentsWithRadius(MinorRadius, Constants.MediumHighConfidence);
            if (minorRadiusSegments.Count() >= 2) return minorRadiusSegments;

            //Else, loosen the tolerance          
            minorRadiusSegments = GetSegmentsWithRadius(MinorRadius, Constants.MediumConfidence);
            if (minorRadiusSegments.Count() >= 2) return minorRadiusSegments;

            //Else, loosen even 
            minorRadiusSegments = GetSegmentsWithRadius(MinorRadius, Constants.LowConfidence);
            return minorRadiusSegments;
        }

        private IEnumerable<BorderSegment> GetSegmentsWithRadius(double target, double confidence)
        {
            var tolerance = target * (1 - confidence);
            foreach (var segment in BorderSegments)
                if (segment.IsCircular && ((Circle)segment.Curve).Radius.IsPracticallySame(MinorRadius, tolerance))
                    yield return segment;
        }

        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            var newAnchor = anchor.Transform(TransformToXYPlane);
            var newDirection = direction.TransformNoTranslate(TransformToXYPlane).Normalize();
            var RSqd = MajorRadius * MajorRadius;
            var k0 = newAnchor.Dot(newAnchor) + RSqd - MinorRadius * MinorRadius;
            var k1 = 2 * newDirection.Dot(newAnchor);
            var roots = PolynomialSolve.Quartic(1,
                2 * k1,
                2 * k0 + k1 * k1 - 4 * RSqd * (newDirection.X * newDirection.X + newDirection.Y * newDirection.Y),
                2 * k0 * k1 - 8 * RSqd * (newAnchor.X * newDirection.X + newAnchor.Y * newDirection.Y),
                k0 * k0 - 4 * RSqd * (newAnchor.X * newAnchor.X + newAnchor.Y * newAnchor.Y));
            foreach (var root in roots)
            {
                if (root.IsRealNumber)
                {
                    yield return (anchor + root.Real * direction, root.Real);
                }
            }
        }
    }
}