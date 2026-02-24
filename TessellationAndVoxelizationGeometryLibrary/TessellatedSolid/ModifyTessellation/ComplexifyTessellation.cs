// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="ComplexifyTessellation.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace TVGL
{
    /// <summary>
    /// This portion of ModifyTessellation includes the functions to complexify a solid, which means
    /// adding more elements to it.
    /// </summary>
    public static partial class ModifyTessellation
    {
        /// <summary>
        /// Complexifies the model by splitting the any edges that are half or more than the longest edge.
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Complexify(this TessellatedSolid ts)
        {
            Complexify(ts, ts.NumberOfFaces / 2, ts.Edges.Max(x => x.Length) * 0.5);
        }
        /// <summary>
        /// Complexifies the tessellation by adding more faces of the provided number.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfNewFaces">The number of faces.</param>
        public static void Complexify(this TessellatedSolid ts, int numberOfNewFaces)
        {
            Complexify(ts, numberOfNewFaces, 0.0);
        }

        /// <summary>
        /// Complexifies the tessellation so that no edge is longer than provided the maximum edge length.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="maxLength">The tolerance.</param>
        public static void Complexify(this TessellatedSolid ts, double maxLength)
        {
            Complexify(ts, -1, maxLength);
        }


        /// <summary>
        /// Complexifies the tessellation so that no edge is longer than provided the maximum edge length
        /// or for adding the provided number of faces - whichever comes first
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="targetNumberOfFaces">The number of new faces to add.</param>
        /// <param name="maxLength">The maximum length.</param>
        public static void Complexify(TessellatedSolid ts, int targetNumberOfFaces, double maxLength)
        {
            Complexify(ts.Edges, out var addedEdges, out var addedVertices, out var addedFaces, targetNumberOfFaces, maxLength);
            ts.AddVertices(addedVertices);
            ts.AddEdges(addedEdges);
            ts.AddFaces(addedFaces);
        }

        /// <summary>
        /// Modifies the mesh by subdividing edges and faces to increase geometric complexity, aiming to reach a
        /// specified target number of faces. Newly created edges, vertices, and faces are returned via output
        /// parameters.
        /// </summary>
        /// <remarks>This method incrementally subdivides qualifying edges and their adjacent faces until
        /// the target number of faces is reached or no further subdivisions are possible. The input mesh is modified in
        /// place, and the output lists provide references to all newly created geometry elements. The method does not
        /// guarantee an exact match to the target number of faces if the mesh cannot be further subdivided under the
        /// given constraints.</remarks>
        /// <param name="edges">An array of edges to process for potential subdivision. Only edges with a length greater than or equal to
        /// maxLength are considered.</param>
        /// <param name="addedEdges">When this method returns, contains the list of edges that were added during the operation.</param>
        /// <param name="addedVertices">When this method returns, contains the list of vertices that were added during the operation.</param>
        /// <param name="addedFaces">When this method returns, contains the list of triangle faces that were added during the operation.</param>
        /// <param name="targetNumberOfFaces">The desired total number of faces in the mesh after the operation. Must be a non-negative integer.</param>
        /// <param name="maxLength">The maximum allowable length for edges to be considered for subdivision. Must be a positive value.</param>
        public static void Complexify(IEnumerable<Edge> edges, out List<Edge> addedEdges, out List<Vertex> addedVertices, out List<TriangleFace> addedFaces,
            int targetNumberOfFaces, double maxLength)
        {
            var edgeQueue = new PriorityQueue<Edge, double>(edges.Where(e => e.Length >= maxLength).Select(e => (e, e.Length)),
                new ReverseSort());
            addedEdges = new List<Edge>();
            addedVertices = new List<Vertex>();
            addedFaces = new List<TriangleFace>();
            var iterations = targetNumberOfFaces > 0 ? (int)Math.Ceiling(targetNumberOfFaces / 2.0) : targetNumberOfFaces;
            while (iterations-- != 0 && edgeQueue.TryDequeue(out var edge, out var edgeLength))
            {
                var origLeftFace = edge.OtherFace;
                var origRightFace = edge.OwnedFace;
                var leftFarVertex = origLeftFace?.OtherVertex(edge);
                var rightFarVertex = origRightFace?.OtherVertex(edge);
                var fromVertex = edge.From;
                var toVertex = edge.To;
                var primitive1 = origLeftFace?.BelongsToPrimitive;
                var primitive2 = origRightFace == null ? primitive1 : origRightFace.BelongsToPrimitive;
                if (primitive1 == null) primitive1 = primitive2;
                var commonPrimitive = (primitive1 == primitive2) ? primitive1 : null;
                var addedVertex = new Vertex(DetermineIntermediateVertexPosition(fromVertex.Coordinates,
                    toVertex.Coordinates, commonPrimitive));
                // modify original faces with new intermediate vertex
                origLeftFace?.ReplaceVertex(toVertex, addedVertex);
                origRightFace?.ReplaceVertex(toVertex, addedVertex);

                var newLeftFace = new TriangleFace(toVertex, addedVertex, leftFarVertex);
                var newRightFace = new TriangleFace(addedVertex, toVertex, rightFarVertex);
                toVertex.Faces.Remove(origLeftFace);
                toVertex.Faces.Remove(origRightFace);

                var inlineEdge = new Edge(addedVertex, toVertex, newRightFace, newLeftFace, true);
                toVertex.Edges.Remove(edge);
                edge.To = addedVertex;
                addedVertex.Edges.Add(edge);
                edge.Update();
                var newLeftEdge = new Edge(leftFarVertex, addedVertex, origLeftFace, newLeftFace, true);
                var newRightEdge = new Edge(rightFarVertex, addedVertex, newRightFace, origRightFace, true);
                origLeftFace.AddEdge(newLeftEdge);
                origRightFace.AddEdge(newRightEdge);
                var bottomEdge = toVertex.Edges.First(e => e.OtherVertex(toVertex) == leftFarVertex);
                if (bottomEdge.OwnedFace == origLeftFace)
                    bottomEdge.OwnedFace = newLeftFace;
                else bottomEdge.OtherFace = newLeftFace;
                newLeftFace.AddEdge(bottomEdge);
                bottomEdge.Update();

                bottomEdge = toVertex.Edges.First(e => e.OtherVertex(toVertex) == rightFarVertex);
                if (bottomEdge.OwnedFace == origRightFace)
                    bottomEdge.OwnedFace = newRightFace;
                else bottomEdge.OtherFace = newRightFace;
                newRightFace.AddEdge(bottomEdge);
                bottomEdge.Update();

                // need to re-add the edge. It was modified in the SplitEdge function (now, half the lenght), but
                // it may still be met by this criteria
                if (edge.Length >= maxLength)
                    edgeQueue.Enqueue(edge, edge.Length);
                if (inlineEdge.Length >= maxLength)
                    edgeQueue.Enqueue(inlineEdge, inlineEdge.Length);
                if (newLeftEdge.Length >= maxLength)
                    edgeQueue.Enqueue(newLeftEdge, newLeftEdge.Length);
                if (newRightEdge.Length >= maxLength)
                    edgeQueue.Enqueue(newRightEdge, newRightEdge.Length);

                addedEdges.Add(inlineEdge);
                addedEdges.Add(newLeftEdge);
                addedEdges.Add(newRightEdge);
                addedFaces.Add(newLeftFace);
                addedFaces.Add(newRightFace);
                addedVertices.Add(addedVertex);
            }
        }
    }

}