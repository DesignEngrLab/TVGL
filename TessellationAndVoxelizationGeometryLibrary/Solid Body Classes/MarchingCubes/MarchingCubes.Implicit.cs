using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Implicit;

namespace TVGL
{
    public class MarchingCubesImplicit : MarchingCubes<ImplicitSolid, double, double>
    {
        private double surfaceLevel;

        public MarchingCubesImplicit(ImplicitSolid solid, double discretization)
            : base(solid, discretization)
        {
            surfaceLevel = solid.SurfaceLevel;
            numGridX = (int)Math.Ceiling((solid.XMax - solid.XMin) / discretization);
            numGridY = (int)Math.Ceiling((solid.YMax - solid.ZMin) / discretization);
            numGridZ = (int)Math.Ceiling((solid.ZMax - solid.ZMin) / discretization);
            for (int i = 0; i < 8; i++)
                GridOffsetTable[i] = _unitOffsetTable[i].multiply(discretization);
        }


        protected override double GetValueFromSolid(double x, double y, double z)
        {
            var xValue = solid.XMin + x * discretization;
            var yValue = solid.YMin + y * discretization;
            var zValue = solid.ZMin + z * discretization;
            return solid[xValue, yValue, zValue];
        }


        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        protected override double GetOffset(double v1, double v2)
        {
            double delta = v2 - v1;

            //not sure I understand this condition.
            return (delta == 0.0f) ? surfaceLevel : (surfaceLevel - v1) / delta;
        }


        /// <summary>
        /// MakeTriangles performs the Marching Cubes algorithm on a single cube
        /// </summary>
        protected override int FindEdgeVertices(int x, int y, int z, long identifier)
        {
            var xValue = solid.XMin + x * discretization;
            var yValue = solid.YMin + y * discretization;
            var zValue = solid.ZMin + z * discretization;
            var cube = new double[8];
            for (var i = 0; i < 8; i++)
                cube[i] = GetValue(xValue + GridOffsetTable[i][0] ,
                   yValue + GridOffsetTable[i][1] ,
                   zValue + GridOffsetTable[i][2] ,
                   identifier);
            
            int cubeType = 0;
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
                if (cube[i] <= surfaceLevel)
                    cubeType |= 1 << i;

            //Find which edges are intersected by the surface
            int edgeFlags = CubeEdgeFlagsTable[cubeType];

            //If the cube is entirely inside or outside of the surface, then there will be no intersections
            if (edgeFlags == 0) return cubeType;

            //this loop creates or retrieves the vertices that are on the edges of the 
            //marching cube. These are stored in the EdgeVertexIndexTable
            for (var i = 0; i < 12; i++)
            {
                //if there is an intersection on this edge
                if ((edgeFlags & 1) != 0)
                {
                    var v1 = cube[EdgeVertexIndexTable[i][0]];
                    var v2 = cube[EdgeVertexIndexTable[i][1]];
                    double offset = (v1 == v2) ? surfaceLevel : (surfaceLevel - v1) / (v2 - v1);
                    var vertexCoord = new[]
                    {
                        x + _unitOffsetTable[EdgeVertexIndexTable[i][0]][0],
                        y + _unitOffsetTable[EdgeVertexIndexTable[i][0]][1],
                        z + _unitOffsetTable[EdgeVertexIndexTable[i][0]][2]
                      };
                    var direction = Math.Abs((int)directionTable[i]) - 1;
                    var sign = directionTable[i] > 0 ? 1 : -1;
                    if (vertexDictionaries[direction].ContainsKey(identifier))
                        EdgeVertex[i] = vertexDictionaries[direction][identifier];
                    else
                    {
                        var coord = vertexCoord.Cast<double>().ToArray();
                        coord[direction] = coord[direction] + sign * offset;
                        EdgeVertex[i] = new Vertex(coord);
                        vertexDictionaries[direction].Add(identifier, EdgeVertex[i]);
                    }
                }
                edgeFlags >>= 1;
            }
            return cubeType;

        }

    }
}
