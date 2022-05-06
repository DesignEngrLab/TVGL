// ***********************************************************************
// Assembly         : StarMath
// Author           : MICampbell
// Created          : 05-14-2015
//
// Last Modified By : MICampbell
// Last Modified On : 07-07-2015
// ***********************************************************************
// <copyright file="solve.cs" company="Design Engineering Lab -- MICampbell">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StarMathLib
{
    public static partial class StarMath
    {
        /// <summary>
        /// Solves the specified A matrix.
        /// </summary>
        /// <param name="A">The A.</param>
        /// <param name="b">The b.</param>
        /// <param name="initialGuess">The initial guess.</param>
        /// <param name="IsASymmetric">Is matrix A symmetric.</param>
        /// <returns>System.Double[].</returns>
        /// <exception cref="System.ArithmeticException">Matrix, A, must be square.
        /// or
        /// Matrix, A, must be have the same number of rows as the vector, b.</exception>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool solveViaCramersRule3(this double[,] a, IList<double> b, out double[] answer)
        {
            var denominator = determinant(a);
            if (Math.Abs(denominator) < TVGL.Constants.BaseTolerance)
            {
                answer = Array.Empty<double>();
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
        /// Solves the by inverse.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="IsASymmetric">Is A known to be Symmetric?</param>
        /// <param name="potentialDiagonals">The potential diagonals.</param>
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