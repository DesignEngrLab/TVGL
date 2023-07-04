// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 06-27-2023
//
// Last Modified By : matth
// Last Modified On : 06-27-2023
// ***********************************************************************
// <copyright file="DetermineIntermediateVertex.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TVGL
{
    /// <summary>
    /// This portion of ModifyTessellation includes the functions to refine a solid, which means
    /// adding more elements to it. invoked during the opening of a tessellated solid from "disk", but the repair function
    /// may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        public static void FlipEdge(this Edge edge, TessellatedSolid ts)
        {
            var edgeIndex1 = edge.IndexInList;
            var oldFace1 = edge.OwnedFace;
            var oldEdge2 = oldFace1.OtherEdge(edge.From);
            var edgeIndex2 = oldEdge2.IndexInList;
            var matingFace2 = oldEdge2.GetMatingFace(oldFace1);
            var oldEdge3 = oldFace1.OtherEdge(edge.To);
            var edgeIndex3 = oldEdge3.IndexInList;
            var matingFace3 = oldEdge3.GetMatingFace(oldFace1);
            var oldFace2 = edge.OtherFace;
            var oldEdge4 = oldFace2.OtherEdge(edge.From);
            var edgeIndex4 = oldEdge4.IndexInList;
            var matingFace4 = oldEdge4.GetMatingFace(oldFace1);
            var oldEdge5 = oldFace2.OtherEdge(edge.To);
            var edgeIndex5 = oldEdge5.IndexInList;
            var matingFace5 = oldEdge5.GetMatingFace(oldFace1);

            edge.From.Faces.Remove(oldFace1);
            edge.From.Faces.Remove(oldFace2);
            edge.To.Faces.Remove(oldFace1);
            edge.To.Faces.Remove(oldFace2);

            var leftVertex = oldFace1.OtherVertex(edge.From, edge.To);
            var rightVertex = oldFace2.OtherVertex(edge.From, edge.To);
            leftVertex.Faces.Remove(oldFace1);
            rightVertex.Faces.Remove(oldFace2);


            var newFace1 = new TriangleFace(leftVertex, edge.From, rightVertex, true);
            ts.Faces[oldFace1.IndexInList] = newFace1;
            var newFace2 = new TriangleFace(leftVertex, rightVertex, edge.To, true);
            ts.Faces[oldFace2.IndexInList] = newFace2;

            ts.Edges[edgeIndex1] = new Edge(rightVertex, leftVertex, newFace1, newFace2, true);
            ts.Edges[edgeIndex2] = new Edge(leftVertex, edge.From, newFace1, matingFace3, true);
            ts.Edges[edgeIndex3] = new Edge(edge.From, rightVertex, newFace1, matingFace5, true);
            ts.Edges[edgeIndex4] = new Edge(rightVertex, edge.To, newFace2, matingFace4, true);
            ts.Edges[edgeIndex5] = new Edge(edge.To, leftVertex, newFace2, matingFace2, true);
        }


        public static bool CollapseEdge(this Edge edge, List<Edge> removeEdges)
        {
            return MergeVertexAndKill3EdgesAnd2Faces(edge.From, edge.To, edge.OwnedFace, edge.OtherFace,
                removeEdges);
        }

        public static bool MergeVertexAndKill3EdgesAnd2Faces(this Vertex removedVertex, Vertex keepVertex,
            TriangleFace removedFace1, TriangleFace removedFace2, List<Edge> removeEdges)
        {
            var vertexEnumerable = removedFace1.Vertices.Concat(removedFace2.Vertices)
                .Where(x => x != keepVertex && x != removedVertex).Distinct();
            var vertexEnumerator = vertexEnumerable.GetEnumerator();
            var otherVertex1 = vertexEnumerator.Current;
            vertexEnumerator.MoveNext();
            var otherVertex2 = vertexEnumerator.Current;
            Edge keepEdge1 = null;
            Edge keepEdge2 = null;

            removeEdges = new List<Edge>();
            foreach (var edge in removedFace1.Edges.Concat(removedFace2.Edges).Distinct())
            {
                if (edge.From == removedVertex || edge.To == removedVertex ||
                    (edge.From != keepVertex && edge.To != keepVertex))
                    removeEdges.Add(edge);
                else if (keepEdge1 == null) keepEdge1 = edge;
                else keepEdge2 = edge;
            }
            var otherEdgesOnTheKeepSide = keepVertex.Edges.Where(e => e != removeEdges[^1] && e != removeEdges[^2] &&
            e != removeEdges[^3] && e != keepEdge1 && e != keepEdge2).ToList();
            var otherEdgesOnTheRemoveSide = removedVertex.Edges.Where(e => e != removeEdges[^1] && e != removeEdges[^2] &&
            e != removeEdges[^3] && e != keepEdge1 && e != keepEdge2).ToList();
            if ( // this is a topologically important check. It ensures that the edge is not deleted if
                 // it serves an important role in ensuring the proper topology of the solid. Essentially, 
                 // if there is a common edge between the vertices that is not accounted for then it will end up being
                 // an edge that will start and end at the same vertex
                otherEdgesOnTheKeepSide.Select(e => e.OtherVertex(keepVertex))
                    .Intersect(otherEdgesOnTheRemoveSide.Select(e => e.OtherVertex(removedVertex)))
                    .Any())
                return false;

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
                face.ReplaceVertex(removedVertex, keepVertex);
            }
            // conversely keepVertex should forget about the edge and the remove faces
            keepVertex.Faces.Remove(removedFace1);
            keepVertex.Faces.Remove(removedFace2);
            otherVertex1.Faces.Remove(removedFace1);
            otherVertex2.Faces.Remove(removedFace2);
            for (int i = removeEdges.Count - 3; i < removeEdges.Count; i++)
            {
                var remEdge = removeEdges[i];
                keepVertex.Edges.Remove(remEdge);
                otherVertex1.Edges.Remove(remEdge);
                otherVertex2.Edges.Remove(remEdge);
                if (remEdge.OwnedFace == removedFace1 && remEdge.OtherFace == removedFace2 ||
                    remEdge.OwnedFace == removedFace2 && remEdge.OtherFace == removedFace1)
                    continue;
                var thisKeepVertex = remEdge.To == removedVertex ? remEdge.From : remEdge.To;
                var keepEdge = thisKeepVertex.Edges.First(e => e.To == keepVertex || e.From == keepVertex);
                var keepFaceOnRemEdge = (remEdge.OwnedFace == removedFace1 || remEdge.OwnedFace == removedFace2)
                    ? remEdge.OtherFace : remEdge.OwnedFace;
                keepFaceOnRemEdge.ReplaceEdge(remEdge, keepEdge);
            }

            keepVertex.Coordinates = DetermineIntermediateVertexPosition(keepVertex, removedVertex);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
            return true;
        }


    }
}
