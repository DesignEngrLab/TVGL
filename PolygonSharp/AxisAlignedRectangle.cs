using MIConvexHull;
using System;
using System.Numerics;

namespace PolygonSharp
{
    /// <summary>
    ///     Axis-aligned rectangle
    /// </summary>
    public readonly struct AxisAlignedRectangle
    {
        public readonly float XMin;
        public readonly float XMax;
        public readonly float YMin;
        public readonly float YMax;

        public AxisAlignedRectangle(MonotoneBox box) : this()
        {
            XMin = MathF.Min(box.Vertex1.X, box.Vertex2.X);
            XMax = MathF.Max(box.Vertex1.X, box.Vertex2.X);
            YMin = MathF.Min(box.Vertex1.Y, box.Vertex2.Y);
            YMax = MathF.Max(box.Vertex1.Y, box.Vertex2.Y);
        }

        public AxisAlignedRectangle(Vertex2D vertex1, Vertex2D vertex2) : this()
        {
            XMin = MathF.Min(vertex1.X, vertex2.X);
            XMax = MathF.Max(vertex1.X, vertex2.X);
            YMin = MathF.Min(vertex1.Y, vertex2.Y);
            YMax = MathF.Max(vertex1.Y, vertex2.Y);
        }

        public AxisAlignedRectangle(float xMin, float yMin, float xMax, float yMax) : this()
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
            return new AxisAlignedRectangle(MathF.Min(XMin, other.XMin), MathF.Min(YMin, other.YMin),
                MathF.Max(XMax, other.XMax), MathF.Max(YMax, other.YMax));
        }


        /// <summary>
        /// Gets the width of the rectangle (always positive).
        /// </summary>
        /// <value>The width.</value>
        public float Width => XMax - XMin;

        /// <summary>
        /// Gets the height of the rectangle (always positive).
        /// </summary>
        /// <value>The height.</value>
        public float Height => YMax - YMin;
        /// <summary>
        /// Gets the  area of the rectangle (always positive).
        /// </summary>
        /// <value>The area.</value>
        public float Area => Width * Height;

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

        /// <summary>
        /// Makes a new rectange that is offset from the current one. The
        /// value can be positive or negative. Positive offsets outward and
        /// negative offsets inward.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>AxisAlignedRectangle.</returns>
        public AxisAlignedRectangle Offset(float offset)
        {
            return new AxisAlignedRectangle(XMin - offset, YMin - offset, XMax + offset, YMax + offset);
        }
    }
}