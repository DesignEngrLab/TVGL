using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// Extrude functions
    /// </summary>
    public static class Extrude
    {
        /// <summary>
        /// Creates a Tessellated Solid by extruding the given loop along the given normal.
        /// Currently, this function recreates the Vertices, so no prior references will impact result.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="normal"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static TessellatedSolid FromLoops(IEnumerable<IEnumerable<Vertex>> loops, double[] normal,
            double distance)
        {
            var enumerable = loops as IEnumerable<Vertex>[] ?? loops.ToArray();
            var loopsWithoutVertices = enumerable.Select(loop => loop.Select(vertex => vertex.Position).ToList()).ToList();
            return FromLoops(loopsWithoutVertices, normal, distance);
        }

        /// <summary>
        /// Creates a Tesselated Solid by extruding the given loop along the given normal.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="extrudeDirection"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static TessellatedSolid FromLoops(IEnumerable<IEnumerable<double[]>> loops, double[] extrudeDirection, 
            double distance)
        {
            //This simplifies the cases we have to handle by always extruding in the positive direction
            if (distance < 0)
            {
                distance = -distance;
                extrudeDirection = extrudeDirection.multiply(-1);
            }

            //First, make sure we are using "clean" loops. (e.g. not connected to any faces or edges
            var cleanLoops = new List<List<Vertex>>();
            var i = 0;
            foreach (var loop in loops)
            {
                var cleanLoop = new List<Vertex>();
                foreach (var vertexPosition in loop)
                {
                    cleanLoop.Add(new Vertex(vertexPosition, i));
                    i++;
                }
                cleanLoops.Add(cleanLoop);
            }
            var distanceFromOriginAlongDirection = extrudeDirection.dotProduct(cleanLoops.First().First().Position);

            //First, triangulate the loops
            var listOfFaces = new List<PolygonalFace>();
            List<List<int>> groupsOfLoops;
            var backTransform = new double[,] {};
            var paths = cleanLoops.Select(loop => MiscFunctions.Get2DProjectionPointsReorderingIfNecessary(loop.ToArray(), extrudeDirection, out backTransform)).ToList();
            var points2D = paths.Select(path => path.ToArray()).ToList();

            var triangles = new List<Vertex[]>();
            try
            {
                bool[] isPositive = null;
                var triangleFaceList = TriangulatePolygon.Run2D(points2D, out groupsOfLoops, ref isPositive);
                if(!triangleFaceList.Any()) throw new Exception();
                foreach (var face in triangleFaceList)
                {
                    triangles.AddRange(face);
                }
            }
            catch
            {
                try
                {
                    //Do some polygon functions to clean up issues and try again
                    paths = PolygonOperations.UnionEvenOdd(paths);
                    paths = PolygonOperations.OffsetSquare(paths, distance/1000);
                    paths = PolygonOperations.OffsetSquare(paths, -distance/1000);

                    //Since triangulate polygon needs the points to have references to their vertices, we need to add vertex references to each point
                    //This also means we need to recreate cleanLoops
                    //Also, give the vertices indices.
                    cleanLoops = new List<List<Vertex>>();
                    var j = 0;
                    foreach (var path in paths)
                    {
                        var cleanLoop = new List<Vertex>();
                        foreach (var point in path)
                        {
                            var position = new[] { point.X, point.Y, 0.0, 1.0 };
                            var vertexPosition1 = backTransform.multiply(position).Take(3).ToArray();
                            //The point has been located back to its original position. It is not necessarily the correct distance along the cutting plane normal.
                            //So, we must move it to be on the plane
                            //This next line gets a second vertex to use for the point on plane function
                            var vertexPosition2 = vertexPosition1.add(extrudeDirection.multiply(5));
                            var vertex = MiscFunctions.PointOnPlaneFromIntersectingLine(extrudeDirection,
                                distanceFromOriginAlongDirection, new Vertex(vertexPosition1),
                                new Vertex(vertexPosition2));
                            vertex.IndexInList = j;
                            point.References.Add(vertex);
                            cleanLoop.Add(vertex);
                            j++;
                        }
                        cleanLoops.Add(cleanLoop);
                    }

                    points2D = paths.Select(path => path.ToArray()).ToList();
                    bool[] isPositive = null;
                    var triangleFaceList = TriangulatePolygon.Run2D(points2D, out groupsOfLoops, ref isPositive);
                    foreach (var face in triangleFaceList)
                    {
                        triangles.AddRange(face);
                    }
                }
                catch
                {
                    throw new Exception("Tried extrusion twice and failed.");
                }
            }
            
            

            //Second, build up the a set of duplicate vertices
            var pairedVertices = new Dictionary<Vertex, Vertex>();
            var vertices = new HashSet<Vertex>();
            foreach (var loop in cleanLoops)
            {
                foreach (var vertex in loop)
                {
                    vertices.Add(vertex);  
                }
            }
            foreach (var vertex in vertices)
            {
                var newVertex = new Vertex(vertex.Position.add(extrudeDirection.multiply(distance)));
                pairedVertices.Add(vertex, newVertex);
            }

            //Third, create the triangles on the two ends
            //var triangleDictionary = new Dictionary<PolygonalFace, PolygonalFace>();
            var topFaces = new List<PolygonalFace>();
            foreach (var triangle in triangles)
            {
                //Create the triangle in plane with the loops
                var v1 = triangle[1].Position.subtract(triangle[0].Position);
                var v2 = triangle[2].Position.subtract(triangle[0].Position);

                //This model reverses the triangle vertex ordering as necessary to line up with the normal.
                var topTriangle = v1.crossProduct(v2).dotProduct(extrudeDirection.multiply(-1)) < 0
                    ? new PolygonalFace(triangle.Reverse(), extrudeDirection.multiply(-1), true)
                    : new PolygonalFace(triangle, extrudeDirection.multiply(-1), true);
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
            for (var j = 0; j < cleanLoops.Count; j++)
            {
                var loop = cleanLoops[j];

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
                if(firstFace == null) throw new Exception("Did not find face with both the vertices");


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

            return new TessellatedSolid(listOfFaces);
        }
    }
}
