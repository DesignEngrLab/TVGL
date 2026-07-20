using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using TVGL;

namespace PolygonImportExport
{
    public static class DXF
    {
        /// <summary>
        /// Reads a DXF file and converts its 2D entities (polylines, lines, arcs,
        /// circles, ellipses, splines, and block inserts) to a list of TVGL Polygons.
        /// </summary>
        /// <param name="curvePrecision">Number of line segments used to approximate each curve entity (arc, circle, ellipse, spline, bulge).</param>
        public static List<Polygon> Open(string filePath, int curvePrecision = 30)
        {
            var cad2DData = DxfReader.Read(filePath);
            var result = new List<Polygon>();
            foreach (var entity in cad2DData.Entities)
                ACadSharpConnector.AddEntity(entity, result, curvePrecision);
            return result;
        }

        public static bool Save(string filePath, IEnumerable<Polygon> polygons, ACadVersion version = ACadVersion.AC1018)
        {
            try
            {
                var doc = new CadDocument(version);
                foreach (var polygon in polygons)
                {
                    foreach (var poly in polygon.AllPolygons)
                    {
                        var polyline = new LwPolyline { IsClosed = poly.IsClosed };
                        foreach (var pt in poly.Path)
                            polyline.Vertices.Add(new LwPolyline.Vertex(pt.X, pt.Y));
                        doc.Entities.Add(polyline);
                    }
                }
                DxfWriter.Write(filePath, doc, binary: false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
