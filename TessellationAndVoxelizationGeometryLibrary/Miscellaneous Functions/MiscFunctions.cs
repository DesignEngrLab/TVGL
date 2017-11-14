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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Miscellaneous Functions for TVGL
    /// </summary>
    public static class MiscFunctions
    {
        #region Sort Along Direction


        /// <summary>
        ///     Returns a list of sorted vertices along a set direction. Ties are broken by direction[1] then direction[2] if
        ///     available.
        /// </summary>
        /// <param name="directions">The directions.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="sortedVertices">The sorted vertices.</param>
        /// <param name="duplicateRanges">The duplicate ranges.</param>
        /// <exception cref="Exception">
        ///     Must provide between 1 to 3 direction vectors
        ///     or
        ///     Must provide between 1 to 3 direction vectors
        /// </exception>
        public static void SortAlongDirection(double[][] directions, IEnumerable<Vertex> vertices,
            out List<Vertex> sortedVertices,
            out List<int[]> duplicateRanges)
        {
            List<Tuple<Vertex, double>> sortedVertexDictionary;
            SortAlongDirection(directions, vertices, out sortedVertexDictionary, out duplicateRanges);
            //Convert output to a list of sorted vertices
            sortedVertices = sortedVertexDictionary.Select(element => element.Item1).ToList();
        }


        /// <summary>
        ///     Returns a list of sorted vertices along a set direction. Ties are broken by direction[1] then direction[2] if
        ///     available.
        /// </summary>
        /// <param name="directions">The directions.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="sortedVertices">The sorted vertices.</param>
        /// <param name="duplicateRanges">The duplicate ranges.</param>
        /// <exception cref="Exception">
        ///     Must provide between 1 to 3 direction vectors
        ///     or
        ///     Must provide between 1 to 3 direction vectors
        /// </exception>
        public static void SortAlongDirection(double[][] directions, IEnumerable<Vertex> vertices,
            out List<Tuple<Vertex, double>> sortedVertices,
            out List<int[]> duplicateRanges)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            duplicateRanges = new List<int[]>();
            sortedVertices = new List<Tuple<Vertex, double>>();
            var points = new List<Point>();
            var pointDistances = new Dictionary<int, double>();
            var pointIndex = 0;
            //Accuracy to the 15th decimal place
            var tolerance = Math.Round(1 / StarMath.EqualityTolerance);
            foreach (var vertex in vertices)
            {
                //Get distance along 3 directions (2 & 3 to break ties) with accuracy to the 15th decimal place
                Point point;
                var dot1 = directions[0].dotProduct(vertex.Position);
   
                switch (directions.Length)
                {                   
                    case 1:
                        {
                            point = new Point(vertex, Math.Round(dot1 * tolerance), 0.0, 0.0);
                        }
                        break;
                    case 2:
                        {
                            var dot2 = directions[1].dotProduct(vertex.Position);
                            point = new Point(vertex, Math.Round(dot1 * tolerance), Math.Round(dot2 * tolerance), 0.0);
                        }
                        break;
                    case 3:
                        {
                            var dot2 = directions[1].dotProduct(vertex.Position);
                            var dot3 = directions[2].dotProduct(vertex.Position);
                            point = new Point(vertex, Math.Round(dot1 * tolerance), Math.Round(dot2 * tolerance), Math.Round(dot3 * tolerance));           
                        }
                        break;
                    default:
                        throw new Exception("Must provide between 1 to 3 direction vectors");

                }
                point.IndexInPath = pointIndex;
                points.Add(point);
                pointDistances.Add(pointIndex, dot1);
                pointIndex++;
            }
            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            var sortedPoints =
                points.OrderBy(point => point.X).ThenBy(point => point.Y).ThenBy(point => point.Z).ToList();


            //Linear operation to locate duplicates and convert back to a list of vertices
            var previousDuplicate = false;
            var startIndex = 0;
            var startPoint = sortedPoints[0];
            sortedVertices.Add(new Tuple<Vertex, double>(startPoint.References[0], pointDistances[startPoint.IndexInPath]));
            var counter = 0;
            int[] intRange;
            switch (directions.Length)
            {
                case 1:
                    {
                        for (var i = 1; i < sortedPoints.Count; i++)
                        {
                            var point = sortedPoints[i];
                            sortedVertices.Add(new Tuple<Vertex, double>(point.References[0], pointDistances[point.IndexInPath]));
                            if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X))
                            {
                                counter++;
                                if (previousDuplicate) continue;
                                startIndex = i - 1;
                                previousDuplicate = true;
                                counter++;
                            }
                            else if (previousDuplicate)
                            {
                                intRange = new[] { startIndex, counter };
                                duplicateRanges.Add(intRange);
                                previousDuplicate = false;
                                counter = 0;
                            }
                        }
                        //Add last duplicate group if necessary
                        if (!previousDuplicate) return;
                        intRange = new[] { startIndex, counter };
                        duplicateRanges.Add(intRange);
                    }
                    break;
                case 2:
                    {
                        for (var i = 1; i < sortedPoints.Count; i++)
                        {
                            var point = sortedPoints[i];
                            sortedVertices.Add(new Tuple<Vertex, double>(point.References[0], pointDistances[point.IndexInPath]));
                            if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X) &&
                                sortedPoints[i - 1].Y.IsPracticallySame(sortedPoints[i].Y))
                            {
                                counter++;
                                if (previousDuplicate) continue;
                                startIndex = i - 1;
                                previousDuplicate = true;
                                counter++;
                            }
                            else if (previousDuplicate)
                            {
                                intRange = new[] { startIndex, counter };
                                duplicateRanges.Add(intRange);
                                previousDuplicate = false;
                                counter = 0;
                            }
                        }
                        //Add last duplicate group if necessary
                        if (!previousDuplicate) return;
                        intRange = new[] { startIndex, counter };
                        duplicateRanges.Add(intRange);
                    }
                    break;
                case 3:
                    {
                        for (var i = 1; i < sortedPoints.Count; i++)
                        {
                            var point = sortedPoints[i];
                            sortedVertices.Add(new Tuple<Vertex, double>(point.References[0], pointDistances[point.IndexInPath]));
                            if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X) &&
                                sortedPoints[i - 1].Y.IsPracticallySame(sortedPoints[i].Y) &&
                                sortedPoints[i - 1].Z.IsPracticallySame(sortedPoints[i].Z))
                            {
                                counter++;
                                if (previousDuplicate) continue;
                                startIndex = i - 1;
                                previousDuplicate = true;
                                counter++;
                            }
                            else if (previousDuplicate)
                            {
                                intRange = new[] { startIndex, counter };
                                duplicateRanges.Add(intRange);
                                previousDuplicate = false;
                                counter = 0;
                            }
                        }
                        //Add last duplicate group if necessary
                        if (!previousDuplicate) return;
                        intRange = new[] { startIndex, counter };
                        duplicateRanges.Add(intRange);
                    }
                    break;
                default:
                    throw new Exception("Must provide between 1 to 3 direction vectors");
            }
        }

        /// <summary>
        ///     Returns a list of sorted points along a set direction. Ties are broken by direction[1] if
        ///     available.
        /// </summary>
        /// <param name="direction">The directions.</param>
        /// <param name="points"></param>
        /// <param name="sortedPoints"></param>
        /// <exception cref="Exception">
        ///     Must provide between 1 to 3 direction vectors
        ///     or
        ///     Must provide between 1 to 3 direction vectors
        /// </exception>
        public static void SortAlongDirection(double[] direction, IList<Point> points,
               out List<Tuple<Point, double>> sortedPoints)
        {
            var directions = new[] {direction};
            SortAlongDirection(directions, points, out sortedPoints);
        }

        /// <summary>
        ///     Returns a list of sorted points along a set direction. Ties are broken by direction[1] if
        ///     available.
        /// </summary>
        /// <param name="directions">The directions.</param>
        /// <param name="points"></param>
        /// <param name="sortedPoints"></param>
        /// <exception cref="Exception">
        ///     Must provide between 1 to 3 direction vectors
        ///     or
        ///     Must provide between 1 to 3 direction vectors
        /// </exception>
        public static void SortAlongDirection(double[][] directions, IList<Point> points,
            out List<Tuple<Point, double>> sortedPoints)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            sortedPoints = new List<Tuple<Point, double>>();
            var tempPoints = new List<Point>();
            var pointDistances = new Dictionary<int, double>();
            var pointReferences = new Dictionary<int, Point>();
            var pointIndex = 0;
            //Accuracy to the 15th decimal place
            var tolerance = Math.Round(1 / StarMath.EqualityTolerance);
            foreach (var point in points)
            {
                //Get distance along 3 directions (2 & 3 to break ties) with accuracy to the 15th decimal place
                Point rotatedPoint;
                var dot1 = directions[0][0]*point.X + directions[0][1] * point.Y; //2D dot product

                switch (directions.Length)
                {
                    case 1:
                        {
                            rotatedPoint = new Point(Math.Round(dot1 * tolerance), 0.0);
                        }
                        break;
                    case 2:
                        {
                            var dot2 = directions[1][0] * point.X + directions[1][1] * point.Y; //2D dot product
                            rotatedPoint = new Point(Math.Round(dot1 * tolerance), Math.Round(dot2 * tolerance));
                        }
                        break;
                    default:
                        throw new Exception("Must provide between 1 to 2 direction vectors");

                }
                tempPoints.Add(rotatedPoint);
                rotatedPoint.ReferenceIndex = pointIndex;
                pointDistances.Add(pointIndex, dot1);
                pointReferences.Add(pointIndex, point);
                pointIndex++;
            }
            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            var sortedPointsTemp = tempPoints.OrderBy(point => point.X).ThenBy(point => point.Y).ToList();

            //Build the output list
            foreach (var rotatedPoint in sortedPointsTemp)
            {
                var originalPoint = pointReferences[rotatedPoint.ReferenceIndex];
                var distance = pointDistances[rotatedPoint.ReferenceIndex];
                sortedPoints.Add(new Tuple<Point, double>(originalPoint, distance));
            }
        }


        #endregion

        #region Perimeter
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
                            DistancePointToPoint2D(listWithStartPointAtEnd[i - 1], listWithStartPointAtEnd[i]);
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


        /// <summary>
        ///     Calculate the area of any non-intersecting polygon.
        /// </summary>
        public static double AreaOfPolygon(IList<List<Point>> paths)
        {
            return paths.Sum(path => AreaOfPolygon(path));
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
        public static double AreaOfPolygon(IList<Point> polygon)
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
        ///     Returns the 2D path (list of points) of the 3D loop (list of vertices) as that they would be represented in
        ///     the x-y plane (although the z-values will be non-zero). This does not destructively alter
        ///     the vertices. Additionally, this function will keep the loops in their original positive/negative
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

            return path ;
    }



        /// <summary>
        ///     Returns the positions (array of 3D arrays) of the vertices as that they would be represented in
        ///     the x-y plane (although the z-values will be non-zero). This does not destructively alter
        ///     the vertices.
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
        ///     Returns the positions (array of 3D arrays) of the vertices as that they would be represented in
        ///     the x-y plane (although the z-values will be non-zero). This does not destructively alter
        ///     the vertices.
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
        ///     Returns the positions (array of 3D arrays) of the vertices as that they would be represented in
        ///     the x-y plane (although the z-values will be non-zero). This does not destructively alter
        ///     the vertices.
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

        ///// <summary>
        ///// Get2s the d projection points.
        ///// </summary>
        ///// <param name="vertices">The vertices.</param>
        ///// <param name="transform">The transform.</param>
        ///// <param name="mergeDuplicateReferences">The merge duplicate references.</param>
        ///// <returns>Point[].</returns>
        //public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[,] transform,
        //    bool mergeDuplicateReferences = false)
        //{
        //    var points = new List<Point>();
        //    var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
        //    foreach (var vertex in vertices)
        //    {
        //        pointAs4[0] = vertex.Position[0];
        //        pointAs4[1] = vertex.Position[1];
        //        pointAs4[2] = vertex.Position[2];
        //        pointAs4 = transform.multiply(pointAs4);
        //        var point2D = new[] { pointAs4[0], pointAs4[1]};
        //        if (mergeDuplicateReferences)
        //        {
        //            var sameIndex = points.FindIndex(p => p.Position2D.IsPracticallySame(point2D));
        //            if (sameIndex >= 0)
        //            {
        //                //Add reference and move to the next vertex.
        //                points[sameIndex].References.Add(vertex);
        //                continue;
        //            }
        //        }
        //        points.Add(new Point(vertex, pointAs4[0], pointAs4[1]));
        //    }
        //    return points.ToArray();
        //}

        /// <summary>
        ///     Get2s the d projection points.
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
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            var simpleCompareDict = new Dictionary<string, Point>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var stringformat = "F" + numDecimalPoints;
            foreach (var vertex in vertices)
            {
                pointAs4[0] = vertex.Position[0];
                pointAs4[1] = vertex.Position[1];
                pointAs4[2] = vertex.Position[2];
                pointAs4 = transform.multiply(pointAs4);
                if (!mergeDuplicateReferences)
                {
                    points.Add(new Point(vertex, pointAs4[0], pointAs4[1]));
                }
                else
                {
                    pointAs4[0] = Math.Round(pointAs4[0], numDecimalPoints);
                    pointAs4[1] = Math.Round(pointAs4[1], numDecimalPoints);
                    var lookupString = pointAs4[0].ToString(stringformat) + "|"
                                       + pointAs4[1].ToString(stringformat);
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        /* if it's in the dictionary, Add reference and move to the next vertex */
                        simpleCompareDict[lookupString].References.Add(vertex);
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var point2D = new Point(vertex, pointAs4[0], pointAs4[1]);
                        simpleCompareDict.Add(lookupString, point2D);
                        points.Add(point2D);
                    }
                }
            }
            return points.ToArray();
        }

        /// <summary>
        ///     Gets the 2D projectsion points of vertices
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

        #endregion

        #region Angle between Edges/Lines

        /// <summary>
        ///     Smallers the angle between edges.
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
        ///     Smallers the angle between edges.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.Double.</returns>
        internal static double SmallerAngleBetweenEdges(Point a, Point b, Point c)
        {
            var edge1 = new[] { b.X - a.X, b.Y - a.Y };
            var edge2 = new[] { c.X - b.X, c.Y - b.Y };
            return Math.Acos(edge1.dotProduct(edge2) / (edge1.norm2() * edge2.norm2()));
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
        ///     Projecteds the angle between vertices CCW.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="positiveNormal">The positive normal.</param>
        /// <returns>System.Double.</returns>
        internal static double ProjectedAngleBetweenVerticesCCW(Vertex a, Vertex b, Vertex c, double[] positiveNormal)
        {
            var points = Get2DProjectionPoints(new List<Vertex> { a, b, c }, positiveNormal);
            return InteriorAngleBetweenEdgesInCCWList(new[] { points[1].X - points[0].X, points[1].Y - points[0].Y },
                new[] { points[2].X - points[1].X, points[2].Y - points[1].Y });
        }

        /// <summary>
        ///     Angles the between edges cw.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        internal static double ExteriorAngleBetweenEdgesInCCWList(double[] v0, double[] v1)
        {
            return 2 * Math.PI - InteriorAngleBetweenEdgesInCCWList(v0, v1);
        }

        //Gets the angle between edges that are ordered in a CCW list. 
        //NOTE: This is opposite from getting the CCW angle from v0 and v1.

        /// <summary>
        ///     Angles the between edges CCW.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <returns>System.Double.</returns>
        internal static double InteriorAngleBetweenEdgesInCCWList(double[] v0, double[] v1)
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
            var p = pt1.Position2D;
            var p2 = pt2.Position2D;
            var q = pt3.Position2D;
            var q2 = pt4.Position2D;
            var points = new List<Point> {pt1, pt2, pt3, pt4};
            intersectionPoint = null;
            var r = p2.subtract(p);
            var s = q2.subtract(q);
            var rxs = r[0] * s[1] - r[1] * s[0]; //2D cross product, determines if parallel
            var qp = q.subtract(p);
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
                var pqs = p.subtract(q)[0] * s[0] + p.subtract(q)[1] * s[1];
                var overlapping = (0 <= qpr && qpr <= r[0]*r[0] + r[1]*r[1]) ||
                                  (0 <= pqs && pqs <= s[0]*s[0] + s[1]*s[1]);
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
                if ((slope1*(pt3.X - pt1.X) + pt1.Y).IsPracticallySame(pt3.Y) &&
                    (slope1*(pt4.X - pt1.X) + pt1.Y).IsPracticallySame(pt4.Y))
                {
                    if (!considerCollinearOverlapAsIntersect) return false;
                    return true;
                }
                if ((slope2*(pt1.X - pt3.X) + pt3.Y).IsPracticallySame(pt1.Y) &&
                    (slope2*(pt2.X - pt3.X) + pt3.Y).IsPracticallySame(pt2.Y))
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
            directionOfLine = n1.crossProduct(n2).normalize();
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


        /// <summary>
        ///     Returns lists of vertices that are inside vs. outside of each solid.
        /// </summary>
        /// <param name="solid1">The solid1.</param>
        /// <param name="solid2">The solid2.</param>
        /// <param name="verticesFromSolid1InsideSolid2">The vertices from solid1 inside solid2.</param>
        /// <param name="verticesFromSolid1OutsideSolid2">The vertices from solid1 outside solid2.</param>
        /// <param name="verticesFromSolid2InsideSolid1">The vertices from solid2 inside solid1.</param>
        /// <param name="verticesFromSolid2OutsideSolid1">The vertices from solid2 outside solid1.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        public static void FindSolidIntersections(TessellatedSolid solid1, TessellatedSolid solid2,
            out List<Vertex> verticesFromSolid1InsideSolid2, out List<Vertex> verticesFromSolid1OutsideSolid2,
            out List<Vertex> verticesFromSolid2InsideSolid1, out List<Vertex> verticesFromSolid2OutsideSolid1,
            bool onBoundaryIsInside = true)
        {
            //Note: This mehtod should accurately tell you if any vertices are inside another solid, 
            //but there may be some cases (though rare) where faces go through another solid without leaving a vertex inside.
            //In this case, it would be better to check for face intersections with the lists rather than doing a ray cast with the vertices.

            //HOW IT WORKS:
            //The code sorts the vertices for each solid along a direction.
            //There is no real reason to pick a random direction, except that is what I was using with 
            //a try/catch to check another direction if it failed.Likely, that should be removed so it can actually be debugged if needed.

            //Then, the search method goes through a list of all the sorted vertices
            //If the vertex belongs to the second solid
            //1) it updates the outsideFaceList, which is the face list for Solid2.
            //2) It performs a ray cast to see how many Solid1 faces it intersects along a perpendicular direction to the search direction
            //It tracks whether the face is above or below the vertex in question.
            //3) It uses the number of faces above and below to determine whether it is inside Solid1.This is the same logic as for determining whether a point is inside a positive polygon.An even number of intercepts means the vertex is outside Solid1, while an odd number means it is inside.This should work whether the solids are convex or concave.

            //If the vertex belongs to the first solid, it does the same thing but using Solid2 and the insideFaceList.




            //Set reference indices to keep track of which vertex belong to which point
            //NOTE: if the two solids are in fact the same solid, this will fail, but it
            //will work as long as it is a copy of the solid.
            var insideVertices = new List<Vertex>(solid1.Vertices);
            foreach (var vertex in insideVertices) vertex.ReferenceIndex = 1;
            var outsideVertices = new List<Vertex>(solid2.Vertices);
            foreach (var vertex in outsideVertices) vertex.ReferenceIndex = 2;

            //Set directions, where dir2 is perpendicular to dir1 and dir3 is perpendicular to both dir1 and dir2.
            var rnd = new Random();
            var direction1 = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() }.normalize();
            var direction2 = new[] { direction1[1] - direction1[2], -direction1[0], direction1[0] }.normalize();
            //one of many
            var direction3 = direction1.crossProduct(direction2).normalize();
            var directions = new[] { direction1, direction2, direction3 };
            var allVertices = new List<Vertex>(insideVertices);
            allVertices.AddRange(outsideVertices);
            List<Vertex> sortedVertices;
            List<int[]> duplicateIndexRanges;
            SortAlongDirection(directions, allVertices, out sortedVertices, out duplicateIndexRanges);
            //if (onBoundaryIsInside && duplicateIndexRanges.Count > 1) return false;
            //Remove all duplicate vertices
            var offset = 0;
            foreach (var duplicateRange in duplicateIndexRanges)
            {
                sortedVertices.RemoveRange(duplicateRange[0] - offset, duplicateRange[1]);
                offset = offset + duplicateRange[1];
            }
            //The solids share all the same vertices (onBoundaryIsInside = false) was considered above
            //if (sortedVertices.Count < 1) return true; 
            //If the first or last vertex along the direction vectors was in the inside solid, then it is not inside
            //if (sortedVertices.First().ReferenceIndex == 1 || tempSortedVertices.Last().ReferenceIndex == 1) return false;

            //Perform a search along direction 1 looking for plane intercepts along direction 2.
            //This method assumes TRIANGLE FACES ONLY.
            var insideFaceList1 = new List<PolygonalFace>();
            var insideFaceList2 = new List<PolygonalFace>();
            var outsideFaceList1 = new List<PolygonalFace>();
            var outsideFaceList2 = new List<PolygonalFace>();
            verticesFromSolid1InsideSolid2 = new List<Vertex>();
            verticesFromSolid1OutsideSolid2 = new List<Vertex>();
            verticesFromSolid2InsideSolid1 = new List<Vertex>();
            verticesFromSolid2OutsideSolid1 = new List<Vertex>();
            foreach (var vertex in sortedVertices)
            {
                if (vertex.ReferenceIndex == 2)
                {
                    foreach (var triangle in vertex.Faces)
                    {
                        if (outsideFaceList1.Contains(triangle))
                        {
                            if (outsideFaceList2.Contains(triangle))
                            {
                                outsideFaceList1.Remove(triangle);
                                outsideFaceList2.Remove(triangle);
                            }
                            else outsideFaceList2.Add(triangle);
                        }
                        else outsideFaceList1.Add(triangle);
                    }
                    var faceCountBelow = 0;
                    var faceCountAbove = 0;
                    var inside = true;
                    foreach (var triangle in insideFaceList1)
                    {
                        double signedDistance;
                        //The following funtion returns null if the vertex does not intersect the triangle
                        var position = PointOnTriangleFromLine(triangle, vertex, direction2, out signedDistance);
                        if (position != null)
                        {
                            if (signedDistance.IsNegligible())
                            {
                                inside = onBoundaryIsInside;
                                //Set face counts to an odd number, and add vertex to list of inside vertices
                                faceCountAbove = 1;
                                faceCountBelow = 1;
                                break;
                            }
                            if (signedDistance < 0.0)
                            {
                                faceCountBelow++;
                            }
                            else
                            {
                                faceCountAbove++;
                            }
                        }
                    }
                    if (faceCountAbove == 0 || faceCountBelow == 0) inside = false;
                    if (faceCountAbove % 2 == 0 || faceCountBelow % 2 == 0)
                        inside = false; //Even number of intercepts, means the vertex is outside
                    if (inside) verticesFromSolid2InsideSolid1.Add(vertex);
                    else verticesFromSolid2OutsideSolid1.Add(vertex);
                }
                else
                {
                    foreach (var triangle in vertex.Faces)
                    {
                        if (insideFaceList1.Contains(triangle))
                        {
                            if (insideFaceList2.Contains(triangle))
                            {
                                insideFaceList1.Remove(triangle);
                                insideFaceList2.Remove(triangle);
                            }
                            else insideFaceList2.Add(triangle);
                        }
                        else insideFaceList1.Add(triangle);
                    }
                    var faceCountBelow = 0;
                    var faceCountAbove = 0;
                    var inside = true;
                    foreach (var triangle in outsideFaceList1)
                    {
                        double signedDistance;
                        //The following funtion returns null if the vertex does not intersect the triangle
                        var position = PointOnTriangleFromLine(triangle, vertex, direction2, out signedDistance);
                        if (position != null)
                        {
                            if (signedDistance.IsNegligible())
                            {
                                inside = onBoundaryIsInside;
                                //Set face counts to an odd number, and add vertex to list of inside vertices
                                faceCountAbove = 1;
                                faceCountBelow = 1;
                                break;
                            }
                            if (signedDistance < 0.0)
                            {
                                faceCountBelow++;
                            }
                            else
                            {
                                faceCountAbove++;
                            }
                        }
                    }
                    if (faceCountAbove == 0 || faceCountBelow == 0) inside = false;
                    if (faceCountAbove % 2 == 0 || faceCountBelow % 2 == 0)
                        inside = false; //Even number of intercepts, means the vertex is outside
                    if (inside) verticesFromSolid1InsideSolid2.Add(vertex);
                    else verticesFromSolid1OutsideSolid2.Add(vertex);
                }
            }
        }

        #endregion

        #region Distance Methods (between point, line, and plane)

        /// <summary>
        ///     Returns the distance the point to line.
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
        ///     Distances the point to line.
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
                pointOnLine = new[] {lineRefPt[0] + lineVector[0]*t, lineRefPt[1] + lineVector[1]*t};
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
        public static double DistancePointToPoint2D(Point p1, Point p2)
        {
            var dX = p1[0] - p2[0];
            var dY = p1[1] - p2[1];
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
        ///     Returns the signed distance of the point to the plane.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="positionOnPlane">The position on plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(double[] point, double[] normalOfPlane, double[] positionOnPlane)
        {
            return DistancePointToPlane(point, normalOfPlane, positionOnPlane.dotProduct(normalOfPlane));
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
            return normalOfPlane.dotProduct(point) - signedDistanceToPlane;
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
            var d1 = normalOfPlane.dotProduct(point1.Position);
            var d2 = normalOfPlane.dotProduct(point2.Position);
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            var position = new double[3];
            for (var i = 0; i < 3; i++)
            {
                position[i] = point2.Position[i] * fraction + point1.Position[i] * (1 - fraction);
                if (double.IsNaN(position[i]))
                    throw new Exception("This should never occur. Prevent this from happening");
            }
            return new Vertex(position);
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
            var d1 = normalOfPlane[0] * line.ToPoint.X + normalOfPlane[1] * line.ToPoint.Y; //2D Dot product
            var d2 = normalOfPlane[0] * line.FromPoint.X + normalOfPlane[1] * line.FromPoint.Y;  //For a point, Position[2] = 0.0
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            var position2D = new double[2];
            for (var i = 0; i < 2; i++)
            {
                position2D[i] = line.FromPoint.Position[i] * fraction + line.ToPoint.Position[i] * (1 - fraction);
                if (double.IsNaN(position2D[i]))
                    throw new Exception("This should never occur. Prevent this from happening");
            }
            return new Point(position2D[0], position2D[1]);
        }

        /// <summary>
        ///     Finds the point on the plane made by a ray. If that ray is not going to pass through the
        ///     that plane, then null is returned.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="rayPosition">The ray position.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <returns>Vertex.</returns>
        public static double[] PointOnPlaneFromRay(double[] normalOfPlane, double distOfPlane, double[] rayPosition,
            double[] rayDirection)
        {
            var d1 = -DistancePointToPlane(rayDirection, normalOfPlane, distOfPlane);
            var angle = SmallerAngleBetweenEdges(normalOfPlane, rayDirection);
            var d2 = d1 / Math.Cos(angle);
            if (d2 < 0) return null;
            return rayPosition.add(rayDirection.multiply(d2));
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
            var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position);
            signedDistance = -(vertex.Position.dotProduct(face.Normal) - distanceToOrigin) /
                             direction.dotProduct(face.Normal);
            //Note that if t == 0, then it is on the plane
            //else, find the intersection point and determine if it is inside the polygon (face)
            var newPoint = signedDistance.IsNegligible()
                ? vertex
                : new Vertex(vertex.Position.add(direction.multiply(signedDistance)));
            return IsPointInsideTriangle(face, newPoint, onBoundaryIsInside) ? newPoint.Position : null;
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
        public static bool IsPointInsideTriangle(PolygonalFace triangle, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsPointInsideTriangle(triangle.Vertices, vertexInQuestion, onBoundaryIsInside);
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
        public static bool IsPointInsideTriangle(IList<Vertex> vertices, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            if (vertices.Count != 3) throw new Exception("Incorrect number of points in traingle");
            var p = vertexInQuestion.Position;
            var a = vertices[0].Position;
            var b = vertices[1].Position;
            var c = vertices[2].Position;
            return SameSide(p, a, b, c) && SameSide(p, b, a, c) && SameSide(p, c, a, b);
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
            var cp1 = b.subtract(a).crossProduct(p1.subtract(a));
            var cp2 = b.subtract(a).crossProduct(p2.subtract(a));
            var dot = cp1.dotProduct(cp2);
            if (dot.IsNegligible()) return onBoundaryIsInside;
            if (Math.Abs(dot) < 1E-10) return onBoundaryIsInside;
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
                var direction = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() }.normalize();
                foreach (var face in ts.Faces)
                {
                    if (face.Vertices.Any(vertex => vertexInQuestion.X.IsPracticallySame(vertex.X) &&
                                                    vertexInQuestion.Y.IsPracticallySame(vertex.Y) &&
                                                    vertexInQuestion.Z.IsPracticallySame(vertex.Z)))
                    {
                        return onBoundaryIsInside;
                    }

                    var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position);
                    var t = -(vertexInQuestion.Position.dotProduct(face.Normal) - distanceToOrigin) /
                            direction.dotProduct(face.Normal);
                    //Note that if t == 0, then it is on the face
                    //else, find the intersection point and determine if it is inside the polygon (face)
                    var newVertex = t.IsNegligible()
                        ? vertexInQuestion
                        : new Vertex(vertexInQuestion.Position.add(direction.multiply(t)));
                    if (!IsPointInsideTriangle(face, newVertex)) continue;
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
        public static bool IsPolygonInsidePolygon(Polygon outerPolygon, Polygon possibleInnerPolygon)
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
        /// <returns></returns>
        public static bool IsPointInsidePolygon(Polygon polygon, Point p)
        {
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
        public static bool IsPointInsidePolygon(List<Point> path, Point p)
        {
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
    }
    #endregion
}