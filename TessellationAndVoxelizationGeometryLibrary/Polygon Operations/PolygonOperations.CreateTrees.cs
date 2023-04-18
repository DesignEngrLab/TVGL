// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.CreateTrees.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
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
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<IEnumerable<Vector2>> paths,
            bool vertexNegPosOrderIsGuaranteedCorrect)
        {
            return CreateShallowPolygonTrees(paths.Select(p => new Polygon(p)), vertexNegPosOrderIsGuaranteedCorrect);
        }

        /// <summary>
        /// Creates the shallow polygon trees from a collection of flat (i.e. no inner polygons) polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="vertexNegPosOrderIsGuaranteedCorrect">if set to <c>true</c> [vertex neg position order is guaranteed correct].</param>
        /// <param name="alreadyOrderedInIncreasingArea">if set to <c>true</c> [already ordered in increasing area].</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<Polygon> polygons, bool vertexNegPosOrderIsGuaranteedCorrect,
            bool alreadyOrderedInIncreasingArea = false)
        {
            var polygonTrees = CreatePolygonTree(polygons, vertexNegPosOrderIsGuaranteedCorrect, alreadyOrderedInIncreasingArea);

            var polygonList = new List<Polygon>();
            foreach (var polygon in polygonTrees.SelectMany(p => p.AllPolygons))
                if (polygon.IsPositive) polygonList.Add(polygon);

            foreach (var polygon in polygonList)
                foreach (var hole in polygon.InnerPolygons)
                    hole.RemoveAllInnerPolygon();
            foreach (var strayHole in polygonTrees.Where(p => !p.IsPositive))
                polygonList.Add(strayHole);
            return polygonList;
        }

        /// <summary>
        /// Creates the polygon tree from a collection of flat (i.e. no inner polygons) polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="polygonSignIsCorrect">if set to <c>true</c> [polygon sign is correct].</param>
        /// <param name="alreadyOrderedInIncreasingArea">if set to <c>true</c> [already ordered in increasing area].</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> CreatePolygonTree(this IEnumerable<Polygon> polygons, bool polygonSignIsCorrect,
            bool alreadyOrderedInIncreasingArea = false)
        {
            var branches = new List<Polygon>();
            var orderedPolygons = alreadyOrderedInIncreasingArea ? polygons : polygons.OrderBy(p => Math.Abs(p.Area));
            foreach (var polygon in orderedPolygons)
            {
                if (polygon.Vertices.Count < 3 || polygon.Area.IsNegligible()) continue;
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
            foreach (var branch in branches)
            {
                if (polygonSignIsCorrect) RecurseDownPolygonTreeCleanUp(branch);
                else
                {
                    branch.IsPositive = true;
                    RecurseDownPolygonTreeAndFlipSigns(branch);
                }
            }
            return branches;
        }

        /// <summary>
        /// Recurses down polygon tree clean up.
        /// </summary>
        /// <param name="parent">The parent.</param>
        private static void RecurseDownPolygonTreeCleanUp(Polygon parent)
        {
            var validChildren = new List<Polygon>();
            foreach (var child in parent.InnerPolygons)
            {
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

        /// <summary>
        /// Recurses down polygon tree and flip signs.
        /// </summary>
        /// <param name="parent">The parent.</param>
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