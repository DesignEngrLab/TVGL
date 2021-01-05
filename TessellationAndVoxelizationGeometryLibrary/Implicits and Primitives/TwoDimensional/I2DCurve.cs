using System.Collections.Generic;
using System;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Interface I2DCurve
    /// </summary>
    public interface I2DCurve
    {
        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double SquaredErrorOfNewPoint(Vector2 point);

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints(IEnumerable<Vector2> points, out I2DCurve curve, out double error)
        {
            throw new NotImplementedException("please implement static method in curve: static I2DCurve " +
                "CreateFromPoints(IEnumerable<Vector2> points)");
        }
    }
}
