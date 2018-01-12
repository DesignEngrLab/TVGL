using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            //2. Remove it from the list if it has a insignificant area
            var projectedFaces = new List<List<Point>>();
            foreach (var face in negativeFaces)
            {
                var polygon = MiscFunctions.Get2DProjectionPoints(face.Vertices, normal, true).ToList();
                if (polygon.Count < 3) continue; //2 of the points must have been merged
                var area = MiscFunctions.AreaOfPolygon(polygon);
                if (area.IsNegligible(0.0000001)) continue; //Higher tolerance because of the conversion to intPoints in the Union function
                if (area < 0) polygon.Reverse();//Make this polygon positive CCW
                projectedFaces.Add(polygon);
            }


            //2. Union it with the prior negative faces.
            var startPolygon = projectedFaces[0];
            projectedFaces.RemoveAt(0);
            var polygonList = new List<List<Point>> { startPolygon };
            var previousArea = MiscFunctions.AreaOfPolygon(startPolygon);
            while (projectedFaces.Any())
            {
                var nextPolygon = projectedFaces[0];
                projectedFaces.RemoveAt(0);
                var oldPolygonList = new List<List<Point>>(polygonList);
                polygonList = PolygonOperations.Union(oldPolygonList, new List<List<Point>> {nextPolygon});

                //Check to make sure the area got larger
                var currentArea = polygonList.Sum(p => MiscFunctions.AreaOfPolygon(p));
                if (currentArea < previousArea*.99)
                {
                    throw new Exception("Adding a triangle should never decrease the area");
                    //oldPolygonList.Add(nextPolygon);
                    //return polygonList; 
                }
                previousArea = currentArea; //ToDo: Remove this check once it is working properly.
            }

            var smallestX = double.PositiveInfinity;
            var largestX = double.NegativeInfinity;
            foreach (var path in polygonList)
            {
                foreach (var point in path)
                {
                    if (point.X < smallestX)
                    {
                        smallestX = point.X;
                    }
                    if (point.X > largestX)
                    {
                        largestX = point.X;
                    }
                }
            }
            var scale = largestX - smallestX;

            //Remove tiny polygons and slivers 
            var offsetPolygons = PolygonOperations.OffsetMiter(polygonList, scale / 1000);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale / 1000);

            return significantSolution;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run3(TessellatedSolid ts, double[] normal, double tolerance = Constants.BaseTolerance)
        {
            //A tolerance of ts.SameTolerance * 100 or greater seems to be the best.

            if (ts.Errors?.SingledSidedEdges != null)
            {
                //Run2 is slower, but may handle missing edge/vertex pairing better.
                return Run2(ts, normal);
            }

            //Get the positive faces into a dictionary
            var positiveFaceDict = new Dictionary<int, PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                //face.Color = new Color(KnownColors.PaleGoldenrod);
                var dot = normal.dotProduct(face.Normal);
                if (dot.IsGreaterThanNonNegligible(tolerance))
                {
                    positiveFaceDict.Add(face.IndexInList, face);
                }
            }

            var unusedPositiveFaces = new Dictionary<int, PolygonalFace>(positiveFaceDict);
            var seperateSurfaces = new List<HashSet<PolygonalFace>>();

            ts.HasUniformColor = false;
            var colorList = new List<Color>
            {
                new Color(KnownColors.Blue),
                new Color(KnownColors.Red),
                new Color(KnownColors.Yellow),
                new Color(KnownColors.Green),
                new Color(KnownColors.Orange),
                new Color(KnownColors.Yellow),
                new Color(KnownColors.Purple),
                new Color(KnownColors.Brown),
                new Color(KnownColors.DarkTurquoise),
                new Color(KnownColors.AntiqueWhite),
                new Color(KnownColors.DarkOliveGreen),
                new Color(KnownColors.DarkGray),
                new Color(KnownColors.Gold)
            };
            var ic = 0;
            while (unusedPositiveFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedPositiveFaces.ElementAt(0).Value });
                
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (surface.Contains(face)) continue;
                    surface.Add(face);
                    face.Color = colorList[ic];
                    unusedPositiveFaces.Remove(face.IndexInList);
                    //Only push adjacent faces that are also negative
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!positiveFaceDict.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore if not negative
                        var dot = surface.Last().Normal.dotProduct(adjacentFace.Normal);
                        if (!dot.IsPracticallySame(1.0, Constants.SameFaceNormalDotTolerance)) continue; //ignore for now. It will be part of a different surface.
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface);
                ic++;
                if (ic == colorList.Count) ic = 0; //Go back to the beginning
            }

            //Get the surface positive and negative loops
            var solution = new List<List<Point>>();
            var loopCount = 0;
            foreach (var surface in seperateSurfaces)
            {
                var surfacePaths = GetSurfacePaths(surface, normal);
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
                if (!significantPaths.Any()) continue;

                var surfaceUnion = PolygonOperations.Union(significantPaths, true, PolygonFillType.EvenOdd);
                if (!surfaceUnion.Any()) continue;

                if (area < 0)
                {
                    area = surfaceUnion.Sum(path => MiscFunctions.AreaOfPolygon(path));
                    if (area < 0) throw new Exception("Area for each surface must be positive");
                }

                if (loopCount == 0)
                {
                    solution = new List<List<Point>>(surfaceUnion);
                }
                else
                {
                    var oldSolution = new List<List<Point>>(solution);
                    //try
                    //{
                        solution = PolygonOperations.Union(oldSolution, surfaceUnion);
                    //}
                    //catch
                    //{
                        //oldSolution.AddRange(surfaceUnion);
                        //solution = oldSolution;
                        //break;
                    //}

                }
                loopCount ++;
                
            }

            var smallestX = double.PositiveInfinity;
            var largestX = double.NegativeInfinity;
            foreach (var point in solution.SelectMany(path => path))
            {
                if (point.X < smallestX)
                {
                    smallestX = point.X;
                }
                if (point.X > largestX)
                {
                    largestX = point.X;
                }
            }
            var scale = largestX - smallestX;

            //Remove tiny polygons and slivers 
            var offsetPolygons = PolygonOperations.OffsetMiter(solution, scale / 1000);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale / 1000);

            return significantSolution;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <param name="minAngle"></param>
        /// <param name="removeTinyPolygons"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal, double minAngle = 1.0,
            bool removeTinyPolygons = true)
        {
            if (ts.Errors?.SingledSidedEdges != null)
            {
                //Run2 is slower, but may handle missing edge/vertex pairing better.
                //Also, it handles low quality and small models better.
                return Run2(ts, normal);
            }
            ts.HasUniformColor = false;

            List<Vertex> v1, v2;
            var depthOfPart = MinimumEnclosure.GetLengthAndExtremeVertices(normal, ts.Vertices, out v1, out v2);

            return Run(ts.Faces, normal, minAngle, ts.SurfaceArea/10000, removeTinyPolygons, depthOfPart);
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. Depth of part is only used if removing tiny polygons.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="normal"></param>
        /// <param name="minAngle"></param>
        /// <param name="minPathAreaToConsider"></param>
        /// <param name="removeTinyPolygons"></param>
        /// <param name="depthOfPart"></param> 
        /// <returns></returns>
        public static List<List<Point>> Run(IList<PolygonalFace> faces, double[] normal, double minAngle = 1.0,
            double minPathAreaToConsider = 0.0, bool removeTinyPolygons = true, double depthOfPart = 0.0)
        {
            //Get the positive faces into a dictionary
            var positiveFaceDict1 = new Dictionary<int, PolygonalFace>();
            var positiveFaceDict2 = new Dictionary<int, PolygonalFace>();
            var positiveFaceDict3 = new Dictionary<int, PolygonalFace>();
            var firstTolerance = Math.Cos(60 * Math.PI / 180); //Angle of 60 Degrees from normal
            var secondTolerance = Math.Cos(85* Math.PI / 180); //Angle of 85 Degrees from normal

            if (minAngle > 4.999) minAngle = 4.999; //min angle must be between 0 and 5 degrees. 1 degree has proven to be good.
            //Note also that the offset is based on the min angle.
            var thirdTolerance = Math.Cos((90-minAngle)*Math.PI/180); //Angle of 89.9 Degrees from normal
 
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (dot.IsGreaterThanNonNegligible(firstTolerance))
                {
                    positiveFaceDict1.Add(face.IndexInList, face);
                    face.Color = new Color(KnownColors.Blue);
                }
                else if (dot.IsGreaterThanNonNegligible(secondTolerance))
                {
                    positiveFaceDict2.Add(face.IndexInList, face);
                    face.Color = new Color(KnownColors.Green);
                }
                else if (dot.IsGreaterThanNonNegligible(thirdTolerance))
                {
                    positiveFaceDict3.Add(face.IndexInList, face);
                    face.Color = new Color(KnownColors.Red);
                }
                else if (dot.IsGreaterThanNonNegligible())
                {
                    face.Color = new Color(KnownColors.DarkRed);
                }
                //Any face not at least 89.9 degrees similar to the normal is ignored
            }

            //Get all the surfaces
            var allSurfaces = SeperateIntoSurfaces(positiveFaceDict1);
            allSurfaces.AddRange(SeperateIntoSurfaces(positiveFaceDict2));
            allSurfaces.AddRange(SeperateIntoSurfaces(positiveFaceDict3));

            //Get the surface paths from all the surfaces and union them together
            var solution = new List<List<Point>>();
            var loopCount = 0;
            foreach (var surface in allSurfaces)
            {
                var surfacePaths = GetSurfacePaths(surface, normal);
                var area = 0.0;
                var significantPaths = new List<List<Point>>();
                foreach (var path in surfacePaths)
                {
                    var pathArea = MiscFunctions.AreaOfPolygon(path);
                    area += pathArea;
                    if (!pathArea.IsNegligible(minPathAreaToConsider))
                    {
                        //Ignore very small patches
                        significantPaths.Add(path);
                    }
                }
                if (!significantPaths.Any()) continue;

                var surfaceUnion = PolygonOperations.Union(significantPaths, true, PolygonFillType.EvenOdd);
                if (!surfaceUnion.Any()) continue;

                if (area < 0)
                {
                    area = surfaceUnion.Sum(path => MiscFunctions.AreaOfPolygon(path));
                    if (area < 0) throw new Exception("Area for each surface must be positive");
                }

                if (loopCount == 0)
                {
                    solution = new List<List<Point>>(surfaceUnion);
                }
                else
                {
                    var oldSolution = new List<List<Point>>(solution);
                    solution = PolygonOperations.Union(oldSolution, surfaceUnion);
                }
                loopCount++;
            }

            if (!removeTinyPolygons) return solution;

            //Offset by enough to account for 0.1 degree limit. 
            var scale = Math.Tan(minAngle * Math.PI / 180)*depthOfPart;

            //Remove tiny polygons and slivers 
            var offsetPolygons = PolygonOperations.OffsetMiter(solution, scale);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale);
            return significantSolution;
        }

        #region GetSurfacePaths
        private static List<List<Point>> GetSurfacePaths(HashSet<PolygonalFace> surface, double[] normal)
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

            var surfacePaths = new List<List<Point>>();
            while (outerEdges.Any())
            {
                var isReversed = false;
                var startEdge = outerEdges.First();
                outerEdges.Remove(startEdge);
                var startVertex = startEdge.From;
                var vertex = startEdge.To;
                var loop = new List<Vertex> { vertex };
                var loopOrder = new List<int> { vertex.IndexInList };
                var edgeLoop = new List<Edge> { startEdge };
                var stallCount = 0;
                var previousLoopSize = 0;
                do
                {
                    if (!vertex.Edges.Any()) throw new Exception("error in model");
                    if (loop.Count - previousLoopSize == 0)
                    {
                        stallCount++;
                    }
                    previousLoopSize = loop.Count;
                    if (stallCount > 10) throw new Exception("Missing relationship between edge and vertex");
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
                    if (v2.crossProduct(v1).dotProduct(edge.OwnedFace.Normal) < 0) throw new Exception("Assumption is faulty");
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
                        if ((fromIndex + 1 == toIndex) || (toIndex == 0 && fromIndex == loop.Count - 1))
                        {
                            inCorrectOrder = true;
                            correctCount++;
                        }
                        else if (((fromIndex == toIndex + 1) || (toIndex == loop.Count && fromIndex == 0)) && !alreadyReversed && !inCorrectOrder)
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
                        if ((fromIndex == toIndex + 1) || (toIndex == loop.Count - 1 && fromIndex == 0))
                        {
                            inCorrectOrder = true;
                            correctCount++;
                        }
                        else if (((fromIndex == toIndex - 1) || (toIndex == 0 && fromIndex == loop.Count - 1)) && !alreadyReversed && !inCorrectOrder)
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
                surfacePaths.Add(MiscFunctions.Get2DProjectionPoints(loop, normal, false).ToList());
            }
            return surfacePaths;
        }
        #endregion

        #region SeperateIntoSurfaces
        private static List<HashSet<PolygonalFace>> SeperateIntoSurfaces(Dictionary<int, PolygonalFace> faces)
        {
            var seperateSurfaces = new List<HashSet<PolygonalFace>>();
            var unusedFaces = new Dictionary<int, PolygonalFace>(faces);
            while (unusedFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedFaces.ElementAt(0).Value });

                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (surface.Contains(face)) continue;
                    surface.Add(face);
                    unusedFaces.Remove(face.IndexInList);
                    //Only push adjacent faces that are also negative
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!faces.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore if not negative
                        var dot = surface.First().Normal.dotProduct(adjacentFace.Normal);
                        if (!dot.IsPracticallySame(1.0, Constants.SameFaceNormalDotTolerance)) continue; //ignore for now. It will be part of a different surface.
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface);
            }
            return seperateSurfaces;
        }
        #endregion
    }
}
