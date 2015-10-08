// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-05-2015
// ***********************************************************************
// <copyright file="Slice.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL.Boolean_Operations;

namespace TVGL
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// </summary>
    public static class Simplify
    {
        /// <summary>
        /// Simplifies by the percentage provided. For example, is ts has 100 triangles, then passing 
        /// a 0.2 will reduce to 80 triangles
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="percentageToReduceBy">The percentage to reduce by.</param>
        public static void SimplifyByPercentage(this TessellatedSolid ts, double percentageToReduceBy)
        {
            SimplifyToNFaces(ts, (int)((1 - percentageToReduceBy) * ts.NumberOfFaces));
        }

        private static void SimplifyToNFaces(this TessellatedSolid ts, int numberOfFaces)
        {
            var numberToRemove = ts.NumberOfFaces - numberOfFaces;
            var sortedEdges = ts.Edges.OrderBy(e => e.Length).ToList();
            var removedEdges = new HashSet<Edge>();
            var removedVertices = new HashSet<Vertex>();
            var removedFaces = new HashSet<PolygonalFace>();
            var i = 0;
            while (numberToRemove > 0)
            {
                var edge = sortedEdges[i++];
                if (removedEdges.Contains(edge)) continue;
                Edge removedEdge1, removedEdge2;
                PolygonalFace removedFace1, removedFace2;
                Vertex removedVertex;
                RemoveEdge(ts, edge, out removedVertex, out removedEdge1, out removedEdge2, out removedFace1, out removedFace2);
                removedEdges.Add(edge);
                removedEdges.Add(removedEdge1);
                removedEdges.Add(removedEdge2);
                removedFaces.Add(removedFace1);
                numberToRemove--;
                removedFaces.Add(removedFace2);
                numberToRemove--;
                removedVertices.Add(removedVertex);
            }
            ts.RemoveEdges(removedEdges);
            ts.RemoveFaces(removedFaces);
            ts.RemoveVertices(removedVertices);
        }

        public static void SimplifyBasic(this TessellatedSolid ts)
        {
            SimplifyByTolerance(ts, ts.sameTolerance * 10);
        }

        /// <summary>
        /// Simplifies by a tolerance whereby vertices within the specified lenght will be merged.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="tolerance">The tolerance.</param>
        public static void SimplifyByTolerance(this TessellatedSolid ts, double tolerance)
        {
            var sortedEdges = ts.Edges.OrderBy(e => e.Length).ToList();
            var removedEdges = new List<Edge>();
            var removedVertices = new List<Vertex>();
            var removedFaces = new List<PolygonalFace>();
            var edge = sortedEdges[0];
            var i = 0;
            do
            {
                i++;
                if (removedEdges.Contains(edge)) continue;
                Edge removedEdge1, removedEdge2;
                PolygonalFace removedFace1, removedFace2;
                Vertex removedVertex;
                RemoveEdge(ts, edge, out removedVertex, out removedEdge1, out removedEdge2, out removedFace1, out removedFace2);
                removedEdges.Add(edge);
                removedEdges.Add(removedEdge1);
                removedEdges.Add(removedEdge2);
                removedFaces.Add(removedFace1);
                removedFaces.Add(removedFace2);
                removedVertices.Add(removedVertex);
                edge = sortedEdges[i];
            }
            while (edge.Length <= tolerance);
            ts.RemoveEdges(removedEdges);
            ts.RemoveFaces(removedFaces);
            ts.RemoveVertices(removedVertices);
        }

        private static void RemoveEdge(TessellatedSolid ts, Edge edge, out Vertex removedVertex, out Edge removedEdge1,
            out Edge removedEdge2, out PolygonalFace removedFace1, out PolygonalFace removedFace2)
        {
            removedVertex = edge.From;
            var keepVertex = edge.To;
            var rEdge1 = removedEdge1 = edge.OwnedFace.Edges.First(e => e != edge && (e.To == edge.From || e.From == edge.From));
            var rEdge2 = removedEdge2 = edge.OtherFace.Edges.First(e => e != edge && (e.To == edge.From || e.From == edge.From));
            removedFace1 = edge.OwnedFace;
            removedFace2 = edge.OtherFace;
            // move edges connected to removeVertex to the keepVertex and let keepVertex link back to these edges
            foreach (var e in removedVertex.Edges)
            {
                if (e == edge || e == removedEdge1 || e == removedEdge2) continue;
                if (e.From == removedVertex) e.From = keepVertex;
                else e.To = keepVertex;
                keepVertex.Edges.Add(e);
            }
            // move faces connected to removeVertex to the keepVertex and let keepVertexlink back to these edges.
            foreach (var face in removedVertex.Faces)
            {
                if (face == removedFace1 || face == removedFace2) continue;
                face.Vertices[face.Vertices.IndexOf(removedVertex)] = keepVertex;
                keepVertex.Faces.Add(face);
            }
            // for the winged edges (removedEdge1 and removedEdge2) that are removed, connected their faces to 
            // the new edge
            // first on the "owned side of edge"
            var keepedge1 = removedFace1.Edges.First(e => e != rEdge1 && e != edge);
            var fromFace = (removedEdge1.OwnedFace == removedFace1) ? removedEdge1.OtherFace : removedEdge1.OwnedFace;
            var index = fromFace.Edges.IndexOf(removedEdge1);
            fromFace.Edges[index] = keepedge1;
            if (keepedge1.OwnedFace == removedFace1) keepedge1.OwnedFace = fromFace;
            else keepedge1.OtherFace = fromFace;
            // second on the "other side of edge"
            var keepedge2 = removedFace2.Edges.First(e => e != rEdge2 && e != edge);
            fromFace = (removedEdge2.OwnedFace == removedFace2) ? removedEdge2.OtherFace : removedEdge2.OwnedFace;
            index = fromFace.Edges.IndexOf(removedEdge2);
            fromFace.Edges[index] = keepedge2;
            if (keepedge2.OwnedFace == removedFace1) keepedge2.OwnedFace = fromFace;
            else keepedge2.OtherFace = fromFace;

            AdjustPositionOfKeptVertexAverage(keepVertex, removedVertex);
            // AdjustPositionOfKeptVertexFourPlane(keepVertex, new List<PolygonalFace> { keepedge1.OwnedFace, keepedge1.OtherFace, keepedge2.OwnedFace, keepedge2.OtherFace});
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
        }

        private static void AdjustPositionOfKeptVertexFourPlane(Vertex keepVertex, List<PolygonalFace> closestFaces)
        {
            throw new NotImplementedException();
        }

        private static void AdjustPositionOfKeptVertexAverage(Vertex keepVertex, Vertex removedVertex)
        {
            //average positions
            var newPosition = keepVertex.Position.add(removedVertex.Position);
            keepVertex.Position = newPosition.divide(2);
        }
    }
}