// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MarchingCubes.Implicit.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class MarchingCubesImplicit.
    /// Implements the <see cref="TVGL.MarchingCubes{TVGL.ImplicitSolid, System.Double}" />
    /// </summary>
    /// <seealso cref="TVGL.MarchingCubes{TVGL.ImplicitSolid, System.Double}" />
    internal class MarchingCubesImplicit : MarchingCubes<ImplicitSolid, double>
    {
        private readonly PrimitiveSurface surface;

        /// <summary>
        /// The surface level
        /// </summary>
        private readonly double surfaceLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarchingCubesImplicit"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="discretization">The discretization.</param>
        internal MarchingCubesImplicit(ImplicitSolid solid, double discretization)
            : base(solid, discretization)
        {
            if (solid.Primitives.Count > 1) throw new NotImplementedException(
                "This method has been rewritten for only a single surface. It has yet to be updated to multiple surfaces and " +
                "CSG tree. Refer back to code from June 2025 for a method that handles trees (although it is slow).");
            surface = solid.Primitives[0];
            surfaceLevel = solid.SurfaceLevel;
        }

        /// <summary>
        /// Gets the value from solid.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>ValueT.</returns>
        protected override double GetValueFromSolid(int x, int y, int z)
        {
            //return 0;
            return solid[
                  _xMin + x * gridToCoordinateFactor,
                            _yMin + y * gridToCoordinateFactor,
                            _zMin + z * gridToCoordinateFactor
                ];
        }

        /// <summary>
        /// Determines whether the specified v is inside.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if the specified v is inside; otherwise, <c>false</c>.</returns>
        protected override bool IsInside(double v)
        {
            return v <= surfaceLevel;
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sign">The sign.</param>
        /// <returns>System.Double.</returns>
        protected override double GetOffset(StoredValue<double> from, StoredValue<double> to,
            int direction)
        {
            if (from.Value.IsPracticallySame(surfaceLevel)) return 0.0;
            if (to.Value.IsPracticallySame(surfaceLevel)) return gridToCoordinateFactor;
            if (to.Value.IsPracticallySame(from.Value)) return gridToCoordinateFactor / 2;
            return gridToCoordinateFactor * (surfaceLevel - from.Value) / (to.Value - from.Value);
        }


        internal override TessellatedSolid Generate()
        {
            FindZPointFromXandY();
            FindXPointFromYandZ();
            FindYPointFromXandZ();
            var ids = new HashSet<long>(vertexDictionaries.SelectMany(vertexDictionary => vertexDictionary.Keys));
            foreach (var id in ids)
            {
                var (xIndex, yIndex, zIndex) = getIndicesFromIdentifier(id);
                if (xIndex + 1 != numGridX && yIndex + 1 != numGridY && zIndex + 1 != numGridZ)
                    MakeFacesInCube(xIndex, yIndex, zIndex);
            }
            //var comments = new List<string>(solid.Comments
            //    .Concat("tessellation (via marching cubes) of the implicit solid, " + solid.Name));
            for (int i = 0; i < faces.Count; i++)
                faces[i].IndexInList = i;
            if (faces.Count == 0)
                return new TessellatedSolid();
            return new TessellatedSolid(faces, buildOptions:TessellatedSolidBuildOptions.Minimal);
        }


        /// <summary>
        /// MakeFacesInCube is the main/difficult function in the Marching Cubes algorithm
        /// </summary>
        /// <param name="xIndex">Index of the x.</param>
        /// <param name="yIndex">Index of the y.</param>
        /// <param name="zIndex">Index of the z.</param>
        protected override void MakeFacesInCube(int xIndex, int yIndex, int zIndex)
        {
            // first solve for the eight values at the vertices of the cubes. The "GetValue" function
            // will either grab the value from the StoredValues or will invoke the "GetValueFromSolid"
            // which is a necessary function of inherited classes. For each one of the eight that is
            // inside the solid, the cubeType is updated to reflect this. Each of the eight bits in the
            // byte will correspond to the "inside" or "outside" of the vertex.
            int cubeType = 0;
            var cube = new StoredValue<double>[8];
            //Find which vertices are inside of the surface and which are outside
            for (var i = 0; i < 8; i++)
            {
                var thisX = xIndex + _unitOffsetTable[i][0];
                var thisY = yIndex + _unitOffsetTable[i][1];
                var thisZ = zIndex + _unitOffsetTable[i][2];
                var id = getIdentifier((int)thisX, (int)thisY, (int)thisZ);
                var v = cube[i] = GetValue((int)thisX, (int)thisY, (int)thisZ, id);
                if (!IsInside(v.Value))
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
                    var direction = (int)directionTable[i] - 1;
                    var fromCorner = cube[EdgeCornerIndexTable[i][0]];
                    var toCorner = cube[EdgeCornerIndexTable[i][1]];
                    if (vertexDictionaries[direction].TryGetValue(fromCorner.ID, out var value))
                        EdgeVertex[i] = value;
                    else
                    {
                        //return;
                        var coord = new Vector3(
                           _xMin + fromCorner.X * gridToCoordinateFactor,
                            _yMin + fromCorner.Y * gridToCoordinateFactor,
                            _zMin + fromCorner.Z * gridToCoordinateFactor);
                        var offSetUnitVector = (direction == 0) ? Vector3.UnitX :
                            (direction == 1) ? Vector3.UnitY : Vector3.UnitZ;
                        double offset = GetOffset(fromCorner, toCorner, direction);
                        coord = coord + (offSetUnitVector * offset);
                        EdgeVertex[i] = new Vertex(coord);
                        vertexDictionaries[direction].Add(fromCorner.ID, EdgeVertex[i]);
                    }
                }
                edgeFlags >>= 1;
            }
            //now the triangular faces are created that connect the vertices identified above.
            //based on the ones that were found. There can be up to five per cube
            var faceVertices = new Vertex[3];
            for (var i = 0; i < NumFacesTable[cubeType]; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var vertexIndex = FaceVertexIndicesTable[cubeType][3 * i + j];
                    faceVertices[j] = EdgeVertex[vertexIndex];
                }
                faces.Add(new TriangleFace(faceVertices));
            }
        }



        private void FindZPointFromXandY()
        {
            for (var i = 0; i < numGridX; i++)
                for (var j = 0; j < numGridY; j++)
                {
                    var anchor = new Vector3(_xMin + i * gridToCoordinateFactor,
                              _yMin + j * gridToCoordinateFactor, 0.0);
                    var intersectionEnumerator = surface.LineIntersection(anchor, Vector3.UnitZ).GetEnumerator();
                    if (!intersectionEnumerator.MoveNext()) continue;
                    (Vector3 p1, _) = intersectionEnumerator.Current;
                    if (p1.IsNull()) continue;
                    if (p1.Z < _zMin || p1.Z > _zMax)
                    {
                        if (!intersectionEnumerator.MoveNext()) continue;
                        (p1, _) = intersectionEnumerator.Current;
                        if (p1.Z < _zMin || p1.Z > _zMax)
                            continue;
                    }
                    var posDir1 = surface.GetNormalAtPoint(p1).Z > 0;
                    var zLowIndex1 = (int)((p1.Z - _zMin) * coordToGridFactor);
                    var p2 = Vector3.Null;
                    var hashID = getIdentifier(i, j, zLowIndex1);
                    if (intersectionEnumerator.MoveNext())
                    {
                        (p2, _) = intersectionEnumerator.Current;
                        if (p2.Z < _zMin || p2.Z > _zMax)
                            p2 = Vector3.Null;
                    }
                    if (p2.IsNull())
                    {
                        vertexDictionaries[2].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = i,
                            Y = j,
                            Z = zLowIndex1,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (zLowIndex1 != numGridZ - 1)
                        {
                            hashID += zMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = i,
                                Y = j,
                                Z = zLowIndex1 + 1,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                    else
                    {
                        var posDir2 = surface.GetNormalAtPoint(p2).Z > 0;
                        var zLowIndex2 = (int)((p2.Z - _zMin) * coordToGridFactor);
                        if (zLowIndex1 == zLowIndex2 || (posDir1 == posDir2 &&
                            (zLowIndex1 + 1 == zLowIndex2 || zLowIndex1 == zLowIndex2 + 1)))
                            continue; // can't have two vertices at the same z level or adjacent z levels

                        vertexDictionaries[2].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = i,
                            Y = j,
                            Z = zLowIndex1,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (zLowIndex1 != numGridZ - 1)
                        {
                            hashID += zMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = i,
                                Y = j,
                                Z = zLowIndex1 + 1,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                        hashID = getIdentifier(i, j, zLowIndex2);
                        vertexDictionaries[2].Add(hashID, new Vertex(p2));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir2 ? -1 : 1,
                            X = i,
                            Y = j,
                            Z = zLowIndex2,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (zLowIndex2 != numGridZ - 1)
                        {
                            hashID += zMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir2 ? 1 : -1,
                                X = i,
                                Y = j,
                                Z = zLowIndex2 + 1,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                }
        }

        private void FindYPointFromXandZ()
        {
            for (var i = 0; i < numGridX; i++)
                for (var k = 0; k < numGridZ; k++)
                {
                    var anchor = new Vector3(_xMin + i * gridToCoordinateFactor,
                              0.0, _zMin + k * gridToCoordinateFactor);
                    var intersectionEnumerator = surface.LineIntersection(anchor, Vector3.UnitY).GetEnumerator();
                    if (!intersectionEnumerator.MoveNext()) continue;
                    (Vector3 p1, _) = intersectionEnumerator.Current;
                    if (p1.IsNull()) continue;
                    if (p1.Y < _yMin || p1.Y > _yMax)
                    {
                        if (!intersectionEnumerator.MoveNext()) continue;
                        (p1, _) = intersectionEnumerator.Current;
                        if (p1.Y < _yMin || p1.Y > _yMax)
                            continue;
                    }
                    var posDir1 = surface.GetNormalAtPoint(p1).Y > 0;
                    var yLowIndex1 = (int)((p1.Y - _yMin) * coordToGridFactor);
                    var p2 = Vector3.Null;
                    var hashID = getIdentifier(i, yLowIndex1, k);
                    if (intersectionEnumerator.MoveNext())
                    {
                        (p2, _) = intersectionEnumerator.Current;
                        if (p2.Y < _yMin || p2.Y > _yMax)
                            p2 = Vector3.Null;
                    }
                    if (p2.IsNull())
                    {
                        vertexDictionaries[1].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = i,
                            Y = yLowIndex1,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (yLowIndex1 != numGridY - 1)
                        {
                            hashID += yMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = i,
                                Y = yLowIndex1 + 1,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                    else
                    {
                        var posDir2 = surface.GetNormalAtPoint(p2).Y > 0;
                        var yLowIndex2 = (int)((p2.Y - _yMin) * coordToGridFactor);
                        if (yLowIndex1 == yLowIndex2 || (posDir1 == posDir2 &&
                            (yLowIndex1 + 1 == yLowIndex2 || yLowIndex1 == yLowIndex2 + 1)))
                            continue; // can't have two vertices at the same y level or adjacent y levels

                        vertexDictionaries[1].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = i,
                            Y = yLowIndex1,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (yLowIndex1 != numGridY - 1)
                        {
                            hashID += yMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = i,
                                Y = yLowIndex1 + 1,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                        hashID = getIdentifier(i, yLowIndex2, k);
                        vertexDictionaries[1].Add(hashID, new Vertex(p2));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir2 ? -1 : 1,
                            X = i,
                            Y = yLowIndex2,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (yLowIndex2 != numGridY - 1)
                        {
                            hashID += yMultiplier;
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir2 ? 1 : -1,
                                X = i,
                                Y = yLowIndex2 + 1,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                }
        }

        private void FindXPointFromYandZ()
        {
            for (var j = 0; j < numGridY; j++)
                for (var k = 0; k < numGridZ; k++)
                {
                    var anchor = new Vector3(0.0, _yMin + j * gridToCoordinateFactor,
                              _zMin + k * gridToCoordinateFactor);
                    var intersectionEnumerator = surface.LineIntersection(anchor, Vector3.UnitX).GetEnumerator();
                    if (!intersectionEnumerator.MoveNext()) continue;
                    (Vector3 p1, _) = intersectionEnumerator.Current;
                    if (p1.IsNull()) continue;
                    if (p1.X < _xMin || p1.X > _xMax)
                    {
                        if (!intersectionEnumerator.MoveNext()) continue;
                        (p1, _) = intersectionEnumerator.Current;
                        if (p1.X < _xMin || p1.X > _xMax)
                            continue;
                    }
                    var posDir1 = surface.GetNormalAtPoint(p1).X > 0;
                    var xLowIndex1 = (int)((p1.X - _xMin) * coordToGridFactor);
                    var p2 = Vector3.Null;
                    var hashID = getIdentifier(xLowIndex1, j, k);
                    if (intersectionEnumerator.MoveNext())
                    {
                        (p2, _) = intersectionEnumerator.Current;
                        if (p2.X < _xMin || p2.X > _xMax)
                            p2 = Vector3.Null;
                    }
                    if (p2.IsNull())
                    {
                        vertexDictionaries[0].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = xLowIndex1,
                            Y = j,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (xLowIndex1 != numGridX - 1)
                        {
                            hashID += 1; // x multiplier is 1
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = xLowIndex1 + 1,
                                Y = j,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                    else
                    {
                        var posDir2 = surface.GetNormalAtPoint(p2).X > 0;
                        var xLowIndex2 = (int)((p2.X - _xMin) * coordToGridFactor);
                        if (xLowIndex1 == xLowIndex2 || (posDir1 == posDir2 &&
                            (xLowIndex1 + 1 == xLowIndex2 || xLowIndex1 == xLowIndex2 + 1)))
                            continue; // can't have two vertices at the same x level or adjacent x levels

                        vertexDictionaries[0].Add(hashID, new Vertex(p1));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir1 ? -1 : 1,
                            X = xLowIndex1,
                            Y = j,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (xLowIndex1 != numGridX - 1)
                        {
                            hashID += 1; // x multiplier is 1
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir1 ? 1 : -1,
                                X = xLowIndex1 + 1,
                                Y = j,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                        hashID = getIdentifier(xLowIndex2, j, k);
                        vertexDictionaries[0].Add(hashID, new Vertex(p2));
                        valueDictionary.TryAdd(hashID, new StoredValue<double>
                        {
                            Value = posDir2 ? -1 : 1,
                            X = xLowIndex2,
                            Y = j,
                            Z = k,
                            NumTimesCalled = 0,
                            ID = hashID
                        });
                        if (xLowIndex2 != numGridX - 1)
                        {
                            hashID += 1; // x multiplier is 1
                            valueDictionary.TryAdd(hashID, new StoredValue<double>
                            {
                                Value = posDir2 ? 1 : -1,
                                X = xLowIndex2 + 1,
                                Y = j,
                                Z = k,
                                NumTimesCalled = 0,
                                ID = hashID
                            });
                        }
                    }
                }
        }
    }
}