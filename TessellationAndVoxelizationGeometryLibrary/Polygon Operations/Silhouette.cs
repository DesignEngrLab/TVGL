using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;


namespace TVGL.TwoDimensional
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        public static List<Polygon> CreateSilhouette(this TessellatedSolid tessellatedSolid, Vector3 direction)
        {
            direction = direction.Normalize();
            var faceHash = tessellatedSolid.Faces.ToHashSet();
            if (tessellatedSolid.Faces[0].Edges == null || tessellatedSolid.Faces[0].Edges.Count == 0)
                tessellatedSolid.CompleteInitiation();
            var positivePolygons = new List<Polygon>();
            var silhouetteLinearTolerance = tessellatedSolid.SameTolerance;
            var silhouetteAreaTolerance = 1000.00 * silhouetteLinearTolerance * silhouetteLinearTolerance / Constants.BaseTolerance;
            int numOldUnvisitedFaces;
            var numNewUnvisitedFaces = faceHash.Count;
            do
            {
                numOldUnvisitedFaces = numNewUnvisitedFaces;
                numNewUnvisitedFaces = GetPolygonFromFacesAndDirection(faceHash, direction, positivePolygons, silhouetteLinearTolerance, silhouetteAreaTolerance);
            }
            while (faceHash.Any() && numOldUnvisitedFaces != numNewUnvisitedFaces);
            //positivePolygons = positivePolygons.Simplify().ToList();
            //Presenter.ShowAndHang(positivePolygons);
            var result = positivePolygons.Union(PolygonCollection.PolygonWithHoles, silhouetteLinearTolerance);
            var totalArea = result.Sum(p => Math.Abs(p.Area));
            silhouetteAreaTolerance = 5e-3 * totalArea;
            for (int i = result.Count - 1; i >= 0; i--)
            {
                var poly = result[i];
                foreach (var hole in poly.InnerPolygons.ToList())
                    if (Math.Abs(hole.Area) < silhouetteAreaTolerance) poly.RemoveHole(hole);
                if (poly.Area < silhouetteAreaTolerance) result.RemoveAt(i);
            }
            return result.Simplify().ToList();
        }

        private static int GetPolygonFromFacesAndDirection(HashSet<PolygonalFace> faceHash, Vector3 direction, List<Polygon> positivePolygons,
             double linearTolerance, double areaTolerance)
        {
            var transform = MiscFunctions.TransformToXYPlane(direction, out _);
            PolygonalFace startingFace = null;
            foreach (var face in faceHash)
            {
                if (!face.Normal.Dot(direction).IsNegligible(0.05) && !face.Area.IsNegligible(areaTolerance))
                {
                    startingFace = face;
                    break;
                }
            }
            if (startingFace == null) return faceHash.Count;
            faceHash.Remove(startingFace);
            var sign = startingFace.Normal.Dot(direction) > 0 ? 1 : -1;
            var stack = new Stack<PolygonalFace>();
            stack.Push(startingFace);
            var outerEdges = new Dictionary<Edge, bool>();
            while (stack.Any())
            {
                var current = stack.Pop();
                foreach (var edge in current.Edges)
                {
                    if (outerEdges.ContainsKey(edge)) outerEdges.Remove(edge);
                    else
                    {
                        var currentOwnsEdge = edge.OwnedFace == current;
                        outerEdges.Add(edge, currentOwnsEdge);
                        var neighbor = currentOwnsEdge ? edge.OtherFace : edge.OwnedFace;
                        if (neighbor != null && sign * neighbor.Normal.Dot(direction) >= 0 && faceHash.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                            faceHash.Remove(neighbor);
                        }
                    }
                }
            }
            ArrangeOuterEdgesIntoPolygon(outerEdges, sign == 1, transform, positivePolygons, linearTolerance, areaTolerance);
            return faceHash.Count;
        }

        private static void ArrangeOuterEdgesIntoPolygon(Dictionary<Edge, bool> outerEdges, bool sign, Matrix4x4 transform, List<Polygon> positivePolygons,
             double linearTolerance, double areaTolerance)
        {
            var polygons = new List<Polygon>();
            var negativePolygons = new List<Polygon>();
            while (outerEdges.Any())
            {   // outer while loop begins a new polygon with the outerEdges. There may easily be multiple polygons in the outer edges as it can represent
                // holes within the polygon
                var polyCoordinates = new List<Vector2>();
                KeyValuePair<Edge, bool> start = outerEdges.First();
                var current = start;
                var startVertex = current.Value == sign ? current.Key.From : current.Key.To;
                var successfulLoop = false;
                while (current.Key != null)
                {   // inner loop adds to the current polygon
                    outerEdges.Remove(current.Key);
                    if (current.Value == sign)
                        polyCoordinates.Add(current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                    else polyCoordinates.Add(current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                    var nextVertex = current.Value == sign ? current.Key.To : current.Key.From;
                    if (nextVertex == startVertex)
                    {
                        successfulLoop = true;
                        break;
                    }
                    var viableEdges = new List<KeyValuePair<Edge, bool>>();
                    if (outerEdges.Any())
                    {
                        foreach (var edge in nextVertex.Edges)
                        {
                            if (edge == current.Key) continue;
                            if (outerEdges.ContainsKey(edge))
                                viableEdges.Add(new KeyValuePair<Edge, bool>(edge, outerEdges[edge]));
                        }
                    }
                    if (viableEdges.Count == 0) current = new KeyValuePair<Edge, bool>(null, false);
                    else if (viableEdges.Count == 1) current = viableEdges[0];
                    else current = ChooseBestNextEdge(current.Key, current.Value, viableEdges, transform);
                }
                if (!successfulLoop)
                {
                    current = start;
                    while (outerEdges.Any())
                    {
                        var nextVertexBackwards = current.Value == sign ? current.Key.From : current.Key.To;
                        var viableEdges = new List<KeyValuePair<Edge, bool>>();
                        if (outerEdges.Any())
                        {
                            foreach (var edge in nextVertexBackwards.Edges)
                            {
                                if (edge == current.Key) continue;
                                if (outerEdges.ContainsKey(edge))
                                    viableEdges.Add(new KeyValuePair<Edge, bool>(edge, outerEdges[edge]));
                            }
                            if (viableEdges.Count == 0)
                                break;
                            current = viableEdges.Count == 1 ? viableEdges[0] : ChooseBestNextEdge(current.Key, current.Value, viableEdges, transform);
                            outerEdges.Remove(current.Key);
                        }
                        if (current.Value == sign)
                            polyCoordinates.Insert(0, current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                        else polyCoordinates.Insert(0, current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                    }
                    break;
                }


                if (polyCoordinates.Count > 2 && !polyCoordinates.Area().IsNegligible(areaTolerance))
                {
                    Presenter.ShowAndHang(polyCoordinates);
                    polygons.AddRange(new Polygon(polyCoordinates).RemoveSelfIntersections(false, out var strayNegativePolygons, linearTolerance));
                    if (strayNegativePolygons != null) polygons.AddRange(strayNegativePolygons);
                }
            }
            Presenter.ShowAndHang(polygons);
            foreach (var inner in polygons)
            {
                if (inner.Area.IsNegligible(areaTolerance)) continue;
                if (inner.IsPositive) positivePolygons.Add(inner);
                else negativePolygons.Add(inner);
            }
            foreach (var hole in negativePolygons)
                AddHoleToLargerPostivePolygon(positivePolygons, hole, linearTolerance);
        }


        private static void AddHoleToLargerPostivePolygon(List<Polygon> positivePolygons, Polygon hole, double tolerance)
        {
            Polygon enclosingPolygon = null;
            foreach (var poly in positivePolygons)
            {
                if (!poly.HasABoundingBoxThatEncompasses(hole)) continue;
                var interaction = poly.GetPolygonInteraction(hole, tolerance);
                if (interaction.Relationship == PolygonRelationship.BIsCompletelyInsideA &&
                    interaction.GetRelationships(hole).Skip(1).All(r => r.Item1 == PolygonRelationship.Separated))
                {
                    enclosingPolygon = poly;
                    break;
                }
            }
            if (enclosingPolygon != null)
                enclosingPolygon.AddInnerPolygon(hole);
        }

        private static KeyValuePair<Edge, bool> ChooseBestNextEdge(Edge current, bool edgeSign, List<KeyValuePair<Edge, bool>> viableEdges, Matrix4x4 transform)
        {
            var edgesScores = new double[viableEdges.Count];
            var currentUnitDirection = current.Vector.Normalize().Transform(transform);
            if (!edgeSign) currentUnitDirection *= -1;
            for (int i = 0; i < viableEdges.Count; i++)
            {
                var direction = viableEdges[i].Key.Vector.Normalize().Transform(transform);
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
