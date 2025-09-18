// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 12-08-2024
//
// Last Modified By : matth
// Last Modified On : 12-08-2024
// ***********************************************************************
// <copyright file="PolygonOperations.Minkowski.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        public static List<Polygon> ArrangementUnion(this IEnumerable<(Vector2, Vector2)> arrangement)
        {
            BuildArrangementGraph(arrangement, out var initNodeDict, out var edges);
            //Global.Presenter2D.ShowAndHang(edges.Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            SplitArrangementEdgesAtIntersections(initNodeDict, edges);
            var nodeList = initNodeDict.Values.ToList();
            PruneIsolatedArrangementNodes(nodeList);
            //Global.Presenter2D.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            RemoveDominatedArrangementEdges(nodeList);
            //Global.Presenter2D.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            PruneIsolatedArrangementNodes(nodeList);
            //Global.Presenter2D.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            var polygons = ExtractPolygonsFromArrangementNodes(nodeList);
            //Global.Presenter2D.ShowAndHang(polygons);

            return polygons.CreateShallowPolygonTrees(true);
        }
        private static void BuildArrangementGraph(IEnumerable<(Vector2, Vector2)> arrangement, out Dictionary<PointKey, ArrangementNode> initNodeDict, out List<PolygonEdge> edges)
        {
            initNodeDict = new Dictionary<PointKey, ArrangementNode>(); // store the nodes by their point key. We will keep finding new nodes later when we look for intersections
                                                                        // so, this entire dictionary is returned
            edges = new List<PolygonEdge>();   // these could be re-created by examining the nodes, but it is easier to keep track of them here

            foreach (var (from, to) in arrangement)
            {
                var fromKey = new PointKey(from);
                var toKey = new PointKey(to);
                var newNodeFound = false;
                if (!initNodeDict.TryGetValue(fromKey, out var fromNode))
                {   // then we haven't seen this node before, it is created here
                    fromNode = new ArrangementNode(fromKey, from);
                    initNodeDict.Add(fromKey, fromNode);
                    newNodeFound = true;
                }
                if (!initNodeDict.TryGetValue(toKey, out var toNode))
                {   // repeat for toNode
                    toNode = new ArrangementNode(toKey, to);
                    initNodeDict.Add(toKey, toNode);
                    newNodeFound = true;
                }
                if (!newNodeFound && fromNode.StartingEdges.Any(e => e.ToPoint == toNode))
                    continue; // then we have already added this edge
                var edge = new PolygonEdge(fromNode, toNode);
                edges.Add(edge);
                fromNode.StartingEdges.Add(edge);
                toNode.EndingEdges.Add(edge);
            }
        }

        // Tricky function to split arrangement edges at their intersections.
        private static void SplitArrangementEdgesAtIntersections(Dictionary<PointKey, ArrangementNode> initNodeDict, List<PolygonEdge> edges)
        {
            var edgeComparer = new EdgeComparer();  // sort by XMin, then YMin
            edges.Sort(edgeComparer);
            var intersections = new List<SegmentIntersection>(1);                    // only one intersection found at a time, but in order to use
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>(1);   // old function, AddIntersectionBetweenLines, we need to make lists
                                                                                     // as input. These are cleared and re-used each time.
            var othersToRemove = new List<PolygonEdge>();
            var newEdgesToAdd = new List<PolygonEdge>();
            while (edges.Count > 0)
            {
                var current = edges[0];  // hmm, why not use a priority queue? Because of the following foreach which looks at the
                edges.RemoveAt(0);       // others edges ahead in the queue.
                othersToRemove.Clear();
                newEdgesToAdd.Clear();
                var keepCheckingCurrentNotDeleted = false;
                foreach (var other in edges)
                {
                    if (current.XMax < other.XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    if (current.FromPoint == other.FromPoint) continue;
                    var endsAreTheSame = current.ToPoint == other.ToPoint;
                    intersections.Clear(); possibleDuplicates.Clear();
                    if (!AddIntersectionBetweenLines(current, other, intersections, possibleDuplicates, 9, false, false))
                        continue;
                    var intersection = intersections[0];
                    if (endsAreTheSame && intersection.WhereIntersection == WhereIsIntersection.Intermediate)
                        // then lines were not parallel and just at tip
                        continue;
                    var intersectionPointKey = new PointKey(intersection.IntersectCoordinates);
                    if (!initNodeDict.TryGetValue(intersectionPointKey, out var intersectNode))
                    {
                        intersectNode = new ArrangementNode(intersectionPointKey, intersection.IntersectCoordinates);
                        initNodeDict.Add(intersectionPointKey, intersectNode);
                    }
                    // shouldn't have any WhereIntersections set to WhereIsIntersection.BothStarts
                    if (intersection.WhereIntersection == WhereIsIntersection.AtStartOfA)
                    {
                        if (current == intersection.EdgeA)
                            keepCheckingCurrentNotDeleted = true;
                    }
                    else
                    {   // split edge A
                        var oldEdgeA = intersection.EdgeA;
                        if (oldEdgeA == other) othersToRemove.Add(other);
                        var aFromNode = (ArrangementNode)oldEdgeA.FromPoint;
                        var newEdgeA = new PolygonEdge(aFromNode, intersectNode);
                        aFromNode.StartingEdges.Remove(oldEdgeA);
                        // often - due to collinearity, we end up added a new edge that already exists
                        if (!aFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                        {
                            newEdgesToAdd.Add(newEdgeA);
                            aFromNode.StartingEdges.Add(newEdgeA);
                            intersectNode.EndingEdges.Add(newEdgeA);
                        }
                        var aToNode = (ArrangementNode)oldEdgeA.ToPoint;
                        newEdgeA = new PolygonEdge(intersectNode, aToNode);
                        aToNode.EndingEdges.Remove(oldEdgeA);
                        if (!aToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                        {
                            newEdgesToAdd.Add(newEdgeA);
                            aToNode.EndingEdges.Add(newEdgeA);
                            intersectNode.StartingEdges.Add(newEdgeA);
                        }
                    }
                    if (intersection.WhereIntersection == WhereIsIntersection.AtStartOfB)
                    {
                        if (current == intersection.EdgeB)
                            keepCheckingCurrentNotDeleted = true;
                    }
                    else
                    {   // split edge B
                        var oldEdgeB = intersection.EdgeB;
                        if (oldEdgeB == other) othersToRemove.Add(other);
                        var bFromNode = (ArrangementNode)oldEdgeB.FromPoint;
                        var newEdgeB = new PolygonEdge(bFromNode, intersectNode);
                        bFromNode.StartingEdges.Remove(oldEdgeB);
                        if (!bFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                        {
                            newEdgesToAdd.Add(newEdgeB);
                            bFromNode.StartingEdges.Add(newEdgeB);
                            intersectNode.EndingEdges.Add(newEdgeB);
                        }
                        var bToNode = (ArrangementNode)oldEdgeB.ToPoint;
                        newEdgeB = new PolygonEdge(intersectNode, bToNode);
                        bToNode.EndingEdges.Remove(oldEdgeB);
                        if (!bToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                        {
                            newEdgesToAdd.Add(newEdgeB);
                            bToNode.EndingEdges.Add(newEdgeB);
                            intersectNode.StartingEdges.Add(newEdgeB);
                        }
                    }
                    if (!keepCheckingCurrentNotDeleted)
                        break; // only handle one intersection at a time
                }
                foreach (var otherToRemove in othersToRemove)
                    edges.Remove(otherToRemove);
                foreach (var newEdge in newEdgesToAdd)
                    AddToSorted(edges, newEdge, edgeComparer);

            }
        }

        private static void RemoveDominatedArrangementEdges(List<ArrangementNode> nodes)
        {
            // remove edges that are dominated by others - in the case of union - these are the ones that are inside other polygons
            var edgesToRemove = new HashSet<PolygonEdge>();
            foreach (var node in nodes)
            {
                if (node.StartingEdges.Count <= 1 && node.EndingEdges.Count <= 1)
                    continue;
                // so, now we know that at least 3 edges come into this node
                var sortedEdges = new SortedList<double, (PolygonEdge, bool)>();
                for (int i = node.StartingEdges.Count - 1; i >= 0; i--)
                {
                    PolygonEdge edge = node.StartingEdges[i];
                    if (edge.ToPoint == edge.FromPoint) node.StartingEdges.RemoveAt(i); // remove self-referencing edges
                    else sortedEdges.Add(Global.Pseudoangle(edge.Vector.X, edge.Vector.Y), (edge, true));
                }

                for (int i = node.EndingEdges.Count - 1; i >= 0; i--)
                {
                    PolygonEdge edge = node.EndingEdges[i];
                    if (edge.ToPoint == edge.FromPoint) node.EndingEdges.RemoveAt(i); // remove self-referencing edges
                    sortedEdges.Add(Global.Pseudoangle(-edge.Vector.X, -edge.Vector.Y), (edge, false));
                }

                for (int i = sortedEdges.Count - 1, j = 0; i >= 0; j = i--) // 'i' is the current index, 'j' is the next index
                {
                    var (thisEdge, isStarting) = sortedEdges.Values[i];
                    var (nextEdge, nextIsStarting) = sortedEdges.Values[j];
                    if (isStarting != nextIsStarting) // if they are not the same direction then they are not nested
                        continue;
                    if (isStarting) // then we keep the one with the smaller angle, the next one is inside
                        edgesToRemove.Add(nextEdge);
                    else // then we keep the one with the larger angle, the next one is inside
                        edgesToRemove.Add(thisEdge);
                }
            }
            foreach (var edge in edgesToRemove)
            {
                var fromNode = (ArrangementNode)edge.FromPoint;
                fromNode.StartingEdges.Remove(edge);
                var toNode = (ArrangementNode)edge.ToPoint;
                toNode.EndingEdges.Remove(edge);
            }
        }
        private static void PruneIsolatedArrangementNodes(List<ArrangementNode> nodeList)
        {   // any nodes that have no starting or ending edges are removed.
            bool removalsFound;
            do // note that there may be a chain of nodes that are isolated, so we keep looking until no more are found
               // as member inside the chain would look not be prune on the first pass, but as the ends are pruned, it becomes isolated
            {
                removalsFound = false;
                for (int i = nodeList.Count - 1; i >= 0; i--)
                {
                    var node = nodeList[i];
                    if (node.StartingEdges.Count > 0 && node.EndingEdges.Count > 0)
                        continue;
                    nodeList.RemoveAt(i);
                    removalsFound = true;
                    foreach (var edge in node.EndingEdges)
                        ((ArrangementNode)edge.FromPoint).StartingEdges.Remove(edge);
                    foreach (var edge in node.StartingEdges)
                        ((ArrangementNode)edge.ToPoint).EndingEdges.Remove(edge);
                }
            } while (removalsFound);
        }

        private static List<Polygon> ExtractPolygonsFromArrangementNodes(List<ArrangementNode> nodeList)
        {   // at this point we essentially have reduced the graph to a set of loops
            // Since there could be multiple loops, and we need to find them all - there is a while loop
            // that starts a new loop each time until no nodes are left.
            var nodeHash = nodeList.ToHashSet();
            var polygons = new List<Polygon>();
            while (nodeHash.Any())
            {
                var loopCoords = new List<Vector2>();
                var validLoop = true;
                var startNode = nodeHash.First();
                var current = startNode;
                do
                {
                    nodeHash.Remove(current);
                    loopCoords.Add(current.Coordinates);
                    if (current.StartingEdges.Count != 1 || current.EndingEdges.Count != 1)
                        validLoop = false;
                    current = (ArrangementNode)current.StartingEdges[0].ToPoint;
                } while (current != startNode);
                if (validLoop)
                {
                    var polygon = new Polygon(loopCoords);
                    if (!polygon.Area.IsNegligible())
                        polygons.Add(polygon);
                }
            }
            return polygons;
        }

        private static void AddToSorted(List<PolygonEdge> sortedEdges, PolygonEdge newEdge, EdgeComparer edgeComparer)
        {   // binary search to find the right place to insert. 
            var index = sortedEdges.BinarySearch(newEdge, edgeComparer);
            index = index >= 0 ? index : ~index;
            sortedEdges.Insert(index, newEdge);
        }

    }

    internal class ArrangementNode : Vertex2D, IEquatable<ArrangementNode>
    {
        internal readonly List<PolygonEdge> StartingEdges;
        internal readonly List<PolygonEdge> EndingEdges;
        internal readonly PointKey pointKey;
        public ArrangementNode(PointKey pk, Vector2 v, int loopID = -1) : base(v, pk.GetHashCode(), loopID)
        {
            StartingEdges = new List<PolygonEdge>();
            EndingEdges = new List<PolygonEdge>();
            pointKey = pk;
        }
        public bool Equals(ArrangementNode other) => pointKey.Equals(other.pointKey);
        public override bool Equals(object obj) => obj is ArrangementNode pk && Equals(pk);
        public override int GetHashCode() => pointKey.GetHashCode();

    }
    internal readonly struct PointKey : IEquatable<PointKey>
    {
        internal readonly long longX;
        internal readonly long longY;
        private const double Scale = 1e9; // quantization for hashing
        private readonly int hashCode;
        public PointKey(Vector2 v)
        {
            longX = (long)Math.Round(v.X * Scale);
            longY = (long)Math.Round(v.Y * Scale);
            hashCode = System.HashCode.Combine(longX, longY);
        }
        public bool Equals(PointKey other) => longX == other.longX && longY == other.longY;
        public override bool Equals(object obj) => obj is PointKey pk && Equals(pk);
        public override int GetHashCode() => hashCode;

    }
    internal class EdgeComparer : IComparer<PolygonEdge>
    {
        public int Compare(PolygonEdge a, PolygonEdge b)
        {
            if (a.XMin < b.XMin) return -1;
            if (a.XMin > b.XMin) return 1;
            if (a.YMin < b.YMin) return -1;
            return 1;
        }
    }
}
