// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="GeneralConicSection.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Enum PrimitiveCurveType
    /// </summary>
    public enum PrimitiveCurveType
    {
        /// <summary>
        /// The straight line
        /// </summary>
        StraightLine,
        /// <summary>
        /// The circle
        /// </summary>
        Circle,
        /// <summary>
        /// The parabola
        /// </summary>
        Parabola,
        /// <summary>
        /// The ellipse
        /// </summary>
        Ellipse,
        /// <summary>
        /// The hyperbola
        /// </summary>
        Hyperbola
    }
    /// <summary>
    /// Public circle structure, given a center point and radius
    /// </summary>
    public struct GeneralConicSection : ICurve
    {
        /// <summary>
        /// the coefficient multiplying x^2
        /// </summary>
        public double A;
        /// <summary>
        /// the coefficient multiplying xy
        /// </summary>
        public double B;
        /// <summary>
        /// the coefficient multiplying y^2
        /// </summary>
        public double C;
        /// <summary>
        /// The d
        /// the coefficient multiplying x
        /// </summary>
        public double D;
        /// <summary>
        /// the coefficient multiplying y
        /// </summary>
        public double E;
        /// <summary>
        /// The constant is zero, otherwise the constant is 1
        /// </summary>
        public bool ConstantIsZero;
        /// <summary>
        /// The conic tolerance
        /// </summary>
        private const double conicTolerance = 1e-4;

        /// <summary>
        /// Gets the type of the curve.
        /// </summary>
        /// <value>The type of the curve.</value>
        public PrimitiveCurveType CurveType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralConicSection"/> struct.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="d">The d.</param>
        /// <param name="e">The e.</param>
        /// <param name="constantIsZero">if set to <c>true</c> [constant is zero].</param>
        public GeneralConicSection(double a, double b, double c, double d, double e, bool constantIsZero)
        {
            var max = (new[] { Math.Abs(a), Math.Abs(b), Math.Abs(c), Math.Abs(d), Math.Abs(e) }).Max();
            A = Math.Abs(a / max) < conicTolerance ? 0 : a;
            B = Math.Abs(b / max) < conicTolerance ? 0 : b;
            C = Math.Abs(c / max) < conicTolerance ? 0 : c;
            D = Math.Abs(d / max) < conicTolerance ? 0 : d;
            E = Math.Abs(e / max) < conicTolerance ? 0 : e;
            ConstantIsZero = constantIsZero;
            CurveType = PrimitiveCurveType.StraightLine;

            #region SetConicType
            if (A.IsNegligible() && B.IsNegligible() && C.IsNegligible())
            {
                A = B = C = 0;
                CurveType = PrimitiveCurveType.StraightLine;
            }
            else if (B.IsNegligible() && A.IsPracticallySame(C))
            {
                B = 0;
                A = C = 0.5 * (A + C);
                CurveType = PrimitiveCurveType.Circle;
            }
            else if ((B * B).IsPracticallySame(A * C))
            {
                B = Math.Sqrt(A * C);
                CurveType = PrimitiveCurveType.Parabola;
            }
            else
            {
                var det = A * C - B * B;
                if (det > 0) CurveType = PrimitiveCurveType.Ellipse;
                else CurveType = PrimitiveCurveType.Hyperbola;
            }
            #endregion
        }

        public GeneralConicSection(double a, double b, double c, double d, double e, double w) : this()
        {
            if (w.IsNegligible())
            {
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
                ConstantIsZero = true;
            }
            else
            {
                A = a / w;
                B = b / w;
                C = c / w;
                D = d / w;
                E = e / w;
                ConstantIsZero = false;
            }
        }

        /// <summary>
        /// Calculates at point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double CalculateAtPoint(Vector2 point)
        {
            double x = point.X;
            double y = point.Y;
            var constant = ConstantIsZero ? 0.0 : 1.0;
            return A * x * x + B * x * y + C * y * y + D * x + E * y + constant;
        }


        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IVector
        {
            return DistancePointToConic(this, point, out _);
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IVector2D
        {
            // this is maybe not sufficient. It assumes the error is the amount the function is off as opposed to the
            // distance. To properly do distance, see two methods at the bottom of this file.
            double xSqSum = 0.0, ySqSum = 0.0, xySum = 0.0, xSum = 0.0, ySum = 0.0;
            double xFourthSum = 0.0, xCubedYSum = 0.0, xSqYSqSum = 0.0, xYCubedSum = 0.0, yFourthSum = 0.0;
            double xCubedSum = 0.0, xSqYSum = 0.0, xYSqSum = 0.0, yCubedSum = 0.0;
            var numPoints = 0;
            foreach (var p in points)
            {
                var x = p.X;
                var y = p.Y;
                var xSq = x * x;
                var ySq = y * y;
                var xCubed = x * xSq;
                var yCubed = y * ySq;

                xSum += x;
                ySum += y;
                xSqSum += xSq;
                ySqSum += ySq;
                xySum += x * y;

                xCubedSum += xCubed;
                xSqYSum += xSq * y;
                xYSqSum += x * ySq;
                yCubedSum += yCubed;

                xFourthSum += xSq * xSq;
                xCubedYSum += xCubed * y;
                xSqYSqSum += xSq * ySq;
                xYCubedSum += x * yCubed;
                yFourthSum += ySq * ySq;
                numPoints++;
            }
            if (numPoints < 5)
            {
                curve = new GeneralConicSection();
                error = double.PositiveInfinity;
                return false;
            }
            var matrix = new double[,]
            {
                { xFourthSum, xCubedYSum, xSqYSqSum,  xCubedSum, xSqYSum },
                { xCubedYSum, xSqYSqSum,  xYCubedSum, xSqYSum,   xYSqSum },
                { xSqYSqSum,  xYCubedSum, yFourthSum, xYSqSum,   yCubedSum},
                { xCubedSum,  xSqYSum,    xYSqSum,    xSqSum,    xySum },
                { xSqYSum,    xYSqSum,    yCubedSum,  xySum,     ySqSum }
            };
            var b = new[] { xSqSum, xySum, ySqSum, xSum, ySum };
            if (matrix.solve(b, out var result, true))
            {
                curve = new GeneralConicSection(result[0], result[1], result[2], result[3], result[4], false);
                error = 0.0;
                foreach (var p in points)
                    error += curve.SquaredErrorOfNewPoint(p);
                error /= numPoints;
                return true;
            }
            curve = new GeneralConicSection();
            error = double.PositiveInfinity;
            return false;
        }

        /// <summary>
        /// Defines the circle from terms.
        /// </summary>
        /// <param name="circle1">The circle1.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool DefineCircleFromTerms(out Circle circle1)
        {
            if (A.IsNegligible())
            {
                circle1 = new Circle();
                return false;
            }
            if (ConstantIsZero)
            {
                var x = -0.5 * D;
                var y = -0.5 * E;
                circle1 = new Circle(new Vector2(x, x), x * x + y * y);
                return true;
            }
            var rSqMinusXSqMinusYSq = 1 / A;
            var xCenter = -0.5 * rSqMinusXSqMinusYSq * D;
            var yCenter = -0.5 * rSqMinusXSqMinusYSq * E;
            var radiusSq = rSqMinusXSqMinusYSq + (xCenter * xCenter) + (yCenter * yCenter);
            circle1 = new Circle(new Vector2(xCenter, yCenter), Math.Abs(radiusSq));
            return true;
        }
        /// <summary>
        /// Find the shortest distance from a point to a conic.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conic">The conic.</param>
        /// <param name="point">The point.</param>
        /// <param name="pointOnCurve">The point on curve closed to the given point.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToConic<T>(GeneralConicSection conic, T point, out Vector2 pointOnCurve)
            where T : IVector
        {
            // this is hard to understand. Please refer to the document call SolvingConics.docx
            var aj = conic.B;
            var bj = 2 * conic.C - 2 * conic.A;
            var cj = -conic.B;
            var rj = conic.E - conic.B * point[0] + 2 * conic.A * point[1];
            var sj = -conic.D + conic.B * point[1] - 2 * conic.C * point[0];
            var tj = conic.D * point[1] - conic.E * point[0];
            var conicJ = new GeneralConicSection(aj, bj, cj, rj, sj, tj);
            var minDistance = double.PositiveInfinity;
            pointOnCurve = Vector2.Null;
            foreach (var p in IntersectingConics(conic, conicJ))
            {
                var distance = (new Vector2(p.X - point[0], p.Y - point[1])).LengthSquared();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    pointOnCurve = p;
                }
            }
            return minDistance;
        }
        public static GeneralConicSection CreateFromQuadric(GeneralQuadric quadric, Plane plane, out Matrix4x4 transfromFromXYPlaneBackToGivenPlane)
        {
            transfromFromXYPlaneBackToGivenPlane = plane.AsTransformFromXYPlane;
            var mTranspose = transfromFromXYPlaneBackToGivenPlane.Transpose();
            var qMatrix = quadric.GetCoefficientMatrix();
            // In TVGL's row-vector convention, a 2D point p maps to 3D as x = p * M.
            // Substituting into the quadric x * Q * x^T = 0 gives p * (M * Q * M^T) * p^T = 0.
            var qNew = transfromFromXYPlaneBackToGivenPlane * qMatrix * mTranspose;
            var a = qNew.M11;
            var b = 2 * qNew.M12;
            var c = qNew.M22;
            var d = 2 * qNew.M14;
            var e = 2 * qNew.M24;
            var w = qNew.M44;

            return new GeneralConicSection(a, b, c, d, e, w);
        }


        /// <summary>
        /// Finds the points on the conic that has a gradient in the same direction as
        /// the specified gradient vector. This input does not have to be normalized.
        /// </summary>
        /// <param name="gradient"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool PointsAtGivenGradient(Vector2 gradient, out Vector2 point)
        {
            point = Vector2.Null;
            // The gradient of f(x,y) = Ax² + Bxy + Cy² + Dx + Ey + const is:
            //   ∇f = [2Ax + By + D, Bx + 2Cy + E]
            // For ∇f ∥ gradient, the 2D cross product ∇f × gradient = 0:
            //   (2Ax + By + D)*gy - (Bx + 2Cy + E)*gx = 0
            // This defines a line: alpha*x + beta*y + gamma = 0
            // Intersecting this line with the conic finds the desired points.
            // Unlike inverting the gradient matrix, this works for all conic types
            // including parabolas where the matrix is singular.
            var gx = gradient.X;
            var gy = gradient.Y;
            var alpha = 2 * A * gy - B * gx;
            var beta = B * gy - 2 * C * gx;
            var gamma = D * gy - E * gx;

            Vector2 anchor, dir;
            if (Math.Abs(alpha) >= Math.Abs(beta))
            {
                if (alpha.IsNegligible()) return false;
                anchor = new Vector2(-gamma / alpha, 0);
                dir = new Vector2(-beta / alpha, 1);
            }
            else
            {
                anchor = new Vector2(0, -gamma / beta);
                dir = new Vector2(1, -alpha / beta);
            }

            foreach (var tuple in LineIntersection(anchor, dir))
            {
                var intersection = tuple.intersection;
                if (GetGradient(intersection).Dot(gradient) > 0)
                {
                    point = intersection;
                    return true;
                }
            }
            return false;
        }

        private Vector2 GetGradient(Vector2 pt)
        {
            return new Vector2(2 * A * pt.X + B * pt.Y + D, 2 * C * pt.Y + B * pt.X + E);
        }


        /// <summary>
        /// Returns the intersection points between this quadric and the given line.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public IEnumerable<(Vector2 intersection, double lineT)> LineIntersection(Vector2 anchor, Vector2 direction)
        {
            //put equation for line p = anchor + t * direction into the conic equation,
            //we get a quadratic equation in terms of t. Solve it and we get the intersection points.
            // so, here we just find the at^2 + bt + c = 0, where a, b, c are calculated as below
            // x = anchor.X + t * direction.X
            // y = anchor.Y + t * direction.Y
            // A * x^2 + B * x * y + C * y^2 + D * x + E * y + constant = 0
            // collect t^2, t, and constant terms, we get
            var constant = ConstantIsZero ? 0.0 : 1.0;
            var a = A * direction.X * direction.X + B * direction.X * direction.Y + C * direction.Y * direction.Y;
            var b = 2 * A * anchor.X * direction.X + B * (anchor.X * direction.Y + anchor.Y * direction.X) + 2 * C * anchor.Y * direction.Y
                    + D * direction.X + E * direction.Y;
            var c = A * anchor.X * anchor.X + B * anchor.X * anchor.Y + C * anchor.Y * anchor.Y + D * anchor.X + E * anchor.Y + constant;
            (var root1, var root2) = PolynomialSolve.Quadratic(a, b, c);

            if (!root1.IsRealNumber)
                yield break;
            if (root1.Real.IsPracticallySame(root2.Real))
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
        /// Finds the zero to four points of two intersectings the conics.
        /// </summary>
        /// <param name="conicH">The conic, H</param>
        /// <param name="conicJ">The conic, J.</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public static IEnumerable<Vector2> IntersectingConics(GeneralConicSection conicH, GeneralConicSection conicJ)
        {
            // this is hard to understand. Please refer to the document call SolvingConics.docx
            var a = conicH.A;
            var b = conicH.B;
            var c = conicH.C;
            var r = conicH.D;
            var s = conicH.E;
            var t = conicH.ConstantIsZero ? 0.0 : 1.0;
            var aj = conicJ.A;
            var bj = conicJ.B;
            var cj = conicJ.C;
            var rj = conicJ.D;
            var sj = conicJ.E;
            var tj = conicJ.ConstantIsZero ? 0.0 : 1.0;
            var ak = a * cj - aj * c;
            var bk = b * cj - bj * c;
            var rk = r * cj - rj * c;
            var sk = s * cj - sj * c;
            var tk = t * cj - tj * c;
            var xValues = PolynomialSolve.Quartic(a * bk * bk - ak * b * bk + ak * ak * c,
                  2 * a * bk * sk - b * bk * rk - ak * b * sk + 2 * ak * c * rk + bk * bk * r - ak * bk * s,
                  a * sk * sk - b * bk * tk - b * rk * sk + 2 * ak * c * tk + c * rk * rk + 2 * bk * r * sk - bk * rk * s - ak * s * sk + bk * bk * t,
                  2 * c * rk * tk - b * sk * tk + r * sk * sk - bk * tk * s - rk * s * sk + 2 * bk * sk * t,
                  c * tk * tk - s * sk * tk + sk * sk * t);
            foreach (var x in xValues)
            {
                if (!x.IsRealNumber) continue;
                (var y1, var y2) = PolynomialSolve.Quadratic(c, s + b * x.Real, t + r * x.Real + a * x.Real * x.Real);
                if (y1.IsRealNumber)
                {
                    var point = new Vector2(x.Real, y1.Real);
                    if (conicJ.CalculateAtPoint(point).IsNegligible(conicTolerance))
                        yield return point;
                }
                if (y2.IsRealNumber)
                {
                    var point = new Vector2(x.Real, y2.Real);
                    if (conicJ.CalculateAtPoint(point).IsNegligible(conicTolerance))
                        yield return point;
                }
            }
        }
    }
}
