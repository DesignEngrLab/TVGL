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
using System.Runtime.CompilerServices;
using StarMathLib;
using TVGL;

namespace TVGL
{
    /// <summary>
    ///     Outputs cross sectional area along a given axis
    /// </summary>
    public static class AreaDecomposition
    {
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
        public static List<double[]> Run(TessellatedSolid ts, double[] axis, double stepSize,
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
        
       /// <summary>
        /// The Decomposition Data Class used to store information from A Directional Decomposition
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
                            distance2 = (previousVertexDistance + currentVertexDistance/2);
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

        /// <summary>
        /// Returns the decomposition data found from each slice of the decomposition. This data is used in other methods.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="direction"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public static List<DecompositionData> UniformDirectionalDecomposition(TessellatedSolid ts, double[] direction,
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

        /// <summary>
        /// Gets the additive volume given a list of decomposition data
        /// </summary>
        /// <param name="decompData"></param>
        /// <param name="additiveAccuracy"></param>
        /// <param name="outputData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double AdditiveVolume(List<DecompositionData> decompData, double additiveAccuracy, out List<DecompositionData> outputData )
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
                if(areaPriorToOffset > areaAfterOffset) throw new Exception("Path is ordered incorrectly");
                if(!areaAfterOffset.IsPracticallySame(areaAfterSimplification, areaAfterOffset*.05)) throw new Exception("Simplify Fuzzy Alterned the Geometry more than 5% of the area");

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
                    additiveVolume += additiveAccuracy*area2;
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
                loops.AddRange(edgeLoops.Select(edgeLoop => edgeLoop.Select(edge =>
                    MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin,
                        edge.To, edge.From)).ToList()));
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
                    unusedEdges.Remove(startEdge);;
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

                    if (reverseDirection > 1 && correctDirection > 1) throw new Exception("Area Decomp Loop Finding needs additional work.");
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
    }
}