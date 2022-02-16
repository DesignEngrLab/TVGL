using MIConvexHull;
using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Axis-aligned rectangle
    /// </summary>
    public readonly struct AxisAlignedRectangle
    {
        public readonly double XMin;
        public readonly double XMax;
        public readonly double YMin;
        public readonly double YMax;

        public AxisAlignedRectangle(MonotoneBox box) : this()
        {
            XMin = Math.Min(box.Vertex1.X, box.Vertex2.X);
            XMax = Math.Max(box.Vertex1.X, box.Vertex2.X);
            YMin = Math.Min(box.Vertex1.Y, box.Vertex2.Y);
            YMax = Math.Max(box.Vertex1.Y, box.Vertex2.Y);
        }

        public AxisAlignedRectangle(IVertex2D vertex1, IVertex2D vertex2) : this()
        {
            XMin = Math.Min(vertex1.X, vertex2.X);
            XMax = Math.Max(vertex1.X, vertex2.X);
            YMin = Math.Min(vertex1.Y, vertex2.Y);
            YMax = Math.Max(vertex1.Y, vertex2.Y);
        }

        public AxisAlignedRectangle(double xMin, double yMin, double xMax, double yMax) : this()
        {
            this.XMin = xMin;
            this.YMin = yMin;
            this.XMax = xMax;
            this.YMax = yMax;
        }

        /// <summary>
        /// Determines if the two rectangles overlap with one another.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Overlaps(AxisAlignedRectangle other)
        {
            return other.XMax >= XMin && other.XMin <= XMax && other.YMax >= YMin && other.YMin <= YMax;
        }
        /// <summary>
        /// Unions the two rectangles into one larger one.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>AxisAlignedRectangle.</returns>
        public AxisAlignedRectangle Union(AxisAlignedRectangle other)
        {
            return new AxisAlignedRectangle(Math.Min(XMin, other.XMin), Math.Min(YMin, other.YMin),
                Math.Max(XMax, other.XMax), Math.Max(YMax, other.YMax));
        }

        /// <summary>
        /// Returns the area of the rectangle (always positive).
        /// </summary>
        /// <returns>System.Double.</returns>
        public double Area()
        {
            return (XMax - XMin) * (YMax - YMin);
        }

        /// <summary>
        /// Returns the four corners of the rectangle starting from the lower left (Xmin, YMin)
        /// and proceeding counterclockwise.
        /// </summary>
        /// <returns>Vector2[].</returns>
        public Vector2[] Corners()
        {
            return new Vector2[] {
                new Vector2(XMin, YMin),
                new Vector2(XMax, YMin),
                new Vector2(XMax, YMax),
                new Vector2(XMin, YMax)
            };
        }
    }
}