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
        public Torus(Vector3 center, Vector3 axis, double majorRadius, double minorRadius, bool isPositive,
            IEnumerable<TriangleFace> faces) : base(faces)
        {
            Center = center;
            Axis = axis;
            this.isPositive = isPositive;
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

        public double FindLargestEncompassingAnglesInTube()
        {
            var globalMinAngle = double.PositiveInfinity;
            var globalMaxAngle = double.NegativeInfinity;
            foreach (var path in Borders)
            {
                FindWindingAroundTube(path.GetVectors(), out var minAngle, out var maxAngle);
                if (globalMinAngle > minAngle) globalMinAngle = minAngle;
                if (globalMaxAngle < maxAngle) globalMaxAngle = maxAngle;
            }
            return globalMaxAngle - globalMinAngle;
        }

        private double FindWindingAroundTube(IEnumerable<Vector3> points, out double minAngle, out double maxAngle)
        {
            minAngle = double.PositiveInfinity;
            maxAngle = double.NegativeInfinity;
            foreach (var point in points)
            {
                var ringPoint = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, point);
                var xAxis = (ringPoint - Center).Normalize();
                var localCoord = point - ringPoint;
                var xCoord = xAxis.Dot(localCoord);
                var yCoord = Axis.Dot(localCoord);
                var angle = Math.Atan2(yCoord, xCoord);
                if (minAngle > angle) minAngle = angle;
                if (maxAngle < angle) maxAngle = angle;
            }
            return maxAngle - minAngle;
        }
    }
}