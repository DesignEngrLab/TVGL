// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-25-2016
// ***********************************************************************
// <copyright file="AreaDecomposition.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Outputs cross sectional area along a given axis
    /// </summary>
    public static class DirectionalDecomposition
    {
        #region Standard Area Decomposition. Non-uniform.
        /// <summary>
        ///     Runs the specified ts.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="minOffset">The minimum offset.</param>
        /// <param name="ignoreNegativeSpace">if set to <c>true</c> [ignore negative space].</param>
        /// <param name="convexHull2DDecompositon">if set to <c>true</c> [convex hull2 d decompositon].</param>
        /// <param name="boundingRectangleArea">if set to <c>true</c> [bounding rectangle area].</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        /// <exception cref="Exception">Pick one or the other. Can't do both at the same time</exception>
        public static List<double[]> NonUniformAreaDecomposition(TessellatedSolid ts, double[] axis, double stepSize,
            double minOffset = double.NaN, bool ignoreNegativeSpace = false, bool convexHull2DDecompositon = false,
            bool boundingRectangleArea = false)
        {
            //individualFaceAreas = new List<List<double[]>>(); //Plot changes for the area of each flat that makes up a slice. (e.g. 2 positive loop areas)
            if (convexHull2DDecompositon && boundingRectangleArea)
                throw new Exception("Pick one or the other. Can't do both at the same time");

            var outputData = new List<double[]>();
            if (double.IsNaN(minOffset)) minOffset = Math.Sqrt(ts.SameTolerance);
            if (stepSize <= minOffset * 2)
            {
                //"step size must be at least 2x as large as the min offset");
                //Change it rather that throwing an exception
                stepSize = minOffset * 2 + ts.SameTolerance;
            }
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            List<Tuple<Vertex, double>> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] { axis }, ts.Vertices.ToList(), out sortedVertices, out duplicateRanges);

            var edgeListDictionary = new Dictionary<int, Edge>();
            var previousDistanceAlongAxis = sortedVertices[0].Item2; //This value can be negative
            var previousVertexDistance = previousDistanceAlongAxis;
            foreach (var element in sortedVertices)
            {
                var vertex = element.Item1;
                var distanceAlongAxis = element.Item2; //This value can be negative
                var difference1 = distanceAlongAxis - previousDistanceAlongAxis;
                var difference2 = distanceAlongAxis - previousVertexDistance;
                if (difference2 > minOffset && difference1 > stepSize)
                {
                    //Determine cross sectional area for section right after previous vertex
                    var distance = previousVertexDistance + minOffset; //X value (distance along axis) 
                    var cuttingPlane = new Flat(distance, axis);
                    List<List<Edge>> outputEdgeLoops = null;
                    var inputEdgeLoops = new List<List<Edge>>();
                    var area = 0.0;
                    if (convexHull2DDecompositon) area = ConvexHull2DArea(edgeListDictionary, cuttingPlane);
                    else if (boundingRectangleArea) area = BoundingRectangleArea(edgeListDictionary, cuttingPlane);
                    else
                        area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out outputEdgeLoops, inputEdgeLoops,
                            ignoreNegativeSpace); //Y value (area)
                    outputData.Add(new[] { distance, area });

                    //If the difference is far enough, add another data point right before the current vertex
                    //Use the vertex loops provided from the first pass above
                    if (difference2 > 3 * minOffset)
                    {
                        var distance2 = distanceAlongAxis - minOffset; //X value (distance along axis) 
                        cuttingPlane = new Flat(distance2, axis);
                        if (convexHull2DDecompositon) area = ConvexHull2DArea(edgeListDictionary, cuttingPlane);
                        else if (boundingRectangleArea) area = BoundingRectangleArea(edgeListDictionary, cuttingPlane);
                        else
                        {
                            inputEdgeLoops = outputEdgeLoops;
                            area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out outputEdgeLoops,
                                inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
                        }
                        outputData.Add(new[] { distance2, area });
                    }

                    //Update the previous distance used to make a data point
                    previousDistanceAlongAxis = distanceAlongAxis;
                }
                foreach (var edge in vertex.Edges)
                {
                    //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                    //and the second removes it from the list.
                    if (edgeListDictionary.ContainsKey(edge.IndexInList))
                    {
                        edgeListDictionary.Remove(edge.IndexInList);
                    }
                    else
                    {
                        edgeListDictionary.Add(edge.IndexInList, edge);
                    }
                }
                //Update the previous distance of the vertex checked
                previousVertexDistance = distanceAlongAxis;
            }
            return outputData;
        }
        #endregion

        #region Uniform Directional Decomposition
        /// <summary>
        /// Returns the decomposition data found from each slice of the decomposition. This data is used in other methods.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public static List<DecompositionData> UniformAreaDecomposition(TessellatedSolid ts, double[] direction,
            double stepSize)
        {
            var outputData = new List<DecompositionData>();

            List<Vertex> bottomVertices, topVertices;
            var length = MinimumEnclosure.GetLengthAndExtremeVertices(direction, ts.Vertices,
                out bottomVertices, out topVertices);

            //Set step size to an even increment over the entire length of the solid
            stepSize = length / Math.Round(length / stepSize + 1);

            //make the minimum step size 1/10 of the length.
            if (length < 10 * stepSize)
            {
                stepSize = length / 10;
            }

            //Choose whichever min offset is smaller
            var minOffset = Math.Min(Math.Sqrt(ts.SameTolerance), stepSize / 1000);

            //First, sort the vertices along the given axis. Duplicate distances are not important.
            List<Tuple<Vertex, double>> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] { direction }, ts.Vertices, out sortedVertices, out duplicateRanges);

            var edgeListDictionary = new Dictionary<int, Edge>();
            var firstDistance = sortedVertices.First().Item2;
            var furthestDistance = sortedVertices.Last().Item2;
            var distanceAlongAxis = firstDistance;
            var currentVertexIndex = 0;
            var inputEdgeLoops = new List<List<Edge>>();

            while (distanceAlongAxis < furthestDistance - stepSize)
            {
                distanceAlongAxis += stepSize;

                //Update vertex/edge list up until distanceAlongAxis
                for (var i = currentVertexIndex; i < sortedVertices.Count; i++)
                {
                    //Update the current vertex index so that this vertex is not visited again
                    //unless it causes the break ( > distanceAlongAxis), then it will start the 
                    //the next iteration.
                    currentVertexIndex = i;
                    var element = sortedVertices[i];
                    var vertex = element.Item1;
                    var vertexDistanceAlong = element.Item2;
                    //If a vertex is too close to the current distance, move it forward by the min offset.
                    //Update the edge list with this vertex.
                    if (vertexDistanceAlong.IsPracticallySame(distanceAlongAxis, minOffset))
                    {
                        //Move the distance enough so that this vertex is now less than 
                        distanceAlongAxis = vertexDistanceAlong + minOffset * 1.1;
                        //if (vertexDistanceAlong.IsPracticallySame(distanceAlongAxis, minOffset))
                        //{
                        //    throw new Exception("Error in implementation. Need to move the distance further");
                        //}
                    }
                    //Else, Break after we get to a vertex that is further than the distance along axis
                    if (vertexDistanceAlong > distanceAlongAxis)
                    {
                        //consider this vertex again next iteration
                        break;
                    }

                    //Else, it is less than the distance along. Update the edge list
                    //Add the passed vertices to a list so that they can be removed from the sorted vertices

                    //Update the edge dictionary that is used to determine the 3D loops.
                    foreach (var edge in vertex.Edges)
                    {
                        //Reset the input edge loops since we have added an edge
                        inputEdgeLoops = new List<List<Edge>>();

                        //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                        //and the second removes it from the list.
                        if (edgeListDictionary.ContainsKey(edge.IndexInList))
                        {
                            edgeListDictionary.Remove(edge.IndexInList);
                        }
                        else
                        {
                            edgeListDictionary.Add(edge.IndexInList, edge);
                        }
                    }
                }

                //Check to make sure that the minor shifts in the distance in the for loop above 
                //Did not move the distance beyond the furthest distance
                if (distanceAlongAxis > furthestDistance || !edgeListDictionary.Any()) break;
                //Make the slice
                var counter = 0;
                var current3DLoops = new List<List<Vertex>>();
                var successfull = true;
                var cuttingPlane = new Flat(distanceAlongAxis, direction);
                do
                {
                    try
                    {
                        List<List<Edge>> outputEdgeLoops;
                        current3DLoops = GetLoops(edgeListDictionary, cuttingPlane, out outputEdgeLoops,
                            inputEdgeLoops);

                        //Use the same output edge loops for outer while loop, since the edge list does not change.
                        //If there is an error, it will occur before this loop.
                        inputEdgeLoops = outputEdgeLoops;
                    }
                    catch
                    {
                        counter++;
                        distanceAlongAxis += minOffset;
                        successfull = false;
                    }
                } while (!successfull && counter < 4);


                if (successfull)
                {
                    double[,] backTransform;
                    //Get a list of 2D paths from the 3D loops
                    var currentPaths =
                        current3DLoops.Select(
                            cp =>
                                MiscFunctions.Get2DProjectionPointsReorderingIfNecessary(cp, direction,
                                    out backTransform));

                    //Get the area of this layer
                    var area = current3DLoops.Sum(p => MiscFunctions.AreaOf3DPolygon(p, direction));
                    if (area < 0)
                    {
                        //Rather than throwing an exception, just assume the polygons were the wrong direction      
                        Debug.WriteLine(
                            "Area for a cross section in UniformDirectionalDecomposition was negative. This means there was an issue with the polygon ordering");
                    }

                    //Add the data to the output
                    outputData.Add(new DecompositionData(currentPaths, distanceAlongAxis));
                }
                else
                {
                    Debug.WriteLine("Slice at this distance was unsuccessful, even with multiple minimum offsets.");
                }
            }

            //Add the first and last cross sections. 
            //Note, these may not be great fits if step size is large
            outputData.Insert(0, new DecompositionData(outputData.First().Paths, firstDistance));
            outputData.Add(new DecompositionData(outputData.Last().Paths, furthestDistance));

            return outputData;
        }
        #endregion

        #region Additive Volume
        /// <summary>
        /// Gets the additive volume given a list of decomposition data
        /// </summary>
        /// <param name="decompData"></param>
        /// <param name="additiveAccuracy"></param>
        /// <param name="outputData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double AdditiveVolume(List<DecompositionData> decompData, double additiveAccuracy, out List<DecompositionData> outputData)
        {
            outputData = new List<DecompositionData>();
            var previousPolygons = new List<List<Point>>();
            var previousDistance = 0.0;
            var previousArea = 0.0;
            var additiveVolume = 0.0;
            var i = 0;
            var n = decompData.Count;
            foreach (var data in decompData)
            {
                var currentPaths = data.Paths;
                //Offset the distance back by the additive accuracy. THis acts as a vertical offset
                var distance = data.DistanceAlongDirection - additiveAccuracy;
                //currentPaths = PolygonOperations.UnionEvenOdd(currentPaths);

                //Offset if the additive accuracy is significant
                var areaPriorToOffset = MiscFunctions.AreaOfPolygon(currentPaths);
                var offsetPaths = !additiveAccuracy.IsNegligible() ? PolygonOperations.OffsetSquare(currentPaths, additiveAccuracy) : new List<List<Point>>(currentPaths);
                var areaAfterOffset = MiscFunctions.AreaOfPolygon(offsetPaths);
                //Simplify the paths, but remove any that are eliminated (e.g. points are all very close together)
                var simpleOffset = offsetPaths.Select(PolygonOperations.SimplifyFuzzy).Where(simplePath => simplePath.Any()).ToList();
                var areaAfterSimplification = MiscFunctions.AreaOfPolygon(simpleOffset);
                if (areaPriorToOffset > areaAfterOffset) throw new Exception("Path is ordered incorrectly");
                if (!areaAfterOffset.IsPracticallySame(areaAfterSimplification, areaAfterOffset * .05)) throw new Exception("Simplify Fuzzy Alterned the Geometry more than 5% of the area");

                //Union this new set of polygons with the previous set.
                if (previousPolygons.Any()) //If not the first iteration
                {
                    previousPolygons = previousPolygons.Select(PolygonOperations.SimplifyFuzzy).Where(simplePath => simplePath.Any()).ToList();
                    try
                    {
                        currentPaths = new List<List<Point>>(PolygonOperations.Union(previousPolygons, simpleOffset));
                    }
                    catch
                    {
                        var testArea1 = simpleOffset.Sum(p => MiscFunctions.AreaOfPolygon(p));
                        var testArea2 = previousPolygons.Sum(p => MiscFunctions.AreaOfPolygon(p));
                        if (testArea1.IsPracticallySame(testArea2, 0.01))
                        {
                            currentPaths = simpleOffset;
                            //They are probably throwing an error because they are closely overlapping
                        }
                        else
                        {
                            ////Debug Mode
                            //var previousData = outputData.Last();
                            //outputData = new List<DecompositionData>() { previousData, new DecompositionData( currentPaths, distance )};
                            //return 0.0;

                            //Run mode: Use previous path
                            Debug.WriteLine("Union failed and not similar");
                            //
                            currentPaths = outputData.Last().Paths;
                        }
                    }
                }

                //Get the area of this layer
                var area = currentPaths.Sum(p => MiscFunctions.AreaOfPolygon(p));
                if (area < 0)
                {
                    //Rather than throwing an exception, just assume the polygons were the wrong direction      
                    area = -area;
                    Debug.WriteLine("Area for a polygon in the Additive Volume estimate was negative. This means there was an issue with the polygon ordering");
                }

                //This is the first iteration. Add it to the output data.
                if (i == 0)
                {
                    outputData.Add(new DecompositionData(simpleOffset, distance));
                    var area2 = simpleOffset.Sum(p => MiscFunctions.AreaOfPolygon(p));
                    if (area2 < 0)
                    {
                        //Rather than throwing an exception, just assume the polygons were the wrong direction      
                        area2 = -area2;
                        Debug.WriteLine("The first polygon in the Additive Volume estimate was negative. This means there was an issue with the polygon ordering");
                    }
                    additiveVolume += additiveAccuracy * area2;
                }

                //Add the volume from this iteration.
                else if (!previousDistance.IsNegligible())
                {
                    var deltaX = Math.Abs(distance - previousDistance);
                    if (area < previousArea * .99)
                    {
                        ////Debug Mode
                        var previousData = outputData.Last();
                        outputData = new List<DecompositionData>() { previousData, new DecompositionData(currentPaths, distance) };
                        return 0.0;

                        //Run Mode: use previous area
                        Debug.WriteLine("Error in your implementation. This should never occur");
                        area = previousArea;

                    }
                    additiveVolume += deltaX * previousArea;
                    outputData.Add(new DecompositionData(currentPaths, distance));
                }

                //This is the last iteration. Add it to the output data.
                if (i == n - 1)
                {
                    outputData.Add(new DecompositionData(currentPaths, distance + additiveAccuracy));
                    additiveVolume += additiveAccuracy * area;
                }

                previousPolygons = currentPaths;
                previousDistance = distance;
                previousArea = area;
                i++;
            }
            return additiveVolume;
        }
        #endregion

        #region Get Cross Section at a Given Distance
        /// <summary>
        /// Gets the Cross Section for a given distance
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static List<List<Point>> GetCrossSectionAtGivenDistance(TessellatedSolid ts, double[] direction, double distance)
        {
            var crossSection = new List<List<Point>>();

            //First, sort the vertices along the given axis. Duplicate distances are not important.
            List<Tuple<Vertex, double>> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] { direction }, ts.Vertices.ToList(), out sortedVertices, out duplicateRanges);

            var edgeListDictionary = new Dictionary<int, Edge>();
            var previousVertexDistance = sortedVertices[0].Item2; //This value can be negative
            foreach (var element in sortedVertices)
            {
                var vertex = element.Item1;
                var currentVertexDistance = element.Item2; //This value can be negative

                if (currentVertexDistance.IsPracticallySame(distance, ts.SameTolerance) || currentVertexDistance > distance)
                {
                    //Determine cross sectional area for section as close to given distance as possitible (after previous vertex, but before current vertex)
                    //But not actually on the current vertex
                    var distance2 = 0.0;
                    if (currentVertexDistance.IsPracticallySame(distance))
                    {
                        if (previousVertexDistance < distance - ts.SameTolerance)
                        {
                            distance2 = distance - ts.SameTolerance;
                        }
                        else
                        {
                            //Take the average if the function above did not work.
                            distance2 = (previousVertexDistance + currentVertexDistance / 2);
                        }
                    }
                    else
                    {
                        //There was a significant enough gap betwwen points to use the exact distance
                        distance2 = distance;
                    }

                    var cuttingPlane = new Flat(distance2, direction);
                    List<List<Edge>> outputEdgeLoops;
                    var inputEdgeLoops = new List<List<Edge>>();
                    var current3DLoops = GetLoops(edgeListDictionary, cuttingPlane, out outputEdgeLoops, inputEdgeLoops);

                    //Get a list of 2D paths from the 3D loops
                    //Get 2D projections does not reorder list if the cutting plane direction is negative
                    //So we need to do this ourselves. 
                    double[,] backTransform;
                    crossSection.AddRange(current3DLoops.Select(loop => MiscFunctions.Get2DProjectionPointsReorderingIfNecessary(loop, direction, out backTransform, ts.SameTolerance)));

                    return crossSection;
                }
                foreach (var edge in vertex.Edges)
                {
                    //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                    //and the second removes it from the list.
                    if (edgeListDictionary.ContainsKey(edge.IndexInList))
                    {
                        edgeListDictionary.Remove(edge.IndexInList);
                    }
                    else
                    {
                        edgeListDictionary.Add(edge.IndexInList, edge);
                    }
                }
                //Update the previous distance of the vertex checked
                previousVertexDistance = currentVertexDistance;
            }
            return null; //The function should return from the if statement inside
        }
        #endregion

        #region Local Classes
        /// <summary>
        /// The Decomposition Data Class used to store information from A Directional Decomposition.
        /// 
        /// </summary>
        public class DecompositionData
        {
            /// <summary>
            /// A list of the paths that make up the slice of the solid at this distance along this direction
            /// </summary>
            public List<List<Point>> Paths;

            /// <summary>
            /// The distance along this direction
            /// </summary>
            public double DistanceAlongDirection;

            /// <summary>
            /// The Decomposition Data Class used to store information from A Directional Decomposition
            /// </summary>
            /// <param name="paths"></param>
            /// <param name="distanceAlongDirection"></param>
            public DecompositionData(IEnumerable<List<Point>> paths, double distanceAlongDirection)
            {
                Paths = new List<List<Point>>(paths);
                DistanceAlongDirection = distanceAlongDirection;
            }
        }

        /// <summary>
        /// A data group for linking the 2D path, 3D path, and edge loop of cross section polygons.
        /// </summary>
        public class PolygonDataGroup
        {
            /// <summary>
            /// The 2D list of points that define this polygon in the cross section
            /// </summary>
            public List<Point> Path2D;

            /// <summary>
            /// Gets the length of the path
            /// </summary>
            public double Length => PolygonOperations.Length(Path2D);

            /// <summary>
            /// The 3D list of intersection vertices that define this polygon in the cross section
            /// </summary>
            public List<Vertex> Path3D;

            /// <summary>
            /// The edge loop used to define the 3D path
            /// </summary>
            public List<Edge> EdgeLoop;

            /// <summary>
            /// The Index of the path in its cross section.
            /// </summary>
            public int IndexInCrossSection;

            /// <summary>
            /// The Index of the step along the search direction
            /// </summary>
            public int StepIndex;

            /// <summary>
            /// The area of this loop
            /// </summary>
            public double Area;

            /// <summary>
            /// The area of this loop
            /// </summary>
            public bool IsPositive;

            private int _segmentIndex = -1;//Default is negative 1.

            /// <summary>
            /// Gets or sets the index of the segment that this loop and path belong to.
            /// </summary>
            public int SegmentIndex
            {
                get { return _segmentIndex; }
                set
                {
                    _segmentIndex = value;
                    //when you set the segment index, set all the edge references as well.
                    //Don't set the vertex references of the eges, since some are not yet visited
                    //and need to retain their unset (-1) reference value;
                    foreach (var edge in EdgeLoop)
                    {
                        edge.ArbitraryReferenceIndex = value;
                    }
                }
            }


            /// <summary>
            /// A list of the connected segments. 
            /// </summary>
            public List<int> PotentialSegmentIndices;

            /// <summary>
            /// Get/set the distance along the search direction (Should match stepIndex)
            /// </summary>
            public double DistanceAlongSearchDirection;

            /// <summary>
            /// A data group for linking the 2D path and edge loop of cross section polygons.
            /// </summary>
            /// <param name="intersectionVertices"></param>
            /// <param name="edgeLoop"></param>
            /// <param name="area"></param>
            /// <param name="indexInCrossSection"></param>
            /// <param name="stepIndex"></param>
            /// <param name="path2D"></param>
            /// <param name="distanceAlongSearchDirection"></param>
            public PolygonDataGroup(List<Point> path2D, List<Vertex> intersectionVertices, 
                List<Edge> edgeLoop, double area, int indexInCrossSection, int stepIndex, double distanceAlongSearchDirection)
            {
                Path2D = path2D;
                Path3D = intersectionVertices;
                EdgeLoop = edgeLoop;
                Area = area;
                IndexInCrossSection = indexInCrossSection;
                PotentialSegmentIndices = new List<int>();
                StepIndex = stepIndex;
                DistanceAlongSearchDirection = distanceAlongSearchDirection;
                IsPositive = (Area > 0);
            }
        }

        /// <summary>
        /// The SegmentationData Class used to store information from A Directional Segmentation Decomposition.
        /// It is the same as DecompositionData, except that it stores the 3D information as well.
        /// </summary>
        public class SegmentationData
        {
            /// <summary>
            /// A list of polygon data groups that makes up the cross section at this distance
            /// </summary>
            public List<PolygonDataGroup> CrossSectionData;

            /// <summary>
            /// The distance along this direction
            /// </summary>
            public double DistanceAlongDirection;

            /// <summary>
            /// The Index of the step along the search direction
            /// </summary>
            public int StepIndex;

            /// <summary>
            /// The Segmentation Data Class used to store information from A Directional Segmented Decomposition
            /// </summary>
            /// <param name="areas"></param>
            /// <param name="distanceAlongDirection"></param>
            /// <param name="paths3D"></param>
            /// <param name="edgeLoops"></param>
            /// <param name="stepIndex"></param>
            /// <param name="paths2D"></param>
            public SegmentationData(List<List<Point>> paths2D, List<List<Vertex>> paths3D,
                List<List<Edge>> edgeLoops, List<double> areas, double distanceAlongDirection, int stepIndex)
            {
                CrossSectionData = new List<PolygonDataGroup>();
                for (var i = 0; i < paths2D.Count(); i++)
                {
                    CrossSectionData.Add(new PolygonDataGroup(paths2D[i], paths3D[i], edgeLoops[i], areas[i], i, stepIndex, distanceAlongDirection));
                }
                DistanceAlongDirection = distanceAlongDirection;
            }
        }
        #endregion

        #region Uniform Directional Segmentation

        /// <summary>
        /// Returns the Directional Segments found from decomposing a solid along a given direction. This data is used in other methods.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <param name="stepDistances"></param>
        /// <param name="sortedVertexDistanceLookup"></param>
        /// <returns></returns>
        public static List<DirectionalSegment> UniformDirectionalSegmentation(TessellatedSolid ts, double[] direction,
            double stepSize, out Dictionary<int, double> stepDistances, out Dictionary<int, double> sortedVertexDistanceLookup)
        {
            //Reset all the arbitrary edge references and vertex references to -1, since they may have been set in another method
            foreach (var vertex in ts.Vertices)
            {
                vertex.ReferenceIndex = -1;
            }
            foreach (var edge in ts.Edges)
            {
                edge.ArbitraryReferenceIndex = -1;
            }
            var outputData = new List<SegmentationData>();

            List<Vertex> bottomVertices, topVertices;
            var length = MinimumEnclosure.GetLengthAndExtremeVertices(direction, ts.Vertices,
                out bottomVertices, out topVertices);

            //Adjust the step size to be an even increment over the entire length of the solid
            stepSize = length / Math.Round(length / stepSize + 1);

            //make the minimum step size 1/10 of the length.
            if (length < 10 * stepSize)
            {
                stepSize = length / 10;
            }

            //This is a list of all the step indices matched with its distance along the axis.
            //This may be different that just multiplying the step index by the step size, because
            //minor adjustments occur to avoid cutting through vertices.
            stepDistances = new Dictionary<int, double>();

            //Choose whichever min offset is smaller
            var minOffset = Math.Min(Math.Sqrt(ts.SameTolerance), stepSize / 1000);

            //First, sort the vertices along the given axis. Duplicate distances are not important because they
            //will all be handled at the same step/distance.
            List<Tuple<Vertex, double>> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] { direction }, ts.Vertices, out sortedVertices, out duplicateRanges);
            //Create a distance lookup dictionary based on the vertex indices
            sortedVertexDistanceLookup = sortedVertices.ToDictionary(element => element.Item1.IndexInList, element => element.Item2);
            //A dictionary used to find the step index for each vertex. The key is the vertex index in list. The value is the step index.
            var referenceVerticesByStepIndex = new Dictionary<int, int>();

            //This is a list of all the current edges, those edges which are cut at the current distance along the axis. 
            //Each edge has an IndexInList, which is used as the dictionary Key.
            var edgeListDictionary = new Dictionary<int, Edge>();

            //This is a list of edges that is set in the GetLoops function. This list of edges is used in conjunction with 
            //outputEdgeLoops to limit the calls to the main GetLoops function, since the loops have not changed if the edgeListDictionary
            //has not changed.
            var inputEdgeLoops = new List<List<Edge>>();

            //A list of all the directional segments.
            var allDirectionalSegments = new Dictionary<int, DirectionalSegment>();

            var firstDistance = sortedVertices.First().Item2;
            var furthestDistance = sortedVertices.Last().Item2;
            var distanceAlongAxis = firstDistance;
            var currentVertexIndex = 0;
            var debugCounter = 0;
            //Start the step index at -1, so that the increment can be at the start of the while loop, 
            //making the final stepIndex correct for use in a later function.
            var stepIndex = -1;

            while (distanceAlongAxis < furthestDistance - stepSize)
            {
                stepIndex++;

                //This is the current distance along the axis. It will move forward by the step size during each iteration.
                distanceAlongAxis += stepSize;

                //inPlaneEdges is a list of edges that are added to the edge list and removed in the same step.
                //This means that they are basically in the current plane. This list will be reset every time we take another step.
                //This list works in conjunction with the newlyAddedEdges list.
                var inPlaneEdges = new List<Edge>();

                //inStepVertices is a hashset of all the vertices that were considered in the current step.
                var inStepVertices = new HashSet<Vertex>();

                //inStepEdges is a hashset of all the edges that started in the current step. It is not the same as the 
                //edgeListDictionary because it ignores edges from prior steps.
                var inStepEdges = new HashSet<Edge>();

                //Update vertex/edge list up until distanceAlongAxis
                for (var i = currentVertexIndex; i < sortedVertices.Count; i++)
                {
                    //Update the current vertex index so that this vertex is not visited again
                    //unless it causes the break ( > distanceAlongAxis), then it will start the 
                    //the next iteration.
                    currentVertexIndex = i;
                    var element = sortedVertices[i];
                    var vertex = element.Item1;
                    var vertexDistanceAlong = element.Item2;
                    //If a vertex is too close to the current distance, move it forward by the min offset.
                    //Update the edge list with this vertex.
                    if (vertexDistanceAlong.IsPracticallySame(distanceAlongAxis, minOffset))
                    {
                        //Move the distance enough so that this vertex is now less than 
                        distanceAlongAxis = vertexDistanceAlong + minOffset * 1.1;
                    }
                    //Else, Break after we get to a vertex that is further than the distance along axis
                    if (vertexDistanceAlong > distanceAlongAxis)
                    {
                        //consider this vertex again next iteration
                        break;
                    }
                    //Else, it is less than the distance along. Add the vertex to the inStepVertices and update the edge list.
                    inStepVertices.Add(vertex);

                    //Update the edge dictionary that is used to determine the 3D loops.
                    foreach (var edge in vertex.Edges)
                    {
                        //Reset the input edge loops since we have added an edge
                        inputEdgeLoops = new List<List<Edge>>();

                        //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                        //and the second removes it from the list.
                        if (edgeListDictionary.ContainsKey(edge.IndexInList))
                        {
                            edgeListDictionary.Remove(edge.IndexInList);

                            //If the edge being removed had also been added during this same step, add it to the inPlaneEdges.
                            if (inStepEdges.Contains(edge))
                            {
                                inPlaneEdges.Add(edge);
                            }
                        }
                        else
                        {
                            edgeListDictionary.Add(edge.IndexInList, edge);
                            inStepEdges.Add(edge);
                        }
                    }
                }

                //Get the decomposition data (cross sections) for this distance.
                //Before doing this, check to make sure that the minor shifts in the distance in the for loop above 
                //Did not move the distance beyond the furthest distance
                if (distanceAlongAxis <= furthestDistance && edgeListDictionary.Any())
                {
                    //The inputEdgeLoops is a reference, since it will be updated in the the method.
                    var segmentationData = GetSegmentationData(distanceAlongAxis, direction, edgeListDictionary,
                        ref inputEdgeLoops, minOffset, stepIndex);
                    if (segmentationData != null) outputData.Add(segmentationData);

                    UpdateSegments(segmentationData, inStepVertices, sortedVertexDistanceLookup, direction,
                        ref allDirectionalSegments, debugCounter);
                    debugCounter++;

                    stepDistances.Add(stepIndex, distanceAlongAxis);

                    foreach(var vertex in inStepVertices)
                    {
                        referenceVerticesByStepIndex.Add(vertex.IndexInList, stepIndex);
                    }
                }
            }

            //Add the remaining vertices to a final step index
            for (var i = currentVertexIndex; i < sortedVertices.Count; i++)
            {
                referenceVerticesByStepIndex.Add(sortedVertices[i].Item1.IndexInList, stepIndex);
            }

            //We have reached the end of the part. Close all the open segments.
            foreach (var segment in allDirectionalSegments.Values)
            {
                if (segment.IsFinished)
                {
                    segment.SetReferenceVerticesByStepIndex(referenceVerticesByStepIndex);
                    continue;
                }

                /*  We need to finish wrapping around all the open segments. There are two types
                of open segments; those that ended before the final step and those that ended
                at the final step. In both cases, there should be no merging or branching if 
                the step size was the correct size.
                    
                For segments that ended before the final step, we cannot use NextVertices or 
                CurrentEdges, since they are removed during the UpdateSegment function calls, 
                so instead we use ReferenceVertices to determine which vertices belonging to the 
                segment are further along the direction than the segment's final step.
                
                For segments that end at the final step, we cannot use ReferenceVertices since it 
                has not been updated with the NextVertices, but we can use NextVertices.
                The GetFinalVertices function takes care of these two different approaches.  */

                var endingVertices = segment.GetFinalVertices(sortedVertexDistanceLookup);
                var stack = new Stack<Vertex>(endingVertices);
                while (stack.Any())
                {
                    var vertex = stack.Pop();
                    vertex.ReferenceIndex = segment.Index;
                    segment.ReferenceVertices.Add(vertex);
                    foreach (var edge in vertex.Edges)
                    {
                        var otherVertex = edge.OtherVertex(vertex);
                        if (otherVertex.ReferenceIndex == -1)
                        {
                            //We have not visited this vertex yet, so push it onto the stack
                            //We will update its index when we pop it off.
                            stack.Push(otherVertex);
                            endingVertices.Add(otherVertex);
                        }
                        //All these edges will be reference edges, since they are complete.
                        //Since this is a hashset, we do not need to see if it contains the edges.
                        segment.ReferenceEdges.Add(edge);
                    }
                }

                //Now we need to set the step index for these vertices. We want it to be the last
                //index of the segment.
                var endIndex = segment.EndStepIndexAlongSearchDirection;
                foreach (var vertex in endingVertices)
                {
                    //var priorInt = referenceVerticesByStepIndex[vertex.IndexInList];
                    referenceVerticesByStepIndex[vertex.IndexInList] = endIndex;
                }
                //Set to IsFinished. This will clear out the current edges
                //(by now they should have all been added to reference edges)
                segment.IsFinished = true;
                segment.SetReferenceVerticesByStepIndex(referenceVerticesByStepIndex);
            }

            foreach (var segment in allDirectionalSegments)
            {
                if(segment.Value.CrossSectionPathDictionary.Count == 0) throw new Exception("A segment must have cross sections");
                //if(segment.Value.StartStepIndexAlongSearchDirection == segment.Value.EndStepIndexAlongSearchDirection) throw new Exception("This segment has zero thickness");
            }

            return allDirectionalSegments.Values.ToList();
        }

        /// <summary>
        /// This function updates the allDirectionalSegments dictionary with the new step's segmentation data.
        /// The segmentation data contains polygons and each of these polygons belongs to a segment. Segments
        /// are created, added to, branched, and merged within this function.
        /// </summary>
        /// <param name="segmentationData"></param>
        /// <param name="inStepVertices"></param>
        /// <param name="vertexDistanceLookup"></param>
        /// <param name="searchDirection"></param>
        /// <param name="allDirectionalSegments"></param>
        /// <param name="debugCounter"></param>
        private static void UpdateSegments(SegmentationData segmentationData, HashSet<Vertex> inStepVertices,
            Dictionary<int, double> vertexDistanceLookup, double[] searchDirection,
            ref Dictionary<int, DirectionalSegment> allDirectionalSegments, int debugCounter)
        {
            /* There are six possible segment cases that may occur. This section describes what they are and how they are handled.  
            
            Segment Case 1: [Continuation] There are no new vertices. Therefore every polygon data group should belong to a
                            current segment and there should be no merging or branching.

            Segment Case 2: [Continuation] The current segment is not connected to another segment. This occurs If the number of positive loops 
                            in a segment does not change (should always be = 1) and every loop it contains only belongs to this 
                            segment. Save the stashed vertices and edges, and keep this segment open.

            Segment Case 3: [Merging] If a polygon's edge loop belongs to multiple segments (as identified in Wrapping Step), end 
                            each connected segment and start a new one. 

            Segment Case 4: [Blind Hole/Pocket] If a negative polygon's edge loop does NOT belongs to any segments, then it is a blind
                            hole or pocket. To find it's segment, use the intersection polygon operation to determine overlap with existing
                            segments. Each blind hole or pocket can only belong to one segment.

            Segment Case 5: [New Segment] If a positive polygon's edge loop does NOT belongs to any segments, then it is the start of
                            a new segement. Simply start a new segment and look through any of the unassigned negative polygons to check
                            if they belong to this new polygon (Do this with the same wrapping technique used earlier).

            Segment Case 6: [Branching] If a positive loop is added to a existing segment (>1 +loops), close that segment and start  
                            new segments, one for each positive loop. It may be required to perform polygon operations to 
                            retain the correct negative loops. 

            In All Cases:   The in-step vertices and edges belong to the parent and child segments. This does repeat information, but there
                            are quite a few different cases and this is the easiest solution (other than not attaching them to any segment).
            
            Fast Transititions: A segment starts at the index after its parent end. If this segment merges with another segment in the next
                                iteration, then it will only be defined for one step (zero thickness). This may also happen if a brand new 
                                segment (A) immediately merges with another segment (B). In this case, segment A will only be defined for the 
                                first step index, since segment B takes over at the next step index. Otherwise, there would be multiple cross
                                sections for a step in an index. 
                                
            Implications:   A segment may only be defined for one step, but its volume should be thought of as extending a half-step forward
                            and backward from that step to form a volume.  
            */
            if(debugCounter == 145) Debug.WriteLine("Bug Stop Hit");
            var distanceAlongAxis = segmentationData.DistanceAlongDirection;

            //Get all the current segments.
            var currentSegmentsToConsider = new HashSet<DirectionalSegment>();
            foreach (var segment in allDirectionalSegments.Values.Where(s => !s.IsFinished))
            {
                currentSegmentsToConsider.Add(segment);
                segment.CurrentPolygonDataGroups = new HashSet<PolygonDataGroup>();
            }
            var allCurrentSegments = new HashSet<DirectionalSegment>(currentSegmentsToConsider);

            #region Segment Case 1: There are no new vertices.
            //Quickly update the polygon data groups (cross sections) if there are no new vertices (or edges)
            if (!inStepVertices.Any())
            {
                foreach (var polygonDataGroup in segmentationData.CrossSectionData)
                {
                    //It does not matter which edge we check, so just use the first one.
                    var parentFound = false;
                    var edge = polygonDataGroup.EdgeLoop.First();
                    foreach (var currentSegment in currentSegmentsToConsider)
                    {
                        if (currentSegment.CurrentEdges.Contains(edge))
                        {
                            //Add it to the current segment. Since no new vertices, it will only belong to one.
                            currentSegment.AddPolygonDataGroup(polygonDataGroup, false);
                            parentFound = true;
                            //We found the owner, so exit to the next loop
                            break;
                        }
                    }
                    if (!parentFound) throw new Exception("Segment with matching edges was not found");
                }
                return;
            }
            #endregion

            #region Wrapping Step 1 (Resolves Segment Cases 2 & 3): wrap edge/vertex pairs forward for each segment.
            //Wrap edge/vertex pairs forward for each segment until all the edges have a vertex 
            //that is further than the current distance (pointing forward). The vertex will get
            //a segment Index assigned to its ReferenceIndex. If a segment wants to use a vertex with 
            //an existing reference index, then stop pursueing that segment and note that the two are connected.
            while (currentSegmentsToConsider.Any())
            {
                var segment = currentSegmentsToConsider.First();
                currentSegmentsToConsider.Remove(segment);

                //The inStep refers to objects that are of interest in the current step.
                //This included edges that go intersect with the current step (think of it as a cutting plane),
                //and vertices that are within the current step. An edge that has two in-step vertices is 
                //an "inStepSegmentEdge".

                //Also, in the case of larger flat surfaces (where we add multiple in-plane edges), the
                //connected inStep vertices may not belong to the current segment.
                var allInStepSegmentVertices = new HashSet<Vertex>();
                var inStepSegmentVertexSet = new Stack<Vertex>();
                var inStepSegmentEdges = new HashSet<Edge>();
                var connectedSegmentsIndices = new HashSet<int>();

                //Get the in-step vertices for the current segment that are contained in inStepVertices.
                //This is only a part of the vertices that will be added to the list
                foreach (var vertex in inStepVertices)
                {
                    //If the vertex is contained in next vertices, add it to the "In-step" lists and 
                    //remove it from "NextVertices"
                    if (segment.NextVertices.Contains(vertex))
                    {
                        allInStepSegmentVertices.Add(vertex);
                        inStepSegmentVertexSet.Push(vertex);

                        //Remove the vertex from next vertices, since the next vertices are those that are on the 
                        //forward side of the plane. Don't add the vertex to references yet, since it may not
                        //belong to this segment, but a new segment that is a child of this segment. 
                        segment.NextVertices.Remove(vertex);

                        //This vertex belongs to the current segment, so update the reference index.
                        if (vertex.ReferenceIndex == -1)
                        {
                            vertex.ReferenceIndex = segment.Index;
                        }
                        else
                        {
                            //The vertex belongs to multiple segments.
                            Debug.WriteLine("The vertex belongs to multiple segments");
                        }
                    }
                }

                //A list of all the edges that were finished during this step, not including the inStepSegmentEdges.
                var finishedEdges = new HashSet<Edge>();
                var newCurrentEdges = new HashSet<Edge>();
                var newNextVertices = new HashSet<Vertex>();

                while (inStepSegmentVertexSet.Any())
                {
                    var vertex = inStepSegmentVertexSet.Pop();

                    foreach (var edge in vertex.Edges)
                    {
                        var otherVertex = edge.OtherVertex(vertex);
                        var otherVertexDistance = vertexDistanceLookup[otherVertex.IndexInList];

                        //Check to see where the otherVertex is located. There are 3 options:
                        //Edge Case 1: The other vertex is in the current step. This occurs when there is a flat surface.
                        //        To resolve this, we need to push the other vertex onto the vertexSet and note that
                        //        this vertex has been visited. This other vertex will lead us to Case 3 if there are
                        //        connected segments.
                        //Edge Case 2: The other vertex is further that the current distance. Update the segement's current edges
                        //        and next vertices so that we can use them later to identify edge loops.
                        //Edge Case 3: The other vertex is already past and belongs to a segment. Save the edge to be 
                        //        added to the segment's edge references. If it belongs to a segment other than the
                        //        the current segment, then those segments must be connected.

                        //Edge Case 1: It is in the current step and has not been visited yet (edge is in-step).
                        if (inStepVertices.Contains(otherVertex))
                        {
                            //Since this is a hashset, we don't need to check if it is contained before adding it.
                            inStepSegmentEdges.Add(edge);

                            //Check if it has already been visited
                            if (allInStepSegmentVertices.Contains(otherVertex))
                            {
                                if (otherVertex.ReferenceIndex == segment.Index) continue;

                                //Else, this vertex belongs to another segment                         
                                var otherSegment = allDirectionalSegments[otherVertex.ReferenceIndex];
                                AddConnectedSegment(otherSegment, ref connectedSegmentsIndices,
                                    ref inStepVertices, ref inStepSegmentVertexSet,
                                    ref allInStepSegmentVertices);
                                continue;
                            }
                            //Else if
                            if (otherVertex.ReferenceIndex != -1)
                            {
                                //This vertex belongs to another segment 
                                var otherSegment = allDirectionalSegments[otherVertex.ReferenceIndex];
                                AddConnectedSegment(otherSegment, ref connectedSegmentsIndices,
                                     ref inStepVertices, ref inStepSegmentVertexSet,
                                     ref allInStepSegmentVertices);
                                continue;
                            }

                            //This vertex belongs to the current segment, so add it to the vertex set and give it a reference index.
                            inStepSegmentVertexSet.Push(otherVertex);
                            allInStepSegmentVertices.Add(otherVertex);
                            otherVertex.ReferenceIndex = segment.Index;

                        }
                        //Edge Case 2: It is after the current step (edge is pointing forward).
                        //Add the edge and the otherVertex to the segment's "Next" lists if they do not already contain it.
                        else if (otherVertexDistance > distanceAlongAxis)
                        {
                            //Add the new current edge and the new next vertex to these temporary lists
                            //If this segment is not connected to another segment, then we will just add these
                            //the segment's lists. If there are multiple connected segments they will not
                            //be added in any of the current segment's lists, but in the new segment.
                            //However, the new segment (from a merger) does not need the next vertices, since
                            //it determines them from the current edges.
                            newCurrentEdges.Add(edge);
                            newNextVertices.Add(otherVertex);
                        }
                        //Edge Case 3: The otherVertex is prior to the current step (edge is pointing back).
                        else
                        {
                            //The edge must point back to a prior segment, so the edge is finished
                            finishedEdges.Add(edge);

                            //Check which segment it points back to using the otherVertex.ReferenceIndex.
                            //The reference index should be the index for a current segment.
                            if (otherVertex.ReferenceIndex == -1)
                            {
                                throw new Exception("Segment not found, when looking for edge ownership");
                            }

                            //Find which Segment contains the edge in its current edges. It should be only one.
                            var edgeWasFound = false;
                            foreach (var otherSegment in allCurrentSegments)
                            {
                                if (otherSegment.CurrentEdges.Contains(edge))
                                {
                                    if (edgeWasFound)
                                    {
                                        throw new Exception("Only one current segment should reference this edge.");
                                    }

                                    //it belongs to this segment. Set its reference index
                                    edge.ArbitraryReferenceIndex = otherSegment.Index;
                                    edgeWasFound = true;

                                    //These two segments are connected. 
                                    if (otherSegment.Index != segment.Index)
                                    {
                                        AddConnectedSegment(otherSegment, ref connectedSegmentsIndices,
                                            ref inStepVertices, ref inStepSegmentVertexSet,
                                            ref allInStepSegmentVertices);
                                    }
                                }
                            }
                        }
                    }
                }

                #region Segment Case 2: Continuation because of no connected segments.
                //If no connected segments, update the current segment
                if (!connectedSegmentsIndices.Any())
                {
                    //Next vertices and next edges have all the new stuff already added.
                    //The reference vertices and edges do need to be updated.
                    foreach (var edge in finishedEdges)
                    {
                        //Add the finished edges and update the current edges 
                        segment.ReferenceEdges.Add(edge);
                        segment.CurrentEdges.Remove(edge);
                    }
                    //All the inStepSegmentEdges can be added directly to references edges.
                    //None of them should be in current edges, since we reached them for the
                    //first time during this step.
                    foreach (var edge in inStepSegmentEdges)
                    {
                        segment.ReferenceEdges.Add(edge);
                    }
                    foreach (var vertex in allInStepSegmentVertices)
                    {
                        segment.ReferenceVertices.Add(vertex);
                        segment.NextVertices.Remove(vertex);
                    }
                    //Add the new edges and vertices that were found
                    foreach (var edge in newCurrentEdges)
                    {
                        segment.CurrentEdges.Add(edge);
                    }
                    foreach (var vertex in newNextVertices)
                    {
                        segment.NextVertices.Add(vertex);
                    }
                }
                #endregion

                #region Segment Case 3: [Merging] one or more segments that connect with the current segment 
                else
                {
                    connectedSegmentsIndices.Add(segment.Index);

                    //Add the finished edges to the reference edges of whichever segment they belong to.
                    //The finished edgs are not truly complete in these segments, since the in-step
                    //vertices will actually belong to the new segment, but this will allow for the segment's
                    //cross sections to be defined with the set of edges for its entire length.
                    foreach (var edge in finishedEdges)
                    {
                        var otherSegment = allDirectionalSegments[edge.ArbitraryReferenceIndex];
                        otherSegment.ReferenceEdges.Add(edge);
                        otherSegment.CurrentEdges.Remove(edge);
                    }

                    //We need to update each segment's edge and vertex lists, finish the segments, and 
                    //compile a full list of edges for the new segment's current edges.
                    var connectedSegments = new HashSet<DirectionalSegment>();
                    var newSegmentCurrentEdges = new HashSet<Edge>(newCurrentEdges);
                    foreach (var connectedSegmentsIndex in connectedSegmentsIndices)
                    {
                        var otherSegment = allDirectionalSegments[connectedSegmentsIndex];
                        connectedSegments.Add(otherSegment);
                        otherSegment.UpdateCurrentEdges();

                        foreach (var edge in otherSegment.CurrentEdges)
                        {
                            if (edge.To.ReferenceIndex == -1 || edge.From.ReferenceIndex == -1)
                            {
                                newSegmentCurrentEdges.Add(edge);
                            }
                            else
                            {
                                otherSegment.ReferenceEdges.Add(edge);
                            }
                        }

                        currentSegmentsToConsider.Remove(otherSegment);
                    }

                    //Create a new segment that starts from these completed segments. 
                    var segmentIndex = allDirectionalSegments.Count();
                    var newSegment = new DirectionalSegment(segmentIndex, inStepSegmentEdges,
                        allInStepSegmentVertices, newSegmentCurrentEdges, connectedSegments.ToList());

                    allDirectionalSegments.Add(segmentIndex, newSegment);
                }
                #endregion
            }
            #endregion

            #region Pairing Step: pair each edge loop from the SegmentationData with its parent segment(s).
            //Pair each edge loop from the SegmentationData with its parent segment(s).
            //If an edge loop does not belong to any segment, check whether it is positive or negative area(solid vs.hole respectively).

            //Get an updated list of the segments.
            var currentSegments = new HashSet<DirectionalSegment>();
            foreach (var segment in allDirectionalSegments.Values.Where(s => !s.IsFinished))
            {
                currentSegments.Add(segment);
                segment.CurrentPolygonDataGroups = new HashSet<PolygonDataGroup>();
            }

            //Now that the segments have been updated, we have additional cases to check using the 2D paths
            //example: Blind holes and a branching segment have not been captured up to this point.
            //First, we need to connect each path (polygonDataSet stores the path and the edge loop) to its segment
            var unassignedPositivePolygonDataGroups = new List<PolygonDataGroup>();
            var unassignedNegativePolygonDataGroups = new HashSet<PolygonDataGroup>();
            foreach (var polygonDataGroup in segmentationData.CrossSectionData)
            {
                //It does not matter which edge we check, so just use the first one.
                var edge = polygonDataGroup.EdgeLoop.First();
                foreach (var currentSegment in currentSegments)
                {
                    if (currentSegment.CurrentEdges.Contains(edge))
                    {
                        polygonDataGroup.PotentialSegmentIndices.Add(currentSegment.Index);
                        currentSegment.CurrentPolygonDataGroups.Add(polygonDataGroup);
                    }
                }

                //If not potential segments, then it is unassigned
                if (!polygonDataGroup.PotentialSegmentIndices.Any())
                {
                    if (polygonDataGroup.Area > 0)
                    {
                        //This is a new segment
                        unassignedPositivePolygonDataGroups.Add(polygonDataGroup);
                    }
                    else
                    {
                        //This is a hole in a new segment or a blind hole in an existing segment. 
                        //Before we can determine this, we need to create the new segments.
                        //For now, just add it to this list.
                        unassignedNegativePolygonDataGroups.Add(polygonDataGroup);
                    }
                }
            }
            #endregion

            #region Wrapping Step 2 (Resolves Segment Cases 4 & 5): wrap unused vertices to find connected vertices, edges, and polygons 
            //If it is a positive, create a new segment.
            //If it is is a negative, use the "Intersection" polygon operation to determine which segment it belongs to.

            //Get all the unassigned in-step vertices.
            var unusedInStepVertices = new HashSet<Vertex>();
            foreach (var inStepVertex in inStepVertices)
            {
                if (inStepVertex.ReferenceIndex == -1) unusedInStepVertices.Add(inStepVertex);
            }
            //Create new segements from any unused vertices
            var usedInStepVertices = new HashSet<Vertex>();
            while (unusedInStepVertices.Any())
            {
                var startVertex = unusedInStepVertices.First();
                //Don't remove from the unusedInStepVertices list until we are done collecting
                //All the edges that belong to this segment. Otherwise, we will be missing some
                //of the edges between in step vertices.
                var newSegmentIndex = allDirectionalSegments.Count;
                usedInStepVertices.Add(startVertex);
                var verticesToConsider = new Stack<Vertex>();
                verticesToConsider.Push(startVertex);
                startVertex.ReferenceIndex = newSegmentIndex;

                //Collect all the vertices that belong to this new segment
                //In addition, get the finished and current edges
                var newSegmentReferenceVertices = new HashSet<Vertex>() { startVertex };
                var finishedEdges = new HashSet<Edge>();
                var currentEdges = new HashSet<Edge>();
                while (verticesToConsider.Any())
                {
                    //Gather all the vertices that belong to this segment that are in the In-Step vertices
                    var vertex = verticesToConsider.Pop();
                    foreach (var edge in vertex.Edges)
                    {
                        currentEdges.Add(edge);
                        var otherVertex = edge.OtherVertex(vertex);
                        if (unusedInStepVertices.Contains(otherVertex))
                        {
                            usedInStepVertices.Add(otherVertex);

                            //This vertex now belongs to the new segment
                            //Since this is a hashset, it will prevent the object from being added multiple times.
                            newSegmentReferenceVertices.Add(otherVertex);

                            //Only add this vertex if it has not been visited before
                            //Then set its segment vertex so it is not visited again
                            if (otherVertex.ReferenceIndex == -1)
                            {
                                verticesToConsider.Push(otherVertex);
                                otherVertex.ReferenceIndex = newSegmentIndex;
                            }

                            //Both vertices for this edge are in the current step, so it is a finished edge
                            finishedEdges.Add(edge);
                            currentEdges.Remove(edge);
                        }
                    }
                }

                //Now we can remove all the used vertices
                foreach (var vertex in usedInStepVertices)
                {
                    unusedInStepVertices.Remove(vertex);
                }

                //Connect the positive and negative polygon data groups for this new segment.
                //Note: that a new segment cannot have a blind hole. (The step size must be smaller than the 
                //smallest wall in the part to insure this). Likewise, it can only have one positive polygon.
                PolygonDataGroup positivePolygonDataGroup = null;
                for (var i = 0; i < unassignedPositivePolygonDataGroups.Count; i++)
                {
                    var polygonDataGroup = unassignedPositivePolygonDataGroups[i];

                    //It does not matter which edge we check, so just use the first one.
                    var edge = polygonDataGroup.EdgeLoop.First();

                    //If it does not include the edge, check the next data group.
                    if (!currentEdges.Contains(edge)) continue;

                    //Else,  Great. This is the polygon we were looking for.
                    //Go ahead and assign it the segment index.
                    polygonDataGroup.SegmentIndex = newSegmentIndex;
                    positivePolygonDataGroup = polygonDataGroup;

                    //Remove it from the list. This would normally cause an error in for loop because
                    //it is modifying the enumerator, but it does not matter because we are breaking 
                    //out of the for loop.
                    unassignedPositivePolygonDataGroups.RemoveAt(i);
                    break;
                }

                //There does not have to be a negative polygon, but go ahead and check
                //There can only be one positive polygon data group, but there may be multiple negative ones.
                //For this reason, we need to check all of them and cannot break early.
                //We will then have a loop to remove the newly assigned data groups from the list of unnassigned ones.
                var negativePolygonDataGroups = new HashSet<PolygonDataGroup>();
                foreach (var polygonDataGroup in unassignedNegativePolygonDataGroups)
                {
                    //It does not matter which edge we check, so just use the first one.
                    var edge = polygonDataGroup.EdgeLoop.First();

                    //If it does not include the edge, check the next data group.
                    if (!currentEdges.Contains(edge)) continue;

                    //Else,  Great. This is the polygon we were looking for
                    polygonDataGroup.SegmentIndex = newSegmentIndex;
                    negativePolygonDataGroups.Add(polygonDataGroup);
                }
                foreach (var assignedNegativePolygonDataGroup in negativePolygonDataGroups)
                {
                    unassignedNegativePolygonDataGroups.Remove(assignedNegativePolygonDataGroup);
                }

                #region  Segment Case 4: [Blind Hole/Pocket]
                //Note that cannot have a blind hole or hidden pocket with a new segment, so it is okay to check where
                //this negative polygon belongs before we finish creating the new segments.
                //This is a blind hole or pocket, not visible from the search direction should exist for pre-existing segments
                //Example of pockets: Aerospace Beam with search direction through side. The pockets on the opposite side are not visible.     
                if (positivePolygonDataGroup == null)
                {
                    if (!negativePolygonDataGroups.Any())
                    {
                        throw new Exception("Either positive or negative polygon data group must be assigned");
                    }

                    foreach (var currentSegment in currentSegments)
                    {
                        var paths = currentSegment.CurrentPolygonDataGroups.Select(g => g.Path2D).ToList();

                        var usedDataGroups = new List<PolygonDataGroup>();
                        foreach (var negativePolygonDataGroup in negativePolygonDataGroups)
                        {
                            //Reverse the blind holes, so it is positive and then we can use intersection
                            var tempPolygons = new List<Point>(negativePolygonDataGroup.Path2D);
                            tempPolygons.Reverse();

                            //IF the intersection results in any overlap, then it belongs to this segment.
                            //As a hole, it cannot belong to multiple segments and cannot split or merge segments.
                            //Note: you cannot just check if a point from the dataSet is inside the positive paths, 
                            //since it the blind hole could be nested inside positive/negative pairings. (ex: a hollow rod 
                            //down the middle of a larger hollow tube. In this case, the hollow rod is a differnt segment).
                            var result = PolygonOperations.Intersection(paths, tempPolygons);
                            if (result != null && result.Any())
                            {
                                negativePolygonDataGroup.SegmentIndex = currentSegment.Index;
                                usedDataGroups.Add(negativePolygonDataGroup);

                                //Update the edge and vertex lists.
                                //NextVertices will be updated with the AddPolygonDataGroup function.
                                foreach (var edge in finishedEdges)
                                {
                                    edge.ArbitraryReferenceIndex = currentSegment.Index;
                                    currentSegment.ReferenceEdges.Add(edge);
                                }
                                foreach (var edge in currentEdges)
                                {
                                    //Don't set the current edge reference index, since it is not a completed edge
                                    currentSegment.CurrentEdges.Add(edge);
                                }
                                foreach (var vertex in newSegmentReferenceVertices)
                                {
                                    vertex.ReferenceIndex = currentSegment.Index;
                                    currentSegment.ReferenceVertices.Add(vertex);
                                }

                                currentSegment.AddPolygonDataGroup(negativePolygonDataGroup, true);
                            }
                        }

                        //Remove any used negative polygon data groups from the list
                        foreach (var negativePolygonDataGroup in usedDataGroups)
                        {
                            negativePolygonDataGroups.Remove(negativePolygonDataGroup);
                        }
                    }
                    if (negativePolygonDataGroups.Any(n => n.SegmentIndex == -1))
                    {
                        throw new Exception("Blind Hole was not assigned to any a pre-existing segment.");
                    }
                }
                #endregion

                #region Segment Case 5: [New Segment (with and without holes)]
                //Create the new segment from the unused vertices and connect the polygon data groups to the segments
                else
                {
                    var newSegment = new DirectionalSegment(newSegmentIndex,
                        finishedEdges, newSegmentReferenceVertices, currentEdges, searchDirection);
                    allDirectionalSegments.Add(newSegmentIndex, newSegment);
                    //Attach the polygon data groups
                    newSegment.AddPolygonDataGroup(positivePolygonDataGroup, false);
                    foreach (var negativePolygonDataGroup in negativePolygonDataGroups)
                    {
                        newSegment.AddPolygonDataGroup(negativePolygonDataGroup, false);
                    }
                }
                #endregion
            }
            #endregion

            if (unassignedPositivePolygonDataGroups.Any()) throw new Exception("At this point, only blind holes should be unassigned");

            #region Attach SegmentationData (Finishing Touch for Segment Case 2 and Resolves Segment Case 6)      
            foreach (var polygonDataGroup in segmentationData.CrossSectionData)
            {
                if (polygonDataGroup.SegmentIndex != -1 || polygonDataGroup.PotentialSegmentIndices.Count > 1)
                {
                    //The polygon data group has already been correctly assigned with a case 
                    //that was already fully handled.
                }
                else if (polygonDataGroup.PotentialSegmentIndices.Count == 1)
                {

                    var potentialSegment = allDirectionalSegments[polygonDataGroup.PotentialSegmentIndices.First()];
                    var counter = potentialSegment.CurrentPolygonDataGroups.Count(p => p.Area > 0.0);

                    if (counter == 1)
                    {
                        //Segment Case 2 Finishing Touch
                        potentialSegment.AddPolygonDataGroup(polygonDataGroup);
                    }
                    else if (counter > 1)
                    {
                        //Segment Case 6 [Branching]: End the potential segment and start new segments for each positive polygon.
                        //If the merger needs to be branched, this function will handle that too.
                        potentialSegment.BranchSegment(ref allDirectionalSegments);
                    }
                    else throw new Exception("One of the polygons must have been positive.");
                }
                else
                {
                    throw new Exception("All the polygon data groups must be assigned to a segment by this point");
                }
            }
            #endregion
        }

        private static void AddConnectedSegment(DirectionalSegment otherSegment,
            ref HashSet<int> connectedSegmentsIndices,
            ref HashSet<Vertex> inStepVertices,
            ref Stack<Vertex> inStepSegmentVertexSet,
            ref HashSet<Vertex> allInStepSegmentVertices)
        {
            //If this connected segment has not already been identified
            if (connectedSegmentsIndices.Contains(otherSegment.Index)) return;

            //Else, add it to the list of connected segments and update the vertex lists
            connectedSegmentsIndices.Add(otherSegment.Index);

            //If the finished edge belongs to another current segment, we need to push all the vertices
            //that are from the connected segment to our set.
            foreach (var vertex2 in inStepVertices)
            {
                //If the vertex is contained in next vertices, add it to the "In-step" lists and 
                //remove it from "NextVertices"
                if (otherSegment.NextVertices.Contains(vertex2))
                {
                    //only push if it has not already been in the vertex set, which can be seen
                    //in whether it is contained in the allInStepSegmentVertices
                    if (allInStepSegmentVertices.Contains(vertex2)) continue;

                    //Else
                    allInStepSegmentVertices.Add(vertex2);
                    inStepSegmentVertexSet.Push(vertex2);

                    //Remove the vertex from next vertices, since the next vertices are those that are on the 
                    //forward side of the plane. Don't add the vertex to references yet, since it may not
                    //belong to this segment, but a new segment that is a child of this segment. 
                    otherSegment.NextVertices.Remove(vertex2);

                    //This vertex belongs to the current segment, so update the reference index.
                    if (vertex2.ReferenceIndex == -1)
                    {
                        vertex2.ReferenceIndex = otherSegment.Index;
                    }
                    else
                    {
                        //The vertex belongs to multiple segments.
                        Debug.WriteLine("The vertex belongs to multiple segments");
                    }
                }
            }
        }

        private static SegmentationData GetSegmentationData(double distanceAlongAxis, double[] direction,
            Dictionary<int, Edge> edgeListDictionary, ref List<List<Edge>> inputEdgeLoops, double minOffset, int stepIndex)
        {
            //Make the slice
            var counter = 0;
            var successfull = false;
            var cuttingPlane = new Flat(distanceAlongAxis, direction);
            do
            {
                try
                {
                    List<List<Edge>> outputEdgeLoops;
                    var current3DLoops = GetLoops(edgeListDictionary, cuttingPlane, out outputEdgeLoops,
                        inputEdgeLoops);

                    //Use the same output edge loops for outer while loop, since the edge list does not change.
                    //If there is an error, it will occur before this.
                    inputEdgeLoops = outputEdgeLoops;

                    //Get the area of this layer
                    var areas = new List<double>();
                    var area = 0.0;
                    foreach (var loop in current3DLoops)
                    {
                        var pathArea = MiscFunctions.AreaOf3DPolygon(loop, direction);
                        areas.Add(pathArea);
                        area += pathArea;
                    }
                    if (area < 0)
                    {
                        Debug.WriteLine(
                            "Area for a cross section in UniformDirectionalDecomposition was negative. This means there was an issue with the polygon ordering");
                    }

                    double[,] backTransform;
                    //Get a list of 2D paths from the 3D loops
                    var currentPaths =
                        current3DLoops.Select(
                            cp =>
                                MiscFunctions.Get2DProjectionPointsReorderingIfNecessary(cp, direction,
                                    out backTransform));

                    successfull = true; //Irrelevant, since we are returning now.

                    //Add the data to the output
                    return new SegmentationData(currentPaths.ToList(), current3DLoops, outputEdgeLoops, areas, distanceAlongAxis, stepIndex);
                }
                catch
                {
                    counter++;
                    distanceAlongAxis += minOffset;
                }
            } while (!successfull && counter < 4);

            Debug.WriteLine("Slice at this distance was unsuccessful, even with multiple minimum offsets.");
            return null;
        }

        /// <summary>
        /// A directional segment is one of the pieces that a part is naturally divided into along a given direction.
        /// A segment never may include multiple positive polygons, but may contain multiple negative polygons (holes).
        /// It includes a collection of cross sections that are overlapping between steps, and it contains the vertices, 
        /// edges, and faces that are intersected by those cross sections.
        /// </summary>
        public class DirectionalSegment
        {
            #region Properties
            private bool _isFinished;

            /// <summary>
            /// Gets or sets whether the directional segment is finished collecting all its cross sections and face references.
            /// </summary>
            public bool IsFinished
            {
                get { return _isFinished; }
                set
                {
                    _isFinished = value;
                    if (_isFinished)
                    {
                        //NextVertices are irrelevant at this point.
                        NextVertices = null;
                        //Current Edges need to be added to the reference edges.
                        foreach (var edge in CurrentEdges)
                        {
                            ReferenceEdges.Add(edge);
                        }

                        //Faces are included if just one edge contains them. This is a 
                        //bit overkill, but was implemented because all the edges of the .STL were
                        //found in the segments during testing, but not all the faces or vertices.
                        var faceList1 = new HashSet<PolygonalFace>();
                        ReferenceFaces = new HashSet<PolygonalFace>();
                        foreach (var edge in ReferenceEdges)
                        {
                            if (faceList1.Contains(edge.OtherFace))
                            {
                                ReferenceFaces.Add(edge.OtherFace);
                                //ReferenceFaces.Add(edge.OwnedFace);
                            }
                            else
                            {
                                faceList1.Add(edge.OtherFace);
                            }
                            if (faceList1.Contains(edge.OwnedFace))
                            {
                                ReferenceFaces.Add(edge.OwnedFace);
                            }
                            else
                            {
                                faceList1.Add(edge.OwnedFace);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// A list of all the vertices that correspond to this segment. Some vertices may belong to multiple segments.
            /// </summary>
            public HashSet<Vertex> ReferenceVertices;

            /// <summary>
            /// Reference vertices by step index that they were added in (passed by the search plane)
            /// New segments not attached to prior segments will have their first set of vertices added
            /// at their first defined step (cross section). Segments that end with no children segments
            /// will have their last set of vertices added to their last defined step.
            /// </summary>
            public Dictionary<int, List<Vertex>> ReferenceVerticesByStepIndex { get; private set; }

            /// <summary>
            /// A list of all the edges that correspond to this segment. Some edges may belong to multiple segments.
            /// </summary>
            public HashSet<Edge> ReferenceEdges;

            /// <summary>
            /// A list of all the edges that are currently being used for the decomposition. The plane is currently cutting through them.
            /// This is a companion to NextVertices.
            /// </summary>
            public HashSet<Edge> CurrentEdges;

            /// <summary>
            /// A list of all the vertices that we are looking into, but that are not yet assured to be part of this segment.
            /// </summary>
            public HashSet<Vertex> NextVertices;

            /// <summary>
            /// A list of all the faces that correspond to this segment. Some faces may be partly in this segment and partly in another.
            /// </summary>
            public HashSet<PolygonalFace> ReferenceFaces;

            /// <summary>
            /// A dictionary that contains all the cross sections corresponding to this segment. The integer is the step number (distance) along
            /// the search direction.
            /// </summary>
            public Dictionary<int, List<PolygonDataGroup>> CrossSectionPathDictionary;

            /// <summary>
            /// Gets the start distance of this segment along the search direction
            /// </summary>
            public double StartDistanceAlongSearchDirection => 
                CrossSectionPathDictionary[StartStepIndexAlongSearchDirection].First().DistanceAlongSearchDirection;

            /// <summary>
            /// Gets the start index of this segment along the search direction
            /// </summary>
            //Note: calling First() on a dictionary is calling for trouble 
            //(https://stackoverflow.com/questions/13979966/get-first-element-from-a-dictionary)
            //Instead, use the minimum key (stepIndex)
            public int StartStepIndexAlongSearchDirection => CrossSectionPathDictionary.Keys.Min();

            /// <summary>
            /// Gets the end distance of this segment along the search direction
            /// </summary>
            public double EndDistanceAlongSearchDirection => 
                CrossSectionPathDictionary[EndStepIndexAlongSearchDirection].First().DistanceAlongSearchDirection;

            /// <summary>
            /// Gets the end index of this segment along the search direction
            /// </summary>
            ///  //Note: calling Last() on a dictionary is calling for trouble 
            //(https://stackoverflow.com/questions/13979966/get-first-element-from-a-dictionary)
            //Instead, use the maximum key (stepIndex)
            public int EndStepIndexAlongSearchDirection => CrossSectionPathDictionary.Keys.Max();

            /// <summary>
            /// The direction by which the directional segment was defined. The cross sections and face references will be ordered
            /// along this direction.
            /// </summary>
            public double[] ForwardDirection;

            /// <summary>
            /// A list of the current polygon data groups, which make up the current segment cross section
            /// </summary>
            public HashSet<PolygonDataGroup> CurrentPolygonDataGroups { get; set; }

            /// <summary>
            /// A list of all the directional segments that are adjoined to this segment along the forward direction.
            /// </summary>
            public List<DirectionalSegment> ForwardAdjoinedDirectionalSegments { get; set; }

            /// <summary>
            /// A list of all the directional segments that are adjoined to this segment along the rearward direction.
            /// </summary>
            public List<DirectionalSegment> RearwardAdjoinedDirectionalSegments { get; set; }

            /// <summary>
            /// A list of all the directional segments that are connected to this segment.
            /// </summary>
            public HashSet<int> ConnectedDirectionalSegments { get; set; }

            /// <summary>
            /// The index that is unique to this directional segment. The segments are ordered based on whichever
            /// is started first along the search direction.
            /// </summary>
            public int Index;

            //A segment temporarily created, that is to be deleted. (Ex. Branching a merged segment in the same step)
            public bool IsDummySegment;

            /// <summary>
            /// Gets the first cross section 
            /// </summary>
            /// <returns></returns>
            public List<PolygonDataGroup> StartCrossSection()
            {
                return CrossSectionPathDictionary[StartStepIndexAlongSearchDirection];
            }


            /// <summary>
            /// Gets the last cross section 
            /// </summary>
            /// <returns></returns>
            public List<PolygonDataGroup> EndCrossSection()
            {
                return CrossSectionPathDictionary[EndStepIndexAlongSearchDirection];
            }

            #endregion

            #region Constructors
            /// <summary>
            /// Starts a directional segment with no prior connected directional segments
            /// </summary>
            /// <param name="index"></param>
            /// <param name="referenceEdges"></param>
            /// <param name="referenceVertices"></param>
            /// <param name="currentEdges"></param>
            /// <param name="direction"></param>
            public DirectionalSegment(int index, IEnumerable<Edge> referenceEdges, IEnumerable<Vertex> referenceVertices,
                IEnumerable<Edge> currentEdges, double[] direction)
            {
                Index = index;
                IsFinished = false;
                CrossSectionPathDictionary = new Dictionary<int, List<PolygonDataGroup>>();
                ForwardAdjoinedDirectionalSegments = new List<DirectionalSegment>();
                RearwardAdjoinedDirectionalSegments = new List<DirectionalSegment>();
                ConnectedDirectionalSegments = new HashSet<int>();
                CurrentPolygonDataGroups = new HashSet<PolygonDataGroup>();
                ReferenceFaces = new HashSet<PolygonalFace>();

                ReferenceEdges = new HashSet<Edge>(referenceEdges);
                ReferenceVertices = new HashSet<Vertex>(referenceVertices);

                //This segment has the same forward direction as its parents
                ForwardDirection = direction;

                CurrentEdges = new HashSet<Edge>(currentEdges);
                //Build the next vertices with the current edges and vertex distance information
                UpdateNextVertices();
            }

            /// <summary>
            /// Starts a directional segment based on prior connected directional segments
            /// </summary>
            /// <param name="index"></param>
            /// <param name="referenceEdges"></param>
            /// <param name="referenceVertices"></param>
            /// <param name="currentEdges"></param>
            /// <param name="parentDirectionalSegments"></param>
            public DirectionalSegment(int index, HashSet<Edge> referenceEdges, HashSet<Vertex> referenceVertices,
                HashSet<Edge> currentEdges, List<DirectionalSegment> parentDirectionalSegments)
            {
                Index = index;
                IsFinished = false;
                CrossSectionPathDictionary = new Dictionary<int, List<PolygonDataGroup>>();
                ForwardAdjoinedDirectionalSegments = new List<DirectionalSegment>();
                ConnectedDirectionalSegments = new HashSet<int>();
                CurrentPolygonDataGroups = new HashSet<PolygonDataGroup>();
                ReferenceFaces = new HashSet<PolygonalFace>();

                //Close the parent directional segments 
                foreach (var parentDirectionalSegment in parentDirectionalSegments)
                {
                    parentDirectionalSegment.IsFinished = true;
                }
                RearwardAdjoinedDirectionalSegments = new List<DirectionalSegment>(parentDirectionalSegments);
                foreach (var parentDirectionalSegment in parentDirectionalSegments)
                {
                    parentDirectionalSegment.ForwardAdjoinedDirectionalSegments.Add(this);
                }

                //Update ownership of the reference edge and vertices to the current segment
                foreach (var edge in referenceEdges)
                {
                    edge.ArbitraryReferenceIndex = index;
                }
                foreach (var vertex in referenceVertices)
                {
                    vertex.ReferenceIndex = index;
                }
                ReferenceEdges = new HashSet<Edge>(referenceEdges);
                ReferenceVertices = new HashSet<Vertex>(referenceVertices);

                //This segment has the same forward direction as its parents
                ForwardDirection = parentDirectionalSegments.First().ForwardDirection;

                CurrentEdges = new HashSet<Edge>(currentEdges);

                //Build the next vertices with the current edges and vertex distance information
                UpdateNextVertices();
            }

            /// <summary>
            /// Starts a directional segment based on one prior connected directional segment
            /// </summary>
            /// <param name="index"></param>
            /// <param name="polygonDataGroup"></param>
            /// <param name="parentSegment"></param>
            public DirectionalSegment(int index, PolygonDataGroup polygonDataGroup, DirectionalSegment parentSegment)
            {
                Index = index;
                IsFinished = false;
                CrossSectionPathDictionary = new Dictionary<int, List<PolygonDataGroup>>();
                ForwardAdjoinedDirectionalSegments = new List<DirectionalSegment>();
                RearwardAdjoinedDirectionalSegments = new List<DirectionalSegment>() { parentSegment };
                parentSegment.ForwardAdjoinedDirectionalSegments.Add(this);

                ConnectedDirectionalSegments = new HashSet<int>();
                CurrentPolygonDataGroups = new HashSet<PolygonDataGroup>();
                ReferenceEdges = new HashSet<Edge>(); //this is empty. all prior edges belong to its parent.
                ReferenceFaces = new HashSet<PolygonalFace>();
                ReferenceVertices = new HashSet<Vertex>(); //this is empty. all prior vertices belong to its parent.

                //This segment has the same forward direction as its parents
                ForwardDirection = parentSegment.ForwardDirection;

                //The current edges are those in the polygon data group
                CurrentEdges = new HashSet<Edge>(polygonDataGroup.EdgeLoop);

                //Add the polygon data group
                AddPolygonDataGroup(polygonDataGroup, false);

                //Build the next vertices with the current edges and vertex reference indices
                UpdateNextVertices();
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Starts a directional segment for each polygon data group assigned to this parent directional segment
            /// </summary>
            /// <param name="allDirectionalSegments"></param>
            public void BranchSegment(ref Dictionary<int, DirectionalSegment> allDirectionalSegments)
            {
                //If a merger resulted in a new segment (this) that needs to be branched, it will have no cross sections set by this point.
                //Replace this segment with new segments. 
                if (!this.CrossSectionPathDictionary.Any())
                {
                    allDirectionalSegments.Remove(this.Index);
                    IsDummySegment = true;
                }

                IsFinished = true;
                var newSegments = new List<DirectionalSegment>();
                if (!IsDummySegment)
                {
                    foreach (var positivePolygonDataGroup in CurrentPolygonDataGroups.Where(p => p.Area > 0.0))
                    {
                        var newSegmentIndex = allDirectionalSegments.Count;
                        var newSegment = new DirectionalSegment(newSegmentIndex, positivePolygonDataGroup, this);
                        allDirectionalSegments.Add(newSegmentIndex, newSegment);
                        newSegments.Add(newSegment);
                    }
                }
                else
                {
                    //We need to match up the positive polygons with the parents of this segment.
                    //If the intersection results in any overlap, then it belongs to this segment.
                    //It can belong to multiple segments, or just one.
                    foreach (var positivePolygonDataGroup in CurrentPolygonDataGroups.Where(p => p.Area > 0.0))
                    {
                        var parentSegments = new List<DirectionalSegment>();
                        foreach (var parentSegment in RearwardAdjoinedDirectionalSegments)
                        {
                            //Get the last positive polygon data group from the parent segment.
                            //There can only be one.
                            var positiveParentPolygonDataGroup =
                                parentSegment.CrossSectionPathDictionary.Last().Value.FirstOrDefault(p => p.Area > 0.0);
                            if (positiveParentPolygonDataGroup == null)
                                throw new Exception(
                                    "No positive polygon found for parent. Check how the parent was created");

                            //Check if the parent's polygon overlaps with the new polygon
                            var result = PolygonOperations.Intersection(positiveParentPolygonDataGroup.Path2D, positivePolygonDataGroup.Path2D);
                            if (result != null && result.Any())
                            {
                                parentSegments.Add(parentSegment);
                            }
                        }

                        //Now that we have all the parent segments fo the current positivePolygonDataGroup, 
                        //we can create the new segment.
                        var newSegmentIndex = allDirectionalSegments.Count;

                        //For the reference Edges and Vertices, all should belong to the parents.
                        //The current edges are those in the polygon data group
                        //Negative loop edges will be added later.
                        var currentEdges = new HashSet<Edge>(positivePolygonDataGroup.EdgeLoop);
                        var referenceVertices = new HashSet<Vertex>();
                        foreach (var edge in currentEdges)
                        {
                            if (edge.To.ReferenceIndex == -1)
                            {
                                referenceVertices.Add(edge.From);
                                ReferenceVertices.Remove(edge.From);
                            }
                            else
                            {
                                referenceVertices.Add(edge.To);
                                ReferenceVertices.Remove(edge.To);
                            }
                        }
                        var referenceEdges = new HashSet<Edge>();
                        var newSegment = new DirectionalSegment(newSegmentIndex, referenceEdges, referenceVertices, currentEdges, parentSegments);
                        newSegment.AddPolygonDataGroup(positivePolygonDataGroup);
                        allDirectionalSegments.Add(newSegmentIndex, newSegment);
                        newSegments.Add(newSegment);
                    }

                    //Attach the reference edges and remaining reference vertices to all the parent segments 
                    foreach (var parent in RearwardAdjoinedDirectionalSegments)
                    {
                        foreach (var vertex in ReferenceVertices)
                        {
                            parent.ReferenceVertices.Add(vertex);
                        }
                        foreach (var edge in ReferenceEdges)
                        {
                            parent.ReferenceEdges.Add(edge);
                        }
                    }
                }

                //Now we need to match up the negative polygon data groups
                foreach (var negativePolygonDataGroup in CurrentPolygonDataGroups.Where(p => p.Area < 0.0))
                {
                    foreach (var newSegment in newSegments)
                    {
                        var paths = newSegment.CurrentPolygonDataGroups.Select(otherPolygonDataGroup => otherPolygonDataGroup.Path2D).ToList();
                        //IF the intersection results in any overlap, then it belongs to this segment.
                        //As a hole, it cannot belong to multiple segments and cannot split or merge segments.
                        //Note: you cannot just check if a point from the dataSet is inside the positive paths, 
                        //since it the blind hole could be nested inside positive/negative pairings. (ex: a hollow rod 
                        //down the middle of a larger hollow tube. In this case, the hollow rod is a differnt segment).
                        var positiveVersionOfHole = new List<Point>(negativePolygonDataGroup.Path2D);
                        positiveVersionOfHole.Reverse();
                        var result = PolygonOperations.Intersection(paths, positiveVersionOfHole);
                        if (result != null && result.Any())
                        {
                            negativePolygonDataGroup.SegmentIndex = newSegment.Index;
                            newSegment.AddPolygonDataGroup(negativePolygonDataGroup);
                            break;
                        }
                    }
                    if (negativePolygonDataGroup.SegmentIndex == -1)
                    {
                        throw new Exception("Blind Hole was not assigned to any a pre-existing segment.");
                    }
                }
            }

            /// <summary>
            /// Adds a polygon data group and updates the segment accordingly
            /// </summary>
            /// <param name="dataGroup"></param>
            /// <param name="updateCurrentEdgesAndNextVertices"></param>
            public void AddPolygonDataGroup(PolygonDataGroup dataGroup, bool updateCurrentEdgesAndNextVertices = true)
            {
                //This data group now belongs to this segment
                dataGroup.SegmentIndex = Index;

                //Add it to the current polygon data groups 
                //Since this is a hashset, it will avoid adding again if it has already been added
                CurrentPolygonDataGroups.Add(dataGroup);

                //Update the cross section path dictionary
                if (CrossSectionPathDictionary.ContainsKey(dataGroup.StepIndex))
                {
                    //Add negative polygon data groups 
                    if (dataGroup.Area < 0.0)
                    {
                        CrossSectionPathDictionary[dataGroup.StepIndex].Add(dataGroup);
                    }
                    else
                    {
                        //This is a positive polygon
                        //Check to make sure that we are not addind a second positive polygon to the same step index 
                        //We can only have one per segment per step 

                        if (CrossSectionPathDictionary[dataGroup.StepIndex].Any(p => p.Area > 0.0))
                        {
                            //if there are any other positive polygon data groups, throw an error
                            throw new Exception("Multiple Positive Polygons in same step of segment. " +
                                                "We can only have one per segment per step.");
                        }
                        CrossSectionPathDictionary[dataGroup.StepIndex].Add(dataGroup);
                    }
                }
                else
                {
                    CrossSectionPathDictionary.Add(dataGroup.StepIndex, new List<PolygonDataGroup>() { dataGroup });
                }

                if (!updateCurrentEdgesAndNextVertices) return;

                //Else, Update current edges and next vertices 
                //Must do current edges first, since UpdateNextVertices references current edges
                foreach (var edge in dataGroup.EdgeLoop)
                {
                    CurrentEdges.Add(edge);
                }
                UpdateNextVertices();
            }

            /// <summary>
            /// Gets the list of the final vertices (those on the other side of the segment's final step index).
            /// </summary>
            public HashSet<Vertex> GetFinalVertices(Dictionary<int, double> sortedVertexDistanceLookup)
            {
                var finalVertices = new HashSet<Vertex>(NextVertices);
                foreach (var vertex in ReferenceVertices)
                {
                    var vertexDistance = sortedVertexDistanceLookup[vertex.IndexInList];
                    if (vertexDistance > EndDistanceAlongSearchDirection)
                    {
                        finalVertices.Add(vertex);
                    }
                }
                return finalVertices;
            }

            /// <summary>
            /// Updates the list of next vertices, using the vertex reference indices and current edges.
            /// </summary>
            public void UpdateNextVertices()
            {
                NextVertices = new HashSet<Vertex>();

                //Build the next vertices with the current edges and vertex distance information
                foreach (var currentEdge in CurrentEdges)
                {
                    if (currentEdge.To.ReferenceIndex != -1)
                    {
                        if (currentEdge.From.ReferenceIndex != -1)
                        {
                            throw new Exception("This edge contains two vertices that have reference indices. One must not have a ref. index");
                        }
                        //Add the other vertex to the next vertices
                        NextVertices.Add(currentEdge.From);
                    }
                    else if (currentEdge.From.ReferenceIndex != -1)
                    {
                        //Add the other vertex to the next vertices
                        NextVertices.Add(currentEdge.To);
                    }
                    else throw new Exception("This edge contains two vertices that do not have reference indices. One must have a ref. index");
                }
            }

            /// <summary>
            /// Updates the list of current edges, using the vertex reference indices.
            /// </summary>
            public void UpdateCurrentEdges()
            {
                var tempEdges = CurrentEdges.Where(edge => edge.To.ReferenceIndex == -1 || edge.From.ReferenceIndex == -1).ToList();
                CurrentEdges = new HashSet<Edge>(tempEdges);
            }  

            /// <summary>
            /// Sets the ReferenceVerticesByStepIndex. Do this when finished if the data is needed.
            /// </summary>
            public void SetReferenceVerticesByStepIndex(Dictionary<int, int> vertexStepIndexReference )
            {
                ReferenceVerticesByStepIndex = new Dictionary<int, List<Vertex>>();
                if (!IsFinished) throw new Exception("Segment must be finished first");
                foreach (var vertex in ReferenceVertices)
                {
                    var vertexStepIndex = vertexStepIndexReference[vertex.IndexInList];
                    if (ReferenceVerticesByStepIndex.ContainsKey(vertexStepIndex))
                    {
                        ReferenceVerticesByStepIndex[vertexStepIndex].Add(vertex);
                    }
                    else
                    {
                        ReferenceVerticesByStepIndex.Add(vertexStepIndex, new List<Vertex>() {vertex});
                    }
                }
            }

            /// <summary>
            /// Sets the face colors for the Tesselated Solid as well as returning a list of cross section vertices.
            /// To Display, use ShowVertexPathsWithSolid().
            /// </summary>
            /// <param name="ts"></param>
            /// <param name="badFaces"></param>
            /// <returns></returns>
            public List<List<List<Vertex>>> DisplaySetup(TessellatedSolid ts, HashSet<PolygonalFace> badFaces = null)
            {
                //reset all face colors 
                var defaultColor = new Color(KnownColors.LightGray);
                foreach (var face in ts.Faces)
                {
                    if (badFaces == null || !badFaces.Contains(face))
                    {
                        face.Color = defaultColor;
                    }
                }
                ts.HasUniformColor = false;

                //Make reference faces red. 
                //Faces are only considered if the edge lists contain two or more edges of that face
                var red = new Color(KnownColors.Red);
                if (IsFinished)
                {
                    foreach (var face in ReferenceFaces)
                    {
                        face.Color = red;
                    }
                }
                else
                {
                    //First, get all the edges.
                    var allEdges = new HashSet<Edge>(ReferenceEdges);
                    foreach (var edge in CurrentEdges)
                    {
                        allEdges.Add(edge);
                    }
                    var faceList1 = new HashSet<PolygonalFace>();
                    foreach (var edge in allEdges)
                    {
                        if (faceList1.Contains(edge.OtherFace))
                        {
                            edge.OtherFace.Color = red;
                        }
                        else
                        {
                            faceList1.Add(edge.OtherFace);
                        }
                        if (faceList1.Contains(edge.OwnedFace))
                        {
                            edge.OwnedFace.Color = red;
                        }
                        else
                        {
                            faceList1.Add(edge.OwnedFace);
                        }
                    }
                }

                var allVertexPaths = CrossSectionPathDictionary.Values.Select(c => c.Select(p => p.Path3D).ToList()).ToList();

                return allVertexPaths;
            }
            #endregion

            /// <summary>
            /// Reverses the direction and all associated dictionaries. It is assumed that the steps are to be 
            /// ordered along the reversed direction. The total number of steps along the direction must be given.
            /// </summary>
            public void Reverse(int maxStepIndex,  Dictionary<int, double> stepDistances)
            {
                ForwardDirection = ForwardDirection.multiply(-1);
                var tempSegmentsSet = ForwardAdjoinedDirectionalSegments;
                ForwardAdjoinedDirectionalSegments = RearwardAdjoinedDirectionalSegments;
                RearwardAdjoinedDirectionalSegments = tempSegmentsSet;

                var tempReferenceVertices = ReferenceVerticesByStepIndex.Reverse();
                var reversedReferenceVerticesByStepIndex = new Dictionary<int, List<Vertex>>();
                foreach (var referenceItem in tempReferenceVertices)
                {
                    var i = maxStepIndex - referenceItem.Key;
                    reversedReferenceVerticesByStepIndex.Add(i, referenceItem.Value);
                }
                ReferenceVerticesByStepIndex = reversedReferenceVerticesByStepIndex;

                var tempCrossSections = CrossSectionPathDictionary.Reverse();
                var reversedCrossSectionPathDictionary = new Dictionary<int, List<PolygonDataGroup>>();
                foreach (var crossSectionItem in tempCrossSections)
                {
                    var i = maxStepIndex - crossSectionItem.Key;
                    var distanceAlongSearchDirection = stepDistances[i];
                    foreach (var polygonDataGroup in crossSectionItem.Value)
                    {
                        polygonDataGroup.StepIndex = i;
                        polygonDataGroup.DistanceAlongSearchDirection = distanceAlongSearchDirection;
                    }
                    reversedCrossSectionPathDictionary.Add(i, crossSectionItem.Value);
                }
                CrossSectionPathDictionary = reversedCrossSectionPathDictionary;
            }
        }
        #endregion

        #region Private Supporting Methods
        /// <summary>
        ///     Crosses the sectional area.
        /// </summary>
        /// <param name="edgeListDictionary">The edge list dictionary.</param>
        /// <param name="cuttingPlane">The cutting plane.</param>
        /// <param name="outputEdgeLoops">The output edge loops.</param>
        /// <param name="intputEdgeLoops">The intput edge loops.</param>
        /// <param name="ignoreNegativeSpace">if set to <c>true</c> [ignore negative space].</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Loop did not complete</exception>
        private static double CrossSectionalArea(Dictionary<int, Edge> edgeListDictionary, Flat cuttingPlane,
            out List<List<Edge>> outputEdgeLoops, List<List<Edge>> intputEdgeLoops, bool ignoreNegativeSpace = false)
        {
            var loops = GetLoops(edgeListDictionary, cuttingPlane, out outputEdgeLoops, intputEdgeLoops);
            var totalArea = 0.0;
            foreach (var loop in loops)
            {
                //The area function returns negative values for negative loops and positive values for positive loops
                var area = MiscFunctions.AreaOf3DPolygon(loop, cuttingPlane.Normal);
                if (ignoreNegativeSpace && Math.Sign(area) < 0) continue;
                totalArea += area;
            }
            return totalArea;
        }

        private static List<List<Vertex>> GetLoops(Dictionary<int, Edge> edgeListDictionary, Flat cuttingPlane,
            out List<List<Edge>> outputEdgeLoops, List<List<Edge>> intputEdgeLoops)
        {
            var edgeLoops = new List<List<Edge>>();
            var loops = new List<List<Vertex>>();
            if (intputEdgeLoops.Any())
            {
                edgeLoops = intputEdgeLoops; //Note that edge loops should all be ordered correctly
                foreach (var edgeLoop in edgeLoops)
                {
                    var loop = new List<Vertex>();
                    foreach (var edge in edgeLoop)
                    {
                        var vertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin,
                        edge.To, edge.From);
                        vertex.Edges.Add(edge);
                        loop.Add(vertex);
                    }
                    loops.Add(loop);
                }
            }
            else
            {
                //Build an edge list that we can modify, without ruining the original
                //After comparing hashset versus dictionary (with known keys)
                //Hashset was slighlty faster during creation and enumeration, 
                //but even more slighlty slower at removing. Overall, Hashset 
                //was about 17% faster than a dictionary.
                var edges = new List<Edge>(edgeListDictionary.Values);
                var unusedEdges = new HashSet<Edge>(edges);
                foreach (var startEdge in edges)
                {
                    if (!unusedEdges.Contains(startEdge)) continue;
                    unusedEdges.Remove(startEdge); ;
                    var loop = new List<Vertex>();
                    var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal,
                        cuttingPlane.DistanceToOrigin, startEdge.To, startEdge.From);
                    loop.Add(intersectVertex);
                    var edgeLoop = new List<Edge> { startEdge };
                    var startFace = startEdge.OwnedFace;
                    var currentFace = startFace;
                    var previousFace = startFace; //This will be set again before its used.
                    var endFace = startEdge.OtherFace;
                    var nextEdgeFound = false;
                    Edge nextEdge = null;
                    var correctDirection = 0.0;
                    var reverseDirection = 0.0;
                    do
                    {
                        //Get the next edge
                        foreach (var edge in currentFace.Edges)
                        {
                            if (!unusedEdges.Contains(edge)) continue;
                            if (edge.OtherFace == currentFace)
                            {
                                previousFace = edge.OtherFace;
                                currentFace = edge.OwnedFace;
                                nextEdgeFound = true;
                                nextEdge = edge;
                                break;
                            }
                            if (edge.OwnedFace == currentFace)
                            {
                                previousFace = edge.OwnedFace;
                                currentFace = edge.OtherFace;
                                nextEdgeFound = true;
                                nextEdge = edge;
                                break;
                            }
                        }
                        if (nextEdgeFound)
                        {
                            //For the first set of edges, check to make sure this list is going in the proper direction
                            intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal,
                                cuttingPlane.DistanceToOrigin, nextEdge.To, nextEdge.From);
                            //Add the edge as a reference for the vertex, so we can get the faces later
                            intersectVertex.Edges.Add(nextEdge);
                            var vector = intersectVertex.Position.subtract(loop.Last().Position);
                            //Use the previous face, since that is the one that contains both of the edges that are in use.
                            var dot = cuttingPlane.Normal.crossProduct(previousFace.Normal).dotProduct(vector);
                            loop.Add(intersectVertex);
                            edgeLoop.Add(nextEdge);
                            unusedEdges.Remove(nextEdge);
                            //Note that removing at an index is FASTER than removing a object.
                            if (Math.Sign(dot) >= 0) correctDirection += dot;
                            else reverseDirection += (-dot);
                        }
                        else throw new Exception("Loop did not complete");
                    } while (currentFace != endFace);

                    //if (reverseDirection > 2 && correctDirection > 2) throw new Exception("Area Decomp Loop Finding needs additional work.");
                    if (reverseDirection > correctDirection)
                    {
                        loop.Reverse();
                        edgeLoop.Reverse();
                    }
                    loops.Add(loop);
                    edgeLoops.Add(edgeLoop);
                }
            }
            outputEdgeLoops = edgeLoops;
            return loops;
        }

        /// <summary>
        ///     Convexes the hull2 d area.
        /// </summary>
        /// <param name="edgeList">The edge list.</param>
        /// <param name="cuttingPlane">The cutting plane.</param>
        /// <returns>System.Double.</returns>
        private static double ConvexHull2DArea(Dictionary<int, Edge> edgeList, Flat cuttingPlane)
        {
            //Don't bother with loops. Just get all the intercept vertices, project to 2d and run 2dConvexHull
            var vertices =
                edgeList.Select(
                    edge =>
                        MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal,
                            cuttingPlane.DistanceToOrigin, edge.Value.To, edge.Value.From));
            var points = MiscFunctions.Get2DProjectionPoints(vertices.ToArray(), cuttingPlane.Normal, true);
            return MinimumEnclosure.ConvexHull2DArea(MinimumEnclosure.ConvexHull2D(points));
        }

        /// <summary>
        ///     Boundings the rectangle area.
        /// </summary>
        /// <param name="edgeList">The edge list.</param>
        /// <param name="cuttingPlane">The cutting plane.</param>
        /// <returns>System.Double.</returns>
        private static double BoundingRectangleArea(Dictionary<int, Edge> edgeList, Flat cuttingPlane)
        {
            //Don't bother with loops. Just get all the intercept vertices, project to 2d and run 2dConvexHull
            var vertices =
                edgeList.Select(
                    edge =>
                        MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal,
                            cuttingPlane.DistanceToOrigin, edge.Value.To, edge.Value.From));
            var points = MiscFunctions.Get2DProjectionPoints(vertices.ToArray(), cuttingPlane.Normal, true);
            var boundingRectangle = MinimumEnclosure.BoundingRectangle(points, false);
            return boundingRectangle.Area;
        }
        #endregion
    }
}