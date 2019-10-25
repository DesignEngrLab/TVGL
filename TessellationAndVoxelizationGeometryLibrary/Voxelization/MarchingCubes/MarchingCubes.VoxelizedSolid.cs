using System;
using System.Collections.Generic;
using TVGL.Implicit;

namespace TVGL.Voxelization
{
    public partial class MarchingCubes
    {
        private static double[][] EdgeVertex;
        private static int[] WindingOrder;

        public static TessellatedSolid Generate(VoxelizedSolid solid, int voxelsPerMarchingCube)
        {
            var vertices = new List<double[]>();
            var facesAsVertexIndices = new List<int[]>();
            var Cube = new double[voxelsPerMarchingCube, voxelsPerMarchingCube, voxelsPerMarchingCube];
            EdgeVertex = new double[12][];
            WindingOrder = new[] { 0, 1, 2 };
            var xDim = (int)Math.Ceiling(solid.VoxelsPerSide[0] / (double)voxelsPerMarchingCube);
            var yDim = (int)Math.Ceiling(solid.VoxelsPerSide[1] / (double)voxelsPerMarchingCube);
            var zDim = (int)Math.Ceiling(solid.VoxelsPerSide[2] / (double)voxelsPerMarchingCube);

            for (var x = 0; x < xDim; x += voxelsPerMarchingCube)
                for (var y = 0; y < yDim; y += voxelsPerMarchingCube)
                    for (var z = 0; z < zDim; z += voxelsPerMarchingCube)
                    {
                        for (var i = 0; i < voxelsPerMarchingCube; i++)
                            for (var j = 0; j < voxelsPerMarchingCube; j++)
                                for (var k = 0; k < voxelsPerMarchingCube; k++)
                                    Cube[i, j, k] = solid[x + i, y + j, z + k];
                        //Perform algorithm
                        MakeTriangles(x, y, z, Cube, vertices, facesAsVertexIndices);
                    }
            return new TessellatedSolid(vertices, facesAsVertexIndices, null);
        }



        /// <summary>
        /// MakeTriangles performs the Marching Cubes algorithm on a single cube
        /// </summary>
        private void MakeTriangles(double x, double y, double z, double[] cube, IList<double[]> vertices,
            IList<int[]> facesAsVertexIndices)
        {
            int flagIndex = 0;
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
                if (cube[i] <= SurfaceLevel)
                    flagIndex |= 1 << i;

            //Find which edges are intersected by the surface
            int edgeFlags = CubeEdgeFlagsTable[flagIndex];

            //If the cube is entirely inside or outside of the surface, then there will be no intersections
            if (edgeFlags == 0) return;

            //Find the point of intersection of the surface with each edge
            for (var i = 0; i < 12; i++)
            {
                //if there is an intersection on this edge
                if ((edgeFlags & (1 << i)) != 0)
                {
                    double offset = GetOffset(cube[EdgeVertexIndexTable[i][0]], cube[EdgeVertexIndexTable[i][1]]);

                    EdgeVertex[i][0] = x + (VertexOffsetTable[EdgeVertexIndexTable[i][0]][0] + offset * EdgeDirectionTable[i][0]);
                    EdgeVertex[i][1] = y + (VertexOffsetTable[EdgeVertexIndexTable[i][0]][1] + offset * EdgeDirectionTable[i][1]);
                    EdgeVertex[i][2] = z + (VertexOffsetTable[EdgeVertexIndexTable[i][0]][2] + offset * EdgeDirectionTable[i][2]);
                }
            }
            var idx = vertices.Count;
            //Save the triangles that were found. There can be up to five per cube
            for (var i = 0; i < NumFacesTable[flagIndex]; i++)
            {
                var face = new int[3];
                for (var j = 0; j < 3; j++)
                {
                    var vertexIndex = FaceVertexIndicesTable[flagIndex][3 * i + j];
                    face[j] = idx + WindingOrder[j];
                    vertices.Add(EdgeVertex[vertexIndex]);
                    idx++;
                }
                facesAsVertexIndices.Add(face);
            }
        }
    }
}
