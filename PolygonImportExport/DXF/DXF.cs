using SharpDxf;
using SharpDxf.Entities;
using SharpDxf.Header;
using TVGL;

namespace PolygonImportExport
{
    public static class DXF
    {
        /// <param name="curvePrecision">Number of line segments used to approximate each curve entity (arc, circle, ellipse, NURBS).</param>
        public static List<Polygon> Open(string filePath, DxfVersion dxfVersion, int curvePrecision = 30)
        {
            var dxf = new DxfDocument();
            dxf.Load(filePath);

            var result = new List<Polygon>();

            // Polylines — PoligonalVertexes tessellates bulge-encoded arc segments as well as straight segments
            foreach (var polyline in dxf.Polylines)
            {
                List<Vector2> points;
                if (polyline is LightWeightPolyline lwp)
                    points = lwp.PoligonalVertexes(curvePrecision, MathHelper.EpsilonD, MathHelper.EpsilonD);
                else if (polyline is Polyline p)
                    points = p.PoligonalVertexes(curvePrecision, MathHelper.EpsilonD, MathHelper.EpsilonD);
                else
                    continue;
                if (points.Count >= 3)
                    result.Add(new Polygon(points));
            }

            // Ellipses — ToPolyline applies the center offset and handles full vs. partial ellipses
            foreach (var ellipse in dxf.Ellipses)
            {
                var poly = ellipse.ToPolyline(curvePrecision);
                var points = poly.Vertexes.Select(v => v.Location).ToList();
                if (points.Count >= 3)
                    result.Add(new Polygon(points, isClosed: ellipse.IsFullEllipse));
            }

            // Arcs — open polygonal chain
            foreach (var arc in dxf.Arcs)
            {
                var points = ArcToPoints(arc, curvePrecision);
                if (points.Count >= 2)
                    result.Add(new Polygon(points, isClosed: false));
            }

            // Circles
            foreach (var circle in dxf.Circles)
            {
                var points = CircleToPoints(circle, curvePrecision);
                if (points.Count >= 3)
                    result.Add(new Polygon(points));
            }

            // NURBS curves
            foreach (var nurbs in dxf.NurbsCurves)
            {
                var points = nurbs.PolygonalVertexes(curvePrecision);
                if (points.Count >= 3)
                    result.Add(new Polygon(points));
            }

            return result;
        }

        private static List<Vector2> ArcToPoints(Arc arc, int precision)
        {
            var points = new List<Vector2>(precision + 1);
            double startRad = arc.StartAngle * MathHelper.DegToRad;
            double endRad = arc.EndAngle * MathHelper.DegToRad;
            // DXF arcs sweep CCW; if end <= start it wraps past 0°
            if (endRad <= startRad) endRad += MathHelper.TwoPI;
            double step = (endRad - startRad) / precision;
            for (int i = 0; i <= precision; i++)
            {
                double angle = startRad + i * step;
                points.Add(new Vector2(
                    arc.Center.X + arc.Radius * Math.Cos(angle),
                    arc.Center.Y + arc.Radius * Math.Sin(angle)));
            }
            return points;
        }

        private static List<Vector2> CircleToPoints(SharpDxf.Entities.Circle circle, int precision)
        {
            var points = new List<Vector2>(precision);
            double step = MathHelper.TwoPI / precision;
            for (int i = 0; i < precision; i++)
            {
                double angle = i * step;
                points.Add(new Vector2(
                    circle.Center.X + circle.Radius * Math.Cos(angle),
                    circle.Center.Y + circle.Radius * Math.Sin(angle)));
            }
            return points;
        }

        public static bool Save(string filePath, IEnumerable<Polygon> polygons, DxfVersion dxfVersion)
        {
            try
            {
                var dxf = new DxfDocument();
                foreach (var polygon in polygons)
                {
                    foreach (var poly in polygon.AllPolygons)
                    {
                        var vertices = poly.Path.Select(pt => new LightWeightPolylineVertex(pt)).ToList();
                        dxf.AddEntity(new LightWeightPolyline(vertices, poly.IsClosed));
                    }
                }
                dxf.Save(filePath, dxfVersion);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
