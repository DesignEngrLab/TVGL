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
using System.Linq.Expressions;
using System.Threading.Tasks;
using TVGL.MathOperations;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid" /> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelDiscretization">The voxel discretization.</param>
        /// <param name="onlyDefineBoundary">if set to <c>true</c> [only define boundary].</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, VoxelDiscretization voxelDiscretization,
            bool onlyDefineBoundary = false,
            double[][] bounds = null) : base(ts.Units, ts.Name, "", ts.Comments, ts.Language)
        {
            Discretization = voxelDiscretization;

            #region Setting Up Parameters 

            double longestSide;
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
                dimensions = new double[3];
                for (int i = 0; i < 3; i++)
                    dimensions[i] = Bounds[1][i] - Bounds[0][i];
                longestSide = dimensions.Max();
                longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            }
            else
            {
                // add a small buffer only if no bounds are provided.
                dimensions = new double[3];
                for (int i = 0; i < 3; i++)
                    dimensions[i] = ts.Bounds[1][i] - ts.Bounds[0][i];
                longestSide = dimensions.Max();
                longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
                var delta = longestSide * Constants.fractionOfWhiteSpaceAroundFinestVoxelFactor;
                Bounds[0] = ts.Bounds[0].subtract(new[] { delta, delta, delta });
                Bounds[1] = ts.Bounds[1].add(new[] { delta, delta, delta });
                for (int i = 0; i < 3; i++)
                    dimensions[i] += 2 * delta;
            }

            longestSide = Bounds[1][longestDimensionIndex] - Bounds[0][longestDimensionIndex];
            VoxelSideLengths = new[]
                {longestSide / 16, longestSide / 256, longestSide / 4096, longestSide / 65536, longestSide / 1048576};
            numVoxels = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[discretizationLevel])).ToArray();

            #endregion

            transformedCoordinates = new double[ts.NumberOfVertices][];

            #region Level-0

            var voxelsZeroLevel = new VoxelHashSet(new VoxelComparerCoarse(), this);
            MakeVertexSimulatedCoordinates(ts.Vertices, 0);
            MakeVertexVoxels(ts.Vertices, null, voxelsZeroLevel);
            MakeVoxelsForFacesAndEdges(ts.Faces, null, voxelsZeroLevel);
            var unknownPartials = DefineBottomCoordinateInside(ref voxelsZeroLevel, null);
            if (!onlyDefineBoundary)
                makeVoxelsInInterior(voxelsZeroLevel, null, null, unknownPartials);
            voxelDictionaryLevel0 = voxelsZeroLevel;

            #endregion

            if (discretizationLevel >= 1)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 1);
               // Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial), voxel0 =>
                foreach (var voxel0 in voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial))
                {
                    var voxels = new VoxelHashSet(new VoxelComparerCoarse(), this);
                    MakeVertexVoxels(((VoxelWithTessellationLinks)voxel0).Vertices, voxel0, voxels);
                    MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel0).Faces, voxel0, voxels);
                    unknownPartials = DefineBottomCoordinateInside(ref voxels, null);
                    if (!onlyDefineBoundary)
                        makeVoxelsInInterior(voxels, voxel0, null, unknownPartials);
                    ((Voxel_Level0_Class)voxel0).InnerVoxels[0] = voxels;
                }  //);
            }

            if (discretizationLevel >= 2)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 2);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial),
                    voxel0 =>
                    //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                    {
                        var voxels = new List<IVoxel>();
                        Parallel.ForEach(
                            ((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial),
                            voxel1 =>
                            //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                            {
                                var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                                MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel1, voxelHash);
                                MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel1,
                                    voxelHash);
                                unknownPartials = DefineBottomCoordinateInside(ref voxelHash, ((VoxelWithTessellationLinks)voxel1).Faces);
                                if (!onlyDefineBoundary)
                                    makeVoxelsInInterior(voxelHash, voxel1, null, unknownPartials);
                                lock (voxels) voxels.AddRange(voxelHash);
                            });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[1] =
                            new VoxelHashSet(new VoxelComparerFine(), this, voxels);
                    });
            }

            if (discretizationLevel >= 3)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 3);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial),
                    voxel0 =>
                    //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                    {
                        var voxels = new List<IVoxel>();
                        Parallel.ForEach(
                            ((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial),
                            voxel1 =>
                            //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                            {
                                var voxelsLevel2 = GetChildVoxels(voxel1);
                                Parallel.ForEach(voxelsLevel2, voxel2 =>
                                {
                                    var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                                    MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel2, voxelHash);
                                    MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel2,
                                        voxelHash);
                                    unknownPartials = DefineBottomCoordinateInside(ref voxelHash, ((VoxelWithTessellationLinks)voxel1).Faces);
                                    if (!onlyDefineBoundary)
                                        makeVoxelsInInterior(voxelHash, voxel2, ((VoxelWithTessellationLinks)voxel1).Faces, unknownPartials);
                                    lock (voxels) voxels.AddRange(voxelHash);
                                });
                            });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[2] =
                            new VoxelHashSet(new VoxelComparerFine(), this, voxels);
                    });
            }

            if (discretizationLevel >= 4)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 4);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial),
                    voxel0 =>
                    //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                    {
                        var voxels = new List<IVoxel>();
                        Parallel.ForEach(
                            ((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial),
                            voxel1 =>
                            //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                            {
                                var voxelsLevel2 = GetChildVoxels(voxel1);
                                Parallel.ForEach(voxelsLevel2, voxel2 =>
                                {
                                    var voxelsLevel3 = GetChildVoxels(voxel2);
                                    Parallel.ForEach(voxelsLevel3, voxel3 =>
                                    {
                                        var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                                        MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel3,
                                            voxelHash);
                                        MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel3,
                                            voxelHash);
                                        unknownPartials = DefineBottomCoordinateInside(ref voxelHash, ((VoxelWithTessellationLinks)voxel1).Faces);
                                        if (!onlyDefineBoundary)
                                            makeVoxelsInInterior(voxelHash, voxel3, ((VoxelWithTessellationLinks)voxel1).Faces, unknownPartials);
                                        lock (voxels) voxels.AddRange(voxelHash);
                                    });
                                });
                            });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[3] =
                            new VoxelHashSet(new VoxelComparerFine(), this, voxels);
                    });
            }
            UpdateProperties();
        }


        private void MakeVertexSimulatedCoordinates(IList<Vertex> vertices, int level)
        {
            var s = VoxelSideLengths[level];
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                // transformedCoordinates[i] = vertices[i].Position.subtract(Offset).divide(VoxelSideLengths[level]);
                // or to make a bit faster
                var p = vertices[i].Position;
                transformedCoordinates[i] = new[]
                    {(p[0] - Offset[0]) / s, (p[1] - Offset[1]) / s, (p[2] - Offset[2]) / s};
            }
            );
        }

        private void UpdateVertexSimulatedCoordinates(IList<Vertex> vertices, int level)
        {
            var s = VoxelSideLengths[level];
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                // transformedCoordinates[i] = vertices[i].Position.subtract(Offset).divide(VoxelSideLengths[level]);
                // or to make a bit faster
                var p = vertices[i].Position;
                var t = transformedCoordinates[i];
                t[0] = (p[0] - Offset[0]) / s;
                t[1] = (p[1] - Offset[1]) / s;
                t[2] = (p[2] - Offset[2]) / s;
            }
            );
        }

        private void MakeVertexVoxels(IList<Vertex> vertices, IVoxel parent, VoxelHashSet voxels)
        {
            setLimitsAndLevel(parent, out var level, out var parentLimits);
            Parallel.ForEach(vertices, vertex =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                int[] intCoords = intCoordsForVertex(vertex);
                MakeAndStorePartialVoxel(intCoords, level, voxels, parentLimits, vertex);
            }
            );
        }
        private int[] intCoordsForVertex(Vertex v)
        {
            var coordinates = transformedCoordinates[v.IndexInList];
            int[] intCoords;
            if (coordinates.Any(atIntegerValue))
            {
                intCoords = new int[3];
                var edgeVectors = v.Edges.Select(e => e.To == v ? e.Vector : e.Vector.multiply(-1))
                    .ToList();
                if (atIntegerValue(coordinates[0]) && edgeVectors.All(ev => ev[0] >= 0))
                    intCoords[0] = (int)(coordinates[0] - 1);
                else intCoords[0] = (int)coordinates[0];
                if (atIntegerValue(coordinates[1]) && edgeVectors.All(ev => ev[1] >= 0))
                    intCoords[1] = (int)(coordinates[1] - 1);
                else intCoords[1] = (int)coordinates[1];
                if (atIntegerValue(coordinates[2]) && edgeVectors.All(ev => ev[2] >= 0))
                    intCoords[2] = (int)(coordinates[2] - 1);
                else intCoords[2] = (int)coordinates[2];
            }
            else intCoords = new[] { (int)coordinates[0], (int)coordinates[1], (int)coordinates[2] };

            return intCoords;
        }

        /// <summary>
        /// Checks if the tessellated object is outside limits. This is a necessary but not sufficient condition.
        /// In otherwords, when TRUE it is assured that the object is outside, but FALSE when inside inside and some
        /// outside cases.
        /// </summary>
        /// <param name="limits">The limits.</param>
        /// <param name="tsObject">The ts object.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool tsObjectIsOutsideLimits(int[][] limits, TessellationBaseClass tsObject)
        {
            if (tsObject is Vertex)
                return allPointsOnOneSideOfLimits(limits, ((Vertex)tsObject).Position);
            if (tsObject is Edge)
                return allPointsOnOneSideOfLimits(limits, ((Edge)tsObject).From.Position, ((Edge)tsObject).To.Position);
            else //if (tsObject is PolygonalFace)
                return allPointsOnOneSideOfLimits(limits, ((PolygonalFace)tsObject).A.Position,
                    ((PolygonalFace)tsObject).B.Position, ((PolygonalFace)tsObject).C.Position);
        }

        private bool allPointsOnOneSideOfLimits(int[][] limits, params double[][] points)
        {
            return points.All(p => p[0] < limits[0][0]) ||
                    points.All(p => p[1] < limits[0][1]) ||
                    points.All(p => p[2] < limits[0][2]) ||
                    points.All(p => p[0] > limits[1][0]) ||
                    points.All(p => p[1] > limits[1][1]) ||
                    points.All(p => p[2] > limits[1][2]);
        }
        private bool allPointsOnOneSideOfLimits(int[][] limits, params int[][] points)
        {
            return points.All(p => p[0] < limits[0][0]) ||
                   points.All(p => p[1] < limits[0][1]) ||
                   points.All(p => p[2] < limits[0][2]) ||
                   points.All(p => p[0] >= limits[1][0]) ||  //notice that this is changed for >= so
                   points.All(p => p[1] >= limits[1][1]) ||  //as to make the upper value exclusive
                   points.All(p => p[2] >= limits[1][2]);    //instead of inclusive.
        }

        #region Face and Edge  Functions

        /// <summary>
        /// Makes the voxels for faces and edges. This is a complicated function! Originally, the scheme used in 
        /// OpenVDB was employed where a voxel group (3x3x3) is initiated at one of the vertices of a face so that 
        /// it is guaranteed to intersect with the face. Then it performs a depth first search outward and makes 
        /// many calls to the closest point on the triangle function (see under proximity). This was implemented
        /// in https://github.com/DesignEngrLab/TVGL/commit/b366f25fa8be05a6d75e0272ff1efb15660880d9 but it was
        /// shown to be 10 times slower than the method devised here. This method simply follows the edges, 
        /// which are obviously straight-lines in space to see what voxels it passes through. For the faces, 
        /// these are done in a sweep across the face progressively with the creation of the edge voxels.
        /// Details of this method are to be presented
        /// on the wiki page: https://github.com/DesignEngrLab/TVGL/wiki/Creating-Voxels-from-Tessellation
        /// </summary>
        /// <param name="linkToTessellatedSolid">if set to <c>true</c> [link to tessellated solid].</param>
        private void MakeVoxelsForFacesAndEdges(IList<PolygonalFace> faces, IVoxel parent, VoxelHashSet voxels)
        {
            setLimitsAndLevel(parent, out int level, out var parentLimits);
            foreach (var face in faces) //loop over the faces
            {
                if (level > 2 && tsObjectIsOutsideLimits(parentLimits, face)) continue;
                if (simpleCase(face, voxels, level, parentLimits)) continue;

                setUpFaceSweepDetails(face, out var startVertex, out var sweepDim, out var maxSweepValue);
                var leftStartPoint = (double[])transformedCoordinates[startVertex.IndexInList].Clone();
                var sweepValue = (int)(atIntegerValue(leftStartPoint[sweepDim])
                    ? leftStartPoint[sweepDim] + 1
                    : Math.Ceiling(leftStartPoint[sweepDim]));

                var sweepIntersections = new Dictionary<int, List<double[]>>();
                foreach (var edge in face.Edges)
                {
                    var toIntCoords = intCoordsForVertex(edge.To);
                    var fromIntCoords = intCoordsForVertex(edge.From);
                    if (toIntCoords[0] == fromIntCoords[0] && toIntCoords[1] == fromIntCoords[1] &&
                       toIntCoords[2] == fromIntCoords[2]) continue;
                    makeVoxelsForLine(transformedCoordinates[edge.From.IndexInList],
                        transformedCoordinates[edge.To.IndexInList], edge, sweepDim, sweepIntersections,
                        level, voxels, parentLimits);
                }

                if (sweepValue < parentLimits[0][sweepDim]) sweepValue = parentLimits[0][sweepDim];
                if (maxSweepValue >= parentLimits[1][sweepDim]) maxSweepValue = parentLimits[1][sweepDim] - 1;
                while (sweepValue <= maxSweepValue) // this is the sweep along the face
                {
                    if (sweepIntersections.ContainsKey(sweepValue))
                    {
                        var intersections = sweepIntersections[sweepValue];
                        //if (intersections.Count() != 2) throw new Exception();
                        if (intersections.Count == 2 && !allPointsOnOneSideOfLimits(parentLimits, intersections[0], intersections[1]))
                            makeVoxelsForLineOnFace(intersections[0], intersections[1], face, sweepDim, level,
                                voxels, parentLimits);
                    }
                    sweepValue++; //increment sweepValue and repeat!
                }
            }
        }

        private void setLimitsAndLevel(IVoxel parent, out int level, out int[][] parentLimits)
        {
            if (parent == null)
            {
                level = 0;
                parentLimits = new[] { new int[3], new[] { 16, 16, 16 } };
            }
            else
            {
                level = parent.Level + 1;
                parentLimits = new int[2][];
                parentLimits[0] = Constants.GetCoordinateIndices(parent.ID, level);
                parentLimits[1] = parentLimits[0].add(new[] { 16, 16, 16, });
            }
        }

        /// <summary>
        /// If it is a simple case, just solve it and return true.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="voxels">The voxels.</param>
        /// <param name="level">The level.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool simpleCase(PolygonalFace face, VoxelHashSet voxels, int level, int[][] limits)
        {
            var aCoords = intCoordsForVertex(face.A);
            var bCoords = intCoordsForVertex(face.B);
            var cCoords = intCoordsForVertex(face.C);
            // The first simple case is that all vertices are within the same voxel. 
            if (aCoords[0] == bCoords[0] && bCoords[0] == cCoords[0] &&
                aCoords[1] == bCoords[1] && bCoords[1] == cCoords[1] &&
                aCoords[2] == bCoords[2] && bCoords[2] == cCoords[2])
                return true;
            // the second, third, and fourth simple cases are if the triangle
            // fits within a line of voxels.
            // this condition checks that all voxels have same x & y values (hence aligned in z-direction)
            if (aCoords[0] == bCoords[0] && aCoords[0] == cCoords[0] &&
                aCoords[1] == bCoords[1] && aCoords[1] == cCoords[1])
            {
                makeVoxelsForFaceInCardinalLine(face, 2, level, voxels, limits, aCoords, bCoords[2], cCoords[2]);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (aCoords[0] == bCoords[0] && aCoords[0] == cCoords[0] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 1, level, voxels, limits, aCoords, bCoords[1], cCoords[1]);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (aCoords[1] == bCoords[1] && aCoords[1] == cCoords[1] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 0, level, voxels, limits, aCoords, bCoords[0], cCoords[0]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Makes the voxels for face in cardinal line. This is only called by the preceding function, simpleCase.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="dim">The dim.</param>
        /// <param name="linkToTessellatedSolid"></param>
        private void makeVoxelsForFaceInCardinalLine(PolygonalFace face, int dim, int level, VoxelHashSet voxels,
            int[][] parentLimits, int[] aCoords, int bValue, int cValue)
        {
            var aValue = aCoords[dim];
            int minCoord = aValue;
            int maxCoord = aValue;
            if (bValue < minCoord) minCoord = bValue;
            else if (bValue > maxCoord)
                maxCoord = bValue;
            if (cValue < minCoord) minCoord = cValue;
            else if (cValue > maxCoord)
                maxCoord = cValue;
            if (minCoord < parentLimits[0][dim]) minCoord = parentLimits[0][dim];
            if (maxCoord >= parentLimits[1][dim]) maxCoord = parentLimits[1][dim] - 1;
            var coordinates = (int[])aCoords.Clone();
            for (var i = minCoord; i <= maxCoord; i++)
            {
                // set up voxels for the face
                coordinates[dim] = i;
                var voxel = MakeAndStorePartialVoxel(coordinates, level, voxels, parentLimits, face);
                if (voxel is VoxelWithTessellationLinks)
                { // this is just to add links to the edges
                    var btmDim = Constants.GetCoordinateIndex(voxel.ID, level, dim);
                    foreach (var faceEdge in face.Edges)
                    {
                        // cycle over the edges to link them to the voxels
                        var fromDim = transformedCoordinates[faceEdge.From.IndexInList][dim];
                        var toDim = transformedCoordinates[faceEdge.To.IndexInList][dim];
                        if ((fromDim < btmDim && toDim > btmDim) ||
                            (fromDim > btmDim && toDim < btmDim) ||
                            (fromDim < btmDim + 1 && toDim > btmDim + 1) ||
                            (fromDim > btmDim + 1 && toDim < btmDim + 1))
                            LinkVoxelToTessellatedObject((VoxelWithTessellationLinks)voxel, faceEdge);
                    }
                }
            }
        }


        /// <summary>
        /// Sets up face sweep details such as which dimension to sweep over and assign start and end points
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="startVertex">The start vertex.</param>
        /// <param name="sweepDim">The sweep dim.</param>
        /// <param name="maxSweepValue">The maximum sweep value.</param>
        private void setUpFaceSweepDetails(PolygonalFace face, out Vertex startVertex, out int sweepDim,
            out double maxSweepValue)
        {
            var xLength = Math.Max(Math.Max(Math.Abs(face.A.X - face.B.X), Math.Abs(face.B.X - face.C.X)),
                Math.Abs(face.C.X - face.A.X));
            var yLength = Math.Max(Math.Max(Math.Abs(face.A.Y - face.B.Y), Math.Abs(face.B.Y - face.C.Y)),
                Math.Abs(face.C.Y - face.A.Y));
            var zLength = Math.Max(Math.Max(Math.Abs(face.A.Z - face.B.Z), Math.Abs(face.B.Z - face.C.Z)),
                Math.Abs(face.C.Z - face.A.Z));
            sweepDim = 0;
            if (yLength > xLength) sweepDim = 1;
            if (zLength > yLength && zLength > xLength) sweepDim = 2;
            startVertex = face.A;
            maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.B.IndexInList][sweepDim],
                transformedCoordinates[face.C.IndexInList][sweepDim]));
            // in the following conditions the actual vertex positions are used instead of this.transformedCoordinates,
            // but it is okay since these are simpley scaled and compared to one another
            if (face.B.Position[sweepDim] < face.A.Position[sweepDim])
            {
                startVertex = face.B;
                maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.C.IndexInList][sweepDim],
                    transformedCoordinates[face.A.IndexInList][sweepDim]));
            }
            if (face.C.Position[sweepDim] < face.B.Position[sweepDim] &&
                face.C.Position[sweepDim] < face.A.Position[sweepDim])
            {
                startVertex = face.C;
                maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.A.IndexInList][sweepDim],
                    transformedCoordinates[face.B.IndexInList][sweepDim]));
            }
        }

        private void makeVoxelsForLine(double[] startPoint, double[] endPoint, TessellationBaseClass tsObject,
            int sweepDim, Dictionary<int, List<double[]>> sweepIntersections, int level, VoxelHashSet voxels,
            int[][] limits)
        {
            //Get every X,Y, and Z integer value intersection
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new Dictionary<int, List<double[]>>
            {
                {0, new List<double[]>()},
                {1, new List<double[]>()},
                {2, new List<double[]>()}
            };
            for (var i = 0; i < 3; i++)
                getIntegerIntersectionsAlongLine(startPoint, endPoint, i, intersections, vectorNorm);

            //Store the sweep dimension intersections.
            foreach (var intersection in intersections[sweepDim])
            {
                var sweepValue = (int)intersection[sweepDim];
                if (sweepIntersections.ContainsKey(sweepValue))
                    sweepIntersections[sweepValue].Add(intersection);
                else sweepIntersections.Add(sweepValue, new List<double[]> { intersection });
            }
            addVoxelsAtIntersections(intersections, level, voxels, limits, tsObject);
        }
        /// <summary>
        /// This function gets the intersection vertices between the startPoint and endPoint on a
        /// line, for every integer value of the given dimension along the line.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="dim"></param>
        /// <param name="intersections"></param>
        /// <param name="vectorNorm"></param>
        private void getIntegerIntersectionsAlongLine(double[] startPoint, double[] endPoint,
            int dim, Dictionary<int, List<double[]>> intersections, double[] vectorNorm = null)
        {
            if (vectorNorm == null) vectorNorm = endPoint.subtract(startPoint).normalize();
            var start = (int)Math.Floor(startPoint[dim]);
            var end = (int)Math.Floor(endPoint[dim]);
            var forwardX = end > start;
            var uDim = (dim + 1) % 3;
            var vDim = (dim + 2) % 3;
            var t = start;
            while (t != end)
            {
                if (forwardX) t++;
                var d = (t - startPoint[dim]) / vectorNorm[dim];
                var intersection = new double[3];
                intersection[dim] = t;
                intersection[uDim] = startPoint[uDim] + d * vectorNorm[uDim];
                intersection[vDim] = startPoint[vDim] + d * vectorNorm[vDim];
                intersections[dim].Add(intersection);
                //If going reverse, do not decriment until after using this voxel index.
                if (!forwardX) t--;
            }
        }

        private void makeVoxelsForLineOnFace(double[] startPoint, double[] endPoint, TessellationBaseClass tsObject,
           int sweepDim, int level, VoxelHashSet voxels, int[][] limits)
        {
            if (allPointsOnOneSideOfLimits(limits, startPoint, endPoint)) return;
            //Get every X, Y, and Z integer value intersection, not including the sweepDim, which won't have any anyways
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new Dictionary<int, List<double[]>>
            {
                {0, new List<double[]>()},
                {1, new List<double[]>()},
                {2, new List<double[]>()}
            };
            for (var i = 0; i < 3; i++)
            {
                if (i == sweepDim) continue;
                getIntegerIntersectionsAlongLine(startPoint, endPoint, i, intersections, vectorNorm);
            }
            addVoxelsAtIntersections(intersections, level, voxels, limits, tsObject);
        }

        private void addVoxelsAtIntersections(Dictionary<int, List<double[]>> intersections, int level, VoxelHashSet voxels,
            int[][] limits, TessellationBaseClass tsObject)
        {
            foreach (var axis in intersections.Keys)
            {
                foreach (var intersection in intersections[axis])
                {
                    //Convert the intersectin values to integers. 
                    var ijk = new[] { (int)intersection[0], (int)intersection[1], (int)intersection[2] };
                    var dimensionsAsIntegers = intersection.Select(atIntegerValue).ToList();
                    var numAsInt = dimensionsAsIntegers.Count(c => c); //Counts number of trues

                    //If one/ three dimensions lands on an integer, the edge goes through a voxel face.
                    //If two/ three, a voxel edge. If three/ three, a corner. 

                    //In any case that it goes through a face, there must be a voxel located on both sides of this face.
                    //This is captured by the intersection conversion to bytes and the decrement along the dimension 
                    //with the integer. 

                    //If two/ three x,y,z values of the intersection are integers, this can be represented by drawing a 
                    //2D and ignoring the non-integer dimension.The intersection of interest is when the line goes intersects 
                    //the two axis(box corner). If you apply the decrement rule above, there are no real issues until you 
                    //try a negative slope line that intersects multiple box corners.Not only is there significant 
                    //inconsistency with the positive slope version, but it downright misses all the voxels with a line 
                    //through them.I am sure this same issue applies to lines through multiple voxel corners or a mix of 
                    //voxel corners and lines.

                    //The simplest and most robust solution I can think of is to add voxels at all the decemented integer 
                    //intersections. For voxel edge intersections, this forms 4 voxels around the intersection. For voxel
                    //corner intersections, this forms 8 voxels around the intersection. This can be expressed as:
                    //numVoxels = 2^numAsInt
                    var numVoxels = 0;
                    var allCombinations = new List<int[]>()
                    {
                        new []{ 0, 0, 0},
                        new []{ -1, 0, 0},
                        new []{ 0, -1, 0},
                        new []{ 0, 0, -1},
                        new []{ -1, -1, 0},
                        new []{ -1, 0, -1},
                        new []{ 0, -1, -1},
                        new []{ -1, -1, -1},
                    };
                    foreach (var combination in allCombinations)
                    {
                        var valid = true;
                        for (var j = 0; j < 3; j++)
                        {
                            if (dimensionsAsIntegers[j]) continue;
                            if (combination[j] == 0) continue;
                            //If not an integer and not 0, then do not add it to the list
                            valid = false;
                            break;
                        }
                        if (!valid) continue;
                        //This is a valid combination, so make it a voxel
                        var newIjk = new[] { ijk[0] + combination[0], ijk[1] + combination[1], ijk[2] + combination[2] };
                        MakeAndStorePartialVoxel(newIjk, level, voxels, limits, tsObject);
                        numVoxels++;
                    }
                    if (numVoxels != (int)Math.Pow(2, numAsInt)) throw new Exception("Error in implementation");
                }
            }

        }
        #endregion

        #region Interior Voxel Creation


        private VoxelHashSet DefineBottomCoordinateInside(ref VoxelHashSet voxels, List<PolygonalFace> parentFaces)
        {
            var level = voxels.FirstOrDefault()?.Level ?? 0;
            var queue = new Queue<IVoxel>(voxels);
            var assignedHashset = new VoxelHashSet(new VoxelComparerCoarse(), this);
            var lastSuccess = 0;
            while (queue.Any() && queue.Count > lastSuccess)
            {
                var voxel = queue.Dequeue();
                var voxCoord = voxel.CoordinateIndices;
                var closestFaceIsPositive = false;
                double minPositiveDistance = 1.0; // this starts at 1.0 because the voxel has length of one (at 
                // least within the current scaling). It's possible that faces are intersected but farther away than
                // this voxel. This is for when the voxel knows its faces as well as when looking over the parent set
                var faces = voxel is VoxelWithTessellationLinks ? ((VoxelWithTessellationLinks)voxel).Faces : parentFaces;
                foreach (var face in faces)
                {
                    var d = face.Normal.dotProduct(transformedCoordinates[face.Vertices[0].IndexInList]);
                    var n = face.Normal;
                    double signedDistance;
                    if (!face.Normal[0].IsNegligible())
                    {
                        var xPt = (d - n[1] * voxCoord[1] - n[2] * voxCoord[2]) / n[0];
                        signedDistance = xPt - voxCoord[0];
                        if (signedDistance >= 0 && signedDistance < minPositiveDistance &&
                            IsPointInsideTriangleTSBuilding(face, new[] { xPt, voxCoord[1], voxCoord[2] }))
                        {
                            minPositiveDistance = signedDistance;
                            closestFaceIsPositive = n[0] > 0;
                        }
                    }
                    if (!face.Normal[1].IsNegligible())
                    {
                        var yPt = (d - n[0] * voxCoord[0] - n[2] * voxCoord[2]) / n[1];
                        signedDistance = yPt - voxCoord[1];
                        if (signedDistance >= 0 && signedDistance < minPositiveDistance &&
                            IsPointInsideTriangleTSBuilding(face, new[] { voxCoord[0], yPt, voxCoord[2] }))
                        {
                            minPositiveDistance = signedDistance;
                            closestFaceIsPositive = n[1] > 0;
                        }
                    }
                    if (!face.Normal[2].IsNegligible())
                    {
                        var zPt = (d - n[0] * voxCoord[0] - n[1] * voxCoord[1]) / n[2];
                        signedDistance = zPt - voxCoord[2];
                        if (signedDistance >= 0 && signedDistance < minPositiveDistance &&
                            IsPointInsideTriangleTSBuilding(face, new[] { voxCoord[0], voxCoord[1], zPt }))
                        {
                            minPositiveDistance = signedDistance;
                            closestFaceIsPositive = n[2] > 0;
                        }
                    }
                }
                if (minPositiveDistance < 1.0)
                {
                    voxel.BtmCoordIsInside = closestFaceIsPositive;
                    assignedHashset.Add(voxel);
                    lastSuccess = 0;
                    continue;
                }
                var maxValue = Constants.MaxForSingleCoordinate >> (4 * (4 - level));
                var gotFromNeighbor = false;
                for (int i = 0; i < 3; i++)
                {
                    if (voxCoord[i] == maxValue) continue;
                    var neighbor = GetNeighborForTSBuilding(voxCoord, (VoxelDirections)(i + 1), assignedHashset,
                        level, out var neighborCoord);
                    if (neighbor != null)
                    {
                        voxel.BtmCoordIsInside = neighbor.BtmCoordIsInside;
                        assignedHashset.Add(voxel);
                        gotFromNeighbor = true;
                        break;
                    }
                }
                if (gotFromNeighbor) lastSuccess = 0;
                else lastSuccess++;
            }
            if (queue.Any())
                return new VoxelHashSet(level > 1 ? (IEqualityComparer<long>)new VoxelComparerFine() : new VoxelComparerCoarse(),
                    this, queue);
            return null;
        }

        private bool IsPointInsideTriangleTSBuilding(PolygonalFace face, double[] point)
        {
            var endPoints = face.Vertices.Select(v => transformedCoordinates[v.IndexInList]).ToList();
            return MiscFunctions.IsPointInsideTriangle(endPoints, point);
        }

        private IVoxel GetNeighborForTSBuilding(int[] point3D, VoxelDirections direction, VoxelHashSet voxelHashSet, int level, out int[] neighborCoord)
        {
            neighborCoord = (int[])point3D.Clone();
            var step = direction > 0 ? 1 : -1;
            var dimension = Math.Abs((int)direction) - 1;
            neighborCoord[dimension] += step;
            var neighborID = Constants.MakeIDFromCoordinates(level, neighborCoord, level);
            return voxelHashSet.GetVoxel(neighborID);
        }

        /// <summary>
        /// Makes the voxels in interior.
        /// </summary>
        private void makeVoxelsInInterior(VoxelHashSet voxels, IVoxel parent, List<PolygonalFace> grandParentFaces,
            VoxelHashSet unknownPartials)
        {
            setLimitsAndLevel(parent, out var level, out var parentLimits);
            var allDirections = new[] { -3, -2, -1, 1, 2, 3 };
            var negDirections = new[] { -3, -2, -1 };
            var posDirections = new[] { 0, 1, 2 };
            var insiders = new Stack<IVoxel>();
            var outsiders = new Stack<IVoxel>();
            if (unknownPartials == null)
                foreach (var voxel in voxels)
                    if (voxel.BtmCoordIsInside) insiders.Push(voxel);
                    else outsiders.Push(voxel);
            else foreach (var voxel in voxels.Where(v => !unknownPartials.Contains(v)))
                    if (voxel.BtmCoordIsInside) insiders.Push(voxel);
                    else outsiders.Push(voxel);
            // the outsiders are harder since need to see if what across boundary is actually inside
            while (outsiders.Any())
            {
                var current = outsiders.Pop();
                var coord = current.CoordinateIndices;
                foreach (var direction in posDirections)
                {
                    if (coord[direction] == parentLimits[1][direction] - 1) continue;   // check to see if current is on a boundary 
                    var neighbor = GetNeighborForTSBuilding(coord, (VoxelDirections)(direction + 1), voxels, level, out var neighborCoord);
                    if ((unknownPartials == null && neighbor != null) ||
                        (unknownPartials != null && !unknownPartials.Contains(neighbor))) continue;
                    var faces = current is VoxelWithTessellationLinks ? ((VoxelWithTessellationLinks)current).Faces :
                        parent is VoxelWithTessellationLinks ? ((VoxelWithTessellationLinks)parent).Faces :
                        grandParentFaces;
                    if (farthestFaceIsNegativeDirection(faces, coord, direction))
                    {
                        if (unknownPartials != null && unknownPartials.Contains(neighbor))
                            neighbor.BtmCoordIsInside = true;
                        else
                        {
                            neighbor = MakeAndStoreFullVoxel(neighborCoord, level, voxels, parentLimits);
                            if (neighbor != null) insiders.Push(neighbor);
                        }
                    }
                }
            }
            while (insiders.Any())
            {
                var current = insiders.Pop();
                var directions = current.Role == VoxelRoleTypes.Full
                                 || (unknownPartials != null && unknownPartials.Contains(current)) ? allDirections : negDirections;
                var coord = current.CoordinateIndices;
                foreach (var direction in directions)
                {
                    // check to see if current is on a boundary 
                    if ((direction < 0 && coord[-direction - 1] > parentLimits[0][-direction - 1]) ||
                    (direction > 0 && coord[direction - 1] < parentLimits[1][direction - 1] - 1))
                    {
                        var neighbor = GetNeighborForTSBuilding(coord, (VoxelDirections)direction, voxels, level, out var neighborCoord);
                        if (unknownPartials != null && unknownPartials.Contains(neighbor))
                            neighbor.BtmCoordIsInside = true;
                        else if (neighbor == null)
                        {
                            neighbor = MakeAndStoreFullVoxel(neighborCoord, level, voxels, parentLimits);
                            insiders.Push(neighbor);
                        }
                    }
                }
            }
        }


        private bool farthestFaceIsNegativeDirection(List<PolygonalFace> faces, int[] coord, int dimension)
        {
            var negativeFace = false;
            double maxDistance = 0.0;
            foreach (var face in faces)
            {
                if (face.Normal[dimension].IsNegligible()) continue; //Use a different face because this face is inconclusive.

                var d = face.Normal.dotProduct(transformedCoordinates[face.Vertices[0].IndexInList]);
                var n = face.Normal;
                double[] newPoint;
                double signedDistance;
                switch (dimension)
                {
                    case 0:
                        var xPt = (d - n[1] * coord[1] - n[2] * coord[2]) / n[0];
                        signedDistance = xPt - coord[0];
                        newPoint = new[] { xPt, coord[1], coord[2] };
                        break;
                    case 1:
                        var yPt = (d - n[0] * coord[0] - n[2] * coord[2]) / n[1];
                        signedDistance = yPt - coord[1];
                        newPoint = new[] { coord[0], yPt, coord[2] };
                        break;
                    default:
                        var zPt = (d - n[0] * coord[0] - n[1] * coord[1]) / n[2];
                        signedDistance = zPt - coord[2];
                        newPoint = new[] { coord[0], coord[1], zPt };
                        break;
                }
                if (signedDistance < 0 || signedDistance > 1 ||
                    !IsPointInsideTriangleTSBuilding(face, newPoint)) continue;
                if (signedDistance > maxDistance)
                {
                    negativeFace = face.Normal[dimension] < 0;
                    maxDistance = signedDistance;
                }
            }
            return negativeFace;
        }
        #endregion


        #region Make and Store Voxel
        private IVoxel MakeAndStorePartialVoxel(int[] coordinates, int level, VoxelHashSet voxels, int[][] limits,
            TessellationBaseClass tsObject = null)
        {
            if (allPointsOnOneSideOfLimits(limits, coordinates)) return null;
            IVoxel voxel;
            var id = Constants.MakeIDFromCoordinates(level, coordinates, level);
            lock (voxels)
            {
                voxel = voxels.GetVoxel(id);
                if (voxel == null)
                {
                    if (level == 0)
                        voxel = new Voxel_Level0_Class(id, VoxelRoleTypes.Partial, this);
                    else if (level == 1)
                        voxel = new Voxel_Level1_Class(id, VoxelRoleTypes.Partial, this);
                    else
                        voxel = new Voxel(id + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), level);
                    voxels.Add(voxel);
                }
            }
            if (level > 1) return voxel;
            var voxelwithTsLinks = (VoxelWithTessellationLinks)voxel;
            if (voxelwithTsLinks.TessellationElements == null)
                voxelwithTsLinks.TessellationElements = new HashSet<TessellationBaseClass>();

            LinkVoxelToTessellatedObject(voxelwithTsLinks, tsObject);
            if (tsObject is Vertex vertex)
            {
                foreach (var edge in vertex.Edges)
                    LinkVoxelToTessellatedObject(voxelwithTsLinks, edge);
                foreach (var face in vertex.Faces)
                    LinkVoxelToTessellatedObject(voxelwithTsLinks, face);
            }
            else if (tsObject is Edge edge)
            {
                LinkVoxelToTessellatedObject(voxelwithTsLinks, edge.OtherFace);
                LinkVoxelToTessellatedObject(voxelwithTsLinks, edge.OwnedFace);
            }
            return voxel;
        }

        private void LinkVoxelToTessellatedObject(VoxelWithTessellationLinks voxel, TessellationBaseClass tsObject)
        {
            if (tsObject==null) return;
            if (voxel.TessellationElements.Contains(tsObject)) return;
            lock (voxel.TessellationElements) voxel.TessellationElements.Add(tsObject);
            tsObject.AddVoxel(voxel);
        }

        private IVoxel MakeAndStoreFullVoxel(int[] coordinates, int level, VoxelHashSet voxels, int[][] limits)
        {
            if (allPointsOnOneSideOfLimits(limits, coordinates)) return null;
            IVoxel voxel;
            var id = Constants.MakeIDFromCoordinates(level, coordinates, level);
            lock (voxels)
            {
                voxel = voxels.GetVoxel(id);
                if (voxel == null)
                {
                    if (level == 0)
                        voxel = new Voxel_Level0_Class(id, VoxelRoleTypes.Full, this);
                    else if (level == 1)
                        voxel = new Voxel_Level1_Class(id, VoxelRoleTypes.Full, this);
                    else
                        voxel = new Voxel(id + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), level);
                    voxels.Add(voxel);
                }
            }
            return voxel;
        }

        #endregion
    }
}