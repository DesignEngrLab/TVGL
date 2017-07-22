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
        /// Simplifies the model by merging the eliminating edges that are closer together
        /// than double the shortest edge length
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void Simplify(this TessellatedSolid ts)
        {
            Simplify(ts, ts.NumberOfFaces / 2, ts.Edges.Min(x => x.Length) * 2.0);
        }

        /// <summary>
        /// Simplifies the tessellation by removing the provided number of faces.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfFacesToRemove">The number of faces.</param>
        public static void Simplify(this TessellatedSolid ts, int numberOfFacesToRemove)
        {
            Simplify(ts, numberOfFacesToRemove, double.PositiveInfinity);
        }

        /// <summary>
        /// Simplifies the tessellation so that no edge are shorter than provided the minimum edge length.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="minLength">The minimum length.</param>
        public static void Simplify(this TessellatedSolid ts, double minLength)
        {
            Simplify(ts, -1, minLength);
        }

        /// <summary>        
        /// Simplifies the tessellation so that no edge are shorter than provided the minimum edge length
        /// or until the provided number of faces are removed - whichever comes first.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="numberOfFaces">The number of faces to remove.</param>
        /// <param name="minLength">The minimum length.</param>
        public static void Simplify(TessellatedSolid ts, int numberOfFaces, double minLength)
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
            var iterations = numberOfFaces > 0 ? (int)Math.Ceiling(numberOfFaces / 2.0) : numberOfFaces;
            while (iterations != 0 && edge.Length <= minLength)
            {
                sortedEdges.RemoveAt(0);
                // naming conventions to ease the latter topological changes
                var removedVertex = edge.From;
                var keepVertex = edge.To;
                var leftFace = edge.OtherFace;
                var rightFace = edge.OwnedFace;
                var leftRemoveEdge = leftFace.OtherEdge(keepVertex);
                var rightRemoveEdge = rightFace.OtherEdge(keepVertex);
                var leftKeepEdge = leftFace.OtherEdge(removedVertex);
                var rightKeepEdge = rightFace.OtherEdge(removedVertex);
                var leftFarVertex = leftFace.OtherVertex(edge);
                var rightFarVertex = rightFace.OtherVertex(edge);

                // this is a topologically important check. It ensures that the edge is not deleted if
                // it serves an important role in ensuring the proper topology of the solid
                var otherEdgesOnTheKeepSide = keepVertex.Edges.Where(e => e != edge && e != leftKeepEdge && e != rightKeepEdge).ToList();
                otherEdgesOnTheKeepSide.Remove(edge);
                var otherEdgesOnTheRemoveSide = removedVertex.Edges.Where(e => e != edge && e != leftRemoveEdge && e != rightRemoveEdge).ToList();
                otherEdgesOnTheRemoveSide.Remove(edge);
                if (leftFarVertex != rightFarVertex &&
                    !otherEdgesOnTheKeepSide.Select(e => e.OtherVertex(keepVertex))
                        .Intersect(otherEdgesOnTheRemoveSide.Select(e => e.OtherVertex(removedVertex)))
                        .Any())
                {
                    iterations--; //now that we passed that test, we can be assured that the reduction will go through
                    // move the keepVertex
                    var primitives = removedVertex.Faces.Select(f => f.BelongsToPrimitive).Distinct()
                        .Union(keepVertex.Faces.Select(f => f.BelongsToPrimitive));
                    double[] position;
                    if (DetermineIntermediateVertexPosition(removedVertex, keepVertex, out position, primitives))
                    {
                        keepVertex.Position = position;
                        // add and remove to the lists at the top of this method
                        removedEdges.Add(edge);
                        removedEdges.Add(leftRemoveEdge);
                        sortedEdges.Remove(leftRemoveEdge);
                        removedEdges.Add(rightRemoveEdge);
                        sortedEdges.Remove(rightRemoveEdge);
                        removedFaces.Add(leftFace);
                        removedFaces.Add(rightFace);
                        removedVertices.Add(removedVertex);

                        keepVertex.Faces.Remove(leftFace);
                        keepVertex.Faces.Remove(rightFace);
                        // the keepVertex's other faces need to be updated given the change in position of keepVertex
                        foreach (var face in keepVertex.Faces)
                            face.Update();
                        // remove the removedVertex from the faces and update their positions with the keepVertex
                        foreach (var face in removedVertex.Faces)
                        {
                            if (face == leftFace || face == rightFace) continue;
                            var index = face.VertexIndex(removedVertex);
                            face.Vertices[index] = keepVertex;
                            face.Update();
                            keepVertex.Faces.Add(face);
                        }

                        keepVertex.Edges.Remove(edge);
                        // update the edges since the keepVertex moved
                        foreach (var currentEdge in keepVertex.Edges)
                            if (currentEdge != leftKeepEdge && currentEdge != rightKeepEdge)
                                currentEdge.Update();
                        // transfer the edges from the removedVertex to the keepVertex
                        foreach (var transferEdge in removedVertex.Edges)
                        {
                            if (transferEdge == edge || transferEdge == leftRemoveEdge
                                || transferEdge == rightRemoveEdge) continue;
                            if (transferEdge.From == removedVertex) transferEdge.From = keepVertex;
                            else transferEdge.To = keepVertex;
                            transferEdge.Update();
                            keepVertex.Edges.Add(transferEdge);
                        }

                        leftFarVertex.Edges.Remove(leftRemoveEdge);
                        leftFarVertex.Faces.Remove(leftFace);
                        rightFarVertex.Edges.Remove(rightRemoveEdge);
                        rightFarVertex.Faces.Remove(rightFace);

                        var upperFace = leftRemoveEdge.OwnedFace == leftFace
                            ? leftRemoveEdge.OtherFace
                            : leftRemoveEdge.OwnedFace;
                        if (leftKeepEdge.OwnedFace == leftFace) leftKeepEdge.OwnedFace = upperFace;
                        else leftKeepEdge.OtherFace = upperFace;
                        upperFace.AddEdge(leftKeepEdge);
                        leftKeepEdge.Update();

                        upperFace = rightRemoveEdge.OwnedFace == rightFace
                            ? rightRemoveEdge.OtherFace
                            : rightRemoveEdge.OwnedFace;
                        if (rightKeepEdge.OwnedFace == rightFace) rightKeepEdge.OwnedFace = upperFace;
                        else rightKeepEdge.OtherFace = upperFace;
                        upperFace.AddEdge(rightKeepEdge);
                        rightKeepEdge.Update();
                    }
                }
                if (sortedEdges.Any()) edge = sortedEdges[0];
                else break;
            }
            ts.RemoveEdges(removedEdges.Select(e => e.IndexInList).ToList());
            ts.RemoveFaces(removedFaces.Select(f => f.IndexInList).ToList());
            ts.RemoveVertices(removedVertices.Select(v => v.IndexInList).ToList());
        }
    }
}