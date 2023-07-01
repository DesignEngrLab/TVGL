// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellationBuildAndRepair.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using ClipperLib;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TVGL
{
    /// <summary>
    /// Stores errors in the tessellated solid
    /// </summary>
    public class TessellationBuildAndRepair
    {
        private readonly TessellatedSolid ts;

        private TessellationBuildAndRepair(TessellatedSolid ts)
        {
            this.ts = ts;
            ContainsErrors = false;
            OverusedEdges = new List<List<(TriangleFace, bool)>>();
            SingleSidedEdgeData = new List<(TriangleFace, Vertex, Vertex)>();
            FacesWithNegligibleArea = new List<TriangleFace>();
            FacePairsForEdges = new List<(TriangleFace, TriangleFace)>();
            InconsistentMatingFacePairs = new List<(TriangleFace, TriangleFace)>();
            ModelHasNegativeVolume = false;
            ChangeTolerance = 0;
        }
        #region Properties

        /// <summary>
        /// Whether ts.Errors contains any errors that need to be resolved
        /// </summary>
        /// <value><c>true</c> if [errors]; otherwise, <c>false</c>.</value>
        public bool ContainsErrors { get; private set; }

        /// <summary>
        /// Edges that are used by more than two faces
        /// </summary>
        /// <value>The overused edges.</value>
        public List<List<(TriangleFace, bool)>> OverusedEdges { get; internal set; }

        /// <summary>
        /// Gets Edges that only have one face
        /// </summary>
        /// <value>The singled sided edges.</value>
        internal List<(TriangleFace, Vertex, Vertex)> SingleSidedEdgeData { get; set; }

        public List<Edge> SingleSidedEdges { get; internal set; }


        /// <summary>
        /// Faces with negligible area (which is not necessarily an error)
        /// </summary>
        /// <value>The faces with negligible area.</value>
        public List<TriangleFace> FacesWithNegligibleArea { get; internal set; }

        /// <summary>
        /// Edges with bad angles
        /// </summary>
        /// <value>The edges with bad angle.</value>
        public List<(TriangleFace, TriangleFace)> FacePairsForEdges { get; internal set; }

        /// <summary>
        /// Edges with bad angles
        /// </summary>
        /// <value>The edges with bad angle.</value>
        public List<(TriangleFace, TriangleFace)> InconsistentMatingFacePairs { get; internal set; }


        /// <summary>
        /// Gets a value indicating whether [model has negative volume].
        /// </summary>
        /// <value><c>true</c> if [model has negative volume]; otherwise, <c>false</c>.</value>
        public bool ModelHasNegativeVolume { get; internal set; }

        /// <summary>
        /// Gets or sets the recommended change in tolerance for solids that didn't load correctly.
        /// If negative (-1), then the tolerance should be decreased. If positive (+1), then the 
        /// tolerance should be increased.
        /// </summary>
        public int ChangeTolerance { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether there is an incorrect face-to-edge ratio.
        /// For watertight solids, there should be 1.5 edges per face.
        /// </summary>
        public bool IncorrectFaceEdgeRatio { get; private set; }
        #endregion Properties

        #region Main Method from TessellatedSolid Constructor
        /// <summary>
        /// Completes the constructor by addressing any other build options in TessellatedSolidBuildOptions
        /// with the exception of CopyElementsPassedToConstructor. These are actually handled in the main constructor
        /// body - but only one constructor uses it: the one accepting faces and vertices.
        /// </summary>
        /// <param name="fromSTL">if set to <c>true</c> [from STL].</param>
        internal static void CompleteBuildOptions(TessellatedSolid ts, TessellatedSolidBuildOptions buildOptions)
        {
            if (buildOptions == null) buildOptions = TessellatedSolidBuildOptions.Default;
            if (!buildOptions.CheckModelIntegrity)
                return;
            var buildAndErrorInfo = new TessellationBuildAndRepair(ts);
            buildAndErrorInfo.CompleteBuildOptions(buildOptions);
        }
        /// <summary>
        /// Completes the constructor by addressing any other build options in TessellatedSolidBuildOptions
        /// with the exception of CopyElementsPassedToConstructor. These are actually handled in the main constructor
        /// body - but only one constructor uses it: the one accepting faces and vertices.
        /// </summary>
        /// <param name="fromSTL">if set to <c>true</c> [from STL].</param>
        void CompleteBuildOptions(TessellatedSolidBuildOptions buildOptions)
        {
            CheckModelIntegrity();
            if (buildOptions.FixEdgeDisassociations && OverusedEdges.Count + SingleSidedEdgeData.Count > 0)
            {
                try
                {
                    TeaseApartOverUsedEdges(OverusedEdges);
                    MatchUpRemainingSingleSidedEdge(out var keptToRemovedDictionary);
                    MergeVertices(keptToRemovedDictionary);
                    ts.RemoveVertices(keptToRemovedDictionary.Values.SelectMany(v => v));
                }
                catch
                {
                    //Continue
                }
            }
            if (buildOptions.AutomaticallyRepairBadFaces && InconsistentMatingFacePairs.Any())
                try
                {
                    if (!SetNegligibleAreaFaceNormals())
                        throw new Exception("Unable to set face normals.");
                    if (!FlipFacesBasedOnInconsistentEdges())
                        throw new Exception("Unable to resolve face directions.");
                }
                catch
                {
                    //Continue
                }
            if (buildOptions.AutomaticallyInvertNegativeSolids && ModelHasNegativeVolume)
                ts.TurnModelInsideOut();
            // the remaining items will need edges so we need to build these here
            if (buildOptions.PredefineAllEdges)
                MakeEdges();
            if (buildOptions.AutomaticallyRepairBadFaces && buildOptions.PredefineAllEdges)
            {
                try
                {
                    if (!PropagateFixToNegligibleFaces())
                        throw new Exception("Unable to flip edges to avoid negligible faces.");
                }
                catch
                {
                    //Continue
                }
            }
            if (buildOptions.AutomaticallyRepairHoles)
            {
                if (!buildOptions.PredefineAllEdges)
                    throw new ArgumentException("AutomaticallyRepairHoles requires PredefineAllEdges to be true.");
                if (SingleSidedEdges != null && SingleSidedEdges.Count > 0)
                    try
                    {
                        this.RepairHoles();
                    }
                    catch
                    {
                        //Continue
                    }
            }

            //If the volume is zero, creating the convex hull may cause a null exception
            if (buildOptions.DefineConvexHull && !ts.Volume.IsNegligible())
                ts.BuildConvexHull();
            if (buildOptions.FindNonsmoothEdges)
            {
                if (!buildOptions.PredefineAllEdges)
                    throw new ArgumentException("AutomaticallyRepairHoles requires PredefineAllEdges to be true.");
                try
                {
                    FindNonSmoothEdges();
                }
                catch
                {
                    //Continue
                }
            }
        }
        #endregion

        private bool PropagateFixToNegligibleFaces()
        {
            var negligibleArea = ts.SameTolerance * ts.SameTolerance;
            foreach (var face in FacesWithNegligibleArea)
            {
                var shortestEdge = ShortestEdge(face);
                if (shortestEdge.Length <= ts.SameTolerance)
                {
                    var removedEdges = new List<Edge>();
                    shortestEdge.CollapseEdge(removedEdges);
                    ts.RemoveVertex(shortestEdge.From);
                    ts.RemoveFaces(new[] { shortestEdge.OwnedFace, shortestEdge.OtherFace });
                    ts.RemoveEdges(removedEdges);
                }
                else
                {
                    var longestEdge = LongestEdge(face);
                    if (longestEdge.GetMatingFace(face).Area.IsNegligible(negligibleArea))
                    {   // the mating face is also negligible, so we need to remove 2 faces, 

                        var removedEdges = new List<Edge>();
                        var removedVertex = longestEdge.OwnedFace.OtherVertex(longestEdge);
                        var keepVertex = longestEdge.OtherFace.OtherVertex(longestEdge);
                        ModifyTessellation.MergeVertexAndKill3EdgesAnd2Faces(removedVertex, keepVertex,
                            longestEdge.OwnedFace, longestEdge.OtherFace, removedEdges);
                        ts.RemoveVertex(removedVertex);
                        ts.RemoveFaces(new[] { longestEdge.OwnedFace, longestEdge.OtherFace });
                        ts.RemoveEdges(removedEdges);
                    }
                    else LongestEdge(face).FlipEdge(ts);
                }
            }
            return true;
        }

        private static Edge LongestEdge(TriangleFace face)
        {
            if (face.AB.Length > face.BC.Length)
            {
                if (face.AB.Length > face.CA.Length)
                    return face.AB;
                else return face.CA;
            }
            else if (face.BC.Length > face.CA.Length)
                return face.BC; //since it is already established that BC is not shorter than AB
            else return face.CA; // since bc>=ab and ca>=bc, ca must be the longest edge
        }
        private static Edge ShortestEdge(TriangleFace face)
        {
            if (face.AB.Length < face.BC.Length)
            {
                if (face.AB.Length < face.CA.Length)
                    return face.AB;
                else return face.CA;
            }
            else if (face.BC.Length < face.CA.Length)
                return face.BC; //since it is already established that BC is not longer than AB
            else return face.CA; // since bc>=ab and ca>=bc, ca must be the longest edge
        }

        #region Check Model Integrity

        /// <summary>
        /// Checks the model integrity.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="repairAutomatically">The repair automatically.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        void CheckModelIntegrity()
        {
            Message.output("Model Integrity Check...", 3);

            if (ts.Volume < 0)
            {
                ContainsErrors = true;
                ModelHasNegativeVolume = true;
            }
            var negligibleArea = ts.SameTolerance * ts.SameTolerance;
            var partlyDefinedEdgeDictionary = new Dictionary<long, (TriangleFace, Vertex, Vertex)>();
            var alreadyDefinedEdges = new Dictionary<long, (TriangleFace, TriangleFace, bool)>();
            var overDefinedEdgesDictionary = new Dictionary<long, List<(TriangleFace, bool)>>();
            foreach (var face in ts.Faces)
            {
                if (face.Area.IsNegligible(negligibleArea))
                {
                    ContainsErrors = true;
                    FacesWithNegligibleArea.Add(face);
                }
                var fromVertex = face.C;
                foreach (var toVertex in face.Vertices)
                {
                    var checksum = Edge.GetEdgeChecksum(fromVertex, toVertex);
                    // the following four-part condition is best read from the bottom up.
                    // the checksum is used to quickly identify if the edge exists (and to access it)
                    // in one of the 3 dictionaries specified above.
                    if (overDefinedEdgesDictionary.TryGetValue(checksum, out var overDefFaces))
                    {
                        (var oldFromVertex, var oldToVertex) = GetCommonVertices(overDefFaces[0].Item1, overDefFaces[1].Item1, overDefFaces[1].Item2);
                        // yet another (4th, 5th, etc) face defines this edge. Better store for now and sort out
                        // later in "TeaseApartOverDefinedEdges" (see next method).
                        overDefFaces.Add((face, DoesFaceOwnEdge(face, oldFromVertex, oldToVertex)));
                    }
                    else if (alreadyDefinedEdges.TryGetValue(checksum, out var edgeEntry))
                    {
                        // if an alreadyDefinedEdge has another face defining it, then it should be
                        // moved to overDefinedEdges
                        (var oldFromVertex, var oldToVertex) = GetCommonVertices(edgeEntry.Item1, edgeEntry.Item2, edgeEntry.Item3);
                        overDefinedEdgesDictionary.Add(checksum,
                            new List<(TriangleFace, bool)> { (edgeEntry.Item1, true),
                                (edgeEntry.Item2, edgeEntry.Item3),
                                (face, DoesFaceOwnEdge(face, oldFromVertex, oldToVertex)) });
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.TryGetValue(checksum, out var formerFaceAndDir))
                    {
                        // found a match to a partlyDefinedEdge. Great! I hope it doesn't turn out
                        // to be overDefined
                        partlyDefinedEdgeDictionary.Remove(checksum);
                        alreadyDefinedEdges.Add(checksum, (formerFaceAndDir.Item1, face,
                            DoesFaceOwnEdge(face, formerFaceAndDir.Item2, formerFaceAndDir.Item3)));
                    }
                    else // this edge doesn't already exist, so create and add to partlyDefinedEdge dictionary
                    {
                        partlyDefinedEdgeDictionary.Add(checksum, (face, fromVertex, toVertex));
                    }
                    fromVertex = toVertex;
                }
            }
            var numOverUsedFaceEdges = 0;
            if (overDefinedEdgesDictionary.Count > 0)
            {
                ContainsErrors = true;
                OverusedEdges = overDefinedEdgesDictionary.Values.ToList();
                numOverUsedFaceEdges = OverusedEdges.Sum(p => p.Count);
            }
            var numSingleSidedEdges = 0;
            if (partlyDefinedEdgeDictionary.Count > 0)
            {
                ContainsErrors = true;
                SingleSidedEdgeData = partlyDefinedEdgeDictionary.Values.ToList();
                numSingleSidedEdges = SingleSidedEdgeData.Count;
            }
            foreach (var connection in alreadyDefinedEdges.Values)
            {
                if (connection.Item3)
                    InconsistentMatingFacePairs.Add((connection.Item1, connection.Item2));
                else
                    FacePairsForEdges.Add((connection.Item1, connection.Item2));
            }
            if (InconsistentMatingFacePairs.Count > 0)
                ContainsErrors = true;

            if (numOverUsedFaceEdges == 0 && numSingleSidedEdges == 0)
                ChangeTolerance = 0;
            else if (numOverUsedFaceEdges > numSingleSidedEdges)
                ChangeTolerance = -1;
            else ChangeTolerance = 1;

            IncorrectFaceEdgeRatio = 3 * ts.NumberOfFaces == 2 * (alreadyDefinedEdges.Count + (numOverUsedFaceEdges + numSingleSidedEdges) / 2);

            if (ContainsErrors)
            {
                ts.Errors = this;
                ReportErrors(ts.Errors);
            }
            else
                Message.output("No errors found.", 3);
        }

        private static (Vertex fromVertex, Vertex toVertex) GetCommonVertices(TriangleFace face1, TriangleFace face2, bool bothThinkTheyOwnEdge)
        {
            if (bothThinkTheyOwnEdge)
            {
                if (face1.A == face2.A && face1.B == face2.B || face1.A == face2.B && face1.B == face2.C || face1.A == face2.C && face1.B == face2.A)
                    return (face1.A, face1.B);
                if (face1.B == face2.A && face1.C == face2.B || face1.B == face2.B && face1.C == face2.C || face1.B == face2.C && face1.C == face2.A)
                    return (face1.B, face1.C);
                // then we know it must be C and A given that we know that they share an "edge" and they both think they own it.
                return (face1.C, face1.A);
            }
            if (face1.A == face2.B && face1.B == face2.A || face1.A == face2.A && face1.B == face2.C || face1.A == face2.C && face1.B == face2.B)
                return (face1.A, face1.B);
            if (face1.B == face2.B && face1.C == face2.A || face1.B == face2.A && face1.C == face2.C || face1.B == face2.C && face1.C == face2.B)
                return (face1.B, face1.C);
            // then we know it must be C and A given that we know that they share an "edge" and they both think they own it.
            return (face1.C, face1.A);
        }

        private static short WhichFaceOwnsEdge(TriangleFace face1, TriangleFace face2, Vertex fromVertex, Vertex toVertex)
        {
            var face1OwnsEdge = DoesFaceOwnEdge(face1, fromVertex, toVertex);
            var face2OwnsEdge = DoesFaceOwnEdge(face2, fromVertex, toVertex);
            var indexFlag = face1OwnsEdge == face2OwnsEdge ? (short)0 :
                face1OwnsEdge ? (short)1 : (short)2;
            return indexFlag;
        }
        private static bool DoesFaceOwnEdge(TriangleFace face, Edge edge)
        {
            return DoesFaceOwnEdge(face, edge.From, edge.To);
        }

        private static bool DoesFaceOwnEdge(TriangleFace face, Vertex from, Vertex to)
        {
            return (face.A == from && face.B == to) ||
                 (face.B == from && face.C == to) ||
                 (face.C == from && face.A == to);
        }

        private static void ReportErrors(TessellationBuildAndRepair tsErrors)
        {
            Message.output("Errors found in model:", 3);
            Message.output("======================", 3);
            if (tsErrors.ModelHasNegativeVolume)
                Message.output("==> The model has negative area. The normals of the faces are pointed inward, or this is only a concave surface - not a watertiht solid.", 3);
            if (tsErrors.FacesWithNegligibleArea.Count > 0)
                Message.output("==> " + tsErrors.FacesWithNegligibleArea.Count + " faces with negligible area.", 3);
            if (tsErrors.OverusedEdges.Count > 0)
            {
                Message.output("==> " + tsErrors.OverusedEdges.Count + " overused edges.", 3);
                Message.output("    The number of faces per overused edge: " + string.Join(',',
                               tsErrors.OverusedEdges.Select(p => p.Count)), 3);
            }
            if (tsErrors.SingleSidedEdgeData.Count > 0) Message.output("==> " + tsErrors.SingleSidedEdgeData.Count + " single-sided edges.", 3);
            if (tsErrors.InconsistentMatingFacePairs.Count > 0) Message.output("==> " + tsErrors.InconsistentMatingFacePairs.Count
                + " edges with opposite-facing faces.", 3);

            if (tsErrors.IncorrectFaceEdgeRatio)
                Message.output("==> While re-connecting faces and edges has lead to errors, there is a likelihood that water-tightness can be acheived."
                    , 3);
            else Message.output("==> The model is not water-tight. It merely represents a surface, but fixing holes may restore it.", 3);
            if (tsErrors.ChangeTolerance == -1)
                Message.output("      Tolerance should be made smaller.");
            else if (tsErrors.ChangeTolerance == 1)
                Message.output("      Tolerance should be made larger.");
        }

        #endregion Check Model Integrity


        #region Optional Build Methods
        /// <summary>
        /// Makes the edges.
        /// </summary>
        /// <param name="fromSTL">if set to <c>true</c> [from STL].</param>
        /// <exception cref="System.Exception"></exception>
        internal void MakeEdges()
        {
            ts.NumberOfEdges = FacePairsForEdges.Count + SingleSidedEdgeData.Count;
            ts.Edges = new Edge[ts.NumberOfEdges];
            var i = 0;
            for (; i < FacePairsForEdges.Count; i++)
            {
                (TriangleFace, TriangleFace) facePair = FacePairsForEdges[i];
                var ownedFace = facePair.Item1;
                var otherFace = facePair.Item2;
                (var fromVertex, var toVertex) = GetCommonVertices(ownedFace, otherFace, false);
                ts.Edges[i] = new Edge(fromVertex, toVertex, ownedFace, otherFace, true);
            }
            if (SingleSidedEdgeData.Count > 0)
                SingleSidedEdges = new List<Edge>(SingleSidedEdgeData.Count);
            for (; i < ts.NumberOfEdges; i++)
            {
                var faceWithVertices = SingleSidedEdgeData[i];
                ts.Edges[i] = new Edge(faceWithVertices.Item2, faceWithVertices.Item3, faceWithVertices.Item1, null, true);
                SingleSidedEdges.Add(ts.Edges[i]);
            }
        }
        #endregion

        #region Repair Functions
        /// <summary>
        /// Sets the face normal for any negligible area faces that have not already been set.
        /// The neighbor's normal (in the next 2 lines) if the original face has no area (collapsed to a line).
        /// This happens with T-Edges. We want to give the face the normal of the two smaller edges' other faces,
        /// to preserve a sharp line. Also, if multiple T-Edges are adjacent, recursion may be necessary.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="checkAllFaces">if set to <c>true</c> [check all faces].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SetNegligibleAreaFaceNormals()
        {
            var facesToCheck = FacesWithNegligibleArea.ToList();
            var success = facesToCheck.Count == 0;
            while (success || facesToCheck.Count > 0)
            {
                success = false;
                for (int i = facesToCheck.Count - 1; i >= 0; i--)
                {
                    foreach (var adjFace in facesToCheck[i].AdjacentFaces)
                    {
                        if (!adjFace._normal.IsNull())
                        {
                            facesToCheck[i]._normal = adjFace._normal;
                            facesToCheck.RemoveAt(i);
                            success = true;
                            break;
                        }
                    }
                }
            }
            return facesToCheck.Count == 0;
        }

        /// <summary>
        /// Flips the faces based on bad angles.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool FlipFacesBasedOnInconsistentEdges()
        {
            var facesToConsiderHashSet = new HashSet<TriangleFace>();
            var singleFaces = new HashSet<TriangleFace>();
            foreach (var facePair in InconsistentMatingFacePairs)
            {
                if (!singleFaces.Add(facePair.Item1))
                    facesToConsiderHashSet.Add(facePair.Item1);
                if (!singleFaces.Add(facePair.Item2))
                    facesToConsiderHashSet.Add(facePair.Item2);
            }
            var facesToConsider = new Stack<TriangleFace>(facesToConsiderHashSet);
            // facesToConsider are faces that have 2 or 3 edges that are inconsistent with their neighbors
            var passes = ts.NumberOfFaces;
            while (facesToConsider.Count > 0 && passes-- > 0)
            {
                var face = facesToConsider.Pop();
                face.Invert();
                foreach (var oldEdge in face.Edges)
                {
                    var otherFace = oldEdge.GetMatingFace(face);
                    var index = oldEdge.IndexInList;
                    ts.Edges[index] = new Edge(face.A, face.B, face, otherFace, true);
                    if (0 == WhichFaceOwnsEdge(face, otherFace, face.A, face.B))
                        if (!singleFaces.Add(otherFace))
                            facesToConsider.Push(otherFace);
                }
            }
            if (facesToConsider.Count == 0 && singleFaces.Count == 0)
            {
                InconsistentMatingFacePairs = null;
                return true;
            }
            return false;
        }



        /// <summary>
        /// Teases apart over-defined edges. By taking in the edges with more than two faces (the over-used edges) a list is
        /// return of newly defined edges.
        /// </summary>
        /// <param name="overUsedEdgesDictionary">The over used edges dictionary.</param>
        /// <param name="moreSingleSidedEdges">The more single sided edges.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;
        /// TVGL.TriangleFace&gt;&gt;&gt;.</returns>
        private void TeaseApartOverUsedEdges(List<List<(TriangleFace, bool)>> overusedEdges)
        {
            foreach (var entry in overusedEdges)
            {
                while (entry.Count > 1)
                {
                    var highestDot = -2.0;
                    var bestI = -1;
                    var bestJ = -1;
                    for (int i = 0; i < entry.Count - 1; i++)
                    {
                        for (int j = i + 1; j < entry.Count; j++)
                        {
                            if (entry[i] != entry[j])
                            {
                                var dot = entry[i].Item1.Normal.Dot(entry[j].Item1.Normal);
                                if (highestDot < dot)
                                {
                                    bestI = i;
                                    bestJ = j;
                                    highestDot = dot;
                                }
                            }
                        }
                    }
                    if (highestDot == -2.0) entry.Last().Item1.Invert();
                    else
                    {
                        FacePairsForEdges.Add((entry[bestI].Item1, entry[bestJ].Item1));
                        entry.RemoveAt(bestJ);
                        entry.RemoveAt(bestI);
                    }
                }
                if (entry.Count == 1)
                {
                    var lastNewPair = FacePairsForEdges.Last();
                    (var fromVertex, var toVertex) = GetCommonVertices(lastNewPair.Item1, lastNewPair.Item2, false);
                    if (entry[0].Item2)
                        SingleSidedEdgeData.Add((entry[0].Item1, fromVertex, toVertex));
                    else
                        SingleSidedEdgeData.Add((entry[0].Item1, toVertex, fromVertex));
                }
            }
        }


        /// <summary>
        /// Matches the up remaining single sided edge.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="keptToRemovedDictionary">The removed replacements.</param>
        /// <returns>A list of (TriangleFace, TriangleFace).</returns>
        private void MatchUpRemainingSingleSidedEdge(out Dictionary<Vertex, List<Vertex>> keptToRemovedDictionary)
        {
            keptToRemovedDictionary = new Dictionary<Vertex, List<Vertex>>();
            var maxtTolerance = 100 * ts.SameTolerance * ts.SameTolerance;
            var orderedEdges = SingleSidedEdgeData.Select(s => (s.Item1, s.Item2, s.Item3,
            (s.Item2.Coordinates - s.Item3.Coordinates).LengthSquared())).OrderBy(s => s.Item4).ToList();
            for (int i = orderedEdges.Count - 1; i >= 0; i--)
            {
                var ithEdge = orderedEdges[i];
                var fromI = ithEdge.Item2;
                var toI = ithEdge.Item3;
                var ithLengthSqd = ithEdge.Item4;
                var minLength = 0.81 * ithLengthSqd;
                var j = i;
                var minJ = Math.Max(0, i - 10);
                var minDist = maxtTolerance;
                var bestJIndex = -1;
                var sameDirection = false;
                while (j > minJ)
                {
                    j--;
                    var jthEdge = orderedEdges[j];
                    var jthLengthSqd = jthEdge.Item4;
                    if (jthLengthSqd < minLength) break;
                    var distSqd =
                        Vector3.DistanceSquared(fromI.Coordinates, jthEdge.Item2.Coordinates) +
                        Vector3.DistanceSquared(toI.Coordinates, jthEdge.Item3.Coordinates);
                    if (minDist > distSqd)
                    {
                        minDist = distSqd;
                        bestJIndex = j;
                        sameDirection = true;
                    }
                    distSqd = Vector3.DistanceSquared(fromI.Coordinates, jthEdge.Item3.Coordinates) +
                        Vector3.DistanceSquared(toI.Coordinates, jthEdge.Item2.Coordinates);
                    if (minDist > distSqd)
                    {
                        minDist = distSqd;
                        bestJIndex = j;
                        sameDirection = false;
                    }
                }
                if (bestJIndex > 0)
                {
                    var jMatchWithFromI = sameDirection ? orderedEdges[bestJIndex].Item2 : orderedEdges[bestJIndex].Item3;
                    var jMatchWithToI = sameDirection ? orderedEdges[bestJIndex].Item3 : orderedEdges[bestJIndex].Item2;
                    if (fromI == jMatchWithFromI) //many times the match will actually be the same vertex.
                        // in which case, don't delete, but do the other end. They both can't be the same - otherwise we wouldn't
                        // be in this situation.
                        MergeMakeEntries(keptToRemovedDictionary, toI, jMatchWithToI);
                    else MergeMakeEntries(keptToRemovedDictionary, fromI, jMatchWithFromI);
                }
                if (sameDirection)
                    InconsistentMatingFacePairs.Add((ithEdge.Item1, orderedEdges[bestJIndex].Item1));
                else
                    FacePairsForEdges.Add((ithEdge.Item1, orderedEdges[bestJIndex].Item1));
                orderedEdges.RemoveAt(i);
                orderedEdges.RemoveAt(j);
            }
            SingleSidedEdgeData.AddRange(orderedEdges.Select(edge => (edge.Item1, edge.Item3, edge.Item3)));
        }

        private static void MergeMakeEntries(Dictionary<Vertex, List<Vertex>> keptToRemovedDictionary, Vertex a, Vertex b)
        {
            var aIsFound = keptToRemovedDictionary.TryGetValue(a, out var aEntry);
            var bIsFound = keptToRemovedDictionary.TryGetValue(b, out var bEntry);
            if (aIsFound && bIsFound)
            {
                aEntry.AddRange(bEntry);
                aEntry.Add(b);
            }
            else if (aIsFound)
                aEntry.Add(b);
            else if (bIsFound)
                bEntry.Add(a);
            else keptToRemovedDictionary.Add(a, new List<Vertex> { b });
        }

        /// <summary>
        /// Merges the edge vertices.
        /// </summary>
        /// <param name="removedToKept">The removed to kept.</param>
        /// <param name="keptToRemoved">The kept to removed.</param>
        /// <param name="keepVertex">The keep coord.</param>
        /// <param name="removeVertex">The remove coord.</param>
        private void MergeVertices(Dictionary<Vertex, List<Vertex>> keptToRemovedDictionary)
        {
            foreach (var keyValuePair in keptToRemovedDictionary)
            {
                var keepVertex = keyValuePair.Key;
                var removedVertices = keyValuePair.Value;
                foreach (var removeVertex in removedVertices)
                {
                    foreach (var face in removeVertex.Faces)
                        face.ReplaceVertex(removeVertex, keepVertex);
                }
                keepVertex.Coordinates += removedVertices.Select(v => v.Coordinates).Aggregate((c, sum) => c + sum);
                keepVertex.Coordinates /= (removedVertices.Count + 1);
            }
        }



        #endregion Repair Functions
        /// <summary>
        /// Finds all non smooth edges in the tessellated solid.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="primitives">The primitives.</param>
        void FindNonSmoothEdges()
        {
            var characteristicLength = Math.Sqrt((ts.Bounds[1] - ts.Bounds[0]).ToArray().Sum(p => p * p));
            var maxRadius = 33 * characteristicLength; //so a 1x1x1 part would have a max primitive radius of 100
            var error = characteristicLength / 2000;
            if (ts.NonsmoothEdges == null) ts.NonsmoothEdges = new List<EdgePath>();
            var nonSmoothHash = new HashSet<Edge>(ts.NonsmoothEdges.SelectMany(ep => ep.EdgeList));
            // first, define any edges for any existing primitive surfaces as non-smooth
            if (ts.Primitives != null)
                foreach (var primitive in ts.Primitives)
                    foreach (var outerEdge in primitive.OuterEdges)
                        if (!nonSmoothHash.Contains(outerEdge))
                        {
                            nonSmoothHash.Add(outerEdge);
                            var edgePath = new EdgePath();
                            edgePath.AddEnd(outerEdge);
                            ts.NonsmoothEdges.Add(edgePath);
                        }
            // then, add any conventional edges that have an abrupt change in angle
            foreach (var e in ts.Edges)
            {
                if (nonSmoothHash.Contains(e)) continue;
                if (e.IsDiscontinous(ts.SameTolerance, error))
                {
                    nonSmoothHash.Add(e);
                    var edgePath = new EdgePath();
                    edgePath.AddEnd(e);
                    ts.NonsmoothEdges.Add(edgePath);
                }
            }
        }



        /// <summary>
        /// Repairs the holes.
        /// </summary>
        private void RepairHoles()
        {
            var loops = OrganizeIntoLoops(SingleSidedEdges, out _);
            var newElements = CreateMissingEdgesAndFaces(loops, out var newFaces, out var remainingEdges2);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the missing edges and faces.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="newFaces">The new faces.</param>
        /// <param name="remainingEdges">The remaining edges.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Edge, TriangleFace, TriangleFace&gt;&gt;.</returns>
        private static IEnumerable<(Edge, TriangleFace, TriangleFace)> CreateMissingEdgesAndFaces(
                    IEnumerable<TriangulationLoop> loops,
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
                    Message.output("Recieving hole with only " + loop.Count + " edges.", 2);
                    continue;
                }
                Message.output("Patching hole #" + ++k + " (has " + loop.Count + " edges) in tessellation.", 2);
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
                        var checksum = Edge.SetAndGetEdgeChecksum(eAD.edge);
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
                                var checksum = Edge.GetEdgeChecksum(fromVertex, toVertex);
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
                                var checksum = Edge.GetEdgeChecksum(edgeAnddir.edge.From, edgeAnddir.edge.To);
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

        public void AutoRepair()
        {
            throw new NotImplementedException();
        }
    }
}