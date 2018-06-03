using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class SilhouetteDebugger
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
        public static List<List<Point>> Slow(IList<PolygonalFace> faces, double[] normal, double minAngle = 0.1,
            double minPathAreaToConsider = 0.0, double depthOfPart = 0.0)
        {
            var angleTolerance = Math.Cos((90 - minAngle) * Math.PI / 180);

            //Get the positive faces (defined as face normal along same direction as the silhoutte normal).
            var positiveFaces = new HashSet<PolygonalFace>();
            var vertices = new HashSet<Vertex>();
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal);
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
            var projectedPoints = new Dictionary<int, Point>();
            foreach (var vertex in vertices)
            {
                projectedPoints.Add(vertex.IndexInList, MiscFunctions.Get2DProjectionPoint(vertex, transform));
            }

            //Build a dictionary of faces to polygons
            //var projectedFacePolygons = positiveFaces.ToDictionary(f => f, f => GetPolygonFromFace(f, projectedPoints, true));
            //Use GetPolygonFromFace and force to be positive faces with true"
            var projectedFacePolygons2 = positiveFaces.Select(f => GetPolygonFromFace(f, projectedPoints, true)).ToList().Where(p => p.Area > minPathAreaToConsider).ToList();
            var solution = PolygonOperations.Union(projectedFacePolygons2, false).Select(p => p.Path).ToList();

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
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal, double minAngle = 0.1)
        {
            var depthOfPart = MinimumEnclosure.GetLengthAndExtremeVertices(normal, ts.Vertices, out _, out _);
            List<List<Point>> silhouette;
            //try
            //{
            silhouette = Run(ts.Faces, normal, ts, minAngle, ts.SameTolerance, depthOfPart);
            //}
            //catch
            //{
            //var silhouette2 = Slow(ts.Faces, normal, minAngle, ts.SameTolerance, depthOfPart);
            //}
            return silhouette;
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
        public static List<List<Point>> Run(IList<PolygonalFace> faces, double[] normal, TessellatedSolid originalSolid, double minAngle = 0.1,
        double minPathAreaToConsider = 0.0, double depthOfPart = 0.0)
        {
            //Get the positive faces into a dictionary
            if (minAngle > 4.999) minAngle = 4.999; //min angle must be between 0 and 5 degrees. 0.1 degree has proven to be good.
            //Note also that the offset is based on the min angle.
            var angleTolerance = Math.Cos((90 - minAngle) * Math.PI / 180); //Angle of 89.9 Degrees from normal

            var positiveFaces = new HashSet<PolygonalFace>();
            var smallFaces = new List<PolygonalFace>();
            var allPositives = new Dictionary<int, PolygonalFace>();
            var allVertices = new HashSet<Vertex>();
            foreach (var face in faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (dot.IsGreaterThanNonNegligible(angleTolerance))
                {
                    allPositives.Add(face.IndexInList, face);
                    positiveFaces.Add(face);
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
                    allPositives.Add(smallFace.IndexInList, smallFace);
                    positiveFaces.Add(smallFace);
                }
            }

            //Get the polygons of all the positive faces. Force the polygons to be positive CCW
            var vertices = new HashSet<Vertex>();
            foreach (var face in positiveFaces)
            {
                foreach (var vertex in face.Vertices)
                {
                    vertices.Add(vertex);
                }
            }
            var transform = MiscFunctions.TransformToXYPlane(normal, out _);
            var projectedPoints = new Dictionary<int, Point>();
            foreach (var vertex in vertices)
            {
                projectedPoints.Add(vertex.IndexInList, MiscFunctions.Get2DProjectionPoint(vertex, transform));
            }
            var projectedFacePolygons = positiveFaces.ToDictionary(f => f.IndexInList, f => GetPathFromFace(f, projectedPoints, true));

            //Get all the surfaces
            var allSurfaces = SeperateIntoSurfaces(allPositives);
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
            originalSolid.HasUniformColor = false;
            var i = 0;
            foreach (var surface in allSurfaces)
            {
                if (i == colors.Count) i = 0;
                var color = colors[i];
                i++;
                foreach (var face in surface)
                {
                    face.Color = color;
                }
            }
            //Presenter.ShowAndHang(originalSolid);

            //Get the surface paths from all the surfaces and union them together
            var solution = new List<List<Point>>();
            var loopCount = 0;
            foreach (var surface in allSurfaces)
            {
                //Split into positive and negative paths. For each of the negative paths, we need to 
                //check whether the path is a hole OR an overhang. It may be also be possible to be both??
                //A path is a hole, IF none of its points are inside the polygons from the adjacent faces
                //on that loop.
                var surfaceUnion = GetSurfacePaths(surface, normal, minPathAreaToConsider, originalSolid, projectedFacePolygons);

                //var area = 0.0;
                //var significantPaths = new List<List<Point>>();
                //foreach (var path in surfacePaths)
                //{
                //    var simplePath = PolygonOperations.SimplifyFuzzy(path);
                //    if (!simplePath.Any()) continue;  //Ignore very small patches
                //    var pathArea = MiscFunctions.AreaOfPolygon(simplePath);
                //    area += pathArea;
                //    if (pathArea.IsNegligible(minPathAreaToConsider)) continue;  //Ignore very small patches
                //    significantPaths.Add(simplePath);
                //}
                //if (!significantPaths.Any()) continue;
                //solution.AddRange(significantPaths);
                //List<List<Point>> surfaceUnion;
                //try
                //{
                //    //Use positive fill, since it handles overlapping surfaces correctly
                //    surfaceUnion = PolygonOperations.Union(significantPaths, false, PolygonFillType.Positive);
                //}
                //catch
                //{
                //    //Simplify likely reduced the polygon to nothing. It is insignificant, so continue.
                //    continue;
                //}
                //if (!surfaceUnion.Any()) continue;

                //if (area < 0)
                //{
                //    area = surfaceUnion.Sum(path => MiscFunctions.AreaOfPolygon(path));
                //    if (area < 0) throw new Exception("Area for each surface must be positive");
                //}
                solution.AddRange(surfaceUnion);
                //if (loopCount == 0)
                //{
                //    solution = new List<List<Point>>(surfaceUnion);
                //}
                //else
                //{

                //    var oldSolution = new List<List<Point>>(solution);
                //    solution = PolygonOperations.Union(oldSolution, surfaceUnion.ToList());
                //}
                //loopCount++;
            }
            Presenter.ShowAndHang(solution);
            solution = PolygonOperations.Union(solution);
            //Offset by enough to account for minimum angle 
            var scale = Math.Tan(minAngle * Math.PI / 180) * depthOfPart;

            //Remove tiny polygons and slivers 
            solution = PolygonOperations.SimplifyFuzzy(solution);
            var offsetPolygons = PolygonOperations.OffsetMiter(solution, scale);
            var significantSolution = PolygonOperations.OffsetMiter(offsetPolygons, -scale);
            Presenter.ShowAndHang(significantSolution);
            return significantSolution;
        }

        #region GetSurfacePaths
        private static IEnumerable<List<Point>> GetSurfacePaths(ICollection<PolygonalFace> surface, double[] normal,
            double minAreaToConsider, TessellatedSolid originalSolid, Dictionary<int, List<Point>> projectedFacePolygons)
        {

            originalSolid.HasUniformColor = false;
            var blue = new Color(KnownColors.Blue);
            var red = new Color(KnownColors.Red);
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
            var errorLoops = new List<List<Vertex>>();
            var assignedEdges = new HashSet<Edge>();
            var loops = new List<List<Vertex>>();
            while (outerEdges.Any())
            {
                //Get the start vertex and edge and save them to the lists
                var startEdge = outerEdges.First();
                var startVertex = startEdge.From;
                var loop = new List<Vertex> { startVertex };

                var nextVertex = startEdge.To;
                var edgeLoop = new List<Tuple<Edge, Vertex, Vertex>>
                {
                    new Tuple<Edge, Vertex, Vertex>(startEdge, startVertex, nextVertex)
                };
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
                        var error2Loops = new List<List<Vertex>>();
                        var minAngle = 2 * Math.PI;
                        var currentFace = surface.Contains(currentEdge.OtherFace) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                        currentFace.Color = new Color(KnownColors.White);
                        var otherVertex = currentFace.OtherVertex(currentEdge.To, currentEdge.From);
                        var angle1 = MiscFunctions.ProjectedExteriorAngleBetweenVerticesCCW(vertex, nextVertex, otherVertex, normal);
                        var angle2 = MiscFunctions.ProjectedInteriorAngleBetweenVerticesCCW(vertex, nextVertex, otherVertex, normal);
                        if (angle1 < angle2)
                        {
                            //Use the exterior angle
                            foreach (var edge in nextEdges)
                            {
                                error2Loops.Add(new List<Vertex> { edge.From, edge.To });
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
                                //error2Loops.Clear();
                                error2Loops.Add(new List<Vertex> { edge.From, edge.To });
                                var furtherVertex = edge.OtherVertex(nextVertex);
                                var angle = MiscFunctions.ProjectedInteriorAngleBetweenVerticesCCW(vertex, nextVertex, furtherVertex, normal);
                                if (!(angle < minAngle)) continue;
                                minAngle = angle;
                                //Update the current edge
                                currentEdge = edge;

                                PolygonalFace faceInQuestion = null;
                                if (surface.Contains(edge.OwnedFace) && surface.Contains(edge.OtherFace))
                                {
                                    edge.OwnedFace.Color = new Color(KnownColors.Green);
                                    edge.OtherFace.Color = red;
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
                    edgeLoop.Add(new Tuple<Edge, Vertex, Vertex>(currentEdge, vertex, nextVertex));
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
                    var v1 = vertex3.Position.subtract(edgeTuple.Item3.Position); //To point according to our loop
                    var v2 = edgeTuple.Item3.Position.subtract(edgeTuple.Item2.Position); //To minus from
                    var dot = v2.crossProduct(v1).dotProduct(positiveFaceBelongingToEdge.Normal);
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

                //Note: Removing all the vertices in the loop that are invisible would make edge overlaps innacurate
                //because it is unlikely there is a vertex exactly where two overlapping edges intersect when projected.
                //The only way to consider visibility is to cut invisible sections from lines. 


                //Get2DProjections does not project directionally (normal and normal.multiply(-1) return the same transform)
                //So we need to use the 3D area to tell us if the path is ordered correctly
                var area3D = MiscFunctions.AreaOf3DPolygon(loop, normal);
                var surfacePath = MiscFunctions.Get2DProjectionPoints(loop, normal).ToList();
                var area2D = MiscFunctions.AreaOfPolygon(surfacePath);
                if (area2D.IsNegligible(minAreaToConsider)) continue;
                if (surfacePath.Count > 3)
                {
                    
                }
                surfacePaths.Add(surfacePath);
                loops.Add(loop);
            }
    
            if (!surfacePaths.Any()) return surfacePaths;
            if (surfacePaths.Sum(p => p.Count) == 3)
            {
                //This is just a triangle. Nothing more to consider.
                return surfacePaths;
            }

            var correctedSurfacePath = new List<List<Point>>();
            //By unioning the path into non-self intersecting paths, 
            //partially covered holes will be reduced to their final non-covered size.
            //This is necessary for the next few checks in determining if it is a hole
            //or an overhang.
            var nonSelfIntersectingPaths = PolygonOperations.Union(surfacePaths, false);
            //var area2D2 = MiscFunctions.AreaOfPolygon(nonSelfIntersectingPaths);
            //if (!area2D2.IsPracticallySame(area2D, 1.0))
            //{
            if (nonSelfIntersectingPaths.Sum(p => p.Count) > 10)
            {
                Presenter.ShowAndHang(surfacePaths);
                Presenter.ShowAndHang(nonSelfIntersectingPaths);
                var alternateMethod = new List<List<Point>>();
                foreach (var surfacePath in surfacePaths)
                {
                    alternateMethod.AddRange(PolygonOperations.Union(new List<List<Point>>{surfacePath}, false, PolygonFillType.NonZero));
                }
                Presenter.ShowAndHang(alternateMethod);
                nonSelfIntersectingPaths = alternateMethod;
            }
            //ToDo: Send all the polygons through the check, since using non-zero changed the CW negative to positive.
            //}

            foreach (var path in nonSelfIntersectingPaths)
            {
                //Trust the ordering from the face normals. A self intersecting polygon may have a negative area, 
                //but in-fact be positive once it undergoes a Fill Positive union. Same goes for positive areas.
                //if (Math.Sign(area2D) != Math.Sign(area3D)) surfacePath.Reverse();

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

                    var polygons = new HashSet<List<Point>>();
                    foreach (var face in surface)
                    {
                        polygons.Add(projectedFacePolygons[face.IndexInList]);
                    }
                    //Presenter.ShowAndHang(polygons.ToList(), new List<List<Point>> { path });
                    //foreach (var borderVertex in loops)
                    //{
                    //    foreach (var face in borderVertex.Faces.Where(surface.Contains))
                    //    {
                    //        //true if the element is added to the HashSet<T> object; false if the element is already present.
                    //        polygons.Add(projectedFacePolygons[face.IndexInList]);
                    //        face.Color = blue;
                    //    }
                    //}
                    //Get a few points that are inside the polygon (It is non-self intersecting,
                    //but taking the center may not work.
                    var pathCenterX = path.Average(v => v.X);
                    var pathCenterY = path.Average(v => v.Y);
                    var centerPoint = new PointLight(pathCenterX, pathCenterY);
                    if (MiscFunctions.IsPointInsidePolygon(path, centerPoint , false))
                    {
                        //Great! We have an easy point that is inside. Check if it is inside any surface polygon
                    }
                    else
                    {
                        //Get a point that is inside the polygon. We could set up a sweep line to do this,
                        //but for now I'm taking an easier approach (take average of three random points)
                        var rnd = new Random();
                        bool isInside = false;
                        var count = 0;
                        while (!isInside && count < 1000)
                        {
                            int r1 = rnd.Next(path.Count);
                            int r2 = rnd.Next(path.Count);
                            int r3 = rnd.Next(path.Count);
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
                                isInside = true;
                            }
                            count++;
                        }
                        if (count == 1000)
                        {
                            Debug.WriteLine("Not able to find a point inside polygon");
                            Presenter.ShowAndHang(path);
                        }
                    }

                    if (polygons.Any(p => MiscFunctions.IsPointInsidePolygon(p, centerPoint, true)))
                    {
                        //This is an overhang
                        path.Reverse();
                        correctedSurfacePath.Add(path);
                    }
                    else
                    {
                        //This is a hole
                        correctedSurfacePath.Add(path);
             
                    }

                    //foreach (var adjacentFacePolygon in polygons)
                    //{
                    //    var centerX = adjacentFacePolygon.Average(v => v.X);
                    //    var centerY = adjacentFacePolygon.Average(v => v.Y);
                    //    if (MiscFunctions.IsPointInsidePolygon(path, new PointLight(centerX, centerY)))
                    //    {
                    //        isOverhangCounter1++;
                    //    }
                    //}
                    //if (isOverhangCounter1 > 0)
                    //{
                    //    //This is an overhang. Include it as a positive. 
                    //    //if (path.Count > 3)
                    //    //{
                    //    //    Presenter.ShowAndHang(path);
                    //    //    Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { loops },
                    //    //        new List<TessellatedSolid> { originalSolid });
                    //    //}
                    //    path.Reverse();
                    //    correctedSurfacePath.Add(path);
                    //    continue;
                    //}

                    ////Note: you cannot use all the positive faces or even all the faces of the surface, 
                    ////because a hole may be partially covered by an upper surface.
                    ////But if that "hole" is covered by any adjacent faces, then it is actually an overhang.     
                    ////If the point is inside any polygon, it is an overhang
                    ////Else, it is a hole.
                    ////Note that there can be points on the saddle points of the overhang that will be classified
                    ////as holes. 
                    //var isHoleCounter2 = 0;
                    //var isOverhangCounter2 = 0;
                    //var overhangDistance = 0.0;
                    //var holeDistance = 0.0;
                    //var priorPointIsInside = false;
                    //Point priorPoint = null;
                    //foreach (var point in path)
                    //{
                    //    if (polygons.Any(poly => MiscFunctions.IsPointInsidePolygon(poly, point, false)))
                    //    {
                    //        isOverhangCounter2++;
                    //        if (priorPoint != null && priorPointIsInside)
                    //        {
                    //            overhangDistance += priorPoint.Position.subtract(point.Position).norm2();
                    //        }
                    //        priorPoint = point;
                    //        priorPointIsInside = true;
                    //    }
                    //    else
                    //    {
                    //        isHoleCounter2++;
                    //        if (priorPoint != null && !priorPointIsInside)
                    //        {
                    //            holeDistance += priorPoint.Position.subtract(point.Position).norm2();
                    //        }
                    //        priorPoint = point;
                    //        priorPointIsInside = false;
                    //    }
                    //}

                    ////if (path.Count > 3)
                    ////{
                    ////    Presenter.ShowAndHang(path);
                    ////    Presenter.ShowVertexPathsWithSolid(new List<List<List<Vertex>>> { loops },
                    ////        new List<TessellatedSolid> { originalSolid });
                    ////}
                    //if (isHoleCounter2 > isOverhangCounter2 && holeDistance > overhangDistance)
                    //{
                    //    if (path.Count > 3)
                    //    {
                    //        //Presenter.ShowAndHang(surfacePath);
                    //        //Presenter.ShowVertexPathsWithSolid(
                    //        //    new List<List<List<Vertex>>> {new List<List<Vertex>> {loop}},
                    //        //    new List<TessellatedSolid> {originalSolid});
                    //    }

                    //    if (isOverhangCounter2 > 0)
                    //    {
                    //        Debug.WriteLine("Determination of hole is inconclusive");
                    //        //
                    //    }
                    //    correctedSurfacePath.Add(path);
                    //}
                    //else
                    //{
                    //    //include this overlap as a positive.
                    //    path.Reverse();
                    //    correctedSurfacePath.Add(path);
                    //}
                }
            }

            var correctedDirections = PolygonOperations.Union(correctedSurfacePath, false);
            if (correctedDirections.Sum(p => p.Count) > 3)
            {
                Presenter.ShowAndHang(correctedDirections);
            }
            var area = correctedSurfacePath.Sum(s => MiscFunctions.AreaOfPolygon(s));
            if (area < 0)
            {
                if (correctedSurfacePath.Count == 1)
                {
                    //This error can be handled by reversing the polygon
                    correctedSurfacePath[0].Reverse();
                }
                else
                {
                    throw new Exception("Boundary loops from positive faces must have a total area that is positive.");
                }
            }
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

        private static List<Point> GetPathFromFace(PolygonalFace face, Dictionary<int, Point> projectedPoints, bool forceToBePositive)
        {
            if (face.Vertices.Count != 3) throw new Exception("This method was only developed with triangles in mind.");
            //Make sure the polygon is ordered correctly (we already know this face is positive)
            var points = face.Vertices.Select(v => projectedPoints[v.IndexInList]).ToList();
            var area = MiscFunctions.AreaOfPolygon(points);
            if (forceToBePositive && area < 0) points.Reverse();
            return points;
        }

        private static Polygon GetPolygonFromFace(PolygonalFace face, Dictionary<int, Point> projectedPoints, bool forceToBePositive)
        {
            if (face.Vertices.Count != 3) throw new Exception("This method was only developed with triangles in mind.");
            //Make sure the polygon is ordered correctly (we already know this face is positive)
            var points = face.Vertices.Select(v => projectedPoints[v.IndexInList]).ToList();
            var facePolygon = new Polygon(points);
            if (forceToBePositive && !facePolygon.IsPositive) facePolygon.Reverse();
            return facePolygon;
        }
    }
}
