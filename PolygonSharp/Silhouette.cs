// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;



namespace PolygonSharp
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        /// <summary>
        /// Creates the silhouette of the solid in the direction provided. 
        /// </summary>
        /// <param name="tessellatedSolid">The tessellated solid.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>Polygon.</returns>
        public static Polygon CreateSilhouette(this TessellatedSolid tessellatedSolid, Vector3 direction)
        {
            direction = direction.Normalize();
            var unvisitedFaces = tessellatedSolid.Faces.ToHashSet();
            tessellatedSolid.MakeEdgesIfNonExistent();
            var transform = direction.TransformToXYPlane(out _);
            var polygons = new List<Polygon>();

            while (true)
            {
                PolygonalFace startingFace = null;
                var dot = 0.0;
                foreach (var face in unvisitedFaces)
                {
                    // get a face that does not have a dot product orthogonal to the direction
                    // notice that IsNegligible is used with the dotTolerance specified above
                    dot = face.Normal.Dot(direction);
                    if (!dot.IsNegligible(Constants.SameFaceNormalDotTolerance))  
                    {
                        startingFace = face;
                        break;
                    }
                }
                if (startingFace == null) break;  // the only way to exit the loop is here in the midst of the loop, hence
                // the use of the while (true) above
                unvisitedFaces.Remove(startingFace); //remove this from unvisitedFaces
                var outerEdges = GetOuterEdgesOfContiguousPatch(unvisitedFaces, direction, MathF.Sign(dot), startingFace);
                // first we get the list of outerEdges of the patch of faces ("GetOuterEdgesOfContiguousPatch") in the same sense as the
                // startingFace with the direction (that's the easy part), then we arrange those outerEdges into polygons in the 
                // function in "ArrangeOuterEdgesIntoPolygon".
                polygons.AddRange(ArrangeOuterEdgesIntoPolygon(outerEdges, dot > 0, transform));
            }
            //Presenter.ShowAndHang(polygons);
            return polygons.UnionPolygons(PolygonCollection.PolygonWithHoles).LargestPolygon();
        }

        /// <summary>
        /// Gets the outer edges of contiguous patch.
        /// </summary>
        /// <param name="visitedFaces">The face hash.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sign">The sign.</param>
        /// <param name="startingFace">The starting face.</param>
        /// <returns>Dictionary&lt;Edge, System.Boolean&gt;.</returns>
        private static Dictionary<Edge, bool> GetOuterEdgesOfContiguousPatch(HashSet<PolygonalFace> visitedFaces, Vector3 direction, double sign, PolygonalFace startingFace)
        {
            // the returned dictionary includes the Edges and a boolean telling us if the included face of the patch is the owner of the edge (true)
            // or the "other face". this is used in providing the proper direction for the edge in the next function
            var outerEdges = new Dictionary<Edge, bool>();
            // this function essentially performs a depth-first search from the provided starting faces
            var stack = new Stack<PolygonalFace>();
            stack.Push(startingFace);
            while (stack.Any())
            {
                var current = stack.Pop();
                foreach (var edge in current.Edges)
                {
                    // this is confusing and subtle. If the outerEdges already includes this edge, then the opposing face is in the patch,
                    // we want to be sure not to continue the tree in this direction, but we also need to remove the edge from outer since 
                    // it is now interior to the patch
                    if (outerEdges.ContainsKey(edge)) outerEdges.Remove(edge);
                    else
                    {
                        var currentOwnsEdge = edge.OwnedFace == current; //the value of the dictionary is this boolean
                        outerEdges.Add(edge, currentOwnsEdge);
                        var neighbor = currentOwnsEdge ? edge.OtherFace : edge.OwnedFace; // get the opposing face
                        if (neighbor != null && sign * neighbor.Normal.Dot(direction) >= 0 && visitedFaces.Contains(neighbor))
                        {  // push the opposing face onto the stack if it has the proper normal direction and it has not be visited yet
                            stack.Push(neighbor);
                            visitedFaces.Remove(neighbor);
                        }
                    }
                }
            }
            return outerEdges;
        }

        /// <summary>
        /// Adds the hole to larger postive polygon as described in the comment immediately above.
        /// </summary>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="hole">The hole.</param>
        /// <param name="tolerance">The tolerance.</param>
        private static void AddHoleToLargerPostivePolygon(List<Polygon> positivePolygons, Polygon hole)
        {
            Polygon enclosingPolygon = null;
            foreach (var poly in positivePolygons)
            {
                if (!poly.HasABoundingBoxThatEncompasses(hole)) continue;
                var interaction = poly.GetPolygonInteraction(hole);
                if (interaction.Relationship == PolygonRelationship.BInsideA &&
                    interaction.GetRelationships(hole).Skip(1).All(r => r.Item1 == PolyRelInternal.Separated))
                {
                    enclosingPolygon = poly;
                    break;
                }
            }
            enclosingPolygon?.AddInnerPolygon(hole);
        }

        /// <summary>
        /// Chooses the best next edge when there are multiple viable edges. The key idea is to choose the edge that is most inline with the current direction.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="edgeSign">if set to <c>true</c> [edge sign].</param>
        /// <param name="viableEdges">The viable edges.</param>
        /// <param name="transform">The transform.</param>
        /// <returns>KeyValuePair&lt;Edge, System.Boolean&gt;.</returns>
        private static KeyValuePair<Edge, bool> ChooseBestNextEdge(Edge current, bool edgeSign, List<KeyValuePair<Edge, bool>> viableEdges, Matrix4x4 transform)
        {
            var edgesScores = new double[viableEdges.Count];
            var currentUnitDirection = current.Vector.Normalize().Multiply(transform);
            if (!edgeSign) currentUnitDirection *= -1;
            for (int i = 0; i < viableEdges.Count; i++)
            {
                var direction = viableEdges[i].Key.Vector.Normalize().Multiply(transform);
                if (!viableEdges[i].Value) direction *= -1;
                edgesScores[i] = currentUnitDirection.Dot(direction);
            }
            var bestEdgeIndex = -1;
            var bestScore = double.NegativeInfinity;
            for (int i = 0; i < viableEdges.Count; i++)
            {
                if (edgesScores[i] > bestScore)
                {
                    bestScore = edgesScores[i];
                    bestEdgeIndex = i;
                }
            }
            return viableEdges[bestEdgeIndex];
        }
    }
}
