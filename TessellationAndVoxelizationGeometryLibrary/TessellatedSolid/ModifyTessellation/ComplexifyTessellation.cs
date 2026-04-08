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
        /// <param name="maxSurfaceDeviation">The maximum allowable length for edges to be considered for subdivision. Must be a positive value.</param>
        public static void Complexify(IEnumerable<Edge> edges, out List<Edge> addedEdges, out List<Vertex> addedVertices,
            out List<TriangleFace> addedFaces, int targetNumberOfFaces, double maxSurfaceDeviation)
        {
            //var initEdgePlot = edges.Select(e => new[] { e.From.Coordinates, e.To.Coordinates }).ToArray();
            var edgeQueue = new PriorityQueue<(Edge, Vector3), double>(new ReverseSort());
            foreach (var edge in edges)
                EnqueueEdgeAndFindNewPoint(edgeQueue, edge, maxSurfaceDeviation);
            //var edgeLengthList = edges.OrderByDescending(e => e.Length).ToArray();
            addedEdges = new List<Edge>();
            addedVertices = new List<Vertex>();
            addedFaces = new List<TriangleFace>();
            var iterations = targetNumberOfFaces > 0 ? (int)Math.Ceiling(targetNumberOfFaces / 2.0) : targetNumberOfFaces;
            var edgeCounter = edgeQueue.Count;
            while (iterations-- != 0 && edgeQueue.TryDequeue(out (Edge edge, Vector3 mpt) c, out _))
            {
                //var map = edgeLengthList.IndexOf(c.edge);
                //Console.WriteLine(map);
                //if (iterations % 50 == 0)
                //    Presenter.ShowAndHang([initEdgePlot, addedEdges.Select(e => new[] { e.From.Coordinates, e.To.Coordinates }), [[c.edge.From.Coordinates, c.mpt, c.edge.To.Coordinates]]],
                //        [false, false, false], colors: [new Color(KnownColors.LightGray), new Color(KnownColors.Blue), new Color(KnownColors.Red)]);
                var origLeftFace = c.edge.OtherFace;
                var origRightFace = c.edge.OwnedFace;
                var leftFarVertex = origLeftFace?.OtherVertex(c.edge);
                var rightFarVertex = origRightFace?.OtherVertex(c.edge);
                var fromVertex = c.edge.From;
                var toVertex = c.edge.To;
                var addedVertex = new Vertex(c.mpt);
                // modify original faces with new intermediate vertex
                origLeftFace?.ReplaceVertex(toVertex, addedVertex);
                origRightFace?.ReplaceVertex(toVertex, addedVertex);

                TriangleFace newLeftFace = null, newRightFace = null;
                if (leftFarVertex != null)
                    newLeftFace = new TriangleFace(toVertex, addedVertex, leftFarVertex)
                    { BelongsToPrimitive = origLeftFace.BelongsToPrimitive };
                if (rightFarVertex != null)
                    newRightFace = new TriangleFace(addedVertex, toVertex, rightFarVertex)
                    { BelongsToPrimitive = origRightFace.BelongsToPrimitive };
                toVertex.Faces.Remove(origLeftFace);
                toVertex.Faces.Remove(origRightFace);

                var inlineEdge = new Edge(addedVertex, toVertex, newRightFace, newLeftFace, true, edgeCounter++);
                toVertex.Edges.Remove(c.edge);
                c.edge.To = addedVertex;
                addedVertex.Edges.Add(c.edge);
                c.edge.Update();
                Edge newLeftEdge = null, newRightEdge = null;
                if (leftFarVertex != null)
                {
                    newLeftEdge = new Edge(leftFarVertex, addedVertex, origLeftFace, newLeftFace, true, edgeCounter++);
                    origLeftFace.AddEdge(newLeftEdge);
                    var bottomEdge = toVertex.Edges.First(e => e.OtherVertex(toVertex) == leftFarVertex);
                    if (bottomEdge.OwnedFace == origLeftFace)
                        bottomEdge.OwnedFace = newLeftFace;
                    else bottomEdge.OtherFace = newLeftFace;
                    newLeftFace.AddEdge(bottomEdge);
                    bottomEdge.Update();
                }
                if (rightFarVertex != null)
                {
                    newRightEdge = new Edge(rightFarVertex, addedVertex, newRightFace, origRightFace, true, edgeCounter++);
                    origRightFace.AddEdge(newRightEdge);
                    var bottomEdge = toVertex.Edges.First(e => e.OtherVertex(toVertex) == rightFarVertex);
                    if (bottomEdge.OwnedFace == origRightFace)
                        bottomEdge.OwnedFace = newRightFace;
                    else bottomEdge.OtherFace = newRightFace;
                    newRightFace.AddEdge(bottomEdge);
                    bottomEdge.Update();
                }

                // need to re-add the edge. It was modified in the SplitEdge function (now, half the lenght), but
                // it may still be met by this criteria
                EnqueueEdgeAndFindNewPoint(edgeQueue, c.edge, maxSurfaceDeviation);
                EnqueueEdgeAndFindNewPoint(edgeQueue, inlineEdge, maxSurfaceDeviation);
                if (newLeftEdge != null)
                    EnqueueEdgeAndFindNewPoint(edgeQueue, newLeftEdge, maxSurfaceDeviation);
                if (newRightEdge != null)
                    EnqueueEdgeAndFindNewPoint(edgeQueue, newRightEdge, maxSurfaceDeviation);

                addedVertices.Add(addedVertex);
                addedEdges.Add(inlineEdge);
                if (newLeftEdge != null) addedEdges.Add(newLeftEdge);
                if (newRightEdge != null) addedEdges.Add(newRightEdge);
                if (newLeftFace != null) addedFaces.Add(newLeftFace);
                if (newRightFace != null) addedFaces.Add(newRightFace);
            }
        }

        private static void EnqueueEdgeAndFindNewPoint(PriorityQueue<(Edge, Vector3), double> edgeQueue, Edge edge, double cutOff)
        {
            var midPoint = DetermineIntermediateVertexPosition(edge);
            var distanceToSurf = MiscFunctions.DistancePointToLine(midPoint, edge.From.Coordinates, edge.Vector);
            if (distanceToSurf > cutOff)
                edgeQueue.Enqueue((edge, midPoint), distanceToSurf);
        }
    }

}