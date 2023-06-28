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
using System;
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
        public static void FlipEdge(this Edge edge, TessellatedSolid ts)
        {
            var edgeIndex1 = edge.IndexInList;
            var oldFace1 = edge.OwnedFace;
            var oldEdge2 = oldFace1.OtherEdge(edge.From);
            var edgeIndex2 = oldEdge2.IndexInList;
            var matingFace2 = oldEdge2.OwnedFace == oldFace1 ? oldEdge2.OtherFace : oldEdge2.OwnedFace;
            var oldEdge3 = oldFace1.OtherEdge(edge.To);
            var edgeIndex3 = oldEdge3.IndexInList;
            var matingFace3 = oldEdge3.OwnedFace == oldFace1 ? oldEdge3.OtherFace : oldEdge3.OwnedFace;
            var oldFace2 = edge.OtherFace;
            var oldEdge4 = oldFace2.OtherEdge(edge.From);
            var edgeIndex4 = oldEdge4.IndexInList;
            var matingFace4 = oldEdge4.OwnedFace == oldFace1 ? oldEdge4.OtherFace : oldEdge4.OwnedFace;
            var oldEdge5 = oldFace2.OtherEdge(edge.To);
            var edgeIndex5 = oldEdge5.IndexInList;
            var matingFace5 = oldEdge5.OwnedFace == oldFace1 ? oldEdge5.OtherFace : oldEdge5.OwnedFace;

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
    }
}