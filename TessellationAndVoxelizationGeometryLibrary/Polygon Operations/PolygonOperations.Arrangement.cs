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
using System.Xml.Linq;

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
            var edges = new List<PolygonEdge>();
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
                edges.Add(edge);
                fromNode.StartingEdges.Add(edge);
                toNode.EndingEdges.Add(edge);
            }
            var verticesOrderByX = initNodeDict.Values.ToArray();
            Array.Sort(verticesOrderByX, new TwoDSortXFirst());
            var edgeComparer = new EdgeComparer();
            var edgesOrderedByX = new List<PolygonEdge>(edges.OrderBy(x => x, edgeComparer));
            var intersections = new List<SegmentIntersection>(1);
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>(1);
            while (edgesOrderedByX.Count > 0)
            {
                var current = edgesOrderedByX[0];
                edgesOrderedByX.RemoveAt(0);
                PolygonEdge otherToRemove = null;
                foreach (var other in edgesOrderedByX)
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
                    if (intersection.WhereIntersection != WhereIsIntersection.AtStartOfA)
                    {   // split edge A
                        var oldEdgeA = intersection.EdgeA;
                        if (oldEdgeA == other) otherToRemove = other;
                        var aFromNode = (ArrangementNode)oldEdgeA.FromPoint;
                        var newEdgeA = new PolygonEdge(aFromNode, intersectNode);
                        aFromNode.StartingEdges.Remove(oldEdgeA);
                        // often - due to collinearity, we end up added a new edge that already exists
                        if (!aFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                        {
                            AddToSorted(edgesOrderedByX, newEdgeA, edgeComparer);
                            aFromNode.StartingEdges.Add(newEdgeA);
                            intersectNode.EndingEdges.Add(newEdgeA);
                        }
                        var aToNode = (ArrangementNode)oldEdgeA.ToPoint;
                        newEdgeA = new PolygonEdge(intersectNode, aToNode);
                        aToNode.EndingEdges.Remove(oldEdgeA);
                        if (!aToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                        {
                            AddToSorted(edgesOrderedByX, newEdgeA, edgeComparer);
                            aToNode.EndingEdges.Add(newEdgeA);
                            intersectNode.StartingEdges.Add(newEdgeA);
                        }
                    }
                    if (intersection.WhereIntersection != WhereIsIntersection.AtStartOfB)
                    {   // split edge B
                        var oldEdgeB = intersection.EdgeB;
                        if (oldEdgeB == other) otherToRemove = other;
                        var bFromNode = (ArrangementNode)oldEdgeB.FromPoint;
                        var newEdgeB = new PolygonEdge(bFromNode, intersectNode);
                        bFromNode.StartingEdges.Remove(oldEdgeB);
                        if (!bFromNode.StartingEdges.Any(e => e.ToPoint == intersectNode))
                        {
                            AddToSorted(edgesOrderedByX, newEdgeB, edgeComparer);
                            bFromNode.StartingEdges.Add(newEdgeB);
                            intersectNode.EndingEdges.Add(newEdgeB);
                        }
                        var bToNode = (ArrangementNode)oldEdgeB.ToPoint;
                        newEdgeB = new PolygonEdge(intersectNode, bToNode);
                        bToNode.EndingEdges.Remove(oldEdgeB);
                        if (!bToNode.EndingEdges.Any(e => e.FromPoint == intersectNode))
                        {
                            AddToSorted(edgesOrderedByX, newEdgeB, edgeComparer);
                            bToNode.EndingEdges.Add(newEdgeB);
                            intersectNode.StartingEdges.Add(newEdgeB);
                        }
                    }
                    break; // only handle one intersection at a time
                }
                if (otherToRemove != null) edgesOrderedByX.Remove(otherToRemove);
            }
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
            var nodeList = initNodeDict.Values.ToList();
            bool removalsFound;
            do
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
            var nodeHash = nodeList.ToHashSet();
            var polygons = new List<Polygon>();
            while (nodeHash.Any())
            {
                var loopCoords = new List<Vector2> ();
                var startNode = nodeHash.First();
                var current = startNode;
                do
                {
                    nodeHash.Remove(current);
                    loopCoords.Add(current.Coordinates);
                    current =(ArrangementNode) current.StartingEdges[0].ToPoint;
                }
                while (current != startNode);
                var polygon =new Polygon(loopCoords);
                if (!polygon.Area.IsNegligible())
                    polygons.Add(polygon);
            }
            #endregion
            return polygons.CreateShallowPolygonTrees(true);
        }

private static void AddToSorted(List<PolygonEdge> sortedEdges, PolygonEdge newEdge, EdgeComparer edgeComparer)
{
    var index = sortedEdges.BinarySearch(newEdge, edgeComparer);
    index = index >= 0 ? index : ~index;
    sortedEdges.Insert(index, newEdge);
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
