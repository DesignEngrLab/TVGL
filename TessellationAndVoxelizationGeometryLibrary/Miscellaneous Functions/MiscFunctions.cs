// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="MiscFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Miscellaneous Functions for TVGL
    /// </summary>
    public static partial class MiscFunctions
    {
        public static Vector3 AggregateNormal(this IEnumerable<TriangleFace> faces)
        {
            return faces.Select(f => f.Normal).Aggregate((v1, v2) => v1 + v2).Normalize();
        }

        #region Sort Along Direction

        /// <summary>
        /// Returns a list of sorted locations along a set direction.
        /// </summary>
        /// <param name="vertices">The locations.</param>
        /// <param name="direction">The directions.</param>
        /// <param name="sortedVertices">The sorted locations.</param>
        public static void SortAlongDirection(this IEnumerable<Vertex> vertices,
            Vector3 direction,
            out List<(Vertex, double)> sortedVertices)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into separate lists. 0 is
            //considered positive.
            //This is an O(n) preprocessing step
            var vertexDistances = GetVertexDistances(vertices, direction);

            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //toleranceForCombiningPoints as the "isNegligible" star math function
            sortedVertices = vertexDistances.OrderBy(p => p.Item2).ToList();
        }

        /// <summary>
        /// Returns a list of sorted locations along a set direction.
        /// </summary>
        /// <param name="vertices">The locations.</param>
        /// <param name="direction">The directions.</param>
        /// <param name="sortedVertices">The sorted locations.</param>
        public static void SortAlongDirection(this IEnumerable<Vector3> vertices,
            Vector3 direction,
            out List<(Vector3, double)> sortedVertices)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into separate lists. 0 is
            //considered positive.
            //This is an O(n) preprocessing step
            var vertexDistances = GetVertexDistances(vertices, direction);

            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //toleranceForCombiningPoints as the "isNegligible" star math function
            sortedVertices = vertexDistances.OrderBy(p => p.Item2).ToList();
        }

        /// <summary>
        /// Gets the vertex distances.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;(TVGL.Vertex, double)&gt;.</returns>
        public static IEnumerable<(Vertex, double)> GetVertexDistances(this IEnumerable<Vertex> vertices,
            Vector3 direction,
            double sameTolerance = Constants.BaseTolerance)
        {
            var vertexDistances = new List<(Vertex, double)>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction
                var d = Math.Round(direction.Dot(vertex.Coordinates), numDecimalPoints);
                vertexDistances.Add((vertex, d));
            }
            return vertexDistances;
        }

        /// <summary>
        /// Gets the vertex distances.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;(TVGL.Vector3, double)&gt;.</returns>
        public static IEnumerable<(Vector3, double)> GetVertexDistances(this IEnumerable<Vector3> vertices,
            Vector3 direction,
            double sameTolerance = Constants.BaseTolerance)
        {
            var vertexDistances = new List<(Vector3, double)>();
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction
                var d = Math.Round(direction.Dot(vertex), numDecimalPoints);
                vertexDistances.Add((vertex, d));
            }
            return vertexDistances;
        }

        /// <summary>
        /// Gets the max vertex distance aloing vector.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>A (T maxPoint, double dotDistance) .</returns>
        public static (T maxPoint, double dotDistance) GetMaxVertexDistanceAlongVector<T>(this IEnumerable<T> vertices, Vector3 direction)
            where T : IVector3D
        {
            var dotDistance = double.NegativeInfinity;
            T maxPoint = default;
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction
                var d = direction.X * vertex.X + direction.Y * vertex.Y + direction.Z * vertex.Z;
                if (d > dotDistance)
                {
                    dotDistance = d;
                    maxPoint = vertex;
                }
            }
            return (maxPoint, dotDistance);
        }

        /// <summary>
        /// Gets the min vertex distance along vector.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>A (T minPoint, double dotDistance) .</returns>
        public static (T minPoint, double dotDistance) GetMinVertexDistanceAlongVector<T>(this IEnumerable<T> vertices, Vector3 direction)
            where T : IVector3D
        {
            var dotDistance = double.PositiveInfinity;
            T minPoint = default;
            foreach (var vertex in vertices)
            {
                //Get distance along the search direction
                var d = direction.X * vertex.X + direction.Y * vertex.Y + direction.Z * vertex.Z;
                if (d < dotDistance)
                {
                    dotDistance = d;
                    minPoint = vertex;
                }
            }
            return (minPoint, dotDistance);
        }


        /// <summary>
        /// Returns a list of sorted Vector2s along a set direction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="direction">The directions.</param>
        /// <param name="sortedPoints">The sorted points.</param>
        /// <param name="numDecimals">The number decimals.</param>
        public static void SortAlongDirection(this IList<Vector2> points,
            Vector2 direction,
            out List<(Vector2, double)> sortedPoints, int numDecimals = -1)
        {
            var distances = GetPointDistances(points, direction, numDecimals);
            sortedPoints = distances.OrderBy(p => p.Item2).ToList();
        }

        /// <summary>
        /// Returns a list of sorted Vector2s along a set direction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="direction">The directions.</param>
        /// <param name="sortedPoints">The sorted points.</param>
        /// <param name="numDecimals">The number decimals.</param>
        public static void SortAlongDirection(this IList<Vector2> points,
            Vector2 direction,
            out List<Vector2> sortedPoints, int numDecimals = -1)
        {
            var distances = GetPointDistances(points, direction, numDecimals);
            sortedPoints = distances.OrderBy(p => p.Item2).Select(p => p.Item1).ToList();
        }

        /// <summary>
        /// Gets the point distances.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="numDecimals">The number decimals.</param>
        /// <returns>(TVGL.Vector2, double)[].</returns>
        private static (Vector2, double)[] GetPointDistances(this IList<Vector2> points, Vector2 direction,
            int numDecimals = -1)
        {
            var distances = new (Vector2, double)[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                //Get distance along the search direction with accuracy to the 15th decimal place
                var d = direction.Dot(point);
                if (numDecimals > 0)
                    d = Math.Round(d, numDecimals); //2D dot product
                distances[i] = (point, d);
            }
            return distances;
        }

        /// <summary>
        /// Defines the inner outer edges.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="innerEdgeHash">The inner edge hash.</param>
        /// <param name="outerEdgeHash">The outer edge hash.</param>
        public static void DefineInnerOuterEdges(IEnumerable<TriangleFace> faces, out HashSet<Edge> innerEdgeHash, out HashSet<Edge> outerEdgeHash)
        {
            innerEdgeHash = new HashSet<Edge>();
            outerEdgeHash = new HashSet<Edge>();
            if (faces != null && faces.Any())
                foreach (var face in faces)
                {
                    foreach (var edge in face.Edges)
                    {
                        if (innerEdgeHash.Contains(edge)) continue;
                        if (!outerEdgeHash.Contains(edge)) outerEdgeHash.Add(edge);
                        else
                        {
                            innerEdgeHash.Add(edge);
                            outerEdgeHash.Remove(edge);
                        }
                    }
                }
        }
        #endregion Sort Along Direction

        #region Perimeter

        /// <summary>
        /// Gets the Perimeter (length of a locations) of a 3D set of Vertices.
        /// </summary>
        /// <param name="polygon3D">The polygon3 d.</param>
        /// <returns>double.</returns>
        public static double Perimeter(this IList<Vertex> polygon3D)
        {
            double perimeter = Vector3.Distance(polygon3D.Last().Coordinates, polygon3D[0].Coordinates);
            for (var i = 1; i < polygon3D.Count; i++)
                perimeter += Vector3.Distance(polygon3D[i - 1].Coordinates, polygon3D[i].Coordinates);
            return perimeter;
        }

        public static double PerimeterSquared(this IList<Vector3> polygon3D)
        {
            double perimeter = Vector3.DistanceSquared(polygon3D.Last(), polygon3D[0]);
            for (var i = 1; i < polygon3D.Count; i++)
                perimeter += Vector3.DistanceSquared(polygon3D[i - 1], polygon3D[i]);
            return perimeter;
        }

        #region Length of Polyline

        /// <summary>
        /// Gets the Perimeter (length of a locations) of a 3D set of Vertices.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="isClosed">The is closed.</param>
        /// <returns>double.</returns>
        public static double Length(this IList<Vertex> polyline, bool isClosed = true)
        {
            if (polyline == null || !polyline.Any()) return 0.0;
            double length = isClosed ? Vector3.Distance(polyline.Last().Coordinates, polyline[0].Coordinates) : 0.0;
            for (var i = 1; i < polyline.Count; i++)
                length += Vector3.Distance(polyline[i - 1].Coordinates, polyline[i].Coordinates);
            return length;
        }

        /// <summary>
        /// Gets the summed length of a locations of a 3D set of Vertices.
        /// If (IsClosed), then the perimeter will include the length between
        /// the last vertex and the first vertex.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="isClosed">The is closed.</param>
        /// <returns>double.</returns>
        /// '
        public static double Length(this IList<Vector3> polyline, bool isClosed = true)
        {
            if (polyline == null || !polyline.Any()) return 0.0;
            double length = isClosed ? Vector3.Distance(polyline.Last(), polyline[0]) : 0.0;
            for (var i = 1; i < polyline.Count; i++)
                length += Vector3.Distance(polyline[i - 1], polyline[i]);
            return length;
        }

        #endregion Length of Polyline

        #endregion Perimeter

        #region Dealing with Flat Patches

        /// <summary>
        /// Determines the normal for a 3D vertex polygon.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="numSides">The number sides.</param>
        /// <param name="reverseVertexOrder">if set to <c>true</c> [reverse vertex order].</param>
        /// <param name="suggestedNormal">The suggested normal.</param>
        /// <param name="distanceToPlane">The distance to plane.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 DetermineNormalForA3DPolygon(this IEnumerable<Vertex> vertices, int numSides,
            out bool reverseVertexOrder, Vector3 suggestedNormal, out double distanceToPlane)
        {
            return DetermineNormalForA3DPolygon(vertices.Select(v => v.Coordinates), numSides,
                out reverseVertexOrder, suggestedNormal, out distanceToPlane);
        }

        /// <summary>
        /// Determines the normal for a 3D vertex polygon.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="numSides">The number sides.</param>
        /// <param name="reverseVertexOrder">if set to <c>true</c> [reverse vertex order].</param>
        /// <param name="suggestedNormal">The suggested normal.</param>
        /// <param name="distanceToPlane">The distance to plane.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 DetermineNormalForA3DPolygon(this IEnumerable<Vector3> vertices, int numSides,
            out bool reverseVertexOrder, Vector3 suggestedNormal, out double distanceToPlane)
        {
            var vertexList = vertices as IList<Vector3> ?? vertices.ToList();
            var areaVector = Vector3.Zero;
            for (int i = 2, j = i - 1; i < numSides; j = i++)
                areaVector += (vertexList[j] - vertexList[0]).Cross(vertexList[i] - vertexList[0]);
            reverseVertexOrder = !suggestedNormal.IsNull() && suggestedNormal.Dot(areaVector) < 0;
            // to be more accurate, call another function to best fit a plane through the points
            Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var normal);
            if ((!suggestedNormal.IsNull() && suggestedNormal.Dot(normal) < 0) ||
                (suggestedNormal.IsNull() && areaVector.Dot(normal) < 0))
            {
                normal *= -1;
                distance *= -1;
            }
            if (normal.IsNull() && !suggestedNormal.IsNull())
                normal = suggestedNormal;
            distanceToPlane = distance;
            return normal;
        }

        /// <summary>
        /// Gets a collection of faces with distinct normals. These are the largest faces within the set with common normal.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="tolerance">The toleranceForCombiningPoints.</param>
        /// <param name="removeOpposites">if set to <c>true</c> [remove opposites].</param>
        /// <returns>List&lt;TriangleFace&gt;.</returns>
        public static List<TriangleFace> FacesWithDistinctNormals(this IEnumerable<TriangleFace> faces,
            double tolerance = Constants.DotToleranceForSame, bool removeOpposites = true)
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
                    if (distinctList[i].Normal.IsAligned(distinctList[i - 1].Normal, tolerance) ||
                        (removeOpposites && distinctList[i].Normal.IsAlignedOrReverse(distinctList[i - 1].Normal, tolerance)))
                    {
                        if (distinctList[i].Area <= distinctList[i - 1].Area) distinctList.RemoveAt(i);
                        else distinctList.RemoveAt(i - 1);
                    }
                }
            }
            return distinctList;
        }

        /// <summary>
        /// Gets a list of flats for a given list of faces.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="minNumberOfFacesPerFlat">The minimum number of faces per flat.</param>
        /// <param name="minFlatArea">The minimum flat area.</param>
        /// <param name="nonCrossingEdges">The non crossing edges.</param>
        /// <returns>List&lt;Flat&gt;.</returns>
        public static IEnumerable<Plane> FindFlats(this IEnumerable<TriangleFace> faces,
               int minNumberOfFacesPerFlat = 2, double minFlatArea = 0.0, HashSet<Edge> nonCrossingEdges = null)
        {
            // to avoid re-enumerating the faces - make a list. If it's already a list, then you're fine to use directly.
            var availableFaces = new HashSet<TriangleFace>(faces);
            if (nonCrossingEdges == null) nonCrossingEdges = new HashSet<Edge>();
            while (availableFaces.Count > 0)
            {
                var startFace = availableFaces.First();
                availableFaces.Remove(startFace);
                var flatHashSet = new HashSet<TriangleFace> { startFace };
                var stack = new Stack<TriangleFace>(new[] { startFace });
                var flatVertices = new List<Vertex>(startFace.Vertices);
                var area = 0.0;
                var numFaces = 0;
                while (stack.Any())
                {
                    var newFace = stack.Pop();
                    //Add new adjacent faces to the stack for consideration
                    //if the faces are already listed in the flat faces, the first
                    //"if" statement in the while locations will ignore them.
                    foreach (var edge in newFace.Edges)
                    {
                        if (nonCrossingEdges.Contains(edge)) continue;
                        var adjacentFace = edge.GetMatingFace(newFace);
                        if (adjacentFace == null || !availableFaces.Contains(adjacentFace)) continue;
                        if (!newFace.Normal.IsAligned(adjacentFace.Normal)) continue;
                        var otherVertex = adjacentFace.A != edge.From && adjacentFace.A != edge.To ? adjacentFace.A :
                            adjacentFace.B != edge.From && adjacentFace.B != edge.To ? adjacentFace.B :
                            adjacentFace.C;
                        flatVertices.Add(otherVertex);
                        if (!Plane.DefineNormalAndDistanceFromVertices(flatVertices, out double distanceToPlane, out Vector3 normal)
                           || !distanceToPlane.IsPracticallySame(otherVertex.Dot(normal), Constants.PolygonSameTolerance))
                            flatVertices.RemoveAt(flatVertices.Count - 1);
                        else
                        {
                            stack.Push(adjacentFace);
                            availableFaces.Remove(adjacentFace);
                            flatHashSet.Add(adjacentFace);
                            area += adjacentFace.Area;
                            numFaces++;
                        }
                    }
                }
                if (numFaces >= minNumberOfFacesPerFlat && area >= minFlatArea)
                    yield return new Plane(flatHashSet);
            }
        }

        #endregion Dealing with Flat Patches

        #region Area of 3D Polygon

        /// <summary>
        /// Calculate the area of any non-intersecting polygon in 3D space (loops)
        /// This is faster than projecting to a 2D surface first in a seperate function.
        /// </summary>
        /// <param name="loop">The loop.</param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double.</returns>
        /// <references>http://geomalgorithms.com/a01-_area.html </references>
        public static double AreaOf3DPolygon(this IEnumerable<Vertex> loop, Vector3 normal)
        {
            var ax = Math.Abs(normal.X);
            var ay = Math.Abs(normal.Y);
            var az = Math.Abs(normal.Z);

            //Make a new list from the locations
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
        /// Calculate the area of any non-intersecting polygon in 3D space (loops)
        /// This is faster than projecting to a 2D surface first in a seperate function.
        /// </summary>
        /// <param name="loop">The loop.</param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double.</returns>
        /// <references>http://geomalgorithms.com/a01-_area.html </references>
        public static double AreaOf3DPolygon(this IEnumerable<Vector3> loop, Vector3 normal)
        {
            var ax = Math.Abs(normal.X);
            var ay = Math.Abs(normal.Y);
            var az = Math.Abs(normal.Z);

            //Make a new list from the locations
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

        #endregion Area of 3D Polygon

        #region Get Vertices from Objects
        /// <summary>
        /// This function gets the vertices from a list of faces.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>System.Collections.Generic.HashSet&lt;TVGL.Vertex&gt;.</returns>
        public static HashSet<Vertex> GetVertices(this List<TriangleFace> faces)
        {
            //Add the face vertices from each vertex to the hashset.
            //Duplicates will automatically be avoided by useing a hash.
            var vertices = new HashSet<Vertex>();
            foreach (var face in faces)
            {
                //use a foreach loop instead of face.A, B, C, since those references
                //get an item in the array instead of just enumerating over the array.
                foreach (var vertex in face.Vertices)
                {
                    vertices.Add(vertex);
                }
            }
            return vertices;
        }

        /// <summary>
        /// This function gets the vertices from a list of edges.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <returns>System.Collections.Generic.HashSet&lt;TVGL.Vertex&gt;.</returns>
        public static HashSet<Vertex> GetVertices(this List<Edge> edges)
        {
            //Add the to and from vertices from each vertex to the hashset.
            //Duplicates will automatically be avoided by useing a hash.
            var vertices = new HashSet<Vertex>();
            foreach (var edge in edges)
            {
                vertices.Add(edge.From);
                vertices.Add(edge.To);
            }
            return vertices;
        }

        #endregion Get Vertices from Objects

        #region Split Tesselated Solid into multiple solids if faces are disconnected

        /// <summary>
        /// Gets all the individual solids from a tesselated solid.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="faceGroupsThatAreBodies">The face groups that are bodies.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        /// <exception cref="Exception"></exception>
        public static IEnumerable<TessellatedSolid> GetMultipleSolids(this TessellatedSolid ts,
            List<int[]> faceGroupsThatAreBodies = null)
        {
            List<List<TriangleFace>> faceGroups;
            HashSet<TriangleFace> unusedFaces;
            if (faceGroupsThatAreBodies != null)
            {
                faceGroups = GetContiguousFaceGroups(ts, faceGroupsThatAreBodies, out unusedFaces);
            }
            else
            {
                faceGroups = new List<List<TriangleFace>>();
                unusedFaces = new HashSet<TriangleFace>(ts.Faces);
            }
            // now, the bigger job of walking through the faces to find groups
            faceGroups.AddRange(GetContiguousFaceGroups(unusedFaces));

            if (faceGroups.Count == 1 && faceGroups[0].Count == ts.NumberOfFaces)
            {
                yield return ts;
                yield break;
            }
            foreach (var faceGroup in faceGroups)
            {
                var newSolid = new TessellatedSolid(faceGroup, null, new TessellatedSolidBuildOptions
                {
                    FindNonsmoothEdges = false,
                    CopyElementsPassedToConstructor = false,
                    AutomaticallyRepairHoles = false,
                    FixEdgeDisassociations = false
                });
                if (ts.NonsmoothEdges != null && ts.NonsmoothEdges.Count > 0)
                {
                    //Get all the edges in order, avoiding duplicates by using a hashset.
                    var edges = new HashSet<Edge>();
                    foreach (var face in faceGroup)
                        foreach (var edge in face.Edges)
                            edges.Add(edge);

                    var nonSmoothVectorPairsInOriginial = new HashSet<(Vector3, Vector3)>();
                    var nonSmoothEdges = new List<Edge>();
                    foreach (var edge in ts.NonsmoothEdges)
                        if (edges.Contains(edge))
                            nonSmoothVectorPairsInOriginial.Add((edge.From.Coordinates, edge.To.Coordinates));

                    foreach (var edge in newSolid.Edges)
                    {
                        if (nonSmoothVectorPairsInOriginial.Contains((edge.From.Coordinates, edge.To.Coordinates))
                            || nonSmoothVectorPairsInOriginial.Contains((edge.To.Coordinates, edge.From.Coordinates)))
                            nonSmoothEdges.Add(edge);
                    }
                    if (nonSmoothEdges.Count > 0)
                        newSolid.NonsmoothEdges = nonSmoothEdges;
                }
                if (ts.Primitives != null && ts.Primitives.Count > 0)
                {
                    foreach (var primitive in ts.Primitives)
                    {
                        if (faceGroup.Contains(primitive.Faces.First()))
                        {
                            var faceIndices = primitive.Faces.Select(f => faceGroup.IndexOf(f)).ToList();
                            var primCopy = primitive.Copy(faceIndices.Select(idx => newSolid.Faces[idx]));
                            newSolid.Primitives.Add(primitive); // does it need to be copied?
                        }
                    }
                }
                yield return newSolid;
            }
        }

        public static List<List<TriangleFace>> GetContiguousFaceGroups(this IEnumerable<TriangleFace> facesInput)
        {
            var unusedFaces = facesInput as HashSet<TriangleFace> ?? facesInput.ToHashSet();
            var faceGroups = new List<List<TriangleFace>>();
            while (unusedFaces.Any())
            {
                var groupHash = new HashSet<TriangleFace>();
                var stack = new Stack<TriangleFace>(new[] { unusedFaces.First() });
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (groupHash.Contains(face)) continue;
                    groupHash.Add(face);
                    unusedFaces.Remove(face);
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!unusedFaces.Contains(adjacentFace)) continue; //Cannot assign the same face twice
                        stack.Push(adjacentFace);
                    }
                }
                faceGroups.Add(groupHash.ToList());
            }
            return faceGroups;
        }

        public static List<List<TriangleFace>> GetContiguousFaceGroups(TessellatedSolid ts, List<int[]> faceGroupsThatAreBodies,
            out HashSet<TriangleFace> unusedFaces)
        {
            var faceGroups = new List<List<TriangleFace>>();
            var unusedFacesDictionary = ts.Faces.ToHashSet();    //ToDictionary(face => face.IndexInList);
                                                                 // first the easy part - simply separate out known groups that have already been determined to be bodies
            if (faceGroupsThatAreBodies != null)
            {
                foreach (var bodyGroupIndices in faceGroupsThatAreBodies)
                {
                    var faceList = new List<TriangleFace>();
                    foreach (var index in bodyGroupIndices)
                    {
                        var face = ts.Faces[index];
                        faceList.Add(face);
                        unusedFacesDictionary.Remove(face);
                    }
                    faceGroups.Add(faceList);
                }
            }
            unusedFaces = unusedFacesDictionary;
            return faceGroups;
        }

        #endregion Split Tesselated Solid into multiple solids if faces are disconnected

        #region change 3D locations into 2D coordinates (e.g. Vector2's)

        /// <summary>
        /// Returns newly created 2D coordinates (of type Vector2) projected using the given transform.
        /// These coordinates do not contain references back to the original vertices but are lighter and
        /// quicker. This does not destructively alter the vertices.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="toleranceForCombiningPoints">The tolerance for combining multiple vertices under a single point.
        /// If not, provided, then one point will be made for each vertex. If zero, then the coordinates will match at
        /// the 15 decimal place. Use a small positive number like 1e-9 to set a wider toleranceForCombiningPoints.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector2&gt;.</returns>
        public static Dictionary<Vector2, List<T>> ProjectTo2DCoordinatesReturnDictionary<T>(this IEnumerable<T> vertices, Vector3 direction,
                    out Matrix4x4 backTransform, double toleranceForCombiningPoints = Constants.BaseTolerance) where T : IVector3D
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return ProjectTo2DCoordinatesReturnDictionary(vertices, transform, toleranceForCombiningPoints);
        }

        /// <summary>
        /// Returns newly created 2D coordinates (of type Vector2) projected using the given transform.
        /// These coordinates do not contain references back to the original vertices but are lighter and
        /// quicker. This does not destructively alter the vertices.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vertices">The vertices.</param>
        /// <param name="transform">The transform matrix.</param>
        /// <param name="toleranceForCombiningPoints">The tolerance for combining multiple vertices under a single point.
        /// If not, provided, then one point will be made for each vertex. If zero, then the coordinates will match at
        /// the 15 decimal place. Use a small positive number like 1e-9 to set a wider toleranceForCombiningPoints.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector2&gt;.</returns>
        public static Dictionary<Vector2, List<T>> ProjectTo2DCoordinatesReturnDictionary<T>(this IEnumerable<T> vertices, Matrix4x4 transform,
            double toleranceForCombiningPoints = Constants.BaseTolerance) where T : IVector3D
        {
            var resultsDict = new Dictionary<Vector2, List<T>>();
            var numDecimalPoints = 0;
            while (numDecimalPoints <= 15 && Math.Round(toleranceForCombiningPoints, numDecimalPoints).IsPracticallySame(0.0))
                numDecimalPoints++;
            foreach (var vertex in vertices)
            {
                var coordinates = ConvertTo2DCoordinates(vertex, transform);
                coordinates = new Vector2(Math.Round(coordinates.X, numDecimalPoints), Math.Round(coordinates.Y, numDecimalPoints));
                if (resultsDict.TryGetValue(coordinates, out var list))
                    list.Add(vertex);
                else
                    /* else, add a new vertex to the list, and a new entry to simpleCompareDict.  */
                    resultsDict.Add(coordinates, new List<T> { vertex });
            }
            return resultsDict;
        }

        /// <summary>
        /// Returns newly created 2D coordinates (of type Vector2) projected using the given transform.
        /// These coordinates do not contain references back to the original locations but are lighter and
        /// quicker.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations">The locations.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="toleranceForCombiningPoints">The tolerance for combining multiple locations under a single point.
        /// If not, provided, then one point will be made for each vertex. If zero, then the coordinates will match at
        /// the 15 decimal place. Use a small positive number like 1e-9 to set a wider toleranceForCombiningPoints.</param>
        /// <param name="duplicateEntriesToMaintainPolygonalOrdering">Output is in the same order as input except when
        /// they are combined from the aforementioned tolerance. If this boolean is true then the output point may appear
        /// multiple times in the output collection to maintain the same order. This is useful if the original data is
        /// to define some polygon with order dictating the definition of edges.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector2&gt;.</returns>
        public static IEnumerable<Vector2> ProjectTo2DCoordinates<T>(this IEnumerable<T> locations, Vector3 direction,
                    out Matrix4x4 backTransform, double toleranceForCombiningPoints = double.NaN, bool duplicateEntriesToMaintainPolygonalOrdering = false)
            where T : IVector3D
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return ProjectTo2DCoordinates(locations, transform, toleranceForCombiningPoints, duplicateEntriesToMaintainPolygonalOrdering);
        }

        /// <summary>
        /// Returns newly created 2D coordinates (of type Vector2) projected using the given transform.
        /// These coordinates do not contain references back to the original locations but are lighter and
        /// quicker. This does not destructively alter the locations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations">The locations.</param>
        /// <param name="transform">The transform matrix.</param>
        /// <param name="toleranceForCombiningPoints">The tolerance for combining multiple locations under a single point.
        /// If not provided or less than zero, then one point will be made for each vertex. If zero to 1e-15, then the coordinates
        /// will match at the 15 decimal place. Use a small positive number like 1e-9 to set a wider toleranceForCombiningPoints.</param>
        /// <param name="duplicateEntriesToMaintainPolygonalOrdering">Output is in the same order as input except when
        /// they are combined from the aforementioned tolerance. If this boolean is true then the output point may appear
        /// multiple times in the output collection to maintain the same order. This is useful if the original data is
        /// to define some polygon with order dictating the definition of edges.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector2&gt;.</returns>
        public static IEnumerable<Vector2> ProjectTo2DCoordinates<T>(this IEnumerable<T> locations, Matrix4x4 transform,
            double toleranceForCombiningPoints = double.NaN, bool duplicateEntriesToMaintainPolygonalOrdering = false) where T : IVector3D
        {
            if (double.IsNaN(toleranceForCombiningPoints) || toleranceForCombiningPoints < 0.0)
            {
                foreach (var location in locations)
                    yield return ConvertTo2DCoordinates(location, transform);
            }
            else
            {
                var numDecimalPoints = 0;
                var simpleCompareDict = new HashSet<Vector2>();
                while (numDecimalPoints < 15 && Math.Round(toleranceForCombiningPoints, numDecimalPoints).IsPracticallySame(0.0))
                    numDecimalPoints++;
                foreach (var location in locations)
                {
                    var coordinates = ConvertTo2DCoordinates(location, transform);
                    coordinates = new Vector2(Math.Round(coordinates.X, numDecimalPoints), Math.Round(coordinates.Y, numDecimalPoints));
                    if (simpleCompareDict.Contains(coordinates))
                    {
                        if (duplicateEntriesToMaintainPolygonalOrdering)
                            yield return coordinates;
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict.  */
                        simpleCompareDict.Add(coordinates);
                        yield return coordinates;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the 3D location (e.g. Vector3) to 2D coordinate (e.g. Vector2).
        /// </summary>
        /// <param name="location3D">The location3 d.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <returns>Vector2.</returns>
        public static Vector2 ConvertTo2DCoordinates(this IVector3D location3D, in Vector3 direction, out Matrix4x4 backTransform)
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return ConvertTo2DCoordinates(location3D, transform);
        }

        /// <summary>
        /// Converts the 3D location (e.g. Vector3) to 2D coordinate (e.g. Vector2).
        /// </summary>
        /// <param name="location3D">The location as a Vector3.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>TVGL.Vector2.</returns>
        public static Vector2 ConvertTo2DCoordinates(this IVector3D location3D, in Matrix4x4 matrix)
        {
            var x3D = location3D.X;
            var y3D = location3D.Y;
            var z3D = location3D.Z;

            var x2D = x3D * matrix.M11 + y3D * matrix.M21 + z3D * matrix.M31 + matrix.M41;
            var y2D = x3D * matrix.M12 + y3D * matrix.M22 + z3D * matrix.M32 + matrix.M42;
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (x3D * matrix.M14 + y3D * matrix.M24 + z3D * matrix.M34 + matrix.M44);
                x2D *= factor;
                y2D *= factor;
            }
            return new Vector2(x2D, y2D);
        }

        #endregion change 3D locations into 2D coordinates (e.g. Vector2's)

        #region change 2D coordinates (e.g. Vector2's) into 3D locations (e.g. Vector3's)

        /// <summary>
        /// Converts the 2D coordinates into 3D locations in a plane defined by normal direction and distance.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="normalDirection">The normal direction of the new plane.</param>
        /// <param name="distanceAlongDirection">The distance of the plane from the origin.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector3&gt;.</returns>
        public static IEnumerable<Vector3> ConvertTo3DLocations(this IEnumerable<Vector2> coordinates, Vector3 normalDirection,
                    double distanceAlongDirection)
        {
            TransformToXYPlane(normalDirection, out var backTransform);
            var transform = backTransform * Matrix4x4.CreateTranslation(normalDirection * distanceAlongDirection);
            return ConvertTo3DLocations(coordinates, transform);
        }

        /// <summary>
        /// Converts the 2D coordinates into 3D locations in a plane defined by normal direction and distance.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="transform">The transform matrix.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector3&gt;.</returns>
        public static IEnumerable<Vector3> ConvertTo3DLocations(this IEnumerable<Vector2> coordinates, Matrix4x4 transform)
        {
            foreach (var point2D in coordinates)
                yield return Vector3.Multiply(new Vector3(point2D, 0), transform);
        }

        /// <summary>
        /// Converts the 2D coordinates into 3D locations in a plane defined by normal direction and distance.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="normalDirection">The normal direction of the new plane.</param>
        /// <param name="distanceAlongDirection">The distance of the plane from the origin.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector3&gt;.</returns>
        public static IEnumerable<Vector3> ConvertTo3DLocations(this Polygon coordinates, Vector3 normalDirection,
            double distanceAlongDirection)
        {
            TransformToXYPlane(normalDirection, out var backTransform);
            var transform = backTransform * Matrix4x4.CreateTranslation(normalDirection * distanceAlongDirection);
            return ConvertTo3DLocations(coordinates, transform);
        }

        /// <summary>
        /// Converts the 2D coordinates into 3D locations in a plane defined by normal direction and distance.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="transform">The transform matrix.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector3&gt;.</returns>
        public static IEnumerable<Vector3> ConvertTo3DLocations(this Polygon polygon, Matrix4x4 transform)
        {
            foreach (var point2D in polygon.Path)
                yield return Vector3.Multiply(new Vector3(point2D, 0), transform);
        }

        /// <summary>
        /// Converts the 3D location (e.g. Vector3) to 2D coordinate (e.g. Vector2).
        /// </summary>
        /// <param name="coordinates2D">The coordinates2 d.</param>
        /// <param name="transform">The transform matrix.</param>
        /// <returns>TVGL.Vector2.</returns>
        public static Vector3 ConvertTo3DLocation(this in Vector2 coordinates2D, in Matrix4x4 transform)
        {
            return Vector3.Multiply(new Vector3(coordinates2D, 0), transform);
        }

        #endregion change 2D coordinates (e.g. Vector2's) into 3D locations (e.g. Vector3's)

        #region Transform 3D-to-2D and vice versa
        /// <summary>
        /// Create a transforms from normal direction for 2D xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="tolerance">This tolerance is used to snap to the cartesian direction if the dot product is within this value.</param>
        /// <returns>System.Vector2.</returns>
        public static Matrix4x4 TransformToXYPlane(this Vector3 direction, out Matrix4x4 backTransform, double tolerance = Constants.BaseTolerance)
        {
            var closestCartesianDirection = SnapDirectionToCartesian(direction, out var withinTolerance, tolerance);
            if (withinTolerance)
                return TransformToXYPlane(closestCartesianDirection, out backTransform);

            var zDir = direction.Normalize();
            var xDir = zDir.GetPerpendicularDirection();
            var yDir = zDir.Cross(xDir);
            backTransform = new Matrix4x4(xDir, yDir, zDir, Vector3.Zero);
            Matrix4x4.Invert(backTransform, out var forwardTransform);
            return forwardTransform;
        }

        /// <summary>
        /// Create a transforms from normal direction for 2D xy plane.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <returns>System.Vector2.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Matrix4x4 TransformToXYPlane(this CartesianDirections direction, out Matrix4x4 backTransform)
        {
            switch (direction)
            {
                case CartesianDirections.XNegative:
                    backTransform = new Matrix4x4(0, 0, 1, 0, 1, 0, -1, 0, 0, 0, 0, 0);
                    return new Matrix4x4(0, 0, -1, 0, 1, 0, 1, 0, 0, 0, 0, 0);
                case CartesianDirections.YNegative:
                    backTransform = new Matrix4x4(1, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0);
                    return new Matrix4x4(1, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0);
                case CartesianDirections.ZNegative:
                    backTransform = new Matrix4x4(1, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0);
                    return backTransform;
                case CartesianDirections.XPositive:
                    backTransform = new Matrix4x4(0, 0, -1, 0, 1, 0, 1, 0, 0, 0, 0, 0);
                    return new Matrix4x4(0, 0, 1, 0, 1, 0, -1, 0, 0, 0, 0, 0);
                case CartesianDirections.YPositive:
                    backTransform = new Matrix4x4(1, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0);
                    return new Matrix4x4(1, 0, 0, 0, 0, 1, 0, -1, 0, 0, 0, 0);
                case CartesianDirections.ZPositive:
                    backTransform = Matrix4x4.Identity;
                    return Matrix4x4.Identity;
            }
            throw new InvalidOperationException();
        }
        #endregion


        #region Angle between Edges/Lines

        /// <summary>
        /// Gets the larger angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double LargerAngleBetweenVectorsEndToEnd(this Vector2 vector1, Vector2 vector2)
        {
            var angleCos = vector1.Dot(vector2) / (vector1.Length() * vector2.Length());
            if (angleCos >= 1) return Math.PI;
            if (angleCos <= -1) return Constants.TwoPi;
            return Math.PI + Math.Acos(angleCos);
        }

        /// <summary>
        /// Gets the smaller angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double SmallerAngleBetweenVectorsEndToEnd(this Vector2 vector1, Vector2 vector2)
        {
            var angleCos = vector1.Dot(vector2) / (vector1.Length() * vector2.Length());
            if (angleCos >= 1) return Math.PI;
            if (angleCos <= -1) return 0;
            return Math.PI - Math.Acos(angleCos);
            //var angle = Math.Atan2(vector1.Cross(vector2), vector1.Dot(vector2));
            //if (angle >= 0) return angle;
            //return Math.PI + angle;
        }

        /// <summary>
        /// Gets the counter-clockwise rotated angle of vector from the x-axis
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCCWFromHorizontal(this Vector2 vector)
        {
            var angle = Math.Atan2(vector.Y, vector.X);
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }

        /// <summary>
        /// Gets the clockwise rotated angle of vector from the x-axis
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCWFromHorizontal(this Vector2 vector)
        {
            var angle = Math.Atan2(-vector.Y, vector.X);
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }

        /// <summary>
        /// Gets the counter-clockwise rotated angle of vector from the x-axis
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCCWFromVertical(this Vector2 vector)
        {
            var angle = Math.Atan2(vector.Y, vector.X) - Constants.HalfPi;
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }

        /// <summary>
        /// Gets the clockwise rotated angle of vector from the x-axis
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCWFromVertical(this Vector2 vector)
        {
            var angle = Math.Atan2(-vector.Y, vector.X) - Constants.HalfPi;
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }

        /// <summary>
        /// Gets the counter-clockwise rotated angle of vector-A from the datum vector
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCCWBetweenVectorAAndDatum(this Vector2 vectorA, Vector2 datum)
        {
            var angle = Math.Atan2(datum.Cross(vectorA), datum.Dot(vectorA));
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }
        /// <summary>
        /// Gets the counter-clockwise rotated angle of vector-A from the datum vector
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <returns>double.</returns>
        public static double AngleCWBetweenVectorAAndDatum(this Vector2 vectorA, Vector2 datum)
        {
            var angle = -Math.Atan2(datum.Cross(vectorA), datum.Dot(vectorA));
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }


        /// <summary>
        /// Gets the larger angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double LargerAngleBetweenVectorsEndToEnd(this Vector3 vector1, Vector3 vector2)
        {
            return Math.PI + SmallerAngleBetweenVectorsSameStart(vector1, vector2);
        }

        /// <summary>
        /// Gets the smaller angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double SmallerAngleBetweenVectorsEndToEnd(this Vector3 vector1, Vector3 vector2)
        {
            return Math.PI - SmallerAngleBetweenVectorsSameStart(vector1, vector2);
        }

        /// <summary>
        /// Gets the larger angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double LargerAngleBetweenVectorsSameStart(this Vector3 vector1, Vector3 vector2)
        {
            return Math.Tau - SmallerAngleBetweenVectorsSameStart(vector1, vector2);
        }

        /// <summary>
        /// Gets the smaller angle between two vectors, assuming vector2 starts that the head of
        /// vector1. The vectors do not need to be normalized.
        /// </summary>
        /// <param name="vector1">The v0.</param>
        /// <param name="vector2">The v1.</param>
        /// <returns>System.Double.</returns>
        public static double SmallerAngleBetweenVectorsSameStart(this Vector3 vector1, Vector3 vector2)
        {
            var dot = Math.Min(vector1.Dot(vector2) / (vector1.Length() * vector2.Length()), 1);
            return Math.Acos(dot);
        }

        /// <summary>
        /// Returns the angle from 0 to 2 pi of vector-A from the datum, where the unit plane normal is as if the 
        /// surface was facing you.
        /// </summary>
        /// <param name="vectorA">The vector a.</param>
        /// <param name="datum">The datum.</param>
        /// <param name="unitPlaneNormal">The unit plane normal.</param>
        /// <returns>double.</returns>
        public static double AngleCCWBetweenVectorAndDatum(this Vector3 vectorA, Vector3 datum, Vector3 unitPlaneNormal)
        {
            var angle = Math.Atan2(datum.Cross(vectorA).Dot(unitPlaneNormal), datum.Dot(vectorA));
            if (angle >= 0) return angle;
            return Constants.TwoPi + angle;
        }


        #endregion Angle between Edges/Lines

        #region Intersection Method (between lines, planes, solids, etc.)

        /// <summary>
        /// Determines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are collinear, they are not considered intersecting.
        /// This method has been renamed ...Conventional because the Projective Geometric Algebra version (see below)
        /// proves to be faster. Using BenchmarkDotNet, this version is 23.75 ns while the one below is 15 ns.
        /// </summary>
        /// <param name="aFrom">The starting point on the a-Line.</param>
        /// <param name="aTo">The end point on the a-Line.</param>
        /// <param name="bFrom">The starting point on the b-Line.</param>
        /// <param name="bTo">The end point on the b-Line.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="t_a">The t a.</param>
        /// <param name="t_b">The t b.</param>
        /// <param name="considerCollinearOverlapAsIntersect">The consider collinear overlap as intersect.</param>
        /// <returns>System.Boolean.</returns>

        public static bool SegmentSegment2DIntersectionConventional(Vector2 aFrom, Vector2 aTo, Vector2 bFrom, Vector2 bTo,
            out Vector2 intersectionPoint, out double t_a, out double t_b, bool considerCollinearOverlapAsIntersect = false)
        {
            t_a = double.NaN;
            t_b = double.NaN;
            intersectionPoint = Vector2.Null;
            var aVector = aTo - aFrom; //vector along p-line
            var bVector = bTo - bFrom; //vector along q-line
            var vCross = aVector.Cross(bVector); //2D cross product, determines if parallel
            var fromPointVector = bFrom - aFrom; // the vector connecting starts

            if (vCross.IsNegligible(Constants.BaseTolerance))
            {
                // if this is also parallel with the vector direction then there is overlap
                // (since bounding boxes overlap). But we cannot set intersectionPoint
                // to a single value since it is infinite points!
                if (fromPointVector.Cross(aVector).IsNegligible(Constants.BaseTolerance))
                    return considerCollinearOverlapAsIntersect;
                return false;
            }
            // solve for the t scalar values for the two lines.
            // the line is define as all values of t from 0 to 1 in the equations
            // p-line(t_p) = (1 - t_p)*p1 + t_p*p2
            // q-line(t_q) = (1 - t_q)*q1 + t_q*q2
            // solve as a system of two equations
            //   |   vp_x      vq_x   | |  t_p  |    | vStarts_x  |
            //   |                    |*|       | =  |            |
            //   |   vp_y      vq_y   | |  t_q  |    | vStarts_y  |
            var oneOverdeterminnant = 1 / vCross;
            t_a = oneOverdeterminnant * (bVector.Y * fromPointVector.X - bVector.X * fromPointVector.Y);
            if (t_a < 0 || t_a > 1)
                //if (t_1.IsLessThanNonNegligible(0, Constants.PolygonSameTolerance)
                //    || !t_1.IsLessThanNonNegligible(1.0, Constants.PolygonSameTolerance))
                return false;
            t_b = oneOverdeterminnant * (aVector.Y * fromPointVector.X - aVector.X * fromPointVector.Y);
            if (t_b < 0 || t_b >= 1)
                return false;

            intersectionPoint = new Vector2(0.5 * (aFrom.X + t_a * aVector.X + bFrom.X + t_b * bVector.X),
                0.5 * (aFrom.Y + t_a * aVector.Y + bFrom.Y + t_b * bVector.Y));
            return true;
        }


        /// <summary>
        /// Determines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are collinear, they are not considered intersecting.
        /// </summary>
        /// <param name="aFrom">The starting point on the a-Line.</param>
        /// <param name="aTo">The end point on the a-Line.</param>
        /// <param name="bFrom">The starting point on the b-Line.</param>
        /// <param name="bTo">The end point on the b-Line.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="t_a">The fractional amount along line-a.</param>
        /// <param name="t_b">The fractional amount along line-b.</param>
        /// <returns>System.Boolean.</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SegmentSegment2DIntersection(Vector2 aFrom, Vector2 aTo, Vector2 bFrom, Vector2 bTo,
            out Vector2 intersectionPoint, out double t_a, out double t_b)
        {
            // first create descriptions of the lines using the Join operator from Project Geometric Algebra (PGA2D; https://bivector.net/2DPGA.pdf)
            var aLine = new Vector3(aTo.Y - aFrom.Y, aFrom.X - aTo.X, aFrom.Y * aTo.X - aFrom.X * aTo.Y); //JoinPointsIntoLineDescriptor(aFrom, aTo);
            var bLine = new Vector3(bTo.Y - bFrom.Y, bFrom.X - bTo.X, bFrom.Y * bTo.X - bFrom.X * bTo.Y); // JoinPointsIntoLineDescriptor(bFrom, bTo);

            // now use the Meet operator to find the intersection point in homogeneous coordinates.
            var interPoint3 = new Vector3(bLine.Z * aLine.Y - bLine.Y * aLine.Z, bLine.X * aLine.Z - bLine.Z * aLine.X, bLine.Y * aLine.X - bLine.X * aLine.Y); // MeetAtProjective2DPointUA(lg1, lg2);

            // there are several ways to check whether to intersection point is in between the endpoints (see https://observablehq.com/@skydog23/point-in-segment)
            // but since one may also want to know the fraction along the line as indicated by the return values t_a and t_b, we define one
            // based on dot products with the sub line to to the total line
            var aLineIPFrom = new Vector3(interPoint3.Y - aFrom.Y * interPoint3.Z, aFrom.X * interPoint3.Z - interPoint3.X, aFrom.Y * interPoint3.X - aFrom.X * interPoint3.Y); // JoinPointsIntoLineDescriptor(aFrom, ip);
            var aLineIPTo = new Vector3(interPoint3.Y - aTo.Y * interPoint3.Z, aTo.X * interPoint3.Z - interPoint3.X, aTo.Y * interPoint3.X - aTo.X * interPoint3.Y); // JoinPointsIntoLineDescriptor(aTo, ip);
            var dotFromA = aLineIPFrom.X * aLine.X + aLineIPFrom.Y * aLine.Y;  // PGAInnerProductUA(iVfg1, lg1);
            var dotToA = -aLineIPTo.X * aLine.X - aLineIPTo.Y * aLine.Y; // -PGAInnerProductUA(iVtg1, lg1);

            // note that the dotToA is actually a negative of the dot product. if both are positive or both are negative then you are in between
            // the end points
            if ((dotFromA < 0 && dotToA >= 0) || (dotFromA >= 0 && dotToA < 0))
            {
                t_a = double.NaN;
                t_b = double.NaN;
                intersectionPoint = Vector2.Null;
                return false;
            }

            // now the same for the b-Line
            var blineIPFrom = new Vector3(interPoint3.Y - bFrom.Y * interPoint3.Z, bFrom.X * interPoint3.Z - interPoint3.X, bFrom.Y * interPoint3.X - bFrom.X * interPoint3.Y); // JoinPointsIntoLineDescriptor(bFrom, ip);
            var blineIPTo = new Vector3(interPoint3.Y - bTo.Y * interPoint3.Z, bTo.X * interPoint3.Z - interPoint3.X, bTo.Y * interPoint3.X - bTo.X * interPoint3.Y); // JoinPointsIntoLineDescriptor(bTo, ip);
            var dotFromB = blineIPFrom.X * bLine.X + blineIPFrom.Y * bLine.Y; // PGAInnerProductUA(iVfg2, lg2);
            var dotToB = -blineIPTo.X * bLine.X - blineIPTo.Y * bLine.Y; // -PGAInnerProductUA(iVtg2, lg2);
            if ((dotFromB < 0 && dotToB >= 0) || (dotFromB >= 0 && dotToB < 0))
            {
                t_a = double.NaN;
                t_b = double.NaN;
                intersectionPoint = Vector2.Null;
                return false;
            }
            t_a = dotFromA / (dotFromA + dotToA);
            t_b = dotFromB / (dotFromB + dotToB);
            intersectionPoint = new Vector2(interPoint3.X / interPoint3.Z, interPoint3.Y / interPoint3.Z);
            return true;
        }




        /// <summary>
        /// Determines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are collinear, they are not considered intersecting.
        /// </summary>
        /// <param name="aFrom">The starting point on the a-Line.</param>
        /// <param name="aTo">The end point on the a-Line.</param>
        /// <param name="bAnchor">Some known point on b-line.</param>
        /// <param name="bDirection">The direction of the b-line.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="considerCollinearOverlapAsIntersect">The consider collinear overlap as intersect.</param>
        /// <returns>System.Boolean.</returns>
        public static bool SegmentLine2DIntersection(Vector2 aFrom, Vector2 aTo, Vector2 bAnchor, Vector2 bDirection,
            out Vector2 intersectionPoint, bool considerCollinearOverlapAsIntersect = false)
        {
            intersectionPoint = Vector2.Null;
            // okay, so bounding boxes overlap
            //first a quick check to see if points are the same
            if (aFrom.IsAligned(bAnchor))
            {
                intersectionPoint = aFrom;
                return true;
            }
            if (aTo.IsAligned(bAnchor))
            {
                intersectionPoint = aTo;
                return true;
            }

            var aVector = aTo - aFrom; //vector along p-line
            var vCross = aVector.Cross(bDirection); //2D cross product, determines if parallel
            var fromPointVector = bAnchor - aFrom; // the vector connecting starts

            if (vCross.IsNegligible(Constants.BaseTolerance))
            {
                // if this is also parallel with the vector direction then there is overlap
                // (since bounding boxes overlap). But we cannot set intersectionPoint
                // to a single value since it is infinite points!
                if (fromPointVector.Cross(aVector).IsNegligible(Constants.BaseTolerance))
                    return considerCollinearOverlapAsIntersect;
                return false;
            }
            // solve for the t scalar values for the two lines.
            // the line is define as all values of t from 0 to 1 in the equations
            // p-line(t_p) = (1 - t_p)*p1 + t_p*p2
            // q-line(t_q) = (1 - t_q)*q1 + t_q*q2
            // solve as a system of two equations
            //   |   vp_x      vq_x   | |  t_p  |    | vStarts_x  |
            //   |                    |*|       | =  |            |
            //   |   vp_y      vq_y   | |  t_q  |    | vStarts_y  |
            var oneOverdeterminnant = 1 / vCross;
            var t_a = oneOverdeterminnant * (bDirection.Y * fromPointVector.X - bDirection.X * fromPointVector.Y);
            if (t_a < 0 || t_a > 1)
                //if (t_1.IsLessThanNonNegligible(0, Constants.PolygonSameTolerance)
                //    || !t_1.IsLessThanNonNegligible(1.0, Constants.PolygonSameTolerance))
                return false;
            intersectionPoint = new Vector2(aFrom.X + t_a * aVector.X, aFrom.Y + t_a * aVector.Y);
            return true;
        }

        /// <summary>
        /// Lines the line2 d intersection.
        /// </summary>
        /// <param name="aAnchor">Some known point on a-line.</param>
        /// <param name="aDirection">The direction of the a-line.</param>
        /// <param name="bAnchor">Some known point on b-line.</param>
        /// <param name="bDirection">The direction of the b-line.</param>
        /// <returns>TVGL.Vector2.</returns>
        public static Vector2 LineLine2DIntersection(Vector2 aAnchor, Vector2 aDirection, Vector2 bAnchor, Vector2 bDirection)
        {
            if (aAnchor.IsAligned(bAnchor, Constants.BaseTolerance)) return aAnchor;
            var vCross = aDirection.Cross(bDirection); //2D cross product, determines if parallel

            if (vCross.IsNegligible(Constants.BaseTolerance))
                return Vector2.Null;

            var oneOverdeterminnant = 1.0 / aDirection.Cross(bDirection); //2D cross product, determines if parallel
            var t_a = oneOverdeterminnant * (bDirection.Y * (bAnchor.X - aAnchor.X) - bDirection.X * (bAnchor.Y - aAnchor.Y));
            return aAnchor + t_a * aDirection;
        }

        /// <summary>
        /// Find the point common to three planes.
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
            var matrixOfNormals = new double[,] { { n1.X, n1.Y, n1.Z }, { n2.X, n2.Y, n2.Z }, { n3.X, n3.Y, n3.Z } };
            var distances = new[] { d1, d2, d3 };
            if (!matrixOfNormals.solve(distances, out var mInv))
                return Vector3.Null;
            return new Vector3(mInv);
        }

        /// <summary>
        /// Find the point common to three planes.
        /// </summary>
        /// <param name="plane1">The plane1.</param>
        /// <param name="plane2">The plane2.</param>
        /// <param name="plane3">The plane3.</param>
        /// <returns>System.Vector2.</returns>
        public static Vector3 PointCommonToThreePlanes(this Plane plane1, Plane plane2, Plane plane3)
        {
            return PointCommonToThreePlanes(plane1.Normal, plane1.DistanceToOrigin,
                plane2.Normal, plane2.DistanceToOrigin,
                plane3.Normal, plane3.DistanceToOrigin);
        }

        /// <summary>
        /// Gets the plane from three points.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="p3">The p3.</param>
        /// <returns>TVGL.Plane.</returns>
        public static Plane GetPlaneFromThreePoints(Vector3 p1, Vector3 p2, Vector3 p3)
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
            var flat2 = new Plane(p1, normal);
            return flat2;
        }

        /// <summary>
        /// Lines the intersecting two planes.
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
        /// Lines the intersecting two planes.
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
            var a = new[,] { { n1.X, n1.Y, n1.Z }, { n2.X, n2.Y, n2.Z }, { directionOfLine.X, directionOfLine.Y, directionOfLine.Z } };
            var b = new[] { d1, d2, 0 };
            if (!a.solve(b, out var aInv))
                pointOnLine = Vector3.Null;
            else pointOnLine = new Vector3(aInv);
        }

        /// <summary>
        /// Finds the "intersection" of two skewed lines in 3D space. Generally such lines do not intersect, so the function
        /// finds the middling point equidistant and closest to both lines.
        /// </summary>
        /// <param name="anchor1">Any point on line 1.</param>
        /// <param name="direction1">The direction of line 1.</param>
        /// <param name="anchor2">Any point on line 2.</param>
        /// <param name="direction2">The direction of line 2.</param>
        /// <param name="center">The resulting "intersection" or middling point.</param>
        /// <returns>The closest distance between the two lines.</returns>
        public static double SkewedLineIntersection(Vector3 anchor1, Vector3 direction1, Vector3 anchor2, Vector3 direction2,
            out Vector3 center)
        {
            return SkewedLineIntersection(anchor1, direction1, anchor2, direction2, out center, out _, out _, out _, out _);
        }

        /// <summary>
        /// Finds the "intersection" of two skewed lines in 3D space. Generally such lines do not intersect, so the function
        /// finds the middling point equidistant and closest to both lines.
        /// </summary>
        /// <param name="anchor1">Any point on line 1.</param>
        /// <param name="direction1">The direction of line 1.</param>
        /// <param name="anchor2">Any point on line 2.</param>
        /// <param name="direction2">The direction of line 2.</param>
        /// <param name="intersect1">The point on line1 closest to line2.</param>
        /// <param name="intersect2">The point on line2 closest to line1.</param>
        /// <returns>The closest distance between the two lines.</returns>
        public static double SkewedLineIntersection(Vector3 anchor1, Vector3 direction1, Vector3 anchor2, Vector3 direction2,
            out Vector3 intersect1, out Vector3 intersect2)
        {
            return SkewedLineIntersection(anchor1, direction1, anchor2, direction2, out _, out intersect1, out intersect2, out _, out _);
        }


        /// <summary>
        /// Finds the "intersection" of two skewed lines in 3D space. Generally such lines do not intersect, so the function
        /// finds the middling point equidistant and closest to both lines.
        /// </summary>
        /// <param name="anchor1">Any point on line 1.</param>
        /// <param name="direction1">The direction of line 1.</param>
        /// <param name="anchor2">Any point on line 2.</param>
        /// <param name="direction2">The direction of line 2.</param>
        /// <param name="center">The resulting "intersection" or middling point.</param>
        /// <param name="intersect1">The point on line1 closest to line2.</param>
        /// <param name="intersect2">The point on line2 closest to line1.</param>
        /// <param name="t1">The scalar parameter for the location of interSect1 on line1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The closest distance between the two lines.</returns>
        public static double SkewedLineIntersection(Vector3 anchor1, Vector3 direction1, Vector3 anchor2, Vector3 direction2,
            out Vector3 center,
            out Vector3 intersect1, out Vector3 intersect2, out double t1, out double t2)
        {
            // set up two equations to solve for the two t's. 
            // intersect1-intersect2 is a vector that is perpendicular to both direction1 and direction2
            // using this as a start (and the dot-product equal to zero for the aforementioned perpendicularity)
            // create two equations and two unknowns

            //var a11 = n1.X * n1.X + n1.Y * n1.Y + n1.Z * n1.Z;
            var a11 = direction1.LengthSquared();
            //var a12 = -n1.X * n2.X - n1.Y * n2.Y - n1.Z * n2.Z;
            var a12 = -direction1.Dot(direction2);
            //var a21 = n1.X * n2.X + n1.Y * n2.Y + n1.Z * n2.Z;
            var a21 = -a12;
            //var a22 = -n2.X * n2.X - n2.Y * n2.Y - n2.Z * n2.Z;
            var a22 = -direction2.LengthSquared();
            //var b1 = n1.X * (p2.X - p1.X) + n1.Y * (p2.Y - p1.Y) + n1.Z * (p2.Z - p1.Z);
            var b1 = direction1.Dot(anchor2 - anchor1);
            //var b2 = n2.X * (p2.X - p1.X) + n2.Y * (p2.Y - p1.Y) + n2.Z * (p2.Z - p1.Z);
            var b2 = direction2.Dot(anchor2 - anchor1);
            //var a = new[,] { { a11, a12 }, { a21, a22 } };
            var aDetInverse = 1 / (a11 * a22 - a21 * a12);
            //var aInv = new[,] { { a22, -a12 }, {-a21,a11 } };
            t1 = (a22 * b1 - a12 * b2) * aDetInverse;
            t2 = (-a21 * b1 + a11 * b2) * aDetInverse;
            intersect1 = anchor1 + t1 * direction1;
            intersect2 = anchor2 + t2 * direction2;
            center = intersect1 + intersect2 / 2;
            return intersect1.Distance(intersect2);
        }

        #endregion Intersection Method (between lines, planes, solids, etc.)

        #region Distance Methods (between point, line, and plane)

        /// <summary>
        /// Returns the distance the point on an infinite line.
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
        /// Returns the distance the point on an infinite line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns>System.Double.</returns>
        public static double DistancePointToLine(Vector3 qPoint, Vector3 lineRefPt, Vector3 lineVector,
            out Vector3 pointOnLine)
        {
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p)
             * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            var t = (lineVector.X * (qPoint.X - lineRefPt.X) + lineVector.Y * (qPoint.Y - lineRefPt.Y) +
                      lineVector.Z * (qPoint.Z - lineRefPt.Z))
                     / (lineVector.X * lineVector.X + lineVector.Y * lineVector.Y + lineVector.Z * lineVector.Z);
            pointOnLine = new Vector3(
            lineRefPt.X + lineVector.X * t, lineRefPt.Y + lineVector.Y * t, lineRefPt.Z + lineVector.Z * t);
            return qPoint.Distance(pointOnLine);
        }

        /// <summary>
        /// Distances the point to line.
        /// </summary>
        /// <param name="qPoint">The q point.</param>
        /// <param name="lineRefPt">The line reference pt.</param>
        /// <param name="lineVector">The line vector.</param>
        /// <param name="pointOnLine">The point on line.</param>
        /// <returns>double.</returns>
        public static double DistancePointToLine(Vector2 qPoint, Vector2 lineRefPt, Vector2 lineVector,
        out Vector2 pointOnLine)
        {
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p)
            * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            var t = (lineVector.X * (qPoint.X - lineRefPt.X) + lineVector.Y * (qPoint.Y - lineRefPt.Y))
                    / (lineVector.X * lineVector.X + lineVector.Y * lineVector.Y);
            pointOnLine = new Vector2(lineRefPt.X + lineVector.X * t, lineRefPt.Y + lineVector.Y * t);
            return qPoint.Distance(pointOnLine);
        }

        /// <summary>
        /// Returns the signed distance of the point to the plane.
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
        /// Returns the signed distance of the point to the plane. If the point is "above" the plane, then a positive
        /// distance is return - if "below" then negative. This "above" means that the point is on the side of the
        /// plane that the normal points towards.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="signedDistanceToPlane">The signed distance to plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(Vector3 point, Vector3 normalOfPlane, double signedDistanceToPlane)
        {
            return normalOfPlane.Dot(point) - signedDistanceToPlane;
        }

        #endregion Distance Methods (between point, line, and plane)

        #region Find Intersecting Element

        #region Point on Face

        /// <summary>
        /// Finds the point on the face made by a line (which is described by connecting point1 and point2) intersecting
        /// with that face. If not intersection exists, then function returns null. Points must be on either side
        /// of triangle to return a valid intersection.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="relativeDistance">The relative distance.</param>
        /// <param name="onBoundaryIsInside">The on boundary is inside.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static Vector3 PointOnTriangleFromLine(this TriangleFace face, Vector3 point1,
            Vector3 point2, out double relativeDistance, bool onBoundaryIsInside = true)
        {
            var positions = face.Vertices.Select(vertex => vertex.Coordinates).ToList();
            return PointOnTriangleFromLine(positions, face.Normal, point1, point2, out relativeDistance, onBoundaryIsInside);
        }

        /// <summary>
        /// Finds the point on the face made by a line (which is described by connecting point1 and point2) intersecting
        /// with that face. If not intersection exists, then function returns a Vector3 with NaN's. Points must
        /// be on either side of triangle to return a valid intersection.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="relativeDistance">The relative distance.</param>
        /// <param name="onBoundaryIsInside">The on boundary is inside.</param>
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnTriangleFromLine(this List<Vector3> vertices, Vector3 normal, Vector3 point1,
            Vector3 point2, out double relativeDistance, bool onBoundaryIsInside = true)
        {
            var distanceToOrigin = normal.Dot(vertices[0]);
            var newPoint = PointOnPlaneFromIntersectingLine(normal, distanceToOrigin, point1, point2, out relativeDistance);
            if (newPoint.IsNull()) return Vector3.Null;
            return IsVertexInsideTriangle(vertices, newPoint, onBoundaryIsInside) ? newPoint : Vector3.Null;
        }

        /// <summary>
        /// Finds the point on the triangle made by a line. If that line is not going to pass through the
        /// that triangle, then null is returned. The signed distance is positive if the vertex points to
        /// the triangle along the direction (ray). User can also specify whether the edges of the triangle
        /// are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point3D">The point3 d.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns>TVGL.Vector3.</returns>
        public static Vector3 PointOnTriangleFromRay(TriangleFace face, Vector3 point3D, Vector3 direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            var distanceToOrigin = face.Normal.Dot(face.A.Coordinates);
            var newPoint = PointOnPlaneFromRay(face.Normal, distanceToOrigin, point3D, direction, out signedDistance);
            if (newPoint.IsNull()) return Vector3.Null;
            return IsVertexInsideTriangle(face, newPoint) ? newPoint : Vector3.Null;
        }

        /// <summary>
        /// Finds the point on the triangle made by a line. If that line is not going to pass through the
        /// that triangle, then the result is comprise of NaN's. The signed distance is positive if the vertex points to
        /// the triangle along the direction (ray). User can also specify whether the edges of the triangle
        /// are considered "inside."
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="point3D">The point3 d.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns>TVGL.Vector3.</returns>
        public static Vector3 PointOnTriangleFromLine(this TriangleFace face, Vector3 point3D, CartesianDirections direction,
            out double signedDistance, bool onBoundaryIsInside = true)
        {
            Vector3 newPoint;
            signedDistance = double.NaN;
            var d = face.Normal.Dot(face.A.Coordinates);
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
            return IsVertexInsideTriangle(face, newPoint) ? newPoint : Vector3.Null;
        }

        #endregion Point on Face

        #region Point on Plane

        /// <summary>
        /// Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        /// with that plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="relativeDistance">The relative distance of the plane. If zero, then at point1. If one, then at point2.
        /// If less than zero, then intersection occurs on the other side of point1 (not between points). If greater than one,
        /// then intersection is on the other side of point2.</param>
        /// <returns>IntersectionPoint.</returns>
        public static Vector3 PointOnPlaneFromIntersectingLine(Plane plane, Vector3 point1, Vector3 point2, out double relativeDistance)
        {
            return PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, point1, point2, out relativeDistance);
        }

        /// <summary>
        /// Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        /// with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="relativeDistance">The relative distance of the plane. If zero, then at point1. If one, then at point2.
        /// If less than zero, then intersection occurs on the other side of point1 (not between points). If greater than one,
        /// then intersection is on the other side of point2.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="Exception">This should never occur. Prevent this from happening</exception>
        public static Vector3 PointOnPlaneFromIntersectingLine(Vector3 normalOfPlane, double distOfPlane, Vector3 point1,
            Vector3 point2, out double relativeDistance)
        {
            var d1 = normalOfPlane.Dot(point1);
            var d2 = normalOfPlane.Dot(point2);
            relativeDistance = (d1 - distOfPlane) / (d1 - d2);
            return Vector3.Lerp(point1, point2, relativeDistance);
        }

        /// <summary>
        /// Finds the point on the plane made by a ray. If that ray is not going to pass through the
        /// that plane, then null is returned.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="rayPosition">The ray position.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnPlaneFromRay(Plane plane, Vector3 rayPosition,
            Vector3 rayDirection, out double signedDistance)
        {
            return PointOnPlaneFromRay(plane.Normal, plane.DistanceToOrigin, rayPosition, rayDirection, out signedDistance);
        }

        /// <summary>
        /// Finds the point on the plane made by a line. If that line is not going to pass through the
        /// that plane, then null is returned.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="anchor">An anchor point on the line.</param>
        /// <param name="lineDirection">The line direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnPlaneFromLine(Vector3 normalOfPlane, double distOfPlane, Vector3 anchor,
            Vector3 lineDirection, out double signedDistance)
        {
            var dot = lineDirection.Dot(normalOfPlane);
            signedDistance = 0.0;
            if (dot == 0) return Vector3.Null;

            var d1 = -DistancePointToPlane(anchor, normalOfPlane, distOfPlane);
            signedDistance = d1 / dot;
            if (signedDistance.IsNegligible()) return anchor;
            return anchor + (lineDirection * signedDistance);
        }


        /// <summary>
        /// Finds the point on the plane made by a ray. If that ray is not going to pass through the
        /// that plane, then null is returned.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="rayPosition">The ray position.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <param name="signedDistance">The signed distance.</param>
        /// <returns>Vertex.</returns>
        public static Vector3 PointOnPlaneFromRay(Vector3 normalOfPlane, double distOfPlane, Vector3 rayPosition,
            Vector3 rayDirection, out double signedDistance)
        {
            var dot = rayDirection.Dot(normalOfPlane);
            signedDistance = 0.0;
            if (dot == 0) return Vector3.Null;

            var d1 = -DistancePointToPlane(rayPosition, normalOfPlane, distOfPlane);
            signedDistance = d1 / dot;
            if (signedDistance < 0) return Vector3.Null;
            if (signedDistance.IsNegligible()) return rayPosition;
            return rayPosition + (rayDirection * signedDistance);
        }

        /// <summary>
        /// Finds the point on the x-plane (plane with normal [1,0,0]) made by a line (which is described by 
        /// connecting point1 and point2) intersecting with that plane.
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the x-coordinate of the plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnXPlaneFromLineSegment(double distOfPlane, Vector3 point1,
            Vector3 point2)
        {
            var toFactor = (distOfPlane - point1.X) / (point2.X - point1.X);
            var fromFactor = 1 - toFactor;

            return new Vector3(distOfPlane, fromFactor * point1.Y + toFactor * point2.Y,
                                            fromFactor * point1.Z + toFactor * point2.Z);
        }

        /// <summary>
        /// Finds the point on the y-plane (plane with normal [0,1,0]) made by a line (which is described by 
        /// connecting point1 and point2) intersecting with that plane.
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the y-coordinate of the plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnYPlaneFromLineSegment(double distOfPlane, Vector3 point1,
            Vector3 point2)
        {
            var toFactor = (distOfPlane - point1.Y) / (point2.Y - point1.Y);
            var fromFactor = 1 - toFactor;

            return new Vector3(fromFactor * point1.X + toFactor * point2.X, distOfPlane,
                               fromFactor * point1.Z + toFactor * point2.Z);
        }

        /// <summary>
        /// Finds the point on the z- plane (plane with normal [0,0,1]) made by a line (which is described by 
        /// connecting point1 and point2) intersecting with that plane. 
        /// Note, the result is a Vector2 - just the x and y value. The z-value would be the input, distOfPlane
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the z-coordinate of the plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnZPlaneFromLineSegment(double distOfPlane, Vector3 point1,
            Vector3 point2)
        {
            var toFactor = (distOfPlane - point1.Z) / (point2.Z - point1.Z);
            var fromFactor = 1 - toFactor;

            return new Vector3(fromFactor * point1.X + toFactor * point2.X,
                fromFactor * point1.Y + toFactor * point2.Y, distOfPlane);
        }

        /// <summary>
        /// Finds the point on the x-plane (plane with normal [1,0,0]) made by a line (which is described by 
        /// connecting anchor and direction) intersecting with that plane.
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the x-coordinate of the plane.</param>
        /// <param name="anchor">An anchor point on the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnXPlaneFromLine(double distOfPlane, Vector3 anchor, Vector3 direction)
        {
            var t = (distOfPlane - anchor.X) / direction.X;
            return new Vector3(distOfPlane, anchor.Y + t * direction.Y, anchor.Z + t * direction.Z);
        }

        /// <summary>
        /// Finds the point on the y-plane (plane with normal [0,1,0]) made by a line (which is described by 
        /// connecting point1 and point2) intersecting with that plane.
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the y-coordinate of the plane.</param>
        /// <param name="anchor">An anchor point on the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnYPlaneFromLine(double distOfPlane, Vector3 anchor, Vector3 direction)
        {
            var t = (distOfPlane - anchor.Y) / direction.Y;
            return new Vector3(anchor.X + t * direction.X, distOfPlane, anchor.Z + t * direction.Z);
        }

        /// <summary>
        /// Finds the point on the z- plane (plane with normal [0,0,1]) made by a line (which is described by 
        /// connecting point1 and point2) intersecting with that plane. 
        /// </summary>
        /// <param name="distOfPlane">The distance of plane or the z-coordinate of the plane.</param>
        /// <param name="anchor">An anchor point on the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <returns>The coordinates of the intersecting point.</returns>
        public static Vector3 PointOnZPlaneFromLine(double distOfPlane, Vector3 anchor, Vector3 direction)
        {
            var t = (distOfPlane - anchor.Z) / direction.Z;
            return new Vector3(anchor.X + t * direction.X, anchor.Y + t * direction.Y, distOfPlane);
        }

        #endregion Point on Plane

        #endregion Find Intersecting Element

        #region Create 2D Circle Paths

        /// <summary>
        /// Returns a the path of a circle made up of points. Increment as needed.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="radianIncrement">The radian increment.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.Vector2&gt;.</returns>
        public static List<Vector2> CreateCirclePath(Vector2 center, double radius, double radianIncrement = Math.PI / 50.0)
        {
            var path = new List<Vector2>();
            for (var theta = 0.0; theta < Math.PI * 2; theta += radianIncrement)
            {
                path.Add(new Vector2(radius * Math.Cos(theta) + center.X, radius * Math.Sin(theta) + center.Y));
            }
            return path;
        }

        #endregion Create 2D Circle Paths

        #region isInside Methods (is 2D point inside polygon, vertex inside solid, ect.)
        /// <summary>
        /// Determines whether [is vertex inside triangle] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">The on boundary is inside.</param>
        /// <returns>bool.</returns
        public static bool IsVertexInsideTriangle(TriangleFace face, Vector3 q)
        {
            var aToQ = q - face.A.Coordinates;
            var aToB = face == face.AB.OwnedFace ? face.AB.Vector : -face.AB.Vector;
            var aToC = face == face.CA.OwnedFace ? -face.CA.Vector : face.CA.Vector;
            if (aToB.Cross(aToQ).Dot(aToQ.Cross(aToC)) < 0) return false;
            var bToQ = q - face.B.Coordinates;
            var bToC = face == face.BC.OwnedFace ? face.BC.Vector : -face.BC.Vector;
            var bToA = -aToB;
            return bToC.Cross(bToQ).Dot(bToQ.Cross(bToA)) > 0;
        }
        /// <summary>
        /// Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        /// triangle are considered "inside." Assumes vertex in question is in the same plane
        /// as the triangle.
        /// </summary>
        /// <param name="triangle">The triangle.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">The on boundary is inside.</param>
        /// <returns>bool.</returns>
        public static bool IsVertexInsideTriangle(IList<Vertex> triangle, Vector3 vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            return IsVertexInsideTriangle(new[] { triangle[0].Coordinates, triangle[1].Coordinates, triangle[2].Coordinates },
                vertexInQuestion, onBoundaryIsInside);
        }

        /// <summary>
        /// Returns whether a vertex lies on a triangle. User can specify whether the edges of the
        /// triangle are considered "inside." Assumes vertex in question is in the same plane
        /// as the triangle.
        /// </summary>
        /// <param name="triangle">The triangle.</param>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="onBoundaryIsInside">The on boundary is inside.</param>
        /// <returns>bool.</returns>
        /// <exception cref="Exception">Incorrect number of points in triangle</exception>
        public static bool IsVertexInsideTriangle(IList<Vector3> triangle, Vector3 pointInQuestion,
            bool onBoundaryIsInside = true)
        {
            if (triangle.Count != 3) throw new Exception("Incorrect number of points in triangle");
            var q = pointInQuestion;
            var a = triangle[0];
            var b = triangle[1];
            var c = triangle[2];
            var aToQ = q - a;
            var aToB = b - a;
            var aToC = c - a;
            if (aToB.Cross(aToQ).Dot(aToQ.Cross(aToC)) < 0) return false;

            var bToQ = q - b;
            var bToC = c - b;
            var bToA = -aToB;
            return bToC.Cross(bToQ).Dot(bToQ.Cross(bToA)) > 0;
        }

        /// <summary>
        /// Determines if a point is inside a tesselated solid (polyhedron).
        /// And the polygon is not self-intersecting
        /// http://www.cescg.org/CESCG-2012/papers/Horvat-Ray-casting_point-in-polyhedron_test.pdf
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is vertex inside solid] [the specified ts]; otherwise, <c>false</c>.</returns>
        public static bool IsVertexInsideSolid(TessellatedSolid ts, Vertex vertexInQuestion,
            bool onBoundaryIsInside = true)
        {
            //ToDo: Rewrite function to use plane list as in SolidIntersectionFunction
            var facesAbove = new List<TriangleFace>();
            var facesBelow = new List<TriangleFace>();
            var inconclusive = true;
            var rnd = new Random(0);
            //Added while inconclusive and random direction because there are some special cases that look the
            //same. For instance, consider a vertex sitting at the center of a half moon. Along the z axis,
            //It will go through 1 edge or vertex (special cases) above and one below. Then consider a box
            //centered on the origin. A point on the origin would point to an edge (of the two faces
            //forming the face) above and one below. Therefore, it was decided that special cases (through
            //edges or locations, will yeild inconclusive results.
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

                    var distanceToOrigin = face.Normal.Dot(face.A.Coordinates);
                    var t = -(vertexInQuestion.Coordinates.Dot(face.Normal) - distanceToOrigin) /
                            direction.Dot(face.Normal);
                    //Note that if t == 0, then it is on the face
                    //else, find the intersection point and determine if it is inside the polygon (face)
                    var newVertex = t.IsNegligible()
                        ? vertexInQuestion
                        : new Vertex(vertexInQuestion.Coordinates + (direction * t));
                    if (!IsVertexInsideTriangle(face, newVertex.Coordinates)) continue;
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
        /// Is the vertex inside Axis-Aligned Bounding Box (AABB) of the provided solid.
        /// </summary>
        /// <param name="vertexInQuestion">The vertex in question.</param>
        /// <param name="solid">The ts.</param>
        /// <param name="onBoundaryIsInside">If true, on boundary is inside.</param>
        /// <returns>A bool.</returns>
        public static bool IsVertexInsideAABB(this Vertex vertexInQuestion, Solid solid,
            bool onBoundaryIsInside = true)
            => IsPointInsideAABB(vertexInQuestion.Coordinates, solid, onBoundaryIsInside);

        /// <summary>    
        /// Is the point inside Axis-Aligned Bounding Box (AABB) of the provided solid.
        /// </summary>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="solid">The ts.</param>
        /// <param name="onBoundaryIsInside">If true, on boundary is inside.</param>
        /// <returns>A bool.</returns>
        public static bool IsPointInsideAABB(this Vector3 pointInQuestion, Solid solid,
    bool onBoundaryIsInside = true)
            => IsPointInsideAABB(pointInQuestion, solid.Bounds[0], solid.Bounds[1], onBoundaryIsInside);

        /// <summary>
        /// Is the point inside Axis-Aligned Bounding Box (AABB) defined by the upper and lower bounds.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="lowerBounds">The lower bounds.</param>
        /// <param name="upperBounds">The upper bounds.</param>
        /// <param name="onBoundaryIsInside">If true, on boundary is inside.</param>
        /// <returns>A bool.</returns>
        private static bool IsPointInsideAABB(Vector3 p, Vector3 lowerBounds, Vector3 upperBounds,
            bool onBoundaryIsInside)
        {
            if (onBoundaryIsInside)
            {
                return p.X >= lowerBounds.X && p.Y >= lowerBounds.Y && p.Z >= lowerBounds.Z
                        && p.X <= upperBounds.X && p.Y <= upperBounds.Y && p.Z <= upperBounds.Z;
            }
            return p.X > lowerBounds.X && p.Y > lowerBounds.Y && p.Z > lowerBounds.Z
                        && p.X < upperBounds.X && p.Y < upperBounds.Y && p.Z < upperBounds.Z;
        }

        #endregion isInside Methods (is 2D point inside polygon, vertex inside solid, ect.)

        /// <summary>
        /// Sets the positive and negative shifts.
        /// </summary>
        /// <param name="distances">The distances.</param>
        /// <param name="distanceAlongDirection">The distance along direction.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="positiveShift">The positive shift.</param>
        /// <param name="negativeShift">The negative shift.</param>
        internal static void SetPositiveAndNegativeShifts(this IList<double> distances,
            double distanceAlongDirection, double tolerance, ref double positiveShift, ref double negativeShift)
        {
            var noChange = true;
            var closestDeltaAbove = double.PositiveInfinity;
            var closestPointBelow = double.NegativeInfinity;
            foreach (var d in distances)
            {
                if (d >= distanceAlongDirection + positiveShift)
                {
                    var delta = d - (distanceAlongDirection + positiveShift);
                    if (closestDeltaAbove > delta) closestDeltaAbove = delta;
                }
                if (d <= distanceAlongDirection + negativeShift)
                {
                    var delta = d - (distanceAlongDirection + negativeShift);
                    if (closestPointBelow < delta) closestPointBelow = delta;
                }
            }
            if (closestDeltaAbove < tolerance)
            {
                positiveShift += tolerance;
                noChange = false;
            }
            // same for the reverse
            if (-closestPointBelow < tolerance)
            {
                negativeShift -= tolerance;
                noChange = false;
            }
            if (noChange) return;
            SetPositiveAndNegativeShifts(distances, distanceAlongDirection, tolerance, ref positiveShift, ref negativeShift);
        }

        /// <summary>
        /// Converts the to a 1D collection of doubles.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>IEnumerable&lt;System.Double&gt;.</returns>
        public static IEnumerable<double> ConvertTo1DDoublesCollection(this IEnumerable<Vector3> coordinates)
        {   // this is not really the place for this function, but since it's so similar to the function above it;
            // it seems okay to leave it here (where else would it go?)
            foreach (var coordinate in coordinates)
            {
                yield return coordinate.X;
                yield return coordinate.Y;
                yield return coordinate.Z;
            }
        }

        /// <summary>
        /// Converts the to a 1D collection of doubles.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>IEnumerable&lt;System.Double&gt;.</returns>
        public static IEnumerable<double> ConvertTo1DDoublesCollection(this IEnumerable<Vertex> coordinates)
        {   // this is not really the place for this function, but since it's so similar to the function above it;
            // it seems okay to leave it here (where else would it go?)
            foreach (var coordinate in coordinates)
            {
                yield return coordinate.X;
                yield return coordinate.Y;
                yield return coordinate.Z;
            }
        }

        /// <summary>
        /// Ordereds the edges and angles CCW at vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="startingEdge">The starting edge.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;(TVGL.Edge edge, double angle)&gt;.</returns>
        public static IEnumerable<(Edge edge, double angle)> OrderedEdgesAndAnglesCCWAtVertex(this Vertex vertex, Edge startingEdge = null)
        {
            if (startingEdge == null) startingEdge = vertex.Edges[0];
            var edgePointsToVertex = startingEdge.To == vertex;
            var normal = Vector3.Zero;
            normal = vertex.Faces.Aggregate(normal, (normal, f) => normal + f.Normal);
            normal = normal.Normalize();
            var inPlaneStartVector = edgePointsToVertex ? -1 * startingEdge.Vector : startingEdge.Vector;
            var edge = startingEdge;
            TriangleFace lastFace = null;
            do
            {
                var inPlaneVector = edgePointsToVertex ? -1 * edge.Vector : edge.Vector;
                yield return (edge, inPlaneVector.AngleCCWBetweenVectorAndDatum(inPlaneStartVector, normal));
                var face = edgePointsToVertex ? edge.OtherFace : edge.OwnedFace;
                if (face == lastFace) face = edge.GetMatingFace(lastFace);
                lastFace = face;
                foreach (var e in face.Edges)
                {
                    if (e == edge) continue;
                    if (e.To == vertex)
                    {
                        edgePointsToVertex = true;
                        edge = e;
                        break;
                    }
                    if (e.From == vertex)
                    {
                        edgePointsToVertex = false;
                        edge = e;
                        break;
                    }
                }
            } while (edge != startingEdge);
        }
        /// <summary>
        /// Ordereds the faces CCW at vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="startingFace">The starting face.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.TriangleFace&gt;.</returns>
        public static IEnumerable<TriangleFace> OrderedFacesCCWAtVertex(this Vertex vertex, TriangleFace startingFace = null)
        {
            if (startingFace == null) startingFace = vertex.Faces[0];
            var face = startingFace;
            if (!face.Edges.Any())
            {
                foreach (var f in OrderedFacesCCWAtVertexNoEdges(vertex, startingFace))
                    yield return f;
                yield break;
            }
            Edge edge = null;
            foreach (var startEdge in face.Edges.Where(e => e.To == vertex || e.From == vertex))
            {
                if ((startEdge.From == vertex && startEdge.OwnedFace == face) ||
                (startEdge.To == vertex && startEdge.OtherFace == face))
                {
                    edge = startEdge;
                    break;
                }
            }
            do
            {
                yield return face;
                var edgePointsToVertex = false;
                foreach (var e in face.Edges)
                {
                    if (e == edge) continue;
                    if (e.To == vertex)
                    {
                        edgePointsToVertex = true;
                        edge = e;
                        break;
                    }
                    if (e.From == vertex)
                    {
                        edgePointsToVertex = false;
                        edge = e;
                        break;
                    }
                }
                face = edgePointsToVertex ? edge.OtherFace : edge.OwnedFace;
            } while (face != startingFace);
        }
        /// <summary>
        /// Ordereds the faces CCW at vertex no edges.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="startingFace">The starting face.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.TriangleFace&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static IEnumerable<TriangleFace> OrderedFacesCCWAtVertexNoEdges(this Vertex vertex, TriangleFace startingFace)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Store the data for a line as the four variables of Vector4. The x & y value of the output are the x & y 
        /// values of where the line passes through the plane through zero. The z value is the polar angle (makes
        /// sense since measured from z-axis) and the w is the azimuthal angle.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Unique3DLine(Vector3 anchor, Vector3 direction)
        {
            if (direction.X.IsNegligible() && direction.Y.IsNegligible())
            {
                if (direction.Z > 0) return new Vector4(anchor.X, anchor.Y, 0, 0);
                else return new Vector4(-anchor.X, anchor.Y, Math.PI, 0);
            }
            else
            {
                var oneOverRadius = 1 / direction.Length();  // normalizing factor
                var PolarAngle = Math.Acos(direction.Z * oneOverRadius);  // when z_dir is 1, then angle is zero or pi
                var AzimuthAngle = Math.Atan2(direction.Y, direction.X);  // regardless of length, we can find azimuth by tangent
                var inPlaneYDir = new Vector3(-direction.Y, direction.X, 0).Normalize(); // the y-dir will never have a z-component - it is like
                // the latitude lines on a globe
                var inPlaneXDir =  // here, x-dir determined by y cross z where z is the given direction
                                   // cross product is i = (y1z2 - y2z1), j = (x2z1 - x1z2), k = (x1y2 - x2y1)
                                   // x1 = -direction.Y, y1 = direction.X, z1=0
                                   // x2 = direction.X, y2 = direction.Y, z2 = direction.z
                     new Vector3(direction.Z * direction.X, direction.Y * direction.Z,
                     -direction.X * direction.X - direction.Y * direction.Y).Normalize();
                // note how in the result, the z-component is always negative. this is like the longitude lines on a globe that point
                // to the south pole

                var distanceToAnchor = direction.Dot(anchor) * oneOverRadius; // like a plane this is the distance from zero-plane (not origin)
                var pointOnZeroPlane = anchor - distanceToAnchor * direction * oneOverRadius; // direction is still not normalized, hence oneOverRadius
                var tx = pointOnZeroPlane.Dot(inPlaneXDir);  //the distance along new x-dir
                var ty = pointOnZeroPlane.Dot(inPlaneYDir);  //the distance along new y-dir
                return new Vector4(tx, ty, PolarAngle, AzimuthAngle);
            }
        }

        /// <summary>
        /// Decodes the Vector4 into an anchor and a unit vector direction. 
        /// </summary>
        /// <param name="unique3DLine">A vector4 where the x & y are values where the line passes through 
        /// the plane through zero, the z value is the polar angle, and the w is the azimuthal angle. </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector3 anchor, Vector3 direction) Get3DLineValuesFromUnique(this Vector4 unique3DLine)
        {
            var direction = SphericalAnglePair.ConvertSphericalToCartesian(1, unique3DLine.Z, unique3DLine.W);

            if (direction.X.IsNegligible() && direction.Y.IsNegligible())
            {
                if (direction.Z > 0)
                    return (new Vector3(unique3DLine.X, unique3DLine.Y, 0), new Vector3(0, 0, 1));
                else
                    return (new Vector3(-unique3DLine.X, unique3DLine.Y, 0), new Vector3(0, 0, -1));
            }
            var jAxis = new Vector3(-direction.Y, direction.X, 0).Normalize();
            var iAxis = new Vector3(direction.Z * direction.X, direction.Z * direction.Y,
                -direction.X * direction.X - direction.Y * direction.Y).Normalize();
            var anchor = unique3DLine.X * iAxis + unique3DLine.Y * jAxis;
            return (anchor, direction);
        }

        /// <summary>
        /// Typeses the implementing i curve.
        /// </summary>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Type&gt;.</returns>
        public static IEnumerable<Type> TypesImplementingICurve()
        {
            var asm = System.Reflection.Assembly.GetAssembly(typeof(ICurve));

            foreach (System.Reflection.TypeInfo ti in asm.DefinedTypes)
                if (ti.ImplementedInterfaces.Contains(typeof(ICurve)))
                    yield return ti;
        }
        /// <summary>
        /// Typeses the inherited from primitive surface.
        /// </summary>
        /// <returns>System.Collections.Generic.IEnumerable&lt;System.Type&gt;.</returns>
        public static IEnumerable<Type> TypesInheritedFromPrimitiveSurface()
        {
            var asm = System.Reflection.Assembly.GetAssembly(typeof(PrimitiveSurface));
            foreach (Type type in asm.GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract
                && myType.IsSubclassOf(typeof(PrimitiveSurface))))
                yield return type;
        }

    }
}