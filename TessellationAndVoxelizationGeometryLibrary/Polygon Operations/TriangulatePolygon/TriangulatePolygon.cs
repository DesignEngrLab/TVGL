using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Triangulates a Polygon into faces in O(n log n) time.
    /// </summary>
    ///  <References>
    ///     Trapezoidation algorithm heavily based on: 
    ///     "A Fast Trapezoidation Technique For Planar Polygons" by
    ///     Gian Paolo Lorenzetto, Amitava Datta, and Richard Thomas. 2000.
    ///     http://www.researchgate.net/publication/2547487_A_Fast_Trapezoidation_Technique_For_Planar_Polygons
    ///     This algorithm should run in O(n log n)  time.    
    /// 
    ///     Triangulation method based on Montuno's work, but referenced material and algorithm are from:
    ///     http://www.personal.kent.edu/~rmuhamma/Compgeometry/MyCG/PolyPart/polyPartition.htm
    ///     This algorithm should run in O(n) time.
    /// </References>
    public static partial class PolygonOperations
    {
        public static IEnumerable<Vertex[]> Triangulate(this IEnumerable<Vertex> vertexLoop, Vector3 normal)
        {
            var vertexList = vertexLoop as List<Vertex> ?? vertexLoop.ToList();
            var transform = normal.TransformToXYPlane(out _);
            var polygon = new Polygon(vertexList
                .Select(v => new Vertex2D(v.ConvertTo2DCoordinates(transform), v.IndexInList, -1)).ToList());
            foreach (var triangleIndices in polygon.Triangulate())
                yield return new[]
                    {vertexList[triangleIndices[0]], vertexList[triangleIndices[1]], vertexList[triangleIndices[2]]};
        }

        /// <summary>
        /// Triangulates a list of loops into faces in O(n*log(n)) time.
        /// If ignoring negative space, the function will fill in holes. 
        /// DO NOT USE "ignoreNegativeSpace" for watertight geometry.
        /// </summary>
        /// <returns>List&lt;List&lt;Vertex[]&gt;&gt;.</returns>
        /// <exception cref="System.Exception">
        /// Inputs into 'TriangulatePolygon' are unbalanced
        /// or
        /// Duplicate point found
        /// or
        /// Incorrect balance of node types
        /// or
        /// Incorrect balance of node types
        /// or
        /// Negative Loop must be inside a positive loop, but no positive loops are left. Check if loops were created correctly.
        /// or
        /// Trapezoidation failed to complete properly. Check to see that the assumptions are met.
        /// or
        /// Incorrect number of triangles created in triangulate function
        /// or
        /// </exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="Exception"></exception>
        //ASSUMPTION: NO lines intersect other lines or points && NO two points in any of the loops are the same.
        //Ex 1) If a negative loop and positive share a point, the negative loop should be inserted into the positive loop after that point and
        //then a slightly altered point (near duplicate) should be inserted after the negative loop such that the lines do not intersect.
        //Ex 2) If a negative loop shares 2 consecutive points on a positive loop, insert the negative loop into the positive loop between those two points.
        //Ex 3) If a positive loop intersects itself, it should be two separate positive loops.

        //ROBUST FEATURES:
        // 1: Two positive loops may share a point, because they are processed separately.
        // 2: Loops can be in given CW or CCW, because as long as the isPositive boolean is correct, 
        // the code recognizes when the loop should be reversed.
        // 3: If isPositive == null, CW and CCW ordering for the loops is unknown. A preprocess step can build a new isPositive variable.
        // 4: It is OK if a positive loop is inside a another positive loop, given that there is a negative loop between them.
        // These "nested" loop cases are handled by ordering the loops (working outward to inward) and the red black tree.
        // 5: If isPositive == null, then 
        public static List<int[]> Triangulate(this Polygon polygon, bool reIndexPolygons = true)
        {
            const int maxNumberOfAttempts = 3;
            var random = new Random(1);
            var randomAngles = new double[maxNumberOfAttempts];
            for (int i = 1; i < maxNumberOfAttempts; i++)
                randomAngles[i] = 2 * Math.PI * random.NextDouble();

            if (reIndexPolygons)
            {
                var index = 0;
                foreach (var subPolygon in polygon.AllPolygons)
                foreach (var vertex in subPolygon.Vertices)
                    vertex.IndexInList = index++;
            }


            foreach (var randomAngle in randomAngles)
            {
                var triangleFaceList =
                    new List<int[]>(); // this is the returned list of triangles. Well, not actually triangles but three integers each - corresponding
                // to the 3 indices of the input polygon's Vertex2D
                try
                {
                    if (randomAngle != 0)
                        polygon.Transform(Matrix3x3.CreateRotation(randomAngle));
                    foreach (var monoPoly in MakeXMonotonePolygons(polygon))
                    {
                        triangleFaceList.AddRange(TriangulateMonotonePolygon(monoPoly));
                    }
                    return triangleFaceList;
                }
                catch (Exception exception)
                {
                    if (randomAngle != 0)
                        polygon.Transform(Matrix3x3.CreateRotation(-randomAngle));
                }
            }
            return null;
        }

        private static IEnumerable<Polygon> MakeXMonotonePolygons(Polygon polygon)
        {
            var sortedVertices =
                CombineSortedVerticesIntoOneCollection(polygon.AllPolygons.Select(p => p.OrderedXVertices).ToList());
            var newEdgeDict = new Dictionary<Vertex2D,Vertex2D>();
            var edgeDatums = new List<(PolygonSegment, Vertex2D)>();
            foreach (var vertex in sortedVertices)
            {
                var monoChange = GetMonotonicityChange(vertex);
                var cornerCross = vertex.EndLine.Vector.Cross(vertex.StartLine.Vector);
                if (monoChange == MonotonicityChange.Neither || monoChange == MonotonicityChange.Y)
                    // then it's regular
                {
                    if (vertex.StartLine.Vector.X > 0)
                        edgeDatums.Add(vertex.StartLine, vertex);
                }
                else if (cornerCross > 0) //then either start or end
                {
                }
                else //then either split or merge
                {
                }
            }
        }

        private static IEnumerable<Vertex2D> CombineSortedVerticesIntoOneCollection(List<List<Vertex2D>> orderedListsOfVertices)
        {
            var numLists = orderedListsOfVertices.Count;
            var currentIndices = new int[numLists];
            while (true)
            {
                var lowestXValue = double.PositiveInfinity;
                var lowestYValue = double.PositiveInfinity;
                var lowestEntry = -1;
                for (int i = 0; i < numLists; i++)
                {
                    var index = currentIndices[i];
                    if (orderedListsOfVertices[i].Count <= index) continue;
                    var vertex = orderedListsOfVertices[i][index];
                    if (vertex.X < lowestXValue ||
                        (vertex.X == lowestXValue && vertex.Y < lowestYValue))
                    {
                        lowestXValue = vertex.X;
                        lowestYValue = vertex.Y;
                        lowestEntry = i;
                    }
                }
                if (lowestEntry==-1) yield break;
                yield return orderedListsOfVertices[lowestEntry][currentIndices[lowestEntry]];
                currentIndices[lowestEntry]++;
            }
        }

        private static IEnumerable<int[]> TriangulateMonotonePolygon(Polygon monoPoly)
        {
            throw new NotImplementedException();
        }

    }
}