using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        /// <summary>
        /// Gets the silhouette of a solid along a given normal.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run2(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces
            var negativeFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaces.Add(face);
                }
            }
            
            //For each negative face.
            //1. Project it onto a plane perpendicular to the given normal.
            //2. Union it with the prior negative faces.
            var startFace = negativeFaces[0];
            negativeFaces.RemoveAt(0);
            var startPolygon = MiscFunctions.Get2DProjectionPoints(startFace.Vertices, normal, false).ToList();
            //Make this polygon positive CCW
            startPolygon = PolygonOperations.CCWPositive(startPolygon);
            var polygonList = new List<List<Point>> { startPolygon };

            while (negativeFaces.Any())
            {
                var negativeFace = negativeFaces[0];
                negativeFaces.RemoveAt(0);
                var nextPolygon = MiscFunctions.Get2DProjectionPoints(negativeFace.Vertices, normal, false).ToList();

                //Make this polygon positive CCW
                nextPolygon = PolygonOperations.CCWPositive(nextPolygon);
                polygonList = PolygonOperations.Union(new List<List<Point>>(polygonList) { nextPolygon});
            }   
            var polygons = polygonList.Select(path => new Polygon(path)).ToList();

            //Get the minimum line length to use for the offset.
            var maxArea = polygons.Select(polygon => polygon.Area).Concat(new[] {double.NegativeInfinity}).Max();

            //Remove tiny polygons.
            var count = 0;
            for(var i =0; i < polygons.Count; i++)
            {
                if (!polygons[i].Area.IsNegligible(maxArea/10000)) continue;
                polygonList.RemoveAt(i-count);
                count ++;
            }

            #region Offset Testing 
            //var smallestX = double.PositiveInfinity;
            //var largestX = double.NegativeInfinity;
            //foreach (var path in polygonList)
            //{
            //    foreach (var point in path)
            //    {
            //        if (point.X < smallestX)
            //        {
            //            smallestX = point.X;
            //        }
            //        if (point.X > largestX)
            //        {
            //            largestX = point.X;
            //        }
            //    }
            //}
            //var scale = largestX - smallestX;

            //var offsetPolygons = PolygonOperations.OffsetRound(polygonList, scale / 10);
            //polygonList.AddRange(offsetPolygons);
            #endregion

            return polygonList;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces into a dictionary
            var negativeFaceDict = new Dictionary<int, PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaceDict.Add(face.IndexInList, face);
                }
            }

            var unusedNegativeFaces = new Dictionary<int, PolygonalFace>(negativeFaceDict);
            var seperateSurfaces = new List<HashSet<PolygonalFace>>();

            while (unusedNegativeFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedNegativeFaces.ElementAt(0).Value });
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (surface.Contains(face)) continue;
                    surface.Add(face);
                    unusedNegativeFaces.Remove(face.IndexInList);
                    //Only push adjacent faces that are also negative
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!negativeFaceDict.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore if not negative
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface);
            }

            //Get the purface positive and negative loops
            var allPaths = new List<List<Point>>();
            foreach (var surface in seperateSurfaces)
            {
                //Get the surface inner and outer edges
                var outerEdges = new HashSet<Edge>();
                var innerEdges = new HashSet<Edge>();
                foreach (var face in surface)
                {
                    if (face.Edges.Count != 3) throw new Exception();
                    foreach (var edge in face.Edges)
                    {
                        //if (innerEdges.Contains(edge)) continue;
                        if (!outerEdges.Contains(edge)) outerEdges.Add(edge);
                        else if (outerEdges.Contains(edge))
                        {
                            innerEdges.Add(edge);
                            outerEdges.Remove(edge);
                        }
                        else throw new Exception();
                    }

                }


                //The inner edges may form 0 to many negative (CW) loops
                var surfacePaths= new List<List<Point>>();
                while (outerEdges.Any())
                {
                    var isReversed = false;
                    var startEdge = outerEdges.First();
                    outerEdges.Remove(startEdge);
                    var startVertex = startEdge.From;
                    var vertex = startEdge.To;
                    var loop = new List<Vertex> { vertex };
                    var dot = 0.0;
                    var previousEdge = startEdge;
                    do
                    {
                        if(!vertex.Edges.Any()) throw new Exception("error in model");
                        foreach (var edge2 in vertex.Edges.Where(edge2 => outerEdges.Contains(edge2)))
                        {
                            if (edge2.From == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.To;
                                loop.Add(vertex);
                                //If the edge2.From vertex is the one that is shared, the previous edge comes first.
                                dot += previousEdge.Vector.crossProduct(edge2.Vector).dotProduct(normal);
                                break;
                            }
                            else if (edge2.To == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.From;
                                loop.Add(vertex);
                                //If the edge2.To vertex is the one that is shared, edge2 comes first.
                                dot += edge2.Vector.crossProduct(previousEdge.Vector).dotProduct(normal);
                                previousEdge = edge2;
                                break;
                            }
                            if (edge2 == vertex.Edges.Last() && !isReversed)
                            {
                                //Swap the vertices were interested in and
                                //Reverse the loop
                                var tempVertex = startVertex;
                                startVertex = vertex;
                                vertex = tempVertex;
                                loop.Reverse();
                                loop.Add(vertex);
                                previousEdge = startEdge;
                                isReversed = true;
                            }
                            else if (edge2 == vertex.Edges.Last() && isReversed)
                            {
                                //Artifically close the loop.
                                vertex = startVertex;
                            }
                        }
                    } while (vertex != startVertex && outerEdges.Any());
                    if (dot.IsNegligible())
                    {
                        continue; //Ignore this loop for now.
                        throw new Exception(
                            "Failed to assign CCW positive ordering. Should not occur, unless poolygon is invalid.");
                    }
                    if (Math.Sign(dot) > 0)
                    {
                        surfacePaths.Add(
                            PolygonOperations.CCWPositive(MiscFunctions.Get2DProjectionPoints(loop, normal)).ToList());
                    } 
                    else
                    {
                        surfacePaths.Add(
                            PolygonOperations.CWNegative(MiscFunctions.Get2DProjectionPoints(loop, normal)).ToList());
                    }
                    
                    

                }
                //Union at the surface level to correctly capture holes
                allPaths.AddRange(PolygonOperations.Union(surfacePaths));
                
            }

            //Union all the paths
            var solution = PolygonOperations.Union(allPaths);
            return solution;
        }
    }
}
