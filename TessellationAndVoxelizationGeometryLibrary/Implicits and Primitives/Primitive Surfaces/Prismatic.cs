// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Prismatic.cs" company="Design Engineering Lab">
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
    /// The class for Prismatic primitives.
    /// </summary>
    public class Prismatic : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = BoundingRadius * Axis.Cross(rVector1);
            rVector1 *= BoundingRadius;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            BoundingRadius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            // we currently don't allow the Prismatic to be squished into an elliptical Prismatic
            // so the radius is the average of the two radius component vectors after the 
            // transform. Earlier, we were doing 
            //Radius*=transformMatrix.M11;
            // but this is not correct since M11 is often non-unity during rotation
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
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            var transform = Axis.TransformToXYPlane(out _);
            foreach (var point in points)
                yield return point.ConvertTo2DCoordinates(transform);
            yield break;
        }

        #region Properties
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        /// Gets or sets the curve2 d.
        /// </summary>
        /// <value>The curve2 d.</value>
        public ICurve Curve2D { get; set; }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double BoundingRadius { get; set; }


        /// <summary>
        /// Gets or sets the maximum distance along axis.
        /// </summary>
        /// <value>The maximum distance along axis.</value>
        public double MaxDistanceAlongAxis { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// Gets or sets the minimum distance along axis.
        /// </summary>
        /// <value>The minimum distance along axis.</value>
        public double MinDistanceAlongAxis { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; set; } = double.PositiveInfinity;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic"/> class.
        /// </summary>
        public Prismatic() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="minDistanceAlongAxis">The minimum distance along axis.</param>
        /// <param name="maxDistanceAlongAxis">The maximum distance along axis.</param>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(Vector3 axis, double minDistanceAlongAxis,
            double maxDistanceAlongAxis, IEnumerable<TriangleFace> faces = null, bool isPositive = true)
            : base(faces)
        {
            Axis = axis;
            this.isPositive = isPositive;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(Vector3 axis, IEnumerable<TriangleFace> faces = null, bool isPositive = true) : base(faces)
        {
            Axis = axis;
            this.isPositive = isPositive;
            var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
            MinDistanceAlongAxis = min;
            MaxDistanceAlongAxis = max;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(IEnumerable<TriangleFace> faces = null, bool isPositive = true) : base(faces)
        {
            Axis =  MiscFunctions.FindMostOrthogonalVector(Faces.Select(face =>
                 (face.B.Coordinates - face.A.Coordinates).Cross(face.C.Coordinates - face.A.Coordinates)));
            this.isPositive = isPositive;
            var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, Axis, out _, out _);//vertices are set in base constructor
            MinDistanceAlongAxis = min;
            MaxDistanceAlongAxis = max;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        #endregion

        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateMeanSquareError(IEnumerable<Vector3> vertices = null)
        {
            if (Axis.X is double.NaN) return double.MaxValue;
            var mse = 0.0;
            foreach (var c in Faces)
            {
                var inPlane1 = c.Normal.Cross(Axis);
                var curveNormal = inPlane1.Cross(Axis).Normalize();
                var distA = c.A.Dot(curveNormal);
                var distB = c.B.Dot(curveNormal);
                var distC = c.C.Dot(curveNormal);
                var d = distA- distB;
                mse += d * d;
                d = distA - distC;
                mse += d * d;
                d = distB - distC;
                mse += d * d;
            }
            return mse / (3 * Faces.Count);
        }

        /// <summary>
        /// Returns where the given point is inside the Prismatic.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool PointIsInside(Vector3 x)
        {
            return PointMembership(x) < Constants.BaseTolerance;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double PointMembership(Vector3 point)
        {
            return double.NaN;
        }

        protected override void CalculateIsPositive()
        {
            // todo: do we want this to be true=convex and false=concave?
            // it's not exactly the same
            //throw new NotImplementedException();
        }
    }
}