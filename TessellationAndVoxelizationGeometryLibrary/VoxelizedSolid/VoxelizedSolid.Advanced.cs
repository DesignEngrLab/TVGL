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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public void MinkowskiAddOne(PrimitiveSurface surface)
        { }
        public void MinkowskiSubtractOne(PrimitiveSurface surface)
        { }
        public void Union(PrimitiveSurface surface)
        { }
        public void Intersect(PrimitiveSurface surface)
        { }
        public void Subtract(PrimitiveSurface surface)
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
                    var startDefined = !crossings.Peek().Item1;
                    while (crossings.Count > 0)
                    {
                        var next = crossings.Dequeue();
                        var xIndex = (ushort)ConvertXCoordToIndex(next.Item2);
                        var breakAfterThis = false;
                        if (xIndex >= numVoxelsX)
                        {
                            if (startDefined) voxRow.TurnOffRange(start, numVoxelsX);
                            breakAfterThis = true;
                        }
                        else if (startDefined)
                        {
                            voxRow.TurnOffRange(start, xIndex);
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
            } );
        }
        #endregion
    }
}
