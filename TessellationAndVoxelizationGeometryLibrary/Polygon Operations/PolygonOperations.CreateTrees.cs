using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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


        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<IEnumerable<Vector2>> paths,
            bool vertexNegPosOrderIsGuaranteedCorrect, out int[] connectingIndices, out List<Polygon> strayHoles)
        {
            return CreateShallowPolygonTrees(paths.Select(p => new Polygon(p)), vertexNegPosOrderIsGuaranteedCorrect,
                out connectingIndices, out strayHoles);
        }

        public static List<Polygon> CreateShallowPolygonTrees(this IEnumerable<Polygon> polygons,
            bool vertexNegPosOrderIsGuaranteedCorrect, out int[] connectingIndices, out List<Polygon> strayHoles)
        {
            var index = 0;
            var polygonList = new List<Polygon>();
            strayHoles = new List<Polygon>();
            foreach (var p in polygons)
            {
                p.Index = index++;
                polygonList.Add(p);
            }

            if (polygonList.Count == 0)
            {
                connectingIndices = null;
                return polygonList;
            }
            connectingIndices = new int[index];
            //Presenter.ShowAndHang(polygons);
            var connectingIndicesList = new List<int>();
            var polygonTrees = CreatePolygonTree(polygonList, vertexNegPosOrderIsGuaranteedCorrect, out strayHoles);
            //Presenter.ShowAndHang(polygonTree.AllPolygons);
            polygonList.Clear();
            index = 0;
            foreach (var strayHole in strayHoles)
            {
                if (strayHole.Index < 0) continue;
                while (connectingIndicesList.Count <= strayHole.Index) connectingIndicesList.Add(-1);
                connectingIndicesList[strayHole.Index] = index;
                strayHole.Index = index++;
            }
            foreach (var polygon in polygonTrees.SelectMany(p => p.AllPolygons))
            {
                if (polygon.IsPositive) polygonList.Add(polygon);
                if (polygon.Index < 0) continue;
                while (connectingIndicesList.Count <= polygon.Index) connectingIndicesList.Add(-1);
                connectingIndicesList[polygon.Index] = index;
                polygon.Index = index++;
            }
            // finally remove references to inner positives with this loop
            foreach (var polygon in polygonList)
                foreach (var hole in polygon.InnerPolygons)
                    hole.RemoveAllInnerPolygon();
            connectingIndices = connectingIndicesList.ToArray();
            return polygonList;
        }


        public static List<Polygon> CreatePolygonTree(this IEnumerable<IEnumerable<Vector2>> paths, bool vertexNegPosOrderIsGuaranteedCorrect)
        {
            return CreatePolygonTree(paths.Select(p => new Polygon(p)), vertexNegPosOrderIsGuaranteedCorrect, out List<Polygon> strayHoles);
        }

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
                    continue;
                }
                if (polygonSignIsCorrect)
                    RecurseDownPolygonTreeCleanUp(current);
                else RecurseDownPolygonTreeAndFlipSigns(current);
                polygonTrees.Add(current);
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
                if (path.Area() < 0)
                    positivePolygon.AddInnerPolygon(new Polygon(path));
                else
                {
                    yield return positivePolygon;
                    positivePolygon = new Polygon(path);
                }
            }
        }

    }
}