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
            OverusedEdges = new List<List<TriangleFace>>();
            SingledSidedEdges = new List<TriangleFace>();
            FacesWithNegligibleArea = new List<TriangleFace>();
            FacePairsForEdges = new List<(TriangleFace, TriangleFace)>();
            MatingInconsistentFacePairs = new List<(TriangleFace, TriangleFace)>();
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
        public List<List<TriangleFace>> OverusedEdges { get; internal set; }

        /// <summary>
        /// Edges that only have one face
        /// </summary>
        /// <value>The singled sided edges.</value>
        public List<TriangleFace> SingledSidedEdges { get; internal set; }

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
        public List<(TriangleFace, TriangleFace)> MatingInconsistentFacePairs { get; internal set; }


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
        #endregion Properties


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
            if (buildOptions.PredefineAllEdges)
                    MakeEdges();
            if (buildOptions.AutomaticallyRepairBadFaces && MatingInconsistentFacePairs.Any())
                try
                {
                    FlipFacesBasedOnBadAngles();
                }
                catch
                {
                    //Continue
                }
            if (buildOptions.AutomaticallyRepairHoles)
                try
                {
                    this.RepairHoles();
                }
                catch
                {
                    //Continue
                }
            if (buildOptions.AutomaticallyInvertNegativeSolids && ModelHasNegativeVolume)
                ts. TurnModelInsideOut();

            //If the volume is zero, creating the convex hull may cause a null exception
            if (buildOptions.DefineConvexHull && !ts.Volume.IsNegligible())
                ts.BuildConvexHull();
            if (buildOptions.FindNonsmoothEdges && ts.Edges != null)
                try
                {
                    FindNonSmoothEdges();
                }
                catch
                {
                    //Continue
                }
        }

        private void RepairHoles()
        {
            throw new NotImplementedException();
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
            var partlyDefinedEdgeDictionary = new Dictionary<long, TriangleFace>();
            var alreadyDefinedEdges = new Dictionary<long, (TriangleFace, TriangleFace, short)>();
            var overDefinedEdgesDictionary = new Dictionary<long, List<TriangleFace>>();
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
                    var checksum = TessellatedSolid.GetEdgeChecksum(fromVertex, toVertex);
                    // the following four-part condition is best read from the bottom up.
                    // the checksum is used to quickly identify if the edge exists (and to access it)
                    // in one of the 3 dictionaries specified above.
                    if (overDefinedEdgesDictionary.TryGetValue(checksum, out var overDefFaces))
                        // yet another (4th, 5th, etc) face defines this edge. Better store for now and sort out
                        // later in "TeaseApartOverDefinedEdges" (see next method).
                        overDefFaces.Add(face);
                    else if (alreadyDefinedEdges.TryGetValue(checksum, out var edgeEntry))
                    {
                        // if an alreadyDefinedEdge has another face defining it, then it should be
                        // moved to overDefinedEdges
                        overDefinedEdgesDictionary.Add(checksum,
                            new List<TriangleFace> { edgeEntry.Item1, edgeEntry.Item2, face });
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.TryGetValue(checksum, out var formerFace))
                    {
                        // found a match to a partlyDefinedEdge. Great! I hope it doesn't turn out
                        // to be overDefined
                        partlyDefinedEdgeDictionary.Remove(checksum);
                        // one more thing to check while you're here. Who should "own" the resulting
                        // edge. In the TVGL context, the OwnedFace of the edge and the edge oriented (from-to)
                        // in the proper right-hand rule direction. This can be correct for only one face and
                        // should be opposite for the other.
                        var formerFaceOwnsEdge = (formerFace.A == fromVertex && formerFace.B == toVertex) ||
                            (formerFace.B == fromVertex && formerFace.C == toVertex) ||
                            (formerFace.C == fromVertex && formerFace.A == toVertex);
                        var newFaceOwnsEdge = (face.A == fromVertex && face.B == toVertex) ||
                            (face.B == fromVertex && face.C == toVertex) ||
                            (face.C == fromVertex && face.A == toVertex);
                        var indexFlag = formerFaceOwnsEdge && newFaceOwnsEdge ? (short)0 :
                            formerFaceOwnsEdge && newFaceOwnsEdge ? (short)1 : (short)2;
                        alreadyDefinedEdges.Add(checksum, (formerFace, face, indexFlag));
                    }
                    else // this edge doesn't already exist, so create and add to partlyDefinedEdge dictionary
                    {
                        partlyDefinedEdgeDictionary.Add(checksum, face);
                    }
                    fromVertex = toVertex;
                }
            }
            if (overDefinedEdgesDictionary.Count > 0)
            {
                ContainsErrors = true;
                OverusedEdges = overDefinedEdgesDictionary.Values.ToList();
            }
            if (partlyDefinedEdgeDictionary.Count > 0)
            {
                ContainsErrors = true;
                SingledSidedEdges = partlyDefinedEdgeDictionary.Values.ToList();
            }
            foreach (var connection in alreadyDefinedEdges.Values)
            {
                if (connection.Item3 == 0)
                    MatingInconsistentFacePairs.Add((connection.Item1, connection.Item2));
                else if (connection.Item3 == 1)
                    FacePairsForEdges.Add((connection.Item1, connection.Item2));
                else
                    FacePairsForEdges.Add((connection.Item2, connection.Item1));
            }
            if (MatingInconsistentFacePairs.Count > 0)
                ContainsErrors = true;

            if (ContainsErrors)
                ReportErrors(ts.Errors, alreadyDefinedEdges.Count, ts.NumberOfFaces);
            else
                Message.output("No errors found.", 3);
        }

        private static void ReportErrors(TessellationBuildAndRepair tsErrors, int numCorrectEdges, int numFaces)
        {
            Message.output("Errors found in model:", 3);
            Message.output("======================", 3);
            if (tsErrors.ModelHasNegativeVolume)
                Message.output("==> The model has negative area. The normals of the faces are pointed inward, or this is only a concave surface - not a watertiht solid.", 3);
            if (tsErrors.FacesWithNegligibleArea != null)
                Message.output("==> " + tsErrors.FacesWithNegligibleArea.Count + " faces with negligible area.", 3);
            if (tsErrors.OverusedEdges != null)
            {
                Message.output("==> " + tsErrors.OverusedEdges.Count + " overused edges.", 3);
                Message.output("    The number of faces per overused edge: " + string.Join(',',
                               tsErrors.OverusedEdges.Select(p => p.Count)), 3);
            }
            if (tsErrors.SingledSidedEdges != null) Message.output("==> " + tsErrors.SingledSidedEdges.Count + " single-sided edges.", 3);
            if (tsErrors.MatingInconsistentFacePairs != null) Message.output("==> " + tsErrors.MatingInconsistentFacePairs.Count
                + " edges with opposite-facing faces.", 3);

            var numOverUsedFaceEdges = tsErrors.OverusedEdges?.Sum(p => p.Count) ?? 0;
            var numOverUsedFaceEdgesMinusPairs = 0;
            if (numOverUsedFaceEdges > 0)
                numOverUsedFaceEdgesMinusPairs = tsErrors.OverusedEdges.Sum(p => p.Count == 2 ? 0 : p.Count);
            var numSingleSidedEdges = tsErrors.SingledSidedEdges?.Count ?? 0;
            numCorrectEdges += (numOverUsedFaceEdges + numSingleSidedEdges) / 2;
            if (3 * numFaces == 2 * numCorrectEdges)
            {
                Message.output("==> While re-connecting faces and edges has lead to errors, there is a likelihood that water-tightness can be acheived."
                    , 3);
                if (numOverUsedFaceEdgesMinusPairs < numSingleSidedEdges)
                {
                    Message.output("      Tolerance should be made smaller.");
                    tsErrors.ChangeTolerance = -1;
                }
                else
                {
                    Message.output("      Tolerance should be made larger.");
                    tsErrors.ChangeTolerance = 1;
                }
            }
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

            ts.Edges = new Edge[FacePairsForEdges.Count];

            for (int i = 0; i < FacePairsForEdges.Count; i++)
            {
                (TriangleFace, TriangleFace) pair = FacePairsForEdges[i];
                var ownedFace = pair.Item1;
                var otherFace = pair.Item2;
                if (ownedFace.A != otherFace.A && ownedFace.A != otherFace.B && ownedFace.A != otherFace.C)
                    ts.Edges[i] = new Edge(ownedFace.B, ownedFace.C, ownedFace, otherFace, true);
                else if (ownedFace.B != otherFace.A && ownedFace.B != otherFace.B && ownedFace.B != otherFace.C)
                    ts.Edges[i] = new Edge(ownedFace.C, ownedFace.A, ownedFace, otherFace, true);
                else //if (ownedFace.C != otherFace.A && ownedFace.C != otherFace.B && ownedFace.C != otherFace.C)
                    ts.Edges[i] = new Edge(ownedFace.A, ownedFace.B, ownedFace, otherFace, true);
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
        bool SetNegligibleAreaFaceNormals(TessellatedSolid ts, bool checkAllFaces = false)
        {
            if (!checkAllFaces && (ts.Errors == null || FacesWithNegligibleArea == null)) return true;
            var success = false;
            var j = 0;
            var facesToCheck = checkAllFaces ? ts.Faces : FacesWithNegligibleArea.ToArray();
            while (!success && j < 10)
            {
                j++;
                success = true;
                foreach (var face in facesToCheck)
                    if (face.Normal.IsNull())
                        if (!face.AdoptNeighborsNormal())
                            success = false;
            }
            return success;
        }

        /// <summary>
        /// Flips the faces based on bad angles.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool FlipFacesBasedOnBadAngles()
        {
            var facesToConsider = new HashSet<TriangleFace>();
            foreach (var facePair in MatingInconsistentFacePairs)
            {
                facesToConsider.Add(facePair.Item1);
                facesToConsider.Add(facePair.Item2);
            };
            var allEdgesToUpdate = new HashSet<Edge>();
            foreach (var face in facesToConsider)
            {
                if (face == null) continue;
                var edgesToUpdate = new List<Edge>();
                foreach (var edge in face.Edges)
                {
                    if (edge == null) continue; //that's enough to know something is not right!
                    if (MatingInconsistentFacePairs.Contains(edge)) edgesToUpdate.Add(edge);
                    else if (facesToConsider.Contains(edge.OwnedFace == face ? edge.OtherFace : edge.OwnedFace))
                        edgesToUpdate.Add(edge);
                    else break;
                }
                if (edgesToUpdate.Count < 3) continue;
                face.Invert();
                foreach (var edge in edgesToUpdate)
                    if (!allEdgesToUpdate.Contains(edge)) allEdgesToUpdate.Add(edge);
            }
            foreach (var edge in allEdgesToUpdate)
            {
                edge.Update();
                MatingInconsistentFacePairs.Remove(edge);
            }
            if (MatingInconsistentFacePairs.Any()) return false;
            MatingInconsistentFacePairs = null;
            return true;
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

        private static List<TriangleFace> CloseHole(PrimitiveBorder border, Vector3 normalGuess)
        {
            var green = new Color(KnownColors.Green);
            var newFaces = new List<TriangleFace>();
            //Global.Plotter.ShowAndHang(border.AsPolygon);
            var vertexLoop = border.GetVertices().ToList();
            var surfaceNormal = border.OwnedPrimitive.GetAxis();
            if (border.PlaneError < Constants.ErrorForFaceInSurface)
            {
                foreach (var triangle in vertexLoop.Triangulate(surfaceNormal, true))
                    newFaces.Add(new TriangleFace(triangle, normalGuess, false) { Color = green });
            }
            else
            {
                foreach (var triangle in Single3DPolygonTriangulation.QuickTriangulate(border, 333))
                    newFaces.Add(new TriangleFace(triangle.vertices, normalGuess, false) { Color = green });
            }
            return newFaces;
        }

    }
}