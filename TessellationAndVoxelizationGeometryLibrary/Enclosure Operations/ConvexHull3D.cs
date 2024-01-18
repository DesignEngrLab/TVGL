// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="ConvexHull3D.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public class ConvexHull3D<T> where T : IPoint3D, new()
    {
        /// <summary>
        /// Calculates the Center and Volume of the convex hull using the faces 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="volume"></param>
        public void CalculateVolumeAndCenter(out Vector3 center, out double volume)
            => TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out volume, out center);
        public double GetSurfaceArea() => Faces.Sum(f => f.Area);

        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        public double tolerance { get; internal init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<T> Vertices = new List<T>();
        internal readonly List<CHFace> cHFaces = new List<CHFace>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public TriangleFace[] Faces
        {
            get
            {
                if (Vertices.Count > 0 && faces == null)
                    faces = MakeFaces(cHFaces, Vertices);
                return faces;
            }
        }
        private TriangleFace[] faces;
        private static TriangleFace[] MakeFaces(List<CHFace> cHFaces, List<T> allVertices)
        {
            var newVertices = new Vertex[allVertices.Count];
            for (int i = 0; i < allVertices.Count; i++)
            {
                var v = allVertices[i];
                if (v is Vertex vertex)
                    newVertices[i] = vertex;
                else newVertices[i] = new Vertex(new Vector3(v.X, v.Y, v.Z), i);
            }

            var numCvxHullFaces = 2 * (allVertices.Count - 2); // a little euler's formula magic
            var faces = new TriangleFace[numCvxHullFaces];
            var k = 0;
            foreach (var chFace in cHFaces)
            {
                for (var i = 2; i < chFace.BorderVertices.Count; i++)
                {
                    faces[k++] = new TriangleFace(new[]
                    {
                        newVertices[chFace.BorderVertices[0]],
                        newVertices[chFace.BorderVertices[i - 1]],
                        newVertices[chFace.BorderVertices[i]]
                    }, false);
                }
            }
            return faces;
        }

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public Edge[] Edges
        {
            get
            {
                if (Vertices.Count > 0 && edges == null)
                    edges = MakeEdges(Faces);
                return edges;
            }
        }
        private Edge[] edges;
        private static Edge[] MakeEdges(TriangleFace[] faces)
        {
            var numVertices = (3 * faces.Length) >> 1;
            var edgeDictionary = new Dictionary<long, Edge>();
            foreach (var face in faces)
            {
                var fromVertex = face.C;
                foreach (var toVertex in face.Vertices)
                {
                    var fromVertexIndex = fromVertex.IndexInList;
                    var toVertexIndex = toVertex.IndexInList;
                    long checksum = fromVertexIndex < toVertexIndex
                        ? fromVertexIndex + numVertices * toVertexIndex
                        : toVertexIndex + numVertices * fromVertexIndex;

                    if (edgeDictionary.TryGetValue(checksum, out var edge))
                    {
                        edge.OtherFace = face;
                        face.AddEdge(edge);
                    }
                    else edgeDictionary.Add(checksum, new Edge(fromVertex, toVertex, face, null, false, checksum));
                    fromVertex = toVertex;
                }
            }
            Edge[] edgeArray = new Edge[edgeDictionary.Count];
            edgeDictionary.Values.CopyTo(edgeArray, 0);
            return edgeArray;
        }

        internal CHFace MakeCHFace(int v1Index, int v2Index, int v3Index)
        {
            var v1 = Vertices[v1Index] is Vertex vertex1 ? vertex1.Coordinates : new Vector3(Vertices[v1Index].X, Vertices[v1Index].Y, Vertices[v1Index].Z);
            var v2 = Vertices[v2Index] is Vertex vertex2 ? vertex2.Coordinates : new Vector3(Vertices[v2Index].X, Vertices[v2Index].Y, Vertices[v2Index].Z);
            var v3 = Vertices[v3Index] is Vertex vertex3 ? vertex3.Coordinates : new Vector3(Vertices[v3Index].X, Vertices[v3Index].Y, Vertices[v3Index].Z);
            var normal = (v2 - v1).Cross(v3 - v1).Normalize();
            var d = normal.Dot(v1);
            return new CHFace
            {
                BorderVertices = new List<int> { v1Index, v2Index, v3Index },
                InteriorVertices = new List<int>(),
                Normal = normal,
                D = d,
                Anchor = (v1 + v2 + v3) / 3
            };
        }
    }
    public class CHFace
    {
        public List<int> BorderVertices { get; internal init; }
        public List<int> InteriorVertices { get; internal init; }
        public Vector3 Normal { get; internal init; }
        public double D { get; internal init; }
        public Vector3 Anchor { get; internal init; }
        public SortedList<double, int> SortedNew { get; internal set; } = new SortedList<double, int>();
    }
}
