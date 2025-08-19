using System.Collections.Generic;
using System.Linq;
using SuperClusterKDTree;

namespace TVGL
{
    public static class KDTreeExtensions
    {
        /// <summary>
        /// Creates the KDTree for the list of points common in TVGL (Vector3, Vertex, Vector2, or Vertex2D).
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="points"></param>
        /// <param name="distanceMetric"></param>
        /// <returns></returns>
        public static KDTree<double, double, TCoord> ToKDTree<TCoord>(this IEnumerable<TCoord> points, DistanceMetrics distanceMetric = DistanceMetrics.EuclideanDistance)
            where TCoord : IVector
        {
            var pointsList = points as IList<TCoord> ?? points.ToList();
            return KDTree.Create((IList<IReadOnlyList<double>>)pointsList, pointsList, distanceMetric);
        }
        /// <summary>
        /// Creates the KDTree for the list of points common in TVGL (Vector3, Vertex, Vector2, or Vertex2D) and some associated objects.
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <typeparam name="TAccObject"></typeparam>
        /// <param name="points"></param>
        /// <param name="accObjects"></param>
        /// <param name="distanceMetric"></param>
        /// <returns></returns>
        public static KDTree<double, double, TAccObject> ToKDTree<TCoord, TAccObject>(this IEnumerable<TCoord> points, IEnumerable<TAccObject> accObjects,
            DistanceMetrics distanceMetric = DistanceMetrics.EuclideanDistance)
            where TCoord : IVector
        {
            var pointsList = points as IList<TCoord> ?? points.ToList();
            var accObjectsList = accObjects as IList<TAccObject> ?? accObjects.ToList();
            return KDTree.Create((IList<IReadOnlyList<double>>)pointsList, accObjectsList, DistanceMetrics.EuclideanDistance);
        }

        /// <summary>
        /// Finds the nearest "n" points in the KDTree that are closest to the target point (where n is numberToFind).
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public static IEnumerable<TCoord> FindNearestPoints<TCoord>(this KDTree<double, double, TCoord> tree, TCoord target, int numberToFind)
            where TCoord : IVector
        => tree.NearestNeighbors(target, numberToFind).Select(t => t.Item2);

        /// <summary>
        /// Finds the nearest points in the KDTree within the given radius, you can also limit this to the a maximum number, numberToFind.
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="radius"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public static IEnumerable<TCoord> FindNearestPoints<TCoord>(this KDTree<double, double, TCoord> tree, TCoord target, double radius, int numberToFind = -1)
            where TCoord : IVector
        => tree.RadialSearch(target, radius, numberToFind).Select(t => t.Item2);

        /// <summary>
        /// Finds the nearest "n" points in the KDTree that are closest to the target point 
        /// (where n is numberToFind) along with the associated objects.
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <typeparam name="TAccObject"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public static IEnumerable<(TCoord, TAccObject)> FindNearest<TCoord, TAccObject>(this KDTree<double, double, TAccObject> tree, TCoord target, int numberToFind)
            where TCoord : IVector
        => tree.NearestNeighbors(target, numberToFind).Cast<(TCoord, TAccObject)>();

        /// <summary>
        /// Finds the nearest points in the KDTree within the given radius, you can also limit this to the 
        /// a maximum number, numberToFind, along with the associated objects.
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="radius"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public static IEnumerable<(TCoord, TAccObject)> FindNearest<TCoord, TAccObject>(this KDTree<double, double, TAccObject> tree, TCoord target, double radius, int numberToFind = -1)
            where TCoord : IVector
        => tree.RadialSearch(target, radius, numberToFind).Cast<(TCoord, TAccObject)>();
    }
}