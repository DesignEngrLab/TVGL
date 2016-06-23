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
    ///     The Convex Hull of a Tesselated Solid
    /// </summary>
    public class TVGLConvexHull
    {
        /// <summary>
        ///     Gets the convex hull, given a list of vertices
        /// </summary>
        /// <param name="allVertices"></param>
        public TVGLConvexHull(IList<Vertex> allVertices)
        {
            var convexHull = ConvexHull.Create(allVertices);
            Vertices = convexHull.Points.ToArray();
            var numCvxFaces = convexHull.Faces.Count();
            if (numCvxFaces < 3)
            {
                var config = new ConvexHullComputationConfig
                {
                    PointTranslationType = PointTranslationType.TranslateInternal,
                    PlaneDistanceTolerance = Constants.ConvexHullRadiusForRobustness,
                    // the translation radius should be lower than PlaneDistanceTolerance / 2
                    PointTranslationGenerator =
                        ConvexHullComputationConfig.RandomShiftByRadius(Constants.ConvexHullRadiusForRobustness)
                };
                convexHull = ConvexHull.Create(allVertices, config);
                Vertices = convexHull.Points.ToArray();
                numCvxFaces = convexHull.Faces.Count();
                if (numCvxFaces < 3)
                {
                    Succeeded = false;
                    return;
                }
            }
            var convexHullFaceList = new List<PolygonalFace>();
            var checkSumMultipliers = new long[3];
            for (var i = 0; i < 3; i++)
                checkSumMultipliers[i] = (long)Math.Pow(allVertices.Count, i);
            foreach (var cvxFace in convexHull.Faces)
            {
                var verts = cvxFace.Vertices;
                var v1 = verts[1].Position.subtract(verts[0].Position);
                var v2 = verts[2].Position.subtract(verts[0].Position);
                convexHullFaceList.Add(v1.crossProduct(v2).dotProduct(cvxFace.Normal) < 0
                    ? new PolygonalFace(verts.Reverse(), cvxFace.Normal, false)
                    : new PolygonalFace(verts, cvxFace.Normal, false));
            }
            Faces = convexHullFaceList.ToArray();
            List<Edge> partlyDefinedEdges;
            List<Tuple<Edge, List<PolygonalFace>>> overDefinedEdges;
            var edgeList = TessellatedSolid.MakeEdges(Faces, false, out overDefinedEdges, out partlyDefinedEdges);
            Succeeded = !partlyDefinedEdges.Any() && !overDefinedEdges.Any();
            Edges = TessellatedSolid.CompleteEdgeArray(edgeList);
            TessellatedSolid.DefineCenterVolumeAndSurfaceArea(Faces, out Center, out Volume, out SurfaceArea);
        }

        #region Public Properties

        /// <summary>
        ///     The surface area
        /// </summary>
        public readonly double SurfaceArea;

        /// <summary>
        ///     The center
        /// </summary>
        public readonly double[] Center;

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

        #endregion
    }
}