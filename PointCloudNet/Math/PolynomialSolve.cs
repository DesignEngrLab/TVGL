// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolynomialSolve.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace PointCloud
{
    /// <summary>
    /// Class PolynomialSolve.
    /// </summary>
    public static class PolynomialSolve
    {
        /// <summary>
        /// The two pi
        /// </summary>
        static double twoPi = 2 * Math.PI;
        /// <summary>
        /// The sqrt3by2
        /// </summary>
        static double sqrt3by2 = Math.Sqrt(3.0) / 2.0;
        /// <summary>
        /// The two thirds pi
        /// </summary>
        static double twoThirdsPi = twoPi / 3.0;
        /// <summary>
        /// The scalfact
        /// </summary>
        static double scalfact = Math.Sqrt(Math.Sqrt(double.MaxValue)) / 1.618034;
        /// <summary>
        /// The meps
        /// </summary>
        const double meps = 10e-15;
        /// <summary>
        /// Gets the roots.
        /// </summary>
        /// <param name="coefficients">The coefficients.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        /// <exception cref="System.NotImplementedException">These are analytical solve methods which only go" +
        ///                 "up to quartic numbers</exception>
        /// <exception cref="System.ArgumentException">Not enough coefficients provided. Please provide for all terms even if zero or one</exception>
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

        /// <summary>
        /// Quadratics as enumeration.
        /// </summary>
        /// <param name="coeffList">The coeff list.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        private static IEnumerable<ComplexNumber> QuadraticAsEnumeration(IList<double> coeffList)
        {
            var roots = QuadraticAsTuple(coeffList);
            yield return roots.Item1;
            yield return roots.Item2;
        }

        /// <summary>
        /// Quadratics as enumeration.
        /// </summary>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="constant">The constant.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        private static IEnumerable<ComplexNumber> QuadraticAsEnumeration(double squaredCoeff, double linearCoeff, double constant)
        {
            var roots = Quadratic(squaredCoeff, linearCoeff, constant);
            yield return roots.Item1;
            yield return roots.Item2;
        }

        /// <summary>
        /// Quadratics as tuple.
        /// </summary>
        /// <param name="coeffList">The coeff list.</param>
        /// <returns>System.ValueTuple&lt;ComplexNumber, ComplexNumber&gt;.</returns>
        /// <exception cref="System.ArgumentException">Missing coefficients to solve quadratic.</exception>
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

        /// <summary>
        /// Quadratics the specified squared coeff.
        /// </summary>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="constant">The constant.</param>
        /// <returns>System.ValueTuple&lt;ComplexNumber, ComplexNumber&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ComplexNumber, ComplexNumber) Quadratic(double squaredCoeff, double linearCoeff, double constant)
        {
            if ((constant / squaredCoeff).IsNegligible())
            {
                return (new ComplexNumber(0), new ComplexNumber(-linearCoeff / squaredCoeff));
            }
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

        /// <summary>
        /// Quadratics the specified squared coeff.
        /// </summary>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="constant">The constant.</param>
        /// <returns>System.ValueTuple&lt;ComplexNumber, ComplexNumber&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ComplexNumber, ComplexNumber) Quadratic(ComplexNumber linearCoeff, ComplexNumber constant)
        {
            if (constant.IsNegligible())
            {
                return (new ComplexNumber(0), -linearCoeff);
            }
            var oneOverDenom = 0.5;
            var radicalTerm = linearCoeff * linearCoeff - 4 * constant;  // more commonly known as b^2 - 4ac
            radicalTerm = ComplexNumber.Sqrt(-radicalTerm);
            radicalTerm *= oneOverDenom;
            var negBTerm = -oneOverDenom * linearCoeff;
            return (negBTerm - radicalTerm, negBTerm + radicalTerm);
        }

        /// <summary>
        /// Cubics the specified coeff list.
        /// </summary>
        /// <param name="coeffList">The coeff list.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        /// <exception cref="System.ArgumentException">Missing coefficients to solve cubic.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Cubic(this IEnumerable<double> coeffList)
        {
            var enumerator = coeffList.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve cubic.");
            var cubedCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve cubic.");
            var squaredCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve cubic.");
            var linearCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve cubic.");
            var constant = enumerator.Current;
            return Cubic(cubedCoeff, squaredCoeff, linearCoeff, constant);
        }

        /// <summary>
        /// Cubics the specified cubed coeff.
        /// </summary>
        /// <param name="cubedCoeff">The cubed coeff.</param>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="onlyReturnRealRoots">if set to <c>true</c> [only return real roots].</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Cubic(double cubedCoeff, double squaredCoeff, double linearCoeff, double offset, bool onlyReturnRealRoots = false)
        {
            /* solve the cubic cubedCoeff * x^3 + squaredCoeff * x^2 +  linearCoeff * x + offset = 0
             * following the equations from the Numerical Recipe book
             * http://phys.uri.edu/nigh/NumRec/bookfpdf/f5-6.pdf */
            var a = squaredCoeff / cubedCoeff;
            var b = linearCoeff / cubedCoeff;
            var c = offset / cubedCoeff;
            if (c.IsNegligible())
            {
                yield return new ComplexNumber(0.0);
                foreach (var root in QuadraticAsEnumeration(1, a, b))
                    if (!onlyReturnRealRoots || root.IsRealNumber)
                        yield return root;
                yield break;
            }
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

        /// <summary>
        /// Cubics the specified cubed coeff.
        /// </summary>
        /// <param name="cubedCoeff">The cubed coeff.</param>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
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
            if (c.IsNegligible())
            {
                yield return new ComplexNumber(0.0);
                var rootsAsTuples = Quadratic(a, b);
                yield return rootsAsTuples.Item1;
                yield return rootsAsTuples.Item2;
                yield break;
            }
            var Q = (a * a - 3.0 * b) / 9.0;
            var R = (2.0 * a * a * a - 9.0 * a * b + 27.0 * c) / 54.0;
            var Q3 = Q * Q * Q;
            var R2 = R * R;
            a /= 3.0;
            if (R.IsRealNumber && Q.IsRealNumber && (R2.IsPracticallySame(Q3) || R2.Real < Q3.Real)) // R^2 - Q^3 is the discriminant of the polynomial
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


        /// <summary>
        /// Quartics the specified coeff list.
        /// </summary>
        /// <param name="coeffList">The coeff list.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        /// <exception cref="System.ArgumentException">Missing coefficients to solve quartic.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Quartic(this IEnumerable<double> coeffList)
        {
            var enumerator = coeffList.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quartic.");
            var fourthOrderCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quartic.");
            var cubedCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quartic.");
            var squaredCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quartic.");
            var linearCoeff = enumerator.Current;
            if (enumerator.MoveNext()) throw new ArgumentException("Missing coefficients to solve quartic.");
            var constant = enumerator.Current;
            return Quartic(fourthOrderCoeff, cubedCoeff, squaredCoeff, linearCoeff, constant);
        }

        /// <summary>
        /// Quartics the specified fourth order coeff.
        /// </summary>
        /// <param name="fourthOrderCoeff">The fourth order coeff.</param>
        /// <param name="cubedCoeff">The cubed coeff.</param>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Quartic(ComplexNumber fourthOrderCoeff,
            ComplexNumber cubedCoeff, ComplexNumber squaredCoeff, ComplexNumber linearCoeff, ComplexNumber offset)
        {
            var b = cubedCoeff / fourthOrderCoeff;
            var c = squaredCoeff / fourthOrderCoeff;
            var d = linearCoeff / fourthOrderCoeff;
            var e = offset / fourthOrderCoeff;

            if (e.IsNegligible())
            {
                yield return new ComplexNumber(0.0);
                foreach (var root in Cubic(new ComplexNumber(1), b, c, d))
                    yield return root;
                yield break;
            }

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
        /// <summary>
        /// Quartics the specified fourth order coeff.
        /// </summary>
        /// <param name="fourthOrderCoeff">The fourth order coeff.</param>
        /// <param name="cubedCoeff">The cubed coeff.</param>
        /// <param name="squaredCoeff">The squared coeff.</param>
        /// <param name="linearCoeff">The linear coeff.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>IEnumerable&lt;ComplexNumber&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ComplexNumber> Quartic(double fourthOrderCoeff,
            double cubedCoeff, double squaredCoeff, double linearCoeff, double offset)
        {
            var b = cubedCoeff / fourthOrderCoeff;
            var c = squaredCoeff / fourthOrderCoeff;
            var d = linearCoeff / fourthOrderCoeff;
            var e = offset / fourthOrderCoeff;

            if (e.IsNegligible())
            {
                yield return new ComplexNumber(0.0);
                foreach (var root in Cubic(1, b, c, d))
                    yield return root;
                yield break;
            }
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
            var sqrtTerm1 = new ComplexNumber(2 * Q4 / 3 - (4 * Q6.Real), -4 * Q6.Imaginary);
            var sqrtTerm2 = Q3 / Q7;
            var addedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 + sqrtTerm2);
            var subtractedSqrtTerm = ComplexNumber.Sqrt(sqrtTerm1 - sqrtTerm2);

            yield return (minusBMinusQ7 - subtractedSqrtTerm) / 4;
            yield return (minusBMinusQ7 + subtractedSqrtTerm) / 4;
            yield return (minusBAddQ7 - addedSqrtTerm) / 4;
            yield return (minusBAddQ7 + addedSqrtTerm) / 4;
        }

    }
}
