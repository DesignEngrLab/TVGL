// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-26-2016
// ***********************************************************************
// <copyright file="MiscFunctions.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL
{
    /// <summary>
    ///     Miscellaneous Functions for TVGL
    /// </summary>
    public static class MiscFunctions
    {
        #region Sort Along Direction

        /// <summary>
        ///     Returns a list of sorted vertices along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="sortedVertices">The sorted vertices.</param>
        public static void SortAlongDirection(double[] direction, IEnumerable<Vertex> vertices,
            out List<Tuple<Vertex, double>> sortedVertices)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            var vertexDistances = GetVertexDistances(direction, vertices);

            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            sortedVertices = vertexDistances.OrderBy(p => p.Item2).ToList();
        }

        /// <summary>
        ///     Returns a list of sorted vertices along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="sortedVertices">The sorted vertices.</param>
        public static void SortAlongDirection(double[] direction, IEnumerable<Vertex> vertices,
            out List<Vertex> sortedVertices)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            var vertexDistances = GetVertexDistances(direction, vertices);

            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            sortedVertices = vertexDistances.OrderBy(p => p.Item2).Select(p =>p.Item1).ToList();
        }

        private static IEnumerable<Tuple<Vertex, double>> GetVertexDistances(double[] direction, IEnumerable<Vertex> vertices)
        {
            var vertexDistances = new List<Tuple<Vertex, double>>(vertices.Count());
            //Accuracy to the 15th decimal place
            var toleranceString = StarMath.EqualityTolerance.ToString(CultureInfo.InvariantCulture);
            var tolerance = int.Parse(toleranceString.Substring((toleranceString.IndexOf("-", StringComparison.Ordinal)+1)));
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction with accuracy to the 15th decimal place to match StarMath
                var d = Math.Round(direction.dotProduct(vertex.Position, 3), tolerance);
                vertexDistances.Add(new Tuple<Vertex, double>(vertex, d));
            }
            return vertexDistances;
        }

        /// <summary>
        ///     Returns a list of sorted points along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="points"></param>
        /// <param name="sortedPoints"></param>
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<Point> points,
               out List<(Point, double)> sortedPoints)
        {
            var pointDistances = GetPointDistances(directionX, directionY, points);
            sortedPoints = pointDistances.OrderBy(point => point.Item2).ToList();
        }

        /// <summary>
        ///     Returns a list of sorted points along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="points"></param>
        /// <param name="sortedPoints"></param>
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<Point> points,
               out List<Point> sortedPoints)
        {
            var pointDistances = GetPointDistances(directionX, directionY, points);
            sortedPoints = pointDistances.OrderBy(point => point.Item2).Select(p => p.Item1).ToList();
        }

        private static IEnumerable<(Point, double)> GetPointDistances(double directionX, double directionY, IEnumerable<Point> points)
        {
            var pointDistances = new List<(Point, double)>(points.Count());
            //Accuracy to the 15th decimal place
            var toleranceString = StarMath.EqualityTolerance.ToString(CultureInfo.InvariantCulture);
            var tolerance = toleranceString.Substring(toleranceString.IndexOf(".", StringComparison.Ordinal) + 1).Length;
            foreach (var point in points)
            {
                //Get distance along the search direction with accuracy to the 15th decimal place
                var d = Math.Round(directionX * point.X + directionY * point.Y, tolerance); //2D dot product
                pointDistances.Add((point, d));
            }
            return pointDistances;
        }

        /// <summary>
        ///     Returns a list of sorted PointLights along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="PointLights"></param>
        /// <param name="sortedPointLights"></param>
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<PointLight> PointLights,
               out List<(PointLight, double)> sortedPointLights, int numDecimals)
        {
            var PointLightDistances = GetPointLightDistances(directionX, directionY, PointLights, numDecimals);
            sortedPointLights = PointLightDistances.OrderBy(PointLight => PointLight.Item2).ToList();
        }

        /// <summary>
        ///     Returns a list of sorted PointLights along a set direction. 
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="PointLights"></param>
        /// <param name="sortedPointLights"></param>
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<PointLight> PointLights,
               out List<PointLight> sortedPointLights, int numDecimals)
        {
            var PointLightDistances = GetPointLightDistances(directionX, directionY, PointLights, numDecimals);
            sortedPointLights = PointLightDistances.OrderBy(PointLight => PointLight.Item2).Select(p => p.Item1).ToList();
        }

        private static IEnumerable<(PointLight, double)> GetPointLightDistances(double directionX, double directionY,
            IEnumerable<PointLight> PointLights, int numDecimals)
        {
            var PointLightDistances = new List<(PointLight, double)>(PointLights.Count());
            //Accuracy to the 15th decimal place
            foreach (var PointLight in PointLights)
            {
                //Get distance along the search direction with accuracy to the 15th decimal place
                var d = Math.Round(directionX * PointLight.X + directionY * PointLight.Y, numDecimals); //2D dot product
                PointLightDistances.Add((PointLight, d));
            }
            return PointLightDistances;
        }
        #endregion

        #region Perimeter
        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static double Perimeter(ICollection<PointLight> polygon)
        {
            var listWithStartPointAtEnd = new List<PointLight>(polygon) { polygon.First() };
            double perimeter = 0;
            for (var i = 1; i < listWithStartPointAtEnd.Count; i++)
            {
                perimeter = perimeter +
                            DistancePointToPoint(listWithStartPointAtEnd[i - 1], listWithStartPointAtEnd[i]);
            }
            return perimeter;
        }

        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static double Perimeter(ICollection<Point> polygon)
        {
            var listWithStartPointAtEnd = new List<Point>(polygon) { polygon.First() };
            double perimeter = 0;
            for (var i = 1; i < listWithStartPointAtEnd.Count; i++)
            {
                perimeter = perimeter +
                            DistancePointToPoint(listWithStartPointAtEnd[i - 1], listWithStartPointAtEnd[i]);
            }
            return perimeter;
        }

        /// <summary>
        /// Gets the Perimeter (length of a loop) of a 3D set of Vertices.
        /// </summary>
        /// <param name="polygon3D"></param>
        /// <returns></returns>
        public static double Perimeter(ICollection<Vertex> polygon3D)
        {
            var listWithStartPointAtEnd = new List<Vertex>(polygon3D) { polygon3D.First() };
            double perimeter = 0;
            for (var i = 1; i < listWithStartPointAtEnd.Count; i++)
            {
                perimeter = perimeter +
                            DistancePointToPoint(listWithStartPointAtEnd[i - 1].Position,
                                listWithStartPointAtEnd[i].Position);
            }
            return perimeter;
        }
        #endregion

        #region Dealing with Flat Patches
        /// <summary>
        /// Gets a collection of faces with distinct normals. These are the largest faces within the set with common normal. 
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="removeOpposites">if set to <c>true</c> [remove opposites].</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        public static List<PolygonalFace> FacesWithDistinctNormals(IEnumerable<PolygonalFace> faces,
            double tolerance = Constants.SameFaceNormalDotTolerance, bool removeOpposites = true)
        {
            // This is done by sorting the normals first by the x-component, then by the y and then the z. 
            // This is to avoid the O(n^2) and be more like O(n). It is a necessary but not sufficient
            // condition that faces with similar x-values in the normal (and then y and then z) will
            // likely be the same normal. So, in this manner we can then check adjacent faces in a sorted
            // set. However, sorting just in x alone may not be sufficient as the list may jump around. 
            // For example, a portion of the list may look like: { ... [0 .3 .4], [0 -.3 .4], [0, .29999, .4] }
            // comparing adjacent pairs will miss the fact that 1 and 3 and similar. But - since they have the
            // same x-component as 2, then they are not compared. Here, the chance to catch such cases by sorting
            // about all 3 cardinal directions. One could continue sorting by a dot-product with an arbitrary normal,
            // but cases where this function have failed have not been observed.
            var distinctList = faces.ToList();
            for (int k = 0; k < 3; k++)
            {
                distinctList = distinctList.OrderBy(f => f.Normal[k]).ToList();
                for (var i = distinctList.Count - 1; i > 0; i--)
                {
                    if (distinctList[i].Normal.dotProduct(distinctList[i - 1].Normal, 3).IsPracticallySame(1.0, tolerance) ||
                        (removeOpposites && distinctList[i].Normal.dotProduct(distinctList[i - 1].Normal, 3).IsPracticallySame(-1, tolerance)))
                    {
                        if (distinctList[i].Area <= distinctList[i - 1].Area) distinctList.RemoveAt(i);
                        else distinctList.RemoveAt(i - 1);
                    }
                }
            }
            return distinctList;
        }

        /// <summary>
        ///     Gets a list of flats for a given list of faces.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="minSurfaceArea">The minimum surface area.</param>
        /// <returns>List&lt;Flat&gt;.</returns>
        public static List<Flat> FindFlats(IList<PolygonalFace> faces, double tolerance = Constants.ErrorForFaceInSurface,
               int minNumberOfFacesPerFlat = 2)
        {
            //Note: This function has been optimized to run very fast for large amount of faces
            //Used hashet for "Contains" function calls 
            var usedFaces = new HashSet<PolygonalFace>();
            var listFlats = new List<Flat>();

            //Use an IEnumerable class (List) for iterating through each part, and then the 
            //"Contains" function to see if it was already used. This is actually much faster
            //than using a while loop with a ".Any" and ".First" call on the Hashset.
            foreach (var startFace in faces)
            {
                //If this faces has already been used, continue to the next face
                if (usedFaces.Contains(startFace)) continue;
                //Get all the faces that should be used on this flat
                //Use a hashset so we can use the ".Contains" function
                var flatHashSet = new HashSet<PolygonalFace> { startFace };
                var flat = new Flat(flatHashSet) {Tolerance = tolerance};
                //Stacks a fast for "Push" and "Pop".
                //Add all the adjecent faces from the first face to the stack for 
                //consideration in the while loop below.
                var stack = new Stack<PolygonalFace>(flatHashSet);
                var reDefineFlat = 3;
                while (stack.Any())
                {
                    var newFace = stack.Pop();
                    //Add new adjacent faces to the stack for consideration
                    //if the faces are already listed in the flat faces, the first
                    //"if" statement in the while loop will ignore them.
                    foreach (var adjacentFace in newFace.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue;
                        if (!flatHashSet.Contains(adjacentFace) && !usedFaces.Contains(adjacentFace) &&
                            !stack.Contains(adjacentFace) && flat.IsNewMemberOf(adjacentFace))
                        {
                            // flat.UpdateWith(adjacentFace);
                            flatHashSet.Add(newFace);
                            if (flatHashSet.Count >= reDefineFlat)
                            {
                                flat = new Flat(flatHashSet);
                                reDefineFlat *= 3;
                            }
                            stack.Push(adjacentFace);
                        }
                    }
                }
                flat = new Flat(flatHashSet);
                //Criteria of whether it should be a flat should be inserted here.
                if (flat.Faces.Count >= minNumberOfFacesPerFlat)
                    listFlats.Add(flat);
                foreach (var polygonalFace in flat.Faces)
                    usedFaces.Add(polygonalFace);
            }
            return listFlats;
        }
        #endregion


        /// <summary>
        ///     Calculate the area of any non-intersecting polygon.
        /// </summary>
        public static double AreaOfPolygon(IList<List<PointLight>> paths)
        {
            return paths.Sum(path => AreaOfPolygon(path));
        }

        public static double AreaOfPolygon(IList<Point> polygon)
        {
            return AreaOfPolygon(polygon.Select(p => p.Light).ToList());
        }

        /// <summary>
        ///     Calculate the area of any non-intersecting polygon.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns>System.Double.</returns>
        /// <reference>
        ///     Method 1: http://www.mathopenref.com/coordpolygonarea2.html
        ///     Faster Method: http://geomalgorithms.com/a01-_area.html
        /// </reference>
        public static double AreaOfPolygon(IList<PointLight> polygon)
        {
            //If less than three points, it is a line and has zero area.
            if (polygon.Count < 3) return 0.0;
            #region Method 1

            //Method 1
            //var area = 0.0;
            //var j = polygon.Count - 1; //Previous to the first vertex
            //for (var i = 0; i < polygon.Count; i++)
            //{
            //    area += (polygon[j].X + polygon[i].X) * (polygon[j].Y - polygon[i].Y);
            //    j = i; //Previous to i
            //}
            //area = -area / 2;

            #endregion

            //First, check if all x are the same. The algorithm will catch all y's and output zero,
            //But it may output a small number, even if all the x's are the same
            var xval = polygon.First().X;
            var returnZero = true;
            for (var i = 1; i < polygon.Count; i++)
            {
                if (polygon[i].X.IsPracticallySame(xval)) continue;
                returnZero = false;
                break;
            }
            if (returnZero) return 0.0;

            //Faster Method
            var area = 0.0;
            var n = polygon.Count;
            for (var i = 1; i < n - 1; i++)
            {
                area += polygon[i].X * (polygon[i + 1].Y - polygon[i - 1].Y);
            }
            //Final wrap around terms
            area += polygon[0].X * (polygon[1].Y - polygon[n - 1].Y);
            area += polygon[n - 1].X * (polygon[0].Y - polygon[n - 2].Y);
            area = area / 2;
            return area;
        }

        /// <summary>
        ///     Calculate the area of any non-intersecting polygon in 3D space (loops)
        ///     This is faster than projecting to a 2D surface first in a seperate function.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double.</returns>
        /// <references>http://geomalgorithms.com/a01-_area.html </references>
        public static double AreaOf3DPolygon(IEnumerable<Vertex> loop, double[] normal)
        {
            var ax = Math.Abs(normal[0]);
            var ay = Math.Abs(normal[1]);
            var az = Math.Abs(normal[2]);

            //Make a new list from the loop
            var vertices = new List<Vertex>(loop);
            //Add the first vertex to the end
            vertices.Add(vertices.First());

            //Choose the largest abs coordinate to ignore for projections
            var coord = 3; //ignore z-coord
            if (ax > az && (ax > ay || ax.IsPracticallySame(ay))) coord = 1; //ignore x-coord
            else if (ay > az && ay > ax) coord = 2; //ignore y-coord
            //These are the results for eqaul directions
            //if az == ax, then ignore z-coord.
            //if az == ax == ay, then ignore z-coord.
            //if ax == ay and both are greater than az, ignore the x-coord

            // compute area of the 2D projection
            // -1 so as to not include the vertex that was added to the end of the list
            var n = vertices.Count - 1;
            var i = 1;
            var area = 0.0;
            switch (coord)
            {
                case 1:
                    for (i = 1; i < n; i++)
                        area += vertices[i].Y * (vertices[i + 1].Z - vertices[i - 1].Z);
                    break;
                case 2:
                    for (i = 1; i < n; i++)
                        area += vertices[i].Z * (vertices[i + 1].X - vertices[i - 1].X);
                    break;
                case 3:
                    for (i = 1; i < n; i++)
                        area += vertices[i].X * (vertices[i + 1].Y - vertices[i - 1].Y);
                    break;
            }
            switch (coord)
            {
                case 1:
                    area += vertices[n].Y * (vertices[1].Z - vertices[n - 1].Z);
                    break;
                case 2:
                    area += vertices[n].Z * (vertices[1].X - vertices[n - 1].X);
                    break;
                case 3:
                    area += vertices[n].X * (vertices[1].Y - vertices[n - 1].Y);
                    break;
            }

            // scale to get area before projection
            var an = Math.Sqrt(ax * ax + ay * ay + az * az); // length of normal vector
            switch (coord)
            {
                case 1:
                    area *= an / (2 * normal[0]);
                    break;
                case 2:
                    area *= an / (2 * normal[1]);
                    break;
                case 3:
                    area *= an / (2 * normal[2]);
                    break;
            }
            return area;
        }

        /// <summary>
        ///     Calculate the area of any non-intersecting polygon in 3D space (loops)
        ///     This is faster than projecting to a 2D surface first in a seperate function.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double.</returns>
        /// <references>http://geomalgorithms.com/a01-_area.html </references>
        public static double AreaOf3DPolygon(IEnumerable<double[]> loop, double[] normal)
        {
            var ax = Math.Abs(normal[0]);
            var ay = Math.Abs(normal[1]);
            var az = Math.Abs(normal[2]);

            //Make a new list from the loop
            var vertices = new List<double[]>(loop);
            //Add the first vertex to the end
            vertices.Add(vertices.First());

            //Choose the largest abs coordinate to ignore for projections
            var coord = 3; //ignore z-coord
            if (ax > az && (ax > ay || ax.IsPracticallySame(ay))) coord = 1; //ignore x-coord
            else if (ay > az && ay > ax) coord = 2; //ignore y-coord
            //These are the results for eqaul directions
            //if az == ax, then ignore z-coord.
            //if az == ax == ay, then ignore z-coord.
            //if ax == ay and both are greater than az, ignore the x-coord

            // compute area of the 2D projection
            // -1 so as to not include the vertex that was added to the end of the list
            var n = vertices.Count - 1;
            var i = 1;
            var area = 0.0;
            switch (coord)
            {
                case 1:
                    for (i = 1; i < n; i++)
                        area += vertices[i][1] * (vertices[i + 1][2] - vertices[i - 1][2]);
                    break;
                case 2:
                    for (i = 1; i < n; i++)
                        area += vertices[i][2] * (vertices[i + 1][0] - vertices[i - 1][0]);
                    break;
                case 3:
                    for (i = 1; i < n; i++)
                        area += vertices[i][0] * (vertices[i + 1][1] - vertices[i - 1][1]);
                    break;
            }
            switch (coord)
            {
                case 1:
                    area += vertices[n][1] * (vertices[1][2] - vertices[n - 1][2]);
                    break;
                case 2:
                    area += vertices[n][2] * (vertices[1][0] - vertices[n - 1][0]);
                    break;
                case 3:
                    area += vertices[n][0] * (vertices[1][1] - vertices[n - 1][1]);
                    break;
            }

            // scale to get area before projection
            var an = Math.Sqrt(ax * ax + ay * ay + az * az); // length of normal vector
            switch (coord)
            {
                case 1:
                    area *= an / (2 * normal[0]);
                    break;
                case 2:
                    area *= an / (2 * normal[1]);
                    break;
                case 3:
                    area *= an / (2 * normal[2]);
                    break;
            }
            return area;
        }

        #region Split Tesselated Solid into multiple solids if faces are disconnected 
        /// <summary>
        ///     Gets all the individual solids from a tesselated solid.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        /// <exception cref="Exception"></exception>
        public static List<TessellatedSolid> GetMultipleSolids(TessellatedSolid ts)
        {
            var solids = new List<TessellatedSolid>();
            var seperateSolids = new List<List<PolygonalFace>>();
            var unusedFaces = ts.Faces.ToDictionary(face => face.IndexInList);
            while (unusedFaces.Any())
            {
                var faces = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedFaces.ElementAt(0).Value });
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (faces.Contains(face)) continue;
                    faces.Add(face);
                    unusedFaces.Remove(face.IndexInList);
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        stack.Push(adjacentFace);
                    }
                }
                seperateSolids.Add(faces.ToList());
            }
            var count = 0;
            if (seperateSolids.Count == 1)
            {
                solids.Add(ts);
                return solids;
            }
            foreach (var seperateSolid in seperateSolids)
            {
                solids.Add(new TessellatedSolid(seperateSolid));
                count = count + seperateSolid.Count;
            }
            return solids;
        }
        #endregion

        #region Flatten to 2D

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices. 
        ///     Additionally, this function will keep the loops in their original positive/negative
        ///     orientation.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="direction"></param>
        /// <param name="backTransform"></param>
        /// <param name="tolerance"></param>
        /// <param name="mergeDuplicateReferences"></param>
        /// <returns></returns>
        public static List<Point> Get2DProjectionPointsReorderingIfNecessary(IEnumerable<Vertex> loop, double[] direction, out double[,] backTransform, double tolerance = Constants.BaseTolerance,
            bool mergeDuplicateReferences = false)
        {
            var enumerable = loop as IList<Vertex> ?? loop.ToList();
            var area1 = AreaOf3DPolygon(enumerable, direction);
            var path = Get2DProjectionPoints(enumerable, direction, out backTransform).ToList();
            var area2 = AreaOfPolygon(path);
            var dif = area1 - area2;
            var successful = false;
            var attempts = 0;
            //Try up to three times if not successful, expanding the tolerance each time
            while (!successful && attempts < 4)
            {
                //For every attempt greater than zero, expand the tolerance by taking its square root
                if (attempts > 0) tolerance = Math.Sqrt(tolerance);

                try
                {
                    if (dif.IsNegligible(tolerance))
                    {
                        successful = true;
                    }
                    else
                    {
                        if ((-area1).IsPracticallySame(area2, tolerance))
                        {
                            dif = area1 + area2;
                            path.Reverse();
                            successful = true;
                        }
                        else
                        {
                            throw new Exception("area mismatch during 2D projection");
                        }
                    }
                }
                catch
                {
                    attempts++;
                }
            }
            if (attempts > 0 && attempts < 4) Debug.WriteLine("Minor area mismatch = " + dif + "  during 2D projection");
            else if (attempts == 4) throw new Exception("Major area mismatch during 2D projection. Resulting path is incorrect");

            return path;
        }



        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, double[] direction,
            bool mergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction);
            return Get2DProjectionPoints(vertices, transform, mergeDuplicateReferences);
        }

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="mergeDuplicateTolerance">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, double[] direction, double mergeDuplicateTolerance)
        {
            if (mergeDuplicateTolerance.IsNegligible()) mergeDuplicateTolerance = Constants.BaseTolerance; //Minimum allowed tolerance.
            var transform = TransformToXYPlane(direction);
            return Get2DProjectionPoints(vertices, transform, true, mergeDuplicateTolerance);
        }

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, double[] direction,
            out double[,] backTransform,
            bool mergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return Get2DProjectionPoints(vertices, transform, mergeDuplicateReferences);
        }

        /// <summary>
        ///     Returns an array of points projected using the given transform.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <returns>Point[].</returns>
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, double[,] transform,
            bool mergeDuplicateReferences = false, double sameTolerance = Constants.BaseTolerance)
        {
            var points = new List<Point>();
            var simpleCompareDict = new Dictionary<string, Point>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var stringformat = "F" + numDecimalPoints;
            foreach (var vertex in vertices)
            {
                var point = Get2DProjectionPoint(vertex, transform);
                if (!mergeDuplicateReferences)
                {
                    points.Add(new Point(vertex, point[0], point[1]));
                }
                else
                {
                    point[0] = Math.Round(point[0], numDecimalPoints);
                    point[1] = Math.Round(point[1], numDecimalPoints);
                    var lookupString = point[0].ToString(stringformat) + "|"
                                       + point[1].ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, Add reference and move to the next vertex */
                        simpleCompareDict[lookupString].References.Add(vertex);
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new Point(vertex, point[0], point[1]);
                        simpleCompareDict.Add(lookupString, point2D);
                        points.Add(point2D);
                    }
                }
            }
            return points.ToArray();
        }

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices. 
        ///     Additionally, this function will keep the loops in their original positive/negative
        ///     orientation.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="direction"></param>
        /// <param name="backTransform"></param>
        /// <param name="tolerance"></param>
        /// <param name="mergeDuplicateReferences"></param>
        /// <returns></returns>
        public static List<PointLight> Get2DProjectionPointsAsLightReorderingIfNecessary(IEnumerable<Vertex> loop, double[] direction, out double[,] backTransform, double tolerance = Constants.BaseTolerance,
            bool mergeDuplicateReferences = false)
        {
            var enumerable = loop as IList<Vertex> ?? loop.ToList();
            var area1 = AreaOf3DPolygon(enumerable, direction);
            var path = Get2DProjectionPointsAsLight(enumerable, direction, out backTransform);
            var area2 = AreaOfPolygon(path);
            var dif = area1 - area2;
            var successful = false;
            var attempts = 0;
            //Try up to three times if not successful, expanding the tolerance each time
            while (!successful && attempts < 4)
            {
                //For every attempt greater than zero, expand the tolerance by taking its square root
                if (attempts > 0) tolerance = Math.Sqrt(tolerance);

                try
                {
                    if (dif.IsNegligible(tolerance))
                    {
                        successful = true;
                    }
                    else
                    {
                        if ((-area1).IsPracticallySame(area2, tolerance))
                        {
                            dif = area1 + area2;
                            path.Reverse();
                            successful = true;
                        }
                        else
                        {
                            attempts++;
                            //throw new Exception("area mismatch during 2D projection");
                        }
                    }
                }
                catch
                {
                    attempts++;
                }
            }

            if (attempts > 0 && attempts < 4) ;//Debug.WriteLine("Minor area mismatch = " + dif + "  during 2D projection");
            else if (attempts == 4) throw new Exception("Major area mismatch during 2D projection. Resulting path is incorrect");

            return path;
        }

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static List<PointLight> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, double[] direction,
            out double[,] backTransform,
            bool mergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return Get2DProjectionPointsAsLight(vertices, transform, mergeDuplicateReferences);
        }

        /// <summary>
        ///     Returns an array of points projected along the given direction onto an x-y plane.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static List<PointLight> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, double[] direction,
            bool mergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction);
            return Get2DProjectionPointsAsLight(vertices, transform, mergeDuplicateReferences);
        }

        /// <summary>
        ///     Returns an array of points projected using the given transform.
        ///     The point z-values will be zero. This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <returns>Point[].</returns>
        public static List<PointLight> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, double[,] transform,
            bool mergeDuplicateReferences = false, double sameTolerance = Constants.BaseTolerance)
        {
            var points = new List<PointLight>();
            var simpleCompareDict = new Dictionary<string, PointLight>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var stringformat = "F" + numDecimalPoints;
            foreach (var vertex in vertices)
            {
                var point = Get2DProjectionPoint(vertex, transform);
                if (!mergeDuplicateReferences)
                {
                    points.Add(new PointLight(point[0], point[1]));
                }
                else
                {
                    point[0] = Math.Round(point[0], numDecimalPoints);
                    point[1] = Math.Round(point[1], numDecimalPoints);
                    var lookupString = point[0].ToString(stringformat) + "|"
                                       + point[1].ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, move to the next vertex */
                        continue;
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new PointLight(point[0], point[1]);
                        simpleCompareDict.Add(lookupString, point2D);
                        points.Add(point2D);
                    }
                }
            }
            return points;
        }

        public static PointLight Get2DProjectionPointAsLight(Vertex vertex, double[,] transform)
        {
            var position = Get2DProjectionPoint(vertex, transform);
            return new PointLight(position[0], position[1]);
        }

        public static double[] Get2DProjectionPoint(Vertex vertex, double[,] transform)
        {
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            pointAs4[0] = vertex.Position[0];
            pointAs4[1] = vertex.Position[1];
            pointAs4[2] = vertex.Position[2];
            pointAs4 = transform.multiply(pointAs4);
            return new [] {pointAs4[0], pointAs4[1]};
        }

        /// <summary>
        ///     Returns the positions (array of 2D arrays) of the vertices as that they would be represented in
        ///     the x-y plane (z-values will be zero). This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Double[][].</returns>
        public static double[][] Get2DProjectionPoints(IList<double[]> vertices, double[] direction)
        {
            var transform = TransformToXYPlane(direction);
            var points = new double[vertices.Count][];
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            for (var i = 0; i < vertices.Count; i++)
            {
                pointAs4[0] = vertices[i][0];
                pointAs4[1] = vertices[i][1];
                pointAs4[2] = vertices[i][2];
                pointAs4 = transform.multiply(pointAs4);
                points[i] = new[] { pointAs4[0], pointAs4[1] };
            }
            return points;
        }

        /// <summary>
        ///     Gets the 2D projection vector
        /// </summary>
        /// <param name="vector3D"></param>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Double[][].</returns>
        public static double[] Get2DProjectionVector(double[] vector3D, double[] direction)
        {
            var transform = TransformToXYPlane(direction);
            var vectorAs4 = transform.multiply(new[] {vector3D[0], vector3D[1], vector3D[2], 1.0});
            return new[] {vectorAs4[0], vectorAs4[1]};
        }

        /// <summary>
        ///     Transforms to xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Double[].</returns>
        public static double[,] TransformToXYPlane(IList<double> direction)
        {
            double[,] backTransformStandIn;
            return TransformToXYPlane(direction, out backTransformStandIn);
        }

        /// <summary>
        ///     Transforms to xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <returns>System.Double[].</returns>
        public static double[,] TransformToXYPlane(IList<double> direction, out double[,] backTransform)
        {
            var xDir = direction[0];
            var yDir = direction[1];
            var zDir = direction[2];

            double[,] rotateX, rotateY, backRotateX, backRotateY;
            if (xDir.IsNegligible() && zDir.IsNegligible())
            {
                rotateX = StarMath.RotationX(Math.Sign(yDir) * Math.PI / 2, true);
                backRotateX = StarMath.RotationX(-Math.Sign(yDir) * Math.PI / 2, true);
                backRotateY = rotateY = StarMath.makeIdentity(4);
            }
            else if (zDir.IsNegligible())
            {
                rotateY = StarMath.RotationY(-Math.Sign(xDir) * Math.PI / 2, true);
                backRotateY = StarMath.RotationY(Math.Sign(xDir) * Math.PI / 2, true);
                var rotXAngle = Math.Atan(yDir / Math.Abs(xDir));
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            else
            {
                var rotYAngle = -Math.Atan(xDir / zDir);
                rotateY = StarMath.RotationY(rotYAngle, true);
                backRotateY = StarMath.RotationY(-rotYAngle, true);
                var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
                var rotXAngle = Math.Sign(zDir) * Math.Atan(yDir / baseLength);
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            backTransform = backRotateY.multiply(backRotateX);
            return rotateX.multiply(rotateY);
        }

        /// <summary>
        /// Backtransforms a 2D vector from an XY plane. Return 3D vector.
        /// </summary>
        /// <param name="direction2D"></param>
        /// <param name="backTransform"></param>
        /// <returns></returns>
        public static double[] Convert2DVectorTo3DVector(double[] direction2D, double[,] backTransform)
        {
            var tempVector = new[] { direction2D[0], direction2D[1], 0.0, 1.0 };
            return backTransform.multiply(tempVector).Take(3).ToArray().normalize(3);
        }

        /// <summary>
        /// Gets 3D vertices from 2D points, the projection direction, and the distance along that direction.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="direction"></param>
        /// <param name="distanceAlongDirection"></param>
        /// <returns></returns>
        public static List<Vertex> GetVerticesFrom2DPoints(List<Point> points, double[] direction, double distanceAlongDirection)
        {
            //Rotate axis back to the original, and then transform points along the given direction.
            //If you try to transform first, it will shift the vertices incorrectly
            double[,] backTransform;
            TransformToXYPlane(direction, out backTransform);
            var directionVector = direction.multiply(distanceAlongDirection);
            var contour = new List<Vertex>();
            foreach (var point in points)
            {
                var position = new[] { point.X, point.Y, 0.0, 1.0 };
                var untransformedPosition = backTransform.multiply(position).Take(3).ToArray();
                var vertexPosition = untransformedPosition.add(directionVector, 3);

                contour.Add(new Vertex(vertexPosition));
            }

            return new List<Vertex>(contour);
        }
        #endregion

        #region Angle between Edges/Lines

        /// <summary>
        ///     Gets the smaller of the two angles between edges.
        /// </summary>
        /// <param name="edge1">The edge1.</param>
        /// <param name="edge2">The edge2.</param>
        /// <returns>System.Double.</returns>
        internal static double SmallerAngleBetweenEdges(Edge edge1, Edge edge2)
        {
            var axis = edge1.Vector.crossProduct(edge2.Vector);
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return Math.Min(ExteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]),
                InteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]));
        }

        /// <summary>
        ///     Gets the smaller of the two angles between edges.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double SmallerAngleBetweenEdges(Point a, Point b, Point c)
        {
            var edge1 = new[] { b.X - a.X, b.Y - a.Y };
            var edge2 = new[] { c.X - b.X, c.Y - b.Y };
            return Math.Acos(edge1.dotProduct(edge2, 3) / (edge1.norm2() * edge2.norm2()));
        }

        /// <summary>
        ///     Smallers the angle between edges.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        internal static double SmallerAngleBetweenEdges(double[] v0, double[] v1)
        {
            return Math.Min(ExteriorAngleBetweenEdgesInCCWList(v0, v1), InteriorAngleBetweenEdgesInCCWList(v0, v1));
        }

        /// <summary>
        ///     Angles the between edges cw.
        /// </summary>
        /// <param name="edge1">The edge1.</param>
        /// <param name="edge2">The edge2.</param>
        /// <param name="axis">The axis.</param>
        /// <returns>System.Double.</returns>
        internal static double ExteriorAngleBetweenEdgesInCCWList(Edge edge1, Edge edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return ExteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]);
        }

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="edge1">The edge1.</param>
        /// <param name="edge2">The edge2.</param>
        /// <param name="axis">The axis.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(Edge edge1, Edge edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return InteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]);
        }

        /// <summary>
        ///     Angles the between edges cw.
        /// </summary>
        /// <param name="edge1">The edge1.</param>
        /// <param name="edge2">The edge2.</param>
        /// <param name="axis">The axis.</param>
        /// <returns>System.Double.</returns>
        internal static double ExteriorAngleBetweenEdgesInCCWList(double[] edge1, double[] edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1, edge2 }, axis);
            return ExteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]);
        }

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="edge1">The edge1.</param>
        /// <param name="edge2">The edge2.</param>
        /// <param name="axis">The axis.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(double[] edge1, double[] edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1, edge2 }, axis);
            return InteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1]);
        }

        /// <summary>
        ///     Angles the between edges cw.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double ExteriorAngleBetweenEdgesInCCWList(Point a, Point b, Point c)
        {
            return ExteriorAngleBetweenEdgesInCCWList(new[] { b.X - a.X, b.Y - a.Y }, new[] { c.X - b.X, c.Y - b.Y });
        }

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(Point a, Point b, Point c)
        {
            return InteriorAngleBetweenEdgesInCCWList(new[] { b.X - a.X, b.Y - a.Y }, new[] { c.X - b.X, c.Y - b.Y });
        }

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(PointLight a, PointLight b, PointLight c)
        {
            return InteriorAngleBetweenEdgesInCCWList(new[] { b.X - a.X, b.Y - a.Y }, new[] { c.X - b.X, c.Y - b.Y });
        }

        /// <summary>
        ///     Projecteds the angle between vertices CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="positiveNormal">The positive normal.</param>
        /// <returns>System.Double.</returns>
        public static double ProjectedInteriorAngleBetweenVerticesCCW(Vertex a, Vertex b, Vertex c, double[] positiveNormal)
        {
            var points = Get2DProjectionPoints(new List<Vertex> { a, b, c }, positiveNormal);
            return InteriorAngleBetweenEdgesInCCWList(new[] { points[1].X - points[0].X, points[1].Y - points[0].Y },
                new[] { points[2].X - points[1].X, points[2].Y - points[1].Y });
        }

        public static double ProjectedExteriorAngleBetweenVerticesCCW(Vertex a, Vertex b, Vertex c, double[] positiveNormal)
        {
            return 2 * Math.PI - ProjectedInteriorAngleBetweenVerticesCCW(a, b, c, positiveNormal);
        }

        /// <summary>
        ///     Gets the exterior angle between two edges, assuming the edges are listed in CCW order.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double ExteriorAngleBetweenEdgesInCCWList(double[] v0, double[] v1)
        {
            return 2 * Math.PI - InteriorAngleBetweenEdgesInCCWList(v0, v1);
        }

        /// <summary>
        ///     Gets the interior angle between two edges, assuming the edges are listed in CCW order.
        ///     NOTE: This is opposite from getting the CCW angle from v0 and v1.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double InteriorAngleBetweenEdgesInCCWList(double[] v0, double[] v1)
        {
            #region Law of Cosines Approach (Commented Out)

            ////This is an alternative approach to the one that is not commented out
            ////Use law of cosines to find smaller angle between two vectors
            //var aSq = v0[0] * v0[0] + v0[1] * v0[1];
            //var bSq = v1[0] * v1[0] + v1[1] * v1[1];
            //var cSq = (v0[0] + v1[0]) * (v0[0] + v1[0]) + (v0[1] + v1[1]) * (v0[1] + v1[1]);
            //var angle = Math.Acos((aSq + bSq - cSq) / (2 * Math.Sqrt(aSq) * Math.Sqrt(bSq)));
            ////Use cross product sign to determine if smaller angle is CCW from v0
            //var cross = v0[0] * v1[1] - v0[1] * v1[0];
            //if (Math.Sign(cross) < 0) angle = 2 * Math.PI - angle;

            #endregion

            var angleV0 = Math.Atan2(v0[1], v0[0]);
            var angleV1 = Math.Atan2(v1[1], v1[0]);
            var angleChange = Math.PI - (angleV1 - angleV0);
            if (angleChange > 2 * Math.PI) return angleChange - 2 * Math.PI;
            if (angleChange < 0) return angleChange + 2 * Math.PI;
            return angleChange;
        }

        #endregion

        #region Intersection Method (between lines, planes, solids, etc.)

        /// <summary>
        /// Detemines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are colinear, they are not considered intersecting.
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="intersectionPoint"></param>
        /// <param name="considerCollinearOverlapAsIntersect"></param>
        /// <returns></returns>
        public static bool LineLineIntersection(Line line1, Line line2, out Point intersectionPoint,
            bool considerCollinearOverlapAsIntersect = false)
        {
            return
                (LineLineIntersection(line1.FromPoint, line1.ToPoint, line2.FromPoint, line2.ToPoint,
                    out intersectionPoint, considerCollinearOverlapAsIntersect));
        }

        /// <summary>
        /// Detemines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are colinear, they are not considered intersecting.
        /// </summary>
        /// <param name="intersectionPoint"></param>
        /// <param name="considerCollinearOverlapAsIntersect"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="pt3"></param>
        /// /// <param name="pt4"></param>
        /// <source>
        /// http://www.codeproject.com/Tips/862988/Find-the-Intersection-Point-of-Two-Line-Segments
        /// </source>
        /// <returns></returns>
        public static bool LineLineIntersection(Point pt1, Point pt2, Point pt3, Point pt4, out Point intersectionPoint, bool considerCollinearOverlapAsIntersect = false)
        {
            var p = pt1.Position;
            var p2 = pt2.Position;
            var q = pt3.Position;
            var q2 = pt4.Position;
            var points = new List<Point> { pt1, pt2, pt3, pt4 };
            intersectionPoint = null;
            var r = p2.subtract(p, 2);
            var s = q2.subtract(q, 2);
            var rxs = r[0] * s[1] - r[1] * s[0]; //2D cross product, determines if parallel
            var qp = q.subtract(p, 2);
            var qpxr = qp[0] * r[1] - qp[1] * r[0];//2D cross product

            // If r x s ~ 0 and (q - p) x r ~ 0, then the two lines are possibly collinear.
            // This is negigible tolerance of 0.00001 is not arbitary. It was chosen because of the second case within this if statement.
            if (rxs.IsNegligible(0.00001) && qpxr.IsNegligible(0.00001))
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                var qpr = qp[0] * r[0] + qp[1] * r[1];
                var pqs = p.subtract(q, 2)[0] * s[0] + p.subtract(q, 2)[1] * s[1];
                var overlapping = (0 <= qpr && qpr <= r[0] * r[0] + r[1] * r[1]) ||
                                  (0 <= pqs && pqs <= s[0] * s[0] + s[1] * s[1]);
                if (rxs.IsNegligible() && qpxr.IsNegligible())
                // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
                {
                    if (!considerCollinearOverlapAsIntersect) return false;
                    return overlapping;
                }
                //It is possible for the rxs or qpxr to be near negligible, but all points of the two lines are negligibly close to the line
                //making the lines collinear
                if (!overlapping) return false;
                //Get the slope for both lines
                double slope1, slope2;
                if (pt1.X.IsPracticallySame(pt2.X)) //Vertical line
                {
                    slope1 = double.MaxValue;
                }
                else if (pt1.Y.IsPracticallySame(pt2.Y))//Horizontal Line
                {
                    slope1 = 0.0;
                }
                else slope1 = (pt2.Y - pt1.Y) / (pt2.X - pt1.X);
                if (pt3.X.IsPracticallySame(pt4.X)) //Vertical line
                {
                    slope2 = double.MaxValue;
                }
                else if (pt3.Y.IsPracticallySame(pt4.Y))//Horizontal Line
                {
                    slope2 = 0.0;
                }
                else slope2 = (pt4.Y - pt3.Y) / (pt4.X - pt3.X);
                //Foreach line, check the Y intercepts of the X values from the other line. If the intercepts match the point.Y values, then it is collinear
                if ((slope1 * (pt3.X - pt1.X) + pt1.Y).IsPracticallySame(pt3.Y) &&
                    (slope1 * (pt4.X - pt1.X) + pt1.Y).IsPracticallySame(pt4.Y))
                {
                    if (!considerCollinearOverlapAsIntersect) return false;
                    return true;
                }
                if ((slope2 * (pt1.X - pt3.X) + pt3.Y).IsPracticallySame(pt1.Y) &&
                    (slope2 * (pt2.X - pt3.X) + pt3.Y).IsPracticallySame(pt2.Y))
                {
                    if (!considerCollinearOverlapAsIntersect) return false;
                    return true;
                }
                //Else look at other cases.
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs.IsNegligible() && !qpxr.IsNegligible()) return false;

            //4. Check if the lines share a point. If yes, return that point.  
            if (pt1 == pt3 || pt1 == pt4)
            {
                intersectionPoint = pt1;
                return true;
            }
            if (pt2 == pt3 || pt2 == pt4)
            {
                intersectionPoint = pt2;
                return true;
            }

            // t = (q - p) x s / (r x s)
            //Note, the output of this will be t = [0,0,#] since this is a 2D cross product.
            var t = q.subtract(p).crossProduct(s).divide(rxs);

            // u = (q - p) x r / (r x s)
            //Note, the output of this will be u = [0,0,#] since this is a 2D cross product.
            var u = q.subtract(p).crossProduct(r).divide(rxs);

            // 5. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsNegligible() &&
                !t[2].IsLessThanNonNegligible() && !t[2].IsGreaterThanNonNegligible(1.0) &&
                !u[2].IsLessThanNonNegligible() && !u[2].IsGreaterThanNonNegligible(1.0))
            {

                ////Tthe intersection point may be one of the existing points
                ////This is needed because the x,y calculated below can be off by a very slight amount, caused by rounding error.
                ////Check if any of the points are on the other line.
                //double slope1, slope2;
                //if (pt1.X.IsPracticallySame(pt2.X)) //Vertical line
                //{
                //    slope1 = double.MaxValue;
                //}
                //else if (pt1.Y.IsPracticallySame(pt2.Y))//Horizontal Line
                //{
                //    slope1 = 0.0;
                //}
                //else slope1 = (pt2.Y - pt1.Y) / (pt2.X - pt1.X);
                //if (pt3.X.IsPracticallySame(pt4.X)) //Vertical line
                //{
                //    slope2 = double.MaxValue;
                //}
                //else if (pt3.Y.IsPracticallySame(pt4.Y))//Horizontal Line
                //{
                //    slope2 = 0.0;
                //}
                //else slope2 = (pt4.Y - pt3.Y) / (pt4.X - pt3.X);

                ////Foreach line, check the Y intercepts of the X values from the other line. If the intercepts match the point.Y values, then it is collinear
                //if ((slope1*(pt3.X - pt1.X) + pt1.Y).IsPracticallySame(pt3.Y))
                //{
                //    intersectionPoint = pt3;
                //    return true;
                //}
                //if ((slope1*(pt4.X - pt1.X) + pt1.Y).IsPracticallySame(pt4.Y))
                //{
                //    intersectionPoint = pt4;
                //    return true;
                //}
                //if ((slope2 * (pt1.X - pt3.X) + pt3.Y).IsPracticallySame(pt1.Y))
                //{
                //    intersectionPoint = pt1;
                //    return true;
                //};
                //if ((slope2*(pt2.X - pt3.X) + pt3.Y).IsPracticallySame(pt2.Y))
                //{
                //    intersectionPoint = pt2;
                //    return true;
                //}

                // We can calculate the intersection point using either t or u.
                var x = p[0] + t[2] * r[0];
                var y = p[1] + t[2] * r[1];
                var x2 = q[0] + u[2] * s[0];
                var y2 = q[1] + u[2] * s[1];

                //If either is equal to any of the given points, return that point
                if (x.IsPracticallySame(x2) && y.IsPracticallySame(y2))
                {
                    //If either is equal to any of the given points, return that point
                    foreach (var point in points)
                    {
                        if (!point.X.IsPracticallySame(x) || !point.Y.IsPracticallySame(y)) continue;
                        intersectionPoint = point;
                        return true;
                    }
                    //Else, return the new intersection point
                    intersectionPoint = new Point(x, y);
                    return true;
                }

                //Values are not even close
                if (!x.IsPracticallySame(x2, 0.0001) || !y.IsPracticallySame(y2, 0.00001)) throw new NotImplementedException();

                //Else, equations were not equal, but one may have been slightly off. 
                //Check to see if either possible intersection point matches an existing point.
                foreach (var point in points)
                {
                    if ((!point.X.IsPracticallySame(x) || !point.Y.IsPracticallySame(y)) &&
                        (!point.X.IsPracticallySame(x2) || !point.Y.IsPracticallySame(y2))) continue;
                    intersectionPoint = point;
                    return true;
                }

                //Else, choose the solution with fewer digits
                var x3 = x;
                var y3 = y;
                if (!x.IsPracticallySame(x2))
                {
                    var i = 0;
                    while (i < 16 && !x.IsPracticallySame(Math.Round(x, i)))
                    {
                        i++;
                    }
                    var j = 0;
                    while (j < 16 && !x2.IsPracticallySame(Math.Round(x2, j)))
                    {
                        j++;
                    }
                    //i < j. x3 is already equal to x.
                    if (i > j)
                    {
                        x3 = x2;
                    }
                    //if i == j, just use the first x.
                }
                if (!y.IsPracticallySame(y2))
                {
                    var i = 0;
                    while (i < 16 && !y.IsPracticallySame(Math.Round(y, i)))
                    {
                        i++;
                    }
                    var j = 0;
                    while (j < 16 && !y2.IsPracticallySame(Math.Round(y2, j)))
                    {
                        j++;
                    }
                    //i < j. x3 is already equal to x.
                    if (i > j)
                    {
                        y3 = y2;
                    }
                    //if i == j, just use the first y.
                }
                intersectionPoint = new Point(x3, y3);
                return true;

            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }

        /// <summary>
        ///     Find the point common to three planes.
        /// </summary>
        /// <param name="n1">The n1.</param>
        /// <param name="d1">The d1.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="d2">The d2.</param>
        /// <param name="n3">The n3.</param>
        /// <param name="d3">The d3.</param>
        /// <returns>System.Double[].</returns>
        public static double[] PointCommonToThreePlanes(double[] n1, double d1, double[] n2, double d2, double[] n3,
            double d3)
        {
            var matrixOfNormals = new[,] { { n1[0], n1[1], n1[2] }, { n2[0], n2[1], n2[2] }, { n3[0], n3[1], n3[2] } };
            var distances = new[] { d1, d2, d3 };
            try
            {
                return StarMath.solve(matrixOfNormals, distances);
            }
            catch
            {
                return new[] { double.NaN, double.NaN, double.NaN };
            }
        }

        /// <summary>
        ///     Lines the intersecting two planes.
        /// </summary>
        /// <param name="n1">The n1.</param>
        /// <param name="d1">The d1.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="d2">The d2.</param>
        /// <param name="directionOfLine">The direction of line.</param>
        /// <param name="pointOnLine">The point on line.</param>
        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2,
            out double[] directionOfLine, out double[] pointOnLine)
        {
            directionOfLine = n1.crossProduct(n2).normalize(3);
            LineIntersectingTwoPlanes(n1, d1, n2, d2, directionOfLine, out pointOnLine);
        }

        /// <summary>
        ///     Lines the intersecting two planes.
        /// </summary>
        /// <param name="n1">The n1.</param>
        /// <param name="d1">The d1.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="d2">The d2.</param>
        /// <param name="directionOfLine">The direction of line.</param>
        /// <param name="pointOnLine">The point on line.</param>
        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2,
            double[] directionOfLine, out double[] pointOnLine)
        {
            /* to find the point on the line...well a point on the line, it turns out that one has three unknowns (px, py, pz)
             * and only two equations. Let's put the point on the plane going through the origin. So this plane would have a normal 
             * of v (or DirectionOfLine). */
            var a = new double[3, 3];
            a.SetRow(0, n1);
            a.SetRow(1, n2);
            a.SetRow(2, directionOfLine);
            var b = new[] { d1, d2, 0 };
            pointOnLine = StarMath.solve(a, b);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2)
        {
            double[] center;
            double[] interSect1, interSect2;
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out interSect1, out interSect2, out t1, out t2);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="center">The center.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] center)
        {
            double[] interSect1, interSect2;
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out interSect1, out interSect2, out t1, out t2);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="interSect1">The inter sect1.</param>
        /// <param name="interSect2">The inter sect2.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] interSect1,
            out double[] interSect2)
        {
            double[] center;
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out interSect1, out interSect2, out t1, out t2);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="center">The center.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] center,
            out double t1, out double t2)
        {
            double[] interSect1, interSect2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out interSect1, out interSect2, out t1, out t2);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <param name="center">The center.</param>
        /// <param name="interSect1">The inter sect1.</param>
        /// <param name="interSect2">The inter sect2.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] center,
            out double[] interSect1, out double[] interSect2, out double t1, out double t2)
        {
            var a00 = n1[0] * n1[0] + n1[1] * n1[1] + n1[2] * n1[2];
            var a01 = -n1[0] * n2[0] - n1[1] * n2[1] - n1[2] * n2[2];
            var a10 = n1[0] * n2[0] + n1[1] * n2[1] + n1[2] * n2[2];
            var a11 = -n2[0] * n2[0] - n2[1] * n2[1] - n2[2] * n2[2];
            var b0 = n1[0] * (p2[0] - p1[0]) + n1[1] * (p2[1] - p1[1]) + n1[2] * (p2[2] - p1[2]);
            var b1 = n2[0] * (p2[0] - p1[0]) + n2[1] * (p2[1] - p1[1]) + n2[2] * (p2[2] - p1[2]);
            var a = new[,] { { a00, a01 }, { a10, a11 } };
            var b = new[] { b0, b1 };
            var t = StarMath.solve(a, b);
            t1 = t[0];
            t2 = t[1];
            interSect1 = new[] { p1[0] + n1[0] * t1, p1[1] + n1[1] * t1, p1[2] + n1[2] * t1 };
            interSect2 = new[] { p2[0] + n2[0] * t2, p2[1] + n2[1] * t2, p2[2] + n2[2] * t2 };
            center = new[]
            {(interSect1[0] + interSect2[0])/2, (interSect1[1] + interSect2[1])/2, (interSect1[2] + interSect2[2])/2};
            return DistancePointToPoint(interSect1, interSect2);
        }

        #endregion

        #region Distance Methods (between point, line, and plane)

        /// <summary>
        ///     Returns the distance the point on an infinite line.
        /// </summary>
        /// <param name="qPoint">The q point that is off of the line.</param>
        /// <param name="lineRefPt">The line reference point on the line.</param>
        /// <param name="lineVector">The line direction vector.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector)
        {
            double[] dummy;
            return DistancePointToLine(qPoint, lineRefPt, lineVector, out dummy);
        }

        /// <summary>
        ///     Returns the distance the point on an infinite line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector,
            out double[] pointOnLine)
        {
            double t;
            if (qPoint.Count() == 2)
            {
                /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
                * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
                t = (lineVector[0] * (qPoint[0] - lineRefPt[0]) + lineVector[1] * (qPoint[1] - lineRefPt[1]))
                        / (lineVector[0] * lineVector[0] + lineVector[1] * lineVector[1]);
                pointOnLine = new[] { lineRefPt[0] + lineVector[0] * t, lineRefPt[1] + lineVector[1] * t };
                return DistancePointToPoint(qPoint, pointOnLine);
            }
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
             * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            t = (lineVector[0] * (qPoint[0] - lineRefPt[0]) + lineVector[1] * (qPoint[1] - lineRefPt[1]) +
                     lineVector[2] * (qPoint[2] - lineRefPt[2]))
                    / (lineVector[0] * lineVector[0] + lineVector[1] * lineVector[1] + lineVector[2] * lineVector[2]);
            pointOnLine = new[]
            {lineRefPt[0] + lineVector[0]*t, lineRefPt[1] + lineVector[1]*t, lineRefPt[2] + lineVector[2]*t};
            return DistancePointToPoint(qPoint, pointOnLine);
        }

        /// <summary>
        ///     Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPoint(Point p1, Point p2)
        {
            return DistancePointToPoint(p1.Light, p2.Light);
        }

        /// <summary>
        ///     Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPoint(PointLight p1, PointLight p2)
        {
            var dX = p1.X - p2.X;
            var dY = p1.Y - p2.Y;
            return Math.Sqrt(dX * dX + dY * dY);
        }

        /// <summary>
        ///     Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPoint(double[] p1, double[] p2)
        {
            var dX = p1[0] - p2[0];
            var dY = p1[1] - p2[1];
            if (p1.Length == 2) return Math.Sqrt(dX * dX + dY * dY);
            var dZ = p1[2] - p2[2];
            return Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
        }

        /// <summary>
        ///     Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double SquareDistancePointToPoint(double[] p1, double[] p2)
        {
            var dX = p1[0] - p2[0];
            var dY = p1[1] - p2[1];
            if (p1.Length == 2) return dX * dX + dY * dY;
            var dZ = p1[2] - p2[2];
            return dX * dX + dY * dY + dZ * dZ;
        }

        /// <summary>
        ///     Returns the signed distance of the point to the plane.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="positionOnPlane">The position on plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(double[] point, double[] normalOfPlane, double[] positionOnPlane)
        {
            return DistancePointToPlane(point, normalOfPlane, positionOnPlane.dotProduct(normalOfPlane, 3));
        }

        /// <summary>
        ///     Returns the signed distance of the point to the plane. If the point is "above" the plane, then a positive
        ///     distance is return - if "below" then negative. This "above" means that the point is on the side of the
        ///     plane that the normal points towards.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="signedDistanceToPlane">The signed distance to plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(double[] point, double[] normalOfPlane, double signedDistanceToPlane)
        {
            return normalOfPlane.dotProduct(point, 3) - signedDistanceToPlane;
        }

        /// <summary>
        ///     Finds the point on the face made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that face. If not intersection exists, then function returns null. Points must be on either side 
        ///     of triangle to return a valid intersection.
        /// </summary>
        /// <param name="face"></param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static double[] PointOnFaceFromIntersectingLine(PolygonalFace face, double[] point1,
            double[] point2)
        {
            var positions = face.Vertices.Select(vertex => vertex.Position).ToList();
            return PointOnFaceFromIntersectingLine(positions, face.Normal, point1, point2);
        }

        /// <summary>
        ///     Finds the point on the face made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that face. If not intersection exists, then function returns null. Points must be on either side 
        ///     of triangle to return a valid intersection.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="vertices"></param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static double[] PointOnFaceFromIntersectingLine(List<double[]> vertices, double[] normal, double[] point1,
            double[] point2)
        {
            var distanceToOrigin = normal.dotProduct(vertices[0], 3);
            var d1 = normal.dotProduct(point1, 3);
            var d2 = normal.dotProduct(point2, 3);
            if (Math.Sign(distanceToOrigin - d1) == Math.Sign(distanceToOrigin - d2)) return null; //Points must be on either side of triangle
            var denominator = d1 - d2;
            if (denominator == 0) return null; //The points form a perpendicular line to the face
            var fraction = (d1 - distanceToOrigin) / (denominator);
            var position = new double[3];
            for (var i = 0; i < 3; i++)
            {
                position[i] = point2[i] * fraction + point1[i] * (1 - fraction);
                if (double.IsNaN(position[i]))
                    throw new Exception("This should never occur. Prevent this from happening");
            }
            return IsVertexInsideTriangle(vertices, position, true) ? position : null;
        }

        /// <summary>
        ///     Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that plane.Returns null if the intersection point is not on the line segment.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static Vertex PointOnPlaneFromIntersectingLineSegment(double[] normalOfPlane, double distOfPlane, Vertex point1,
            Vertex point2)
        {
            var position =
                PointOnPlaneFromIntersectingLineSegment(normalOfPlane, distOfPlane, point1.Position, point2.Position);
            return position == null ? null : new Vertex(position);
        }

        /// <summary>
        ///     Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that plane. Returns null if the intersection point is not on the line segment.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static double[] PointOnPlaneFromIntersectingLineSegment(double[] normalOfPlane, double distOfPlane, double[] point1,
            double[] point2)
        {
            var position = PointOnPlaneFromIntersectingLine(normalOfPlane, distOfPlane, point1, point2);
            var d1 = point2.subtract(point1).norm2();
            var d2 = point2.subtract(position).norm2();
            var d3 = point1.subtract(position).norm2();
            return d1.IsPracticallySame(d2 + d3, 1 - Constants.HighConfidence) ? position : null;
        }

        /// <summary>
        ///     Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static Vertex PointOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, Vertex point1,
            Vertex point2)
        {
            return new Vertex(PointOnPlaneFromIntersectingLine(normalOfPlane, distOfPlane, point1.Position, point2.Position));
        }

        /// <summary>
        ///     Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static double[] PointOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, double[] point1,
            double[] point2)
        {
            var d1 = normalOfPlane.dotProduct(point1, 3);
            var d2 = normalOfPlane.dotProduct(point2, 3);
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            var position = new double[3];
            for (var i = 0; i < 3; i++)
            {
                position[i] = point2[i] * fraction + point1[i] * (1 - fraction);
                if (double.IsNaN(position[i]))
                    throw new Exception("This should never occur. The line must not be in-plane. Prevent this from happening");
            }
            return position;
        }

        /// <summary>
        ///     Finds the point on the plane made by a line intersecting
        ///     with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane. Can be 2D or 3D. </param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="line"></param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static Point PointOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, Line line)
        {
            PointLightOnPlaneFromIntersectingLine(normalOfPlane[0], normalOfPlane[1], distOfPlane, line.FromPoint.X, line.FromPoint.Y,
                line.ToPoint.X, line.FromPoint.Y, out var x, out var y);
            return new Point(x, y);
        }

        /// <summary>
        ///     Finds the point on the plane made by a line intersecting
        ///     with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane. Can be 2D or 3D. </param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="line"></param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static PointLight PointLightOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, Line line)
        {
            PointLightOnPlaneFromIntersectingLine(normalOfPlane[0], normalOfPlane[1], distOfPlane, line.FromPoint.X, line.FromPoint.Y,
                line.ToPoint.X, line.FromPoint.Y, out var x, out var y);
            return new PointLight(x, y);
        }

        public static void PointLightOnPlaneFromIntersectingLine(double normalOfPlaneX, double normalOfPlaneY, double distOfPlane, 
            double fromPointX, double fromPointY, double toPointX, double toPointY, out double x, out double y)
        {
            var d1 = normalOfPlaneX * toPointX + normalOfPlaneY * toPointY; //2D Dot product
            var d2 = normalOfPlaneX * fromPointX + normalOfPlaneY * fromPointY;  //For a point, Position[2] = 0.0
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            x = fromPointX * fraction + toPointX * (1 - fraction);
            y = fromPointY * fraction + toPointY * (1 - fraction);
        }

        /// <summary>
        ///     Finds the point on the plane made by a ray. If that ray is not going to pass through the
        ///     that plane, then null is returned.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="rayPosition">The ray position.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <param name="signedDistance"></param>
        /// <returns>Vertex.</returns>
        public static double[] PointOnPlaneFromRay(double[] normalOfPlane, double distOfPlane, double[] rayPosition,
            double[] rayDirection, out double signedDistance)
        {
            var dot = rayDirection.dotProduct(normalOfPlane, 3);
            signedDistance = 0.0;
            if (dot == 0) return null;

            var d1 = -DistancePointToPlane(rayPosition, normalOfPlane, distOfPlane);
            signedDistance = d1 / dot;
            if (signedDistance.IsNegligible()) return rayPosition;
            return rayPosition.add(rayDirection.multiply(signedDistance), 3);
        }

        /// <summary>
        ///     Finds the point on the triangle made by a line. If that line is not going to pass through the
        ///     that triangle, then null is returned. The signed distance is positive if the vertex points to
        ///     the triangle along the direction (ray). User can also specify whether the edges of the triangle
        ///     are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="vertex">The vertex.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns>Vertex.</returns>
        public static double[] PointOnTriangleFromLine(PolygonalFace face, Vertex vertex, double[] direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            return PointOnTriangleFromLine(face, vertex.Position, direction, out signedDistance);
        }

        /// <summary>
        ///     Finds the point on the triangle made by a line. If that line is not going to pass through the
        ///     that triangle, then null is returned. The signed distance is positive if the vertex points to
        ///     the triangle along the direction (ray). User can also specify whether the edges of the triangle
        ///     are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point3D"></param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        public static double[] PointOnTriangleFromLine(PolygonalFace face, double[] point3D, double[] direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position, 3);
            var newPoint = PointOnPlaneFromRay(face.Normal, distanceToOrigin, point3D, direction, out signedDistance);
            if (newPoint == null) return null;
            return IsVertexInsideTriangle(face.Vertices, newPoint, onBoundaryIsInside) ? newPoint : null;
        }

        /// <summary>
        ///     Finds the point on the triangle made by a line. If that line is not going to pass through the
        ///     that triangle, then null is returned. The signed distance is positive if the vertex points to
        ///     the triangle along the direction (ray). User can also specify whether the edges of the triangle
        ///     are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point3D"></param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        public static double[] PointOnTriangleFromLine(PolygonalFace face, double[] point3D, VoxelDirections direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            var newPoint = (double[])point3D.Clone();
            signedDistance = double.NaN;
            var d = face.Normal.dotProduct(face.Vertices[0].Position, 3);
            var n = face.Normal;
            switch (direction)
            {
                case VoxelDirections.XNegative:
                case VoxelDirections.XPositive:
                    if (face.Normal[0].IsNegligible()) return null;
                    newPoint = new[] { (d - n[1] * point3D[1] - n[2] * point3D[2]) / n[0], point3D[1], point3D[2] };
                    signedDistance = (Math.Sign((int)direction)) * (newPoint[0] - point3D[0]);
                    break;
                case VoxelDirections.YNegative:
                case VoxelDirections.YPositive:
                    if (face.Normal[1].IsNegligible()) return null;
                    newPoint = new[] { point3D[0], (d - n[0] * point3D[0] - n[2] * point3D[2]) / n[1], point3D[2] };
                    signedDistance = (Math.Sign((int)direction)) * (newPoint[1] - point3D[1]);
                    break;
                default:
                    if (face.Normal[2].IsNegligible()) return null;
                    newPoint = new[] { point3D[0], point3D[1], (d - n[0] * point3D[0] - n[1] * point3D[1]) / n[2] };
                    signedDistance = (Math.Sign((int)direction)) * (newPoint[2] - point3D[2]);
                    break;
            }

            return IsVertexInsideTriangle(face.Vertices, newPoint, onBoundaryIsInside) ? newPoint : null;
        }
        #endregion

        #region Create 2D Circle Paths
        /// <summary>
        /// Returns a the path of a circle made up of points. Increment as needed.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="radianIncrement"></param>
        /// <returns></returns>
        public static List<Point> CreateCirclePath(Point center, double radius, double radianIncrement = Math.PI / 50.0)
        {
            var path = new List<Point>();
            for (var theta = 0.0; theta < Math.PI * 2; theta += radianIncrement)
            {
                path.Add(new Point(radius * Math.Cos(theta) + center.X, radius * Math.Sin(theta) + center.Y));
            }
            return path;
        }

        /// <summary>
        /// Returns a the path of a circle made up of points. Increment as needed.
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="radianIncrement"></param>
        /// <returns></returns>
        public static List<Point> CreateCirclePath(BoundingCircle circle, double radianIncrement = Math.PI / 50.0)
        {
            return CreateCirclePath(circle.Center, circle.Radius, radianIncrement);
        }
        #endregion

        #region isInside Methods (is 2D point inside polygon, vertex inside solid, ect.)

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside."
        /// </summary>
        /// <param name="triangle">The triangle.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside triangle] [the specified triangle]; otherwise, <c>false</c>.</returns>
        public static bool IsVertexInsideTriangle(PolygonalFace triangle, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsVertexInsideTriangle(triangle.Vertices, vertexInQuestion, onBoundaryIsInside);
        }

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside."
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside triangle] [the specified vertices]; otherwise, <c>false</c>.</returns>
        /// <exception cref="Exception">Incorrect number of points in traingle</exception>
        /// <exception cref="ArgumentException"></exception>
        /// <references>
        ///     http://www.blackpawn.com/texts/pointinpoly/
        /// </references>
        public static bool IsVertexInsideTriangle(IList<Vertex> vertices, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsVertexInsideTriangle(vertices, vertexInQuestion.Position, onBoundaryIsInside);
        }

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside." Assumes vertex in question is in the same plane
        ///     as the triangle.
        /// </summary>
        public static bool IsVertexInsideTriangle(IList<Vertex> vertices, double[] vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            var positions = vertices.Select(vertex => vertex.Position).ToList();
            return IsVertexInsideTriangle(positions, vertexInQuestion, onBoundaryIsInside);
        }

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside." Assumes vertex in question is in the same plane
        ///     as the triangle.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="vertexInQuestion"></param>
        /// <param name="onBoundaryIsInside"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool IsVertexInsideTriangle(IList<double[]> vertices, double[] vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            if (vertices.Count != 3) throw new Exception("Incorrect number of points in traingle");
            var p = vertexInQuestion;
            var a = vertices[0];
            var b = vertices[1];
            var c = vertices[2];
            return SameSide(p, a, b, c, onBoundaryIsInside) &&
                   SameSide(p, b, a, c, onBoundaryIsInside) &&
                   SameSide(p, c, a, b, onBoundaryIsInside);
        }

        /// <summary>
        ///     Sames the side.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SameSide(double[] p1, double[] p2, double[] a, double[] b, bool onBoundaryIsInside = true)
        {
            var cp1 = b.subtract(a, 3).crossProduct(p1.subtract(a, 3));
            var cp2 = b.subtract(a, 3).crossProduct(p2.subtract(a, 3));
            var dot = cp1.dotProduct(cp2, 3);
            if (dot.IsNegligible()) return onBoundaryIsInside;
            if (Math.Abs(dot) < Constants.BaseTolerance) return onBoundaryIsInside;
            return dot > 0.0;
        }

        /// <summary>
        ///     Determines if a point is inside a tesselated solid (polyhedron).
        ///     And the polygon is not self-intersecting
        ///     http://www.cescg.org/CESCG-2012/papers/Horvat-Ray-casting_point-in-polyhedron_test.pdf
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is vertex inside solid] [the specified ts]; otherwise, <c>false</c>.</returns>
        public static bool IsVertexInsideSolid(TessellatedSolid ts, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            //ToDo: Rewrite function to use plane list as in SolidIntersectionFunction
            var facesAbove = new List<PolygonalFace>();
            var facesBelow = new List<PolygonalFace>();
            var inconclusive = true;
            var rnd = new Random();
            //Added while inconclusive and random direction because there are some special cases that look the  
            //same. For instance, consider a vertex sitting at the center of a half moon. Along the z axis, 
            //It will go through 1 edge or vertex (special cases) above and one below. Then consider a box
            //centered on the origin. A point on the origin would point to an edge (of the two triangles
            //forming the face) above and one below. Therefore, it was decided that special cases (through
            //edges or vertices, will yeild inconclusive results. 
            while (inconclusive)
            {
                inconclusive = false;
                var direction = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() }.normalize(3);
                foreach (var face in ts.Faces)
                {
                    if (face.Vertices.Any(vertex => vertexInQuestion.X.IsPracticallySame(vertex.X) &&
                                                    vertexInQuestion.Y.IsPracticallySame(vertex.Y) &&
                                                    vertexInQuestion.Z.IsPracticallySame(vertex.Z)))
                    {
                        return onBoundaryIsInside;
                    }

                    var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position, 3);
                    var t = -(vertexInQuestion.Position.dotProduct(face.Normal, 3) - distanceToOrigin) /
                            direction.dotProduct(face.Normal, 3);
                    //Note that if t == 0, then it is on the face
                    //else, find the intersection point and determine if it is inside the polygon (face)
                    var newVertex = t.IsNegligible()
                        ? vertexInQuestion
                        : new Vertex(vertexInQuestion.Position.add(direction.multiply(t), 3));
                    if (!IsVertexInsideTriangle(face, newVertex)) continue;
                    //If the distance between the vertex and a plane is neglible and the vertex is inside that face
                    if (t.IsNegligible())
                    {
                        return onBoundaryIsInside;
                    }
                    if (t > 0.0) //Face is higher on Z axis than vertex.
                    {
                        //Check to make sure no adjacent faces were already added to list (e.g., the projected vertex goes 
                        //through an edge).
                        var onAdjacentFace = face.AdjacentFaces.Any(adjacentFace => facesAbove.Contains(adjacentFace));
                        //Else, inconclusive (e.g., corners of cresent moon) 
                        if (!onAdjacentFace) facesAbove.Add(face);
                        else
                        {
                            inconclusive = true;
                            break;
                        }
                    }
                    else //Face is lower on Z axis than vertex.
                    {
                        //Check to make sure no adjacent faces were already added to list (e.g., the projected vertex goes 
                        //through an edge).
                        var onAdjacentFace = face.AdjacentFaces.Any(adjacentFace => facesBelow.Contains(adjacentFace));
                        if (!onAdjacentFace) facesBelow.Add(face);
                        else //Else, inconclusive (e.g., corners of cresent moon) 
                        {
                            inconclusive = true;
                            break;
                        }
                    }
                }
            }
            if (facesAbove.Count == 0 || facesBelow.Count == 0) return false;
            return facesAbove.Count % 2 != 0 && facesBelow.Count % 2 != 0;
            //Even number of intercepts, means the vertex is inside
        }

        /// <summary>
        /// Returns the number of lines above a point in question. Also returns the closest line above, if any is above.
        /// </summary>
        /// <param name="pointInQuestion"></param>
        /// <param name="lineList"></param>
        /// <param name="closestLineAbove"></param>
        /// <param name="isOnLine"></param>
        /// <returns></returns>
        public static int NumberOfLinesAbovePoint(Point pointInQuestion, IEnumerable<Line> lineList, out Line closestLineAbove, out bool isOnLine)
        {
            isOnLine = false;
            var count = 0;
            var minD = double.MaxValue;
            closestLineAbove = null;
            foreach (var line in lineList)
            {
                var d = line.YGivenX(pointInQuestion.X) - pointInQuestion.Y;
                if (d.IsNegligible())
                {
                    minD = d;
                    isOnLine = true;
                    closestLineAbove = line;
                }

                if (d < 0) continue; //line is below

                //Else, line is above
                count++;
                if (d > minD) continue;
                minD = d;
                closestLineAbove = line;
            }
            return count;
        }

        /// <summary>
        /// Returns the number of lines below a point in question. Also returns the closest line below, if any is below.
        /// </summary>
        /// <param name="pointInQuestion"></param>
        /// <param name="lineList"></param>
        /// <param name="closestLineBelow"></param>
        /// <param name="isOnLine"></param>
        /// <returns></returns>
        public static int NumberOfLinesBelowPoint(Point pointInQuestion, IEnumerable<Line> lineList, out Line closestLineBelow, out bool isOnLine)
        {
            isOnLine = false;
            var count = 0;
            var maxD = double.MinValue; //Max value should never be greater than 0.
            closestLineBelow = null;
            foreach (var line in lineList)
            {
                var d = line.YGivenX(pointInQuestion.X) - pointInQuestion.Y;
                if (d.IsNegligible())
                {
                    maxD = d;
                    isOnLine = true;
                    closestLineBelow = line;
                }

                if (d > 0) continue; //line is above

                //Else, line is below
                count++;
                if (d < maxD) continue;
                maxD = d;
                closestLineBelow = line;
            }
            return count;
        }

        /// <summary>
        ///     Determines if a polygon is inside another polygon.
        ///     
        ///     Assumptions:
        ///     1) Polygon ordering does not matter.
        ///     2) The polygons do not intersect
        /// 
        ///     Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        public static bool IsPolygonInsidePolygon(PolygonLight outerPolygon, PolygonLight possibleInnerPolygon)
        {
            //The inner polygon can only fully be inside a polygon that has a larger absolute area.
            if (Math.Abs(outerPolygon.Area) < Math.Abs(possibleInnerPolygon.Area)) return false;

            //Since the polygons are assumed not the intersect, we only need to test if one point is from the
            // possibleInnerPolygon is inside the outer polygon.
            return IsPointInsidePolygon(outerPolygon, possibleInnerPolygon.Path.First());
        }

        /// <summary>
        ///     Determines if a point is inside a polygon, using ray casting. This is slower than the method
        ///     below, but does allow determination of whether a point is on the boundary.
        /// </summary>
        public static bool IsPointInsidePolygon(Polygon polygon, Point pointInQuestion, out Line closestLineAbove, out Line closestLineBelow, out bool onBoundary,
            bool onBoundaryIsInside = true)
        {
            //This function has three layers of checks. 
            //(1) Check if the point is inside the axis aligned bouning box. If it is not, then return false.
            //(2) Check if the point is == to a polygon point, return onBoundaryIsInside.
            //(3) Use line-sweeping / ray casting to determine if the polygon contains the point.
            closestLineAbove = null;
            closestLineBelow = null;
            onBoundary = false;
            //1) Check if center point is within bounding box of each polygon
            if (!pointInQuestion.X.IsLessThanNonNegligible(polygon.MaxX) ||
                !pointInQuestion.X.IsGreaterThanNonNegligible(polygon.MinX) ||
                !pointInQuestion.Y.IsLessThanNonNegligible(polygon.MaxY) ||
                !pointInQuestion.Y.IsGreaterThanNonNegligible(polygon.MinY)) return false;

            var points = new List<Point>(polygon.Path);

            //2) If the point in question is == a point in points, then it is inside the polygon
            if (
                points.Any(
                    point =>
                        point.X.IsPracticallySame(pointInQuestion.X) && point.Y.IsPracticallySame(pointInQuestion.Y)))
            {
                onBoundary = true;
                return onBoundaryIsInside;
            }

            //Make sure polygon indices are set properly
            if (polygon.Index == -1) polygon.Index = 0;
            foreach (var point in points.Where(point => point.PolygonIndex != polygon.Index))
            {
                point.PolygonIndex = polygon.Index;
            }
            //Force the point in question not to have the same index, if it does.
            if (pointInQuestion.PolygonIndex == polygon.Index) pointInQuestion.PolygonIndex = -1;

            //Sort points ascending x, then by ascending y.
            points.Add(pointInQuestion);
            var sortedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var lineList = new HashSet<Line>();

            //3) Use Line sweep to determine if the polygon contains the point.
            //An odd number of lines above and below a point, means the point is inside the polygon.
            //Note: either above or below should work. Checks both to catch errors.
            foreach (var point in sortedPoints)
            {
                if (point.PolygonIndex == polygon.Index)
                {
                    //Add to or remove from Line Sweep
                    foreach (var line in point.Lines)
                    {
                        if (lineList.Contains(line))
                        {
                            lineList.Remove(line);
                        }
                        else
                        {
                            lineList.Add(line);
                        }
                    }
                }
                else
                {
                    //If reached the point in question, then find intercepts on the lineList 
                    bool isOnLine;
                    var numberOfLinesAbove = NumberOfLinesAbovePoint(pointInQuestion, lineList, out closestLineAbove, out isOnLine);
                    //Check if the point is on the left line or right line (note that one direction search is sufficient).
                    if (isOnLine)
                    {
                        onBoundary = true;
                        return onBoundaryIsInside;
                    }

                    //Else, not on a boundary, so check to see that it is in between an odd number of lines to left and right
                    if (numberOfLinesAbove % 2 == 0) return false;
                    var numberOfLinesBelow = NumberOfLinesBelowPoint(pointInQuestion, lineList, out closestLineBelow, out isOnLine);
                    //No need to check isOnLine, since it is the same lines and point as the lines above check.
                    return numberOfLinesBelow % 2 != 0;
                }
            }
            //If not returned, throw error
            throw new Exception("Failed to return intercept information");
        }

        /// <summary>
        ///     Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        ///     And the polygon is not self-intersecting. This is a newer, much faster implementation than prior
        ///     the prior method, making use of W. Randolph Franklin's compact algorithm
        ///     https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        ///     Major Assumptions: 
        ///     1) The polygon can be convex
        ///     2) The direction of the polygon does not matter  
        /// 
        ///     Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="p"></param>
        /// <param name="returnSharedPointAsInside"></param>
        /// <returns></returns>
        public static bool IsPointInsidePolygon(PolygonLight polygon, PointLight p, bool returnSharedPointAsInside = false)
        {
            //Check if the point is the same as any of the polygon's points
            foreach (var point in polygon.Path)
            {
                if (point.X.IsPracticallySame(p.X) && point.Y.IsPracticallySame(p.Y))
                {
                    return returnSharedPointAsInside;
                }
            }

            //1) Check if center point is within bounding box of each polygon
            if (!p.X.IsLessThanNonNegligible(polygon.MaxX) ||
                !p.X.IsGreaterThanNonNegligible(polygon.MinX) ||
                !p.Y.IsLessThanNonNegligible(polygon.MaxY) ||
                !p.Y.IsGreaterThanNonNegligible(polygon.MinY)) return false;

            //2) Next, see how many lines are to the left of the point, using a fixed y value.
            //This compact, effecient 7 lines of code is from W. Randolph Franklin
            //<https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html>
            var path = polygon.Path;
            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                if ((path[i].Y > p.Y) != (path[j].Y > p.Y) &&
                     p.X < (path[j].X - path[i].X) * (p.Y - path[i].Y) / (path[j].Y - path[i].Y) + path[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        ///     Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        ///     And the polygon is not self-intersecting. This is a newer, much faster implementation than prior
        ///     the prior method, making use of W. Randolph Franklin's compact algorithm
        ///     https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        ///     Major Assumptions: 
        ///     1) The polygon can be convex
        ///     2) The direction of the polygon does not matter  
        /// 
        ///     Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        /// <param name="path"></param>
        /// <param name="p"></param>
        public static bool IsPointInsidePolygon(List<Point> path, Point point, bool returnSharedPointAsInside = false)
        {
            return IsPointInsidePolygon(path.Select(p => p.Light).ToList(), new PointLight(point.X, point.Y), returnSharedPointAsInside);
        }

        /// <summary>
        ///     Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        ///     And the polygon is not self-intersecting. This is a newer, much faster implementation than prior
        ///     the prior method, making use of W. Randolph Franklin's compact algorithm
        ///     https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        ///     Major Assumptions: 
        ///     1) The polygon can be convex
        ///     2) The direction of the polygon does not matter  
        /// 
        ///     Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        /// <param name="path"></param>
        /// <param name="p"></param>
        public static bool IsPointInsidePolygon(List<PointLight> path, PointLight p, bool returnSharedPointAsInside = false)
        {
            //Check if the point is the same as any of the polygon's points
            foreach (var point in path)
            {
                if (point.X.IsPracticallySame(p.X) && point.Y.IsPracticallySame(p.Y))
                {
                    return returnSharedPointAsInside;
                }
            }

            //1) Get the axis aligned bounding box of the path. This is super fast.
            //If the point is inside the bounding box, continue to check with more detailed methods, 
            //Else, retrun false.
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            foreach (var point in path)
            {
                if (point.X < xMin) xMin = point.X;
                if (point.X > xMax) xMax = point.X;
                if (point.Y < yMin) yMin = point.Y;
                if (point.Y > yMax) yMax = point.Y;
            }
            if (p.Y < yMin || p.Y > yMax || p.X < xMin || p.X > xMax) return false;

            //2) Next, see how many lines are to the left of the point, using a fixed y value.
            //This compact, effecient 7 lines of code is from W. Randolph Franklin
            //<https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html>
            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                if ((path[i].Y > p.Y) != (path[j].Y > p.Y) &&
                     p.X < (path[j].X - path[i].X) * (p.Y - path[i].Y) / (path[j].Y - path[i].Y) + path[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// This algorithm returns whether two polygons intersect. It can be used to triangle/triangle intersections,
        /// or any abitrary set of polygons. If two polygons are touching, they are not considered to be intersecting.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        //To get the area of intersection, use the Sutherland–Hodgman algorithm for polygon clipping
        // https://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
        public static bool IsPolygonIntersectingPolygon(Polygon subject, Polygon clip)
        {
            //Get the axis aligned bounding box of the path. This is super fast.
            //If the point is inside the bounding box, continue to check with more detailed methods, 
            //Else, return false.
            if( subject.MinX > clip.MaxX ||
                subject.MaxX < clip.MinX ||
                subject.MinY > clip.MaxY ||
                subject.MaxY < clip.MinY) return false;     

            //Check if either polygon is fully encompassed by the other
            if(clip.Path.Any(p => IsPointInsidePolygon(subject.Light, p.Light))) return true;
            if(subject.Path.Any(p => IsPointInsidePolygon(clip.Light, p.Light))) return true;

            //Else, any remaining intersection will be defined by one or more crossing lines
            //Check for intersections between all but one of the clip lines with all of the subject lines.
            //This is enough to test for intersection because we have already performed a point check.
            //This makes very little difference for large polygons, but cuts 9 operations down to 6 for 
            //a triangle/triangle intersection
            var clipPathLength = clip.Path.Count;
            var subjectPathLength = subject.Path.Count;
            //This next section gathers the points rather than using polygon.PathLines, so that the 
            //PathLines do not need to be set (we don't even use them in LineLineIntersection).
            for (var i = 0; i < clipPathLength - 1; i++) //-1 since we only need two lines
            {
                var point1 = clip.Path[i];
                var point2 = (i == clipPathLength - 1) ? clip.Path[0] : clip.Path[i + 1]; //Wrap back around to 0. Else use i+1
                for (var j = 0; j < subjectPathLength; j++) //Need to consider all the lines
                {
                    var point3 = subject.Path[j];
                    var point4 = (j == subjectPathLength - 1) ? subject.Path[0] : subject.Path[j + 1]; //Wrap back around to 0. Else use i+1
                    if (LineLineIntersection(point1, point2, point3, point4, out var intersectionPoint, false))
                    {
                        if (intersectionPoint == point1 ||
                            intersectionPoint == point1 ||
                            intersectionPoint == point3 ||
                            intersectionPoint == point4)
                        {
                            continue;
                        }
                        //Else
                        return true;
                    }
                }
            }

            //No intersections identified. Return false.
            return false;
        }
    }
    #endregion
}