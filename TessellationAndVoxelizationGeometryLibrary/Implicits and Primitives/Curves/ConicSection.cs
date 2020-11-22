using System;
using System.Collections.Generic;
using System.Text;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL.Curves
{
    public enum ConicSectionType
    {
        StraightLine,
        Circle,
        Ellipse,
        Parabola,
        Hyperbola
    }

    /// <summary>
    ///  <para>Class ConicSection
    ///   A curve defined by the quadratic equation:
    ///   Ax^2 + Bxy + Cy^2 + Dx + Ey - 1 = 0 
    ///</summary>
    public class ConicSection
    {
        public ConicSectionType ConicType;
        public Plane Plane;
        public List<Vector2> Points;
        public List<(Edge, bool)> EdgesAndDirection;
        public double A;
        public double B;
        public double C;
        public double D;
        public double E;
        // F, the constant is at -1, or +1 when moved to the other side of the equation
        public bool ConstantIsZero;

        public Matrix4x4 Transform { get; private set; }

        internal static ConicSection DefineForLine(Vector3 coordinates, Vector3 lineDir)
        {
            var planeDir = (lineDir.X <= lineDir.Y && lineDir.X <= lineDir.Z) ? Vector3.UnitX :
                (lineDir.Y <= lineDir.X && lineDir.Y <= lineDir.Z) ? Vector3.UnitY : Vector3.UnitZ;
            var plane = new Plane(coordinates, planeDir);
            var anchor = coordinates.ConvertTo2DCoordinates(plane.AsTransformToXYPlane);
            var dir2D = lineDir.ConvertTo2DCoordinates(plane.AsTransformToXYPlane);
            var denom = anchor.X * dir2D.Y - anchor.Y * dir2D.X;
            if (denom.IsNegligible()) // then line goes through the origin, and we need to set the "F" to zero (ConstantIsZero)
                return new ConicSection
                {
                    ConicType = ConicSectionType.StraightLine,
                    A = 0,
                    B = 0,
                    C = 0,
                    D = dir2D.Y,
                    E = -dir2D.X,
                    ConstantIsZero = true,
                    Plane = plane
                };
            return new ConicSection
            {
                ConicType = ConicSectionType.StraightLine,
                A = 0,
                B = 0,
                C = 0,
                D = dir2D.Y / denom,
                E = -dir2D.X / denom,
                Plane = plane
            };
        }

        internal static ConicSection DefineForCircle(Plane plane, Circle2D circle)
        {
            var denom = circle.RadiusSquared - circle.Center.LengthSquared();
            if (denom.IsNegligible())
            {
                return new ConicSection
                {
                    ConicType = ConicSectionType.Circle,
                    A = 1,
                    B = 0,
                    C = 1,
                    D = -2 * circle.Center.X,
                    E = -2 * circle.Center.Y,
                    Plane = plane,
                    ConstantIsZero = true
                };
            }
            var oneOverDenom = 1 / denom;
            return new ConicSection
            {
                ConicType = ConicSectionType.Circle,
                A = oneOverDenom,
                B = 0,
                C = oneOverDenom,
                D = -2 * circle.Center.X * oneOverDenom,
                E = -2 * circle.Center.Y * oneOverDenom,
                Plane = plane
            };
        }

        internal void AddEnd(Edge edge, bool correctDirection)
        {
            EdgesAndDirection.Add((edge, correctDirection));
            if (correctDirection) Points.Add(edge.To.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            else Points.Add(edge.From.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            UpdateTerms();
        }

        internal void AddStart(Edge edge, bool correctDirection)
        {
            EdgesAndDirection.Insert(0, (edge, correctDirection));
            if (correctDirection) Points.Insert(0, edge.From.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            else Points.Insert(0, edge.To.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            UpdateTerms();
        }

        private ConicSection()
        {
            Points = new List<Vector2>();
            EdgesAndDirection = new List<(Edge, bool)>();
        }

        internal double CalcError(Vector3 point)
        {
            return CalcError(point.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
        }
        internal double CalcError(Vector2 point)
        {
            var x = point.X;
            var y = point.Y;
            if (ConstantIsZero)
                return Math.Abs(A * x * x + B * x * y + C * y * y + D * x + E * y);
            return Math.Abs(A * x * x + B * x * y + C * y * y + D * x + E * y - 1);
        }

        internal bool Upgrade(Vector3 newPoint, double tolerance)
        {
            return false;
            throw new NotImplementedException();
            // in the future - see if upgrading from straight to circle
            // then circle to parabola
            // or ellipse and hyperbola
            // make the new fit better.
        }

        // Least Squares fit of parameters: A - E
        // in  Z = Ax^2 + Bxy + Cy^2 + Dx + Ey - 1 = 0
        // to do this take the derivative of the sum of the Z's
        // 
        private bool UpdateTerms()
        {
            switch (ConicType)
            {
                case ConicSectionType.StraightLine: return UpdateStraightLine();
                case ConicSectionType.Circle: return UpdateCircle();
                //case ConicSectionType.Ellipse: return UpdateEllipse();
                //case ConicSectionType.Parabola: return UpdateParabola();
                //case ConicSectionType.Hyperbola: return UpdateHyperbola();
                default:
                    UpdateFull();
                    SetConicType();
                    return true;
            }
        }

        private void SetConicType()
        {
            if (A.IsNegligible() && B.IsNegligible() && C.IsNegligible())
            {
                A = B = C = 0;
                ConicType = ConicSectionType.StraightLine;
            }
            else if (B.IsNegligible() && A.IsPracticallySame(C))
            {
                B = 0;
                A = C = 0.5 * (A + C);
                ConicType = ConicSectionType.Circle;
            }
            else if ((B * B).IsPracticallySame(A * C))
            {
                B = Math.Sqrt(A * C);
                ConicType = ConicSectionType.Parabola;
            }
            else
            {
                var det = A * C;
                if (det > 0) ConicType = ConicSectionType.Ellipse;
                else ConicType = ConicSectionType.Hyperbola;
                //var det = ConstantIsZero ? 0.0 : -A * C;
                //det += B * E * D + D * B * E;
                //det -= D * C * D + A * E * E;
                //if (!ConstantIsZero) det -= B * B;
                //if (det.IsNegligible())
            }
        }

        /// <summary>Updates the straight line using least squares fit. As is shown in DefineForLine abvoe, the 
        /// only parameters that are non-zero are D and E.</summary>
        private bool UpdateStraightLine()
        {
            A = 0;
            B = 0;
            C = 0;
            // based on word file, we will solve the two simultaneous equations with substitution
            double g = 0.0, h = 0.0, k = 0.0, m = 0.0, n = 0.0;
            foreach (var p in Points)
            {
                g += p.X * p.X;
                h += p.X * p.Y;
                k += p.Y * p.Y;
                m += p.X;
                n += p.Y;
            }
            var denom = k * g - h * h;
            if (denom.IsNegligible())
            {
                ConstantIsZero = true;
                if (g.IsNegligible())
                {
                    E = 0;
                    D = 1;
                }
                else
                {
                    E = 1;
                    D = h / g;
                }
            }
            else
            {
                ConstantIsZero = false;
                E = (n * g - m * h) / denom;
                D = (m - E * h) / g;
            }
            return true;
        }
        private bool UpdateCircle()
        {
            B = 0;
            // based on word file, we will solve the two simultaneous equations with substitution
            double xSqSum = 0.0, ySqSum = 0.0, xySum = 0.0, xSum = 0.0, ySum = 0.0;
            double m11alt1 = 0.0, m21alt1 = 0.0, m31alt1 = 0.0;
            double m11alt2 = 0.0, m21alt2 = 0.0, m31alt2 = 0.0;
            double m11alt3 = 0.0, m21alt3 = 0.0, m31alt3 = 0.0;
            double m12 = 0.0, m22 = 0.0, m32 = 0.0;
            double m13 = 0.0, m23 = 0.0, m33 = 0.0;
            var whichAlternative = 1;
            foreach (var p in Points)
            {
                var x = p.X;
                var y = p.Y;
                xSum += x;
                ySum += y;
                xSqSum += x * x;
                ySqSum += y * y;
                xySum += x * y;
                m11alt1 += x * x * x * x + x * x * y * y;
                m21alt1 += x * x * x;
                m31alt1 += x * x * y;
                m11alt2 += x * x * x * y + x * y * y * y;
                m21alt2 += x * x * y;
                m31alt2 += x * y * y;
                m11alt3 += x * x * y * y + y * y * y * y;
                m21alt3 += x * x * y;
                m31alt3 += y * y * y;
                m12 += x * x * x + x * y * y;
                m22 += x * x;
                m32 += x * y;
                m13 += x * x * y + y * y * y;
                m23 += x * y;
                m33 += y * y;
            }
            var matrix = new Matrix3x3(m11alt1, m12, m13, m21alt1, m22, m23, m31alt1, m32, m33);
            if (!Matrix3x3.Invert(matrix, out var invMatrix))
            {
                whichAlternative = 2;
                matrix = new Matrix3x3(m11alt2, m12, m13, m21alt2, m22, m23, m31alt2, m32, m33);
                if (!Matrix3x3.Invert(matrix, out invMatrix))
                {
                    whichAlternative = 3;
                    matrix = new Matrix3x3(m11alt3, m12, m13, m21alt3, m22, m23, m31alt3, m32, m33);
                    if (!Matrix3x3.Invert(matrix, out invMatrix))
                    {
                        ConstantIsZero = true;
                        A = 1;
                        C = 1;
                        var g = m22;
                        var h = m23;
                        var k = m33;
                        var m = xSum;
                        var n = ySum;
                        var denom = k * g - h * h;
                        if (denom.IsNegligible())
                        {
                            ConstantIsZero = true;
                            if (g.IsNegligible())
                            {
                                E = 0;
                                D = 1;
                            }
                            else
                            {
                                E = 1;
                                D = h / g;
                            }
                        }
                        else
                        {
                            ConstantIsZero = false;
                            E = (n * g - m * h) / denom;
                            D = (m - E * h) / g;
                        }
                        return true;
                    }
                }
            }
            ConstantIsZero = false;
            var answerTerm1 = whichAlternative == 1 ? xSqSum : whichAlternative == 2 ? xySum : ySqSum;
            var rHS = new Vector3(answerTerm1, xSum, ySum);
            var result = rHS.Multiply(invMatrix);
            A = C = result[0];
            D = result[1];
            E = result[2];
            return true;
        }
        private bool UpdateFull()
        {
            // based on word file, we will solve the two simultaneous equations with substitution
            double xSqSum = 0.0, ySqSum = 0.0, xySum = 0.0, xSum = 0.0, ySum = 0.0;
            double xFourthSum = 0.0, xCubedYSum = 0.0, xSqYSqSum = 0.0, xYCubedSum = 0.0, yFourthSum = 0.0;
            double xCubedSum = 0.0, xSqYSum = 0.0, xYSqSum = 0.0, yCubedSum = 0.0;
            foreach (var p in Points)
            {
                var x = p.X;
                var y = p.Y;
                xSum += x;
                ySum += y;
                xSqSum += x * x;
                ySqSum += y * y;
                xySum += x * y;
                xFourthSum += x * x * x * x;
                xCubedYSum = x * x * y * y;
                xSqYSqSum += x * x * x * y;
                xYCubedSum += x * y * y * y;
                yFourthSum += y * y * y * y;
                xCubedSum += x * x * x;
                xSqYSum += x * x * y;
                xYSqSum += x * y * y;
                yCubedSum += y * y * y;
                xSqSum += x * x;
                xySum += x * y;
                ySqSum += y * y;
            }
            var matrix = new double[,]
            {
                { xFourthSum, xCubedYSum, xSqYSqSum, xCubedSum, xSqYSum },
                { xCubedYSum, xSqYSqSum, xYCubedSum, xSqYSum, xYSqSum },
                { xSqYSqSum, xYCubedSum, yFourthSum, xYSqSum, yCubedSum},
                { xCubedSum, xSqYSum, xYSqSum, xSqSum, xySum },
                { xSqYSum, xYSqSum, yCubedSum, xySum, ySqSum }
            };
            var b = new[] { xSqSum, xySum, ySqSum, xSum, ySum };
            var result = SolveAnalytically(matrix, b);
            A = result[0];
            B = result[1];
            C = result[2];
            D = result[3];
            E = result[4];
            return true;
        }


        public static double[] SolveAnalytically(double[,] A, IList<double> b)
        {
            var length = 5;
            var L = CholeskyDecomposition(A);
            var x = new double[length];
            // forward substitution
            for (int i = 0; i < length; i++)
            {
                var sumFromKnownTerms = 0.0;
                for (int j = 0; j < i; j++)
                    sumFromKnownTerms += L[i, j] * x[j];
                x[i] = (b[i] - sumFromKnownTerms);
            }

            for (int i = 0; i < length; i++)
                x[i] /= L[i, i];

            // backward substitution
            for (int i = length - 1; i >= 0; i--)
            {
                var sumFromKnownTerms = 0.0;
                for (int j = i + 1; j < length; j++)
                    sumFromKnownTerms += L[j, i] * x[j];
                x[i] -= sumFromKnownTerms;
            }
            return x;
        }
        /// <summary>
        /// Returns the Cholesky decomposition of A in a new matrix. The new matrix is a lower triangular matrix, and
        /// the diagonals are the D matrix in the L-D-LT formulation. To get the L-LT format.
        /// </summary>
        /// <param name="A">The matrix to invert. This matrix is unchanged by this function.</param>
        /// <param name="NoSeparateDiagonal">if set to <c>true</c> [no separate diagonal].</param>
        /// <returns>System.Double[].</returns>
        /// <exception cref="System.ArithmeticException">Matrix cannot be inverted. Can only invert square matrices.</exception>
        /// <exception cref="ArithmeticException">Matrix cannot be inverted. Can only invert square matrices.</exception>
        public static double[,] CholeskyDecomposition(double[,] A, bool NoSeparateDiagonal = false)
        {
            var length = 5;
            var L = (double[,])A.Clone();

            for (var i = 0; i < length; i++)
            {
                double sum;
                for (var j = 0; j < i; j++)
                {
                    sum = 0.0;
                    for (int k = 0; k < j; k++)
                        sum += L[i, k] * L[j, k] * L[k, k];
                    L[i, j] = (L[i, j] - sum) / L[j, j];
                }
                sum = 0.0;
                for (int k = 0; k < i; k++)
                    sum += L[i, k] * L[i, k] * L[k, k];
                L[i, i] -= sum;
                for (int j = i + 1; j < length; j++)
                    L[i, j] = 0.0;
            }
            return L;
        }
    }
}
