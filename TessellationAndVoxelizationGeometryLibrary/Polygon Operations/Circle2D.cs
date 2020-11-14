using System;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public readonly struct Circle2D
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
        ///     Area of circle
        /// </summary>
        public readonly double Area;

        /// <summary>
        ///     Circumference of circle
        /// </summary>
        public readonly double Circumference;

        /// <summary>
        ///     Creates a circle, given a radius. Center point is optional
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="center">The center.</param>
        public Circle2D(double radius, Vector2 center)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI * radius * radius;
            Circumference = Constants.TwoPi * radius;
        }
    }
}
