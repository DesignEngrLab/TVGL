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
        /// <summary>
        /// Builds polygons from an arbitrary collection of line segments (aka an arrangement
        /// https://en.wikipedia.org/wiki/Arrangement_of_lines). This is called a
        /// union since we define a polygon that bounds all pockets
        /// of the 2D that are contained by the segments (not the pocket that all segments
        /// agree is "contained" (e.g. the intersection). One wrinkle in this approach is the creation
        /// of degenerate "seams" - these are lines in the polygon where two edges are back-to-back.
        /// These are returned as <paramref name="degenerateSeams"/> in many cases they can be
        /// safely ignored but they are useful for no-fit-polygon applications.
        /// </summary>
        public static List<Polygon> ArrangementUnion(this IEnumerable<(Vector2, Vector2)> arrangement,
            out List<(Vector2, Vector2)> degenerateSeams)
        {
            var initNodeDict = BuildArrangementGraph(arrangement, out var edges);
            //Presenter.ShowAndHang(edges.Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            var nodeList = SplitArrangementEdgesAtIntersections(initNodeDict, edges);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            PruneIsolatedArrangementNodes(nodeList);
            //Presenter.ShowAndHang(nodeList.SelectMany(f => f.StartingEdges).Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));
            degenerateSeams = FindDegenerateSeams(nodeList);
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
            var initialCapacity = arrangement.TryGetNonEnumeratedCount(out var segmentCount) ? segmentCount : 0;
            var initNodeDict = new Dictionary<PointKey, ArrangementNode>(initialCapacity);
            var directedEdges = new HashSet<(PointKey From, PointKey To)>();
            edges = new List<PolygonEdge>(initialCapacity);

            foreach (var (from, to) in arrangement)
            {
                var fromKey = new PointKey(from);
                var toKey = new PointKey(to);
                if (!initNodeDict.TryGetValue(fromKey, out var fromNode))
                {
                    fromNode = new ArrangementNode(fromKey, from);
                    initNodeDict.Add(fromKey, fromNode);
                }
                if (!initNodeDict.TryGetValue(toKey, out var toNode))
                {
                    toNode = new ArrangementNode(toKey, to);
                    initNodeDict.Add(toKey, toNode);
                }
                if (!directedEdges.Add((fromKey, toKey)))
                    continue;
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
            edges.Sort(new EdgeComparer());
            var splitNodesByEdge = new Dictionary<PolygonEdge, HashSet<ArrangementNode>>();

            // Find intersections on the original segments. Splitting a straight segment does not
            // create geometrically new intersections, so each original edge can be rebuilt once
            // after all of its split points have been collected.
            for (var i = 0; i < edges.Count - 1; i++)
            {
                var current = edges[i];
                for (var j = i + 1; j < edges.Count; j++)
                {
                    var other = edges[j];
                    if (current.XMax.IsLessThanNonNegligible(other.XMin)) break;
                    if (!FindIfIntersectionBetweenEdges(current, other, out var intersection))
                        continue;
                    var intersectionPointKey = new PointKey(intersection);
                    if (!initNodeDict.TryGetValue(intersectionPointKey, out var intersectNode))
                    {
                        intersectNode = new ArrangementNode(intersectionPointKey, intersection);
                        initNodeDict.Add(intersectionPointKey, intersectNode);
                    }
                    AddSplitNode(splitNodesByEdge, current, intersectNode);
                    AddSplitNode(splitNodesByEdge, other, intersectNode);
                }
            }

            var nodeComparer = new NodeComparer();
            var nodes = initNodeDict.Values.ToList();
            nodes.Sort(nodeComparer);

            // Handle T-junctions and collinear overlaps. The crossing pass above creates every
            // non-parallel intersection node; this pass also places any existing node that lies
            // strictly inside an original edge onto that edge's split list.
            foreach (var edge in edges)
            {
                var firstCandidate = FindFirstNodeAtOrAfterX(nodes, edge.XMin);
                for (var i = firstCandidate; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    if (node.X.IsGreaterThanNonNegligible(edge.XMax))
                        break;
                    if (ReferenceEquals(node, edge.FromPoint) || ReferenceEquals(node, edge.ToPoint))
                        continue;
                    if (node.Y.IsLessThanNonNegligible(edge.YMin)
                        || node.Y.IsGreaterThanNonNegligible(edge.YMax))
                        continue;
                    if (NodeIsOnEdge(node, edge))
                        AddSplitNode(splitNodesByEdge, edge, node);
                }
            }

            RebuildArrangementEdges(edges, nodes, splitNodesByEdge, nodeComparer);
            return nodes;
        }

        private static void AddSplitNode(
            Dictionary<PolygonEdge, HashSet<ArrangementNode>> splitNodesByEdge,
            PolygonEdge edge,
            ArrangementNode node)
        {
            if (ReferenceEquals(node, edge.FromPoint) || ReferenceEquals(node, edge.ToPoint))
                return;
            if (!splitNodesByEdge.TryGetValue(edge, out var splitNodes))
            {
                splitNodes = new HashSet<ArrangementNode>();
                splitNodesByEdge.Add(edge, splitNodes);
            }
            splitNodes.Add(node);
        }

        private static int FindFirstNodeAtOrAfterX(List<ArrangementNode> nodes, double x)
        {
            var lower = 0;
            var upper = nodes.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) >> 1);
                if (nodes[middle].X < x)
                    lower = middle + 1;
                else
                    upper = middle;
            }
            return lower;
        }

        private static void RebuildArrangementEdges(
            List<PolygonEdge> originalEdges,
            List<ArrangementNode> nodes,
            Dictionary<PolygonEdge, HashSet<ArrangementNode>> splitNodesByEdge,
            NodeComparer nodeComparer)
        {
            foreach (var node in nodes)
            {
                node.StartingEdges.Clear();
                node.EndingEdges.Clear();
            }

            var directedEdges = new HashSet<(ArrangementNode From, ArrangementNode To)>();
            foreach (var edge in originalEdges)
            {
                var fromNode = (ArrangementNode)edge.FromPoint;
                var toNode = (ArrangementNode)edge.ToPoint;
                if (!splitNodesByEdge.TryGetValue(edge, out var splitNodeSet)
                    || splitNodeSet.Count == 0)
                {
                    AddArrangementEdge(fromNode, toNode, directedEdges);
                    continue;
                }

                var direction = edge.Vector;
                var splitNodes = splitNodeSet.ToList();
                splitNodes.Sort((a, b) =>
                {
                    var aProjection = (a.Coordinates - fromNode.Coordinates).Dot(direction);
                    var bProjection = (b.Coordinates - fromNode.Coordinates).Dot(direction);
                    var comparison = aProjection.CompareTo(bProjection);
                    return comparison != 0 ? comparison : nodeComparer.Compare(a, b);
                });

                var previousNode = fromNode;
                foreach (var splitNode in splitNodes)
                {
                    if (previousNode.Equals(splitNode))
                        continue;
                    AddArrangementEdge(previousNode, splitNode, directedEdges);
                    previousNode = splitNode;
                }
                if (!previousNode.Equals(toNode))
                    AddArrangementEdge(previousNode, toNode, directedEdges);
            }
        }

        private static void AddArrangementEdge(
            ArrangementNode fromNode,
            ArrangementNode toNode,
            HashSet<(ArrangementNode From, ArrangementNode To)> directedEdges)
        {
            if (!directedEdges.Add((fromNode, toNode)))
                return;
            var edge = new PolygonEdge(fromNode, toNode);
            fromNode.StartingEdges.Add(edge);
            toNode.EndingEdges.Add(edge);
        }

        private static bool NodeIsOnEdge(ArrangementNode node, PolygonEdge edge)
        {
            var from = edge.FromPoint.Coordinates;
            var to = edge.ToPoint.Coordinates;
            var segment = to - from;
            var lengthSquared = segment.LengthSquared();

            // Require the node's projection to be strictly inside the segment.
            // This also rejects zero-length edges.
            var projection = (node.Coordinates - from).Dot(segment);
            if (projection <= 0.0 || projection >= lengthSquared)
                return false;

            return MiscFunctions.IsPointOnSegment(
                node.Coordinates,
                from,
                to,
                Constants.BaseTolerance);
        }
        private static List<(Vector2, Vector2)> FindDegenerateSeams(List<ArrangementNode> nodes)
        {
            var degenerateSeams = new List<(Vector2, Vector2)>();
            // remove edges that are dominated by others - in the case of union - these are the ones that are inside other polygons
            foreach (var node in nodes)
            {
                for (int i = node.StartingEdges.Count - 1; i >= 0; i--)
                {
                    var startEdge = node.StartingEdges[i];
                    var otherNode = startEdge.ToPoint;
                    var equalAndOppositeEdgeIndex = node.EndingEdges.FindIndex(ee => ee.FromPoint == otherNode);
                    if (equalAndOppositeEdgeIndex >= 0) // you've detected a degenerate seam - two edges that are back-to-back.
                    {
                        var otherEdge = node.EndingEdges[equalAndOppositeEdgeIndex];
                        node.StartingEdges.RemoveAt(i);
                        node.EndingEdges.RemoveAt(equalAndOppositeEdgeIndex);
                        ((ArrangementNode)otherNode).EndingEdges.Remove(startEdge);
                        ((ArrangementNode)otherNode).StartingEdges.Remove(otherEdge);
                        degenerateSeams.Add((node.Coordinates, otherNode.Coordinates));
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
                        degenerateSeams.Add((node.Coordinates, otherNode.Coordinates));
                    }
                }
            }
            return degenerateSeams;
        }
        private static void RemoveDominatedArrangementEdges(List<ArrangementNode> nodes)
        {
            // remove edges that are dominated by others - in the case of union - these are the ones that are inside other polygons
            foreach (var node in nodes)
            {
                if (node.StartingEdges.Count <= 1 && node.EndingEdges.Count <= 1)
                    continue;

                //Sorted edges needs to allow for duplicate angles.
                var sortedEdges = new SortedList<(double Angle, int Id), (PolygonEdge Edge, bool IsStarting)>();
                int id = 0;
                // so, now we know that at least 3 edges come into this node
                for (int i = node.StartingEdges.Count - 1; i >= 0; i--)
                {
                    PolygonEdge edge = node.StartingEdges[i];
                    if (edge.ToPoint == edge.FromPoint) node.StartingEdges.RemoveAt(i); // remove self-referencing edges
                    else 
                    {
                        double angle = Global.Pseudoangle(edge.Vector.X, edge.Vector.Y);
                        sortedEdges.Add((angle, id++), (edge, true));
                    }
                }

                for (int i = node.EndingEdges.Count - 1; i >= 0; i--)
                {
                    PolygonEdge edge = node.EndingEdges[i];
                    if (edge.ToPoint == edge.FromPoint) node.EndingEdges.RemoveAt(i); // remove self-referencing edges
                    else
                    {
                        double angle = Global.Pseudoangle(-edge.Vector.X, -edge.Vector.Y);
                        sortedEdges.Add((angle, id++), (edge, false));
                    }
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
        {
            var nodesToPrune = new Queue<ArrangementNode>();
            var queuedNodes = new HashSet<ArrangementNode>();
            var removedNodes = new HashSet<ArrangementNode>();

            foreach (var node in nodeList)
            {
                if ((node.StartingEdges.Count == 0 || node.EndingEdges.Count == 0)
                    && queuedNodes.Add(node))
                    nodesToPrune.Enqueue(node);
            }

            while (nodesToPrune.TryDequeue(out var node))
            {
                if (removedNodes.Contains(node)
                    || (node.StartingEdges.Count > 0 && node.EndingEdges.Count > 0))
                    continue;
                removedNodes.Add(node);

                foreach (var edge in node.EndingEdges)
                {
                    var neighbor = (ArrangementNode)edge.FromPoint;
                    neighbor.StartingEdges.Remove(edge);
                    if (!removedNodes.Contains(neighbor)
                        && (neighbor.StartingEdges.Count == 0 || neighbor.EndingEdges.Count == 0)
                        && queuedNodes.Add(neighbor))
                        nodesToPrune.Enqueue(neighbor);
                }
                foreach (var edge in node.StartingEdges)
                {
                    var neighbor = (ArrangementNode)edge.ToPoint;
                    neighbor.EndingEdges.Remove(edge);
                    if (!removedNodes.Contains(neighbor)
                        && (neighbor.StartingEdges.Count == 0 || neighbor.EndingEdges.Count == 0)
                        && queuedNodes.Add(neighbor))
                        nodesToPrune.Enqueue(neighbor);
                }
                node.StartingEdges.Clear();
                node.EndingEdges.Clear();
            }

            if (removedNodes.Count > 0)
                nodeList.RemoveAll(removedNodes.Contains);
        }

        private static List<Polygon> ExtractPolygonsFromArrangementNodes(List<ArrangementNode> nodeList)
        {
            // Trace loops by following each directed edge to the success making the most right turn.
            // Because the successor of a directed edge is unique, we eventually end up where we started.
            var polygons = new List<Polygon>();
            var globallyVisited = new HashSet<PolygonEdge>();
            foreach (var node in nodeList)
            {
                foreach (var startEdge in node.StartingEdges)
                {
                    if (globallyVisited.Contains(startEdge)) continue;
                    var trail = new List<PolygonEdge>();
                    var indexInTrail = new Dictionary<PolygonEdge, int>();
                    var current = startEdge;
                    while (true)
                    {
                        if (indexInTrail.TryGetValue(current, out var cycleStart))
                        {   // we've re-entered our own trail: edges [cycleStart..] form the cycle;
                            // edges before it are an open tail leading into the cycle - discarded
                            var loopCoords = new List<Vector2>();
                            for (int i = cycleStart; i < trail.Count; i++)
                                loopCoords.Add(trail[i].FromPoint.Coordinates);
                            if (loopCoords.Count >= 3)
                            {
                                var polygon = new Polygon(loopCoords);
                                if (!polygon.Area.IsNegligible())
                                    polygons.Add(polygon);
                            }
                            break;
                        }
                        if (globallyVisited.Contains(current))
                            break; // flowed into a previously handled cycle; this tail is discarded
                        indexInTrail.Add(current, trail.Count);
                        trail.Add(current);
                        globallyVisited.Add(current);
                        var next = NextEdgeByAngularSuccessor(current);
                        if (next == null) break; // dead end; open chain discarded
                        current = next;
                    }
                }
            }
            return polygons;
        }

        /// <summary>
        /// Returns the outgoing edge at the incoming edge's ToPoint that makes the sharpest
        /// clockwise turn from the reversed incoming direction. An exact u-turn (retracing the
        /// incoming edge backwards) is treated as a full turn, i.e. the last resort - which is
        /// exactly what lets a zero-width seam be walked down-and-back correctly.
        /// </summary>
        private static PolygonEdge NextEdgeByAngularSuccessor(PolygonEdge incoming)
        {
            var node = (ArrangementNode)incoming.ToPoint;
            if (node.StartingEdges.Count == 0) return null;
            if (node.StartingEdges.Count == 1) return node.StartingEdges[0];
            var reverseAngle = Global.Pseudoangle(-incoming.Vector.Y, -incoming.Vector.X);
            // find the candidate direction with a minimum CCW angle from the (reversed)
            // incoming direction 
            PolygonEdge best = null;
            var bestAngle = double.MaxValue;
            foreach (var candidate in node.StartingEdges)
            {
                // clockwise angle from the reversed incoming direction to the candidate, in (0, 2*pi]
                var angle = Global.Pseudoangle(candidate.Vector.X, candidate.Vector.Y) - reverseAngle;
                if (angle < 0) angle += 4;      // normalize to [0, 2*pi)
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = candidate;
                }
            }
            return best;
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
                if (ReferenceEquals(a, b)) return 0;
                var comparison = a.XMin.CompareTo(b.XMin);
                if (comparison != 0) return comparison;
                comparison = a.YMin.CompareTo(b.YMin);
                if (comparison != 0) return comparison;
                comparison = a.XMax.CompareTo(b.XMax);
                if (comparison != 0) return comparison;
                comparison = a.YMax.CompareTo(b.YMax);
                if (comparison != 0) return comparison;
                comparison = a.FromPoint.X.CompareTo(b.FromPoint.X);
                if (comparison != 0) return comparison;
                comparison = a.FromPoint.Y.CompareTo(b.FromPoint.Y);
                if (comparison != 0) return comparison;
                comparison = a.ToPoint.X.CompareTo(b.ToPoint.X);
                return comparison != 0 ? comparison : a.ToPoint.Y.CompareTo(b.ToPoint.Y);
            }
        }
        internal class NodeComparer : IComparer<ArrangementNode>
        {
            public int Compare(ArrangementNode a, ArrangementNode b)
            {
                if (ReferenceEquals(a, b)) return 0;
                var comparison = a.X.CompareTo(b.X);
                return comparison != 0 ? comparison : a.Y.CompareTo(b.Y);
            }
        }
    }
}
