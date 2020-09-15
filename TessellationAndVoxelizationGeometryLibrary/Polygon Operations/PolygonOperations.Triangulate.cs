using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Triangulates a Polygon into faces in O(n log n) time.
    /// </summary>
    ///  <References>
    ///     The new approach is based on how it is presented in 
    ///     the book
    ///     "Computational geometry: algorithms and applications". 2000
    ///     Authors: de Berg, Mark and van Kreveld, Marc and Overmars, Mark and Schwarzkopf, Otfried and Overmars, M
    /// </References>
    /// A good summary of how the monotone polygons are created can be seen in the video: https://youtu.be/IkA-2Y9lBvM
    /// and the algorithm for triangulating the monotone polygons can be found here: https://youtu.be/pfXXgV9u6cw
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static IEnumerable<Vertex[]> Triangulate(this IEnumerable<Vertex> vertexLoop, Vector3 normal)
        {
            var transform = normal.TransformToXYPlane(out _);
            var coords = new List<Vertex2D>();
            var indexToVertexDict = new Dictionary<int, Vertex>();
            foreach (var vertex in vertexLoop)
            {
                coords.Add(new Vertex2D(vertex.ConvertTo2DCoordinates(transform), vertex.IndexInList, -1));
                if (indexToVertexDict.ContainsKey(vertex.IndexInList))
                    throw new ArgumentException("The vertices must all have a unique IndexInList value", "vertexLoop");
                indexToVertexDict.Add(vertex.IndexInList, vertex);
            }
            var polygon = new Polygon(coords);
            foreach (var triangleIndices in polygon.Triangulate(false))
                yield return new[]
                    {indexToVertexDict[triangleIndices[0]], indexToVertexDict[triangleIndices[1]],
                        indexToVertexDict[triangleIndices[2]]};
        }
        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoops">The vertex loops.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static IEnumerable<Vertex[]> Triangulate(this IEnumerable<IList<Vertex>> vertexLoops, Vector3 normal)
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
                        throw new ArgumentException("The vertices must all have a unique IndexInList value", "vertexLoop");
                    indexToVertexDict.Add(vertex.IndexInList, vertex);
                }
                polygons.Add(new Polygon(coords));
            }
            polygons = polygons.CreateShallowPolygonTrees(false, out _);
            foreach (var polygon in polygons)
            {
                foreach (var triangleIndices in polygon.Triangulate(false))
                    yield return new[]
                        {indexToVertexDict[triangleIndices[0]], indexToVertexDict[triangleIndices[1]],
                        indexToVertexDict[triangleIndices[2]]};
            }
        }


        /// <summary>
        /// Triangulates the specified polygons which may include holes. However, the .
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="reIndexPolygons">if set to <c>true</c> [re index polygons].</param>
        /// <returns>List&lt;System.Int32[]&gt;.</returns>
        public static List<int[]> Triangulate(this Polygon polygon, bool reIndexPolygons = true)
        {
            if (!polygon.IsPositive)
                throw new ArgumentException("Triangulate Polygon requires a positive polygon. A negative one was provided.", "polygon");
            const int maxNumberOfAttempts = 3;
            var random = new Random(1);
            var randomAngles = new double[maxNumberOfAttempts];
            for (int i = 1; i < maxNumberOfAttempts; i++)
                randomAngles[i] = 2 * Math.PI * random.NextDouble();
            var numVertices = -1;
            if (reIndexPolygons)
            {
                var index = 0;
                foreach (var subPolygon in polygon.AllPolygons)
                    foreach (var vertex in subPolygon.Vertices)
                        vertex.IndexInList = index++;
                numVertices = index;
            }
            else numVertices = polygon.AllPolygons.Sum(polygon => polygon.Vertices.Count);

            if (numVertices <= 2) return new List<int[]>();
            if (numVertices == 3) return new List<int[]> { polygon.Vertices.Select(v => v.IndexInList).ToArray() };
            if (numVertices == 4)
            {
                var verts = polygon.Vertices.Select(v => v.IndexInList).ToList();
                var concaveEdge = polygon.Vertices.FirstOrDefault(v => v.EndLine.Vector.Cross(v.StartLine.Vector) < 0);
                if (concaveEdge != null)
                {
                    while (verts[0] != concaveEdge.IndexInList)
                    {
                        verts.Add(concaveEdge.IndexInList);
                        verts.RemoveAt(0);
                    }
                }
                return new List<int[]> { new[] { verts[0], verts[1], verts[2] }, new[] { verts[0], verts[2], verts[3] } };
            }
            var triangleFaceList = new List<int[]>();
            // this is the returned list of triangles. Well, not actually triangles but three integers each - corresponding
            // to the 3 indices of the input polygon's Vertex2D

            // in case this is a deep polygon tree - recurse down and solve for the inner positive polygons
            foreach (var hole in polygon.InnerPolygons)
                foreach (var smallInnerPolys in hole.InnerPolygons)
                    triangleFaceList.AddRange(Triangulate(smallInnerPolys, false));

            foreach (var randomAngle in randomAngles)
            {
                try
                {
                    var localTriangleFaceList = new List<int[]>();
                    if (randomAngle != 0)
                        polygon.Transform(Matrix3x3.CreateRotation(randomAngle));
                    foreach (var monoPoly in MakeXMonotonePolygons(polygon))
                    {
                        localTriangleFaceList.AddRange(TriangulateMonotonePolygon(monoPoly));
                    }
                    triangleFaceList.AddRange(localTriangleFaceList);
                    return triangleFaceList;
                }
                catch (Exception exception)
                {
                    if (randomAngle != 0)
                        polygon.Transform(Matrix3x3.CreateRotation(-randomAngle));
                }
            }
            return null;
        }

        private static IEnumerable<Polygon> MakeXMonotonePolygons(Polygon polygon)
        {
            var newEdgeDict = FindInternalDiagonalsForMonotone(polygon);
            var unVisitedVertices = polygon.Vertices.ToHashSet();
            foreach (var vert in polygon.InnerPolygons.SelectMany(p => p.Vertices))
                unVisitedVertices.Add(vert);
            while (unVisitedVertices.Any())
            {
                var newVertices = new List<Vertex2D>();
                var start = newVertices.First();
                var current = start;
                do
                {
                    newVertices.Add(current.Copy());
                    unVisitedVertices.Remove(current);
                    if (newEdgeDict.ContainsKey(current))
                    {
                        current = newEdgeDict[current];
                        newEdgeDict.Remove(current);
                    }
                    else current = current.StartLine.ToPoint;
                } while (current != start);
                yield return new Polygon(newVertices);
            }
        }

        private static Dictionary<Vertex2D, Vertex2D> FindInternalDiagonalsForMonotone(Polygon polygon)
        {
            var orderedListsOfVertices = new List<List<Vertex2D>>();
            orderedListsOfVertices.Add(polygon.OrderedXVertices);
            foreach (var hole in polygon.InnerPolygons)
                orderedListsOfVertices.Add(hole.OrderedXVertices);
            var sortedVertices = CombineSortedVerticesIntoOneCollection(orderedListsOfVertices);
            var newEdgeDict = new Dictionary<Vertex2D, Vertex2D>();
            var edgeDatums = new Dictionary<PolygonEdge, (Vertex2D, bool)>();
            foreach (var vertex in sortedVertices)
            {
                var monoChange = GetMonotonicityChange(vertex);
                var cornerCross = vertex.EndLine.Vector.Cross(vertex.StartLine.Vector);
                if (monoChange == MonotonicityChange.Neither || monoChange == MonotonicityChange.Y)
                // then it's regular
                {
                    if (vertex.StartLine.Vector.X > 0)
                    {
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                        edgeDatums.Add(vertex.StartLine, (vertex, false));
                    }
                    else
                    {
                        PolygonEdge closestDatum = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, closestDatum, vertex);
                        edgeDatums[closestDatum] = (vertex, false);
                    }
                }
                else if (cornerCross > 0) //then either start or end
                {
                    if (vertex.StartLine.Vector.X >= 0 && vertex.EndLine.Vector.X <= 0) // then start
                        edgeDatums.Add(vertex.StartLine, (vertex, false));
                    else // then it's an end
                    {
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                    }
                }
                else //then either split or merge
                {
                    if (vertex.StartLine.Vector.X >= 0 && vertex.EndLine.Vector.X <= 0) // then split
                    {
                        PolygonEdge closestDatum = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, closestDatum, vertex);
                        edgeDatums[closestDatum] = (vertex, false);
                        edgeDatums[vertex.StartLine] = (vertex, false);

                    }
                    else //the it's a merge
                    {
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, vertex.EndLine, vertex);
                        edgeDatums.Remove(vertex.EndLine);
                        PolygonEdge closestDatum = FindClosestLowerDatum(edgeDatums.Keys, vertex.Coordinates);
                        MakeNewDiagonalEdgeIfMerge(newEdgeDict, edgeDatums, closestDatum, vertex);
                        edgeDatums[closestDatum] = (vertex, false);
                    }
                }
            }
            return newEdgeDict;
        }

        private static void MakeNewDiagonalEdgeIfMerge(Dictionary<Vertex2D, Vertex2D> newEdgeDict,
            Dictionary<PolygonEdge, (Vertex2D, bool)> edgeDatums, PolygonEdge datum, Vertex2D vertex)
        {
            var prevLineHelperData = edgeDatums[datum];
            var helperVertex = prevLineHelperData.Item1;
            var isMergePoint = prevLineHelperData.Item2;
            if (isMergePoint) //if this was a merge point
            {
                newEdgeDict.Add(vertex, helperVertex);
                newEdgeDict.Add(helperVertex, vertex);
            }
        }


        private static PolygonEdge FindClosestLowerDatum(IEnumerable<PolygonEdge> edges, Vector2 point)
        {
            var closestDistance = double.PositiveInfinity;
            PolygonEdge closestEdge = null;
            foreach (var edge in edges)
            {
                var intersectionYValue = edge.YGivenX(point.X, out var betweenPoints);
                if (!betweenPoints) continue;
                var delta = point.Y - intersectionYValue;
                if (delta >= 0 && delta < closestDistance)
                {
                    closestDistance = delta;
                    closestEdge = edge;
                }
            }
            return closestEdge;
        }


        private static IEnumerable<Vertex2D> CombineSortedVerticesIntoOneCollection(List<List<Vertex2D>> orderedListsOfVertices)
        {
            var numLists = orderedListsOfVertices.Count;
            var currentIndices = new int[numLists];
            while (true)
            {
                var lowestXValue = double.PositiveInfinity;
                var lowestYValue = double.PositiveInfinity;
                var lowestEntry = -1;
                for (int i = 0; i < numLists; i++)
                {
                    var index = currentIndices[i];
                    if (orderedListsOfVertices[i].Count <= index) continue;
                    var vertex = orderedListsOfVertices[i][index];
                    if (vertex.X < lowestXValue ||
                        (vertex.X == lowestXValue && vertex.Y < lowestYValue))
                    {
                        lowestXValue = vertex.X;
                        lowestYValue = vertex.Y;
                        lowestEntry = i;
                    }
                }
                if (lowestEntry == -1) yield break;
                yield return orderedListsOfVertices[lowestEntry][currentIndices[lowestEntry]];
                currentIndices[lowestEntry]++;
            }
        }

        private static IEnumerable<int[]> TriangulateMonotonePolygon(Polygon monoPoly)
        {
            Vertex2D bottomVertex = monoPoly.Vertices[0];
            foreach (var vertex in monoPoly.Vertices.Skip(1))
                if (bottomVertex.X > vertex.X || (bottomVertex.X == vertex.X && bottomVertex.Y > vertex.Y))
                    bottomVertex = vertex;
            var topVertex = bottomVertex;
            var nextVertex = NextXVertex(ref bottomVertex, ref topVertex, out var belongsToBottom);

            var concaveFunnelStack = new Stack<Vertex2D>();
            concaveFunnelStack.Push(bottomVertex);
            concaveFunnelStack.Push(nextVertex);

            do
            {
                nextVertex = NextXVertex(ref bottomVertex, ref topVertex, out var newVertexIsOnBottom);
                if (newVertexIsOnBottom == belongsToBottom)
                {
                    Vertex2D vertex1 = concaveFunnelStack.Pop();
                    Vertex2D vertex2 = concaveFunnelStack.Pop();
                    while (vertex1 != null &&
                        (vertex1.Coordinates - nextVertex.Coordinates).Cross(vertex2.Coordinates - vertex1.Coordinates) > 0)
                    {
                        if (belongsToBottom)
                            yield return new[] { nextVertex.IndexInList, vertex2.IndexInList, vertex1.IndexInList };
                        else yield return new[] { nextVertex.IndexInList, vertex1.IndexInList, vertex2.IndexInList };
                        vertex2 = vertex1;
                        if (!concaveFunnelStack.Any()) vertex1 = concaveFunnelStack.Pop();
                        else vertex1 = null;
                    }
                    concaveFunnelStack.Push(vertex2);
                    if (vertex1 != null) concaveFunnelStack.Push(vertex1);
                    concaveFunnelStack.Push(nextVertex);
                }
                else //connect this to all on the stack
                {
                    Vertex2D topOfStackVertex = null;
                    Vertex2D prevVertex2 = null;
                    while (!concaveFunnelStack.Any())
                    {
                        var prevVertex1 = concaveFunnelStack.Pop();
                        if (topOfStackVertex == null) topOfStackVertex = prevVertex1;
                        if (prevVertex2 != null)
                        {
                            if (belongsToBottom)
                                yield return new[] { nextVertex.IndexInList, prevVertex2.IndexInList, prevVertex1.IndexInList };
                            else yield return new[] { nextVertex.IndexInList, prevVertex1.IndexInList, prevVertex2.IndexInList };
                        }
                        prevVertex2 = prevVertex1;
                    }
                    concaveFunnelStack.Push(topOfStackVertex);
                    concaveFunnelStack.Push(nextVertex);
                    belongsToBottom = newVertexIsOnBottom;
                }
            } while (bottomVertex != null);
        }

        private static Vertex2D NextXVertex(ref Vertex2D topVertex, ref Vertex2D bottomVertex, out bool belongsToBottom)
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

    }
}