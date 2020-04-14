using System;
using System.Collections.Generic;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGLUnitTestsAndBenchmarking
{

    internal static class MiscellaneousFunctions
    {
        private static void Run()
        {
            MiscFunctions.SortAlongDirection(Vector3.UnitX, new Vector3[0], out var sortedCoordinates);
            MiscFunctions.SortAlongDirection(Vector3.UnitX, new Vertex[0], out var sortedVertices);
            MiscFunctions.SortAlongDirection(Vector2.UnitX, new Vector2[0], out List<Vector2> sortedCoords2D);
            MiscFunctions.SortAlongDirection(Vector2.UnitX, new Vector2[0], out List<(Vector2, double)> sortedTuples);

            var perimeter = MiscFunctions.Perimeter(new Vertex[0]);
          

            //extrude

            //silhouette
        }
    }
}