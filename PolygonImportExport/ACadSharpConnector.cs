using ACadSharp.Entities;
using CSMath;
using TVGL;
using Circle = ACadSharp.Entities.Circle;

namespace PolygonImportExport
{
    internal static class ACadSharpConnector
    {
        internal static void AddEntity(Entity entity, List<Polygon> result, int curvePrecision)
        {
            if (entity.IsInvisible) return;
            switch (entity)
            {
                case IPolyline polyline: // LwPolyline, Polyline2D, Polyline3D
                    var isClosed = polyline.IsClosed;
                    var points = PolylineToPoints(polyline, curvePrecision, ref isClosed);
                    AddPolygon(result, points, isClosed);
                    break;

                case Line line:
                    AddPolygon(result, new List<Vector2> { ToVector2(line.StartPoint), ToVector2(line.EndPoint) }, isClosed: false);
                    break;

                case Arc arc: // must precede Circle: Arc derives from Circle
                    AddPolygon(result, arc.PolygonalVertexes(curvePrecision).Select(ToVector2).ToList(), isClosed: false);
                    break;

                case Circle circle:
                    AddPolygon(result, circle.PolygonalVertexes(curvePrecision).Select(ToVector2).ToList(), isClosed: true);
                    break;

                case Ellipse ellipse:
                    AddPolygon(result, ellipse.PolygonalVertexes(curvePrecision).Select(ToVector2).ToList(), ellipse.IsFullEllipse);
                    break;

                case Spline spline:
                    if (spline.TryPolygonalVertexes(curvePrecision, out var splinePoints))
                        AddPolygon(result, splinePoints.Select(ToVector2).ToList(), spline.IsClosed);
                    break;

                case Insert insert: // block reference: explode to entities in parent coordinates
                    foreach (var exploded in insert.Explode())
                        AddEntity(exploded, result, curvePrecision);
                    break;
            }
        }

        private static void AddPolygon(List<Polygon> result, List<Vector2> points, bool isClosed)
        {
            if (points.Count >= (isClosed ? 3 : 2))
                result.Add(new Polygon(points, isClosed: isClosed));
        }

        /// <summary>
        /// Converts a polyline's vertices to points, tessellating bulge-encoded arc
        /// segments. For closed polylines the first point is not repeated at the end.
        /// </summary>
        private static List<Vector2> PolylineToPoints(IPolyline polyline, int curvePrecision, ref bool isClosed)
        {
            var vertices = polyline.Vertices.ToList();
            var points = new List<Vector2>();
            if (vertices.Count == 0) return points;
            points.Add(ToVector2(vertices[0].Location));
            var numSegments = isClosed ? vertices.Count : vertices.Count - 1;
            for (int i = 0; i < numSegments; i++)
            {
                var start = ToVector2(vertices[i].Location);
                var end = ToVector2(vertices[(i + 1) % vertices.Count].Location);
                var isClosingSegment = i == vertices.Count - 1; // ends at first point, already in list
                var bulge = vertices[i].Bulge;
                var chord = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
                if (Math.Abs(bulge) > 1e-10 && chord > 1e-12)
                    AddBulgeArcPoints(points, start, end, bulge, chord, curvePrecision, includeEnd: !isClosingSegment);
                else if (!isClosingSegment)
                    points.Add(end);
            }
            if (!isClosed)
            {
                var xMin = points.Min(p => p.X);
                var yMin = points.Min(p => p.Y);
                var xMax = points.Max(p => p.X);
                var yMax = points.Max(p => p.Y);
                var error = 1e-9 * Math.Max(xMax - xMin, yMax - yMin);
                isClosed = points[0].IsPracticallySame(points[^1], error);
                if (isClosed) points.RemoveAt(0);
            }
            return points;
        }

        /// <summary>
        /// Tessellates a bulge-encoded circular arc from start to end. The bulge is the
        /// tangent of a quarter of the included angle, negative for clockwise arcs.
        /// </summary>
        private static void AddBulgeArcPoints(List<Vector2> points, Vector2 start, Vector2 end,
            double bulge, double chord, int precision, bool includeEnd)
        {
            var theta = 4 * Math.Atan(Math.Abs(bulge)); // included angle, in (0, 2*pi)
            var radius = 0.5 * chord / Math.Sin(0.5 * theta);
            var phi = Math.Atan2(end.Y - start.Y, end.X - start.X) + Math.Sign(bulge) * 0.5 * (Math.PI - theta);
            var center = new Vector2(start.X + radius * Math.Cos(phi), start.Y + radius * Math.Sin(phi));
            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var sweep = Math.Sign(bulge) * theta;
            for (int i = 1; i < precision; i++)
            {
                var angle = startAngle + sweep * i / precision;
                points.Add(new Vector2(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)));
            }
            if (includeEnd) points.Add(end);
        }

        private static Vector2 ToVector2(XYZ point) => new Vector2(point.X, point.Y);
        private static Vector2 ToVector2(CSMath.IVector point) => new Vector2(point[0], point[1]);

        internal static List<Polygon> OrganizeIntoShallowTree(List<Polygon> rawPolygons)
        {
            var openPolylines = new List<Polygon>();
            for (int i = rawPolygons.Count - 1; i >= 0; i--)
            {
                if (!rawPolygons[i].IsClosed)
                {
                    openPolylines.Add(rawPolygons[i]);
                    rawPolygons.RemoveAt(i);
                }
            }
            var result = rawPolygons.CreateShallowPolygonTrees(false);
            result.AddRange(openPolylines);
            return result;
        }
    }
}
