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

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        /// <exception cref="Exception">Pick one or the other. Can't do both at the same time</exception>
        public static List<double[]> NonUniformAreaDecomposition(TessellatedSolid ts, double[] axis, double stepSize,
            double minOffset = double.NaN, bool ignoreNegativeSpace = false)
        {
            var outputData = new List<double[]>();
            if (double.IsNaN(minOffset)) minOffset = Math.Sqrt(ts.SameTolerance);
            if (stepSize <= minOffset * 2)
            {
                //"step size must be at least 2x as large as the min offset");
                //Change it rather that throwing an exception
                stepSize = minOffset * 2 + ts.SameTolerance;
            }
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            MiscFunctions.SortAlongDirection(axis, ts.Vertices.ToList(), out List<(Vertex, double)> sortedVertices);

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
                    var inputEdgeLoops = new List<List<Edge>>();
                    var area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out var outputEdgeLoops, inputEdgeLoops,
                        ignoreNegativeSpace); //Y value (area)
                    outputData.Add(new[] { distance, area });

                    //If the difference is far enough, add another data point right before the current vertex
                    //Use the vertex loops provided from the first pass above
                    if (difference2 > 3 * minOffset)
                    {
                        var distance2 = distanceAlongAxis - minOffset; //X value (distance along axis) 
                        cuttingPlane = new Flat(distance2, axis);
                        inputEdgeLoops = outputEdgeLoops;
                        area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out outputEdgeLoops,
                            inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
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

        public enum SnapType
        {
            //The first distance along the given direction
            ClosestAlong,

            //All cross sections are within the given solid.
            CenterAllInside,

            //Adds a cross section to the top and bottom
            CenterEndsOutside,

            //The last distance along the given direction
            FurthestAlong,
        }

        /// <summary>
        /// Returns the decomposition data found from each slice of the decomposition. This data is used in other methods.
        /// The slices are spaced as close to the stepSizes as possible, while avoiding in-plane faces. The cross sections
        /// will all be interior to the part, unless addCrossSectionAtStartAndEnd = true.
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <param name="stepDistances"></param>
        /// <param name="sortedVertexDistanceLookup"></param>
        /// <param name="snapTo"></param>
        /// <param name="addCrossSectionAtStartAndEndForCenterSnap"></param>
        public static List<DecompositionData> UniformDecomposition(TessellatedSolid solid, double[] direction,
        double stepSize, out Dictionary<int, double> stepDistances,
            out Dictionary<int, double> sortedVertexDistanceLookup,
            SnapType snapTo, bool addCrossSectionAtStartAndEndForCenterSnap = false)
        {
            return UniformDecomposition(new List<TessellatedSolid> { solid }, direction, stepSize, out stepDistances,
                out sortedVertexDistanceLookup, snapTo, addCrossSectionAtStartAndEndForCenterSnap)[0];
        }

        /// <summary>
        /// Returns the decomposition data found from each slice of the decomposition. This data is used in other methods.
        /// The slices are spaced as close to the stepSizes as possible, while avoiding in-plane faces. The cross sections
        /// will all be interior to the part, unless addCrossSectionAtStartAndEnd = true.
        /// This version supports multiple solids, with one set of stepDistances, such that the total distance along the direction
        /// includes the vertices from all the solids. If a solid does not have a cross section at a particular distance
        /// (i.e. starts before other solid or ends after), then it's DecompositionData will either end early or start late.
        /// sortedVertexDistanceLookup is only created if there is just one solid (otherwise the Vertex.IndexInList will have duplicates).
        /// </summary>
        /// <param name="solids"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <param name="stepDistances"></param>
        /// <param name="sortedVertexDistanceLookup"></param>
        /// <param name="snapTo"></param>
        /// <param name="addCrossSectionAtStartAndEndForCenterSnap"></param>
        public static Dictionary<int, List<DecompositionData>> UniformDecomposition(List<TessellatedSolid> solids, double[] direction,
        double stepSize, out Dictionary<int, double> stepDistances,
        out Dictionary<int, double> sortedVertexDistanceLookup,
        SnapType snapTo, bool addCrossSectionAtStartAndEndForCenterSnap = false)
        {
            sortedVertexDistanceLookup = new Dictionary<int, double>();

            //First, sort the vertices along the given axis. Duplicate distances are not important.
            MiscFunctions.SortAlongDirection(direction, solids.SelectMany(s => s.Vertices), out List<(Vertex, double)> sortedVertices);
            //Create a distance lookup dictionary based on the vertex indices
            //This only works if there is just one solid
            if (solids.Count == 1) sortedVertexDistanceLookup = sortedVertices.ToDictionary(element => element.Item1.IndexInList, element => element.Item2);

            var edgeListDictionary = new Dictionary<int, Dictionary<int, Edge>>();
            var firstDistance = sortedVertices.First().Item2;
            var furthestDistance = sortedVertices.Last().Item2;

            //Choose whichever min offset is smaller
            var minOffset = Math.Min(Math.Sqrt(solids[0].SameTolerance), stepSize / 1000); //solids[0].SameTolerance

            //This is a list of all the step indices matched with its distance along the axis.
            //This may be different that just multiplying the step index by the step size, because
            //minor adjustments occur to avoid cutting through vertices.
            stepDistances = GetEvenlySpacedStepDistances(snapTo, stepSize, minOffset, firstDistance, furthestDistance, 
                out int numSteps, out bool addToStart, out bool addToEnd);

            //Initialize the size of the list.
            var outputData = new Dictionary<int, List<DecompositionData>>();

            //Initialize the dictionaries that store info for each solid           
            var inputEdgeLoops = new Dictionary<int, List<List<Edge>>>();
            for (var i = 0; i < solids.Count; i++)
            {
                outputData[i] = new List<DecompositionData>(new DecompositionData[numSteps]);
                edgeListDictionary[i] = new Dictionary<int, Edge>();
                inputEdgeLoops[i] = new List<List<Edge>>();
                foreach (var v in solids[i].Vertices) v.ReferenceIndex = i;
            }

            //Start the step index at +1, so that the increment can be at the start of the while loop, 
            //making the final stepIndex correct for use in a later function.
            var stepIndex = addToStart ? 1 : 0;
            //Stop at -1 if adding additional cross sections, because there is one item at the end of the list we want to skip.
            var n = addToEnd ? numSteps - 1 : numSteps;
            var currentVertexIndex = 0;
            double distanceAlongAxis;
            while (stepIndex < n)
            {
                distanceAlongAxis = stepDistances[stepIndex];

                //Update vertex/edge list up until distanceAlongAxis
                for (var i = currentVertexIndex; i < sortedVertices.Count; i++)
                {
                    //Update the current vertex index so that this vertex is not visited again
                    //unless it causes the break ( > distanceAlongAxis), then it will start the 
                    //the next iteration.
                    currentVertexIndex = i;
                    var element = sortedVertices[i];
                    var vertex = element.Item1;
                    var solid = vertex.ReferenceIndex;
                    var vertexDistanceAlong = element.Item2;
                    //If a vertex is too close to the current distance, move it forward by the min offset.
                    //Update the edge list with this vertex.
                    if (vertexDistanceAlong.IsPracticallySame(distanceAlongAxis, minOffset))
                    {
                        if (stepIndex == n - 1)
                        {
                            //Move backward
                            distanceAlongAxis = vertexDistanceAlong - minOffset * 1.1;
                        }
                        else
                        {
                            //Move the distance enough so that this vertex is now less than 
                            distanceAlongAxis = vertexDistanceAlong + minOffset * 1.1;
                        }
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
                        inputEdgeLoops[solid] = new List<List<Edge>>();

                        //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                        //and the second removes it from the list.
                        if (edgeListDictionary[solid].ContainsKey(edge.IndexInList))
                        {
                            edgeListDictionary[solid].Remove(edge.IndexInList);
                        }
                        else
                        {
                            edgeListDictionary[solid].Add(edge.IndexInList, edge);
                        }
                    }
                }

                //Check to make sure that the minor shifts in the distance in the for loop above 
                //Did not move the distance beyond the furthest distance
                if (distanceAlongAxis > furthestDistance) break;

                //Update each solid
                for (var i = 0; i < solids.Count; i++)
                {
                    //Check to see if the solid has any cross sections at this depth.
                    if (!edgeListDictionary[i].Any()) break;
                    //Make the slice
                    var counter = 0;
                    var current3DLoops = new List<List<Vertex>>();
                    var successful = true;
                    var cuttingPlane = new Flat(distanceAlongAxis, direction);
                    do
                    {
                        try
                        {
                            current3DLoops = GetLoops(edgeListDictionary[i], cuttingPlane, out var outputEdgeLoops,
                                inputEdgeLoops[i]);

                            //Use the same output edge loops for outer while loop, since the edge list does not change.
                            //If there is an error, it will occur before this loop.
                            inputEdgeLoops[i] = outputEdgeLoops;
                        }
                        catch
                        {
                            counter++;
                            distanceAlongAxis += minOffset;
                            successful = false;
                        }
                    } while (!successful && counter < 4);


                    if (successful)
                    {
                        //Get a list of 2D paths from the 3D loops
                        var currentPaths =
                            current3DLoops.Select(
                                cp =>
                                    MiscFunctions.Get2DProjectionPointsAsLightReorderingIfNecessary(cp, direction,
                                        out _));

                        //Get the area of this layer
                        var area = current3DLoops.Sum(p => MiscFunctions.AreaOf3DPolygon(p, direction));
                        if (area < 0)
                        {
                            //Rather than throwing an exception, just assume the polygons were the wrong direction      
                            Debug.WriteLine(
                                "Area for a cross section in UniformDirectionalDecomposition was negative. This means there was an issue with the polygon ordering");
                        }

                        //Add the data to the output
                        //Use the original distance value for this index.
                        outputData[i][stepIndex] = new DecompositionData(currentPaths, current3DLoops, stepDistances[stepIndex], stepIndex);
                    }
                    else
                    {
                        Debug.WriteLine("Slice at this distance was unsuccessful, even with multiple minimum offsets.");
                    }
                }

                stepIndex++;
            }

            if (addToStart)
            {
                for (var i = 0; i < solids.Count; i++)
                {
                    AddToStartOfDecomposition(outputData[i], direction, stepDistances);                  
                }
            }
            if (addToEnd)
            {
                for (var i = 0; i < solids.Count; i++)
                {
                    AddToEndOfDecomposition(outputData[i], direction, stepDistances);
                }
            }

            return outputData;
        }

        private static void AddToStartOfDecomposition(List<DecompositionData> outputData, double[] direction, Dictionary<int, double> stepDistances)
        {
            var startIndex = outputData.First(d => d != null).StepIndex;
            var firstCrossSection = new List<List<Vertex>>();
            foreach (var path in outputData[startIndex].Paths)
            {
                firstCrossSection.Add(MiscFunctions.GetVerticesFrom2DPoints(path.Path.Select(p => new Point(p)).ToList(),
                    direction, stepDistances[startIndex - 1]));
            }
            outputData[startIndex - 1] = new DecompositionData(outputData[startIndex].Paths,
                firstCrossSection, stepDistances[startIndex - 1], startIndex - 1);
        }

        private static void AddToEndOfDecomposition(List<DecompositionData> outputData, double[] direction, Dictionary<int, double> stepDistances)
        {
            var lastIndex = outputData.Last(d => d != null).StepIndex + 1; //Add one, since we are adding to the end
            var lastCrossSection = new List<List<Vertex>>();
            foreach (var path in outputData[lastIndex - 1].Paths)
            {
                lastCrossSection.Add(MiscFunctions.GetVerticesFrom2DPoints(
                    path.Path.Select(p => new Point(p)).ToList(),
                    direction, stepDistances[lastIndex]));
            }

            outputData[lastIndex] = new DecompositionData(outputData[lastIndex - 1].Paths, lastCrossSection,
                stepDistances[lastIndex], lastIndex);
        }

        private static Dictionary<int, double> GetEvenlySpacedStepDistances(SnapType snapTo,  double stepSize, double minOffset,
            double firstDistance, double furthestDistance, out int numSteps, out bool addToStart, out bool addToEnd)
        {
            var length = furthestDistance - firstDistance;
            var t = (int)(length / stepSize);
            numSteps = t + 1; //Round up to nearest integer (or down and then add 1)
            var remainder = length - t * stepSize;
            if (remainder.IsPracticallySame(stepSize, minOffset))
            {
                remainder = 0.0;
                numSteps++;
            }

            ////If the remainder is less than the minOffset, set it equal to the step size.
            ////This is to avoid the issue of missing the last cutting plane. 
            //if (remainder.IsNegligible(minOffset * 2)) remainder += stepSize;

            double topRemainder;
            double bottomRemainder;
            addToStart = false;
            addToEnd = false;
            switch (snapTo)
            {
                case SnapType.ClosestAlong:
                    topRemainder = 0.0;
                    bottomRemainder = stepSize - remainder; //move outward (positive)
                    numSteps++; //one cross section will be outside the part
                    addToEnd = true;
                    break;
                case SnapType.CenterAllInside: //Subtract the remainder, split between the top and bottom.
                    topRemainder = -remainder / 2; //move inward (negative)
                    bottomRemainder = -remainder / 2; //move inward (negative)
                    addToStart = true;
                    addToEnd = true;
                    break;
                case SnapType.CenterEndsOutside: //Add (stepsize - remainder) split between the top and bottom. 
                    topRemainder = (stepSize - remainder) / 2; //move outward (positive)
                    bottomRemainder = (stepSize - remainder) / 2; //move outward (positive)
                    numSteps++; //Add the normal +1 cross section caused by centering and adding the remainder
                    addToStart = true;
                    addToEnd = true;
                    break;
                case SnapType.FurthestAlong:
                    topRemainder = stepSize - remainder; //move outward (positive)
                    bottomRemainder = 0.0;
                    numSteps++; //one cross section will be outside the part
                    addToStart = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(snapTo), snapTo, null);
            }

            var stepDistances = new Dictionary<int, double>();
            if (numSteps < 2) return null; //Steps will not be accurate if too few. 
            var distanceAlongAxis = firstDistance - topRemainder; //positive moves inward, negative outward      
            for (var i = 0; i < numSteps; i++)
            {
                stepDistances.Add(i, distanceAlongAxis);
                distanceAlongAxis += stepSize;
            }
            if (!(distanceAlongAxis - stepSize).IsPracticallySame(furthestDistance + bottomRemainder, 0.0001))
            {
                stepDistances.Add(numSteps, distanceAlongAxis);
                numSteps++;
                //throw new Exception(); //positive moves inward, negative outward     
            }
            return stepDistances;
        }

                    {
                        lastCrossSection.Add(MiscFunctions.GetVerticesFrom2DPoints(
                            path.Path.Select(p => new Point(p)).ToList(),
                            direction, stepDistances[lastIndex]));
                    }

                    outputData[i][lastIndex] = new DecompositionData(outputData[i][lastIndex - 1].Paths, lastCrossSection,
                        stepDistances[lastIndex], lastIndex);
                }
            }

            return outputData;
        }

        private static List<Vertex> GetIntersections(IEnumerable<long> edges, Dictionary<int, Vertex> vertexLookup, double[] direction, double distanceAlongAxis)
        {
            var intersections = new List<Vertex>();
            foreach (var edge in edges)
            {
                (var v1, var v2) = Edge.GetVertexIndices(edge);
                var vertex1 = vertexLookup[v1];
                var vertex2 = vertexLookup[v2];
                var intersectionVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(direction, distanceAlongAxis, vertex1, vertex2);
                intersections.Add(intersectionVertex);
            }
            return intersections;
        }

        #endregion

        #region Additive Volume

        /// <summary>
        /// Gets the additive volume given a list of decomposition data
        /// </summary>
        /// <param name="decompData"></param>
        /// <param name="layerHeight"></param>
        /// <param name="scanningAccuracy"></param>
        /// <param name="outputData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double AdditiveVolume(List<DecompositionData> decompData, double layerHeight,
            double scanningAccuracy, out List<DecompositionData> outputData)
        {
            outputData = new List<DecompositionData>();
            var previousPolygons = new List<PolygonLight>();
            var previousDistance = 0.0;
            var previousArea = 0.0;
            var additiveVolume = 0.0;
            var i = 0;
            var n = decompData.Count;
            foreach (var data in decompData)
            {
                var currentPaths = data.Paths;
                //Offset the distance back by the layer height. THis acts as a vertical offset
                var distance = data.DistanceAlongDirection - layerHeight;
                //currentPaths = PolygonOperations.UnionEvenOdd(currentPaths);

                //Offset if the additive accuracy is significant
                var areaPriorToOffset = currentPaths.Sum(p => p.Area);
                var offsetPaths = !scanningAccuracy.IsNegligible() ? PolygonOperations.OffsetSquare(currentPaths, scanningAccuracy) : new List<PolygonLight>(currentPaths);
                var areaAfterOffset = offsetPaths.Sum(p => p.Area);
                //Simplify the paths, but remove any that are eliminated (e.g. points are all very close together)
                var simpleOffset = offsetPaths.Select(p => PolygonOperations.SimplifyFuzzy(p.Path))
                    .Where(simplePath => simplePath.Any()).Select(p => new PolygonLight(p)).ToList();
                var areaAfterSimplification = simpleOffset.Sum(p => p.Area);
                if (areaPriorToOffset > areaAfterOffset) throw new Exception("Path is ordered incorrectly");
                if (!areaAfterOffset.IsPracticallySame(areaAfterSimplification, areaAfterOffset * .05)) throw new Exception("Simplify Fuzzy Altered the Geometry more than 5% of the area");

                //Union this new set of polygons with the previous set.
                if (previousPolygons.Any()) //If not the first iteration
                {
                    previousPolygons = previousPolygons.Select(p => PolygonOperations.SimplifyFuzzy(p.Path))
                        .Where(simplePath => simplePath.Any()).Select(p => new PolygonLight(p)).ToList();
                    try
                    {
                        currentPaths = PolygonOperations.Union(previousPolygons, simpleOffset);
                    }
                    catch
                    {
                        var testArea1 = simpleOffset.Sum(p => p.Area);
                        var testArea2 = previousPolygons.Sum(p => p.Area);
                        if (testArea1.IsPracticallySame(testArea2, 0.01))
                        {
                            currentPaths = simpleOffset;
                            //They are probably throwing an error because they are closely overlapping
                        }
                        else
                        {
                            currentPaths = outputData.Last().Paths;
                        }
                    }
                }

                //Get the area of this layer
                var area = currentPaths.Sum(p => p.Area);
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
                    var area2 = simpleOffset.Sum(p => p.Area);
                    if (area2 < 0)
                    {
                        //Rather than throwing an exception, just assume the polygons were the wrong direction      
                        area2 = -area2;
                        Debug.WriteLine("The first polygon in the Additive Volume estimate was negative. This means there was an issue with the polygon ordering");
                    }
                    additiveVolume += layerHeight * area2;
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

                        ////Run Mode: use previous area
                        //Debug.WriteLine("Error in your implementation. This should never occur");
                        //area = previousArea;

                    }
                    additiveVolume += deltaX * previousArea;
                    outputData.Add(new DecompositionData(currentPaths, distance));
                }

                //This is the last iteration. Add it to the output data.
                if (i == n - 1)
                {
                    outputData.Add(new DecompositionData(currentPaths, distance + layerHeight));
                    additiveVolume += layerHeight * area;
                }

                previousPolygons = currentPaths;
                previousDistance = distance;
                previousArea = area;
                i++;
            }
            return additiveVolume;
        }

        /// <summary>
        /// Gets the additive volume given a list of decomposition data
        /// </summary>
        /// <param name="decompData"></param>
        /// <param name="layerHeight"></param>
        /// <param name="scanningAccuracy"></param>
        /// <param name="outputData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double AdditiveVolumeWithoutSupport(List<DecompositionData> decompData, double layerHeight,
            double scanningAccuracy, out List<DecompositionData> outputData)
        {
            var n = decompData.Count;
            var offsetCrossSections = new List<PolygonLight>[n];
            //Offset all paths 
            for (var i = 0; i < n; i++)
            {
                var currentCrossSection = decompData[i].Paths;
                //Offset if the additive accuracy is significant
                var areaPriorToOffset = currentCrossSection.Sum(p => p.Area);
                var offsetPaths = !scanningAccuracy.IsNegligible() ? PolygonOperations.OffsetSquare(currentCrossSection, scanningAccuracy) : currentCrossSection;
                var areaAfterOffset = offsetPaths.Sum(p => p.Area);
                //Simplify the paths, but remove any that are eliminated (e.g. points are all very close together)
                var simpleOffset = offsetPaths.Select(p => PolygonOperations.SimplifyFuzzy(p.Path))
                    .Where(simplePath => simplePath.Any()).Select(p => new PolygonLight(p)).ToList();
                var areaAfterSimplification = simpleOffset.Sum(p => p.Area);
                if (areaPriorToOffset > areaAfterOffset) throw new Exception("Path is ordered incorrectly");
                if (!areaAfterOffset.IsPracticallySame(areaAfterSimplification, areaAfterOffset * .05))
                    throw new Exception("Simplify Fuzzy Altered the Geometry more than 5% of the area");
                offsetCrossSections[i] = new List<PolygonLight>(simpleOffset);
            };

            outputData = new List<DecompositionData>();

            //Foreach cross section, union it with the next two cross sections and the prior one to ensure the surfaces are covered.
            //This creates a cross section that is dependent on 4 offset crossSections (or three layers).
            //This assumes that cross sections were added to the top and bottom of decomp data.    
            //This also assumes that the extrusion will be done from the cross section along the build direction.

            //Add the first and second cross section
            //var onePrior = offsetCrossSections[1]; 
            //outputData.Add(new DecompositionData(onePrior.Select(p => p.Path), previousDistance));
            //previousDistance += layerHeight;
            var additiveVolume = 0.0;// Math.Abs(onePrior.Sum(p => p.Area)) * layerHeight;
            //var twoPrior = PolygonOperations.Union(offsetCrossSections[0], offsetCrossSections[2]);
            //outputData.Add(new DecompositionData(twoPrior.Select(p => p.Path), previousDistance));
            //additiveVolume += Math.Abs(twoPrior.Sum(p => p.Area)) * layerHeight;
            //var priorVolume = 0.0;

            double currentVolume;
            if (true) //Add extra top layer
            {
                outputData.Add(new DecompositionData(offsetCrossSections[0].Select(p => p.Path), decompData[0].DistanceAlongDirection - layerHeight));
                currentVolume = Math.Abs(offsetCrossSections[0].Sum(p => p.Area)) * layerHeight;
                additiveVolume += currentVolume;
            }
            var union = offsetCrossSections[1];
            for (var i = 0; i < n - 1; i++)
            {
                var distance = decompData[i].DistanceAlongDirection;
                var deltaX = Math.Abs(distance - decompData[i + 1].DistanceAlongDirection);
                var current = offsetCrossSections[i];
                try
                {
                    //Union with next two and the prior one
                    if (i > 0) union = PolygonOperations.Union(current, offsetCrossSections[i - 1]); //if not the first layer
                    if (i < n - 1) union = PolygonOperations.Union(union, offsetCrossSections[i + 1]); //if not the final layer
                    if (i < n - 2) union = PolygonOperations.Union(union, offsetCrossSections[i + 2]);
                    currentVolume = Math.Abs(union.Sum(p => p.Area)) * deltaX;
                    ////Set the additive information. Instead of using the current path area, use the max between this one & below it.
                    ////If the part was cut at this index, the furthest cross section would be at i.
                    ////Therefore, the additive shape from i-1 to i should be based on the union of i-1 and i.
                    ////Once we area past this index, the union 
                    ////Since we are at i and want the volume below it, we want to update the prior two cross sections / volumes
                    //onePriorUnion = PolygonOperations.Union(onePriorCrossSection, current);
                    //currentVolume = Math.Abs(onePrior.Sum(p => p.Area)) * deltaX; //volume between onePrior and current
                    //twoPrior = PolygonOperations.Union(twoPrior, current);
                    //priorVolume = Math.Abs(twoPrior.Sum(p => p.Area)) * deltaX; //volume between twoPrior and onePrior               
                }
                catch
                {
                    //They are probably throwing an error because they are closely overlapping.
                    //Use the previous path
                    var onePrior = new List<PolygonLight>(outputData.Last().Paths);
                    currentVolume = Math.Abs(onePrior.Sum(p => p.Area)) * deltaX; //volume between onePrior and current   
                }

                //if (i != 1)
                //{
                outputData.Add(new DecompositionData(union.Select(p => p.Path), distance));
                additiveVolume += currentVolume;
                //if (i == n - 1)
                //{
                //    //Add the final layer
                //    outputData.Add(new DecompositionData(onePriorUnion.Select(p => p.Path), distance));
                //    additiveVolume += currentVolume;
                //}
                //}

                //Update the values for the next iteration.
                // previousDistance = distance;
                //Two prior is now done. One prior becomes the prior and the current becomes onePrior
                //oPrior = onePriorUnion;
                //priorVolume = currentVolume;
                //onePriorUnion = currentUnion;
                //onePrior = current;
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
        public static List<PolygonLight> GetCrossSectionAtGivenDistance(TessellatedSolid ts, double[] direction, double distance)
        {
            var crossSection3D = Get3DCrossSectionAtGivenDistance(ts, direction, distance);

            //Get a list of 2D paths from the 3D loops
            //Get 2D projections does not reorder list if the cutting plane direction is negative
            //So we need to do this ourselves. 
            //Return null if crossSection3D is null (uses null propagation "?")
            var crossSection = crossSection3D?.Select(loop => MiscFunctions.Get2DProjectionPointsAsLightReorderingIfNecessary(loop, direction, out _, ts.SameTolerance)).ToList();
            return crossSection?.Select(p => new PolygonLight(p)).ToList();
        }

        /// <summary>
        /// Gets the Cross Section for a given distance
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static List<List<Vertex>> Get3DCrossSectionAtGivenDistance(TessellatedSolid ts, double[] direction, double distance)
        {
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            MiscFunctions.SortAlongDirection(direction, ts.Vertices.ToList(), out List<(Vertex, double)> sortedVertices);
            if (distance.IsLessThanNonNegligible(sortedVertices.First().Item2) ||
                distance.IsGreaterThanNonNegligible(sortedVertices.Last().Item2))
            {
                //Distance is out of range of this solid.
                return null;
            }

            var edgeListDictionary = new Dictionary<int, Edge>();
            var previousVertexDistance = sortedVertices[0].Item2; //This value can be negative
            foreach (var element in sortedVertices)
            {
                var vertex = element.Item1;
                var currentVertexDistance = element.Item2; //This value can be negative

                if (currentVertexDistance.IsPracticallySame(distance, ts.SameTolerance) || currentVertexDistance > distance)
                {
                    //Determine cross sectional area for section as close to given distance as possible (after previous vertex, but before current vertex)
                    //But not actually on the current vertex
                    double distance2;
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
                        //There was a significant enough gap between points to use the exact distance
                        distance2 = distance;
                    }

                    var cuttingPlane = new Flat(distance2, direction);
                    var inputEdgeLoops = new List<List<Edge>>();
                    var loops = GetLoops(edgeListDictionary, cuttingPlane, out _, inputEdgeLoops);
                    return loops;
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

        public class SectionData3D
        {
            public List<(Vertex, double)> SortedVertices;
            public Dictionary<int, Vertex> VertexLookup;
            public Dictionary<int, List<long>> VertexEdges;
            public Dictionary<long, PolygonalFace[]> EdgeFaces;
            public Dictionary<PolygonalFace, long[]> FaceEdgeLookup;
            public HashSet<long> EdgeList;
            public int Index;
            public int PreviousIndex;
            public double[] Direction;
            public double Tolerance;
            public List<List<long>> InputEdgeLoops;
            public int N;
        }

        /// <summary>
        /// Gets the Section 3D Data for a given distance without using the Edge class. This can be used to get
        /// 3D cross section at a given distance multiple times, without needing to re-create it.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="faces"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static SectionData3D GetSection3dData(PolygonalFace[] faces, Vertex[] vertices, double tolerance, double[] direction)
        {
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            MiscFunctions.SortAlongDirection(direction, vertices.ToList(), out List<(Vertex, double)> sortedVertices);

            var vertexLookup = new Dictionary<int, Vertex>();
            //initialize the vertex to edge dictionary
            var vertexEdges = new Dictionary<int, List<long>>(); //Key = vertex index, Value = List<edge checkSums>
            foreach (var vertex in vertices)
            {
                vertexEdges.Add(vertex.IndexInList, new List<long>());
                vertexLookup.Add(vertex.IndexInList, vertex);
            }

            //The easiest way to build the lookup dictionaries is to create edges for all the faces
            //Duplicate edges will be an issue, since edges are longs and are stored in a hashset
            var edgeFaces = new Dictionary<long, PolygonalFace[]>(); //Key = edge checksum, Value = face 1 , face 2. Owned/Other is not known.
            var faceEdgeLookup = new Dictionary<PolygonalFace, long[]>(faces.Length);
            foreach (var face in faces)
            {
                var edges = new long[3];
                for (var i = 0; i < 3; i++)
                {
                    var j = (i == 2) ? 0 : i + 1;
                    var v1 = face.Vertices[i];
                    var v2 = face.Vertices[j];
                    var newEdge = Edge.GetEdgeChecksum(v1, v2);
                    edges[i] = newEdge;
                    if (edgeFaces.ContainsKey(newEdge))
                    {
                        //Add the face to the edge. The vertices are already attached to the edge.
                        edgeFaces[newEdge][1] = face;
                    }
                    else
                    {
                        //Add the face to the edge and the edge to the vertices
                        edgeFaces[newEdge] = new[] { face, null };
                        vertexEdges[v1.IndexInList].Add(newEdge);
                        vertexEdges[v2.IndexInList].Add(newEdge);
                    }
                }
                faceEdgeLookup[face] = edges;
            }

            var data = new SectionData3D()
            {
                SortedVertices = sortedVertices,
                VertexLookup = vertexLookup,
                VertexEdges = vertexEdges,
                EdgeFaces = edgeFaces,
                FaceEdgeLookup = faceEdgeLookup,
                EdgeList = new HashSet<long>(),
                Index = 0,
                PreviousIndex = 0,
                Direction = direction,
                InputEdgeLoops = new List<List<long>>(),
                Tolerance = tolerance,
                N = sortedVertices.Count
            };
            return data;
        }

        public static List<List<Vertex>> Get3DCrossSectionAtGivenDistance(PolygonalFace[] faces, Vertex[] vertices, double tolerance, double[] direction, double distance,
            out SectionData3D data)
        {
            data = GetSection3dData(faces, vertices, tolerance, direction);
            return Get3DCrossSectionAtGivenDistance(data, distance);
        }

        /// <summary>
        /// Gets the Cross Section for a given distance without using the Edge class.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="faces"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static List<PolygonLight> GetCrossSectionAtGivenDistance(SectionData3D sectionData, double distance)
        {
            var crossSection3D = Get3DCrossSectionAtGivenDistance(sectionData, distance);

            //Get a list of 2D paths from the 3D loops
            //Get 2D projections does not reorder list if the cutting plane direction is negative
            //So we need to do this ourselves. 
            //Return null if crossSection3D is null (uses null propagation "?")
            var crossSection = crossSection3D?.Select(loop =>
                MiscFunctions.Get2DProjectionPointsAsLightReorderingIfNecessary(loop, sectionData.Direction, out _, sectionData.Tolerance)).ToList();
            var polygons = crossSection?.Select(p => new PolygonLight(p)).ToList();
            if (polygons?.Sum(a => a.Area) < 0.0) Debug.WriteLine("Cross section should not have a negative area");
            return polygons;
        }

        /// <summary>
        /// This function returns the 3D cross section at a given distance. The SectionData3D class can be used to efficiently
        /// get the cross section multiple times. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tolerance"></param>
        /// <param name="direction"></param>
        /// <param name="targetDistance"></param>
        /// <returns></returns>
        public static List<List<Vertex>> Get3DCrossSectionAtGivenDistance(SectionData3D data, double targetDistance)
        {
            if (targetDistance.IsLessThanNonNegligible(data.SortedVertices.First().Item2) ||
              targetDistance.IsGreaterThanNonNegligible(data.SortedVertices.Last().Item2))
            {
                //Distance is out of range of this solid.
                return null;
            }

            //First, we need to get the current vertex distance to determine if the target distance is forward or backward from it.
            //Then, we need to the the vertex distance directionally prior in the list (previous == -- if forward and ++ if backward)
            var previousVertexDistance = data.SortedVertices[data.PreviousIndex].Item2;
            var currentVertexDistance = data.SortedVertices[data.Index].Item2; 
            var sign = targetDistance >= Math.Min(previousVertexDistance, currentVertexDistance) ? 1 : -1;

            //If the sign does not match the order of the indices, then the current and previous need to be flipped
            if (sign == data.PreviousIndex - data.Index)
            {
                var temp = data.Index;
                data.Index = data.PreviousIndex;
                data.PreviousIndex = temp;
                previousVertexDistance = currentVertexDistance;
            }

            //This function essentially allows cross sections to slide forward or backward along the data direction.
            //If the target distance is greater than the previous distance, step forward along the SortedVertices until a vertex distance > target distance.
            //Else, if the target distance is less than the previous distance, step backward along the SortedVertices until a vertex distance < target distance.            
            var start = data.Index;
            var tolerance = data.Tolerance;
            for (data.Index = start; sign == 1 ? data.Index < data.N : data.Index >= 0; data.Index += sign) //update both the current and previous index
            {
                currentVertexDistance = data.SortedVertices[data.Index].Item2;
                
                //Check if we are at a vertex that is beyond or at the target distance
                if (currentVertexDistance.IsPracticallySame(targetDistance, tolerance) || 
                    (sign == 1 ? currentVertexDistance > targetDistance : currentVertexDistance < targetDistance))
                {
                    //Determine cross sectional area for section as close to given distance as possible (after previous vertex, but before current vertex)
                    //But not actually on the current vertex
                    var distance2 = targetDistance;
                    if (currentVertexDistance.IsPracticallySame(targetDistance))
                    {
                        if (sign == 1 ? previousVertexDistance < targetDistance - tolerance :
                            previousVertexDistance > targetDistance + tolerance)
                        {
                            distance2 = targetDistance - tolerance * sign; //subtract tolerance for a forward sign and add if negative
                        }
                        else
                        {
                            //Take the average if the function above did not work.
                            distance2 = (previousVertexDistance + currentVertexDistance / 2);
                        }
                    }
                    //Else, there was a significant enough gap between points to use the exact distance.
                    return GetLoops(data, distance2); //May return null if line intersections are not valid
                }

                var vertex = data.SortedVertices[data.Index].Item1;
                foreach (var edge in data.VertexEdges[vertex.IndexInList])
                {
                    //reset the input edge loops any time the list of edges changes
                    data.InputEdgeLoops = new List<List<long>>(); 
                    //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                    //and the second removes it from the list.
                    //Note: this allows us to move forward or backwards along edges.
                    if (data.EdgeList.Contains(edge))
                    {
                        data.EdgeList.Remove(edge);
                    }
                    else
                    {
                        data.EdgeList.Add(edge);
                    }
                }
                previousVertexDistance = currentVertexDistance;
                data.PreviousIndex = data.Index;
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
            public List<PolygonLight> Paths;

            /// <summary>
            /// An optional list of paths that have been offset
            /// </summary>
            public List<PolygonLight> OffsetPaths;

            /// <summary>
            /// The convex hull for each slice. Optional parameter that is only set when SetConvexHull() is called.
            /// </summary>
            public IEnumerable<PointLight> ConvexHull;

            /// <summary>
            /// A list of the paths that make up the slice of the solid at this distance along this direction
            /// </summary>
            public List<List<Vertex>> Vertices;

            /// <summary>
            /// The distance along this direction
            /// </summary>
            public double DistanceAlongDirection;

            /// <summary>
            /// The index along the direction
            /// </summary>
            public int StepIndex;

            /// <summary>
            /// The Decomposition Data Class used to store information from A Directional Decomposition
            /// </summary>
            /// <param name="paths"></param>
            /// <param name="vertices"></param>
            /// <param name="distanceAlongDirection"></param>
            public DecompositionData(IEnumerable<List<PointLight>> paths, IEnumerable<List<Vertex>> vertices, double distanceAlongDirection, int stepIndex)
            {
                Paths = new List<PolygonLight>(paths.Select(p => new PolygonLight(p)));
                if (vertices != null)
                    Vertices = new List<List<Vertex>>(vertices);
                DistanceAlongDirection = distanceAlongDirection;
                StepIndex = stepIndex;
            }
            public DecompositionData(IEnumerable<PolygonLight> paths, IEnumerable<List<Vertex>> vertices, double distanceAlongDirection, int stepIndex)
            {
                Paths = new List<PolygonLight>(paths);
                if (vertices != null)
                    Vertices = new List<List<Vertex>>(vertices);
                DistanceAlongDirection = distanceAlongDirection;
                StepIndex = stepIndex;
            }

            /// <summary>
            /// The Decomposition Data Class used to store information from A Directional Decomposition
            /// </summary>
            /// <param name="paths"></param>
            /// <param name="distanceAlongDirection"></param>
            public DecompositionData(IEnumerable<List<PointLight>> paths, double distanceAlongDirection)
            {
                Paths = new List<PolygonLight>(paths.Select(p => new PolygonLight(p)));
                DistanceAlongDirection = distanceAlongDirection;
            }
            public DecompositionData(IEnumerable<PolygonLight> paths, double distanceAlongDirection)
            {
                Paths = new List<PolygonLight>(paths);
                DistanceAlongDirection = distanceAlongDirection;
            }

            public void SetConvexHull()
            {
                ConvexHull = MinimumEnclosure.ConvexHull2D(Paths.SelectMany(s => s.Path.Select(p => p)).ToList());
            }

            public void SetOffset(double offsetDistance)
            {
                OffsetPaths = PolygonOperations.OffsetSquare(Paths, offsetDistance);
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
            public List<PointLight> Path2D;

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
                get => _segmentIndex;
                set
                {
                    _segmentIndex = value;
                    //when you set the segment index, set all the edge references as well.
                    //Don't set the vertex references of the edges, since some are not yet visited
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
            public PolygonDataGroup(List<PointLight> path2D, List<Vertex> intersectionVertices,
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
            /// The Segmentation Data Class used to store information from A Directional Segmented Decomposition
            /// </summary>
            /// <param name="areas"></param>
            /// <param name="distanceAlongDirection"></param>
            /// <param name="paths3D"></param>
            /// <param name="edgeLoops"></param>
            /// <param name="stepIndex"></param>
            /// <param name="paths2D"></param>
            public SegmentationData(List<List<PointLight>> paths2D, List<List<Vertex>> paths3D,
                List<List<Edge>> edgeLoops, List<double> areas, double distanceAlongDirection, int stepIndex)
            {
                CrossSectionData = new List<PolygonDataGroup>();
                for (var i = 0; i < paths2D.Count; i++)
                {
                    CrossSectionData.Add(new PolygonDataGroup(paths2D[i], paths3D[i], edgeLoops[i], areas[i], i, stepIndex, distanceAlongDirection));
                }
                DistanceAlongDirection = distanceAlongDirection;
            }
        }
        #endregion

        #region Uniform Directional Segmentation
        /// <summary>
        /// Returns the Directional Segments found from decomposing a solid along a given direction. 
        /// This data is used in other methods. Optional parameter "orderedForcedSteps" adds in steps at the 
        /// given distances, but it must be ordered.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <param name="stepDistances"></param>
        /// <param name="sortedVertexDistanceLookup"></param>
        /// <param name="orderedForcedSteps"></param>
        /// <returns></returns>
        public static List<DirectionalSegment> UniformDirectionalSegmentation(TessellatedSolid ts, double[] direction,
            double stepSize,  out Dictionary<int, double> stepDistances,
            out Dictionary<int, double> sortedVertexDistanceLookup,
            out SegmentationData error, List<double> orderedForcedSteps = null)
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
            //var outputData = new List<SegmentationData>();

            var length = MinimumEnclosure.GetLengthAndExtremeVertices(direction, ts.Vertices,
                out _, out _);

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
            MiscFunctions.SortAlongDirection(direction, ts.Vertices, out List<(Vertex, double)> sortedVertices);
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

            //Start the step index at -1, so that the increment can be at the start of the while loop, 
            //making the final stepIndex correct for use in a later function.
            var stepIndex = -1;
            var nextForcedDistance = double.MaxValue;
            var forcedDistanceIndex = 0;
            var numberOfForcedDistances = 0;
            var cleanOrderedForcedSteps = new List<double>();
            if (orderedForcedSteps != null)
            {
                numberOfForcedDistances = orderedForcedSteps.Count;
                nextForcedDistance = orderedForcedSteps[forcedDistanceIndex];
                cleanOrderedForcedSteps.Add(nextForcedDistance);
                for (var i = 1; i < numberOfForcedDistances; i++)
                {
                    //Remove duplicates
                    if (!orderedForcedSteps[i].IsPracticallySame(cleanOrderedForcedSteps.Last(), minOffset))
                    {
                        cleanOrderedForcedSteps.Add(orderedForcedSteps[i]);
                    }
                }
                numberOfForcedDistances = cleanOrderedForcedSteps.Count;
                forcedDistanceIndex++;
            }
            var priorNonForcedDistanceAlongAxis = distanceAlongAxis;
            while (distanceAlongAxis < furthestDistance - stepSize)
            {
                stepIndex++;

                //This is the current distance along the axis. It will move forward by the step size during each iteration.
                distanceAlongAxis = priorNonForcedDistanceAlongAxis + stepSize;

                if (orderedForcedSteps != null && forcedDistanceIndex < numberOfForcedDistances
                    && (distanceAlongAxis > nextForcedDistance || distanceAlongAxis.IsPracticallySame(nextForcedDistance)))
                {
                    distanceAlongAxis = nextForcedDistance;
                    nextForcedDistance = cleanOrderedForcedSteps[forcedDistanceIndex];
                    forcedDistanceIndex++;
                    //Don't update priorNonForcedDistanceAlongAxis
                }
                else
                {
                    priorNonForcedDistanceAlongAxis = distanceAlongAxis;
                }

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
                    //if (segmentationData != null) outputData.Add(segmentationData);

                    if (!UpdateSegments(segmentationData, inStepVertices, sortedVertexDistanceLookup, direction,
                        allDirectionalSegments))
                    {
                        //Not Successful.
                        error = segmentationData;
                        return new List<DirectionalSegment>();
                    }

                    stepDistances.Add(stepIndex, distanceAlongAxis);

                    foreach (var vertex in inStepVertices)
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
                if (segment.Value.CrossSectionPathDictionary.Count == 0) throw new Exception("A segment must have cross sections");
                //if(segment.Value.StartStepIndexAlongSearchDirection == segment.Value.EndStepIndexAlongSearchDirection) throw new Exception("This segment has zero thickness");
            }

            error = null;
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
        private static bool UpdateSegments(SegmentationData segmentationData, HashSet<Vertex> inStepVertices,
            Dictionary<int, double> vertexDistanceLookup, double[] searchDirection,
            Dictionary<int, DirectionalSegment> allDirectionalSegments)
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
                            a new segment. Simply start a new segment and look through any of the unassigned negative polygons to check
                            if they belong to this new polygon (Do this with the same wrapping technique used earlier).

            Segment Case 6: [Branching] If a positive loop is added to a existing segment (>1 +loops), close that segment and start  
                            new segments, one for each positive loop. It may be required to perform polygon operations to 
                            retain the correct negative loops. 

            In All Cases:   The in-step vertices and edges belong to the parent and child segments. This does repeat information, but there
                            are quite a few different cases and this is the easiest solution (other than not attaching them to any segment).
            
            Fast Transitions: A segment starts at the index after its parent end. If this segment merges with another segment in the next
                                iteration, then it will only be defined for one step (zero thickness). This may also happen if a brand new 
                                segment (A) immediately merges with another segment (B). In this case, segment A will only be defined for the 
                                first step index, since segment B takes over at the next step index. Otherwise, there would be multiple cross
                                sections for a step in an index. 
                                
            Implications:   A segment may only be defined for one step, but its volume should be thought of as extending a half-step forward
                            and backward from that step to form a volume.  
            */
            var distanceAlongAxis = segmentationData.DistanceAlongDirection;
            var firstIterationFailed = false;
            WrappingStep2:

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
                return true;
            }
            #endregion

            #region Wrapping Step 1 (Resolves Segment Cases 2 & 3): wrap edge/vertex pairs forward for each segment.
            //Wrap edge/vertex pairs forward for each segment until all the edges have a vertex 
            //that is further than the current distance (pointing forward). The vertex will get
            //a segment Index assigned to its ReferenceIndex. If a segment wants to use a vertex with 
            //an existing reference index, then stop pursuing that segment and note that the two are connected.
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
                        //Edge Case 2: The other vertex is further that the current distance. Update the segment's current edges
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
                                AddConnectedSegment(otherSegment, connectedSegmentsIndices,
                                    inStepVertices, inStepSegmentVertexSet,
                                    allInStepSegmentVertices);
                                continue;
                            }
                            //Else if
                            if (otherVertex.ReferenceIndex != -1)
                            {
                                //This vertex belongs to another segment 
                                var otherSegment = allDirectionalSegments[otherVertex.ReferenceIndex];
                                AddConnectedSegment(otherSegment, connectedSegmentsIndices,
                                     inStepVertices, inStepSegmentVertexSet,
                                     allInStepSegmentVertices);
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
                                        AddConnectedSegment(otherSegment, connectedSegmentsIndices,
                                            inStepVertices, inStepSegmentVertexSet,
                                            allInStepSegmentVertices);
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
                    if (!segment.NextVertices.Any())
                    {
                        segment.IsFinished = true;
                    }
                }
                #endregion

                #region Segment Case 3: [Merging] one or more segments that connect with the current segment 
                else
                {
                    connectedSegmentsIndices.Add(segment.Index);

                    //Add the finished edges to the reference edges of whichever segment they belong to.
                    //The finished edges are not truly complete in these segments, since the in-step
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
                    var segmentIndex = allDirectionalSegments.Keys.Max() + 1;
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
            var unassignedPositivePolygonDataGroups = new HashSet<PolygonDataGroup>();
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
            //Create new segments from any unused vertices
            var usedInStepVertices = new HashSet<Vertex>();
            while (unusedInStepVertices.Any())
            {
                var startVertex = unusedInStepVertices.First();

                //Don't remove from the unusedInStepVertices list until we are done collecting
                //All the edges that belong to this segment. Otherwise, we will be missing some
                //of the edges between in step vertices.
                var newSegmentIndex = allDirectionalSegments.Any() ? allDirectionalSegments.Keys.Max() + 1 : 0;
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
                        var otherVertex = edge.OtherVertex(vertex);
                        currentEdges.Add(edge);
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
                //smallest wall in the part to insure this). Likewise, it can only have one positive polygon per data group.
                //However, to handle too large of steps, multiple positivePolygonDataGroups can be set, 
                //as long as they have current edges.
                var positivePolygonDataGroups = new List<PolygonDataGroup>();
                foreach(var polygonDataGroup in unassignedPositivePolygonDataGroups)
                { 
                    //It does not matter which edge we check, so just use the first one.
                    var edge = polygonDataGroup.EdgeLoop.First();

                    //If it does not include the edge, check the next data group.
                    if (!currentEdges.Contains(edge)) continue;

                    //Else,  Great. This is the polygon we were looking for.
                    //Go ahead and assign it the segment index.
                    polygonDataGroup.SegmentIndex = newSegmentIndex + positivePolygonDataGroups.Count();
                    positivePolygonDataGroups.Add(polygonDataGroup);
                }
                foreach (var assignedPositivePolygonDataGroup in positivePolygonDataGroups)
                {
                    unassignedPositivePolygonDataGroups.Remove(assignedPositivePolygonDataGroup);
                }

                //There does not have to be a negative polygon, but go ahead and check
                //There can only be one positive polygon data group, but there may be multiple negative ones.
                //For this reason, we need to check all of them and cannot break early.
                //We will then have a loop to remove the newly assigned data groups from the list of unassigned ones.
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

                if(positivePolygonDataGroups.Count > 1 && negativePolygonDataGroups.Any())
                {
                    throw new NotImplementedException("Not yet implemented because of lacking example (which you now have!). " +
                        "Currently, there is no sense as to how to connect the negative polygons to the positive ones.");
                }

                #region  Segment Case 4: [Blind Hole/Pocket]
                //Note that cannot have a blind hole or hidden pocket with a new segment, so it is okay to check where
                //this negative polygon belongs before we finish creating the new segments.
                //This is a blind hole or pocket, not visible from the search direction should exist for pre-existing segments
                //Example of pockets: Aerospace Beam with search direction through side. The pockets on the opposite side are not visible.     
                if (!positivePolygonDataGroups.Any())
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
                            var tempPolygons = new List<PointLight>(negativePolygonDataGroup.Path2D);
                            tempPolygons.Reverse();

                            //IF the intersection results in any overlap, then it belongs to this segment.
                            //As a hole, it cannot belong to multiple segments and cannot split or merge segments.
                            //Note: you cannot just check if a point from the dataSet is inside the positive paths, 
                            //since it the blind hole could be nested inside positive/negative pairings. (ex: a hollow rod 
                            //down the middle of a larger hollow tube. In this case, the hollow rod is a different segment).
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

                                //If this segment is not branching, go ahead and set this negative polygon.
                                //Otherwise, it will be set when creating the branches
                                if (paths.Count == 1)
                                {
                                    currentSegment.AddPolygonDataGroup(negativePolygonDataGroup);
                                }
                                else
                                {
                                    currentSegment.CurrentPolygonDataGroups.Add(negativePolygonDataGroup);
                                    negativePolygonDataGroup.SegmentIndex = -1;
                                    negativePolygonDataGroup.PotentialSegmentIndices.Add(currentSegment.Index);
                                }
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
                    var c = 0;
                    foreach(var positivePolygonDataGroup in positivePolygonDataGroups)
                    {
                        //Use the current edges unless there is more than one positive polygon, in which case, we need to make a new hashset
                        var edges = positivePolygonDataGroups.Count == 1 ? currentEdges : new HashSet<Edge>(positivePolygonDataGroup.EdgeLoop);
                        c += edges.Count;
                        var newSegment = new DirectionalSegment(positivePolygonDataGroup.SegmentIndex,
                            finishedEdges, newSegmentReferenceVertices, edges, searchDirection);
                        allDirectionalSegments.Add(positivePolygonDataGroup.SegmentIndex, newSegment);
                        //Attach the polygon data groups
                        newSegment.AddPolygonDataGroup(positivePolygonDataGroup, false);
                        foreach (var negativePolygonDataGroup in negativePolygonDataGroups)
                        {
                            newSegment.AddPolygonDataGroup(negativePolygonDataGroup, false);
                        }
                    }
                    //Check that the current edge count matches the edge loop count
                    if (c != currentEdges.Count) throw new Exception("Current edges do not match edge loops. Need to debug further and fix");  
                }
                #endregion
            }
            #endregion

            if (unassignedPositivePolygonDataGroups.Any())
            {
                Debug.WriteLine("Unassigned positive loop in directional decomposition, likely caused by too large a step size. Attempting to solve.");
                return false;
                throw new Exception("At this point, only blind holes should be unassigned");
            }

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
                        potentialSegment.BranchSegment(allDirectionalSegments);
                    }
                    else throw new Exception("One of the polygons must have been positive.");
                }
                else
                {
                    throw new Exception("All the polygon data groups must be assigned to a segment by this point");
                }
            }
            #endregion
            return true;
        }

        private static void AddConnectedSegment(DirectionalSegment otherSegment,
            HashSet<int> connectedSegmentsIndices,
            HashSet<Vertex> inStepVertices,
            Stack<Vertex> inStepSegmentVertexSet,
            HashSet<Vertex> allInStepSegmentVertices)
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
            var successful = false;
            var cuttingPlane = new Flat(distanceAlongAxis, direction);
            do
            {
                try
                {
                    var current3DLoops = GetLoops(edgeListDictionary, cuttingPlane, out var outputEdgeLoops,
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

                    //Get a list of 2D paths from the 3D loops
                    var currentPaths =
                        current3DLoops.Select(
                            cp =>
                                MiscFunctions.Get2DProjectionPointsAsLightReorderingIfNecessary(cp, direction,
                                    out _));

                    successful = true; //Irrelevant, since we are returning now.

                    //Add the data to the output
                    return new SegmentationData(currentPaths.ToList(), current3DLoops, outputEdgeLoops, areas, distanceAlongAxis, stepIndex);
                }
                catch
                {
                    counter++;
                    distanceAlongAxis += minOffset;
                }
            } while (!successful && counter < 4);

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
                get => _isFinished;
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
            public void BranchSegment(Dictionary<int, DirectionalSegment> allDirectionalSegments)
            {
                //If a merger resulted in a new segment (this) that needs to be branched, it will have no cross sections set by this point.
                //Replace this segment with new segments. 
                if (!CrossSectionPathDictionary.Any())
                {
                    allDirectionalSegments.Remove(Index);
                    IsDummySegment = true;
                }

                IsFinished = true;
                var newSegments = new List<DirectionalSegment>();
                if (!IsDummySegment)
                {
                    foreach (var positivePolygonDataGroup in CurrentPolygonDataGroups.Where(p => p.Area > 0.0))
                    {
                        var newSegmentIndex = allDirectionalSegments.Keys.Max() + 1;
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
                        var newSegmentIndex = allDirectionalSegments.Keys.Max() + 1;

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
                        //down the middle of a larger hollow tube. In this case, the hollow rod is a different segment).
                        var positiveVersionOfHole = new List<PointLight>(negativePolygonDataGroup.Path2D);
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
                        //Check to make sure that we are not adding a second positive polygon to the same step index 
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
            public void SetReferenceVerticesByStepIndex(Dictionary<int, int> vertexStepIndexReference)
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
                        ReferenceVerticesByStepIndex.Add(vertexStepIndex, new List<Vertex>() { vertex });
                    }
                }
            }
            #endregion

            /// <summary>
            /// Reverses the direction and all associated dictionaries. It is assumed that the steps are to be 
            /// ordered along the reversed direction. The total number of steps along the direction must be given.
            /// </summary>
            public void Reverse(int maxStepIndex, Dictionary<int, double> stepDistances)
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
        /// <param name="inputEdgeLoops">The input edge loops.</param>
        /// <param name="ignoreNegativeSpace">if set to <c>true</c> [ignore negative space].</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Loop did not complete</exception>
        private static double CrossSectionalArea(Dictionary<int, Edge> edgeListDictionary, Flat cuttingPlane,
            out List<List<Edge>> outputEdgeLoops, List<List<Edge>> inputEdgeLoops, bool ignoreNegativeSpace = false)
        {
            var loops = GetLoops(edgeListDictionary, cuttingPlane, out outputEdgeLoops, inputEdgeLoops);
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
            out List<List<Edge>> outputEdgeLoops, List<List<Edge>> inputEdgeLoops)
        {
            var edgeLoops = new List<List<Edge>>();
            var loops = new List<List<Vertex>>();
            if (inputEdgeLoops.Any())
            {
                edgeLoops = inputEdgeLoops; //Note that edge loops should all be ordered correctly
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
                //Hashset was slightly faster during creation and enumeration, 
                //but even more slightly slower at removing. Overall, Hashset 
                //was about 17% faster than a dictionary.
                var edges = new List<Edge>(edgeListDictionary.Values);
                var unusedEdges = new HashSet<Edge>(edges);
                foreach (var startEdge in edges)
                {
                    if (!unusedEdges.Contains(startEdge)) continue;
                    unusedEdges.Remove(startEdge);
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
                            var vector = intersectVertex.Position.subtract(loop.Last().Position, 3);
                            //Use the previous face, since that is the one that contains both of the edges that are in use.
                            var dot = cuttingPlane.Normal.crossProduct(previousFace.Normal).dotProduct(vector, 3);
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
   
        private static List<List<Vertex>> GetLoops(SectionData3D data, double distance)
        {
            var direction = data.Direction;
            var vertexLookup = data.VertexLookup;
            var edgeFaceLookup = data.EdgeFaces;
            var faceEdgeLookup = data.FaceEdgeLookup;

            var edgeLoops = new List<List<long>>();
            var loops = new List<List<Vertex>>();
            if (data.InputEdgeLoops.Any())
            {
                edgeLoops = data.InputEdgeLoops; //Note that edge loops should all be ordered correctly
                foreach (var edgeLoop in edgeLoops)
                {
                    loops.Add(GetIntersections(edgeLoop, vertexLookup, direction, distance));
                }
            }
            else
            {
                //Build an edge list that we can modify, without ruining the original
                //After comparing hashset versus dictionary (with known keys)
                //Hashset was slightly faster during creation and enumeration, 
                //but even more slightly slower at removing. Overall, Hashset 
                //was about 17% faster than a dictionary.
                var edges = new List<long>(data.EdgeList);
                var unusedEdges = new HashSet<long>(edges);
                foreach (var startEdge in edges)
                {
                    if (!unusedEdges.Contains(startEdge)) continue;
                    unusedEdges.Remove(startEdge);
                    var loop = new List<Vertex>();
                    var (vertex1, vertex2) = Edge.GetVertexIndices(startEdge);
                    var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(direction,
                        distance, vertexLookup[vertex1], vertexLookup[vertex2]);
                    loop.Add(intersectVertex);
                    var edgeLoop = new List<long> { startEdge };
                    var (ownedFace, otherFace) = Edge.GetOwnedAndOtherFace(startEdge, edgeFaceLookup[startEdge][0], edgeFaceLookup[startEdge][1]);
                    var startFace = ownedFace;
                    var currentFace = startFace;
                    var previousFace = startFace; //This will be set again before its used.
                    var endFace = otherFace;
                    var nextEdgeFound = false;
                    long nextEdge = -1;
                    var correctDirection = 0.0;
                    var reverseDirection = 0.0;
                    do
                    {
                        //Get the next edge
                        foreach (var edge in faceEdgeLookup[currentFace])
                        {
                            (ownedFace, otherFace) = Edge.GetOwnedAndOtherFace(edge, edgeFaceLookup[edge][0], edgeFaceLookup[edge][1]);
                            if (!unusedEdges.Contains(edge)) continue;
                            if (otherFace == currentFace)
                            {
                                previousFace = otherFace;
                                currentFace = ownedFace;
                                nextEdgeFound = true;
                                nextEdge = edge;
                                break;
                            }
                            if (ownedFace == currentFace)
                            {
                                previousFace = ownedFace;
                                currentFace = otherFace;
                                nextEdgeFound = true;
                                nextEdge = edge;
                                break;
                            }
                        }
                        if (nextEdgeFound)
                        {
                            (vertex1, vertex2) = Edge.GetVertexIndices(nextEdge);
                            //For the first set of edges, check to make sure this list is going in the proper direction
                            intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLineSegment(direction,
                                distance, vertexLookup[vertex1], vertexLookup[vertex2]);
                            if (intersectVertex == null) return null;

                            var vector = intersectVertex.Position.subtract(loop.Last().Position, 3);
                            //Use the previous face, since that is the one that contains both of the edges that are in use.
                            var dot = direction.crossProduct(previousFace.Normal).dotProduct(vector, 3);
                            if (Math.Sign(dot) >= 0) correctDirection += dot;
                            else reverseDirection += (-dot);

                            //Add the edge as a reference for the vertex, so we can get the faces later
                            loop.Add(intersectVertex);
                            edgeLoop.Add(nextEdge);
                            unusedEdges.Remove(nextEdge);
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
            data.InputEdgeLoops = edgeLoops; //Set the input edge loops, in case we can use them again.
            return loops;
        }
        #endregion
    }
}