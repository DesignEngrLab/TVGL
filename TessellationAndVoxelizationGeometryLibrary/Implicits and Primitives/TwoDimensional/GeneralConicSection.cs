using System;
using System.Collections.Generic;
using StarMathLib;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public readonly struct GeneralConicSection : I2DCurve
    {
        public readonly double A;
        public readonly double B;
        public readonly double C;
        public readonly double D;
        public readonly double E;
        public readonly bool ConstantIsZero;

        public GeneralConicSection(double a, double b, double c, double d, double e, bool constantIsZero)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            ConstantIsZero = constantIsZero;
        }
        public double SquaredErrorOfNewPoint(Vector2 point)
        {
            var x = point.X;
            var y = point.Y;
            var error = A * x * x + B * x * y + C * y * y + D * x + E * y;
            if (!ConstantIsZero) error -= 1;
            return error * error / ((A * A + B * B + C * C) / 3);
        }

        public static bool CreateFromPoints(IEnumerable<Vector2> points, out GeneralConicSection conic, out double error)
        {
            // based on word file, we will solve the two simultaneous equations with substitution
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
                xySum  += x * y;
                
                xCubedSum += xCubed;
                xSqYSum   += xSq * y;
                xYSqSum   += x * ySq;
                yCubedSum += yCubed;

                xFourthSum += xSq * xSq;
                xCubedYSum += xCubed * y;
                xSqYSqSum  += xSq * ySq;
                xYCubedSum += x * yCubed;
                yFourthSum += ySq * ySq;
                numPoints++;
            }
            if (numPoints < 5)
            {
                conic = new GeneralConicSection();
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
                conic = new GeneralConicSection(result[0], result[1], result[2], result[3], result[4], true);
                error = 0.0;
                foreach (var p in points)
                    error += conic.SquaredErrorOfNewPoint(p);
                error /= numPoints;
                return true;
            }
            conic = new GeneralConicSection();
            error = double.PositiveInfinity;
            return false;
        }

        private void SetConicType()
        {
            //if (A.IsNegligible() && B.IsNegligible() && C.IsNegligible())
            //{
            //    A = B = C = 0;
            //    CurveType = PrimitiveCurveType.StraightLine;
            //}
            //else if (B.IsNegligible() && A.IsPracticallySame(C))
            //{
            //    B = 0;
            //    A = C = 0.5 * (A + C);
            //    CurveType = PrimitiveCurveType.Circle;
            //}
            //else if ((B * B).IsPracticallySame(A * C))
            //{
            //    B = Math.Sqrt(A * C);
            //    CurveType = PrimitiveCurveType.Parabola;
            //}
            //else
            //{
            //    var det = A * C - B * B;
            //    if (det > 0) CurveType = PrimitiveCurveType.Ellipse;
            //    else CurveType = PrimitiveCurveType.Hyperbola;
            //    //var det = ConstantIsZero ? 0.0 : -A * C;
            //    //det += B * E * D + D * B * E;
            //    //det -= D * C * D + A * E * E;
            //    //if (!ConstantIsZero) det -= B * B;
            //    //if (det.IsNegligible())
            //}
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
        public static double DistancePointToConic(double A, double B, double C, double D, double E, double F,
            Vector2 point, out Vector2 pointOnCurve)
        {


        }
    }
}
