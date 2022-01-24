using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL.Numerics
{
    public static class PolynomialSolve
    {
        static double twoPi = 2 * Math.PI;
        static double sqrt3by2 = Math.Sqrt(3.0) / 2.0;
        static double twoThirdsPi = twoPi / 3.0;
        static double scalfact = Math.Sqrt(Math.Sqrt(double.MaxValue)) / 1.618034;
        const double meps = 10e-15;
        public static IEnumerable<ComplexNumber> GetRoots(this IEnumerable<double> coefficients)
        {
            var coeffList = coefficients as IList<double> ?? coefficients.ToList();
            if (coeffList.Count > 5) throw new NotImplementedException("These are analytical solve methods which only go" +
                "up to quartic numbers");
            if (coeffList.Count == 5) return Quartic(coeffList);
            if (coeffList.Count == 4) return Cubic(coeffList);
            if (coeffList.Count == 3) return QuadraticAsEnumeration(coeffList);

            else throw new ArgumentException("Not enough coefficients provided. Please provide for all terms even if zero or one");
        }

        private static IEnumerable<ComplexNumber> QuadraticAsEnumeration(IList<double> coeffList)
        {
            var roots = QuadraticAsTuple(coeffList);
            yield return roots.Item1;
            yield return roots.Item2;
        }

        public static (ComplexNumber, ComplexNumber) QuadraticAsTuple(this IEnumerable<double> coeffList)
        {
            var enumerator = coeffList.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var squaredCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var linearCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var constant = enumerator.Current;
            return Quadratic(squaredCoeff, linearCoeff, constant);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ComplexNumber, ComplexNumber) Quadratic(double squaredCoeff, double linearCoeff, double constant)
        {
            var oneOverDenom = 1 / (2 * squaredCoeff);
            var radicalTerm = linearCoeff * linearCoeff - 4 * squaredCoeff * constant;  // more commonly known as b^2 - 4ac
            if (radicalTerm < 0)  // then imaginary roots
            {
                radicalTerm = Math.Sqrt(-radicalTerm);
                radicalTerm *= oneOverDenom;
                var negBTerm = -oneOverDenom * linearCoeff;
                return (new ComplexNumber(negBTerm, -radicalTerm), new ComplexNumber(negBTerm, radicalTerm));
            }
            else
            {
                radicalTerm = Math.Sqrt(radicalTerm);
                radicalTerm *= oneOverDenom;
                var negBTerm = oneOverDenom * linearCoeff;
                return (new ComplexNumber(radicalTerm - negBTerm), new ComplexNumber(-radicalTerm - negBTerm));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Cubic(this IEnumerable<double> coeffList)
        {
            var enumerator = coeffList.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var cubedCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var squaredCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var linearCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var constant = enumerator.Current;
            return Cubic(cubedCoeff, squaredCoeff, linearCoeff, constant);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Cubic(double cubedCoeff, double squaredCoeff, double linearCoeff, double offset, bool onlyReturnRealRoots = false)
        {
            /* solve the cubic cubedCoeff * x^3 + squaredCoeff * x^2 +  linearCoeff * x + offset = 0
             * following the equations from the Numerical Recipe book
             * http://phys.uri.edu/nigh/NumRec/bookfpdf/f5-6.pdf */
            var a = squaredCoeff / cubedCoeff;
            var b = linearCoeff / cubedCoeff;
            var c = offset / cubedCoeff;
            var Q = (a * a - 3.0 * b) / 9.0;
            var R = (2.0 * a * a * a - 9.0 * a * b + 27.0 * c) / 54.0;
            var Q3 = Q * Q * Q;
            var R2 = R * R;
            a /= 3.0;
            if (R2.IsPracticallySame(Q3) || R2 < Q3) // R^2 - Q^3 is the discriminant of the polynomial
            {  //for q-cubed to be greater than R-squared, then Q is guaranteed to be a positive real
                var theta = Math.Acos(R / Math.Sqrt(Q3)) / 3;
                var sqrtQ = Math.Sqrt(Q);
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta) - a);
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta + twoThirdsPi) - a);
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta - twoThirdsPi) - a);
            }
            else
            {
                var sqrtM = Math.Sqrt(R2 - Q3);
                var A = -Math.Sign(R) * Math.Cbrt(Math.Abs(R) + sqrtM);
                var B = A.IsNegligible() ? 0 : Q / A;
                yield return new ComplexNumber(A + B - a);
                if (onlyReturnRealRoots) yield break;
                yield return new ComplexNumber(-(A + B) / 2 - a, sqrt3by2 * (A - B));
                yield return new ComplexNumber(-(A + B) / 2 - a, -sqrt3by2 * (A - B));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Cubic(ComplexNumber cubedCoeff, ComplexNumber squaredCoeff,
            ComplexNumber linearCoeff, ComplexNumber offset)
        {
            /* solve the cubic cubedCoeff * x^3 + squaredCoeff * x^2 +  linearCoeff * x + offset = 0
             * following the equations from the Numerical Recipe book
             * http://phys.uri.edu/nigh/NumRec/bookfpdf/f5-6.pdf */
            var a = squaredCoeff / cubedCoeff;
            var b = linearCoeff / cubedCoeff;
            var c = offset / cubedCoeff;
            var Q = (a * a - 3.0 * b) / 9.0;
            var R = (2.0 * a * a * a - 9.0 * a * b + 27.0 * c) / 54.0;
            var Q3 = Q * Q * Q;
            var R2 = R * R;
            a /= 3.0;
            if (R.JustRealNumber && Q.JustRealNumber && (R2.IsPracticallySame(Q3) || R2.Real < Q3.Real)) // R^2 - Q^3 is the discriminant of the polynomial
            {  //for q-cubed to be greater than R-squared, then Q is guaranteed to be a positive real
                var theta = Math.Acos(R.Real / Math.Sqrt(Q3.Real)) / 3;
                var sqrtQ = Math.Sqrt(Q.Real);
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta)) - a;
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta + twoThirdsPi)) - a;
                yield return new ComplexNumber(-2 * sqrtQ * Math.Cos(theta - twoThirdsPi)) - a;
            }
            else
            {
                var sqrtM = ComplexNumber.Sqrt(R2 - Q3);
                var A = -ComplexNumber.Cbrt(R + sqrtM);
                var B = A.IsNegligible() ? new ComplexNumber(0) : Q / A;
                yield return A + B - a;
                var firstTerm = -((A + B) / 2) - a;
                var secondTerm = (new ComplexNumber(0, 1)) * (sqrt3by2 * (A - B));
                    yield return firstTerm + secondTerm;
                yield return firstTerm - secondTerm;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Quartic(this IEnumerable<double> coeffList)
        {
            var enumerator = coeffList.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var fourthOrderCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var cubedCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var squaredCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var linearCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quadratic.");
            var constant = enumerator.Current;
            return Quartic(fourthOrderCoeff, cubedCoeff, squaredCoeff, linearCoeff, constant);
        }
        public static IEnumerable<ComplexNumber> Quartic(ComplexNumber fourthOrderCoeff,
            ComplexNumber cubedCoeff, ComplexNumber squaredCoeff, ComplexNumber linearCoeff, ComplexNumber offset)
        {
            var b = cubedCoeff / fourthOrderCoeff;
            var c = squaredCoeff / fourthOrderCoeff;
            var d = linearCoeff / fourthOrderCoeff;
            var e = offset / fourthOrderCoeff;


            var Q1 = (c * c - 3 * b * d) + (12 * e);
            var Q2 = (2 * c * c * c) - (9 * b * c * d) + (27 * ((d * d) + (b * b * e))) - (72 * c * e);
            var Q3 = (8 * b * c) - (16 * d) - (2 * b * b * b);
            var Q4 = (3 * b * b) - (8 * c);

            var Q5 = ComplexNumber.Cbrt((Q2 / 2) + ComplexNumber.Sqrt(((Q2 * Q2) / 4) - (Q1 * Q1 * Q1)));
            var Q6 = ((Q1 / Q5) + Q5) / 3;
            var Q7 = 2 * ComplexNumber.Sqrt((Q4 / 12) + Q6);

            var minusBAddQ7 = -b + Q7;
            var minusBMinusQ7 = -b - Q7;
            var sqrtTerm1 = (2 * Q4) / 3 - (4 * Q6);
            var sqrtTerm2 = (Q3 / Q7);
            var addedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 + sqrtTerm2);
            var subtractedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 - sqrtTerm2);

            yield return (minusBMinusQ7 - subtractedSqrtTerm) / 4;
            yield return (minusBMinusQ7 + subtractedSqrtTerm) / 4;
            yield return (minusBAddQ7 - addedSqrtTerm) / 4;
            yield return (minusBAddQ7 + addedSqrtTerm) / 4;
        }
        public static IEnumerable<ComplexNumber> Quartic(double fourthOrderCoeff,
            double cubedCoeff, double squaredCoeff, double linearCoeff, double offset)
        {
            var b = cubedCoeff / fourthOrderCoeff;
            var c = squaredCoeff / fourthOrderCoeff;
            var d = linearCoeff / fourthOrderCoeff;
            var e = offset / fourthOrderCoeff;

            var Q1 = (c * c - 3 * b * d) + (12 * e);
            var Q2 = (2 * c * c * c) - (9 * b * c * d) + (27 * ((d * d) + (b * b * e))) - (72 * c * e);
            var Q3 = (8 * b * c) - (16 * d) - (2 * b * b * b);
            var Q4 = (3 * b * b) - (8 * c);

            var radicalTerm = ((Q2 * Q2) / 4) - (Q1 * Q1 * Q1);
            var Q5 = ComplexNumber.NaN;
            if (!radicalTerm.IsNegligible() && radicalTerm < 0)
                Q5 = ComplexNumber.Cbrt(new ComplexNumber(Q2 / 2, Math.Sqrt(-radicalTerm)));
            else Q5 = new ComplexNumber(Math.Cbrt((Q2 / 2) + Math.Sqrt(radicalTerm)));
            var Q6 = ((Q1 / Q5) + Q5) / 3;
            var Q7 = 2 * ComplexNumber.Sqrt(new ComplexNumber(Q4 / 12) + Q6);

            var complexB = new ComplexNumber(b);
            var minusBAddQ7 = -complexB + Q7;
            var minusBMinusQ7 = -complexB - Q7;
            var sqrtTerm1 = new ComplexNumber((2 * Q4) / 3 - (4 * Q6.Real), -4 * Q6.Imaginary);
            var sqrtTerm2 = (Q3 / Q7);
            var addedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 + sqrtTerm2);
            var subtractedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 - sqrtTerm2);

            yield return (minusBMinusQ7 - subtractedSqrtTerm) / 4;
            yield return (minusBMinusQ7 + subtractedSqrtTerm) / 4;
            yield return (minusBAddQ7 - addedSqrtTerm) / 4;
            yield return (minusBAddQ7 + addedSqrtTerm) / 4;
        }

    }
}
