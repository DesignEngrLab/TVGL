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
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal)
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
            var startPolygon = MiscFunctions.Get2DProjectionPoints(startFace.Vertices, normal, true).ToList();
            //Make this polygon positive CCW
            startPolygon = PolygonOperations.CCWPositive(startPolygon);
            var polygonList = new List<List<Point>> { startPolygon };

            while (negativeFaces.Any())
            {
                var negativeFace = negativeFaces[0];
                negativeFaces.RemoveAt(0);
                var nextPolygon = MiscFunctions.Get2DProjectionPoints(negativeFace.Vertices, normal, true).ToList();
                var area = MiscFunctions.AreaOfPolygon(nextPolygon);
                if (area.IsNegligible(0.0000001)) continue; //Higher tolerance because of the conversion to intPoints in the Union function
                //Make this polygon positive CCW
                if (area < 0) nextPolygon.Reverse();
                if (negativeFaces.Count == 22) negativeFaces = negativeFaces; 
                var oldPolygonList = new List<List<Point>>(polygonList);
                try
                {
                    polygonList = PolygonOperations.Union(oldPolygonList, new List<List<Point>> {nextPolygon});
                }
                catch
                {
                    oldPolygonList.Add(nextPolygon);
                    return oldPolygonList;
                }
            }
            try
            {
                polygonList = PolygonOperations.Union(polygonList);
            }
            catch
            {
                return polygonList;
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
        public static List<List<Point>> Run2(TessellatedSolid ts, double[] normal)
        {
            if (ts.Errors?.SingledSidedEdges != null)
            {
                //Run2 is slower, but may handle missing edge/vertex pairing better.
                return Run2(ts, normal);
            }

            //Get the positive faces into a dictionary
            var positiveFaceDict = new Dictionary<int, PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) > 0)
                {
                    positiveFaceDict.Add(face.IndexInList, face);
                }
            }

            var unusedPositiveFaces = new Dictionary<int, PolygonalFace>(positiveFaceDict);
            var seperateSurfaces = new List<HashSet<PolygonalFace>>();

            while (unusedPositiveFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedPositiveFaces.ElementAt(0).Value });
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (surface.Contains(face)) continue;
                    surface.Add(face);
                    unusedPositiveFaces.Remove(face.IndexInList);
                    //Only push adjacent faces that are also negative
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!positiveFaceDict.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore if not negative
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface);
            }

            //Get the surface positive and negative loops
            var solution = new List<List<Point>>();
            var loopCount = 0;
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
         
                var surfacePaths= new List<List<Point>>();               
                while (outerEdges.Any())
                {
                    var isReversed = false;
                    var startEdge = outerEdges.First();
                    outerEdges.Remove(startEdge);
                    var startVertex = startEdge.From;
                    var vertex = startEdge.To;
                    var loop = new List<Vertex> { vertex };
                    var loopOrder = new List<int> { vertex.IndexInList};
                    var edgeLoop = new List<Edge> {startEdge};
                    var stallCount = 0;
                    var previousLoopSize = 0;
                    do
                    {
                        if(!vertex.Edges.Any()) throw new Exception("error in model");
                        if (loop.Count - previousLoopSize == 0)
                        {
                            stallCount++;
                        }
                        previousLoopSize = loop.Count;
                        if(stallCount > 10) throw new Exception("Missing relationship between edge and vertex");
                        foreach (var edge2 in vertex.Edges.Where(edge2 => outerEdges.Contains(edge2)))
                        {
                            if (edge2.From == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.To;
                                loop.Add(vertex);
                                loopOrder.Add(vertex.IndexInList);
                                edgeLoop.Add(edge2);
                                break;
                            }
                            else if (edge2.To == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.From;
                                loop.Add(vertex);
                                loopOrder.Add(vertex.IndexInList);
                                edgeLoop.Add(edge2);
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
                                loop.Reverse();
                                loopOrder.Add(vertex.IndexInList);
                                edgeLoop.Reverse();
                                isReversed = true;
                            }
                            else if (edge2 == vertex.Edges.Last() && isReversed)
                            {
                                //Artifically close the loop.
                                vertex = startVertex;
                            }
                        }
                    } while (vertex != startVertex && outerEdges.Any());

                    //To determine order, make sure the loop is ordered in the correct orientation
                    //This is based on the assumption that the edge direction is CCW with its owned face.
                    var alreadyReversed = false;
                    var inCorrectOrder = false;
                    var errorCount = 0;
                    var correctCount = 0;
                    foreach (var edge in edgeLoop)
                    {
                        //Check assumption
                        var vertex3 = edge.OwnedFace.OtherVertex(edge);
                        var v1 = vertex3.Position.subtract(edge.To.Position);
                        var v2 = edge.Vector;
                        if(v2.crossProduct(v1).dotProduct(edge.OwnedFace.Normal) < 0) throw new Exception("Assumption is faulty");
                        if (edge.OwnedFace.Normal.dotProduct(normal) > 0.0)
                        {
                            //Owned face is visible from positive side. 
                            //edge.From must be right before Edge.To in list of vertices.
                            var fromIndex = 0;
                            for (var i = 0; i < loop.Count; i++)
                            {
                                if (edge.From.IndexInList == loopOrder[i])
                                {
                                    fromIndex = i;
                                    break;
                                }
                            }
                            var toIndex = 0;
                            for (var i = 0; i < loop.Count; i++)
                            {
                                if (edge.To.IndexInList == loopOrder[i])
                                {
                                    toIndex = i;
                                    break;
                                }
                            }
                            if ((fromIndex + 1 == toIndex) || (toIndex == 0 && fromIndex == loop.Count-1))
                            {
                                inCorrectOrder = true;
                                correctCount ++;
                            }
                            else if(((fromIndex == toIndex + 1) || (toIndex == loop.Count && fromIndex == 0)) && !alreadyReversed && !inCorrectOrder)
                            {
                                loop.Reverse();
                                loopOrder.Reverse();
                                alreadyReversed = true;
                                inCorrectOrder = true;
                                correctCount++;
                            }
                            else errorCount++;
                        }
                        else
                        {
                            //Owned face is not visible from negative side. 
                            //edge.To must be right before Edge.From in list of vertices.
                            var toIndex = 0;
                            for (var i = 0; i < loop.Count; i++)
                            {
                                if (edge.To.IndexInList == loopOrder[i])
                                {
                                    toIndex = i;
                                    break;
                                }
                            }
                            var fromIndex = 0;
                            for (var i = 0; i < loop.Count; i++)
                            {
                                if (edge.From.IndexInList == loopOrder[i])
                                {
                                    fromIndex = i;
                                    break;
                                }
                            }
                            if ((fromIndex == toIndex + 1 ) || (toIndex == loop.Count-1 && fromIndex == 0))
                            {
                                inCorrectOrder = true;
                                correctCount++;
                            }
                            else if (((fromIndex == toIndex - 1) || (toIndex == 0 && fromIndex == loop.Count-1)) && !alreadyReversed && !inCorrectOrder)
                            {
                                loop.Reverse();
                                loopOrder.Reverse();
                                alreadyReversed = true;
                                inCorrectOrder = true;
                                correctCount++;
                            }
                            else errorCount++;
                        }
                    }
                    if (errorCount > correctCount) loop.Reverse();
                    //if(alreadyReversed) loop.Reverse(); //Should have been other direction. Internal reverse was just to complete loop

                    surfacePaths.Add(MiscFunctions.Get2DProjectionPoints(loop, normal, false).ToList());
                }
                var area = 0.0;
                var significantPaths = new List<List<Point>>();
                foreach (var path in surfacePaths)
                {
                    var pathArea = MiscFunctions.AreaOfPolygon(path);
                    area += pathArea;
                    if (!pathArea.IsNegligible(ts.SurfaceArea/10000))
                    {
                        //Ignore very small patches
                        significantPaths.Add(path);
                    }
                }
                if(area  < 0) throw new Exception("Area for each surface must be positive");
                if (!significantPaths.Any()) continue;
                //var simplifiedPaths = new List<List<Point>>();
                //foreach (var significantPath in significantPaths)
                //{
                //    simplifiedPaths.AddRange(PolygonOperations.SimplifyForSilhouette(significantPath));
                //}

                //Union at the surface level to correctly capture holes
                List<List<Point>> surfaceUnion;
                try
                {
                    surfaceUnion = PolygonOperations.Union(significantPaths);
                }
                catch
                {
                    solution = significantPaths;
                    break;
                }
                if (!surfaceUnion.Any()) continue;
                if (loopCount == 0)
                {
                    solution = new List<List<Point>>(surfaceUnion);
                }
                else
                {
                    var oldSolution = new List<List<Point>>(solution);
                    try
                    {
                        solution = PolygonOperations.Union(oldSolution, surfaceUnion);
                    }
                    catch
                    {
                        oldSolution.AddRange(surfaceUnion);
                        solution = oldSolution;
                        break;
                    }

                }
                loopCount ++;
                
            }

            //Remove tiny polygons.
            var polygons = solution.Select(path => new Polygon(path)).ToList();

            //Remove tiny polygons.
            var count = 0;
            for (var i = 0; i < polygons.Count; i++)
            {
                if (!polygons[i].Area.IsNegligible(ts.SurfaceArea / 10000)) continue;
                solution.RemoveAt(i - count);
                count++;
            }

            return solution;
        }
    }
}
