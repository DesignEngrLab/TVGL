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
using System;
using System.Collections.Generic;
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
            var minJ = Math.Max(0, minIndices[1]);
            var maxJ = Math.Min(numVoxelsY, maxIndices[1]);
            var minK = Math.Max(0, minIndices[2]);
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
                    var start = (ushort)0;
                    if (crossings.Count == 0) continue;
                    var startDefined = inverseRange == crossings.Peek().Item1;
                    while (crossings.Count > 0)
                    {
                        var next = crossings.Dequeue();
                        var xIndex = (ushort)ConvertXCoordToIndex(next.Item2);
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
    }
}
