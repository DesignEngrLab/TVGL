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
            var bag = new ConcurrentBag<PolygonalFace>();
            var verticesDict = new Dictionary<long, Vertex>();
            foreach (var v in this.Voxels(VoxelRoleTypes.Partial))
            {
                var neighbors = GetNeighbors(v);
                var neighborX = neighbors[3];
                var neighborY = neighbors[4];
                var neighborZ = neighbors[5];
                var neighborXY = neighborX == null ? null : GetNeighbor(neighborX, VoxelDirections.YPositive);
                var neighborXZ = neighborX == null ? null : GetNeighbor(neighborX, VoxelDirections.ZPositive);
                var neighborYZ = neighborY == null ? null : GetNeighbor(neighborY, VoxelDirections.ZPositive);
                var neighborXYZ = neighborXY == null ? null : GetNeighbor(neighborXZ, VoxelDirections.ZPositive);
                //todo: oh! we may need to add the faces at the positive extremees
                if (neighbors[0] == null || neighbors[0].Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict, neighborX, v, neighborY, neighborXY);
                if (neighbors[1] == null || neighbors[1].Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict, v, neighborX, neighborXZ, neighborZ);
                if (neighbors[2] == null || neighbors[2].Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict, v, neighborZ, neighborYZ, neighborY);
                if (neighborX == null || neighborX.Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict,neighborXZ, neighborX, neighborXY,neighborXYZ);
                if (neighborY == null || neighborY.Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict,neighborYZ,neighborXYZ, neighborXY, neighborY);
                if (neighborZ == null || neighborZ.Role == VoxelRoleTypes.Empty)
                    MakeFaces(bag, verticesDict, neighborZ, neighborXZ, neighborXYZ, neighborYZ);
            }
            return new TessellatedSolid(bag.ToList(), verticesDict.Values.ToList());
        }

        private void MakeFaces(ConcurrentBag<PolygonalFace> faceCollection, Dictionary<long, Vertex> verticesDict, IVoxel btmLeft, IVoxel btmRight, IVoxel topRight, IVoxel topLeft)
        {
            Vertex vbl = GetOrMakeVertex(btmLeft, verticesDict);
            Vertex vbr = GetOrMakeVertex(btmRight, verticesDict);
            Vertex vtr = GetOrMakeVertex(topRight, verticesDict);
            Vertex vtl = GetOrMakeVertex(topLeft, verticesDict);
            faceCollection.Add(new PolygonalFace(new[] { vbl, vbr, vtl }));
            faceCollection.Add(new PolygonalFace(new[] { vbr, vtr, vtl }));
        }

        Vertex GetOrMakeVertex(IVoxel voxel, Dictionary<long, Vertex> verticesDict)
        {
            lock (verticesDict)
            {
                if (verticesDict.ContainsKey(voxel.ID))
                    return verticesDict[voxel.ID];
                var newVertex = new Vertex(voxel.BottomCoordinate);
                verticesDict.Add(voxel.ID, newVertex);
                return newVertex;
            }

        }
    }
}