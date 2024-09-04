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
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

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
        public GeneralQuadric(IEnumerable<double> coefficients)
        {
            var enumerator = coefficients.GetEnumerator();
            enumerator.MoveNext();
            this.XSqdCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.YSqdCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.ZSqdCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.XYCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.XZCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.YZCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.XCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.YCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.ZCoeff = enumerator.Current;
            enumerator.MoveNext();
            this.W = enumerator.Current;
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
        public override bool PointIsInside(Vector3 x)
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
            if (Faces == null || !Faces.Any()) return;
            var firstFace = Faces.First();
            isPositive = firstFace.Normal.Dot(firstFace.Center - Center) > 0;
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

        /// <summary>
        /// The is the mathematical center of the quadric. It is not the centroid of the faces.
        /// It is the location where the gradient of the quadric function is zero.
        /// It would be the center of an ellipsoid, but for other quadrics, it is ...?
        /// Desmos it up!
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (center.IsNull())
                {
                    // the center is defined where the gradient is zero for the quadric function
                    // this produces a system of 3 equations with 3 unknowns
                    var coeffs = new Matrix3x3(2 * XSqdCoeff, XYCoeff, XZCoeff,
                                               XYCoeff, 2 * YSqdCoeff, YZCoeff,
                                               XZCoeff, YZCoeff, 2 * ZSqdCoeff);
                    var b = new Vector3(-XCoeff, -YCoeff, -ZCoeff);
                    center = coeffs.Solve(b);
                }
                return center;
            }
        }
        Vector3 center = Vector3.Null;


        public Vector3 Axis1
        {
            get
            {
                if (axis1.IsNull())
                {
                    throw new NotImplementedException();
                }
                return axis1;
            }
        }
        public Vector3 Axis2
        {
            get
            {
                if (axis2.IsNull())
                {
                    throw new NotImplementedException();
                }
                return axis2;
            }
        }
        public Vector3 Axis3
        {
            get
            {
                if (axis3.IsNull())
                {
                    throw new NotImplementedException();
                }
                return axis3;
            }
        }
        Vector3 axis1 = Vector3.Null;
        Vector3 axis2 = Vector3.Null;
        Vector3 axis3 = Vector3.Null;

        public override string KeyString => "Quadric|" + XSqdCoeff.ToString("F5") + "|" +
            YSqdCoeff.ToString("F5") + "|" + ZSqdCoeff.ToString("F5") + "|" +
            XYCoeff.ToString("F5") + "|" + XZCoeff.ToString("F5") + "|" +
            YZCoeff.ToString("F5") + "|" + XCoeff.ToString("F5") + "|" +
            YCoeff.ToString("F5") + "|" + ZCoeff.ToString("F5") + "|" + W.ToString("F5")
            + GetCommonKeyDetails();

        /// <summary>
        /// Defines the quadric from points using minimum least squares.
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="errSqd"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeneralQuadric DefineFromPoints(IEnumerable<Vector3> samples, out double errSqd)
        {
            var numVerts = 0;
            double Sx = 0.0, Sy = 0.0, Sz = 0.0;
            double Sxx = 0.0;
            double Sxy = 0.0, Syy = 0.0;
            double Sxz = 0.0, Syz = 0.0, Szz = 0.0;
            double Sxxx = 0.0, Sxxy = 0.0, Sxyy = 0.0, Syyy = 0.0;
            double Sxxz = 0.0, Sxzz = 0.0, Szzz = 0.0;
            double Syyz = 0.0, Syzz = 0.0, Sxyz = 0.0;
            double Sx4 = 0.0, Sy4 = 0.0, Sz4 = 0.0;
            double Sx3y = 0.0, Sx3z = 0.0, Sxy3 = 0.0, Sy3z = 0.0, Sxz3 = 0.0, Syz3 = 0.0;
            double Sx2y2 = 0.0, Sx2z2 = 0.0, Sy2z2 = 0.0;
            double Sx2yz = 0.0, Sxy2z = 0.0, Sxyz2 = 0.0;
            foreach (var point in samples)
            {
                var x = point.X;
                var y = point.Y;
                var z = point.Z;
                Sx += x;
                Sy += y;
                Sz += z;
                var xx = x * x;
                var yy = y * y;
                var zz = z * z;
                Sxx += xx;
                Syy += yy;
                Szz += zz;
                var xy = x * y;
                var xz = x * z;
                var yz = y * z;
                Sxy += xy;
                Sxz += xz;
                Syz += yz;
                Sxxx += xx * x;
                Sxxy += xx * y;
                Sxyy += xy * y;
                Syyy += yy * y;
                Sxxz += xx * z;
                Sxzz += xz * z;
                Szzz += zz * z;
                Syyz += yy * z;
                Syzz += yz * z;
                Sxyz += xy * z;
                Sx4 += xx * xx;
                Sy4 += yy * yy;
                Sz4 += zz * zz;
                Sx3y += xx * xy;
                Sx3z += xx * xz;
                Sxy3 += xy * yy;
                Sy3z += yy * yz;
                Sxz3 += xz * zz;
                Syz3 += yz * zz;
                Sx2y2 += xx * yy;
                Sx2z2 += xx * zz;
                Sy2z2 += yy * zz;
                Sx2yz += xx * yz;
                Sxy2z += yy * xz;
                Sxyz2 += xy * zz;
                numVerts++;
            }
            if (numVerts >= 10)
            {
                double[,] Amatrix = {
                    { Sx4,   Sx2y2, Sx2z2, Sx3y,  Sx3z,  Sx2yz, Sxxx, Sxxy, Sxxz },
                    { Sx2y2, Sy4,   Sy2z2, Sxy3,  Sxy2z, Sy3z,  Sxyy, Syyy, Syyz },
                    { Sx2z2, Sy2z2, Sz4,   Sxyz2, Sxz3,  Syz3,  Sxzz, Syzz, Szzz },
                    { Sx3y,  Sxy3,  Sxyz2, Sx2y2, Sx2yz, Sxy2z, Sxxy, Sxyy, Sxyz },
                    { Sx3z,  Sxy2z, Sxz3,  Sx2yz, Sx2z2, Sxyz2, Sxxz, Sxyz, Sxzz },
                    { Sx2yz, Sy3z,  Syz3,  Sxy2z, Sxyz2, Sy2z2, Sxyz, Syyz, Syzz },
                    { Sxxx,  Sxyy,  Sxzz,  Sxxy,  Sxxz,  Sxyz,  Sxx,  Sxy,  Sxz },
                    { Sxxy,  Syyy,  Syzz,  Sxyy,  Sxyz,  Syyz,  Sxy,  Syy,  Syz },
                    { Sxxz,  Syyz,  Szzz,  Sxyz,  Sxzz,  Syzz,  Sxz,  Syz,  Szz }
                };
                // we set the constant term, W (or the 'J' term), to -1, this allows us to bring over
                // the unique part of the gradient to the solved side
                double[] b = [Sxx, Syy, Szz, Sxy, Sxz, Syz, Sx, Sy, Sz];
                if (Amatrix.solve(b, out var coefficients, true))
                {
                    var quadric = new GeneralQuadric(coefficients.Concat([-1]));
                    errSqd = quadric.XSqdCoeff * Sxx + quadric.YSqdCoeff * Syy + quadric.ZSqdCoeff * Szz
                        + quadric.XYCoeff * Sxy + quadric.XZCoeff * Sxz + quadric.YZCoeff * Syz
                        + quadric.XCoeff * Sx + quadric.YCoeff * Sy + quadric.ZCoeff * Sz + quadric.W;
                    errSqd *= errSqd / numVerts;
                    return quadric;
                }
            }
            errSqd = double.NaN;
            return null;
        }

        /// <summary>
        /// Defines the quadric as a cylinder. The error in terms should be close to zero for a perfect cylinder.
        /// </summary>
        /// <param name="errorInTerms"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Cylinder DefineAsCylinder()
        {
            var K = 2 / (XSqdCoeff + YSqdCoeff + ZSqdCoeff);
            var A = K * XSqdCoeff;
            var B = K * YSqdCoeff;
            var C = K * ZSqdCoeff;
            var D = K * XYCoeff;
            var E = K * XZCoeff;
            var F = K * YZCoeff;
            var G = K * XCoeff;
            var H = K * YCoeff;
            var I = K * ZCoeff;
            var J = K * W;

            Vector3 axis, anchor;
            double radius;

            if (A.IsNegligible(Constants.BaseTolerance) && B.IsPracticallySame(1, Constants.BaseTolerance)
                && C.IsPracticallySame(1, Constants.BaseTolerance))
            {
                axis = Vector3.UnitX;
                var ty = -0.5 * H;
                var tz = -0.5 * I;
                anchor = new Vector3(0, ty, tz);
                radius = Math.Sqrt(ty * ty + tz * tz - J);
            }
            else if (A.IsPracticallySame(1, Constants.BaseTolerance)
                && B.IsNegligible(Constants.BaseTolerance) && C.IsPracticallySame(1, Constants.BaseTolerance))
            {
                axis = Vector3.UnitY;
                var tx = -0.5 * G;
                var tz = -0.5 * I;
                anchor = new Vector3(tx, 0, tz);
                radius = Math.Sqrt(tx * tx + tz * tz - J);
            }
            else
            {
                if (A.IsPracticallySame(1, Constants.BaseTolerance)
                && B.IsPracticallySame(1, Constants.BaseTolerance) && C.IsNegligible(Constants.BaseTolerance))
                    axis = Vector3.UnitZ;
                else
                {
                    if (A + C - 1 < 0) throw new NotImplementedException("The quadric is not a cylinder.");
                    var y = Math.Sqrt(A + C - 1);
                    axis = new Vector3(-D / (2 * y), y, -F / (2 * y));
                }
                var g = Math.Sqrt(axis.X * axis.X + axis.Z * axis.Z);
                var ty = -H / (2 * g);
                var tx = (ty * axis.X * axis.Y - 0.5 * G * g) / axis.Z;
                var mTo = axis.TransformToXYPlane(out var mFrom);
                anchor = new Vector3(tx, ty, 0).Transform(mFrom);
                radius = Math.Sqrt(tx * tx + ty * ty - J);
            }
            return new Cylinder
            {
                Axis = axis,
                Anchor = anchor,
                Radius = radius,
            };

        }

        /// <summary>
        /// Defines the quadric as a cone. The error in terms should be close to zero for a perfect cone.
        /// </summary>
        /// <param name="errorInTerms"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Cone DefineAsCone()
        {
            Vector3 apex, axis;
            double mSqd;
            var thetaY = Math.Atan2(XYCoeff, YZCoeff);

            var thetaX1 = Math.Atan(Math.Sin(thetaY) * YZCoeff / XZCoeff);
            var thetaX2 = Math.Atan(Math.Cos(thetaY) * XYCoeff / XZCoeff);
            var thetaX = 0.5 * (thetaX1 + thetaX2);
            var rotMatrix = Matrix4x4.CreateRotationY(thetaY) * Matrix4x4.CreateRotationX(-thetaX);
            if (thetaX.IsNegligible(Constants.BaseTolerance) || thetaY.IsPracticallySame(Math.PI,
                Constants.BaseTolerance))
            {
                axis = Vector3.UnitY;
                throw new NotImplementedException();
                //var tx = -K * XCoeff;
                //var ty = -K * YCoeff;
                //var tz = K*ZCoeff/mSqd;
                //apex = apex.Transform(rotMatrix);

            }
            else
            {
                axis = Vector3.UnitZ.Transform(rotMatrix);
                mSqd = (Math.Cos(thetaX) * Math.Cos(thetaX) - YSqdCoeff) /
                    (Math.Sin(thetaX) * Math.Sin(thetaX));
                var K = (1 - 0.5 * mSqd) / (XSqdCoeff + YSqdCoeff + ZSqdCoeff);
                var cTX = Math.Cos(thetaX);
                var sTX = Math.Sin(thetaX);
                var cTY = Math.Cos(thetaY);
                var sTY = Math.Sin(thetaY);
                var aMatrix = new Matrix3x3(-cTY, sTY*sTX,mSqd*sTY*cTX, 
                    0,-cTX,mSqd*sTX,
                    sTY,cTY*sTX,mSqd*cTY*cTX);
                apex = aMatrix.Solve(new Vector3(K * XCoeff, K * YCoeff, K * ZCoeff));
                apex = apex.Transform(rotMatrix);
            }
            return new Cone
            {
                Axis = axis,
                Apex = apex,
                Aperture = Math.Sqrt(mSqd)
            };

        }

    }
}