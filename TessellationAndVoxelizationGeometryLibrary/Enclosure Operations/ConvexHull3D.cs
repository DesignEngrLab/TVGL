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
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public class ConvexHull3D
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
        public readonly double tolerance;

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<Vertex> Vertices = new List<Vertex>();
        readonly List<CHFace> cHFaces = new List<CHFace>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public TriangleFace[] Faces
        {
            get
            {
                if (Vertices.Count > 0 && faces == null)
                    faces = MakeFaces(cHFaces);
                return faces;
            }
        }
        private TriangleFace[] faces;

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public Edge[] Edges
        {
            get
            {
                if (Vertices.Count > 0 && edges == null)
                    edges = MakeEdges(Faces, Vertices);
                return edges;
            }
        }
        private Edge[] edges;
        private static Edge[] MakeEdges(IEnumerable<TriangleFace> faces, IList<Vertex> vertices)
        {
            var numVertices = vertices.Count;
            var vertexIndices = new Dictionary<Vertex, int>();
            for (var i = 0; i < vertices.Count; i++)
                vertexIndices.Add(vertices[i], i);
            var edgeDictionary = new Dictionary<long, Edge>();
            foreach (var face in faces)
            {
                var fromVertex = face.C;
                foreach (var toVertex in face.Vertices)
                {
                    var fromVertexIndex = vertexIndices[fromVertex];
                    var toVertexIndex = vertexIndices[toVertex];
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

    }
    public class CHFace
    {
        public readonly Vertex A;
        public readonly Vertex B;
        public readonly Vertex C;
        public readonly Vector3 Normal;
        public readonly double D;
        public readonly double Area;
        public readonly double Tolerance;
        public readonly List<Edge> Edges = new List<Edge>();
        public CHFace(Vertex a, Vertex b, Vertex c, double tolerance)
        {
            A = a;
            B = b;
            C = c;
            Tolerance = tolerance;
            Normal = Vector3.CrossProduct(B.Position - A.Position, C.Position - A.Position);
            Normal.Normalize();
            D = Vector3.DotProduct(Normal, A.Position);
            Area = Vector3.CrossProduct(B.Position - A.Position, C.Position - A.Position).Length / 2;
        }
        public void AddEdge(Edge edge)
        {
            if (!Edges.Contains(edge)) Edges.Add(edge);
        }
    }
}