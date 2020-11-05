using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OxyPlot.Axes;
using StarMathLib;

namespace OldTVGL
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        /// <summary>
        /// Gets the silhouette of a solid along a given normal.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="normal"></param>
        /// <param name="minAngle"></param>
        /// <param name="minPathAreaToConsider"></param>
        /// <param name="depthOfPart"></param>
        /// <returns></returns>
        public static List<List<PointLight>> Slow(IList<PolygonalFace> faces, double[] normal, double minAngle = 0.1,
            double minPathAreaToConsider = 0.0, double depthOfPart = 0.0)
        {
            var angleTolerance = Math.Cos((90 - minAngle) * Math.PI / 180);

            //Get the positive faces (defined as face normal along same direction as the silhoutte normal).
            var positiveFaces = new HashSet<PolygonalFace>();
            var vertices = new HashSet<Vertex>();
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal, 3);
                if (dot.IsGreaterThanNonNegligible(angleTolerance))
                {
                    positiveFaces.Add(face);
                    //face.Color = new Color(KnownColors.Blue);
                    foreach (var vertex in face.Vertices)
                    {
                        vertices.Add(vertex);
                    }
                }
            }

            //Project all the vertices into points
            //The vertex is saved as a reference in the point
            var transform = MiscFunctions.TransformToXYPlane(normal, out _);
            var projectedPoints = new Dictionary<int, PointLight>();
            foreach (var vertex in vertices)
            {
                projectedPoints.Add(vertex.IndexInList, MiscFunctions.Get2DProjectionPointAsLight(vertex, transform));
            }

            //Build a dictionary of faces to polygons
            //var projectedFacePolygons = positiveFaces.ToDictionary(f => f, f => GetPolygonFromFace(f, projectedPoints, true));
            //Use GetPolygonFromFace and force to be positive faces with true"
            var projectedFacePolygons2 = positiveFaces.Select(f => GetPolygonFromFace(f, projectedPoints, true)).ToList().Where(p => p.Area > minPathAreaToConsider).ToList();
            var solution = PolygonOperations.Union(projectedFacePolygons2, out _, false).Select(p => p.Path).ToList();

            //Offset by enough to account for minimum angle 
            var scale = Math.Tan(minAngle * Math.PI / 180) * depthOfPart;

            //Remove tiny polygons and slivers 
            solution = PolygonOperations.SimplifyFuzzy(solution);
            var offsetPolygons = PolygonOperations.OffsetMiter(solution, scale);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale);
            //Presenter.ShowAndHang(significantSolution);
            return significantSolution; //.Select(p => p.Path).ToList();
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <param name="minAngle"></param>
        /// <returns></returns>
        public static List<List<PointLight>> Run(TessellatedSolid ts, double[] normal, double minAngle = 0.1)
        {
            var depthOfPart = MinimumEnclosure.GetLengthAndExtremeVertices(normal, ts.Vertices, out _, out _);
            return Run(ts.Faces, normal, ts, minAngle, ts.SameTolerance, depthOfPart);
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. Depth of part is only used if removing tiny polygons.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="normal"></param>
        /// <param name="originalSolid"></param>
        /// <param name="minAngle"></param>
        /// <param name="minPathAreaToConsider"></param>
        /// <param name="depthOfPart"></param> 
        /// <returns></returns>
        public static List<List<PointLight>> Run(IList<PolygonalFace> faces, double[] normal, TessellatedSolid originalSolid, double minAngle = 0.1,
        double minPathAreaToConsider = 0.0, double depthOfPart = 0.0)
        {
            //Get the positive faces into a dictionary
            if (minAngle > 4.999) minAngle = 4.999; //min angle must be between 0 and 5 degrees. 0.1 degree has proven to be good.
            //Note also that the offset is based on the min angle.
            var angleTolerance = Math.Cos((90 - minAngle) * Math.PI / 180); //Angle of 89.9 Degrees from normal
            var angleTolerance2 = Math.Cos((90 - 5) * Math.PI / 180); //Angle of 85 Degrees from normal

            var positiveFaces = new HashSet<PolygonalFace>();
            var smallFaces = new List<PolygonalFace>();
            var allPositives = new Dictionary<int, PolygonalFace>();
            var allVertices = new HashSet<Vertex>();
            var positiveEdgeFaces = new HashSet<PolygonalFace>();
            foreach (var face in faces)
            {
                if (face.Area.IsNegligible()) continue;
                var dot = normal.dotProduct(face.Normal, 3);               
                if (dot.IsGreaterThanNonNegligible(angleTolerance2))
                {
                    allPositives.Add(face.IndexInList, face);
                    positiveFaces.Add(face);
                }
                else if (dot.IsGreaterThanNonNegligible(angleTolerance))
                {
                    //allPositives.Add(face.IndexInList, face);
                    positiveEdgeFaces.Add(face);
                }
                else if (Math.Sign(dot) > 0 && face.Area < 1.0)
                {
                    smallFaces.Add(face);
                }
                foreach (var vertex in face.Vertices)
                {
                    allVertices.Add(vertex);
                }
            }
            //Add any small sliver faces that are sandwinched between two positive faces.
            foreach (var smallFace in smallFaces)
            {
                var largerEdges = smallFace.Edges.OrderBy(e => e.Length).Take(2).ToList();
                var addToPositives = true;
                foreach (var edge in largerEdges)
                {
                    if (edge.OwnedFace == smallFace && allPositives.ContainsKey(edge.OtherFace.IndexInList))
                    {
                    }
                    else if (edge.OtherFace == smallFace && allPositives.ContainsKey(edge.OwnedFace.IndexInList))
                    {
                    }
                    else
                    {
                        addToPositives = false;
                    }
                }
                if (addToPositives)
                {
                    //allPositives.Add(smallFace.IndexInList, smallFace);
                    positiveEdgeFaces.Add(smallFace);
                }
            }

            //Get the polygons of all the positive faces. Force the polygons to be positive CCW
            var vertices = new HashSet<Vertex>();
            foreach (var face in allPositives.Values)
            {
                foreach (var vertex in face.Vertices)
                {
                    vertices.Add(vertex);
                }
            }
            var transform = MiscFunctions.TransformToXYPlane(normal, out _);
            var projectedPoints = new Dictionary<int, PointLight>();
            foreach (var vertex in vertices)
            {
                projectedPoints.Add(vertex.IndexInList, MiscFunctions.Get2DProjectionPointAsLight(vertex, transform));
            }
            var projectedFacePolygons = positiveFaces.ToDictionary(f => f.IndexInList, f => GetPathFromFace(f, projectedPoints, true));

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
            //originalSolid.HasUniformColor = false;
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
            //Presenter.ShowAndHang(originalSolid);

            //Get the surface paths from all the surfaces and union them together
            var solution = GetSurfacePaths(allSurfaces, normal, minPathAreaToConsider, originalSolid, projectedFacePolygons).ToList();

            var positiveEdgeFacePolygons = new List<List<PointLight>>();
            foreach(var face in positiveEdgeFaces)
            {
                var polygon = new PolygonLight(MiscFunctions.Get2DProjectionPointsAsLight(face.Vertices, normal));
                if (!polygon.IsPositive) polygon.Path.Reverse();
                positiveEdgeFacePolygons.Add(polygon.Path);
            }
          
            try //Try to merge them all at once
            {
                solution = PolygonOperations.Union(solution, positiveEdgeFacePolygons,out _, false, PolygonFillType.NonZero);
            }         
            catch
            {
                //Do them one at a time, skipping those that fail
                foreach (var face in positiveEdgeFacePolygons)
                {
                    try
                    {
                        solution = PolygonOperations.Union(solution, face, out _, false, PolygonFillType.NonZero);
                    }
                    catch 
                    {
                        continue;
                    }
                }
            }
          

            //Offset by enough to account for minimum angle 
            var scale = Math.Tan(minAngle * Math.PI / 180) * depthOfPart;

            //Remove tiny polygons and slivers 
            //First, Offset out and then perform a quick check for overhang polygons.
            //This is helpful when the polygon is nearly self-intersecting. 
            //Then offset back out.  

            solution = PolygonOperations.SimplifyFuzzy(solution, Math.Min(scale / 1000, Constants.LineLengthMinimum),
                Math.Min(angleTolerance / 1000, Constants.LineSlopeTolerance));
            var offsetPolygons = PolygonOperations.OffsetMiter(solution, scale);
            offsetPolygons = EliminateOverhangPolygons(offsetPolygons, projectedFacePolygons);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale);

            return significantSolution;
        }

        #region Eliminate Overhangs
        private static List<List<PointLight>> EliminateOverhangPolygons(List<List<PointLight>> nonSelfIntersectingPaths,
                    Dictionary<int, List<PointLight>> projectedFacePolygons)
        {
            var correctedSurfacePath = new List<List<PointLight>>();
            var negativePaths = new List<List<PointLight>>();
            foreach (var path in nonSelfIntersectingPaths)
            {
                if (path.Count < 3) continue; //Don't include lines. It must be a valid polygon.
                //If the area is negative, we need to check if it is a hole or an overhang
                //If it is an overhang, we ignore it. An overhang exists if any the points
                //in the path are inside any of the positive faces touching the path
                var area2D = MiscFunctions.AreaOfPolygon(path);
                if (Math.Sign(area2D) > 0)
                {
                    correctedSurfacePath.Add(path);
                }
                else
                {
                    negativePaths.Add(path);
                }
            }

            foreach (var path in negativePaths)
            {
                var isHoleCounter1 = 0;
                var isOverhangCounter1 = 0;
                //Get all the adjacent faces on this surface 
                //We need to check the face centers with the surface path.
                //If any adjacent face centers are inside the surface path, then it is an overhang
                //Note: regardless of whether the face is further than the loop in question or 
                //before, being inside the loop is enough to say that it is not a hole.
                if (path.Count > 3)
                {
                    //Presenter.ShowAndHang(path);
                    //Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { loops },
                    //    new List<TessellatedSolid> { originalSolid });
                }

                var polygons = new HashSet<List<PointLight>>(projectedFacePolygons.Values);

                //Get a few points that are inside the polygon (It is non-self intersecting,
                //but taking the center may not work.)
                var pathCenterX = path.Average(v => v.X);
                var pathCenterY = path.Average(v => v.Y);
                var centerPoint = new PointLight(pathCenterX, pathCenterY);
                var centerPointIsValid = false;
                if (MiscFunctions.IsPointInsidePolygon(path, centerPoint, false))
                {
                    //A negative polygon may be inside of a positive polygon without issue, however, 
                    //If there is a negative polygon inside a negative polygon an issue may arise.
                    //So we need to check to make sure this point is not inside any of the other negative polgyons.
                    if (negativePaths.Count == 1) centerPointIsValid = true;
                    else
                    {
                        centerPointIsValid = negativePaths.Where(otherPath => otherPath != path).Any(otherPath =>
                            MiscFunctions.IsPointInsidePolygon(path, centerPoint, true));
                    }
                }

                if (centerPointIsValid)
                {
                    //Great! We have an easy point that is inside. Check if it is inside any surface polygon
                }
                else
                {
                    //Get a point that is inside the polygon. We could set up a sweep line to do this,
                    //but for now I'm taking an easier approach (take average of three random points)
                    //Use the overload of the Random constructor which accepts a seed value, so that this is repeatable
                    var rnd = new Random(0);
                    var count = 0;
                    while (!centerPointIsValid && count < 1000)
                    {
                        var r1 = rnd.Next(path.Count);
                        var r2 = rnd.Next(path.Count);
                        var r3 = rnd.Next(path.Count);
                        while (r1 == r2)
                        {
                            //Get a new r2
                            r2 = rnd.Next(path.Count);
                        }
                        while (r3 == r2 || r3 == r1)
                        {
                            //Get a new r3
                            r3 = rnd.Next(path.Count);
                        }
                        var p1 = path[r1];
                        var p2 = path[r2];
                        var p3 = path[r3];
                        var centerX = (p1.X + p2.X + p3.X) / 3;
                        var centerY = (p1.Y + p2.Y + p3.Y) / 3;
                        var newCenter = new PointLight(centerX, centerY);
                        if (MiscFunctions.IsPointInsidePolygon(path, newCenter, false))
                        {
                            centerPoint = newCenter;

                            //A negative polygon may be inside of a positive polygon without issue, however, 
                            //If there is a negative polygon inside a negative polygon an issue may arise.
                            //So we need to check to make sure this point is not inside any of the other negative polgyons.
                            if (negativePaths.Count == 1) centerPointIsValid = true;
                            else
                            {
                                centerPointIsValid = negativePaths.Where(otherPath => otherPath != path).Any(otherPath =>
                                MiscFunctions.IsPointInsidePolygon(path, newCenter, true));
                            }
                        }
                        count++;
                    }
                    if (count == 1000)
                    {
                        //Presenter.ShowAndHang(negativePaths);
                        throw new Exception("Not able to find a point inside polygon");
                    }
                }

                if (polygons.Any(p => MiscFunctions.IsPointInsidePolygon(p, centerPoint, true)))
                {
                    //This is an overhang
                    //path.Reverse();
                    //correctedSurfacePath.Add(path);
                }
                else
                {
                    //This is a hole
                    correctedSurfacePath.Add(path);
                }
            }
            return correctedSurfacePath;
        }
        #endregion

        #region GetSurfacePaths
        private static IEnumerable<List<PointLight>> GetSurfacePaths(List<HashSet<PolygonalFace>> surfaces, double[] normal,
            double minAreaToConsider, TessellatedSolid originalSolid, Dictionary<int, List<PointLight>> projectedFacePolygons)
        {
            originalSolid.HasUniformColor = false;

            var red = new Color(KnownColors.Red);
            var allPaths = new List<List<PointLight>>();
            foreach (var surface in surfaces)
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

                var surfacePaths = new List<List<PointLight>>();
                var assignedEdges = new HashSet<Edge>();
                while (outerEdges.Any())
                {
                    //Get the start vertex and edge and save them to the lists
                    var startEdge = outerEdges.First();
                    var startVertex = startEdge.From;
                    var loop = new List<Vertex> { startVertex };

                    var nextVertex = startEdge.To;
                    var edgeLoop = new List<(Edge, Vertex, Vertex)> { (startEdge, startVertex, nextVertex) };
                    assignedEdges.Add(startEdge);

                    //Initialize the current vertex and edge
                    var vertex = startEdge.From;
                    var currentEdge = startEdge;

                    //Loop until back to the start vertex
                    while (nextVertex.IndexInList != startVertex.IndexInList)
                    {
                        //Get the next edge
                        var nextEdges = nextVertex.Edges.Where(e => !assignedEdges.Contains(e)).Where(e => outerEdges.Contains(e))
                            .ToList();
                        if (nextEdges.Count == 0)
                        {
                            Debug.WriteLine("Surface paths do not wrap around properly. Artificially closing loop.");
                            break;
                        }
                        if (nextEdges.Count > 1)
                        {
                            //There are multiple edges to go to next. Simply reversing will cause an issue
                            //if the same thing happens along the other direction.
                            //To avoid this, we go in the direction of the current edge's surface face, until we
                            //hit an edge.
                            var minAngle = 2 * Math.PI;
                            var currentFace = surface.Contains(currentEdge.OtherFace) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                            //currentFace.Color = new Color(KnownColors.White);
                            var otherVertex = currentFace.OtherVertex(currentEdge.To, currentEdge.From);
                            var angle1 = MiscFunctions.ProjectedExteriorAngleBetweenVerticesCCW(vertex, nextVertex, otherVertex, normal);
                            var angle2 = MiscFunctions.ProjectedInteriorAngleBetweenVerticesCCW(vertex, nextVertex, otherVertex, normal);
                            if (angle1 < angle2)
                            {
                                //Use the exterior angle
                                foreach (var edge in nextEdges)
                                {
                                    var furtherVertex = edge.OtherVertex(nextVertex);
                                    var angle = MiscFunctions.ProjectedExteriorAngleBetweenVerticesCCW(vertex, nextVertex, furtherVertex, normal);
                                    if (!(angle < minAngle)) continue;
                                    minAngle = angle;
                                    //Update the current edge
                                    currentEdge = edge;
                                }
                            }
                            else
                            {
                                //Use the interior angle
                                foreach (var edge in nextEdges)
                                {
                                    var furtherVertex = edge.OtherVertex(nextVertex);
                                    var angle = MiscFunctions.ProjectedInteriorAngleBetweenVerticesCCW(vertex, nextVertex, furtherVertex, normal);
                                    if (!(angle < minAngle)) continue;
                                    minAngle = angle;
                                    //Update the current edge
                                    currentEdge = edge;

                                    PolygonalFace faceInQuestion = null;
                                    if (surface.Contains(edge.OwnedFace) && surface.Contains(edge.OtherFace))
                                    {
                                        //edge.OwnedFace.Color = new Color(KnownColors.Green);
                                        //edge.OtherFace.Color = red;
                                        //Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { error2Loops },
                                        //    new List<TessellatedSolid> { originalSolid });
                                    }
                                    else if (surface.Contains(edge.OwnedFace))
                                    {
                                        faceInQuestion = edge.OtherFace;
                                    }
                                    else if (surface.Contains(edge.OtherFace))
                                    {
                                        faceInQuestion = edge.OwnedFace;
                                    }
                                    if (faceInQuestion != null)
                                    {
                                        //faceInQuestion.Color = red;
                                        //var n2 = PolygonalFace.DetermineNormal(faceInQuestion.Vertices, out _);
                                        //Presenter.ShowAndHang(originalSolid);
                                        //Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { error2Loops },
                                        //    new List<TessellatedSolid> { originalSolid });
                                    }
                                }
                            }
                            foreach (var edge in nextEdges)
                            {
                                if (currentFace.Edges.Contains(edge))
                                {
                                    if (edge == currentEdge) break; //This is what we want
                                                                    //Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { error2Loops },
                                                                    //    new List<TessellatedSolid> { originalSolid });
                                }
                            }
                        }
                        else
                        {
                            //Update the current edge
                            currentEdge = nextEdges.First();
                        }
                        //Update the current vertex
                        vertex = nextVertex;
                        loop.Add(vertex);
                        //Get the next vertex                    
                        nextVertex = currentEdge.OtherVertex(vertex);
                        edgeLoop.Add((currentEdge, vertex, nextVertex));
                        assignedEdges.Add(currentEdge);
                    }

                    //To determine order:
                    //The vertices should be listed such that their edge vector cross producted with the second point,
                    //toward a third point on the positive face that provided this edge lines up with the normal.
                    //If that is incorrect, then this may be a hole.
                    //All edges should agree on this test.
                    var correct = 0;
                    var needsReversal = 0;
                    foreach (var edgeTuple in edgeLoop)
                    {
                        var edge = edgeTuple.Item1;
                        outerEdges.Remove(edge);
                        var isOtherFace = surface.Contains(edge.OtherFace);
                        var isOwnedFace = surface.Contains(edge.OwnedFace);
                        if (isOwnedFace == isOtherFace) throw new Exception("Should be one and only one face for this edge on this surface");
                        var positiveFaceBelongingToEdge = isOwnedFace ? edge.OwnedFace : edge.OtherFace;
                        var vertex3 = positiveFaceBelongingToEdge.OtherVertex(edge);
                        var v1 = vertex3.Position.subtract(edgeTuple.Item3.Position, 3); //To point according to our loop
                        var v2 = edgeTuple.Item3.Position.subtract(edgeTuple.Item2.Position, 3); //To minus from
                        var dot = v2.crossProduct(v1).dotProduct(positiveFaceBelongingToEdge.Normal, 3);
                        if (dot > 0)
                        {
                            correct++;
                        }
                        else
                        {
                            needsReversal++;
                        }
                    }
                    if (needsReversal > correct) loop.Reverse();
                    //if(needsReversal*correct != 0) Debug.WriteLine("Reversed Loop Count: " + needsReversal + " Forward Loop Count: " + correct);

                    //Get2DProjections does not project directionally (normal and normal.multiply(-1) return the same transform)
                    //However, the way we are unioning the polygons and eliminating overhand polygons seems to be taking care of this
                    var surfacePath = MiscFunctions.Get2DProjectionPointsAsLight(loop, normal).ToList();
                    var area2D = MiscFunctions.AreaOfPolygon(surfacePath);
                    if (area2D.IsNegligible(minAreaToConsider)) continue;

                    //Trust the ordering from the face normals. A self intersecting polygon may have a negative area, 
                    //but in-fact be positive once it undergoes a Fill Positive union. Same goes for positive areas.
                    //if (Math.Sign(area2D) != Math.Sign(area3D)) surfacePath.Reverse();
                    surfacePaths.Add(surfacePath);
                }
                if (!surfacePaths.Any()) continue;
                allPaths.AddRange(surfacePaths);
            }

            //By unioning the path into non-self intersecting paths, 
            //partially covered holes will be reduced to their final non-covered size.
            //This is necessary for the next few checks in determining if it is a hole or an overhang.
            //This union operation is the trickiest union in the silhouette function to reason through. 
            //Using positive fill or even/odd perform pretty well, but they union overlapping 
            //negative regions. This is undesirable, since we do not want to union a hole
            //with an overlapping region. For this reason, Union Non-Zero is used. It keeps
            //the holes in their proper orientation and does not combine them together. 
            var nonSelfIntersectingPaths = PolygonOperations.Union(allPaths, out _,false,  PolygonFillType.NonZero);
            var correctedSurfacePath = EliminateOverhangPolygons(nonSelfIntersectingPaths, projectedFacePolygons);
            //if (allPaths.Sum(p => p.Count) > 10) Presenter.ShowAndHang(nonSelfIntersectingPaths);

            return correctedSurfacePath;
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

        private static List<PointLight> GetPathFromFace(PolygonalFace face, Dictionary<int, PointLight> projectedPoints, bool forceToBePositive)
        {
            if (face.Vertices.Count != 3) throw new Exception("This method was only developed with triangles in mind.");
            //Make sure the polygon is ordered correctly (we already know this face is positive)
            var points = face.Vertices.Select(v => projectedPoints[v.IndexInList]).ToList();
            var area = MiscFunctions.AreaOfPolygon(points);
            if (forceToBePositive && area < 0) points.Reverse();
            return points;
        }

        private static PolygonLight GetPolygonFromFace(PolygonalFace face, Dictionary<int, PointLight> projectedPoints, bool forceToBePositive)
        {
            if (face.Vertices.Count != 3) throw new Exception("This method was only developed with triangles in mind.");
            //Make sure the polygon is ordered correctly (we already know this face is positive)
            var points = face.Vertices.Select(v => projectedPoints[v.IndexInList]).ToList();
            var facePolygon = new PolygonLight(points);
            if (forceToBePositive && facePolygon.Area < 0) facePolygon = PolygonLight.Reverse(facePolygon);
            return facePolygon;
        }
    }
}
