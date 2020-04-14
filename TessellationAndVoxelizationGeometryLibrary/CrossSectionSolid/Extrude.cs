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
        /// Setting midPlane to true, extrudes half forward and half reverse.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static TessellatedSolid ExtrusionSolidFrom3DLoops(IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
            double extrusionHeight, bool midPlane = false)
        {
            return new TessellatedSolid(ExtrusionFacesFrom3DLoops(loops, extrudeDirection, extrusionHeight, midPlane), null, false);
        }

        /// <summary>
        /// Create the Polygonal Faces for a new Tessellated Solid by extruding the given loop along the given normal.
        /// Setting midPlane to true, extrudes half forward and half reverse.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static List<PolygonalFace> ExtrusionFacesFrom3DLoops(IEnumerable<IEnumerable<Vector3>> loops, Vector3 extrudeDirection,
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
            var paths = loops.Select(loop => loop.Project3DLocationsTo2DCoordinates(transform));
            // the basePlaneDistance defines the plane closer to the origin. we can get this from the any input coordinate
            var basePlaneDistance = extrudeDirection.Dot(loops.First().First());
            if (midPlane) basePlaneDistance -= extrusionHeight / 2.0;
            return ExtrusionFacesFrom2DPolygons(paths, extrudeDirection, basePlaneDistance, extrusionHeight);
        }


        public static List<PolygonalFace> ExtrusionFacesFrom2DPolygons(this IEnumerable<IEnumerable<Vector2>> paths, Vector3 basePlaneNormal,
            double basePlaneDistance, double extrusionHeight)
        {
            List<List<int[]>> triangleIndices;
            bool[] isPositive;
            #region First, run the triangulate polygons to define how the ends of the extruded shape will be defined
            try
            {
                triangleIndices = paths.Triangulate(out _, out isPositive);
            }
            catch
            {
                try
                {
                    //Do some polygon functions to clean up issues and try again
                    //This is important because the Get2DProjections may produce invalid paths and because
                    //triangulate will try 3 times before throwing the exception to go to the catch.
                    paths = PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);
                    triangleIndices = paths.Triangulate(out _, out isPositive);
                }
                catch
                {
                    try
                    {
                        //Do some polygon functions to clean up issues and try again
                        paths = PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);
                        paths = PolygonOperations.OffsetRound(paths, extrusionHeight / 1000);
                        paths = PolygonOperations.OffsetRound(paths, -extrusionHeight / 1000);
                        paths = PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);
                        triangleIndices = paths.Triangulate(out _, out isPositive);
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Tried extrusion three-times and it failed." + exc.Message);
                        return null;
                    }
                }
            }
            #endregion

            MiscFunctions.TransformToXYPlane(basePlaneNormal, out var rotateTransform);
            #region Make Base faces
            var int2VertexDict = new Dictionary<int, Vertex>();
            var baseVertices = new List<List<Vertex>>();
            var vertexID = 0;
            foreach (var loop in paths)
            {
                var vertexLoop = new List<Vertex>();
                baseVertices.Add(vertexLoop);
                foreach (var position2D in loop)
                {
                    var position3D = new Vector3(position2D, 0);
                    var newVertex = new Vertex(position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal, vertexID);
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(vertexID, newVertex);
                    vertexID++;
                }
            }
            var result = new List<PolygonalFace>();
            foreach (var polygonTriangleIndices in triangleIndices)
                foreach (var triangle in polygonTriangleIndices)
                    result.Add(new PolygonalFace(new[] { int2VertexDict[triangle[2]],
                        int2VertexDict[triangle[1]], int2VertexDict[triangle[0]] }));
            #endregion
            #region Make Top faces
            int2VertexDict.Clear();
            var topVertices = new List<List<Vertex>>();
            vertexID = 0;
            basePlaneDistance += extrusionHeight;
            foreach (var loop in paths)
            {
                var vertexLoop = new List<Vertex>();
                topVertices.Add(vertexLoop);
                foreach (var position2D in loop)
                {
                    var position3D = new Vector3(position2D, 0);
                    var newVertex = new Vertex(position3D.Transform(rotateTransform) + basePlaneDistance * basePlaneNormal, vertexID);
                    vertexLoop.Add(newVertex);
                    int2VertexDict.Add(vertexID, newVertex);
                    vertexID++;
                }
            }
            foreach (var polygonTriangleIndices in triangleIndices)
                foreach (var triangle in polygonTriangleIndices)
                    result.Add(new PolygonalFace(new[] { int2VertexDict[triangle[0]],
                        int2VertexDict[triangle[1]], int2VertexDict[triangle[2]] }));
            #endregion
            #region Make Faces on the sides
            //The normals of the faces are dependent on the whether the loops are ordered correctly from the view of the extrude direction
            //This influences which order the vertices are used to create triangles.
            var index = 0;
            foreach (var vectorLoop in paths)
            {
                var topLoop = topVertices[index];
                var baseLoop = baseVertices[index];
                if (vectorLoop.ToArray().Area() > 0 == isPositive[index]) //then loop is  in the proper direction
                {
                    result.Add(new PolygonalFace(new[] { topLoop[^1], baseLoop[^1], topLoop[0] }));
                    result.Add(new PolygonalFace(new[] { topLoop[0], baseLoop[^1], baseLoop[0] }));
                    for (int i = 1; i < topLoop.Count; i++)
                    {
                        result.Add(new PolygonalFace(new[] { topLoop[i - 1], baseLoop[i - 1], topLoop[i] }));
                        result.Add(new PolygonalFace(new[] { topLoop[i], baseLoop[i - 1], baseLoop[i] }));
                    }
                }
                else
                {
                    result.Add(new PolygonalFace(new[] { topLoop[^1], topLoop[0], baseLoop[^1] }));
                    result.Add(new PolygonalFace(new[] { topLoop[0], baseLoop[0], baseLoop[^1] }));
                    for (int i = 1; i < topLoop.Count; i++)
                    {
                        result.Add(new PolygonalFace(new[] { topLoop[i - 1], topLoop[i], baseLoop[i - 1] }));
                        result.Add(new PolygonalFace(new[] { topLoop[i], baseLoop[i], baseLoop[i - 1] }));
                    }
                }
            }
            #endregion

            return result;
        }
    }
}
