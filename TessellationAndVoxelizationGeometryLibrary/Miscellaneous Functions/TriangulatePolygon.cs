using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    internal static class TriangulatePolygon
    {
        /// <summary>
        ///     Triangulates a Polygon into faces.
        /// </summary>
        /// <param name="points2D">The 2D points represented in loops.</param>
        /// <param name="isPositive">Indicates whether the corresponding loop is positive or not.</param>
        /// <returns>List&lt;Point[]&gt;, which represents vertices of new faces.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static List<Point[]> Run(Point[][] points2D, Boolean[] isPositive)
        {
            var firstPosLoop = points2D[isPositive.FindIndex(true)];

            var n = firstPosLoop.GetLength(0);
            var edgeVectors = new double[n][];
            edgeVectors[0] = firstPosLoop[0].Position.subtract(firstPosLoop[n - 1].Position);
            for (var i = 1; i < n; i++)
                edgeVectors[i] = firstPosLoop[i].Position.subtract(firstPosLoop[i - 1].Position);

            var normals = new List<double[]>();
            var tempCross = edgeVectors[n - 1].crossProduct(edgeVectors[0]).normalize();
            if (!tempCross.Any(double.IsNaN)) normals.Add(tempCross);
            for (var i = 1; i < n; i++)
            {
                tempCross = edgeVectors[i - 1].crossProduct(edgeVectors[i]).normalize();
                if (!tempCross.Any(double.IsNaN))
                    normals.Add(tempCross);
            }
            n = normals.Count;
            throw new NotImplementedException("Brandon to write this code");
        }
    }
}