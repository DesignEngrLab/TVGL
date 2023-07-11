// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="StraightLine2D.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using TVGL.ConvexHull;
using System.Collections.Generic;


namespace TVGL
{
    /// <summary>
    /// Struct StraightLine2D
    /// Implements the <see cref="TVGL.ICurve" />
    /// </summary>
    /// <seealso cref="TVGL.ICurve" />
    public readonly struct StraightLine2D : ICurve
    {

        /// <summary>
        /// The anchor
        /// </summary>
        public readonly Vector2 Anchor;

        /// <summary>
        /// The direction
        /// </summary>
        public readonly Vector2 Direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="StraightLine2D"/> struct.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="direction">The direction.</param>
        public StraightLine2D(Vector2 anchor, Vector2 direction)
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
        public double SquaredErrorOfNewPoint<T>(T point) where T : IPoint2D
        {
            var fromAnchor =new Vector2(point.X - Anchor.X, point.Y - Anchor.Y);
            var cross = fromAnchor.Cross(Direction);
            return cross * cross;
        }

        /// <summary>
        /// Creates from points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IPoint2D
        {
            double xCoeff;
            double yCoeff;
            bool ConstantIsZero;
            // based on word file, we will solve the two simultaneous equations with substitution
            double g = 0.0, h = 0.0, k = 0.0, m = 0.0, n = 0.0;
            var numPoints = 0;
            foreach (var p in points)
            {
                g += p.X * p.X;
                h += p.X * p.Y;
                k += p.Y * p.Y;
                m += p.X;
                n += p.Y;
                numPoints++;
            }
            if (numPoints < 2)
            {
                curve = new StraightLine2D();
                error = double.PositiveInfinity;
                return false;
            }
            var denom = k * g - h * h;
            if (denom.IsNegligible())
            {
                ConstantIsZero = true;
                if (g.IsNegligible())
                {
                    yCoeff = 0;
                    xCoeff = 1;
                }
                else
                {
                    yCoeff = 1;
                    xCoeff = h / g;
                }
            }
            else
            {
                ConstantIsZero = false;
                yCoeff = (n * g - m * h) / denom;
                xCoeff = (m - yCoeff * h) / g;
            }
            if (yCoeff == 0) //line is vertical
                curve = new StraightLine2D(new Vector2(1 / xCoeff, 0), Vector2.UnitY);
            else
            {
                var anchor = ConstantIsZero ? Vector2.Zero : new Vector2(0, 1 / yCoeff);
                curve = new StraightLine2D(anchor, new Vector2(yCoeff, -xCoeff).Normalize());
            }
            error = 0.0;
            foreach (var p in points)
                error += curve.SquaredErrorOfNewPoint(p);
            error /= numPoints;
            return true;
        }
    }
}
