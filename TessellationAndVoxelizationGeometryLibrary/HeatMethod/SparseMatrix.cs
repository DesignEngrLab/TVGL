// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="SparseMatrix.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>A high-performance sparse matrix implementation using Compressed Sparse Row (CSR) format.</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// A high-performance sparse matrix with Compressed Sparse Row (CSR) storage,
    /// optimized for geometric PDEs and iterative linear solvers.
    /// </summary>
    public class SparseMatrix
    {
        private readonly Dictionary<int, double>[]? _dynamicRows;
        private bool _isCompressed;

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        public int Rows { get; }

        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        public int Columns { get; }

        /// <summary>
        /// Gets the row pointers array for CSR format.
        /// </summary>
        public int[] RowPointers { get; private set; }

        /// <summary>
        /// Gets the column indices array for CSR format.
        /// </summary>
        public int[] ColumnIndices { get; private set; }

        /// <summary>
        /// Gets the non-zero values array for CSR format.
        /// </summary>
        public double[] Values { get; private set; }

        /// <summary>
        /// Gets the total number of non-zero entries.
        /// </summary>
        public int NonZeroCount => _isCompressed ? Values.Length : GetDynamicNonZeroCount();

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseMatrix"/> class.
        /// </summary>
        /// <param name="rows">The number of rows.</param>
        /// <param name="cols">The number of columns.</param>
        public SparseMatrix(int rows, int cols)
        {
            Rows = rows;
            Columns = cols;
            _dynamicRows = new Dictionary<int, double>[rows];
            for (int i = 0; i < rows; i++)
            {
                _dynamicRows[i] = new Dictionary<int, double>();
            }
            RowPointers = Array.Empty<int>();
            ColumnIndices = Array.Empty<int>();
            Values = Array.Empty<double>();
            _isCompressed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseMatrix"/> class from existing CSR data.
        /// </summary>
        public SparseMatrix(int rows, int cols, int[] rowPointers, int[] columnIndices, double[] values)
        {
            Rows = rows;
            Columns = cols;
            RowPointers = rowPointers;
            ColumnIndices = columnIndices;
            Values = values;
            _isCompressed = true;
            _dynamicRows = null;
        }

        private int GetDynamicNonZeroCount()
        {
            if (_dynamicRows == null) return 0;
            int count = 0;
            for (int i = 0; i < Rows; i++)
                count += _dynamicRows[i].Count;
            return count;
        }

        /// <summary>
        /// Adds a value to the specified matrix coordinate (row, col).
        /// </summary>
        public void Add(int row, int col, double value)
        {
            if (_isCompressed)
                throw new InvalidOperationException("Cannot add entries to a compressed SparseMatrix. Uncompress or build before compression.");

            if (Math.Abs(value) < 1e-30) return;

            var rowDict = _dynamicRows![row];
            if (rowDict.TryGetValue(col, out var currentVal))
            {
                rowDict[col] = currentVal + value;
            }
            else
            {
                rowDict[col] = value;
            }
        }

        /// <summary>
        /// Sets a value at the specified matrix coordinate (row, col).
        /// </summary>
        public void Set(int row, int col, double value)
        {
            if (_isCompressed)
                throw new InvalidOperationException("Cannot set entries on a compressed SparseMatrix.");

            _dynamicRows![row][col] = value;
        }

        /// <summary>
        /// Compresses the dynamic builder into the CSR format for fast SpMV and solving.
        /// </summary>
        public void Compress()
        {
            if (_isCompressed) return;

            int totalNnz = GetDynamicNonZeroCount();
            RowPointers = new int[Rows + 1];
            ColumnIndices = new int[totalNnz];
            Values = new double[totalNnz];

            int nnzIdx = 0;
            for (int i = 0; i < Rows; i++)
            {
                RowPointers[i] = nnzIdx;
                var rowDict = _dynamicRows![i];
                if (rowDict.Count > 0)
                {
                    // Sort columns for deterministic CSR layout and cache locality
                    var sortedKeys = new int[rowDict.Count];
                    rowDict.Keys.CopyTo(sortedKeys, 0);
                    Array.Sort(sortedKeys);

                    foreach (var col in sortedKeys)
                    {
                        ColumnIndices[nnzIdx] = col;
                        Values[nnzIdx] = rowDict[col];
                        nnzIdx++;
                    }
                }
            }
            RowPointers[Rows] = nnzIdx;
            _isCompressed = true;
        }

        /// <summary>
        /// Multiplies this sparse matrix by a vector: y = A * x.
        /// </summary>
        public void Multiply(double[] x, double[] y)
        {
            if (!_isCompressed)
                Compress();

            for (int i = 0; i < Rows; i++)
            {
                double sum = 0.0;
                int start = RowPointers[i];
                int end = RowPointers[i + 1];
                for (int p = start; p < end; p++)
                {
                    sum += Values[p] * x[ColumnIndices[p]];
                }
                y[i] = sum;
            }
        }

        /// <summary>
        /// Multiplies this sparse matrix by a vector: returns A * x.
        /// </summary>
        public double[] Multiply(double[] x)
        {
            var y = new double[Rows];
            Multiply(x, y);
            return y;
        }

        /// <summary>
        /// Extracts the diagonal elements of the matrix.
        /// </summary>
        public double[] GetDiagonal()
        {
            if (!_isCompressed)
                Compress();

            var diag = new double[Rows];
            for (int i = 0; i < Rows; i++)
            {
                int start = RowPointers[i];
                int end = RowPointers[i + 1];
                for (int p = start; p < end; p++)
                {
                    if (ColumnIndices[p] == i)
                    {
                        diag[i] = Values[p];
                        break;
                    }
                }
            }
            return diag;
        }

        /// <summary>
        /// Computes the linear combination C = alpha * A + beta * B of two sparse matrices.
        /// </summary>
        public static SparseMatrix Combine(SparseMatrix A, SparseMatrix B, double alpha = 1.0, double beta = 1.0)
        {
            if (A.Rows != B.Rows || A.Columns != B.Columns)
                throw new ArgumentException("Matrix dimensions must match for combination.");

            if (!A._isCompressed) A.Compress();
            if (!B._isCompressed) B.Compress();

            var result = new SparseMatrix(A.Rows, A.Columns);

            for (int i = 0; i < A.Rows; i++)
            {
                int aStart = A.RowPointers[i];
                int aEnd = A.RowPointers[i + 1];
                for (int p = aStart; p < aEnd; p++)
                {
                    result.Add(i, A.ColumnIndices[p], alpha * A.Values[p]);
                }

                int bStart = B.RowPointers[i];
                int bEnd = B.RowPointers[i + 1];
                for (int p = bStart; p < bEnd; p++)
                {
                    result.Add(i, B.ColumnIndices[p], beta * B.Values[p]);
                }
            }

            result.Compress();
            return result;
        }
    }
}

