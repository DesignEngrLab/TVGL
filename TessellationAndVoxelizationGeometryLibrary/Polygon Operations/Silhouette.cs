using System;
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
        public static List<Polygon> CreateSilhouette(this TessellatedSolid tessellatedSolid, Vector3 normal, double lowerDistance = double.NegativeInfinity,
            double upperDistance = double.PositiveInfinity)
        {
            var faceHash = tessellatedSolid.Faces.ToHashSet();
            var polygons = new List<Polygon>();
            while (faceHash.Any())
            {
                polygons.AddRange(GetPolygonFromFacesAndDirection(faceHash, normal, lowerDistance, upperDistance));
            }
            return polygons.Union();
        }

        private static List<Polygon> GetPolygonFromFacesAndDirection(HashSet<PolygonalFace> faceHash, Vector3 direction, double lowerDistance, double upperDistance)
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
                        if (sign * neighbor.Normal.Dot(direction) > 0 && faceHash.Contains(neighbor))
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
            var positivePolygons = new List<Polygon>();
            var negativePolygons = new List<Polygon>();
            while (outerEdges.Any())
            {
                var polyCoordinates = new List<Vector2>();
                KeyValuePair<Edge, bool> start = outerEdges.First();
                var current = start.Key;
                var edgeSign = start.Value;
                var startVertex = edgeSign == sign ? current.From : current.To;
                outerEdges.Remove(current);
                while (true)
                {
                    if (edgeSign == sign)
                        polyCoordinates.Add(current.From.Coordinates.ConvertTo2DCoordinates(transform));
                    else polyCoordinates.Add(current.To.Coordinates.ConvertTo2DCoordinates(transform));
                    var nextVertex = edgeSign == sign ? current.To : current.From;
                    if (nextVertex == startVertex) break;
                    var successfulNextEdgeFound = false;
                    if (outerEdges.Any())
                        foreach (var edge in nextVertex.Edges)
                        {
                            if (edge == current) continue;
                            if (outerEdges.ContainsKey(edge))
                            {
                                current = edge;
                                edgeSign = outerEdges[edge];
                                outerEdges.Remove(edge);
                                successfulNextEdgeFound = true;
                                break;
                            }
                        }
                    if (!successfulNextEdgeFound) throw new Exception();
                }
                var innerPositivePolygons = new Polygon(polyCoordinates).RemoveSelfIntersections(false, out var innerNegativePolygons);
                positivePolygons.AddRange(innerPositivePolygons);
                negativePolygons.AddRange(innerNegativePolygons);
            }
            var positivePolygonDictionary = new SortedDictionary<double, Polygon>(new NoEqualSort());
            var negativePolygonDictionary = new SortedDictionary<double, Polygon>(new NoEqualSort(false));
            foreach (var path in positivePolygons) positivePolygonDictionary.Add(path.Area, path);
            foreach (var path in negativePolygons) negativePolygonDictionary.Add(path.Area, path);
            PolygonOperations.CreateShallowPolygonTreesOrderedVertexLoops(positivePolygonDictionary, negativePolygonDictionary,
                positivePolygons.Count + negativePolygons.Count, out var resultingPolygons, out _);
            resultingPolygons = resultingPolygons.Union();
            return resultingPolygons;
        }
    }
}
