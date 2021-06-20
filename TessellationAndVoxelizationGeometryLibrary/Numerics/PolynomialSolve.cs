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
            if (coeffList.Count == 3) return Quadratic(coeffList);
            if (coeffList.Count == 4) return Cubic(coeffList);
            if (coeffList.Count == 5) return Quartic(coeffList);
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
            throw new NotImplementedException();
        }

        public static IEnumerable<ComplexNumber> IntersectingConics()
        {
            throw new NotImplementedException();
        }
    }
}
