using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TVGL.Numerics;

namespace TVGL
{

    internal class DomainClass : EdgePath
    {
        internal DomainClass()
        {
            IsClosed = true;
        }
        internal double Score { get; set; }
        internal Vector3 Normal { get; set; }

        internal List<int> VertexIDList()
        {
            //return GetVertices().Select(v => v.IndexInList).OrderBy(x => x).ToList();
            //instead of sorting the list, we simply need to change the starting location
            //to prevent isomorphic solutions. So, this longer, but faster method justs
            //shifts the start of the list to the lowest entry
            var lowestIndex = int.MaxValue;
            var positionOfLowestIndex = -1;
            var resultList = new List<int>();
            int i = 0;
            foreach (var vertex in GetVertices())
            {
                var vIndex = vertex.IndexInList;
                resultList.Add(vIndex);
                if (vIndex < lowestIndex)
                {
                    lowestIndex = vIndex;
                    positionOfLowestIndex = i;
                }
                i++;
            }
            for (i = 0; i < positionOfLowestIndex; i++)
            {
                var thisIndex = resultList[0];
                resultList.RemoveAt(0);
                resultList.Add(thisIndex);
            }
            return resultList;

        }
    }
    internal static class Single3DPolygonTriangulation
    {
        const int MaxStatesToSearch = 1000000000; // 1 billion
        // this is all the Catalan Numbers that fit within the bounds of int32. In this context, they represent the maximum
        // number of triangulations for a given 3D polygon.
        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static bool Triangulate(DomainClass startDomain, out List<DomainClass> triangles)
        {
            triangles = null;
            var numTriangles = startDomain.Count - 2;
            var visitedDomains = new Dictionary<List<int>, DomainClass>();
            var maxSurfaceArea = startDomain.EdgeList.Sum(e => e.Length);
            maxSurfaceArea *= 0.25 * maxSurfaceArea; //the max surface area is to take the perimeter and assume that 
                                                     // the shape is a circle. I never thought about the before, but the area of a circle is the circumference 
                                                     // squared divided by 4.
            var weightForDotProduct = maxSurfaceArea / numTriangles;
            (Edge edge, bool dir) startingEdgeAndDirection = FindSharpestTurn(startDomain);

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
#if PRESENT
                Presenter.ShowVertexPathsWithSolid(startDomain.EdgeList.Select(eg => new[] { eg.From.Coordinates, eg.To.Coordinates }), new TessellatedSolid[] { });
#endif
                return false;
            }
            return true;
        }

        private static (Edge edge, bool dir) FindSharpestTurn(DomainClass domain)
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

        private static double TriangulateRecurse(Vector3 accessFaceNormal, Edge accessEdge, DomainClass domain,
                Dictionary<List<int>, DomainClass> visitedDomains, double upperLimit, int branchingFactorLimit, double dotWeight, in double currentValue, out List<DomainClass> triangles)
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
            var sortedBranches = new SimplePriorityQueue<(DomainClass, int), double>();
            Debug.Indent();
            Debug.WriteLine("main:  " + string.Join(',', domain.GetVertices().Select(v => v.IndexInList)));
            for (int i = 1; i < domain.Count - 1; i++)
            {
                var index = i + accessEdgeIndex;
                if (index >= domain.Count) index -= domain.Count;
                var edgeAt3rdVertex = domain[index];
                var thirdVertex = edgeAt3rdVertex.dir ? edgeAt3rdVertex.edge.To : edgeAt3rdVertex.edge.From;
                var thisTriangle = new DomainClass();
                thisTriangle.Add(accessEdgeAndDir);
                if (i == 1)
                    thisTriangle.Add(edgeAt3rdVertex);
                else thisTriangle.Add(new Edge(secondVertex, thirdVertex, false), true);
                if (secondVertex == thirdVertex) secondVertex = thirdVertex;
                if (i == domain.Count - 2)
                    thisTriangle.Add(domain[^1]);
                else thisTriangle.Add(new Edge(thirdVertex, firstVertex, false), true);
                if (firstVertex == thirdVertex) firstVertex = thirdVertex;

                thisTriangle.Score = CalcObjFunction(accessFaceNormal, dotWeight, thisTriangle);
                if (!double.IsInfinity(thisTriangle.Score))
                {
                    //visitedDomains.Add(thisTriangle.VertexIDList(), thisTriangle);
                    sortedBranches.Enqueue((thisTriangle, index), thisTriangle.Score);
                }
            }
            triangles = new List<DomainClass>();
            foreach (var branch in sortedBranches.Take(branchingFactorLimit))
            {
                var thisTriangle = branch.Item1;
                var index = branch.Item2;
                var domainScore = thisTriangle.Score;
                if (currentValue + domainScore > bestDomainScore) continue;
                DomainClass rightDomain, leftDomain;
                List<DomainClass> rhsTriangles = null;
                var distance = index - accessEdgeIndex;
                if (distance < 0) distance += domain.Count;
                if (distance != 1) // if 1, then it's the first CCW triangle, so no right-side domain
                {
                    rightDomain = new DomainClass();
                    rightDomain.Add((thisTriangle.EdgeList[1], false));
                    for (int j = 1; j <= distance; j++)
                    {
                        var innerIndex = j + accessEdgeIndex;
                        if (innerIndex >= domain.Count) innerIndex -= domain.Count;
                        rightDomain.Add(domain[innerIndex]);
                    }
                    Debug.WriteLine("right: " + string.Join(',', rightDomain.GetVertices().Select(v => v.IndexInList)));

                    var vertexIDs = rightDomain.VertexIDList();
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

                List<DomainClass> lhsTriangles = null;

                if (distance < domain.Count - 2) // if last one, then it's the last CCW triangle, so no left-side domain
                {
                    leftDomain = new DomainClass();
                    leftDomain.Add((thisTriangle.EdgeList[2], false));
                    for (int j = 1; j < domain.Count -distance; j++)
                    {
                        var innerIndex = j + index;
                        if (innerIndex >= domain.Count) innerIndex -= domain.Count;
                        leftDomain.Add(domain[innerIndex]);
                    }
                    Debug.WriteLine("left:  " + string.Join(',', leftDomain.GetVertices().Select(v => v.IndexInList)));

                    var vertexIDs = leftDomain.VertexIDList();
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
                triangles = new List<DomainClass> { thisTriangle };
                if (rhsTriangles != null)
                    triangles.AddRange(rhsTriangles);
                if (lhsTriangles != null)
                    triangles.AddRange(lhsTriangles);
                //}
            }
            Debug.Unindent();
            if (triangles.Any()) return bestDomainScore;
            return double.PositiveInfinity;
        }

        private static double CalcObjFunction(Vector3 neighborNormal, double dotWeight, DomainClass triangle)
        {
            //be sure to put in normal in the triangle
            var vector1 = triangle.EdgeList[0].Vector;
            if (!triangle.DirectionList[0]) vector1 *= -1;
            var vector2 = triangle.EdgeList[1].Vector;
            if (!triangle.DirectionList[1]) vector2 *= -1;
            var cross = vector1.Cross(vector2);
            var area = cross.Length(); // you're suppose to divided by 2 here but since all triangles scored this way,
                                       // then it's okay to work on the doubled value
            triangle.Normal = cross.Normalize();
            return area;
            //return area * (2 - neighborNormal.Dot(triangle.Normal));
            //return area + dotWeight * (1 - neighborNormal.Dot(triangle.Normal));
        }
    }
}
