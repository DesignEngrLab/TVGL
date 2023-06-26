// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellatedSolid.EdgeInitialization.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class TessellatedSolid - functions related to edge initialization.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>This partial class file includes all the weird and complicated ways that edges are created when a tessellated solid
    /// is made.
    /// Since edges are rarely explicitly defined in a file, we create these after vertices and faces. In so doing, one may
    /// find some
    /// error with the file. Here we attempt to patch those up..</remarks>
    public partial class TessellatedSolid : Solid
    {
        /// <summary>
        /// The fraction of single edges for trying expand
        /// </summary>
        const double fractionOfSingleEdgesForTryingExpand = 0.1;
        /// <summary>
        /// The number of attempts default
        /// </summary>
        const int numberOfAttemptsDefault = 6;
        /// <summary>
        /// The expansion factor
        /// </summary>
        const double expansionFactor = 1.78; // it takes four to get to 10


        /// <summary>
        /// Makes the edges.
        /// </summary>
        /// <param name="fromSTL">if set to <c>true</c> [from STL].</param>
        /// <exception cref="System.Exception"></exception>
        internal void MakeEdges()
        {
            // #1 define edges from faces - this leads to the good, the bad (single-sided), and the ugly
            // (more than 2 faces per edge)
            var edgeList = DefineEdgesFromFaces(Faces, true, out var overDefinedEdges, out var singleSidedEdges);
            // #2 if more than 10% of edges do not have a matching face then we should try to expand the tolerance for common 
            // edges and vertices.
            if (singleSidedEdges.Count / (double)edgeList.Count > fractionOfSingleEdgesForTryingExpand)
            {
                var numAttempts = numberOfAttemptsDefault;
                var improvementOccurred = true;
                while (numAttempts-- > 0 && improvementOccurred && (singleSidedEdges.Count > 0 || overDefinedEdges.Count > 0))
                // attempt to increase tolerance to allow more matches
                {
                    var numOverDefined = overDefinedEdges.Count;
                    var numSingleSided = singleSidedEdges.Count;
                    Message.output("Repairing STL connections...(this may take several iterations).");
                    SameTolerance *= expansionFactor;
                    RestartVerticesToAvoidSingleSidedEdges();
                    edgeList = DefineEdgesFromFaces(Faces, true, out overDefinedEdges, out singleSidedEdges);
                    improvementOccurred = (singleSidedEdges.Count < numSingleSided && overDefinedEdges.Count < numOverDefined) ||
                      (singleSidedEdges.Count < numSingleSided && overDefinedEdges.Count == numOverDefined) ||
                      (singleSidedEdges.Count == numSingleSided && overDefinedEdges.Count < numOverDefined);
                }
                if (!improvementOccurred)
                {
                    //one step too far, back up tolerance and just use this
                    SameTolerance /= expansionFactor;
                    RestartVerticesToAvoidSingleSidedEdges();
                    edgeList = DefineEdgesFromFaces(Faces, true, out overDefinedEdges, out singleSidedEdges);
                }
            }
            // #2 the ugly over-defined ones can be teased apart sometimes but it means the solid is
            // self-intersecting. This function will spit out the ones that couldn't be matched up as
            // moreSingleSidedEdges
            if (overDefinedEdges.Any())
            {
                Message.output("Edges in Tessellation with 3 or more faces (attempting to fix).", 2);
                edgeList.AddRange(TeaseApartOverUsedEdges(overDefinedEdges, out var moreSingleSidedEdges));
                singleSidedEdges.AddRange(moreSingleSidedEdges);
            }
            // #3 the remainingEdges may be close enough that they should have been matched together
            // in the beginning. We check that here, and we spit out the final unrepairable edges as the border
            // edges and removed vertices. we need to make sure we remove vertices that were paired up here.
            List<Edge> borderEdges;
            List<TriangleFace> newFaces;
            List<Vertex> removedVertices;
            if (singleSidedEdges.Any())
            {
                if (singleSidedEdges.Count > 1000)
                    throw new Exception(singleSidedEdges.Count + " Single-sided edges. Likely a shell.");
                edgeList.AddRange(MatchUpRemainingSingleSidedEdge(singleSidedEdges,
                    Math.Pow(expansionFactor, numberOfAttemptsDefault) * this.SameTolerance, out var remainingEdges,
                    out removedVertices));
                //often the singleSided Edges make loops that we can triangulate. If they are not in loops
                // then we spit back the remainingEdges.
                var hubVertices = FindHubVertices(remainingEdges);
                var loops = OrganizeIntoLoops(remainingEdges, hubVertices, out borderEdges);
                // well, even if they were in loops - sometimes we can't triangulate - yet moreRemainingEdges
                edgeList.AddRange(CreateMissingEdgesAndFaces(loops, out newFaces, out var moreRemainingEdges));
                borderEdges.AddRange(moreRemainingEdges); //Add two remaining lists together
            }
            else
            {
                borderEdges = new List<Edge>();
                newFaces = new List<TriangleFace>();
                removedVertices = new List<Vertex>();
            }
            // well, the edgelist is definitely going to work out so, we are going to need to make
            // sure that they are known to their vertices for the next few steps - so here we take
            // a moment to stitch these to the vertices
            foreach (var tuple in edgeList)
                tuple.Item1.DoublyLinkVertices();
            // finally, 

            // now, we have list, we can do some finally cleanup and stitching
            NumberOfEdges = edgeList.Count;
          var  _edges = new Edge[NumberOfEdges];
            for (var i = 0; i < NumberOfEdges; i++)
            {
                //stitch together edges and faces. Note, the first face is already attached to the edge, due to the edge constructor
                //above
                var edge = edgeList[i].Item1;
                var otherFace = edgeList[i].Item3;
                edge.IndexInList = i;
                SetAndGetEdgeChecksum(edge);
                edge.OtherFace = otherFace;
                if (otherFace != null)
                    otherFace.AddEdge(edge);
                Edges[i] = edge;
            }
            //var t = newFaces.Where(p => !p.Edges.Any());
            AddFaces(newFaces.Where(p => p.Edges.Count() == 3).ToList());

            // The neighbor's normal (in the next 2 lines) if the original face has no area (collapsed to a line).
            // This happens with T-Edges. We want to give the face the normal of the two smaller edges' other faces,
            // to preserve a sharp line. Also, if multiple T-Edges are adjacent, recursion may be necessary. 
            var success = false;
            var j = 0;
            while (!success && j < 10)
            {
                j++;
                success = true;
                foreach (var face in Faces)
                    if (face.Normal.IsNull())
                        if (!face.AdoptNeighborsNormal())
                            success = false;
            }
            RemoveVertices(removedVertices);
        }

        /// <summary>
        /// Finds the hub vertices. These are vertices that connect to 3 or more single-sided edges. These are a good place to start
        /// looking for holes (in non-watertight solids) because they are difficult to handle when encountered DURING loop creation.
        /// So, the solution is to start with them.
        /// </summary>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>Dictionary&lt;Vertex, System.Int32&gt;.</returns>
        private Dictionary<Vertex, int> FindHubVertices(HashSet<Edge> remainingEdges)
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

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        internal void RestartVerticesToAvoidSingleSidedEdges()
        {
            var faceIndices = Faces.Select(f => (f.A.IndexInList, f.B.IndexInList, f.C.IndexInList)).ToList();
            var colors = Faces.Select(f => f.Color).ToArray();
            var numDecimalPoints = 0;
            //Gets the number of decimal places. this is the crucial part where we consolidate vertices...
            while (numDecimalPoints < 15 && Math.Round(SameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var coords = new List<Vector3>();
            var simpleCompareDict = new Dictionary<Vector3, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            for (int i = 0; i < faceIndices.Count; i++)
            {
                var newIndices = new List<int>();
                foreach (var oldIndex in faceIndices[i].EnumerateThruple())
                {
                    //Get coord from list of vertices
                    var coord = Vertices[oldIndex];
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points.
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices. This will catch bidirectional tolerancing (+/-)
                    var position = new Vector3(Math.Round(coord.X, numDecimalPoints),
                          Math.Round(coord.Y, numDecimalPoints), Math.Round(coord.Z, numDecimalPoints));

                    if (simpleCompareDict.TryGetValue(position, out var index))
                    {
                        // if it's in the dictionary, update the newIndices
                        newIndices.Add(index);
                    }
                    else
                    {
                        /* else, add a new coord to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = coords.Count;
                        coords.Add(position);
                        simpleCompareDict.Add(position, newIndex);
                        newIndices.Add(newIndex);
                    }
                }
                faceIndices[i] = (newIndices[0], newIndices[1], newIndices[2]);
            }
            MakeVertices(coords, coords.Count);
            MakeFaces(faceIndices, faceIndices.Count, colors);
        }

        /// <summary>
        /// The first pass to making edges. It returns the good ones, and two lists of bad ones. The first, overDefinedEdges,
        /// are those which appear to have more than two faces interfacing with the edge. This happens when CAD tools
        /// tessellate
        /// and save B-rep surfaces that are created through boolean operations (such as union). The second list,
        /// partlyDefinedEdges,
        /// only have one face connected to them (aka singleSidedEdges).
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// <param name="overDefinedEdges">The over defined edges.</param>
        /// <param name="partlyDefinedEdges">The partly defined edges.</param>
        /// <returns>List&lt;Tuple&lt;Edge, List&lt;TriangleFace&gt;&gt;&gt;.</returns>
        internal static List<(Edge, TriangleFace, TriangleFace)> DefineEdgesFromFaces(IList<TriangleFace> faces,
            bool doublyLinkToVertices,
            out List<(Edge, List<TriangleFace>)> overDefinedEdges, out List<Edge> partlyDefinedEdges)
        {
            var partlyDefinedEdgeDictionary = new Dictionary<long, Edge>();
            var alreadyDefinedEdges = new Dictionary<long, (Edge, TriangleFace, TriangleFace)>();
            var overDefinedEdgesDictionary = new Dictionary<long, (Edge, List<TriangleFace>)>();
            foreach (var face in faces)
            {
                var fromVertex = face.C;
                foreach (var toVertex in face.Vertices)
                {
                    var checksum = GetEdgeChecksum(fromVertex, toVertex);
                    // the following four-part condition is best read from the bottom up.
                    // the checksum is used to quickly identify if the edge exists (and to access it)
                    // in one of the 3 dictionaries specified above.
                    if (overDefinedEdgesDictionary.TryGetValue(checksum, out var overDefFaces))
                        // yet another (4th, 5th, etc) face defines this edge. Better store for now and sort out
                        // later in "TeaseApartOverDefinedEdges" (see next method).
                        overDefFaces.Item2.Add(face);
                    else if (alreadyDefinedEdges.TryGetValue(checksum, out var edgeEntry))
                    {
                        // if an alreadyDefinedEdge has another face defining it, then it should be
                        // moved to overDefinedEdges
                        overDefinedEdgesDictionary.Add(checksum, (edgeEntry.Item1,
                            new List<TriangleFace> { edgeEntry.Item2, edgeEntry.Item3, face }));
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.TryGetValue(checksum, out var edge))
                    {
                        // found a match to a partlyDefinedEdge. Great! I hope it doesn't turn out
                        // to be overDefined
                        alreadyDefinedEdges.Add(checksum, (edge, edge.OwnedFace, face));
                        partlyDefinedEdgeDictionary.Remove(checksum);
                    }
                    else // this edge doesn't already exist, so create and add to partlyDefinedEdge dictionary
                    {
                        var edgeNew = new Edge(fromVertex, toVertex, face, null, false, checksum);
                        partlyDefinedEdgeDictionary.Add(checksum, edgeNew);
                    }
                    fromVertex = toVertex;
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
        /// TVGL.TriangleFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<(Edge, TriangleFace, TriangleFace)> TeaseApartOverUsedEdges(
            List<(Edge, List<TriangleFace>)> overUsedEdgesDictionary,
            out List<Edge> moreSingleSidedEdges)
        {
            var newListOfGoodEdges = new List<(Edge, TriangleFace, TriangleFace)>();
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
                    var highestDot = -2.0;
                    TriangleFace bestMatch = null;
                    var refFace = candidateFaces[0];
                    candidateFaces.RemoveAt(0);
                    var refOwnsEdge = FaceShouldBeOwnedFace(edge, refFace);
                    foreach (var candidateMatchingFace in candidateFaces)
                    {
                        var DotScore = refOwnsEdge == FaceShouldBeOwnedFace(edge, candidateMatchingFace)
                            ? -2 //edge cannot be owned by both faces, thus this is not a good candidate for this.
                            : refFace.Normal.Dot(candidateMatchingFace.Normal);
                        //  To take it "out of the running", we simply give it a value of -2
                        if (DotScore > highestDot)
                        {
                            highestDot = DotScore;
                            bestMatch = candidateMatchingFace;
                        }
                    }
                    if (highestDot > -1)
                    // -1 is a valid dot-product but it is not practical to match faces with completely opposite
                    // faces
                    {
                        numFailedTries = 0;
                        candidateFaces.Remove(bestMatch);
                        if (FaceShouldBeOwnedFace(edge, refFace))
                            newListOfGoodEdges.Add((new Edge(edge.From, edge.To, refFace, bestMatch, false, edge.EdgeReference),
                                 refFace, bestMatch));
                        else
                            newListOfGoodEdges.Add((new Edge(edge.From, edge.To, bestMatch, refFace, false, edge.EdgeReference),
                                bestMatch, refFace));
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
                oldEdge.To.Edges.Remove(entry.Item1); //so, here we remove it from the coord references
                foreach (var face in entry.Item2)
                    moreSingleSidedEdges.Add(FaceShouldBeOwnedFace(oldEdge, face)
                        ? new Edge(oldEdge.From, oldEdge.To, face, null, false, oldEdge.EdgeReference)
                        : new Edge(oldEdge.To, oldEdge.From, face, null, false, oldEdge.EdgeReference));
            }
            return newListOfGoodEdges;
        }

        /// <summary>
        /// Faces the should be owned face.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool FaceShouldBeOwnedFace(Edge edge, TriangleFace face)
        {
            var otherEdgeVector = face.OtherVertex(edge.From, edge.To).Coordinates.Subtract(edge.To.Coordinates);
            var isThisNormal = edge.Vector.Cross(otherEdgeVector);
            return face.Normal.Dot(isThisNormal) > 0;
        }

        /// <summary>
        /// Matches up remaining single sided edge.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="borderEdges">The border edges.</param>
        /// <param name="removedVertices">The removed vertices.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Edge, TriangleFace, TriangleFace&gt;&gt;.</returns>
        private static IEnumerable<(Edge, TriangleFace, TriangleFace)> MatchUpRemainingSingleSidedEdge(
            List<Edge> singleSidedEdges, double tolerance, out HashSet<Edge> borderEdges, out List<Vertex> removedVertices)
        {
            borderEdges = new HashSet<Edge>(singleSidedEdges);

            // First, do a pairwise check with all entries in the partly defined edges
            var numRemaining = singleSidedEdges.Count;
            Message.output(numRemaining + " Single-Sided Edges in Tessellation (attempting to fix).", 2);

            var removedToKept = new Dictionary<Vertex, Vertex>();
            var keptToRemoved = new Dictionary<Vertex, List<Vertex>>();
            var completedEdges = new List<(Edge, TriangleFace, TriangleFace)>();
            for (var i = 0; i < numRemaining; i++)
                for (var j = i + 1; j < numRemaining; j++)
                {
                    var edge1 = singleSidedEdges[i];
                    var edge2 = singleSidedEdges[j];
                    var sameDir = edge1.Vector.Dot(edge2.Vector) > 0;
                    var itsAMatch = sameDir ?
                        (edge1.To.Equals(edge2.To) || edge1.To.Coordinates.IsPracticallySame(edge2.To.Coordinates, tolerance))
                            && (edge1.From.Equals(edge2.From) || edge1.From.Coordinates.IsPracticallySame(edge2.From.Coordinates, tolerance))
                            :
                        (edge1.To.Equals(edge2.From) || edge1.To.Coordinates.IsPracticallySame(edge2.From.Coordinates, tolerance))
                            && (edge1.From.Equals(edge2.To) || edge1.From.Coordinates.IsPracticallySame(edge2.To.Coordinates, tolerance));

                    if (!itsAMatch || !borderEdges.Contains(edge1) || !borderEdges.Contains(edge2))
                        continue;
                    borderEdges.Remove(edge1);
                    borderEdges.Remove(edge2);
                    // next, for each successful match, we have to decide what edge to keep and what vertices
                    // to merge. we'll put pairs 
                    Edge keepEdge, removeEdge;
                    if (keptToRemoved.ContainsKey(edge1.From) || keptToRemoved.ContainsKey(edge1.To))
                    {
                        keepEdge = edge1;
                        removeEdge = edge2;
                    }
                    else if (keptToRemoved.ContainsKey(edge2.From) || keptToRemoved.ContainsKey(edge2.To) ||
                        removedToKept.ContainsKey(edge1.From) || removedToKept.ContainsKey(edge1.To))
                    {
                        keepEdge = edge2;
                        removeEdge = edge1;
                    }
                    else //but this seems like a problem. And it is! how to fix?
                    {
                        keepEdge = edge1;
                        removeEdge = edge2;
                    }
                    completedEdges.Add((keepEdge, keepEdge.OwnedFace, removeEdge.OwnedFace));
                    var keepVertex = keepEdge.From;
                    var removeVertex = sameDir ? removeEdge.From : removeEdge.To;
                    MergeEdgeVertices(removedToKept, keptToRemoved, keepVertex, removeVertex);
                    keepVertex = keepEdge.To;
                    removeVertex = sameDir ? removeEdge.To : removeEdge.From;
                    MergeEdgeVertices(removedToKept, keptToRemoved, keepVertex, removeVertex);
                }
            removedVertices = removedToKept.Keys.ToList();
            return completedEdges;
        }

        /// <summary>
        /// Merges the edge vertices.
        /// </summary>
        /// <param name="removedToKept">The removed to kept.</param>
        /// <param name="keptToRemoved">The kept to removed.</param>
        /// <param name="keepVertex">The keep coord.</param>
        /// <param name="removeVertex">The remove coord.</param>
        private static void MergeEdgeVertices(Dictionary<Vertex, Vertex> removedToKept, Dictionary<Vertex, List<Vertex>> keptToRemoved,
            Vertex keepVertex, Vertex removeVertex)
        {
            while (removedToKept.TryGetValue(keepVertex, out var newKeepVertex))
                keepVertex = newKeepVertex;
            while (removedToKept.TryGetValue(removeVertex, out var newRemoveVertex))
                removeVertex = newRemoveVertex;
            if (removeVertex == keepVertex) return;
            foreach (var face in removeVertex.Faces)
            {
                face.ReplaceVertex(removeVertex, keepVertex);
                foreach (var edge in face.Edges)
                {
                    if (edge == null) continue;
                    if (edge.From == removeVertex) edge.From = keepVertex;
                    else if (edge.To == removeVertex) edge.To = keepVertex;
                }
            }
            keepVertex.Coordinates = ModifyTessellation.DetermineIntermediateVertexPosition(keepVertex, removeVertex);
            foreach (var f in keepVertex.Faces)
                f.Update();
            removedToKept.Add(removeVertex, keepVertex);
            if (keptToRemoved.TryGetValue(keepVertex, out var keeps))
                keeps.Add(removeVertex);
            else keptToRemoved.Add(keepVertex, new List<Vertex> { removeVertex });
        }


        /// <summary>
        /// Organizes the into loops.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="hubVertices">The hub vertices.</param>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>List&lt;TriangulationLoop&gt;.</returns>
        internal static List<TriangulationLoop> OrganizeIntoLoops(IEnumerable<Edge> singleSidedEdges,
            Dictionary<Vertex, int> hubVertices, out List<Edge> remainingEdges)
        {
            var listOfLoops = new List<TriangulationLoop>();
            var remainingEdgesInner = new HashSet<Edge>(singleSidedEdges);
            if (!singleSidedEdges.Any())
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
                    hubVertices[firstHubVertex] = hubEdgeCount;
                    if (hubEdgeCount <= 2) hubVertices.Remove(firstHubVertex);
                    // becuase using this here will drop it down to 2 - in which case - it's just like all
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
                //var coord = loop[i].edge.To;
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
        /// Creates the missing edges and faces.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="newFaces">The new faces.</param>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Edge, TriangleFace, TriangleFace&gt;&gt;.</returns>
        private static IEnumerable<(Edge, TriangleFace, TriangleFace)> CreateMissingEdgesAndFaces(
                    List<TriangulationLoop> loops,
                    out List<TriangleFace> newFaces, out List<Edge> remainingEdges)
        {
            var completedEdges = new List<(Edge, TriangleFace, TriangleFace)>();
            newFaces = new List<TriangleFace>();
            remainingEdges = new List<Edge>();
            var k = 0;
            foreach (var loop in loops)
            {
                if (loop.Count <= 2)
                {
                    Message.output("Recieving hole with only " + loops.Count + " edges.", 2);
                    continue;
                }
                Message.output("Patching hole #" + ++k + " (has " + loop.Count + " edges) in tessellation (" + loops.Count + " loops in total).", 2);
                //if a simple triangle, create a new face from vertices
                if (loop.Count == 3)
                {
                    var newFace = new TriangleFace(loop.GetVertices());
                    foreach (var eAD in loop)
                        if (eAD.dir)
                            completedEdges.Add((eAD.edge, newFace, eAD.edge.OtherFace));
                        else completedEdges.Add((eAD.edge, eAD.edge.OwnedFace, newFace));
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else
                {
                    Dictionary<long, Edge> edgeDic = new Dictionary<long, Edge>();
                    foreach (var eAD in loop)
                    {
                        var checksum = GetEdgeChecksum(eAD.edge.From, eAD.edge.To);
                        if (!edgeDic.ContainsKey(checksum))
                            edgeDic.Add(checksum, eAD.edge);
                    }
                    var vertices = loop.GetVertices().ToList();
                    Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
                    var plane = new Plane(distance, planeNormal);
                    var success = false;
                    List<Vertex[]> triangleFaceList = null;
                    if (plane.CalculateMaxError(vertices.Select(v => v.Coordinates)) < Constants.ErrorForFaceInSurface)
                    {
                        try
                        {
                            var coords2D = vertices.Select(v => v.Coordinates).ProjectTo2DCoordinates(planeNormal, out _);
                            if (coords2D.Area() < 0) planeNormal *= -1;
                            triangleFaceList = vertices.Triangulate(planeNormal, true).ToList();
                            success = true;
                        }
                        catch
                        {
                            success = false;
                        }
                    }
                    if (success && triangleFaceList.Any())
                    {
                        Message.output("loop successfully repaired with " + triangleFaceList.Count, 2);
                        foreach (var triangle in triangleFaceList)
                        {
                            var newFace = new TriangleFace(triangle, planeNormal);
                            newFaces.Add(newFace);
                            foreach (var fromVertex in newFace.Vertices)
                            {
                                var toVertex = newFace.NextVertexCCW(fromVertex);
                                var checksum = GetEdgeChecksum(fromVertex, toVertex);
                                if (edgeDic.TryGetValue(checksum, out var edge))
                                {
                                    //Finish creating edge.
                                    completedEdges.Add((edge, edge.OwnedFace, newFace));
                                    edgeDic.Remove(checksum);
                                }
                                else
                                    edgeDic.Add(checksum, new Edge(fromVertex, toVertex, newFace, null, false, checksum));
                            }
                        }
                    }
                    if (!success)
                    {
                        //try
                        //{
                        var triangles = Single3DPolygonTriangulation.QuickTriangulate(loop, 5);
                        //if (!Single3DPolygonTriangulation.Triangulate(loop, out var triangles)) continue;
                        foreach (var triangle in triangles)
                        {
                            var newFace = new TriangleFace(triangle.GetVertices(), triangle.Normal);
                            newFaces.Add(newFace);
                            foreach (var edgeAnddir in triangle)
                            {
                                newFace.AddEdge(edgeAnddir.edge);
                                if (edgeAnddir.dir)
                                    edgeAnddir.edge.OwnedFace = newFace;
                                else edgeAnddir.edge.OtherFace = newFace;
                                var checksum = GetEdgeChecksum(edgeAnddir.edge.From, edgeAnddir.edge.To);
                                if (edgeDic.TryGetValue(checksum, out var formerEdge))
                                {
                                    edgeDic.Remove(checksum);
                                    completedEdges.Add((formerEdge, formerEdge.OwnedFace, edgeAnddir.edge.OwnedFace));
                                }
                                else
                                {
                                    edgeAnddir.edge.EdgeReference = checksum;
                                    edgeDic.Add(checksum, edgeAnddir.edge);
                                }
                            }
                        }
                        //}
                        //catch (Exception exc)
                        //{
                        //    remainingEdges.AddRange(loop.EdgeList);
                        //}
                    }
                }
            }
            return completedEdges;
        }


        /// <summary>
        /// Sets the and get edge checksum.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <returns>System.Int64.</returns>
        internal static long SetAndGetEdgeChecksum(Edge edge)
        {
            var checksum = GetEdgeChecksum(edge.From, edge.To);
            edge.EdgeReference = checksum;
            return checksum;
        }

        /// <summary>
        /// Gets the edge checksum.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <returns>System.Int64.</returns>
        internal static long GetEdgeChecksum(Vertex vertex1, Vertex vertex2)
        {
            return GetEdgeChecksum(vertex1.IndexInList, vertex2.IndexInList);
        }

        /// <summary>
        /// Gets the edge checksum.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <returns>System.Int64.</returns>
        /// <exception cref="System.Exception">edge to same vertices.</exception>
        internal static long GetEdgeChecksum(int v1, int v2)
        {
            if (v1 == -1 || v2 == -1) return -1;
            if (v1 == v2) throw new Exception("edge to same vertices.");
            return v1 < v2
                ? v1 + Constants.VertexCheckSumMultiplier * v2
                : v2 + Constants.VertexCheckSumMultiplier * v1;
        }
    }
}