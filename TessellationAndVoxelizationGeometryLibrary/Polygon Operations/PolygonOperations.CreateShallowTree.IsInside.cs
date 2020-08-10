using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {


        /// <summary>
        /// Gets the Shallow Polygon Trees for a given set of paths.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="vertexNegPosOrderIsGuaranteedCorrect">if set to <c>true</c> [vertices are properly ordered to represents positive (CCW) and negative (CW) polygons].</param>
        /// <param name="pathsAreNotSelfIntersecting">if set to <c>true</c> [paths are known to not be self-intersecting]. Like the previous boolean, computaional time can
        /// be saved if these two are known going into this function.</param>
        /// <param name="polygons">The polygons.</param>
        /// <param name="connectingIndices">The connecting indices.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        /// <exception cref="Exception">Negative polygon was not inside any positive polygons</exception>
        public static bool CreateShallowPolygonTrees(this IEnumerable<IEnumerable<Vector2>> paths,
            bool vertexNegPosOrderIsGuaranteedCorrect, out List<Polygon> polygons,
            out int[] connectingIndices)
        {
            if (vertexNegPosOrderIsGuaranteedCorrect)
                return CreateShallowPolygonTreesOrderedVertexLoops(paths, out polygons,
                    out connectingIndices);
            else
                return CreateShallowPolygonTreesUnorderedVertexLoops(paths, out polygons,
                    out connectingIndices);
        }

        internal static bool CreateShallowPolygonTreesOrderedVertexLoops(this IEnumerable<IEnumerable<Vector2>> paths,
            out List<Polygon> polygons, out int[] connectingIndices)
        {
            var positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort());
            var negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort(false));
            var index = 0;
            foreach (var path in paths)
            {
                var polygon = new Polygon(path, index++);
                var area = polygon.Area;
                if (area < 0) negativePolygons.Add(area, polygon);
                if (area > 0) positivePolygons.Add(area, polygon);
            }
            return CreateShallowPolygonTreesOrderedVertexLoops(positivePolygons, negativePolygons, index,
                out polygons, out connectingIndices);
        }

        internal static bool CreateShallowPolygonTreesOrderedVertexLoops(SortedDictionary<double, Polygon> positivePolygons,
            SortedDictionary<double, Polygon> negativePolygons, int count, out List<Polygon> polygons, out int[] connectingIndices)
        {
            connectingIndices = new int[count];
            polygons = positivePolygons.Values.ToList();
            foreach (var negativePolygonKVP in negativePolygons)
            {
                // Find the positive polygon that this negative polygon is inside.
                //The negative polygon belongs to the smallest positive polygon that it fits inside.
                //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
                //and the reversed ordering, gaurantee that we get the correct shallow tree.
                var isInside = false;
                var area = negativePolygonKVP.Key;
                var negativePolygon = negativePolygonKVP.Value;
                //Start with the smallest positive polygon           
                foreach (var positivePolygon in polygons)
                {
                    if (-area > positivePolygon.Area) continue;
                    var polygonRelationship =
                        positivePolygon.GetPolygonRelationshipAndIntersections(negativePolygon, out _);
                    if ((polygonRelationship & PolygonRelationship.EdgesCross) != 0)
                        return false;

                    if ((polygonRelationship & PolygonRelationship.BInsideA) != 0
                        && (polygonRelationship & PolygonRelationship.InsideHole) == 0)
                    {
                        positivePolygon.AddHole(negativePolygon);
                        //The negative polygon ONLY belongs to the smallest positive polygon that it fits inside.
                        isInside = true;
                        break;
                    }
                }
                if (!isInside) return false; // Negative polygon was not inside any positive polygons
            }

            //Set the polygon indices
            count = 0;
            foreach (var polygon in polygons)
            {
                if (polygon.Index < 0) continue;
                connectingIndices[polygon.Index] = count;
                polygon.Index = count++;
                foreach (var hole in polygon.Holes)
                {
                if (hole.Index < 0) continue;
                    connectingIndices[hole.Index] = count++;
                    hole.Index = polygon.Index;
                }
            }
            return true;
        }

        private static bool CreateShallowPolygonTreesUnorderedVertexLoops(this IEnumerable<IEnumerable<Vector2>> paths,
            out List<Polygon> polygons, out int[] connectingIndices)
        {
            var polygonDictionary = new SortedDictionary<double, Polygon>(new NoEqualSort(false));
            polygons = null;
            var index = 0;
            // start out by building the dictionary of polygons sorted by their area
            foreach (var path in paths)
            {
                var polygon = new Polygon(path, index++);
                var area = polygon.Area;
                if (area < 0)
                {
                    polygon.Reverse();
                    area = -area;
                }

                polygonDictionary.Add(area, polygon);
            }

            connectingIndices = new int[index];
            polygons = new List<Polygon>();
            foreach (var polygon in polygonDictionary.Values)
            {
                var detectedAsAHoleInOther = false;
                for (int i = 0; i < polygons.Count; i++)
                {
                    var outerPolygon = polygons[i];
                    var polygonRelationship = outerPolygon.GetPolygonRelationshipAndIntersections(polygon, out _);

                    if ((polygonRelationship & PolygonRelationship.EdgesCross) != 0)
                        return false;
                    if ((polygonRelationship & PolygonRelationship.BInsideA) != 0
                        && (polygonRelationship & PolygonRelationship.InsideHole) == 0)
                    {
                        polygon.Reverse();
                        outerPolygon.AddHole(polygon);
                        detectedAsAHoleInOther = true;
                        break;
                    }
                }
                if (!detectedAsAHoleInOther) polygons.Add(polygon);
            }

            //Set the polygon indices
            index = 0;
            foreach (var polygon in polygons)
            {
                connectingIndices[polygon.Index] = index;
                polygon.Index = index++;
                foreach (var hole in polygon.Holes)
                {
                    connectingIndices[hole.Index] = index++;
                    hole.Index = polygon.Index;
                }

                //index += 1 + polygon.Holes.Count;
            }

            return true;
        }


        /// <summary>
        /// Creates the shallow polygon trees from ordered lists and vertices. This means that the loops are one 
        /// positive loop followed by the inner negative holes, then another positive loops and it's holes.
        /// This is an internal/private function which is quick and currently used by Simplify
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        private static IEnumerable<Polygon> CreateShallowPolygonTreesOrderedListsAndVertices(
            this IEnumerable<IEnumerable<Vector2>> paths)
        {
            Polygon positivePolygon = new Polygon(paths.First());
            foreach (var path in paths.Skip(1))
            {
                if (path.Area() < 0)
                    positivePolygon.AddHole(new Polygon(path));
                else
                {
                    yield return positivePolygon;
                    positivePolygon = new Polygon(path);
                }
            }
        }

    }
}