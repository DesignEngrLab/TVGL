// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.IOFunctions;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    ///     and
    ///     all interesting operations work on the TessellatedSolid.
    /// </remarks>
    public partial class TessellatedSolid
    {
        internal static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }
        #region Make Edges
        internal static List<Tuple<Edge, List<PolygonalFace>>> MakeEdges(IList<PolygonalFace> faces, bool doublyLinkToVertices,
            int vertexCheckSumMultiplier, out List<Tuple<Edge, List<PolygonalFace>>> overDefinedEdges,
            out List<Edge> partlyDefinedEdges)
        {
            var partlyDefinedEdgeDictionary = new Dictionary<long, Edge>();
            var alreadyDefinedEdges = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            var overDefinedEdgesDictionary = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            foreach (var face in faces)
            {
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[j == lastIndex ? 0 : j + 1];
                    var checksum = SetEdgeChecksum(fromVertex, toVertex, vertexCheckSumMultiplier);

                    if (overDefinedEdgesDictionary.ContainsKey(checksum))
                        overDefinedEdgesDictionary[checksum].Item2.Add(face);
                    else if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        var edgeEntry = alreadyDefinedEdges[checksum];
                        edgeEntry.Item2.Add(face);
                        overDefinedEdgesDictionary.Add(checksum, edgeEntry);
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdgeDictionary.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdgeDictionary[checksum];
                        alreadyDefinedEdges.Add(checksum, new Tuple<Edge, List<PolygonalFace>>(edge,
                            new List<PolygonalFace> { edge.OwnedFace, face }));
                        partlyDefinedEdgeDictionary.Remove(checksum);
                    }
                    else // this edge doesn't already exist.
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, false, checksum);
                        partlyDefinedEdgeDictionary.Add(checksum, edge);
                    }
                }
            }
            overDefinedEdges = overDefinedEdgesDictionary.Values.ToList();
            partlyDefinedEdges = partlyDefinedEdgeDictionary.Values.ToList();
            return alreadyDefinedEdges.Values.ToList();
        }
        /// <summary>
        /// Teases the apart over used edges. By taking in the edges with more than two faces (the over-used edges) a list is return of newly defined edges.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;TVGL.PolygonalFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> TeaseApartOverUsedEdges(List<Tuple<Edge, List<PolygonalFace>>> overUsedEdgesDictionary,
            out List<Edge> moreSingleSidedEdges)
        {
            var newListOfGoodEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            foreach (var entry in overUsedEdgesDictionary)
            {
                var edge = entry.Item1;
                var candidateFaces = entry.Item2;
                var numFailedTries = 0;
                // foreach over-used edge:
                // first, try to find the best match for each face. Basically, it is assumed that faces with the most similar normals 
                // should be paired together. 
                while (candidateFaces.Count > 1 && numFailedTries < candidateFaces.Count)
                {
                    var highestDotProduct = -2.0;
                    PolygonalFace bestMatch = null;
                    var refFace = candidateFaces[0];
                    candidateFaces.RemoveAt(0);
                    var refOwnsEdge = TessellatedSolid.FaceShouldBeOwnedFace(edge, refFace);
                    foreach (var candidateMatchingFace in candidateFaces)
                    {
                        var dotProductScore = refOwnsEdge == TessellatedSolid.FaceShouldBeOwnedFace(edge, candidateMatchingFace)
                            ? -2 //edge cannot be owned by both faces, thus this is not a good candidate for this.
                            : refFace.Normal.dotProduct(candidateMatchingFace.Normal);
                        //  To take it "out of the running", we simply give it a value of -2
                        if (dotProductScore > highestDotProduct)
                        {
                            highestDotProduct = dotProductScore;
                            bestMatch = candidateMatchingFace;
                        }
                    }
                    if (highestDotProduct > -1)
                    // -1 is a valid dot-product but it is not practical to match faces with completely opposite
                    // faces
                    {
                        numFailedTries = 0;
                        candidateFaces.Remove(bestMatch);
                        if (TessellatedSolid.FaceShouldBeOwnedFace(edge, refFace))
                            newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                                new Edge(edge.From, edge.To, refFace, bestMatch, false),
                                new List<PolygonalFace> { refFace, bestMatch }));
                        else
                            newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(
                                new Edge(edge.From, edge.To, bestMatch, refFace, false),
                                new List<PolygonalFace> { bestMatch, refFace }));
                    }
                    else
                    {
                        candidateFaces.Add(refFace);
                        //referenceFace was removed 24 lines earlier. Here, we re-add it to the
                        // end of the list.
                        numFailedTries++;
                    }
                }
            }
            moreSingleSidedEdges = new List<Edge>();
            foreach (var entry in overUsedEdgesDictionary)
            {
                var oldEdge = entry.Item1;
                oldEdge.From.Edges.Remove(entry.Item1); //the original over-used edge will not be used in the model.
                oldEdge.To.Edges.Remove(entry.Item1);   //so, here we remove it from the vertex references
                foreach (var face in entry.Item2)
                    moreSingleSidedEdges.Add(FaceShouldBeOwnedFace(oldEdge, face)
                            ? new Edge(oldEdge.From, oldEdge.To, face, null, false)
                            : new Edge(oldEdge.To, oldEdge.From, face, null, false));
            }
            return newListOfGoodEdges;
        }


        internal static bool FaceShouldBeOwnedFace(Edge edge, PolygonalFace face)
        {
            var otherEdgeVector = face.OtherVertex(edge.From, edge.To).Position.subtract(edge.To.Position);
            var isThisNormal = edge.Vector.crossProduct(otherEdgeVector);
            return face.Normal.dotProduct(isThisNormal) > 0;
        }

        /// <summary>
        /// Fixes the bad edges. By taking in the edges with more than two faces (the over-used edges) and the edges with only one face (the partlyDefinedEdges), this
        /// repair method attempts to repair the edges as best possible through a series of pairwise searches.
        /// </summary>
        /// <param name="overUsedEdgesDictionary">The over used edges dictionary.</param>
        /// <param name="partlyDefinedEdgesIEnumerable">The partly defined edges i enumerable.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Tuple&lt;TVGL.Edge, System.Collections.Generic.List&lt;TVGL.PolygonalFace&gt;&gt;&gt;.</returns>
        private static IEnumerable<Tuple<Edge, List<PolygonalFace>>> MediateSingleSidedEdges(List<Edge> singleSidedEdges,
            out List<PolygonalFace> newFaces, out List<Vertex> removedVertices)
        {
            newFaces=new List<PolygonalFace>();
            removedVertices = new List<Vertex>();
            var newListOfGoodEdges = new List<Tuple<Edge, List<PolygonalFace>>>();
            List<Edge> remainingEdges;
            var loops = OrganizeIntoLoops(singleSidedEdges, out remainingEdges);
            for (var i = 0; i < loops.Count(); i++)
            {
                //if a simple triangle, create a new face from vertices
                if (loops[i].Item1.Count == 3)
                {
                    var newFace = new PolygonalFace(loops[i].Item1.Select(e=>e.To),loops[i].Item2);
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else 
                {
                    //First, get an average normal from all vertices, assuming CCW order.
                    List<List<Vertex[]>> triangleFaceList;
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>>
                    { loops[i].Item1.Select(e => e.To).ToList() }, loops[i].Item2, out triangleFaceList);
                    foreach (var triangle in triangles)
                    {
                        var newFace = new PolygonalFace(triangle, loops[i].Item2);
                        newFaces.Add(newFace);
                    }
                }
            }
            if (newFaces.Count == 1) Message.output("1 missing face was fixed", 3);
            if (newFaces.Count > 1) Message.output(newFaces.Count + " missing faces were fixed", 3);
            return LinkUpNewFaces(newFaces, ts, ts.Errors.SingledSidedEdges);
        
        // now do a pairwise check with all entries in the partly defined edges
        var numRemaining = remainingEdges.Count;
            var scoresAndPairs = new SortedDictionary<double, int[]>(new NoEqualSort());
            for (int i = 0; i < numRemaining; i++)
                for (int j = i + 1; j < numRemaining; j++)
                {
                    var score = GetEdgeSimilarityScore(remainingEdges[i], remainingEdges[j]);
                    if (score <= Constants.MaxAllowableEdgeSimilarityScore)
                        scoresAndPairs.Add(score, new[] { i, j });
                }
            // basically, we go through from best match to worst until the MaxAllowableEdgeSimilarityScore is exceeded.
            var alreadyMatchedIndices = new HashSet<int>();
            foreach (var score in scoresAndPairs)
            {
                if (alreadyMatchedIndices.Contains(score.Value[0]) || alreadyMatchedIndices.Contains(score.Value[1]))
                    continue;
                alreadyMatchedIndices.Add(score.Value[0]);
                alreadyMatchedIndices.Add(score.Value[1]);
                newListOfGoodEdges.Add(new Tuple<Edge, List<PolygonalFace>>(singleSidedEdges[score.Value[0]],
                        new List<PolygonalFace> { singleSidedEdges[score.Value[0]].OwnedFace, singleSidedEdges[score.Value[1]].OwnedFace }));
            }
            newFaces = null;
            removedVertices = null;
            return newListOfGoodEdges;
        }

        private static List<Tuple<List<Edge>, double[]>> OrganizeIntoLoops(List<Edge> singleSidedEdges, out List<Edge> remainingEdges)
        {
            remainingEdges = new List<Edge>(singleSidedEdges);
            var attempts = 0;
            var listOfLoops = new List<Tuple<List<Edge>, double[]>>();
            while (remainingEdges.Count > 0 && attempts < remainingEdges.Count)
            {
                var loop = new List<Edge>();
                var successful = true;
                var removedEdges = new List<Edge>();
                var startingEdge = remainingEdges[0];
                var normal = startingEdge.OwnedFace.Normal;
                loop.Add(startingEdge);
                removedEdges.Add(startingEdge);
                remainingEdges.RemoveAt(0);
                do
                {
                    var possibleNextEdges = remainingEdges.Where(e => e.To == loop.Last().From);
                    if (possibleNextEdges.Any())
                    {
                        var bestNext = pickBestEdge(possibleNextEdges, loop.Last().Vector, normal);
                        loop.Add(bestNext);
                        var n1 = loop[loop.Count - 1].Vector.crossProduct(loop[loop.Count - 2].Vector).normalize();
                        if (!n1.Contains(double.NaN))
                        {
                            n1 = n1.dotProduct(normal) < 0 ? n1.multiply(-1) : n1;
                            normal = loop.Count == 2 ? n1
                                : normal.multiply(loop.Count).add(n1).divide(loop.Count + 1).normalize();
                        }
                        removedEdges.Add(bestNext);
                        remainingEdges.Remove(bestNext);
                    }
                    else
                    {
                        possibleNextEdges = remainingEdges.Where(e => e.From == loop[0].To);
                        if (possibleNextEdges.Any())
                        {
                            var bestPrev = pickBestEdge(possibleNextEdges, loop[0].Vector.multiply(-1),
                                normal);
                            loop.Insert(0, bestPrev);
                            var n1 = loop[1].Vector.crossProduct(loop[0].Vector).normalize();
                            if (!n1.Contains(double.NaN))
                            {
                                n1 = n1.dotProduct(normal) < 0 ? n1.multiply(-1) : n1;
                                normal = loop.Count == 2 ? n1
                                    : normal.multiply(loop.Count).add(n1).divide(loop.Count + 1).normalize();
                            }
                            removedEdges.Add(bestPrev);
                            remainingEdges.Remove(bestPrev);
                        }
                        else successful = false;
                    }
                } while (loop.First().To != loop.Last().From && successful);
                if (successful)
                {
                    //Average the normals from all the owned faces.
                    listOfLoops.Add(new Tuple<List<Edge>, double[]>(loop, normal));
                    attempts = 0;
                }
                else
                {
                    remainingEdges.AddRange(removedEdges);
                    attempts++;
                }
            }
            return listOfLoops;
        }

        private static Edge pickBestEdge(IEnumerable<Edge> possibleNextEdges, double[] refEdge, double[] normal)
        {
            var unitRefEdge = refEdge.normalize();
            var max = -2.0;
            Edge bestEdge = null;
            foreach (var candEdge in possibleNextEdges)
            {
                var unitCandEdge = candEdge.Vector.normalize();
                var cross = unitRefEdge.crossProduct(unitCandEdge);
                var temp = cross.dotProduct(normal);
                if (max < temp)
                {
                    max = temp;
                    bestEdge = candEdge;
                }
            }
            return bestEdge;
        }

        private static double GetEdgeSimilarityScore(Edge e1, Edge e2)
        {
            var score = Math.Abs(e1.Length - e2.Length) / e1.Length;
            score += 1 - Math.Abs(e1.Vector.normalize().dotProduct(e2.Vector.normalize()));
            score += Math.Min(e2.From.Position.subtract(e1.To.Position).norm2()
                + e2.To.Position.subtract(e1.From.Position).norm2(),
                e2.From.Position.subtract(e1.From.Position).norm2()
                + e2.To.Position.subtract(e1.To.Position).norm2())
                     / e1.Length;
            return score;
        }

        internal static Edge[] CompleteEdgeArray(List<Tuple<Edge, List<PolygonalFace>>> edgeList)
        {
            var numEdges = edgeList.Count;
            var edgeArray = new Edge[numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                //stitch together edges and faces. Note, the first face is already attached to the edge, due to the edge constructor
                //above
                var edge = edgeList[i].Item1;
                var ownedFace = edgeList[i].Item2[0];
                var otherFace = edgeList[i].Item2[1];
                edge.IndexInList = i;
                // grabbing the neighbor's normal (in the next 2 lines) should only happen if the original
                // face has no area (collapsed to a line).
                if (otherFace.Normal.Contains(Double.NaN)) otherFace.Normal = (double[])ownedFace.Normal.Clone();
                if (ownedFace.Normal.Contains(Double.NaN)) ownedFace.Normal = (double[])otherFace.Normal.Clone();
                edge.OtherFace = otherFace;
                otherFace.AddEdge(edge);
                edge.To.Edges.Add(edge);
                edge.From.Edges.Add(edge);
                edgeArray[i] = edge;
            }
            return edgeArray;
        }

        #endregion

        /// <summary>
        /// Defines the center, the volume and the surface area.
        /// </summary>
        internal static void DefineCenterVolumeAndSurfaceArea(IList<PolygonalFace> faces, out double[] center,
            out double volume, out double surfaceArea)
        {
            surfaceArea = faces.Sum(face => face.Area);
            volume = MiscFunctions.Volume(faces, out center);
        }

        const double oneSixtieth = 1.0 / 60.0;

        private static double[,] DefineInertiaTensor(IEnumerable<PolygonalFace> Faces, double[] Center, double Volume)
        {
            var matrixA = StarMath.makeZero(3, 3);
            var matrixCtotal = StarMath.makeZero(3, 3);
            var canonicalMatrix = new[,]
            {
                {oneSixtieth, 0.5*oneSixtieth, 0.5*oneSixtieth},
                {0.5*oneSixtieth, oneSixtieth, 0.5*oneSixtieth}, {0.5*oneSixtieth, 0.5*oneSixtieth, oneSixtieth}
            };
            foreach (var face in Faces)
            {
                matrixA.SetRow(0,
                    new[]
                    {
                        face.Vertices[0].Position[0] - Center[0], face.Vertices[0].Position[1] - Center[1],
                        face.Vertices[0].Position[2] - Center[2]
                    });
                matrixA.SetRow(1,
                    new[]
                    {
                        face.Vertices[1].Position[0] - Center[0], face.Vertices[1].Position[1] - Center[1],
                        face.Vertices[1].Position[2] - Center[2]
                    });
                matrixA.SetRow(2,
                    new[]
                    {
                        face.Vertices[2].Position[0] - Center[0], face.Vertices[2].Position[1] - Center[1],
                        face.Vertices[2].Position[2] - Center[2]
                    });

                var matrixC = matrixA.transpose().multiply(canonicalMatrix);
                matrixC = matrixC.multiply(matrixA).multiply(matrixA.determinant());
                matrixCtotal = matrixCtotal.add(matrixC);
            }

            var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            var matrixCprime =
                translateMatrix.multiply(-1)
                    .multiply(translateMatrix.transpose())
                    .add(translateMatrix.multiply(translateMatrix.multiply(-1).transpose()))
                    .add(translateMatrix.multiply(-1).multiply(translateMatrix.multiply(-1).transpose()))
                    .multiply(Volume);
            matrixCprime = matrixCprime.add(matrixCtotal);
            var result =
                StarMath.makeIdentity(3).multiply(matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            return result.subtract(matrixCprime);
        }


        private static void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces)
            {
                var index = face.Vertices.IndexOf(vertex);
                if (index >= 0) face.Vertices.RemoveAt(index);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
        }

        internal static long SetEdgeChecksum(Vertex fromVertex, Vertex toVertex, int VertexCheckSumMultiplier)
        {
            var fromIndex = fromVertex.IndexInList;
            var toIndex = toVertex.IndexInList;
            if (fromIndex == -1 || toIndex == -1) return -1;
            //  if (fromIndex == toIndex) throw new Exception("edge to same vertices.");
            return fromIndex < toIndex
                ? fromIndex + VertexCheckSumMultiplier * toIndex
                : toIndex + VertexCheckSumMultiplier * fromIndex;
        }


        public static void Transform(IEnumerable<Vertex> Vertices, double[,] transformMatrix)
        {
            double[] tempCoord;
            foreach (var vert in Vertices)
            {
                tempCoord = transformMatrix.multiply(new[] { vert.X, vert.Y, vert.Z, 1 });
                vert.Position[0] = tempCoord[0];
                vert.Position[1] = tempCoord[1];
                vert.Position[2] = tempCoord[2];
            }
            /*     tempCoord = transformMatrix.multiply(new[] {XMin, YMin, ZMin, 1});
                 XMin = tempCoord[0];
                 YMin = tempCoord[1];
                 ZMin = tempCoord[2];

                 tempCoord = transformMatrix.multiply(new[] {XMax, YMax, ZMax, 1});
                 XMax = tempCoord[0];
                 YMax = tempCoord[1];
                 ZMax = tempCoord[2];
                 Center = transformMatrix.multiply(new[] {Center[0], Center[1], Center[2], 1});
                 // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
                 if (_inertiaTensor != null)
                 {
                     var rotMatrix = new double[3, 3];
                     for (int i = 0; i < 3; i++)
                         for (int j = 0; j < 3; j++)
                             rotMatrix[i, j] = transformMatrix[i, j];
                     _inertiaTensor = rotMatrix.multiply(_inertiaTensor);
                 }
                 if (Primitives != null)
                     foreach (var primitive in Primitives)
                         primitive.Transform(transformMatrix);
         */
        }

    }
}