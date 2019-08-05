using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StarMathLib;

namespace TVGL.Voxelization
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSparseDense.
    /// </summary>
    public partial class VoxelizedSolid : IEnumerable<int[]>
    {
        #region Basic Functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetVoxelSparse(int x, int y, int z)
        {
            var yStartIndex = zSlices[z];
            if (zSlices[z + 1] == yStartIndex) return 0;
            var numYLines = zSlices[z + 1] - 1 - yStartIndex;
            if (numYLines <= 0) return 0;//there are no voxels at this value of z
            var yOffset = yOffsetsAndXIndices[yStartIndex];
            if (y < yOffset) return 0;  //queried y is lower than the start for this z-slice's y range
            if (y >= yOffset + numYLines) return 0; //queried y is greater than the end for this z-slice's y range
            var yLineIndex = yStartIndex + y - yOffset + 1;
            var xStartIndex = yOffsetsAndXIndices[yLineIndex];
            var xEndIndex = (y == yOffset + numYLines - 1) // if its the last line for this z-slice, 
                ? yOffsetsAndXIndices[yLineIndex + 2]  //then step over the next yOffset to get to the beginning of the next xRange
                : yOffsetsAndXIndices[yLineIndex + 1]; // else its just the next one minus one 
            if (xStartIndex == xEndIndex) return 0;  //then there is no xRange for this y-Line
            var xStart = xRanges[xStartIndex];
            if (x < xStart) return 0; //queried x is lower than the start for this x-range for this y-line at this z-slice
            var xStop = xRanges[xEndIndex - 1];
            if (x > xStop) return 0;  //queried x is greater than the end of this x-range for this y-line at this z-slice
            for (int i = xStartIndex + 1; i < xEndIndex - 1; i += 2)
                if (x > xRanges[i] && x < xRanges[i + 1]) return 0; // this is actually checking the gap between xRanges
            //otherwise, we're in an x-range for this y-line at this z-slice
            return 1;
        }
        private void RemoveVoxelSparse(int x, int y, int z)
        {
            var yStartIndex = zSlices[z];
            var numYLines = zSlices[z + 1] - 1 - yStartIndex;
            if (numYLines <= 0) return;//there are no voxels at this value of z
            var yOffset = yOffsetsAndXIndices[yStartIndex];
            if (y < yOffset) return;  //queried y is lower than the start for this z-slice's y range
            if (y >= yOffset + numYLines) return; //queried y is greater than the end for this z-slice's y range
            var yLineIndex = yStartIndex + y - yOffset + 1;
            var xStartIndex = yOffsetsAndXIndices[yLineIndex];
            var xEndIndex = (y == yOffset + numYLines - 1) // if its the last line for this z-slice, 
                ? yOffsetsAndXIndices[yLineIndex + 2]  //then step over the next yOffset to get to the beginning of the next xRange
                : yOffsetsAndXIndices[yLineIndex + 1]; // else its just the next one minus one 
            if (xStartIndex == xEndIndex) return;

            for (int i = xStartIndex; i < xEndIndex; i += 2)
            {
                var xStart = xRanges[i];
                var xEnd = xRanges[i + 1];
                if (x < xStart) ; //queried x is lower than the start for this x-range for this y-line at this z-slice
                else if (x == xStart)
                {
                    if (xStart == xEnd)
                    {
                        xRanges.RemoveAt(i);
                        xRanges.RemoveAt(i);
                        //need to delete the range
                        DecrementRangesInRemainder(yLineIndex, yStartIndex, z);
                    }
                    else
                    {
                        xRanges[i]++;
                        return;
                    }
                }
                else if (x == xEnd)
                {
                    xRanges[i + 1]--;
                    return;
                }
                else if (x > xEnd) continue;  //go to the next xRange
                //otherwise we need to alter this range, which means inserting a new upperbound and lower bound into
                // the xRange, and then incrementing yOffsetsAndXIndices
                else
                {
                    xRanges.Insert(i, x + 1);
                    xRanges.Insert(i, x - 1);
                    // with these additions need to increment all the remaining values in yOffsetsAndXIndices
                    // but not the ones corresponding to the yOffset
                    IncrementRangesInRemainder(yLineIndex, yStartIndex, z);
                }
            }
        }

        private void DecrementRangesInRemainder(int yLineIndex, int yStartIndex, int z)
        {
            if (yLineIndex - yStartIndex == 1 && zSlices[z + 1] - yLineIndex == 1)
            {
                //delete from
            }
            for (int i = z + 1; i < VoxelsPerSide[2]; i++)
            {

            }
        }

        private void IncrementRangesInRemainder(int yLineIndex, int yStartIndex, int z)
        {
            throw new NotImplementedException();
        }

        private void AddVoxelSparse(int x, int y, int z)
        {
            var yStartIndex = zSlices[z];
            var numYLines = zSlices[z + 1] - 1 - yStartIndex;
            var yOffset = yOffsetsAndXIndices[yStartIndex];
            var yLineIndex = yStartIndex + y - yOffset + 1;
            if (numYLines <= 0)
            {

                //need to add a y-Line to this z
                // xRanges.Insert(i, x + 1);
                // xRanges.Insert(i, x - 1);
                // with these additions need to increment all the remaining values in yOffsetsAndXIndices
                // but not the ones corresponding to the yOffset
                IncrementRangesInRemainder(yLineIndex, yStartIndex, z);
            }
            if (y < yOffset) return;  //queried y is lower than the start for this z-slice's y range
            if (y >= yOffset + numYLines) return; //queried y is greater than the end for this z-slice's y range
            var xStartIndex = yOffsetsAndXIndices[yLineIndex];
            var xEndIndex = (y == yOffset + numYLines - 1) // if its the last line for this z-slice, 
                ? yOffsetsAndXIndices[yLineIndex + 2]  //then step over the next yOffset to get to the beginning of the next xRange
                : yOffsetsAndXIndices[yLineIndex + 1]; // else its just the next one minus one 
            if (xStartIndex == xEndIndex) return;

            for (int i = xStartIndex; i < xEndIndex; i += 2)
            {
                var xStart = xRanges[i];
                var xEnd = xRanges[i + 1];
                if (x < xStart) ; //queried x is lower than the start for this x-range for this y-line at this z-slice
                else if (x == xStart)
                {
                    if (xStart == xEnd)
                    {
                        xRanges.RemoveAt(i);
                        xRanges.RemoveAt(i);
                        //need to delete the range
                        DecrementRangesInRemainder(yLineIndex, yStartIndex, z);
                    }
                    else
                    {
                        xRanges[i]++;
                        return;
                    }
                }
                else if (x == xEnd)
                {
                    xRanges[i + 1]--;
                    return;
                }
                else if (x > xEnd) continue;  //go to the next xRange
                //otherwise we need to alter this range, which means inserting a new upperbound and lower bound into
                // the xRange, and then incrementing yOffsetsAndXIndices
                else
                {
                    xRanges.Insert(i, x + 1);
                    xRanges.Insert(i, x - 1);
                    // with these additions need to increment all the remaining values in yOffsetsAndXIndices
                    // but not the ones corresponding to the yOffset
                    IncrementRangesInRemainder(yLineIndex, yStartIndex, z);
                }
            }
        }

        #endregion
        #region Set/update properties

        public void UpdatePropertiesSparse()
        {
            var xRangeCount = xRanges.Count;
            int count = xRangeCount / 2;
            var xMin = int.MaxValue;
            var xMax = int.MinValue;
            for (int i = 0; i < xRangeCount; i += 2)
            {
                var xStart = xRanges[i];
                var xEnd = xRanges[i + 1];
                if (xMin > xStart) xMin = xStart;
                if (xMax < xEnd) xMax = xEnd;
                count += xEnd - xStart;
            }
            Volume = Count * Math.Pow(VoxelSideLength, 3);
            var zMin = 0;
            while (zSlices[zMin] == 0) zMin++;
            var zMax = VoxelsPerSide[2] - 1;
            while (zSlices[zMax] == zSlices[zMax + 1]) zMax--;
            var yMin = int.MaxValue;
            var yMax = int.MinValue;
            for (int i = zMin; i <= zMax; i++)
            {
                var yStartIndex = zSlices[i];
                var yOffset = yOffsetsAndXIndices[yStartIndex];
                if (yMin > yOffset) yMin = yOffset;
                var numYLines = zSlices[i + 1] - 1 - yStartIndex;
                if (yMax < yOffset + numYLines) yMax = yOffset + numYLines;
            }
            VoxelBounds = new[]
            {
                new[] { xMin, yMin, zMin },
                new[] { xMax, yMax, zMax },
            };
            var numNeighbors = this.Sum(v => NumNeighbors(v[0], v[1], v[2]));
            SurfaceArea = (6 * Count - numNeighbors) * Math.Pow(VoxelSideLength, 2);
        }
        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VoxelSparseEnumerator(this);
        }

        public IEnumerator<int[]> GetEnumerator()
        {
            return new VoxelSparseEnumerator(this);

        }
  
    }
    public class VoxelSparseEnumerator : IEnumerator<int[]>
    {
        VoxelizedSolid vs;
        int[] currentVoxelPosition = new int[3];
        int xValue;
        int yLineIndex;
        int zSliceIndex;
        int xEndRange;
        int xLim;
        int yLim;
        int zLim;
        public VoxelSparseEnumerator(VoxelizedSolid vs)
        {
            this.vs = vs;
            this.xLim = vs.VoxelsPerSide[0];
            this.yLim = vs.VoxelsPerSide[1];
            this.zLim = vs.VoxelsPerSide[2];
        }

        public object Current => currentVoxelPosition;

        int[] IEnumerator<int[]>.Current => currentVoxelPosition;


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            if (++xValue>xEndRange)
            {
                throw new NotImplementedException();
            }
            currentVoxelPosition[0] = xValue;
            currentVoxelPosition[1] = yLineIndex;
            currentVoxelPosition[2] = zSliceIndex;
            return true;
        }

        public void Reset()
        {
            xValue = yLineIndex = zSliceIndex = 0;
        }
    }

}
