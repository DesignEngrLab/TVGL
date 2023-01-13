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
    ///     Class Torus.
    /// </summary>
    public class Torus : PrimitiveSurface
    {
        public Torus() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Torus"/> class.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="minorRadius">The minor radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Torus(Vector3 center, Vector3 axis, double majorRadius, double minorRadius, bool isPositive,
            IEnumerable<PolygonalFace> faces) : base(faces)
        {
            Center = center;
            Axis = axis;
            IsPositive = isPositive;
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Torus"/> class.
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
            IsPositive = isPositive;
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
        /// Initializes a new instance of the <see cref="Torus"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Torus(Torus originalToBeCopied, int[] newFaceIndices, TessellatedSolid copiedTessellatedSolid)
            : base(newFaceIndices, copiedTessellatedSolid)
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
        ///     Gets the major radius, which is the distance from the center of the tube to the center of the torus
        /// </summary>
        /// <value>The major radius.</value>
        public double MajorRadius { get; set; }

        /// <summary>
        ///     Gets the minor radius, which the radius of the tube. 
        /// </summary>
        /// <value>The minor radius.</value>
        public double MinorRadius { get; set; }


        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
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

        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (MinorRadius is double.NaN)
                return double.MaxValue;
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var planeDist = Center.Dot(Axis);
            var maxError = 0.0;
            foreach (var c in vertices)
            {
                Vector3 ptOnCircle = ClosestPointOnCenterRingToPoint(Axis, Center, MajorRadius, c, planeDist);
                var d = Math.Abs((c - ptOnCircle).Length() - MinorRadius);
                if (d > maxError)
                    maxError = d;
            }
            return maxError;
        }

        public static Vector3 ClosestPointOnCenterRingToPoint(Vector3 axis, Vector3 center, double majorRadius, Vector3 vertexCoord, double planeDist = double.NaN)
        {
            if (double.IsNaN(planeDist)) planeDist = center.Dot(axis);
            var d = planeDist - vertexCoord.Dot(axis);
            var ptInPlane = vertexCoord + d * axis;
            var dirToCircle = (ptInPlane - center).Normalize();
            return center + majorRadius * dirToCircle;
        }

        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;
        private Vector3 faceZDir = Vector3.Null;
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
            var hoopAngle = Math.Atan2(-d,deltaRing);
            var polarAngle = Math.Atan2(vectorToPiP.Dot(faceYDir), vectorToPiP.Dot(faceXDir));

            return new Vector2(polarAngle * MajorRadius, hoopAngle * MinorRadius);
        }

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

        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            if (pathIsClosed && BorderEncirclesAxis(points, Axis, Center))
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

        public override double PointMembership(Vector3 point)
        {
            throw new NotImplementedException();
        }
    }
}