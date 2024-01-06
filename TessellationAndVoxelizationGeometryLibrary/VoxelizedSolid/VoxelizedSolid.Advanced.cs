// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelizedSolid.PublicFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid : Solid, IEnumerable<(int xIndex, int yIndex, int zIndex)>
    {
        public void MinkowskiAddBlock(PrimitiveSurface surface, double xLength, double yLength, double zLength)
        { }
        public void MinkowskiSubtractBlock(PrimitiveSurface surface, double xLength, double yLength, double zLength)
        { }
        public void MinkowskiAddSphere(PrimitiveSurface surface, double radius)
        { }
        public void MinkowskiSubtractSphere(PrimitiveSurface surface, double radius)
        { }
        public static VoxelizedSolid MinkowskiSubtractOne(VoxelizedSolid reference, bool? xNegFilter = null,
            bool? xPosFilter = null, bool? yNegFilter = null, bool? yPosFilter = null, bool? zNegFilter = null, bool? zPosFilter = null)
        {
            var newSolid = reference.Copy();
            var lastYIndex = -1;
            var lastZIndex = -1;
            var xStartIndex = -1;
            var xEndIndex = -1;
            var Xranges = new List<(ushort, ushort)>();
            foreach ((int xIndex, int yIndex, int zIndex, bool xNeg, bool xPos, bool yNeg, bool yPos, bool zNeg, bool zPos)
                in reference.GetExposedVoxelsWithSides())
            {
                if (yIndex != lastYIndex || zIndex != lastZIndex)
                {
                    foreach (var range in Xranges)
                        newSolid.voxels[lastYIndex + newSolid.zMultiplier * lastZIndex].TurnOffRange(range.Item1, range.Item2);
                    if (xStartIndex != -1)
                    {
                        newSolid.voxels[lastYIndex + newSolid.zMultiplier * lastZIndex].TurnOffRange((ushort)xStartIndex, (ushort)xEndIndex);
                        xStartIndex = -1;
                    }
                    lastYIndex = yIndex;
                    lastZIndex = zIndex;
                    Xranges.Clear();
                }
                if (xNegFilter.HasValue && xNegFilter.Value != xNeg) continue;
                if (xPosFilter.HasValue && xPosFilter.Value != xPos) continue;
                if (yNegFilter.HasValue && yNegFilter.Value != yNeg) continue;
                if (yPosFilter.HasValue && yPosFilter.Value != yPos) continue;
                if (zNegFilter.HasValue && zNegFilter.Value != zNeg) continue;
                if (zPosFilter.HasValue && zPosFilter.Value != zPos) continue;
                if (xIndex == xEndIndex)
                    xEndIndex++;
                else
                {
                    if (xStartIndex != -1)
                        Xranges.Add(((ushort)xStartIndex, (ushort)xEndIndex));
                    xStartIndex = xIndex;
                    xEndIndex = xStartIndex + 1;
                }
            }
            return newSolid;
        }
        public static VoxelizedSolid MinkowskiAddOne(in VoxelizedSolid reference, bool? xNegFilter = null,
            bool? xPosFilter = null, bool? yNegFilter = null, bool? yPosFilter = null, bool? zNegFilter = null, bool? zPosFilter = null)
        { throw new NotImplementedException(); }

        /// <summary>
        ///  Creates the union of the voxels on the "inside" of this surface with this solid.
        /// </summary>
        /// <param name="surface"></param>
        public void Union(PrimitiveSurface surface) => BooleanOperation(surface, true, false);

        /// <summary>
        /// Intersects the voxels on the "inside" of this surface with this solid.
        /// </summary>
        /// <param name="surface"></param>
        public void Intersect(PrimitiveSurface surface) => BooleanOperation(surface, false, true);

        /// <summary>
        /// Subrtracts the voxels on the "inside" of this surface from this solid.
        /// </summary>
        /// <param name="surface"></param>
        public void Subtract(PrimitiveSurface surface) => BooleanOperation(surface, false, false);

        private void BooleanOperation(PrimitiveSurface surface, bool turnOn, bool inverseRange)
        {
            var minIndices = ConvertCoordinatesToIndices(new Vector3(surface.MinX, surface.MinY, surface.MinZ));
            var maxIndices = ConvertCoordinatesToIndices(new Vector3(surface.MaxX, surface.MaxY, surface.MaxZ));
            var minJ = minIndices[1];
            var maxJ = Math.Min(numVoxelsY, maxIndices[1]);
            var minK = minIndices[2];
            var maxK = Math.Min(numVoxelsZ, maxIndices[2]);


            Parallel.For(minK, maxK, k =>
            //for (var k = minK; k < maxK; k++)
            {
                var zCoord = ConvertZIndexToCoord(k);
                for (int j = minJ; j < maxJ; j++)
                {
                    var yCoord = ConvertYIndexToCoord(j);
                    var voxRow = (VoxelRowSparse)voxels[k * zMultiplier + j];
                    var crossings = new PriorityQueue<(bool, double), double>();
                    foreach (var q in surface.LineIntersection(new Vector3(XMin, yCoord, zCoord), Vector3.UnitX))
                        crossings.Enqueue((surface.GetNormalAtPoint(q.intersection).X < 0, q.lineT), q.lineT);
                    if (crossings.Count == 0) continue;
                    var start = (ushort)0;
                    // startDefined is true if the start of the range is defined. If it is false,
                    // then the start of the range is the beginning of the row.
                    var startDefined = inverseRange == crossings.Peek().Item1;
                    while (crossings.Count > 0)
                    {
                        var next = crossings.Dequeue();
                        var xIndex = ConvertXCoordToIndex(next.Item2);
                        var breakAfterThis = false;
                        if (xIndex >= numVoxelsX)
                        {
                            if (startDefined)
                                if (turnOn) voxRow.TurnOnRange(start, numVoxelsX);
                                else voxRow.TurnOffRange(start, numVoxelsX);
                            breakAfterThis = true;
                        }
                        else if (startDefined)
                        {
                            if (turnOn) voxRow.TurnOnRange(start, xIndex);
                            else voxRow.TurnOffRange(start, xIndex);
                            startDefined = false;
                        }
                        else
                        {
                            start = xIndex;
                            startDefined = true;
                        }
                        if (breakAfterThis) break;
                    }
                }
            });
        }


        /// <summary>
        ///  Creates the union of the voxels on the "inside" of this surface with this solid.
        /// </summary>
        /// <param name="surfaces"></param>
        public void Union(IList<PrimitiveSurface> surfaces) => BooleanOperation(surfaces, true, false);

        /// <summary>
        /// Intersects the voxels on the "inside" of this surface with this solid.
        /// </summary>
        /// <param name="surfaces"></param>
        public void Intersect(IList<PrimitiveSurface> surfaces) => BooleanOperation(surfaces, false, true);

        /// <summary>
        /// Subrtracts the voxels on the "inside" of this surface from this solid.
        /// </summary>
        /// <param name="surfaces"></param>
        public void Subtract(IList<PrimitiveSurface> surfaces) => BooleanOperation(surfaces, false, false);

        private void BooleanOperation(IList<PrimitiveSurface> surfaces, bool turnOn, bool inverseRange)
        {
            var totalMinK = int.MaxValue;
            var totalMaxK = 0;
            var surfacePerZLevel = new SimplePriorityQueue<(PrimitiveSurface, ushort), ushort>[numVoxelsZ];
            foreach (var surface in surfaces)
            {
                var minJ = ConvertYCoordToIndex(surface.MinY);
                var maxJ = Math.Min((ushort)(numVoxelsY - 1), ConvertYCoordToIndex(surface.MaxY));
                var minK = ConvertZCoordToIndex(surface.MinZ);
                if (totalMinK > minK) totalMinK = minK;
                var maxK = Math.Min((ushort)(numVoxelsZ - 1), ConvertZCoordToIndex(surface.MaxZ));
                if (totalMaxK < maxK) totalMaxK = maxK;
                for (var k = minK; k <= maxK; k++)
                {
                    if (surfacePerZLevel[k] == null)
                        surfacePerZLevel[k] = new SimplePriorityQueue<(PrimitiveSurface, ushort), ushort>();
                    surfacePerZLevel[k].Enqueue((surface, maxJ), minJ);
                }
            }

            //Parallel.For(totalMinK, totalMaxK+1, k =>
            for (var k = totalMinK; k <= totalMaxK; k++)
            {
                var zCoord = ConvertZIndexToCoord(k);
                var yQueue = surfacePerZLevel[k];
                var currentSurfaces = new Queue<(PrimitiveSurface, ushort)>();
                for (int j = yQueue.GetPriority(yQueue.First); j < numVoxelsY; j++)
                {
                    while (currentSurfaces.Count > 0 && currentSurfaces.Peek().Item2 < j)
                        currentSurfaces.Dequeue();
                    while (yQueue.Count > 0 && yQueue.GetPriority(yQueue.First) == j)
                    {
                        var next = yQueue.Dequeue();
                        currentSurfaces.Enqueue(next);
                    }
                    if (currentSurfaces.Count == 0)
                    {
                        if (yQueue.Count == 0) break;
                        else continue;
                    }
                    var yCoord = ConvertYIndexToCoord(j);
                    var voxRow = (VoxelRowSparse)voxels[k * zMultiplier + j];
                    var crossingTValues = new List<double>();
                    var crossingDirections = new List<bool>();
                    //if (k>=10&&j>=6)Presenter.ShowAndHang(this.ConvertToTessellatedSolidRectilinear());
                    var enteringIndex = int.MaxValue;
                    foreach ((var surface, var _) in currentSurfaces)
                    {
                        var lineCrossings = surface.LineIntersection(new Vector3(XMin, yCoord, zCoord), Vector3.UnitX).OrderBy(q => q.lineT).ToList();
                        if (lineCrossings.Count == 2 && lineCrossings[0].lineT.IsPracticallySame(lineCrossings[1].lineT))
                            continue;
                        foreach (var q in lineCrossings)
                        {
                            var normalX = surface.GetNormalAtPoint(q.intersection).X;
                            if (normalX.IsNegligible(Constants.DotToleranceOrthogonal)) continue;
                            var entering = normalX < 0;
                            var index = crossingTValues.IncreasingDoublesBinarySearch(q.lineT);

                            if (entering)
                            {
                                if (index == 0 || (index > 0 && !crossingDirections[index - 1]))
                                {
                                    crossingTValues.Insert(index, q.lineT);
                                    crossingDirections.Insert(index, entering);
                                    enteringIndex = index;
                                }
                                else enteringIndex = index - 1;
                            }
                            else // entering == false
                            {
                                if (index == crossingDirections.Count || (index < crossingDirections.Count && crossingDirections[index]))
                                {
                                    crossingTValues.Insert(index, q.lineT);
                                    crossingDirections.Insert(index, entering);
                                }
                                // unlike the entering case, we can remove the previous one if it is false (we can't do this in the
                                // entering case because we are adding a new sorted list of lineCrossings and dont'know what the subsequent
                                // ones are
                                if (index - enteringIndex > 1)
                                {
                                    crossingTValues.RemoveRange(enteringIndex + 1, index - enteringIndex - 1);
                                    crossingDirections.RemoveRange(enteringIndex + 1, index - enteringIndex - 1);
                                }
                            }
                        }
                    }
                    // now we need to remove any entering points that follow an existing entering point
                    for (int i = crossingTValues.Count - 1; i >= 1; i--)
                    {
                        if (crossingDirections[i - 1] && crossingDirections[i])
                            crossingTValues.RemoveAt(i);
                    }
                    if (crossingDirections.Count == 0) continue;
                    if (inverseRange == crossingDirections[0])
                    {
                        //crossingDirections.Insert(0, !crossingDirections[0]);
                        crossingTValues.Insert(0, 0);
                    }
                    for (int i = 0; i < crossingTValues.Count; i += 2)
                    {
                        var start = ConvertXCoordToIndex(crossingTValues[i]);
                        // for the end - it could be that the list has an odd number of crossings, in which case the last one is the end
                        var end = i + 1 == crossingTValues.Count ? numVoxelsX : ConvertXCoordToIndex(crossingTValues[i + 1]);
                        var lastOne = end >= numVoxelsX;
                        if (lastOne) end = numVoxelsX;

                        if (turnOn) voxRow.TurnOnRange(start, end);
                        else voxRow.TurnOffRange(start, end);
                        if (lastOne) break;
                    }
                    //Presenter.ShowAndHang(this.ConvertToTessellatedSolidRectilinear());

                }
            }  //);
        }
    }
}
