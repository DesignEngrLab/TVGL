using SuperClusterKDTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A KDTree for points that are of type IVector, which includes Vector3, Vertex, Vector2, and Vertex2D.
    /// </summary>
    /// <typeparam name="TCoord"></typeparam>
    /// <param name="points"></param>
    public class KDTree<TCoord>(IEnumerable<TCoord> points, int pointsCount) : KDTree<double, double, TCoord>
        (points.First().Count, points.Cast<IReadOnlyList<double>>(), pointsCount, points,
         (x, y) => KDTree.ManhattanDistance(x, y), double.MinValue, double.MaxValue)
        where TCoord : IVector
    {
        public KDTree(ICollection<TCoord> points) : this(points, points.Count) { }

        /// <summary>
        /// Finds the nearest "n" points in the KDTree that are closest to the target point (where n is numberToFind).
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public IEnumerable<TCoord> FindNearest(TCoord target, int numberToFind)
        => NearestNeighbors(target, numberToFind).Select(t => t.Item2);

        /// <summary>
        /// Finds the nearest points in the KDTree within the given radius, you can also limit this to the a maximum number, numberToFind.
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="tree"></param>
        /// <param name="target"></param>
        /// <param name="radius"></param>
        /// <param name="numberToFind"></param>
        /// <returns></returns>
        public IEnumerable<TCoord> FindNearest(TCoord target, double radius, int numberToFind = -1)
        => RadialSearch(target, radius, numberToFind).Select(t => t.Item2);
    }

    /// <summary>
    /// A KDTree for the list of points common in TVGL (Vector3, Vertex, Vector2, or Vertex2D) and some associated objects.
    /// 
    /// </summary>
    /// <typeparam name="TCoord"></typeparam>
    /// <typeparam name="TAccObject"></typeparam>
    /// <param name="points"></param>
    /// <param name="accObjects"></param>
    public class KDTree<TCoord, TAccObject>(IEnumerable<TCoord> points, int pointsCount, IEnumerable<TAccObject> accObjects) : KDTree<double, double, TAccObject>
        (points.First().Count, points.Cast<IReadOnlyList<double>>(), pointsCount, accObjects,
         (x, y) => KDTree.ManhattanDistance(x, y), double.MinValue, double.MaxValue)
        where TCoord : IVector
    {
        public KDTree(ICollection<TCoord> points, IEnumerable<TAccObject> accObjects) : this(points, points.Count, accObjects) { }

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
        public IEnumerable<(TCoord, TAccObject)> FindNearest(TCoord target, int numberToFind)
        => NearestNeighbors(target, numberToFind).Cast<(TCoord, TAccObject)>();

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
        public IEnumerable<(TCoord, TAccObject)> FindNearest(TCoord target, double radius, int numberToFind = -1)
        => RadialSearch(target, radius, numberToFind).Cast<(TCoord, TAccObject)>();
    }
    
    public static class KDTreeExtensions
    {
        /// <summary>
        /// Creates the KDTree for the list of points common in TVGL (Vector3, Vertex, Vector2, or Vertex2D).
        /// </summary>
        /// <typeparam name="TCoord"></typeparam>
        /// <param name="points"></param>
        /// <param name="distanceMetric"></param>
        /// <returns></returns>
        public static KDTree<TCoord> ToKDTree<TCoord>(this IEnumerable<TCoord> points, int pointsCount = -1)
            where TCoord : IVector
        {
            if (pointsCount < 0)
                pointsCount = points.Count();
            return new TVGL.KDTree<TCoord>(points, pointsCount);
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
        public static KDTree<TCoord, TAccObject> ToKDTree<TCoord, TAccObject>(this IEnumerable<TCoord> points,
            IEnumerable<TAccObject> accObjects, int pointsCount = -1)
            where TCoord : IVector
        {
            if (pointsCount < 0)
                pointsCount = points.Count();
            //var accObjectsList = accObjects as IList<TAccObject> ?? accObjects.ToList();
            return new TVGL.KDTree<TCoord, TAccObject>(points, pointsCount, accObjects);
        }
    }
        
}