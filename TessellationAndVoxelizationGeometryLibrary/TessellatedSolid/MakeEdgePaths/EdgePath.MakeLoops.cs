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
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class MiscFunctions
    {
        public static IEnumerable<EdgePath> MakeEdgePaths(this IEnumerable<Edge> edges, bool preferMakeLoopsOverComparer,
            IEdgePathPairEvaluator bestEdgePathComparer)
        {
            var vertexToEdgeDictionary = new Dictionary<Vertex, List<EdgePath>>();
            foreach (var edge in edges)
            {
                var edgePath = new EdgePath();
                edgePath.AddBegin(edge, true);
                List<EdgePath> edgePathList;
                if (!vertexToEdgeDictionary.TryGetValue(edge.From, out edgePathList))
                {
                    edgePathList = new List<EdgePath>();
                    vertexToEdgeDictionary.Add(edge.From, edgePathList);
                }
                edgePathList.Add(edgePath);

                if (!vertexToEdgeDictionary.TryGetValue(edge.To, out edgePathList))
                {
                    edgePathList = new List<EdgePath>();
                    vertexToEdgeDictionary.Add(edge.To, edgePathList);
                }
                edgePathList.Add(edgePath);
            }
            var vertsSortedByValence = new SimplePriorityQueue<Vertex, int>();
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
                    ep1.AddRange(ep2);
                    edgePaths.Remove(ep1);
                    edgePaths.Remove(ep2);
                    if (ep1.UpdateIsClosed())
                    {
                        foreach (var entry in vertexToEdgeDictionary)
                        {
                            var ep1Removed = entry.Value.Remove(ep1);
                            var ep2Removed = entry.Value.Remove(ep2);
                            if (ep1Removed || ep2Removed)
                                vertsSortedByValence.UpdatePriority(entry.Key, entry.Value.Count);
                        }
                        foreach (var innerEdgePath in SplitSelfCrossingEdgePaths(ep1))
                            yield return innerEdgePath;
                    }
                    else
                    {
                        foreach (var entry in vertexToEdgeDictionary)
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
                // second, check if the two edge paths connect end-to-end. here we would combine them into one, then - later - 
                // they'll connect from the above condition.
                for (var i = 0; i < numEdges - 1; i++)
                    for (int j = i + 1; j < numEdges; j++)
                        if ((edgePaths[i].FirstVertex == edgePaths[j].FirstVertex && edgePaths[i].LastVertex == edgePaths[j].LastVertex)
                            || (edgePaths[i].FirstVertex == edgePaths[j].LastVertex && edgePaths[i].LastVertex == edgePaths[j].FirstVertex))
                            return (edgePaths[i], edgePaths[j]);
                // third, if the other end of the paths share a common edgePath, then connect these to lead to completing the
                // loop by basically calling the above condition, which in turn calls the first condition. We could keep going with this
                // logic, but anything beyond this becomes n^2, so we'll stop here. The imperfect EdgePathPairEvaluator below will 
                // handle the rest.
                for (var i = 0; i < numEdges - 1; i++)
                    for (int j = i + 1; j < numEdges; j++)
                    {
                        var otherEndI = edgePaths[i].FirstVertex == vertex ? edgePaths[i].LastVertex : edgePaths[i].FirstVertex;
                        var otherEndJ = edgePaths[j].FirstVertex == vertex ? edgePaths[j].LastVertex : edgePaths[j].FirstVertex;
                        if (vertexToEdgeDictionary[otherEndI].Intersect(vertexToEdgeDictionary[otherEndJ]).Any())
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
                var startVertex = vertexPositions.Keys.First();
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
                        // the sequence of indices.
                        var indexToAvoid = index + 1; //the index to avoid would be the next one in the sequence
                        if (indexToAvoid == origEdgePath.Count)
                            // or it could be the first if wrapping around
                            indexToAvoid = 0;
                        var i = 0;  // weird use of while loop, but it solves the problem succinctly
                        while (indices[i] == indexToAvoid) { i++; }
                        index = indices[i];
                        indices.RemoveAt(i);
                    }
                    newEdgePath.AddEnd(origEdgePath.EdgeList[index], origEdgePath.DirectionList[index]);
                    vertex = newEdgePath.LastVertex;
                } while (vertex != startVertex);
                newEdgePath.UpdateIsClosed();
                yield return newEdgePath;
            }
        }
    }
}
