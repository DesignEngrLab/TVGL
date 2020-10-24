// Copyright 2015-2020 Design Engineering Lab
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
        /// <summary>
        /// Create the triangular faces of an extruded solid from polygons.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        /// 

        public static List<PolygonalFace> ExtrusionFacesFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
                double basePlaneDistance, double extrusionHeight)
        {
            List<int[]> triangleIndices;
            #region First, run the triangulate polygons to define how the ends of the extruded shape will be defined

            //try
            //{
                triangleIndices = polygon.TriangulateToIndices();
            //}
            //catch
            //{
            //    try
            //    {
            //        //Do some polygon functions to clean up issues and try again
            //        //This is important because the Get2DProjections may produce invalid paths and because
            //        //triangulate will try 3 times before throwing the exception to go to the catch.
            //        //Do some polygon functions to clean up issues and try again
            //        polygon = polygon.OffsetMiter(extrusionHeight / 1000)[0];
            //        polygon = polygon.OffsetMiter(-extrusionHeight / 1000)[0];
            //        triangleIndices = polygon.TriangulateToIndices();
            //    }
            //    catch (Exception exc)
            //    {
            //        Debug.WriteLine("Tried extrusion three-times and it failed." + exc.Message);
            //        return new List<PolygonalFace>();
            //    }
            //}
            #endregion

            MiscFunctions.TransformToXYPlane(basePlaneNormal, out var rotateTransform);
            #region Make Base faces
            var int2VertexDict = new Dictionary<int, Vertex>();
            var baseVertices = new List<List<Vertex>>();
            var vertexID = 0;
            foreach (var loop in polygon.AllPolygons)
            {
                var vertexLoop = new List<Vertex>();
                baseVertices.Add(vertexLoop);
                foreach (var position2D in loop.Path)
                {
                    var position3D = new Vector3(position2D, 0);
                    var newVertex = new Vertex(position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal, vertexID);
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(vertexID, newVertex);
                    vertexID++;
                }
            }
            var result = new List<PolygonalFace>();
            foreach (var triangle in triangleIndices)
                result.Add(new PolygonalFace(new[] { int2VertexDict[triangle[2]],
                        int2VertexDict[triangle[1]], int2VertexDict[triangle[0]] }));
            #endregion
            #region Make Top faces
            int2VertexDict.Clear();
            var topVertices = new List<List<Vertex>>();
            vertexID = 0;
            basePlaneDistance += extrusionHeight;
            foreach (var loop in polygon.AllPolygons)
            {
                var vertexLoop = new List<Vertex>();
                topVertices.Add(vertexLoop);
                foreach (var position2D in loop.Path)
                {
                    var position3D = new Vector3(position2D, 0);
                    var newVertex = new Vertex(position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal, vertexID);
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(vertexID, newVertex);
                    vertexID++;
                }
            }
            foreach (var triangle in triangleIndices)
                result.Add(new PolygonalFace(new[] { int2VertexDict[triangle[0]],
                        int2VertexDict[triangle[1]], int2VertexDict[triangle[2]] }));
            #endregion
            #region Make Faces on the sides
            //The normals of the faces are dependent on the whether the loops are ordered correctly from the view of the extrude direction
            //This influences which order the vertices are used to create triangles.
            var index = 0;
            foreach (var vectorLoop in polygon.AllPolygons)
            {
                var topLoop = topVertices[index];
                var baseLoop = baseVertices[index];
                result.Add(new PolygonalFace(new[] { topLoop[^1], baseLoop[^1], topLoop[0] }));
                result.Add(new PolygonalFace(new[] { topLoop[0], baseLoop[^1], baseLoop[0] }));
                for (int i = 1; i < topLoop.Count; i++)
                {
                    result.Add(new PolygonalFace(new[] { topLoop[i - 1], baseLoop[i - 1], topLoop[i] }));
                    result.Add(new PolygonalFace(new[] { topLoop[i], baseLoop[i - 1], baseLoop[i] }));
                }
            }
            #endregion
            return result;
        }
    }
}
