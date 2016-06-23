// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.IOFunctions;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    ///     and
    ///     all interesting operations work on the TessellatedSolid.
    /// </remarks>
    public partial class TessellatedSolid
    {
        internal static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }
        #region Make Edges
        internal static List<Tuple<Edge, List<PolygonalFace>>> MakeEdges(IList<PolygonalFace> faces, bool doublyLinkToVertices,
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

                    if (overDefinedEdgesDictionary.ContainsKey(checksum))
                        overDefinedEdgesDictionary[checksum].Item2.Add(face);
                    else if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        var edgeEntry = alreadyDefinedEdges[checksum];
                        edgeEntry.Item2.Add(face);
                        overDefinedEdgesDictionary.Add(checksum, edgeEntry);
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdgeDictionary[checksum];
                        alreadyDefinedEdges.Add(checksum, new Tuple<Edge, List<PolygonalFace>>(edge,
                            new List<PolygonalFace> { edge.OwnedFace, face }));
                        partlyDefinedEdgeDictionary.Remove(checksum);
                    }
                    else // this edge doesn't already exist.
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
        /// Teases apart over used edges. By taking in the edges with more than two faces (the over-used edges) a list is return of newly defined edges.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;TVGL.PolygonalFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> TeaseApartOverUsedEdges(List<Tuple<Edge, List<PolygonalFace>>> overUsedEdgesDictionary,
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
                    var refOwnsEdge = TessellatedSolid.FaceShouldBeOwnedFace(edge, refFace);
                    foreach (var candidateMatchingFace in candidateFaces)
                    {
                        var dotProductScore = refOwnsEdge == TessellatedSolid.FaceShouldBeOwnedFace(edge, candidateMatchingFace)
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
                        if (TessellatedSolid.FaceShouldBeOwnedFace(edge, refFace))
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
            moreSingleSidedEdges = new List<Edge>();
            foreach (var entry in overUsedEdgesDictionary)
            {
                var oldEdge = entry.Item1;
                oldEdge.From.Edges.Remove(entry.Item1); //the original over-used edge will not be used in the model.
                oldEdge.To.Edges.Remove(entry.Item1);   //so, here we remove it from the vertex references
                foreach (var face in entry.Item2)
                    moreSingleSidedEdges.Add(FaceShouldBeOwnedFace(oldEdge, face)
                            ? new Edge(oldEdge.From, oldEdge.To, face, null, false, oldEdge.EdgeReference)
                            : new Edge(oldEdge.To, oldEdge.From, face, null, false, oldEdge.EdgeReference));
            }
            return newListOfGoodEdges;
        }


        internal static bool FaceShouldBeOwnedFace(Edge edge, PolygonalFace face)
        {
            var otherEdgeVector = face.OtherVertex(edge.From, edge.To).Position.subtract(edge.To.Position);
            var isThisNormal = edge.Vector.crossProduct(otherEdgeVector);
            return face.Normal.dotProduct(isThisNormal) > 0;
        }

        /// <summary>
        /// Fixes the bad edges. By taking in the edges with more than two faces (the over-used edges) and the edges with only one face (the partlyDefinedEdges), this
        /// repair method attempts to repair the edges as best possible through a series of pairwise searches.
        /// </summary>
        /// <param name="overUsedEdgesDictionary">The over used edges dictionary.</param>
        /// <param name="partlyDefinedEdgesIEnumerable">The partly defined edges i enumerable.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;TVGL.PolygonalFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> MediateSingleSidedEdges(List<Edge> singleSidedEdges,
            out List<PolygonalFace> newFaces, out ICollection<Vertex> removedVertices)
        {
            var newListOfGoodEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            List<Edge> remainingEdges, moreRemainingEdges;
            var loops = OrganizeIntoLoops(singleSidedEdges, out remainingEdges);
            newListOfGoodEdges.AddRange(CreateMissingEdgesAndFaces(loops, out newFaces, out moreRemainingEdges));
            remainingEdges.AddRange(moreRemainingEdges);
            newListOfGoodEdges.AddRange(MatchUpRemainingSingleSidedEdge(remainingEdges, out removedVertices));

            return newListOfGoodEdges;
        }


        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> MatchUpRemainingSingleSidedEdge(List<Edge> singleSidedEdges, out ICollection<Vertex> removedVertices)
        {
            removedVertices = new HashSet<Vertex>();
            var completedEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            // now do a pairwise check with all entries in the partly defined edges
            var numRemaining = singleSidedEdges.Count;
            var scoresAndPairs = new SortedDictionary<double, int[]>(new NoEqualSort());
            for (int i = 0; i < numRemaining; i++)
                for (int j = i + 1; j < numRemaining; j++)
                {
                    var score = GetEdgeSimilarityScore(singleSidedEdges[i], singleSidedEdges[j]);
                    if (score <= Constants.MaxAllowableEdgeSimilarityScore)
                        scoresAndPairs.Add(score, new[] { i, j });
                }
            // basically, we go through from best match to worst until the MaxAllowableEdgeSimilarityScore is exceeded.
            var alreadyMatchedIndices = new HashSet<int>();
            foreach (var score in scoresAndPairs)
            {
                if (alreadyMatchedIndices.Contains(score.Value[0]) || alreadyMatchedIndices.Contains(score.Value[1]))
                    continue;
                alreadyMatchedIndices.Add(score.Value[0]);
                var keepEdge = singleSidedEdges[score.Value[0]];
                alreadyMatchedIndices.Add(score.Value[1]);
                var removeEdge = singleSidedEdges[score.Value[1]];
                completedEdges.Add(new Tuple<Edge, List<PolygonalFace>>(keepEdge,
                        new List<PolygonalFace> { keepEdge.OwnedFace, removeEdge.OwnedFace }));
                
                if (!removedVertices.Contains(removeEdge.From) && keepEdge.To != removeEdge.From)
                {
                    CombineVerticesOfEdge(keepEdge.To, removeEdge.From);
                    removedVertices.Add(removeEdge.From);
                }
                if (!removedVertices.Contains(removeEdge.To) && keepEdge.From != removeEdge.To)
                {
                    CombineVerticesOfEdge(keepEdge.From, removeEdge.To);
                    removedVertices.Add(removeEdge.To);
                }
            }
            return completedEdges;
        }
        private static void CombineVerticesOfEdge(Vertex keepVertex, Vertex removedVertex)
        {
            foreach (var edge in removedVertex.Edges)
            {
                keepVertex.Edges.Add(edge);
                if (edge.To == removedVertex) edge.To = keepVertex;
                if (edge.From == removedVertex) edge.From = keepVertex;
            }
            foreach (var face in removedVertex.Faces)
            {
                keepVertex.Faces.Add(face);
                var index = face.Vertices.IndexOf(removedVertex);
                face.Vertices[index] = keepVertex;
            }
            AdjustPositionOfKeptVertex(keepVertex, removedVertex);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
        }

        /// <summary>
        ///     Combines the vertices of edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="removedVertexOut">The removed vertex out.</param>
        /// <param name="removedEdge1Out">The removed edge1 out.</param>
        /// <param name="removedEdge2Out">The removed edge2 out.</param>
        /// <param name="removedFace1">The removed face1.</param>
        /// <param name="removedFace2">The removed face2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool CombineVerticesOfEdge(Edge edge, out Vertex removedVertexOut, out Edge removedEdge1Out,
            out Edge removedEdge2Out, out PolygonalFace removedFace1, out PolygonalFace removedFace2)
        {
            var keepVertex = edge.To; // arbitrarily choose the To as the keep vertex, but this may be swapped below
            var removedVertex = edge.From; // if the To has some missing faces
            if (keepVertex == removedVertex)
            {
                removedVertexOut = null;
                removedEdge2Out = removedEdge1Out = null;
                removedFace1 = removedFace2 = null;
                return false;
            }
            removedFace1 = edge.OwnedFace;
            removedFace2 = edge.OtherFace;
            var removedEdge1 = removedFace1 == null ? null : removedFace1.OtherEdge(keepVertex, true);
            var removedEdge2 = removedFace2 == null ? null : removedFace2.OtherEdge(keepVertex, true);
            var keepEdge1 = removedFace1 == null ? null : removedFace1.OtherEdge(removedVertex, true);
            var keepEdge2 = removedFace2 == null ? null : removedFace2.OtherEdge(removedVertex, true);
            if (removedEdge1 != null && removedEdge2 != null && (keepEdge1 == null || keepEdge2 == null))
            { // swap with removed.
                var tempVertex = keepVertex; keepVertex = removedVertex; removedVertex = tempVertex;
                var tempEdge = keepEdge1; keepEdge1 = removedEdge1; removedEdge1 = tempEdge;
                tempEdge = keepEdge2; keepEdge2 = removedEdge2; removedEdge2 = tempEdge;
            }
            var otherEdgesOnTheKeepSide =
            keepVertex.Edges.Where(e => e != edge && e != keepEdge1 && e != keepEdge2).ToList();
            var otherEdgesOnTheRemoveSide =
                removedVertex.Edges.Where(e => e != edge && e != removedEdge1 && e != removedEdge2).ToList();
            if ( // this is a topologically important check. It ensures that the edge is not deleted if
                 // it serves an important role in ensuring the proper topology of the solid
                otherEdgesOnTheKeepSide.Select(e => e.OtherVertex(keepVertex))
                    .Intersect(otherEdgesOnTheRemoveSide.Select(e => e.OtherVertex(removedVertex)))
                    .Any())
            {
                removedVertexOut = null;
                removedEdge2Out = removedEdge1Out = null;
                removedFace1 = removedFace2 = null;
                return false;
            }
            // move edges connected to removeVertex to the keepVertex and let keepVertex link back to these edges
            foreach (var e in otherEdgesOnTheRemoveSide)
            {
                keepVertex.Edges.Add(e);
                if (e.From == removedVertex) e.From = keepVertex;
                else e.To = keepVertex;
            }
            // move faces connected to removeVertex to the keepVertex and let keepVertex link back to these edges.
            foreach (var face in removedVertex.Faces)
            {
                if (face == removedFace1 || face == removedFace2) continue;
                keepVertex.Faces.Add(face);
                face.Vertices[face.Vertices.IndexOf(removedVertex)] = keepVertex;
            }
            // conversely keepVertex should forget about the edge and the remove faces
            keepVertex.Edges.Remove(edge);
            keepVertex.Faces.Remove(removedFace1);
            keepVertex.Faces.Remove(removedFace2);
            var farVertex = removedFace1 == null ? null : removedFace1.OtherVertex(edge, true);
            if (farVertex != null)
            {
                farVertex.Edges.Remove(removedEdge1);
                farVertex.Faces.Remove(removedFace1);
            }
            farVertex = removedFace2 == null ? null : removedFace2.OtherVertex(edge, true);
            if (farVertex != null)
            {
                farVertex.Edges.Remove(removedEdge2);
                farVertex.Faces.Remove(removedFace2);
            }
            // for the winged edges (removedEdge1 and removedEdge2) that are removed, connected their faces to 
            // the new edge
            // first on the "owned side of edge"
            var fromFace = removedEdge1 == null ? null
                : removedEdge1.OwnedFace == removedFace1 ? removedEdge1.OtherFace : removedEdge1.OwnedFace;
            if (fromFace != null)
            {
                var index = fromFace.Edges.IndexOf(removedEdge1);
                if (index >= 0 && index < fromFace.Edges.Count)
                    fromFace.Edges[index] = keepEdge1;
            }
            if (keepEdge1 != null && keepEdge1.OwnedFace == removedFace1) keepEdge1.OwnedFace = fromFace;
            else if (keepEdge1 != null) keepEdge1.OtherFace = fromFace;
            // second on the "other side of edge"
            fromFace = removedEdge2 == null ? null
                : removedEdge2.OwnedFace == removedFace2 ? removedEdge2.OtherFace : removedEdge2.OwnedFace;
            if (fromFace != null)
            {
                var index = fromFace.Edges.IndexOf(removedEdge2);
                if (index >= 0 && index < fromFace.Edges.Count)
                    fromFace.Edges[index] = keepEdge2;
            }
            if (keepEdge2 != null && keepEdge2.OwnedFace == removedFace2) keepEdge2.OwnedFace = fromFace;
            else if (keepEdge2 != null) keepEdge2.OtherFace = fromFace;


            //AdjustPositionOfKeptVertexAverage(keepVertex, removedVertex);
            AdjustPositionOfKeptVertex(keepVertex, removedVertex);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
            removedVertexOut = removedVertex;
            removedEdge1Out = removedEdge1;
            removedEdge2Out = removedEdge2;
            return true;
        }


        /// <summary>
        ///     Adjusts the position of kept vertex.
        /// </summary>
        /// <param name="keepVertex">The keep vertex.</param>
        /// <param name="removedVertex">The removed vertex.</param>
        private static void AdjustPositionOfKeptVertex(Vertex keepVertex, Vertex removedVertex)
        {
            //average positions
            var newPosition = keepVertex.Position.add(removedVertex.Position);
            keepVertex.Position = newPosition.divide(2);
        }

        /// <summary>
        ///     Adjusts the position of kept vertex experimental.
        /// </summary>
        /// <param name="keepVertex">The keep vertex.</param>
        /// <param name="removedVertex">The removed vertex.</param>
        /// <param name="removeFace1">The remove face1.</param>
        /// <param name="removeFace2">The remove face2.</param>
        private static void AdjustPositionOfKeptVertexExperimental(Vertex keepVertex, Vertex removedVertex,
            PolygonalFace removeFace1, PolygonalFace removeFace2)
        {
            //average positions
            var newPosition = keepVertex.Position.add(removedVertex.Position);
            var radius = keepVertex.Position.subtract(removedVertex.Position).norm2() / 2.0;
            keepVertex.Position = newPosition.divide(2);
            var avgNormal = removeFace1.Normal.add(removeFace2.Normal).normalize();
            var otherVertexAvgDistanceToEdgePlane =
                keepVertex.Edges.Select(e => e.OtherVertex(keepVertex).Position.dotProduct(avgNormal)).Sum() /
                (keepVertex.Edges.Count - 1);
            var distanceOfEdgePlane = keepVertex.Position.dotProduct(avgNormal);

            // use a sigmoid function to determine how far out to move the vertex
            var x = 0.05 * (distanceOfEdgePlane - otherVertexAvgDistanceToEdgePlane) / radius;
            var length = 2 * radius * x / Math.Sqrt(1 + x * x) - radius;
            keepVertex.Position = keepVertex.Position.add(avgNormal.multiply(length));
        }
        private static List<Tuple<List<Edge>, double[]>> OrganizeIntoLoops(List<Edge> singleSidedEdges, out List<Edge> remainingEdges)
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
                            normal = loop.Count == 2 ? n1
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
                                normal = loop.Count == 2 ? n1
                                    : normal.multiply(loop.Count).add(n1).divide(loop.Count + 1).normalize();
                            }
                            removedEdges.Add(bestPrev);
                            remainingEdges.Remove(bestPrev);
                        }
                        else successful = false;
                    }
                } while (loop.First().To != loop.Last().From && successful);
                if (successful)
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

        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> CreateMissingEdgesAndFaces(List<Tuple<List<Edge>, double[]>> loops,
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
                    List<List<Vertex[]>> triangleFaceList;
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>>
                    { edges.Select(e => e.To).ToList() }, normal, out triangleFaceList);
                    if (triangles.Any())
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

        internal static Edge[] CompleteEdgeArray(List<Tuple<Edge, List<PolygonalFace>>> edgeList)
        {
            var numEdges = edgeList.Count;
            var edgeArray = new Edge[numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                //stitch together edges and faces. Note, the first face is already attached to the edge, due to the edge constructor
                //above
                var edge = edgeList[i].Item1;
                var ownedFace = edgeList[i].Item2[0];
                var otherFace = edgeList[i].Item2[1];
                edge.IndexInList = i;
                // grabbing the neighbor's normal (in the next 2 lines) should only happen if the original
                // face has no area (collapsed to a line).
                if (otherFace.Normal.Contains(Double.NaN)) otherFace.Normal = (double[])ownedFace.Normal.Clone();
                if (ownedFace.Normal.Contains(Double.NaN)) ownedFace.Normal = (double[])otherFace.Normal.Clone();
                edge.OtherFace = otherFace;
                otherFace.AddEdge(edge);
                edge.To.Edges.Add(edge);
                edge.From.Edges.Add(edge);
                edgeArray[i] = edge;
            }
            return edgeArray;
        }

        #endregion

        /// <summary>
        /// Defines the center, the volume and the surface area.
        /// </summary>
        internal static void DefineCenterVolumeAndSurfaceArea(IList<PolygonalFace> faces, out double[] center,
            out double volume, out double surfaceArea)
        {
            surfaceArea = faces.Sum(face => face.Area);
            volume = MiscFunctions.Volume(faces, out center);
        }

        const double oneSixtieth = 1.0 / 60.0;

        private static double[,] DefineInertiaTensor(IEnumerable<PolygonalFace> Faces, double[] Center, double Volume)
        {
            var matrixA = StarMath.makeZero(3, 3);
            var matrixCtotal = StarMath.makeZero(3, 3);
            var canonicalMatrix = new[,]
            {
                {oneSixtieth, 0.5*oneSixtieth, 0.5*oneSixtieth},
                {0.5*oneSixtieth, oneSixtieth, 0.5*oneSixtieth}, {0.5*oneSixtieth, 0.5*oneSixtieth, oneSixtieth}
            };
            foreach (var face in Faces)
            {
                matrixA.SetRow(0,
                    new[]
                    {
                        face.Vertices[0].Position[0] - Center[0], face.Vertices[0].Position[1] - Center[1],
                        face.Vertices[0].Position[2] - Center[2]
                    });
                matrixA.SetRow(1,
                    new[]
                    {
                        face.Vertices[1].Position[0] - Center[0], face.Vertices[1].Position[1] - Center[1],
                        face.Vertices[1].Position[2] - Center[2]
                    });
                matrixA.SetRow(2,
                    new[]
                    {
                        face.Vertices[2].Position[0] - Center[0], face.Vertices[2].Position[1] - Center[1],
                        face.Vertices[2].Position[2] - Center[2]
                    });

                var matrixC = matrixA.transpose().multiply(canonicalMatrix);
                matrixC = matrixC.multiply(matrixA).multiply(matrixA.determinant());
                matrixCtotal = matrixCtotal.add(matrixC);
            }

            var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            var matrixCprime =
                translateMatrix.multiply(-1)
                    .multiply(translateMatrix.transpose())
                    .add(translateMatrix.multiply(translateMatrix.multiply(-1).transpose()))
                    .add(translateMatrix.multiply(-1).multiply(translateMatrix.multiply(-1).transpose()))
                    .multiply(Volume);
            matrixCprime = matrixCprime.add(matrixCtotal);
            var result =
                StarMath.makeIdentity(3).multiply(matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            return result.subtract(matrixCprime);
        }


        private static void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces)
            {
                var index = face.Vertices.IndexOf(vertex);
                if (index >= 0) face.Vertices.RemoveAt(index);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
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


        public static void Transform(IEnumerable<Vertex> Vertices, double[,] transformMatrix)
        {
            double[] tempCoord;
            foreach (var vert in Vertices)
            {
                tempCoord = transformMatrix.multiply(new[] { vert.X, vert.Y, vert.Z, 1 });
                vert.Position[0] = tempCoord[0];
                vert.Position[1] = tempCoord[1];
                vert.Position[2] = tempCoord[2];
            }
            /*     tempCoord = transformMatrix.multiply(new[] {XMin, YMin, ZMin, 1});
                 XMin = tempCoord[0];
                 YMin = tempCoord[1];
                 ZMin = tempCoord[2];

                 tempCoord = transformMatrix.multiply(new[] {XMax, YMax, ZMax, 1});
                 XMax = tempCoord[0];
                 YMax = tempCoord[1];
                 ZMax = tempCoord[2];
                 Center = transformMatrix.multiply(new[] {Center[0], Center[1], Center[2], 1});
                 // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
                 if (_inertiaTensor != null)
                 {
                     var rotMatrix = new double[3, 3];
                     for (int i = 0; i < 3; i++)
                         for (int j = 0; j < 3; j++)
                             rotMatrix[i, j] = transformMatrix[i, j];
                     _inertiaTensor = rotMatrix.multiply(_inertiaTensor);
                 }
                 if (Primitives != null)
                     foreach (var primitive in Primitives)
                         primitive.Transform(transformMatrix);
         */
        }

    }
}