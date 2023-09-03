// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Silhouette.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
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
                TriangleFace startingFace = null;
                var dot = 0.0;
                foreach (var face in unvisitedFaces)
                {
                    // get a face that does not have a dot product orthogonal to the direction
                    // notice that IsNegligible is used with the dotTolerance specified above
                    dot = face.Normal.Dot(direction);
                    if (!dot.IsNegligible(Constants.DotToleranceOrthogonal))  
                    {
                        startingFace = face;
                        break;
                    }
                }
                if (startingFace == null) break;  // the only way to exit the loop is here in the midst of the loop, hence
                // the use of the while (true) above
                unvisitedFaces.Remove(startingFace); //remove this from unvisitedFaces
                var outerEdges = GetOuterEdgesOfContiguousPatch(unvisitedFaces, direction, Math.Sign(dot), startingFace);
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
        private static Dictionary<Edge, bool> GetOuterEdgesOfContiguousPatch(HashSet<TriangleFace> visitedFaces, Vector3 direction, double sign, TriangleFace startingFace)
        {
            // the returned dictionary includes the Edges and a boolean telling us if the included face of the patch is the owner of the edge (true)
            // or the "other face". this is used in providing the proper direction for the edge in the next function
            var outerEdges = new Dictionary<Edge, bool>();
            // this function essentially performs a depth-first search from the provided starting faces
            var stack = new Stack<TriangleFace>();
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
        /// Arranges the outer edges into polygons.
        /// </summary>
        /// <param name="outerEdges">The outer edges.</param>
        /// <param name="isPositive">if set to <c>true</c> [positive].</param>
        /// <param name="transform">The transform.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> ArrangeOuterEdgesIntoPolygon(Dictionary<Edge, bool> outerEdges, bool isPositive, Matrix4x4 transform)
        {
            var polygons = new List<Polygon>();
            var negativePolygons = new List<Polygon>();
            while (outerEdges.Any())
            {   // outer while loop begins a new polygon with the outerEdges. There may easily be multiple polygons in the outer edges as it can represent
                // holes within the polygon
                var polyCoordinates = new List<Vector2>();
                #region build the loop forwards
                var start = outerEdges.First(); // the separate "start" variable is only in case we need to work backwards. see 
                var current = start;
                var startVertex = current.Value == isPositive ? current.Key.From : current.Key.To; //used to define the end of the loop - when you get back to the startVertex
                var successfulLoop = false;
                while (outerEdges.Any())
                {   // inner loop adds to the current polygon
                    outerEdges.Remove(current.Key);
                    if (current.Value == isPositive) //if ownedEdge and positive, then edge is in the right direction. OR if not-owned and negative, then also in the proper
                        // direction.
                        polyCoordinates.Add(current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                    else polyCoordinates.Add(current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                    // set the nextVertex and check if it is the same as the startVertex
                    var nextVertex = current.Value == isPositive ? current.Key.To : current.Key.From;
                    if (nextVertex == startVertex || nextVertex.ConvertTo2DCoordinates(transform).IsPracticallySame(polyCoordinates[0]))
                    {
                        successfulLoop = true;
                        break;
                    }
                    // the viable edges should hopefully be only one, but to be robust - let us find all the edges that emanate from the 
                    // nextVertex and are in the collection of outerEdges
                    var viableEdges = new List<KeyValuePair<Edge, bool>>();
                    if (outerEdges.Any())
                    {
                        foreach (var edge in nextVertex.Edges)
                        {
                            if (edge == current.Key) continue;
                            if (outerEdges.TryGetValue(edge, out var thisDir))
                                viableEdges.Add(new KeyValuePair<Edge, bool>(edge, thisDir));
                        }
                    }
                    if (viableEdges.Count == 1) current = viableEdges[0];
                    else break;//if (viableEdges.Count == 0) 
                               //current = new KeyValuePair<Edge, bool>(null, false);
                               // when there are more than one, we can try "ChooseBestNextEdge", but it has been found
                               // that it works better to just move to the backwards direction
                               //else current = ChooseBestNextEdge(current.Key, current.Value, viableEdges, transform);
                }
                #endregion
                #region if you get stuch, then work backwards
                // if the loop was unsuccessful in the forward direction, work backwards. This code is similar to the above loop. Note that instead
                // of "polyCoordinates.Add", we use "polyCoordinates.Insert(0,"
                if (!successfulLoop)
                {
                    startVertex = current.Value == isPositive ? current.Key.From : current.Key.To; //assign startVertex to the last one added above
                    current = start; //assign current back to the start
                    var nextVertex = current.Value == isPositive ? current.Key.From : current.Key.To; // notice that the condition for traversing the edges
                    // is switched in this loop
                    while (outerEdges.Any())
                    {   // this loop is similar to the above but the code has been staggered so that viable edges are checked first - since
                        // the last edge has been added already
                        var viableEdges = new List<KeyValuePair<Edge, bool>>();
                        foreach (var edge in nextVertex.Edges)
                        {
                            if (edge == current.Key) continue;
                            if (outerEdges.TryGetValue(edge, out var thisDir))
                                viableEdges.Add(new KeyValuePair<Edge, bool>(edge, thisDir));
                        }
                        if (viableEdges.Count == 1) current = viableEdges[0];
                        else if (viableEdges.Count > 1)
                            current = ChooseBestNextEdge(current.Key, current.Value, viableEdges, transform);
                        else break;

                        outerEdges.Remove(current.Key);
                        if (current.Value != isPositive)
                            polyCoordinates.Insert(0, current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                        else polyCoordinates.Insert(0, current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                        nextVertex = current.Value == isPositive ? current.Key.From : current.Key.To;

                        if (nextVertex == startVertex || nextVertex.ConvertTo2DCoordinates(transform).IsPracticallySame(polyCoordinates[^1]))
                        {
                            successfulLoop = true;
                            break;
                        }
                    }
                }
                #endregion
                #region handle unsuccessful loops
                // if the loop is still not successfully closed - what should we do? I'm open to suggestions, but what I ended
                // up with is to make the decision to keep or discard based on how far apart the first and last vertices are.
                // this is done by find the area enclosed by the current edges. If the additional edge to close the polygon 
                // represents a 25% change in area (or whatever the const "closeTheLoopAreaFraction" is set to), then just discard
                // it.
                const double closeTheLoopAreaFraction = 0.25;
                if (!successfulLoop)
                {
                    var center = polyCoordinates.Aggregate((result, coord) => result + coord) / polyCoordinates.Count;
                    var area = 0.0;
                    for (int i = 1; i < polyCoordinates.Count; i++)
                        area += (polyCoordinates[i - 1] - center).Cross(polyCoordinates[i] - center);
                    // if closing the polygon is a substantial part of the area then don't include it
                    var closingArea = (polyCoordinates[^1] - center).Cross(polyCoordinates[0] - center);
                    if (Math.Abs(closingArea / area) > closeTheLoopAreaFraction)
                    {
                        //Presenter.ShowAndHang(polyCoordinates);
                        polyCoordinates.Clear();
                    }
                }
                #endregion
                if (polyCoordinates.Count > 2)
                {
                    var xDim = polyCoordinates.Max(c => c.X) - polyCoordinates.Min(c => c.X);
                    var yDim = polyCoordinates.Max(c => c.Y) - polyCoordinates.Min(c => c.Y);
                    var tolerance = Math.Min(xDim, yDim) * Constants.PolygonSameTolerance;
                    var newPolygons = new Polygon(polyCoordinates.SimplifyMinLengthToNewList(tolerance)).RemoveSelfIntersections(ResultType.BothPermitted);
                    // make the coordinates into polygons. Simplify and remove self intersections. 
                    foreach (var newPolygon in newPolygons)
                    {
                        if (newPolygon.IsPositive) polygons.Add(newPolygon);
                        else negativePolygons.Add(newPolygon);
                    }
                }
            }
            var areaTolerance = polygons.Sum(p=>p.Area) * Constants.BaseTolerance;
            for (int i = polygons.Count - 1; i >= 0; i--)
            {   // before we return, we cycle over the generated polygons and remove any that are too small as well as separate
                // out other negative polygons
                var polygon = polygons[i];
                if (polygon.Area.IsNegligible(areaTolerance)) polygons.RemoveAt(i);
                else if (!polygon.IsPositive)
                {
                    polygons.RemoveAt(i);
                    negativePolygons.Add(polygon);
                }
            }
            // This seems to be the biggest problem. Holes may be through or blind and can still be occluded by other material. We don't want to union them away. But if 
            // each hole is properly nested in a positive polygon - even if it is not from that same polygon, then we can move to union the set of them. The small function
            // "AddHoleToLargerPostivePolygon" places negatives in a positive
            foreach (var hole in negativePolygons)
                AddHoleToLargerPostivePolygon(polygons, hole);
            //now union this result before returning to the main loop - to, again, union with the other polygons
            polygons = polygons.UnionPolygons(PolygonCollection.PolygonWithHoles);
            return polygons;
        }


        /// <summary>
        /// Adds the hole to larger postive polygon as described in the comment immediately above.
        /// </summary>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="hole">The hole.</param>
        private static void AddHoleToLargerPostivePolygon(List<Polygon> positivePolygons, Polygon hole)
        {
            Polygon enclosingPolygon = null;
            foreach (var poly in positivePolygons)
            {
                if (!poly.HasABoundingBoxThatEncompasses(hole)) continue;
                var interaction = poly.GetPolygonInteraction(hole);
                if (interaction.Relationship == ABRelationships.BInsideA &&
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
