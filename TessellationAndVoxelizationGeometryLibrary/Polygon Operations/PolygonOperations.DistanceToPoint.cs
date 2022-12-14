using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.Polygon_Operations
{
    public static partial class PolygonOperations
    {
        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, Vector2 qPoint)
        {
            return MinDistanceToPolygons(polygons, qPoint.X, qPoint.Y);
        }

        public static double MinDistanceToPolygons(this IEnumerable<Polygon> polygons, double x, double y)
        {
            var edges = new List<PolygonEdge>();
            foreach (var polygon in polygons)
            {
                polygon.MakePolygonEdgesIfNonExistent();
                edges.AddRange(polygon.AllPolygons.SelectMany(p => p.Edges));
            }
            return MinDistanceToPolygon(x, y, edges);
        }

        public static double MinDistanceToPolygon(this Polygon polygon, Vector2 qPoint)
        {
            return MinDistanceToPolygon(polygon, qPoint.X, qPoint.Y);
        }

        public static double MinDistanceToPolygon(this Polygon polygon, double x, double y)
        {
            polygon.MakePolygonEdgesIfNonExistent();
            var edges = polygon.AllPolygons.SelectMany(p => p.Edges);
            return MinDistanceToPolygon(x, y, edges);
        }

        private static double MinDistanceToPolygon(double x, double y, IEnumerable<PolygonEdge> edges)
        {
            var minDistance = double.MaxValue;
            foreach (var edge in edges)
            {
                /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p)
                * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
                var d = SqDistancePointToLineSegment(x, y, edge);
                if (d < minDistance) { minDistance = d; }
            }
            return Math.Sqrt(minDistance);
        }

        //Refer to Joshua's answer: https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
        //x, y is your target point and x1, y1 to x2, y2 is your line segment.
        private static double SqDistancePointToLineSegment(double x, double y, PolygonEdge edge)
        {
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
                xx = edge.FromPoint.X;
                yy = edge.FromPoint.Y;
            }
            else if (param > 1)
            {
                xx = edge.Vector.X;
                yy = edge.Vector.Y;
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
