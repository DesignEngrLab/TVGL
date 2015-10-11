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
    public static class SimplifyTessellation
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
            if (ts.Errors != null)
                Debug.WriteLine("** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).");

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
                if (CombineVerticesOfEdge(edge, out removedVertex, out removedEdge1, out removedEdge2, out removedFace1,
                    out removedFace2))
                {
                    removedEdges.Add(edge);
                    removedEdgesSorted.Add(edge.IndexInList,edge);
                    removedEdges.Add(removedEdge1);
                    removedEdgesSorted.Add(removedEdge1.IndexInList, removedEdge1);
                    removedEdges.Add(removedEdge2);
                    removedEdgesSorted.Add(removedEdge2.IndexInList, removedEdge2);
                    removedFaces.Add(removedFace1.IndexInList, removedFace1);
                    numberToRemove--;
                    removedFaces.Add(removedFace2.IndexInList, removedFace2);
                    numberToRemove--;
                    removedVertices.Add(removedVertex.IndexInList, removedVertex);
                }
            }
            ts.RemoveEdges(removedEdgesSorted.Keys.ToList());
            ts.RemoveFaces(removedFaces.Keys.ToList());
            ts.RemoveVertices(removedVertices.Keys.ToList());
        }

        public static void Simplify(this TessellatedSolid ts)
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
            if (ts.Errors != null)
                Debug.WriteLine("** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).");

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
                    if (CombineVerticesOfEdge(edge, out removedVertex, out removedEdge1, out removedEdge2,
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

        private static bool CombineVerticesOfEdge(Edge edge, out Vertex removedVertexOut, out Edge removedEdge1Out,
            out Edge removedEdge2Out, out PolygonalFace removedFace1, out PolygonalFace removedFace2)
        {
            var keepVertex = edge.To;
            var removedVertex = edge.From;
            removedFace1 = edge.OwnedFace;
            removedFace2 = edge.OtherFace;
            var removedEdge1 = removedFace1.OtherEdge(keepVertex);
            var removedEdge2 = removedFace2.OtherEdge(keepVertex);
            var keepEdge1 = removedFace1.OtherEdge(removedVertex);
            var keepEdge2 = removedFace2.OtherEdge(removedVertex);
            var otherEdgesOnTheToSide =
                keepVertex.Edges.Where(e => e != edge && e != keepEdge1 && e != keepEdge2).ToList();
            var otherEdgesOnTheFromSide =
                removedVertex.Edges.Where(e => e != edge && e != removedEdge1 && e != removedEdge2).ToList();
            if (
                otherEdgesOnTheToSide.Select(e => e.OtherVertex(keepVertex))
                    .Intersect(otherEdgesOnTheFromSide.Select(e => e.OtherVertex(removedVertex)))
                    .Any())
            {
                removedVertexOut = null;
                removedEdge1Out = null;
                removedEdge2Out = null;
                return false;
            }

            // move edges connected to removeVertex to the keepVertex and let keepVertex link back to these edges
            foreach (var e in otherEdgesOnTheFromSide)
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
            var farVertex = removedFace1.OtherVertex(edge);
            farVertex.Edges.Remove(removedEdge1);
            farVertex.Faces.Remove(removedFace1);
            farVertex = removedFace2.OtherVertex(edge);
            farVertex.Edges.Remove(removedEdge2);
            farVertex.Faces.Remove(removedFace2);
            // for the winged edges (removedEdge1 and removedEdge2) that are removed, connected their faces to 
            // the new edge
            // first on the "owned side of edge"
            var fromFace = (removedEdge1.OwnedFace == removedFace1) ? removedEdge1.OtherFace : removedEdge1.OwnedFace;
            var index = fromFace.Edges.IndexOf(removedEdge1);
            fromFace.Edges[index] = keepEdge1;
            if (keepEdge1.OwnedFace == removedFace1) keepEdge1.OwnedFace = fromFace;
            else keepEdge1.OtherFace = fromFace;
            // second on the "other side of edge"
            fromFace = (removedEdge2.OwnedFace == removedFace2) ? removedEdge2.OtherFace : removedEdge2.OwnedFace;
            index = fromFace.Edges.IndexOf(removedEdge2);
            fromFace.Edges[index] = keepEdge2;
            if (keepEdge2.OwnedFace == removedFace2) keepEdge2.OwnedFace = fromFace;
            else keepEdge2.OtherFace = fromFace;


            //AdjustPositionOfKeptVertexAverage(keepVertex, removedVertex);
            AdjustPositionOfKeptVertex(keepVertex, removedVertex, new List<PolygonalFace> { keepEdge1.OwnedFace, keepEdge1.OtherFace, keepEdge2.OwnedFace, keepEdge2.OtherFace });
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
            removedVertexOut = removedVertex;
            removedEdge1Out = removedEdge1;
            removedEdge2Out = removedEdge2;
            return true;
        }

        private static void AdjustPositionOfKeptVertex(Vertex keepVertex, Vertex removedVertex, List<PolygonalFace> closestFaces)
        {
            //average positions
            var newPosition = keepVertex.Position.add(removedVertex.Position);
            var radius = keepVertex.Position.subtract(removedVertex.Position).norm2() / 2.0;
            keepVertex.Position = newPosition.divide(2);
            return;
            var distances = closestFaces.Select(f => f.Normal.dotProduct(f.OtherEdge(keepVertex).To.Position)).ToList();
            var points = new double[4][];
            points[0] = MiscFunctions.PointCommonToThreePlanes(closestFaces[1].Normal, distances[1],
               closestFaces[2].Normal, distances[2], closestFaces[3].Normal, distances[3]);
            points[1] = MiscFunctions.PointCommonToThreePlanes(closestFaces[0].Normal, distances[0],
                closestFaces[2].Normal, distances[2], closestFaces[3].Normal, distances[3]);
            points[2] = MiscFunctions.PointCommonToThreePlanes(closestFaces[0].Normal, distances[0],
                closestFaces[1].Normal, distances[1], closestFaces[3].Normal, distances[3]);
            points[3] = MiscFunctions.PointCommonToThreePlanes(closestFaces[0].Normal, distances[0],
                closestFaces[1].Normal, distances[1], closestFaces[2].Normal, distances[2]);
            var pointSum = new double[3];
            var numValid = 0;
            for (int i = 0; i < 4; i++)
            {
                if (points[i].Contains(double.NaN)) continue;
                pointSum = pointSum.add(points[i]);
                numValid++;
            }
            if (numValid == 0) return;
            var targetPoint = pointSum.divide(numValid);
            if (targetPoint.subtract(keepVertex.Position).norm2() <= radius)
                keepVertex.Position = targetPoint;
            else
                keepVertex.Position = keepVertex.Position.add(targetPoint.subtract(keepVertex.Position).normalize().multiply(radius));
        }

        private static void AdjustPositionOfKeptVertexAverage(Vertex keepVertex, Vertex removedVertex)
        {
        }
    }
}