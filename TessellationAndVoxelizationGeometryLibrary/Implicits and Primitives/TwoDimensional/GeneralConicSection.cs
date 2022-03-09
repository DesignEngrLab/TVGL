using System;
using System.Collections.Generic;
using MIConvexHull;
using StarMathLib;
using TVGL.Numerics;

namespace TVGL.Primitives
{
    public enum PrimitiveCurveType
    {
        StraightLine,
        Circle,
        Parabola,
        Ellipse,
        Hyperbola
    }
    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public struct GeneralConicSection : ICurve
    {
        public double A;
        public double B;
        public double C;
        public double D;
        public double E;
        public bool ConstantIsZero;
        private const double OnPathTolerance = 1e-6;

        public PrimitiveCurveType CurveType { get; private set; }

        public GeneralConicSection(double a, double b, double c, double d, double e, bool constantIsZero)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            ConstantIsZero = constantIsZero;
            CurveType = PrimitiveCurveType.StraightLine;
            SetConicType();
        }

        public double CalculateAtPoint(Vector2 point)
        {
            double x = point.X;
            double y = point.Y;
            return A * x * x + B * x * y + C * y * y + D * x + E * y + 1;
        }


        public double SquaredErrorOfNewPoint<T>(T point) where T : IVertex2D
        {
            var x = point.X;
            var y = point.Y;
            // this can be very inaccurate - need to get the actual distance
            var error = A * x * x + B * x * y + C * y * y + D * x + E * y;
            if (!ConstantIsZero) error -= 1;
            return error * error / ((A * A + B * B + C * C) / 3);
        }

        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IVertex2D
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
                curve = new GeneralConicSection(result[0], result[1], result[2], result[3], result[4], true);
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

        private void SetConicType()
        {
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
                //var det = ConstantIsZero ? 0.0 : -A * C;
                //det += B * E * D + D * B * E;
                //det -= D * C * D + A * E * E;
                //if (!ConstantIsZero) det -= B * B;
                //if (det.IsNegligible())
            }
        }
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
        /// <param name="conic">The conic.</param>
        /// <param name="point">The point.</param>
        /// <param name="pointOnCurve">The point on curve closed to the given point.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToConic(GeneralConicSection conic, Vector2 point, out Vector2 pointOnCurve)
        {
            // this is hard to understand. Please refer to the document call SolvingConics.docx
            var aj = conic.B;
            var bj = 2 * conic.C - 2 * conic.A;
            var cj = -conic.B;
            var rj = conic.E - conic.B * point.X + 2 * conic.A * point.Y;
            var sj = -conic.D + conic.B * point.Y - 2 * conic.C * point.X;
            var tj = conic.D * point.Y - conic.E * point.X;
            GeneralConicSection conicJ;
            if (tj.IsNegligible())
                conicJ = new GeneralConicSection(aj, bj, cj, rj, sj, true);
            else conicJ = new GeneralConicSection(aj / tj, bj / tj, cj / tj, rj / tj, sj / tj, false);
            var minDistance = double.PositiveInfinity;
            pointOnCurve = Vector2.Null;
            foreach (var p in IntersectingConics(conic, conicJ))
            {
                var distance = (p - point).LengthSquared();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    pointOnCurve = p;
                }
            }
            return Math.Sqrt(minDistance);
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
                    if (conicJ.CalculateAtPoint(point).IsNegligible(OnPathTolerance))
                        yield return point;
                }
                if (y2.IsRealNumber)
                {
                    var point = new Vector2(x.Real, y2.Real);
                    if (conicJ.CalculateAtPoint(point).IsNegligible(OnPathTolerance))
                        yield return point;
                }
            }
        }
    }
}
