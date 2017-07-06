// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="RefineTessellation.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///  This portion of ModifyTessellation includes the functions to refine a solid, which means 
    ///  adding more elements to it. invoked during the opening of a tessellated solid from "disk", but the repair function
    ///  may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        /// <summary>
        ///     Refines the model by splitting the longest edges 
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Refine(this TessellatedSolid ts)
        {
            RefineByMaxEdgeLength(ts, ts.Edges.Max(x => x.Length) * 0.5);
        }
        /// <summary>
        ///     Refines by the percentage provided. For example, is ts has 100 faces, then passing
        ///     a 0.2 will increase to 120 faces
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="percentageToIncreaseBy">The percentage to reduce by.</param>
        public static void RefineByPercentage(this TessellatedSolid ts, double percentageToIncreaseBy)
        {
            RefineToNMoreFaces(ts, (int)(percentageToIncreaseBy * ts.NumberOfFaces));
        }
        /// <summary>
        /// Refines the tessellation by adding more faces of the provided number.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfNewFaces">The number of faces.</param>
        private static void RefineToNMoreFaces(this TessellatedSolid ts, int numberOfNewFaces)
        {
            RefineBody(ts, numberOfNewFaces, double.PositiveInfinity);
        }

        /// <summary>
        /// Refines the tessellation so that no edge are longer than provided the maximum edge length.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="maxLength">The tolerance.</param>
        public static void RefineByMaxEdgeLength(this TessellatedSolid ts, double maxLength)
        {
            RefineBody(ts, -1, maxLength);
        }


        private static void RefineBody(TessellatedSolid ts, int numberOfFaces, double maxLength)
        {
            var sortedEdges = new SortedSet<Edge>(ts.Edges, new SortByLength(false));
            var addedEdges = new List<Edge>();
            var addedVertices = new List<Vertex>();
            var addedFaces = new List<PolygonalFace>();
            var edge = sortedEdges.First();
            var iterations = numberOfFaces > 0 ? (int)Math.Ceiling(numberOfFaces / 2.0) : numberOfFaces;
            while (iterations-- != 0 && edge.Length >= maxLength)
            {
                sortedEdges.Remove(edge);
                var origLeftFace = edge.OtherFace;
                var origRightFace = edge.OwnedFace;
                var leftFarVertex = origLeftFace.OtherVertex(edge);
                var rightFarVertex = origRightFace.OtherVertex(edge);
                var addedVertex = new Vertex(DetermineIntermediateVertexPosition(edge.From, edge.To));

                var index = origLeftFace.Vertices.IndexOf(edge.To);
                origLeftFace.Vertices[index] = addedVertex;
                origLeftFace.Update();
                index = origRightFace.Vertices.IndexOf(edge.To);
                origRightFace.Vertices[index] = addedVertex;
                origRightFace.Update();

                var newLeftFace = new PolygonalFace(new[] { edge.To, addedVertex, leftFarVertex }, true);
                var newRightFace = new PolygonalFace(new[] { addedVertex, edge.To, rightFarVertex }, true);

                var inlineEdge = new Edge(addedVertex, edge.To, newRightFace, newLeftFace, true);
                edge.To = addedVertex;
                edge.Update();
                var newLeftEdge = new Edge(leftFarVertex, addedVertex, origLeftFace, newLeftFace, true);
                var newRightEdge = new Edge(rightFarVertex, addedVertex, newRightFace, origRightFace, true);

                // need to re-add the edge. It was modified in the SplitEdge function (now, half the lenght), but
                // it may still be met by this criteria
                sortedEdges.Add(edge);
                sortedEdges.Add(inlineEdge);
                addedEdges.Add(inlineEdge);
                sortedEdges.Add(newLeftEdge);
                addedEdges.Add(newLeftEdge);
                sortedEdges.Add(newRightEdge);
                addedEdges.Add(newRightEdge);
                addedFaces.Add(newLeftFace);
                addedFaces.Add(newRightFace);
                addedVertices.Add(addedVertex);
                edge = sortedEdges.First();
            }
            ts.AddEdges(addedEdges);
            ts.AddFaces(addedFaces);
            ts.AddVertices(addedVertices);
        }
    }

}