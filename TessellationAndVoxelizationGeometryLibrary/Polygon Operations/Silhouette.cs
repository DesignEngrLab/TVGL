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
            var polygons = new List<Polygon>();
            while (faceHash.Any())
                polygons.AddRange(GetPolygonFromFacesAndDirection(faceHash, direction));
             Presenter.ShowAndHang(polygons);
            return polygons.Union();
        }

        private static List<Polygon> GetPolygonFromFacesAndDirection(HashSet<PolygonalFace> faceHash, Vector3 direction)
        {
            var transform = MiscFunctions.TransformToXYPlane(direction, out _);
            var visitedFaces = new List<PolygonalFace>();
            PolygonalFace startingFace;
            do
            {
                startingFace = faceHash.First();
                faceHash.Remove(startingFace);
            } while ((startingFace.Area * startingFace.Normal.Dot(direction)).IsNegligible(Constants.BaseTolerance));
            visitedFaces.Add(startingFace);
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
                        if (neighbor !=null && sign * neighbor.Normal.Dot(direction) > 0 && faceHash.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                            faceHash.Remove(neighbor);
                            visitedFaces.Add(neighbor);
                        }
                    }
                }
            }
            return ArrangeOuterEdgesIntoPolygon(outerEdges, sign == 1, transform);
        }

        private static List<Polygon> ArrangeOuterEdgesIntoPolygon(Dictionary<Edge, bool> outerEdges, bool sign, Matrix4x4 transform)
        {
            var polygons = new List<Polygon>();
           // var negativePolygons = new List<Polygon>();
            while (outerEdges.Any())
            {
                var polyCoordinates = new List<Vector2>();
                KeyValuePair<Edge, bool> start = outerEdges.First();
                var current = start;
                var startVertex = current.Value == sign ? current.Key.From : current.Key.To;
                while (outerEdges.Any())
                {
                    outerEdges.Remove(current.Key);
                    if (current.Value == sign)
                        polyCoordinates.Add(current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                    else polyCoordinates.Add(current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                    var nextVertex = current.Value == sign ? current.Key.To : current.Key.From;
                    if (nextVertex == startVertex) break;
                    var viableEdges = new List<KeyValuePair<Edge, bool>>();
                    foreach (var edge in nextVertex.Edges)
                    {
                        if (edge == current.Key) continue;
                        if (outerEdges.ContainsKey(edge))
                            viableEdges.Add(new KeyValuePair<Edge, bool>(edge, outerEdges[edge]));
                    }
                    if (viableEdges.Count == 0)
                    {
                        current = start;
                        while (outerEdges.Any())
                        {
                            var nextVertexBackwards = current.Value == sign ? current.Key.From : current.Key.To;
                            var viableEdgesInner = new List<KeyValuePair<Edge, bool>>();
                            if (outerEdges.Any())
                            {
                                foreach (var edge in nextVertexBackwards.Edges)
                                {
                                    if (edge == current.Key) continue;
                                    if (outerEdges.ContainsKey(edge))
                                        viableEdgesInner.Add(new KeyValuePair<Edge, bool>(edge, outerEdges[edge]));
                                }
                                if (viableEdgesInner.Count == 0)
                                    break;
                                current = viableEdgesInner.Count == 1 ? viableEdgesInner[0] : ChooseBestNextEdge(current.Key, current.Value, viableEdgesInner, transform);
                                outerEdges.Remove(current.Key);
                            }
                            if (current.Value == sign)
                                polyCoordinates.Insert(0, current.Key.To.Coordinates.ConvertTo2DCoordinates(transform));
                            else polyCoordinates.Insert(0, current.Key.From.Coordinates.ConvertTo2DCoordinates(transform));
                        }
                        break;
                    }
                    current = viableEdges.Count == 1 ? viableEdges[0] : ChooseBestNextEdge(current.Key, current.Value, viableEdges, transform);
                }
                if (polyCoordinates.Count > 2 && Math.Abs(polyCoordinates.Area()) > 0)
                {
                    polygons.AddRange(new Polygon(polyCoordinates).RemoveSelfIntersections(false, out var strayNegativePolygons));
                    //var innerPositivePolygons = new Polygon(polyCoordinates).RemoveSelfIntersections(false, out var strayNegativePolygons);
                    //positivePolygons.AddRange(innerPositivePolygons);
                    //negativePolygons.AddRange(strayNegativePolygons);
                }
            }
            //foreach (var path in positivePolygons) positivePolygonDictionary.Add(path.Area, path);
            //foreach (var path in negativePolygons) negativePolygonDictionary.Add(path.Area, path);
            //var resultingPolygons = resultingPolygons.CreateShallowPolygonTreesFromPolygons(positivePolygonDictionary, negativePolygonDictionary,
            //    polygons.Count + negativePolygons.Count, out _, out _);
            polygons = polygons.Union();
            return polygons;
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
