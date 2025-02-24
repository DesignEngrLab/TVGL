// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Grid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Class Grid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Grid<T>
    {
        /// <summary>
        /// Gets the values.
        /// Don't make this a concurrent dictionary. Since the operations are generally very small and quick,
        /// parallel threads will cost more to boot up that the time they save, resulting in slower processing.
        /// Could use a GPU.
        /// </summary>
        /// <value>The values.</value>
        public T[] Values { get; private protected set; }
        /// <summary>
        /// Gets the minimum x of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum x.</value>
        public double MinX { get; private protected set; }
        /// <summary>
        /// Gets the minimum y of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum y.</value>
        public double MinY { get; private protected set; }
        /// <summary>
        /// Gets the maximum x of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum x.</value>
        public double MaxX { get; private protected set; }
        /// <summary>
        /// Gets the maximum y of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum y.</value>
        public double YLength { get; private protected set; }
        /// <summary>
        /// Gets the minimum x of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum x.</value>
        public double XLength { get; private protected set; }
        /// <summary>
        /// Gets the minimum y of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum y.</value>
        public double MaxY { get; private protected set; }
        /// <summary>
        /// Gets the length of the pixel side.
        /// </summary>
        /// <value>The length of the pixel side.</value>
        public double PixelSideLength { get; private protected set; }
        /// <summary>
        /// Gets the Inverse length of the pixel side.
        /// </summary>
        /// <value>The length of the pixel side.</value>
        public double inversePixelSideLength { get; private protected set; }
        /// <summary>
        /// Gets the count of pixels in the x-direction.
        /// </summary>
        /// <value>The x count.</value>
        public int XCount { get; private protected set; }
        /// <summary>
        /// Gets the count of pixels in the y-direction.
        /// </summary>
        /// <value>The y count.</value>
        public int YCount { get; private protected set; }

        /// <summary>
        /// The maximum index
        /// </summary>
        public int MaxIndex;

        /// <summary>
        /// Initializes the specified minimum x.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public void Initialize(double minX, double maxX, double minY, double maxY, int pixelsPerRow, int pixelBorder = 2)
        {
            XLength = maxX - minX;
            YLength = maxY - minY;
            var MaxLength = XLength > YLength ? XLength : YLength;

            //Calculate the size of a pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 2xpixelBorder
            PixelSideLength = MaxLength / (pixelsPerRow - pixelBorder * 2);
            inversePixelSideLength = 1 / PixelSideLength;
            XCount = (int)Math.Ceiling(XLength * inversePixelSideLength);
            YCount = (int)Math.Ceiling(YLength * inversePixelSideLength);
            // shift the grid slightly so that the part is centered in the grid
            var xStickout = XCount * PixelSideLength - XLength;
            MinX = minX - xStickout / 2;
            var yStickout = YCount * PixelSideLength - YLength;
            MinY = minY - yStickout / 2;
            // add the pixel border...2x since includes both sides (left and right, or top and bottom)
            XCount += pixelBorder * 2;
            YCount += pixelBorder * 2;
            MinX -= pixelBorder * PixelSideLength;
            MinY -= pixelBorder * PixelSideLength;
            MaxX = MinX + XCount * PixelSideLength;
            MaxY = MinY + YCount * PixelSideLength;
            MaxIndex = XCount * YCount - 1;
            Values = new T[XCount * YCount];
        }

        /// <summary>
        /// Initializes the specified minimum x.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <param name="pixelSideLength">Length of the pixel side.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public void Initialize(double minX, double maxX, double minY, double maxY, double pixelSideLength, int pixelBorder = 2)
        {
            MaxX = maxX;
            MinX = minX;
            MaxY = maxY;
            MinY = minY;
            XLength = maxX - MinX;
            YLength = maxY - MinY;
            //Calculate the size of each pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = pixelSideLength; // MaxLength / (pixelsPerRow - pixelBorder * 2);
            inversePixelSideLength = 1 / PixelSideLength;
            XCount = (int)Math.Ceiling(XLength * inversePixelSideLength);
            YCount = (int)Math.Ceiling(YLength * inversePixelSideLength);
            // shift the grid slightly so that the part grid points are better aligned within the bounds
            var xStickout = XLength - XCount * PixelSideLength;
            MinX += xStickout / 2;
            var yStickout = YLength - YCount * PixelSideLength;
            MinY += yStickout / 2;
            // add the pixel border...2 since includes both sides (left and right, or top and bottom)
            XCount += pixelBorder * 2;
            YCount += pixelBorder * 2;

            MaxIndex = XCount * YCount - 1;
            Values = new T[XCount * YCount];
        }
        public void Initialize<U>(Grid<U> grid)
        {
            PixelSideLength = grid.PixelSideLength;
            inversePixelSideLength = grid.inversePixelSideLength;
            MinX =grid.MinX;
            MaxX = grid.MaxX;
            MinY =grid.MinY;
            MaxY = grid.MaxY;
            XLength = MaxX - MinX;
            YLength = MaxY - MinY;

            XCount = grid.XCount;
            YCount = grid.YCount;
            MaxIndex = grid.MaxIndex;
            Values = new T[grid.Values.Length];
        }

        /// <summary>
        /// Indiceses this instance.
        /// </summary>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;System.Int32, System.Int32, System.Int32&gt;&gt;.</returns>
        public IEnumerable<(int index, int x, int y)> Indices()
        {
            for (var yIndex = 0; yIndex < YCount; yIndex++)
                for (var xIndex = 0; xIndex < XCount; xIndex++)
                    yield return (YCount * xIndex + yIndex, xIndex, yIndex);
        }

        /// <summary>
        /// Tries the get.
        /// </summary>
        /// <param name="xIndex">Index of the x.</param>
        /// <param name="yIndex">Index of the y.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryGet(int xIndex, int yIndex, out T value) => TryGet(GetIndex(xIndex, yIndex), out value);
        /// <summary>
        /// Tries the get.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryGet(double x, double y, out T value) => TryGet(GetIndex(x, y), out value);
        /// <summary>
        /// Tries the get.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryGet(int index, out T value)
        {
            if (index < 0 || index > MaxIndex)
            {
                value = default;
                return false;
            }
            value = Values[index];
            return  !EqualityComparer<T>.Default.Equals(value, default(T));
        }

        /// <summary>
        /// Sets the specified x index.
        /// </summary>
        /// <param name="xIndex">Index of the x.</param>
        /// <param name="yIndex">Index of the y.</param>
        /// <param name="newValue">The new value.</param>
        public void Set(int xIndex, int yIndex, T newValue) => Set(GetIndex(xIndex, yIndex), newValue);
        /// <summary>
        /// Sets the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="newValue">The new value.</param>
        public void Set(int index, T newValue)
        {
            Values[index] = newValue;
        }

        /// <summary>
        /// Get the value at the specified x and y indices.
        /// </summary>        
        public T this[int x, int y]
        {
            get
            {
                TryGet(x, y, out var result);
                return result;
            }
            set
            {
                Set(x, y, value);
            }
        }
        /// <summary>
        /// Plots the line.
        /// </summary>
        /// <param name="x0">The x0.</param>
        /// <param name="y0">The y0.</param>
        /// <param name="x1">The x1.</param>
        /// <param name="y1">The y1.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;System.Int32, System.Int32&gt;&gt;.</returns>
        public IEnumerable<(int, int)> PlotLine(double x0, double y0, double x1, double y1)
        {
            if (Math.Abs(y1 - y0) < Math.Abs(x1 - x0))
            {
                if (x0 > x1)
                    return PlotShallowLine(x1, y1, x0, y0);
                else
                    return PlotShallowLine(x0, y0, x1, y1);
            }
            else
            {
                if (y0 > y1)
                    return PlotSteepLine(x1, y1, x0, y0);
                else
                    return PlotSteepLine(x0, y0, x1, y1);
            }
        }

        /// <summary>
        /// Plots the steep line.
        /// </summary>
        /// <param name="x0">The x0.</param>
        /// <param name="y0">The y0.</param>
        /// <param name="x1">The x1.</param>
        /// <param name="y1">The y1.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;System.Int32, System.Int32&gt;&gt;.</returns>
        protected IEnumerable<(int, int)> PlotSteepLine(double x0, double y0, double x1, double y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            var xi = 1;
            if (dx < 0)
            {
                xi = -1;
                dx = -dx;
            }
            var D = 2 * dx - dy;
            var x = GetXIndex(x0);
            var bottom =Math.Max(0, GetYIndex(y0));
            var top = Math.Min(YCount-1, GetYIndex(y1));
            for (var y = bottom; y <= top; y++)
            {
                yield return (x, y);
                if (D > 0)
                {
                    x += xi;
                    D += 2 * (dx - dy);
                }
                else
                {
                    D += 2 * dx;
                }
            }
        }

        /// <summary>
        /// Plots the shallow line.
        /// </summary>
        /// <param name="x0">The x0.</param>
        /// <param name="y0">The y0.</param>
        /// <param name="x1">The x1.</param>
        /// <param name="y1">The y1.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;System.Int32, System.Int32&gt;&gt;.</returns>
        protected IEnumerable<(int, int)> PlotShallowLine(double x0, double y0, double x1, double y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            var yi = 1;
            if (dy < 0)
            {
                yi = -1;
                dy = -dy;
            }
            var D = 2 * dy - dx;

            var y = GetYIndex(y0);
            var left =Math.Max(0, GetXIndex(x0));
            var right =Math.Min(XCount-1, GetXIndex(x1));
            for (var x = left; x <= right; x++)
            {
                yield return (x, y);
                if (D > 0)
                {
                    y += yi;
                    D += 2 * (dy - dx);
                }
                else
                {
                    D += 2 * dy;
                }
            }
        }

        /// <summary>
        /// Gets the xy indices from pixel indices.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>System.ValueTuple&lt;System.Int32, System.Int32&gt;.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int, int) GetXYIndicesFromPixelIndices(int index)
        {
            var (x, y) = (index / YCount, index % YCount);
            var test = GetIndex(x, y);
            if (index != test) { }
            return (x, y);
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(double x, double y) => GetIndex(GetXIndex(x), GetYIndex(y));

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <param name="xIndex">Index of the x.</param>
        /// <param name="yIndex">Index of the y.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int xIndex, int yIndex)
        {

            if (xIndex < 0 || xIndex >= XCount || yIndex < 0 || yIndex >= YCount) return -1;
            return YCount * xIndex + yIndex;
        }

        /// <summary>
        /// Gets the index of the x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetXIndex(double x) => (int)((x - MinX) * inversePixelSideLength);

        /// <summary>
        /// Gets the index of the y.
        /// </summary>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetYIndex(double y) => (int)((y - MinY) * inversePixelSideLength);

        /// <summary>
        /// Gets the snapped x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Double.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSnappedX(int x) => x * PixelSideLength + MinX;

        /// <summary>
        /// Gets the snapped y.
        /// </summary>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSnappedY(int y) => y * PixelSideLength + MinY;
    }
}
