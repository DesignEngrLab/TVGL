using SharpDxf;
using SharpDxf.Entities;
using SharpDxf.Header;
using System;
using System.Collections.Generic;
using System.Text;
using TVGL;

namespace PolygonImportExport.SVG
{
    internal class SVG
    {

        /// <param name="curvePrecision">Number of line segments used to approximate each curve entity (arc, circle, ellipse, NURBS).</param>
        public static List<Polygon> Open(string filePath, DxfVersion dxfVersion, int curvePrecision = 30)
        {
            var result = new List<Polygon>();

            // convert SVG schema code to polygons


            return result;
        }


        public static bool Save(string filePath, IEnumerable<Polygon> polygons)
        {
            try
            {
                // convert polygons to a valid svg file and save it to the specified path
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
