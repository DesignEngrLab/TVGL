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
using System.Reflection;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        public static List<Polygon> ArrangementUnion(this IEnumerable<(Vector2, Vector2)> arrangement)
        {
            var initNodeDict = new Dictionary<PointKey, ArrangementNode>();
            var numEdges = 0;
            foreach (var (from, to) in arrangement)
            {
                numEdges++;
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
                var edge = new PolygonEdge(fromNode, toNode);
                fromNode.StartingEdges.Add(edge);
                toNode.EndingEdges.Add(edge);
            }
            Global.Presenter2D.ShowAndHang(initNodeDict.Values.SelectMany(n => n.StartingEdges)
                .Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));

            var verticesOrderByX = initNodeDict.Values.ToArray();
            Array.Sort(verticesOrderByX, new TwoDSortXFirst());
            var edgesOrderedByX = GetOrderedLinesArrangement(verticesOrderByX, numEdges);
            var intersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>();
            for (int i = 0; i < numEdges - 1; i++)
            {
                var current = edgesOrderedByX[i];
                for (int j = i + 1; j < numEdges; j++)
                {
                    var other = edgesOrderedByX[j];
                    if (current.XMax < other.XMin) break;
                    if (IsAdjacentTo(current, other)) continue;
                    AddIntersectionBetweenLines(current, other, intersections, possibleDuplicates, 9, false, false);
                }
            }
            foreach (var intersection in intersections)
            {
                var intersectionPointKey = new PointKey(intersection.IntersectCoordinates);
                if (!initNodeDict.TryGetValue(intersectionPointKey, out var intersectNode))
                {
                    intersectNode = new ArrangementNode(intersectionPointKey, intersection.IntersectCoordinates);
                    initNodeDict.Add(intersectionPointKey, intersectNode);
                }
                // shouldn't have any WhereIntersections set to WhereIsIntersection.BothStarts
                if (intersection.WhereIntersection != WhereIsIntersection.AtStartOfA)
                {   // split edge A
                    var aFromNode = (ArrangementNode)intersection.EdgeA.FromPoint;
                    var newEdgeA = new PolygonEdge(aFromNode, intersectNode);
                    aFromNode.StartingEdges.Remove(intersection.EdgeA);
                    // often - due to collinearity, we end up added a new edge that already exists
                    if (!aFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                    {
                        aFromNode.StartingEdges.Add(newEdgeA);
                        intersectNode.EndingEdges.Add(newEdgeA);
                    }
                    var aToNode = (ArrangementNode)intersection.EdgeA.ToPoint;
                    newEdgeA = new PolygonEdge(intersectNode, aToNode);
                    aToNode.EndingEdges.Remove(intersection.EdgeA);
                    if (!aToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                    {
                        aToNode.EndingEdges.Add(newEdgeA);
                        intersectNode.StartingEdges.Add(newEdgeA);
                    }
                }
                if (intersection.WhereIntersection != WhereIsIntersection.AtStartOfB)
                {   // split edge B
                    var bFromNode = (ArrangementNode)intersection.EdgeB.FromPoint;
                    var newEdgeB = new PolygonEdge(bFromNode, intersectNode);
                    bFromNode.StartingEdges.Remove(intersection.EdgeB);
                    if (!bFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                    {
                        bFromNode.StartingEdges.Add(newEdgeB);
                        intersectNode.EndingEdges.Add(newEdgeB);
                    }
                    var bToNode = (ArrangementNode)intersection.EdgeB.ToPoint;
                    newEdgeB = new PolygonEdge(intersectNode, bToNode);
                    bToNode.EndingEdges.Remove(intersection.EdgeB);
                    if (!bToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                    {
                        bToNode.EndingEdges.Add(newEdgeB);
                        intersectNode.StartingEdges.Add(newEdgeB);
                    }
                }
            }
            Global.Presenter2D.ShowAndHang(initNodeDict.Values.SelectMany(n => n.StartingEdges)
                .Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));

            #region remove edges that are dominated by others - in the case of union - these are the ones that are inside other polygons
            var edgesToRemove = new HashSet<PolygonEdge>();
            foreach (var node in initNodeDict.Values)
            {
                if (node.StartingEdges.Count <= 1 && node.EndingEdges.Count <= 1)
                    continue;
                // so, now we know that at least 3 edges come into this node
                var sortedEdges = new SortedList<double, (PolygonEdge, bool)>();
                foreach (var edge in node.StartingEdges)
                    sortedEdges.Add(Global.Pseudoangle(edge.Vector.X, edge.Vector.Y), (edge, true));
                foreach (var edge in node.EndingEdges)
                    sortedEdges.Add(Global.Pseudoangle(-edge.Vector.X, -edge.Vector.Y), (edge, false));
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
            Global.Presenter2D.ShowAndHang(initNodeDict.Values.SelectMany(n => n.StartingEdges)
                .Select(s => new[] { s.FromPoint.Coordinates, s.ToPoint.Coordinates }));

            #endregion
            var nodeList = initNodeDict.Values.Where(node => node.StartingEdges.Count > 0 && node.EndingEdges.Count > 0).ToList();
            return null;


        }


        public static bool IsAdjacentTo(PolygonEdge a, PolygonEdge b)
        {   // because edges are going all over the place, just check if they share a point
            return a.IsAdjacentTo(b) || a.FromPoint == b.FromPoint;
            // adding a check with FromPoint to FromPoint since - now - nodes can have multiple
            // edges starting from them
            //|| a.ToPoint == b.ToPoint;...actually, we don't check if they end at the same node
            //                             because 2 lines may intersect in an important way at their start.
        }

        /// <summary>
        /// Gets the ordered lines.
        /// </summary>
        /// <param name="orderedNodes">The ordered points.</param>
        /// <returns>PolygonEdge[].</returns>
        private static PolygonEdge[] GetOrderedLinesArrangement(ArrangementNode[] orderedNodes, int numEdges)
        {
            var result = new PolygonEdge[numEdges];
            var k = 0;
            foreach (var point in orderedNodes)
            {
                foreach (var startLine in point.StartingEdges)
                {
                    if (startLine.OtherPoint(point).X >= point.X)
                        result[k++] = startLine;
                }
                foreach (var endLine in point.EndingEdges)
                {
                    if (endLine.OtherPoint(point).X > point.X)
                        result[k++] = endLine;
                }
                if (k >= numEdges) break;
            }
            return result;
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
}
