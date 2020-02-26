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
using TVGL.Numerics;
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
        public static void SortAlongDirection(Vector3 direction, IEnumerable<Vertex> vertices,
            out List<(Vertex, double)> sortedVertices)
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
        public static void SortAlongDirection(Vector3 direction, IEnumerable<Vertex> vertices,
            out List<Vertex> sortedVertices)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            var vertexDistances = GetVertexDistances(direction, vertices);

            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            sortedVertices = vertexDistances.OrderBy(p => p.Item2).Select(p => p.Item1).ToList();
        }

        private static IEnumerable<(Vertex, double)> GetVertexDistances(Vector3 direction, IEnumerable<Vertex> vertices)
        {
            var vertexDistances = new List<(Vertex, double)>(vertices.Count());
            //Accuracy to the 15th decimal place
            var toleranceString = EqualityExtensions.EqualityTolerance.ToString(CultureInfo.InvariantCulture);
            var tolerance = int.Parse(toleranceString.Substring((toleranceString.IndexOf("-", StringComparison.Ordinal) + 1)));
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction with accuracy to the 15th decimal place to match StarMath
                var d = Math.Round(direction.Dot(vertex.Coordinates), tolerance);
                vertexDistances.Add((vertex, d));
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
            var toleranceString = EqualityExtensions.EqualityTolerance.ToString(CultureInfo.InvariantCulture);
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
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<Vector2> PointLights,
               out List<(Vector2, double)> sortedPointLights, int numDecimals)
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
        public static void SortAlongDirection(double directionX, double directionY, IEnumerable<Vector2> PointLights,
               out List<Vector2> sortedPointLights, int numDecimals)
        {
            var PointLightDistances = GetPointLightDistances(directionX, directionY, PointLights, numDecimals);
            sortedPointLights = PointLightDistances.OrderBy(PointLight => PointLight.Item2).Select(p => p.Item1).ToList();
        }

        private static IEnumerable<(Vector2, double)> GetPointLightDistances(double directionX, double directionY,
            IEnumerable<Vector2> PointLights, int numDecimals)
        {
            var PointLightDistances = new List<(Vector2, double)>(PointLights.Count());
            //Accuracy to the 15th decimal place
            foreach (var PointLight in PointLights)
            {
                //Get distance along the search direction with accuracy to the 15th decimal place
                var d = Math.Round(directionX * PointLight.X + directionY * PointLight.Y, numDecimals); //2D dot product
                PointLightDistances.Add((PointLight, d));
            }
            return PointLightDistances;
        }


        public static Vector3 GetPerpendicularDirection(Vector3 direction)
        {
            //If any of the normal terms (X, Y, or Z) are zero, then that will be direction 2
            if (direction.X.IsNegligible())
            {
                return new Vector3(1.0, 0.0, 0.0);
            }
            else if (direction.Y.IsNegligible())
            {
                return new Vector3(0.0, 1.0, 0.0);
            }
            else if (direction.Z.IsNegligible())
            {
                return new Vector3(0.0, 0.0, 1.0);
            }
            //Otherwise, 
            else
            {
                //Choose two perpendicular vectors.
                var v1 = new Vector3(direction.Y, -direction.X, 0.0);
                var v2 = new Vector3(-direction.Z, 0.0, direction.X);

                //Any linear combination of them is also perpendicular to the original vector
                return (v1 + v2).Normalize();
            }
        }
        #endregion

        #region Perimeter
        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static double Perimeter(IList<Vector2> polygon)
        {
            double perimeter = Vector2.Distance(polygon.Last(), polygon[0]);
            for (var i = 1; i < polygon.Count; i++)
                perimeter += Vector2.Distance(polygon[i - 1], polygon[i]);
            return perimeter;
        }

        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static double Perimeter(IList<Point> polygon)
        {
            double perimeter = Vector2.Distance(polygon.Last().Light, polygon[0].Light);
            for (var i = 1; i < polygon.Count; i++)
                perimeter += Vector2.Distance(polygon[i - 1].Light, polygon[i].Light);
            return perimeter;
        }

        /// <summary>
        /// Gets the Perimeter (length of a loop) of a 3D set of Vertices.
        /// </summary>
        /// <param name="polygon3D"></param>
        /// <returns></returns>
        public static double Perimeter(IList<Vertex> polygon3D)
        {

            double perimeter = Vector3.Distance(polygon3D.Last().Coordinates, polygon3D[0].Coordinates);
            for (var i = 1; i < polygon3D.Count; i++)
                perimeter += Vector3.Distance(polygon3D[i - 1].Coordinates, polygon3D[i].Coordinates);
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
                    if (distinctList[i].Normal.Dot(distinctList[i - 1].Normal).IsPracticallySame(1.0, tolerance) ||
                        (removeOpposites && distinctList[i].Normal.Dot(distinctList[i - 1].Normal).IsPracticallySame(-1, tolerance)))
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
            //Used hashset for "Contains" function calls 
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
                var flat = new Flat(flatHashSet) { Tolerance = tolerance };
                //Stacks are fast for "Push" and "Pop".
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
        public static double AreaOfPolygon(IList<List<Vector2>> paths)
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
        ///     Faster Method: http://geomalgorithms.com/a01-_area.html.
        ///     The faster method has been optimized for speed, since it is called often.
        /// </reference>
        public static double AreaOfPolygon(IList<Vector2> polygon)
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

            //Also check if all x are the same. The algorithm will catch all y's and output zero,
            //But it may output a small number, even if all the x's are the same
            var n = polygon.Count;
            var p0 = polygon[0];
            var xval = p0.X;

            //Optimized version reduces get functions from arrays and point.X and point.Y
            // j == i - 1;
            // k == i + 1
            var area = 0.0;
            var p1 = polygon[1];
            var jY = p0.Y;
            var iX = p1.X;
            var iY = p1.Y;
            var returnZero = true;
            for (var i = 1; i < n - 1; i++)
            {
                var kPoint = polygon[i + 1]; //Thus i < n - 1
                var kX = kPoint.X;
                var kY = kPoint.Y;
                area += iX * (kY - jY);

                if (returnZero && !iX.IsPracticallySame(xval))
                {
                    returnZero = false;
                }

                //Update values
                jY = iY; //move j to i
                iY = kY; //move i to k
                iX = kX;
            }
            //Final wrap around terms (Note: this is faster than checking an if condition in the for loop).
            var pN = polygon[n - 1];
            area += p0.X * (p1.Y - pN.Y);
            area += pN.X * (p0.Y - polygon[n - 2].Y);
            //If all x's were the same, we still need to check the last point, since i doesn't do it in the loop.
            if (returnZero && pN.X.IsPracticallySame(xval)) return 0.0;

            return area / 2;
        }

        /// <summary>
        ///     Calculate the area of any non-intersecting polygon in 3D space (loops)
        ///     This is faster than projecting to a 2D surface first in a seperate function.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double.</returns>
        /// <references>http://geomalgorithms.com/a01-_area.html </references>
        public static double AreaOf3DPolygon(IEnumerable<Vertex> loop, Vector3 normal)
        {
            var ax = Math.Abs(normal.X);
            var ay = Math.Abs(normal.Y);
            var az = Math.Abs(normal.Z);

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
            var area = 0.0;
            int i;
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
                    area *= an / (2 * normal.X);
                    break;
                case 2:
                    area *= an / (2 * normal.Y);
                    break;
                case 3:
                    area *= an / (2 * normal.Z);
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
        public static double AreaOf3DPolygon(IEnumerable<Vector3> loop, Vector3 normal)
        {
            var ax = Math.Abs(normal.X);
            var ay = Math.Abs(normal.Y);
            var az = Math.Abs(normal.Z);

            //Make a new list from the loop
            var vertices = new List<Vector3>(loop);
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
            var area = 0.0;
            int i;
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
                    area *= an / (2 * normal.X);
                    break;
                case 2:
                    area *= an / (2 * normal.Y);
                    break;
                case 3:
                    area *= an / (2 * normal.Z);
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
                count += seperateSolid.Count;
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
        /// 
        /// <returns></returns>
        public static List<Point> Get2DProjectionPointsReorderingIfNecessary(IEnumerable<Vertex> loop, Vector3 direction, out Matrix4x4 backTransform, double tolerance = Constants.BaseTolerance)
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
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, Vector3 direction,
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
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, Vector3 direction, double mergeDuplicateTolerance)
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
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, Vector3 direction,
            out Matrix4x4 backTransform,
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
        public static Point[] Get2DProjectionPoints(IEnumerable<Vertex> vertices, Matrix4x4 transform,
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
                    points.Add(new Point(vertex, point.X, point.Y));
                }
                else
                {
                    point = new Vector2(Math.Round(point.X, numDecimalPoints), Math.Round(point.Y, numDecimalPoints));
                    // todo....is this lookupString to slow
                    var lookupString = point.X.ToString(stringformat) + "|"
                                       + point.Y.ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, Add reference and move to the next vertex */
                        if (simpleCompareDict[lookupString].References != null)
                        {
                            simpleCompareDict[lookupString].References.Add(vertex);
                        }
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new Point(vertex, point.X, point.Y);
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
        /// 
        /// <returns></returns>
        public static List<Vector2> Get2DProjectionPointsAsLightReorderingIfNecessary(IEnumerable<Vertex> loop, Vector3 direction,
            out Matrix4x4 backTransform, double tolerance = Constants.BaseTolerance)
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
            if (attempts == 4) throw new Exception("Major area mismatch during 2D projection. Resulting path is incorrect");
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
        public static List<Vector2> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, Vector3 direction,
            out Matrix4x4 backTransform,
            bool mergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return Get2DProjectionPointsAsLight(vertices, transform, mergeDuplicateReferences);
        }
        public static List<Vector2> Get2DProjectionPointsAsLight(IEnumerable<Vector3> vertices, Vector3 direction,
            out Matrix4x4 backTransform,
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
        public static List<Vector2> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, Vector3 direction,
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
        public static List<Vector2> Get2DProjectionPointsAsLight(IEnumerable<Vertex> vertices, Matrix4x4 transform,
            bool mergeDuplicateReferences = false, double sameTolerance = Constants.BaseTolerance)
        {
            var points = new List<Vector2>();
            var simpleCompareDict = new Dictionary<string, Vector2>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var stringformat = "F" + numDecimalPoints;
            foreach (var vertex in vertices)
            {
                var point = Get2DProjectionPoint(vertex, transform);
                if (!mergeDuplicateReferences)
                {
                    points.Add(new Vector2(point.X, point.Y));
                }
                else
                {
                    point = new Vector2(Math.Round(point.X, numDecimalPoints), Math.Round(point.Y, numDecimalPoints));
                    // todo...is this lookupString slow?
                    var lookupString = point.X.ToString(stringformat) + "|"
                                       + point.Y.ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, move to the next vertex */
                        continue;
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new Vector2(point.X, point.Y);
                        simpleCompareDict.Add(lookupString, point2D);
                        points.Add(point2D);
                    }
                }
            }
            return points;
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
        public static List<Vector2> Get2DProjectionPointsAsLight(IEnumerable<Vector3> vertices, Matrix4x4 transform,
            bool mergeDuplicateReferences = false, double sameTolerance = Constants.BaseTolerance)
        {
            var points = new List<Vector2>();
            var simpleCompareDict = new Dictionary<string, Vector2>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var stringformat = "F" + numDecimalPoints;
            foreach (var vertex in vertices)
            {
                var point = Get2DProjectionPoint(vertex, transform);
                if (!mergeDuplicateReferences)
                {
                    points.Add(new Vector2(point.X, point.Y));
                }
                else
                {
                    point = new Vector2(Math.Round(point.X, numDecimalPoints), Math.Round(point.Y, numDecimalPoints));
                    // todo....lookupString is too slow
                    var lookupString = point.X.ToString(stringformat) + "|"
                                       + point.Y.ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, move to the next vertex */
                        continue;
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new Vector2(point.X, point.Y);
                        simpleCompareDict.Add(lookupString, point2D);
                        points.Add(point2D);
                    }
                }
            }
            return points;
        }

        public static Vector2 Get2DProjectionPointAsLight(Vertex vertex, Matrix4x4 transform)
        {
            var position = Get2DProjectionPoint(vertex, transform);
            return new Vector2(position.X, position.Y);
        }

        public static Vector2 Get2DProjectionPoint(Vertex vertex, Matrix4x4 transform)
        {
            return Get2DProjectionPoint(vertex.Coordinates, transform);
        }

        public static Vector2 Get2DProjectionPoint(Vector3 vertex, Matrix4x4 transform)
        {
            var point2D = Vector3.Transform(vertex, transform);
            return new Vector2(point2D.X, point2D.Y);
        }


        /// <summary>
        ///     Returns the positions (array of 2D arrays) of the vertices as that they would be represented in
        ///     the x-y plane (z-values will be zero). This does not destructively alter the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Vector2[].</returns>
        public static Vector2[] Get2DProjectionPoints(IList<Vector3> vertices, Vector3 direction)
        {
            var transform = TransformToXYPlane(direction);
            var points = new Vector2[vertices.Count];
            for (var i = 0; i < vertices.Count; i++)
            {
                var pointAs2D = Vector3.Transform(vertices[i], transform);
                points[i] = new Vector2(pointAs2D.X, pointAs2D.Y);
            }
            return points;
        }

        /// <summary>
        ///     Gets the 2D projection vector
        /// </summary>
        /// <param name="vector3D"></param>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Vector2[].</returns>
        public static Vector2 Get2DProjectionVector(Vector3 vector3D, Vector3 direction)
        {
            var transform = TransformToXYPlane(direction);
            var vProjected = Vector3.Transform(vector3D, transform);
            return new Vector2(vProjected.X, vProjected.Y);
        }

        /// <summary>
        ///     Transforms to xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Vector2.</returns>
        public static Matrix4x4 TransformToXYPlane(Vector3 direction)
        {
            return TransformToXYPlane(direction, out _);
        }

        /// <summary>
        ///     Transforms to xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <returns>System.Vector2.</returns>
        public static Matrix4x4 TransformToXYPlane(Vector3 direction, out Matrix4x4 backTransform)
        {
            var xDir = direction.X;
            var yDir = direction.Y;
            var zDir = direction.Z;

            Matrix4x4 rotateX, rotateY, backRotateX, backRotateY;
            if (xDir.IsNegligible() && zDir.IsNegligible())
            {
                rotateX = Matrix4x4.CreateRotationX(Math.Sign(yDir) * Math.PI / 2);
                backRotateX = Matrix4x4.CreateRotationX(-Math.Sign(yDir) * Math.PI / 2);
                backRotateY = rotateY = Matrix4x4.Identity;
            }
            else if (zDir.IsNegligible())
            {
                rotateY = Matrix4x4.CreateRotationY(-Math.Sign(xDir) * Math.PI / 2);
                backRotateY = Matrix4x4.CreateRotationY(Math.Sign(xDir) * Math.PI / 2);
                var rotXAngle = Math.Atan(yDir / Math.Abs(xDir));
                rotateX = Matrix4x4.CreateRotationX(rotXAngle);
                backRotateX = Matrix4x4.CreateRotationX(-rotXAngle);
            }
            else
            {
                var rotYAngle = -Math.Atan(xDir / zDir);
                rotateY = Matrix4x4.CreateRotationY(rotYAngle);
                backRotateY = Matrix4x4.CreateRotationY(-rotYAngle);
                var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
                var rotXAngle = Math.Sign(zDir) * Math.Atan(yDir / baseLength);
                rotateX = Matrix4x4.CreateRotationX(rotXAngle);
                backRotateX = Matrix4x4.CreateRotationX(-rotXAngle);
            }
            backTransform = backRotateY * backRotateX;
            return rotateX * rotateY;
        }

        /// <summary>
        /// Backtransforms a 2D vector from an XY plane. Return 3D vector.
        /// </summary>
        /// <param name="direction2D"></param>
        /// <param name="backTransform"></param>
        /// <returns></returns>
        public static Vector3 Convert2DVectorTo3DVector(Vector2 direction2D, Matrix4x4 backTransform)
        {
            var tempVector = new Vector3(direction2D.X, direction2D.Y, 0);
            return Vector3.Transform(tempVector, backTransform);
        }


        /// <summary>
        /// Gets 3D vertices from 2D points, the projection direction, and the distance along that direction.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="direction"></param>
        /// <param name="distanceAlongDirection"></param>
        /// <returns></returns>
        public static List<Vertex> GetVerticesFrom2DPoints(IEnumerable<Point> points, Vector3 direction, double distanceAlongDirection)
        {
            //Rotate axis back to the original, and then transform points along the given direction.
            //If you try to transform first, it will shift the vertices incorrectly
            TransformToXYPlane(direction, out var backTransform);
            var directionVector = direction * distanceAlongDirection;
            var contour = new List<Vertex>();
            foreach (var point in points)
            {
                var position = new Vector3(point.X, point.Y, 0.0);
                var untransformedPosition = position.Transform(backTransform);
                var vertexPosition = untransformedPosition + directionVector;

                contour.Add(new Vertex(vertexPosition));
            }

            return new List<Vertex>(contour);
        }

        /// <summary>
        /// Gets 3D vertices from 2D points, the projection direction, and the distance along that direction.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="direction"></param>
        /// <param name="distanceAlongDirection"></param>
        /// <returns></returns>
        public static List<Vertex> GetVerticesFrom2DPoints(List<Vector2> points, Vector3 direction, double distanceAlongDirection)
        {
            //Rotate axis back to the original, and then transform points along the given direction.
            //If you try to transform first, it will shift the vertices incorrectly
            TransformToXYPlane(direction, out var backTransform);
            var directionVector = direction * distanceAlongDirection;
            var contour = new List<Vertex>();
            foreach (var point in points)
            {
                var position = new Vector3(point.X, point.Y, 0.0);
                var untransformedPosition = position.Transform(backTransform);
                var vertexPosition = untransformedPosition + directionVector;

                contour.Add(new Vertex(vertexPosition));
            }

            return contour;
            //replacing the below line with the above. just return the contour, right? why put it in a new list
            //return new List<Vertex>(contour);
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
            var axis = edge1.Vector.Cross(edge2.Vector);
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return Math.Min(ExteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1], axis),
                InteriorAngleBetweenEdgesInCCWList(twoDEdges[0], twoDEdges[1], axis));
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
            var edge1 = new Vector2(b.X - a.X, b.Y - a.Y);
            var edge2 = new Vector2(c.X - b.X, c.Y - b.Y);
            return Math.Acos(edge1.Dot(edge2) / (edge1.Length() * edge2.Length()));
        }

        /// <summary>
        ///     Smallers the angle between edges.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        internal static double SmallerAngleBetweenEdges(Vector2 v0, Vector2 v1)
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
        internal static double ExteriorAngleBetweenEdgesInCCWList(Edge edge1, Edge edge2, Vector3 axis)
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
        internal static double InteriorAngleBetweenEdgesInCCWList(Edge edge1, Edge edge2, Vector3 axis)
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
        internal static double ExteriorAngleBetweenEdgesInCCWList(Vector2 edge1, Vector2 edge2, Vector3 axis)
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
        internal static double InteriorAngleBetweenEdgesInCCWList(Vector2 edge1, Vector2 edge2, Vector3 axis)
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
            return ExteriorAngleBetweenEdgesInCCWList(new Vector2(b.X - a.X, b.Y - a.Y), new Vector2(c.X - b.X, c.Y - b.Y));
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
            return InteriorAngleBetweenEdgesInCCWList(new Vector2(b.X - a.X, b.Y - a.Y), new Vector2(c.X - b.X, c.Y - b.Y));
        }

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(Vector2 a, Vector2 b, Vector2 c)
        {
            return InteriorAngleBetweenEdgesInCCWList(new Vector2(b.X - a.X, b.Y - a.Y), new Vector2(c.X - b.X, c.Y - b.Y));
        }

        /// <summary>
        ///     Projecteds the angle between vertices CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="positiveNormal">The positive normal.</param>
        /// <returns>System.Double.</returns>
        public static double ProjectedInteriorAngleBetweenVerticesCCW(Vertex a, Vertex b, Vertex c, Vector3 positiveNormal)
        {
            var points = Get2DProjectionPoints(new List<Vertex> { a, b, c }, positiveNormal);
            return InteriorAngleBetweenEdgesInCCWList(new Vector2(points[1].X - points[0].X, points[1].Y - points[0].Y),
                new Vector2(points[2].X - points[1].X, points[2].Y - points[1].Y));
        }

        public static double ProjectedExteriorAngleBetweenVerticesCCW(Vertex a, Vertex b, Vertex c, Vector3 positiveNormal)
        {
            return 2 * Math.PI - ProjectedInteriorAngleBetweenVerticesCCW(a, b, c, positiveNormal);
        }

        /// <summary>
        ///     Gets the exterior angle between two edges, assuming the edges are listed in CCW order.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double ExteriorAngleBetweenEdgesInCCWList(Vector2 v0, Vector2 v1)
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
        public static double InteriorAngleBetweenEdgesInCCWList(Vector2 v0, Vector2 v1)
        {
            #region Law of Cosines Approach (Commented Out)

            ////This is an alternative approach to the one that is not commented out
            ////Use law of cosines to find smaller angle between two vectors
            //var aSq = v0.X * v0.X + v0.Y * v0.Y;
            //var bSq = v1.X * v1.X + v1.Y * v1.Y;
            //var cSq = (v0.X + v1.X) * (v0.X + v1.X) + (v0.Y + v1.Y) * (v0.Y + v1.Y);
            //var angle = Math.Acos((aSq + bSq - cSq) / (2 * Math.Sqrt(aSq) * Math.Sqrt(bSq)));
            ////Use cross product sign to determine if smaller angle is CCW from v0
            //var cross = v0.X * v1.Y - v0.Y * v1.X;
            //if (Math.Sign(cross) < 0) angle = 2 * Math.PI - angle;

            #endregion

            var angleV0 = Math.Atan2(v0.Y, v0.X);
            var angleV1 = Math.Atan2(v1.Y, v1.X);
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
            var p = pt1;
            var p2 = pt2;
            var q = pt3;
            var q2 = pt4;
            var points = new List<Point> { pt1, pt2, pt3, pt4 };
            intersectionPoint = null;
            var r = p2 - p;
            var s = q2 - q;
            var rxs = r.X * s.Y - r.Y * s.X; //2D cross product, determines if parallel
            var qp = q - p;
            var qpxr = qp.X * r.Y - qp.Y * r.X;//2D cross product

            // If r x s ~ 0 and (q - p) x r ~ 0, then the two lines are possibly collinear.
            // This is negigible tolerance of 0.00001 is not arbitary. It was chosen because of the second case within this if statement.
            if (rxs.IsNegligible(0.00001) && qpxr.IsNegligible(0.00001))
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                var qpr = qp.X * r.X + qp.Y * r.Y;
                var pqs = (p - q).X * s.X + (p - q).Y * s.Y;
                var overlapping = (0 <= qpr && qpr <= r.X * r.X + r.Y * r.Y) ||
                                  (0 <= pqs && pqs <= s.X * s.X + s.Y * s.Y);
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
            var t = (q - p).Cross(s) / rxs;

            // u = (q - p) x r / (r x s)
            //Note, the output of this will be u = [0,0,#] since this is a 2D cross product.
            var u = (q - p).Cross(r) / rxs;

            // 5. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsNegligible() &&
                !t.IsLessThanNonNegligible() && !t.IsGreaterThanNonNegligible(1.0) &&
                !u.IsLessThanNonNegligible() && !u.IsGreaterThanNonNegligible(1.0))
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
                var x = p.X + t * r.X;
                var y = p.Y + t * r.Y;
                var x2 = q.X + u * s.X;
                var y2 = q.Y + u * s.Y;

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
        /// <returns>System.Vector2.</returns>
        public static Vector3 PointCommonToThreePlanes(Vector3 n1, double d1, Vector3 n2, double d2, Vector3 n3,
            double d3)
        {
            var matrixOfNormals = new Matrix3x3(n1.X, n1.Y, n1.Z, n2.X, n2.Y, n2.Z, n3.X, n3.Y, n3.Z);
            var distances = new Vector3(d1, d2, d3);
            if (!Matrix3x3.Invert(matrixOfNormals, out var mInv))
                return Vector3.Null;
            return distances.Transform(mInv);
        }

        public static Flat GetPlaneFromThreePoints(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var a1 = p2.X - p1.X;
            var b1 = p2.Y - p1.Y;
            var c1 = p2.Z - p1.Z;
            var a2 = p3.X - p1.X;
            var b2 = p3.Y - p1.Y;
            var c2 = p3.Z - p1.Z;
            var a = b1 * c2 - b2 * c1;
            var b = a2 * c1 - a1 * c2;
            var c = a1 * b2 - b1 * a2;
            var normal = new Vector3(a, b, c).Normalize();
            var flat2 = new Flat(p1, normal);
            return flat2;
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
        public static void LineIntersectingTwoPlanes(Vector3 n1, double d1, Vector3 n2, double d2,
            out Vector3 directionOfLine, out Vector3 pointOnLine)
        {
            directionOfLine = n1.Cross(n2).Normalize();
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
        internal static void LineIntersectingTwoPlanes(Vector3 n1, double d1, Vector3 n2, double d2,
            Vector3 directionOfLine, out Vector3 pointOnLine)
        {
            /* to find the point on the line...well a point on the line, it turns out that one has three unknowns (px, py, pz)
             * and only two equations. Let's put the point on the plane going through the origin. So this plane would have a normal 
             * of v (or DirectionOfLine). */
            var a = new Matrix3x3(n1.X, n1.Y, n1.Z, n2.X, n2.Y, n2.Z, directionOfLine.X, directionOfLine.Y, directionOfLine.Z);
            var b = new Vector3(d1, d2, 0);
            if (!Matrix3x3.Invert(a, out var aInv))
                pointOnLine = Vector3.Null;
            pointOnLine = b.Transform(aInv);
        }

        /// <summary>
        ///     Skeweds the line intersection.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="n1">The n1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="n2">The n2.</param>
        /// <returns>System.Double.</returns>
        internal static double SkewedLineIntersection(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2)
        {
            return SkewedLineIntersection(p1, n1, p2, n2, out _, out _, out _, out _, out _);
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
        internal static double SkewedLineIntersection(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2,
            out Vector3 center)
        {
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out _, out _, out _, out _);
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
        internal static double SkewedLineIntersection(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2,
            out Vector3 interSect1, out Vector3 interSect2)
        {
            return SkewedLineIntersection(p1, n1, p2, n2, out _, out interSect1, out interSect2, out _, out _);
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
        internal static double SkewedLineIntersection(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2,
            out Vector3 center, out double t1, out double t2)
        {
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out _, out _, out t1, out t2);
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
        internal static double SkewedLineIntersection(Vector3 p1, Vector3 n1, Vector3 p2, Vector3 n2,
            out Vector3 center,
            out Vector3 interSect1, out Vector3 interSect2, out double t1, out double t2)
        {
            var a11 = n1.X * n1.X + n1.Y * n1.Y + n1.Z * n1.Z;
            var a12 = -n1.X * n2.X - n1.Y * n2.Y - n1.Z * n2.Z;
            var a21 = n1.X * n2.X + n1.Y * n2.Y + n1.Z * n2.Z;
            var a22 = -n2.X * n2.X - n2.Y * n2.Y - n2.Z * n2.Z;
            var b1 = n1.X * (p2.X - p1.X) + n1.Y * (p2.Y - p1.Y) + n1.Z * (p2.Z - p1.Z);
            var b2 = n2.X * (p2.X - p1.X) + n2.Y * (p2.Y - p1.Y) + n2.Z * (p2.Z - p1.Z);
            //var a = new[,] { { a11, a12 }, { a21, a22 } };
            var aDetInverse = 1 / (a11 * a22 - a21 * a12);
            //var aInv = new[,] { { a22, -a12 }, {-a21,a11 } };
            var b = new[] { b1, b2 };
            //var t = solve(a, b);
            t1 = (a22 * b1 - a12 * b2) * aDetInverse;
            t2 = (-a21 * b1 + a11 * b2) * aDetInverse;
            interSect1 = new Vector3(p1.X + n1.X * t1, p1.Y + n1.Y * t1, p1.Z + n1.Z * t1);
            interSect2 = new Vector3(p2.X + n2.X * t2, p2.Y + n2.Y * t2, p2.Z + n2.Z * t2);
            center = new Vector3((interSect1.X + interSect2.X) / 2, (interSect1.Y + interSect2.Y) / 2,
                (interSect1.Z + interSect2.Z) / 2);
            return interSect1.Distance(interSect2);
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
        public static double DistancePointToLine(Vector3 qPoint, Vector3 lineRefPt, Vector3 lineVector)
        {
            return DistancePointToLine(qPoint, lineRefPt, lineVector, out _);
        }

        /// <summary>
        ///     Returns the distance the point on an infinite line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToLine(Vector3 qPoint, Vector3 lineRefPt, Vector3 lineVector,
            out Vector3 pointOnLine)
        {
            double t;
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
             * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            t = (lineVector.X * (qPoint.X - lineRefPt.X) + lineVector.Y * (qPoint.Y - lineRefPt.Y) +
                     lineVector.Z * (qPoint.Z - lineRefPt.Z))
                    / (lineVector.X * lineVector.X + lineVector.Y * lineVector.Y + lineVector.Z * lineVector.Z);
            pointOnLine = new Vector3(
            lineRefPt.X + lineVector.X * t, lineRefPt.Y + lineVector.Y * t, lineRefPt.Z + lineVector.Z * t);
            return qPoint.Distance(pointOnLine);
        }

        public static double DistancePointToLine(Vector2 qPoint, Vector2 lineRefPt, Vector2 lineVector,
    out Vector2 pointOnLine)
        {
            double t;
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
            * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            t = (lineVector.X * (qPoint.X - lineRefPt.X) + lineVector.Y * (qPoint.Y - lineRefPt.Y))
                    / (lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);
            pointOnLine = new Vector2(lineRefPt.X + lineVector.X * t, lineRefPt.Y + lineVector.Y * t);
            return qPoint.Distance(pointOnLine);
        }

        /// <summary>
        ///     Returns the distance the point on an infinite line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToLine(Point qPoint, Point lineRefPt, Vector2 lineVector,
            out Point pointOnLine)
        {
            double t;
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
            * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            t = (lineVector.X * (qPoint.X - lineRefPt.X) + lineVector.Y * (qPoint.Y - lineRefPt.Y))
                    / (lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);
            pointOnLine = new Point(lineRefPt.X + lineVector.X * t, lineRefPt.Y + lineVector.Y * t);
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
            return p1.Light.Distance(p2.Light);
        }

        /// <summary>
        ///     Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPoint(Vertex v1, Vertex v2)
        {
            return v1.Coordinates.Distance(v2.Coordinates);
        }

        /// <summary>
        ///     Returns the signed distance of the point to the plane.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="positionOnPlane">The position on plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(Vector3 point, Vector3 normalOfPlane, Vector3 positionOnPlane)
        {
            return DistancePointToPlane(point, normalOfPlane, positionOnPlane.Dot(normalOfPlane));
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
        public static double DistancePointToPlane(Vector3 point, Vector3 normalOfPlane, double signedDistanceToPlane)
        {
            return normalOfPlane.Dot(point) - signedDistanceToPlane;
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
        public static Vector3 PointOnFaceFromIntersectingLine(PolygonalFace face, Vector3 point1, Vector3 point2)
        {
            var positions = face.Vertices.Select(vertex => vertex.Coordinates).ToList();
            return PointOnFaceFromIntersectingLine(positions, face.Normal, point1, point2);
        }

        /// <summary>
        ///     Finds the point on the face made by a line (which is described by connecting point1 and point2) intersecting
        ///     with that face. If not intersection exists, then function returns a Vector3 with NaN's. Points must
        ///     be on either side of triangle to return a valid intersection.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="vertices"></param>
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnFaceFromIntersectingLine(List<Vector3> vertices, Vector3 normal, Vector3 point1,
            Vector3 point2)
        {
            var planeDistance = normal.Dot(vertices[0]);
            var d1 = normal.Dot(point1);
            var d2 = normal.Dot(point2);
            if (Math.Sign(planeDistance - d1) == Math.Sign(planeDistance - d2)) return Vector3.Null; //Points must be on either side of triangle
            var denominator = d2 - d1;
            if (denominator == 0) return Vector3.Null; //The points form a perpendicular line to the face
            var fraction = (planeDistance - d1) / denominator;
            var intersectionPoint = Vector3.Lerp(point1, point2, fraction);

            return IsVertexInsideTriangle(vertices, intersectionPoint, true) ? intersectionPoint : null;
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
        public static Vertex PointOnPlaneFromIntersectingLineSegment(Vector3 normalOfPlane, double distOfPlane, Vertex point1,
            Vertex point2)
        {
            var position =
                PointOnPlaneFromIntersectingLineSegment(normalOfPlane, distOfPlane, point1.Coordinates, point2.Coordinates);
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
        public static Vector3 PointOnPlaneFromIntersectingLineSegment(Vector3 normalOfPlane, double distOfPlane, Vector3 point1,
            Vector3 point2)
        {
            var position = PointOnPlaneFromIntersectingLine(normalOfPlane, distOfPlane, point1, point2);
            var d1 = point2.Subtract(point1).Length();
            var d2 = point2.Subtract(position).Length();
            var d3 = point1.Subtract(position).Length();
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
        public static Vertex PointOnPlaneFromIntersectingLine(Vector3 normalOfPlane, double distOfPlane, Vertex point1,
            Vertex point2)
        {
            return new Vertex(PointOnPlaneFromIntersectingLine(normalOfPlane, distOfPlane, point1.Coordinates, point2.Coordinates));
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
        public static Vector2 PointLightOnZPlaneFromIntersectingLine(double distOfPlane, Vertex point1,
            Vertex point2)
        {
            var toFactor = (distOfPlane - point1.Z) / (point2.Z - point1.Z);
            var fromFactor = 1 - toFactor;

            return new Vector2(fromFactor * point1.X + toFactor * point2.X,
                fromFactor * point1.Y + toFactor * point2.Y);
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
        public static Vector3 PointOnPlaneFromIntersectingLine(Vector3 normalOfPlane, double distOfPlane, Vector3 point1,
            Vector3 point2)
        {
            var d1 = normalOfPlane.Dot(point1);
            var d2 = normalOfPlane.Dot(point2);
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            return Vector3.Lerp(point1, point2, fraction);
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
        public static Point PointOnPlaneFromIntersectingLine(Vector2 normalOfPlane, double distOfPlane, Line line)
        {
            PointLightOnPlaneFromIntersectingLine(normalOfPlane.X, normalOfPlane.Y, distOfPlane, line.FromPoint.X, line.FromPoint.Y,
                line.ToPoint.X, line.ToPoint.Y, out var x, out var y);
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
        public static Vector2 PointLightOnPlaneFromIntersectingLine(Vector2 normalOfPlane, double distOfPlane, Line line)
        {
            PointLightOnPlaneFromIntersectingLine(normalOfPlane.X, normalOfPlane.Y, distOfPlane, line.FromPoint.X, line.FromPoint.Y,
                line.ToPoint.X, line.FromPoint.Y, out var x, out var y);
            return new Vector2(x, y);
        }

        public static void PointLightOnPlaneFromIntersectingLine(double normalOfPlaneX, double normalOfPlaneY, double distOfPlane,
            double fromPointX, double fromPointY, double toPointX, double toPointY, out double x, out double y)
        {
            var d1 = normalOfPlaneX * toPointX + normalOfPlaneY * toPointY; //2D Dot product
            var d2 = normalOfPlaneX * fromPointX + normalOfPlaneY * fromPointY;  //For a point, Position.Z = 0.0
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
        public static Vector3 PointOnPlaneFromRay(Vector3 normalOfPlane, double distOfPlane, Vector3 rayPosition,
            Vector3 rayDirection, out double signedDistance)
        {
            var dot = rayDirection.Dot(normalOfPlane);
            signedDistance = 0.0;
            if (dot == 0) return Vector3.Null;

            var d1 = -DistancePointToPlane(rayPosition, normalOfPlane, distOfPlane);
            signedDistance = d1 / dot;
            if (signedDistance.IsNegligible()) return rayPosition;
            return rayPosition + (rayDirection * signedDistance);
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
        /// 
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnTriangleFromLine(PolygonalFace face, Vertex vertex, Vector3 direction,
            out double signedDistance)
        {
            return PointOnTriangleFromLine(face, vertex.Coordinates, direction, out signedDistance);
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
        public static Vector3 PointOnTriangleFromLine(PolygonalFace face, Vector3 point3D, Vector3 direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            var distanceToOrigin = face.Normal.Dot(face.Vertices[0].Coordinates);
            var newPoint = PointOnPlaneFromRay(face.Normal, distanceToOrigin, point3D, direction, out signedDistance);
            if (newPoint == null) return Vector3.Null;
            return IsVertexInsideTriangle(face.Vertices, newPoint, onBoundaryIsInside) ? newPoint : Vector3.Null;
        }

        /// <summary>
        ///     Finds the point on the triangle made by a line. If that line is not going to pass through the
        ///     that triangle, then the result is comprise of NaN's. The signed distance is positive if the vertex points to
        ///     the triangle along the direction (ray). User can also specify whether the edges of the triangle
        ///     are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point3D"></param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        public static Vector3 PointOnTriangleFromLine(PolygonalFace face, Vector3 point3D, CartesianDirections direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            Vector3 newPoint;
            signedDistance = double.NaN;
            var d = face.Normal.Dot(face.Vertices[0].Coordinates);
            var n = face.Normal;
            switch (direction)
            {
                case CartesianDirections.XNegative:
                case CartesianDirections.XPositive:
                    if (face.Normal.X.IsNegligible()) return Vector3.Null;
                    newPoint = new Vector3((d - n.Y * point3D.Y - n.Z * point3D.Z) / n.X, point3D.Y, point3D.Z);
                    signedDistance = (Math.Sign((int)direction)) * (newPoint.X - point3D.X);
                    break;
                case CartesianDirections.YNegative:
                case CartesianDirections.YPositive:
                    if (face.Normal.Y.IsNegligible()) return Vector3.Null;
                    newPoint = new Vector3(point3D.X, (d - n.X * point3D.X - n.Z * point3D.Z) / n.Y, point3D.Z);
                    signedDistance = (Math.Sign((int)direction)) * (newPoint.Y - point3D.Y);
                    break;
                default:
                    if (face.Normal.Z.IsNegligible()) return Vector3.Null;
                    newPoint = new Vector3(point3D.X, point3D.Y, (d - n.X * point3D.X - n.Y * point3D.Y) / n.Z);
                    signedDistance = (Math.Sign((int)direction)) * (newPoint.Z - point3D.Z);
                    break;
            }

            return IsVertexInsideTriangle(face.Vertices, newPoint, onBoundaryIsInside) ? newPoint : Vector3.Null;
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
        public static List<Vector2> CreateCirclePath(Vector2 center, double radius, double radianIncrement = Math.PI / 50.0)
        {
            var path = new List<Vector2>();
            for (var theta = 0.0; theta < Math.PI * 2; theta += radianIncrement)
            {
                path.Add(new Vector2(radius * Math.Cos(theta) + center.X, radius * Math.Sin(theta) + center.Y));
            }
            return path;
        }

        /// <summary>
        /// Returns a the path of a circle made up of points. Increment as needed.
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="radianIncrement"></param>
        /// <returns></returns>
        public static List<Vector2> CreateCirclePath(BoundingCircle circle, double radianIncrement = Math.PI / 50.0)
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
        /// <param name="triangle">The vertices.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside triangle] [the specified vertices]; otherwise, <c>false</c>.</returns>
        /// <exception cref="Exception">Incorrect number of points in traingle</exception>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsVertexInsideTriangle(IList<Vertex> triangle, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsVertexInsideTriangle(triangle, vertexInQuestion.Coordinates, onBoundaryIsInside);
        }

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside." Assumes vertex in question is in the same plane
        ///     as the triangle.
        /// </summary>
        public static bool IsVertexInsideTriangle(IList<Vertex> triangle, Vector3 vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsVertexInsideTriangle(new[] { triangle[0].Coordinates, triangle[1].Coordinates, triangle[2].Coordinates }, 
                vertexInQuestion, onBoundaryIsInside);
        }

        /// <summary>
        ///     Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        ///     triangle are considered "inside." Assumes vertex in question is in the same plane
        ///     as the triangle.
        /// </summary>
        /// <param name="triangle"></param>
        /// <param name="pointInQuestion"></param>
        /// <param name="onBoundaryIsInside"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool IsVertexInsideTriangle(IList<Vector3> triangle, Vector3 pointInQuestion,
            bool onBoundaryIsInside = true)
        {
            if (triangle.Count != 3) throw new Exception("Incorrect number of points in triangle");
            var p = pointInQuestion;
            var a = triangle[0];
            var b = triangle[1];
            var c = triangle[2];
            return ((b - a).Cross(p - a)).Dot((p - a).Cross(c - a)) >= 0
                && ((c - b).Cross(p - b)).Dot((p - b).Cross(a - b)) >= 0;
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
                var direction = Vector3.Normalize(new Vector3(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble()));
                foreach (var face in ts.Faces)
                {
                    if (face.Vertices.Any(vertex => vertexInQuestion.X.IsPracticallySame(vertex.X) &&
                                                    vertexInQuestion.Y.IsPracticallySame(vertex.Y) &&
                                                    vertexInQuestion.Z.IsPracticallySame(vertex.Z)))
                    {
                        return onBoundaryIsInside;
                    }

                    var distanceToOrigin = face.Normal.Dot(face.Vertices[0].Coordinates);
                    var t = -(vertexInQuestion.Coordinates.Dot(face.Normal) - distanceToOrigin) /
                            direction.Dot(face.Normal);
                    //Note that if t == 0, then it is on the face
                    //else, find the intersection point and determine if it is inside the polygon (face)
                    var newVertex = t.IsNegligible()
                        ? vertexInQuestion
                        : new Vertex(vertexInQuestion.Coordinates + (direction * t));
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
            //(1) Check if the point is inside the axis aligned bounding box. If it is not, then return false.
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
                    var numberOfLinesAbove = NumberOfLinesAbovePoint(pointInQuestion, lineList, out closestLineAbove, out bool isOnLine);
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
        public static bool IsPointInsidePolygon(PolygonLight polygon, Vector2 p, bool returnSharedPointAsInside = false)
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
            return IsPointInsidePolygon(path.Select(p => p.Light).ToList(), new Vector2(point.X, point.Y), returnSharedPointAsInside);
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
        public static bool IsPointInsidePolygon(List<Vector2> path, Vector2 p, bool returnSharedPointAsInside = false)
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
            if (subject.MinX > clip.MaxX ||
                subject.MaxX < clip.MinX ||
                subject.MinY > clip.MaxY ||
                subject.MaxY < clip.MinY) return false;

            //Check if either polygon is fully encompassed by the other
            if (clip.Path.Any(p => IsPointInsidePolygon(subject.Light, p.Light))) return true;
            if (subject.Path.Any(p => IsPointInsidePolygon(clip.Light, p.Light))) return true;

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