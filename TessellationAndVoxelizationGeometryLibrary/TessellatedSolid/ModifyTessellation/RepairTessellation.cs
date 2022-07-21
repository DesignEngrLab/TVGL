// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    ///  This portion of ModifyTessellation includes the functions to repair a solid. It is
    ///  invoked during the opening of a tessellated solid from "disk", but the repair function
    ///  may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        #region Check Model Integrity

        /// <summary>
        /// Checks the model integrity.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="repairAutomatically">The repair automatically.</param>
        public static bool CheckModelIntegrity(this TessellatedSolid ts, bool repairAutomatically = true)
        {
            Message.output("Model Integrity Check...", 3);
            ts.Errors = new TessellationError { NoErrors = true };

            if (ts.Volume < 0) StoreModelIsInsideOut(ts);
            var edgeFaceRatio = ts.NumberOfEdges / (double)ts.NumberOfFaces;
            if (!edgeFaceRatio.IsPracticallySame(1.5)) StoreEdgeFaceRatio(ts, edgeFaceRatio);
            //Check if each face has cyclic references with each edge, vertex, and adjacent faces.
            foreach (var face in ts.Faces)
            {
                if (face.Vertices.Count == 1) StoreFaceWithOneVertex(ts, face);
                if (face.Vertices.Count == 2) StoreFaceWithTwoVertices(ts, face);
                if (face.Edges.Count == 1 || face.Edges.Count(e => e != null) == 1) StoreFaceWithOneEdge(ts, face);
                if (face.Edges.Count == 2 || face.Edges.Count(e => e != null) == 2) StoreFaceWithTwoEdges(ts, face);
                if (face.Area.IsNegligible(ts.SameTolerance)) StoreFaceWithNegligibleArea(ts, face);
                foreach (var edge in face.Edges)
                {
                    if (edge == null) continue;
                    if (edge.OwnedFace == null || edge.OtherFace == null) StoreSingleSidedEdge(ts, edge);
                    if (edge.OwnedFace != face && edge.OtherFace != face)
                        StoreEdgeDoesNotLinkBackToFace(ts, face, edge);
                }
                foreach (var vertex in face.Vertices.Where(vertex => !vertex.Faces.Contains(face)))
                    StoreVertexDoesNotLinkBackToFace(ts, face, vertex);
            }
            //Check if each edge has cyclic references with each vertex and each face.
            foreach (var edge in ts.Edges)
            {
                if (!edge.OwnedFace.Edges.Contains(edge)) StoreFaceDoesNotLinkBackToEdge(ts, edge, edge.OwnedFace);
                if (edge.OtherFace != null && !edge.OtherFace.Edges.Contains(edge)) StoreFaceDoesNotLinkBackToEdge(ts, edge, edge.OtherFace);
                if (!edge.To.Edges.Contains(edge)) StoreVertDoesNotLinkBackToEdge(ts, edge, edge.To);
                if (!edge.From.Edges.Contains(edge)) StoreVertDoesNotLinkBackToEdge(ts, edge, edge.From);
                if (double.IsNaN(edge.InternalAngle) || edge.InternalAngle < 0 || edge.InternalAngle > Constants.TwoPi)
                    StoreEdgeHasBadAngle(ts, edge);
            }
            //Check if each vertex has cyclic references with each edge and each face.
            foreach (var vertex in ts.Vertices)
            {
                foreach (var edge in vertex.Edges.Where(edge => edge.To != vertex && edge.From != vertex))
                    StoreEdgeDoesNotLinkBackToVertex(ts, vertex, edge);
                foreach (var face in vertex.Faces.Where(face => !face.Vertices.Contains(vertex)))
                    StoreFaceDoesNotLinkBackToVertex(ts, vertex, face);
            }

            //Always perform any "repair" functions for issues that are not considered errors
            SetNegligibleAreaFaceNormals(ts);
            if (ts.Errors.NoErrors)
            {
                Message.output("** Model contains no errors.", 3);
                ts.Errors = null;
                return true;
            }
            if (repairAutomatically)
            {
                Message.output("Some errors found. Attempting to Repair...", 2);
                var success = Repair(ts);
                if (success)
                {
                    ts.Errors = null;
                    Message.output("Repairs functions completed successfully.", 2);
                }
                else Message.output("Repair did not successfully fix all the problems.", 1);
                return CheckModelIntegrity(ts, false);
            }

            #region Report details

            if ((int)Message.Verbosity < 3) return false;
            //Note that negligible faces are not truly errors.
            Message.output("Errors found in model:");
            Message.output("======================");
            if (ts.Errors.ModelIsInsideOut)
                Message.output("==> The model is inside-out! All the normals of the faces are pointed inward.");
            if (!double.IsNaN(ts.Errors.EdgeFaceRatio))
                Message.output("==> Edges / Faces = " + ts.Errors.EdgeFaceRatio + ", but it should be 1.5.");
            if (ts.Errors.OverusedEdges != null)
            {
                Message.output("==> " + ts.Errors.OverusedEdges.Count + " overused edges.");
                Message.output("    The number of faces per overused edge: " + string.Join(',',
                               ts.Errors.OverusedEdges.Select(p => p.Item2.Count)));
            }
            if (ts.Errors.SingledSidedEdges != null) Message.output("==> " + ts.Errors.SingledSidedEdges.Count + " single-sided edges.");
            if (ts.Errors.DegenerateFaces != null) Message.output("==> " + ts.Errors.DegenerateFaces.Count + " degenerate faces in file.");
            if (ts.Errors.DuplicateFaces != null) Message.output("==> " + ts.Errors.DuplicateFaces.Count + " duplicate faces in file.");
            if (ts.Errors.FacesWithOneVertex != null)
                Message.output("==> " + ts.Errors.FacesWithOneVertex.Count + " faces with only one vertex.");
            if (ts.Errors.FacesWithOneEdge != null)
                Message.output("==> " + ts.Errors.FacesWithOneEdge.Count + " faces with only one edge.");
            if (ts.Errors.FacesWithTwoVertices != null)
                Message.output("==> " + ts.Errors.FacesWithTwoVertices.Count + "  faces with only two vertices.");
            if (ts.Errors.FacesWithTwoEdges != null)
                Message.output("==> " + ts.Errors.FacesWithTwoEdges.Count + " faces with only two edges.");
            if (ts.Errors.EdgesWithBadAngle != null) Message.output("==> " + ts.Errors.EdgesWithBadAngle.Count + " edges with bad angles.");
            if (ts.Errors.EdgesThatDoNotLinkBackToFace != null)
                Message.output("==> " + ts.Errors.EdgesThatDoNotLinkBackToFace.Count +
                               " edges that do not link back to faces that link to them.");
            if (ts.Errors.EdgesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + ts.Errors.EdgesThatDoNotLinkBackToVertex.Count +
                               " edges that do not link back to vertices that link to them.");
            if (ts.Errors.VertsThatDoNotLinkBackToFace != null)
                Message.output("==> " + ts.Errors.VertsThatDoNotLinkBackToFace.Count +
                               " vertices that do not link back to faces that link to them.");
            if (ts.Errors.VertsThatDoNotLinkBackToEdge != null)
                Message.output("==> " + ts.Errors.VertsThatDoNotLinkBackToEdge.Count +
                               " vertices that do not link back to edges that link to them.");
            if (ts.Errors.FacesThatDoNotLinkBackToEdge != null)
                Message.output("==> " + ts.Errors.FacesThatDoNotLinkBackToEdge.Count +
                               " faces that do not link back to edges that link to them.");
            if (ts.Errors.FacesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + ts.Errors.FacesThatDoNotLinkBackToVertex.Count +
                               " faces that do not link back to vertices that link to them.");

            #endregion Report details

            return false;
        }

        #endregion Check Model Integrity

        #region Error Storing

        /// <summary>
        /// Stores the model is inside out.
        /// </summary>
        /// <param name="ts">The ts.</param>
        private static void StoreModelIsInsideOut(TessellatedSolid ts)
        {
            ts.Errors.NoErrors = false;
            ts.Errors.ModelIsInsideOut = true;
        }

        /// <summary>
        ///     Stores the edge face ratio.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="edgeFaceRatio">The edge face ratio.</param>
        private static void StoreEdgeFaceRatio(TessellatedSolid ts, double edgeFaceRatio)
        {
            ts.Errors.NoErrors = false;
            ts.Errors.EdgeFaceRatio = edgeFaceRatio;
        }

        /// <summary>
        ///     Stores the face does not link back to vertex.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="vertex">The vertex.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceDoesNotLinkBackToVertex(TessellatedSolid ts, Vertex vertex, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesThatDoNotLinkBackToVertex == null)
                ts.Errors.FacesThatDoNotLinkBackToVertex
                    = new List<(Vertex, PolygonalFace)> { (vertex, face) };
            else ts.Errors.FacesThatDoNotLinkBackToVertex.Add((vertex, face));
        }

        /// <summary>
        ///     Stores the edge does not link back to vertex.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="vertex">The vertex.</param>
        /// <param name="edge">The edge.</param>
        private static void StoreEdgeDoesNotLinkBackToVertex(TessellatedSolid ts, Vertex vertex, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesThatDoNotLinkBackToVertex == null)
                ts.Errors.EdgesThatDoNotLinkBackToVertex
                    = new List<(Vertex, Edge)> { (vertex, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToVertex.Add((vertex, edge));
        }

        /// <summary>
        ///     Stores the edge has bad angle.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="edge">The edge.</param>
        private static void StoreEdgeHasBadAngle(TessellatedSolid ts, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesWithBadAngle == null) ts.Errors.EdgesWithBadAngle = new List<Edge> { edge };
            else if (!ts.Errors.EdgesWithBadAngle.Contains(edge)) ts.Errors.EdgesWithBadAngle.Add(edge);
        }

        /// <summary>
        ///     Stores the vert does not link back to edge.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="edge">The edge.</param>
        /// <param name="vert">The vert.</param>
        private static void StoreVertDoesNotLinkBackToEdge(TessellatedSolid ts, Edge edge, Vertex vert)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.VertsThatDoNotLinkBackToEdge == null)
                ts.Errors.VertsThatDoNotLinkBackToEdge
                    = new List<(Edge, Vertex)> { (edge, vert) };
            else ts.Errors.VertsThatDoNotLinkBackToEdge.Add((edge, vert));
        }

        /// <summary>
        ///     Stores the face does not link back to edge.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="edge">The edge.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceDoesNotLinkBackToEdge(TessellatedSolid ts, Edge edge, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesThatDoNotLinkBackToEdge == null)
                ts.Errors.FacesThatDoNotLinkBackToEdge
                    = new List<(Edge, PolygonalFace)> { (edge, face) };
            else ts.Errors.FacesThatDoNotLinkBackToEdge.Add((edge, face));
        }

        /// <summary>
        ///     Stores the face with negligible area.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceWithNegligibleArea(TessellatedSolid ts, PolygonalFace face)
        {
            //This is not truly an error, to don't change the NoErrors boolean.
            if (ts.Errors.FacesWithNegligibleArea == null)
                ts.Errors.FacesWithNegligibleArea = new List<PolygonalFace> { face };
            else if (!ts.Errors.FacesWithNegligibleArea.Contains(face)) ts.Errors.FacesWithNegligibleArea.Add(face);
        }

        /// <summary>
        ///     Stores the vertex does not link back to face.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        /// <param name="vertex">The vertex.</param>
        private static void StoreVertexDoesNotLinkBackToFace(TessellatedSolid ts, PolygonalFace face, Vertex vertex)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.VertsThatDoNotLinkBackToFace == null)
                ts.Errors.VertsThatDoNotLinkBackToFace
                    = new List<(PolygonalFace, Vertex)> { (face, vertex) };
            else ts.Errors.VertsThatDoNotLinkBackToFace.Add((face, vertex));
        }

        /// <summary>
        ///     Stores the edge does not link back to face.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        /// <param name="edge">The edge.</param>
        private static void StoreEdgeDoesNotLinkBackToFace(TessellatedSolid ts, PolygonalFace face, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesThatDoNotLinkBackToFace == null)
                ts.Errors.EdgesThatDoNotLinkBackToFace
                    = new List<(PolygonalFace, Edge)> { (face, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToFace.Add((face, edge));
        }

        /// <summary>
        ///     Stores the face with o two edges.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceWithTwoEdges(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithTwoEdges == null) ts.Errors.FacesWithTwoEdges = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithTwoEdges.Add(face);
        }

        /// <summary>
        ///     Stores the face with two vertices.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceWithTwoVertices(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithTwoVertices == null) ts.Errors.FacesWithTwoVertices = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithTwoVertices.Add(face);
        }

        /// <summary>
        ///     Stores the face with one edge.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceWithOneEdge(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithOneEdge == null) ts.Errors.FacesWithOneEdge = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithOneEdge.Add(face);
        }

        /// <summary>
        ///     Stores the face with one vertex.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="face">The face.</param>
        private static void StoreFaceWithOneVertex(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithOneVertex == null) ts.Errors.FacesWithOneVertex = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithOneVertex.Add(face);
        }

        /// <summary>
        ///     Stores the overused edges.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="edgeFaceTuples">The edge face tuples.</param>
        internal static void StoreOverusedEdges(TessellatedSolid ts,
            IEnumerable<(Edge, List<PolygonalFace>)> edgeFaceTuples)
        {
            ts.Errors.NoErrors = false;
            ts.Errors.OverusedEdges = edgeFaceTuples.ToList();
        }

        /// <summary>
        ///     Stores the single sided edge.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="singledSidedEdge">The singled sided edge.</param>
        internal static void StoreSingleSidedEdge(TessellatedSolid ts, Edge singledSidedEdge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.SingledSidedEdges == null) ts.Errors.SingledSidedEdges = new List<Edge> { singledSidedEdge };
            else ts.Errors.SingledSidedEdges.Add(singledSidedEdge);
        }

        /// <summary>
        ///     Stores the degenerate face.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="faceVertexIndices">The face vertex indices.</param>
        internal static void StoreDegenerateFace(TessellatedSolid ts, int[] faceVertexIndices)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.DegenerateFaces == null) ts.Errors.DegenerateFaces = new List<int[]> { faceVertexIndices };
            else ts.Errors.DegenerateFaces.Add(faceVertexIndices);
        }

        /// <summary>
        ///     Stores the duplicate face.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="faceVertexIndices">The face vertex indices.</param>
        internal static void StoreDuplicateFace(TessellatedSolid ts, int[] faceVertexIndices)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.DuplicateFaces == null) ts.Errors.DuplicateFaces = new List<int[]> { faceVertexIndices };
            else ts.Errors.DuplicateFaces.Add(faceVertexIndices);
        }

        #endregion Error Storing

        #region Repair Functions

        /// <summary>
        ///     Repairs the specified ts.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Repair(this TessellatedSolid ts)
        {
            if (ts.Errors == null)
            {
                Message.output("No errors to fix!", 4);
                return true;
            }
            Message.output("Some errors found. Attempting to Repair...", 2);
            var completelyRepaired = true;
            if (ts.Errors.ModelIsInsideOut)
                completelyRepaired = ts.TurnModelInsideOut();
            if (ts.Errors.EdgesWithBadAngle != null)
                completelyRepaired = completelyRepaired && FlipFacesBasedOnBadAngles(ts);
            //Note that negligible faces are not truly errors, so they are not repaired
            if (completelyRepaired)
            {
                ts.Errors = null;
                Message.output("Repairs functions completed successfully (errors may still occur).", 2);
            }
            else Message.output("Repair did not successfully fix all the problems.", 1);
            return completelyRepaired;
        }

        /// <summary>
        /// Sets the face normal for any negligible area faces that have not already been set.
        /// The neighbor's normal (in the next 2 lines) if the original face has no area (collapsed to a line).
        /// This happens with T-Edges. We want to give the face the normal of the two smaller edges' other faces,
        /// to preserve a sharp line. Also, if multiple T-Edges are adjacent, recursion may be necessary. 
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static bool SetNegligibleAreaFaceNormals(this TessellatedSolid ts, bool checkAllFaces = false)
        {
            if (!checkAllFaces && (ts.Errors == null || ts.Errors.FacesWithNegligibleArea == null)) return true;
            var success = false;
            var j = 0;
            var facesToCheck = checkAllFaces ? ts.Faces : ts.Errors.FacesWithNegligibleArea.ToArray();
            while (!success && j < 10)
            {
                j++;
                success = true;
                foreach (var face in facesToCheck)
                    if (face.Normal.IsNull())
                        if (!face.AdoptNeighborsNormal())
                            success = false;
            }
            var faceToCheck = ts.Faces.Where(p => p.IndexInList == 97530).FirstOrDefault();
            return success;
        }

        /// <summary>
        ///     Flips the faces based on bad angles.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool FlipFacesBasedOnBadAngles(TessellatedSolid ts)
        {
            var edgesWithBadAngles = new HashSet<Edge>(ts.Errors.EdgesWithBadAngle);
            var facesToConsider = new HashSet<PolygonalFace>(
                edgesWithBadAngles.SelectMany(e => new[] { e.OwnedFace, e.OtherFace }).Distinct());
            var allEdgesToUpdate = new HashSet<Edge>();
            foreach (var face in facesToConsider)
            {
                if (face == null) continue;
                var edgesToUpdate = new List<Edge>();
                foreach (var edge in face.Edges)
                {
                    if (edge == null) continue; //that's enough to know something is not right!
                    if (edgesWithBadAngles.Contains(edge)) edgesToUpdate.Add(edge);
                    else if (facesToConsider.Contains(edge.OwnedFace == face ? edge.OtherFace : edge.OwnedFace))
                        edgesToUpdate.Add(edge);
                    else break;
                }
                if (edgesToUpdate.Count < face.Edges.Count) continue;
                face.Invert();
                foreach (var edge in edgesToUpdate)
                    if (!allEdgesToUpdate.Contains(edge)) allEdgesToUpdate.Add(edge);
            }
            foreach (var edge in allEdgesToUpdate)
            {
                edge.Update();
                ts.Errors.EdgesWithBadAngle.Remove(edge);
            }
            if (ts.Errors.EdgesWithBadAngle.Any()) return false;
            ts.Errors.EdgesWithBadAngle = null;
            return true;
        }

        #endregion Repair Functions
    }
}