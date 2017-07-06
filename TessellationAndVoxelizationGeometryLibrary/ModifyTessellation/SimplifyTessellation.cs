// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="SimplifyTessellation.cs" company="Design Engineering Lab">
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
    ///  This portion of ModifyTessellation includes the functions to simplify a tessellated solid. 
    /// </summary>
    public static partial class ModifyTessellation
    {
        /// <summary>
        /// Simplifies the model by merging the vertices that are closest together
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Simplify(this TessellatedSolid ts)
        {
            SimplifyByTolerance(ts, ts.Edges.Min(x => x.Length) * 2.0);
        }
        /// <summary>
        /// Simplifies by the percentage provided. For example, is ts has 100 faces, then passing
        /// a 0.2 will reduce to 80 faces.
        /// </summary>
        /// <param name="ts">The tesselated solid.</param>
        /// <param name="percentageToReduceBy">The percentage to reduce by.</param>
        public static void SimplifyByPercentage(this TessellatedSolid ts, double percentageToReduceBy)
        {
            SimplifyToNFewerFaces(ts, (int)((1 - percentageToReduceBy) * ts.NumberOfFaces));
        }

        /// <summary>
        /// Simplifies the tessellation by remving the  provided number of faces.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfFacesToRemove">The number of faces.</param>
        public static void SimplifyToNFewerFaces(this TessellatedSolid ts, int numberOfFacesToRemove)
        {
            SimplifyBody(ts, numberOfFacesToRemove, 0.0);
        }

        /// <summary>
        /// Simplifies the tessellation so that no edge are shorter than provided the minimum edge length.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="minLength">The minimum length.</param>
        public static void SimplifyByMinEdgeLength(this TessellatedSolid ts, double minLength)
        {
            SimplifyBody(ts, -1, minLength);
        }

        private static void SimplifyBody(TessellatedSolid ts, int numberOfFaces, double minLength)
        {
            if (ts.Errors != null)
                Message.output(
                    "** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).",
                    1);
            var sortedEdges = new SortedSet<Edge>(ts.Edges, new SortByLength(true));
            var removedEdges = new HashSet<Edge>();
            var removedEdgesSorted = new SortedSet<Edge>(new SortByIndexInList());
            var removedVertices = new SortedSet<Vertex>(new SortByIndexInList());
            var removedFaces = new SortedSet<PolygonalFace>(new SortByIndexInList());

            var edge = sortedEdges.First();
            var iterations = numberOfFaces > 0 ? (int)Math.Ceiling(numberOfFaces / 2.0) : numberOfFaces;
            while (iterations != 0 && edge.Length <= minLength)
            {
                sortedEdges.Remove(edge);
                if (removedEdges.Contains(edge)) continue;
                Edge removedEdge1, removedEdge2;
                PolygonalFace removedFace1, removedFace2;
                Vertex removedVertex;
                if (CombineVerticesOfEdge(edge, out removedVertex, out removedEdge1, out removedEdge2,
                    out removedFace1,
                    out removedFace2))
                {
                    removedEdges.Add(edge);
                    removedEdgesSorted.Add(edge);
                    if (removedEdge1 != null)
                    {
                        removedEdges.Add(removedEdge1);
                        if (!removedEdgesSorted.Contains(removedEdge1))
                            removedEdgesSorted.Add(removedEdge1);
                    }
                    if (removedEdge2 != null)
                    {
                        removedEdges.Add(removedEdge2);
                        if (!removedEdgesSorted.Contains(removedEdge2))
                            removedEdgesSorted.Add(removedEdge2);
                    }
                    if (removedFace1 != null)
                    {
                        removedFaces.Add(removedFace1);
                        iterations--;
                    }
                    if (removedFace2 != null)
                    {
                        removedFaces.Add(removedFace2);
                        iterations--;
                    }
                    removedVertices.Add(removedVertex);
                }
            }
            ts.RemoveEdges(removedEdgesSorted.Select(e => e.IndexInList).ToList());
            ts.RemoveFaces(removedFaces.Select(f => f.IndexInList).ToList());
            ts.RemoveVertices(removedVertices.Select(v => v.IndexInList).ToList());
        }

        
        /// <summary>
        ///     Combines the vertices of edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="removedVertexOut">The removed vertex out.</param>
        /// <param name="removedEdge1Out">The removed edge1 out.</param>
        /// <param name="removedEdge2Out">The removed edge2 out.</param>
        /// <param name="removedFace1">The removed face1.</param>
        /// <param name="removedFace2">The removed face2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool CombineVerticesOfEdge(Edge edge, out Vertex removedVertexOut, out Edge removedEdge1Out,
            out Edge removedEdge2Out, out PolygonalFace removedFace1, out PolygonalFace removedFace2)
        {
            var keepVertex = edge.To; // arbitrarily choose the To as the keep vertex, but this may be swapped below
            var removedVertex = edge.From; // if the To has some missing faces
            if (keepVertex == removedVertex)
            {
                removedVertexOut = null;
                removedEdge2Out = removedEdge1Out = null;
                removedFace1 = removedFace2 = null;
                return false;
            }
            removedFace1 = edge.OwnedFace;
            removedFace2 = edge.OtherFace;
            var removedEdge1 = removedFace1 == null ? null : removedFace1.OtherEdge(keepVertex, true);
            var removedEdge2 = removedFace2 == null ? null : removedFace2.OtherEdge(keepVertex, true);
            var keepEdge1 = removedFace1 == null ? null : removedFace1.OtherEdge(removedVertex, true);
            var keepEdge2 = removedFace2 == null ? null : removedFace2.OtherEdge(removedVertex, true);
            if (removedEdge1 != null && removedEdge2 != null && (keepEdge1 == null || keepEdge2 == null))
            {
                // swap with removed.
                var tempVertex = keepVertex;
                keepVertex = removedVertex;
                removedVertex = tempVertex;
                var tempEdge = keepEdge1;
                keepEdge1 = removedEdge1;
                removedEdge1 = tempEdge;
                tempEdge = keepEdge2;
                keepEdge2 = removedEdge2;
                removedEdge2 = tempEdge;
            }
            var otherEdgesOnTheKeepSide =
                keepVertex.Edges.Where(e => e != edge && e != keepEdge1 && e != keepEdge2).ToList();
            var otherEdgesOnTheRemoveSide =
                removedVertex.Edges.Where(e => e != edge && e != removedEdge1 && e != removedEdge2).ToList();
            if ( // this is a topologically important check. It ensures that the edge is not deleted if
                 // it serves an important role in ensuring the proper topology of the solid
                otherEdgesOnTheKeepSide.Select(e => e.OtherVertex(keepVertex))
                    .Intersect(otherEdgesOnTheRemoveSide.Select(e => e.OtherVertex(removedVertex)))
                    .Any())
            {
                removedVertexOut = null;
                removedEdge2Out = removedEdge1Out = null;
                removedFace1 = removedFace2 = null;
                return false;
            }
            // move edges connected to removeVertex to the keepVertex and let keepVertex link back to these edges
            foreach (var e in otherEdgesOnTheRemoveSide)
            {
                keepVertex.Edges.Add(e);
                if (e.From == removedVertex) e.From = keepVertex;
                else e.To = keepVertex;
            }
            // move faces connected to removeVertex to the keepVertex and let keepVertex link back to these edges.
            foreach (var face in removedVertex.Faces)
            {
                if (face == removedFace1 || face == removedFace2) continue;
                keepVertex.Faces.Add(face);
                face.Vertices[face.Vertices.IndexOf(removedVertex)] = keepVertex;
            }
            // conversely keepVertex should forget about the edge and the remove faces
            keepVertex.Edges.Remove(edge);
            keepVertex.Faces.Remove(removedFace1);
            keepVertex.Faces.Remove(removedFace2);
            var farVertex = removedFace1 == null ? null : removedFace1.OtherVertex(edge, true);
            if (farVertex != null)
            {
                farVertex.Edges.Remove(removedEdge1);
                farVertex.Faces.Remove(removedFace1);
            }
            farVertex = removedFace2 == null ? null : removedFace2.OtherVertex(edge, true);
            if (farVertex != null)
            {
                farVertex.Edges.Remove(removedEdge2);
                farVertex.Faces.Remove(removedFace2);
            }
            // for the winged edges (removedEdge1 and removedEdge2) that are removed, connected their faces to 
            // the new edge
            // first on the "owned side of edge"
            var fromFace = removedEdge1 == null
                ? null
                : removedEdge1.OwnedFace == removedFace1 ? removedEdge1.OtherFace : removedEdge1.OwnedFace;
            if (fromFace != null)
            {
                var index = fromFace.Edges.IndexOf(removedEdge1);
                if (index >= 0 && index < fromFace.Edges.Count)
                    fromFace.Edges[index] = keepEdge1;
            }
            if (keepEdge1 != null && keepEdge1.OwnedFace == removedFace1) keepEdge1.OwnedFace = fromFace;
            else if (keepEdge1 != null) keepEdge1.OtherFace = fromFace;
            // second on the "other side of edge"
            fromFace = removedEdge2 == null
                ? null
                : removedEdge2.OwnedFace == removedFace2 ? removedEdge2.OtherFace : removedEdge2.OwnedFace;
            if (fromFace != null)
            {
                var index = fromFace.Edges.IndexOf(removedEdge2);
                if (index >= 0 && index < fromFace.Edges.Count)
                    fromFace.Edges[index] = keepEdge2;
            }
            if (keepEdge2 != null && keepEdge2.OwnedFace == removedFace2) keepEdge2.OwnedFace = fromFace;
            else if (keepEdge2 != null) keepEdge2.OtherFace = fromFace;
            keepVertex.Position = DetermineIntermediateVertexPosition(keepVertex, removedVertex);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
            removedVertexOut = removedVertex;
            removedEdge1Out = removedEdge1;
            removedEdge2Out = removedEdge2;
            return true;
        }


    }
}