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
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class MiscFunctions
    {

        public static IEnumerable<EdgePath> MakeEdgePaths(this IEnumerable<Edge> edges, bool preferMakeLoopsOverComparer,
            IEdgePathPairEvaluator bestEdgePathComparer)
        {
            return MakeEdgePaths(edges.Select(e => new EdgePath(e)), preferMakeLoopsOverComparer, bestEdgePathComparer);
        }

        public static IEnumerable<EdgePath> MakeEdgePaths(this IEnumerable<EdgePath> inputEdgePaths, bool preferMakeLoopsOverComparer,
            IEdgePathPairEvaluator bestEdgePathComparer)
        {
            var vertexToEdgeDictionary = new Dictionary<Vertex, List<EdgePath>>();
            foreach (var edgePath in inputEdgePaths)
            {
                List<EdgePath> edgePathList;
                if (!vertexToEdgeDictionary.TryGetValue(edgePath.FirstVertex, out edgePathList))
                {
                    edgePathList = new List<EdgePath>();
                    vertexToEdgeDictionary.Add(edgePath.FirstVertex, edgePathList);
                }
                edgePathList.Add(edgePath);

                if (!vertexToEdgeDictionary.TryGetValue(edgePath.LastVertex, out edgePathList))
                {
                    edgePathList = new List<EdgePath>();
                    vertexToEdgeDictionary.Add(edgePath.LastVertex, edgePathList);
                }
                edgePathList.Add(edgePath);
            }
            var vertsSortedByValence = new UpdatablePriorityQueue<Vertex, int>();
            foreach (var vertex in vertexToEdgeDictionary.Keys)
                vertsSortedByValence.Enqueue(vertex, vertexToEdgeDictionary[vertex].Count);
            var verticesAtEndOfNonCyclicPaths = new HashSet<Vertex>();
            while (vertsSortedByValence.Count > 0)
            {
                var vertex = vertsSortedByValence.Dequeue();
                var edgePaths = vertexToEdgeDictionary[vertex];
                vertexToEdgeDictionary.Remove(vertex);
                while (edgePaths.Count > 1)
                {
                    (var ep1, var ep2) = FindBestEdgePathPair(vertex, edgePaths,
                        preferMakeLoopsOverComparer, vertexToEdgeDictionary, bestEdgePathComparer);
                    edgePaths.Remove(ep1);
                    edgePaths.Remove(ep2);
                    if (ep1 != ep2)
                        ep1.AddRange(ep2);

                    if (ep1.UpdateIsClosed() || ep1 == ep2)
                    {
                        foreach (var v in ep1.GetVertices())
                        {
                            if (vertexToEdgeDictionary.TryGetValue(v, out var listOfEps))
                            {
                                var ep1Removed = listOfEps.Remove(ep1);
                                var ep2Removed = listOfEps.Remove(ep2);
                                if (listOfEps.Count == 0)
                                    vertsSortedByValence.Remove(v);
                                else if (ep1Removed || ep2Removed)
                                    vertsSortedByValence.UpdatePriority(v, listOfEps.Count);
                            }
                        }
                        foreach (var innerEdgePath in SplitSelfCrossingEdgePaths(ep1))
                            yield return innerEdgePath;
                    }
                    else
                    {
                        foreach (var v in ep2.GetVertices())
                        {
                            if (vertexToEdgeDictionary.TryGetValue(v, out var listOfEps))
                            {
                                var ep2Index = listOfEps.FindIndex(ep => ep == ep2);
                                if (ep2Index >= 0)
                                    listOfEps[ep2Index] = ep1;
                            }
                        }
                    }
                }
                if (edgePaths.Count == 1)
                {
                    var edgePath = edgePaths[0];
                    var otherEnd = edgePath.FirstVertex == vertex ? edgePath.LastVertex : edgePath.FirstVertex;
                    if (verticesAtEndOfNonCyclicPaths.Contains(otherEnd))
                    {
                        foreach (var innerEdgePath in SplitSelfCrossingEdgePaths(edgePath))
                            yield return innerEdgePath;
                        verticesAtEndOfNonCyclicPaths.Remove(otherEnd);
                    }
                    else verticesAtEndOfNonCyclicPaths.Add(vertex);
                }
            }
        }
        public static IEnumerable<EdgePath> MakeEdgePathsTooNew(this IEnumerable<Edge> edges, bool preferMakeLoopsOverComparer,
            IEdgePathPairEvaluator bestEdgePathComparer)
        {
            var vertexWithOneDictionary = new Dictionary<Vertex, EdgePath>();
            var vertexWithTwoDictionary = new Dictionary<Vertex, (EdgePath, EdgePath)>();
            var vertexWithHighValenceDictionary = new Dictionary<Vertex, List<EdgePath>>();
            foreach (var edge in edges)
            {
                var edgePath = new EdgePath();
                edgePath.AddBegin(edge, true);
                for (int i = 0; i < 2; i++)
                {
                    var vertexOfEdge = i == 0 ? edge.From : edge.To;
                    if (vertexWithHighValenceDictionary.TryGetValue(vertexOfEdge, out var edgePathList))
                        edgePathList.Add(edgePath);
                    else if (vertexWithTwoDictionary.TryGetValue(vertexOfEdge, out var edgePathPair))
                    {
                        vertexWithTwoDictionary.Remove(vertexOfEdge);
                        vertexWithHighValenceDictionary[vertexOfEdge] = new List<EdgePath> { edgePathPair.Item1, edgePathPair.Item2, edgePath };
                    }
                    else if (vertexWithOneDictionary.TryGetValue(vertexOfEdge, out var edgePath1))
                    {
                        vertexWithOneDictionary.Remove(vertexOfEdge);
                        vertexWithTwoDictionary[vertexOfEdge] = (edgePath1, edgePath);
                    }
                    else vertexWithOneDictionary[vertexOfEdge] = edgePath;
                }
            }
            var verticesAtEndOfNonCyclicPaths = new HashSet<Vertex>();
            MakeStrandsFromEdgePaths(vertexWithOneDictionary, vertexWithTwoDictionary,
                           out verticesAtEndOfNonCyclicPaths, out List<EdgePath> completedAcyclicStrands, out List<EdgePath> simpleCycles);

            foreach (var e in simpleCycles.Concat(completedAcyclicStrands))
                yield return e;
            foreach (var kvp in vertexWithTwoDictionary)
                vertexWithHighValenceDictionary.Add(kvp.Key, new List<EdgePath> { kvp.Value.Item1, kvp.Value.Item2 });
            var vertsSortedByValence = new UpdatablePriorityQueue<Vertex, int>();
            foreach (var vertex in vertexWithHighValenceDictionary.Keys)
                vertsSortedByValence.Enqueue(vertex, vertexWithHighValenceDictionary[vertex].Count);
            while (vertsSortedByValence.Count > 0)
            {
                var vertex = vertsSortedByValence.Dequeue();
                var edgePaths = vertexWithHighValenceDictionary[vertex];
                vertexWithHighValenceDictionary.Remove(vertex);
                while (edgePaths.Count > 1)
                {
                    (var ep1, var ep2) = FindBestEdgePathPair(vertex, edgePaths,
                        preferMakeLoopsOverComparer, vertexWithHighValenceDictionary, bestEdgePathComparer);
                    ep1.AddRange(ep2);
                    edgePaths.Remove(ep1);
                    edgePaths.Remove(ep2);
                    if (ep1.UpdateIsClosed())
                    {
                        foreach (var v in ep1.GetVertices())
                        {
                            if (vertexWithHighValenceDictionary.TryGetValue(v, out var listOfEps))
                            {
                                var ep1Removed = listOfEps.Remove(ep1);
                                var ep2Removed = listOfEps.Remove(ep2);
                                if (listOfEps.Count == 0)
                                    vertsSortedByValence.Remove(v);
                                else if (ep1Removed || ep2Removed)
                                    vertsSortedByValence.UpdatePriority(v, listOfEps.Count);
                            }
                        }
                        foreach (var innerEdgePath in SplitSelfCrossingEdgePaths(ep1))
                            yield return innerEdgePath;
                    }
                    else
                    {
                        foreach (var v in ep2.GetVertices())
                        {
                            if (vertexWithHighValenceDictionary.TryGetValue(v, out var listOfEps))
                            {
                                var ep2Index = listOfEps.FindIndex(ep => ep == ep2);
                                if (ep2Index >= 0)
                                    listOfEps[ep2Index] = ep1;
                            }
                        }
                    }
                }
                if (edgePaths.Count == 1)
                {
                    var edgePath = edgePaths[0];
                    var otherEnd = edgePath.FirstVertex == vertex ? edgePath.LastVertex : edgePath.FirstVertex;
                    if (verticesAtEndOfNonCyclicPaths.Contains(otherEnd))
                    {
                        foreach (var innerEdgePath in SplitSelfCrossingEdgePaths(edgePath))
                            yield return innerEdgePath;
                        verticesAtEndOfNonCyclicPaths.Remove(otherEnd);
                    }
                    else verticesAtEndOfNonCyclicPaths.Add(vertex);
                }
            }
        }

        private static void MakeStrandsFromEdgePaths(Dictionary<Vertex, EdgePath> vertexWithOneDictionary,
            Dictionary<Vertex, (EdgePath, EdgePath)> vertexWithTwoDictionary,
            out HashSet<Vertex> verticesAtEndOfNonCyclicPaths, out List<EdgePath> completedAcyclicStrands, out List<EdgePath> simpleCycles)
        {
            var edgePathsAppearingOnce = vertexWithOneDictionary.Values.ToHashSet();
            var edgePathsAppearingTwice = new HashSet<EdgePath>();
            foreach (var edgePathPair in vertexWithTwoDictionary.Values)
            {
                if (!edgePathsAppearingOnce.Add(edgePathPair.Item1))
                {
                    edgePathsAppearingOnce.Remove(edgePathPair.Item1);
                    edgePathsAppearingTwice.Add(edgePathPair.Item1);
                }
                if (!edgePathsAppearingOnce.Add(edgePathPair.Item2))
                {
                    edgePathsAppearingOnce.Remove(edgePathPair.Item2);
                    edgePathsAppearingTwice.Add(edgePathPair.Item2);
                }
            }
            completedAcyclicStrands = new List<EdgePath>();
            verticesAtEndOfNonCyclicPaths = new HashSet<Vertex>();
            foreach (var kvp in vertexWithOneDictionary)
            {
                var endVertex = kvp.Key;
                var nextVertex = kvp.Value.FirstVertex == endVertex ? kvp.Value.LastVertex : kvp.Value.FirstVertex;
                var edgePath = kvp.Value;
                while (vertexWithTwoDictionary.TryGetValue(nextVertex, out var edgePathPair))
                {
                    edgePathsAppearingTwice.Remove(edgePath);
                    vertexWithTwoDictionary.Remove(nextVertex); // we don't need to process this vertex again and we want to cycle of the dictionary below
                    var nextEdgePath = edgePathPair.Item1 == edgePath ? edgePathPair.Item2 : edgePathPair.Item1;
                    nextEdgePath.AddRange(edgePath);
                    edgePath = nextEdgePath;
                    nextVertex = kvp.Value.FirstVertex == endVertex ? edgePath.LastVertex : edgePath.FirstVertex;
                }
                if (vertexWithOneDictionary.ContainsKey(nextVertex))
                    completedAcyclicStrands.Add(edgePath);
                else
                    verticesAtEndOfNonCyclicPaths.Add(endVertex);
            }
            simpleCycles = new List<EdgePath>();
            while (edgePathsAppearingTwice.Count > 0)
            {
                var edgePath = edgePathsAppearingTwice.First();
                edgePathsAppearingTwice.Remove(edgePath);
                var newCycle = new EdgePath();
                newCycle.AddEnd(edgePath.EdgeList[0], edgePath.DirectionList[0]);
                var startVertex = edgePath.FirstVertex;
                var nextVertex = edgePath.LastVertex;
                while (vertexWithTwoDictionary.TryGetValue(nextVertex, out var edgePathPair))
                {
                    edgePath = edgePathPair.Item1 == edgePath ? edgePathPair.Item2 : edgePathPair.Item1;
                    edgePathsAppearingTwice.Remove(edgePath);
                    newCycle.AddRange(edgePath);
                    nextVertex = edgePath.FirstVertex == nextVertex ? edgePath.LastVertex : edgePath.FirstVertex;
                    if (nextVertex == startVertex)
                    {
                        newCycle.UpdateIsClosed();
                        simpleCycles.Add(newCycle);
                        foreach (var v in newCycle.GetVertices())
                            vertexWithTwoDictionary.Remove(v);
                        break;
                    }
                }
            }
        }

        private static (EdgePath ep1, EdgePath ep2) FindBestEdgePathPair(Vertex vertex, List<EdgePath> edgePaths,
            bool preferMakeLoopsOverComparer, Dictionary<Vertex, List<EdgePath>> vertexToEdgeDictionary, IEdgePathPairEvaluator edgePathEvaluator)
        {
            if (edgePaths.Count == 2) return (edgePaths[0], edgePaths[1]);
            var numEdges = edgePaths.Count;
            // first, check for the same edge path in the list, this would mean that we are simply closing a loop
            for (var i = 0; i < numEdges - 1; i++)
                for (int j = i + 1; j < numEdges; j++)
                    if (edgePaths[i] == edgePaths[j])
                        return (edgePaths[i], edgePaths[j]);
            if (preferMakeLoopsOverComparer)
            {
                // second, check if the two edge paths connect v-to-v. here we would combine them into one, then - later - 
                // they'll connect from the above condition.
                for (var i = 0; i < numEdges - 1; i++)
                    for (int j = i + 1; j < numEdges; j++)
                        if ((edgePaths[i].FirstVertex == edgePaths[j].FirstVertex && edgePaths[i].LastVertex == edgePaths[j].LastVertex)
                            || (edgePaths[i].FirstVertex == edgePaths[j].LastVertex && edgePaths[i].LastVertex == edgePaths[j].FirstVertex))
                            return (edgePaths[i], edgePaths[j]);
                // third, if the other v of the paths share a common edgePath, then connect these to lead to completing the
                // loop by basically calling the above condition, which in turn calls the first condition. We could keep going with this
                // logic, but anything beyond this becomes n^2, so we'll stop here. The imperfect EdgePathPairEvaluator below will 
                // handle the rest.
                for (var i = 0; i < numEdges - 1; i++)
                    for (int j = i + 1; j < numEdges; j++)
                    {
                        var otherEndI = edgePaths[i].FirstVertex == vertex ? edgePaths[i].LastVertex : edgePaths[i].FirstVertex;
                        var otherEndJ = edgePaths[j].FirstVertex == vertex ? edgePaths[j].LastVertex : edgePaths[j].FirstVertex;
                        if (vertexToEdgeDictionary.TryGetValue(otherEndI, out var otherEndIEdgePaths) &&
                            vertexToEdgeDictionary.TryGetValue(otherEndJ, out var otherEndJEdgePaths) &&
                            otherEndIEdgePaths.Intersect(otherEndJEdgePaths).Any())
                            return (edgePaths[i], edgePaths[j]);
                    }
            }
            // finally, use the comparer to find the best pair
            var enclosingVectors = edgePaths.Select(ep => edgePathEvaluator.CharacterizeAsVector(vertex, ep)).ToList();
            var bestI = -1;
            var bestJ = -1;
            var bestScore = double.MaxValue;
            for (var i = 0; i < numEdges - 1; i++)
            {
                for (int j = i + 1; j < numEdges; j++)
                {
                    var score = edgePathEvaluator.ScorePair(enclosingVectors[i], enclosingVectors[j]);
                    if (bestScore > score)
                    {
                        bestScore = score;
                        bestI = i;
                        bestJ = j;
                    }
                }
            }
            return (edgePaths[bestI], edgePaths[bestJ]);
        }



        private static IEnumerable<EdgePath> SplitSelfCrossingEdgePaths(EdgePath origEdgePath)
        {
            // first check if the edge path needs to split. This is a time/space saving measure
            // since most will not need to split.
            var origVertices = origEdgePath.GetVertices().ToList();
            var vertexHash = new HashSet<Vertex>();
            var duplicatesPresent = false;
            foreach (var v in origVertices)
            {
                if (!vertexHash.Add(v))
                {
                    duplicatesPresent = true;
                    break; // there are duplicates, so we need to split
                }
            }
            if (!duplicatesPresent)
            {
                yield return origEdgePath;
                yield break;
            }
            // if we get here, then we need to split the edge path, start over the hash/dictionary
            // thing by keeping track of positions of vertices in the original edge path
            var vertexPositions = new Dictionary<Vertex, List<int>>();
            var index = 0;
            foreach (var v in origVertices)
            {
                if (vertexPositions.TryGetValue(v, out var prevIndices))
                    prevIndices.Add(index);
                else vertexPositions.Add(v, new List<int> { index });
                index++;
            }
            while (vertexPositions.Count > 0)
            {
                var startVertexKVP = vertexPositions.FirstOrDefault(kvp => kvp.Value.Count > 1);
                if (startVertexKVP.Equals(default(KeyValuePair<Vertex, List<int>>)))
                    startVertexKVP = vertexPositions.First();
                var startVertex = startVertexKVP.Key;
                var vertex = startVertex;
                var newEdgePath = new EdgePath();
                do
                {
                    var indices = vertexPositions[vertex];
                    if (indices.Count == 1)
                    {
                        index = indices[0];
                        vertexPositions.Remove(vertex);
                    }
                    else
                    {   // in order to avoid the same tangling issue that occurred
                        // in the origEdgePath, we choose the next index that breaks
                        // the sequence of listOfEps.
                        var indexToAvoid = index + 1; //the index to avoid would be the next one in the sequence
                        if (indexToAvoid == origEdgePath.Count)
                            // or it could be the first if wrapping around
                            indexToAvoid = 0;
                        var i = 0;  // choose an index that is not the one to avoid
                        if (indices[0] == indexToAvoid) { i = 1; }
                        index = indices[i];
                        indices.RemoveAt(i);
                    }
                    if (index >= origEdgePath.Count) break; // this happens for acyclic edge paths
                    newEdgePath.AddEnd(origEdgePath.EdgeList[index], origEdgePath.DirectionList[index]);
                    vertex = newEdgePath.LastVertex;
                } while (vertex != startVertex);
                newEdgePath.UpdateIsClosed();
                yield return newEdgePath;
            }
        }
    }
}
