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
        /// Finds the intersection between a quadric and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="quadric">The quadric.</param>
        /// <param name="anchor">An anchor point on the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public bool LineIntersection(Vector3 anchor, Vector3 direction, out Vector3 point1,
            out Vector3 point2, out double t1, out double t2)
        {
            //solve for t in the quadratic equation
            var a = XSqdCoeff * direction.X * direction.X + YSqdCoeff * direction.Y * direction.Y + ZSqdCoeff * direction.Z * direction.Z
                + XYCoeff * direction.X * direction.Y + XZCoeff * direction.X * direction.Z + YZCoeff * direction.Y * direction.Z;
            var b = 2 * (XSqdCoeff * anchor.X * direction.X + YSqdCoeff * anchor.Y * direction.Y + ZSqdCoeff * anchor.Z * direction.Z)
                               + XYCoeff * (anchor.X * direction.Y + anchor.Y * direction.X)
                               + XZCoeff * (anchor.X * direction.Z + anchor.Z * direction.X)
                               + YZCoeff * (anchor.Y * direction.Z + anchor.Z * direction.Y)
                                              + XCoeff * direction.X + YCoeff * direction.Y + ZCoeff * direction.Z;
            var c = XSqdCoeff * anchor.X * anchor.X + YSqdCoeff * anchor.Y * anchor.Y + ZSqdCoeff * anchor.Z * anchor.Z
                + XYCoeff * anchor.X * anchor.Y + XZCoeff * anchor.X * anchor.Z + YZCoeff * anchor.Y * anchor.Z
                + XCoeff * anchor.X + YCoeff * anchor.Y + ZCoeff * anchor.Z + W;
            (var root1, var root2) = PolynomialSolve.Quadratic(a, b, c);

            if (root1.IsRealNumber && !root1.Real.IsPracticallySame(root2.Real))
            {
                t1 = root1.Real;
                t2 = root2.Real;
                point1 = anchor + t1 * direction;
                point2 = anchor + t2 * direction;
                return true;
            }
            else
            {
                t1 = root1.Real;
                t2 = double.NaN;
                point1 = anchor + t1 * direction;
                point2 = Vector3.Null;
                return root1.IsRealNumber;
            }
        }

        /// <summary>
        /// Finds the signed distance from the surface to the point
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            // start with the assumption that the normal is the best direction to go
            var dir = GetNormalAtPoint(point).Normalize();
            var dot = double.NaN;
            var minPointDist = double.NaN;
            var iterLeft = Constants.MaxIterationsNonlinearSolve;
            while (iterLeft-- > 0)
            {
                LineIntersection(point, dir, out var point1, out var point2, out _, out _);
                var dxToPoint1 = Vector3.DistanceSquared(point, point1);
                var dxToPoint2 = Vector3.DistanceSquared(point, point2);
                // get the distance to the closest point and store in minPoint and minPointDist
                var minPoint = point1;
                minPointDist = dxToPoint1;
                if (minPoint.IsNull() || dxToPoint2 < dxToPoint1)
                {
                    minPoint = point2;
                    minPointDist = dxToPoint2;
                }
                if (minPoint.IsNull()) break;
                // the nw direction is the vector from the point to the closest point
                var newDir = (point - minPoint).Normalize();
                // it should be the same as the normal, but if not, then we need to iterate
                dot = Vector3.Dot(dir, newDir);
                if (Math.Abs(dot) > Constants.DotToleranceForSame) break;
                dir = newDir;
            }
            return Math.Sign(dot) * Math.Sqrt(minPointDist);
        }

        protected override void CalculateIsPositive()
        {
            // for ellipsoids (including spheres), paraboloids, cones and cylinders, a
            // meaningful isPositive can be calculated. For hyperboloids, it is not clear.
            // if a cone, cylinder or sphere is desired than the general quadric should 
            // not be used. Instead, the specific class should be used.
            isPositive = null;
        }

        protected override void SetPrimitiveLimits()
        {
            //todo: if ellipsoid, then you will actually have values here
            MinX = MinY = MinZ = double.NegativeInfinity;
            MaxX = MaxY = MaxZ = double.PositiveInfinity;
        }

        /// <summary>
        /// Returns the intersection points between this quadric and the given line.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            //solve for t in the quadratic equation
            var a = XSqdCoeff * direction.X * direction.X + YSqdCoeff * direction.Y * direction.Y + ZSqdCoeff * direction.Z * direction.Z
                + XYCoeff * direction.X * direction.Y + XZCoeff * direction.X * direction.Z + YZCoeff * direction.Y * direction.Z;
            var b = 2 * (XSqdCoeff * anchor.X * direction.X + YSqdCoeff * anchor.Y * direction.Y + ZSqdCoeff * anchor.Z * direction.Z)
                               + XYCoeff * (anchor.X * direction.Y + anchor.Y * direction.X)
                               + XZCoeff * (anchor.X * direction.Z + anchor.Z * direction.X)
                               + YZCoeff * (anchor.Y * direction.Z + anchor.Z * direction.Y)
                                              + XCoeff * direction.X + YCoeff * direction.Y + ZCoeff * direction.Z;
            var c = XSqdCoeff * anchor.X * anchor.X + YSqdCoeff * anchor.Y * anchor.Y + ZSqdCoeff * anchor.Z * anchor.Z
                + XYCoeff * anchor.X * anchor.Y + XZCoeff * anchor.X * anchor.Z + YZCoeff * anchor.Y * anchor.Z
                + XCoeff * anchor.X + YCoeff * anchor.Y + ZCoeff * anchor.Z + W;
            (var root1, var root2) = PolynomialSolve.Quadratic(a, b, c);

            if (root1.IsRealNumber && root1.Real.IsPracticallySame(root2.Real))
            {
                var t = 0.5 * (root1.Real + root2.Real);
                yield return (anchor + t * direction, root1.Real);
                yield break;
            }
            if (root1.IsRealNumber)
                yield return (anchor + root1.Real * direction, root1.Real);
            if (root2.IsRealNumber)
                yield return (anchor + root2.Real * direction, root2.Real);
        }

    }
}