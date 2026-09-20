// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="SparsePcgSolver.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>Preconditioned Conjugate Gradient (PCG) solver for symmetric positive definite sparse linear systems.</summary>
// ***********************************************************************

using System;

namespace TVGL
{
    /// <summary>
    /// Type of preconditioner to use in the PCG solver.
    /// </summary>
    public enum PreconditionerType
    {
        /// <summary>
        /// Jacobi (diagonal) preconditioner.
        /// </summary>
        Jacobi,

        /// <summary>
        /// Symmetric Successive Over-Relaxation (SSOR) preconditioner.
        /// </summary>
        SSOR
    }

    /// <summary>
    /// Preconditioned Conjugate Gradient (PCG) linear solver for symmetric positive definite (SPD) sparse matrices.
    /// </summary>
    public class SparsePcgSolver
    {
        private readonly SparseMatrix _matrix;
        private readonly double[] _diag;
        private readonly double[] _invDiag;
        private readonly PreconditionerType _preconditioner;

        /// <summary>
        /// Maximum number of iterations.
        /// </summary>
        public int MaxIterations { get; set; } = 2000;

        /// <summary>
        /// Relative residual convergence tolerance.
        /// </summary>
        public double Tolerance { get; set; } = 1e-10;

        /// <summary>
        /// Initializes a new instance of the <see cref="SparsePcgSolver"/> class.
        /// </summary>
        /// <param name="matrix">The SPD sparse matrix to solve.</param>
        /// <param name="preconditioner">The preconditioner to use (defaults to SSOR).</param>
        public SparsePcgSolver(SparseMatrix matrix, PreconditionerType preconditioner = PreconditionerType.SSOR)
        {
            _matrix = matrix;
            _preconditioner = preconditioner;
            _diag = _matrix.GetDiagonal();
            _invDiag = new double[_diag.Length];
            for (int i = 0; i < _diag.Length; i++)
            {
                _invDiag[i] = Math.Abs(_diag[i]) > 1e-20 ? 1.0 / _diag[i] : 1.0;
            }
        }

        /// <summary>
        /// Solves A * x = b for x using the Preconditioned Conjugate Gradient method.
        /// </summary>
        /// <param name="b">The right-hand side vector.</param>
        /// <param name="initialGuess">Optional initial guess for x.</param>
        /// <returns>The solution vector x.</returns>
        public double[] Solve(double[] b, double[]? initialGuess = null)
        {
            int n = _matrix.Rows;
            var x = new double[n];
            if (initialGuess != null)
                Array.Copy(initialGuess, x, n);

            var r = new double[n];
            var z = new double[n];
            var p = new double[n];
            var q = new double[n];

            // Compute initial residual r = b - A * x
            _matrix.Multiply(x, q);
            double bNorm2 = 0.0;
            double rNorm2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                r[i] = b[i] - q[i];
                bNorm2 += b[i] * b[i];
                rNorm2 += r[i] * r[i];
            }

            if (bNorm2 < 1e-30)
                return x; // RHS is zero, x = 0 is exact solution

            double tol2 = Tolerance * Tolerance * bNorm2;
            if (rNorm2 <= tol2)
                return x;

            // Apply preconditioner: z = M^-1 * r
            ApplyPreconditioner(r, z);
            Array.Copy(z, p, n);

            double rho = Dot(r, z);

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // q = A * p
                _matrix.Multiply(p, q);

                double pDotQ = Dot(p, q);
                if (Math.Abs(pDotQ) < 1e-30)
                    break;

                double alpha = rho / pDotQ;

                rNorm2 = 0.0;
                for (int i = 0; i < n; i++)
                {
                    x[i] += alpha * p[i];
                    r[i] -= alpha * q[i];
                    rNorm2 += r[i] * r[i];
                }

                if (rNorm2 <= tol2)
                    break;

                // z = M^-1 * r
                ApplyPreconditioner(r, z);

                double rhoNew = Dot(r, z);
                double beta = rhoNew / rho;
                rho = rhoNew;

                for (int i = 0; i < n; i++)
                {
                    p[i] = z[i] + beta * p[i];
                }
            }

            return x;
        }

        private void ApplyPreconditioner(double[] r, double[] z)
        {
            int n = _matrix.Rows;
            if (_preconditioner == PreconditionerType.Jacobi)
            {
                for (int i = 0; i < n; i++)
                    z[i] = r[i] * _invDiag[i];
                return;
            }

            // SSOR Preconditioning: (D + L) * D^-1 * (D + U) * z = r
            // Forward solve: (D + L) * y = r
            var y = new double[n];
            var rowPtr = _matrix.RowPointers;
            var colIdx = _matrix.ColumnIndices;
            var vals = _matrix.Values;

            for (int i = 0; i < n; i++)
            {
                double sum = 0.0;
                int start = rowPtr[i];
                int end = rowPtr[i + 1];
                for (int p = start; p < end; p++)
                {
                    int j = colIdx[p];
                    if (j < i)
                        sum += vals[p] * y[j];
                }
                y[i] = (r[i] - sum) * _invDiag[i];
            }

            // Backward solve: (D + U) * z = D * y
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0.0;
                int start = rowPtr[i];
                int end = rowPtr[i + 1];
                for (int p = start; p < end; p++)
                {
                    int j = colIdx[p];
                    if (j > i)
                        sum += vals[p] * z[j];
                }
                // (D_i * y_i - sum) / D_i = y_i - sum * _invDiag[i]
                z[i] = y[i] - sum * _invDiag[i];
            }
        }

        private static double Dot(double[] a, double[] b)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * b[i];
            return sum;
        }
    }
}

