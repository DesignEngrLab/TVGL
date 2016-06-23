// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="Slice.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     The Slice class includes static functions for cutting a tessellated solid.
    /// </summary>
    public static class SimplifyTessellation
    {
        /// <summary>
        ///     Simplifies by the percentage provided. For example, is ts has 100 triangles, then passing
        ///     a 0.2 will reduce to 80 triangles
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="percentageToReduceBy">The percentage to reduce by.</param>
        public static void SimplifyByPercentage(this TessellatedSolid ts, double percentageToReduceBy)
        {
            SimplifyToNFaces(ts, (int)((1 - percentageToReduceBy) * ts.NumberOfFaces));
        }

        /// <summary>
        ///     Simplifies to n faces.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfFaces">The number of faces.</param>
        private static void SimplifyToNFaces(this TessellatedSolid ts, int numberOfFaces)
        {
            if (ts.Errors != null)
                Message.output(
                    "** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).",
                    1);

            var numberToRemove = ts.NumberOfFaces - numberOfFaces;
            var sortedEdges = ts.Edges.OrderBy(e => e.Length).ToList();
            var removedEdges = new HashSet<Edge>();
            var removedEdgesSorted = new SortedList<int, Edge>();
            var removedVertices = new SortedList<int, Vertex>();
            var removedFaces = new SortedList<int, PolygonalFace>();
            var i = 0;
            //var edge = sortedEdges[0];
            while (numberToRemove > 0)
            {
                var edge = sortedEdges[i++];
                if (removedEdges.Contains(edge)) continue;
                Edge removedEdge1, removedEdge2;
                PolygonalFace removedFace1, removedFace2;
                Vertex removedVertex;
                if (TessellatedSolid.CombineVerticesOfEdge(edge, out removedVertex, out removedEdge1, out removedEdge2,
                    out removedFace1,
                    out removedFace2))
                {
                    removedEdges.Add(edge);
                    removedEdgesSorted.Add(edge.IndexInList, edge);
                    if (removedEdge1 != null)
                    {
                        removedEdges.Add(removedEdge1);
                        if (!removedEdgesSorted.ContainsKey(removedEdge1.IndexInList))
                            removedEdgesSorted.Add(removedEdge1.IndexInList, removedEdge1);
                    }
                    if (removedEdge2 != null)
                    {
                        removedEdges.Add(removedEdge2);
                        if (!removedEdgesSorted.ContainsKey(removedEdge2.IndexInList))
                            removedEdgesSorted.Add(removedEdge2.IndexInList, removedEdge2);
                    }
                    if (removedFace1 != null)
                    {
                        removedFaces.Add(removedFace1.IndexInList, removedFace1);
                        numberToRemove--;
                    }
                    if (removedFace2 != null)
                    {
                        removedFaces.Add(removedFace2.IndexInList, removedFace2);
                        numberToRemove--;
                    }
                    removedVertices.Add(removedVertex.IndexInList, removedVertex);
                }
            }
            ts.RemoveEdges(removedEdgesSorted.Keys.ToList());
            ts.RemoveFaces(removedFaces.Keys.ToList());
            ts.RemoveVertices(removedVertices.Keys.ToList());
        }

        /// <summary>
        ///     Simplifies the model by merging the vertices that are closest together
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Simplify(this TessellatedSolid ts)
        {
            SimplifyByTolerance(ts, ts.SameTolerance * 10);
        }

        /// <summary>
        ///     Simplifies by a tolerance whereby vertices within the specified length will be merged.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="tolerance">The tolerance.</param>
        public static void SimplifyByTolerance(this TessellatedSolid ts, double tolerance)
        {
            if (ts.Errors != null)
                Message.output(
                    "** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).",
                    1);

            var sortedEdges = ts.Edges.OrderBy(e => e.Length).ToList();
            var removedEdges = new List<Edge>();
            var removedVertices = new List<Vertex>();
            var removedFaces = new List<PolygonalFace>();
            var edge = sortedEdges[0];
            var i = 0;
            while (edge.Length <= tolerance)
            {
                if (!removedEdges.Contains(edge))
                {
                    Edge removedEdge1, removedEdge2;
                    PolygonalFace removedFace1, removedFace2;
                    Vertex removedVertex;
                    if (TessellatedSolid.CombineVerticesOfEdge(edge, out removedVertex, out removedEdge1, out removedEdge2,
                        out removedFace1,
                        out removedFace2))
                    {
                        removedEdges.Add(edge);
                        removedEdges.Add(removedEdge1);
                        removedEdges.Add(removedEdge2);
                        removedFaces.Add(removedFace1);
                        removedFaces.Add(removedFace2);
                        removedVertices.Add(removedVertex);
                    }
                }
                edge = sortedEdges[++i];
            }
            ts.RemoveEdges(removedEdges);
            ts.RemoveFaces(removedFaces);
            ts.RemoveVertices(removedVertices);
        }

    }
}