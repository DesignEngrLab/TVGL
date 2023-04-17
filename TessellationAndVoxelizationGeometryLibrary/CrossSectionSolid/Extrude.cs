// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Extrude.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;



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
        /// <param name="loops">The loops.</param>
        /// <param name="extrudeDirection">The extrude direction.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <param name="midPlane">if set to <c>true</c> [mid plane].</param>
        /// <returns>TessellatedSolid.</returns>
        public static TessellatedSolid ExtrusionSolidFrom3DLoops(this IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
            double extrusionHeight, bool midPlane = false)
        {
            return new TessellatedSolid(ExtrusionFacesFrom3DLoops(loops, extrudeDirection, extrusionHeight, midPlane), false, false);
        }

        /// <summary>
        /// Create the Triangle Faces for a new Tessellated Solid by extruding the given loop along the given normal.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="extrudeDirection">The extrude direction.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <param name="midPlane">if set to <c>true</c> [mid plane].</param>
        /// <returns>List&lt;TriangleFace&gt;.</returns>
        public static List<TriangleFace> ExtrusionFacesFrom3DLoops(this IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
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
        /// <returns>List&lt;TriangleFace&gt;.</returns>
        public static List<TriangleFace> ExtrusionFacesFrom2DPolygons(this IEnumerable<IEnumerable<Vector2>> paths, Vector3 basePlaneNormal,
            double basePlaneDistance, double extrusionHeight)
        {
            var polygons = paths.CreateShallowPolygonTrees(false);
            return polygons.SelectMany(polygon => Extrude.ExtrusionFacesFrom2DPolygons(polygon,
             basePlaneNormal, basePlaneDistance, extrusionHeight)).ToList();
        }

        //public static List<TriangleFace> ExtrusionFacesFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
        //        double basePlaneDistance, double extrusionHeight)
        //{
        //    var vectors = ExtrusionFaceVectorsFrom2DPolygons(polygon, basePlaneNormal, basePlaneDistance, extrusionHeight);
        //    var polyFaces = new List<TriangleFace>(vectors.Count);
        //    var i = 0;
        //    foreach(var (A, B, C) in vectors)
        //    {
        //        polyFaces.Add(new TriangleFace(new Vertex(A), new Vertex(B), new Vertex(C)));
        //        i++;
        //    }
        //    return polyFaces;
        //}

        /// <summary>
        /// Create the triangular faces of an extruded solid from polygons.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;TriangleFace&gt;.</returns>

        public static IEnumerable<TriangleFace> ExtrusionFacesFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
                double basePlaneDistance, double extrusionHeight)
        {
            foreach (var triple in ExtrusionFaceVectorsFrom2DPolygons(polygon, basePlaneNormal,
               basePlaneDistance, extrusionHeight))
            {
                yield return new TriangleFace(new Vertex(triple.A), new Vertex(triple.B),
                    new Vertex(triple.C));
            }
        }


        /// <summary>
        /// Create the triangular faces of an extruded solid from polygons.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;TriangleFace&gt;.</returns>
        public static List<(Vector3 A, Vector3 B, Vector3 C)> ExtrusionFaceVectorsFrom2DPolygons(this Polygon polygon, Vector3 basePlaneNormal,
               double basePlaneDistance, double extrusionHeight)
        {
            var triangleIndices = polygon.TriangulateToIndices().ToList();
            return ExtrusionFaceVectorsFrom2DPolygons(polygon, triangleIndices, basePlaneNormal, basePlaneDistance, extrusionHeight);
        }

        /// <summary>
        /// Extrusions the face vectors from2 d polygons.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="triangleIndices">The triangle indices.</param>
        /// <param name="basePlaneNormal">The base plane normal.</param>
        /// <param name="basePlaneDistance">The base plane distance.</param>
        /// <param name="extrusionHeight">Height of the extrusion.</param>
        /// <returns>List&lt;System.ValueTuple&lt;Vector3, Vector3, Vector3&gt;&gt;.</returns>
        public static List<(Vector3 A, Vector3 B, Vector3 C)> ExtrusionFaceVectorsFrom2DPolygons(this Polygon polygon,
            List<(int A, int B, int C)> triangleIndices,
            Vector3 basePlaneNormal, double basePlaneDistance, double extrusionHeight)
        {
            PolygonBooleanBase.NumberVerticesAndGetPolygonVertexDelimiter(polygon);
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
                    var newVertex = position3D.Multiply(rotateTransform) + basePlaneDistance * basePlaneNormal;
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
                    var newVertex = position3D.Multiply(rotateTransform) + basePlaneDistance * basePlaneNormal;
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
