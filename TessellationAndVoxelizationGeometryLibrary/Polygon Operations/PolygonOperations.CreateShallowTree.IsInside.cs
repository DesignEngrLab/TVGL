using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            bool vertexNegPosOrderIsGuaranteedCorrect, bool pathsAreNotSelfIntersecting, out Polygon[] polygons,
            out int[] connectingIndices)
        {
            if (vertexNegPosOrderIsGuaranteedCorrect)
                return CreateShallowPolygonTreesOrderedVertexLoops(paths, !pathsAreNotSelfIntersecting, out polygons,
                    out connectingIndices);
            else
                return CreateShallowPolygonTreesUnorderedVertexLoops(paths, !pathsAreNotSelfIntersecting, out polygons,
                    out connectingIndices);
        }

        private static bool CreateShallowPolygonTreesOrderedVertexLoops(this IEnumerable<IEnumerable<Vector2>> paths,
            bool removeSelfIntersections,
            out Polygon[] polygons, out int[] connectingIndices)
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

            connectingIndices = new int[index];
            polygons = positivePolygons.Values.ToArray();
            //2) Find the positive polygon that this negative polygon is inside.
            //The negative polygon belongs to the smallest positive polygon that it fits inside.
            //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
            //and the reversed ordering, gaurantee that we get the correct shallow tree.
            foreach (var negativePolygonKVP in negativePolygons)
            {
                var isInside = false;
                var area = negativePolygonKVP.Key;
                var negativePolygon = negativePolygonKVP.Value;
                //Start with the smallest positive polygon           
                foreach (var positivePolygon in polygons)
                {
                    if (-area > positivePolygon.Area) continue;
                    var polygonRelationship =
                        positivePolygon.GetPolygonRelationshipAndIntersections(negativePolygon, out _);
                    if (((byte) polygonRelationship & 0b1) != 0
                    ) // the "1" flag is intersection. We can't handle that here.
                        return false;
                    if (((byte) polygonRelationship & 0b010) != 0) // the "2" flag means that boundaries touch.
                        return false;
                    if (((byte) polygonRelationship & 0b100) != 0) // the "4" flag is B is inside A. We can do that
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

        private static bool CreateShallowPolygonTreesUnorderedVertexLoops(this IEnumerable<IEnumerable<Vector2>> paths,
            bool removeSelfIntersections,
            out Polygon[] polygons, out int[] connectingIndices)
        {
            var polygonDictionary = new SortedDictionary<double, Polygon>(new NoEqualSort(false));
            polygons = null;
            var index = 0;
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
            var polygonList = new List<Polygon>();
            foreach (var polygon in polygonDictionary.Values)
            {
                for (int i = 0; i < polygonList.Count; i++)
                {
                    var outerPolygon = polygonList[i];
                    var polygonRelationship = outerPolygon.GetPolygonRelationshipAndIntersections(polygon, out _);
                    if (((byte) polygonRelationship & 0b1) != 0
                    ) // the "1" flag is intersection. We can't handle that here.
                        return false;
                    if (((byte) polygonRelationship & 0b010) != 0) // the "2" flag means that boundaries touch.
                        return false;
                    if (((byte) polygonRelationship & 0b100) != 0) // the "4" flag is B is inside A. We can do that
                    {
                        var insideAHoleOfOuterPolygon = false;
                        foreach (var innerPolygon in outerPolygon.Holes)
                        {
                            var innerPolygonRelationship =
                                innerPolygon.GetPolygonRelationshipAndIntersections(polygon, out _);
                            if (((byte) innerPolygonRelationship & 0b1) != 0
                            ) // the "1" flag is intersection. We can't handle that here.
                                return false;
                            if (((byte) innerPolygonRelationship & 0b010) != 0
                            ) // the "2" flag means that boundaries touch.
                                return false;
                            if (((byte) innerPolygonRelationship & 0b100) != 0
                            ) // the "4" flag is B is inside A. We can do that
                            {
                                polygonList.Add(polygon);
                                insideAHoleOfOuterPolygon = true;
                                break;
                            }
                        }

                        if (!insideAHoleOfOuterPolygon)
                        {
                            polygon.Reverse();
                            outerPolygon.AddHole(polygon);
                        }

                        break;
                    }
                }
            }

            //Set the polygon indices
            index = 0;
            foreach (var polygon in polygonList)
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

            polygons = polygonList.ToArray();
            return true;
        }

        /// <summary>
        /// Creates the shallow polygon trees following boolean operations. The name follows the public methods,
        /// this is meant to be used only internally as it requires several assumptions:
        /// 1. positive polygons are ordered by increasing area (from 0 to +inf)
        /// 2. negative polygons are ordered by increasing area (from -inf to 0)
        /// 3. there are not intersections between the solids (this should be the result following the boolean
        /// operation; however, it is possible that they share a vertex (e.g. in XOR))
        /// </summary>
        /// <param name="Polygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <returns>Polygon[].</returns>
        /// <exception cref="Exception">Intersections still exist between hole and positive polygon.</exception>
        /// <exception cref="Exception">Negative polygon was not inside any positive polygons</exception>
        private static List<Polygon> CreateShallowPolygonTreesPostBooleanOperation(List<Polygon> polygons,
                IEnumerable<Polygon> negativePolygons)
            //SortedDictionary<double, Polygon>.ValueCollection negativePolygons)
        {
            int i = 0;
            while (i < polygons.Count)
            {
                var foundToBeInsideOfOther = false;
                int j = i;
                while (++j < polygons.Count)
                {
                    if (polygons[j].IsNonIntersectingPolygonInside(polygons[i], out _))
                    {
                        foundToBeInsideOfOther = true;
                        break;
                    }
                }

                if (foundToBeInsideOfOther)
                    polygons.RemoveAt(i);
                else i++;
            }

            //  Find the positive polygon that this negative polygon is inside.
            //The negative polygon belongs to the smallest positive polygon that it fits inside.
            //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
            //and the reversed ordering, gaurantee that we get the correct shallow tree.
            foreach (var negativePolygon in negativePolygons)
            {
                var isInside = false;
                //Start with the smallest positive polygon           
                for (var j = 0; j < polygons.Count; j++)
                {
                    var positivePolygon = polygons[j];
                    if (positivePolygon.IsNonIntersectingPolygonInside(negativePolygon, out var onBoundary))
                    {
                        isInside = true;
                        if (onBoundary)
                        {
                            var newPolys = positivePolygon.Intersect(negativePolygon);
                            polygons[j] = newPolys[0]; // i don't know if this is a problem, but the
                            // new polygon at j may be smaller (now that it has a big hole in it ) than the preceding ones. I don't think
                            // we need to maintain ordered by area - since the first loop above will already merge positive loops
                            for (int k = 1; k < newPolys.Count; k++)
                                polygons.Add(newPolys[i]);
                        }
                        else positivePolygon.AddHole(negativePolygon);

                        //The negative polygon ONLY belongs to the smallest positive polygon that it fits inside.
                        //isInside = true;
                        break;
                    }
                }

                if (!isInside)
                    polygons.Add(
                        negativePolygon); //this feels like it should come with a warning. but perhaps the user/developer intends to create a negative polygon
            }

            //Set the polygon indices
            var index = 0;
            foreach (var polygon in polygons)
            {
                polygon.Index = index++;
                foreach (var hole in polygon.Holes)
                {
                    hole.Index = polygon.Index;
                }
            }

            return polygons;
        }



        /// <summary>
        /// Creates the shallow polygon trees from ordered lists and vertices. This means that the loops are one positive loops
        /// followed by the inner negative holes, then another positive loops and it's holes.
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