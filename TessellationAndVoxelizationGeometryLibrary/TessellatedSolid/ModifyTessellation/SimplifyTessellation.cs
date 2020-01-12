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
using StarMathLib;

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
        public static void SimplifyFlatPatches(this TessellatedSolid ts)
        {
            //   throw new NotImplementedException();
            var edgesToRemove = new List<Edge>();
            var edgesToAdd = new List<Edge>();
            var facesToRemove = new List<PolygonalFace>();
            var facesToAdd = new List<PolygonalFace>();
            var verticesToRemove = new List<Vertex>();
            var flats = TVGL.MiscFunctions.FindFlats(ts.Faces);
            if (ts.Primitives == null) ts.Primitives = new List<PrimitiveSurface>();
            foreach (var flat in flats)
            {
                if (flat.InnerEdges.Count < flat.Faces.Count) continue;
                var newFaces = new List<PolygonalFace>();
                var outerEdgeHashSet = new HashSet<Edge>(flat.OuterEdges);
                facesToRemove.AddRange(flat.Faces);
                edgesToRemove.AddRange(flat.InnerEdges);
                var innerVertices = new HashSet<Vertex>(flat.InnerEdges.Select(e => e.To));
                innerVertices.UnionWith(flat.InnerEdges.Select(e => e.From));
                innerVertices.RemoveWhere(v => outerEdgeHashSet.Overlaps(v.Edges));
                verticesToRemove.AddRange(innerVertices);
                var vertexLoops = OrganizeIntoLoop(flat.OuterEdges, flat.Normal);
                List<List<Vertex[]>> triangulatedListofLists = TriangulatePolygon.Run(new[] { vertexLoops }, flat.Normal);
                var triangulatedList = triangulatedListofLists.SelectMany(tl => tl).ToList();
                var oldEdgeDictionary = flat.OuterEdges.ToDictionary(TessellatedSolid.SetAndGetEdgeChecksum);
                Dictionary<long, Edge> newEdgeDictionary = new Dictionary<long, Edge>();
                foreach (var triangle in triangulatedList)
                {
                    var newFace = new PolygonalFace(triangle, flat.Normal);
                    if (newFace.Area.IsNegligible() && newFace.Normal.Any(double.IsNaN)) continue;
                    newFaces.Add(newFace);
                    for (var j = 0; j < 3; j++)
                    {
                        var fromVertex = newFace.Vertices[j];
                        var toVertex = newFace.NextVertexCCW(fromVertex);
                        var checksum = TessellatedSolid.GetEdgeChecksum(fromVertex, toVertex);
                        if (oldEdgeDictionary.ContainsKey(checksum))
                        {
                            //fix up old outer edge.
                            var edge = oldEdgeDictionary[checksum];
                            if (fromVertex == edge.From) edge.OwnedFace = newFace;
                            else edge.OtherFace = newFace;
                            newFace.AddEdge(edge);
                            oldEdgeDictionary.Remove(checksum);
                        }
                        else if (newEdgeDictionary.ContainsKey(checksum))
                        {
                            //Finish creating edge.
                            var newEdge = newEdgeDictionary[checksum];
                            newEdge.OtherFace = newFace;
                            newFace.AddEdge(newEdge);
                            newEdgeDictionary.Remove(checksum);
                            edgesToAdd.Add(newEdge);
                        }
                        else
                            newEdgeDictionary.Add(checksum, new Edge(fromVertex, toVertex, newFace, null, false, checksum));
                    }
                }
                ts.Primitives.Add(new Flat(newFaces));
            }
            ts.RemoveVertices(verticesToRemove);  //todo: check if the order of these five commands 
            ts.RemoveFaces(facesToRemove);        // matters. There may be an ordering that is more efficient
            ts.AddFaces(facesToAdd);
            ts.RemoveEdges(edgesToRemove);
            ts.AddEdges(edgesToAdd);
        }


        internal static List<Vertex> OrganizeIntoLoop(List<Edge> singleSidedEdges, double[] normal)
        {
            var edgesHashSet = new HashSet<Edge>(singleSidedEdges);
            var loop = new List<Vertex>();
            var currentEdge = edgesHashSet.First();
            Vertex startVertex, currentVertex;
            if (normal.dotProduct(currentEdge.OwnedFace.Normal,3).IsPracticallySame(1))
            {
                startVertex = currentEdge.From;
                currentVertex = currentEdge.To;
            }
            else
            {
                startVertex = currentEdge.To;
                currentVertex = currentEdge.From;
            }
            edgesHashSet.Remove(currentEdge);
            loop.Add(startVertex);
            loop.Add(currentVertex);
            while (edgesHashSet.Any())
            {
                if (startVertex == currentVertex) return loop;
                var possibleNextEdges = currentVertex.Edges.Where(e => e != currentEdge && edgesHashSet.Contains(e));
                if (!possibleNextEdges.Any()) throw new Exception();
                var lastEdge = currentEdge;
                currentEdge = (possibleNextEdges.Count() == 1) ? possibleNextEdges.First()
                    : pickBestEdge(possibleNextEdges, currentEdge.Vector, normal);
                currentVertex = currentEdge.OtherVertex(currentVertex);
                loop.Add(currentVertex);
                edgesHashSet.Remove(currentEdge);
            }
            throw new Exception();
        }


        private static Edge pickBestEdge(IEnumerable<Edge> possibleNextEdges, double[] refEdge, double[] normal)
        {
            var unitRefEdge = refEdge.normalize(3);
            var min = 2.0;
            Edge bestEdge = null;
            foreach (var candEdge in possibleNextEdges)
            {
                var unitCandEdge = candEdge.Vector.normalize(3);
                var cross = unitRefEdge.crossProduct(unitCandEdge);
                var temp = cross.dotProduct(normal, 3);
                if (min > temp)
                {
                    min = temp;
                    bestEdge = candEdge;
                }
            }
            return bestEdge;
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
                Message.output(
                    "** The model should be free of errors before running this routine (run TessellatedSolid.Repair()).",
                    1);
            var sortedEdges = ts.Edges.OrderBy(e => e.Length).ToList();
            var removedEdges = new SortedSet<Edge>(new SortByIndexInList());
            var removedVertices = new SortedSet<Vertex>(new SortByIndexInList());
            var removedFaces = new SortedSet<PolygonalFace>(new SortByIndexInList());

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
                    keepVertex.Position = DetermineIntermediateVertexPosition(removedVertex, keepVertex);
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
                        var index = face.Vertices.IndexOf(removedVertex);
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
                        if (transferEdge == leftRemoveEdge || transferEdge == rightRemoveEdge) continue;
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
                if (sortedEdges.Any()) edge = sortedEdges[0];
                else break;
            }
            ts.RemoveEdges(removedEdges.Select(e => e.IndexInList).ToList());
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