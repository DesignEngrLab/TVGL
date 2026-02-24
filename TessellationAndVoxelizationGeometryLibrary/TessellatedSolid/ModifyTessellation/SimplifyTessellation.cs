// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="SimplifyTessellation.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace TVGL
{
    /// <summary>
    /// This portion of ModifyTessellation includes the functions to simplify a tessellated solid.
    /// </summary>
    public static partial class ModifyTessellation
    {
        /// <summary>
        /// Simplifies the model by merging the eliminating edges that are closer together
        /// than double the shortest edge length
        /// </summary>
        /// <param name="ts">The ts.</param>
        public static void SimplifyFlatPatches(this TessellatedSolid ts)
        {
            //   throw new NotImplementedException();
            var edgesToRemove = new List<Edge>();
            var edgesToAdd = new List<Edge>();
            var facesToRemove = new List<TriangleFace>();
            var facesToAdd = new List<TriangleFace>();
            var verticesToRemove = new List<Vertex>();
            var flats = TVGL.MiscFunctions.FindFlats(ts.Faces, minNumberOfFacesPerFlat: 3);
            ts.Primitives ??= new List<PrimitiveSurface>();
            foreach (var flat in flats)
            {
                if (flat.InnerEdges.Count < flat.Faces.Count) continue;
                var newFaces = new List<TriangleFace>();
                var outerEdgeHashSet = new HashSet<Edge>(flat.OuterEdges);
                facesToRemove.AddRange(flat.Faces);
                edgesToRemove.AddRange(flat.InnerEdges);
                var innerVertices = new HashSet<Vertex>(flat.InnerEdges.Select(e => e.To));
                innerVertices.UnionWith(flat.InnerEdges.Select(e => e.From));
                innerVertices.RemoveWhere(v => outerEdgeHashSet.Overlaps(v.Edges));
                verticesToRemove.AddRange(innerVertices);
                var vertexLoops = flat.OuterEdges.MakeEdgePaths(true, new EdgePathLoopsAroundInputFaces(flat.Faces)).Select(ep => ep.GetVertices().ToList());
                var triangulatedList = vertexLoops.Triangulate(flat.Normal);
                var oldEdgeDictionary = flat.OuterEdges.ToDictionary(Edge.SetAndGetEdgeChecksum);
                Dictionary<long, Edge> newEdgeDictionary = new Dictionary<long, Edge>();
                foreach (var triangle in triangulatedList)
                {
                    var newFace = new TriangleFace(triangle, flat.Normal);
                    if (newFace.Area.IsNegligible() && newFace.Normal.IsNull()) continue;
                    newFaces.Add(newFace);
                    foreach (var fromVertex in newFace.Vertices)
                    {
                        var toVertex = newFace.NextVertexCCW(fromVertex);
                        var checksum = Edge.GetEdgeChecksum(fromVertex, toVertex);
                        if (oldEdgeDictionary.TryGetValue(checksum, out var edge))
                        {
                            //fix up old outer edge.
                            if (fromVertex == edge.From) edge.OwnedFace = newFace;
                            else edge.OtherFace = newFace;
                            newFace.AddEdge(edge);
                            oldEdgeDictionary.Remove(checksum);
                        }
                        else if (newEdgeDictionary.TryGetValue(checksum, out var newEdge))
                        {
                            //Finish creating edge.
                            newEdge.OtherFace = newFace;
                            newFace.AddEdge(newEdge);
                            newEdgeDictionary.Remove(checksum);
                            edgesToAdd.Add(newEdge);
                        }
                        else
                            newEdgeDictionary.Add(checksum, new Edge(fromVertex, toVertex, newFace, null, false, checksum));
                    }
                }
                ts.Primitives.Add(new Plane(newFaces));
            }
            ts.RemoveVertices(verticesToRemove);  //todo: check if the order of these five commands 
            ts.RemoveFaces(facesToRemove);        // matters. There may be an ordering that is more efficient
            ts.AddFaces(facesToAdd);
            ts.RemoveEdges(edgesToRemove);
            ts.AddEdges(edgesToAdd);
        }


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
                Log.Warning(
                    "** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).",
                    1);
            var sortedEdges = new UpdatablePriorityQueue<Edge, double>( ts.Edges.Select(e => (e, e.Length)));
            var removedEdges = new SortedSet<Edge>(new SortByIndexInList());
            var removedVertices = new SortedSet<Vertex>(new SortByIndexInList());
            var removedFaces = new SortedSet<TriangleFace>(new SortByIndexInList());

            var iterations = numberOfFaces > 0 ? (int)Math.Ceiling(numberOfFaces / 2.0) : numberOfFaces;
            while (iterations != 0 && sortedEdges.TryDequeue(out var edge, out _))
            {
                //naming conventions to ease the latter topological changes
                var removedVertex = edge.From;
                var keepVertex = edge.To;
                var leftFace = edge.OtherFace;
                var rightFace = edge.OwnedFace;
                iterations--; //now that we passed that test, we can be assured that the reduction will go through
                              // move the keepVertex
                if (MergeVertexAndKill3EdgesAnd2Faces(removedVertex, keepVertex, leftFace, rightFace, out var removeEdges, 
                    out var keepEdge1, out var keepEdge2))
                {
                    iterations--;
                    removedEdges.Add(removeEdges[0]);
                    removedEdges.Add(removeEdges[1]);
                    removedEdges.Add(removeEdges[2]);
                    sortedEdges.Remove(removeEdges[0]);
                    sortedEdges.Remove(removeEdges[1]);
                    sortedEdges.Remove(removeEdges[2]);
                    sortedEdges.UpdatePriority(keepEdge1, keepEdge1.Length);
                    sortedEdges.UpdatePriority(keepEdge2, keepEdge2.Length);
                    removedFaces.Add(leftFace);
                    removedFaces.Add(rightFace);
                    removedVertices.Add(removedVertex);
                }
            }
            ts.RemoveEdges(removedEdges.Select(e => e.IndexInList).ToList());
            ts.RemoveFaces(removedFaces.Select(f => f.IndexInList).ToList());
            ts.RemoveVertices(removedVertices.Select(v => v.IndexInList).ToList());
        }
    }
}