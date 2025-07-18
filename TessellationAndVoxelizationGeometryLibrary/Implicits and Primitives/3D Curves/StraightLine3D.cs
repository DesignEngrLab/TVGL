﻿// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="StraightLine3D.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Struct StraightLine3D
    /// Implements the <see cref="TVGL.ICurve" />
    /// </summary>
    /// <seealso cref="TVGL.ICurve" />
    public readonly struct StraightLine3D : ICurve
    {

        /// <summary>
        /// The anchor
        /// </summary>
        public readonly Vector3 Anchor;

        /// <summary>
        /// The direction
        /// </summary>
        public readonly Vector3 Direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="StraightLine3D"/> struct.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="direction">The direction.</param>
        public StraightLine3D(Vector3 anchor, Vector3 direction)
        {
            Anchor = anchor;
            Direction = direction;
        }

        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IVector
        {
            if (point is IVector3D vector3D)
            {
                var cross = (new Vector3(vector3D.X - Anchor.X, vector3D.Y - Anchor.Y, vector3D.Z - Anchor.Z)).Cross(Direction);
                return cross.Dot(cross);
            }
            else return double.PositiveInfinity;
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        // this is based on this: https://stackoverflow.com/questions/24747643/3d-linear-regression/67303867#67303867

        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IVector2D
        {
            double x, y, z;
            double xSqd, ySqd, zSqd;
            double xy, xz, yz;
            x = y = z = xSqd = ySqd = zSqd = xy = xz = yz = 0.0;
            var n = 0;
            foreach (var point in points)
            {
                if (!(point is IVector3D point3D))
                {
                    curve = null;
                    error = 0;
                    return false;
                }
                var px = point.X;
                var py = point.Y;
                var pz = ((IVector3D)point).Z;
                x += px;
                y += py;
                z += pz;
                xSqd += px * px;
                ySqd += py * py;
                zSqd += pz * pz;
                xy += px * py;
                xz += px * pz;
                yz += py * pz;
                n++;
            }
            x /= n;
            y /= n;
            z /= n;
            xSqd /= n;
            ySqd /= n;
            zSqd /= n;
            xy /= n;
            xz /= n;
            yz /= n;
            if (n == 2)
            {
                var p1 = points.First();
                var p2 = points.Last();
                var anchor = new Vector3(p1.X, p1.Y, ((IVector3D)p1).Z);
                var dir = new Vector3(p2.X, p2.Y, ((IVector3D)p2).Z) - anchor;
                curve = new StraightLine3D(anchor, (dir - anchor).Normalize());
                error = 0;
                return !dir.IsNegligible();
            }

            var matrix = new double[,] {
                { xSqd - x * x, xy - x * y,   xz - x * z},
                { xy - x * y,   ySqd - y * y, yz - y * z }   ,
                { xz - x * z,   yz - y * z,   zSqd - z * z }
            };
            var direction = Vector3.Null;
            if (matrix.IsSingular())
            {
                if (!matrix[0, 0].IsNegligible()) direction = Vector3.UnitX;
                else if (!matrix[1, 1].IsNegligible()) direction = Vector3.UnitY;
                direction = Vector3.UnitZ;
            }
            else
            {
                var eigens = StarMath.GetEigenValuesAndVectors(matrix, out var eigenVectors);
                // either all 3 are real or 2 are complex conjugates
                var validForUse = new bool[3];
                for (int i = 0; i < 3; i++)
                {
                    if (eigenVectors[i] == null) validForUse[i] = false;
                    else validForUse[i] = eigens[i].IsRealNumber;
                }
                var containsComplex = !eigens[0].IsRealNumber || !eigens[1].IsRealNumber;
                int indexToUse = -1;
                if (validForUse[0] &&
                    (!validForUse[1] || Math.Abs(eigens[0].Real) >= Math.Abs(eigens[1].Real)) &&
                   (!validForUse[2] || Math.Abs(eigens[0].Real) >= Math.Abs(eigens[2].Real)))
                    indexToUse = 0;
                else if (validForUse[1] &&
                      (!validForUse[0] || Math.Abs(eigens[1].Real) >= Math.Abs(eigens[0].Real)) &&
                     (!validForUse[2] || Math.Abs(eigens[1].Real) >= Math.Abs(eigens[2].Real)))
                    indexToUse = 1;
                else if (validForUse[2] &&
                      (!validForUse[1] || Math.Abs(eigens[2].Real) >= Math.Abs(eigens[1].Real)) &&
                     (!validForUse[0] || Math.Abs(eigens[2].Real) >= Math.Abs(eigens[0].Real)))
                    indexToUse = 2;
                else
                {
                    curve = null;
                    error = 0;
                    return false;
                }
                direction = new Vector3(eigenVectors[indexToUse][0].Real,
                    eigenVectors[indexToUse][1].Real,
                    eigenVectors[indexToUse][2].Real);
            }
            curve = new StraightLine3D(new Vector3(x, y, z), direction.Normalize());
            //var result = new StraightLine3D(new Vector3(x, y, z), direction.Normalize());
            error = 0.0;
            foreach (var point in points)
                error += curve.SquaredErrorOfNewPoint(point);
            error /= n;
            return true;
        }
    }
}
