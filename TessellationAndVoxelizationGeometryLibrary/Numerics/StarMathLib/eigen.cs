// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="eigen.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
/*************************************************************************
*     This file & class is part of the StarMath Project
*     Copyright 2014 Matthew Ira Campbell, PhD.
*
*     StarMath is free software: you can redistribute it and/or modify
*     it under the terms of the MIT License as published by
*     the Free Software Foundation, either version 3 of the License, or
*     (at your option) any later version.
*  
*     StarMath is distributed in the hope that it will be useful,
*     but WITHOUT ANY WARRANTY; without even the implied warranty of
*     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*     MIT License for more details.
*  
*     You should have received a copy of the MIT License
*     along with StarMath.  If not, see <http://www.gnu.org/licenses/>.
*     
*     Please find further details and contact information on StarMath
*     at http://starmath.codeplex.com/.
*************************************************************************/

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
        /// <summary>
        /// Gets the eigenvalues for matrix, A.
        /// </summary>
        /// <param name="A">the matrix in question, A.</param>
        /// <returns>The eigenvalues as complex numbers.</returns>
        public static ComplexNumber[] GetEigenValues(this double[,] A)
        {
            return GetEigenValuesAndVectors(A, out _);
        }

        /// <summary>
        /// Gets the eigenvalues and eigenvectors for matrix, A.
        /// </summary>
        /// <param name="A">the matrix in question, A.</param>
        /// <param name="eigenVectors">The eigenvectors as an array of arrays/vectors.</param>
        /// <returns>ComplexNumber[].</returns>
        /// <exception cref="System.ArithmeticException">Matrix, A, must be square.</exception>
        /// <exception cref="System.ArithmeticException">Eigen decomposition does not converge.</exception>
        /// <exception cref="System.ArithmeticException">Eigen decomposition failed due to norm = 0.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber[] GetEigenValuesAndVectors(this double[,] A, out ComplexNumber[][] eigenVectors)
        {
            var length = A.GetLength(0);
            if (length != A.GetLength(1))
                throw new ArithmeticException("Matrix, A, must be square.");
            if (length == 2)
                return GetEigenValuesAndVectors2(A[0, 0], A[0, 1], A[1, 0], A[1, 1], out eigenVectors);
            if (length == 3)
                return GetEigenValuesAndVectors3(A[0, 0], A[0, 1], A[0, 2], A[1, 0], A[1, 1], A[1, 2], A[2, 0], A[2, 1], A[2, 2], out eigenVectors);
            if (length == 4)
                return GetEigenValuesAndVectors4(A[0, 0], A[0, 1], A[0, 2], A[0, 3],
                    A[1, 0], A[1, 1], A[1, 2], A[1, 3],
                    A[2, 0], A[2, 1], A[2, 2], A[2, 3],
                    A[3, 0], A[3, 1], A[3, 2], A[3, 3],
                    out eigenVectors);
            eigenVectors = new ComplexNumber[length][];
            /* start out with the eigenvectors assigned to unit vectors */
            for (var i = 0; i < length; i++)
            {
                var eigenVector = new ComplexNumber[length];
                eigenVector[i] = new ComplexNumber(1.0);
                eigenVectors[i] = eigenVector;
            }
            var eigenvaluesReal = new double[length];
            var eigenvaluesImag = new double[length];
            var B = (double[,])A.Clone();

            #region Reduce to Hessenberg form

            // This is derived from the Algol procedures orthes and ortran,
            // by Martin and Wilkinson, Handbook for Auto. Comp.,
            // Vol.ii-Linear Algebra, and the corresponding
            // Fortran subroutines in EISPACK.
            var ort = new double[length];
            var high = length - 1;
            for (var m = 1; m <= high - 1; m++)
            {
                var mm1 = m - 1;
                // Scale column.
                var scale = 0.0;
                for (var i = m; i <= high; i++)
                    scale += Math.Abs(B[mm1, i]);

                if (!scale.IsNegligible())
                {
                    // Compute Householder transformation.
                    var h = 0.0;
                    for (var i = high; i >= m; i--)
                    {
                        ort[i] = B[mm1, i] / scale;
                        h += ort[i] * ort[i];
                    }

                    var g = Math.Sqrt(h);
                    if (ort[m] > 0) g = -g;
                    h = h - (ort[m] * g);
                    ort[m] = ort[m] - g;

                    // Apply Householder similarity transformation
                    // H = (I-u*u'/h)*H*(I-u*u')/h)
                    for (var j = m; j < length; j++)
                    {
                        var f = 0.0;
                        for (var i = length - 1; i >= m; i--)
                            f += ort[i] * B[j, i];
                        f = f / h;
                        for (var i = m; i <= high; i++)
                            B[j, i] -= f * ort[i];
                    }

                    for (var i = 0; i <= high; i++)
                    {
                        var f = 0.0;
                        for (var j = high; j >= m; j--)
                            f += ort[j] * B[j, i];
                        f = f / h;
                        for (var j = m; j <= high; j++)
                            B[j, i] -= f * ort[j];
                    }

                    ort[m] = scale * ort[m];
                    B[mm1, m] = scale * g;
                }
            }

            // Accumulate transformations (Algol's ortran).
            for (var m = high - 1; m >= 1; m--)
            {
                var mm1 = m - 1;
                if (!B[mm1, m].IsNegligible())
                {
                    for (var i = m + 1; i <= high; i++)
                        ort[i] = B[mm1, i];
                    for (var j = m; j <= high; j++)
                    {
                        var g = ComplexNumber.Zero;
                        for (var i = m; i <= high; i++)
                            g += ort[i] * eigenVectors[j][i];
                        // Double division avoids possible underflow
                        g = (g / ort[m]) / B[mm1, m];
                        for (var i = m; i <= high; i++)
                            eigenVectors[j][i] += g * ort[i];
                    }
                }
            }

            #endregion

            #region now convert to real Schur form

            // Initialize
            var n = length - 1;
            var eps = Math.Pow(2.0, -52.0);
            var exshift = 0.0;
            double p, q, r = 0, s = 0, z = 0;
            double w, x, y;

            // Store roots isolated by balance and compute matrix norm
            var norm = 0.0;
            for (var i = 0; i < length; i++)
                for (var j = Math.Max(i - 1, 0); j < length; j++)
                    norm = norm + Math.Abs(B[j, i]);

            // Outer loop over eigenvalue index
            var iter = 0;
            while (n >= 0)
            {
                // Look for single small sub-diagonal element
                var l = n;
                while (l > 0)
                {
                    var lm1 = l - 1;
                    s = Math.Abs(B[lm1, lm1]) + Math.Abs(B[l, l]);
                    if (s.IsNegligible()) s = norm;
                    if (Math.Abs(B[lm1, l]) < eps * s)
                        break;
                    l--;
                }

                // Check for convergence
                // One root found
                if (l == n)
                {
                    B[n, n] += exshift;
                    eigenvaluesReal[n] = B[n, n];
                    eigenvaluesImag[n] = 0.0;
                    n--;
                    iter = 0;

                    // Two roots found
                }
                else if (l == n - 1)
                {
                    var nm1 = n - 1;
                    w = B[nm1, n] * B[n, nm1];
                    p = (B[nm1, nm1] - B[n, n]) / 2.0;
                    q = (p * p) + w;
                    z = Math.Sqrt(Math.Abs(q));

                    B[n, n] += exshift;
                    B[nm1, nm1] += exshift;
                    x = B[n, n];

                    // Real pair
                    if (q >= 0)
                    {
                        z = (p >= 0) ? p + z : p - z;
                        eigenvaluesReal[nm1] = x + z;

                        eigenvaluesReal[n] = eigenvaluesReal[nm1];
                        if (!z.IsNegligible()) eigenvaluesReal[n] = x - (w / z);
                        eigenvaluesImag[n - 1] = 0.0;
                        eigenvaluesImag[n] = 0.0;
                        x = B[nm1, n];
                        s = Math.Abs(x) + Math.Abs(z);
                        p = x / s;
                        q = z / s;
                        r = Math.Sqrt((p * p) + (q * q));
                        p = p / r;
                        q = q / r;

                        // Row modification
                        for (var j = n - 1; j < length; j++)
                        {
                            z = B[j, nm1];
                            B[j, nm1] = (q * z) + (p * B[j, n]);
                            B[j, n] = (q * B[j, n]) - (p * z);
                        }
                        // Column modification
                        for (var i = 0; i <= n; i++)
                        {
                            z = B[nm1, i];
                            B[nm1, i] = (q * z) + (p * B[n, i]);
                            B[n, i] = (q * B[n, i]) - (p * z);
                        }
                        // Accumulate transformations
                        for (var i = 0; i < length; i++)
                        {
                            var zA = eigenVectors[nm1][i];
                            eigenVectors[nm1][i] = (q * zA) + (p * eigenVectors[n][i]);
                            eigenVectors[n][i] = (q * eigenVectors[n][i]) - (p * zA);
                        }
                        // Complex pair
                    }
                    else
                    {
                        eigenvaluesReal[n - 1] = x + p;
                        eigenvaluesReal[n] = x + p;
                        eigenvaluesImag[n - 1] = z;
                        eigenvaluesImag[n] = -z;
                    }
                    n = n - 2;
                    iter = 0;
                    // No convergence yet
                }
                else
                {
                    var nm1 = n - 1;
                    // Form shift
                    x = B[n, n];
                    y = 0.0;
                    w = 0.0;
                    if (l < n)
                    {
                        y = B[nm1, nm1];
                        w = B[nm1, n] * B[n, nm1];
                    }

                    // Wilkinson's original ad hoc shift
                    if (iter == 10)
                    {
                        exshift += x;
                        for (var i = 0; i <= n; i++) B[i, i] -= x;
                        s = Math.Abs(B[nm1, n]) + Math.Abs(B[(n - 2), nm1]);
                        x = y = 0.75 * s;
                        w = (-0.4375) * s * s;
                    }

                    // MATLAB's new ad hoc shift
                    if (iter == 30)
                    {
                        s = (y - x) / 2.0;
                        s = (s * s) + w;
                        if (s > 0)
                        {
                            s = Math.Sqrt(s);
                            if (y < x) s = -s;
                            s = x - (w / (((y - x) / 2.0) + s));
                            for (var i = 0; i <= n; i++) B[i, i] -= s;
                            exshift += s;
                            x = y = w = 0.964;
                        }
                    }

                    iter++;
                    if (iter >= 30 * length)
                    {
                        throw new ArithmeticException("Eigen decomposition does not converge.");
                    }

                    // Look for two consecutive small sub-diagonal elements
                    var m = n - 2;
                    do
                    {
                        var mp1 = m + 1;
                        var mm1 = m - 1;
                        z = B[m, m];
                        r = x - z;
                        s = y - z;
                        p = (((r * s) - w) / B[m, mp1]) + B[mp1, m];
                        q = B[mp1, mp1] - z - r - s;
                        r = B[mp1, (m + 2)];
                        s = Math.Abs(p) + Math.Abs(q) + Math.Abs(r);
                        p = p / s;
                        q = q / s;
                        r = r / s;

                        if ((m == l) || (Math.Abs(B[mm1, m]) * (Math.Abs(q) + Math.Abs(r)) <
                                         eps * (Math.Abs(p) * (Math.Abs(B[mm1, mm1]) + Math.Abs(z) + Math.Abs(B[mp1, mp1])))))
                            break;
                    } while (--m >= l);

                    var mp2 = m + 2;
                    for (var i = mp2; i <= n; i++)
                    {
                        B[(i - 2), i] = 0.0;
                        if (i > mp2) B[(i - 3), i] = 0.0;
                    }

                    // Double QR step involving rows l:n and columns m:n
                    for (var k = m; k <= n - 1; k++)
                    {
                        var notlast = k != n - 1;
                        var km1 = k - 1;
                        var kp1 = k + 1;
                        var kp2 = k + 2;
                        if (k != m)
                        {
                            p = B[km1, k];
                            q = B[km1, kp1];
                            r = notlast ? B[km1, kp2] : 0.0;
                            x = Math.Abs(p) + Math.Abs(q) + Math.Abs(r);
                            if (x.IsNegligible()) continue;

                            p = p / x;
                            q = q / x;
                            r = r / x;
                        }

                        s = Math.Sqrt((p * p) + (q * q) + (r * r));
                        if (p < 0) s = -s;

                        if (!s.IsNegligible())
                        {
                            if (k != m) B[km1, k] = (-s) * x;
                            else if (l != m) B[km1, k] = -B[km1, k];
                            p = p + s;
                            x = p / s;
                            y = q / s;
                            z = r / s;
                            q = q / p;
                            r = r / p;

                            // Row modification
                            for (var j = k; j < length; j++)
                            {
                                p = B[j, k] + (q * B[j, kp1]);
                                if (notlast)
                                {
                                    p = p + (r * B[j, kp2]);
                                    B[j, kp2] -= (p * z);
                                }

                                B[j, k] -= (p * x);
                                B[j, kp1] -= (p * y);
                            }

                            // Column modification
                            for (var i = 0; i <= Math.Min(n, k + 3); i++)
                            {
                                p = (x * B[k, i]) + (y * B[kp1, i]);

                                if (notlast)
                                {
                                    p = p + (z * B[kp2, i]);
                                    B[kp2, i] -= (p * r);
                                }

                                B[k, i] -= p;
                                B[kp1, i] -= (p * q);
                            }

                            // Accumulate transformations
                            for (var i = 0; i < length; i++)
                            {
                                var pA = (x * eigenVectors[k][i]) + (y * eigenVectors[kp1][i]);

                                if (notlast)
                                {
                                    pA = pA + (z * eigenVectors[kp2][i]);
                                    eigenVectors[kp2][i] -= pA * r;
                                }

                                eigenVectors[k][i] -= pA;
                                eigenVectors[kp1][i] -= pA * q;
                            }
                        } // (s != 0)
                    } // k loop
                } // check convergence
            } // while (n >= low)

            // Backsubstitute to find vectors of upper triangular form
            if (norm.IsNegligible())
            {
                throw new ArithmeticException("Eigen decomposition failed due to norm = 0.");
            }

            for (n = length - 1; n >= 0; n--)
            {
                var nm1 = n - 1;
                p = eigenvaluesReal[n];
                q = eigenvaluesImag[n];
                // Real vector
                double t;
                if (q.IsNegligible())
                {
                    var l = n;
                    B[n, n] = 1.0;
                    for (var i = n - 1; i >= 0; i--)
                    {
                        var ip1 = i + 1;
                        w = B[i, i] - p;
                        r = 0.0;
                        for (var j = l; j <= n; j++)
                        {
                            r = r + (B[j, i] * B[n, j]);
                        }

                        if (eigenvaluesImag[i].IsNegativeNonNegligible())
                        {
                            z = w;
                            s = r;
                        }
                        else
                        {
                            l = i;
                            if (eigenvaluesImag[i].IsNegligible())
                            {
                                if (!w.IsNegligible())
                                {
                                    B[n, i] = (-r) / w;
                                }
                                else
                                {
                                    B[n, i] = (-r) / (eps * norm);
                                }

                                // Solve real equations
                            }
                            else
                            {
                                x = B[ip1, i];
                                y = B[i, ip1];
                                q = ((eigenvaluesReal[i] - p) * (eigenvaluesReal[i] - p)) +
                                    (eigenvaluesImag[i] * eigenvaluesImag[i]);
                                t = ((x * s) - (z * r)) / q;
                                B[n, i] = t;
                                if (Math.Abs(x) > Math.Abs(z))
                                {
                                    B[n, ip1] = (-r - (w * t)) / x;
                                }
                                else
                                {
                                    B[n, ip1] = (-s - (y * t)) / z;
                                }
                            }

                            // Overflow control
                            t = Math.Abs(B[n, i]);
                            if ((eps * t) * t > 1)
                            {
                                for (var j = i; j <= n; j++)
                                {
                                    B[n, j] /= t;
                                }
                            }
                        }
                    }

                    // Complex vector
                }
                else if (q < 0)
                {
                    var l = n - 1;

                    // Last vector component imaginary so matrix is triangular
                    if (Math.Abs(B[nm1, n]) > Math.Abs(B[n, nm1]))
                    {
                        B[nm1, nm1] = q / B[nm1, n];
                        B[n, nm1] = (-(B[n, n] - p)) / B[nm1, n];
                    }
                    else
                    {
                        var res = ComplexNumberDivide(0.0, -B[n, nm1], B[nm1, nm1] - p, q);
                        B[nm1, nm1] = res.Item1;
                        B[n, nm1] = res.Item2;
                    }

                    B[nm1, n] = 0.0;
                    B[n, n] = 1.0;
                    for (var i = n - 2; i >= 0; i--)
                    {
                        var ip1 = i + 1;
                        var ra = 0.0;
                        var sa = 0.0;
                        for (var j = l; j <= n; j++)
                        {
                            ra = ra + (B[j, i] * B[nm1, j]);
                            sa = sa + (B[j, i] * B[n, j]);
                        }

                        w = B[i, i] - p;

                        if (eigenvaluesImag[i] < 0.0)
                        {
                            z = w;
                            r = ra;
                            s = sa;
                        }
                        else
                        {
                            l = i;
                            if (eigenvaluesImag[i].IsNegligible())
                            {
                                var res = ComplexNumberDivide(-ra, -sa, w, q);
                                B[nm1, i] = res.Item1;
                                B[n, i] = res.Item2;
                            }
                            else
                            {
                                // Solve complex equations
                                x = B[ip1, i];
                                y = B[i, ip1];

                                var vr = ((eigenvaluesReal[i] - p) * (eigenvaluesReal[i] - p)) +
                                         (eigenvaluesImag[i] * eigenvaluesImag[i]) - (q * q);
                                var vi = (eigenvaluesReal[i] - p) * 2.0 * q;
                                if ((vr.IsNegligible()) && (vi.IsNegligible()))
                                    vr = eps * norm * (Math.Abs(w) + Math.Abs(q) + Math.Abs(x) + Math.Abs(y) + Math.Abs(z));
                                var res = ComplexNumberDivide((x * r) - (z * ra) + (q * sa), (x * s) - (z * sa) - (q * ra), vr,
                                    vi);
                                B[nm1, i] = res.Item1;
                                B[n, i] = res.Item2;
                                if (Math.Abs(x) > (Math.Abs(z) + Math.Abs(q)))
                                {
                                    B[nm1, ip1] = (-ra - (w * B[nm1, i]) + (q * B[n, i])) / x;
                                    B[n, ip1] = (-sa - (w * B[n, i]) - (q * B[nm1, i])) / x;
                                }
                                else
                                {
                                    res = ComplexNumberDivide(-r - (y * B[nm1, i]), -s - (y * B[n, i]), z, q);
                                    B[nm1, ip1] = res.Item1;
                                    B[n, ip1] = res.Item2;
                                }
                            }

                            // Overflow control
                            t = Math.Max(Math.Abs(B[nm1, i]), Math.Abs(B[n, i]));
                            if ((eps * t) * t > 1)
                            {
                                for (var j = i; j <= n; j++)
                                {
                                    B[nm1, j] /= t;
                                    B[n, j] /= t;
                                }
                            }
                        }
                    }
                }
            }

            // Back transformation to get eigenvectors of original matrix
            for (var j = length - 1; j >= 0; j--)
            {
                for (var i = 0; i < length; i++)
                {
                    var zA = ComplexNumber.Zero;
                    for (var k = 0; k <= j; k++)
                        zA += (eigenVectors[k][i] * B[j, k]);
                    eigenVectors[j][i] = zA;
                }
            }

            #endregion
            var result = new ComplexNumber[length];
            for (int i = 0; i < length; i++)
                result[i] = new ComplexNumber(eigenvaluesReal[i], eigenvaluesImag[i]);
            return result;
        }

        internal static ComplexNumber[] GetEigenValuesAndVectors4(double M11, double M12, double M13, double M14,
            double M21, double M22, double M23, double M24,
            double M31, double M32, double M33, double M34,
            double M41, double M42, double M43, double M44, out ComplexNumber[][] eigenVectors)
        {

            var eigValues = GetEigenValues4(M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
            eigenVectors = GetEigenVectors4(M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44, eigValues);
            return eigValues;
        }

        internal static ComplexNumber[] GetEigenValues4(double M11, double M12, double M13, double M14,
            double M21, double M22, double M23, double M24,
            double M31, double M32, double M33, double M34,
            double M41, double M42, double M43, double M44)
        {
            double a = M11, b = M12, c = M13, d = M14; // | a b c d |  
            double e = M21, f = M22, g = M23, h = M24; // | e f g h |
            double i = M31, j = M32, k = M33, l = M34; // | i j k l | 
            double m = M41, n = M42, o = M43, p = M44; // | m n o p |

            double fourthOrderCoeff = 1;
            double cubedCoeff = -a - f - k - p;
            var af = a * f;
            var ak = a * k;
            var ap = a * p;
            var be = b * e;
            var ci = c * i;
            var dm = d * m;
            var fk = f * k;
            var fp = f * p;
            var gj = g * j;
            var hn = h * n;
            var kp = k * p;
            var lo = l * o;
            double squaredCoeff = af + ak + ap - be - ci - dm + fk + fp - gj - hn + kp - lo;
            var afk = af * k;
            var afp = af * p;
            var agj = a * gj;
            var ahn = a * hn;
            var akp = a * kp;
            var alo = a * lo;
            var bek = be * k;
            var bep = be * p;
            var bgi = b * g * i;
            var bhm = b * h * m;
            var cej = c * e * j;
            var cfi = ci * f;
            var cip = ci * p;
            var clm = c * l * m;
            var den = d * e * n;
            var dfm = dm * f;
            var dio = d * i * o;
            var dkm = dm * k;
            var fkp = f * kp;
            var flo = f * lo;
            var gjp = gj * p;
            var gln = g * l * n;
            var hjo = h * j * o;
            var hkn = hn * k;
            double linearCoeff = -afk - afp + agj + ahn - akp + alo + bek
                + bep - bgi - bhm - cej + cfi + cip - clm - den + dfm
                - dio + dkm - fkp + flo + gjp - gln - hjo + hkn;
            var afkp = afk * p;
            var aflo = af * lo;
            var agjp = agj * p;
            var agln = a * gln;
            var ahjo = a * hjo;
            var ahkn = ahn * k;
            var bekp = bek * p;
            var belo = be * lo;
            var bgip = bgi * p;
            var bglm = b * g * l * m;
            var bhio = b * h * i * o;
            var bhkm = bhm * k;
            var cejp = cej * p;
            var celn = c * e * l * n;
            var cfip = cfi * p;
            var cflm = f * clm;
            var chin = ci * hn;
            var chjm = c * h * j * m;
            var dejo = d * e * j * o;
            var dekn = den * k;
            var dfio = dio * f;
            var dfkm = dfm * k;
            var dgin = d * g * i * n;
            var dgjm = dm * gj;
            double offset = afkp - aflo - agjp + agln + ahjo - ahkn - bekp + belo + bgip
                - bglm - bhio + bhkm + cejp - celn - cfip + cflm + chin - chjm - dejo
                + dekn + dfio - dfkm - dgin + dgjm;

            var roots = PolynomialSolve.Quartic(fourthOrderCoeff, cubedCoeff, squaredCoeff, linearCoeff, offset).ToArray();
            return roots;
        }

        internal static ComplexNumber[][] GetEigenVectors4(double M11, double M12, double M13, double M14,
            double M21, double M22, double M23, double M24,
            double M31, double M32, double M33, double M34,
            double M41, double M42, double M43, double M44, IEnumerable<ComplexNumber> eigenValues)
        {
            var eigenVectors = new ComplexNumber[4][];
            var i = 0;
            foreach (var eV in eigenValues)
                eigenVectors[i++] = GetEigenVector4(M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44,
                     eV);
            return eigenVectors;
        }

        internal static ComplexNumber[] GetEigenVector4(double M11, double M12, double M13, double M14, double M21, double M22, double M23,
            double M24, double M31, double M32, double M33, double M34, double M41, double M42, double M43, double M44,
            ComplexNumber lambda)
        {
            var y11 = M11 - lambda;
            var y22 = M22 - lambda;
            var y33 = M33 - lambda;
            var y44 = M44 - lambda;

            if (Solve3x3ComplexMatrix(y11, new ComplexNumber(M12), new ComplexNumber(M13), new ComplexNumber(M21),
                        y22, new ComplexNumber(M23), new ComplexNumber(M31), new ComplexNumber(M32), y33,
                        new ComplexNumber(-M14), new ComplexNumber(-M24), new ComplexNumber(-M34),
                        out var ans41, out var ans42, out var ans43))
            {
                double normFactor1 = 1 / Math.Sqrt(1 + ans41.LengthSquared() + ans42.LengthSquared() + ans43.LengthSquared());
                return [ans41 * normFactor1, ans42 * normFactor1, ans43 * normFactor1, new ComplexNumber(normFactor1)];
            }
            else if (Solve3x3ComplexMatrix(y22, new ComplexNumber(M23), new ComplexNumber(M24), new ComplexNumber(M32),
                        y33, new ComplexNumber(M34), new ComplexNumber(M42), new ComplexNumber(M43), y44,
                        new ComplexNumber(-M21), new ComplexNumber(-M31), new ComplexNumber(-M41), out var ans11, out var ans12, out var ans13))
            {
                double normFactor1 = 1 / Math.Sqrt(1 + ans11.LengthSquared() + ans12.LengthSquared() + ans13.LengthSquared());
                return [new ComplexNumber(normFactor1), ans11 * normFactor1, ans12 * normFactor1, ans13 * normFactor1];
            }
            else if (Solve3x3ComplexMatrix(y11, new ComplexNumber(M13), new ComplexNumber(M14), new ComplexNumber(M31),
                        y33, new ComplexNumber(M34), new ComplexNumber(M41), new ComplexNumber(M43), y44,
                        new ComplexNumber(-M12), new ComplexNumber(-M32), new ComplexNumber(-M42),
                        out var ans21, out var ans22, out var ans23))
            {
                double normFactor1 = 1 / Math.Sqrt(1 + ans21.LengthSquared() + ans22.LengthSquared() + ans23.LengthSquared());
                return [ans21 * normFactor1, new ComplexNumber(normFactor1), ans22 * normFactor1, ans23 * normFactor1];
            }
            else if (Solve3x3ComplexMatrix(y11, new ComplexNumber(M12), new ComplexNumber(M14), new ComplexNumber(M21),
                        y22, new ComplexNumber(M24), new ComplexNumber(M41), new ComplexNumber(M42), y44,
                        new ComplexNumber(-M13), new ComplexNumber(-M23), new ComplexNumber(-M43),
                        out var ans31, out var ans32, out var ans33))
            {
                double normFactor1 = 1 / Math.Sqrt(1 + ans31.LengthSquared() + ans32.LengthSquared() + ans33.LengthSquared());
                return [ans31 * normFactor1, ans32 * normFactor1, new ComplexNumber(normFactor1), ans33 * normFactor1];
            }
            return null;
        }

        internal static ComplexNumber[] GetEigenValuesAndVectors3(double x11, double x12, double x13,
            double x21, double x22, double x23, double x31, double x32, double x33, out ComplexNumber[][] eigenVectors)
        {
            var lambdas = GetEigenValues3(x11, x12, x13, x21, x22, x23, x31, x32, x33);
            eigenVectors = GetEigenVectors3(x11, x12, x13, x21, x22, x23, x31, x32, x33, lambdas);
            return lambdas;
        }
        internal static ComplexNumber[] GetEigenValues3(double x11, double x12, double x13,
            double x21, double x22, double x23, double x31, double x32, double x33)
        {
            var d = x11 * x22 * x33 + x12 * x23 * x31 + x13 * x21 * x32 - x11 * x23 * x32 - x13 * x22 * x31 - x12 * x21 * x33;
            var c = x23 * x32 + x12 * x21 + x13 * x31 - x11 * x22 - x33 * (x11 + x22);
            var b = x11 + x22 + x33;
            var a = -1.0;
            return PolynomialSolve.Cubic(a, b, c, d).ToArray();
        }
        internal static ComplexNumber[][] GetEigenVectors3(double x11, double x12, double x13,
            double x21, double x22, double x23, double x31, double x32, double x33, IEnumerable<ComplexNumber> eigenValues)
        {
            var eigenVectors = new ComplexNumber[3][];
            var i = 0;
            foreach (var eValue in eigenValues)
                eigenVectors[i++] = GetEigenVector3(x11, x12, x13, x21, x22, x23, x31, x32, x33, eValue);
            return eigenVectors;
        }

        public static ComplexNumber[] GetEigenVector3(double x11, double x12, double x13, double x21, double x22,
            double x23, double x31, double x32, double x33, ComplexNumber eigenValue)
        {
            var y11 = x11 - eigenValue;
            var y22 = x22 - eigenValue;
            var y33 = x33 - eigenValue;
            if (Solve2x2ComplexMatrix(y11, new ComplexNumber(x12), new ComplexNumber(x21), y22,
                        new ComplexNumber(-x13), new ComplexNumber(-x23), out var ans31, out var ans32))
            {
                var normFactor3 = 1 / Math.Sqrt(1 + ans31.LengthSquared() + ans32.LengthSquared());
                return [ans31 * normFactor3, ans32 * normFactor3, new ComplexNumber(normFactor3)];
            }
            else if (Solve2x2ComplexMatrix(y22, new ComplexNumber(x23), new ComplexNumber(x32),
                        y33, new ComplexNumber(-x21), new ComplexNumber(-x31), out var ans11, out var ans12))
            {
                var normFactor1 = 1 / Math.Sqrt(1 + ans11.LengthSquared() + ans12.LengthSquared());
                return [new ComplexNumber(normFactor1), ans11 * normFactor1, ans12 * normFactor1];
            }
            else if (Solve2x2ComplexMatrix(y11, new ComplexNumber(x13), new ComplexNumber(x31), y33,
                        new ComplexNumber(-x12), new ComplexNumber(-x32), out var ans21, out var ans22))
            {
                var normFactor2 = 1 / Math.Sqrt(1 + ans21.LengthSquared() + ans22.LengthSquared());
                return [ans21 * normFactor2, new ComplexNumber(normFactor2), ans22 * normFactor2];
            }
            return null;
        }

        internal static ComplexNumber[] GetEigenValuesAndVectors2(double x11, double x12,
            double x21, double x22, out ComplexNumber[][] eigenVectors)
        {
            var (lambda1, lambda2) = GetEigenValues2(x11, x12, x21, x22);
            eigenVectors = GetEigenVectors2(x11, x12, x21, x22, lambda1, lambda2);
            return [lambda1, lambda2];
        }
        internal static (ComplexNumber, ComplexNumber) GetEigenValues2(double x11, double x12,
            double x21, double x22)
        {
            var c = x11 * x22 - x12 * x21;
            var b = -x11 - x22;
            return PolynomialSolve.Quadratic(1, b, c);
        }
        internal static ComplexNumber[][] GetEigenVectors2(double x11, double x12,
            double x21, double x22, ComplexNumber lambda1, ComplexNumber lambda2)
        {
            return new ComplexNumber[][]
                 {
                     GetEigenVector2(x11, x12, x21, x22, lambda1),
                     GetEigenVector2(x11, x12, x21, x22, lambda2)
                 };
        }

        internal static ComplexNumber[] GetEigenVector2(double x11, double x12, double x21, double x22, ComplexNumber lambda)
        {
            var y11 = x11 - lambda;
            var y22 = x22 - lambda;
            if (y11.LengthSquared() > y22.LengthSquared())
            {
                var f = -x21 / y22;
                var normFactor = 1.0 / Math.Sqrt(1 + f.Real * f.Real + f.Imaginary * f.Imaginary);
                return [new ComplexNumber(normFactor), -x21 * normFactor / y22];
            }
            else
            {
                var f = -x12 / y11;
                var normFactor = 1.0 / Math.Sqrt(1 + f.Real * f.Real + f.Imaginary * f.Imaginary);
                return [-x12 * normFactor / y11, new ComplexNumber(normFactor)];
            }
        }

        /// <summary>
        /// Does complex number division for the main function without needing to cast to the ComplexNumber structure.
        /// </summary>
        /// <param name="xreal">The xreal.</param>
        /// <param name="ximag">The ximag.</param>
        /// <param name="yreal">The yreal.</param>
        /// <param name="yimag">The yimag.</param>
        /// <returns>System.Double[].</returns>
        private static (double, double) ComplexNumberDivide(double xreal, double ximag, double yreal, double yimag)
        {
            var yReciprocal = 1.0 / (yreal * yreal + yimag * yimag);
            return ((xreal * yreal + ximag * yimag) * yReciprocal, (ximag * yreal - xreal * yimag) * yReciprocal);
        }
    }
}