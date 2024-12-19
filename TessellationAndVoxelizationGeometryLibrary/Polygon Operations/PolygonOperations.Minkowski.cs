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
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Windows.Input;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// The Minkowski sum of the two polygons. This only functions on the outermost polygon (no holes).
        /// However, the operation does work on negative polygons, so the result can be fused totheger but this
        /// is left for the user's code due to ambiguities that may arise.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static List<Polygon> MinkowskiSum(this Polygon a, Polygon b)
        {
            var aConcaveVertices = a.GetConcaveVertices().ToList();
            var bConcaveVertices = b.GetConcaveVertices().ToList();
            if (aConcaveVertices.Count == 0 & bConcaveVertices.Count == 0)
                return [MinkowskiSumConvex(a, b)];
            else if (aConcaveVertices.Count == 0)
                return MinkowskiSumConvexConcave(a, b);
            else if (bConcaveVertices.Count == 0)
                return MinkowskiSumConvexConcave(b, a);
            if (aConcaveVertices.Count < bConcaveVertices.Count)
                return MinkowskiSumGeneral(a, aConcaveVertices, b);
            else return MinkowskiSumGeneral(b, bConcaveVertices, a);
        }

        private static Polygon MinkowskiSumConvex(Polygon a, Polygon b)
        {
            int aStartIndex = FindMinY(a.Vertices);
            int bStartIndex = FindMinY(b.Vertices);

            var result = new List<Vector2>();
            var i = 0;
            var j = 0;
            var aLength = a.Vertices.Count;
            var bLength = b.Vertices.Count;
            while (i < aLength || j < bLength)
            {
                result.Add(a.Path[(i + aStartIndex) % aLength] + b.Path[(j + bStartIndex) % bLength]);
                var cross = (a.Path[(i + 1 + aStartIndex) % aLength] - a.Path[(i + aStartIndex) % aLength])
                    // will this always be correct? I'm worried that angle could be greater than 180, and then a
                    // false result would be returned. ...although, I tried to come up with a case to break it
                    // and couldn't I guess because you can't have an angle greater than 180 on convex shapes
                    .Cross(b.Path[(j + 1 + bStartIndex) % bLength] - b.Path[(j + bStartIndex) % bLength]);
                if (cross >= 0 && i < aLength)
                    ++i;
                if (cross <= 0 && j < bLength)
                    ++j;
            }
            return new Polygon(result);
        }

        private static int FindMinY(List<Vertex2D> vertices)
        {
            var minIndex = -1;
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].Y < minY)
                {
                    minIndex = i;
                    minX = vertices[i].X;
                    minY = vertices[i].Y;
                }
                else if (vertices[i].Y.IsPracticallySame(minY) && vertices[i].X < minX)
                {
                    minIndex = i;
                    minX = vertices[i].X;
                }
            }
            return minIndex;
        }

        private static List<Polygon> MinkowskiSumConvexConcave(Polygon a, Polygon b)
        {
            var aStartEdge = a.Edges[FindMinY(a.Vertices)];
            var bStartEdge = b.Edges[FindMinY(b.Vertices)];
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new Dictionary<(Vertex2D, Vertex2D), (int, int)>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var complexPaths = new List<Polygon>();
            var knownWrongPoints = new List<List<bool>>();
            ConvolutionCycle(a, b, aStartEdge, bStartEdge, visitedHash, aEdgeAngles, bEdgeAngles, complexPaths, knownWrongPoints);

            return complexPaths[0].RemoveSelfIntersections(
                flipResult ? ResultType.OnlyKeepNegative : ResultType.OnlyKeepPositive, knownWrongPoints[0]);
        }

        private static void ConvolutionCycle(Polygon a, Polygon b, PolygonEdge aStartEdge, PolygonEdge bStartEdge,
            Dictionary<(Vertex2D, Vertex2D), (int, int)> visitedHash, Dictionary<PolygonEdge, double> aEdgeAngles,
            Dictionary<PolygonEdge, double> bEdgeAngles, List<Polygon> results, List<List<bool>> allKnownWrongPoints)
        {
            var prevAEdge = aStartEdge;
            var prevBEdge = bStartEdge;
            var result = new List<Vector2> { prevAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates };
            var nextAEdge = prevAEdge.ToPoint.StartLine;
            var nextBEdge = prevBEdge.ToPoint.StartLine;
            var knownWrongPoints = new List<bool> { prevBEdge.Vector.Cross(nextBEdge.Vector) < 0 };
            var currentLoopIndex = results.Count;
            do
            {
                var newPointAdded = false;
                var aIndices = (-1, -1);
                var bIndices = (-1, -1);
                var aAngle = aEdgeAngles[nextAEdge];
                var aPrevAngle = aEdgeAngles[prevAEdge];
                var bAngle = bEdgeAngles[nextBEdge];
                var bPrevAngle = bEdgeAngles[prevBEdge];
                if (firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle) &&
                    !visitedHash.TryGetValue((prevAEdge.ToPoint, prevBEdge.ToPoint), out aIndices))
                {
                    visitedHash.Add((prevAEdge.ToPoint, prevBEdge.ToPoint), (currentLoopIndex, result.Count));
                    result.Add(nextAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates);
                    var prevBCrossNextB = prevBEdge.Vector.Cross(nextBEdge.Vector);
                    knownWrongPoints.Add(prevBCrossNextB < 0);
                    prevAEdge = nextAEdge;
                    nextAEdge = nextAEdge.ToPoint.StartLine;
                    newPointAdded = true;
                }
                if (firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle) &&
                    !visitedHash.TryGetValue((prevAEdge.ToPoint, prevBEdge.ToPoint), out bIndices))
                {
                    visitedHash.Add((prevAEdge.ToPoint, prevBEdge.ToPoint), (currentLoopIndex, result.Count));
                    result.Add(prevAEdge.ToPoint.Coordinates + nextBEdge.ToPoint.Coordinates);
                    var prevACrossNextA = prevAEdge.Vector.Cross(nextAEdge.Vector);
                    knownWrongPoints.Add(prevACrossNextA < 0);
                    prevBEdge = nextBEdge;
                    nextBEdge = nextBEdge.ToPoint.StartLine;
                    newPointAdded = true;
                }
                Presenter.ShowAndHang(result, closeShape: false);
                if (!newPointAdded)
                {
                    (var loopIndex, var vertexIndex) = aIndices; //= Math.Max(aIndices.Item1, bIndices.Item1);
                    if (bIndices.Item1 > loopIndex) (loopIndex, vertexIndex) = bIndices;

                    if (loopIndex == -1)
                    {   // weird. no path found and not because it encountered an existing loop (is this even possible)?
                        throw new Exception("path just ends...feasible?");
                        allKnownWrongPoints.Add(knownWrongPoints);
                        results.Add(new Polygon(result, currentLoopIndex, false, false));
                    }
                    else if (loopIndex == currentLoopIndex)
                    {   // so this run started with a strand and ended making a complete loop. return loop first, then strand
                        results.Add(new Polygon(result.Skip(vertexIndex), currentLoopIndex, false, true));
                        allKnownWrongPoints.Add(knownWrongPoints.Skip(vertexIndex).ToList());
                        results.Add(new Polygon(result.Take(vertexIndex), currentLoopIndex + 1, false, false));
                        allKnownWrongPoints.Add(knownWrongPoints.Take(vertexIndex).ToList());
                        Presenter.ShowAndHang(results, closeShape: true);
                    }
                    else // a strand has been produced that can be attached to a former strand
                    {
                        var formerStrand = results[loopIndex];
                        if (vertexIndex == 0)
                            results[loopIndex] = new Polygon(result.Concat(formerStrand.Path), loopIndex, false, false);
                        else throw new Exception("This should never happen");
                        knownWrongPoints.AddRange(allKnownWrongPoints[loopIndex]);
                        allKnownWrongPoints[loopIndex] = knownWrongPoints;
                    }
                    return;
                }
            } while (prevAEdge != aStartEdge || prevBEdge != bStartEdge);
            allKnownWrongPoints.Add(knownWrongPoints);
            results.Add(new Polygon(result, currentLoopIndex, false, true));
        }

        private static bool firstAngleIsBetweenOthersCCW(double aAngle, double bPrevAngle, double bAngle)
        {
            if (bPrevAngle <= aAngle && aAngle <= bAngle) return true;
            if (bPrevAngle > bAngle)
                if (aAngle >= bPrevAngle || aAngle <= bAngle) return true;
            return false;
        }

        private static List<Polygon> MinkowskiSumGeneral(Polygon a, List<Vertex2D> aConcaveVertices, Polygon b)
        {
            var polygons = new List<Polygon>();
            var aStartEdge = new Vertex2D[] { a.Edges[FindMinY(a.Vertices)].FromPoint };
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new Dictionary<(Vertex2D, Vertex2D), (int, int)>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var complexPaths = new List<Polygon>();
            var knownWrongPoints = new List<List<bool>>();
            foreach (var aConcavity in aStartEdge.Concat(aConcaveVertices))
            {
                var aAngle = aEdgeAngles[aConcavity.StartLine];
                foreach (var bVertex in b.Vertices)
                {
                    if (!visitedHash.ContainsKey((aConcavity, bVertex)))
                    {
                        var bPrevAngle = bEdgeAngles[bVertex.EndLine];
                        var bAngle = bEdgeAngles[bVertex.StartLine];
                        if (firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle))
                        {
                            ConvolutionCycle(a, b, aConcavity.StartLine, bVertex.EndLine,
                                visitedHash, aEdgeAngles, bEdgeAngles, complexPaths, knownWrongPoints);
                        }
                    }
                }
            }
            Presenter.ShowAndHang(complexPaths);
            return complexPaths.UnionPolygons().CreatePolygonTree(true);
            //return polygons.CreatePolygonTree(true);
        }

        private static Polygon MinkowskiDiffGeneral(Polygon a, Polygon b)
        {
            int aNum = a.Vertices.Count;
            var aPathD = new PathD(aNum);
            for (int i = 0; i < aNum; i++)
                aPathD.Add(new PointD(a.Path[i].X, a.Path[i].Y));

            int bNum = b.Vertices.Count;
            var bPathD = new PathD(bNum);
            for (int i = 0; i < bNum; i++)
                bPathD.Add(new PointD(b.Path[i].X, b.Path[i].Y));
            var pathsD = Clipper2Lib.Minkowski.Diff(aPathD, bPathD, true, 7);
            var resultPolygons = new List<Polygon>();
            foreach (var pathD in pathsD)
            {
                var path = new List<Vector2>();
                foreach (var pointD in pathD)
                    path.Add(new Vector2(pointD.x, pointD.y));
                resultPolygons.Add(new Polygon(path));
            }
            return resultPolygons.CreatePolygonTree(true).FirstOrDefault();
        }

    }

    internal class EdgePairToIntComparator : IEqualityComparer<(Vertex2D, Vertex2D)>
    {
        readonly int factor;
        public EdgePairToIntComparator(Polygon a, Polygon b)
        {
            factor = a.Vertices.Count;
        }
        public bool Equals((Vertex2D, Vertex2D) x, (Vertex2D, Vertex2D) y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public int GetHashCode([DisallowNull] (Vertex2D, Vertex2D) obj)
        {
            return obj.Item1.IndexInList + obj.Item2.IndexInList * factor;
        }
    }
}