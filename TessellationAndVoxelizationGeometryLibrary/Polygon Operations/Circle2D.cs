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
        public Circle2D(Vector2 center, double radiusSquared)
        {
            Center = center;
            RadiusSquared = radiusSquared;
            Radius = Math.Sqrt(radiusSquared);
            Area = Math.PI * radiusSquared;
            Circumference = Constants.TwoPi * Radius;
        }
    }
}
