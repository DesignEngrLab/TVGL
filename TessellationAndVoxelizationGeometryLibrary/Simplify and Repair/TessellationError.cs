// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="TessellationError.cs" company="Design Engineering Lab">
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
    ///     Stores errors in the tessellated solid
    /// </summary>
    public class TessellationError
    {
        #region Puplic Properties

        /// <summary>
        ///     Edges that are used by more than two faces
        /// </summary>
        /// <value>The overused edges.</value>
        public List<Tuple<Edge, List<PolygonalFace>>> OverusedEdges { get; private set; }

        /// <summary>
        ///     Edges that only have one face
        /// </summary>
        /// <value>The singled sided edges.</value>
        public List<Edge> SingledSidedEdges { get; private set; }

        /// <summary>
        ///     Faces with errors
        /// </summary>
        /// <value>The degenerate faces.</value>
        public List<int[]> DegenerateFaces { get; private set; }

        /// <summary>
        ///     Duplicate Faces
        /// </summary>
        /// <value>The duplicate faces.</value>
        public List<int[]> DuplicateFaces { get; private set; }

        /// <summary>
        ///     Faces with more that three vertices or 3 edges
        /// </summary>
        /// <value>The non triangular faces.</value>
        public List<PolygonalFace> NonTriangularFaces { get; private set; }

        /// <summary>
        ///     Faces with only one vertex
        /// </summary>
        /// <value>The faces with one vertex.</value>
        public List<PolygonalFace> FacesWithOneVertex { get; private set; }

        /// <summary>
        ///     Faces with only one edge
        /// </summary>
        /// <value>The faces with one edge.</value>
        public List<PolygonalFace> FacesWithOneEdge { get; private set; }

        /// <summary>
        ///     Faces with only two vertices
        /// </summary>
        /// <value>The faces with two vertices.</value>
        public List<PolygonalFace> FacesWithTwoVertices { get; private set; }

        /// <summary>
        ///     Faces with only two edges
        /// </summary>
        /// <value>The faces with two edges.</value>
        public List<PolygonalFace> FacesWithTwoEdges { get; private set; }

        /// <summary>
        ///     Faces with negligible area (which is not necessarily an error)
        /// </summary>
        /// <value>The faces with negligible area.</value>
        public List<PolygonalFace> FacesWithNegligibleArea { get; private set; }

        /// <summary>
        ///     Edges that do not link back to faces that link to them
        /// </summary>
        /// <value>The edges that do not link back to face.</value>
        public List<Tuple<PolygonalFace, Edge>> EdgesThatDoNotLinkBackToFace { get; private set; }

        /// <summary>
        ///     Edges that do not link back to vertices that link to them
        /// </summary>
        /// <value>The edges that do not link back to vertex.</value>
        public List<Tuple<Vertex, Edge>> EdgesThatDoNotLinkBackToVertex { get; private set; }

        /// <summary>
        ///     Vertices that do not link back to faces that link to them
        /// </summary>
        /// <value>The verts that do not link back to face.</value>
        public List<Tuple<PolygonalFace, Vertex>> VertsThatDoNotLinkBackToFace { get; private set; }

        /// <summary>
        ///     Vertices that do not link back to edges that link to them
        /// </summary>
        /// <value>The verts that do not link back to edge.</value>
        public List<Tuple<Edge, Vertex>> VertsThatDoNotLinkBackToEdge { get; private set; }

        /// <summary>
        ///     Faces that do not link back to edges that link to them
        /// </summary>
        /// <value>The faces that do not link back to edge.</value>
        public List<Tuple<Edge, PolygonalFace>> FacesThatDoNotLinkBackToEdge { get; private set; }

        /// <summary>
        ///     Faces that do not link back to vertices that link to them
        /// </summary>
        /// <value>The faces that do not link back to vertex.</value>
        public List<Tuple<Vertex, PolygonalFace>> FacesThatDoNotLinkBackToVertex { get; private set; }

        /// <summary>
        ///     Edges with bad angles
        /// </summary>
        /// <value>The edges with bad angle.</value>
        public List<Edge> EdgesWithBadAngle { get; private set; }

        /// <summary>
        ///     Edges to face ratio
        /// </summary>
        /// <value>The edge face ratio.</value>
        public double EdgeFaceRatio { get; private set; } = double.NaN;

        /// <summary>
        ///     Whether ts.Errors contains any errors that need to be resolved
        /// </summary>
        /// <value><c>true</c> if [no errors]; otherwise, <c>false</c>.</value>
        internal bool NoErrors { get; set; }
        /// <summary>
        /// Gets a value indicating whether [model is inside out].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [model is inside out]; otherwise, <c>false</c>.
        /// </value>
        public bool ModelIsInsideOut { get; private set; }


        #endregion

        #region Check Model Integrity

        /// <summary>
        ///     Checks the model integrity.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="repairAutomatically">The repair automatically.</param>
        public static void CheckModelIntegrity(TessellatedSolid ts, bool repairAutomatically = true)
        {
            ts.Errors = new TessellationError();
            ts.Errors.NoErrors = true;
            Message.output("Model Integrity Check...", 3);
            if (ts.Volume < 0) StoreModelIsInsideOut(ts);
            if (ts.MostPolygonSides > 3) StoreHigherThanTriFaces(ts);
            var edgeFaceRatio = ts.NumberOfEdges / (double)ts.NumberOfFaces;
            if (ts.MostPolygonSides == 3 && !edgeFaceRatio.IsPracticallySame(1.5))
                StoreEdgeFaceRatio(ts, edgeFaceRatio);
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
                if (!edge.OtherFace.Edges.Contains(edge)) StoreFaceDoesNotLinkBackToEdge(ts, edge, edge.OtherFace);
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
            if (ts.Errors.NoErrors)
            {
                Message.output("** Model contains no errors.", 3);
                ts.Errors = null;
                return;
            }
            if (repairAutomatically)
            {
                Message.output("Some errors found. Attempting to Repair...", 2);
                var success = ts.Repair();
                if (success)
                {
                    ts.Errors = null;
                    Message.output("Repairs functions completed successfully (errors may still occur).", 2);
                }
                else Message.output("Repair did not successfully fix all the problems.", 1);
                CheckModelIntegrity(ts, false);
                return;
            }
            ts.Errors.Report();
        }

        /// <summary>
        ///     Report out any errors
        /// </summary>
        public void Report()
        {
            if (3 > (int)Message.Verbosity) return;
            //Note that negligible faces are not truly errors.
            Message.output("Errors found in model:");
            Message.output("======================");
            if (ModelIsInsideOut)
                Message.output("==> The model is inside-out! All the normals of the faces are pointed inward.");
            if (NonTriangularFaces != null)
                Message.output("==> " + NonTriangularFaces.Count + " faces are polygons with more than 3 sides.");
            if (!double.IsNaN(EdgeFaceRatio))
                Message.output("==> Edges / Faces = " + EdgeFaceRatio + ", but it should be 1.5.");
            if (OverusedEdges != null)
            {
                Message.output("==> " + OverusedEdges.Count + " overused edges.");
                Message.output("    The number of faces per overused edge: " +
                               OverusedEdges.Select(p => p.Item2.Count).MakePrintString());
            }
            if (SingledSidedEdges != null) Message.output("==> " + SingledSidedEdges.Count + " single-sided edges.");
            if (DegenerateFaces != null) Message.output("==> " + DegenerateFaces.Count + " degenerate faces in file.");
            if (DuplicateFaces != null) Message.output("==> " + DuplicateFaces.Count + " duplicate faces in file.");
            if (FacesWithOneVertex != null)
                Message.output("==> " + FacesWithOneVertex.Count + " faces with only one vertex.");
            if (FacesWithOneEdge != null)
                Message.output("==> " + FacesWithOneEdge.Count + " faces with only one edge.");
            if (FacesWithTwoVertices != null)
                Message.output("==> " + FacesWithTwoVertices.Count + "  faces with only two vertices.");
            if (FacesWithTwoEdges != null)
                Message.output("==> " + FacesWithTwoEdges.Count + " faces with only two edges.");
            if (EdgesWithBadAngle != null) Message.output("==> " + EdgesWithBadAngle.Count + " edges with bad angles.");
            if (EdgesThatDoNotLinkBackToFace != null)
                Message.output("==> " + EdgesThatDoNotLinkBackToFace.Count +
                               " edges that do not link back to faces that link to them.");
            if (EdgesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + EdgesThatDoNotLinkBackToVertex.Count +
                               " edges that do not link back to vertices that link to them.");
            if (VertsThatDoNotLinkBackToFace != null)
                Message.output("==> " + VertsThatDoNotLinkBackToFace.Count +
                               " vertices that do not link back to faces that link to them.");
            if (VertsThatDoNotLinkBackToEdge != null)
                Message.output("==> " + VertsThatDoNotLinkBackToEdge.Count +
                               " vertices that do not link back to edges that link to them.");
            if (FacesThatDoNotLinkBackToEdge != null)
                Message.output("==> " + FacesThatDoNotLinkBackToEdge.Count +
                               " faces that do not link back to edges that link to them.");
            if (FacesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + FacesThatDoNotLinkBackToVertex.Count +
                               " faces that do not link back to vertices that link to them.");
        }

        #endregion

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
        ///     Stores the higher than tri faces.
        /// </summary>
        /// <param name="ts">The ts.</param>
        private static void StoreHigherThanTriFaces(TessellatedSolid ts)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.NonTriangularFaces == null)
                ts.Errors.NonTriangularFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces.Where(face => face.Vertices.Count > 3))
                ts.Errors.NonTriangularFaces.Add(face);
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
                    = new List<Tuple<Vertex, PolygonalFace>> { new Tuple<Vertex, PolygonalFace>(vertex, face) };
            else ts.Errors.FacesThatDoNotLinkBackToVertex.Add(new Tuple<Vertex, PolygonalFace>(vertex, face));
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
                    = new List<Tuple<Vertex, Edge>> { new Tuple<Vertex, Edge>(vertex, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToVertex.Add(new Tuple<Vertex, Edge>(vertex, edge));
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
                    = new List<Tuple<Edge, Vertex>> { new Tuple<Edge, Vertex>(edge, vert) };
            else ts.Errors.VertsThatDoNotLinkBackToEdge.Add(new Tuple<Edge, Vertex>(edge, vert));
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
                    = new List<Tuple<Edge, PolygonalFace>> { new Tuple<Edge, PolygonalFace>(edge, face) };
            else ts.Errors.FacesThatDoNotLinkBackToEdge.Add(new Tuple<Edge, PolygonalFace>(edge, face));
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
                    = new List<Tuple<PolygonalFace, Vertex>> { new Tuple<PolygonalFace, Vertex>(face, vertex) };
            else ts.Errors.VertsThatDoNotLinkBackToFace.Add(new Tuple<PolygonalFace, Vertex>(face, vertex));
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
                    = new List<Tuple<PolygonalFace, Edge>> { new Tuple<PolygonalFace, Edge>(face, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToFace.Add(new Tuple<PolygonalFace, Edge>(face, edge));
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
            IEnumerable<Tuple<Edge, List<PolygonalFace>>> edgeFaceTuples)
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

        #endregion

        #region Repair Functions

        /// <summary>
        ///     Repairs the specified ts.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool Repair(TessellatedSolid ts)
        {
            var completelyRepaired = true;
            if (ModelIsInsideOut)
                completelyRepaired = TurnModelInsideOut(ts);
            if (EdgesWithBadAngle != null)
                completelyRepaired = completelyRepaired && FlipFacesBasedOnBadAngles(ts);
            if (NonTriangularFaces != null)
                completelyRepaired = completelyRepaired && DivideUpNonTriangularFaces(ts);
            if (SingledSidedEdges != null) //what about faces with only one or two edges?
                completelyRepaired = completelyRepaired && RepairMissingFacesFromEdges(ts);
            //Note that negligible faces are not truly errors, so they are not repaired
            return completelyRepaired;
        }

        private bool TurnModelInsideOut(TessellatedSolid ts)
        {
            ts.Volume = -1 * ts.Volume;
            ts._inertiaTensor = null;
            foreach (var face in ts.Faces)
            {
                face.Normal = face.Normal.multiply(-1);
                face.Vertices.Reverse();
                face.Edges.Reverse();
                face.Curvature = (CurvatureType)(-1 * (int)face.Curvature);
            }
            foreach (var edge in ts.Edges)
            {
                edge.Curvature = (CurvatureType)(-1 * (int)edge.Curvature);
                edge.InternalAngle = Constants.TwoPi - edge.InternalAngle;
                var tempFace = edge.OwnedFace;
                edge.OwnedFace = edge.OtherFace;
                edge.OtherFace = tempFace;
            }
            return true;
        }

        /// <summary>
        ///     Flips the faces based on bad angles.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool FlipFacesBasedOnBadAngles(TessellatedSolid ts)
        {
            var edgesWithBadAngles = new HashSet<Edge>(ts.Errors.EdgesWithBadAngle);
            var facesToConsider = new HashSet<PolygonalFace>(
                edgesWithBadAngles.SelectMany(e => new[] { e.OwnedFace, e.OtherFace }).Distinct());
            var allEdgesToUpdate = new HashSet<Edge>();
            foreach (var face in facesToConsider)
            {
                var edgesToUpdate = new List<Edge>();
                foreach (var edge in face.Edges)
                {
                    if (edge == null) ; //that's enough to know something is not right!
                    else if (edgesWithBadAngles.Contains(edge)) edgesToUpdate.Add(edge);
                    else if (facesToConsider.Contains(edge.OwnedFace == face ? edge.OtherFace : edge.OwnedFace))
                        edgesToUpdate.Add(edge);
                    else break;
                }
                if (edgesToUpdate.Count < face.Edges.Count) continue;
                face.Normal = face.Normal.multiply(-1);
                face.Edges.Reverse();
                face.Vertices.Reverse();
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


        /// <summary>
        ///     Divides up non triangular faces.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool DivideUpNonTriangularFaces(TessellatedSolid ts)
        {
            var allNewFaces = new List<PolygonalFace>();
            var singleSidedEdges = new HashSet<Edge>();
            var zeroSidedEdges = new HashSet<Edge>();
            foreach (var nonTriangularFace in ts.Errors.NonTriangularFaces)
            {
                var newFaces = new List<PolygonalFace>();
                foreach (var edge in nonTriangularFace.Edges)
                    if (singleSidedEdges.Contains(edge))
                    {
                        singleSidedEdges.Remove(edge);
                        zeroSidedEdges.Add(edge);
                    }
                    else singleSidedEdges.Add(edge);
                //Using Triangulate Polygon guarantees that even if the face has concave edges, it will triangulate properly.
                List<List<Vertex[]>> triangleFaceList;
                var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { nonTriangularFace.Vertices },
                    nonTriangularFace.Normal, out triangleFaceList);
                foreach (var triangle in triangles)
                {
                    var newFace = new PolygonalFace(triangle, nonTriangularFace.Normal) { Color = nonTriangularFace.Color };
                    newFaces.Add(newFace);
                }
                ts.AddPrimitive(new Flat(newFaces));
                allNewFaces.AddRange(newFaces);
            }
            ts.RemoveFaces(ts.Errors.NonTriangularFaces);
            ts.RemoveEdges(zeroSidedEdges);
            ts.Errors.NonTriangularFaces = null;
            ts.MostPolygonSides = 3;
            return LinkUpNewFaces(allNewFaces, ts, singleSidedEdges.ToList());
        }

        /// <summary>
        ///     Repairs the missing faces from edges.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool RepairMissingFacesFromEdges(TessellatedSolid ts)
        {
            var newFaces = new List<PolygonalFace>();
            var loops = new List<List<Vertex>>();
            var loopNormals = new List<double[]>();
            var attempts = 0;
            var remainingEdges = new List<Edge>(ts.Errors.SingledSidedEdges);
            while (remainingEdges.Count > 0 && attempts < remainingEdges.Count)
            {
                var loop = new List<Vertex>();
                var successful = true;
                var removedEdges = new List<Edge>();
                var remainingEdge = remainingEdges[0];
                var startVertex = remainingEdge.From;
                var newStartVertex = remainingEdge.To;
                var normal = remainingEdge.OwnedFace.Normal;
                loop.Add(newStartVertex);
                removedEdges.Add(remainingEdge);
                remainingEdges.RemoveAt(0);
                do
                {
                    var possibleNextEdges =
                        remainingEdges.Where(e => e.To == newStartVertex || e.From == newStartVertex).ToList();
                    if (possibleNextEdges.Count() != 1) successful = false;
                    else
                    {
                        var currentEdge = possibleNextEdges[0];
                        normal = normal.multiply(loop.Count).add(currentEdge.OwnedFace.Normal).divide(loop.Count + 1);
                        normal.normalizeInPlace();
                        newStartVertex = currentEdge.OtherVertex(newStartVertex);
                        loop.Add(newStartVertex);
                        removedEdges.Add(currentEdge);
                        remainingEdges.Remove(currentEdge);
                    }
                } while (newStartVertex != startVertex && successful);
                if (successful)
                {
                    //Average the normals from all the owned faces.
                    loopNormals.Add(normal);
                    loops.Add(loop);
                    attempts = 0;
                }
                else
                {
                    remainingEdges.AddRange(removedEdges);
                    attempts++;
                }
            }

            for (var i = 0; i < loops.Count; i++)
            {
                //first check if a loop matches with another loop
                int j = FindMatchingLoop(i, loops, ts.SameTolerance);
                if (j != -1)
                {
                    GlueTogetherLoops(loops[i], loops[j]);
                    loops.RemoveAt(j);
                    loopNormals.RemoveAt(j);
                }
                //if a simple triangle, create a new face from vertices
                else if (loops[i].Count == 3)
                {
                    var newFace = new PolygonalFace(loops[i], loopNormals[i]);
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else if (loops[i].Count > 3)
                {
                    //First, get an average normal from all vertices, assuming CCW order.
                    List<List<Vertex[]>> triangleFaceList;
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { loops[i] }, loopNormals[i],
                        out triangleFaceList);
                    foreach (var triangle in triangles)
                    {
                        var newFace = new PolygonalFace(triangle, loopNormals[i]);
                        newFaces.Add(newFace);
                    }
                }
            }
            if (newFaces.Count == 1) Message.output("1 missing face was fixed", 3);
            if (newFaces.Count > 1) Message.output(newFaces.Count + " missing faces were fixed", 3);
            return LinkUpNewFaces(newFaces, ts, ts.Errors.SingledSidedEdges);
        }


        private void GlueTogetherLoops(List<Vertex> iLoop, List<Vertex> jLoop)
        {
            throw new NotImplementedException();
        }

        private int FindMatchingLoop(int i, List<List<Vertex>> loops, double sameTolerance)//, out bool sameDirection)
        {
            var j = i + 1;
            var iLoopFirstVertex = loops[i][0];
            while (j < loops.Count)
            {
                var testLoop = loops[j++];
                if (testLoop.Count != loops[i].Count) continue;
                var k = testLoop.Count - 1;
                while (k >= 0 && !testLoop[k].Position.IsPracticallySame(iLoopFirstVertex.Position, 10 * sameTolerance))
                    k--;
                if (k == -1) continue;

            }
            return -1;
        }

        /// <summary>
        ///     Links up new faces.
        /// </summary>
        /// <param name="newFaces">The new faces.</param>
        /// <param name="ts">The ts.</param>
        /// <param name="singledSidedEdges">The singled sided edges.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool LinkUpNewFaces(List<PolygonalFace> newFaces, TessellatedSolid ts, List<Edge> singledSidedEdges)
        {
            ts.AddFaces(newFaces);
            var newEdges = new List<Edge>();
            var completedEdges = new List<Edge>();
            var partlyDefinedEdges = singledSidedEdges.ToDictionary(ts.SetEdgeChecksum);
            ts.UpdateAllEdgeCheckSums();

            foreach (var face in newFaces)
            {
                for (var j = 0; j < 3; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[j == 2 ? 0 : j + 1];
                    var checksum = ts.SetEdgeChecksum(fromVertex, toVertex);

                    if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdges[checksum];
                        if (edge.OwnedFace == null) edge.OwnedFace = face;
                        else if (edge.OtherFace == null) edge.OtherFace = face;
                        face.AddEdge(edge);
                        completedEdges.Add(edge);
                        partlyDefinedEdges.Remove(checksum);
                    }
                    else
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, true, checksum);
                        newEdges.Add(edge);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
            ts.AddEdges(newEdges);
            return !partlyDefinedEdges.Any();
        }

        internal static IEnumerable<Tuple<Edge, List<PolygonalFace>>> FixBadEdges(
             IEnumerable<Tuple<Edge, List<PolygonalFace>>> overUsedEdgesDictionary,
            IEnumerable<Edge> partlyDefinedEdgesIEnumerable)
        {
            var newListOfGoodEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            var partlyDefinedEdges = new List<Edge>(partlyDefinedEdgesIEnumerable);
            foreach (var entry in overUsedEdgesDictionary)
            {
                var candidateFaces = entry.Item2;
                var edge = entry.Item1;
                var numFailedTries = 0;
                while (candidateFaces.Count > 1 && numFailedTries < candidateFaces.Count)
                {
                    var highestDotProduct = -2.0;
                    PolygonalFace bestMatch = null;
                    var referenceFace = candidateFaces[0];
                    candidateFaces.RemoveAt(0);
                    var refOwnsEdge = FaceShouldBeOwnedFace(edge, referenceFace);
                    foreach (var candidateMatchingFace in candidateFaces)
                    {
                        var dotProductScore = refOwnsEdge == FaceShouldBeOwnedFace(edge, candidateMatchingFace) ? -2 //edge cannot be owned by both faces, 
                            : referenceFace.Normal.dotProduct(candidateMatchingFace.Normal);          // thus this is not a good candidate for this
                        if (dotProductScore > highestDotProduct)
                        {
                            highestDotProduct = dotProductScore;
                            bestMatch = candidateMatchingFace;
                        }
                    }
                    if (highestDotProduct > -1)
                    {
                        numFailedTries = 0;
                        storeSuccessfulFaceMatch(edge, referenceFace, bestMatch, newListOfGoodEdges);
                        candidateFaces.Remove(bestMatch);
                    }
                    else
                    {
                        candidateFaces.Add(referenceFace);
                        numFailedTries++;
                    }
                }
                while (partlyDefinedEdges.Any() && candidateFaces.Any() && numFailedTries < candidateFaces.Count)
                {
                    var smallestDistance = double.PositiveInfinity;
                    Edge bestMatch = null;
                    var referenceFace = candidateFaces[0];
                    candidateFaces.RemoveAt(0);
                    foreach (var partlyDefinedEdge in partlyDefinedEdges)
                    {
                        var score = GetEdgeSimilarityScore(edge, partlyDefinedEdge);
                        if (score < smallestDistance)
                        {
                            smallestDistance = score;
                            bestMatch = partlyDefinedEdge;
                        }
                    }
                    if (smallestDistance < 0.1)
                    {
                        numFailedTries = 0;
                        storeSuccessfulFaceMatch(bestMatch, bestMatch.OwnedFace, referenceFace, newListOfGoodEdges);
                        partlyDefinedEdges.Remove(bestMatch);
                    }
                    else
                    {
                        candidateFaces.Add(referenceFace);
                        numFailedTries++;
                    }
                }
            }
            foreach (var entry in overUsedEdgesDictionary)
            {
                foreach (var face in entry.Item2)
                {
                    for (int i = 0; i < face.Edges.Count; i++)
                    {
                        var edge = face.Edges[i];
                        if (edge != null) continue;
                        var fromVertex = face.Vertices[i];
                        var toVertex = face.NextVertexCCW(fromVertex);
                        partlyDefinedEdges.Add(new Edge(fromVertex, toVertex, face, null));
                    }
                }
                entry.Item1.From.Edges.Remove(entry.Item1);
                entry.Item1.To.Edges.Remove(entry.Item1);
            }
            var pDELength = partlyDefinedEdges.Count;
            var scores = new SortedDictionary<double, int[]>(new NoEqualSort());
            for (int i = 0; i < pDELength; i++)
                for (int j = i + 1; j < pDELength; j++)
                    scores.Add(GetEdgeSimilarityScore(partlyDefinedEdges[i], partlyDefinedEdges[j]), new[] { i, j });
            var alreadyMatchedIndices = new HashSet<int>();
            var highestScore = 0.0;
            foreach (var score in scores)
            {
                if (highestScore > Constants.MaxAllowableEdgeSimilarityScore) break;
                if (alreadyMatchedIndices.Contains(score.Value[0]) || alreadyMatchedIndices.Contains(score.Value[1]))
                    continue;
                highestScore = score.Key;
                alreadyMatchedIndices.Add(score.Value[0]);
                alreadyMatchedIndices.Add(score.Value[1]);
                newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(partlyDefinedEdges[score.Value[0]],
                        new List<PolygonalFace> { partlyDefinedEdges[score.Value[0]].OwnedFace, partlyDefinedEdges[score.Value[1]].OwnedFace }));
            }
            return newListOfGoodEdges;
        }

        private static double GetEdgeSimilarityScore(Edge e1, Edge e2)
        {
            var score = Math.Abs(e1.Length - e2.Length);
            score += 1 - Math.Abs(e1.Vector.normalize().dotProduct(e2.Vector.normalize()));
            score += Math.Min(e2.From.Position.subtract(e1.To.Position).norm2()
                + e2.To.Position.subtract(e1.From.Position).norm2(),
                e2.From.Position.subtract(e1.From.Position).norm2()
                + e2.To.Position.subtract(e1.To.Position).norm2())
                     / e1.Length;
            return score;
        }

        private static void storeSuccessfulFaceMatch(Edge edge, PolygonalFace refFace, PolygonalFace bestMatch, List<Tuple<Edge, List<PolygonalFace>>> newListOfGoodEdges)
        {
            if (FaceShouldBeOwnedFace(edge, refFace))
                newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                     new Edge(edge.From, edge.To, refFace, bestMatch), new List<PolygonalFace> { refFace, bestMatch }));
            else
                newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                     new Edge(edge.From, edge.To, bestMatch, refFace), new List<PolygonalFace> { bestMatch, refFace }));
        }

        private static bool FaceShouldBeOwnedFace(Edge edge, PolygonalFace face)
        {
            var otherEdgeVector = face.OtherVertex(edge.From, edge.To).Position.subtract(edge.To.Position);
            var isThisNormal = edge.Vector.crossProduct(otherEdgeVector);
            return face.Normal.dotProduct(isThisNormal) > 0;
        }
        #endregion
    }
}