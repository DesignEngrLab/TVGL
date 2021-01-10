// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL
{
    internal class StoredValue<ValueT>
    {
        internal ValueT Value;
        internal int X;
        internal int Y;
        internal int Z;
        internal int NumTimesCalled;
        internal long ID;
    }

    internal abstract class MarchingCubes<SolidT, ValueT>
        where SolidT : Solid
        // where double and ValueT are numbers or bool
    {
        #region Constructor

        protected MarchingCubes(SolidT solid, double discretization)
        {
            this.solid = solid;
            this.gridToCoordinateFactor = discretization;
            this.coordToGridFactor = 1 / gridToCoordinateFactor;
            var buffer = discretization * fractionOfGridToExpand;
            _xMin = solid.XMin - buffer;
            _yMin = solid.YMin - buffer;
            _zMin = solid.ZMin - buffer;
            var _xMax = solid.XMax + buffer;
            var _yMax = solid.YMax + buffer;
            var _zMax = solid.ZMax + buffer;
            numGridX = (int)Math.Ceiling((_xMax - _xMin) / discretization) + 1;
            numGridY = (int)Math.Ceiling((_yMax - _yMin) / discretization) + 1;
            numGridZ = (int)Math.Ceiling((_zMax - _zMin) / discretization) + 1;
            yMultiplier = numGridX;
            zMultiplier = numGridX * numGridY;

            vertexDictionaries = new[] {
                new Dictionary<long, Vertex>(),
                new Dictionary<long, Vertex>(),
                new Dictionary<long, Vertex>()
            };
            valueDictionary = new Dictionary<long, StoredValue<ValueT>>();
            faces = new List<PolygonalFace>();
            GridOffsetTable = new Vector3[8];
            for (int i = 0; i < 8; i++)
                GridOffsetTable[i] = _unitOffsetTable[i] * this.gridToCoordinateFactor;
        }

        #endregion Constructor

        #region Fields

        private readonly Dictionary<long, Vertex>[] vertexDictionaries;
        protected readonly SolidT solid;
        protected readonly double gridToCoordinateFactor;
        protected readonly double coordToGridFactor;
        protected readonly Vector3[] GridOffsetTable;
        private readonly Dictionary<long, StoredValue<ValueT>> valueDictionary;
        protected readonly List<PolygonalFace> faces;
        protected const double fractionOfGridToExpand = 0.05;

        #region to be assigned in inherited constructor

        protected int numGridX, numGridY, numGridZ;
        protected double _xMin, _yMin, _zMin;
        protected int yMultiplier;
        protected int zMultiplier;

        #endregion to be assigned in inherited constructor

        #endregion Fields

        #region Abstract Methods

        protected abstract bool IsInside(ValueT v);

        protected abstract ValueT GetValueFromSolid(int x, int y, int z);

        protected abstract double GetOffset(StoredValue<ValueT> from, StoredValue<ValueT> to,
            int direction, int sign);

        #endregion Abstract Methods

        #region Main Methods

        internal virtual TessellatedSolid Generate()
        {
            for (var i = 0; i < numGridX - 1; i++)
                for (var j = 0; j < numGridY - 1; j++)
                    for (var k = 0; k < numGridZ - 1; k++)
                        MakeFacesInCube(i, j, k);
            var comments = new List<string>(solid.Comments)
            {
                "tessellation (via marching cubes) of the voxelized solid, " + solid.Name
            };
            return new TessellatedSolid(faces, false, false);
            // vertexDictionaries.SelectMany(d => d.Values), false,
            //new[] { solid.SolidColor }, solid.Units, solid.Name + "TS", solid.FileName, comments, solid.Language);
        }

        protected long getIdentifier(int x, int y, int z)
        {
            return x + (long)(yMultiplier * y) + zMultiplier * z;
        }

        protected StoredValue<ValueT> GetValue(int x, int y, int z, long identifier)
        {
            if (valueDictionary.ContainsKey(identifier))
            {
                var prevValue = valueDictionary[identifier];
                if (prevValue.NumTimesCalled < 7)
                    prevValue.NumTimesCalled++;
                else valueDictionary.Remove(identifier);
                return prevValue;
            }
            var newValue = new StoredValue<ValueT>
            {
                Value = GetValueFromSolid(x, y, z),
                X = x,
                Y = y,
                Z = z,
                NumTimesCalled = 1,
                ID = identifier
            };
            valueDictionary.Add(identifier, newValue);
            return newValue;
        }

        /// <summary>
        /// MakeFacesInCube is the main/difficult function in the Marching Cubes algorithm 
        /// </summary>
        protected void MakeFacesInCube(int xIndex, int yIndex, int zIndex)
        {
            // first solve for the eight values at the vertices of the cubes. The "GetValue" function
            // will either grab the value from the StoredValues or will invoke the "GetValueFromSolid"
            // which is a necessary function of inherited classes. For each one of the eight that is
            // inside the solid, the cubeType is updated to reflect this. Each of the eight bits in the
            // byte will correspond to the "inside" or "outside" of the vertex.
            int cubeType = 0;
            var cube = new StoredValue<ValueT>[8];
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
            {
                var thisX = xIndex + _unitOffsetTable[i][0];
                var thisY = yIndex + _unitOffsetTable[i][1];
                var thisZ = zIndex + _unitOffsetTable[i][2];
                var id = getIdentifier((int)thisX, (int)thisY, (int)thisZ);
                var v = cube[i] = GetValue((int)thisX, (int)thisY, (int)thisZ, id);
                if (IsInside(v.Value))
                    cubeType |= 1 << i;
            }
            // Based upon the cubeType, the CubeEdgeFlagsTable will tell us which of the 12 edges of the cube
            // intersect with the surface of the solid
            int edgeFlags = CubeEdgeFlagsTable[cubeType];

            //If the cube is entirely inside or outside of the surface, then there will be no intersections
            if (edgeFlags == 0) return;
            var EdgeVertex = new Vertex[12];
            //this loop creates or retrieves the vertices that are on the edges of the
            //marching cube. These are stored in the EdgeVertexIndexTable
            for (var i = 0; i < 12; i++)
            {
                //if there is an intersection on this edge
                if ((edgeFlags & 1) != 0)
                {
                    var direction = Math.Abs((int)directionTable[i]) - 1;
                    var sign = directionTable[i] > 0 ? 1 : -1;
                    var fromCorner = cube[EdgeCornerIndexTable[i][0]];
                    var toCorner = cube[EdgeCornerIndexTable[i][1]];
                    // var id = fromCorner.ID ;
                    var id = sign > 0 ? fromCorner.ID : toCorner.ID;
                    if (vertexDictionaries[direction].ContainsKey(id))
                        EdgeVertex[i] = vertexDictionaries[direction][id];
                    else
                    {
                        var coord = new Vector3(
                           _xMin + fromCorner.X * gridToCoordinateFactor,
                            _yMin + fromCorner.Y * gridToCoordinateFactor,
                            _zMin + fromCorner.Z * gridToCoordinateFactor);
                        var offSetUnitVector = (direction == 0) ? Vector3.UnitX :
                            (direction == 1) ? Vector3.UnitY : Vector3.UnitZ;
                        double offset = GetOffset(fromCorner, toCorner, direction, sign);
                        coord = coord + (offSetUnitVector * sign * offset);
                        EdgeVertex[i] = new Vertex(coord);
                        vertexDictionaries[direction].Add(id, EdgeVertex[i]);
                    }
                }
                edgeFlags >>= 1;
            }
            //now the triangular faces are created that connect the vertices identified above.
            //based on the les that were found. There can be up to five per cube
            var faceVertices = new Vertex[3];
            for (var i = 0; i < NumFacesTable[cubeType]; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var vertexIndex = FaceVertexIndicesTable[cubeType][3 * i + j];
                    faceVertices[j] = EdgeVertex[vertexIndex];
                }
                faces.Add(new PolygonalFace(faceVertices));
            }
        }

        #endregion Main Methods

        #region Static Tables

        /// <summary>
        /// VertexOffset lists the positions, relative to vertex0,
        /// of each of the 8 vertices of a cube.
        /// vertexOffset[8][3]
        /// </summary>
        protected static readonly Vector3[] _unitOffsetTable = new[]
        {
            new Vector3(0, 0, 0),new Vector3(1, 0, 0),new Vector3(1, 1, 0),new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),new Vector3(1, 0, 1),new Vector3(1, 1, 1),new Vector3(0, 1, 1)
        };

        /// <summary>
        /// For any edge, if one vertex is inside of the surface and the other
        /// is outside of the surface then the edge intersects the surface.
        /// For each of the 8 vertices of the cube can be two possible states,
        /// either inside or outside of the surface.
        /// For any cube the are 2^8=256 possible sets of vertex states.
        /// This table lists the edges intersected by the surface for all 256
        /// possible vertex states. There are 12 edges.
        /// For each entry in the table, if edge #n is intersected, then bit #n is set to 1.
        /// So, each entry uses the first 12 bits of the byte - hence there are 3 hexadecimcal numbers
        /// (as each hexadecimal is 4 bits)
        /// cubeEdgeFlags[256]
        /// </summary>
        protected static readonly int[] CubeEdgeFlagsTable = new int[]
        {
        0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c, 0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c, 0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c, 0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac, 0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c, 0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc, 0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c, 0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc, 0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc, 0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c, 0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc, 0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c, 0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac, 0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c, 0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c, 0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c, 0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
        };

        /// <summary>
        /// EdgeVertexIndexTable lists the index of the endpoint vertices for each
        /// of the 12 edges of the cube.
        /// edgeConnection[12][2]
        /// </summary>
        protected static readonly int[][] EdgeCornerIndexTable = new int[][]
        {
            new[]{0,1}, new[]{1,2}, new[]{3,2}, new[]{0,3},
            new[]{4,5}, new[]{5,6}, new[]{7,6}, new[]{4,7},
            new[]{0,4}, new[]{1,5}, new[]{2,6}, new[]{3,7}
        };

        /// <summary>
        /// EdgeDirectionTable lists the direction vector (vertexFrom-vertexTo) for each edge in the cube.
        /// edgeDirection[12][3]
        /// </summary>
        protected static readonly Vector3[] EdgeDirectionTable = new Vector3[]
        {
            Vector3.UnitX, Vector3.UnitY, Vector3.UnitX , Vector3.UnitY,
            Vector3.UnitX, Vector3.UnitY, Vector3.UnitX , Vector3.UnitY,
            Vector3.UnitZ,Vector3.UnitZ,Vector3.UnitZ,Vector3.UnitZ
        };

        protected static readonly CartesianDirections[] directionTable = new CartesianDirections[]
          {
              CartesianDirections.XPositive,CartesianDirections.YPositive,
              CartesianDirections.XPositive,CartesianDirections.YPositive,
              CartesianDirections.XPositive,CartesianDirections.YPositive,
              //CartesianDirections.XNegative,CartesianDirections.YNegative,
              CartesianDirections.XPositive,CartesianDirections.YPositive,
              //CartesianDirections.XNegative,CartesianDirections.YNegative,
              CartesianDirections.ZPositive, CartesianDirections.ZPositive,
              CartesianDirections.ZPositive,CartesianDirections.ZPositive
          };

        /// <summary>
        /// For each of the possible vertex states listed in cubeEdgeFlags there is a specific triangulation
        /// of the edge intersection points.  triangleConnectionTable lists all of them in the form of
        /// 0-5 edge triples .
        /// For example: FaceVertexIndicesTable[3] list the 2 triangles formed when corner[0]
        /// and corner[1] are inside of the surface, but the rest of the cube is not.
        /// triangleConnectionTable[256][16]
        /// </summary>
        protected static readonly int[][] FaceVertexIndicesTable = new int[][]
        {
            new int[0], //0
            new[]{0, 8, 3}, //1
            new[]{0, 1, 9}, //2
            new[]{1, 8, 3, 9, 8, 1}, //3
            new[]{1, 2, 10},
            new[]{0, 8, 3, 1, 2, 10},
        new[]{9, 2, 10, 0, 2, 9},
        new[]{2, 8, 3, 2, 10, 8, 10, 9, 8},
        new[]{3, 11, 2},
        new[]{0, 11, 2, 8, 11, 0},
        new[]{1, 9, 0, 2, 3, 11},
        new[]{1, 11, 2, 1, 9, 11, 9, 8, 11},
        new[]{3, 10, 1, 11, 10, 3},
        new[]{0, 10, 1, 0, 8, 10, 8, 11, 10},
        new[]{3, 9, 0, 3, 11, 9, 11, 10, 9},
        new[]{9, 8, 10, 10, 8, 11},
        new[]{4, 7, 8},
        new[]{4, 3, 0, 7, 3, 4},
        new[]{0, 1, 9, 8, 4, 7},
        new[]{4, 1, 9, 4, 7, 1, 7, 3, 1},
        new[]{1, 2, 10, 8, 4, 7},
        new[]{3, 4, 7, 3, 0, 4, 1, 2, 10},
        new[]{9, 2, 10, 9, 0, 2, 8, 4, 7},
        new[]{2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4},
        new[]{8, 4, 7, 3, 11, 2},
        new[]{11, 4, 7, 11, 2, 4, 2, 0, 4},
        new[]{9, 0, 1, 8, 4, 7, 2, 3, 11},
        new[]{4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1},
        new[]{3, 10, 1, 3, 11, 10, 7, 8, 4},
        new[]{1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4},
        new[]{4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3},
        new[]{4, 7, 11, 4, 11, 9, 9, 11, 10},
        new[]{9, 5, 4},
        new[]{9, 5, 4, 0, 8, 3},
        new[]{0, 5, 4, 1, 5, 0},
        new[]{8, 5, 4, 8, 3, 5, 3, 1, 5},
        new[]{1, 2, 10, 9, 5, 4},
        new[]{3, 0, 8, 1, 2, 10, 4, 9, 5},
        new[]{5, 2, 10, 5, 4, 2, 4, 0, 2},
        new[]{2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8},
        new[]{9, 5, 4, 2, 3, 11},
        new[]{0, 11, 2, 0, 8, 11, 4, 9, 5},
        new[]{0, 5, 4, 0, 1, 5, 2, 3, 11},
        new[]{2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5},
        new[]{10, 3, 11, 10, 1, 3, 9, 5, 4},
        new[]{4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10},
        new[]{5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3},
        new[]{5, 4, 8, 5, 8, 10, 10, 8, 11},
        new[]{9, 7, 8, 5, 7, 9},
        new[]{9, 3, 0, 9, 5, 3, 5, 7, 3},
        new[]{0, 7, 8, 0, 1, 7, 1, 5, 7},
        new[]{1, 5, 3, 3, 5, 7},
        new[]{9, 7, 8, 9, 5, 7, 10, 1, 2},
        new[]{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3},
        new[]{8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2},
        new[]{2, 10, 5, 2, 5, 3, 3, 5, 7},
        new[]{7, 9, 5, 7, 8, 9, 3, 11, 2},
        new[]{9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11},
        new[]{2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7},
        new[]{11, 2, 1, 11, 1, 7, 7, 1, 5},
        new[]{9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11},
        new[]{5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0},
        new[]{11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0},
        new[]{11, 10, 5, 7, 11, 5},
        new[]{10, 6, 5},
        new[]{0, 8, 3, 5, 10, 6},
        new[]{9, 0, 1, 5, 10, 6},
        new[]{1, 8, 3, 1, 9, 8, 5, 10, 6},
        new[]{1, 6, 5, 2, 6, 1},
        new[]{1, 6, 5, 1, 2, 6, 3, 0, 8},
        new[]{9, 6, 5, 9, 0, 6, 0, 2, 6},
        new[]{5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8},
        new[]{2, 3, 11, 10, 6, 5},
        new[]{11, 0, 8, 11, 2, 0, 10, 6, 5},
        new[]{0, 1, 9, 2, 3, 11, 5, 10, 6},
        new[]{5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11},
        new[]{6, 3, 11, 6, 5, 3, 5, 1, 3},
        new[]{0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6},
        new[]{3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9},
        new[]{6, 5, 9, 6, 9, 11, 11, 9, 8},
        new[]{5, 10, 6, 4, 7, 8},
        new[]{4, 3, 0, 4, 7, 3, 6, 5, 10},
        new[]{1, 9, 0, 5, 10, 6, 8, 4, 7},
        new[]{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4},
        new[]{6, 1, 2, 6, 5, 1, 4, 7, 8},
        new[]{1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7},
        new[]{8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6},
        new[]{7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9},
        new[]{3, 11, 2, 7, 8, 4, 10, 6, 5},
        new[]{5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11},
        new[]{0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6},
        new[]{9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6},
        new[]{8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6},
        new[]{5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11},
        new[]{0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7},
        new[]{6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9},
        new[]{10, 4, 9, 6, 4, 10},
        new[]{4, 10, 6, 4, 9, 10, 0, 8, 3},
        new[]{10, 0, 1, 10, 6, 0, 6, 4, 0},
        new[]{8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10},
        new[]{1, 4, 9, 1, 2, 4, 2, 6, 4},
        new[]{3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4},
        new[]{0, 2, 4, 4, 2, 6},
        new[]{8, 3, 2, 8, 2, 4, 4, 2, 6},
        new[]{10, 4, 9, 10, 6, 4, 11, 2, 3},
        new[]{0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6},
        new[]{3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10},
        new[]{6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1},
        new[]{9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3},
        new[]{8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1},
        new[]{3, 11, 6, 3, 6, 0, 0, 6, 4},
        new[]{6, 4, 8, 11, 6, 8},
        new[]{7, 10, 6, 7, 8, 10, 8, 9, 10},
        new[]{0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10},
        new[]{10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0},
        new[]{10, 6, 7, 10, 7, 1, 1, 7, 3},
        new[]{1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7},
        new[]{2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9},
        new[]{7, 8, 0, 7, 0, 6, 6, 0, 2},
        new[]{7, 3, 2, 6, 7, 2},
        new[]{2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7},
        new[]{2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7},
        new[]{1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11},
        new[]{11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1},
        new[]{8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6},
        new[]{0, 9, 1, 11, 6, 7},
        new[]{7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0},
        new[]{7, 11, 6},
        new[]{7, 6, 11},
        new[]{3, 0, 8, 11, 7, 6},
        new[]{0, 1, 9, 11, 7, 6},
        new[]{8, 1, 9, 8, 3, 1, 11, 7, 6},
        new[]{10, 1, 2, 6, 11, 7},
        new[]{1, 2, 10, 3, 0, 8, 6, 11, 7},
        new[]{2, 9, 0, 2, 10, 9, 6, 11, 7},
        new[]{6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8},
        new[]{7, 2, 3, 6, 2, 7},
        new[]{7, 0, 8, 7, 6, 0, 6, 2, 0},
        new[]{2, 7, 6, 2, 3, 7, 0, 1, 9},
        new[]{1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6},
        new[]{10, 7, 6, 10, 1, 7, 1, 3, 7},
        new[]{10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8},
        new[]{0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7},
        new[]{7, 6, 10, 7, 10, 8, 8, 10, 9},
        new[]{6, 8, 4, 11, 8, 6},
        new[]{3, 6, 11, 3, 0, 6, 0, 4, 6},
        new[]{8, 6, 11, 8, 4, 6, 9, 0, 1},
        new[]{9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6},
        new[]{6, 8, 4, 6, 11, 8, 2, 10, 1},
        new[]{1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6},
        new[]{4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9},
        new[]{10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3},
        new[]{8, 2, 3, 8, 4, 2, 4, 6, 2},
        new[]{0, 4, 2, 4, 6, 2},
        new[]{1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8},
        new[]{1, 9, 4, 1, 4, 2, 2, 4, 6},
        new[]{8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1},
        new[]{10, 1, 0, 10, 0, 6, 6, 0, 4},
        new[]{4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3},
        new[]{10, 9, 4, 6, 10, 4},
        new[]{4, 9, 5, 7, 6, 11},
        new[]{0, 8, 3, 4, 9, 5, 11, 7, 6},
        new[]{5, 0, 1, 5, 4, 0, 7, 6, 11},
        new[]{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5},
        new[]{9, 5, 4, 10, 1, 2, 7, 6, 11},
        new[]{6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5},
        new[]{7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2},
        new[]{3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6},
        new[]{7, 2, 3, 7, 6, 2, 5, 4, 9},
        new[]{9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7},
        new[]{3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0},
        new[]{6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8},
        new[]{9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7},
        new[]{1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4},
        new[]{4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10},
        new[]{7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10},
        new[]{6, 9, 5, 6, 11, 9, 11, 8, 9},
        new[]{3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5},
        new[]{0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11},
        new[]{6, 11, 3, 6, 3, 5, 5, 3, 1},
        new[]{1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6},
        new[]{0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10},
        new[]{11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5},
        new[]{6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3},
        new[]{5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2},
        new[]{9, 5, 6, 9, 6, 0, 0, 6, 2},
        new[]{1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8},
        new[]{1, 5, 6, 2, 1, 6},
        new[]{1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6},
        new[]{10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0},
        new[]{0, 3, 8, 5, 6, 10},
        new[]{10, 5, 6},
        new[]{11, 5, 10, 7, 5, 11},
        new[]{11, 5, 10, 11, 7, 5, 8, 3, 0},
        new[]{5, 11, 7, 5, 10, 11, 1, 9, 0},
        new[]{10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1},
        new[]{11, 1, 2, 11, 7, 1, 7, 5, 1},
        new[]{0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11},
        new[]{9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7},
        new[]{7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2},
        new[]{2, 5, 10, 2, 3, 5, 3, 7, 5},
        new[]{8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5},
        new[]{9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2},
        new[]{9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2},
        new[]{1, 3, 5, 3, 7, 5},
        new[]{0, 8, 7, 0, 7, 1, 1, 7, 5},
        new[]{9, 0, 3, 9, 3, 5, 5, 3, 7},
        new[]{9, 8, 7, 5, 9, 7},
        new[]{5, 8, 4, 5, 10, 8, 10, 11, 8},
        new[]{5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0},
        new[]{0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5},
        new[]{10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4},
        new[]{2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8},
        new[]{0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11},
        new[]{0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5},
        new[]{9, 4, 5, 2, 11, 3},
        new[]{2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4},
        new[]{5, 10, 2, 5, 2, 4, 4, 2, 0},
        new[]{3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9},
        new[]{5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2},
        new[]{8, 4, 5, 8, 5, 3, 3, 5, 1},
        new[]{0, 4, 5, 1, 0, 5},
        new[]{8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5},
        new[]{9, 4, 5},
        new[]{4, 11, 7, 4, 9, 11, 9, 10, 11},
        new[]{0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11},
        new[]{1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11},
        new[]{3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4},
        new[]{4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2},
        new[]{9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3},
        new[]{11, 7, 4, 11, 4, 2, 2, 4, 0},
        new[]{11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4},
        new[]{2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9},
        new[]{9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7},
        new[]{3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10},
        new[]{1, 10, 2, 8, 7, 4},
        new[]{4, 9, 1, 4, 1, 7, 7, 1, 3},
        new[]{4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1},
        new[]{4, 0, 3, 7, 4, 3},
        new[]{4, 8, 7},
        new[]{9, 10, 8, 10, 11, 8},
        new[]{3, 0, 9, 3, 9, 11, 11, 9, 10},
        new[]{0, 1, 10, 0, 10, 8, 8, 10, 11},
        new[]{3, 1, 10, 11, 3, 10},
        new[]{1, 2, 11, 1, 11, 9, 9, 11, 8},
        new[]{3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9},
        new[]{0, 2, 11, 8, 0, 11},
        new[]{3, 2, 11},
        new[]{2, 3, 8, 2, 8, 10, 10, 8, 9},
        new[]{9, 10, 2, 0, 9, 2},
        new[]{2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8},
        new[]{1, 10, 2},
        new[]{1, 3, 8, 9, 1, 8},
        new[]{0, 9, 1},
        new[]{0, 3, 8},
        new int[0]
        };

        /// <summary>
        /// The FaceVertexIndicesTable is a jagged array since the number of triangles differs from
        /// one marching cube to the next, there can be from 0 to 5 triangles. Within the for-loop to
        /// create these triangular faces one needs to check the inner array size or check if a -1
        /// is reached (as was done in the 2D array), but it is both simpler, quicker to store
        /// the number of triangles in this new array and simply find the upper limit for the for-loop.
        /// In short, this is simply the length of the inner arrays of FaceVertexIndicesTable divided by 3.
        /// </summary>
        protected static readonly int[] NumFacesTable = new int[]
        {
        0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 2,
        1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3,
        1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3,
        2, 3, 3, 2, 3, 4, 4, 3, 3, 4, 4, 3, 4, 5, 5, 2,
        1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3,
        2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 4,
        2, 3, 3, 4, 3, 4, 2, 3, 3, 4, 4, 5, 4, 5, 3, 2,
        3, 4, 4, 3, 4, 5, 3, 2, 4, 5, 5, 4, 5, 2, 4, 1,
        1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 3,
        2, 3, 3, 4, 3, 4, 4, 5, 3, 2, 4, 3, 4, 3, 5, 2,
        2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 4,
        3, 4, 4, 3, 4, 5, 5, 4, 4, 3, 5, 2, 5, 4, 2, 1,
        2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 2, 3, 3, 2,
        3, 4, 4, 5, 4, 5, 5, 2, 4, 3, 5, 4, 3, 2, 4, 1,
        3, 4, 4, 5, 4, 5, 3, 4, 4, 5, 5, 2, 3, 4, 2, 1,
        2, 3, 3, 2, 3, 4, 2, 1, 3, 2, 4, 1, 2, 1, 1, 0
        };

        #endregion Static Tables
    }
}