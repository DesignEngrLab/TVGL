using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL
{
    public class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, int, byte>
    {

        public MarchingCubesDenseVoxels(VoxelizedSolid solid, int discretization)
            : base(solid, discretization)
        {
            numGridX = (int)Math.Ceiling(solid.VoxelsPerSide[0] / (double)discretization);
            numGridY = (int)Math.Ceiling(solid.VoxelsPerSide[1] / (double)discretization);
            numGridZ = (int)Math.Ceiling(solid.VoxelsPerSide[2] / (double)discretization);
            for (int i = 0; i < 8; i++)
                GridOffsetTable[i] = _unitOffsetTable[i].multiply(discretization);
        }

        protected override int GetOffset(byte v1, byte v2)
        {
            throw new NotImplementedException();
        }

        protected override byte GetValueFromSolid(int x, int y, int z)
        {
            return solid[x, y, z];
        }

        /// <summary>
        /// MakeTriangles performs the Marching Cubes algorithm on a single cube
        /// </summary>
        protected override int FindEdgeVertices(int x, int y, int z, long identifier)
        {
            int cubeType = 0;
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
            {
                if (solid[x + GridOffsetTable[i][0],
                    y + GridOffsetTable[i][1],
                    z + GridOffsetTable[i][2]] > 0)
                    cubeType |= 1 << i;
            }
            //Find which edges are intersected by the surface
            int edgeFlags = CubeEdgeFlagsTable[cubeType];

            //If the cube is entirely inside or outside of the surface, then there will be no intersections
            if (edgeFlags == 0) return cubeType;

            //Find the point of intersection of the surface with each edge
            for (var i = 0; i < 12; i++)
            {
                //if there is an intersection on this edge
                if ((edgeFlags & 1) != 0)
                {
                    double offset = GetOffset(cube[EdgeVertexIndexTable[i][0]], cube[EdgeVertexIndexTable[i][1]]);

                    EdgeVertex[i][0] = x + (_unitOffsetTable[EdgeVertexIndexTable[i][0]][0] + offset * EdgeDirectionTable[i][0]);
                    EdgeVertex[i][1] = y + (_unitOffsetTable[EdgeVertexIndexTable[i][0]][1] + offset * EdgeDirectionTable[i][1]);
                    EdgeVertex[i][2] = z + (_unitOffsetTable[EdgeVertexIndexTable[i][0]][2] + offset * EdgeDirectionTable[i][2]);
                }
                edgeFlags >>= 1;
            }
            return cubeType;
        }
    }
}
