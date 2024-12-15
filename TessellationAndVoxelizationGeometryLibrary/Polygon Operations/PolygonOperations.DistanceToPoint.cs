// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.DistanceToPoint.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class PolygonOperations.
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Minimums the distance to polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="qPoint">The q point.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, Vector2 qPoint)
        {
            return MinDistanceToPolygons(polygons, qPoint.X, qPoint.Y);
        }

        /// <summary>
        /// Minimums the distance to polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, double x, double y)
        {
            var edges = new List<PolygonEdge>();
            foreach (var polygon in polygons)
            {
                polygon.MakePolygonEdgesIfNonExistent();
                edges.AddRange(polygon.AllPolygons.SelectMany(p => p.Edges));
            }
            return MinDistanceToPolygon(x, y, edges, out _);
        }

        /// <summary>
        /// Minimums the distance to polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="qPoint">The q point.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, Vector2 qPoint, out PolygonEdge closestEdge)
        {
            return MinDistanceToPolygons(polygons, qPoint.X, qPoint.Y, out closestEdge);
        }

        /// <summary>
        /// Minimums the distance to polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, double x, double y, out PolygonEdge closestEdge)
        {
            var edges = new List<PolygonEdge>();
            foreach (var polygon in polygons)
            {
                polygon.MakePolygonEdgesIfNonExistent();
                edges.AddRange(polygon.AllPolygons.SelectMany(p => p.Edges));
            }
            return MinDistanceToPolygon(x, y, edges, out closestEdge);
        }

        /// <summary>
        /// Minimums the distance to polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="qPoint">The q point.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygon(this Polygon polygon, Vector2 qPoint)
        {
            return MinDistanceToPolygon(polygon, qPoint.X, qPoint.Y);
        }

        /// <summary>
        /// Minimums the distance to polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygon(this Polygon polygon, double x, double y)
        {
            polygon.MakePolygonEdgesIfNonExistent();
            var edges = polygon.AllPolygons.SelectMany(p => p.Edges);
            return MinDistanceToPolygon(x, y, edges, out _);
        }
        /// <summary>
        /// Minimums the distance to polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="qPoint">The q point.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygon(this Polygon polygon, Vector2 qPoint, out PolygonEdge closestEdge)
        {
            return MinDistanceToPolygon(polygon, qPoint.X, qPoint.Y, out closestEdge);
        }

        /// <summary>
        /// Minimums the distance to polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        public static double MinDistanceToPolygon(this Polygon polygon, double x, double y, out PolygonEdge closestEdge)
        {
            polygon.MakePolygonEdgesIfNonExistent();
            var edges = polygon.AllPolygons.SelectMany(p => p.Edges);
            return MinDistanceToPolygon(x, y, edges, out closestEdge);
        }

        /// <summary>
        /// Minimums the distance to polygon.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="edges">The edges.</param>
        /// <returns>System.Double.</returns>
        private static double MinDistanceToPolygon(double x, double y, IEnumerable<PolygonEdge> edges, out PolygonEdge closestEdge)
        {
            var queryPoint = new Vector2(x, y);
            var minDistance = double.MaxValue;
            closestEdge = null;
            Vertex2D closestVertex = null;
            foreach (var edge in edges)
            {
                /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p)
                * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
                var d = SqDistancePointToLineSegment(x, y, edge, out var atEndPoint);
                if (d < minDistance)
                {
                    if (atEndPoint)
                    {
                        var fromDistance = edge.FromPoint.Coordinates.DistanceSquared(queryPoint);
                        var toDistance = edge.ToPoint.Coordinates.DistanceSquared(queryPoint);
                        closestVertex = fromDistance < toDistance ? edge.FromPoint : edge.ToPoint;
                    }
                    else closestVertex = null;
                    closestEdge = edge;
                    minDistance = d;
                }
            }
            if (closestVertex != null)
            {
                var startLine = closestVertex.StartLine;
                var endLine = closestVertex.EndLine;
                var t = 0.01;
                var startLinePoint = closestVertex.Coordinates + t * startLine.Vector;
                var endLinePoint = closestVertex.Coordinates - t * endLine.Vector;
                if (startLinePoint.DistanceSquared(queryPoint) < endLinePoint.DistanceSquared(queryPoint))
                    closestEdge = startLine;
                else closestEdge = endLine;
            }
            return Math.Sqrt(minDistance);
        }
    }
}
