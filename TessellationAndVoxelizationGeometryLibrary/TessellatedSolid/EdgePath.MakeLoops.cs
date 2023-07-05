// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="EdgePath.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using ClipperLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace TVGL
{
    public partial class EdgePath : IList<(Edge edge, bool dir)>
    {

        /// <summary>
        /// Gets the edge path loops around null border.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <returns>A list of EdgePaths.</returns>
        public static IEnumerable<EdgePath> GetEdgePathLoopsAroundNullBorder(IEnumerable<Edge> edges)
        {
            return GetEdgePathLoopsInner(edges, null);
        }
        /// <summary>
        /// Gets the edge path loops around input faces.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <param name="innerFaces">The inner faces.</param>
        /// <returns>A list of EdgePaths.</returns>
        public static IEnumerable<EdgePath> GetEdgePathLoopsAroundInputFaces(IEnumerable<Edge> edges, IEnumerable<TriangleFace> innerFaces)
        {
            return GetEdgePathLoopsInner(edges, innerFaces);
        }
        /// <summary>
        /// Gets the edge path segments.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <returns>A list of EdgePaths.</returns>
        public static IEnumerable<EdgePath> GetEdgePathSegments(IEnumerable<Edge> edges)
        {
            var vertexToEdgeDictionary = new Dictionary<Vertex, List<Edge>>();
            foreach (var edge in edges)
            {
                if (vertexToEdgeDictionary.TryGetValue(edge.From, out var entry))
                    entry.Add(edge);
                else vertexToEdgeDictionary.Add(edge.From, new List<Edge> { edge });
                if (vertexToEdgeDictionary.TryGetValue(edge.To, out entry))
                    entry.Add(edge);
                else vertexToEdgeDictionary.Add(edge.To, new List<Edge> { edge });
            }
            while (vertexToEdgeDictionary.Count > 0)
            {
                var entry = vertexToEdgeDictionary.First();
                var startVertex = entry.Key;
                var startEdge = entry.Value.First(ep => ep != null);
                var edge = startEdge;
                var vertex = edge.OtherVertex(startVertex);
                //var connections = entry.Value;
                //var edgeIndex = connections.FindIndex(ep => ep != null);
                //var dir = edge.From == vertex;
                //if (connections.Count(ep => ep != null) == 1)
                //    vertexToEdgeDictionary.Remove(vertex);
                //else connections[edgeIndex] = null;
                var edgePath = new EdgePath();
                var buildDirection = true;
                //if (buildDirection)
                //    edgePath.AddEnd(edge, dir);
                //else edgePath.AddBegin(edge, dir);
                //vertex = dir ? edge.To : edge.From;
                while (true)
                {
                    var newAdditionPossible = false;
                    if (vertexToEdgeDictionary.TryGetValue(vertex, out var connections))
                    {
                        int edgeIndex = connections.FindIndex(ep => ep == edge);
                        connections[edgeIndex] = null;
                        var dir = (edge.To == vertex) == buildDirection;
                        if (buildDirection)
                            edgePath.AddEnd(edge, dir);
                        else if (edge != edgePath.EdgeList[0])
                            edgePath.AddBegin(edge, dir);
                        if (connections.Count == 2) // && connections.Count(ep=>ep!=null)==1)
                        {
                            if (connections[0] == null)
                            {
                                edge = connections[1];
                                connections[1] = null;
                            }
                            else
                            {
                                edge = connections[0];
                                connections[0] = null;
                            }
                            newAdditionPossible = true;
                            if (connections.All(ep => ep == null))
                                vertexToEdgeDictionary.Remove(vertex);
                            vertex = edge.OtherVertex(vertex);
                        }
                        else if (connections.All(ep => ep == null))
                            vertexToEdgeDictionary.Remove(vertex);
                        //else // for 3, 4, 5, ... just remove the connection from the  
                        //connections[edgeIndex] = null;
                    }
                    if (!newAdditionPossible && buildDirection)
                    {
                        buildDirection = false;
                        vertex = startVertex;
                        edge = startEdge;
                    }
                    else if (!newAdditionPossible) break;
                }
                edgePath.UpdateIsClosed();
                yield return edgePath;
            }
        }

        private static IEnumerable<EdgePath> GetEdgePathLoopsInner(IEnumerable<Edge> edges, IEnumerable<TriangleFace> innerFaces)
        {
            var pathSegments = GetEdgePathSegments(edges);
            var vertexDictionary = new Dictionary<Vertex, List<EdgePath>>();
            HashSet<TriangleFace> faceHash = innerFaces == null ? null : new HashSet<TriangleFace>(innerFaces);
            foreach (var pathSegment in pathSegments)
            {
                if (pathSegment.IsClosed) yield return pathSegment;
                else
                {
                    if (vertexDictionary.TryGetValue(pathSegment.FirstVertex, out var entry))
                        entry.Add(pathSegment);
                    else vertexDictionary.Add(pathSegment.FirstVertex, new List<EdgePath> { pathSegment });
                    if (vertexDictionary.TryGetValue(pathSegment.LastVertex, out entry))
                        entry.Add(pathSegment);
                    else vertexDictionary.Add(pathSegment.LastVertex, new List<EdgePath> { pathSegment });
                }
            }
            while (vertexDictionary.Any(kvp => kvp.Value.Count > 1)) // match up according to the input rule
            {
                var firstKVP = vertexDictionary.First();
                if (firstKVP.Value.Count <= 1)
                    vertexDictionary.Remove(firstKVP.Key);
                else
                {
                    (var ep1, var ep2) = FindBestFaceEnclosingPair(firstKVP.Key, firstKVP.Value, faceHash);
                    ep1.AddRange(ep2);
                    if (ep1.UpdateIsClosed())
                    {
                        yield return ep1;
                        foreach (var entry in vertexDictionary)
                        {
                            entry.Value.Remove(ep1);
                            entry.Value.Remove(ep2);
                        }
                    }
                    else
                    {
                        firstKVP.Value.Remove(ep1);
                        firstKVP.Value.Remove(ep2);
                        foreach (var entry in vertexDictionary)
                        {
                            var ep2Index = entry.Value.FindIndex(ep => ep == ep2);
                            if (ep2Index >= 0)
                            {
                                entry.Value[ep2Index] = ep1;
                                break;
                            }
                        }
                    }
                }
            }
            //var verticesWithValenceOne = vertexToEdgeDictionary.Where(kvp => kvp.Value.Count == 1).Select(kvp => kvp.Key).ToList();
            foreach (var singleStrand in vertexDictionary)
            {
                yield return singleStrand.Value[0];
            }
        }

        private static (EdgePath ep1, EdgePath ep2) FindBestFaceEnclosingPair(Vertex vertex, List<EdgePath> edgePaths, 
            HashSet<TriangleFace> innerFaces = null)
        {
            var numEdges = edgePaths.Count;
            var enclosingVectors = MakeEnclosingVectors(vertex, edgePaths, innerFaces).ToList();
            var emanatingVectors = MakeEmanatingUnitVectors(vertex, edgePaths).ToList();
            var bestI = -1;
            var bestJ = -1;
            var bestAngle1 = double.MaxValue;
            var bestAngle2 = double.MaxValue;
            for (var i = 0; i < numEdges - 1; i++)
            {
                for (int j = i + 1; j < numEdges; j++)
                {
                    if (EdgePathsMakeClosedPath(edgePaths[i], edgePaths[j]))
                    {
                        bestAngle1 = double.MinValue;
                        bestAngle2 = double.MinValue;
                        bestI = i;
                        bestJ = j;
                    }
                    else
                    {
                        var angle1 = MiscFunctions.SmallerAngleBetweenVectorsEndToEnd(enclosingVectors[i], enclosingVectors[j]);
                        var angle2 = MiscFunctions.SmallerAngleBetweenVectorsSameStart(emanatingVectors[i], emanatingVectors[j]);
                        if (bestAngle1.IsGreaterThanNonNegligible(angle1) ||
                            (bestAngle1.IsPracticallySame(angle1) && bestAngle2 > angle2))
                        {
                            bestAngle1 = angle1;
                            bestAngle2 = angle2;
                            bestI = i;
                            bestJ = j;
                        }
                    }
                }
            }
            return (edgePaths[bestI], edgePaths[bestJ]);
        }

        private static bool EdgePathsMakeClosedPath(EdgePath edgePath1, EdgePath edgePath2)
        {
            return (edgePath1.FirstVertex == edgePath2.FirstVertex && edgePath1.LastVertex == edgePath2.LastVertex) ||
                (edgePath1.FirstVertex == edgePath2.LastVertex && edgePath1.LastVertex == edgePath2.FirstVertex);
        }

        private static IEnumerable<Vector3> MakeEnclosingVectors(Vertex vertex, List<EdgePath> edgePaths, HashSet<TriangleFace> innerFaces)
        {
            foreach (var edgePath in edgePaths)
            {
                var edge = (edgePath.FirstVertex == vertex) ? edgePath.EdgeList[0] : edgePath.EdgeList[^1];
                var edgeUnitVector = edge.Vector.Normalize();
                if (innerFaces != null && innerFaces.Count > 0)
                {
                    if (edge.OwnedFace != null && innerFaces.Contains(edge.OwnedFace))
                        yield return edge.OwnedFace.Normal.Cross(edgeUnitVector);
                    else if (edge.OtherFace != null && innerFaces.Contains(edge.OtherFace))
                        yield return edgeUnitVector.Cross(edge.OtherFace.Normal);
                    else yield return Vector3.Null;
                }
                else
                {
                    if (edge.OwnedFace == null && edge.OtherFace != null)
                        yield return edge.OtherFace.Normal.Cross(edgeUnitVector);
                    else if (edge.OtherFace == null && edge.OwnedFace != null)
                        yield return edgeUnitVector.Cross(edge.OwnedFace.Normal);
                    else yield return Vector3.Null;
                }
            }
        }
        private static IEnumerable<Vector3> MakeEmanatingUnitVectors(Vertex vertex, List<EdgePath> edgePaths)
        {
            foreach (var edgePath in edgePaths)
            {
                var edge = (edgePath.FirstVertex == vertex) ? edgePath.EdgeList[0] : edgePath.EdgeList[^1];
                var edgeUnitVector = edge.Vector.Normalize();
                if (edge.From == vertex)
                    yield return edgeUnitVector;
                else yield return -edgeUnitVector;
            }
        }

        #region from initial hole patching
        /// <summary>
        /// Organizes the into loops.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="hubVertices">The hub vertices.</param>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>List&lt;TriangulationLoop&gt;.</returns>
        internal static List<TriangulationLoop> OrganizeIntoLoops(IEnumerable<Edge> singleSidedEdges,
             out List<Edge> remainingEdges)
        {
            var hubVertices = FindHubVertices(singleSidedEdges);
            List<TriangulationLoop> listOfLoops = new List<TriangulationLoop>();
            var remainingEdgesInner = new HashSet<Edge>(singleSidedEdges);
            if (remainingEdgesInner.Count == 0)
            {
                remainingEdges = new List<Edge>();
                return listOfLoops;
            }
            var attempts = 0;
            while (remainingEdgesInner.Count > 0 && attempts < remainingEdgesInner.Count)
            {
                var loop = new TriangulationLoop();
                var successful = false;
                var removedEdges = new List<Edge>();
                Edge startingEdge;
                if (hubVertices.Any())
                {
                    var firstHubVertexTuple = hubVertices.First();
                    var firstHubVertex = firstHubVertexTuple.Key;
                    var hubEdgeCount = firstHubVertexTuple.Value;
                    startingEdge = remainingEdgesInner.First(e => e.From == firstHubVertex);
                    hubEdgeCount -= 2;
                    if (hubEdgeCount <= 2) hubVertices.Remove(firstHubVertex);
                    else hubVertices[firstHubVertex] = hubEdgeCount;
                    // because using this here will drop it down to 2 - in which case - it's just like all
                    // the other vertices in singledSidedEdges
                }
                else startingEdge = remainingEdgesInner.First();
                loop.AddBegin(startingEdge, true);  //all the directions really should be false since the edges were defined
                                                    //with the ownedFace but this is confusing and we'll switch later
                removedEdges.Add(startingEdge);
                remainingEdgesInner.Remove(startingEdge);
                do
                {
                    var lastLoop = loop.Last();
                    var lastVertex = lastLoop.dir ? lastLoop.edge.To : lastLoop.edge.From;
                    Edge bestNext = null;
                    if (hubVertices.ContainsKey(lastVertex))
                    {
                        var possibleNextEdges = lastVertex.Edges.Where(e => e != lastLoop.edge &&
                        e.From == lastVertex && remainingEdgesInner.Contains(e));
                        bestNext = possibleNextEdges.ChooseHighestCosineSimilarity(lastLoop.edge, !lastLoop.dir,
                            possibleNextEdges.Select(e => e.From == lastVertex), 0.0);
                        if (bestNext != null)
                        {
                            var hubEdgeCount = hubVertices[lastVertex];
                            hubEdgeCount -= 2;
                            hubVertices[lastVertex] = hubEdgeCount;
                            if (hubEdgeCount <= 2) hubVertices.Remove(lastVertex);
                        }
                    }
                    else bestNext = lastVertex.Edges.FirstOrDefault(e => e != lastLoop.edge &&
                        e.From == lastVertex && remainingEdgesInner.Contains(e));
                    if (bestNext != null)
                    {
                        loop.AddEnd(bestNext);
                        removedEdges.Add(bestNext);
                        remainingEdgesInner.Remove(bestNext);
                        successful = true;
                    }
                    else
                    {
                        lastLoop = loop[0];
                        lastVertex = lastLoop.dir ? lastLoop.edge.From : lastLoop.edge.To;

                        if (hubVertices.ContainsKey(lastVertex))
                        {
                            var possibleNextEdges = lastVertex.Edges.Where(e => e != lastLoop.edge &&
                            e.To == lastVertex && remainingEdgesInner.Contains(e));
                            bestNext = possibleNextEdges.ChooseHighestCosineSimilarity(lastLoop.edge, !lastLoop.dir,
                                possibleNextEdges.Select(e => e.From == lastVertex), 0.0);
                            if (bestNext != null)
                            {
                                var hubEdgeCount = hubVertices[lastVertex];
                                hubEdgeCount -= 2;
                                hubVertices[lastVertex] = hubEdgeCount;
                                if (hubEdgeCount <= 2) hubVertices.Remove(lastVertex);
                            }
                        }
                        else bestNext = lastVertex.Edges.FirstOrDefault(e => e != lastLoop.edge &&
                        e.To == lastVertex && remainingEdgesInner.Contains(e));
                        if (bestNext == null) break;

                        var dir = bestNext.To == lastVertex;
                        loop.AddBegin(bestNext, dir);
                        removedEdges.Add(bestNext);
                        remainingEdgesInner.Remove(bestNext);
                        successful = true;
                    }
                } while (loop.FirstVertex != (loop.DirectionList[^1] ? loop.EdgeList[^1].To : loop.EdgeList[^1].From)
                && successful);
                if (successful && loop.Count > 2)
                {
                    //#if PRESENT
                    //Presenter.ShowVertexPathsWithSolid(new[] { loop.GetVertices().Select(v => v.Coordinates) }.Skip(7), new TessellatedSolid[] { });
                    //#endif
                    foreach (var subLoop in SeparateIntoMultipleLoops(loop))
                        listOfLoops.Add(subLoop);
                    attempts = 0;
                }
                else
                {
                    foreach (var edge in removedEdges)
                        remainingEdgesInner.Add(edge);
                    attempts++;
                }
            }
            remainingEdges = remainingEdgesInner.ToList();
            return listOfLoops;
        }



        /// <summary>
        /// Separates the into multiple loops.
        /// </summary>
        /// <param name="loop">The loop.</param>
        /// <returns>IEnumerable&lt;TriangulationLoop&gt;.</returns>
        private static IEnumerable<TriangulationLoop> SeparateIntoMultipleLoops(TriangulationLoop loop)
        {
            var visitedToVertices = new HashSet<Vertex>(); //used initially to find when a coord repeats
            var vertexLocations = new Dictionary<Vertex, List<int>>(); //duplicate vertices and the indices where they occur
            var lastDuplicateAt = -1; // a small saving to prevent looping a full second time
            var i = -1;
            foreach (var vertex in loop.GetVertices())
            {
                i++;
                //var coord = loop[i].pathSegment.To;
                if (vertexLocations.TryGetValue(vertex, out var locationInts))
                {
                    lastDuplicateAt = i; // this is just to 
                    locationInts.Add(i);
                }
                else if (visitedToVertices.Contains(vertex))
                {
                    lastDuplicateAt = i; // this is just to 
                    vertexLocations.Add(vertex, new List<int> { i });
                }
                else visitedToVertices.Add(vertex);
            }
            i = -1;
            foreach (var vertex in loop.GetVertices().Take(lastDuplicateAt))
            {
                i++;
                if (vertexLocations.TryGetValue(vertex, out var otherIndices))
                {
                    if (!otherIndices.Contains(i))  // it's already been discovered
                        otherIndices.Insert(0, i);
                }
            }
            var loopStartEnd = new List<int>();
            foreach (var indices in vertexLocations.Values)
            {
                for (i = 0; i < indices.Count - 1; i++)
                {
                    loopStartEnd.Add(indices[i]);
                    loopStartEnd.Add(indices[i + 1]);
                }
            }
            while (loopStartEnd.Any())
            {
                var successfulUnknot = false;
                for (i = 0; i < loopStartEnd.Count; i += 2)
                {
                    var lb = loopStartEnd[i];
                    var ub = loopStartEnd[i + 1];
                    var loopEncompasssOther = false;
                    for (int j = 0; j < loopStartEnd.Count; j++)
                    {
                        if (j == i || j == i + 1) continue;
                        var index = loopStartEnd[j];
                        if (index > lb && index < ub)
                        {
                            loopEncompasssOther = true;
                            break;
                        }
                    }
                    if (loopEncompasssOther) continue;
                    yield return new TriangulationLoop(loop.GetRange(lb, ub), true);
                    successfulUnknot = true;
                    loop.RemoveRange(lb, ub);
                    loopStartEnd.RemoveAt(i); //remove the lb
                    loopStartEnd.RemoveAt(i); //remove the ub
                    var numInLoop = ub - lb;
                    for (int j = 0; j < loopStartEnd.Count; j++)
                        if (loopStartEnd[j] > lb) loopStartEnd[j] -= numInLoop;
                    break;
                }
                if (!successfulUnknot) break;
            }
            yield return new TriangulationLoop(loop, true);
        }



        /// <summary>
        /// Finds the hub vertices. These are vertices that connect to 3 or more single-sided edges. These are a good place to start
        /// looking for holes (in non-watertight solids) because they are difficult to handle when encountered DURING loop creation.
        /// So, the solution is to start with them.
        /// </summary>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>Dictionary&lt;Vertex, System.Int32&gt;.</returns>
        private static Dictionary<Vertex, int> FindHubVertices(IEnumerable<Edge> remainingEdges)
        {
            var dict = new Dictionary<Vertex, int>();
            foreach (var edge in remainingEdges)
            {
                if (dict.ContainsKey(edge.To)) dict[edge.To]++;
                else dict.Add(edge.To, 1);
                edge.To.Edges.Add(edge);
                if (dict.ContainsKey(edge.From)) dict[edge.From]++;
                else dict.Add(edge.From, 1);
                edge.From.Edges.Add(edge);
            }
            foreach (var key in dict.Keys.ToList())
                if (dict[key] <= 2)
                    dict.Remove(key);
            return dict;
        }
        #endregion

        #region from SimplifyTessellation

        /// <summary>
        /// Organizes the into loop.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="normal">The normal.</param>
        /// <returns>List&lt;Vertex&gt;.</returns>
        /// <exception cref="System.Exception"></exception>
        internal static List<Vertex> OrganizeIntoLoop(IEnumerable<Edge> singleSidedEdges, Vector3 normal)
        {
            var edgesHashSet = new HashSet<Edge>(singleSidedEdges);
            var loop = new List<Vertex>();
            var currentEdge = edgesHashSet.First();
            Vertex startVertex, currentVertex;
            if (normal.Dot(currentEdge.OwnedFace.Normal).IsPracticallySame(1))
            {
                startVertex = currentEdge.From;
                currentVertex = currentEdge.To;
            }
            else
            {
                startVertex = currentEdge.To;
                currentVertex = currentEdge.From;
            }
            edgesHashSet.Remove(currentEdge);
            loop.Add(startVertex);
            loop.Add(currentVertex);
            while (edgesHashSet.Any())
            {
                if (startVertex == currentVertex) return loop;
                var possibleNextEdges = currentVertex.Edges.Where(e => e != currentEdge && edgesHashSet.Contains(e)).ToList();
                if (!possibleNextEdges.Any()) throw new Exception();
                var lastEdge = currentEdge;
                currentEdge = (possibleNextEdges.Count == 1) ? possibleNextEdges.First()
                    : pickBestEdge(possibleNextEdges, currentEdge.Vector, normal);
                currentVertex = currentEdge.OtherVertex(currentVertex);
                loop.Add(currentVertex);
                edgesHashSet.Remove(currentEdge);
            }
            throw new Exception();
        }


        /// <summary>
        /// Picks the best pathSegment.
        /// </summary>
        /// <param name="possibleNextEdges">The possible next edges.</param>
        /// <param name="refEdge">The reference pathSegment.</param>
        /// <param name="normal">The normal.</param>
        /// <returns>Edge.</returns>
        private static Edge pickBestEdge(IEnumerable<Edge> possibleNextEdges, Vector3 refEdge, Vector3 normal)
        {
            var unitRefEdge = refEdge.Normalize();
            var min = 2.0;
            Edge bestEdge = null;
            foreach (var candEdge in possibleNextEdges)
            {
                var unitCandEdge = candEdge.Vector.Normalize();
                var cross = unitRefEdge.Cross(unitCandEdge);
                var temp = cross.Dot(normal);
                if (min > temp)
                {
                    min = temp;
                    bestEdge = candEdge;
                }
            }
            return bestEdge;
        }

        #endregion


        #region from MinimumEnclosure.cs


        /// <summary>
        /// Arranges the edges into loops, which are now stored in a BorderSegment object.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <param name="FacesToContain">The faces to contain.</param>
        /// <returns>IEnumerable&lt;BorderSegment&gt;.</returns>
        public static IEnumerable<PrimitiveBorder> GetLoops(HashSet<Edge> edges, HashSet<TriangleFace> FacesToContain)
        {
            while (edges.Any())
            {
                var currentEdge = edges.First();
                edges.Remove(currentEdge);
                var correctDirection = FacesToContain.Contains(currentEdge.OwnedFace);
                var startVertex = correctDirection ? currentEdge.From : currentEdge.To;
                var currentVertex = correctDirection ? currentEdge.To : currentEdge.From;
                var border = new PrimitiveBorder();
                border.AddEnd(currentEdge, correctDirection);
                foreach (var forwardDir in new[] { true, false })
                {
                    do
                    {
                        var possibleEdges = currentVertex.Edges.Where(e => e != currentEdge && edges.Contains(e)).ToList();
                        if (possibleEdges.Count == 0)
                        {
                            currentVertex = null;
                            currentEdge = null;
                            continue;
                        }
                        if (possibleEdges.Count == 1) currentEdge = possibleEdges[0];
                        else
                        {
                            var forwardVector = currentEdge.Vector.Normalize();
                            if (currentEdge.From == currentVertex) forwardVector *= -1;
                            var bestDot = double.NegativeInfinity;
                            Edge bestEdge = null;
                            foreach (var e in possibleEdges)
                            {
                                var candidateVector = e.Vector.Normalize();
                                if (e.To == currentVertex) candidateVector *= -1;
                                var dot = candidateVector.Dot(forwardVector);
                                if (bestDot < dot)
                                {
                                    bestDot = dot;
                                    bestEdge = e;
                                }
                            }
                            currentEdge = bestEdge;
                        }
                        correctDirection = (currentEdge.From == currentVertex) == forwardDir;
                        edges.Remove(currentEdge);
                        if (forwardDir) border.AddEnd(currentEdge, correctDirection);
                        else border.AddBegin(currentEdge, correctDirection);
                        currentVertex = currentEdge.OtherVertex(currentVertex);
                    } while (currentEdge != null && currentVertex != startVertex);
                    border.IsClosed = currentVertex == startVertex && border.NumPoints > 2;
                    //#if PRESENT
                    //TVGL.Presenter.ShowVertexPathsWithSolid(new [] {border.GetVertices().Select(v => v.Coordinates) }, new[] { debugSolid }, false);
                    //#endif
                    if (border.IsClosed) break;
                    var currentEdgeAndDir = border[0];
                    currentEdge = currentEdgeAndDir.edge;
                    currentVertex = currentEdgeAndDir.dir ? currentEdge.From : currentEdge.To;
                }
                yield return border;
            }
        }

        #endregion
    }
}
