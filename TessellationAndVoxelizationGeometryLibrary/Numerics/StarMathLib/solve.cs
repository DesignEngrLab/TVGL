// ***********************************************************************
// Assembly         : StarMath
// Author           : MICampbell
// Created          : 05-14-2015
//
// Last Modified By : MICampbell
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="solve.cs" company="Design Engineering Lab -- MICampbell">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TVGL;

namespace StarMathLib
{
    /// <summary>
    /// Class StarMath.
    /// </summary>
    public static partial class StarMath
    {
        /// <summary>
        /// Solves the specified A matrix.
        /// </summary>
        /// <param name="A">The A.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <param name="IsASymmetric">Is matrix A symmetric.</param>
        /// <returns>System.Double[].</returns>
        /// <exception cref="System.ArithmeticException">Matrix, A, must be square.</exception>
        /// <exception cref="System.ArithmeticException">Matrix, A, must be have the same number of rows as the vector, b.</exception>
        public static bool solve(this double[,] A, IList<double> b, out double[] answer,
            Boolean IsASymmetric = false)
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("Matrix, A, must be square.");
            if (length != b.Count)
                throw new ArithmeticException("Matrix, A, must be have the same number of rows as the vector, b.");
            if (length == 3)
                return solveViaCramersRule3(A, b, out answer);
            if (length == 2)
                return solveViaCramersRule2(A, b, out answer);

            return solveBig(A, b, out answer, IsASymmetric);
        }

        /// <summary>
        /// Solves the via cramers rule3.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool solveViaCramersRule3(this double[,] a, IList<double> b, out double[] answer)
        {
            var oneOverDeterminant = 1 / Determinant(a);
            var x = oneOverDeterminant * ((b[0] * a[1, 1] * a[2, 2])
                 + (a[0, 1] * a[1, 2] * b[2])
                 + (a[0, 2] * b[1] * a[2, 1])
                 - (b[0] * a[1, 2] * a[2, 1])
                 - (a[0, 1] * b[1] * a[2, 2])
                 - (a[0, 2] * a[1, 1] * b[2]));
            if (double.IsNaN(x))
            {
                answer = Array.Empty<double>();
                return false;
            }
            var y =
               oneOverDeterminant * ((a[0, 0] * b[1] * a[2, 2])
                  + (b[0] * a[1, 2] * a[2, 0])
                  + (a[0, 2] * a[1, 0] * b[2])
                  - (a[0, 0] * a[1, 2] * b[2])
                  - (b[0] * a[1, 0] * a[2, 2])
                  - (a[0, 2] * b[1] * a[2, 0]));
            if (double.IsNaN(y))
            {
                answer = Array.Empty<double>();
                return false;
            }
            var z = oneOverDeterminant * ((a[0, 0] * a[1, 1] * b[2])
                              + (a[0, 1] * b[1] * a[2, 0])
                              + (b[0] * a[1, 0] * a[2, 1])
                              - (a[0, 0] * b[1] * a[2, 1])
                              - (a[0, 1] * a[1, 0] * b[2])
                              - (b[0] * a[1, 1] * a[2, 0]));
            if (double.IsNaN(z))
            {
                answer = Array.Empty<double>();
                return false;
            }
            answer = [x, y, z];
            return true;
        }


        /// <summary>
        /// Solve3x3s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool Solve3x3ComplexMatrix(ComplexNumber[,] a, IList<double> b, out ComplexNumber[] answer)
        {
            var n = b.Count;
            var bComplex = new ComplexNumber[n];
            for (int i = 0; i < n; i++)
                bComplex[i] = new ComplexNumber(b[i]);
            return Solve3x3ComplexMatrix(a, bComplex, out answer);
        }
        /// <summary>
        /// Solve3x3s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Solve3x3ComplexMatrix(this ComplexNumber[,] a, IList<ComplexNumber> b, out ComplexNumber[] answer)
        {
            var denominator = Determinant(a);
            if (denominator.Length() < TVGL.Constants.BaseTolerance)
            {
                answer = Array.Empty<ComplexNumber>();
                return false;
            }
            denominator = 1 / denominator;
            answer = new[]
            {
              denominator*  ((b[0] * a[1, 1] * a[2, 2])
                 + (a[0, 1] * a[1, 2] * b[2])
                 + (a[0, 2] * b[1] * a[2, 1])
                 - (b[0] * a[1, 2] * a[2, 1])
                 - (a[0, 1] * b[1] * a[2, 2])
                 - (a[0, 2] * a[1, 1] * b[2])),
               denominator*   ( (a[0, 0] * b[1] * a[2, 2])
                  + (b[0] * a[1, 2] * a[2, 0])
                  + (a[0, 2] * a[1, 0] * b[2])
                  - (a[0, 0] * a[1, 2] * b[2])
                  - (b[0] * a[1, 0] * a[2, 2])
                  - (a[0, 2] * b[1] * a[2, 0])),
               denominator*   ( (a[0, 0] * a[1, 1] * b[2])
                  + (a[0, 1] * b[1] * a[2, 0])
                  + (b[0] * a[1, 0] * a[2, 1])
                  - (a[0, 0] * b[1] * a[2, 1])
                  - (a[0, 1] * a[1, 0] * b[2])
                  - (b[0] * a[1, 1] * a[2, 0]))
            };
            return true;
        }

        /// <summary>
        /// Solves the via cramers rule2.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool solveViaCramersRule2(double[,] a, IList<double> b, out double[] answer)
        {
            var denominator = a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0];
            if (denominator == 0)
            {
                answer = Array.Empty<double>();
                return false;
            }
            denominator = 1 / denominator;
            answer = new[]
            {
              denominator * (b[0]*a[1,1]-b[1]*a[0,1]),
              denominator * (b[1]*a[0,0]-b[0]*a[1,0])
            };
            return true;
        }
        /// <summary>
        /// Solve2x2s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool Solve2x2ComplexMatrix(ComplexNumber[,] a, IList<double> b, out ComplexNumber answer0, out ComplexNumber answer1)
        {
            return Solve2x2ComplexMatrix(a[0, 0], a[0, 1], a[1, 0], a[1, 1],
           new ComplexNumber(b[0]), new ComplexNumber(b[1]), out answer0, out answer1);
        }
        /// <summary>
        /// Solve2x2s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Solve2x2ComplexMatrix(ComplexNumber a00, ComplexNumber a01, ComplexNumber a10, ComplexNumber a11,
            ComplexNumber b0, ComplexNumber b1, out ComplexNumber answer0, out ComplexNumber answer1)
        {
            var denominator = a00 * a11 - a01 * a10;
            if (denominator.Length() < TVGL.Constants.BaseTolerance)
            {
                answer0 = ComplexNumber.NaN;
                answer1 = ComplexNumber.NaN;
                return false;
            }
            denominator = 1 / denominator;
            answer0 = denominator * (b0 * a11 - b1 * a01);
            answer1 = denominator * (b1 * a00 - b0 * a10);
            return true;
        }


        /// <summary>
        /// Solve2x2s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool Solve3x3ComplexMatrix(ComplexNumber[,] a, IList<double> b, out ComplexNumber answer0, out ComplexNumber answer1,
             out ComplexNumber answer2)
        {
            return Solve3x3ComplexMatrix(a[0, 0], a[0, 1], a[0, 2],
                a[1, 0], a[1, 1], a[1, 2],
                a[2, 0], a[2, 1], a[2, 2],
                new ComplexNumber(b[0]), new ComplexNumber(b[1]), new ComplexNumber(b[2]),
                out answer0, out answer1, out answer2);
        }
        /// <summary>
        /// Solve2x2s the complex matrix.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Solve3x3ComplexMatrix(ComplexNumber M11, ComplexNumber M12, ComplexNumber M13, ComplexNumber M21,
            ComplexNumber M22, ComplexNumber M23, ComplexNumber M31, ComplexNumber M32, ComplexNumber M33,
            ComplexNumber b0, ComplexNumber b1, ComplexNumber b2, out ComplexNumber answer0, out ComplexNumber answer1, out ComplexNumber answer2)
        {
            var det = (M11 * M22 * M33)
                      + (M12 * M23 * M31)
                      + (M13 * M21 * M32)
                      - (M11 * M23 * M32)
                      - (M12 * M21 * M33)
                      - (M13 * M22 * M31);
            if (det.IsNegligible())
            {
                answer0 = ComplexNumber.NaN;
                answer1 = ComplexNumber.NaN;
                answer2 = ComplexNumber.NaN;
                return false;
            }
            var invDet = ComplexNumber.Reciprocal(det);
            var N11 = (M22 * M33 - M23 * M32) * invDet;
            var N12 = (M13 * M32 - M12 * M33) * invDet;
            var N13 = (M12 * M23 - M13 * M22) * invDet;
            // Second row
            var N21 = (M23 * M31 - M21 * M33) * invDet;
            var N22 = (M11 * M33 - M13 * M31) * invDet;
            var N23 = (M13 * M21 - M11 * M23) * invDet;
            // Third row
            var N31 = (M21 * M32 - M31 * M22) * invDet;
            var N32 = (M31 * M12 - M11 * M32) * invDet;
            var N33 = (M11 * M22 - M12 * M21) * invDet;

            answer0 = N11 * b0 + N12 * b1 + N13 * b2;
            answer1 = N21 * b0 + N22 * b1 + N23 * b2;
            answer2 = N31 * b0 + N32 * b1 + N33 * b2;

            return true;
        }


        /// <summary>
        /// Solves the by Inverse.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="answer">The answer.</param>
        /// <param name="IsASymmetric">Is A known to be Symmetric?</param>
        /// <returns>System.Double[].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool solveBig(double[,] A, IList<double> b, out double[] answer, bool IsASymmetric = false)
        {
            var length = b.Count;
            if (IsASymmetric)
            {
                if (!CholeskyDecomposition(A, out var L))
                {
                    answer = Array.Empty<double>();
                    return false;
                }
                answer = new double[length];
                // forward substitution
                for (int i = 0; i < length; i++)
                {
                    var sumFromKnownTerms = 0.0;
                    for (int j = 0; j < i; j++)
                        sumFromKnownTerms += L[i, j] * answer[j];
                    answer[i] = (b[i] - sumFromKnownTerms);
                }

                for (int i = 0; i < length; i++)
                {
                    if (L[i, i] == 0) return false;
                    answer[i] /= L[i, i];
                }
                // backward substitution
                for (int i = length - 1; i >= 0; i--)
                {
                    var sumFromKnownTerms = 0.0;
                    for (int j = i + 1; j < length; j++)
                        sumFromKnownTerms += L[j, i] * answer[j];
                    answer[i] -= sumFromKnownTerms;
                }
                return true;
            }
            else
            {
                double[,] LU = null;
                int[] permutationVector = null;
                try
                {
                    LU = LUDecomposition(A, out permutationVector, length);
                }
                catch
                {
                    answer = Array.Empty<double>();
                    return false;
                }
                answer = new double[length];
                // forward substitution
                for (int i = 0; i < length; i++)
                {
                    var sumFromKnownTerms = 0.0;
                    for (int j = 0; j < i; j++)
                        sumFromKnownTerms += LU[permutationVector[i], j] * answer[j];
                    answer[i] = (b[permutationVector[i]] - sumFromKnownTerms) / LU[permutationVector[i], i];
                }
                // backward substitution
                for (int i = length - 1; i >= 0; i--)
                {
                    var sumFromKnownTerms = 0.0;
                    for (int j = i + 1; j < length; j++)
                        sumFromKnownTerms += LU[permutationVector[i], j] * answer[j];
                    answer[i] -= sumFromKnownTerms;
                }
                return true;
            }
        }
    }
}