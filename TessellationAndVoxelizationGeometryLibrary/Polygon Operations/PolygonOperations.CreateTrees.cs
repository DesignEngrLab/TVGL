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
    {/*
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
       */

        /// <summary>
        /// Creates the shallow polygon trees from a collection of a collection of coordinates.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="vertexNegPosOrderIsGuaranteedCorrect">if set to <c>true</c> [vertex neg position order is guaranteed correct].</param>
        /// <param name="connectingIndices">The connecting indices.</param>
        /// <param name="strayHoles">The stray holes.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<IEnumerable<Vector2>> paths,
            bool vertexNegPosOrderIsGuaranteedCorrect, out List<Polygon> strayHoles)
        {
            return CreateShallowPolygonTrees(paths.Select(p => new Polygon(p)), vertexNegPosOrderIsGuaranteedCorrect,
                out strayHoles);
        }

        /// <summary>
        /// Creates the shallow polygon trees from a collection of flat (i.e. no inner polygons) polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="vertexNegPosOrderIsGuaranteedCorrect">if set to <c>true</c> [vertex neg position order is guaranteed correct].</param>
        /// <param name="connectingIndices">The connecting indices.</param>
        /// <param name="strayHoles">The stray holes.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<Polygon> polygons, bool vertexNegPosOrderIsGuaranteedCorrect,
            out List<Polygon> strayHoles)
        {
            var polygonTrees = CreatePolygonTree(polygons, vertexNegPosOrderIsGuaranteedCorrect, out strayHoles);
            var polygonList = new List<Polygon>();
            foreach (var polygon in polygonTrees.SelectMany(p => p.AllPolygons))
                if (polygon.IsPositive) polygonList.Add(polygon);

            foreach (var polygon in polygonList)
                foreach (var hole in polygon.InnerPolygons)
                    hole.RemoveAllInnerPolygon();
            return polygonList;
        }

        /// <summary>
        /// Creates the polygon tree from a collection of flat (i.e. no inner polygons) polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="polygonSignIsCorrect">if set to <c>true</c> [polygon sign is correct].</param>
        /// <param name="strayHoles">The stray holes.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreatePolygonTree(this IEnumerable<Polygon> polygons, bool polygonSignIsCorrect, out List<Polygon> strayHoles)
        {
            var branches = new List<Polygon>();
            strayHoles = new List<Polygon>();
            foreach (var polygon in polygons.OrderBy(p => Math.Abs(p.Area)))
            {
                for (int i = branches.Count - 1; i >= 0; i--)
                {
                    if (polygon.HasABoundingBoxThatEncompasses(branches[i]) &&  // for speed, check the bb first
                        polygon.IsNonIntersectingPolygonInside(true, branches[i], true, out _) == true)
                    {
                        polygon.AddInnerPolygon(branches[i]);
                        branches.RemoveAt(i);
                    }
                }
                branches.Add(polygon);
            }
            var polygonTrees = new List<Polygon>();
            int j = branches.Count;
            if (polygonSignIsCorrect)
            {
                while (j-- > 0)
                {
                    var current = branches[j];
                    if (!current.IsPositive)
                    {
                        strayHoles.Add(current);
                        branches.RemoveAt(j);
                        foreach (var inner in current.InnerPolygons)
                            branches.Insert(j++, inner);
                        current.RemoveAllInnerPolygon();
                    }
                    else
                    {
                        RecurseDownPolygonTreeCleanUp(current);
                        polygonTrees.Add(current);
                    }
                }
            }
            else
            {
                var negativeTopParent = new Polygon(new List<Vector2>());
                negativeTopParent.IsPositive = false;
                foreach (var branch in branches)
                {
                    RecurseDownPolygonTreeAndFlipSigns(negativeTopParent);
                    polygonTrees.Add(branch);
                }
            }
            return polygonTrees;
        }

        private static void RecurseDownPolygonTreeCleanUp(Polygon parent)
        {
            var childQueue = new Queue<Polygon>(parent.InnerPolygons);
            var validChildren = new List<Polygon>();
            while (childQueue.Any())
            {
                var child = childQueue.Dequeue();
                //if (child.IsPositive == parent.IsPositive) // this is not good. children should have oppositve sign than parent
                //    foreach (var grandChild in child.Holes)
                //        childQueue.Enqueue(grandChild);
                //else
                if (child.IsPositive != parent.IsPositive)
                {
                    validChildren.Add(child);
                    RecurseDownPolygonTreeCleanUp(child);
                }
            }
            parent.RemoveAllInnerPolygon();
            foreach (var c in validChildren)
                parent.AddInnerPolygon(c);
        }

        private static void RecurseDownPolygonTreeAndFlipSigns(Polygon parent)
        {
            var childIsPositive = !parent.IsPositive;
            foreach (var child in parent.InnerPolygons)
            {
                child.IsPositive = childIsPositive;
                RecurseDownPolygonTreeAndFlipSigns(child);
            }
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
                var coordinates = path as IList<Vector2> ?? path.ToList();
                if (coordinates.Area() < 0)
                    positivePolygon.AddInnerPolygon(new Polygon(coordinates));
                else
                {
                    yield return positivePolygon;
                    positivePolygon = new Polygon(coordinates);
                }
            }
        }
    }
}