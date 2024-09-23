using System.Collections.Generic;
using TVGL;



namespace TVGLUnitTestsAndBenchmarking
{

    internal static class MiscellaneousFunctions
    {
        private static void Run()
        {
            MiscFunctions.SortAlongDirection(new Vector3[0], Vector3.UnitX, out var sortedCoordinates);
            MiscFunctions.SortAlongDirection(new Vertex[0], Vector3.UnitX, out var sortedVertices);
            MiscFunctions.SortAlongDirection(new Vector2[0], Vector2.UnitX, out List<Vector2> sortedCoords2D);
            MiscFunctions.SortAlongDirection(new Vector2[0], Vector2.UnitX, out List<(Vector2, double)> sortedTuples);

            var perimeter = MiscFunctions.Perimeter(new Vertex[0]);
          

            //extrude

            //silhouette
        /*
         
         */
        
        }
    }
}