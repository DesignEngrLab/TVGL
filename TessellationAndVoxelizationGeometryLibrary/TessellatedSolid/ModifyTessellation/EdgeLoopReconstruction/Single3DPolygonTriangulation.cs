using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TVGL.Numerics;

namespace TVGL
{

    internal class TriangulationLoop : EdgePath
    {
        public TriangulationLoop(IEnumerable<(Edge edge, bool dir)> inputs, bool reverse = false) : this()
        {
            if (reverse)
            {
                foreach (var tuple in inputs)
                {
                    EdgeList.Add(tuple.edge);
                    DirectionList.Add(!tuple.dir);
                }
                EdgeList.Reverse();
                DirectionList.Reverse();
            }
            else
            {
                foreach (var tuple in inputs)
                {
                    EdgeList.Add(tuple.edge);
                    DirectionList.Add(tuple.dir);
                }
            }
        }

        internal TriangulationLoop() : base()
        {
            IsClosed = true;
        }
        internal double Score { get; set; }
        internal Vector3 Normal { get; set; }
        internal int[] VertexIDList => GetVertices().Select(v => v.IndexInList).ToArray();
    }

    public class LoopOfIntsComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            var length = x.Length;
            if (x.Length != y.Length)
            {
                return false;
            }
            var yOffset = 0;
            for (; yOffset < length; yOffset++)
            {
                if (x[0] == y[yOffset]) break;
            }
            for (int i = 0; i < length; i++)
            {
                var yIndex = i + yOffset;
                if (yIndex == length)
                {
                    yIndex = 0;
                    yOffset -= length;
                }
                if (x[i] != y[yIndex])
                    return false;
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            var lowestIndex = int.MaxValue;
            var positionOfLowestIndex = -1;
            var length = obj.Length;
            for (int i = 0; i < length; i++)
            {
                var vIndex = obj[i];
                if (vIndex < lowestIndex)
                {
                    lowestIndex = vIndex;
                    positionOfLowestIndex = i;
                }
            }
            int result = 1;
            for (int i = positionOfLowestIndex; i < length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i];
                }
            }
            for (int i = 0; i < positionOfLowestIndex; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i];
                }
            }
            return result;
        }
    }


    public static class Single3DPolygonTriangulation
    {
        const int MaxStatesToSearch = 1000000000; // 1 billion

        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="edgePath">The edge path.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Vertex[], Vector3&gt;&gt;.</returns>
        public static IEnumerable<(List<Vertex> vertices, Vector3 normal)> QuickTriangulate(IList<(Edge edge, bool dir)> edgePath, double weightForSmoothness)
        {
            var triangles = QuickTriangulate(new TriangulationLoop(edgePath), weightForSmoothness);
            foreach (var triangle in triangles)
                yield return (triangle.GetVertices().ToList(), triangle.Normal);
        }

        internal static IEnumerable<TriangulationLoop> QuickTriangulate(TriangulationLoop edgeLoop, double weightForSmoothness)
        {
            var origNum = edgeLoop.Count;
            var sortedOrigCornerIndices = new SimplePriorityQueue<int, double>(); // sorted by the score/obj.fun and the key is the index
            // into the candidate triangles. Note that these do need to be updated occasionally
            var candidateTriangles = new TriangulationLoop[origNum];
            var neighborNormals = new Dictionary<Edge, Vector3>(); // for each each there is one side in which we should know the face normal
            // first we normalize the two obj fun terms by the max average area of a triangle, which would be if the edges were arranged like
            // a circle. This turns out to be the circumference squared divided by 4 divided by the number of triangles (edges - 2).
            var circumference = edgeLoop.Sum(e => e.edge.Length);
            weightForSmoothness *= circumference * circumference / (4 * (origNum - 2));

            #region Initial priority queue creation
            var prevEAD = edgeLoop[^1];  // EAD = Edge And Dir
            PolygonalFace prevFace;
            Vertex prevVertex, thisVertex;
            if (prevEAD.dir)
            {
                prevFace = prevEAD.edge.OtherFace;
                prevVertex = prevEAD.edge.From;
                thisVertex = prevEAD.edge.To;
            }
            else
            {
                prevFace = prevEAD.edge.OwnedFace;
                prevVertex = prevEAD.edge.To;
                thisVertex = prevEAD.edge.From;
            }
            for (int i = 0; i < origNum; i++)
            {
                var thisEAD = edgeLoop[i];
                PolygonalFace thisFace = thisEAD.dir ? thisEAD.edge.OtherFace : thisEAD.edge.OwnedFace;
                neighborNormals.Add(thisEAD.edge, thisFace.Normal);
                Vertex nextVertex;
                if (thisEAD.dir)
                {
                    thisFace = thisEAD.edge.OtherFace;
                    nextVertex = thisEAD.edge.To;
                }
                else
                {
                    thisFace = thisEAD.edge.OwnedFace;
                    nextVertex = thisEAD.edge.From;
                }
                Edge newEdge = new Edge(nextVertex, prevVertex, false);
                var triangle = new TriangulationLoop(new[] { prevEAD, thisEAD, (newEdge, true) });
                candidateTriangles[i] = triangle;
                sortedOrigCornerIndices.Enqueue(i, CalcObjFunction(weightForSmoothness, triangle, prevFace.Normal, thisFace.Normal, Vector3.Null));
                neighborNormals.Add(newEdge, triangle.Normal);
                prevEAD = thisEAD;
                prevFace = thisFace;
                prevVertex = thisVertex;
                thisVertex = nextVertex;
            }
            #endregion
            for (int i = 0; true; i++)
            //while (sortedVertices.Any())
            {
                var triangleIndex = sortedOrigCornerIndices.Dequeue();
                var triangle = candidateTriangles[triangleIndex];
                yield return triangle;
                if (i == origNum - 3) break;
                // that was the easy part, now we have to update the priority queue
                candidateTriangles[triangleIndex] = null;  // we set to null so that the next two little functions can find the two adjacent
                // corners which will need to be updated
                var posDirIndex = GetForwardIndex(candidateTriangles, triangleIndex);
                var negDirIndex = GetBackwardsIndex(candidateTriangles, triangleIndex);
                if (i == origNum - 4) // then this is second to last or last, then there is no need to update the neighbors
                {
                    sortedOrigCornerIndices.Remove(posDirIndex);
                    sortedOrigCornerIndices.Remove(negDirIndex);
                }
                else
                {
                    var posDirTriangle = candidateTriangles[posDirIndex];
                    var negDirTriangle = candidateTriangles[negDirIndex];
                    // this is tricky, and you kind of have to draw it out
                    posDirTriangle = new TriangulationLoop(new[] { (triangle[2].edge, !triangle[2].dir),posDirTriangle[1],
                (new Edge(posDirTriangle[1].dir?posDirTriangle[1].edge.To:posDirTriangle[1].edge.From,
                triangle.FirstVertex,false), true)});
                    candidateTriangles[posDirIndex] = posDirTriangle;
                    negDirTriangle = new TriangulationLoop(new[] { negDirTriangle[0], (triangle[2].edge, !triangle[2].dir),
                (new Edge(triangle[2].dir?triangle[2].edge.From:triangle[2].edge.To,negDirTriangle.FirstVertex,false),true)});
                    candidateTriangles[negDirIndex] = negDirTriangle;

                    sortedOrigCornerIndices.UpdatePriority(posDirIndex, CalcObjFunction(weightForSmoothness, posDirTriangle,
                        triangle.Normal, neighborNormals[posDirTriangle[1].edge], Vector3.Null));

                    sortedOrigCornerIndices.UpdatePriority(negDirIndex, CalcObjFunction(weightForSmoothness, negDirTriangle,
                        neighborNormals[negDirTriangle[0].edge], triangle.Normal, Vector3.Null));

                    neighborNormals.Remove(triangle[0].edge);
                    neighborNormals.Remove(triangle[1].edge);
                    //neighborNormals.Remove(triangle[2].edge); nope, don't remove this one, you still may need it 
                    neighborNormals.Add(posDirTriangle[2].edge, posDirTriangle.Normal);
                    neighborNormals.Add(negDirTriangle[2].edge, negDirTriangle.Normal);
                }
            }
        }

        private static int GetForwardIndex(TriangulationLoop[] candidateTriangles, int triangleIndex)
        {
            var i = triangleIndex;
            while (++i < candidateTriangles.Length)
                if (candidateTriangles[i] != null) return i;
            i = -1;
            while (++i < triangleIndex)
                if (candidateTriangles[i] != null) return i;
            return -1;
        }

        private static int GetBackwardsIndex(TriangulationLoop[] candidateTriangles, int triangleIndex)
        {
            var i = triangleIndex;
            while (--i >= 0)
                if (candidateTriangles[i] != null) return i;
            i = candidateTriangles.Length;
            while (--i > triangleIndex)
                if (candidateTriangles[i] != null) return i;
            return -1;
        }


        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="edgePath">The edge path.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Vertex[], Vector3&gt;&gt;.</returns>
        public static IEnumerable<(List<Vertex> vertices, Vector3 normal)> Triangulate(IList<(Edge edge, bool dir)> edgePath)
        {
            if (!Triangulate(new TriangulationLoop(edgePath), out var triangles)) yield break;
            foreach (var triangle in triangles)
            {
                yield return (triangle.GetVertices().ToList(), triangle.Normal);
            }
        }


        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        internal static bool Triangulate(TriangulationLoop startDomain, out List<TriangulationLoop> triangles)
        {
            triangles = null;
            var numTriangles = startDomain.Count - 2;
            var visitedDomains = new Dictionary<int[], TriangulationLoop>(new LoopOfIntsComparer());
            var maxSurfaceArea = startDomain.EdgeList.Sum(e => e.Length);
            var weightForDotProduct = 3 * maxSurfaceArea / numTriangles;
            var startingEdgeAndDirection = FindSharpestTurn(startDomain);

            var startingNeighborFaceNormal = startingEdgeAndDirection.dir ? startingEdgeAndDirection.edge.OtherFace.Normal
                : startingEdgeAndDirection.edge.OwnedFace.Normal;
            var fLimit = double.PositiveInfinity;
            var branchingFactorLimit = 0;
            var maxBranchingFactor = Math.Min(numTriangles,
                (int)Math.Floor(Math.Pow(MaxStatesToSearch, 1.0 / numTriangles)));
            while (branchingFactorLimit++ < maxBranchingFactor)
            {
                var fResult = Single3DPolygonTriangulation.TriangulateRecurse(startingNeighborFaceNormal, startingEdgeAndDirection.edge,
                       startDomain, visitedDomains, fLimit, branchingFactorLimit, weightForDotProduct, 0.0, out var tempTriangles);
                if (double.IsInfinity(fResult)) break;
                fLimit = fResult;
                triangles = tempTriangles;
            }
            if (double.IsInfinity(fLimit))
            {
//#if PRESENT
//                Presenter.ShowVertexPathsWithSolid(startDomain.EdgeList.Select(eg => new[] { eg.From.Coordinates, eg.To.Coordinates }), new TessellatedSolid[] { });
//#endif
                return false;
            }
            return true;
        }

        private static (Edge edge, bool dir) FindSharpestTurn(TriangulationLoop domain)
        {
            (Edge edge, bool dir) sharpestTurn = (null, false);
            var lowestDot = 1.0;
            for (int i = 0, j = domain.Count - 1; i < domain.Count; j = i++)
            {
                var turnJ = domain[j];
                var turnI = domain[i];
                var vJ = turnJ.edge.UnitVector;
                if (!turnJ.dir) vJ *= -1;
                var vI = turnI.edge.UnitVector;
                if (!turnI.dir) vI *= -1;
                var dot = vJ.Dot(vI);
                if (lowestDot > dot)
                {
                    lowestDot = dot;
                    sharpestTurn = turnJ;
                }
            }
            return sharpestTurn;
        }

        private static double TriangulateRecurse(Vector3 accessFaceNormal, Edge accessEdge, TriangulationLoop domain,
                Dictionary<int[], TriangulationLoop> visitedDomains, double upperLimit, int branchingFactorLimit, double dotWeight, in double currentValue, out List<TriangulationLoop> triangles)
        {
            var accessEdgeIndex = domain.IndexOf(accessEdge);
            var accessEdgeAndDir = domain[accessEdgeIndex];
            Vertex firstVertex, secondVertex;
            if (accessEdgeAndDir.dir)
            {
                firstVertex = accessEdgeAndDir.edge.From;
                secondVertex = accessEdgeAndDir.edge.To;
            }
            else
            {
                secondVertex = accessEdgeAndDir.edge.From;
                firstVertex = accessEdgeAndDir.edge.To;
            }
            var bestDomainScore = upperLimit;
            var sortedBranches = new SimplePriorityQueue<(TriangulationLoop, int), double>();
            //Debug.Indent();
            //Debug.WriteLine("main:  " + string.Join(',', domain.GetVertices().Select(v => v.IndexInList)));
            for (int i = 1; i < domain.Count - 1; i++)
            {
                var index = i + accessEdgeIndex;
                if (index >= domain.Count) index -= domain.Count;
                var edgeAt3rdVertex = domain[index];
                var thirdVertex = edgeAt3rdVertex.dir ? edgeAt3rdVertex.edge.To : edgeAt3rdVertex.edge.From;
                var thisTriangle = new TriangulationLoop();
                thisTriangle.Add(accessEdgeAndDir);
                var neighborNormal1 = Vector3.Null;
                if (i == 1)
                {
                    thisTriangle.Add(edgeAt3rdVertex);
                    var neighborFace = edgeAt3rdVertex.dir ? edgeAt3rdVertex.edge.OtherFace : edgeAt3rdVertex.edge.OwnedFace;
                    if (neighborFace != null) neighborNormal1 = neighborFace.Normal;
                }
                else thisTriangle.Add(new Edge(secondVertex, thirdVertex, false), true);
                if (secondVertex == thirdVertex) secondVertex = thirdVertex;
                var neighborNormal2 = Vector3.Null;
                if (i == domain.Count - 2)
                {
                    var lastEdgeAndDir = domain[accessEdgeIndex == 0 ? domain.Count - 1 : accessEdgeIndex - 1];
                    thisTriangle.Add(lastEdgeAndDir);
                    var neighborFace = lastEdgeAndDir.dir ? lastEdgeAndDir.edge.OtherFace : lastEdgeAndDir.edge.OwnedFace;
                    if (neighborFace != null) neighborNormal2 = neighborFace.Normal;
                }
                else thisTriangle.Add(new Edge(thirdVertex, firstVertex, false), true);
                if (firstVertex == thirdVertex) firstVertex = thirdVertex;

                thisTriangle.Score = CalcObjFunction(dotWeight, thisTriangle, accessFaceNormal, neighborNormal1, neighborNormal2);
                if (!double.IsInfinity(thisTriangle.Score))
                {
                    //visitedDomains.Add(thisTriangle.VertexIDList(), thisTriangle);
                    sortedBranches.Enqueue((thisTriangle, index), thisTriangle.Score);
                }
            }
            triangles = new List<TriangulationLoop>();
            foreach (var branch in sortedBranches.Take(branchingFactorLimit))
            {
                var thisTriangle = branch.Item1;
                var index = branch.Item2;
                var domainScore = thisTriangle.Score;
                if (currentValue + domainScore > bestDomainScore) continue;
                TriangulationLoop rightDomain, leftDomain;
                List<TriangulationLoop> rhsTriangles = null;
                var distance = index - accessEdgeIndex;
                if (distance < 0) distance += domain.Count;
                if (distance != 1) // if 1, then it's the first CCW triangle, so no right-side domain
                {
                    rightDomain = new TriangulationLoop();
                    rightDomain.Add((thisTriangle.EdgeList[1], false));
                    for (int j = 1; j <= distance; j++)
                    {
                        var innerIndex = j + accessEdgeIndex;
                        if (innerIndex >= domain.Count) innerIndex -= domain.Count;
                        rightDomain.Add(domain[innerIndex]);
                    }
                    //Debug.WriteLine("right: " + string.Join(',', rightDomain.GetVertices().Select(v => v.IndexInList)));

                    var vertexIDs = rightDomain.VertexIDList;
                    if (visitedDomains.ContainsKey(vertexIDs))
                        rightDomain = visitedDomains[vertexIDs];
                    else
                    {
                        rightDomain.Score = TriangulateRecurse(thisTriangle.Normal, thisTriangle.EdgeList[1], rightDomain, visitedDomains,
                            bestDomainScore, branchingFactorLimit,
                            dotWeight, currentValue + domainScore, out rhsTriangles);
                        if (!double.IsInfinity(rightDomain.Score))
                            visitedDomains.Add(vertexIDs, rightDomain);
                    }
                    domainScore += rightDomain.Score;
                }
                if (currentValue + domainScore > bestDomainScore) continue;

                List<TriangulationLoop> lhsTriangles = null;

                if (distance < domain.Count - 2) // if last one, then it's the last CCW triangle, so no left-side domain
                {
                    leftDomain = new TriangulationLoop();
                    leftDomain.Add((thisTriangle.EdgeList[2], false));
                    for (int j = 1; j < domain.Count - distance; j++)
                    {
                        var innerIndex = j + index;
                        if (innerIndex >= domain.Count) innerIndex -= domain.Count;
                        leftDomain.Add(domain[innerIndex]);
                    }
                    //Debug.WriteLine("left:  " + string.Join(',', leftDomain.GetVertices().Select(v => v.IndexInList)));

                    var vertexIDs = leftDomain.VertexIDList;
                    if (visitedDomains.ContainsKey(vertexIDs))
                        leftDomain = visitedDomains[vertexIDs];
                    else
                    {
                        leftDomain.Score = TriangulateRecurse(thisTriangle.Normal, thisTriangle.EdgeList[2], leftDomain, visitedDomains,
                            bestDomainScore, branchingFactorLimit,
                            dotWeight, currentValue + domainScore, out lhsTriangles);
                        if (!double.IsInfinity(leftDomain.Score))
                            visitedDomains.Add(vertexIDs, leftDomain);
                    }
                    domainScore += leftDomain.Score;
                }
                if (currentValue + domainScore > bestDomainScore) continue;

                //if (bestDomainScore > domainScore)
                //{
                bestDomainScore = currentValue + domainScore;
                triangles = new List<TriangulationLoop> { thisTriangle };
                if (rhsTriangles != null)
                    triangles.AddRange(rhsTriangles);
                if (lhsTriangles != null)
                    triangles.AddRange(lhsTriangles);
                //}
            }
            //Debug.Unindent();
            if (triangles.Any()) return bestDomainScore;
            return double.PositiveInfinity;
        }

        private static double CalcObjFunction(double dotWeight, TriangulationLoop triangle,
            Vector3 neighborNormal1, Vector3 neighborNormal2, Vector3 neighborNormal3)
        {
            //be sure to put in normal in the triangle
            var vector1 = triangle.EdgeList[0].Vector;
            if (!triangle.DirectionList[0]) vector1 *= -1;
            var vector2 = triangle.EdgeList[1].Vector;
            if (!triangle.DirectionList[1]) vector2 *= -1;
            var cross = vector1.Cross(vector2);
            var area = cross.Length(); // you're suppose to divided by 2 here but since all triangles scored this way,
                                       // then it's okay to work on the doubled value
            triangle.Normal = cross / area; // instead of calling Normalize, we've already found the Length so ever
                                            //so slightly quicker to divide by it here rather than calling .Normalize().
                                            //return area;
                                            //return area * (2 - neighborNormal.Dot(triangle.Normal));
            var dotPenalty = 0.0;
            if (!neighborNormal1.IsNull())
                dotPenalty += (1 - neighborNormal1.Dot(triangle.Normal));
            if (!neighborNormal2.IsNull())
                dotPenalty += (1 - neighborNormal2.Dot(triangle.Normal));
            if (!neighborNormal3.IsNull())
                dotPenalty += (1 - neighborNormal3.Dot(triangle.Normal));
            foreach (var ead in triangle)
            {
                var neighborFace = ead.dir ? ead.edge.OtherFace : ead.edge.OwnedFace;
                if (neighborFace != null)
                    dotPenalty += (1 - neighborFace.Normal.Dot(triangle.Normal));
            }
            //return area * dotPenalty;
            return area + dotWeight * dotPenalty;
            //return area * (1 + dotWeight * (1 - neighborNormal.Dot(triangle.Normal)));
        }
    }
}
