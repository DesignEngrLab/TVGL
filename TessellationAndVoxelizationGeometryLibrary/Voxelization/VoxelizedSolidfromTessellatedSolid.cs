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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        /// <param name="discretization">The number of voxels on the longest size is 2 raised to this number (e.g 7 means 128 voxels on longest side).</param>
        /// <param name="onlyDefineBoundary">if set to <c>true</c> [only define boundary].</param>
        /// <param name="levelAtWhichLinkToTessellation">The level at which every at this level and below links to tessellation elements.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, int discretization,
            bool onlyDefineBoundary = false, int levelAtWhichLinkToTessellation = Constants.DefaultLevelAtWhichLinkToTessellation,
            double[][] bounds = null) : base(ts.Units, ts.Name, "", ts.Comments, ts.Language)
        {
            Discretization = discretization;
            LevelAtWhichLinkToTessellation = levelAtWhichLinkToTessellation;
            bitLevelDistribution = Constants.DefaultBitLevelDistribution[Discretization];
            voxelsPerSide = bitLevelDistribution.Select(b => (int)Math.Pow(2, b)).ToArray();
            voxelsInParent = voxelsPerSide.Select(s => s * s * s).ToArray();
            defineMaskAndShifts(bitLevelDistribution);
            numberOfLevels = bitLevelDistribution.Length;
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

                //const double delta = Constants.fractionOfWhiteSpaceAroundFinestVoxel;
                var voxelsOnLongSide = voxelsPerSide.Aggregate(1, (current, num) => current * num);
                var delta = new double[3];
                for (var i = 0; i < 3; i++) delta[i] = dimensions[i] * ((voxelsOnLongSide / (voxelsOnLongSide - 2 * Constants.fractionOfWhiteSpaceAroundFinestVoxel)) - 1) / 2;
                //var delta = longestSide * ((voxelsOnLongSide / (voxelsOnLongSide - 2 * Constants.fractionOfWhiteSpaceAroundFinestVoxel)) - 1) / 2;

                Bounds[0] = ts.Bounds[0].subtract(delta);
                Bounds[1] = ts.Bounds[1].add(delta);
                dimensions = dimensions.add(delta.multiply(2));
            }

            longestSide = dimensions[longestDimensionIndex];
            VoxelSideLengths = new double[numberOfLevels];
            VoxelSideLengths[0] = longestSide / voxelsPerSide[0];
            for (int i = 1; i < numberOfLevels; i++)
                VoxelSideLengths[i] = VoxelSideLengths[i - 1] / voxelsPerSide[i];
            #endregion

            voxelDictionaryLevel0 = new VoxelBinSet(dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[0])).ToArray(), bitLevelDistribution[0]);
            voxelsPerDimension = new int[NumberOfLevels][];
            for (var i = 0; i < numberOfLevels; i++)
                voxelsPerDimension[i] = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[i])).ToArray();
            var transformedCoordinates = MakeVertexSimulatedCoordinates(ts.Vertices, numberOfLevels - 1, ts.NumberOfVertices);
            MakeVertexVoxels(ts.Vertices, (byte)(numberOfLevels - 1), transformedCoordinates);
            MakeVoxelsForFacesAndEdges(ts.Faces, (byte)(numberOfLevels - 1), transformedCoordinates);
            if (onlyDefineBoundary) { UpdateProperties(); return; }
            UpdateVertexSimulatedCoordinates(transformedCoordinates, ts.Vertices, 0);
            DefineBottomCoordinateInsideLevel0(ts.Faces, transformedCoordinates);
            makeVoxelsInInteriorLevel0(transformedCoordinates);

            #region For all higher levels
            for (byte level = 1; level < numberOfLevels; level++)
            {
                UpdateVertexSimulatedCoordinates(transformedCoordinates, ts.Vertices, level);
                Parallel.ForEach(voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial),
                  voxel0 =>
                  //foreach (var voxel0 in voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial))
                  {
                      var voxels = voxel0.InnerVoxels[level - 1];
                      if (level == 1)
                      {
                              var children = voxels.ToList();
                          children = DefineBottomCoordinateInside(children, voxel0, 1, voxel0.BtmCoordIsInside, GetFacesToCheck(voxel0.ID, 0, voxel0), transformedCoordinates);
                          foreach (var child in children)
                              voxels.AddOrReplace(child);
                          makeVoxelsInInterior(voxels, children, 1, voxel0.ID, voxel0, transformedCoordinates);
                      }
                      else
                      {
                          foreach (var parent in voxel0.InnerVoxels[level - 2]
                              .Where(v => Constants.GetRole(v) == VoxelRoleTypes.Partial))
                          {
                              var children = GetChildVoxels(parent).ToList();
                              children = DefineBottomCoordinateInside(children, voxel0, level,
                                  Constants.GetIfBtmIsInside(parent),
                                  GetFacesToCheck(parent, level, voxel0), transformedCoordinates);
                              lock (voxels)
                                  foreach (var child in children)
                                      voxels.AddOrReplace(child);
                              makeVoxelsInInterior(voxels, children, level, parent, voxel0, transformedCoordinates);
                          }
                      }
                  });
            }
            #endregion
            UpdateProperties();
        }


        private double[][] MakeVertexSimulatedCoordinates(IList<Vertex> vertices, int level, int numberOfVertices)
        {
            var transformedCoords = new double[numberOfVertices][];
            var s = VoxelSideLengths[level];
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                // transformedCoordinates[i] = vertices[i].Position.subtract(Offset).divide(VoxelSideLengths[level]);
                // or to make a bit faster
                var p = vertices[i].Position;
                transformedCoords[i] = new[]
                    {(p[0] - Offset[0]) / s, (p[1] - Offset[1]) / s, (p[2] - Offset[2]) / s};
            });
            return transformedCoords;
        }
        // this is effectively the same function as above but the 2nd, 3rd, 4th, etc. times we do it, we can just
        // overwrite the previous values.
        private void UpdateVertexSimulatedCoordinates(double[][] transformedCoordinates, IList<Vertex> vertices, int level)
        {
            var s = VoxelSideLengths[level];
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < vertices.Count; i++)
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

        /// <summary>
        /// Makes the voxels that each vertex is within.
        /// </summary>
        private void MakeVertexVoxels(IList<Vertex> vertices, byte level, double[][] transformedCoordinates)
        {
            Parallel.ForEach(vertices, vertex =>
            //foreach (var vertex in vertices)
            {
                int[] intCoords = intCoordsForVertex(vertex, transformedCoordinates);
                var id = Constants.MakeIDFromCoordinates(intCoords, singleCoordinateShifts[level]);
                var voxel0 = MakeLevel0Voxel(id);
                var voxel = level == 0 ? voxel0.ID : MakeAndStorePartialVoxel(id, level, voxel0);
                LinkVoxelToTessellatedObject(voxel, voxel0, vertex);
            }
            );
        }
        /// <summary>
        /// This is a helper function for the previous function. Extra care is take when the actual coords of the vertex
        /// lie on the boundary between voxels.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>System.Int32[].</returns>
        private int[] intCoordsForVertex(Vertex v, double[][] transformedCoordinates)
        {
            var coordinates = transformedCoordinates[v.IndexInList];
            int[] intCoords;
            if (coordinates.Any(Constants.atIntegerValue))
            {
                intCoords = new int[3];
                var edgeVectors = v.Edges.Select(e => e.To == v ? e.Vector : e.Vector.multiply(-1))
                    .ToList();
                if (Constants.atIntegerValue(coordinates[0]) && edgeVectors.All(ev => ev[0] >= 0))
                    intCoords[0] = (int)(coordinates[0] - 1);
                else intCoords[0] = (int)coordinates[0];
                if (Constants.atIntegerValue(coordinates[1]) && edgeVectors.All(ev => ev[1] >= 0))
                    intCoords[1] = (int)(coordinates[1] - 1);
                else intCoords[1] = (int)coordinates[1];
                if (Constants.atIntegerValue(coordinates[2]) && edgeVectors.All(ev => ev[2] >= 0))
                    intCoords[2] = (int)(coordinates[2] - 1);
                else intCoords[2] = (int)coordinates[2];
            }
            else intCoords = new[] { (int)coordinates[0], (int)coordinates[1], (int)coordinates[2] };

            return intCoords;
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
        private void MakeVoxelsForFacesAndEdges(IList<PolygonalFace> faces, byte level, double[][] transformedCoordinates)
        {
            Parallel.ForEach(faces, face =>
            //foreach (var face in faces) //loop over the faces
            {
                if (simpleCase(face, level, transformedCoordinates)) return; //continue;

                setUpFaceSweepDetails(face, transformedCoordinates, out var startVertex, out var sweepDim, out var maxSweepValue);
                var leftStartPoint = (double[])transformedCoordinates[startVertex.IndexInList].Clone();
                var sweepValue = (int)(Constants.atIntegerValue(leftStartPoint[sweepDim])
                    ? leftStartPoint[sweepDim] + 1
                    : Math.Ceiling(leftStartPoint[sweepDim]));

                var sweepIntersections = new Dictionary<int, List<double[]>>();
                foreach (var edge in face.Edges)
                {
                    var toIntCoords = intCoordsForVertex(edge.To, transformedCoordinates);
                    var fromIntCoords = intCoordsForVertex(edge.From, transformedCoordinates);
                    if (toIntCoords[0] == fromIntCoords[0] && toIntCoords[1] == fromIntCoords[1] &&
                        toIntCoords[2] == fromIntCoords[2]) continue;
                    makeVoxelsForLine(transformedCoordinates[edge.From.IndexInList],
                        transformedCoordinates[edge.To.IndexInList], edge, sweepDim, sweepIntersections,
                        level);
                }
                while (sweepValue <= maxSweepValue) // this is the sweep along the face
                {
                    if (sweepIntersections.ContainsKey(sweepValue))
                    {
                        var intersections = sweepIntersections[sweepValue];
                        if (intersections.Count == 2
                        )
                            makeVoxelsForLineOnFace(intersections[0], intersections[1], face, sweepDim, level);
                    }
                    sweepValue++; //increment sweepValue and repeat!
                }
            });
        }

        private int[][] setLimits(long parent, byte level)
        {
            var parentLimits = new int[2][];
            parentLimits[0] = level == 0 ? new int[3] : Constants.GetCoordinateIndices(parent, singleCoordinateShifts[level]);
            parentLimits[1] = new[]
            {
                parentLimits[0][0]+ voxelsPerSide[level]-1,
                parentLimits[0][1]+ voxelsPerSide[level]-1,
                parentLimits[0][2]+ voxelsPerSide[level]-1
                };
            return parentLimits;
        }

        /// <summary>
        /// If it is a simple case, just solve it and return true.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="voxels">The voxels.</param>
        /// <param name="level">The level.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool simpleCase(PolygonalFace face, byte level, double[][] transformedCoordinates)
        {
            var aCoords = intCoordsForVertex(face.A, transformedCoordinates);
            var bCoords = intCoordsForVertex(face.B, transformedCoordinates);
            var cCoords = intCoordsForVertex(face.C, transformedCoordinates);
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
                makeVoxelsForFaceInCardinalLine(face, 2, level, aCoords, bCoords[2], cCoords[2], transformedCoordinates);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (aCoords[0] == bCoords[0] && aCoords[0] == cCoords[0] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 1, level, aCoords, bCoords[1], cCoords[1], transformedCoordinates);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (aCoords[1] == bCoords[1] && aCoords[1] == cCoords[1] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 0, level, aCoords, bCoords[0], cCoords[0], transformedCoordinates);
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
        private void makeVoxelsForFaceInCardinalLine(PolygonalFace face, int dim, byte level, int[] aCoords, int bValue, int cValue,
            double[][] transformedCoordinates)
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
            //if (minCoord < parentLimits[0][dim]) minCoord = parentLimits[0][dim];
            //if (maxCoord >= parentLimits[1][dim]) maxCoord = parentLimits[1][dim] - 1;
            var coordinates = (int[])aCoords.Clone();
            for (var i = minCoord; i <= maxCoord; i++)
            {
                // set up voxels for the face
                coordinates[dim] = i;
                var voxel = Constants.MakeIDFromCoordinates(coordinates, singleCoordinateShifts[level]);
                var voxel0 = MakeLevel0Voxel(voxel);
                voxel = level == 0 ? voxel0.ID : MakeAndStorePartialVoxel(voxel, level, voxel0);
                LinkVoxelToTessellatedObject(voxel, voxel0, face);
                if (level <= LevelAtWhichLinkToTessellation)
                { // this is just to add links to the edges
                    var btmDim = Constants.GetCoordinateIndex(voxel, dim, singleCoordinateShifts[level]);
                    foreach (var faceEdge in face.Edges)
                    {
                        // cycle over the edges to link them to the voxels
                        var fromDim = transformedCoordinates[faceEdge.From.IndexInList][dim];
                        var toDim = transformedCoordinates[faceEdge.To.IndexInList][dim];
                        if ((fromDim < btmDim && toDim > btmDim) ||
                            (fromDim > btmDim && toDim < btmDim) ||
                            (fromDim < btmDim + 1 && toDim > btmDim + 1) ||
                            (fromDim > btmDim + 1 && toDim < btmDim + 1))
                            LinkVoxelToTessellatedObject(voxel, voxel0, faceEdge);
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
        private void setUpFaceSweepDetails(PolygonalFace face, double[][] transformedCoordinates, out Vertex startVertex, out int sweepDim,
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
            // but it is okay since these are simply scaled and compared to one another
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
            int sweepDim, Dictionary<int, List<double[]>> sweepIntersections, byte level)
        {
            //Get every X,Y, and Z integer value intersection
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new[] { new List<double[]>(), new List<double[]>(), new List<double[]>() };
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
            addVoxelsAtIntersections(intersections, level, tsObject);
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
        private static void getIntegerIntersectionsAlongLine(double[] startPoint, double[] endPoint,
            int dim, List<double[]>[] intersections, double[] vectorNorm = null)
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
                //If going reverse, do not decrement until after using this voxel index.
                if (!forwardX) t--;
            }
        }

        private void makeVoxelsForLineOnFace(double[] startPoint, double[] endPoint, TessellationBaseClass tsObject,
           int sweepDim, byte level)
        {
            //if (allPointsOnOneSideOfLimits(limits, startPoint, endPoint)) return;
            //Get every X, Y, and Z integer value intersection, not including the sweepDim, which won't have any anyways
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new[] { new List<double[]>(), new List<double[]>(), new List<double[]>() };

            for (var i = 0; i < 3; i++)
            {
                if (i == sweepDim) continue;
                getIntegerIntersectionsAlongLine(startPoint, endPoint, i, intersections, vectorNorm);
            }
            addVoxelsAtIntersections(intersections, level, tsObject);
        }

        private void addVoxelsAtIntersections(List<double[]>[] intersections, byte level, TessellationBaseClass tsObject)
        {
            var ijk = new int[3];
            var dimensionsAsIntegers = new bool[3];
            foreach (var intersectionSet in intersections)
            {
                foreach (var intersection in intersectionSet)
                {
                    var numAsInt = 0;
                    //Convert the intersection values to integers. 
                    for (int i = 0; i < 3; i++)
                    {
                        if (Constants.atIntegerValue(intersection[i]))
                        {
                            dimensionsAsIntegers[i] = true;
                            numAsInt++;
                        }
                        else dimensionsAsIntegers[i] = false;
                        ijk[i] = (int)intersection[i];
                    }
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

                    //The simplest and most robust solution I can think of is to add voxels at all the decremented integer 
                    //intersections. For voxel edge intersections, this forms 4 voxels around the intersection. For voxel
                    //corner intersections, this forms 8 voxels around the intersection. This can be expressed as:
                    //numVoxels = 2^numAsInt
                    var numVoxels = 0;
                    foreach (var combination in TVGL.Constants.TessellationToVoxelizationIntersectionCombinations)
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
                        var voxel = Constants.MakeIDFromCoordinates(newIjk, singleCoordinateShifts[level]);
                        var voxel0 = MakeLevel0Voxel(voxel);
                        voxel = level == 0 ? voxel0.ID : MakeAndStorePartialVoxel(voxel, level, voxel0);
                        LinkVoxelToTessellatedObject(voxel, voxel0, tsObject);
                        numVoxels++;
                    }
                    if (numVoxels != (int)Math.Pow(2, numAsInt)) throw new Exception("Error in implementation");
                }
            }

        }
        #endregion

        #region Interior Voxel Creation

        private const double startingMinFaceToCornerDistance = 1.00000001;// this starts close to 1.0 because the voxel has length of one (at 
                                                                          // least within the current scaling). It's possible that faces are intersected but farther away than
                                                                          // this voxel. This is for when the voxel knows its faces as well as when looking over the parent set. It may be
                                                                          // completely fine to start at 1.0 instead of 1.001 but I wanted a definitive value slightly greater than one for 
                                                                          // a conditional statement used below, plus I wanted to be a little practical if the face was just slightly
                                                                          // away from the voxel but - in all practicality intersect in a way that can tell us about the bottom corner.
                                                                          /// <summary>
                                                                          /// For each of the discovered partial voxels, we now need to determine is the bottom coordinate inside or not. This is
                                                                          /// important for filling in the internal voxels. We are to know for one true point whether or not it is inside or not.
                                                                          /// </summary>
                                                                          /// <param name="voxels">The voxels.</param>
                                                                          /// <param name="parentFaces">The parent faces.</param>
                                                                          /// <param name="parentBtmCoordIsInside">if set to <c>true</c> [parent BTM coord is inside].</param>
        private List<long> DefineBottomCoordinateInside(IEnumerable<long> partialChildren, VoxelBinClass voxel0, byte level,
                                                                          bool parentBtmCoordIsInside, IEnumerable<PolygonalFace> parentFaces, double[][] transformedCoordinates)
        {
            // This is a tricky function that has been rewritten alot in the first half of 2018. The four steps are arranged to 
            // quickly solve easy cases before a longer process to find the remaining ones.
            var queue = new Queue<long>();
            var assignedHashSet = new VoxelHashSet(level, bitLevelDistribution);
            foreach (var voxel in partialChildren)
            {
                long newVoxel;
                #region Step 1: if the face has all positive normals or all negative then quick and easy
                List<PolygonalFace> faces = GetFacesToCheck(voxel, level, voxel0);
                if (faces.SelectMany(f => f.Normal).All(n => n > 0))
                {
                    newVoxel = Constants.SetBtmCoordIsInside(voxel, true);
                    assignedHashSet.AddOrReplace(newVoxel);
                    continue;
                }
                if (faces.SelectMany(f => f.Normal).All(n => n < 0))
                {
                    newVoxel = Constants.SetBtmCoordIsInside(voxel, false);
                    assignedHashSet.AddOrReplace(newVoxel);
                    continue;
                }
                #endregion
                #region Step 2: check if a face intersects with local coordinate frame
                // in the next loop, we check to see if a face intersects with the local coordinate frame of the voxel.
                // That is, the 3 lines  going from 0,0,0 to 1,0,0 or 0,1,0 or 0,0,1. We 
                var voxCoord = Constants.GetCoordinateIndices(voxel, singleCoordinateShifts[level]);
                var closestFaceIsPositive = false;
                var closestFaceDistance = startingMinFaceToCornerDistance;
                foreach (var face in faces)
                {
                    for (int i = 0; i < 3; i++)
                        if (lineToFaceIntersection(face, voxCoord, i, closestFaceDistance, transformedCoordinates, out var signedDistance))
                        {
                            closestFaceDistance = signedDistance;
                            closestFaceIsPositive = face.Normal[i] > 0;
                        }
                }
                if (closestFaceDistance <= 1.0)
                {
                    newVoxel = Constants.SetBtmCoordIsInside(voxel, closestFaceIsPositive);
                    assignedHashSet.AddOrReplace(newVoxel);
                }
                #endregion
                // if not, then add the voxel to a queue for step 3
                else queue.Enqueue(voxel);
            }
            #region Step 3: infer from neighbors
            // those that we weren't able to get from Step 2 may be neighbors of ones that were figured out from one and two
            // this is why the successes in the above were also put onto the "assignedHashSet". We now know that those on the queue
            // have no faces along their coordinate axes, so we use this to see if the neighbor is inside, then we know the one in
            // question is inside. We also move that to the "assignedHashSet" so that it can aid other unknowns in the queue. 
            var cyclesSinceLastSuccess = 0;
            while (queue.Any() && queue.Count > cyclesSinceLastSuccess)
            {  // note the "cyclesSincesLastSuccess". This allows us to go one full additional pass through any remaining in the queue
                // to see if they can be determined from their recently determined neighbors that were formerly on the queue
                var voxel = queue.Dequeue();
                var voxCoord = Constants.GetCoordinateIndices(voxel, singleCoordinateShifts[level]);
                var gotFromNeighbor = false;
                for (int i = 0; i < 3; i++)
                {
                    var neighbor = GetNeighborForTSBuilding(voxCoord, (VoxelDirections)(i + 1), assignedHashSet,
                        level, out var neighborCoord);
                    if (neighbor == 0) continue;
                    voxel = Constants.SetBtmCoordIsInside(voxel, Constants.GetIfBtmIsInside(neighbor));
                    assignedHashSet.AddOrReplace(voxel);
                    cyclesSinceLastSuccess = 0;
                    gotFromNeighbor = true;
                    break;
                }
                if (!gotFromNeighbor)
                {
                    queue.Enqueue(voxel);
                    cyclesSinceLastSuccess++;
                }
            }
            #endregion
            #region Step 4 look to the parent
            while (queue.Any())
            {  // check the vector connection the btmCoordinate to the parent's btmCoordinate. Why? 1) the parent is known (at this point) and if nothing intersects
                // this line then it is the same of the parent, 2) the vector back to the parent has no positive values which simplifies the check with the face normals,
                // 3) has a superset of the tesselated elements
                var voxel = queue.Dequeue();
                var voxCoord = Constants.GetCoordinateIndices(voxel, singleCoordinateShifts[level]);
                var closestFaceIsPositive = false;
                double minDistance = startingMinFaceToCornerDistance;
                var btmIsInside = false;
                var foundIntersectingFace = false;
                var line = voxCoord.Select(x => (double)(x % voxelsPerSide[level])).ToArray().multiply(-1);
                foreach (var face in parentFaces)
                {
                    if (lineToFaceIntersection(face, voxCoord, line, minDistance, out var signedDistance, transformedCoordinates))
                        if (signedDistance < minDistance)
                        {
                            foundIntersectingFace = true;
                            btmIsInside = face.Normal.dotProduct(line) >= 0;
                            minDistance = signedDistance;
                        }
                }
                voxel = Constants.SetBtmCoordIsInside(voxel, foundIntersectingFace ? btmIsInside : parentBtmCoordIsInside);
                assignedHashSet.AddOrReplace(voxel);
            }
            #endregion
            return assignedHashSet.ToList();
        }

        private void DefineBottomCoordinateInsideLevel0(IEnumerable<PolygonalFace> parentFaces, double[][] transformedCoordinates)
        {
            // This is a tricky function that has been rewritten alot in the first half of 2018. The four steps are arranged to 
            // quickly solve easy cases before a longer process to find the remaining ones.
            var queue = new Queue<VoxelBinClass>();
            var assignedHashSet = new HashSet<VoxelBinClass>();
            foreach (var voxel in voxelDictionaryLevel0)
            {
                #region Step 1: if the face has all positive normals or all negative then quick and easy
                List<PolygonalFace> faces = GetFacesToCheck(voxel.ID, 0, (VoxelBinClass)voxel);
                if (faces.SelectMany(f => f.Normal).All(n => n > 0))
                {
                    voxel.BtmCoordIsInside = true;
                    assignedHashSet.Add((VoxelBinClass)voxel);
                    continue;
                }
                if (faces.SelectMany(f => f.Normal).All(n => n < 0))
                {
                    voxel.BtmCoordIsInside = false;
                    assignedHashSet.Add((VoxelBinClass)voxel);
                    continue;
                }
                #endregion
                #region Step 2: check if a face intersects with local coordinate frame
                // in the next loop, we check to see if a face intersects with the local coordinate frame of the voxel.
                // That is, the 3 lines  going from 0,0,0 to 1,0,0 or 0,1,0 or 0,0,1. We 
                var voxCoord = voxel.CoordinateIndices;
                var closestFaceIsPositive = false;
                var closestFaceDistance = startingMinFaceToCornerDistance;
                foreach (var face in faces)
                {
                    for (int i = 0; i < 3; i++)
                        if (lineToFaceIntersection(face, voxCoord, i, closestFaceDistance, transformedCoordinates, out var signedDistance))
                        {
                            closestFaceDistance = signedDistance;
                            closestFaceIsPositive = face.Normal[i] > 0;
                        }
                }
                if (closestFaceDistance <= 1.0)
                {
                    voxel.BtmCoordIsInside = closestFaceIsPositive;
                    assignedHashSet.Add(voxel);
                }
                #endregion
                // if not, then add the voxel to a queue for step 3
                else queue.Enqueue(voxel);
            }
            #region Step 3: infer from neighbors
            // those that we weren't able to get from Step 2 may be neighbors of ones that were figured out from one and two
            // this is why the successes in the above were also put onto the "assignedHashSet". We now know that those on the queue
            // have no faces along their coordinate axes, so we use this to see if the neighbor is inside, then we know the one in
            // question is inside. We also move that to the "assignedHashSet" so that it can aid other unknowns in the queue. 
            var cyclesSinceLastSuccess = 0;
            while (queue.Any() && queue.Count > cyclesSinceLastSuccess)
            {  // note the "cyclesSincesLastSuccess". This allows us to go one full additional pass through any remaining in the queue
                // to see if they can be determined from their recently determined neighbors that were formerly on the queue
                var voxel = queue.Dequeue();
                var voxCoord = voxel.CoordinateIndices;
                var gotFromNeighbor = false;
                for (int i = 0; i < 3; i++)
                {
                    var neighbor = GetNeighborForTSBuildingLevel0(voxCoord, (VoxelDirections)(i + 1), out var neighborCoord);
                    if (neighbor == null || !assignedHashSet.Contains(neighbor)) continue;
                    voxel.BtmCoordIsInside = neighbor.BtmCoordIsInside;
                    assignedHashSet.Add(voxel);
                    cyclesSinceLastSuccess = 0;
                    gotFromNeighbor = true;
                    break;
                }
                if (!gotFromNeighbor)
                {
                    queue.Enqueue(voxel);
                    cyclesSinceLastSuccess++;
                }
            }
            #endregion
            if (!queue.Any()) return;
            #region Step 4 look to the parent
            while (queue.Any())
            {  // check the vector connection the btmCoordinate to the parent's btmCoordinate. Why? 1) the parent is known (at this point) and if nothing intersects
                // this line then it is the same of the parent, 2) the vector back to the parent has no positive values which simplifies the check with the face normals,
                // 3) has a superset of the tesselated elements
                var voxel = queue.Dequeue();
                var voxCoord = voxel.CoordinateIndices;
                var closestFaceIsPositive = false;
                double minDistance = startingMinFaceToCornerDistance;
                var btmIsInside = false;
                var foundIntersectingFace = false;
                var line = (new[] { (double)voxCoord[0], voxCoord[1], voxCoord[2] }).multiply(-1);
                foreach (var face in parentFaces)
                {
                    if (lineToFaceIntersection(face, voxCoord, line, minDistance, out var signedDistance, transformedCoordinates))
                        if (signedDistance < minDistance)
                        {
                            btmIsInside = face.Normal.dotProduct(line) >= 0;
                            minDistance = signedDistance;
                        }
                }
                voxel.BtmCoordIsInside = btmIsInside;
            }
            #endregion
        }


        private List<PolygonalFace> GetFacesToCheck(long voxel, byte level, VoxelBinClass voxel0)
        {
            if (voxel0.tsElementsForChildVoxels == null) return null;
            if (level <= LevelAtWhichLinkToTessellation)
                return voxel0.tsElementsForChildVoxels[voxel].Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
            return voxel0.tsElementsForChildVoxels[MakeParentVoxelID(voxel, LevelAtWhichLinkToTessellation)].Where(te => te is PolygonalFace)
                        .Cast<PolygonalFace>().ToList();
        }

        private bool lineToFaceIntersection(PolygonalFace face, int[] c, int dimension, double upperLimit, double[][] transformedCoordinates, out double signedDistance)
        {
            var n = face.Normal;
            var d = n.dotProduct(transformedCoordinates[face.Vertices[0].IndexInList]);
            if (n[dimension].IsNegligible())
            {
                signedDistance = double.NaN;
                return false;
            }
            signedDistance = (d - n[0] * c[0] - n[1] * c[1] - n[2] * c[2]) / n[dimension];
            if (signedDistance >= 0 && signedDistance < upperLimit)
            {
                var intersection = new double[] { c[0], c[1], c[2] };
                intersection[dimension] += signedDistance;
                if (isPointInsideFaceTSBuilding(face, intersection, transformedCoordinates))
                    return true;
            }
            return false;
        }
        private bool lineToFaceIntersection(PolygonalFace face, int[] c, double[] direction, double upperLimit,
            out double signedDistance, double[][] transformedCoordinates)
        {
            var n = face.Normal;
            var d = n.dotProduct(transformedCoordinates[face.Vertices[0].IndexInList]);
            var normalDottedWithDirection = n.dotProduct(direction);
            if (normalDottedWithDirection.IsNegligible())
            {
                signedDistance = double.NaN;
                return false;
            }
            signedDistance = (d - n[0] * c[0] - n[1] * c[1] - n[2] * c[2]) / normalDottedWithDirection;
            if (signedDistance >= 0 && signedDistance < upperLimit)
            {
                if (isPointInsideFaceTSBuilding(face, c.add(direction.multiply(signedDistance)), transformedCoordinates))
                    return true;
            }
            return false;
        }
        private bool isPointInsideFaceTSBuilding(PolygonalFace face, double[] p, double[][] transformedCoordinates)
        {
            var a = transformedCoordinates[face.A.IndexInList];
            var b = transformedCoordinates[face.B.IndexInList];
            var c = transformedCoordinates[face.C.IndexInList];
            var line = b.subtract(a, 3);
            var dot = line.crossProduct(p.subtract(a, 3)).dotProduct(face.Normal);
            if (!dot.IsNegligible(TVGL.Constants.BaseTolerance) && dot < 0) return false;

            line = c.subtract(b, 3);
            dot = line.crossProduct(p.subtract(b, 3)).dotProduct(face.Normal);
            if (!dot.IsNegligible(TVGL.Constants.BaseTolerance) && dot < 0) return false;

            line = a.subtract(c, 3);
            dot = line.crossProduct(p.subtract(c, 3)).dotProduct(face.Normal);
            if (!dot.IsNegligible(TVGL.Constants.BaseTolerance) && dot < 0) return false;

            return true;
        }

        private long GetNeighborForTSBuilding(int[] point3D, VoxelDirections direction, VoxelHashSet voxelHashSet, int level, out int[] neighborCoord)
        {
            neighborCoord = new[] { point3D[0], point3D[1], point3D[2] };
            var positiveStep = direction > 0;
            var step = positiveStep ? 1 : -1;
            var dimension = Math.Abs((int)direction) - 1;

            #region Check if steps outside or neighbor has different parent
            var coordValue = point3D[dimension];
            var maxValue = Constants.MaxForSingleCoordinate >> singleCoordinateShifts[level];
            if ((coordValue == 0 && !positiveStep) || (positiveStep && coordValue == maxValue))
                //then stepping outside of entire bounds!
                return 0;
            var maxForThisLevel = voxelsPerSide[level] - 1;
            var justThisLevelCoordValue = coordValue & maxForThisLevel;
            if ((justThisLevelCoordValue == 0 && !positiveStep) ||
                (justThisLevelCoordValue == maxForThisLevel && positiveStep))
                return 0;
            #endregion
            neighborCoord[dimension] += step;
            var neighborID = Constants.MakeIDFromCoordinates(neighborCoord, singleCoordinateShifts[level]);
            return voxelHashSet.GetVoxel(neighborID);
        }
        private VoxelBinClass GetNeighborForTSBuildingLevel0(int[] point3D, VoxelDirections direction, out int[] neighborCoord)
        {
            neighborCoord = new[] { point3D[0], point3D[1], point3D[2] };
            var positiveStep = direction > 0;
            var step = positiveStep ? 1 : -1;
            var dimension = Math.Abs((int)direction) - 1;
            neighborCoord[dimension] += step;
            if (neighborCoord[dimension] < 0 || neighborCoord[dimension] >= voxelDictionaryLevel0.size[dimension])
                return null;
            return voxelDictionaryLevel0.GetVoxel(neighborCoord);
        }

        /// <summary>
        /// Makes the voxels in interior.
        /// </summary>
        private void makeVoxelsInInteriorLevel0(double[][] transformedCoordinates)
        {
            var allDirections = new[] { -3, -2, -1, 1, 2, 3 };
            var insiders = new Stack<VoxelBinClass>();
            var outsiders = new Stack<VoxelBinClass>();
            /* separate the partial/surface voxels into insiders and outsiders.  */
            foreach (var voxel in voxelDictionaryLevel0)
                if (voxel.BtmCoordIsInside) insiders.Push((VoxelBinClass)voxel);
                else outsiders.Push((VoxelBinClass)voxel);
            /* the outsiders are done first as these may create insiders that are useful to finding other insiders.
             * Note, that new interior voxels created here (20 lines down) are added to the insider queue. */
            // debug: potential problem!!! what if there are no outsiders or insiders but there are unknown partials?
            while (outsiders.Any())
            {
                var current = outsiders.Pop();
                var coord = current.CoordinateIndices;
                /* We are going to cycle of the posDirections only. If the positive neighbor is null, then it could be
                 * a new interior if we cross over a face going in that positive direction. If the positive neighbor is
                 * an unknown, then we should be able to resolve it. */
                for (int direction = 0; direction < 3; direction++)
                {
                    if (coord[direction] == voxelDictionaryLevel0.size[direction] - 1) continue;
                    var neighbor = GetNeighborForTSBuildingLevel0(coord, (VoxelDirections)(direction + 1), out var neighborCoord);
                    /* if neighbor is null, then there is a possibility to add it - otherwise, move on to the next */
                    if (neighbor != null) continue;
                    /* get the faces that goes through the current face */
                    var faces = GetFacesToCheck(current.ID, 0, current);
                    /* so, if the farthest face in this positive direction is pointing in the negative direction, then
                     * that neighboring voxel is inside. */
                    if (farthestFaceIsNegativeDirection(faces, coord, direction, transformedCoordinates, false))
                    {
                        var id = Constants.MakeIDFromCoordinates(neighborCoord, singleCoordinateShifts[0]);
                        neighbor = new VoxelBinClass(id, VoxelRoleTypes.Full, this);
                        voxelDictionaryLevel0.AddOrReplace(neighbor);
                        insiders.Push(neighbor);
                    }
                }
            }
            insiders = new Stack<VoxelBinClass>(insiders);
            while (insiders.Any())
            {
                var current = insiders.Pop();
                //var directions = current.Role == VoxelRoleTypes.Full ? allDirections : negDirections;
                var coord = current.CoordinateIndices;
                foreach (var direction in allDirections)
                {
                    // check to see if current is not going put the neighbor outside of the boundary 
                    if ((direction < 0 && coord[-direction - 1] == 0) || (direction > 0 && coord[direction - 1] == voxelDictionaryLevel0.size[direction - 1] - 1))
                        continue;
                    var neighbor = GetNeighborForTSBuildingLevel0(coord, (VoxelDirections)direction, out var neighborCoord);
                    if (neighbor == null)
                    {
                        var addTheNeighbor = true;
                        if (direction > 0 && current.Role == VoxelRoleTypes.Partial)
                        {
                            /* get the faces that goes through the current face */
                            var faces = GetFacesToCheck(current.ID, 0, current);
                            //addTheNeighbor = !faces.Any(f => lineToFaceIntersection(f, coord, direction - 1, 1, out var signedDistance));
                            addTheNeighbor = farthestFaceIsNegativeDirection(faces, coord, direction - 1, transformedCoordinates, true);
                        }
                        if (addTheNeighbor)
                        {
                            var id = Constants.MakeIDFromCoordinates(neighborCoord, singleCoordinateShifts[0]);
                            neighbor = new VoxelBinClass(id, VoxelRoleTypes.Full, this);
                            voxelDictionaryLevel0.AddOrReplace(neighbor);
                            insiders.Push(neighbor);
                        }
                    }
                }
            }
        }
        private void makeVoxelsInInterior(VoxelHashSet voxels, IEnumerable<long> partialChildren, byte level,
            long parent, VoxelBinClass voxel0, double[][] transformedCoordinates)
        {
            Constants.GetAllFlags(parent, out var parentLevel, out var parentRole, out var parentBtmIsInside);
            if (!partialChildren.Any() && parentBtmIsInside)
            {
                voxels.AddRange(AddAllDescendants(Constants.ClearFlagsFromID(parent), parentLevel));
                return;
            }
            /* define the box of the parent in terms of lower and upper arrays */
            var parentLimits = setLimits(parent, level);
            var allDirections = new[] { -3, -2, -1, 1, 2, 3 };
            var posDirections = new[] { 0, 1, 2 };
            var insiders = new Stack<long>();
            var outsiders = new Stack<long>();
            /* separate the partial/surface voxels into insiders and outsiders.  */
            foreach (var voxel in partialChildren)
                if (Constants.GetIfBtmIsInside(voxel)) insiders.Push(voxel);
                else outsiders.Push(voxel);
            /* the outsiders are done first as these may create insiders that are useful to finding other insiders.
             * Note, that new interior voxels created here (20 lines down) are added to the insider queue. */
            while (outsiders.Any())
            {
                var current = outsiders.Pop();
                var coord = Constants.GetCoordinateIndices(current, singleCoordinateShifts[level]);
                /* We are going to cycle of the posDirections only. If the positive neighbor is null, then it could be
                 * a new interior if we cross over a face going in that positive direction. If the positive neighbor is
                 * an unknown, then we should be able to resolve it. */
                foreach (var direction in posDirections)
                {
                    if (coord[direction] == parentLimits[1][direction]) continue;   /* check to see if current is on a boundary.
                    if so, just skip this one. */
                    var neighbor = GetNeighborForTSBuilding(coord, (VoxelDirections)(direction + 1), voxels, level, out var neighborCoord);
                    /* if neighbor is null, then there is a possibility to add it - otherwise, move on to the next */
                    if (neighbor != 0) continue;
                    /* get the faces that goes through the current face */
                    var faces = GetFacesToCheck(current, level, voxel0);
                    /* so, if the farthest face in this positive direction is pointing in the negative direction, then
                     * that neighboring voxel is inside. */
                    if (farthestFaceIsNegativeDirection(faces, coord, direction, transformedCoordinates, false))
                    {
                        neighbor = MakeAndStoreFullVoxel(neighborCoord, level, voxels);
                        insiders.Push(neighbor);
                    }
                }
            }
            insiders = new Stack<long>(insiders);
            while (insiders.Any())
            {
                var current = insiders.Pop();
                //var directions = current.Role == VoxelRoleTypes.Full ? allDirections : negDirections;
                var coord = Constants.GetCoordinateIndices(current, singleCoordinateShifts[level]);
                foreach (var direction in allDirections)
                {  // check to see if current is not going put the neighbor outside of the boundary 
                    if ((direction < 0 && coord[-direction - 1] == parentLimits[0][-direction - 1]) ||
                        (direction > 0 && coord[direction - 1] == parentLimits[1][direction - 1])) continue;
                    var neighbor = GetNeighborForTSBuilding(coord, (VoxelDirections)direction, voxels, level,
                        out var neighborCoord);
                    if (neighbor != 0) continue;
                    var addTheNeighbor = true;
                    if (direction > 0 && Constants.GetRole(current) == VoxelRoleTypes.Partial)
                    {
                        /* get the faces that goes through the current face */
                        var faces = GetFacesToCheck(current, level, voxel0);
                        //addTheNeighbor = !faces.Any(f => lineToFaceIntersection(f, coord, direction - 1, 1, out var signedDistance));
                        addTheNeighbor = farthestFaceIsNegativeDirection(faces, coord, direction - 1, transformedCoordinates, true);
                    }
                    if (addTheNeighbor)
                    {
                        neighbor = MakeAndStoreFullVoxel(neighborCoord, level, voxels);
                        insiders.Push(neighbor);
                    }
                }
            }
        }


        private bool farthestFaceIsNegativeDirection(List<PolygonalFace> faces, int[] coord, int dimension, double[][] transformedCoordinates,
            bool defaultIfNoIntersect)
        {
            var negativeFace = defaultIfNoIntersect;
            double maxDistance = 0.0;
            foreach (var face in faces)
            {
                if (lineToFaceIntersection(face, coord, dimension, 1, transformedCoordinates, out var signedDistance))
                    //if (lineToFaceIntersection(face, coord, dimension, startingMinFaceToCornerDistance, out var signedDistance))
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
        private VoxelBinClass MakeLevel0Voxel(long id)
        {
            VoxelBinClass voxel0;
            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            lock (voxelDictionaryLevel0)
            {
                voxel0 = voxelDictionaryLevel0.GetVoxel(id);
                if (voxel0 == null)
                {
                    voxel0 = new VoxelBinClass(MakeParentVoxelID(id, 0), VoxelRoleTypes.Partial, this);
                    voxelDictionaryLevel0.AddOrReplace(voxel0);
                }
            }
            return voxel0;
        }

        private long MakeAndStorePartialVoxel(long id, byte level, VoxelBinClass voxel0)
        {
            // make the new Voxel, and add it to the proper hashset
            var voxel = Constants.ClearFlagsFromID(id) + Constants.MakeFlags(level, VoxelRoleTypes.Partial);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            // we may need to create the parent. we don't need to create the parent for level-1, since that would
            // have occured in the "lock (voxelDictionaryLevel0)" part of this function
            if (level > 1)
            {
                var parentVoxelQuery = MakeParentVoxelID(id, level - 1);
                long parentVoxelFound;
                lock (voxel0.InnerVoxels[level - 2])
                    parentVoxelFound = voxel0.InnerVoxels[level - 2].GetVoxel(parentVoxelQuery);
                if (parentVoxelFound == 0) MakeAndStorePartialVoxel(parentVoxelQuery, (byte)(level - 1), voxel0);
            }
            return voxel;
        }

        private void LinkVoxelToTessellatedObject(long voxel, VoxelBinClass voxel0, TessellationBaseClass tsObject)
        {
            var level = Constants.GetLevel(voxel);
            if (level <= LevelAtWhichLinkToTessellation)
            {
                lock (voxel0)
                {
                    if (voxel0.tsElementsForChildVoxels == null)
                        voxel0.tsElementsForChildVoxels =
                            new Dictionary<long, HashSet<TessellationBaseClass>>(
                                new VoxelToTessellationComparer(bitLevelDistribution[0]));
                }

                lock (voxel0.tsElementsForChildVoxels)
                {
                    if (voxel0.tsElementsForChildVoxels.ContainsKey(voxel))
                    {
                        var tsElements = voxel0.tsElementsForChildVoxels[voxel];
                        if (!tsElements.Contains(tsObject)) tsElements.Add(tsObject);
                    }
                    else
                        voxel0.tsElementsForChildVoxels.Add(voxel, new HashSet<TessellationBaseClass> { tsObject });
                }

                if (tsObject is Vertex vertex)
                {
                    foreach (var edge in vertex.Edges)
                        LinkVoxelToTessellatedObject(voxel, voxel0, edge);
                    foreach (var face in vertex.Faces)
                        LinkVoxelToTessellatedObject(voxel, voxel0, face);
                }
                else if (tsObject is Edge edge)
                {
                    LinkVoxelToTessellatedObject(voxel, voxel0, edge.OtherFace);
                    LinkVoxelToTessellatedObject(voxel, voxel0, edge.OwnedFace);
                }

                tsObject.AddVoxel(voxel);
            }
            if (level == 0) return;
            var parentVoxel = GetParentVoxel(voxel);
            if (parentVoxel != 0) LinkVoxelToTessellatedObject(parentVoxel, voxel0, tsObject);
        }

        private long MakeAndStoreFullVoxel(int[] coordinates, int level, VoxelHashSet voxels)
        {
            long voxel = Constants.MakeIDFromCoordinates(coordinates, singleCoordinateShifts[level]) + Constants.MakeFlags(level, VoxelRoleTypes.Full);
            lock (voxels)
                voxels.AddOrReplace(voxel);
            return voxel;
        }

        #endregion
    }
}