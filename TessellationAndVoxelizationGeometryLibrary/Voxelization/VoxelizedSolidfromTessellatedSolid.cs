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
        public VoxelizedSolid(TessellatedSolid ts, VoxelDiscretization voxelDiscretization, bool onlyDefineBoundary = false,
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
            {  // add a small buffer only if no bounds are provided.
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
            VoxelSideLengths = new[] { longestSide / 16, longestSide / 256, longestSide / 4096, longestSide / 65536, longestSide / 1048576 };
            numVoxels = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[discretizationLevel])).ToArray();
            #endregion

            transformedCoordinates = new double[ts.NumberOfVertices][];
            #region Level-0
            var voxelsZeroLevel = new VoxelHashSet(new VoxelComparerCoarse(), this);
            MakeVertexSimulatedCoordinates(ts.Vertices, 0);
            MakeVertexVoxels(ts.Vertices, null, voxelsZeroLevel);
            MakeVoxelsForFacesAndEdges(ts.Faces, null, voxelsZeroLevel);
            DefineBottomCoordinateInside(voxelsZeroLevel, null);
            if (!onlyDefineBoundary)
                makeVoxelsInInterior(voxelsZeroLevel, null);
            voxelDictionaryLevel0 = voxelsZeroLevel;
            #endregion

            if (discretizationLevel >= 1)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 1);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial), voxel0 =>
                //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                {
                    var voxels = new VoxelHashSet(new VoxelComparerCoarse(), this);
                    MakeVertexVoxels(((VoxelWithTessellationLinks)voxel0).Vertices, voxel0, voxels);
                    MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel0).Faces, voxel0, voxels);
                    DefineBottomCoordinateInside(voxels, voxel0);
                    if (!onlyDefineBoundary)
                        makeVoxelsInInterior(voxels, voxel0);
                    ((Voxel_Level0_Class)voxel0).InnerVoxels[0] = voxels;
                });
            }
            if (discretizationLevel >= 2)
            {
                UpdateVertexSimulatedCoordinates(ts.Vertices, 2);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial),
                    voxel0 =>
                    //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                    {
                        var voxels = new List<IVoxel>();
                        Parallel.ForEach(((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial), voxel1 =>
                        //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                        {
                            var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                            MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel1, voxelHash);
                            MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel1, voxelHash);
                            DefineBottomCoordinateInside(voxelHash, voxel1);
                            if (!onlyDefineBoundary)
                                makeVoxelsInInterior(voxelHash, voxel1);
                            lock (voxels) voxels.AddRange(voxelHash);
                        });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[1] = new VoxelHashSet(new VoxelComparerFine(), this, voxels);
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
                        Parallel.ForEach(((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial), voxel1 =>
                        //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                        {
                            var voxelsLevel2 = GetChildVoxels(voxel1);
                            Parallel.ForEach(voxelsLevel2, voxel2 =>
                            {
                                var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                                MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel2, voxelHash);
                                MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel2, voxelHash);
                                DefineBottomCoordinateInside(voxelHash, voxel2);
                                if (!onlyDefineBoundary)
                                    makeVoxelsInInterior(voxelHash, voxel2);
                                lock (voxels) voxels.AddRange(voxelHash);
                            });
                        });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[2] = new VoxelHashSet(new VoxelComparerFine(), this, voxels);
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
                        Parallel.ForEach(((Voxel_Level0_Class)voxel0).InnerVoxels[0].Where(v => v.Role == VoxelRoleTypes.Partial), voxel1 =>
                        //  foreach (var voxel0 in voxelDictionaryLevel0.Values.Where(v=>v.Role==VoxelRoleTypes.Partial))
                        {
                            var voxelsLevel2 = GetChildVoxels(voxel1);
                            Parallel.ForEach(voxelsLevel2, voxel2 =>
                            {
                                var voxelsLevel3 = GetChildVoxels(voxel2);
                                Parallel.ForEach(voxelsLevel3, voxel3 =>
                                {
                                    var voxelHash = new VoxelHashSet(new VoxelComparerFine(), this);
                                    MakeVertexVoxels(((VoxelWithTessellationLinks)voxel1).Vertices, voxel3, voxelHash);
                                    MakeVoxelsForFacesAndEdges(((VoxelWithTessellationLinks)voxel1).Faces, voxel3, voxelHash);
                                    DefineBottomCoordinateInside(voxelHash, voxel3);
                                    if (!onlyDefineBoundary)
                                        makeVoxelsInInterior(voxelHash, voxel3);
                                    lock (voxels) voxels.AddRange(voxelHash);
                                });
                            });
                        });
                        ((Voxel_Level0_Class)voxel0).InnerVoxels[3] = new VoxelHashSet(new VoxelComparerFine(), this, voxels);
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
                transformedCoordinates[i] = new[] { (p[0] - Offset[0]) / s, (p[1] - Offset[1]) / s, (p[2] - Offset[2]) / s };
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
            var level = parent?.Level + 1 ?? 0;

            Parallel.ForEach(vertices, vertex =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var coordinates = transformedCoordinates[vertex.IndexInList];
                int x, y, z;
                if (level > 2 && !ParentOverlapsElement(parent, vertex)) return;
                if (coordinates.Any(atIntegerValue))
                {
                    var edgeVectors = vertex.Edges.Select(e => e.To == vertex ? e.Vector : e.Vector.multiply(-1))
                        .ToList();
                    if (atIntegerValue(coordinates[0]) && edgeVectors.All(ev => ev[0] >= 0))
                        x = (int)(coordinates[0] - 1);
                    else x = (int)coordinates[0];
                    if (atIntegerValue(coordinates[1]) && edgeVectors.All(ev => ev[1] >= 0))
                        y = (int)(coordinates[1] - 1);
                    else y = (int)coordinates[1];
                    if (atIntegerValue(coordinates[2]) && edgeVectors.All(ev => ev[2] >= 0))
                        z = (int)(coordinates[2] - 1);
                    else z = (int)coordinates[2];
                }
                else
                {
                    //Gets the integer coordinates, rounded down for the point.
                    x = (int)coordinates[0];
                    y = (int)coordinates[1];
                    z = (int)coordinates[2];
                }
                MakeAndStorePartialVoxel(x, y, z, level, level, voxels, vertex);
            }
            );
        }

        private bool ParentOverlapsElement(IVoxel parent, TessellationBaseClass tsObject)
        {
            throw new NotImplementedException();
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
            var level = parent?.Level ?? 0;
            foreach (var face in faces) //loop over the faces
            {
                if (level > 2 && !ParentOverlapsElement(parent, face)) continue;
                if (simpleCase(face, voxels, level)) continue;

                setUpFaceSweepDetails(face, out var startVertex, out var sweepDim, out var maxSweepValue);
                var leftStartPoint = (double[])transformedCoordinates[startVertex.IndexInList].Clone();
                var sweepValue = (int)(atIntegerValue(leftStartPoint[sweepDim])
                    ? leftStartPoint[sweepDim] + 1
                    : Math.Ceiling(leftStartPoint[sweepDim]));

                var sweepIntersections = new Dictionary<int, List<double[]>>();
                foreach (var edge in face.Edges)
                {
                    if (edge.To.Voxels.Last() == edge.From.Voxels.Last()) continue;
                    makeVoxelsForLine(transformedCoordinates[edge.From.IndexInList],
                        transformedCoordinates[edge.To.IndexInList], edge, sweepDim, sweepIntersections, level, voxels);
                }

                while (sweepValue <= maxSweepValue) // this is the sweep along the face
                {
                    if (sweepIntersections.ContainsKey(sweepValue))
                    {
                        var intersections = sweepIntersections[sweepValue];
                        if (intersections.Count() != 2) throw new Exception();
                        makeVoxelsForLineOnFace(intersections[0], intersections[1], face, sweepDim, level, voxels);
                    }
                    sweepValue++; //increment sweepValue and repeat!
                }
            }
        }

        /// <summary>
        /// If it is a simple case, just solve it and return true.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="voxels">The voxels.</param>
        /// <param name="level">The level.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool simpleCase(PolygonalFace face, VoxelHashSet voxels, int level, IVoxel parent)
        {
            var faceAVoxel = face.A.Voxels.First(v => v.Level == level);
            var faceBVoxel = face.B.Voxels.First(v => v.Level == level);
            var faceCVoxel = face.C.Voxels.First(v => v.Level == level);
            // The first simple case is that all vertices are within the same voxel. 
            if (faceAVoxel.Equals(faceBVoxel) && faceAVoxel.Equals(faceCVoxel))
                return true;
            // the second, third, and fourth simple cases are if the triangle
            // fits within a line of voxels.
            // this condition checks that all voxels have same x & y values (hence aligned in z-direction)
            if (faceAVoxel.CoordinateIndices[0] == faceBVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[0] == faceCVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[1] == faceBVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[1] == faceCVoxel.CoordinateIndices[1])
            {
                makeVoxelsForFaceInCardinalLine(face, 2, level, voxels, parent);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (faceAVoxel.CoordinateIndices[0] == faceBVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[0] == faceCVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[2] == faceBVoxel.CoordinateIndices[2] &&
                faceAVoxel.CoordinateIndices[2] == faceCVoxel.CoordinateIndices[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 1, level, voxels, parent);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (faceAVoxel.CoordinateIndices[1] == faceBVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[1] == faceCVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[2] == faceBVoxel.CoordinateIndices[2] &&
                faceAVoxel.CoordinateIndices[2] == faceCVoxel.CoordinateIndices[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 0, level, voxels, parent);
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
            IVoxel parent)
        {
            var coordA = face.A.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            var coordB = face.B.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            var coordC = face.C.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            int minCoord = coordA;
            int maxCoord = coordA;
            if (coordB < minCoord) minCoord = coordB;
            else if (coordB > maxCoord) maxCoord = coordB;
            if (coordC < minCoord) minCoord = coordC;
            else if (coordC > maxCoord) maxCoord = coordC;
            var parentMin =16* Constants.GetCoordinateIndex(parent.ID, parent.Level, dim);
            var parentMax = parentMin + 15;
            if (minCoord < parentMin) minCoord = parentMin;
            if (maxCoord > parentMax) maxCoord = parentMax;
            var coordinates = (int[])face.A.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices.Clone();
            for (var i = minCoord; i <= maxCoord; i++)
            {
                // set up voxels for the face
                coordinates[dim] = i;
                MakeAndStorePartialVoxel(coordinates, level, level, voxels, face);
            }
            foreach (var faceEdge in face.Edges)
            {
                // cycle over the edges to link them to the voxels
                int fromIndex = faceEdge.From.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
                int toIndex = faceEdge.To.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
                var step = Math.Sign(toIndex - fromIndex);
                if (step == 0) continue;
                for (var i = fromIndex; i != toIndex; i += step)
                {
                    coordinates[dim] = i;
                    MakeAndStorePartialVoxel(coordinates, level, level, voxels, face);
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
            int sweepDim, Dictionary<int, List<double[]>> sweepIntersections, int level, VoxelHashSet voxels)
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

            foreach (var axis in intersections.Keys)
            {
                foreach (var intersection in intersections[axis])
                {
                    //todo: need a statement to prevent creating voxels from different parents
                    //Convert the intersectin values to integers. 
                    var ijk = new[] { (int)intersection[0], (int)intersection[1], (int)intersection[2] };
                    MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);
                    var dimensionsAsIntegers = intersection.Select(atIntegerValue).ToList();
                    var numAsInt = dimensionsAsIntegers.Count(c => c); //Counts number of trues

                    //If only one int, then add voxel + 1 along that direction 
                    if (numAsInt == 1)
                    {
                        if (dimensionsAsIntegers[0]) ijk[0]--;
                        else if (dimensionsAsIntegers[1]) ijk[1]--;
                        else ijk[2]--;
                        MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);
                    }
                    else if (numAsInt == 2)
                    {
                        //This line goes through an edge of the voxel
                        //ToDo: Does this need to add something?
                    }
                    //Else this line goes through the corner of a voxel
                    //only add a voxel exactly at this intersection, which is done above  
                }
            }
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
           int sweepDim, int level, VoxelHashSet voxels)
        {
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

            foreach (var axis in intersections.Keys)
            {
                //There will be no intersections along the sweepDim
                foreach (var intersection in intersections[axis])
                {
                    //Convert the intersection values to integers. 
                    var ijk = new[] { (int)intersection[0], (int)intersection[1], (int)intersection[2] };
                    MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);
                    //Also add the -1 sweepDim voxel
                    ijk[sweepDim]--;
                    MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);

                    var dimensionsAsIntegers = intersection.Select(atIntegerValue).ToList();
                    var numAsInt = dimensionsAsIntegers.Count(c => c); //Counts number of trues

                    //If only one int, then add voxel + 1 along that direction 
                    if (numAsInt == 1) throw new Exception("this cannot occur");
                    if (numAsInt == 2)
                    {
                        //Add the increment that is not the sweepDim
                        if (dimensionsAsIntegers[0] && sweepDim != 0) ijk[0]--;
                        else if (dimensionsAsIntegers[1] && sweepDim != 1) ijk[1]--;
                        else ijk[2]--; //(dimensionsAsIntegers[2] && sweepDim != 2)
                        MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);
                        ijk[sweepDim]++;
                        MakeAndStorePartialVoxel(ijk, level, level, voxels, tsObject);
                    }
                    else
                    {
                    }
                    //Else this line goes through the corner of a voxel
                    //only add a voxel exactly at this intersection ? 
                }
            }
        }


        #endregion

        #region Interior Voxel Creation

        private void DefineBottomCoordinateInside(VoxelHashSet voxels, IVoxel parent)
        {
            foreach (var voxel in voxels)
            {

            }
        }

        /// <summary>
        /// Makes the voxels in interior.
        /// </summary>
        private IEnumerable<IVoxel> makeVoxelsInInterior(VoxelHashSet voxels, IVoxel parent)
        {
            var sweepDim = longestDimensionIndex;
            var ids = voxelDictionaryLevel1.Select(vx => Constants.MakeCoordinateZero(vx.ID, sweepDim))
                .Distinct()
                .AsParallel();
            var rows = ids.ToDictionary(id => id,
                id => new SortedSet<Voxel_Level1_Class>(new SortByVoxelIndex(sweepDim + 1)));
            //VoxelDirection enumerator, and since there is no negative 0, we start at 1 (x=1).
            Parallel.ForEach(voxelDictionaryLevel1, voxelKeyValuePair =>
            //foreach (var voxelKeyValuePair in voxelDictionaryLevel1)
            {
                var voxel = voxelKeyValuePair.Value;
                var id = Constants.MakeCoordinateZero(voxelKeyValuePair.Key, sweepDim);
                var sortedSet = rows[id];
                lock (sortedSet) sortedSet.Add(voxel);
            });
            Parallel.ForEach(rows.Values.Where(v => v.Any()), v =>
                //foreach (var v in rows.Values.Where(v => v.Any()))
                MakeInteriorVoxelsAlongLine(v, sweepDim));
        }

        //Sort partial voxels along a given direction and then consider rows along that direction 
        //End that line at every partial voxel, regardless of face orientation. This voxel may start a new 
        //line if it has faces in both directions.
        //If the next partial voxel is adjacent to the current voxel, go to the next voxel.
        //Start a new line anytime there is a voxel with all its faces pointed away from the search direction
        //OR if it contains faces pointing both ways and the next voxel is fully inside the solid.
        //To Determine if a voxel is fully inside the solid, use the normal of the closest
        //face cast back from the voxel in question. 
        private void MakeInteriorVoxelsAlongLine(SortedSet<Voxel_Level1_Class> sortedVoxelsInRow, int sweepDim,
            int level, VoxelHashSet voxels)
        {
            var voxelsInRow = new List<Voxel_Level1_Class>(sortedVoxelsInRow);
            var coords = (int[])sortedVoxelsInRow.First().CoordinateIndices.Clone();
            var consecutiveVoxels = new List<Voxel_Level1_Class>();
            var lineStartIndex = int.MinValue;
            var finalIndex = voxelsInRow.Last().CoordinateIndices[sweepDim];
            while (voxelsInRow.Any())
            {
                var currentVoxel = voxelsInRow[0];
                voxelsInRow.RemoveAt(0);
                var currentIndex = currentVoxel.CoordinateIndices[sweepDim];

                //End that line at every partial voxel, regardless of face orientation. This voxel may start a new 
                //line if it has faces in both directions.
                if (lineStartIndex != int.MinValue)
                {
                    consecutiveVoxels.Clear();
                    //Construct the line
                    for (int i = lineStartIndex + 1; i < currentIndex; i++)
                    {
                        coords[sweepDim] = i;
                            MakeAndStoreFullVoxel(coords,level,level,voxels);
                    }
                    lineStartIndex = int.MinValue;
                }
                //If it is the last voxel, then it cannot start a new line 
                if (finalIndex == currentIndex) break;
                //Start a new line anytime there is a voxel with any faces pointed away from the search direction
                //and the next voxel is fully inside the solid (not a partial). 
                consecutiveVoxels.Add(currentVoxel);
                //If it is positive, then it must be outside. Do not make a new line.
                if (currentVoxel.Faces.All(f => f.Normal[sweepDim] >= 0)) continue;
                //If the next partial voxel is adjacent, then the current voxel cannot start a new line.
                var nextVoxel = voxelsInRow[0];
                if (nextVoxel.CoordinateIndices[sweepDim] - currentIndex == 1) continue;
                //If the current voxel is negative, then it must be inside. Start a new line.
                if (nextVoxel.Faces.All(f => f.Normal[sweepDim] <= 0)) continue;
                if (currentVoxel.Faces.All(f => f.Normal[sweepDim] <= 0)
                    && (nextVoxel.Faces.All(f => f.Normal[sweepDim] >= 0)))
                {
                    lineStartIndex = currentIndex;
                    continue;
                }
                //Else, the current voxel contains both positive and negative faces - as does the next voxel
                //We need to determine if the next voxel is fully inside the solid or empty
                //Check the reversed prior voxels, looking for the closest face that intersects
                //a ray casted from the next voxel
                var prevFacesToConsider = consecutiveVoxels.SelectMany(v => v.Faces).Distinct().ToList();
                consecutiveVoxels.Clear();
                var nextFacesToConsider = new List<PolygonalFace>();
                var nextIndex = nextVoxel.CoordinateIndices[sweepDim];
                do
                {
                    voxelsInRow.RemoveAt(0);
                    consecutiveVoxels.Add(nextVoxel);
                    nextFacesToConsider.AddRange(nextVoxel.Faces);
                    nextVoxel = (voxelsInRow.Count > 1) ? voxelsInRow[1] : null;
                } while (nextVoxel != null && nextVoxel.CoordinateIndices[sweepDim] == ++nextIndex);
                voxelsInRow.Insert(0, consecutiveVoxels.Last());
                nextFacesToConsider = nextFacesToConsider.Distinct().ToList();
                var random = new Random();
                int successes = 0;
                for (int i = 0; i < Constants.NumberOfInteriorAttempts; i++)
                {
                    var randDelta = new[] { random.NextDouble(), random.NextDouble(), random.NextDouble() };
                    var randCoordinate = currentVoxel.BottomCoordinate.add(randDelta);
                    PolygonalFace prevFace = ClosestIntersectingFace(prevFacesToConsider, randCoordinate,
                        (VoxelDirections)(-(sweepDim + 1)), out double distancePrev);
                    var nextFace = ClosestIntersectingFace(nextFacesToConsider, randCoordinate,
                        (VoxelDirections)(sweepDim + 1), out double distanceNext);
                    if (prevFace != null && nextFace != null && prevFace.Normal[sweepDim] < 0
                        && nextFace.Normal[sweepDim] > 0)
                        successes++;
                }
                if (successes >= Constants.NumberOfInteriorSuccesses)
                    lineStartIndex = currentIndex;
            }
        }

        private PolygonalFace ClosestIntersectingFace(List<PolygonalFace> faces, double[] coordinate,
            VoxelDirections direction, out double distance)
        {
            PolygonalFace closestFace = null;
            distance = double.MaxValue;
            var sweepDim = Math.Abs((int)direction) - 1;
            foreach (var face in faces)
            {
                var dot = face.Normal[sweepDim];
                if (dot == 0) continue; //Use a different face because this face is inconclusive.

                double signedDistance;
                var intersectionPoint = MiscFunctions.PointOnTriangleFromLine(face, coordinate, direction,
                    out signedDistance, true);

                if (intersectionPoint == null || signedDistance <= 0) continue;

                if (signedDistance < distance)
                {
                    closestFace = face;
                    distance = signedDistance;
                }
            }
            return closestFace;
        }
        #endregion


        #region Make and Store Voxel
        private void MakeAndStorePartialVoxel(int[] xyz, int level, int inputCoordLevel, VoxelHashSet voxels, TessellationBaseClass tsObject = null)
        {
            MakeAndStorePartialVoxel(xyz[0], xyz[1], xyz[2], level, inputCoordLevel, voxels, tsObject);
        }
        private void MakeAndStorePartialVoxel(int x, int y, int z, int level, int inputCoordLevel, VoxelHashSet voxels, TessellationBaseClass tsObject = null)
        {
            IVoxel voxel;
            var id = Constants.MakeIDFromCoordinates(level, x, y, z, inputCoordLevel);
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
            if (level > 1) return;
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
        }

        private void LinkVoxelToTessellatedObject(VoxelWithTessellationLinks voxel , TessellationBaseClass tsObject)
        {
            if (voxel.TessellationElements.Contains(tsObject)) return;
            lock (voxel.TessellationElements) voxel.TessellationElements.Add(tsObject);
            tsObject.AddVoxel(voxel);
        }

        private IVoxel MakeAndStoreFullVoxel(int[] xyz, int level, int inputCoordLevel, VoxelHashSet voxels)
        {
            return MakeAndStoreFullVoxel(xyz[0], xyz[1], xyz[2], level, inputCoordLevel, voxels);
        }
        private IVoxel MakeAndStoreFullVoxel(int x, int y, int z, int level, int inputCoordLevel, VoxelHashSet voxels)
        {
            IVoxel voxel;
            var id = Constants.MakeIDFromCoordinates(level, x, y, z, inputCoordLevel);
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