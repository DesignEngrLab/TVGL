// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="AxisAlignedRectangle.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using MIConvexHull;
using System;



namespace TVGL
{
    /// <summary>
    /// Axis-aligned rectangle
    /// </summary>
    public readonly struct AxisAlignedRectangle
    {
        /// <summary>
        /// The x minimum
        /// </summary>
        public readonly double XMin;
        /// <summary>
        /// The x maximum
        /// </summary>
        public readonly double XMax;
        /// <summary>
        /// The y minimum
        /// </summary>
        public readonly double YMin;
        /// <summary>
        /// The y maximum
        /// </summary>
        public readonly double YMax;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedRectangle"/> struct.
        /// </summary>
        /// <param name="box">The box.</param>
        public AxisAlignedRectangle(MonotoneBox box) : this()
        {
            XMin = Math.Min(box.Vertex1.X, box.Vertex2.X);
            XMax = Math.Max(box.Vertex1.X, box.Vertex2.X);
            YMin = Math.Min(box.Vertex1.Y, box.Vertex2.Y);
            YMax = Math.Max(box.Vertex1.Y, box.Vertex2.Y);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedRectangle"/> struct.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        public AxisAlignedRectangle(IPoint2D vertex1, IPoint2D vertex2) : this()
        {
            XMin = Math.Min(vertex1.X, vertex2.X);
            XMax = Math.Max(vertex1.X, vertex2.X);
            YMin = Math.Min(vertex1.Y, vertex2.Y);
            YMax = Math.Max(vertex1.Y, vertex2.Y);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignedRectangle"/> struct.
        /// </summary>
        /// <param name="xMin">The x minimum.</param>
        /// <param name="yMin">The y minimum.</param>
        /// <param name="xMax">The x maximum.</param>
        /// <param name="yMax">The y maximum.</param>
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
        /// Gets the width of the rectangle (always positive).
        /// </summary>
        /// <value>The width.</value>
        public double Width => XMax - XMin;

        /// <summary>
        /// Gets the height of the rectangle (always positive).
        /// </summary>
        /// <value>The height.</value>
        public double Height => YMax - YMin;
        /// <summary>
        /// Gets the  area of the rectangle (always positive).
        /// </summary>
        /// <value>The area.</value>
        public double Area => Width * Height;

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
        public AxisAlignedRectangle Offset(double offset)
        {
            return new AxisAlignedRectangle(XMin - offset, YMin - offset, XMax + offset, YMax + offset);
        }
    }
}