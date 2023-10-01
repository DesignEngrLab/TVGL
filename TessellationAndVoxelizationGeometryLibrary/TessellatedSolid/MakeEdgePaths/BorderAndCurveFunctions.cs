using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public partial class MiscFunctions
    {
        internal static void DefineBorderSegments(TessellatedSolid ts)
        {
            var borderEdges = new HashSet<Edge>(ts.NonsmoothEdges.Concat(ts.Primitives.SelectMany(prim => prim.OuterEdges)));
            var edgePaths = ConsolidateEdgePaths(borderEdges);
            DividePathAtDiscontinuities(edgePaths, ts.TessellationError);

            foreach (var patch in TessellationBuildAndRepair.GetFacePatchesBetweenBorderEdges(borderEdges, ts.Faces, 
                ts.Primitives.SelectMany(prim => prim.Faces).ToHashSet()))
                ts.Primitives.Add(new UnknownRegion(patch));
            foreach (var prim in ts.Primitives)
                prim.BorderSegments = new List<BorderSegment>();
            foreach (var edgePath in edgePaths)
            {
                var segment = new BorderSegment(edgePath);
                var ownedFace = segment.DirectionList[0] ? segment.EdgeList[0].OwnedFace : segment.EdgeList[0].OtherFace;
                var otherFace = segment.DirectionList[0] ? segment.EdgeList[0].OtherFace : segment.EdgeList[0].OwnedFace;
                var ownedPrimitive = ts.Primitives.FirstOrDefault(p => p.Faces.Contains(ownedFace));
                ownedPrimitive.BorderSegments.Add(segment);
                var otherPrimitive = ts.Primitives.FirstOrDefault(p => p.Faces.Contains(otherFace));
                otherPrimitive.BorderSegments.Add(segment);
            }
        }

        private static List<EdgePath> ConsolidateEdgePaths(IEnumerable<Edge> edges)
        {
            var vertex2PathDictionary = new List<Dictionary<Vertex, List<EdgePath>>>();
            // add the 0th dictionary, which will hold the borders that are closed loops
            // These are added 5 lines down if a given border IsClosed
            vertex2PathDictionary.Add(new Dictionary<Vertex, List<EdgePath>>());
            foreach (var e in edges)
            {
                // add for both the beginning and ending vertex
                AddToDictionary(e.From, e, vertex2PathDictionary);
                AddToDictionary(e.To, e, vertex2PathDictionary);
            }
            // now, these dictionaries are used to consolidate the borders into longer ones.
            // ->the 0th ones are not going to change. Nothing to do here (why make dictionary anyway?)
            // ->same with the 1st. These are simply dead-ends
            // ->the 2s should all be connected together. Later we will see if the angle is too big
            //   between the borders. that's not the job for this step
            // ->for 3s, we will connect the two paths that are most similar and leave the other as a
            //   dead-end. This is a T-juncture and may be fairly common 
            // now, we need to work backwards from the most connected vertices to the least.
            // actually
            var dead2LiveEdgePathDictionary = new Dictionary<EdgePath, EdgePath>();
            var topOddIndex = vertex2PathDictionary.Count % 2 == 0 ? vertex2PathDictionary.Count - 1 : vertex2PathDictionary.Count - 2;
            var topEvenIndex = vertex2PathDictionary.Count % 2 == 0 ? vertex2PathDictionary.Count - 2 : vertex2PathDictionary.Count - 1;
            for (int i = topOddIndex; i > 2; i -= 2)
            {
                var v2PDict = vertex2PathDictionary[i];
                foreach (var entry in v2PDict)
                {
                    var vertex = entry.Key;
                    var edgePaths = entry.Value;
                    if (edgePaths.Count == 0) continue;
                    var oddPath = FindLeastSimilarEdgePath(vertex, edgePaths);
                    edgePaths.Remove(oddPath);
                    vertex2PathDictionary[i - 1][vertex] = edgePaths;
                    vertex2PathDictionary[1][vertex] = new List<EdgePath> { oddPath };
                }
            }
            for (int i = topEvenIndex; i > 2; i -= 2)
            {
                var v2PDict = vertex2PathDictionary[i];
                foreach (var entry in v2PDict)
                {
                    var vertex = entry.Key;
                    var edgePaths = entry.Value;
                    //if (primitiveBorders.Count == 0) continue;
                    var (closePath1, closePath2) = FindMostSimilarEdgePathPairs(vertex, edgePaths);
                    edgePaths.Remove(closePath1);
                    edgePaths.Remove(closePath2);
                    vertex2PathDictionary[i - 2][vertex] = edgePaths;
                    CombineTwoEdgePaths(dead2LiveEdgePathDictionary, vertex, closePath1, closePath2);
                }
            }

            // at this point all the dictionaries are removed except 2,1,0
            foreach (var keyValuePair in vertex2PathDictionary[2])
            {
                var vertex = keyValuePair.Key;
                var edgepath1 = keyValuePair.Value[0];
                var edgepath2 = keyValuePair.Value[1];
                CombineTwoEdgePaths(dead2LiveEdgePathDictionary, vertex, edgepath1, edgepath2);
            }
            var resultEdgePaths = dead2LiveEdgePathDictionary.Values
                .Where(e => !dead2LiveEdgePathDictionary.ContainsKey(e)).Distinct().ToHashSet();
            foreach (var item in vertex2PathDictionary[1].Values)
                if (item.Count > 0 && !dead2LiveEdgePathDictionary.ContainsKey(item[0]))
                    resultEdgePaths.Add(item[0]);
            foreach (var item in vertex2PathDictionary[0].Values)
                if (item.Count > 0)
                    resultEdgePaths.Add(item[0]);
            return resultEdgePaths.ToList();
        }


        private static void CombineTwoEdgePaths(Dictionary<EdgePath, EdgePath> dead2LiveEdgePathDictionary, Vertex vertex, EdgePath edgepath1, EdgePath edgepath2)
        {
            while (dead2LiveEdgePathDictionary.TryGetValue(edgepath1, out var newEdgepath1))
                edgepath1 = newEdgepath1;
            while (dead2LiveEdgePathDictionary.TryGetValue(edgepath2, out var newEdgepath2))
                edgepath2 = newEdgepath2;
            if (edgepath1 == edgepath2) //this happens after an edgepath has made a loop with itself
            {
                edgepath1.UpdateIsClosed();
                return;
            }
            dead2LiveEdgePathDictionary.Add(edgepath2, edgepath1);
            CombineTwoEdgePaths(vertex, edgepath1, edgepath2);
        }

        private static void CombineTwoEdgePaths(Vertex vertex, EdgePath edgepath1, EdgePath edgepath2)
        {
            var addToEnd = vertex == edgepath1.LastVertex;
            var addInForward = addToEnd == (vertex == edgepath2.FirstVertex);
            if (addInForward && addToEnd)
                foreach (var item in edgepath2)
                    edgepath1.AddEnd(item.edge, item.dir);
            else if (addToEnd)
                foreach (var item in edgepath2.Reverse())
                    edgepath1.AddEnd(item.edge, !item.dir);
            else if (addInForward)
                foreach (var item in edgepath2.Reverse())
                    edgepath1.AddBegin(item.edge, item.dir);
            else //add to beginning
                foreach (var item in edgepath2)
                    edgepath1.AddBegin(item.edge, !item.dir);
        }

        private static EdgePath FindLeastSimilarEdgePath(Vertex vertex, List<EdgePath> edgePaths)
        {
            var n = edgePaths.Count;
            var vectors = edgePaths.Select(e => GetPathVector(vertex, e)).ToList();
            var scoreMatrix = new double[n, n];
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    scoreMatrix[i, j] = scoreMatrix[j, i] = -GetPathScore(vectors[i], vectors[j]);
                }
            }
            var worstScore = double.PositiveInfinity;
            var worstIndex = -1;
            for (int i = 0; i < n; i++)
            {
                var score = RowSum(scoreMatrix, i, n);
                if (score < worstScore)
                {
                    worstIndex = i;
                    worstScore = score;
                }
            }
            return edgePaths[worstIndex];
        }
        private static (EdgePath, EdgePath) FindMostSimilarEdgePathPairs(Vertex vertex, List<EdgePath> edgePaths)
        {
            var n = edgePaths.Count;
            var vectors = edgePaths.Select(e => GetPathVector(vertex, e)).ToList();
            var bestScore = double.NegativeInfinity;
            var bestIndices = (-1, -1);
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var score = -GetPathScore(vectors[i], vectors[j]);
                    if (score > bestScore)
                    {
                        bestIndices = (i, j);
                        bestScore = score;
                    }
                }
            }
            return (edgePaths[bestIndices.Item1], edgePaths[bestIndices.Item2]);
        }

        private static double RowSum(double[,] matrix, int i, int length)
        {
            var sum = 0.0;
            for (int j = 0; j < length; j++)
                sum += matrix[i, j];
            return sum;
        }

        private static double GetPathScore(Vector3 vectorI, Vector3 vectorJ)
        {
            var vectorILength = vectorI.LengthSquared();
            var vectorJLength = vectorJ.LengthSquared();
            double maxLength = (vectorILength < vectorJLength)
                ? vectorJLength : vectorILength;
            var dot = vectorI.Dot(vectorJ);
            return dot / maxLength;
        }

        private static Vector3 GetPathVector(Vertex vertex, EdgePath edgePath)
        {
            var start = vertex.Coordinates;
            Vector3 end = (edgePath.FirstVertex == vertex)
                ? (edgePath.DirectionList[0] ? edgePath.EdgeList[0].To.Coordinates
                    : edgePath.EdgeList[0].From.Coordinates)
                    : (edgePath.DirectionList[^1] ? edgePath.EdgeList[^1].From.Coordinates
                    : edgePath.EdgeList[^1].To.Coordinates);
            return end - start;
        }

        private static void AddToDictionary(Vertex vertex, Edge edge,
            List<Dictionary<Vertex, List<EdgePath>>> vertex2PathDictionary)
        {
            var edgePath = new EdgePath();
            edgePath.AddEnd(edge);
            List<EdgePath> collectedEdgePaths = new List<EdgePath> { edgePath };
            for (int i = 1; i <= vertex2PathDictionary.Count; i++)
            {
                if (i == vertex2PathDictionary.Count)
                    vertex2PathDictionary.Add(new Dictionary<Vertex, List<EdgePath>>());
                if (vertex2PathDictionary[i].TryGetValue(vertex, out var existingPaths))
                {
                    if (existingPaths.Count > 0)
                    {
                        collectedEdgePaths.AddRange(existingPaths);
                        existingPaths.Clear();
                        // the reason we don't remove from this dictionary is that - if the same vertex
                        // comes up again - it will think it is new to this level. We need to keep a record
                        // of it at lower levels
                    }
                }
                else
                {
                    vertex2PathDictionary[i].Add(vertex, collectedEdgePaths);
                    break;
                }
            }
        }

        private static void DividePathAtDiscontinuities(List<EdgePath> edgePaths, double chordError)
        {
            for (int i = edgePaths.Count - 1; i >= 0; i--)
            {
                EdgePath edgePath = edgePaths[i];
                var length = edgePath.Count;
                if (length == 1) continue;
                var corners = new List<(int, double, double, double, double)>();
                //Presenter.ShowVertexPaths(border.GetVertices().Select(v => v.Coordinates), ts, 2, null);
                var vectorA = edgePath.EdgeList[^1].Vector;
                if (!edgePath.DirectionList[^1]) vectorA *= -1;
                var vectorALength = vectorA.Length();
                var prevLengths = 0.0;
                for (int j = 0; j < length; j++)
                {
                    var vectorB = edgePath.EdgeList[j].Vector;
                    if (!edgePath.DirectionList[j]) vectorB *= -1;
                    var vectorBLength = vectorB.Length();
                    var dot = vectorA.Dot(vectorB);
                    var cross = vectorA.Cross(vectorB);
                    var crossLength = cross.Length();
                    var cosAngle = dot / (vectorALength * vectorBLength);
                    if (cosAngle < Constants.DotToleranceForSame) //
                    {
                        corners.Add((j, dot, crossLength, vectorALength + prevLengths, vectorBLength));
                        prevLengths = 0.0;
                    }
                    else
                    {
                        if (corners.Count > 0)
                            corners[^1] = (corners[^1].Item1, corners[^1].Item2, corners[^1].Item3, corners[^1].Item4, corners[^1].Item5 + vectorBLength);
                        prevLengths += vectorALength;
                    }
                    vectorA = vectorB;
                    vectorALength = vectorBLength;
                }
                if (prevLengths > 0.0 && corners.Count > 0)
                    corners[0] = (corners[0].Item1, corners[0].Item2, corners[0].Item3, corners[0].Item4 + prevLengths, corners[0].Item5);
                var indexOfFirstNewEdgePath = edgePath.IsClosed ? edgePaths.Count : -1;
                var cornerIndex = corners.Count - 1;
                for (int j = length - 1; j >= 0; j--)
                {
                    if (cornerIndex >= 0 && j == corners[cornerIndex].Item1)
                    {
                        var corner = corners[cornerIndex];
                        if (LineSegmentsAreC1Discontinuous(corner.Item2, corner.Item3, corner.Item4, corner.Item5, chordError))
                        {
                            var newEdgePath = BreakOffNewEdgePath(edgePath, length, j);
                            if (newEdgePath != null)
                                edgePaths.Add(newEdgePath);
                            //Presenter.ShowVertexPaths((new[] { border, ts.NonsmoothEdges[^1] }).Select(r => r.GetVertices().Select(v => v.Coordinates)), ts, 2, null, false);
                            if (cornerIndex == 0) indexOfFirstNewEdgePath = -1;
                        }
                        cornerIndex--;
                    }
                }
                if (indexOfFirstNewEdgePath >= 0 && indexOfFirstNewEdgePath < edgePaths.Count)
                {
                    var endPath = edgePaths[indexOfFirstNewEdgePath];
                    CombineTwoEdgePaths(edgePath.FirstVertex, edgePath, endPath);
                    edgePaths.RemoveAt(indexOfFirstNewEdgePath);
                }
            }
        }

        private static EdgePath BreakOffNewEdgePath(EdgePath edgePath, int length, int j)
        {
            if (j == 0)
            {
                edgePath.UpdateIsClosed();
                return null;
            }
            var newEdgePath = new EdgePath();
            newEdgePath.DirectionList.AddRange(edgePath.DirectionList.GetRange(j, edgePath.Count - j));
            newEdgePath.EdgeList.AddRange(edgePath.EdgeList.GetRange(j, edgePath.Count - j));
            newEdgePath.UpdateIsClosed();
            edgePath.DirectionList.RemoveRange(j, edgePath.Count - j);
            edgePath.EdgeList.RemoveRange(j, edgePath.Count - j);
            edgePath.UpdateIsClosed();
            return newEdgePath;
        }

        /// <summary>
        /// Define Borders given that the primitives and border segments have already been defined.
        /// </summary>
        /// <param name="solid"></param>
        /// <exception cref="Exception"></exception>
        public static void DefineBorders(TessellatedSolid solid)
        {
            DefineBorderSegments(solid);
            foreach (var prim in solid.Primitives)
                DefineBorders(solid, prim);
        }

        public static List<PrimitiveBorder> DefineBorders(TessellatedSolid solid, PrimitiveSurface prim)
        {
            var debug = true;
            //Define the borders
            var borders = PrimitiveBorder.GetBorders(prim.BorderSegments);
            if (borders.Count == 0 || borders.Any(p => !p.IsClosed))
            {
                if (debug)
                {
                    solid.ResetDefaultColor();
                    prim.SetColor(new Color(KnownColors.Red));
                    var lines = new List<IEnumerable<Vector3>>();
                    foreach (var border in prim.BorderSegments)
                        lines.Add(border.GetVectors());
                    //Presenter.ShowVertexPaths(lines, solid, 3, null, false);
                }
                //Messenger.Error("All borders must be closed loops.");
                //throw new Exception("All borders must be closed loops.");
            }
            prim.Borders = borders;
            return borders;
        }
    }
}