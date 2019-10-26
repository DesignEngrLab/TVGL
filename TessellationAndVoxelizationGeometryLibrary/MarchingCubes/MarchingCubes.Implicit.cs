using System;
using System.Collections.Generic;
using TVGL.Implicit;

namespace TVGL.Voxelization
{
    public class MarchingCubesImplicit : MarchingCubes
    {
        private readonly double marchingCubeSideLength;

        public MarchingCubesImplicit(double marchingCubeSideLength, bool surfaceLevelIsPositive)
            : base(surfaceLevelIsPositive)
        {
            this.marchingCubeSideLength = marchingCubeSideLength;
        }

        public override TessellatedSolid Generate(Solid solid)
        {
            var implicitSolid = (ImplicitSolid)solid;
            var vertices = new List<double[]>();
            var facesAsVertexIndices = new List<int[]>();
            var Cube = new double[8];
            var surfaceLevel = implicitSolid.SurfaceLevel;
            var xDim = (int)Math.Ceiling((solid.XMax - solid.XMin) / marchingCubeSideLength);
            var yDim = (int)Math.Ceiling((solid.YMax - solid.ZMin) / marchingCubeSideLength);
            var zDim = (int)Math.Ceiling((solid.ZMax - solid.ZMin) / marchingCubeSideLength);
            for (var x = 0; x < xDim - 1; x++)
            {
                var xValue = solid.XMin + x * marchingCubeSideLength;
                for (var y = 0; y < yDim - 1; y++)
                {
                    var yValue = solid.YMin + x * marchingCubeSideLength;
                    for (var z = 0; z < zDim - 1; z++)
                    {
                        var zValue = solid.ZMin + x * marchingCubeSideLength;
                        //Get the values in the 8 neighbours which make up a cube
                        //oh wow. This is really efficient. Re-evaluate points multiple times.
                        // pretty far from a the BFS marching idea
                        for (var i = 0; i < 8; i++)
                            Cube[i] = implicitSolid[xValue + VertexOffsetTable[i][0] * marchingCubeSideLength,
                               yValue + VertexOffsetTable[i][1] * marchingCubeSideLength,
                               zValue + VertexOffsetTable[i][2] * marchingCubeSideLength];
                        //Perform algorithm
                        MakeTriangles(x, y, z, Cube, vertices, facesAsVertexIndices, surfaceLevel);
                    }
                }
            }
            return new TessellatedSolid(vertices, facesAsVertexIndices, null);
        }

        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        protected virtual double GetOffset(double v1, double v2, double surfaceLevel)
        {
            double delta = v2 - v1;

            //not sure I understand this condition.
            return (delta == 0.0f) ? surfaceLevel : (surfaceLevel - v1) / delta;
        }


        /// <summary>
        /// MakeTriangles performs the Marching Cubes algorithm on a single cube
        /// </summary>
        private void MakeTriangles(double x, double y, double z, double[] cube, IList<double[]> vertices,
            IList<int[]> facesAsVertexIndices, double surfaceLevel)
        {
            int flagIndex = 0;
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
                if (cube[i] <= surfaceLevel)
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
                    var v1 = cube[EdgeVertexIndexTable[i][0]];
                    var v2 = cube[EdgeVertexIndexTable[i][1]];
                    double offset = (v1 == v2) ? surfaceLevel : (surfaceLevel - v1) / (v2 - v1);
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
