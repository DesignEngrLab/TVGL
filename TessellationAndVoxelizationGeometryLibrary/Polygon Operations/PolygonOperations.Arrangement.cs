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
using static TVGL.PolygonOperations;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        public static List<Polygon> ArrangementUnion(this IEnumerable<(Vector2, Vector2)> arrangement)
        {
            var initNodeDict = BuildArrangementGraph(arrangement, out var edges);
            //Presenter.ShowAndHang(edges.Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            var nodeList = SplitArrangementEdgesAtIntersections(initNodeDict, edges);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            PruneIsolatedArrangementNodes(nodeList);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            RemoveDominatedArrangementEdges(nodeList);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            PruneIsolatedArrangementNodes(nodeList);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            var polygons = ExtractPolygonsFromArrangementNodes(nodeList);
            //Presenter.ShowAndHang(polygons);

            return polygons.CreateShallowPolygonTrees(true);
        }
        private static Dictionary<PointKey, ArrangementNode> BuildArrangementGraph(IEnumerable<(Vector2, Vector2)> arrangement, out List<PolygonEdge> edges)
        {
            var initNodeDict = new Dictionary<PointKey, ArrangementNode>(); // store the nodes by their point key. We will keep finding new nodes later 
                                                                            // when we look for intersections so, this entire dictionary is returned
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
            return initNodeDict;
        }

        // Tricky function to split arrangement edges at their intersections.
        private static List<ArrangementNode> SplitArrangementEdgesAtIntersections(Dictionary<PointKey, ArrangementNode> initNodeDict,
            List<PolygonEdge> edges)
        {
            var edgeComparer = new EdgeComparer();  // sort by XMin, then YMin
            edges.Sort(edgeComparer);
            while (edges.Count > 0)
            {
                var current = edges[0];  // hmm, why not use a priority queue? Because of the following 
                edges.RemoveAt(0);       // foreach which looks at the others edges ahead in the queue.
                foreach (var other in edges)
                {
                    if (current.XMax.IsLessThanNonNegligible(other.XMin)) break;
                    if (!FindIfIntersectionBetweenEdges(current, other, out var intersection))
                        continue;
                    var splitCurrent = false;
                    var splitOther = false;
                    var intersectionPointKey = new PointKey(intersection);
                    if (initNodeDict.TryGetValue(intersectionPointKey, out var intersectNode))
                    {
                        splitCurrent = intersectNode != current.FromPoint && intersectNode != current.ToPoint;
                        splitOther = intersectNode != other.FromPoint && intersectNode != other.ToPoint;
                    }
                    else
                    {
                        splitCurrent = splitOther = true;
                        intersectNode = new ArrangementNode(intersectionPointKey, intersection);
                        initNodeDict.Add(intersectionPointKey, intersectNode);
                    }
                    if (splitCurrent)
                        foreach (var newEdge in SplitReplaceOldEdge(current, intersectNode))
                            AddToSorted(edges, newEdge, edgeComparer);
                    if (splitOther)
                    {
                        foreach (var newEdge in SplitReplaceOldEdge(other, intersectNode))
                            AddToSorted(edges, newEdge, edgeComparer);
                        edges.Remove(other);
                    }
                    break; // we break because the current edge has been split, and is no longer valid
                }
            }
            var nodeComparer = new NodeComparer();
            var nodes = initNodeDict.Values.ToList();
            nodes.Sort(nodeComparer);
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var currentNode = nodes[i];
                var connectedEdges = new Queue<PolygonEdge>(currentNode.StartingEdges.Concat(currentNode.EndingEdges));
                while (connectedEdges.TryDequeue(out var edge))
                {
                    var adjacentNode = edge.FromPoint == currentNode ? edge.ToPoint : edge.FromPoint;
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        var otherNode = nodes[j];
                        if (adjacentNode == otherNode )
                            continue;
                        if (otherNode.X.IsGreaterThanNonNegligible(edge.XMax)) break;
                        if (NodeIsOnEdge(otherNode, edge))
                        {
                            foreach (var newEdge in SplitReplaceOldEdge(edge, otherNode))
                                if (newEdge.FromPoint == currentNode || newEdge.ToPoint == currentNode)
                                    connectedEdges.Enqueue(newEdge);
                            break; // we break because the current edge has been split, and is no longer valid
                        }
                    }
                }
            }
            return nodes;
        }

        private static bool NodeIsOnEdge(ArrangementNode otherNode, PolygonEdge edge)
        {
            if (otherNode.Y.IsLessThanNonNegligible(edge.YMin)
                || otherNode.Y.IsGreaterThanNonNegligible(edge.YMax))
                return false;
            // so, we now know that the node is within the bounding box of the edge
            var edgeNormal = new Vector2(-edge.Vector.Y, edge.Vector.X);
            var edgeOffset = edgeNormal.Dot(edge.FromPoint.Coordinates);
            var testOffset = edgeNormal.Dot(otherNode.Coordinates);
            return edgeOffset.IsPracticallySame(testOffset,Constants.BaseTolerance);
        }

        private static IEnumerable<PolygonEdge> SplitReplaceOldEdge(PolygonEdge oldEdge, ArrangementNode intersectNode)
        {
            // split current edge
            var fromNode = (ArrangementNode)oldEdge.FromPoint;
            fromNode.StartingEdges.Remove(oldEdge);
            if (fromNode == intersectNode) ;
            if (!fromNode.StartingEdges.Intersect(intersectNode.EndingEdges).Any())
            {  // only add if this edge does not already exist
                var newEdge1 = new PolygonEdge(fromNode, intersectNode);
                fromNode.StartingEdges.Add(newEdge1);
                intersectNode.EndingEdges.Add(newEdge1);
                yield return newEdge1;
            }
            // now the second half
            var toNode = (ArrangementNode)oldEdge.ToPoint;
            if (toNode == intersectNode) ;
            toNode.EndingEdges.Remove(oldEdge);
            if (!toNode.EndingEdges.Intersect(intersectNode.StartingEdges).Any())
            {  // only add if this edge does not already exist
                var newEdge2 = new PolygonEdge(intersectNode, toNode);
                toNode.EndingEdges.Add(newEdge2);
                intersectNode.StartingEdges.Add(newEdge2);
                yield return newEdge2;
            }
        }
        private static void RemoveDominatedArrangementEdges(List<ArrangementNode> nodes)
        {
            // remove edges that are dominated by others - in the case of union - these are the ones that are inside other polygons

            foreach (var node in nodes)
            {
                //actually, before we get to the main purpose, let's remove any pair of edges that are equal and opposite. This is not 
                // exactly wrong, as it is possible that the resulting union is a line in some sections
                for (int i = node.StartingEdges.Count - 1; i >= 0; i--)
                {
                    var startEdge = node.StartingEdges[i];
                    var otherNode = startEdge.ToPoint;
                    var equalAndOppositeEdgeIndex = node.EndingEdges.FindIndex(ee => ee.FromPoint == otherNode);
                    if (equalAndOppositeEdgeIndex >= 0)
                    {
                        var otherEdge = node.EndingEdges[equalAndOppositeEdgeIndex];
                        node.StartingEdges.RemoveAt(i);
                        node.EndingEdges.RemoveAt(equalAndOppositeEdgeIndex);
                        ((ArrangementNode)otherNode).EndingEdges.Remove(startEdge);
                        ((ArrangementNode)otherNode).StartingEdges.Remove(otherEdge);
                    }
                }
                for (int i = node.EndingEdges.Count - 1; i >= 0; i--)
                {
                    var startEdge = node.EndingEdges[i];
                    var otherNode = startEdge.FromPoint;
                    var equalAndOppositeEdgeIndex = node.StartingEdges.FindIndex(se => se.ToPoint == otherNode);
                    if (equalAndOppositeEdgeIndex >= 0)
                    {
                        var otherEdge = node.StartingEdges[equalAndOppositeEdgeIndex];
                        node.EndingEdges.RemoveAt(i);
                        node.StartingEdges.RemoveAt(equalAndOppositeEdgeIndex);
                        ((ArrangementNode)otherNode).StartingEdges.Remove(startEdge);
                        ((ArrangementNode)otherNode).EndingEdges.Remove(otherEdge);
                    }
                }
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
                    else sortedEdges.Add(Global.Pseudoangle(-edge.Vector.X, -edge.Vector.Y), (edge, false));
                }

                for (int i = sortedEdges.Count - 1, j = 0; i >= 0; j = i--) // 'i' is the current index, 'j' is the next index
                {
                    var (thisEdge, isStarting) = sortedEdges.Values[i];
                    var (nextEdge, nextIsStarting) = sortedEdges.Values[j];
                    if (isStarting != nextIsStarting) // if they are not the same direction then they are not nested
                        continue;
                    if (isStarting) // then we keep the one with the smaller angle, the next one is inside
                        RemoveArrangementEdge(nextEdge);
                    else // then we keep the one with the larger angle, the next one is inside
                        RemoveArrangementEdge(thisEdge);
                }
            }
        }

        private static void RemoveArrangementEdge(PolygonEdge edge)
        {
            var fromNode = (ArrangementNode)edge.FromPoint;
            fromNode.StartingEdges.Remove(edge);
            var toNode = (ArrangementNode)edge.ToPoint;
            toNode.EndingEdges.Remove(edge);
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

        /// <summary>
        /// Determines if Two polygon line segments intersect. Because they are part of a polygon, it is decided to make the
        /// fromPoint Inclusive, and the toPoint exclusive. Thus, if lines touch at their endpoints, it is only recorded
        /// if both points are from points. Also no "close" operations are used (e.g. IsPracticallySame). Because the method is
        /// intended to be invoked for all lines on the polygon, this prevents an intersection from being caught by multiple lines,
        /// and makes the methods simpler (easier to debug and edit) and quicker.
        /// </summary>
        /// <param name="lineA">The line a.</param>
        /// <param name="lineB">The line b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="possibleDuplicates">The possible duplicates.</param>
        /// <param name="numSigDigs">The number sig digs.</param>
        /// <param name="needToRoundA">if set to <c>true</c> [need to round a].</param>
        /// <param name="needToRoundB">if set to <c>true</c> [need to round b].</param>
        /// <returns>PolygonSegmentRelationship.</returns>
        internal static bool FindIfIntersectionBetweenEdges(PolygonEdge lineA, PolygonEdge lineB,
            out Vector2 intersection)
        {
            intersection = Vector2.Null;
            // first check if bounding boxes overlap. Actually, we don't need to check the x values (lineA.XMax < lineB.XMin || 
            // lineB.XMax < lineA.XMin)- this is already known from the calling function and the way it calls based on sorted x values
            if (lineA.YMax < lineB.YMin || lineB.YMax < lineA.YMin)
                // the two lines do not touch since their bounding boxes do not overlap
                return false;

            if (lineA.IsAdjacentTo(lineB) || lineA.FromPoint == lineB.FromPoint || lineA.ToPoint == lineB.ToPoint)
                return false;
            var aFrom = lineA.FromPoint.Coordinates;
            var aTo = lineA.ToPoint.Coordinates;
            var bFrom = lineB.FromPoint.Coordinates;
            var bTo = lineB.ToPoint.Coordinates;
            var aVector = lineA.Vector;
            var bVector = lineB.Vector;
            var lineACrossLineB = aVector.Cross(bVector); //2D cross product, determines if parallel
            if (lineACrossLineB.IsNegligible())
                return false; // then the lines are parallel or collinear - we will handle this later
            var fromPointVector = bFrom - aFrom; // the vector connecting starts

            // now check the intersection by detecting where non-parallel lines cross
            // solve for the t scalar values for the two lines.
            // the line is define as all values of t from 0 to 1 in the equations
            // line1(t_1) = (1 - t_1)*line1.From + t_1*line1.To
            // line2(t_2) = (1 - t_2)*line2.From + t_2*line2.To
            // ...solving for the x-value at the intersection...
            // xIntersect =  (1 - t_1)*line1.From.X + t_1*line1.To.X = (1 - t_2)*line2.From.X + t_2*line2.To.X (Eq.1)
            // yIntersect =  (1 - t_1)*line1.From.Y + t_1*line1.To.Y = (1 - t_2)*line2.From.Y + t_2*line2.To.Y (Eq.2)
            //rewriting Eq.1 as...
            // t_1*(line1.To.X - line1.From.X) + t_2*(line2.From.X - line2.To.X) = line2.From.X - line1.From.X 
            // which can be simplified to...
            // t_1*(line1.Vector.X) - t_2*(line2.Vector.X) = vStart.X
            // similiarly for Y
            // t_1*(line1.Vector.Y) - t_2*(line2.Vector.Y) = vStart.Y
            // solve as a system of two equations
            //   |   line1.Vector.X      -line2.Vector.X   | |  t_1  |    | vStart.X  |
            //   |                                         |*|       | =  |           |
            //   |   line1.Vector.Y      -line2.Vector.Y   | |  t_2  |    | vStart.Y  |
            var oneOverdeterminnant = 1 / lineACrossLineB;
            var t_1 = oneOverdeterminnant * (bVector.Y * fromPointVector.X - bVector.X * fromPointVector.Y);
            if (t_1 < 0 || t_1 >= 1)
                return false;
            var t_2 = oneOverdeterminnant * (aVector.Y * fromPointVector.X - aVector.X * fromPointVector.Y);
            if (t_2 < 0 || t_2 >= 1)
                return false;
            intersection = new Vector2(
                   0.5 * (aFrom.X + t_1 * aVector.X + bFrom.X + t_2 * bVector.X),
                  0.5 * (aFrom.Y + t_1 * aVector.Y + bFrom.Y + t_2 * bVector.Y));
            return true;
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
                //if (a.XMin < b.XMin) return -1;
                //if (a.XMin > b.XMin) return 1;
                //if (a.YMin < b.YMin) return -1;
                if (a.XMin.IsLessThanNonNegligible(b.XMin)) return -1;
                if (b.XMin.IsLessThanNonNegligible(a.XMin)) return 1;
                if (a.YMin.IsLessThanNonNegligible(b.YMin)) return -1;
                if (b.YMin.IsLessThanNonNegligible(a.YMin)) return 1;
                if (a.XMax.IsLessThanNonNegligible(b.XMax)) return -1;
                if (b.XMax.IsLessThanNonNegligible(a.XMax)) return 1;
                if (a.YMax.IsLessThanNonNegligible(b.YMax)) return -1;
                //if (b.YMax.IsLessThanNonNegligible(a.YMax)) return 1; // not needed since same as default return
                return 1;
            }
        }
        internal class NodeComparer : IComparer<ArrangementNode>
        {
            public int Compare(ArrangementNode a, ArrangementNode b)
            {
                if (a.X.IsLessThanNonNegligible(b.X)) return -1;
                if (b.X.IsLessThanNonNegligible(a.X)) return 1;
                if (a.Y.IsLessThanNonNegligible(b.Y)) return -1;
                //if (b.Y.IsLessThanNonNegligible(a.Y)) return 1; // not needed since same as default return
                return 1;
            }
        }
    }
}
