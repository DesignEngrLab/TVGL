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
    public static partial class ConvexHullAlgorithm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public static bool Create(this TessellatedSolid ts, out ConvexHull3D convexHull) 
            => Create(ts.Vertices, ts.SameTolerance, out convexHull);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// Optionally can choose to create faces and edges. Cannot make edges without faces.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="createFaces">if set to <c>true</c> [create faces].</param>
        /// <param name="createEdges">if set to <c>true</c> [create edges].</param>
        public static bool Create(this IList<Vertex> vertices, double tolerance, out ConvexHull3D convexHull, 
            bool createFaces = true, bool createEdges = true)
        {
            Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
            var plane = new Plane(distance, planeNormal);
            var closeToPlane = plane.CalculateMaxError(vertices.Select(v => v.Coordinates)) < Constants.DefaultPlaneDistanceTolerance;
            if (closeToPlane)
            {
                var coords2D = vertices.Select(v => v.Coordinates).ProjectTo2DCoordinates(planeNormal, out _);
                if (coords2D.Area() < 0) planeNormal *= -1;
                coords2D.Get2DConvexHull();
                //todo: this is not complete...does it need to be?
                return;
            }
            var convexHull = ConvexHull.Create(vertices, tolerance);
            if (convexHull.Result == null) return;
            Vertices = convexHull.Result.Points;
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
        /// Initializes a new instance of the <see cref="ConvexHull3D"/> class.
        /// </summary>
        /// <param name="allVertices">All vertices.</param>
        /// <param name="convexHullPoints">The convex hull points.</param>
        /// <param name="convexHullFaceIndices">The convex hull face indices.</param>
        /// <param name="tolerance">The tolerance.</param>
        internal ConvexHull3D(IList<Vertex> allVertices, IList<Vertex> convexHullPoints,
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
            TessellatedSolid.CalculateVolumeAndCenter(Faces, tolerance, out Volume, out Center);
        }
    }
}