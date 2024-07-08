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
            var minDistance = double.MaxValue;
            closestEdge = null;
            var atAnEndPoint = false;
            foreach (var edge in edges)
            {
                /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p)
                * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
                var d = SqDistancePointToLineSegment(x, y, edge, out var atThisEndPoint);
                if (d < minDistance)
                {
                    atAnEndPoint = atThisEndPoint;
                    closestEdge = edge;
                    minDistance = d;
                }
            }
            if (atAnEndPoint)
            {
                var queryPoint = new Vector2(x, y);
                var prevEdge = closestEdge.FromPoint.EndLine;
                var t = 0.01;
                var prevEdgeClosePoint = new Vector2(closestEdge.FromPoint.X - t * prevEdge.Vector.X, closestEdge.FromPoint.Y - t * prevEdge.Vector.Y);
                var thisEdgeClosePoint = new Vector2(closestEdge.FromPoint.X + t * closestEdge.Vector.X, closestEdge.FromPoint.Y + t * closestEdge.Vector.Y);
                if ((prevEdgeClosePoint - queryPoint).LengthSquared() < (thisEdgeClosePoint - queryPoint).LengthSquared())
                    closestEdge = prevEdge;
            }
            return Math.Sqrt(minDistance);
        }

        //Refer to Joshua's answer: https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
        //x, y is your target point and x1, y1 to x2, y2 is your line segment.
        /// <summary>
        /// Sqs the distance point to line segment.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="edge">The edge.</param>
        /// <returns>System.Double.</returns>
        private static double SqDistancePointToLineSegment(double x, double y, PolygonEdge edge, out bool atEndpoint)
        {
            atEndpoint = false;
            var A = x - edge.FromPoint.X;
            var B = y - edge.FromPoint.Y;
            var C = edge.Vector.X;
            var D = edge.Vector.Y;

            var dot = A * C + B * D;
            var param = -1.0;
            if (!edge.Length.IsNegligible()) //in case of 0 length line
                param = dot / (edge.Length * edge.Length);

            double xx, yy;
            if (param < 0)
            {
                atEndpoint = true;
                xx = edge.FromPoint.X;
                yy = edge.FromPoint.Y;
            }
            else if (param > 1)
            {
                atEndpoint = true;
                xx = edge.ToPoint.X;
                yy = edge.ToPoint.Y;
            }
            else
            {
                xx = edge.FromPoint.X + param * C;
                yy = edge.FromPoint.Y + param * D;
            }

            var dx = x - xx;
            var dy = y - yy;
            return dx * dx + dy * dy;
        }
    }
}
