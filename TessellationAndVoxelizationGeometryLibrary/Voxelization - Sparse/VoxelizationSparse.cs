// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="VoxelizedSparseDense_Constructors.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TVGL.IOFunctions;

namespace TVGL.Voxelization
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSparseDense.
    /// </summary>
    public partial class VoxelizedSparse : Solid
    {
        #region Properties
        public byte this[int x, int y, int z]
        {
            get
            {
                return GetVoxel(x, y, z);
            }
            set
            {
                if (value == 0) RemoveVoxel(x, y, z);
                else AddVoxel(x, y, z);
            }
        }


        public byte this[int[] index]
        {
            get
            {
                return GetVoxel(index[0], index[1], index[2]);
            }
            set
            {
                if (value == 0) RemoveVoxel(index[0], index[1], index[2]);
                AddVoxel(index[0], index[1], index[2]);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetVoxel(int x, int y, int z)
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
        private void RemoveVoxel(int x, int y, int z)
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

        private void AddVoxel(int x, int y, int z)
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
                        DecrementRangesInRemainder(yLineIndex,yStartIndex, z);
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


        int[] zSlices;
        List<int> yOffsetsAndXIndices;
        List<int> xRanges; //inclusive!
        public readonly int Discretization;
        public int[] VoxelsPerSide;
        public int[][] VoxelBounds { get; set; }
        public double VoxelSideLength { get; internal set; }
        public double[] TessToVoxSpace { get; }
        private readonly double[] Dimensions;
        public double[] Offset => Bounds[0];
        public int NumVoxels { get; internal set; }
        public TessellatedSolid TS { get; set; }

        #endregion

        public VoxelizedSparse(int[] voxelsPerSide, int discretization, double voxelSideLength,
            IEnumerable<double[]> bounds, byte value = 0)
        {
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            NumVoxels = 0;
            SurfaceArea = 0;
            Volume = 0;
            if (value != 0)
            {
                zSlices = new int[zLim + 1];
                var yArray = new int[zLim * (1 + yLim)];
                var xArray = new int[2 * yLim * zLim];
                for (int i = 0; i <= zLim; i++)
                {
                    zSlices[i + 1] = zSlices[i] + 1 + yLim;
                    for (int j = 0; j < yLim; j++)
                    {
                        throw new NotImplementedException();
                    }
                }
                yOffsetsAndXIndices = yArray.ToList();
                xRanges = xArray.ToList();
                UpdateBoundingProperties();

            }
            Discretization = discretization;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
        }

        public VoxelizedSparse(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds)
        {
            Discretization = discretization;
            VoxelsPerSide = voxelsPerSide;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }



        public VoxelizedSparse(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
            TS = ts;
            Discretization = discretization;
            SolidColor = new Color(Constants.DefaultColor);
            var voxelsOnLongSide = Math.Pow(2, Discretization);

            Bounds = new double[2][];
            Dimensions = new double[3];

            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
            }
            else
            {
                Bounds[0] = ts.Bounds[0];
                Bounds[1] = ts.Bounds[1];
            }
            for (var i = 0; i < 3; i++)
                Dimensions[i] = Bounds[1][i] - Bounds[0][i];

            //var longestSide = Dimensions.Max();
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int)Math.Round(d / VoxelSideLength)).ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);


            FillInFromTessellation(ts);
            UpdateProperties();
        }

        public VoxelizedSparse(TVGLFileData fileData, string fileName) : base(fileData, fileName)
        {
        }

        private void FillInFromTessellation(TessellatedSolid ts, bool possibleNullSlices = false)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            zSlices = new int[zLim + 1];
            yOffsetsAndXIndices = new List<int>();  // { 0 };
            xRanges = new List<int>();
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = AllSlicesAlongZ(ts, zBegin, zLim, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time

            //Parallel.For(0, zLim, k =>
            for (var k = 0; k < zLim; k++)
            {
                var loops = decomp[k];
                if (loops.Any())
                {
                    var intersections = AllPolygonIntersectionPointsAlongY(loops, yBegin, yLim, VoxelSideLength, out var yStartIndex);
                    var numYlines = intersections.Count;
                    yOffsetsAndXIndices.Add(yStartIndex);
                    for (int j = 0; j < numYlines; j++)
                    {
                        var intersectionPoints = intersections[j];
                        var numXRangesOnThisLine = intersectionPoints.Length;
                        yOffsetsAndXIndices.Add(xRanges.Count);
                        for (var m = 0; m < numXRangesOnThisLine; m += 2)
                        {
                            //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                            //Although other dimensions do not also do this. Everything operates with Round (effectively).
                            //Could reverse this to add more voxels
                            var sp = (int)((intersectionPoints[m] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (sp < 0) sp = 0;
                            var ep = (int)((intersectionPoints[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (ep >= xLim) ep = xLim - 1;
                            xRanges.Add(sp);
                            xRanges.Add(ep);
                        }
                    }
                }
                zSlices[k + 1] = yOffsetsAndXIndices.Count;
            }   //);
            yOffsetsAndXIndices.Add(-1);
            yOffsetsAndXIndices.Add(xRanges.Count);
        }
        private static List<List<PointLight>> GetZLoops(HashSet<Edge> penetratingEdges, double ZOfPlane)
        {
            var loops = new List<List<PointLight>>();

            var unusedEdges = new HashSet<Edge>(penetratingEdges);
            while (unusedEdges.Any())
            {
                var loop = new List<PointLight>();
                var firstEdgeInLoop = unusedEdges.First();
                var finishedLoop = false;
                var currentEdge = firstEdgeInLoop;
                do
                {
                    unusedEdges.Remove(currentEdge);
                    var intersectVertex = MiscFunctions.PointLightOnZPlaneFromIntersectingLine(ZOfPlane, currentEdge.From, currentEdge.To);
                    loop.Add(intersectVertex);
                    var nextFace = (currentEdge.From.Z < ZOfPlane) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                    Edge nextEdge = null;
                    foreach (var whichEdge in nextFace.Edges)
                    {
                        if (currentEdge == whichEdge) continue;
                        if (whichEdge == firstEdgeInLoop)
                        {
                            finishedLoop = true;
                            loops.Add(loop);
                            break;
                        }
                        else if (unusedEdges.Contains(whichEdge))
                        {
                            nextEdge = whichEdge;
                            break;
                        }
                    }
                    if (!finishedLoop && nextEdge == null)
                    {
                        Console.WriteLine("Incomplete loop.");
                        loops.Add(loop);
                    }
                    else currentEdge = nextEdge;
                } while (!finishedLoop);
            }
            return loops;
        }

        static List<List<PointLight>>[] AllSlicesAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            List<List<PointLight>>[] loopsAlongZ = new List<List<PointLight>>[numSteps];
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            var sortedVertices = ts.Vertices.OrderBy(v => v.Z).ToArray();
            var currentEdges = new HashSet<Edge>();
            var nextDistance = sortedVertices.First().Z;
            var vIndex = 0;
            for (int step = 0; step < numSteps; step++)
            {
                var z = startDistance + step * stepSize;
                var thisVertex = sortedVertices[vIndex];
                var needToOffset = false;
                while (thisVertex.Z <= z)
                {
                    if (thisVertex.Z == z) needToOffset = true;
                    foreach (var edge in thisVertex.Edges)
                    {
                        if (currentEdges.Contains(edge)) currentEdges.Remove(edge);
                        else currentEdges.Add(edge);
                    }
                    vIndex++;
                    thisVertex = sortedVertices[vIndex];
                }
                if (needToOffset)
                    z += (z + Math.Min(stepSize / 2, sortedVertices[vIndex + 1].Z)) / 2;
                if (currentEdges.Any()) loopsAlongZ[step] = GetZLoops(currentEdges, z);
                else loopsAlongZ[step] = new List<List<PointLight>>();
            }
            return loopsAlongZ;
        }

        internal string[] GetVoxelsAsStringArrays()
        {
            var zSlicesBitArray = zSlices.SelectMany(slice => BitConverter.GetBytes(slice)).ToArray();
            var yStartsBitArray = yOffsetsAndXIndices.SelectMany(yStart => BitConverter.GetBytes(yStart)).ToArray();
            var xRangeBitArray = xRanges.SelectMany(xRange => BitConverter.GetBytes(xRange)).ToArray();

            return new[]{
                BitConverter.ToString(zSlicesBitArray).Replace("-", ""),
                BitConverter.ToString(yStartsBitArray).Replace("-", ""),
                BitConverter.ToString(xRangeBitArray).Replace("-", "") };
        }

        internal static List<double[]> AllPolygonIntersectionPointsAlongY(List<List<PointLight>> loops, double start, int numSteps, double stepSize,
            out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongY(loops.Select(p => new Polygon(p.Select(point => new Point(point)), true)), start, numSteps, stepSize,
                out firstIntersectingIndex);
        }
        internal static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<Polygon> polygons, double start, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Path).OrderBy(p => p.Y).ToList();
            var currentLines = new HashSet<Line>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - start) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = start + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.Y <= y)
                {
                    if (thisPoint.Y == y) needToOffset = true;
                    foreach (var line in thisPoint.Lines)
                    {
                        if (currentLines.Contains(line)) currentLines.Remove(line);
                        else currentLines.Add(line);
                    }
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    y += (y + Math.Min(stepSize / 2, sortedPoints[pIndex + 1].Y)) / 2;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.XGivenY(y);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }

        public TessellatedSolid ConvertToTessellatedSolid(Color color)
        {
            throw new NotImplementedException();
        }

        public override Solid Copy()
        {
            throw new NotImplementedException();
        }
    }
}
