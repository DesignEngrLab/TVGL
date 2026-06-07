// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.Triangulate.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;


namespace TVGL
{
    /// <summary>
    /// Triangulates a Polygon into faces in O(n log n) time.
    /// </summary>
    /// <References>
    /// The new approach is based on how it is presented in
    /// the book
    /// "Computational geometry: algorithms and applications". 2000
    /// Authors: de Berg, Mark and van Kreveld, Marc and Overmars, Mark and Schwarzkopf, Otfried and Overmars, M
    /// </References>
    /// A good summary of how the monotone polygons are created can be seen in the video: https://youtu.be/IkA-2Y9lBvM
    /// and the algorithm for triangulating the monotone polygons can be found here: https://youtu.be/pfXXgV9u6cw
    public static partial class PolygonOperations
    {
        #region Triangulation via Sweep Line. This is efficient and robust, but the triangles are not necessarily of good quality.
        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <param name="forceToPositive">if set to <c>true</c> [force to positive].</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="System.ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static IEnumerable<(Vertex A, Vertex B, Vertex C)> TriangulateSweepLine(this IEnumerable<Vertex> vertexLoop,
            Vector3 normal, bool forceToPositive = false, bool handleSelfIntersects = true, double suggestedAngle = 0.0)
        {
            var transform = normal.TransformToXYPlane(out _);
            var coords = new List<Vertex2D>();
            var indexToVertexDict = new Dictionary<int, Vertex>();
            foreach (var vertex in vertexLoop)
            {
                coords.Add(new Vertex2D(vertex.ConvertTo2DCoordinates(transform), vertex.IndexInList, -1));
                if (indexToVertexDict.ContainsKey(vertex.IndexInList))
                    throw new ArgumentException("The vertices must all have a unique IndexInList value", nameof(vertexLoop));
                indexToVertexDict.Add(vertex.IndexInList, vertex);
            }
            var polygon = new Polygon(coords);
            if (forceToPositive && !polygon.IsPositive) polygon.IsPositive = true;
            foreach (var triangleIndices in polygon.TriangulateToIndicesSweepLine(handleSelfIntersects, suggestedAngle))
            {
                if (indexToVertexDict[triangleIndices.A] != indexToVertexDict[triangleIndices.B]
                    && indexToVertexDict[triangleIndices.B] != indexToVertexDict[triangleIndices.C]
                    && indexToVertexDict[triangleIndices.C] != indexToVertexDict[triangleIndices.A])
                    yield return (
                        indexToVertexDict[triangleIndices.A], indexToVertexDict[triangleIndices.B],
                        indexToVertexDict[triangleIndices.C]);
            }
        }

        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoops">The vertex loops.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="System.ArgumentException">The vertices must all have a unique IndexInList value - vertexLoops</exception>
        public static IEnumerable<Vertex[]> Triangulate(this IEnumerable<IList<Vertex>> vertexLoops, Vector3 normal,
            bool handleSelfIntersects = true, double suggestedAngle = 0.0)
        {
            var transform = normal.TransformToXYPlane(out _);
            var polygons = new List<Polygon>();
            var indexToVertexDict = new Dictionary<int, Vertex>();
            foreach (var vertexLoop in vertexLoops)
            {
                var coords = new List<Vertex2D>();
                foreach (var vertex in vertexLoop)
                {
                    coords.Add(new Vertex2D(vertex.ConvertTo2DCoordinates(transform), vertex.IndexInList, -1));
                    if (indexToVertexDict.ContainsKey(vertex.IndexInList))
                        throw new ArgumentException("The vertices must all have a unique IndexInList value", nameof(vertexLoops));
                    indexToVertexDict.Add(vertex.IndexInList, vertex);
                }
                polygons.Add(new Polygon(coords));
            }
            polygons = polygons.CreateShallowPolygonTrees(false);
            foreach (var polygon in polygons)
            {
                foreach (var triangleIndices in polygon.TriangulateToIndicesSweepLine(handleSelfIntersects, suggestedAngle))
                    yield return new[]
                        {indexToVertexDict[triangleIndices.A], indexToVertexDict[triangleIndices.B],
                        indexToVertexDict[triangleIndices.C]};
            }
        }


        /// <summary>
        /// Triangulates the specified polygons which may include holes. However, the .
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        public static IEnumerable<Vector2[]> TriangulateToCoordinatesSweepLine(this Polygon polygon, bool handleSelfIntersects = true,
            double suggestedAngle = 0.0)
        {
            foreach (var triangle in polygon.TriangulateSweepLine(handleSelfIntersects, suggestedAngle))
                yield return new[] { triangle[0].Coordinates, triangle[1].Coordinates, triangle[2].Coordinates };
        }

        /// <summary>
        /// Triangulates the specified polygons which may include holes. However, the .
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="handleSelfIntersects">if set to <c>true</c> [handle self intersects].</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        public static IEnumerable<(int A, int B, int C)> TriangulateToIndicesSweepLine(this Polygon polygon, bool handleSelfIntersects = true,
            double suggestedAngle = 0.0)
        {
            var vertexIndices = new HashSet<int>();
            //var needToReIndex = false;
            //foreach (var v in polygon.AllPolygons.SelectMany(p => p.Vertices))
            //{
            //    if (vertexIndices.Contains(v.IndexInList))
            //    {
            //        needToReIndex = true;
            //        break;
            //    }
            //    vertexIndices.Add(v.IndexInList);
            //}
            var index = 0;
            //if (needToReIndex)
            //{
            foreach (var subPolygon in polygon.AllPolygons)
                foreach (var vertex in subPolygon.Vertices)
                    vertex.IndexInList = index++;
            //}
            foreach (var triangle in polygon.TriangulateSweepLine(handleSelfIntersects, suggestedAngle))
                yield return (triangle[0].IndexInList, triangle[1].IndexInList, triangle[2].IndexInList);
        }
        /// <summary>
        /// Triangulates the specified polygons which may include holes.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="handleSelfIntersects">if set to <c>true</c> [handle self intersects].</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        /// <exception cref="System.ArgumentException">Triangulate Polygon requires a positive polygon. A negative one was provided. - polygon</exception>
        /// <exception cref="System.Exception">Unable to triangulate polygon.</exception>
        public static List<Vertex2D[]> TriangulateSweepLine(this Polygon polygon, bool handleSelfIntersects = true,
            double suggestedAngle = 0.0)
        {
            if (polygon.Area.IsNegligible() || (polygon.IsConvex && !polygon.InnerPolygons.Any()))
            {
                var triangleList = new List<Vertex2D[]>();
                for (int i = 2; i < polygon.Vertices.Count; i++)
                    triangleList.Add([polygon.Vertices[0], polygon.Vertices[i - 1], polygon.Vertices[i]]);
                return triangleList;
            }
            if (!polygon.IsPositive)
                throw new ArgumentException("Triangulate Polygon requires a positive polygon. A negative one was provided.", nameof(polygon));

            var numVertices = 0;
            foreach (var subPolygon in polygon.AllPolygons)
            {
                numVertices += subPolygon.Vertices.Count;
                if (numVertices > 4) break;
            }
            if (numVertices <= 2) return new List<Vertex2D[]>();
            if (numVertices == 3) return new List<Vertex2D[]> { polygon.Vertices.ToArray() };
            if (numVertices == 4)
            {
                polygon.MakePolygonEdgesIfNonExistent();
                IList<Vertex2D> verts = polygon.Vertices;
                var concaveEdge = polygon.Vertices.FirstOrDefault(v => v.EndLine.Vector.Cross(v.StartLine.Vector) < 0);
                if (concaveEdge != null)
                {
                    while (verts[0].IndexInList != concaveEdge.IndexInList)
                        verts = [verts[1], verts[2], verts[3], verts[0]];
                }
                return new List<Vertex2D[]> { new[] { verts[0], verts[1], verts[2] }, new[] { verts[0], verts[2], verts[3] } };
            }
            if (polygon.IsConvex && !polygon.InnerPolygons.Any())
            {
                var triangleList = new List<Vertex2D[]>();
                for (int i = 2; i < polygon.Vertices.Count; i++)
                    triangleList.Add([polygon.Vertices[0], polygon.Vertices[i - 1], polygon.Vertices[i]]);
                return triangleList;
            }
            var triangleFaceList = new List<Vertex2D[]>();
            // this is the returned list of triangles. 

            // in case this is a deep polygon tree - recurse down and solve for the inner positive polygons
            foreach (var hole in polygon.InnerPolygons)
                foreach (var smallInnerPolys in hole.InnerPolygons)
                    triangleFaceList.AddRange(TriangulateSweepLine(smallInnerPolys, handleSelfIntersects, suggestedAngle));

            var selfIntersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            if (selfIntersections.Count > 0)
            {
                if (selfIntersections.All(si => si.WhereIntersection == WhereIsIntersection.BothStarts))
                    return polygon.RemoveSelfIntersections(ResultType.OnlyKeepPositive).SelectMany(p => p.TriangulateSweepLine(false, suggestedAngle)).ToList();
                else
                {
                    //Try to simplify and then re-check to see if it is valid now. This will also fix threading issues.
                    polygon = SimplifyByAreaChangeToNewPolygon(polygon, areaSimplificationFraction);
                    selfIntersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
                    if (selfIntersections.Count > 0)
                    {
                        //IO.Save(polygon, "errorPolygon" + DateTime.Now.ToOADate() + ".json");
                        //throw new Exception("Self-Intersecting Polygon cannot be triangulated.");
                    }
                }
            }
            const int maxNumberOfAttempts = 10;
            var attempts = 0;
            var random = new Random(1);
            var successful = false;
            var angle = suggestedAngle;
            var localTriangleFaceList = new List<Vertex2D[]>();
            do
            {
                var c = Math.Cos(angle);
                var s = Math.Sin(angle);
                localTriangleFaceList.Clear();
                var triangleArea = double.NegativeInfinity;
                if (angle != 0)
                {
                    var rotateMatrix = new Matrix3x3(c, s, -s, c, 0, 0);
                    polygon.Transform(rotateMatrix); //This destructively alters polygon coordinates, but if used - we rotate back in 22 lines
                }
                try
                {
                    foreach (var monoPoly in CreateXMonotonePolygons(polygon))
                        localTriangleFaceList.AddRange(TriangulateMonotonePolygon(monoPoly));
                    triangleArea = 0.5 * localTriangleFaceList
                       .Sum(tri => Math.Abs((tri[1].Coordinates - tri[0].Coordinates).Cross(tri[2].Coordinates - tri[0].Coordinates)));
                }
                catch
                {
                    //IO.Save(polygon, "errorPolygon" + DateTime.Now.ToOADate() + ".json");
                    //throw new Exception("Unable to triangulate polygon.");
                }
                successful = 2 * Math.Abs(polygon.Area - triangleArea) / (polygon.Area + triangleArea) < 0.01;
                if (!successful && !double.IsNegativeInfinity(triangleArea))
                    Log.Information(polygon.Area + ",   " + triangleArea, 4);
                if (angle != 0)
                {
                    var rotateMatrix = new Matrix3x3(c, -s, s, c, 0, 0);
                    polygon.Transform(rotateMatrix);
                }
                angle = random.NextDouble() * 2 * Math.PI;

            } while (!successful && attempts++ < maxNumberOfAttempts);
            if (!successful)
            {
                if (handleSelfIntersects)
                    return polygon.RemoveSelfIntersections(ResultType.OnlyKeepPositive).SelectMany(p => p.TriangulateSweepLine(false, suggestedAngle)).ToList();
                else
                {
                    //Try to simplify and then re-check to see if it is valid now.
                    polygon = SimplifyByAreaChangeToNewPolygon(polygon, areaSimplificationFraction);
                    selfIntersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
                    if (selfIntersections.Count > 0)
                    {
                        IO.Save(polygon, "errorPolygon" + DateTime.Now.ToOADate() + ".json");
                        throw new Exception("Unable to triangulate polygon.");
                    }
                }
            }
            triangleFaceList.AddRange(localTriangleFaceList);
            return triangleFaceList;
        }


        /// <summary>
        /// Creates the x monotone polygons.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> CreateXMonotonePolygons(this Polygon polygon)
        {
            if (polygon.PartitionIntoMonotoneBoxes(MonotonicityChange.X).Count() == 1)
            {
                yield return polygon;
                yield break;
            }
            polygon.MakePolygonEdgesIfNonExistent();
            var connections = FindConnectionsToConvertToMonotonePolygons(polygon);
            foreach (var p in polygon.AllPolygons)
                foreach (var edge in p.Edges)
                    AddNewConnection(connections, edge.FromPoint, edge.ToPoint);
            while (connections.Any())
            {
                var startingConnectionKVP = connections.First();
                var start = startingConnectionKVP.Key;
                var newVertices = new List<Vertex2D> { start };
                var current = start;
                var nextConnections = startingConnectionKVP.Value;
                Vertex2D next = nextConnections[0];
                while (next != start)
                {
                    newVertices.Add(next);
                    RemoveConnection(connections, current, next);
                    current = next;
                    nextConnections = connections[current];
                    if (nextConnections.Count == 1) next = nextConnections[0];
                    else next = MiscFunctions.ChooseTightestLeftTurn(nextConnections, current, newVertices[^2]);
                }
                RemoveConnection(connections, current, next);
                yield return new Polygon(newVertices.Select(v => v.Copy()));
            }
        }

        /// <summary>
        /// Finds the connections to convert to monotone polygons.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>Dictionary&lt;Vertex2D, List&lt;Vertex2D&gt;&gt;.</returns>
        private static Dictionary<Vertex2D, List<Vertex2D>> FindConnectionsToConvertToMonotonePolygons(Polygon polygon)
        {
            var sortedVertices = new List<Vertex2D>();
            var comparer = new TwoDSortXFirst();
            foreach (var p in polygon.AllPolygons)
                sortedVertices = CombineSortedVertexLists(sortedVertices, p.OrderedXVertices, comparer).ToList();
            var connections = new Dictionary<Vertex2D, List<Vertex2D>>();
            // the edgeDatums are the current edges in the sweep. The Vertex is the past polygon point (aka helper)
            // that is often connected to the current vertex in the sweep. The boolean is only true when the vertex
            // was a merge vertex.
            var edgeDatums = new Dictionary<PolygonEdge, (Vertex2D, bool)>();
            foreach (var vertex in sortedVertices)
            {
                var monoChange = GetMonotonicityChange(vertex);
                var cornerCross = vertex.EndLine.Vector.Cross(vertex.StartLine.Vector);
                if (monoChange == MonotonicityChange.SameAsPrevious || monoChange == MonotonicityChange.Neither || monoChange == MonotonicityChange.Y)
                // then it's regular
                {
                    if (vertex.StartLine.Vector.X > 0 || vertex.EndLine.Vector.X > 0 ||  //headed in the positive x direction (enclosing along the bottom)
                        (vertex.StartLine.Vector.X == 0 && vertex.EndLine.Vector.X == 0 && vertex.StartLine.Vector.Y > 0))
                    {   // in the CCW direction or along the bottom
                        MakeNewDiagonalEdgeIfMerge(connections, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                        edgeDatums.Add(vertex.StartLine, (vertex, false));
                    }
                    else // then in the CW direction along the top
                    {
                        var closestDatumEdge = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        MakeNewDiagonalEdgeIfMerge(connections, edgeDatums, closestDatumEdge, vertex);
                        edgeDatums[closestDatumEdge] = (vertex, false);
                    }
                }
                else if (cornerCross >= 0) //then either start or end
                {
                    if (vertex.StartLine.Vector.X > 0 && vertex.EndLine.Vector.X < 0 || // then start
                        (vertex.StartLine.Vector.X > 0 && vertex.EndLine.Vector.X == 0 && vertex.EndLine.Vector.Y < 0))
                        edgeDatums.Add(vertex.StartLine, (vertex, false));
                    else // then it's an end
                    {
                        MakeNewDiagonalEdgeIfMerge(connections, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                    }
                }
                else //then either split or merge
                {
                    if (vertex.StartLine.Vector.X > 0 && vertex.EndLine.Vector.X < 0 || // then split
                       (vertex.StartLine.Vector.Y > 0 && vertex.EndLine.Vector.Y > 0))
                    {   // it's a split
                        var closestDatumEdge = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        var helperVertex = edgeDatums[closestDatumEdge].Item1;
                        AddNewConnection(connections, vertex, helperVertex);
                        AddNewConnection(connections, helperVertex, vertex);
                        edgeDatums[closestDatumEdge] = (vertex, false);
                        edgeDatums.Add(vertex.StartLine, (vertex, false));
                    }
                    else //then it's a merge
                    {
                        MakeNewDiagonalEdgeIfMerge(connections, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                        PolygonEdge closestDatum = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        if (closestDatum != null)
                        {
                            MakeNewDiagonalEdgeIfMerge(connections, edgeDatums, closestDatum, vertex);
                            edgeDatums[closestDatum] = (vertex, true);
                        }
                    }
                }
            }
            return connections;
        }

        /// <summary>
        /// Makes the new diagonal edge if merge.
        /// </summary>
        /// <param name="connections">The connections.</param>
        /// <param name="edgeDatums">The edge datums.</param>
        /// <param name="datum">The datum.</param>
        /// <param name="vertex">The vertex.</param>
        private static void MakeNewDiagonalEdgeIfMerge(Dictionary<Vertex2D, List<Vertex2D>> connections,
            Dictionary<PolygonEdge, (Vertex2D, bool)> edgeDatums, PolygonEdge datum, Vertex2D vertex)
        {
            if (!edgeDatums.TryGetValue(datum, out var prevLineHelperData)) return;
            var helperVertex = prevLineHelperData.Item1;
            var isMergePoint = prevLineHelperData.Item2;
            if (isMergePoint) //if this was a merge point
            {
                AddNewConnection(connections, vertex, helperVertex);
                AddNewConnection(connections, helperVertex, vertex);
            }
        }

        /// <summary>
        /// Adds the new connection.
        /// </summary>
        /// <param name="connections">The connections.</param>
        /// <param name="fromVertex">From vertex.</param>
        /// <param name="toVertex">To vertex.</param>
        private static void AddNewConnection(Dictionary<Vertex2D, List<Vertex2D>> connections, Vertex2D fromVertex, Vertex2D toVertex)
        {
            if (connections.TryGetValue(fromVertex, out var verts))
                verts.Add(toVertex);
            else
            {
                var newToVertices = new List<Vertex2D> { toVertex };
                connections.Add(fromVertex, newToVertices);
            }
        }
        /// <summary>
        /// Removes the connection.
        /// </summary>
        /// <param name="connections">The connections.</param>
        /// <param name="fromVertex">From vertex.</param>
        /// <param name="toVertex">To vertex.</param>
        /// <exception cref="System.Exception"></exception>
        private static void RemoveConnection(Dictionary<Vertex2D, List<Vertex2D>> connections, Vertex2D fromVertex, Vertex2D toVertex)
        {
            var toVertices = connections[fromVertex];
            if (toVertices.Count == 1)
            {
                if (toVertices[0] == toVertex)
                    connections.Remove(fromVertex);
                else throw new Exception();
            }
            else toVertices.Remove(toVertex);
        }


        /// <summary>
        /// Finds the closest lower datum.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <param name="point">The point.</param>
        /// <param name="minfeasible">The minfeasible.</param>
        /// <returns>PolygonEdge.</returns>
        private static PolygonEdge FindClosestLowerDatum(IEnumerable<PolygonEdge> edges, Vector2 point, double minfeasible = 0.0)
        {
            var numEdges = 0;
            var closestDistance = double.PositiveInfinity;
            PolygonEdge closestEdge = null;
            foreach (var edge in edges)
            {
                numEdges++;
                var intersectionYValue = edge.FindYGivenX(point.X, out var betweenPoints);
                if (!betweenPoints) continue;
                var delta = point.Y - intersectionYValue;
                if (delta >= minfeasible && delta < closestDistance)
                {
                    closestDistance = delta;
                    closestEdge = edge;
                }
            }

            //if (closestEdge == null && numEdges > 0) return FindClosestLowerDatum(edges, point, double.NegativeInfinity);
            return closestEdge;
        }



        /// <summary>
        /// Triangulates the monotone polygon.
        /// </summary>
        /// <param name="monoPoly">The mono poly.</param>
        /// <returns>IEnumerable&lt;Vertex2D[]&gt;.</returns>
        private static IEnumerable<Vertex2D[]> TriangulateMonotonePolygon(Polygon monoPoly)
        {
            monoPoly.MakePolygonEdgesIfNonExistent();
            if (monoPoly.Vertices.Count < 3) yield break;
            if (monoPoly.Vertices.Count == 3)
            {
                yield return new[] { monoPoly.Vertices[0], monoPoly.Vertices[1], monoPoly.Vertices[2] };
                yield break;
            }
            Vertex2D bottomVertex = monoPoly.Vertices[0]; // Q: why is this called bottom and not leftmost?
            // A: because in the loop below it becomes the vertex on the bottom branch of the polygon
            foreach (var vertex in monoPoly.Vertices.Skip(1))
                if (bottomVertex.X > vertex.X || (bottomVertex.X == vertex.X && bottomVertex.Y > vertex.Y))
                    bottomVertex = vertex;
            var topVertex = bottomVertex; //initialize top to the same as bottom
            var concaveFunnelStack = new Stack<Vertex2D>();
            concaveFunnelStack.Push(bottomVertex);
            var nextVertex = NextXVertex(ref bottomVertex, ref topVertex, out var belongsToBottom);
            concaveFunnelStack.Push(nextVertex);

            do
            {
                nextVertex = NextXVertex(ref bottomVertex, ref topVertex, out var newVertexIsOnBottom);
                if (newVertexIsOnBottom == belongsToBottom)
                {
                    Vertex2D vertex1 = concaveFunnelStack.Pop();
                    Vertex2D vertex2 = concaveFunnelStack.Pop();
                    while (vertex2 != null && newVertexIsOnBottom ==
                        (vertex1.Coordinates - nextVertex.Coordinates).Cross(vertex2.Coordinates - vertex1.Coordinates) < 0)
                    {
                        if (newVertexIsOnBottom)
                            yield return new[] { nextVertex, vertex2, vertex1 };
                        else yield return new[] { nextVertex, vertex1, vertex2 };
                        vertex1 = vertex2;
                        vertex2 = concaveFunnelStack.Any() ? concaveFunnelStack.Pop() : null;
                    }
                    if (vertex2 != null) concaveFunnelStack.Push(vertex2);
                    concaveFunnelStack.Push(vertex1);
                    concaveFunnelStack.Push(nextVertex);
                }
                else //connect this to all on the stack
                {
                    Vertex2D topOfStackVertex = null;
                    Vertex2D prevVertex2 = null;
                    while (concaveFunnelStack.Any())
                    {
                        var prevVertex1 = concaveFunnelStack.Pop();
                        topOfStackVertex ??= prevVertex1;
                        if (prevVertex2 != null)
                        {
                            if (newVertexIsOnBottom)
                                yield return new[] { nextVertex, prevVertex2, prevVertex1 };
                            else yield return new[] { nextVertex, prevVertex1, prevVertex2 };
                        }
                        prevVertex2 = prevVertex1;
                    }
                    concaveFunnelStack.Push(topOfStackVertex);
                    concaveFunnelStack.Push(nextVertex);
                    belongsToBottom = newVertexIsOnBottom;
                }
            } while (bottomVertex != null);
        }

        /// <summary>
        /// Nexts the x vertex.
        /// </summary>
        /// <param name="bottomVertex">The bottom vertex.</param>
        /// <param name="topVertex">The top vertex.</param>
        /// <param name="belongsToBottom">if set to <c>true</c> [belongs to bottom].</param>
        /// <returns>Vertex2D.</returns>
        private static Vertex2D NextXVertex(ref Vertex2D bottomVertex, ref Vertex2D topVertex, out bool belongsToBottom)
        {
            var nextTopVertex = topVertex.EndLine.FromPoint;
            var nextBottomVertex = bottomVertex.StartLine.ToPoint;
            if (nextTopVertex == nextBottomVertex)
            {
                topVertex = bottomVertex = null;
                belongsToBottom = false;
                return nextTopVertex;
            }
            if (nextBottomVertex.X <= nextTopVertex.X)
            {
                belongsToBottom = true;
                bottomVertex = nextBottomVertex;
                return bottomVertex;
            }
            belongsToBottom = false;
            topVertex = nextTopVertex;
            return topVertex;
        }

        /// <summary>
        /// Combines the sorted vertex lists.
        /// </summary>
        /// <param name="leftCollection">The left collection.</param>
        /// <param name="rightCollection">The right collection.</param>
        /// <param name="comparer">The comparer.</param>
        /// <returns>IEnumerable&lt;Vertex2D&gt;.</returns>
        private static IEnumerable<Vertex2D> CombineSortedVertexLists(IEnumerable<Vertex2D> leftCollection, IEnumerable<Vertex2D> rightCollection,
            IComparer<Vertex2D> comparer)
        {
            var leftEnumerator = leftCollection.GetEnumerator();
            var rightEnumerator = rightCollection.GetEnumerator();
            var leftContinues = leftEnumerator.MoveNext();
            var rightContinues = rightEnumerator.MoveNext();
            while (leftContinues || rightContinues)
            {
                if (!rightContinues ||
                    leftContinues && comparer.Compare(leftEnumerator.Current, rightEnumerator.Current) <= 0)
                {
                    yield return leftEnumerator.Current;
                    leftContinues = leftEnumerator.MoveNext();
                }
                else
                {
                    yield return rightEnumerator.Current;
                    rightContinues = rightEnumerator.MoveNext();
                }
            }
        }
        #endregion

        #region Triangulate via Delaunay. This produce better quality triangles, but is slower and more complex.

        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <param name="forceToPositive">if set to <c>true</c> [force to positive].</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="System.ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static IEnumerable<(Vertex A, Vertex B, Vertex C)> TriangulateDelaunay(this IEnumerable<Vertex> vertexLoop,
            Vector3 normal, bool forceToPositive = false, bool handleSelfIntersects = true, double suggestedAngle = 0.0)
        {
            var transform = normal.TransformToXYPlane(out _);
            var coords = new List<Vertex2D>();
            var indexToVertexDict = new Dictionary<int, Vertex>();
            foreach (var vertex in vertexLoop)
            {
                coords.Add(new Vertex2D(vertex.ConvertTo2DCoordinates(transform), vertex.IndexInList, -1));
                if (indexToVertexDict.ContainsKey(vertex.IndexInList))
                    throw new ArgumentException("The vertices must all have a unique IndexInList value", nameof(vertexLoop));
                indexToVertexDict.Add(vertex.IndexInList, vertex);
            }
            var polygon = new Polygon(coords);
            if (forceToPositive && !polygon.IsPositive) polygon.IsPositive = true;
            foreach (var triangleIndices in polygon.TriangulateToIndicesDelaunay(handleSelfIntersects, suggestedAngle))
            {
                if (indexToVertexDict[triangleIndices.A] != indexToVertexDict[triangleIndices.B]
                    && indexToVertexDict[triangleIndices.B] != indexToVertexDict[triangleIndices.C]
                    && indexToVertexDict[triangleIndices.C] != indexToVertexDict[triangleIndices.A])
                    yield return (
                        indexToVertexDict[triangleIndices.A], indexToVertexDict[triangleIndices.B],
                        indexToVertexDict[triangleIndices.C]);
            }
        }

        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoops">The vertex loops.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="System.ArgumentException">The vertices must all have a unique IndexInList value - vertexLoops</exception>
        public static IEnumerable<Vertex[]> TriangulateDelaunay(this IEnumerable<IList<Vertex>> vertexLoops, Vector3 normal,
            bool handleSelfIntersects = true, double suggestedAngle = 0.0)
        {
            var transform = normal.TransformToXYPlane(out _);
            var polygons = new List<Polygon>();
            var indexToVertexDict = new Dictionary<int, Vertex>();
            foreach (var vertexLoop in vertexLoops)
            {
                var coords = new List<Vertex2D>();
                foreach (var vertex in vertexLoop)
                {
                    coords.Add(new Vertex2D(vertex.ConvertTo2DCoordinates(transform), vertex.IndexInList, -1));
                    if (indexToVertexDict.ContainsKey(vertex.IndexInList))
                        throw new ArgumentException("The vertices must all have a unique IndexInList value", nameof(vertexLoops));
                    indexToVertexDict.Add(vertex.IndexInList, vertex);
                }
                polygons.Add(new Polygon(coords));
            }
            polygons = polygons.CreateShallowPolygonTrees(false);
            foreach (var polygon in polygons)
            {
                foreach (var triangleIndices in polygon.TriangulateToIndicesDelaunay(handleSelfIntersects, suggestedAngle))
                    yield return new[]
                        {indexToVertexDict[triangleIndices.A], indexToVertexDict[triangleIndices.B],
                        indexToVertexDict[triangleIndices.C]};
            }
        }


        /// <summary>
        /// Triangulates the specified polygons which may include holes. However, the .
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        public static IEnumerable<Vector2[]> TriangulateToCoordinatesDelaunay(this Polygon polygon, bool handleSelfIntersects = true,
            double suggestedAngle = 0.0)
        {
            foreach (var triangle in polygon.TriangulateDelaunay(out var mapping, false, false).Faces)
                yield return new[] { new Vector2(triangle.A.X,triangle.A.Y),
                    new Vector2(triangle.B.X,triangle.B.Y),
                    new Vector2(triangle.C.X,triangle.C.Y) };
        }

        /// <summary>
        /// Triangulates the specified polygons which may include holes. However, the .
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="handleSelfIntersects">if set to <c>true</c> [handle self intersects].</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        public static IEnumerable<(int A, int B, int C)> TriangulateToIndicesDelaunay(this Polygon polygon, bool handleSelfIntersects = true,
            double suggestedAngle = 0.0)
        {
            var vertexIndices = new HashSet<int>();
            var index = 0;
            foreach (var subPolygon in polygon.AllPolygons)
                foreach (var vertex in subPolygon.Vertices)
                    vertex.IndexInList = index++;
            foreach (var triangle in polygon.TriangulateDelaunay(out var mapping, false, false).Faces)
                yield return (triangle.A.IndexInList, triangle.B.IndexInList, triangle.C.IndexInList);
        }
        /// <summary>
        /// Triangulates the specified polygons which may include holes.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="handleSelfIntersects">if set to <c>true</c> [handle self intersects].</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        /// <exception cref="System.ArgumentException">Triangulate Polygon requires a positive polygon. A negative one was provided. - polygon</exception>
        /// <exception cref="System.Exception">Unable to triangulate polygon.</exception>
        public static Delaunay2D TriangulateDelaunay(this Polygon polygon, out Dictionary<Vertex, Vertex2D> mappingToPolygonVertices,
            bool allowNewPolygonPoints, bool preservePolygonEdgesInTriangulation, int targetNumTriangles = -1, double targetSideLength = double.NaN)
        {
            if (targetNumTriangles > 0 && targetSideLength > 0)
                throw new ArgumentException("Cannot specify both a target number of triangles and a target side length.");
            if (targetNumTriangles <= 0 && !(targetSideLength > 0))
                throw new ArgumentException("Must specify either a target number of triangles or a target side length.");
            if (targetNumTriangles > 0)
            {
                var targetTriangleArea = polygon.Area / targetNumTriangles;
                // assuming an equilateral triangle, the area is (sqrt(3)/4)*sideLength^2, so sideLength = 2*sqrt(area/sqrt(3))
                targetSideLength = 2 * Math.Sqrt(targetTriangleArea / Math.Sqrt(3));
            }
            if (!polygon.IsPositive)
                throw new ArgumentException("Triangulate Polygon requires a positive polygon. A negative one was provided.", nameof(polygon));

            if (allowNewPolygonPoints)
                polygon.Complexify(targetSideLength);
            polygon.MakePolygonEdgesIfNonExistent();

            mappingToPolygonVertices = new Dictionary<Vertex, Vertex2D>();
            var allVertices = new List<Vertex>();
            var constraintIndices = new List<(int From, int To)>();
            var vertID = 0;
            foreach (var v2d in polygon.AllPolygons.SelectMany(p => p.Vertices))
            {
                v2d.IndexInList = vertID++;
                var v3D = new Vertex(v2d.X, v2d.Y, 0, v2d.IndexInList);
                allVertices.Add(v3D);
                mappingToPolygonVertices.Add(v3D, v2d);
            }
            if (preservePolygonEdgesInTriangulation || allowNewPolygonPoints)
            {
                foreach (var edge in polygon.AllPolygons.SelectMany(p => p.Edges))
                    constraintIndices.Add((edge.FromPoint.IndexInList, edge.ToPoint.IndexInList));
            }
            else // then we need to make new points along the polygon, but not alter the original polygon
            {
                foreach (var edge in polygon.AllPolygons.SelectMany(p => p.Edges))
                {
                    var fromIndex = edge.FromPoint.IndexInList;
                    if (edge.Length > targetSideLength)
                    {
                        var numNewPoints = (int)(edge.Length / targetSideLength);
                        for (int j = 0; j < numNewPoints; j++)
                        {
                            var fraction = j / (double)numNewPoints;
                            var newCoordinates = fraction * edge.FromPoint.Coordinates + ((1 - fraction) * edge.ToPoint.Coordinates);
                            var newIntermediateVert = new Vertex(newCoordinates.X, newCoordinates.Y, 0, vertID++);
                            allVertices.Add(newIntermediateVert);
                            constraintIndices.Add((fromIndex, newIntermediateVert.IndexInList));
                            fromIndex = newIntermediateVert.IndexInList;
                        }
                    }
                    constraintIndices.Add((fromIndex, edge.ToPoint.IndexInList));
                }
            }
            allVertices.AddRange(polygon.FindInternalPointsOffset(targetSideLength).Select(p => new Vertex(p.X, p.Y, 0, vertID++)));
            if (!RunConstrainedDelaunay(allVertices, constraintIndices, true, out var delaunay2D))
                throw new Exception("There was a problem with the triangulation.");
            return delaunay2D;
        }

        private static bool RunConstrainedDelaunay(List<Vertex> allVertices, List<(int From, int To)> constraintIndices,
            bool rebuildOnBothSidesOfConstraints, out Delaunay2D delaunay2D)
        {
            if (!Delaunay2D.Create(allVertices, out delaunay2D))
                return false;
            var faces = delaunay2D.Faces.ToList();
            // 1. Build a quick lookup for existing edges in the Delaunay Triangulation
            var edgeHash = new Dictionary<long, Edge>();
            foreach (var edge in delaunay2D.Edges)
                edgeHash.Add(edge.EdgeReference, edge);

            // terate through each constraint
            foreach (var constraint in constraintIndices)
            {
                var edgeChecksum = Edge.GetEdgeChecksum(allVertices[constraint.From], allVertices[constraint.To]);
                if (edgeHash.ContainsKey(edgeChecksum))
                    continue; // This constraint edge already exists in the Delaunay triangulation

                var newConstraintEdge = new Edge(allVertices[constraint.From], allVertices[constraint.To], false)
                { EdgeReference = edgeChecksum };
                edgeHash.Add(edgeChecksum, newConstraintEdge);
                var cFrom = new Vector2(newConstraintEdge.From.X, newConstraintEdge.From.Y);
                var cTo = new Vector2(newConstraintEdge.To.X, newConstraintEdge.To.Y);
                // 3. Find all existing edges that intersect this constraint line segment and remove them
                var crossingEdges = new List<Edge>();
                foreach (var edge in edgeHash.Values)
                {
                    var eFrom = new Vector2(edge.From.X, edge.From.Y);
                    var eTo = new Vector2(edge.To.X, edge.To.Y);
                    if (newConstraintEdge.From != edge.From && newConstraintEdge.To != edge.To &&
                        newConstraintEdge.From != edge.To && newConstraintEdge.To != edge.From &&
                        MiscFunctions.SegmentSegment2DIntersection(cFrom, cTo, eFrom, eTo, out _, out _, out _))
                    {
                        crossingEdges.Add(edge);
                        // Delete the intersecting edges and their associated two triangles from the mesh.
                        if (edge.OwnedFace != null) faces.Remove(edge.OwnedFace);
                        if (edge.OtherFace != null) faces.Remove(edge.OtherFace);
                    }
                }
                var tempNewFaces = new List<TriangleFace>();
                var tempNewEdges = new List<Edge>();
                // from "Fast Segment Insertion and Incremental Construction of Constrained Delaunay Triangulations"
                // by Jonathan Richard Shewchuk and Bintami C.Brown(2015).
                // Loop through this list of crossing edges.
                while (crossingEdges.Count > 0)
                {  // For each edge, look at the quadrilateral formed by its two adjacent triangles.
                   // If that quadrilateral is convex, flip the edge. If the flipped edge no longer intersects
                   // the constraint edge, remove it from your crossing list. If it still intersects, or if the
                   // quad was concave, leave it and move to the next edge in the list.
                   // With every successful flip, the total number of edges intersecting your constraint edge strictly decreases.
                    for (var i = crossingEdges.Count - 1; i >= 0; i--)
                    {
                        var crossingEdge = crossingEdges[i];
                        var pTo = new Vector2(crossingEdge.To.X, crossingEdge.To.Y);
                        var pFrom = new Vector2(crossingEdge.From.X, crossingEdge.From.Y);
                        var vOwned = crossingEdge.OwnedFace.OtherVertex(crossingEdge);
                        var pOwned = new Vector2(vOwned.X, vOwned.Y);
                        var vOther = crossingEdge.OtherFace.OtherVertex(crossingEdge);
                        var pOther = new Vector2(vOther.X, vOther.Y);
                        // first check if new flipped edge would still intersect the constraint edge.
                        if (newConstraintEdge.From != vOwned && newConstraintEdge.To != vOwned &&
                            newConstraintEdge.From != vOther && newConstraintEdge.To != vOther &&
                            MiscFunctions.SegmentSegment2DIntersection(cFrom, cTo, pOwned, pOther, out _, out _, out _))
                            continue; // then this edge flip won't help so skip it
                        // new check if convex quadrilateral is formed by the two triangles adjacent to this edge.
                        // Necessarily, the corner at vOwned or vOther must already be convex.
                        // so, just need to check at pFrom or pTo, and only need to check if the cross product is negative
                        // (indicating a right turn and thus a convex corner) 
                        if ((pFrom - pOwned).Cross(pOther - pFrom) < 0 || (pTo - pOther).Cross(pOwned - pTo) < 0)
                            continue;
                        faces.Remove(crossingEdge.OwnedFace);
                        faces.Remove(crossingEdge.OtherFace);
                        var newOwnedFace = new TriangleFace(crossingEdge.From, vOther, vOwned, false);
                        var newOtherFace = new TriangleFace(crossingEdge.To, vOwned, vOther, false);
                        var newEdge = new Edge(vOther, vOwned, newOwnedFace, newOtherFace, false, Edge.GetEdgeChecksum(vOther, vOwned));
                        tempNewFaces.Add(newOwnedFace);
                        tempNewFaces.Add(newOtherFace);
                        tempNewEdges.Add(newEdge);
                        // also need to update the 4 quadrilateral edges to point to the new faces
                        foreach (var borderEdge in crossingEdge.OwnedFace.Edges.Concat(crossingEdge.OtherFace.Edges))
                        {
                            if (borderEdge == crossingEdge) continue;
                            if (borderEdge.From == crossingEdge.From || borderEdge.To == crossingEdge.From)
                            {
                                if (borderEdge.OwnedFace == crossingEdge.OwnedFace || borderEdge.OwnedFace == crossingEdge.OtherFace)
                                    borderEdge.OwnedFace = newOwnedFace;
                                else borderEdge.OtherFace = newOwnedFace;
                                newOwnedFace.AddEdge(borderEdge);
                            }
                            else //if (borderEdge.From == crossingEdge.To || borderEdge.To == crossingEdge.To)
                            {
                                if (borderEdge.OwnedFace == crossingEdge.OwnedFace || borderEdge.OwnedFace == crossingEdge.OtherFace)
                                    borderEdge.OwnedFace = newOtherFace;
                                else borderEdge.OtherFace = newOtherFace;
                                newOtherFace.AddEdge(borderEdge);
                            }
                        }
                        edgeHash.Remove(crossingEdge.EdgeReference);
                        crossingEdges.RemoveAt(i);
                    }
                }
                var newConstraintOutDir = new Vector3(newConstraintEdge.Vector.Y, -newConstraintEdge.Vector.X, 0);
                var cFrom3D = newConstraintEdge.From.Coordinates;
                foreach (var face in tempNewFaces)
                {
                    if (rebuildOnBothSidesOfConstraints || face.Vertices.All(v => (v.Coordinates - cFrom3D).Dot(newConstraintOutDir) <= 0))
                        faces.Add(face);
                }
                foreach (var edge in tempNewEdges)
                {
                    if (rebuildOnBothSidesOfConstraints ||
                        ((edge.From.Coordinates - cFrom3D).Dot(newConstraintOutDir) <= 0 &&
                        (edge.To.Coordinates - cFrom3D).Dot(newConstraintOutDir) <= 0))
                        edgeHash.Add(edge.EdgeReference, edge);
                }
            }
            delaunay2D = new Delaunay2D
            {
                Vertices = delaunay2D.Vertices,
                Faces = faces.ToArray(),
                Edges = edgeHash.Values.ToArray()
            };
            return true;
        }

        private static void RecursiveDelaunay(List<Vertex> vertices, Edge startingEdge, Vector2 edgeFrom, Vector2 edgeTo,
            Dictionary<long, Edge> edgeHash, List<TriangleFace> faces)
        {
            var bestVertices = vertices.ToList();
            bestVertices.Remove(startingEdge.From);
            bestVertices.Remove(startingEdge.To);
            var candidateVertices = bestVertices.ToArray();
            for (var i = 0; i < bestVertices.Count; i++)
            {
                var vI = bestVertices[i];
                var v2D = new Vector2(vI.X, vI.Y);
                if (!Circle.CreateFrom3Points(edgeFrom, edgeTo, v2D, out var circle))
                    continue;
                foreach (var vJ in candidateVertices)
                {
                    if (vJ == startingEdge.From)
                        continue;

                }
            }

        #endregion
        }
    }
}