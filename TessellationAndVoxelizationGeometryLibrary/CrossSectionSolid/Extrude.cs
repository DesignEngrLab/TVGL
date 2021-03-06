﻿// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    /// Extrude functions
    /// </summary>
    public static class Extrude
    {
        /// <summary>
        /// Creates a Tesselated Solid by extruding the given loop along the given normal.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static TessellatedSolid ExtrusionSolidFrom3DLoops(this IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
            double extrusionHeight, bool midPlane = false)
        {
            return new TessellatedSolid(ExtrusionFacesFrom3DLoops(loops, extrudeDirection, extrusionHeight, midPlane), false, false);
        }

        /// <summary>
        /// Create the Polygonal Faces for a new Tessellated Solid by extruding the given loop along the given normal.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static List<PolygonalFace> ExtrusionFacesFrom3DLoops(this IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
            double extrusionHeight, bool midPlane = false)
        {
            // for consistency with adding the extrusionHeight to the base plane, negate if it comes in negative
            if (extrusionHeight < 0)
            {
                extrusionHeight = -extrusionHeight;
                extrudeDirection = -1 * extrudeDirection;
            }
            // find transform to the XY plane and store the backTransform (the transform back to the original)
            var transform = MiscFunctions.TransformToXYPlane(extrudeDirection, out var backTransform);
            // make paths, the 2D polygons represening the 3D loops
            var paths = loops.Select(loop => loop.ProjectTo2DCoordinates(transform, 0, true));
            // the basePlaneDistance defines the plane closer to the origin. we can get this from the any input coordinate
            var basePlaneDistance = extrudeDirection.Dot(loops.First().First());
            if (midPlane) basePlaneDistance -= extrusionHeight / 2.0;
            var polygons = paths.CreateShallowPolygonTrees(false);
            return polygons.SelectMany(polygon => Extrude.ExtrusionFacesFrom2DPolygons(polygon,
             extrudeDirection, basePlaneDistance, extrusionHeight)).ToList();
        }


        /// <summary>
        /// Create the triangular faces of an extruded solid from 2D paths.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        public static List<PolygonalFace> ExtrusionFacesFrom2DPolygons(this IEnumerable<IEnumerable<Vector2>> paths, Vector3 basePlaneNormal,
            double basePlaneDistance, double extrusionHeight)
        {
            var polygons = paths.CreateShallowPolygonTrees(false);
            return polygons.SelectMany(polygon => Extrude.ExtrusionFacesFrom2DPolygons(polygon,
             basePlaneNormal, basePlaneDistance, extrusionHeight)).ToList();
        }

        //public static List<PolygonalFace> ExtrusionFacesFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
        //        double basePlaneDistance, double extrusionHeight)
        //{
        //    var vectors = ExtrusionFaceVectorsFrom2DPolygons(polygon, basePlaneNormal, basePlaneDistance, extrusionHeight);
        //    var polyFaces = new List<PolygonalFace>(vectors.Count);
        //    var i = 0;
        //    foreach(var (A, B, C) in vectors)
        //    {
        //        polyFaces.Add(new PolygonalFace(new Vertex(A), new Vertex(B), new Vertex(C)));
        //        i++;
        //    }
        //    return polyFaces;
        //}

        /// <summary>
        /// Create the triangular faces of an extruded solid from polygons.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        /// 

        public static IEnumerable<PolygonalFace> ExtrusionFacesFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
                double basePlaneDistance, double extrusionHeight)
        {
            foreach (var triple in ExtrusionFaceVectorsFrom2DPolygons(polygon, basePlaneNormal,
               basePlaneDistance, extrusionHeight))
            {
                yield return new PolygonalFace(new Vertex(triple.A), new Vertex(triple.B),
                    new Vertex(triple.C));
            }
        }


        /// <summary>
        /// Create the triangular faces of an extruded solid from polygons.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        public static List<(Vector3 A, Vector3 B, Vector3 C)> ExtrusionFaceVectorsFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
               double basePlaneDistance, double extrusionHeight)
        {
            var triangleIndices = polygon.TriangulateToIndices().ToList();
            return ExtrusionFaceVectorsFrom2DPolygons(polygon, triangleIndices, basePlaneNormal, basePlaneDistance, extrusionHeight);
        }

        public static List<(Vector3 A, Vector3 B, Vector3 C)> ExtrusionFaceVectorsFrom2DPolygons(this Polygon polygon,
            List<(int A, int B, int C)> triangleIndices,
            Vector3 basePlaneNormal, double basePlaneDistance, double extrusionHeight)
        {
            MiscFunctions.TransformToXYPlane(basePlaneNormal, out var rotateTransform);
            #region Make Base faces
            var int2VertexDict = new Dictionary<int, Vector3>();
            var baseVertices = new List<List<Vector3>>();
            foreach (var poly in polygon.AllPolygons)
            {
                var vertexLoop = new List<Vector3>();
                baseVertices.Add(vertexLoop);
                foreach (var polyVertex in poly.Vertices)
                {
                    var position3D = new Vector3(polyVertex.X, polyVertex.Y, 0);
                    var newVertex = position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal;
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(polyVertex.IndexInList, newVertex);
                }
            }
            var result = new List<(Vector3 A, Vector3 B, Vector3 C)>();
            foreach (var (A, B, C) in triangleIndices)
            {
                result.Add((int2VertexDict[C], int2VertexDict[B], int2VertexDict[A]));
            }

            #endregion
            #region Make Top faces
            int2VertexDict.Clear();
            var topVertices = new List<List<Vector3>>();
            basePlaneDistance += extrusionHeight;
            foreach (var poly in polygon.AllPolygons)
            {
                var vertexLoop = new List<Vector3>();
                topVertices.Add(vertexLoop);
                foreach (var polyVertex in poly.Vertices)
                {
                    var position3D = new Vector3(polyVertex.X, polyVertex.Y, 0);
                    var newVertex = position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal;
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(polyVertex.IndexInList, newVertex);
                }
            }
            foreach (var (A, B, C) in triangleIndices)
            {
                result.Add((int2VertexDict[A], int2VertexDict[B], int2VertexDict[C]));
            }

            #endregion
            #region Make Faces on the sides
            //The normals of the faces are dependent on the whether the loops are ordered correctly from the view of the extrude direction
            //This influences which order the vertices are used to create triangles.
            for (var index = 0; index < baseVertices.Count; index++)
            {
                var topLoop = topVertices[index];
                var baseLoop = baseVertices[index];
                for (int i = 0, j = topLoop.Count - 1; i < topLoop.Count; j = i++)
                {
                    result.Add((topLoop[j], baseLoop[j], topLoop[i]));
                    result.Add((topLoop[i], baseLoop[j], baseLoop[i]));
                }
            }
            #endregion
            return result;
        }
    }
}
