using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    public abstract class Grid<T>
    {
        public T[] Values { get; private set; }
        /// <summary>
        /// Gets the minimum x of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum x.</value>
        public double MinX { get; private set; }
        /// <summary>
        /// Gets the minimum y of the projected grid of 2D points.
        /// </summary>
        /// <value>The minimum y.</value>
        public double MinY { get; private set; }
        /// <summary>
        /// Gets the length of the pixel side.
        /// </summary>
        /// <value>The length of the pixel side.</value>
        public double PixelSideLength { get; private set; }
        /// <summary>
        /// Gets the inverse length of the pixel side.
        /// </summary>
        /// <value>The length of the pixel side.</value>
        public double inversePixelSideLength { get; private set; }
        /// <summary>
        /// Gets the count of pixels in the x-direction.
        /// </summary>
        /// <value>The x count.</value>
        public int XCount { get; private set; }
        /// <summary>
        /// Gets the count of pixels in the y-direction.
        /// </summary>
        /// <value>The y count.</value>
        public int YCount { get; private set; }

        public int MaxIndex;

        public void Initialize(double minX, double maxX, double minY, double maxY, int pixelsPerRow, int pixelBorder = 2)
        {
            //MaxX = maxX;
            MinX = minX;
            //MaxY = maxY;
            MinY = minY;
            var XLength = maxX - MinX;
            var YLength = maxY - MinY;
            var MaxLength = XLength > YLength ? XLength : YLength;

            //Calculate the size of each pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = MaxLength / (pixelsPerRow - pixelBorder * 2);
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

        public void Initialize(double minX, double maxX, double minY, double maxY, double pixelSideLength, int pixelBorder = 2)
        {
            MinX = minX;
            MinY = minY;
            var xLength = maxX - MinX;
            var yLength = maxY - MinY;

            //Calculate the size of each pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = pixelSideLength; // MaxLength / (pixelsPerRow - pixelBorder * 2);
            inversePixelSideLength = 1 / PixelSideLength;
            XCount = (int)Math.Ceiling(xLength * inversePixelSideLength);
            YCount = (int)Math.Ceiling(yLength * inversePixelSideLength);
            // shift the grid slightly so that the part grid points are better aligned within the bounds
            var xStickout = xLength - XCount * PixelSideLength;
            MinX += xStickout / 2;
            var yStickout = yLength - YCount * PixelSideLength;
            MinY += yStickout / 2;
            // add the pixel border...2 since includes both sides (left and right, or top and bottom)
            XCount += pixelBorder * 2;
            YCount += pixelBorder * 2;

            MaxIndex = XCount * YCount - 1;
            Values = new T[XCount * YCount];
        }

        public IEnumerable<(int index, int x, int y)> Indices()
        {
            for (var yIndex = 0; yIndex < YCount; yIndex++)
                for (var xIndex = 0; xIndex < XCount; xIndex++)
                    yield return (YCount * xIndex + yIndex, xIndex, yIndex);
        }

        public bool TryGet(int xIndex, int yIndex, out T value) => TryGet(GetIndex(xIndex, yIndex), out value);
        public bool TryGet(double x, double y, out T value) => TryGet(GetIndex(x, y), out value);
        public bool TryGet(int index, out T value)
        {
            if (index < 0 || index > MaxIndex)
            {
                value = default;
                return false;
            }
            value = Values[index];
            return !EqualityComparer<T>.Default.Equals(value, default(T));
        }

        public void Set(int xIndex, int yIndex, T newValue) => Set(GetIndex(xIndex, yIndex), newValue);
        public void Set(int index, T newValue)
        {
            Values[index] = newValue;
        }

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

        private IEnumerable<(int, int)> PlotSteepLine(double x0, double y0, double x1, double y1)
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
            var bottom = GetYIndex(y0);
            var top = GetYIndex(y1);
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

        private IEnumerable<(int, int)> PlotShallowLine(double x0, double y0, double x1, double y1)
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
            var left = GetXIndex(x0);
            var right = GetXIndex(x1);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int, int) GetXYIndicesFromPixelIndices(int index)
        {
            var (x, y) = (index / YCount, index % YCount);
            var test = GetIndex(x, y);
            if(index != test) { }
            return(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(double x, double y) => YCount * GetXIndex(x) + GetYIndex(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int xIndex, int yIndex) => YCount * xIndex + yIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetXIndex(double x) => (int)((x - MinX) * inversePixelSideLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetYIndex(double y) => (int)((y - MinY) * inversePixelSideLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSnappedX(int x) => x * PixelSideLength + MinX;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSnappedY(int y) => y * PixelSideLength + MinY;
    }
}
