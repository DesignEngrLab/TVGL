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
        private bool GetVoxelSparse(int x, int y, int z)
        {
            var yStartIndex = zSlices[z];
            if (zSlices[z + 1] == yStartIndex) return false;
            var numYLines = zSlices[z + 1] - 1 - yStartIndex;
            if (numYLines <= 0) return false;//there are no voxels at this value of z
            var yOffset = yOffsetsAndXIndices[yStartIndex];
            if (y < yOffset) return false;  //queried y is lower than the start for this z-slice's y range
            if (y >= yOffset + numYLines) return false; //queried y is greater than the end for this z-slice's y range
            var yLineIndex = yStartIndex + y - yOffset + 1;
            var xStartIndex = yOffsetsAndXIndices[yLineIndex];
            var xEndIndex = (y == yOffset + numYLines - 1) // if its the last line for this z-slice, 
                ? yOffsetsAndXIndices[yLineIndex + 2]  //then step over the next yOffset to get to the beginning of the next xRange
                : yOffsetsAndXIndices[yLineIndex + 1]; // else its just the next one minus one 
            if (xStartIndex == xEndIndex) return false;  //then there is no xRange for this y-Line
            var xStart = xRanges[xStartIndex];
            if (x < xStart) return false; //queried x is lower than the start for this x-range for this y-line at this z-slice
            var xStop = xRanges[xEndIndex - 1];
            if (x > xStop) return false;  //queried x is greater than the end of this x-range for this y-line at this z-slice
            for (int i = xStartIndex + 1; i < xEndIndex - 1; i += 2)
                if (x > xRanges[i] && x < xRanges[i + 1]) return false; // this is actually checking the gap between xRanges
            //otherwise, we're in an x-range for this y-line at this z-slice
            return true;
        }
        public void SetVoxelSparse(bool value, int xCoord, int yCoord, int zCoord)
        {
            var oldValue = GetVoxelSparse(xCoord, yCoord, zCoord);
            if (oldValue == value) return;
            if (value) AddVoxelSparse(xCoord, yCoord, zCoord);
            else RemoveVoxelSparse(xCoord, yCoord, zCoord);
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
            var numNeighbors = 0;
            //var numNeighbors = this.Sum(v => NumNeighborsSparse(v[0], v[1], v[2]));
            SurfaceArea = (6 * Count - numNeighbors) * Math.Pow(VoxelSideLength, 2);
        }


        #region Getting Neighbors
        //Neighbors functions use VoxelsPerSide
        public int[][] GetNeighborsSparse(int xCoord, int yCoord, int zCoord)
        {
            return GetNeighborsSparse(xCoord, yCoord, zCoord, VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]);
        }

        public int NumNeighborsSparse(int xCoord, int yCoord, int zCoord)
        {
            return NumNeighborsSparse(xCoord, yCoord, zCoord, VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]);
        }

        public int[][] GetNeighborsSparse(int xCoord, int yCoord, int zCoord, int xLim, int yLim, int zLim)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };

            if (xCoord != 0 && GetVoxelSparse(xCoord - 1, yCoord, zCoord)) neighbors[0] = new[] { xCoord - 1, yCoord, zCoord };
            if (xCoord + 1 != xLim && GetVoxelSparse(xCoord + 1, yCoord, zCoord)) neighbors[1] = new[] { xCoord + 1, yCoord, zCoord };
            if (yCoord != 0 && GetVoxelSparse(xCoord, yCoord - 1, zCoord)) neighbors[2] = new[] { xCoord, yCoord - 1, zCoord };
            if (yCoord + 1 != yLim && GetVoxelSparse(xCoord, yCoord + 1, zCoord)) neighbors[3] = new[] { xCoord, yCoord + 1, zCoord };
            if (zCoord != 0 && GetVoxelSparse(xCoord, yCoord, zCoord - 1)) neighbors[4] = new[] { xCoord, yCoord, zCoord - 1 };
            if (zCoord + 1 != zLim && GetVoxelSparse(xCoord, yCoord, zCoord + 1)) neighbors[5] = new[] { xCoord, yCoord, zCoord + 1 };

            return neighbors;
        }
        public int NumNeighborsSparse(int xCoord, int yCoord, int zCoord, int xLim, int yLim, int zLim)
        {
            var neighbors = 0;

            if (xCoord != 0 && GetVoxelSparse(xCoord - 1, yCoord, zCoord)) neighbors++;
            if (xCoord + 1 != xLim && GetVoxelSparse(xCoord + 1, yCoord, zCoord)) neighbors++;
            if (yCoord != 0 && GetVoxelSparse(xCoord, yCoord - 1, zCoord)) neighbors++;
            if (yCoord + 1 != yLim && GetVoxelSparse(xCoord, yCoord + 1, zCoord)) neighbors++;
            if (zCoord != 0 && GetVoxelSparse(xCoord, yCoord, zCoord - 1)) neighbors++;
            if (zCoord + 1 != zLim && GetVoxelSparse(xCoord, yCoord, zCoord + 1)) neighbors++;
            return neighbors;
        }
        #endregion


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
        int x, y, z;
        int xLineEnd, numYLines, zEnd;
        int xIndex, yIndex;
        int xRangesLength;

        public VoxelSparseEnumerator(VoxelizedSolid vs)
        {
            this.vs = vs;
            Reset();
        }

        public object Current => new[] { x, y, z };

        int[] IEnumerator<int[]>.Current => new[] { z, y, z };


        public void Dispose()
        {
          //Dispose();
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            x++;
            if (x > xLineEnd)
            {
                xIndex++;
                if (xIndex >= xRangesLength) return false;
                x = vs.xRanges[xIndex];
                if (xIndex == vs.yOffsetsAndXIndices[yIndex + 1])
                {
                    numYLines--;
                    y++;
                }
                if (numYLines == 0)
                {
                    z++;
                    if (z == zEnd) return false;

                    var yStartIndex = vs.zSlices[z];
                    y = vs.yOffsetsAndXIndices[yStartIndex];
                    numYLines = vs.zSlices[z + 1] - 1 - yStartIndex;
                }
            }
            return true;
        }

        public void Reset()
        {
            this.z = vs.VoxelBounds[0][2];
            this.zEnd = vs.VoxelBounds[1][2];
            var yStartIndex = vs.zSlices[z];
            y = vs.yOffsetsAndXIndices[yStartIndex];
            numYLines = vs.zSlices[z + 1] - 1 - yStartIndex;

            xIndex = 0;
            this.x = vs.xRanges[0];
            this.xLineEnd = vs.xRanges[1];
            xRangesLength = vs.xRanges.Count;
        }
    }

}
