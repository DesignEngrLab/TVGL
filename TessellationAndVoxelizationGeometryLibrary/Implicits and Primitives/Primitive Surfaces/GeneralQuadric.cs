// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="GeneralQuadric.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// The class for GeneralQuadric primitive
    /// </summary>
    public class GeneralQuadric : PrimitiveSurface
    {
        /// <summary>
        /// Gets the coefficent multiplying the x^2 term. This is often list as "A".
        /// </summary>
        public double XSqdCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the y^2 term. This is often list as "B" (or "D").
        /// </summary>
        public double YSqdCoeff { get; }
        /// <summary>        
        /// Gets the coefficent multiplying the z^2 term. This is often list as "C" (or "F").
        /// </summary>
        public double ZSqdCoeff { get; }
        /// <summary>       
        /// Gets the coefficent multiplying the xy term. This is often list as "D" (or "2B").
        /// </summary>
        public double XYCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the xz term. This is often list as "E" (or "2C").
        /// </summary>
        public double XZCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the yz term. This is often list as "F" (or "2E").
        /// </summary>
        public double YZCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the x-term. This is often list as "G" (or "P").
        /// </summary>
        public double XCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the y-term. This is often list as "H" (or "Q").
        /// </summary>
        public double YCoeff { get; }
        /// <summary>
        /// Gets the coefficent multiplying the z-term. This is often list as "I" (or "R").
        /// </summary>
        public double ZCoeff { get; }
        /// <summary>
        /// W is the constant term. like weight in homogeneous coordinate systems.
        /// </summary>
        public double W { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralQuadric"/> class.
        /// </summary>
        public GeneralQuadric() { }
        /// <summary>
        /// GeneralQuadric
        /// </summary>
        public GeneralQuadric(double xSqdCoeff, double ySqdCoeff, double zSqdCoeff, double xyCoeff,
            double xzCoeff, double yzCoeff, double xCoeff, double yCoeff, double zCoeff, double w)
        {
            this.XSqdCoeff = xSqdCoeff;
            this.YSqdCoeff = ySqdCoeff;
            this.ZSqdCoeff = zSqdCoeff;
            this.XYCoeff = xyCoeff;
            this.XZCoeff = xzCoeff;
            this.YZCoeff = yzCoeff;
            this.XCoeff = xCoeff;
            this.YCoeff = yCoeff;
            this.ZCoeff = zCoeff;
            this.W = w;
        }
        public GeneralQuadric(double[] coefficients)
        {
            this.XSqdCoeff = coefficients[0];
            this.YSqdCoeff = coefficients[1];
            this.ZSqdCoeff = coefficients[2];
            this.XYCoeff = coefficients[3];
            this.XZCoeff = coefficients[4];
            this.YZCoeff = coefficients[5];
            this.XCoeff = coefficients[6];
            this.YCoeff = coefficients[7];
            this.ZCoeff = coefficients[8];
            this.W = coefficients[9];
        }
        /// <summary>
        /// GeneralQuadric
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public GeneralQuadric(double xSqdCoeff, double ySqdCoeff, double zSqdCoeff, double xyCoeff,
             double xzCoeff, double yzCoeff, double xCoeff, double yCoeff, double zCoeff, double w,
             IEnumerable<TriangleFace> faces)
            : base(faces)
        {
            this.XSqdCoeff = xSqdCoeff;
            this.YSqdCoeff = ySqdCoeff;
            this.ZSqdCoeff = zSqdCoeff;
            this.XYCoeff = xyCoeff;
            this.XZCoeff = xzCoeff;
            this.YZCoeff = yzCoeff;
            this.XCoeff = xCoeff;
            this.YCoeff = yCoeff;
            this.ZCoeff = zCoeff;
            this.W = w;
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
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
        /// Transforms the from 3d points on the GeneralQuadric to a 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            throw new NotImplementedException();
        }



        /// <summary>
        /// Returns where the given point is inside the cylinder.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool PointIsInside(Vector3 x)
        {
            return QuadricValue(x) < 0;
        }

        /// <summary>
        /// Gets the normal at point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>A Vector3.</returns>
        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var x = 2 * XSqdCoeff * point.X + XYCoeff * point.Y + XZCoeff * point.Z + XCoeff;
            var y = 2 * YSqdCoeff * point.Y + XYCoeff * point.X + YZCoeff * point.Z + YCoeff;
            var z = 2 * ZSqdCoeff * point.Z + XZCoeff * point.X + YZCoeff * point.Y + ZCoeff;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the value of the quadric function at the given point, but this is not the
        /// true distance to the surface.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double QuadricValue(Vector3 point)
        {
            return XSqdCoeff * point.X * point.X
                + YSqdCoeff * point.Y * point.Y
                + ZSqdCoeff * point.Z * point.Z
                + XYCoeff * point.X * point.Y
                + XZCoeff * point.X * point.Z
                + YZCoeff * point.Y * point.Z
                + XCoeff * point.X
                + YCoeff * point.Y
                + ZCoeff * point.Z
                + W;
        }


        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            // like the GeneralConic, the shortest perpendicular distance to the surface
            // requires solving a quartic equation
            throw new NotImplementedException();
        }

        protected override void CalculateIsPositive()
        {
            // for ellipsoids (including spheres), paraboloids, cones and cylinders, a
            // meaningful isPositive can be calculated. For hyperboloids, it is not clear.
            // if a cone, cylinder or sphere is desired than the general quadric should 
            // not be used. Instead, the specific class should be used.
            isPositive = null;
        }
    }
}