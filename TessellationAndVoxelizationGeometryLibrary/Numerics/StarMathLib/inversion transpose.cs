// ***********************************************************************
// Assembly         : StarMath
// Author           : MICampbell
// Created          : 05-14-2015
//
// Last Modified By : MICampbell
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="inversion transpose.cs" company="Design Engineering Lab -- MICampbell">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TVGL;

namespace StarMathLib
{
    /// <summary>
    /// Class StarMath.
    /// </summary>
    public static partial class StarMath
    {
        #region Matrix Inversion
        /// <summary>
        /// Inverses the matrix A only if the diagonal is all non-zero.
        /// A[i,i] != 0.0
        /// </summary>
        /// <param name="A">The matrix to invert. This matrix is unchanged by this function.</param>
        /// <returns>The inverted matrix, A^-1.</returns>
        /// <exception cref="System.ArithmeticException">Matrix cannnot be inverted. Can only invert sqare matrices.</exception>
        public static double[,] Inverse(this double[,] A) // need bool IsSymmetric to switch to Cholesky
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("Matrix cannnot be inverted. Can only invert sqare matrices.");
            if (length == 1) return new[,] { { 1 / A[0, 0] } };

            var LU = LUDecomposition(A, out var permutationVector, length);
            return inverseWithLUResult(LU, permutationVector, length);
        }

        /// <summary>
        /// Attempts to invert a square matrix without changing it.
        /// </summary>
        /// <param name="A">The matrix to invert.</param>
        /// <param name="inverse">The inverse on success; null on failure.</param>
        /// <returns>False if a zero pivot or non-finite intermediate or result is encountered.
        /// A successful result does not guarantee that the matrix is well-conditioned.</returns>
        /// <exception cref="ArgumentNullException">A is null.</exception>
        /// <exception cref="ArithmeticException">A is not square.</exception>
        public static bool TryInverse(this double[,] A, out double[,] inverse)
        {
            if (A == null) throw new ArgumentNullException(nameof(A));
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("Matrix cannot be inverted. Can only invert square matrices.");
            inverse = null;
            if (!TryLUDecomposition(A, out var lu, out var permutation, out _, length))
                return false;
            var result = inverseWithLUResult(lu, permutation, length);
            foreach (var value in result)
                if (!double.IsFinite(value)) return false;
            inverse = result;
            return true;
        }

        /// <summary>
        /// Inverses the matrix A only if the diagonal is all non-zero.
        /// A[i,i] != 0.0
        /// </summary>
        /// <param name="A">The matrix to invert. This matrix is unchanged by this function.</param>
        /// <returns>The inverted matrix, A^-1.</returns>
        /// <exception cref="System.ArithmeticException">Matrix cannnot be inverted. Can only invert sqare matrices.</exception>
        internal static double[,] inverse(this int[,] A)
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("Matrix cannnot be inverted. Can only invert sqare matrices.");
            if (length == 1) return new[,] { { 1 / (double)A[0, 0] } };
            var LU = LUDecomposition(A, out var permute, length);
            return inverseWithLUResult(LU, permute, length);
        }

        /// <summary>
        /// Inverses the with lu result.
        /// </summary>
        /// <param name="B">The b.</param>
        /// <param name="permute">The permute.</param>
        /// <param name="length">The length.</param>
        /// <returns>System.Double[].</returns>
        private static double[,] inverseWithLUResult(double[,] lu, int[] permute, int length)
        {
            var inverse = new double[length, length];
            var x = new double[length];
            for (var column = 0; column < length; column++)
            {
                // Solve L y = P e_column, followed by U x = y.
                for (var i = 0; i < length; i++)
                {
                    var row = permute[i];
                    var value = row == column ? 1.0 : 0.0;
                    for (var j = 0; j < i; j++) value -= lu[row, j] * x[j];
                    x[i] = value / lu[row, i];
                }
                for (var i = length - 1; i >= 0; i--)
                {
                    for (var j = i + 1; j < length; j++) x[i] -= lu[permute[i], j] * x[j];
                    inverse[i, column] = x[i];
                }
            }
            return inverse;
        }

        #endregion

        #region LU Decomposition

        /// <summary>
        /// Factors A as L U, with rows stored in the original order and indexed by
        /// permute. L contains the diagonal and U has a unit diagonal.
        /// </summary>
        internal static void LUDecomposition(double[,] A, out double[,] L, out double[,] U, out int[] permute)
        {
            var length = A.GetLength(0);
            L = LUDecomposition(A, out permute, length);
            U = new double[length, length];
            for (var i = 0; i < length; i++)
            {
                U[permute[i], i] = 1.0;
                for (var j = i + 1; j < length; j++)
                {
                    U[permute[i], j] = L[permute[i], j];
                    L[permute[i], j] = 0.0;
                }
            }
        }

        /// <summary>
        /// Crout factorization with partial pivoting. The packed factors retain the
        /// original row locations; permutationVector maps factor rows to input rows.
        /// </summary>
        private static double[,] LUDecomposition(double[,] A, out int[] permutationVector, int length = -1)
        {
            if (!TryLUDecomposition(A, out var result, out permutationVector, out var nonFinite, length))
            {
                if (nonFinite)
                    throw new InvalidOperationException("LU decomposition encountered a non-finite value.");
                throw new ArithmeticException("Matrix is singular; LU decomposition cannot be completed.");
            }
            return result;
        }

        private static bool TryLUDecomposition(double[,] A, out double[,] B, out int[] permutationVector,
            out bool nonFinite, int length = -1)
        {
            if (length == -1) length = A.GetLength(0);
            if (length != A.GetLength(0) || length != A.GetLength(1))
                throw new ArithmeticException("LU Decomposition can only be determined for square matrices.");

            B = (double[,])A.Clone();
            permutationVector = Enumerable.Range(0, length).ToArray();
            nonFinite = false;
            for (var i = 0; i < length; i++)
            {
                // Complete the current column of L before selecting its pivot.
                var pivot = i;
                var largest = 0.0;
                for (var j = i; j < length; j++)
                {
                    var row = permutationVector[j];
                    var value = B[row, i];
                    for (var k = 0; k < i; k++)
                        value -= B[row, k] * B[permutationVector[k], i];
                    B[row, i] = value;
                    if (!double.IsFinite(value))
                    {
                        nonFinite = true;
                        return false;
                    }
                    if (Math.Abs(value) > largest)
                    {
                        largest = Math.Abs(value);
                        pivot = j;
                    }
                }
                if (largest == 0.0) return false;
                (permutationVector[i], permutationVector[pivot]) = (permutationVector[pivot], permutationVector[i]);
                var pivotRow = permutationVector[i];
                for (var j = i + 1; j < length; j++)
                {
                    var value = B[pivotRow, j];
                    for (var k = 0; k < i; k++)
                        value -= B[pivotRow, k] * B[permutationVector[k], j];
                    B[pivotRow, j] = value / B[pivotRow, i];
                    if (!double.IsFinite(B[pivotRow, j]))
                    {
                        nonFinite = true;
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Factors an integer matrix using the same pivoting as the double overload.</summary>
        internal static void LUDecomposition(int[,] A, out double[,] L, out double[,] U, out int[] permute)
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("LU Decomposition can only be determined for square matrices.");
            var values = new double[length, length];
            for (var i = 0; i < length; i++)
                for (var j = 0; j < length; j++)
                    values[i, j] = A[i, j];
            LUDecomposition(values, out L, out U, out permute);
        }

        private static double[,] LUDecomposition(int[,] A, out int[] permutationVector, int length = -1)
        {
            if (length == -1) length = A.GetLength(0);
            if (length != A.GetLength(0) || length != A.GetLength(1))
                throw new ArithmeticException("LU Decomposition can only be determined for square matrices.");
            var values = new double[length, length];
            for (var i = 0; i < length; i++)
                for (var j = 0; j < length; j++)
                    values[i, j] = A[i, j];
            return LUDecomposition(values, out permutationVector, length);
        }

        #endregion

        #region Cholesky Decomposition
        /// <summary>
        /// Returns the Cholesky decomposition of A in a new matrix. The new matrix is a lower triangular matrix, and
        /// the diagonals are the D matrix in the L-D-LT formulation. To get the L-LT format.
        /// </summary>
        /// <param name="A">The matrix to invert. This matrix is unchanged by this function.</param>
        /// <param name="LUMatrix">The lu matrix.</param>
        /// <param name="NoSeparateDiagonal">if set to <c>true</c> [no separate diagonal].</param>
        /// <returns>System.Double[].</returns>
        /// <exception cref="System.ArithmeticException">Matrix cannot be inverted. Can only invert square matrices.</exception>
        /// <exception cref="ArithmeticException">Matrix cannot be inverted. Can only invert square matrices.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool CholeskyDecomposition(double[,] A, IList<double> b, out double[] answer, bool NoSeparateDiagonal = false)
        {
            var LUMatrix = (double[,])A.Clone();
            var length = A.GetLength(0);
            answer = new double[length];
            if (length != A.GetLength(1)) return false;
            // throw new ArithmeticException("Cholesky Decomposition can only be determined for square matrices.");

            for (var i = 0; i < length; i++)
            {
                double sum;
                for (var j = 0; j < i; j++)
                {
                    sum = 0.0;
                    for (int k = 0; k < j; k++)
                        sum += LUMatrix[i, k] * LUMatrix[j, k] * LUMatrix[k, k];
                    if (LUMatrix[j, j].IsNegligible())
                    {
                        if (LUMatrix[i, j].IsPracticallySame(sum, Constants.BaseTolerance)) LUMatrix[i, j] = 0.0;
                        else return false;
                    }
                    else
                        LUMatrix[i, j] = (LUMatrix[i, j] - sum) / LUMatrix[j, j];
                }
                sum = 0.0;
                for (int k = 0; k < i; k++)
                    sum += LUMatrix[i, k] * LUMatrix[i, k] * LUMatrix[k, k];
                LUMatrix[i, i] -= sum;
                for (int j = i + 1; j < length; j++)
                    LUMatrix[i, j] = 0.0;
            }
            if (NoSeparateDiagonal)
                for (int i = 0; i < length; i++)
                {
                    if (LUMatrix[i, i] < 0)
                        return false;
                    //throw new ArithmeticException("Cannot complete L-LT Cholesky Decomposition due to indefinite matrix (must be positive semidefinite).");
                    LUMatrix[i, i] = Math.Sqrt(LUMatrix[i, i]);
                }
            // forward substitution
            for (int i = 0; i < length; i++)
            {
                var sumFromKnownTerms = 0.0;
                for (int j = 0; j < i; j++)
                    sumFromKnownTerms += LUMatrix[i, j] * answer[j];
                answer[i] = (b[i] - sumFromKnownTerms);
            }

            for (int i = 0; i < length; i++)
            {
                if (LUMatrix[i, i] == 0.0)
                {
                    if (!answer[i].IsNegligible())
                        return false;
                }
                else answer[i] /= LUMatrix[i, i];
            }
            // backward substitution
            for (int i = length - 1; i >= 0; i--)
            {
                var sumFromKnownTerms = 0.0;
                for (int j = i + 1; j < length; j++)
                    sumFromKnownTerms += LUMatrix[j, i] * answer[j];
                answer[i] -= sumFromKnownTerms;
            }
            return true;
        }

        #endregion

        #region Transpose

        /// <summary>
        /// Transposes the matrix, A.
        /// </summary>
        /// <param name="A">The matrix to transpose. This matrix is unchanged by this function.</param>
        /// <returns>The transpose of A.</returns>
        /// <exception cref="System.ArithmeticException">The matrix, A, is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double[,] Transpose(this double[,] A)
        {
            if (A == null) throw new ArithmeticException("The matrix, A, is null.");
            var numRows = A.GetLength(1);
            var numCols = A.GetLength(0);
            var C = new double[numRows, numCols];

            for (var i = 0; i != numRows; i++)
                for (var j = 0; j != numCols; j++)
                    C[i, j] = A[j, i];
            return C;
        }

        /// <summary>
        /// Transposes the matrix, A.
        /// </summary>
        /// <param name="A">The matrix to transpose. This matrix is unchanged by this function.</param>
        /// <returns>The transpose of A.</returns>
        /// <exception cref="System.ArithmeticException">The matrix, A, is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int[,] Transpose(this int[,] A)
        {
            if (A == null) throw new ArithmeticException("The matrix, A, is null.");
            var numRows = A.GetLength(1);
            var numCols = A.GetLength(0);

            var C = new int[numRows, numCols];

            for (var i = 0; i != numRows; i++)
                for (var j = 0; j != numCols; j++)
                    C[i, j] = A[j, i];
            return C;
        }

        #endregion

        #region IsSingular
        /// <summary>
        /// Determines whether the specified a is singular.
        /// </summary>
        /// <param name="A">a.</param>
        /// <returns><c>true</c> if the specified a is singular; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSingular(this double[,] A)
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1)) return true;
            if (length > 6)
            { // if matrix has length greater than 7 it'll take
                //fewer computations to first check for a row or
                //column of all zeros
                for (int i = 0; i < length; i++)
                {
                    // first check for row of all zeros
                    var nonZeroFound = false;
                    for (int j = 0; j < length; j++)
                    {
                        if (!A[i, j].IsNegligible())
                        {
                            nonZeroFound = true;
                            break;
                        }
                    }
                    if (!nonZeroFound) return true;
                    // now check for column of all zeros
                    nonZeroFound = false;
                    for (int j = 0; j < length; j++)
                    {
                        if (!A[j, i].IsNegligible())
                        {
                            nonZeroFound = true;
                            break;
                        }
                    }
                    if (!nonZeroFound) return true;
                }
            }
            return A.Determinant().IsNegligible();
        }

        #endregion

        #region Determinant

        /// <summary>
        /// Returns the Determinant of matrix, A.
        /// </summary>
        /// <param name="A">The input argument matrix. This matrix is unchanged by this function.</param>
        /// <returns>a single value representing the matrix's Determinant.</returns>
        /// <exception cref="System.ArithmeticException">The matrix, A, is null.</exception>
        /// <exception cref="System.ArithmeticException">The Determinant is only possible for square matrices.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Determinant(this double[,] A)
        {
            if (A == null) throw new ArithmeticException("The matrix, A, is null.");
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("The Determinant is only possible for square matrices.");
            if (length == 0) return 0.0;
            if (length == 1) return A[0, 0];
            if (length == 2) return (A[0, 0] * A[1, 1]) - (A[0, 1] * A[1, 0]);
            if (length == 3)
                return (A[0, 0] * A[1, 1] * A[2, 2])
                       + (A[0, 1] * A[1, 2] * A[2, 0])
                       + (A[0, 2] * A[1, 0] * A[2, 1])
                       - (A[0, 0] * A[1, 2] * A[2, 1])
                       - (A[0, 1] * A[1, 0] * A[2, 2])
                       - (A[0, 2] * A[1, 1] * A[2, 0]);
            return determinantBig(A, length);
        }
        /// <summary>
        /// Determinants the specified a.
        /// </summary>
        /// <param name="A">a.</param>
        /// <returns>ComplexNumber.</returns>
        /// <exception cref="System.ArithmeticException">The matrix, A, is null.</exception>
        /// <exception cref="System.ArithmeticException">The Determinant is only possible for square matrices.</exception>
        /// <exception cref="System.NotImplementedException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ComplexNumber Determinant(this ComplexNumber[,] A)
        {
            if (A == null) throw new ArithmeticException("The matrix, A, is null.");
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("The Determinant is only possible for square matrices.");
            if (length == 0) return new ComplexNumber();
            if (length == 1) return A[0, 0];
            if (length == 2) return (A[0, 0] * A[1, 1]) - (A[0, 1] * A[1, 0]);
            if (length == 3)
                return (A[0, 0] * A[1, 1] * A[2, 2])
                       + (A[0, 1] * A[1, 2] * A[2, 0])
                       + (A[0, 2] * A[1, 0] * A[2, 1])
                       - (A[0, 0] * A[1, 2] * A[2, 1])
                       - (A[0, 1] * A[1, 0] * A[2, 2])
                       - (A[0, 2] * A[1, 1] * A[2, 0]);
            throw new NotImplementedException();
            //return determinantBig(A, length);
        }

        public static double GetDeterminant4(double a, double b, double c, double d, double e, double f, double g, double h, double i, double j,
            double k, double l, double m, double n, double o, double p)
        {
            double kp_lo = k * p - l * o;
            double jp_ln = j * p - l * n;
            double jo_kn = j * o - k * n;
            double ip_lm = i * p - l * m;
            double io_km = i * o - k * m;
            double in_jm = i * n - j * m;

            return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
                   b * (e * kp_lo - g * ip_lm + h * io_km) +
                   c * (e * jp_ln - f * ip_lm + h * in_jm) -
                   d * (e * jo_kn - f * io_km + g * in_jm);
        }
        public static ComplexNumber GetDeterminant4(ComplexNumber a, ComplexNumber b, ComplexNumber c, ComplexNumber d, ComplexNumber e, ComplexNumber f, ComplexNumber g, ComplexNumber h, ComplexNumber i, ComplexNumber j,
            ComplexNumber k, ComplexNumber l, ComplexNumber m, ComplexNumber n, ComplexNumber o, ComplexNumber p)
        {
            var kp_lo = k * p - l * o;
            var jp_ln = j * p - l * n;
            var jo_kn = j * o - k * n;
            var ip_lm = i * p - l * m;
            var io_km = i * o - k * m;
            var in_jm = i * n - j * m;

            return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
                   b * (e * kp_lo - g * ip_lm + h * io_km) +
                   c * (e * jp_ln - f * ip_lm + h * in_jm) -
                   d * (e * jo_kn - f * io_km + g * in_jm);
        }
        /// <summary>
        /// Determinants the big.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="length">The length.</param>
        /// <returns>System.Double.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double determinantBig(double[,] A, int length)
        {
            try
            {
                var L = LUDecomposition(A, out var permute, length);
                var result = PermutationSign(permute);
                for (var i = 0; i < length; i++) result *= L[permute[i], i];
                return result;
            }
            catch (ArithmeticException) { return 0; }
        }

        /// <summary>
        /// Returns the Determinant of matrix, A.
        /// </summary>
        /// <param name="A">The input argument matrix. This matrix is unchanged by this function.</param>
        /// <returns>a single value representing the matrix's Determinant.</returns>
        /// <exception cref="System.ArithmeticException">The matrix, A, is null.</exception>
        /// <exception cref="System.ArithmeticException">The Determinant is only possible for square matrices.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int determinant(this int[,] A)
        {
            if (A == null) throw new ArithmeticException("The matrix, A, is null.");
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("The Determinant is only possible for square matrices.");
            if (length == 0) return 0;
            if (length == 1) return A[0, 0];
            if (length == 2) return (A[0, 0] * A[1, 1]) - (A[0, 1] * A[1, 0]);
            if (length == 3)
                return (A[0, 0] * A[1, 1] * A[2, 2])
                       + (A[0, 1] * A[1, 2] * A[2, 0])
                       + (A[0, 2] * A[1, 0] * A[2, 1])
                       - (A[0, 0] * A[1, 2] * A[2, 1])
                       - (A[0, 1] * A[1, 0] * A[2, 2])
                       - (A[0, 2] * A[1, 1] * A[2, 0]);
            return determinantBig(A, length);
        }

        /// <summary>
        /// Returns the Determinant of matrix, A. Only used internally for matrices larger than 3.
        /// </summary>
        /// <param name="A">The input argument matrix. This matrix is unchanged by this function.</param>
        /// <param name="length">The length of the side of the square matrix.</param>
        /// <returns>a single value representing the matrix's Determinant.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int determinantBig(int[,] A, int length)
        {
            try
            {
                var L = LUDecomposition(A, out var permute, length);
                double result = PermutationSign(permute);
                for (var i = 0; i < length; i++) result *= L[permute[i], i];
                return (int)Math.Round(result);
            }
            catch (ArithmeticException) { return 0; }
        }

        private static double PermutationSign(int[] permutation)
        {
            var sign = 1.0;
            for (var i = 0; i < permutation.Length; i++)
                for (var j = i + 1; j < permutation.Length; j++)
                    if (permutation[i] > permutation[j]) sign = -sign;
            return sign;
        }

        #endregion
    }
}
