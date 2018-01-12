// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid
    {

        public TessellatedSolid ConvertToTessellatedSolid(double minEdgeLength = -1)
        {
            var faceCollection = new ConcurrentBag<PolygonalFace>();
            var voxelVertexDictionary = Voxels(this.Discretization, VoxelRoleTypes.Partial, true)
                .ToDictionary(v => v.ID, v => new Vertex(v.BottomCoordinate));
            var boundaryVertexDictionary = new Dictionary<long, Vertex>();
            var sideLength = VoxelSideLengths[discretizationLevel];
            var deltaX = 1L << (4 + 4 * (4 - discretizationLevel));
            var deltaY = 1L << (24 + 4 * (4 - discretizationLevel));
            var deltaZ = 1L << (44 + 4 * (4 - discretizationLevel));

            //Parallel.ForEach(Voxels(VoxelRoleTypes.Partial), v =>
            foreach (var v in Voxels(this.Discretization, VoxelRoleTypes.Partial, true))
            {
                var neighborX = GetNeighbor(v, VoxelDirections.XPositive);
                var neighborY = GetNeighbor(v, VoxelDirections.YPositive);
                var neighborZ = GetNeighbor(v, VoxelDirections.ZPositive);
                var neighborXY = neighborX == null ? null : GetNeighbor(neighborX, VoxelDirections.YPositive);
                var neighborYZ = neighborY == null ? null : GetNeighbor(neighborY, VoxelDirections.ZPositive);
                var neighborZX = neighborZ == null ? null : GetNeighbor(neighborZ, VoxelDirections.XPositive);
                var neighborXYZ = neighborXY == null ? null : GetNeighbor(neighborXY, VoxelDirections.ZPositive);
                var vertexbase = voxelVertexDictionary[v.ID];
                var vertexX = neighborX != null ? GetOrMakeVoxelVertex(neighborX, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { sideLength, 0, 0 }, deltaX);
                var vertexY = neighborY != null ? GetOrMakeVoxelVertex(neighborY, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { 0.0, sideLength, 0 }, deltaY);
                var vertexZ = neighborZ != null ? GetOrMakeVoxelVertex(neighborZ, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { 0.0, 0.0, sideLength }, deltaZ);
                var vertexXY = neighborXY != null ? GetOrMakeVoxelVertex(neighborXY, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { sideLength, sideLength, 0 }, deltaX + deltaY);
                var vertexYZ = neighborYZ != null ? GetOrMakeVoxelVertex(neighborYZ, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { 0.0, sideLength, sideLength }, deltaZ + deltaY);
                var vertexZX = neighborZX != null ? GetOrMakeVoxelVertex(neighborZX, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { sideLength, 0.0, sideLength }, deltaX + deltaZ);
                var vertexXYZ = neighborXYZ != null ? GetOrMakeVoxelVertex(neighborXYZ, voxelVertexDictionary)
                    : MakeBoundaryVertex(v, boundaryVertexDictionary, new[] { sideLength, sideLength, sideLength }, deltaX + deltaY + deltaZ);

                // negative X face
                IVoxel negativeNeighbor = GetNeighbor(v, VoxelDirections.XNegative);
                if (negativeNeighbor == null || negativeNeighbor.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexbase, vertexZ, vertexYZ, vertexY);
                // negative Y face
                negativeNeighbor = GetNeighbor(v, VoxelDirections.YNegative);
                if (negativeNeighbor == null || negativeNeighbor.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexbase, vertexX, vertexZX, vertexZ);
                // negative Z face
                negativeNeighbor = GetNeighbor(v, VoxelDirections.ZNegative);
                if (negativeNeighbor == null || negativeNeighbor.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexbase, vertexY, vertexXY, vertexX);
                // positive X face
                if (neighborX == null || neighborX.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexZX, vertexX, vertexXY, vertexXYZ);
                // positive Y face
                if (neighborY == null || neighborY.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexYZ, vertexXYZ, vertexXY, vertexY);
                // positive Z face
                if (neighborZ == null || neighborZ.Role == VoxelRoleTypes.Empty)
                    MakeFaces(faceCollection, vertexZ, vertexZX, vertexXYZ, vertexYZ);
            } //);
            var vertices = voxelVertexDictionary.Values.ToList();
            vertices.AddRange(boundaryVertexDictionary.Values);
            var ts= new TessellatedSolid(faceCollection.ToList(), vertices, false);
            ts.SimplifyFlatPatches();
            return ts;
        }

        private void MakeFaces(ConcurrentBag<PolygonalFace> faceCollection, Vertex v1, Vertex v2, Vertex v3, Vertex v4)
        {
            //var f1=new PolygonalFace(new[] { v1, v2, v3 });
            //faceCollection.Add(f1);
            faceCollection.Add(new PolygonalFace(new[] { v1, v2, v3 }));
            faceCollection.Add(new PolygonalFace(new[] { v1, v3, v4 }));
        }

        private Vertex MakeBoundaryVertex(IVoxel baseVoxel, Dictionary<long, Vertex> boundaryDictionary, double[] shift, long delta)
        {
            var hash = Constants.ClearFlagsFromID(baseVoxel.ID) + delta;
            // I really over-thought this function for a long time, but I now realize that even thought the delta will cause the 
            // voxel coordinates to rollover and cause entries into the flags area - this is all totally fine as boundaryDictionary
            // is empty of regular voxels.
            lock (boundaryDictionary)
            {
                if (boundaryDictionary.ContainsKey(hash))
                    return boundaryDictionary[hash];
                var newVertex = new Vertex(baseVoxel.BottomCoordinate.add(shift));
                boundaryDictionary.Add(hash, newVertex);
                return newVertex;
            }
        }
        private Vertex GetOrMakeVoxelVertex(IVoxel baseVoxel, Dictionary<long, Vertex> voxelDictionary)
        {
            lock (voxelDictionary)
            {
                if (voxelDictionary.ContainsKey(baseVoxel.ID))
                    return voxelDictionary[baseVoxel.ID];
                var newVertex = new Vertex(baseVoxel.BottomCoordinate);
                voxelDictionary.Add(baseVoxel.ID, newVertex);
                return newVertex;
            }
        }

    }
}