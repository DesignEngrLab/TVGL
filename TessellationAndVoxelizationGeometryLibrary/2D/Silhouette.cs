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
        public static List<List<Point>> Slow(TessellatedSolid ts, double[] normal)
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
        /// <param name="minAngle"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal, double minAngle = 0.1)
        {
            if (ts.Errors?.SingledSidedEdges != null)
            {
                //HighlyAccurate is slower, but more accurate and may handle missing edge/vertex pairing better.
                //Also, it handles low quality and small models better.
                return Slow(ts, normal);
            }
            ts.HasUniformColor = false;

            var depthOfPart = MinimumEnclosure.GetLengthAndExtremeVertices(normal, ts.Vertices, out _, out _);
            return Run(ts.Faces, normal, minAngle, ts.SameTolerance, depthOfPart);
        }

        public static List<List<Point>> AvoidingOverlappingSurfaces(IList<PolygonalFace> faces, double[] normal)
        {
            throw new NotImplementedException("Not fully finished");
            var positiveFaces = new HashSet<PolygonalFace>();
            var vertices = new HashSet<Vertex>();
            var firstTolerance = 0.0001;

            //Get all positive faces
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (dot.IsGreaterThanNonNegligible(firstTolerance))
                {
                    positiveFaces.Add(face);
                    face.Color = new Color(KnownColors.Blue);
                    foreach (var vertex in face.Vertices)
                    {
                        vertices.Add(vertex);
                    }
                }
            }

            //Project all the vertices into points
            //The vertex is saved as a reference in the point
            var transform = MiscFunctions.TransformToXYPlane(normal, out _);
            var projectedPoints = new Dictionary<int, Point>();
            foreach (var vertex in vertices)
            {
                projectedPoints.Add(vertex.IndexInList, MiscFunctions.Get2DProjectionPoint(vertex, transform));
            }

            //Build a dictionary of faces to polygons
            var projectedFacePolygons = positiveFaces.ToDictionary(f => f, f => GetPolygonFromFace(f, projectedPoints, true));

            //Order all the vertices along the silhouette direction
            MiscFunctions.SortAlongDirection(new[] { normal }, vertices, out List<Vertex> sortedVertices, out _);
            //Order all the vertices along the two perpendicular directions.
            var direction2 = new[] { normal[1] - normal[2], -normal[0], normal[0] }.normalize();
            MiscFunctions.SortAlongDirection(new[] { direction2 }, vertices, out List<Tuple<Vertex, double>> sortedVertices2, out _);
            var sortedVerticesAlongD2 = sortedVertices2.ToDictionary(vertex => vertex.Item1, vertex => vertex.Item2);
            var direction3 = normal.crossProduct(direction2).normalize();
            MiscFunctions.SortAlongDirection(new[] { direction3 }, vertices, out List<Tuple<Vertex, double>> sortedVertices3, out _);
            var sortedVerticesAlongD3 = sortedVertices3.ToDictionary(vertex => vertex.Item1, vertex => vertex.Item2);

            //Order all the positive faces along the search direction
            //To ensure the upper faces are processed first, the face is only
            //assigned a reference index once its third vertex is reached
            //Add a face into the queue if it is not 
            var referenceIndex = 0;
            var faceList1 = new HashSet<PolygonalFace>();
            var faceList2 = new HashSet<PolygonalFace>();
            var orderedFacesDict = new Dictionary<PolygonalFace, int>();
            var orderedFaces = new HashSet<PolygonalFace>();
            foreach (var vertex in sortedVertices)
            {
                foreach (var face in vertex.Faces)
                {
                    if (!positiveFaces.Contains(face)) continue;
                    if (!faceList1.Contains(face))
                    {
                        faceList1.Add(face);
                    }
                    else if (!faceList2.Contains(face))
                    {
                        faceList2.Add(face);
                    }
                    else if (!orderedFacesDict.Keys.Contains(face))
                    {
                        orderedFacesDict.Add(face, referenceIndex);
                        orderedFaces.Add(face);
                        referenceIndex++;
                    }
                }
            }

            //Define non-overlapping surfaces.
            //Start a new surface with a face and all its adjacent faces (they cannot overlap)
            //Add all the adjacent faces to a queue
            //Wrap around the ts using face adjacentcy and positiveFaces.Contains()
            //If an adjacent face (A) is not already in the surface,
            //Adjacent faces should be considered first if they have lower distances along the search direction
            //  Parallel => Test for TriangleTriangleIntersection with all non-adjacent positive faces in the surface
            //      Use sorted vertices to eliminate possible faces to a column.
            //      If any points are inside either face in question, taboo A for this surface
            //      If any of the edges overlap, taboo A for this surface (only need to test 2 edges, since we already did points) 
            //Continue wrapping until all surrounding faces are negative or taboo.
            //Repeat until all the positive faces are assigned surfaces
            //Project the border edges for all surfaces and union them together. (Optionally offset slightly before union)
            var facesAssignedToPriorSurfaces = new HashSet<PolygonalFace>();
            var surfaces = new List<HashSet<PolygonalFace>>();
            while (orderedFaces.Any())
            {
                var startFace = orderedFaces.First();
                var nonOverlappingSurface = new HashSet<PolygonalFace> {startFace};
                //Faces from prior surfaces need not be considered again.
                var consideredFaces = new HashSet<PolygonalFace> (facesAssignedToPriorSurfaces){ startFace};
                //Order the adjacent positive faces 
                //Note that these faces could overlap one another (possible, but unlikely), which is why they are ordered.
                //This will process the highest faces first.
                var queue = new Queue<PolygonalFace>(startFace.AdjacentFaces.Where(f => positiveFaces.Contains(f))
                    .OrderBy(f => orderedFacesDict[f]));
                while (queue.Any())
                {
                    var currentFace = queue.Dequeue();
                    if(consideredFaces.Contains(currentFace)) continue;

                    #region Get all the column faces and check if the adjacent face intersects with any of them
                    //Get all the faces in this column 
                    //To do this, first get all the vertices in the column by using the sorted dictionaries
                    //along the perpendicular directions;
                    var d2Min = double.MaxValue;
                    var d2Max = double.MinValue;
                    var d3Min = double.MaxValue;
                    var d3Max = double.MinValue;
                    foreach (var v in currentFace.Vertices)
                    {
                        var d2 = sortedVerticesAlongD2[v];
                        d2Min = Math.Min(d2Min, d2);
                        d2Max = Math.Max(d2Max, d2);
                        var d3 = sortedVerticesAlongD3[v];
                        d3Min = Math.Min(d3Min, d3);
                        d3Max = Math.Max(d3Max, d3);
                    }
                    var adjacentFaces = currentFace.AdjacentFaces;
                    var priorFacesInColumn = new List<PolygonalFace>();
                    foreach (var priorFace in nonOverlappingSurface)
                    {
                        //check if the prior face is adjacent to the face in question.
                        //If it is, then it cannot possible be affecting visibility
                        if (adjacentFaces.Contains(priorFace)) continue;

                        //Check if this face is completely to one side of the bounds.
                        //If the face is inside the bounds or crossing the bounds, all
                        //four of these booleans will be false.
                        var allBelowD3Min = true;
                        var allAboveD3Max = true;
                        var allBelowD2Min = true;
                        var allAboveD2Max = true;
                        foreach (var priorFaceVertex in priorFace.Vertices)
                        {
                            var d2 = sortedVerticesAlongD2[priorFaceVertex];
                            if (!d2.IsLessThanNonNegligible(d2Min)) allBelowD2Min = false;
                            else if (!d2.IsGreaterThanNonNegligible(d2Min)) allAboveD2Max = false;

                            var d3 = sortedVerticesAlongD2[priorFaceVertex];
                            if (!d3.IsLessThanNonNegligible(d3Min)) allBelowD3Min = false;
                            else if (!d3.IsGreaterThanNonNegligible(d3Min)) allAboveD3Max = false;
                        }
                        if (allAboveD2Max || allBelowD2Min || allAboveD3Max || allBelowD3Min) continue;

                        //Else,
                        //This face is crossing the boundary. Add it to faces in column
                        priorFacesInColumn.Add(priorFace);
                    }
                    #endregion

                    //The face is visible because there are no other faces within the column
                    //Since we removed adjacent faces, this is likely to occur.
                    var addToSurface = false;
                    if (!priorFacesInColumn.Any())
                    {
                        addToSurface = true;
                    }
                    //Else we need to check intersections to make a determination
                    //  If there is any intersection, then this triangle is overlapping with this surface.
                    //  Else, add it to the surface
                    else
                    {
                        var polygon = projectedFacePolygons[currentFace];
                        //ToDo: This loop could be parallel. With the ability to terminate if any intersection exists.
                        foreach (var priorFaceInColumn in priorFacesInColumn)
                        {
                            var priorFacePolygon = projectedFacePolygons[priorFaceInColumn];
                            if (MiscFunctions.TriangleTriangleIntersection(polygon, priorFacePolygon)) continue;
                            addToSurface = true;
                            break;
                        }      
                    }

                    if (addToSurface)
                    {
                        //Any time we add a face to the surface, update the queue
                        nonOverlappingSurface.Add(currentFace);
                        foreach (var adjacentFace in adjacentFaces)
                        {
                            //Don't add negative faces, or faces that have already been considered
                            if (!positiveFaces.Contains(adjacentFace)) continue;
                            if (consideredFaces.Contains(adjacentFace)) continue;
                            queue.Enqueue(adjacentFace);
                        }
                    }
                    //Either way, add the current face to the list of considered faces
                    consideredFaces.Add(currentFace);
                }

                //Remove all the faces from this surface
                //and add them to the assigned faces
                foreach (var face in nonOverlappingSurface)
                {
                    orderedFaces.Remove(face);
                    facesAssignedToPriorSurfaces.Add(face);
                }
                surfaces.Add(nonOverlappingSurface);
            }

            var colors = new List<Color>()
            {
                new Color(KnownColors.Blue),
                new Color(KnownColors.Red),
                new Color(KnownColors.Green),
                new Color(KnownColors.Yellow),
                new Color(KnownColors.Purple),
                new Color(KnownColors.Pink),
                new Color(KnownColors.Orange),
                new Color(KnownColors.Turquoise),
                new Color(KnownColors.White),
                new Color(KnownColors.Tan)
            };
            var i = 0;
            foreach (var surface in surfaces)
            {
                if (i == colors.Count) i = 0;
                var color = colors[i];
                i++;
                foreach (var face in surface)
                {
                    face.Color = color;
                }
            }

            //Get and union the polygons from each of the surfaces.
            //Get the surface paths from all the surfaces and union them together
            var solution = new List<List<Point>>();
            var loopCount = 0;
            foreach (var surface in surfaces)
            {
                var surfacePaths = GetSurfacePaths(surface, normal);
                var area = 0.0;
                var significantPaths = new List<List<Point>>();
                foreach (var path in surfacePaths)
                {
                    var simplePath = PolygonOperations.SimplifyFuzzy(path);
                    if (!simplePath.Any()) continue;  //Ignore very small patches
                    var pathArea = MiscFunctions.AreaOfPolygon(simplePath);
                    area += pathArea;
                    if (pathArea.IsNegligible(0.0000001)) continue;  //Ignore very small patches
                    significantPaths.Add(simplePath);
                }
                if (!significantPaths.Any()) continue;

                List<List<Point>> surfaceUnion;
                try
                {
                    surfaceUnion = PolygonOperations.Union(significantPaths, false, PolygonFillType.EvenOdd);
                }
                catch
                {
                    //Simplify likely reduced the polygon to nothing. It is insignificant, so continue.
                    continue;
                }
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

            //Remove tiny polygons and slivers 
            solution = PolygonOperations.SimplifyFuzzy(solution);
            return solution;
        }

        private static Polygon GetPolygonFromFace(PolygonalFace face, Dictionary<int, Point> projectedPoints, bool forceToBePositive)
        {
            if(face.Vertices.Count != 3) throw new Exception("This method was only developed with triangles in mind.");
            //Make sure the polygon is ordered correctly (we already know this face is positive)
            var points = face.Vertices.Select(v => projectedPoints[v.IndexInList]).ToList();
            var facePolygon = new Polygon(points);
            if (forceToBePositive && !facePolygon.IsPositive) facePolygon.Reverse();
            return facePolygon;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. Depth of part is only used if removing tiny polygons.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="normal"></param>
        /// <param name="minAngle"></param>
        /// <param name="minPathAreaToConsider"></param>
        /// <param name="depthOfPart"></param> 
        /// <returns></returns>
        public static List<List<Point>> Run(IList<PolygonalFace> faces, double[] normal, double minAngle = 0.1,
        double minPathAreaToConsider = 0.0, double depthOfPart = 0.0)
        {
            //Get the positive faces into a dictionary
            if (minAngle > 4.999) minAngle = 4.999; //min angle must be between 0 and 5 degrees. 0.1 degree has proven to be good.
            //Note also that the offset is based on the min angle.
            var angleTolerance = Math.Cos((90-minAngle)*Math.PI/180); //Angle of 89.9 Degrees from normal

            var allPositives = new Dictionary<int, PolygonalFace>();
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (dot.IsGreaterThanNonNegligible(angleTolerance))
                {
                    allPositives.Add(face.IndexInList, face);
                }
                //Any face not at least 89.9 degrees similar to the normal is ignored
            }

            //Get all the surfaces
            var allSurfaces = SeperateIntoSurfaces(allPositives);
            //var colors = new List<Color>()
            //{
            //    new Color(KnownColors.Blue),
            //    new Color(KnownColors.Red),
            //    new Color(KnownColors.Green),
            //    new Color(KnownColors.Yellow),
            //    new Color(KnownColors.Purple),
            //    new Color(KnownColors.Pink),
            //    new Color(KnownColors.Orange),
            //    new Color(KnownColors.Turquoise),
            //    new Color(KnownColors.White),
            //    new Color(KnownColors.Tan)
            //};
            //var i = 0;
            //foreach (var surface in allSurfaces)
            //{
            //    if (i == colors.Count) i = 0;
            //    var color = colors[i];
            //    i++;
            //    foreach (var face in surface)
            //    {
            //        face.Color = color;
            //    }
            //}

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
                    var simplePath = PolygonOperations.SimplifyFuzzy(path);
                    if (!simplePath.Any()) continue;  //Ignore very small patches
                    var pathArea = MiscFunctions.AreaOfPolygon(simplePath);
                    area += pathArea;
                    if (pathArea.IsNegligible(minPathAreaToConsider)) continue;  //Ignore very small patches
                    significantPaths.Add(simplePath);          
                }
                if (!significantPaths.Any()) continue;

                List<List<Point>> surfaceUnion;
                try
                {
                    //Use positive fill, since it handles overlapping surfaces correctly
                    surfaceUnion = PolygonOperations.Union(significantPaths, false, PolygonFillType.Positive);
                }
                catch
                {
                    //Simplify likely reduced the polygon to nothing. It is insignificant, so continue.
                    continue;
                }
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
                    //Use positive fill, since it handles overlapping surfaces correctly
                    var oldSolution = new List<List<Point>>(solution);
                    solution = PolygonOperations.Union(oldSolution, surfaceUnion, true, PolygonFillType.Positive);
                }
                loopCount++;
            }

            //Offset by enough to account for minimum angle 
            var scale = Math.Tan(minAngle * Math.PI / 180)*depthOfPart;

            //Remove tiny polygons and slivers 
            solution = PolygonOperations.SimplifyFuzzy(solution);
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
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!faces.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore, only push adjacent faces that are in the faces list.
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
