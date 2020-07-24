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
        #region Simplify
        public static IEnumerable<Polygon> Simplify(this IEnumerable<Polygon> polygons, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            return polygons.Select(poly => poly.Simplify(allowableChangeInAreaFraction));
        }
        public static Polygon Simplify(this Polygon polygon, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            var simplifiedPositivePolygon = new Polygon(polygon.Path.Simplify(allowableChangeInAreaFraction));
            foreach (var polygonHole in polygon.Holes)
                simplifiedPositivePolygon.AddHole(new Polygon(polygonHole.Path.Simplify(allowableChangeInAreaFraction)));
            return simplifiedPositivePolygon;
        }


        public static List<List<Vector2>> Simplify(this IEnumerable<IEnumerable<Vector2>> paths, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            return paths.Select(p => Simplify(p, allowableChangeInAreaFraction)).ToList();
        }

        /// <summary>
        /// Simplifies the lines on a polygon to use fewer points when possible.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> Simplify(this IEnumerable<Vector2> path, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            var polygon = path.ToList();
            var numPoints = polygon.Count;
            var origArea = Math.Abs(polygon.Area()); //take absolute value s.t. it works on holes as well
            #region build initial list of cross products
            // queue is sorted on the cross-product at the polygon corner (requiring knowledge of the previous and next points. I'm very tempted
            // to call it vertex, which is a better name but I don't want to confuse with the Vertex class in TessellatedSolid). 
            // Here we are using the SimplePriorityQueue from BlueRaja (https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)
            var convexCornerQueue = new SimplePriorityQueue<int, double>(new ForwardSort());
            var concaveCornerQueue = new SimplePriorityQueue<int, double>(new ReverseSort());
            // cross-products which are kept in the same order as the corners they represent. This is solely used with the above
            // dictionary - to essentially do the reverse lookup. given a corner-index, crossProductsArray will instanly tell us the 
            // cross-product. The cross-product is used as the key in the dictionary - to find corner-indices.
            var crossProductsArray = new double[numPoints];

            // make the cross-products. this is a for-loop that is preceded with the first element (requiring the last element, "^1" in 
            // C# 8 terms) and succeeded by one for the last corner 
            AddCrossProductToOneOfTheLists(polygon[^1], polygon[0], polygon[1], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, 0);
            for (int i = 1; i < numPoints - 1; i++)
                AddCrossProductToOneOfTheLists(polygon[i - 1], polygon[i], polygon[i + 1], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, i);
            AddCrossProductToOneOfTheLists(polygon[^2], polygon[^1], polygon[0], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, numPoints - 1);
            #endregion

            // after much thought, the idea to split up into positive and negative sorted lists is so that we don't over remove vertices
            // by bouncing back and forth between convex and concave while staying with the target deltaArea. So, we do as many convex corners
            // before reaching a reducation of deltaArea - followed by a reduction of concave edges so that no omre than deltaArea is re-added
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                var deltaArea = 2 * allowableChangeInAreaFraction * origArea; //multiplied by 2 in order to reduce all the divide by 2
                                                                              // that happens when we change cross-product to area of a triangle
                var relevantSortedList = (sign == 1) ? convexCornerQueue : concaveCornerQueue;
                // first we remove any convex corners that would reduce the area
                while (relevantSortedList.Count > 0)
                {
                    var index = relevantSortedList.Dequeue();
                    var smallestArea = crossProductsArray[index];
                    if (deltaArea < sign * smallestArea)
                    { //this was one tricky little bug! in order to keep this fast, we first dequeue before examining
                      // the result. if the resulting index produces more area than we need we switch to the
                      // concave queue. That dequeueing and updating will want this last index on the queues
                      // if it is a neighbor to a new one being removing. Confusing, eh? So, we need to put it
                      // back in. Looks kludge-y but this only happens once, and it's better to do this once
                      // then add more logic to the above statements that would slow it down.
                        relevantSortedList.Enqueue(index, smallestArea);
                        break;
                    }
                    deltaArea -= sign * smallestArea;
                    //  set the corner to null. we'll remove null corners at the end. for now, just set to null. 
                    // this is for speed and keep the indices correct in the various collections
                    polygon[index] = Vector2.Null;
                    // find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                    // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                    // (nextnextIndex and prevprevIndex)
                    int nextIndex = FindValidNeighborIndex(index, true, polygon, numPoints);
                    int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygon, numPoints);
                    int prevIndex = FindValidNeighborIndex(index, false, polygon, numPoints);
                    int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygon, numPoints);
                    // if the polygon has been reduced to 2 points, then we're going to delete it
                    if (nextnextIndex == prevIndex || nextIndex == prevprevIndex) // then reduced to two points.
                        continue;

                    // now, add these new crossproducts both to the dictionary and to the sortedLists. Note, that nothing is
                    // removed from the sorted lists here. it is more efficient to just remove them if they bubble to the top of the list, 
                    // which is done in PopNextSmallestArea
                    UpdateCrossProductInQueues(polygon[prevIndex], polygon[nextIndex], polygon[nextnextIndex], convexCornerQueue, concaveCornerQueue,
                        crossProductsArray, nextIndex);
                    UpdateCrossProductInQueues(polygon[prevprevIndex], polygon[prevIndex], polygon[nextIndex], convexCornerQueue, concaveCornerQueue,
                            crossProductsArray, prevIndex);
                }
            }
            return polygon.Where(v => !v.IsNull()).ToList();
        }


        public static Polygon Simplify(this Polygon polygon, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygon.AllPolygons.Select(poly => poly.Path).Simplify(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths).First();
        }

        public static IEnumerable<Polygon> Simplify(this IEnumerable<Polygon> polygons, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygons.SelectMany(poly => poly.AllPolygons.Select(poly => poly.Path)).Simplify(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths);
        }

        /// <summary>
        /// Simplifies the lines on a polygon to use fewer points when possible.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<List<Vector2>> Simplify(this IEnumerable<Vector2> path, int targetNumberOfPoints)
        { return Simplify(new[] { path }, targetNumberOfPoints); }

        /// <summary>
        /// Simplifies the lines on a polygon to be at the target amount.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        /// <exception cref="ArgumentOutOfRangeException">targetNumberOfPoints - The number of points to remove in PolygonOperations.Simplify"
        ///                   + " is more than the total number of points in the polygon(s).</exception>
        public static List<List<Vector2>> Simplify(this IEnumerable<IEnumerable<Vector2>> path, int targetNumberOfPoints)
        {
            var polygons = path.Select(p => p.ToList()).ToList();
            var numPoints = polygons.Select(p => p.Count).ToList();
            var numToRemove = numPoints.Sum() - targetNumberOfPoints;
            #region build initial list of cross products
            var cornerQueue = new SimplePriorityQueue<int, double>(new AbsoluteValueSort());
            var crossProductsArray = new double[numPoints.Sum()];
            var index = 0;
            for (int j = 0; j < polygons.Count; j++)
            {
                AddCrossProductToQueue(polygons[j][^1], polygons[j][0], polygons[j][1], cornerQueue, crossProductsArray, index++);
                for (int i = 1; i < numPoints[j] - 1; i++)
                    AddCrossProductToQueue(polygons[j][i - 1], polygons[j][i], polygons[j][i + 1], cornerQueue, crossProductsArray, index++);
                AddCrossProductToQueue(polygons[j][^2], polygons[j][^1], polygons[j][0], cornerQueue, crossProductsArray, index++);
            }
            #endregion
            if (numToRemove <= 0) throw new ArgumentOutOfRangeException("targetNumberOfPoints", "The number of points to remove in PolygonOperations.Simplify"
                  + " is more than the total number of points in the polygon(s).");
            while (numToRemove-- > 0)
            {
                index = cornerQueue.Dequeue();
                var cornerIndex = index;
                var polygonIndex = 0;
                // the index is from stringing together all the original polygons into one long array
                while (cornerIndex >= polygons[polygonIndex].Count) cornerIndex -= polygons[polygonIndex++].Count;
                polygons[polygonIndex][cornerIndex] = Vector2.Null;

                // find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                // (nextnextIndex and prevprevIndex)
                int nextIndex = FindValidNeighborIndex(cornerIndex, true, polygons[polygonIndex], numPoints[polygonIndex]);
                int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygons[polygonIndex], numPoints[polygonIndex]);
                int prevIndex = FindValidNeighborIndex(cornerIndex, false, polygons[polygonIndex], numPoints[polygonIndex]);
                int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygons[polygonIndex], numPoints[polygonIndex]);
                // if the polygon has been reduced to 2 points, then we're going to delete it
                if (nextnextIndex == prevIndex || nextIndex == prevprevIndex) // then reduced to two points.
                {
                    polygons[polygonIndex][nextIndex] = Vector2.Null;
                    polygons[polygonIndex][nextnextIndex] = Vector2.Null;
                    numToRemove -= 2;
                }
                var polygonStartIndex = index - cornerIndex;
                // like the AddCrossProductToQueue function used above, we need a global index from stringing together all the polygons.
                // So, polygonStartIndex is used to find the start of this particular polygon's index and then add prevIndex and nextIndex to it.
                UpdateCrossProductInQueue(polygons[polygonIndex][prevprevIndex], polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex],
                    cornerQueue, crossProductsArray, polygonStartIndex + prevIndex);
                UpdateCrossProductInQueue(polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex], polygons[polygonIndex][nextnextIndex],
                    cornerQueue, crossProductsArray, polygonStartIndex + nextIndex);
            }

            var result = new List<List<Vector2>>();
            foreach (var polygon in polygons)
            {
                var resultPolygon = new List<Vector2>();
                foreach (var corner in polygon)
                    if (!corner.IsNull()) resultPolygon.Add(corner);
                if (resultPolygon.Count > 2)
                    result.Add(resultPolygon);
            }
            return result;
        }

        private static int FindValidNeighborIndex(int index, bool forward, IList<Vector2> polygon, int numPoints)
        {
            int increment = forward ? 1 : -1;
            var hitLimit = false;
            do
            {
                index += increment;
                if (index < 0)
                {
                    index = numPoints - 1;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
                else if (index == numPoints)
                {
                    index = 0;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
            }
            while (polygon[index].IsNull());
            return index;
        }

        private class ReverseSort : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                if (x == y) return 0;
                if (x < y) return 1;
                return -1;
            }
        }
        private class ForwardSort : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                if (x == y) return 0;
                if (x < y) return -1;
                return 1;
            }
        }
        private class AbsoluteValueSort : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                if (Math.Abs(x) == Math.Abs(y)) return 0;
                if (Math.Abs(x) < Math.Abs(y)) return -1;
                return 1;
            }
        }

        private static void AddCrossProductToOneOfTheLists(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> convexCornerQueue, SimplePriorityQueue<int, double> concaveCornerQueue,
            double[] crossProducts, int index)
        {
            var cross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = cross;
            if (cross < 0) concaveCornerQueue.Enqueue(index, (float)cross);
            else convexCornerQueue.Enqueue(index, (float)cross);
        }
        private static void UpdateCrossProductInQueues(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> convexCornerQueue, SimplePriorityQueue<int, double> concaveCornerQueue,
            double[] crossProducts, int index)
        {
            var oldCross = crossProducts[index];
            var newCross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = newCross;
            if (newCross < 0)
            {
                if (oldCross < 0)
                    concaveCornerQueue.UpdatePriority(index, newCross);
                else //then it used to be positive and needs to be removed from the convexCornerQueue
                {
                    convexCornerQueue.Remove(index);
                    concaveCornerQueue.Enqueue(index, newCross);
                }
            }
            // else newCross is positive and should be on the convexCornerQueue
            else if (oldCross >= 0)
                convexCornerQueue.UpdatePriority(index, newCross);
            else //then it used to be negative and needs to be removed from the concaveCornerQueue
            {
                concaveCornerQueue.Remove(index);
                convexCornerQueue.Enqueue(index, newCross);
            }
        }

        private static void AddCrossProductToQueue(Vector2 fromPoint, Vector2 currentPoint,
            Vector2 nextPoint, SimplePriorityQueue<int, double> cornnerQueue,
            double[] crossProducts, int index)
        {
            var cross = Math.Abs((currentPoint - fromPoint).Cross(nextPoint - currentPoint));
            crossProducts[index] = cross;
            cornnerQueue.Enqueue(index, cross);
        }

        private static void UpdateCrossProductInQueue(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> cornerQueue, double[] crossProducts, int index)
        {
            var newCross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = newCross;
            cornerQueue.UpdatePriority(index, newCross);
        }
        #endregion

    }
}
