// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    ///     The Convex Hull of a Tesselated Solid
    /// </summary>
    public class TVGLConvexHull
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TVGLConvexHull" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public TVGLConvexHull(TessellatedSolid ts) : this(ts.Vertices, ts.SameTolerance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TVGLConvexHull" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public TVGLConvexHull(IList<Vertex> vertices, double tolerance)
        {
            var convexHull = ConvexHull.Create(vertices, tolerance);
            if (convexHull.Result == null) return;
            Vertices = convexHull.Result.Points.ToArray();
            var convexHullFaceList = new List<PolygonalFace>();
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
                convexHullFaceList.Add(new PolygonalFace(faceVertices, new Vector3(cvxFace.Normal), false));
            }
            Faces = convexHullFaceList.ToArray();
            Edges = MakeEdges(Faces, Vertices);
            TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out Volume, out Center);
        }

        internal TVGLConvexHull(IList<Vertex> allVertices, IList<Vertex> convexHullPoints,
            IList<int> convexHullFaceIndices, double tolerance)
        {
            Vertices = convexHullPoints.ToArray();
            var numCvxHullFaces = convexHullFaceIndices.Count / 3;
            Faces = new PolygonalFace[numCvxHullFaces];
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
                Faces[i] = new PolygonalFace(faceVertices, false);
            }
            Edges = MakeEdges(Faces, Vertices);
            SurfaceArea = Faces.Sum(face => face.Area);
            TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out Volume, out Center);
        }

        private static Edge[] MakeEdges(IEnumerable<PolygonalFace> faces, IList<Vertex> vertices)
        {
            var numVertices = vertices.Count;
            var vertexIndices = new Dictionary<Vertex, int>();
            for (var i = 0; i < vertices.Count; i++)
                vertexIndices.Add(vertices[i], i);
            var edgeDictionary = new Dictionary<long, Edge>();
            foreach (var face in faces)
            {
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var fromVertexIndex = vertexIndices[fromVertex];
                    var toVertex = face.Vertices[j == lastIndex ? 0 : j + 1];
                    var toVertexIndex = vertexIndices[toVertex];
                    long checksum = fromVertexIndex < toVertexIndex
                        ? fromVertexIndex + numVertices * toVertexIndex
                        : toVertexIndex + numVertices * fromVertexIndex;

                    if (edgeDictionary.ContainsKey(checksum))
                    {
                        var edge = edgeDictionary[checksum];
                        edge.OtherFace = face;
                        face.AddEdge(edge);
                    }
                    else edgeDictionary.Add(checksum, new Edge(fromVertex, toVertex, face, null, false, checksum));
                }
            }
            return edgeDictionary.Values.ToArray();
        }

        #region Public Properties

        /// <summary>
        ///     The surface area
        /// </summary>
        public readonly double SurfaceArea;

        /// <summary>
        ///     The center
        /// </summary>
        public readonly Vector3 Center;

        /// <summary>
        ///     The volume of the Convex Hull.
        /// </summary>
        public readonly double Volume;

        /// <summary>
        ///     The vertices of the ConvexHull
        /// </summary>
        public readonly Vertex[] Vertices;

        /// <summary>
        ///     Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly PolygonalFace[] Faces;

        /// <summary>
        ///     Gets whether the convex hull creation was successful.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly bool Succeeded;

        /// <summary>
        ///     Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public readonly Edge[] Edges;

        #endregion Public Properties
    }
}