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
            if(stepSize <= minOffset*2) throw new Exception("step size must be at least 2x as large as the min offset");
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            List<Vertex> sortedVertices;
            List<int[]> duplicateRanges;
            MiscFunctions.SortAlongDirection(new[] {axis}, ts.Vertices.ToList(), out sortedVertices, out duplicateRanges );

            var edgeList = new List<Edge>();
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
                    if (convexHull2DDecompositon) area = ConvexHull2DArea(new List<Edge>(edgeList), cuttingPlane);
                    else area = CrossSectionalArea(new List<Edge>(edgeList), cuttingPlane, out outputEdgeLoops, inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
                    outputData.Add(new []{distance, area}); 

                    //If the difference is far enough, add another data point right before the current vertex
                    //Use the vertex loops provided from the first pass above
                    if (difference1 > 3*minOffset)
                    {
                        distance = distanceAlongAxis - minOffset; //X value (distance along axis) 
                        cuttingPlane = new Flat(distance, axis);
                        if (convexHull2DDecompositon) area = ConvexHull2DArea(new List<Edge>(edgeList), cuttingPlane);
                        else 
                        {
                            inputEdgeLoops = outputEdgeLoops;
                            area = CrossSectionalArea(new List<Edge>(edgeList), cuttingPlane, out outputEdgeLoops, inputEdgeLoops, ignoreNegativeSpace); //Y value (area)
                        }
                        outputData.Add(new []{distance, area}); 
                    }
                    
                    //Update the previous distance used to make a data point
                    previousDistanceAlongAxis = distanceAlongAxis;
                }
                foreach (var edge in vertex.Edges)
                {
                    //Every edge has only two vertices. So the first sorted vertex adds the edge to this list
                    //and the second removes it from the list.
                    if (edgeList.Contains(edge))
                    {
                        edgeList.Remove(edge);
                    }
                    else
                    {
                        edgeList.Add(edge);
                    }
                }
                //Update the previous distance of the vertex checked
                previousVertexDistance = distanceAlongAxis;
            }
            return outputData;
        }

        private static double CrossSectionalArea(IList<Edge> edgeList, Flat cuttingPlane, 
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
                while (edgeList.Any())
                {
                    var startEdge = edgeList[0];
                    var loop = new List<Vertex>();
                    var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, startEdge.To, startEdge.From);
                    loop.Add(intersectVertex);
                    var edgeLoop = new List<Edge>{startEdge};
                    edgeList.RemoveAt(0);
                    var startFace = startEdge.OwnedFace;
                    var currentFace = startFace;
                    var endFace = startEdge.OtherFace; 
                    var nextEdgeFound = false;
                    Edge nextEdge = null;
                    var correctDirection = 0;
                    var reverseDirection = 0;
                    do
                    {
                        var index = 0;
                        for (index = 0; index < edgeList.Count; index++)
                        {
                            var edge = edgeList[index];
                            if (edge.OtherFace == currentFace)
                            {
                                currentFace = edge.OwnedFace;
                                nextEdgeFound = true;
                                nextEdge = edge;
                                break;
                            }
                            if (edge.OwnedFace == currentFace)
                            {
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
                            var dot = cuttingPlane.Normal.crossProduct(currentFace.Normal).dotProduct(vector);
                            loop.Add(intersectVertex);
                            edgeLoop.Add(nextEdge);
                            edgeList.RemoveAt(index); //Note that removing at an index is FASTER than removing a object.
                            if (Math.Sign(dot) >= 0) correctDirection++;
                            else reverseDirection++;
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
                    //if (reverseDirection > 10 && correctDirection > 10) throw new Exception("Inconsistent Ordering");
                }
            }
            outputEdgeLoops = edgeLoops;

            //Now create a list of vertices from the edgeLoops
            //var loops = new List<List<Vertex>>();
            //foreach (var edgeLoop in edgeLoops)
            //{
            //    var loop = new List<Vertex>();
            //    foreach (var edge in edgeLoop)
            //    {
            //        var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, edge.To, edge.From);
            //        loop.Add(intersectVertex);
            //    }
            //    loops.Add(loop);
            //}

            //var totalArea = 0.0;
            var totalArea2 = 0.0;
            foreach (var loop in loops)
            {
                //ToDo: The loops must be ordered correctly!!!!!!!!!!!! Currently, they are not.
                //var points = MiscFunctions.Get2DProjectionPoints(loop, cuttingPlane.Normal, true);
                //The area function returns negative values for negative loops and positive values for positive loops
                //totalArea += MiscFunctions.AreaOfPolygon(points);
                //totalArea2 += MiscFunctions.AreaOf3DPolygon(loop, cuttingPlane.Normal);
            }
            //var totalAreaDifference = totalArea - totalArea2;

            //List<List<Vertex[]>> triangleFaceList;
            //var triangles = TriangulatePolygon.Run(loops, cuttingPlane.Normal, out triangleFaceList, ignoreNegativeSpace);
            ////You can determine +/- loops from a line sweep along a random direction.
            //var totalArea = 0.0;
            //var areaList = new List<double>();
            //foreach (var faceList in triangleFaceList)
            //{
            //    var area = 0.0;
            //    foreach (var triangle in faceList)
            //    {
            //        var edge1 = triangle[1].Position.subtract(triangle[0].Position);
            //        var edge2 = triangle[2].Position.subtract(triangle[0].Position);
            //        // the area of each triangle in the face is the area is half the magnitude of the cross product of two of the edges
            //        area += Math.Abs(edge1.crossProduct(edge2).dotProduct(cuttingPlane.Normal)) / 2;
            //    }
            //    areaList.Add(area);
            //    totalArea += area;
            //    //ToDo: need to figure out how to track which area belong to which edge loops
            //}

            return totalArea2;
        }

        private static double ConvexHull2DArea(IEnumerable<Edge> edgeList, Flat cuttingPlane)
        {
            //Don't bother with loops. Just get all the intercept vertices, project to 2d and run 2dConvexHull
            var vertices = edgeList.Select(edge => MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, edge.To, edge.From)).ToList();
            var points = MiscFunctions.Get2DProjectionPoints(vertices.ToArray(), cuttingPlane.Normal, true);
            var area = MinimumEnclosure.ConvexHull2DArea(MinimumEnclosure.ConvexHull2D(points));
            return area;
        }
    }
}
