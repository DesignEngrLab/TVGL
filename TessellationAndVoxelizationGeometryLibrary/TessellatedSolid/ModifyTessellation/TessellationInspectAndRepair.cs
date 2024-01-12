// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellationInspectAndRepair.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TVGL;

namespace TVGL
{
    /// <summary>
    /// Stores errors in the tessellated solid
    /// </summary>
    public class TessellationInspectAndRepair
    {
        private readonly TessellatedSolid ts;

        private TessellationInspectAndRepair(TessellatedSolid ts)
        {
            this.ts = ts;
            ContainsErrors = false;
            OverusedEdges = new List<List<(TriangleFace, bool)>>();
            SingleSidedEdgeData = new List<(TriangleFace, Vertex, Vertex)>();
            FacesWithNegligibleArea = new List<TriangleFace>();
            FacePairsForEdges = new List<(TriangleFace, TriangleFace)>();
            InconsistentMatingFacePairs = new List<(TriangleFace, TriangleFace)>();
            ModelHasNegativeVolume = false;
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
        /// The data needed to build edges used only during construction
        /// </summary>
        /// <value>The singled sided edges.</value>
        private List<(TriangleFace, Vertex, Vertex)> SingleSidedEdgeData { get; set; }

        /// <summary>
        /// Gets the single sided edges.
        /// </summary>
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
        internal static void CompleteBuildOptions(TessellatedSolid ts, TessellatedSolidBuildOptions buildOptions,
            out List<TriangleFace> removedFaces)
        {
            if (buildOptions == null) buildOptions = TessellatedSolidBuildOptions.Default;
            if (!buildOptions.CheckModelIntegrity)
            {
                removedFaces = new List<TriangleFace>();
                return;
            }
            var buildAndErrorInfo = new TessellationInspectAndRepair(ts);
            buildAndErrorInfo.CompleteBuildOptions(buildOptions, out removedFaces);
        }

        /// <summary>
        /// Completes the constructor by addressing any other build options in TessellatedSolidBuildOptions
        /// with the exception of CopyElementsPassedToConstructor. These are actually handled in the main constructor
        /// body - but only one constructor uses it: the one accepting faces and vertices.
        /// </summary>
        /// <param name="fromSTL">if set to <c>true</c> [from STL].</param>
        void CompleteBuildOptions(TessellatedSolidBuildOptions buildOptions,
            out List<TriangleFace> removedFaces)
        {
            removedFaces = new List<TriangleFace>();
            CheckModelIntegrityPreBuild();
            if (buildOptions.FixEdgeDisassociations && OverusedEdges.Count + SingleSidedEdgeData.Count > 0)
            {
                try
                {
                    TeaseApartOverUsedEdges();
                    var removedVertices = MatchUpRemainingSingleSidedEdge();
                    ts.RemoveVertices(removedVertices);
                }
                catch
                {
                    Message.output("Error setting up all faces-edges-vertices associations.", 1);
                }
            }
            if (buildOptions.AutomaticallyRepairNegligibleTFaces && InconsistentMatingFacePairs.Any())
                try
                {
                    if (!FlipFacesBasedOnInconsistentEdges())
                        Message.output("Unable to resolve face all inconsistent faces.", 1);
                }
                catch
                {
                    //Continue
                }
            if (buildOptions.AutomaticallyInvertNegativeSolids && ModelHasNegativeVolume)
                ts.TurnModelInsideOut();
            // the remaining items will need edges so we need to build these here
            if (buildOptions.PredefineAllEdges)
                try
                {
                    MakeEdges();
                }
                catch
                {
                    Message.output("Unable to construct edges.", 1);
                }
            if (buildOptions.AutomaticallyRepairNegligibleTFaces && buildOptions.PredefineAllEdges)
            {
                //try
                //{
                if (!PropagateFixToNegligibleFaces(removedFaces))
                    Message.output("Unable to flip edges to avoid negligible faces.", 1);
                //}
                //catch
                //{
                //    //Continue
                //}
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
                        Message.output("Unable to repair all holes in the model.", 1);
                    }
            }
            //If the volume is zero, creating the convex hull may cause a null exception
            if (buildOptions.DefineConvexHull && !ts.Volume.IsNegligible())
                try
                {
                    ts.BuildConvexHull();
                }
                catch
                {
                    Message.output("Unable to create convex hull.", 1);

                }
            if (buildOptions.FindNonsmoothEdges)
            {
                if (!buildOptions.PredefineAllEdges)
                    throw new ArgumentException("Finding Nonsmooth Edges requires PredefineAllEdges to be true.");
                try
                {
                    FindNonSmoothEdges();
                }
                catch
                {
                    Message.output("Unable to find all non-smooth edges.", 1);
                }
            }
            if (buildOptions.CheckModelPostBuild)
            {
                try
                {
                    CheckModelIntegrityPostBuild();
                }
                catch
                {
                    Message.output("Failed final post-build check.", 1);
                }
            }
        }
        #endregion

        /// <summary>
        /// This function will solve the problem of faces with negligible error, by changing the tessellation
        /// with the neighbor. Imagine a quadrilateral divided into two triangles. if one of those triangles
        /// has negligible area, then it's 3 edges are nearly collinear. the inner edge is flipped to the 
        /// other two vertices of the quadrilateral, so that 2 proper triangles are created.
        /// </summary>
        /// <returns>A bool.</returns>
        private bool PropagateFixToNegligibleFaces(List<TriangleFace> removedFaces)
        {
            var faceHash = FacesWithNegligibleArea.ToHashSet();
            var negligibleArea = ts.SameTolerance * ts.SameTolerance;
            while (faceHash.Count > 0)
            {
                var face = faceHash.First();
                var shortestEdge = ShortestEdge(face);
                // if the shortest edge is negligible, then we need to remove the vertex and the face
                if (shortestEdge.Length <= ts.SameTolerance)
                {
                    shortestEdge.CollapseEdge(out var removedEdges);
                    ts.RemoveVertex(shortestEdge.From);
                    removedFaces.Add(shortestEdge.OwnedFace);
                    faceHash.Remove(shortestEdge.OwnedFace);
                    removedFaces.Add(shortestEdge.OtherFace);
                    faceHash.Remove(shortestEdge.OtherFace);
                    ts.RemoveFaces(new[] { shortestEdge.OwnedFace, shortestEdge.OtherFace });
                    ts.RemoveEdges(removedEdges);
                }
                else
                { // else we need to flip the longest edge as described above
                    var longestEdge = LongestEdge(face);
                    if (longestEdge.GetMatingFace(face).Area.IsNegligible(negligibleArea))
                    {   // the mating face is also negligible, so we need to remove 2 faces, 

                        var removedVertex = longestEdge.OwnedFace.OtherVertex(longestEdge);
                        var keepVertex = longestEdge.OtherFace.OtherVertex(longestEdge);
                        ModifyTessellation.MergeVertexAndKill3EdgesAnd2Faces(removedVertex, keepVertex,
                            longestEdge.OwnedFace, longestEdge.OtherFace, out var removedEdges);
                        ts.RemoveVertex(removedVertex);
                        removedFaces.Add(longestEdge.OwnedFace);
                        faceHash.Remove(longestEdge.OwnedFace);
                        removedFaces.Add(longestEdge.OtherFace);
                        faceHash.Remove(longestEdge.OtherFace);
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
        /// Checks the model integrity. This sets up various dictionaries and lists that are used to
        /// build the proper 'graph' of the model - mainly the edges. 
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="repairAutomatically">The repair automatically.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        void CheckModelIntegrityPreBuild()
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

            IncorrectFaceEdgeRatio = 3 * ts.NumberOfFaces == 2 * (alreadyDefinedEdges.Count + (numOverUsedFaceEdges + numSingleSidedEdges) / 2);

            if (ContainsErrors)
            {
                ts.Errors = this;
                ReportErrors(ts.Errors);
            }
            else
            {
                ts.Errors = null;
                Message.output("No errors found.", 3);
            }
        }

        /// <summary>
        /// Checks the model integrity after it has been built. It runs through all vertices, edges and faces to see if 
        /// everything is properly doubly linked.
        /// </summary>
        void CheckModelIntegrityPostBuild()
        {
            if (3 * ts.NumberOfFaces != 2 * ts.NumberOfEdges)
                Message.output("3 x numFaces = " + 3 * ts.NumberOfFaces + ", 2 x numEdges = " + 2 * ts.NumberOfEdges, 0);
            if (ts.Errors == null)
                Message.output("No errors were found initially.", 2);
            else
            {
                Message.output("Errors were found initially.", 2);
            }//Check if each face has cyclic references with each edge, vertex, and adjacent faces.
            var numSingleSidedEdges = 0;
            var errors = false;
            foreach (var face in ts.Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (edge.OwnedFace != face && edge.OtherFace != face)
                    {
                        errors = true;
                        Message.output("face's edge doesn't reconnect to face", 0);
                    }
                }
                foreach (var vertex in face.Vertices.Where(vertex => !vertex.Faces.Contains(face)))
                {
                    errors = true;
                    Message.output("face's vertex doesn't reconnect to face", 0);
                }
            }
            //Check if each edge has cyclic references with each vertex and each face.
            foreach (var edge in ts.Edges)
            {
                if (!edge.OwnedFace.Edges.Contains(edge))
                {
                    errors = true;
                    Message.output("edge's face doesn't reconnect to edge", 0);
                }
                if (edge.OtherFace == null)
                    numSingleSidedEdges++;
                else if (!edge.OtherFace.Edges.Contains(edge))
                {
                    errors = true;
                    Message.output("edge's face doesn't reconnect to edge", 0);
                }
                if (!edge.From.Edges.Contains(edge))
                {
                    errors = true;
                    Message.output("edge's vertex doesn't reconnect to edge", 0);
                }
                if (!edge.To.Edges.Contains(edge))
                {
                    errors = true;
                    Message.output("edge's vertex doesn't reconnect to edge", 0);
                }
            }
            if (numSingleSidedEdges != 0 ||
                (ts.Errors != null && ts.Errors.SingleSidedEdges != null && numSingleSidedEdges != ts.Errors.SingleSidedEdges.Count)
                || (ts.Errors != null && ts.Errors.SingleSidedEdges != null && ts.Errors.SingleSidedEdges?.Count != 0))
            {
                errors = true;
                Message.output("there are " + numSingleSidedEdges + " single sided edges", 0);
            }
            //Check if each vertex has cyclic references with each edge and each face.
            foreach (var vertex in ts.Vertices)
            {
                foreach (var edge in vertex.Edges.Where(edge => edge.To != vertex && edge.From != vertex))
                {
                    errors = true;
                    Message.output("vertex's edge doesn't reconnect to vertex", 0);
                }
                foreach (var face in vertex.Faces.Where(face => !face.Vertices.Contains(vertex)))
                {
                    errors = true;
                    Message.output("vertex's face doesn't reconnect to vertex", 0);
                }
            }
            if (errors)
                Message.output("The model still contains errors.", 0);
            else if (ts.Errors != null)
            {
                Message.output("All errors in the model have been fixed.", 1);
                ts.Errors = null;
            }
        }

        /// <summary>
        /// Gets the common vertices between two faces.
        /// </summary>
        /// <param name="face1">The face1.</param>
        /// <param name="face2">The face2.</param>
        /// <param name="bothThinkTheyOwnEdge">If true, both think they own edge.</param>
        /// <returns>A (Vertex fromVertex, Vertex toVertex) .</returns>
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

        private static bool DoesFaceOwnEdge(TriangleFace face, Vertex from, Vertex to)
        {
            return (face.A == from && face.B == to) ||
                 (face.B == from && face.C == to) ||
                 (face.C == from && face.A == to);
        }

        private static void ReportErrors(TessellationInspectAndRepair tsErrors)
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
            ts.NumberOfEdges = FacePairsForEdges.Count
                + InconsistentMatingFacePairs.Count + SingleSidedEdgeData.Count;
            ts.Edges = new Edge[ts.NumberOfEdges];
            var i = 0;
            // first make the proper edges between two faces
            for (; i < FacePairsForEdges.Count; i++)
            {
                (TriangleFace, TriangleFace) facePair = FacePairsForEdges[i];
                var ownedFace = facePair.Item1;
                var otherFace = facePair.Item2;
                (var fromVertex, var toVertex) = GetCommonVertices(ownedFace, otherFace, false);
                ts.Edges[i] = new Edge(fromVertex, toVertex, ownedFace, otherFace, true);
                ts.Edges[i].IndexInList = i;
            }
            // next are the edges between two faces that are inconsistent - meaning the order of 
            // vertices in the two faces are in the same direction. They either both think they own the edge
            // or neither thinks they own the edge.
            for (i = FacePairsForEdges.Count; i < FacePairsForEdges.Count + InconsistentMatingFacePairs.Count; i++)
            {
                (TriangleFace, TriangleFace) facePair = InconsistentMatingFacePairs[i - FacePairsForEdges.Count];
                var ownedFace = facePair.Item1;
                var otherFace = facePair.Item2;
                (var fromVertex, var toVertex) = GetCommonVertices(ownedFace, otherFace, true);
                ts.Edges[i] = new Edge(fromVertex, toVertex, ownedFace, otherFace, true);
                ts.Edges[i].IndexInList = i;
            }
            // finally, the single-sided edges are defined.
            if (SingleSidedEdgeData.Count > 0)
            {
                SingleSidedEdges = new List<Edge>(SingleSidedEdgeData.Count);
                foreach (var ssed in SingleSidedEdgeData)
                {
                    var newEdge = new Edge(ssed.Item2, ssed.Item3, ssed.Item1, null, true);
                    newEdge.IndexInList = i;
                    ts.Edges[i++] = newEdge;
                    SingleSidedEdges.Add(newEdge);
                }
            }
            SingleSidedEdgeData = null;
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
            var faceToIndexDictionary = new Dictionary<TriangleFace, List<int>>();
            for (int i = 0; i < InconsistentMatingFacePairs.Count; i++)
            {
                var facePair = InconsistentMatingFacePairs[i];
                if (faceToIndexDictionary.TryGetValue(facePair.Item1, out var listOfIndices))
                    listOfIndices.Add(i);
                else faceToIndexDictionary.Add(facePair.Item1, new List<int> { i });
                if (faceToIndexDictionary.TryGetValue(facePair.Item2, out listOfIndices))
                    listOfIndices.Add(i);
                else faceToIndexDictionary.Add(facePair.Item2, new List<int> { i });
            }
            // build three hashsets of faces with 3, 2, and 1 inconsistent edges
            var facesWith3InconsistentEdges = new HashSet<TriangleFace>();
            var facesWith2InconsistentEdges = new HashSet<TriangleFace>();
            var facesWith1InconsistentEdges = new HashSet<TriangleFace>();
            foreach (var face in faceToIndexDictionary)
            {
                if (face.Value.Count == 3) facesWith3InconsistentEdges.Add(face.Key);
                else if (face.Value.Count == 2) facesWith2InconsistentEdges.Add(face.Key);
                else if (face.Value.Count == 1) facesWith1InconsistentEdges.Add(face.Key);
            }
            // without edge connectivity information, we can't do anything much here
            // to be thorough, such a function needs to be evaluated after edges are defined.
            // that hasn't been written yet.
            var indicesToRemove = new List<int>();
            // the easy case to solve are faces with 3 inconsistent edges
            while (facesWith3InconsistentEdges.Count > 0)
            {
                var face = facesWith3InconsistentEdges.First();
                facesWith3InconsistentEdges.Remove(face);
                face.Invert();
                var listOfIndices = faceToIndexDictionary[face];
                foreach (var index in listOfIndices)
                {
                    var pair = InconsistentMatingFacePairs[index];
                    FacePairsForEdges.Add(pair);
                    indicesToRemove.Add(index);
                    var neighbor = pair.Item1 == face ? pair.Item2 : pair.Item1;
                    if (facesWith3InconsistentEdges.Contains(neighbor))
                    {
                        facesWith3InconsistentEdges.Remove(neighbor);
                        facesWith2InconsistentEdges.Add(neighbor);
                    }
                    else if (facesWith2InconsistentEdges.Contains(neighbor))
                    {
                        facesWith2InconsistentEdges.Remove(neighbor);
                        facesWith1InconsistentEdges.Add(neighbor);
                    }
                    else facesWith1InconsistentEdges.Remove(neighbor);
                }
            }
            foreach (var index in indicesToRemove.OrderByDescending(i => i))
                InconsistentMatingFacePairs.RemoveAt(index);
            return InconsistentMatingFacePairs.Count == 0;
        }



        /// <summary>
        /// Teases apart over-defined edges. By taking in the edges with more than two faces (the over-used edges) a list is
        /// return of newly defined edges.
        /// </summary>
        /// <param name="overUsedEdgesDictionary">The over used edges dictionary.</param>
        /// <param name="moreSingleSidedEdges">The more single sided edges.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;
        /// TVGL.TriangleFace&gt;&gt;&gt;.</returns>
        private void TeaseApartOverUsedEdges()
        {
            foreach (var entry in OverusedEdges)
            {
                //#if PRESENT
                //                foreach (var face in ts.Faces)
                //                    face.Color = new Color(0.5f, 0.75f, 0.75f, 0.75f);
                //                foreach (var facePair in entry)
                //                    facePair.Item1.Color = new Color(1f, 1f, 0.0f, 0.5f);
                //                ts.HasUniformColor = false;
                //                Presenter.ShowAndHang(ts);
                //                Presenter.ShowAndHang(entry.Select(p => p.Item1).ToList());
                //#endif
                (var fromVertex, var toVertex) = GetCommonVertices(entry[0].Item1, entry[1].Item1, entry[1].Item2);
                while (entry.Count > 1)
                {
                    // the decision of what two faces to pair is simply based on the highest dot-product of the normals
                    // note tht this loop is nearly identical to the one below. First we run this loop to find faces that
                    // have proper owned/other relationships. Then we run the loop below to find faces that don't
                    var highestDot = -2.0;
                    var bestI = -1;
                    var bestJ = -1;
                    for (int i = 0; i < entry.Count - 1; i++)
                    {
                        for (int j = i + 1; j < entry.Count; j++)
                        {
                            if (entry[i].Item2 != entry[j].Item2) // this is the condition that they are owned/other
                            {
                                double dot = entry[i].Item1.Normal.Dot(entry[j].Item1.Normal);
                                if (highestDot < dot)
                                {
                                    bestI = i;
                                    bestJ = j;
                                    highestDot = dot;
                                }
                            }
                        }
                    }
                    if (highestDot != -2.0) //entry.Last().Item1.Invert();
                    {
                        FacePairsForEdges.Add((entry[bestI].Item1, entry[bestJ].Item1));
                        entry.RemoveAt(bestJ);
                        entry.RemoveAt(bestI);
                    }
                    else
                    {   // hmm, all the faces are on the same side. This is a problem, so we'll re-run
                        // it and find the best
                        for (int i = 0; i < entry.Count - 1; i++)
                        {
                            for (int j = i + 1; j < entry.Count; j++)
                            {
                                var dot = Math.Abs(entry[i].Item1.Normal.Dot(entry[j].Item1.Normal));
                                if (highestDot < dot)
                                {
                                    bestI = i;
                                    bestJ = j;
                                    highestDot = dot;
                                }
                            }
                        }
                        InconsistentMatingFacePairs.Add((entry[bestI].Item1, entry[bestJ].Item1));
                        entry.RemoveAt(bestJ);
                        entry.RemoveAt(bestI);
                    }
                }
                if (entry.Count == 1)
                {
                    if (entry[0].Item2)
                        SingleSidedEdgeData.Add((entry[0].Item1, fromVertex, toVertex));
                    else
                        SingleSidedEdgeData.Add((entry[0].Item1, toVertex, fromVertex));
                }
            }
            OverusedEdges = null;
        }


        /// <summary>
        /// Matches the up remaining single sided edge.
        /// </summary>
        /// <param name="singleSidedEdges">The single sided edges.</param>
        /// <param name="keptToRemovedDictionary">The removed replacements.</param>
        /// <returns>A list of (TriangleFace, TriangleFace).</returns>
        private IEnumerable<Vertex> MatchUpRemainingSingleSidedEdge()
        {
            //#if PRESENT
            //            var relatedFaces = SingleSidedEdgeData.Select(s => s.Item1).ToList();
            //            if (relatedFaces.Count > 0)
            //            {
            //                Presenter.ShowAndHang(ts);
            //                Presenter.ShowAndHang(relatedFaces);
            //            }
            //#endif
            // we need both remove-to-kept and kept-to-remove dictionaries because as new faces are found
            // we need to know if a previous decision has been made to keep a particular face or not. In a drastic
            // case you can imagine many removed faces being funneled down to only a few kept faces.
            var removedToKeptDictionary = new Dictionary<Vertex, Vertex>();
            var keptToRemovedDictionary = new Dictionary<Vertex, HashSet<Vertex>>();
            var maxTolerance = 100 * ts.SameTolerance; // making the tolerance 100 times larger
            var orderedEdges = SingleSidedEdgeData.Select(s => (s.Item1, s.Item2, s.Item3,
            (s.Item3.Coordinates - s.Item2.Coordinates).Length())).OrderBy(s => s.Item4).ToList();
            for (int i = orderedEdges.Count - 1; i >= 0; i--)
            {
                var ithEdge = orderedEdges[i];
                var fromI = ithEdge.Item2;
                var toI = ithEdge.Item3;
                var ithLength = ithEdge.Item4;
                var minLength = ithLength - 2 * maxTolerance; // because there are 2 ends of the edge f
                                                              // that can be of by the tolerance
                var j = i;
                var minDist = maxTolerance;
                var bestJIndex = -1;
                var sameDirection = false;
                while (j > 0)
                {
                    j--;
                    var jthEdge = orderedEdges[j];
                    var jthLength = jthEdge.Item4;
                    if (jthLength < minLength) break;
                    var distSqd = double.PositiveInfinity;
                    if (fromI != jthEdge.Item3 && toI != jthEdge.Item2)
                    {
                        distSqd =
                            Vector3.DistanceSquared(fromI.Coordinates, jthEdge.Item2.Coordinates) +
                            Vector3.DistanceSquared(toI.Coordinates, jthEdge.Item3.Coordinates);
                        if (minDist > distSqd)
                        {
                            minDist = distSqd;
                            bestJIndex = j;
                            sameDirection = true;
                        }
                    }
                    if (fromI != jthEdge.Item2 && toI != jthEdge.Item3)
                    {
                        distSqd = Vector3.DistanceSquared(fromI.Coordinates, jthEdge.Item3.Coordinates) +
                        Vector3.DistanceSquared(toI.Coordinates, jthEdge.Item2.Coordinates);
                        if (minDist > distSqd)
                        {
                            minDist = distSqd;
                            bestJIndex = j;
                            sameDirection = false;
                        }
                    }
                }
                if (bestJIndex >= 0)
                {
                    var jMatchWithFromI = sameDirection ? orderedEdges[bestJIndex].Item2 : orderedEdges[bestJIndex].Item3;
                    var jMatchWithToI = sameDirection ? orderedEdges[bestJIndex].Item3 : orderedEdges[bestJIndex].Item2;
                    if (fromI != jMatchWithFromI) //many times the match will actually be the same vertex.
                                                  // in which case, we don't need to add it to the remove/kept dictionary
                        MergeMakeEntries(keptToRemovedDictionary, removedToKeptDictionary, fromI, jMatchWithFromI);
                    //MergeMakeEntries(removedToKeptDictionary, fromI, jMatchWithFromI);
                    if (toI != jMatchWithToI)
                        MergeMakeEntries(keptToRemovedDictionary, removedToKeptDictionary, toI, jMatchWithToI);

                    if (sameDirection)
                        InconsistentMatingFacePairs.Add((ithEdge.Item1, orderedEdges[bestJIndex].Item1));
                    else
                        FacePairsForEdges.Add((ithEdge.Item1, orderedEdges[bestJIndex].Item1));
                    orderedEdges.RemoveAt(i);
                    orderedEdges.RemoveAt(bestJIndex);
                    i--; // since we removed two from the list, we need to decrement the index by an additional value.
                }
            }
            // now, we use the keptToRemovedDictionary to fix the references in the faces and update the coordinates of the kept vertices
            foreach (var keyValuePair in removedToKeptDictionary)
            {
                var removeVertex = keyValuePair.Key;
                var keptVertex = keyValuePair.Value;
                foreach (var face in removeVertex.Faces.ToList())
                    face.ReplaceVertex(removeVertex, keptVertex);
                keptVertex.Coordinates += removeVertex.Coordinates;
                keptVertex.Coordinates *= 0.5;
            }
            // in addition to updating the faces, we must update the tuple in SingleSidedEdgeData. Additionally,
            // the reassignment may have solved further single-sided edges into correct pair. Or collapsed face if
            // two or three vertices become equal or two triangles become equal.
            var newPossibleMatches = new Dictionary<long, int>();
            var indicesToRemove = new List<int>();
            for (int index = 0; index < orderedEdges.Count; index++)
            {
                (TriangleFace, Vertex, Vertex, double) edgeData = orderedEdges[index];
                var face = edgeData.Item1;
                var fromVertex = edgeData.Item2;
                var toVertex = edgeData.Item3;
                if (removedToKeptDictionary.TryGetValue(fromVertex, out var newFromVertex))
                    fromVertex = newFromVertex;
                if (removedToKeptDictionary.TryGetValue(toVertex, out var newToVertex))
                    toVertex = newToVertex;
                if (fromVertex == toVertex) // then triangle has collapsed to a line (effectively redunandant with edge)
                {
                    ts.RemoveFace(face);
                    var neighborFaces = GetNeighborsPreEdgeCreation(face);
                    var leftFace = neighborFaces.FirstOrDefault();
                    var rightFace = neighborFaces.LastOrDefault();
                    var commonVerts = GetCommonVertices(leftFace, rightFace, false);
                    toVertex = commonVerts.fromVertex == toVertex ? commonVerts.toVertex : commonVerts.fromVertex;
                    if (leftFace == null || rightFace == null)
                        continue;
                    if (DoesFaceOwnEdge(leftFace, fromVertex, toVertex) == DoesFaceOwnEdge(rightFace, fromVertex, toVertex))
                        InconsistentMatingFacePairs.Add((leftFace, rightFace));
                    else if (DoesFaceOwnEdge(rightFace, fromVertex, toVertex))
                        FacePairsForEdges.Add((rightFace, leftFace));
                    else FacePairsForEdges.Add((leftFace, rightFace));
                    continue;
                }
                // the next loop checks for new matches with the edge checksum values
                var checksum = Edge.GetEdgeChecksum(fromVertex, toVertex);
                if (newPossibleMatches.TryGetValue(checksum, out var oldIndex))
                {
                    var oldEdgeData = orderedEdges[oldIndex];
                    var oldFace = oldEdgeData.Item1;
                    var oldFaceOwns = DoesFaceOwnEdge(oldFace, fromVertex, toVertex);
                    var newFaceOwns = DoesFaceOwnEdge(face, fromVertex, toVertex);
                    if (oldFaceOwns == newFaceOwns)
                        InconsistentMatingFacePairs.Add((face, oldFace));
                    else if (oldFaceOwns)
                        FacePairsForEdges.Add((oldFace, face));
                    else FacePairsForEdges.Add((face, oldFace));
                    newPossibleMatches.Remove(checksum);
                    indicesToRemove.Add(oldIndex);
                    indicesToRemove.Add(index);
                }
                else newPossibleMatches[checksum] = index;
            }
            SingleSidedEdgeData.Clear();
            indicesToRemove.Sort();
            var nextIndexToAvoid = 0;
            // finally, the SingleSidedEdgeData is updated to reflect the new vertex references
            for (int i = 0; i < orderedEdges.Count; i++)
            {
                if (nextIndexToAvoid < indicesToRemove.Count && i == indicesToRemove[nextIndexToAvoid])
                    nextIndexToAvoid++;
                else
                {
                    var edgeData = orderedEdges[i];
                    var face = edgeData.Item1;
                    var fromVertex = edgeData.Item2;
                    var toVertex = edgeData.Item3;
                    if (removedToKeptDictionary.TryGetValue(fromVertex, out var newFromVertex))
                        fromVertex = newFromVertex;
                    if (removedToKeptDictionary.TryGetValue(toVertex, out var newToVertex))
                        toVertex = newToVertex;
                    SingleSidedEdgeData.Add((face, fromVertex, toVertex));
                }
            }
            return removedToKeptDictionary.Keys;
        }

        /// <summary>
        /// Gets the neighbors pre edge creation. This is a slow check used rarely in the following routine.
        /// Note that is also alters the FacePairsForEdges and InconsistentMatingFacePairs lists.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>A list of TriangleFaces.</returns>
        IEnumerable<TriangleFace> GetNeighborsPreEdgeCreation(TriangleFace face)
        {
            for (int i = FacePairsForEdges.Count - 1; i >= 0; i--)
            {
                if (FacePairsForEdges[i].Item1 != face && FacePairsForEdges[i].Item2 != face)
                    continue;
                if (FacePairsForEdges[i].Item1 != face)
                    yield return FacePairsForEdges[i].Item1;
                if (FacePairsForEdges[i].Item2 != face)
                    yield return FacePairsForEdges[i].Item2;
                FacePairsForEdges.RemoveAt(i);
            }
            for (int i = InconsistentMatingFacePairs.Count - 1; i >= 0; i--)
            {
                if (InconsistentMatingFacePairs[i].Item1 != face && InconsistentMatingFacePairs[i].Item2 != face)
                    continue;
                if (InconsistentMatingFacePairs[i].Item1 != face)
                    yield return InconsistentMatingFacePairs[i].Item1;
                if (InconsistentMatingFacePairs[i].Item2 != face)
                    yield return InconsistentMatingFacePairs[i].Item2;
                InconsistentMatingFacePairs.RemoveAt(i);
            }
        }

        /// <summary>
        /// Makes and merges entries in the two dictionaries. the two provided vertices are to be
        /// merged, but which to keep? This depends on whether or not a previous decision to remove
        /// or keep them has been made. This makes this function quite complex.
        /// </summary>
        /// <param name="keptToRemoveDictionary">The kept to remove dictionary.</param>
        /// <param name="removedToKeptDictionary">The removed to kept dictionary.</param>
        /// <param name="vA">The v a.</param>
        /// <param name="vB">The v b.</param>
        private static void MergeMakeEntries(Dictionary<Vertex, HashSet<Vertex>> keptToRemoveDictionary,
            Dictionary<Vertex, Vertex> removedToKeptDictionary, Vertex vA, Vertex vB)
        {
            var vAIsAlreadyKept = keptToRemoveDictionary.TryGetValue(vA, out var reassignedToVa);
            var vBIsAlreadyKept = keptToRemoveDictionary.TryGetValue(vB, out var reassignedToVb);
            var vAIsAlreadyRemoved = removedToKeptDictionary.TryGetValue(vA, out var whatVaIsReassignedTo);
            var vBIsAlreadyRemoved = removedToKeptDictionary.TryGetValue(vB, out var whatVbIsReassignedTo);
            if (vBIsAlreadyKept)
            {
                if (vAIsAlreadyKept)
                {
                    foreach (var v in reassignedToVa)
                    {
                        removedToKeptDictionary[v] = vB;
                        reassignedToVb.Add(v);
                    }
                    keptToRemoveDictionary.Remove(vA);
                }
                else if (vAIsAlreadyRemoved)
                {
                    if (whatVaIsReassignedTo == vB) return;
                    reassignedToVb.Add(whatVaIsReassignedTo); // wherever vA was targetted, that v needs to be sent to vB 
                    var removedForOldVaTarget = keptToRemoveDictionary[whatVaIsReassignedTo];
                    // the old target of vA now needs to be targetted to vB, so we need to add all the removed
                    foreach (var v in removedForOldVaTarget)
                        reassignedToVb.Add(v);
                    keptToRemoveDictionary.Remove(whatVaIsReassignedTo);
                }
                removedToKeptDictionary[vA] = vB; //re-assign or add where vA is reassigned to vB
                reassignedToVb.Add(vA);
            }
            else if (vAIsAlreadyKept) //implies that vBis NOT already kept, so need need to check that case as above
            {
                if (vBIsAlreadyRemoved)
                {
                    if (vA == whatVbIsReassignedTo) return;
                    reassignedToVa.Add(whatVbIsReassignedTo); // wherever vB was targetted, that v needs to be sent to vA 
                    var removedForOldVbTarget = keptToRemoveDictionary[whatVbIsReassignedTo];
                    // the old target of vA now needs to be targetted to vB, so we need to add all the removed
                    foreach (var v in removedForOldVbTarget)
                        reassignedToVa.Add(v);
                    keptToRemoveDictionary.Remove(whatVbIsReassignedTo);
                }
                removedToKeptDictionary[vB] = vA; //re-assign or add where vA is reassigned to vB
                reassignedToVa.Add(vB);
            }
            else if (vAIsAlreadyRemoved) // neither have been kept at this point
            {
                // redefine vA as the target of vA and follow the procedure above where vA is already kept
                vA = whatVaIsReassignedTo;
                reassignedToVa = keptToRemoveDictionary[vA];
                if (vBIsAlreadyRemoved)
                {
                    if (vA == whatVbIsReassignedTo) return;
                    reassignedToVa.Add(whatVbIsReassignedTo); // wherever vB was targetted, that v needs to be sent to vA 
                    var removedForOldVbTarget = keptToRemoveDictionary[whatVbIsReassignedTo];
                    keptToRemoveDictionary.Remove(whatVbIsReassignedTo);
                    removedToKeptDictionary[whatVbIsReassignedTo] = vA;
                    // the old target of vA now needs to be targetted to vB, so we need to add all the removed
                    foreach (var v in removedForOldVbTarget)
                    {
                        removedToKeptDictionary[v] = vA;
                        reassignedToVa.Add(v); // this should include vB
                    }
                }
                else
                {
                    removedToKeptDictionary[vB] = vA; //re-assign or add where vA is reassigned to vB
                    reassignedToVa.Add(vB);
                }
            }
            else if (vBIsAlreadyRemoved) // implies that A is neither kept or removed yet
            {
                removedToKeptDictionary[vA] = whatVbIsReassignedTo;
                keptToRemoveDictionary[whatVbIsReassignedTo].Add(vA);
            }
            else //phew! nothing is kept or removed yet
            {
                keptToRemoveDictionary[vA] = new HashSet<Vertex> { vB };
                removedToKeptDictionary[vB] = vA;
            }

            //if (removedToKeptDictionary.Keys.Intersect(keptToRemoveDictionary.Keys).Any())
            //    throw new Exception("should not have any keys in common");
        }


        #endregion Repair Functions
        /// <summary>
        /// Finds all non smooth edges in the tessellated solid.
        /// non-smooth edges are defined as those that have c0 continuity of course
        /// (locally water-tight), but do not have C1 continuity
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="primitives">The primitives.</param>
        void FindNonSmoothEdges(double chordError = double.NaN)
        {
            ts.NonsmoothEdges = new List<Edge>();
            if (double.IsNaN(chordError))
            {
                var diagonal = (ts.Bounds[1] - ts.Bounds[0]).Length();
                chordError = 0.03 * diagonal;
            }
            // lastIndex don't like the 2.5 here either, but it is the result of some testing
            foreach (var e in ts.Edges)
            {
                e.Curvature = CurvatureType.SaddleOrFlat;
                var angleFromFlat = e.InternalAngle - Math.PI;
                if (angleFromFlat.IsNegligible(Constants.DefaultSameAngleRadians)) continue;
                if (Math.Abs(angleFromFlat) >= Constants.MinSmoothAngle || e.IsDiscontinuous(chordError))
                {
                    if (angleFromFlat < 0) e.Curvature = CurvatureType.Convex;
                    else e.Curvature = CurvatureType.Concave;
                    ts.NonsmoothEdges.Add(e);
                }
            }
        }


        public static IEnumerable<HashSet<TriangleFace>> GetFacePatchesBetweenBorderEdges(HashSet<Edge> borderEdges,
            IEnumerable<TriangleFace> faces, HashSet<TriangleFace> usedFaces)
        {
            var remainingFaces = new HashSet<TriangleFace>(faces);
            //Pick a start edge, then collect all adjacent faces on one side of the face, without crossing over significant edges.
            //This collection of faces will be used to create a patch.
            while (remainingFaces.Any())
            {
                var startFace = remainingFaces.First();
                remainingFaces.Remove(startFace);
                if (usedFaces.Contains(startFace)) continue; // this is redundant with the below check for the same, but
                                                             // check here saves a split second and the creation of the next two collections.
                var patch = new HashSet<TriangleFace>();
                var stack = new Stack<TriangleFace>();
                stack.Push(startFace);
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (usedFaces.Contains(face)) continue;
                    usedFaces.Add(face);
                    patch.Add(face);
                    foreach (var edge in face.Edges)
                    {
                        if (borderEdges.Contains(edge)) continue;//Don't cross over significant edges
                        var otherFace = face == edge.OwnedFace ? edge.OtherFace : edge.OwnedFace;
                        if (remainingFaces.Contains(otherFace))
                        {
                            stack.Push(otherFace);
                            remainingFaces.Remove(otherFace);
                        }
                    }
                }
                yield return patch;
            }
        }



        /// <summary>
        /// Repairs the holes.
        /// </summary>
        private void RepairHoles()
        {
            var edgePaths = SingleSidedEdges.MakeEdgePaths(true, new GetEdgePathLoopsAroundNullBorder()).ToList();
            var loops = SeparateOutClosedLoops(edgePaths);
            CreateMissingEdgesAndFaces(loops, out var newEdges, out var newFaces);
            ts.AddFaces(newFaces);
            ts.AddEdges(newEdges);
        }

        private IEnumerable<TriangulationLoop> SeparateOutClosedLoops(List<EdgePath> edgePaths)
        {
            SingleSidedEdges = new List<Edge>();
            foreach (var edgePath in edgePaths)
            {
                if (edgePath.IsClosed) yield return new TriangulationLoop(edgePath);
                else SingleSidedEdges.AddRange(edgePath.EdgeList);
            }
        }

        private static void CreateMissingEdgesAndFaces(IEnumerable<TriangulationLoop> loops,
                    out List<Edge> newEdges, out List<TriangleFace> newFaces)
        {
            newEdges = new List<Edge>();
            newFaces = new List<TriangleFace>();
            var k = 0;
            foreach (var loop in loops)
            {
                Message.output("Patching hole #" + ++k + " (has " + loop.Count + " edges) in tessellation.", 2);
                //if a simple triangle, create a new face from vertices
                if (loop.Count == 3)
                {
                    var newFace = new TriangleFace(loop.GetVertices());
                    for (int i = 0; i < 3; i++)
                    {
                        var edge = loop.EdgeList[i];
                        edge.OtherFace = newFace;
                        newFace.AddEdge(edge);
                    }
                    newFaces.Add(newFace);
                    continue;
                }
                //Else, use the triangulate function
                var edgeDic = loop.EdgeList.ToDictionary(e => Edge.SetAndGetEdgeChecksum(e), e => e);
                var vertices = loop.GetVertices().ToList();
                Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
                var plane = new Plane(distance, planeNormal);
                var success = false;
                List<TriangulationLoop> triangleFaceList1 = null;
                List<Vertex[]> triangleFaceList2 = null;
                var closeToPlane = plane.CalculateMaxError(vertices.Select(v => v.Coordinates)) < Constants.BaseTolerance;
                if (closeToPlane)
                {
                    var coords2D = vertices.Select(v => v.Coordinates).ProjectTo2DCoordinates(planeNormal, out _);
                    if (coords2D.Area() < 0) planeNormal *= -1;
                }

                var tasks = new List<Task>();
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        triangleFaceList1 = Single3DPolygonTriangulation.QuickTriangulate(loop, 5).ToList();
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }));
                if (closeToPlane)
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            triangleFaceList2 = vertices.Triangulate(planeNormal, true).ToList();
                            success = true;
                        }
                        catch
                        {
                            success = false;
                        }
                    }));
                tasks.Add(Task.Run(() => Thread.Sleep(1000)));


                int winningTask = Task.WaitAny(tasks.ToArray());
                if (winningTask == 0)
                {
                    Message.output("loop successfully repaired with 3D QuickTriangulate " + triangleFaceList1.Count, 4);
                    foreach (var triangle in triangleFaceList1)
                    {
                        var newFace = new TriangleFace(triangle.GetVertices(), -triangle.Normal);
                        newFaces.Add(newFace);
                        foreach (var edgeAnddir in triangle)
                        {
                            var edge = edgeAnddir.edge;
                            var checksum = Edge.GetEdgeChecksum(edge.From, edge.To);
                            if (edgeDic.TryGetValue(checksum, out edge))
                            {
                                edgeDic.Remove(checksum);
                                edge.OtherFace = newFace;
                            }
                            else
                            {
                                edge = edgeAnddir.edge;
                                edge.OwnedFace = newFace;
                                edge.EdgeReference = checksum;
                                edgeDic.Add(checksum, edge);
                                newEdges.Add(edge);
                            }
                            newFace.AddEdge(edge);
                        }
                    }
                }
                else if (winningTask == 1)
                {
                    Message.output("loop successfully repaired with SweepLine 2D Triangulate" + triangleFaceList2.Count, 4);
                    foreach (var triangle in triangleFaceList2)
                    {
                        var newFace = new TriangleFace(triangle, planeNormal);
                        newFaces.Add(newFace);
                        var fromVertex = newFace.C;
                        foreach (var toVertex in newFace.Vertices)
                        {
                            var checksum = Edge.GetEdgeChecksum(fromVertex, toVertex);
                            if (edgeDic.TryGetValue(checksum, out var edge))
                            {
                                edgeDic.Remove(checksum);
                                //Finish creating edge.
                                edge.OtherFace = newFace;
                                newFace.AddEdge(edge);
                            }
                            else
                            {
                                var newEdge = new Edge(fromVertex, toVertex, newFace, null, true, checksum);
                                newFace.AddEdge(newEdge);
                                edgeDic.Add(checksum, newEdge);
                                newEdges.Add(newEdge);
                            }
                            fromVertex = toVertex;
                        }
                    }
                }
            }
        }


        #region Border Building Functions
        /// <summary>
        /// Define Borders given that the primitives and border segments have already been defined.
        /// </summary>
        /// <param name="solid"></param>
        /// <exception cref="Exception"></exception>
        public static void DefineBorders(TessellatedSolid solid)
        {
            DefineBorderSegments(solid);
            foreach (var prim in solid.Primitives)
                DefineBorders(prim);
        }


        /// <summary>
        /// Creates a List of PrimitiveBorders from a collection of border segments
        /// </summary>
        /// <param name="borderSegments"></param>
        /// <param name="borders"></param>
        public static List<BorderLoop> DefineBorders(PrimitiveSurface primitive)
        {
            primitive.Borders = DefineBorders(primitive.BorderSegments, primitive).ToList();
            return primitive.Borders;
        }
        public static IEnumerable<BorderLoop> DefineBorders(List<BorderSegment> inputBorderSegments, PrimitiveSurface primitive)
        {
            var borderSegments = new List<BorderSegment>();
            var dirs = new List<bool>();
            var startVertices = new List<Vertex>();
            var endVertices = new List<Vertex>();
            foreach (var segment in inputBorderSegments)
            {
                var feature = segment.OwnedPrimitive.BelongsToFeature;
                var dir = segment.OwnedPrimitive == primitive || (feature != null && feature == primitive);
                if (segment.IsClosed || segment.FirstVertex == segment.LastVertex)
                {
                    var border = new BorderLoop { OwnedPrimitive = primitive };
                    border.Add(segment, true, dir);
                    border.UpdateIsClosed();
                    yield return border;
                }
                else if (dir)
                {
                    borderSegments.Add(segment);
                    startVertices.Add(segment.FirstVertex);
                    endVertices.Add(segment.LastVertex);
                    dirs.Add(true);
                }
                else
                {
                    borderSegments.Add(segment);
                    startVertices.Add(segment.LastVertex);
                    endVertices.Add(segment.FirstVertex);
                    dirs.Add(false);
                }
            }
            var lastIndex = borderSegments.Count - 1;
            while (lastIndex >= 0)
            {
                var start = startVertices[lastIndex];
                startVertices.RemoveAt(lastIndex);
                var end = endVertices[lastIndex];
                endVertices.RemoveAt(lastIndex);
                var dirI = dirs[lastIndex];
                dirs.RemoveAt(lastIndex);
                var border = new BorderLoop { OwnedPrimitive = primitive };
                border.Add(borderSegments[lastIndex], true, dirI);
                borderSegments.RemoveAt(lastIndex);
                var loopUpdated = true;
                while (loopUpdated && start != end)
                {
                    loopUpdated = false;
                    for (int j = borderSegments.Count - 1; j >= 0; j--)
                    {
                        if (end == startVertices[j])
                        {
                            border.Add(borderSegments[j], true, dirs[j]);
                            end = border.LastVertex;
                        }
                        else if (start == endVertices[j])
                        {
                            border.Add(borderSegments[j], false, dirs[j]);
                            start = border.FirstVertex;
                        }
                        else continue;
                        loopUpdated = true;
                        borderSegments.RemoveAt(j);
                        startVertices.RemoveAt(j);
                        endVertices.RemoveAt(j);
                        dirs.RemoveAt(j);
                    }
                }
                border.UpdateIsClosed();
                yield return border;
                lastIndex = borderSegments.Count - 1;
            }
        }

        private static void DefineBorderSegments(TessellatedSolid solid)
        {
            foreach (var prim in solid.Primitives)
                prim.BorderSegments = new List<BorderSegment>();
            var borderSegments = GatherEdgesIntoSegments(solid.Primitives.SelectMany(prim => prim.OuterEdges));
            foreach (var segment in borderSegments)
            {
                var ownedFace = segment.DirectionList[0] ? segment.EdgeList[0].OwnedFace : segment.EdgeList[0].OtherFace;
                var otherFace = segment.DirectionList[0] ? segment.EdgeList[0].OtherFace : segment.EdgeList[0].OwnedFace;
                var ownedPrimitive = solid.Primitives.FirstOrDefault(p => p.Faces.Contains(ownedFace));
                var otherPrimitive = solid.Primitives.FirstOrDefault(p => p.Faces.Contains(otherFace));
                if (ownedPrimitive == otherPrimitive)
                    continue;
                segment.OwnedPrimitive = ownedPrimitive;
                segment.OtherPrimitive = otherPrimitive;
                ownedPrimitive.BorderSegments.Add(segment);
                otherPrimitive.BorderSegments.Add(segment);
            }
        }
        public static void RedefineBorderSegments(TessellatedSolid solid, PrimitiveSurface primitive)
        {
            var outerEdgeHash = new HashSet<Edge>(primitive.OuterEdges);
            primitive.BorderSegments = new List<BorderSegment>();
            foreach (var segment in solid.Primitives.SelectMany(p => p.BorderSegments))
                if (outerEdgeHash.Contains(segment.EdgeList[0]))
                {
                    primitive.BorderSegments.Add(segment);
                    foreach (var edge in segment.EdgeList)
                        outerEdgeHash.Remove(edge);
                }
            primitive.BorderSegments.AddRange(GatherEdgesIntoSegments(outerEdgeHash));
        }

        private static IEnumerable<BorderSegment> GatherEdgesIntoSegments(IEnumerable<Edge> edges)
        {
            var deadEnds = new Dictionary<Vertex, Edge>();
            var connectingVertices = new Dictionary<Vertex, (Edge, Edge)>();
            var intesections = new Dictionary<Vertex, List<Edge>>();
            var distinctEdges = new HashSet<Edge>();
            var distinctBorders = new HashSet<BorderSegment>();
            foreach (var edge in edges)
            {
                if (distinctEdges.Add(edge))
                {
                    AddEdgeToDictionaries(deadEnds, connectingVertices, intesections, edge, edge.From);
                    AddEdgeToDictionaries(deadEnds, connectingVertices, intesections, edge, edge.To);
                }
            }
            // at this point, each edge should be in 2 dictionaries
            var edgeToSegments = new Dictionary<Edge, BorderSegment>();
            foreach (var entry in connectingVertices)
            {
                var vertex = entry.Key;
                var edgePair = entry.Value;
                var edge1 = edgePair.Item1;
                var edge2 = edgePair.Item2;
                var edge1Found = edgeToSegments.TryGetValue(edge1, out var segment1);
                var edge2Found = edgeToSegments.TryGetValue(edge2, out var segment2);
                if (edge1Found && edge2Found)
                {
                    if (segment1 == segment2)
                    {
                        if (distinctBorders.Add(segment1))
                        {
                            segment1.UpdateIsClosed();
                            yield return segment1;
                        }
                    }
                    else
                    {
                        segment1.AddRange(segment2);
                        foreach (var (edge, dir) in segment2)
                            edgeToSegments[edge] = segment1;
                    }
                }
                else if (edge1Found)
                { // edge2 is new
                    if (segment1.LastVertex == vertex) segment1.AddEnd(edge2);
                    else if (segment1.FirstVertex == vertex) segment1.AddBegin(edge2);
                    else throw new Exception("This should never happen");
                    edgeToSegments.Add(edge2, segment1);
                }
                else if (edge2Found)
                { // edge1 is new
                    if (segment2.LastVertex == vertex) segment2.AddEnd(edge1);
                    else if (segment2.FirstVertex == vertex) segment2.AddBegin(edge1);
                    else throw new Exception("This should never happen");
                    edgeToSegments.Add(edge1, segment2);
                }
                else
                {
                    var newSegment = new BorderSegment();
                    newSegment.AddEnd(edge1);
                    if (newSegment.LastVertex == vertex) newSegment.AddEnd(edge2);
                    else if (newSegment.FirstVertex == vertex) newSegment.AddBegin(edge2);
                    edgeToSegments.Add(edge1, newSegment);
                    edgeToSegments.Add(edge2, newSegment);
                }
            }
            foreach (var edge in deadEnds.Values.Concat(intesections.Values.SelectMany(v => v)))
            {
                if (!edgeToSegments.ContainsKey(edge))
                {
                    var newSegment = new BorderSegment();
                    newSegment.AddEnd(edge);
                    edgeToSegments.Add(edge, newSegment);
                }
            }
            foreach (var entry in edgeToSegments)
            {
                var segment = entry.Value;
                if (distinctBorders.Add(segment))
                {
                    segment.UpdateIsClosed();
                    yield return segment;
                }
            }
        }

        private static void AddEdgeToDictionaries(Dictionary<Vertex, Edge> deadEnds, Dictionary<Vertex, (Edge, Edge)> connectingVertices, Dictionary<Vertex, List<Edge>> intesections, Edge edge, Vertex v)
        {
            if (intesections.TryGetValue(v, out var edgeCollection))
                edgeCollection.Add(edge);
            else if (connectingVertices.TryGetValue(v, out var edgePair))
            {
                connectingVertices.Remove(v);
                intesections.Add(v, new List<Edge> { edgePair.Item1, edgePair.Item2, edge });
            }
            else if (deadEnds.TryGetValue(v, out var matingEdge))
            {
                deadEnds.Remove(v);
                connectingVertices.Add(v, (matingEdge, edge));
            }
            else deadEnds.Add(v, edge);
        }

        #endregion
    }
}
