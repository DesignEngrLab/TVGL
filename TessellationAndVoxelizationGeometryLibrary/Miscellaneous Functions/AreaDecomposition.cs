using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// Outputs cross sectional area along a given axis
    /// </summary>
    public static class AreaDecomposition
    {
        /// <summary>
        /// Outputs cross sectional area along a given axis. Use a smallar step size for 
        /// densly triangluated parts. Min offset is used to ensure all edges are straddle 
        /// edges, and can be a very small value.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="axis"></param>
        /// <param name="stepSize"></param>
        /// <param name="individualFaceAreas"></param>
        /// <param name="minOffset"></param>
        /// <param name="ignoreNegativeSpace"></param>
        /// <param name="convexHull2DDecompositon"></param>
        /// <returns></returns>
        /// public static List<double[]> Run(TessellatedSolid ts, double[] axis, double stepSize, out List<List<double[]>> individualFaceAreas, 
        ///   double minOffset = double.NaN, bool ignoreNegativeSpace = false, bool convexHull2DDecompositon = false)
        public static List<double[]> Run(TessellatedSolid ts, double[] axis, double stepSize, 
            double minOffset = double.NaN, bool ignoreNegativeSpace = false, bool convexHull2DDecompositon = false)
        {
            //individualFaceAreas = new List<List<double[]>>(); //Plot changes for the area of each flat that makes up a slice. (e.g. 2 positive loop areas)
            var outputData = new List<double[]>();
            if (double.IsNaN(minOffset)) minOffset = Math.Sqrt(ts.SameTolerance);
            if (stepSize <= minOffset*2)
            {
                //"step size must be at least 2x as large as the min offset");
                //Change it rather that throwing an exception
                stepSize = minOffset*2 + ts.SameTolerance;
            }
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            List<Vertex> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] {axis}, ts.Vertices.ToList(), out sortedVertices, out duplicateRanges );

            var edgeListDictionary = new Dictionary<int, Edge>();
            var previousDistanceAlongAxis = axis.dotProduct(sortedVertices[0].Position); //This value can be negative
            var previousVertexDistance = previousDistanceAlongAxis;
            foreach (var vertex in sortedVertices)
            {
                var distanceAlongAxis = axis.dotProduct(vertex.Position); //This value can be negative
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
                    else area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out outputEdgeLoops, inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
                    outputData.Add(new []{distance, area}); 

                    //If the difference is far enough, add another data point right before the current vertex
                    //Use the vertex loops provided from the first pass above
                    if (difference2 > 3*minOffset)
                    {
                        var distance2 = distanceAlongAxis - minOffset; //X value (distance along axis) 
                        cuttingPlane = new Flat(distance2, axis);
                        if (convexHull2DDecompositon) area = ConvexHull2DArea(edgeListDictionary, cuttingPlane);
                        else 
                        {
                            inputEdgeLoops = outputEdgeLoops;
                            area = CrossSectionalArea(edgeListDictionary, cuttingPlane, out outputEdgeLoops, inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
                        }
                        outputData.Add(new []{distance2, area}); 
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

        private static double CrossSectionalArea(Dictionary<int, Edge> edgeListDictionary, Flat cuttingPlane, 
            out List<List<Edge>> outputEdgeLoops, List<List<Edge>> intputEdgeLoops, bool ignoreNegativeSpace = false)
        {
            var edgeLoops = new List<List<Edge>>();
            var loops = new List<List<Vertex>>();
            if (intputEdgeLoops.Any())
            {
                edgeLoops = intputEdgeLoops; //Note that edge loops should all be ordered correctly
                loops.AddRange(edgeLoops.Select(edgeLoop => edgeLoop.Select(edge => 
                    MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, edge.To, edge.From)).ToList()));
            }
            else
            {
                //Build an edge list that we can modify, without ruining the original
                var edgeList = edgeListDictionary.ToDictionary(element => element.Key, element => element.Value);
                while (edgeList.Any())
                {
                    var startEdge = edgeList.ElementAt(0).Value;
                    var loop = new List<Vertex>();
                    var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, startEdge.To, startEdge.From);
                    loop.Add(intersectVertex);
                    var edgeLoop = new List<Edge>{startEdge};
                    edgeList.Remove(startEdge.IndexInList);
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
                        foreach (var edge in edgeList.Values)
                        {
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
                            intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, nextEdge.To, nextEdge.From);
                            var vector = intersectVertex.Position.subtract(loop.Last().Position);
                            //Use the previous face, since that is the one that contains both of the edges that are in use.
                            var dot = cuttingPlane.Normal.crossProduct(previousFace.Normal).dotProduct(vector);
                            loop.Add(intersectVertex);
                            edgeLoop.Add(nextEdge);
                            edgeList.Remove(nextEdge.IndexInList); //Note that removing at an index is FASTER than removing a object.
                            if (Math.Sign(dot) >= 0) correctDirection ++;
                            else reverseDirection ++;
                        }   
                        else throw new Exception("Loop did not complete");
                    } while (currentFace != endFace);
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

            var totalArea = 0.0;
            foreach (var loop in loops)
            {
                //var points = MiscFunctions.Get2DProjectionPoints(loop, cuttingPlane.Normal, true);
                //The area function returns negative values for negative loops and positive values for positive loops
                totalArea += MiscFunctions.AreaOf3DPolygon(loop, cuttingPlane.Normal);
            }

            return totalArea;
        }

        private static double ConvexHull2DArea(Dictionary<int, Edge> edgeList, Flat cuttingPlane)
        {
            //Don't bother with loops. Just get all the intercept vertices, project to 2d and run 2dConvexHull
            var vertices = edgeList.Select(edge => MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, edge.Value.To, edge.Value.From)).ToList();
            var points = MiscFunctions.Get2DProjectionPoints(vertices.ToArray(), cuttingPlane.Normal, true);
            return MinimumEnclosure.ConvexHull2DArea(MinimumEnclosure.ConvexHull2DMinimal(points));
        }
    }
}
