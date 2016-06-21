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

        private static Edge[] MakeEdges(IList<PolygonalFace> faces, bool doublyLinkToVertices,
            int vertexCheckSumMultiplier)
        {
            var partlyDefinedEdgeDictionary = new Dictionary<long, Edge>();
            var alreadyDefinedEdges = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            var overUsedEdgesDictionary = new Dictionary<long, Tuple<Edge, List<PolygonalFace>>>();
            foreach (var face in faces)
            {
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[j == lastIndex ? 0 : j + 1];
                    var checksum = SetEdgeChecksum(fromVertex, toVertex, vertexCheckSumMultiplier);

                    if (overUsedEdgesDictionary.ContainsKey(checksum))
                        overUsedEdgesDictionary[checksum].Item2.Add(face);
                    else if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        var edgeEntry = alreadyDefinedEdges[checksum];
                        edgeEntry.Item2.Add(face);
                        overUsedEdgesDictionary.Add(checksum, edgeEntry);
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
            var goodEdgeEntries = alreadyDefinedEdges.Values.ToList();
            goodEdgeEntries.AddRange(TessellationError.TeaseApartOverUsedEdges(overUsedEdgesDictionary.Values));
            foreach (var entry in goodEdgeEntries)
            {
                //stitch together edges and faces. Note, the first face is already attached to the edge, due to the edge constructor
                //above
                var edge = entry.Item1;
                var ownedFace = entry.Item2[0];
                var otherFace = entry.Item2[1];
                // grabbing the neighbor's normal (in the next 2 lines) should only happen if the original
                // face has no area (collapsed to a line).
                if (otherFace.Normal.Contains(double.NaN)) otherFace.Normal = (double[])ownedFace.Normal.Clone();
                if (ownedFace.Normal.Contains(double.NaN)) ownedFace.Normal = (double[])otherFace.Normal.Clone();
                edge.OtherFace = otherFace;
                otherFace.AddEdge(edge);
                if (doublyLinkToVertices)
                {
                    edge.To.Edges.Add(edge);
                    edge.From.Edges.Add(edge);
                }
            }
            return goodEdgeEntries.Select(entry => entry.Item1).ToArray();
        }

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