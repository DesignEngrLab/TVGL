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
            for (var i = 0; i < numGridX - 1; i++)
                for (var j = 0; j < numGridY - 1; j++)
                    for (var k = 0; k < numGridZ - 1; k++)
                        MakeFacesInCube(i, j, k);
            var comments = new List<string>(solid.Comments)
            {
                "tessellation (via marching cubes) of the voxelized solid, " + solid.Name
            };
            for (int i = 0; i < faces.Count; i++)
                faces[i].IndexInList = i;
            if (faces.Count == 0)
                return new TessellatedSolid();
            return new TessellatedSolid(faces);
            // vertexDictionaries.SelectMany(d => d.Values), false,
            //new[] { solid.SolidColor }, solid.Units, solid.Name + "TS", solid.FileName, comments, solid.Language);
        }

        private void FindZPointFromXandY()
        {
            for (var i = 0; i < numGridX - 1; i++)
                for (var j = 0; j < numGridY - 1; j++)
                {
                    var anchor = new Vector3(_xMin + i * gridToCoordinateFactor,
                              _yMin + j * gridToCoordinateFactor, 0.0);
                    var intersectionEnumerator = surface.LineIntersection(anchor, Vector3.UnitZ).GetEnumerator();
                    if (!intersectionEnumerator.MoveNext()) continue;
                    (Vector3 p1, _) = intersectionEnumerator.Current;
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
                                Value = posDir1 ? 1 : -1,
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
        }
        private void FindXPointFromYandZ()
        {
        }
    }
}