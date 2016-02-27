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
        /// <param name="minOffset"></param>
        /// <returns></returns>
        public static List<double[]> Run(TessellatedSolid ts, double[] axis, double stepSize, double minOffset = double.NaN)
        {
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
                    var area = CrossSectionalArea(new List<Edge>(edgeList), cuttingPlane); //Y value (area)
                    outputData.Add(new []{distance, area}); 

                    //If the difference is far enough, add another data point right before the current vertex
                    if (difference1 > 3*minOffset)
                    {
                        distance = distanceAlongAxis - minOffset; //X value (distance along axis) 
                        cuttingPlane = new Flat(distance, axis);
                        area = CrossSectionalArea(edgeList, cuttingPlane); //Y value (area)
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

        private static double CrossSectionalArea(IList<Edge> edgeList, Flat cuttingPlane)
        {
            var edgeLoops = new List<List<Edge>>();
            while (edgeList.Any())
            {
                var startEdge = edgeList[0];
                var edgeLoop = new List<Edge>{startEdge};
                edgeList.RemoveAt(0);
                var startFace = startEdge.OwnedFace;
                var currentFace = startFace;
                var endFace = startEdge.OtherFace; 
                var nextEdgeFound = false;
                Edge nextEdge = null;
                do
                {
                    foreach (var edge in edgeList)
                    {
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
                        edgeLoop.Add(nextEdge);
                        edgeList.Remove(nextEdge);
                    }
                    else throw new Exception("Loop did not complete");
                } while (currentFace != endFace);
                edgeLoops.Add(edgeLoop);
            }

            //Now create a list of vertices from the edgeLoops
            var loops = new List<List<Vertex>>();
            foreach (var edgeLoop in edgeLoops)
            {
                var loop = new  List<Vertex>();
                foreach (var edge in edgeLoop)
                {
                    var intersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(cuttingPlane.Normal, cuttingPlane.DistanceToOrigin, edge.To, edge.From);
                    loop.Add(intersectVertex);
                }
                loops.Add(loop);
            }
            var triangles = TriangulatePolygon.Run(loops, cuttingPlane.Normal);
            //ToDo: Could instead write a PolgygonArea function that does not need to triangulate.
            //ToDo: It would be faster. Just determine which loops are positive and negative. 
            //ToDo: Add the positive loop polygon areas and subtract the negative loop polygon areas. 
            //You can determine +/- loops from a line sweep along a random direction.
            var area = 0.0;
            foreach (var triangle in triangles)
            {
                //Reference: http://www.mathopenref.com/coordtrianglearea.html
                var a = triangle[0];
                var b = triangle[1];
                var c = triangle[2];
                area += Math.Abs(a.X*(b.Y-c.Y)+b.X*(c.Y-a.Y)+c.X*(a.Y-b.Y))/2;
            }
            return area;
        }

        private static double ConvexHull2DArea(List<Edge> edgeList, Flat cuttingPlane)
        {
            throw new NotImplementedException();
            //Don't bother with loops. Just get all the intercept vertices, project to 2d and run 2dConvexHull
            while (edgeList.Any())
            {
                
            }
            
        }
    }
}
