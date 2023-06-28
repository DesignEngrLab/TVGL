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
        public List<List<TriangleFace>> OverusedEdges { get; internal set; }

        /// <summary>
        /// Edges that only have one face1
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
            if (buildOptions.FixEdgeDisassociations && OverusedEdges.Count + SingledSidedEdges.Count > 0)
            {
                if (!buildOptions.PredefineAllEdges)
                    throw new ArgumentException("Fixing Edge Disassociations requires PredefineAllEdges to be true.");
                try
                {
                    this.RepairHoles();
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
            if (buildOptions.AutomaticallyRepairBadFaces && buildOptions.PredefineAllEdges)
            {
                try
                {
                    if (!PropagateFixToNegligibleFaces())
                        throw new Exception("Unable to propagate out negligible faces.");
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
                try
                {
                    this.RepairHoles();
                }
                catch
                {
                    //Continue
                }
            }
            if (buildOptions.AutomaticallyInvertNegativeSolids && ModelHasNegativeVolume)
                ts.TurnModelInsideOut();

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

        private bool PropagateFixToNegligibleFaces()
        {
            foreach (var face in FacesWithNegligibleArea)
                LongestEdge(face).FlipEdge(ts);
            return true;
        }

        private static Edge LongestEdge(TriangleFace face)
        {
            var ABlengthSqd = face.AB.Vector.LengthSquared();
            var BClengthSqd = face.BC.Vector.LengthSquared();
            var CAlengthSqd = face.CA.Vector.LengthSquared();
            if (ABlengthSqd > BClengthSqd)
            {
                if (ABlengthSqd > CAlengthSqd)
                    return face.AB;
                else return face.CA;
            }
            else if (BClengthSqd > CAlengthSqd)
                return face.BC; //since it is already established that BC is not shorter than AB
            else return face.CA; // since bc>=ab and ca>=bc, ca must be the longest edge
        }


        private void RepairHoles()
        {
            var edgeHash = SingledSidedEdges.SelectMany(f => f.Edges.Where(e => e.OtherFace == null));
            var loops = OrganizeIntoLoops(edgeHash, out var remainingEdges1);
            var newElements = CreateMissingEdgesAndFaces(loops, out var newFaces, out var remainingEdges2);
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
                        // yet another (4th, 5th, etc) face1 defines this edge. Better store for now and sort out
                        // later in "TeaseApartOverDefinedEdges" (see next method).
                        overDefFaces.Add(face);
                    else if (alreadyDefinedEdges.TryGetValue(checksum, out var edgeEntry))
                    {
                        // if an alreadyDefinedEdge has another face1 defining it, then it should be
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
                        // in the proper right-hand rule direction. This can be correct for only one face1 and
                        // should be opposite for the other.
                        short indexFlag = WhichFaceOwnsEdge(formerFace, face, fromVertex, toVertex);
                        alreadyDefinedEdges.Add(checksum, (formerFace, face, indexFlag));
                    }
                    else // this edge doesn't already exist, so create and add to partlyDefinedEdge dictionary
                    {
                        partlyDefinedEdgeDictionary.Add(checksum, face);
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
                SingledSidedEdges = partlyDefinedEdgeDictionary.Values.ToList();
                numSingleSidedEdges = SingledSidedEdges.Count;
            }
            foreach (var connection in alreadyDefinedEdges.Values)
            {
                if (connection.Item3 == 0)
                    InconsistentMatingFacePairs.Add((connection.Item1, connection.Item2));
                else if (connection.Item3 == 1)
                    FacePairsForEdges.Add((connection.Item1, connection.Item2));
                else
                    FacePairsForEdges.Add((connection.Item2, connection.Item1));
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

        private static short WhichFaceOwnsEdge(TriangleFace face1, TriangleFace face2, Vertex fromVertex, Vertex toVertex)
        {
            var face1OwnsEdge = (face1.A == fromVertex && face1.B == toVertex) ||
                (face1.B == fromVertex && face1.C == toVertex) ||
                (face1.C == fromVertex && face1.A == toVertex);
            var face2OwnsEdge = (face2.A == fromVertex && face2.B == toVertex) ||
                (face2.B == fromVertex && face2.C == toVertex) ||
                (face2.C == fromVertex && face2.A == toVertex);
            var indexFlag = face1OwnsEdge == face2OwnsEdge ? (short)0 :
                face1OwnsEdge ? (short)1 : (short)2;
            return indexFlag;
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
            if (tsErrors.SingledSidedEdges.Count > 0) Message.output("==> " + tsErrors.SingledSidedEdges.Count + " single-sided edges.", 3);
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
            var edges = new List<Edge>(FacePairsForEdges.Count + SingledSidedEdges.Count);
            foreach (var facePair in FacePairsForEdges)
            {
                var ownedFace = facePair.Item1;
                var otherFace = facePair.Item2;
                if (ownedFace.A != otherFace.A && ownedFace.A != otherFace.B && ownedFace.A != otherFace.C)
                    edges.Add(new Edge(ownedFace.B, ownedFace.C, ownedFace, otherFace, true));
                else if (ownedFace.B != otherFace.A && ownedFace.B != otherFace.B && ownedFace.B != otherFace.C)
                    edges.Add(new Edge(ownedFace.C, ownedFace.A, ownedFace, otherFace, true));
                else //if (ownedFace.C != otherFace.A && ownedFace.C != otherFace.B && ownedFace.C != otherFace.C)
                    edges.Add(new Edge(ownedFace.A, ownedFace.B, ownedFace, otherFace, true));
            }
            foreach (var faceWithSingle in SingledSidedEdges)
            {
                if (faceWithSingle.AB == null)
                    edges.Add(new Edge(faceWithSingle.A, faceWithSingle.B, faceWithSingle, null, true));
                if (faceWithSingle.BC == null)
                    edges.Add(new Edge(faceWithSingle.B, faceWithSingle.C, faceWithSingle, null, true));
                if (faceWithSingle.CA == null)
                    edges.Add(new Edge(faceWithSingle.C, faceWithSingle.A, faceWithSingle, null, true));
            }
            ts.Edges = new Edge[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                edges[i].IndexInList = i;
                ts.Edges[i] = edges[i];
            }
        }
        #endregion

        #region Repair Functions
        /// <summary>
        /// Sets the face1 normal for any negligible area faces that have not already been set.
        /// The neighbor's normal (in the next 2 lines) if the original face1 has no area (collapsed to a line).
        /// This happens with T-Edges. We want to give the face1 the normal of the two smaller edges' other faces,
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
                    var otherFace = oldEdge.OwnedFace == face ? oldEdge.OtherFace : oldEdge.OwnedFace;
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
                        var checksum = Edge.GetEdgeChecksum(eAD.edge.From, eAD.edge.To);
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