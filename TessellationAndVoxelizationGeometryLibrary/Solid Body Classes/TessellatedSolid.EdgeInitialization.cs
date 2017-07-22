// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 06-23-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 06-23-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid - functions related to edge initialization.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This partial class file includes all the weird and complicated ways that edges are created when a tessellated solid
    ///     is made.
    ///     Since edges are rarely explicitly defined in a file, we create these after vertices and faces. In so doing, one may
    ///     find some
    ///     error with the file. Here we attempt to patch those up..
    /// </remarks>
    public partial class TessellatedSolid
    {
        private void MakeEdges(out List<PolygonalFace> newFaces, out List<Vertex> removedVertices)
        {
            List<Tuple<Edge, List<PolygonalFace>>> overDefinedEdges;
            List<Edge> singleSidedEdges, moreSingleSidedEdges;
            // #1 define edges from faces - this leads to the good, the bad (single-sided), and the ugly
            // (more than 2 faces per edge)
            var edgeList = DefineEdgesFromFaces(Faces, true, out overDefinedEdges, out singleSidedEdges);
            // #2 the ugly over-defined ones can be teased apart sometimes but it means the solid is
            // self-intersecting. This function will spit out the ones that couldn't be matched up as
            // moreSingleSidedEdges
            edgeList.AddRange(TeaseApartOverUsedEdges(overDefinedEdges, out moreSingleSidedEdges));
            singleSidedEdges.AddRange(moreSingleSidedEdges);
            // #3 often the singleSided Edges make loops that we can triangulate. If they are not in loops
            // then we spit back the remainingEdges.
            List<Edge> remainingEdges, moreRemainingEdges;
            var loops = OrganizeIntoLoops(singleSidedEdges, out remainingEdges);
            // well, even if they were in loops - sometimes we can't triangulate - yet moreRemainingEdges
            edgeList.AddRange(CreateMissingEdgesAndFaces(loops, out newFaces, out moreRemainingEdges));
            remainingEdges.AddRange(moreRemainingEdges); //Add two remaining lists together

            // well, the edge list is definitely going to work out so, we are going to need to make
            // sure that they are known to their vertices for the next few steps - so here we take 
            // a moment to stitch these to the vertices
            foreach (var tuple in edgeList)
                tuple.Item1.DoublyLinkVertices();
            HashSet<Edge> borderEdges;
            // finally, the remainingEdges may be close enough that they should have been matched together
            // in the beginning. We check that here, and we spit out the final unrepairable edges as the border
            // edges and removed vertices. we need to make sure we remove vertices that were paired up here.
            edgeList.AddRange(MatchUpRemainingSingleSidedEdge(remainingEdges, out borderEdges, out removedVertices));
            BorderEdges = borderEdges.ToArray();
            // now, we have list, we can do some finally cleanup and stitching
            NumberOfEdges = edgeList.Count;
            Edges = new Edge[NumberOfEdges];
            for (var i = 0; i < NumberOfEdges; i++)
            {
                //stitch together edges and faces. Note, the first face is already attached to the edge, due to the edge constructor
                //above
                var edge = edgeList[i].Item1;
                var ownedFace = edgeList[i].Item2[0];
                var otherFace = edgeList[i].Item2[1];
                edge.IndexInList = i;
                SetAndGetEdgeChecksum(edge);
                // grabbing the neighbor's normal (in the next 2 lines) should only happen if the original
                // face has no area (collapsed to a line).
                if (otherFace.Normal.Contains(double.NaN)) otherFace.Normal = (double[])ownedFace.Normal.Clone();
                if (ownedFace.Normal.Contains(double.NaN)) ownedFace.Normal = (double[])otherFace.Normal.Clone();
                edge.OtherFace = otherFace;
                otherFace.AddEdge(edge);
                Edges[i] = edge;
            }
        }
        /// <summary>
        ///     The first pass to making edges. It returns the good ones, and two lists of bad ones. The first, overDefinedEdges,
        ///     are those which appear to have more than two faces interfacing with the edge. This happens when CAD tools
        ///     tessellate
        ///     and save B-rep surfaces that are created through boolean operations (such as union). The second list,
        ///     partlyDefinedEdges,
        ///     only have one face connected to them (aka singleSidedEdges).
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// <param name="overDefinedEdges">The over defined edges.</param>
        /// <param name="partlyDefinedEdges">The partly defined edges.</param>
        /// <returns>List&lt;Tuple&lt;Edge, List&lt;PolygonalFace&gt;&gt;&gt;.</returns>
        internal static List<Tuple<Edge, List<PolygonalFace>>> DefineEdgesFromFaces(IList<PolygonalFace> faces,
            bool doublyLinkToVertices,
            out List<Tuple<Edge, List<PolygonalFace>>> overDefinedEdges, out List<Edge> partlyDefinedEdges)
        {
            var partlyDefinedEdgeDictionary = new Dictionary<long, Edge>();
            var alreadyDefinedEdges = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            var overDefinedEdgesDictionary = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            foreach (var face in faces)
            {
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[j == lastIndex ? 0 : j + 1];
                    var checksum = GetEdgeChecksum(fromVertex, toVertex);
                    // the following four-part condition is best read from the bottom up.
                    // the checksum is used to quickly identify if the edge exists (and to access it)
                    // in one of the 3 dictionaries specified above.
                    if (overDefinedEdgesDictionary.ContainsKey(checksum))
                        // yet another (4th, 5th, etc) face defines this edge. Better store for now and sort out
                        // later in "TeaseApartOverDefinedEdges" (see next method).
                        overDefinedEdgesDictionary[checksum].Item2.Add(face);
                    else if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        // if an alreadyDefinedEdge has another face defining it, then it should be
                        // moved to overDefinedEdges
                        var edgeEntry = alreadyDefinedEdges[checksum];
                        edgeEntry.Item2.Add(face);
                        overDefinedEdgesDictionary.Add(checksum, edgeEntry);
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.ContainsKey(checksum))
                    {
                        // found a match to a partlyDefinedEdge. Great! I hope it doesn't turn out
                        // to be overDefined
                        var edge = partlyDefinedEdgeDictionary[checksum];
                        alreadyDefinedEdges.Add(checksum, new Tuple<Edge, List<PolygonalFace>>(edge,
                            new List<PolygonalFace> { edge.OwnedFace, face }));
                        partlyDefinedEdgeDictionary.Remove(checksum);
                    }
                    else // this edge doesn't already exist, so create and add to partlyDefinedEdge dictionary
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, false, checksum);
                        partlyDefinedEdgeDictionary.Add(checksum, edge);
                    }
                }
            }
            overDefinedEdges = overDefinedEdgesDictionary.Values.ToList();
            partlyDefinedEdges = partlyDefinedEdgeDictionary.Values.ToList();
            return alreadyDefinedEdges.Values.ToList();
        }

        /// <summary>
        /// Teases apart over-defined edges. By taking in the edges with more than two faces (the over-used edges) a list is
        /// return of newly defined edges.
        /// </summary>
        /// <param name="overUsedEdgesDictionary">The over used edges dictionary.</param>
        /// <param name="moreSingleSidedEdges">The more single sided edges.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;
        /// TVGL.PolygonalFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> TeaseApartOverUsedEdges(
            List<Tuple<Edge, List<PolygonalFace>>> overUsedEdgesDictionary,
            out List<Edge> moreSingleSidedEdges)
        {
            var newListOfGoodEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            foreach (var entry in overUsedEdgesDictionary)
            {
                var edge = entry.Item1;
                var candidateFaces = entry.Item2;
                var numFailedTries = 0;
                // foreach over-used edge:
                // first, try to find the best match for each face. Basically, it is assumed that faces with the most similar normals 
                // should be paired together. 
                while (candidateFaces.Count > 1 && numFailedTries < candidateFaces.Count)
                {
                    var highestDotProduct = -2.0;
                    PolygonalFace bestMatch = null;
                    var refFace = candidateFaces[0];
                    candidateFaces.RemoveAt(0);
                    var refOwnsEdge = FaceShouldBeOwnedFace(edge, refFace);
                    foreach (var candidateMatchingFace in candidateFaces)
                    {
                        var dotProductScore = refOwnsEdge == FaceShouldBeOwnedFace(edge, candidateMatchingFace)
                            ? -2 //edge cannot be owned by both faces, thus this is not a good candidate for this.
                            : refFace.Normal.dotProduct(candidateMatchingFace.Normal);
                        //  To take it "out of the running", we simply give it a value of -2
                        if (dotProductScore > highestDotProduct)
                        {
                            highestDotProduct = dotProductScore;
                            bestMatch = candidateMatchingFace;
                        }
                    }
                    if (highestDotProduct > -1)
                    // -1 is a valid dot-product but it is not practical to match faces with completely opposite
                    // faces
                    {
                        numFailedTries = 0;
                        candidateFaces.Remove(bestMatch);
                        if (FaceShouldBeOwnedFace(edge, refFace))
                            newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                                new Edge(edge.From, edge.To, refFace, bestMatch, false, edge.EdgeReference),
                                new List<PolygonalFace> { refFace, bestMatch }));
                        else
                            newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                                new Edge(edge.From, edge.To, bestMatch, refFace, false, edge.EdgeReference),
                                new List<PolygonalFace> { bestMatch, refFace }));
                    }
                    else
                    {
                        candidateFaces.Add(refFace);
                        //referenceFace was removed 24 lines earlier. Here, we re-add it to the
                        // end of the list.
                        numFailedTries++;
                    }
                }
            }
            // now, whatever is left in the overUsedEdgesDictionary will be pulled out and made into new singleSided
            // edges to be handled by the subsequent functions.
            moreSingleSidedEdges = new List<Edge>();
            foreach (var entry in overUsedEdgesDictionary)
            {
                var oldEdge = entry.Item1;
                oldEdge.From.Edges.Remove(entry.Item1); //the original over-used edge will not be used in the model.
                oldEdge.To.Edges.Remove(entry.Item1); //so, here we remove it from the vertex references
                foreach (var face in entry.Item2)
                    moreSingleSidedEdges.Add(FaceShouldBeOwnedFace(oldEdge, face)
                        ? new Edge(oldEdge.From, oldEdge.To, face, null, false, oldEdge.EdgeReference)
                        : new Edge(oldEdge.To, oldEdge.From, face, null, false, oldEdge.EdgeReference));
            }
            return newListOfGoodEdges;
        }


        private static bool FaceShouldBeOwnedFace(Edge edge, PolygonalFace face)
        {
            var otherEdgeVector = face.OtherVertex(edge.From, edge.To).Position.subtract(edge.To.Position);
            var isThisNormal = edge.Vector.crossProduct(otherEdgeVector);
            return face.Normal.dotProduct(isThisNormal) > 0;
        }



        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> MatchUpRemainingSingleSidedEdge(
            List<Edge> singleSidedEdges, out HashSet<Edge> borderEdges, out List<Vertex> removedVertices)
        {
            borderEdges = new HashSet<Edge>(singleSidedEdges);
            var removedVerticesHash = new HashSet<Vertex>();
            removedVertices = new List<Vertex>();
            var keptVerticesHash = new HashSet<Vertex>();
            var keptVerticesList = new List<Vertex>();
            // now do a pairwise check with all entries in the partly defined edges
            var numRemaining = singleSidedEdges.Count;
            var scoresAndPairs = new SortedDictionary<double, int[]>(new NoEqualSort());
            for (var i = 0; i < numRemaining; i++)
                for (var j = i + 1; j < numRemaining; j++)
                {
                    var score = GetEdgeSimilarityScore(singleSidedEdges[i], singleSidedEdges[j]);
                    if (score <= Constants.MaxAllowableEdgeSimilarityScore)
                        scoresAndPairs.Add(score, new[] { i, j });
                }
            // basically, we go through from best match to worst until the MaxAllowableEdgeSimilarityScore is exceeded.
            var completedEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            var alreadyMatchedIndices = new HashSet<int>();
            foreach (var score in scoresAndPairs)
            {
                if (alreadyMatchedIndices.Contains(score.Value[0]) || alreadyMatchedIndices.Contains(score.Value[1]))
                    continue;
                alreadyMatchedIndices.Add(score.Value[0]);
                alreadyMatchedIndices.Add(score.Value[1]);
                var keepEdge = singleSidedEdges[score.Value[0]];
                var removeEdge = singleSidedEdges[score.Value[1]];
                if (VerticesAreAdjacent(keepEdge.From, removeEdge.To) ||
                    VerticesAreAdjacent(keepEdge.To, removeEdge.From))
                    continue;
                borderEdges.Remove(keepEdge);
                borderEdges.Remove(removeEdge);
                completedEdges.Add(new Tuple<Edge, List<PolygonalFace>>(keepEdge,
                    new List<PolygonalFace> { keepEdge.OwnedFace, removeEdge.OwnedFace }));
                keepEdge.DoublyLinkVertices();
                keptVerticesList.Add(keepEdge.From);
                removedVertices.Add(removeEdge.To);
                keptVerticesList.Add(keepEdge.To);
                removedVertices.Add(removeEdge.From);
                if (!keptVerticesHash.Contains(keepEdge.From)) keptVerticesHash.Add(keepEdge.From);
                if (!keptVerticesHash.Contains(keepEdge.To)) keptVerticesHash.Add(keepEdge.To);
            }
            for (var i = keptVerticesList.Count - 1; i >= 0; i--)
            {
                var vertexToRemove = removedVertices[i];
                if (keptVerticesHash.Contains(vertexToRemove) || removedVerticesHash.Contains(vertexToRemove))
                    removedVertices.RemoveAt(i);
                else
                {
                    removedVerticesHash.Add(vertexToRemove);
                    CombineVerticesOfEdge(keptVerticesList[i], vertexToRemove);
                }
            }
            return completedEdges;
        }

        internal static bool VerticesAreAdjacent(Vertex v1, Vertex v2)
        {
            Edge commonEdge;
            return VerticesAreAdjacent(v1, v2, out commonEdge);
        }

        internal static bool VerticesAreAdjacent(Vertex v1, Vertex v2, out Edge commonEdge)
        {
            commonEdge = v1.Edges.Find(e => v2.Edges.Contains(e));
            return commonEdge != null;
        }

        private static void CombineVerticesOfEdge(Vertex keepVertex, Vertex removedVertex)
        {
            foreach (var face in removedVertex.Faces)
            {
                keepVertex.Faces.Add(face);
                var index = face.VertexIndex(removedVertex);
                face.Vertices[index] = keepVertex;
            }
            foreach (var edge in removedVertex.Edges)
            {
                if (edge.From == removedVertex) edge.From = keepVertex;
                else if (edge.To == removedVertex) edge.To = keepVertex;
                keepVertex.Edges.Add(edge);
            }
            keepVertex.Position = ModifyTessellation.DetermineIntermediateVertexPosition(keepVertex, removedVertex);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
        }


        internal static List<Tuple<List<Edge>, double[]>> OrganizeIntoLoops(List<Edge> singleSidedEdges,
            out List<Edge> remainingEdges)
        {
            remainingEdges = new List<Edge>(singleSidedEdges);
            var attempts = 0;
            var listOfLoops = new List<Tuple<List<Edge>, double[]>>();
            while (remainingEdges.Count > 0 && attempts < remainingEdges.Count)
            {
                var loop = new List<Edge>();
                var successful = true;
                var removedEdges = new List<Edge>();
                var startingEdge = remainingEdges[0];
                var normal = startingEdge.OwnedFace.Normal;
                loop.Add(startingEdge);
                removedEdges.Add(startingEdge);
                remainingEdges.RemoveAt(0);
                do
                {
                    var possibleNextEdges = remainingEdges.Where(e => e.To == loop.Last().From);
                    if (possibleNextEdges.Any())
                    {
                        var bestNext = pickBestEdge(possibleNextEdges, loop.Last().Vector, normal);
                        loop.Add(bestNext);
                        var n1 = loop[loop.Count - 1].Vector.crossProduct(loop[loop.Count - 2].Vector).normalize();
                        if (!n1.Contains(double.NaN))
                        {
                            n1 = n1.dotProduct(normal) < 0 ? n1.multiply(-1) : n1;
                            normal = loop.Count == 2
                                ? n1
                                : normal.multiply(loop.Count).add(n1).divide(loop.Count + 1).normalize();
                        }
                        removedEdges.Add(bestNext);
                        remainingEdges.Remove(bestNext);
                    }
                    else
                    {
                        possibleNextEdges = remainingEdges.Where(e => e.From == loop[0].To);
                        if (possibleNextEdges.Any())
                        {
                            var bestPrev = pickBestEdge(possibleNextEdges, loop[0].Vector.multiply(-1),
                                normal);
                            loop.Insert(0, bestPrev);
                            var n1 = loop[1].Vector.crossProduct(loop[0].Vector).normalize();
                            if (!n1.Contains(double.NaN))
                            {
                                n1 = n1.dotProduct(normal) < 0 ? n1.multiply(-1) : n1;
                                normal = loop.Count == 2
                                    ? n1
                                    : normal.multiply(loop.Count).add(n1).divide(loop.Count + 1).normalize();
                            }
                            removedEdges.Add(bestPrev);
                            remainingEdges.Remove(bestPrev);
                        }
                        else successful = false;
                    }
                } while (loop.First().To != loop.Last().From && successful);
                if (successful && loop.Count > 2)
                {
                    //Average the normals from all the owned faces.
                    listOfLoops.Add(new Tuple<List<Edge>, double[]>(loop, normal));
                    attempts = 0;
                }
                else
                {
                    remainingEdges.AddRange(removedEdges);
                    attempts++;
                }
            }
            return listOfLoops;
        }

        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> CreateMissingEdgesAndFaces(
            List<Tuple<List<Edge>, double[]>> loops,
            out List<PolygonalFace> newFaces, out List<Edge> remainingEdges)
        {
            var completedEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            newFaces = new List<PolygonalFace>();
            remainingEdges = new List<Edge>();
            foreach (var tuple in loops)
            {
                var edges = tuple.Item1;
                var normal = tuple.Item2;
                //if a simple triangle, create a new face from vertices
                if (edges.Count == 3)
                {
                    var newFace = new PolygonalFace(edges.Select(e => e.To), normal);
                    foreach (var edge in edges)
                        completedEdges.Add(new Tuple<Edge, List<PolygonalFace>>(edge,
                            new List<PolygonalFace> { edge.OwnedFace, newFace }));
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else
                {
                    var edgeDic = edges.ToDictionary(SetAndGetEdgeChecksum);
                    var triangleFaceList = TriangulatePolygon.Run(new List<List<Vertex>>
                    {edges.Select(e => e.To).ToList()}, normal); ;
                    var triangles = triangleFaceList.SelectMany(tl => tl).ToList();
                    if (triangles.Any())
                    {
                        Message.output("loop successfully repaired with " + triangles.Count, 5);
                        foreach (var triangle in triangles)
                        {
                            var newFace = new PolygonalFace(triangle, normal);
                            newFaces.Add(newFace);
                            for (var j = 0; j < 3; j++)
                            {
                                var fromVertex = newFace.Vertices[j];
                                var toVertex = newFace.NextVertexCCW(fromVertex);
                                var checksum = GetEdgeChecksum(fromVertex, toVertex);
                                if (edgeDic.ContainsKey(checksum))
                                {
                                    //Finish creating edge.
                                    var edge = edgeDic[checksum];
                                    completedEdges.Add(new Tuple<Edge, List<PolygonalFace>>(edge,
                                        new List<PolygonalFace> { edge.OwnedFace, newFace }));
                                    edgeDic.Remove(checksum);
                                }
                                else
                                    edgeDic.Add(checksum, new Edge(fromVertex, toVertex, newFace, null, false, checksum));
                            }
                        }
                    }
                    else remainingEdges.AddRange(edges);
                }
            }
            return completedEdges;
        }

        private static Edge pickBestEdge(IEnumerable<Edge> possibleNextEdges, double[] refEdge, double[] normal)
        {
            var unitRefEdge = refEdge.normalize();
            var max = -2.0;
            Edge bestEdge = null;
            foreach (var candEdge in possibleNextEdges)
            {
                var unitCandEdge = candEdge.Vector.normalize();
                var cross = unitRefEdge.crossProduct(unitCandEdge);
                var temp = cross.dotProduct(normal);
                if (max < temp)
                {
                    max = temp;
                    bestEdge = candEdge;
                }
            }
            return bestEdge;
        }


        private static double GetEdgeSimilarityScore(Edge e1, Edge e2)
        {
            var score = Math.Abs(e1.Length - e2.Length) / e1.Length;
            score += 1 - Math.Abs(e1.Vector.normalize().dotProduct(e2.Vector.normalize()));
            score += Math.Min(e2.From.Position.subtract(e1.To.Position).norm2()
                              + e2.To.Position.subtract(e1.From.Position).norm2(),
                e2.From.Position.subtract(e1.From.Position).norm2()
                + e2.To.Position.subtract(e1.To.Position).norm2())
                     / e1.Length;
            return score;
        }


        internal static long SetAndGetEdgeChecksum(Edge edge)
        {
            var checksum = GetEdgeChecksum(edge.From, edge.To);
            edge.EdgeReference = checksum;
            return checksum;
        }

        internal static long GetEdgeChecksum(Vertex fromVertex, Vertex toVertex)
        {
            var fromIndex = fromVertex.IndexInList;
            var toIndex = toVertex.IndexInList;
            if (fromIndex == -1 || toIndex == -1) return -1;
            if (fromIndex == toIndex) throw new Exception("edge to same vertices.");
            return fromIndex < toIndex
                ? fromIndex + Constants.VertexCheckSumMultiplier * toIndex
                : toIndex + Constants.VertexCheckSumMultiplier * fromIndex;
        }
    }
}