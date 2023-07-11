// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Circle.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using MIConvexHull;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Public circle structure, given a center point and radius
    /// </summary>
    public readonly struct Circle : ICurve
    {
        /// <summary>
        /// Center Point of circle
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// Radius of circle
        /// </summary>
        [JsonIgnore]
        public readonly double Radius;

        /// <summary>
        /// Radius of circle squared
        /// </summary>
        public readonly double RadiusSquared;

        /// <summary>
        /// Area of circle
        /// </summary>
        [JsonIgnore]
        public readonly double Area;

        /// <summary>
        /// Circumference of circle
        /// </summary>
        [JsonIgnore]
        public readonly double Circumference;

        /// <summary>
        /// Creates a circle, given the center point and the radius Squared
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radiusSquared">The radius squared.</param>
        [JsonConstructor]
        public Circle(Vector2 center, double radiusSquared)
        {
            Center = center;
            RadiusSquared = radiusSquared;
            Radius = Math.Sqrt(radiusSquared);
            Area = Math.PI * radiusSquared;
            Circumference = Constants.TwoPi * Radius;
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
            var diff =new Vector2(point.X - Center.X, point.Y - Center.Y);
            var error = Math.Sqrt(diff.Dot(diff)) - Radius;
            return error * error;
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IPoint2D
        {
            // Updates the circle using Landau's method ( https://doi.org/10.1016/0734-189X(89)90088-1 ), which
            // seems like it would be same as the Minimum Least Squares approach, but this is a million times
            // more accurate. Is it just a result of dividing large numbers?
            // modified from Matlab: http://freesourcecode.net/matlabprojects/62157/circle-fit-using-landau-method-in-matlab
            var n = 0;
            double Sxx = 0.0, Syy = 0.0, Sxy = 0.0, Sx = 0.0, Sy = 0.0;
            double Sxxx = 0.0, Sxxy = 0.0, Sxyy = 0.0, Syyy = 0.0;
            double xMin = double.PositiveInfinity, yMin = double.PositiveInfinity;
            double xMax = double.NegativeInfinity, yMax = double.NegativeInfinity;
            foreach (var p in points)
            {
                var x = p.X;
                var y = p.Y;
                if (xMin > x) xMin = x;
                if (yMin > y) yMin = y;
                if (xMax < x) xMax = x;
                if (yMax < y) yMax = y;
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
                curve = new Circle();
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
                curve = new Circle();
                error = double.PositiveInfinity;
                return false;
            }
            var xc = ((c1 * b2) - (c2 * b1)) / cross; // returns the center along x
            var yc = ((a1 * c2) - (a2 * c1)) / cross; // returns the center along y
            var radiusSquared = (Sxx - 2 * Sx * xc + n * xc * xc + Syy - 2 * Sy * yc + n * yc * yc) / n; // Radius squared of circle
            var center = new Vector2(xc, yc);
            #region find angle range - if it's too low then probably should be just a line
            var angle = double.PositiveInfinity;
            if (xc <= xMin && yc <= yMin) //if the center is below and to the left of bounding box
                angle = Math.Atan2(yMax - yc, xMin - xc) - Math.Atan2(yMin - yc, xMax - xc);
            else if (xc <= xMin && yc >= yMax) //if the center is above and to the left of bounding box
                angle = Math.Atan2(yMax - yc, xMax - xc) - Math.Atan2(yMin - yc, xMin - xc);
            else if (xc >= xMax && yc >= yMax) //if the center is above and to the right of bounding box
                angle = Math.Atan2(yMin - yc, xMax - xc) - Math.Atan2(yMax - yc, xMin - xc);
            else if (xc >= xMax && yc <= yMin) //if the center is below and to the right of bounding box
                angle = Math.Atan2(yMin - yc, xMin - xc) - Math.Atan2(yMax - yc, xMax - xc);
            if (angle < 0.02) // which is about 1 degree
            {
                curve = new Circle();
                error = double.PositiveInfinity;
                return false;
            }
            #endregion
            curve = new Circle(center, radiusSquared);
            error = 0.0;
            foreach (var p in points)
                error += curve.SquaredErrorOfNewPoint(p);
            error /= n;
            return true;
        }


        /// <summary>
        /// Creates the path.
        /// </summary>
        /// <param name="numPoints">The number points.</param>
        /// <returns>List&lt;Vector2&gt;.</returns>
        public List<Vector2> CreatePath(int numPoints)
        {
            if (numPoints <= 1)
                return new List<Vector2>();
            var increment = Math.PI * 2 / numPoints;
            var path = new List<Vector2>();
            for (var theta = 0.0; theta < Math.PI * 2; theta += increment)
            {
                path.Add(new Vector2(Radius * Math.Cos(theta) + Center.X, Radius * Math.Sin(theta) + Center.Y));
            }
            return new List<Vector2>(path);
        }

    }
}
