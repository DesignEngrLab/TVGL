// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="VoxelizedSolidDense_Constructors.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MIConvexHull;
using StarMathLib;
using TVGL.Boolean_Operations;
using TVGL.Enclosure_Operations;
using TVGL.Voxelization;
using TVGL._2D;
using System.Runtime.CompilerServices;

namespace TVGL.DenseVoxels
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense : Solid
    {
        #region Properties
        public byte this[int x, int y, int z]
        {
            get
            {
                //var result = GetVoxel(x, y, z);
                //if (result != Voxels[x, y, z]) Console.WriteLine("NOT SAME");
                return Voxels[x, y, z];
            }
            set { Voxels[x, y, z] = value; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetVoxel(int x, int y, int z)
        {
            var yStartIndex = ySofZ[z];
            var numYLines = ySofZ[z + 1] - 1 - yStartIndex;
            if (numYLines <= 0) return 0;//there are no voxels at this value of z
            var yStart = yStartsAndXIndices[yStartIndex];
            if (y < yStart) return 0;  //queried y is lower than the start for this z-slice's y range
            if (y >= yStart + numYLines) return 0; //queried y is greater than the end for this z-slice's y range
            var xStartIndex = yStartsAndXIndices[y - yStart + 1];
            var xEndIndex = yStartsAndXIndices[y - yStart + 2] - 1; //the start of the next one minus one is the greatest
            var xStart = xRanges[xStartIndex];
            if (x < xStart) return 0; //queried x is lower than the start for this x-range for this y-line at this z-slice
            var xStop = xRanges[xEndIndex];
            if (x > xStop) return 0;  //queried x is greater than the end of this x-range for this y-line at this z-slice
            for (int i = xStartIndex + 1; i < xEndIndex; i += 2)
                if (x > xRanges[i] && x < xRanges[i + 1]) return 0; // this is actually checking the gap between xRanges
            //otherwise, we're in an x-range for this y-line at this z-slice
            return 1;
        }

        byte[,,] Voxels;
        int[] ySofZ;
        List<int> yStartsAndXIndices;
        List<int> xRanges;
        public readonly int Discretization;
        public int[] VoxelsPerSide;
        public int[][] VoxelBounds { get; set; }
        public double VoxelSideLength { get; internal set; }
        public double[] TessToVoxSpace { get; }
        private readonly double[] Dimensions;
        public double[] Offset => Bounds[0];
        public int Count { get; internal set; }
        public TessellatedSolid TS { get; set; }

        #endregion

        public VoxelizedSolidDense(int[] voxelsPerSide, int discretization, double voxelSideLength,
            IEnumerable<double[]> bounds, byte value = 0)
        {
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];
            Count = 0;
            SurfaceArea = 0;
            Volume = 0;
            if (value != 0)
            {
                Parallel.For(0, xLim, m =>
                {
                    for (var n = 0; n < yLim; n++)
                        for (var o = 0; o < zLim; o++)
                            Voxels[m, n, o] = value;
                });
                UpdateBoundingProperties();

            }
            Discretization = discretization;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
        }

        public VoxelizedSolidDense(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds)
        {
            Voxels = (byte[,,])voxels.Clone();
            Discretization = discretization;
            VoxelsPerSide = voxelsPerSide;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolidDense(VoxelizedSolidDense vs)
        {
            Voxels = (byte[,,])vs.Voxels.Clone();
            Discretization = vs.Discretization;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolidDense(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
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

            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];

            VoxelizeSolid(ts);
            UpdateProperties();
        }

        private void VoxelizeSolid(TessellatedSolid ts, bool possibleNullSlices = false)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            ySofZ = new int[zLim + 1];
            yStartsAndXIndices = new List<int>();
            xRanges = new List<int> { 0 };
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = UniformDecompositionAlongZ(ts, zBegin, zLim, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength;

            for (var k = 0; k < zLim; k++)
            {
                var loops = decomp[k];
                if (!loops.Any())
                {
                    ySofZ[k + 1] = ySofZ[k];
                    continue;
                }

                var sortedPoints = loops.SelectMany(loop => loop).OrderBy(p => p.Y).ToList();
                var yStartIndex = (int)((sortedPoints.First().Y - yBegin) * inverseVoxelSideLength);
                var numYlines = (int)((sortedPoints.Last().Y - yBegin) * inverseVoxelSideLength) + 1 - yStartIndex;
                ySofZ[k + 1] = ySofZ[k] + numYlines + 1;
                yStartsAndXIndices.Add(yStartIndex);
                var intersectionPoints = Slice2D.IntersectionPointsAtUniformDistancesAlongX(
                    loops.Select(p => new PolygonLight(p)), yBegin, VoxelSideLength, yLim);

                for (var q = 0; q < numYlines; q++)
                {
                    var yIntercept = (yStartIndex + q) * VoxelSideLength + (VoxelSideLength / 2.0);
                    yStartsAndXIndices.Add(xRanges.Count);
                    //parallel lines aligned with Y axis
                    var numYSections = 1;
                    var lastKey = -2;
                    // since its quicker to multiple then to divide, maybe doing this once at the top will save some time
                    foreach (var intersections in intersectionPoints)
                    {
                        var j = (int) Math.Floor((intersections.Key - yBegin) * inverseVoxelSideLength);
                        if (j - lastKey > 1)
                        {

                            numYSections++;
                        }

                        lastKey = j;
                        var intersectValues = intersections.Value;
                        yStartsAndXIndices.Add(intersectValues.Count);

                        var n = intersectValues.Count;
                        yStartsAndXIndices.Add(j);

                        for (var m = 0; m < n - 1; m += 2)
                        {
                            //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                            //Although other dimensions do not also do this. Everything operates with Round (effectively).
                            //Could reverse this to add more voxels
                            var sp = (int) Math.Round((intersectValues[m] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (sp < 0) sp = 0;
                            var ep = (int) Math.Round((intersectValues[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (ep > xLim) ep = xLim;
                            xRanges.Add(sp);
                            xRanges.Add(ep);
                            for (var i = sp; i < ep; i++)
                                Voxels[i, j, k] = 1;
                        }
                    }

                    ySofZ[k + 1] = ySofZ[k] + numYSections + 1;
                } //);

                ySofZ[VoxelsPerSide[2]] = yStartsAndXIndices.Count; //add the last one
            }
        }

        private static List<List<PointLight>>[] UniformDecompositionAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            List<List<PointLight>>[] loopsAlongZ = new List<List<PointLight>>[numSteps];
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            var sortedVertices = ts.Vertices.OrderBy(v => v.Z).ToArray();
            var currentEdges = new HashSet<Edge>();
            var nextDistance = sortedVertices.First().Z;
            var vIndex = 0;
            for (var step = 0; step < numSteps; step++)
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

                if (currentEdges.Any())
                {
                    //------------- GetZLoops() --------------//
                    var loops = new List<List<PointLight>>();

                    var unusedEdges = new HashSet<Edge>(currentEdges);
                    while (unusedEdges.Any())
                    {
                        var firstEdgeInLoop = unusedEdges.First();
                        var finishedLoop = false;
                        var currentEdge = unusedEdges.First();
                        var loop = new List<PointLight>();
                        do
                        {
                            unusedEdges.Remove(currentEdge);
                            var intersectVertex =
                                MiscFunctions.PointLightOnZPlaneFromIntersectingLine(z, currentEdge.From,
                                    currentEdge.To);
                            loop.Add(intersectVertex);
                            var nextFace = (currentEdge.From.Z < z) ? currentEdge.OtherFace : currentEdge.OwnedFace;
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

                            if (nextEdge == null && !finishedLoop)
                            {
                                Debug.WriteLine("Incomplete loop.");
                                loops.Add(loop);
                                finishedLoop = true;
                            }
                            else currentEdge = nextEdge;
                        } while (!finishedLoop);
                    }

                    //----------------------------------------//
                    loopsAlongZ[step] = loops;
                }
                else loopsAlongZ[step] = new List<List<PointLight>>();
            }

            return loopsAlongZ;
        }
    }
}
