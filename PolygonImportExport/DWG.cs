using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using TVGL;

namespace PolygonImportExport
{
    /// <summary>
    /// Imports and exports two-dimensional polygon geometry in Drawing (DWG) files.
    /// </summary>
    public static class DWG
    {
        /// <summary>
        /// Reads a DWG file and converts its 2D entities (polylines, lines, arcs,
        /// circles, ellipses, splines, and block inserts) to a list of TVGL Polygons.
        /// </summary>
        /// <param name="filePath">The path of the DWG file to read.</param>
        /// <param name="curvePrecision">The number of line segments used to approximate each curve entity.</param>
        /// <returns>The imported closed polygons and open polylines.</returns>
        public static List<Polygon> Open(string filePath, int curvePrecision = 30)
        {
            var cad2DData = DwgReader.Read(filePath);
            var result = new List<Polygon>();
            foreach (var entity in cad2DData.Entities)
                ACadSharpConnector.AddEntity(entity, result, curvePrecision);
            return ACadSharpConnector.OrganizeIntoShallowTree(result);

        }


        /// <summary>
        /// Writes TVGL polygons to a DWG file as lightweight polylines.
        /// </summary>
        /// <param name="filePath">The path of the DWG file to create.</param>
        /// <param name="polygons">The polygons and open polylines to write.</param>
        /// <param name="version">The AutoCAD file-format version to use.</param>
        /// <returns><see langword="true"/> when the file is written successfully; otherwise, <see langword="false"/>.</returns>
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
                DwgWriter.Write(filePath, doc);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
