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
        ///     Refines by the percentage provided. For example, is ts has 100 faces, then passing
        ///     a 0.2 will increase to 120 faces
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="percentageToIncreaseBy">The percentage to reduce by.</param>
        public static void RefineByPercentage(this TessellatedSolid ts, double percentageToIncreaseBy)
        {
            RefineToNFaces(ts, (int)((1 + percentageToIncreaseBy) * ts.NumberOfFaces));
        }
        /// <summary>
        ///     Refines the model by splitting the longest edges 
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Refine(this TessellatedSolid ts)
        {
            RefineByMaxEdgeLength(ts, ts.Edges.Max(x => x.Length) * 0.5);
        }
        /// <summary>
        ///     Refines to n edges.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfFaces">The number of faces.</param>
        private static void RefineToNFaces(this TessellatedSolid ts, int numberOfEdges)
        {
            var sortedEdges = new SortedSet<Edge>(ts.Edges, new SortByLength(false));
            var addedEdges = new List<Edge>();
            var addedVertices = new List<Vertex>();
            var addedFaces = new List<PolygonalFace>();
            var edge = sortedEdges.First();
            var i = 0;
            while (i <= numberOfEdges)
            {
            }
        }



        /// <summary>
        ///     Refines by a tolerance whereby edges within the specified length will be split.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="maxLength">The tolerance.</param>
        public static void RefineByMaxEdgeLength(this TessellatedSolid ts, double maxLength)
        {
            var sortedEdges = new SortedSet<Edge>(ts.Edges, new SortByLength(false));
            var addedEdges = new List<Edge>();
            var addedVertices = new List<Vertex>();
            var addedFaces = new List<PolygonalFace>();
            var edge = sortedEdges.First();
            while (edge.Length <= maxLength)
            {
                sortedEdges.Remove(edge);
                Edge addedEdge1, addedEdge2, addedEdge3;
                PolygonalFace addedFace1, addedFace2;
                Vertex addedVertex;
                SplitEdge(edge, out addedVertex, out addedEdge1, out addedEdge2, out addedEdge3,
                    out addedFace1, out addedFace2);
                {
                    addedEdges.Add(edge);
                    addedEdges.Add(addedEdge1);
                    addedEdges.Add(addedEdge2);
                    addedEdges.Add(addedEdge3);
                    addedFaces.Add(addedFace1);
                    addedFaces.Add(addedFace2);
                    addedVertices.Add(addedVertex);
                }
                edge = sortedEdges.First();
            }
            ts.AddEdges(addedEdges);
            ts.AddFaces(addedFaces);
            ts.AddVertices(addedVertices);
        }

        private static void SplitEdge(Edge edge, out Vertex addedVertex, out Edge addedEdge1, out Edge addedEdge2, out Edge addedEdge3, out PolygonalFace addedFace1, out PolygonalFace addedFace2)
        {
            throw new NotImplementedException();
        }
    }

}