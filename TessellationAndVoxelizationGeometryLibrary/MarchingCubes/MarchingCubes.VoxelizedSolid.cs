using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Implicit;

namespace TVGL.Voxelization
{
    public class MarchingCubesDenseVoxels : MarchingCubes
    {
        private readonly int voxelsPerMarchingCube;

        public MarchingCubesDenseVoxels(int voxelsPerMarchingCube)
            : base(true)
        {
            this.voxelsPerMarchingCube = voxelsPerMarchingCube;
        }
        public override TessellatedSolid Generate(Solid solid)
        {
            var vs = (VoxelizedSolid)solid;
            var faces = new List<PolygonalFace>();
            var xDim = (int)Math.Ceiling(vs.VoxelsPerSide[0] / (double)voxelsPerMarchingCube);
            var yDim = (int)Math.Ceiling(vs.VoxelsPerSide[1] / (double)voxelsPerMarchingCube);
            var zDim = (int)Math.Ceiling(vs.VoxelsPerSide[2] / (double)voxelsPerMarchingCube);

            for (var x = 0; x < xDim; x += voxelsPerMarchingCube)
                for (var y = 0; y < yDim; y += voxelsPerMarchingCube)
                    for (var z = 0; z < zDim; z += voxelsPerMarchingCube)
                        MakeTriangles(x, y, z, voxelsPerMarchingCube, faces);
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the voxelized solid, " + solid.Name);
            return new TessellatedSolid(faces, vertexDictionaries.SelectMany(d => d.Values), false,
                new[] { solid.SolidColor },
                solid.Units, solid.Name + "TS", solid.FileName, comments, solid.Language);
        }



        /// <summary>
        /// MakeTriangles performs the Marching Cubes algorithm on a single cube
        /// </summary>
        private void MakeTriangles(double x, double y, double z, int voxelsPerMarchingCube,List<PolygonalFace> faces)
        {
            int flagIndex = 0;
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
            {

                if (cube[i] <= SurfaceLevel)
                    flagIndex |= 1 << i;
            }
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
