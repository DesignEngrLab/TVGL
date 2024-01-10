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
using TVGL.Miscellaneous_Functions;

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
        /// <param name="numberOfFaces">The number of new faces to add.</param>
        /// <param name="maxLength">The maximum length.</param>
        public static void Complexify(TessellatedSolid ts, int numberOfFaces, double maxLength)
        {
            var edgeQueue = new PriorityQueue<Edge, double>(new ReverseSort());
            foreach (var e in ts.Edges)
                edgeQueue.Enqueue(e, e.Length);
            var addedEdges = new List<Edge>();
            var addedVertices = new List<Vertex>();
            var addedFaces = new List<TriangleFace>();
            var edge = edgeQueue.Dequeue();
            var iterations = numberOfFaces > 0 ? (int)Math.Ceiling(numberOfFaces / 2.0) : numberOfFaces;
            while (iterations-- != 0 && edge.Length >= maxLength)
            {
                var origLeftFace = edge.OtherFace;
                var origRightFace = edge.OwnedFace;
                var leftFarVertex = origLeftFace.OtherVertex(edge);
                var rightFarVertex = origRightFace.OtherVertex(edge);
                var fromVertex = edge.From;
                var toVertex = edge.To;
                var addedVertex = new Vertex(DetermineIntermediateVertexPosition(fromVertex, toVertex));
                // modify original faces with new intermediate vertex
                origLeftFace.ReplaceVertex(toVertex, addedVertex);
                origRightFace.ReplaceVertex(toVertex, addedVertex);

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
                edgeQueue.Enqueue(edge, edge.Length);
                edgeQueue.Enqueue(inlineEdge, inlineEdge.Length);
                addedEdges.Add(inlineEdge);
                edgeQueue.Enqueue(newLeftEdge, newLeftEdge.Length);
                addedEdges.Add(newLeftEdge);
                edgeQueue.Enqueue(newRightEdge, newRightEdge.Length);
                addedEdges.Add(newRightEdge);
                addedFaces.Add(newLeftFace);
                addedFaces.Add(newRightFace);
                addedVertices.Add(addedVertex);
                edge = edgeQueue.Dequeue();
            }
            ts.AddVertices(addedVertices);
            ts.AddEdges(addedEdges);
            ts.AddFaces(addedFaces);
        }
    }

}