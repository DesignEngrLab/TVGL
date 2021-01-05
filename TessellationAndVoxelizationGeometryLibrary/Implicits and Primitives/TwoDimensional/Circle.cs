using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public readonly struct Circle : I2DCurve
    {
        /// <summary>
        ///     Center Point of circle
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        ///     Radius of circle
        /// </summary>
        public readonly double Radius;

        /// <summary>
        ///     Radius of circle squared
        /// </summary>
        public readonly double RadiusSquared;

        /// <summary>
        ///     Area of circle
        /// </summary>
        public readonly double Area;

        /// <summary>
        ///     Circumference of circle
        /// </summary>
        public readonly double Circumference;

        /// <summary>Creates a circle, given the center point and the radius Squared</summary>
        /// <param name="center">The center.</param>
        /// <param name="radiusSquared">The radius squared.</param>
        public Circle(Vector2 center, double radiusSquared)
        {
            Center = center;
            RadiusSquared = radiusSquared;
            Radius = Math.Sqrt(radiusSquared);
            Area = Math.PI * radiusSquared;
            Circumference = Constants.TwoPi * Radius;
        }

        public double SquaredErrorOfNewPoint(Vector2 point)
        {
            var diff = point - Center;
            var error = Math.Sqrt(diff.Dot(diff)) - Radius;
            return error * error ;
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints(IEnumerable<Vector2> points, out Circle circle, out double error)
        {
            // Updates the circle using Landau's method ( https://doi.org/10.1016/0734-189X(89)90088-1 ), which
            // seems like it would be same as the Minimum Least Squares approach, but this is a million times
            // more accurate. Is it just a result of dividing large numbers?
            // modified from Matlab: http://freesourcecode.net/matlabprojects/62157/circle-fit-using-landau-method-in-matlab
            var n = 0;
            double Sxx = 0.0, Syy = 0.0, Sxy = 0.0, Sx = 0.0, Sy = 0.0;
            double Sxxx = 0.0, Sxxy = 0.0, Sxyy = 0.0, Syyy = 0.0;
            foreach (var p in points)
            {
                var x = p.X;
                var y = p.Y;
                Sx += x;
                Sy += y;
                Sxx += x * x;
                Syy += y * y;
                Sxy += x * y;
                Sxxx += x * x * x;
                Sxxy += x * x * y;
                Sxyy += x * y * y;
                Syyy += y * y * y;
                n++;
            }
            if (n < 3)
            {
                circle = new Circle();
                error = double.PositiveInfinity;
                return false;
            }
            var a1 = 2 * ((Sx * Sx) - (n * Sxx));
            var b1 = 2 * ((Sx * Sy) - (n * Sxy));
            var a2 = b1;
            var b2 = 2 * ((Sy * Sy) - (n * Syy));
            var c1 = (Sxx * Sx) - (n * Sxxx) + (Sx * Syy) - (n * Sxyy);
            var c2 = (Sxx * Sy) - (n * Syyy) + (Sy * Syy) - (n * Sxxy);
            var cross = (a1 * b2) - (a2 * b1);
            if (cross.IsNegligible())
            {
                circle = new Circle();
                error = double.PositiveInfinity;
                return false;
            }
            var xc = ((c1 * b2) - (c2 * b1)) / cross; // returns the center along x
            var yc = ((a1 * c2) - (a2 * c1)) / cross; // returns the center along y
            var radiusSquared = (Sxx - 2 * Sx * xc + n * xc * xc + Syy - 2 * Sy * yc + n * yc * yc) / n; // Radius squared of circle
            var center = new Vector2(xc, yc);
            circle= new Circle(center, radiusSquared);
            error = 0.0;
            foreach (var p in points)
                error += circle.SquaredErrorOfNewPoint(p);
            error /= n;
            return true;
        }

    }
}
