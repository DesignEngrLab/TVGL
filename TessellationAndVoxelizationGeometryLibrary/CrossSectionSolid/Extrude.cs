using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;

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
        /// <param name="distance"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static TessellatedSolid FromLoops(Vector3[][] loops, Vector3 extrudeDirection,
            double distance, bool midPlane = false)
        {
            return new TessellatedSolid(ReturnFacesFromLoops(loops, extrudeDirection, distance, midPlane), null, false);
        }

        /// <summary>
        /// Create the Polygonal Faces for a new Tessellated Solid by extruding the given loop along the given normal.
        /// Setting midPlane to true, extrudes half forward and half reverse.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="distance"></param>
        /// <param name="midPlane"></param>
        /// <returns></returns>
        public static List<PolygonalFace> ReturnFacesFromLoops(IList<IList<Vector3>> loops, Vector3 extrudeDirection,
            double distance, bool midPlane = false)
        {
            //This simplifies the cases we have to handle by always extruding in the positive direction
            if (distance < 0)
            {
                distance = -distance;
                extrudeDirection = -1 * extrudeDirection;
            }
            //First, make the vertices that will be used on the perimeter (positioned at the loop coordinates)
            var vertexLoops = new List<List<Vertex>>();
            var count = 0;
            for (int loopI = 0; loopI < loops.Count; loopI++)
            {
                var vertexLoop = new Vertex[loops[loopI].Count];
                for (int vertexI = 0; vertexI < loops[loopI].Count; vertexI++)
                {
                    //If a midPlane extrusion, move the original vertices backwards by 1/2 the extrude distance.
                    //These vertices will be used as the base for offsetting the paired vertices forward by the 
                    //entire extrude distance.
                    if (midPlane)
                    {
                        var midPlaneVertexPosition = loops[loopI][vertexI] + (extrudeDirection * (-distance / 2));
                        vertexLoop[vertexI] = new Vertex(midPlaneVertexPosition, count);
                    }
                    else vertexLoop[vertexI] = new Vertex(loops[loopI][vertexI], count);
                    count++;
                }
                vertexLoops[loopI] = vertexLoop.ToList();
            }
            var distanceFromOriginAlongDirection = extrudeDirection.Dot(vertexLoops.First().First().Coordinates);

            //First, triangulate the loops
            var listOfFaces = new List<PolygonalFace>();
            var transform = MiscFunctions.TransformToXYPlane(extrudeDirection, out var backTransform);
            var paths = vertexLoops.Select(loop => loop.ProjectVerticesTo2DCoordinates(transform).ToArray()).ToArray();
            List<Vector2[]> points2D;
            List<Vertex[]> triangles;
            try
            {
                //Reset the list of triangles
                triangles = new List<Vertex[]>();

                //Do some polygon functions to clean up issues and try again
                //This is important because the Get2DProjections may produce invalid paths and because
                //triangulate will try 3 times before throwing the exception to go to the catch.
                paths = (Vector2[][])PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);

                //Since triangulate polygon needs the points to have references to their vertices, we need to add vertex references to each point
                //This also means we need to recreate cleanLoops
                //Also, give the vertices indices.
                vertexLoops = new List<List<Vertex>>();
                points2D = new List<Vector2[]>();
                var j = 0;
                foreach (var path in paths)
                {
                    var pathAsPoints = path.Select(p => new Vector2(p.X, p.Y)).ToArray();
                    var area = new PolygonLight(path).Area;
                    points2D.Add(pathAsPoints);
                    var cleanLoop = new List<Vertex>();
                    foreach (var point in pathAsPoints)
                    {
                        var position = new Vector3(point.X, point.Y, 0.0);
                        var vertexPosition1 = position.Transform(backTransform);
                        //The point has been located back to its original position. It is not necessarily the correct distance along the cutting plane normal.
                        //So, we must move it to be on the plane
                        //This next line gets a second vertex to use for the point on plane function
                        var vertexPosition2 = vertexPosition1 + (extrudeDirection * 5);
                        var vertex = MiscFunctions.PointOnPlaneFromIntersectingLine(extrudeDirection,
                            distanceFromOriginAlongDirection, new Vertex(vertexPosition1),
                            new Vertex(vertexPosition2));
                        vertex.IndexInList = j;
                        cleanLoop.Add(vertex);
                        j++;
                    }
                    vertexLoops.Add(cleanLoop);
                }

                bool[] isPositive = null;
                var triangleFaceList = TriangulatePolygon.Run2D(points2D, out _, ref isPositive);
                foreach (var face in triangleFaceList)
                {
                    triangles.AddRange(face);
                }
            }
            catch
            {
                try
                {
                    //Reset the list of triangles
                    triangles = new List<Vertex[]>();

                    //Do some polygon functions to clean up issues and try again
                    paths = (Vector2[][])PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);
                    paths = (Vector2[][])PolygonOperations.OffsetRound(paths, distance / 1000);
                    paths = (Vector2[][])PolygonOperations.OffsetRound(paths, -distance / 1000);
                    paths = (Vector2[][])PolygonOperations.Union(paths, true, PolygonFillType.EvenOdd);

                    //Since triangulate polygon needs the points to have references to their vertices, we need to add vertex references to each point
                    //This also means we need to recreate cleanLoops
                    //Also, give the vertices indices.
                    vertexLoops = new List<List<Vertex>>();
                    points2D = new List<Vector2[]>();
                    var j = 0;
                    foreach (var path in paths)
                    {
                        var pathAsPoints = path.Select(p => new Vector2(p.X, p.Y)).ToArray();
                        points2D.Add(pathAsPoints);
                        var cleanLoop = new List<Vertex>();
                        foreach (var point in pathAsPoints)
                        {
                            var position = new Vector3(point.X, point.Y, 0.0);
                            var vertexPosition1 = position.Transform(backTransform);
                            //The point has been located back to its original position. It is not necessarily the correct distance along the cutting plane normal.
                            //So, we must move it to be on the plane
                            //This next line gets a second vertex to use for the point on plane function
                            var vertexPosition2 = vertexPosition1 + (extrudeDirection * 5);
                            var vertex = MiscFunctions.PointOnPlaneFromIntersectingLine(extrudeDirection,
                                distanceFromOriginAlongDirection, new Vertex(vertexPosition1),
                                new Vertex(vertexPosition2));
                            vertex.IndexInList = j;
                            cleanLoop.Add(vertex);
                            j++;
                        }
                        vertexLoops.Add(cleanLoop);
                    }

                    bool[] isPositive = null;
                    var triangleFaceList = TriangulatePolygon.Run2D(points2D, out _, ref isPositive);
                    foreach (var face in triangleFaceList)
                    {
                        triangles.AddRange(face);
                    }
                }
                catch
                {
                    Debug.WriteLine("Tried extrusion twice and failed.");
                    return null;
                }
            }

            //Second, build up the a set of duplicate vertices
            var vertices = new HashSet<Vertex>();
            foreach (var vertex in vertexLoops.SelectMany(loop => loop))
            {
                vertices.Add(vertex);
            }
            var pairedVertices = new Dictionary<Vertex, Vertex>();
            foreach (var vertex in vertices)
            {
                var newVertex = new Vertex(vertex.Coordinates + (extrudeDirection * distance));
                pairedVertices.Add(vertex, newVertex);
            }

            //Third, create the triangles on the two ends
            //var triangleDictionary = new Dictionary<PolygonalFace, PolygonalFace>();
            var topFaces = new List<PolygonalFace>();
            foreach (var triangle in triangles)
            {
                //Create the triangle in plane with the loops
                var v1 = triangle[1].Coordinates.Subtract(triangle[0].Coordinates);
                var v2 = triangle[2].Coordinates.Subtract(triangle[0].Coordinates);

                //This model reverses the triangle vertex ordering as necessary to line up with the normal.
                var topTriangle = v1.Cross(v2).Dot(extrudeDirection * -1) < 0
                    ? new PolygonalFace(triangle.Reverse(), extrudeDirection * -1, true)
                    : new PolygonalFace(triangle, extrudeDirection * -1, true);
                topFaces.Add(topTriangle);
                listOfFaces.Add(topTriangle);

                //Create the triangle on the opposite side of the extrusion
                var bottomTriangle = new PolygonalFace(
                        new List<Vertex>
                        {
                            pairedVertices[triangle[0]],
                            pairedVertices[triangle[2]],
                            pairedVertices[triangle[1]]
                        }, extrudeDirection, false);
                listOfFaces.Add(bottomTriangle);
                //triangleDictionary.Add(topTriangle, bottomTriangle);
            }

            //Fourth, create the triangles on the sides 
            //The normals of the faces are dependent on the whether the loops are ordered correctly from the view of the extrude direction
            //This influences which order the vertices are used to create triangles.
            for (var j = 0; j < vertexLoops.Count; j++)
            {
                var loop = vertexLoops[j];

                //Determine if the loop direction is correct by using the top face
                var v1 = loop[0];
                var v2 = loop[1];

                //Find the face with both of these vertices
                PolygonalFace firstFace = null;
                foreach (var face in topFaces)
                {
                    if (face.Vertices[0] == v1 || face.Vertices[1] == v1 || face.Vertices[2] == v1)
                    {
                        if (face.Vertices[0] == v2 || face.Vertices[1] == v2 || face.Vertices[2] == v2)
                        {
                            firstFace = face;
                            break;
                        }
                    }
                }
                if (firstFace == null) throw new Exception("Did not find face with both the vertices");


                if (firstFace.NextVertexCCW(v1) == v2)
                {
                    //Do nothing
                }
                else if (firstFace.NextVertexCCW(v2) == v1)
                {
                    //Reverse the loop
                    loop.Reverse();
                }
                else throw new Exception();

                //The loop is now ordered correctly
                //It does not matter whether the loop is positive or negative, only that it is ordered correctly for the given extrude direction
                for (var k = 0; k < loop.Count; k++)
                {
                    var g = k + 1;
                    if (k == loop.Count - 1) g = 0;

                    //Create the new triangles
                    listOfFaces.Add(new PolygonalFace(new List<Vertex>() { loop[k], pairedVertices[loop[k]], pairedVertices[loop[g]] }));
                    listOfFaces.Add(new PolygonalFace(new List<Vertex>() { loop[k], pairedVertices[loop[g]], loop[g] }));
                }
            }

            return listOfFaces;
        }
    }
}
