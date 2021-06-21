using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.Numerics
{
    public static class PolynomialSolve
    {
        public static IEnumerable<ComplexNumber> GetRoots(this IEnumerable<double> coefficients)
        {
            var coeffList = coefficients as IList<double> ?? coefficients.ToList();
            if (coeffList.Count > 5) throw new NotImplementedException("These are analytical solve methods which only go" +
                "up to quartic numbers");
            if (coeffList.Count == 5) return Quartic(coeffList);
            if (coeffList.Count == 4) return Cubic(coeffList);
            if (coeffList.Count == 3) return Quadratic(coeffList);
            else throw new ArgumentException("Not enough coefficients provided. Please provide for all terms even if zero or one");
        }

        public static IEnumerable<ComplexNumber> Quadratic(this IEnumerable<double> coeffList)
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

        public static IEnumerable<ComplexNumber> Quadratic(double squaredCoeff, double linearCoeff, double constant)
        {
            var oneOverDenom = 1 / (2 * squaredCoeff);
            var radicalTerm = linearCoeff * linearCoeff - 4 * squaredCoeff * constant;  // more commonly known as b^2 - 4ac
            if (radicalTerm < 0)  // then imaginary roots
            {
                radicalTerm = Math.Sqrt(-radicalTerm);
                radicalTerm *= oneOverDenom;
                var negBTerm = -oneOverDenom * linearCoeff;
                yield return new ComplexNumber(negBTerm, -radicalTerm);
                yield return new ComplexNumber(negBTerm, radicalTerm);
            }
            else
            {
                radicalTerm = Math.Sqrt(radicalTerm);
                radicalTerm *= oneOverDenom;
                var negBTerm = oneOverDenom * linearCoeff;
                yield return new ComplexNumber(radicalTerm - negBTerm, 0);
                yield return new ComplexNumber(-radicalTerm - negBTerm, 0);
            }
        }

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

        public static IEnumerable<ComplexNumber> Cubic(double cubedCoeff, double squaredCoeff, double linearCoeff, double offset)
        {
            throw new NotImplementedException();
        }
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
        public static IEnumerable<ComplexNumber> Quartic(double fourthOrderCoeff, double cubedCoeff, double squaredCoeff, double linearCoeff, double offset)
        {
            var b = cubedCoeff / fourthOrderCoeff;
            var c = squaredCoeff / fourthOrderCoeff;
            var d = linearCoeff / fourthOrderCoeff;
            var e = offset / fourthOrderCoeff;

            var Q1 = c * c - 3 * b * d + 12 * e;
            var Q2 = 2 * c * c * c - 9 * b * c * d + 27 * d * d + 27 * b * b * e - 72 * c * e;
            var Q3 = new ComplexNumber(8 * b * c - 16 * d - 2 * b * b * b, 0);
            var Q4 = new ComplexNumber(3 * b * b - 8 * c, 0);

            var radicalTerm = 0.25 * Q2 * Q2 - Q1 * Q1 * Q1;
            var Q5 = radicalTerm > 0 ? new ComplexNumber(Math.Cbrt(0.5 * Q2 + Math.Sqrt(radicalTerm)), 0) :
                new ComplexNumber(Math.Cbrt(0.5 * Q2), Math.Cbrt(Math.Sqrt(-radicalTerm)));
            var Q6 = (Q1 / Q5 + Q5) / 3;
            var Q7 = 2 * ComplexNumber.Sqrt(Q4 / 12 + Q6);

            var Q8 = ComplexNumber.Sqrt((0.66666666667 * Q4) - (4 * Q6) - (Q3 / Q7));
            var negBAsComplex = new ComplexNumber(-b, 0);
            yield return 0.25 * (negBAsComplex - Q7 - Q8);
            yield return 0.25 * (negBAsComplex - Q7 + Q8);
            yield return 0.25 * (negBAsComplex + Q7 - Q8);
            yield return 0.25 * (negBAsComplex + Q7 + Q8);
        }

        public static IEnumerable<ComplexNumber> IntersectingConics()
        {
            throw new NotImplementedException();
        }
    }
}
