// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TVGLConvexHull.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public class TVGLConvexHull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TVGLConvexHull" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public TVGLConvexHull(TessellatedSolid ts) : this(ts.Vertices, ts.SameTolerance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TVGLConvexHull" /> class.
        /// Optionally can choose to create faces and edges. Cannot make edges without faces.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="createFaces">if set to <c>true</c> [create faces].</param>
        /// <param name="createEdges">if set to <c>true</c> [create edges].</param>
        public TVGLConvexHull(IList<Vertex> vertices, double tolerance, bool createFaces = true, bool createEdges = true)
        {
            var convexHull = ConvexHull.Create(vertices, tolerance);
            if (convexHull.Result == null) return;
            Vertices = convexHull.Result.Points.ToArray();
            if (!createFaces && !createEdges) return;

            var convexHullFaceList = new List<TriangleFace>();
            var checkSumMultipliers = new long[3];
            for (var i = 0; i < 3; i++)
                checkSumMultipliers[i] = (long)Math.Pow(Constants.CubeRootOfLongMaxValue, i);
            var alreadyCreatedFaces = new HashSet<long>();
            foreach (var cvxFace in convexHull.Result.Faces)
            {
                var faceVertices = cvxFace.Vertices;
                var orderedIndices = faceVertices.Select(v => v.IndexInList).ToList();
                orderedIndices.Sort();
                var checksum = orderedIndices.Select((t, j) => t * checkSumMultipliers[j]).Sum();
                if (alreadyCreatedFaces.Contains(checksum)) continue;
                alreadyCreatedFaces.Add(checksum);
                convexHullFaceList.Add(new TriangleFace(faceVertices, new Vector3(cvxFace.Normal), false));
            }
            Faces = convexHullFaceList.ToArray();
            if (createEdges)
                Edges = MakeEdges(Faces, Vertices);
            if (createFaces)
                TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out Volume, out Center);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TVGLConvexHull"/> class.
        /// </summary>
        /// <param name="allVertices">All vertices.</param>
        /// <param name="convexHullPoints">The convex hull points.</param>
        /// <param name="convexHullFaceIndices">The convex hull face indices.</param>
        /// <param name="tolerance">The tolerance.</param>
        internal TVGLConvexHull(IList<Vertex> allVertices, IList<Vertex> convexHullPoints,
            IList<int> convexHullFaceIndices, double tolerance)
        {
            Vertices = convexHullPoints.ToArray();
            var numCvxHullFaces = convexHullFaceIndices.Count / 3;
            Faces = new TriangleFace[numCvxHullFaces];
            var checkSumMultipliers = new long[3];
            for (var i = 0; i < 3; i++)
                checkSumMultipliers[i] = (long)Math.Pow(Constants.CubeRootOfLongMaxValue, i);
            var alreadyCreatedFaces = new HashSet<long>();
            for (int i = 0; i < numCvxHullFaces; i++)
            {
                var orderedIndices = new List<int>
                    {convexHullFaceIndices[3*i], convexHullFaceIndices[3*i + 1], convexHullFaceIndices[3*i + 2]};
                orderedIndices.Sort();
                var checksum = orderedIndices.Select((t, j) => t * checkSumMultipliers[j]).Sum();
                if (alreadyCreatedFaces.Contains(checksum)) continue;
                alreadyCreatedFaces.Add(checksum);
                var faceVertices = new[]
                {
                    allVertices[convexHullFaceIndices[3*i]],
                    allVertices[convexHullFaceIndices[3*i + 1]],
                    allVertices[convexHullFaceIndices[3*i + 2]],
                };
                Faces[i] = new TriangleFace(faceVertices, false);
            }
            Edges = MakeEdges(Faces, Vertices);
            SurfaceArea = Faces.Sum(face => face.Area);
            TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out Volume, out Center);
        }

        /// <summary>
        /// Makes the edges.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="vertices">The vertices.</param>
        /// <returns>Edge[].</returns>
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
            return edgeDictionary.Values.ToArray();
        }

        #region Public Properties

        /// <summary>
        /// The surface area
        /// </summary>
        public readonly double SurfaceArea;

        /// <summary>
        /// The center
        /// </summary>
        public readonly Vector3 Center;

        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        public readonly double Volume;

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly Vertex[] Vertices;

        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly TriangleFace[] Faces;

        /// <summary>
        /// Gets whether the convex hull creation was successful.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly bool Succeeded;

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public readonly Edge[] Edges;

        #endregion Public Properties
    }
}