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
using System.Diagnostics;
using System.Linq;
using MIConvexHull;
using StarMathLib;

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
        public TVGLConvexHull(TessellatedSolid ts)
        {
            Succeeded = false;

            //Always do the config, since it was breaking about 50% of the time without.
            var config = new ConvexHullComputationConfig
            {
                PointTranslationType = PointTranslationType.TranslateInternal,
                PlaneDistanceTolerance = ts.SameTolerance,
                PointTranslationGenerator = ConvexHullComputationConfig.RandomShiftByRadius(Constants.ConvexHullRadiusForRobustness)
            };

            var convexHull = ConvexHull.Create(ts.Vertices, config);
            Vertices = convexHull.Points.ToArray();
            var convexHullFaceList = new List<PolygonalFace>();
            var checkSumMultipliers = new long[3];
            for (var i = 0; i < 3; i++)
                checkSumMultipliers[i] = (long) Math.Pow(Constants.CubeRootOfLongMaxValue, i);
            var alreadyCreatedFaces = new HashSet<long>();
            foreach (var cvxFace in convexHull.Faces)
            {
                var vertices = cvxFace.Vertices;
                var orderedIndices = vertices.Select(v => v.IndexInList).ToList();
                orderedIndices.Sort();
                var checksum = orderedIndices.Select((t, j) => t*checkSumMultipliers[j]).Sum();
                if (alreadyCreatedFaces.Contains(checksum)) continue;
                alreadyCreatedFaces.Add(checksum);
                convexHullFaceList.Add(new PolygonalFace(vertices, cvxFace.Normal, false));
            }
            Faces = convexHullFaceList.ToArray();
            Edges = MakeEdges(Faces, Vertices);
            TessellatedSolid.DefineCenterVolumeAndSurfaceArea(Faces, out Center, out Volume, out SurfaceArea);
            if (ts.Volume < 0.1)
            {
                //This solid has a small volume. Relax the constraint.
                Succeeded = Volume > ts.Volume || Volume.IsPracticallySame(ts.Volume, ts.Volume / 10);
            }
            else
            {
                //Use a loose tolerance based on the size of the solid, since accuracy is not terribly important
                Succeeded = Volume > ts.Volume || Volume.IsPracticallySame(ts.Volume, ts.Volume / 1000);
            }


            if (Succeeded) return;
            //Presenter.ShowWithConvexHull(ts);
            //Else, why did it not succeed?
            if (Volume < 0)
            {
                Debug.WriteLine("ConvexHullCreation failed to create a positive volume");
            }
            else if (Volume < ts.Volume)
            {
                var diff = ts.Volume - Volume;
                Debug.WriteLine("ConvexHullCreation failed to created a larger volume than the solid by " + diff +
                                " [mm^3]. The Solid's volume was " + ts.Volume + " [mm^3].");
            }
            else
            {
                Debug.WriteLine("Error in implementation of ConvexHull3D or Volume Calculation");
            }
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
                        ? fromVertexIndex + numVertices*toVertexIndex
                        : toVertexIndex + numVertices*fromVertexIndex;

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