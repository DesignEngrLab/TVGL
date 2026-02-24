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
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// This portion of ModifyTessellation includes the functions to refine a solid, which means
    /// adding more elements to it. invoked during the opening of a tessellated solid from "disk", but the repair function
    /// may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        public static void FlipEdge(this Edge edge, TessellatedSolid ts, out Edge newEdge)
        {
            var oldOwnedFace = edge.OwnedFace;
            var oldOtherFace = edge.OtherFace;
            // make a bunch of variables for the four edges around this patch
            var borderEdge1 = oldOwnedFace.OtherEdge(edge.From);
            var replaceOwned1 = borderEdge1.OwnedFace == oldOwnedFace;
            var borderEdge2 = oldOwnedFace.OtherEdge(edge.To);
            var replaceOwned2 = borderEdge2.OwnedFace == oldOwnedFace;
            var borderEdge3 = oldOtherFace.OtherEdge(edge.From);
            var replaceOwned3 = borderEdge3.OwnedFace == oldOtherFace;
            var borderEdge4 = oldOtherFace.OtherEdge(edge.To);
            var replaceOwned4 = borderEdge4.OwnedFace == oldOtherFace;

            // remove references in the vertices to the 2 old faces (that we're about to delete)
            edge.From.Faces.Remove(oldOwnedFace);
            edge.From.Faces.Remove(oldOtherFace);
            edge.To.Faces.Remove(oldOwnedFace);
            edge.To.Faces.Remove(oldOtherFace);
            var oppOldOwnedVertex = oldOwnedFace.OtherVertex(edge.From, edge.To); // the vertex on the old owned face
            // that is opposite (opp) the edge that we are flipping
            oppOldOwnedVertex.Faces.Remove(oldOwnedFace);
            var oppOldOtherVertex = oldOtherFace.OtherVertex(edge.From, edge.To);
            oppOldOtherVertex.Faces.Remove(oldOtherFace);

            // now make the two new faces
            var newFace1 = new TriangleFace(oppOldOwnedVertex, edge.From, oppOldOtherVertex, true);
            ts.Faces[oldOwnedFace.IndexInList] = newFace1;
            newFace1.IndexInList = oldOwnedFace.IndexInList;
            var newFace2 = new TriangleFace(oppOldOwnedVertex, oppOldOtherVertex, edge.To, true);
            ts.Faces[oldOtherFace.IndexInList] = newFace2;
            newFace2.IndexInList = oldOtherFace.IndexInList;
            var primitive = oldOtherFace.BelongsToPrimitive == oldOwnedFace.BelongsToPrimitive
                ? oldOwnedFace.BelongsToPrimitive
                : oldOwnedFace.Area > oldOtherFace.Area ? oldOwnedFace.BelongsToPrimitive :
                oldOtherFace.BelongsToPrimitive;
            newFace1.BelongsToPrimitive = primitive;
            newFace2.BelongsToPrimitive = primitive;

            // now fix the edges up
            newEdge = new Edge(oppOldOtherVertex, oppOldOwnedVertex, newFace1, newFace2, true);
            ts.Edges[edge.IndexInList] = newEdge;
            newEdge.IndexInList = edge.IndexInList;
            if (replaceOwned1) borderEdge1.OwnedFace = newFace2;
            else borderEdge1.OtherFace = newFace2;
            newFace2.AddEdge(borderEdge1);
            if (replaceOwned2) borderEdge2.OwnedFace = newFace1;
            else borderEdge2.OtherFace = newFace1;
            newFace1.AddEdge(borderEdge2);
            if (replaceOwned3) borderEdge3.OwnedFace = newFace2;
            else borderEdge3.OtherFace = newFace2;
            newFace2.AddEdge(borderEdge3);
            if (replaceOwned4) borderEdge4.OwnedFace = newFace1;
            else borderEdge4.OtherFace = newFace1;
            newFace1.AddEdge(borderEdge4);
            if (primitive != null)
            {
                primitive.AddFace(newFace1);
                primitive.AddFace(newFace2);
            }
        }


        public static bool CollapseEdgeAndKill2MoreEdgesAnd2Faces(this Edge edge, out List<Edge> removeEdges,
            out Edge keepEdge1, out Edge keepEdge2)
        {
            return MergeVertexAndKill3EdgesAnd2Faces(edge.From, edge.To, edge.OwnedFace, edge.OtherFace,
                out removeEdges, out keepEdge1, out keepEdge2);
        }

        public static bool MergeVertexAndKill3EdgesAnd2Faces(this Vertex removedVertex, Vertex keepVertex,
            TriangleFace removedFace1, TriangleFace removedFace2, out List<Edge> removeEdges, out Edge keepEdge1, out Edge keepEdge2)
        {
            var vertexEnumerable = removedFace1.Vertices.Concat(removedFace2.Vertices)
                .Where(x => x != keepVertex && x != removedVertex).Distinct();
            var vertexEnumerator = vertexEnumerable.GetEnumerator();
            vertexEnumerator.MoveNext();
            var otherVertex1 = vertexEnumerator.Current;
            vertexEnumerator.MoveNext();
            var otherVertex2 = vertexEnumerator.Current;
            // Use local variables instead of out parameters
            Edge localKeepEdge1 = null;
            Edge localKeepEdge2 = null;

            // the next loop assigns the five edges that are effected to the appropriate variables
            // no effect on the solid yet. Just getting the variables ready: removeEdges[0], 
            // removeEdges[1], removeEdges[2], keepEdge1, keepEdge2 
            var removeEdgesLocal = new List<Edge>();
            foreach (var edge in removedFace1.Edges.Concat(removedFace2.Edges).Distinct())
            {
                if (edge.From == removedVertex || edge.To == removedVertex ||
                    (edge.From != keepVertex && edge.To != keepVertex))
                    removeEdgesLocal.Add(edge);
                else if (localKeepEdge1 == null) localKeepEdge1 = edge;
                else keepEdge2 = edge;
            }
            // having established the five edges, we can now check to see if the edge is topologically important
            var otherEdgesOnTheKeepSide = keepVertex.Edges.Where(e => e != removeEdgesLocal[^1] && e != removeEdgesLocal[^2] &&
            e != removeEdgesLocal[^3] && e != localKeepEdge1 && e != localKeepEdge2).ToList();
            var otherEdgesOnTheRemoveSide = removedVertex.Edges.Where(e => e != removeEdgesLocal[^1] && e != removeEdgesLocal[^2] &&
            e != removeEdgesLocal[^3] && e != localKeepEdge1 && e != localKeepEdge2).ToList();
            if ( // this is a topologically important check. It ensures that the edge is not deleted if
                 // it serves an important role in ensuring the proper topology of the solid. Essentially, 
                 // if there is a common edge between the vertices that is not accounted for then it will end up being
                 // an edge that will start and end at the same vertex
                otherEdgesOnTheKeepSide.Select(e => e.OtherVertex(keepVertex))
                    .Intersect(otherEdgesOnTheRemoveSide.Select(e => e.OtherVertex(removedVertex)))
                    .Any())
            {
                removeEdges = null;
                keepEdge1 = null;
                keepEdge2 = null;
                return false;
            }
            // Assign local variables to out parameters
            removeEdges = removeEdgesLocal;
            keepEdge1 = localKeepEdge1;
            keepEdge2 = localKeepEdge2;
            // move edges connected to removeVertex to the keepVertex and let keepVertex link back to these edges
            foreach (var e in otherEdgesOnTheRemoveSide)
            {
                keepVertex.Edges.Add(e);
                if (e.From == removedVertex) e.From = keepVertex;
                else e.To = keepVertex;
            }
            // move faces connected to removeVertex to the keepVertex and let keepVertex link back to these edges.
            foreach (var face in removedVertex.Faces.ToList()) // because the "ReplaceVertex" function will alter the
                                                               //list in the removedVertex.Faces collection, we make a copy first (ToList is used here)
            {
                if (face == removedFace1 || face == removedFace2) continue;
                //keepVertex.Faces.Add(face);
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
            if (removedFace1.BelongsToPrimitive != null && removedFace2.BelongsToPrimitive != null &&
                removedFace1.BelongsToPrimitive == removedFace2.BelongsToPrimitive)
                keepVertex.Coordinates = DetermineIntermediateVertexPosition(keepVertex.Coordinates, removedVertex.Coordinates,
                    removedFace1.BelongsToPrimitive);
            keepVertex.Coordinates = DetermineIntermediateVertexPosition(keepVertex.Coordinates, removedVertex.Coordinates);
            foreach (var e in keepVertex.Edges)
                e.Update();
            foreach (var f in keepVertex.Faces)
                f.Update();
            return true;
        }
    }
}
