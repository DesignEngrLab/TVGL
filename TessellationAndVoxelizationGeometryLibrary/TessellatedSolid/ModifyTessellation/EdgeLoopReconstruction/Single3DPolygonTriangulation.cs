using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Triangulates the specified loop of 3D vertices using the projection from the provided normal.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="normal">The normal direction.</param>
        /// <returns>IEnumerable&lt;Vertex[]&gt; where each represents a triangular polygonal face.</returns>
        /// <exception cref="ArgumentException">The vertices must all have a unique IndexInList value - vertexLoop</exception>
        public static double Triangulate(Vector3 accessFaceNormal, Edge accessEdge, DomainClass domain,
            Dictionary<List<int>, DomainClass> visitedDomains, double upperLimit, double dotWeight, double currentValue, out List<DomainClass> triangles)
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
            triangles = new List<DomainClass>();
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
                if (i == domain.Count - 2)
                    thisTriangle.Add(domain[^1]);
                else thisTriangle.Add(new Edge(thirdVertex, firstVertex, false), true);
                var domainScore = CalcObjFunction(accessFaceNormal, dotWeight, thisTriangle);
                if (currentValue + domainScore > upperLimit) continue;

                DomainClass rightDomain, leftDomain;
                List<DomainClass> rhsTriangles = null;
                if (i > 1) // if 1, then it's the first CCW triangle, so no right-side domain
                {
                    rightDomain = new DomainClass();
                    rightDomain.Add((thisTriangle.EdgeList[1], false));
                    for (int j = 2; j <= i; j++)
                        rightDomain.Add(domain[j]);
                    var vertexIDs = rightDomain.VertexIDList();
                    if (visitedDomains.ContainsKey(vertexIDs))
                        rightDomain = visitedDomains[vertexIDs];
                    else
                    {
                        rightDomain.Score = Triangulate(thisTriangle.Normal, thisTriangle.EdgeList[1], rightDomain, visitedDomains, upperLimit, 
                            dotWeight, domainScore, out rhsTriangles);
                        visitedDomains.Add(vertexIDs, rightDomain);
                    }
                    domainScore += rightDomain.Score;
                }
                if (currentValue + domainScore > upperLimit) continue;

                List<DomainClass> lhsTriangles = null;

                if (i < domain.Count - 2) // if last one, then it's the last CCW triangle, so no left-side domain
                {
                    leftDomain = new DomainClass();
                    leftDomain.Add((thisTriangle.EdgeList[2], false));
                    for (int j = i + 1; j < domain.Count; j++)
                        leftDomain.Add(domain[j]);
                    var vertexIDs = leftDomain.VertexIDList();
                    if (visitedDomains.ContainsKey(vertexIDs))
                        leftDomain = visitedDomains[vertexIDs];
                    else
                    {
                        leftDomain.Score = Triangulate(thisTriangle.Normal, thisTriangle.EdgeList[2], leftDomain, visitedDomains, upperLimit, 
                            dotWeight, domainScore, out lhsTriangles);
                        visitedDomains.Add(vertexIDs, leftDomain);
                    }
                    domainScore += leftDomain.Score;
                }
                if (currentValue + domainScore > upperLimit) continue;

                if (bestDomainScore > domainScore)
                {
                    bestDomainScore = domainScore;
                    triangles = new List<DomainClass> { thisTriangle };
                    if (rhsTriangles != null)
                        triangles.AddRange(rhsTriangles);
                    if (lhsTriangles != null)
                        triangles.AddRange(lhsTriangles);
                }
            }
            return bestDomainScore;
        }

        private static double CalcObjFunction(Vector3 neighborNormal, double dotWeight, DomainClass triangle)
        {
            //be sure to put in normal in the triangle
            var vector1 = triangle.EdgeList[0].Vector;
            if (!triangle.DirectionList[0]) vector1 *= -1;
            var vector2 = triangle.EdgeList[1].Vector;
            if (!triangle.DirectionList[1]) vector2 *= -1;
            var cross = vector1.Cross(vector2);
            var area = cross.Length();
            triangle.Normal = cross.Normalize();
            return area - dotWeight * neighborNormal.Dot(triangle.Normal);
        }
    }
}
