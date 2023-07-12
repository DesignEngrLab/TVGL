/// modified from original source:  
namespace TVGL.PointCloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TVGL.ConvexHullDetails;

    public class KDTree<TPoint, TAccObject> where TPoint : IPoint
    {
        /// <summary>
        /// The number of points in the KDTree
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        public TPoint[] Points { get; }

        /// <summary>
        /// An array of accompanying objects that match one-to-one with the points.
        /// </summary>
        public TAccObject[] AccompanyingObjects { get; }

        /// <summary>
        /// Gets a <see cref="BinaryTreeNavigator{TPoint,TNode}"/> that allows for manual tree navigation,
        /// </summary>
        private BinaryTreeNavigator<TPoint, TAccObject> Navigator
            => new BinaryTreeNavigator<TPoint, TAccObject>(Points, AccompanyingObjects);

        readonly int Dimensions;
        readonly bool HasAccompanyingObjects;

        public KDTree(int dimensions, IList<TPoint> points, IList<TAccObject> accompanyingObjects)
        {
            HasAccompanyingObjects = accompanyingObjects != null;
            if (HasAccompanyingObjects && points.Count != accompanyingObjects.Count)
                throw new ArgumentException("The number of points and accompanying objects must be the same.");
            Dimensions = dimensions;
            Count = points.Count;

            // Calculate the number of nodes needed to contain the binary tree.
            // This is equivalent to finding the power of 2 greater than the number of points
            var elementCount = (int)Math.Pow(2, (int)(Math.Log(Count) / Math.Log(2)) + 1);
            Points = new TPoint[elementCount];
            if (HasAccompanyingObjects)
                AccompanyingObjects = new TAccObject[elementCount];
            GenerateTree(0, 0, points, accompanyingObjects);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions (usually 2 or 3 for geometry problems).</param>
        /// <param name="points">The points.</param>
        public KDTree(int dimensions, IList<TPoint> points) : this(dimensions, points, null) { }

        /// <summary>
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="numberToFind">The number to find.</param>
        public IEnumerable<TPoint> FindNearest(TPoint target, int numberToFind = -1)
        { return FindNearest(target, double.MaxValue, numberToFind); }

        /// <summary>       
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="radius">The maximum radius to search.</param>
        /// <param name="numberToFind">The number to find.</param>
        /// <returns>A list of (TPoint, TAccObject).</returns>
        public IEnumerable<TPoint> FindNearest(TPoint target, double radius, int numberToFind = -1)
        {
            var nearestNeighbors = numberToFind == -1
                ? new BoundedPriorityList<int, double>(this.Count)
                : new BoundedPriorityList<int, double>(numberToFind, true);
            SearchForNearestNeighbors(0, target, new HyperRect(this.Dimensions), 0,
                nearestNeighbors, radius);
            foreach (var item in nearestNeighbors)
                yield return Points[item];
        }
        /// <summary>
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="numberToFind">The number to find.</param>
        public IEnumerable<(TPoint, TAccObject)> FindNearestAndAccompanyingObject(TPoint target, int numberToFind = -1)
        { return FindNearestAndAccompanyingObject(target, double.MaxValue, numberToFind); }

        /// <summary>       
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="radius">The maximum radius to search.</param>
        /// <param name="numberToFind">The number to find.</param>
        /// <returns>A list of (TPoint, TAccObject).</returns>
        public IEnumerable<(TPoint, TAccObject)> FindNearestAndAccompanyingObject(TPoint target, double radius, int numberToFind = -1)
        {
            var nearestNeighbors = numberToFind == -1
                ? new BoundedPriorityList<int, double>(this.Count)
                : new BoundedPriorityList<int, double>(numberToFind, true);
            SearchForNearestNeighbors(0, target, new HyperRect(this.Dimensions), 0,
                nearestNeighbors, radius);
            foreach (var item in nearestNeighbors)
                yield return (Points[item], AccompanyingObjects[item]);
        }

        /// <summary>
        /// Grows a KD tree recursively via median splitting. We find the median by doing a full sort.
        /// </summary>
        /// <param name="index">The array index for the current node.</param>
        /// <param name="dim">The current splitting dimension.</param>
        /// <param name="points">The set of points remaining to be added to the kd-tree</param>
        /// <param name="nodes">The set of nodes RE</param>
        private void GenerateTree(int index, int dim, IList<TPoint> points, IList<TAccObject> nodes)
        {
            // note that the real median is sometimes the average if the number of points is even.
            // but here, we just take the lower of the two middle points
            var count = points.Count;
            var leftSideLength = count / 2;
            var medianPointValue = points.Select(p => p[dim]).NthOrderStatistic(leftSideLength);
            //is this a plus one or not?

            var leftPoints = new TPoint[leftSideLength];
            var leftNodes = HasAccompanyingObjects ? new TAccObject[leftSideLength] : Array.Empty<TAccObject>();
            var leftIndex = 0;
            var rightSideLength = count - leftSideLength - 1; // the minus one since the median is not included in either side
            var rightPoints = new TPoint[rightSideLength];
            var rightNodes = HasAccompanyingObjects ? new TAccObject[rightSideLength] : Array.Empty<TAccObject>();
            var rightIndex = 0;
            var medianPoints = new List<TPoint>();
            var medianNodes = new List<TAccObject>();
            for (int i = 0; i < points.Count; i++)
            {
                TPoint pt = points[i];
                TAccObject node = HasAccompanyingObjects ? nodes[i] : default(TAccObject);
                if (pt[dim] > medianPointValue)
                {
                    rightPoints[rightIndex] = pt;
                    if (HasAccompanyingObjects)
                        rightNodes[rightIndex] = node;
                    rightIndex++;
                }
                else if (pt[dim] < medianPointValue)
                {
                    leftPoints[leftIndex] = pt;
                    if (HasAccompanyingObjects)
                        leftNodes[leftIndex] = node;
                    leftIndex++;
                }
                else
                {
                    medianPoints.Add(pt);
                    if (HasAccompanyingObjects)
                        medianNodes.Add(node);
                }
            }
            // The target with the median value all the current dimension now becomes the value of the current tree node
            // The previous node becomes the parents of the current node.
            Points[index] = medianPoints[0];
            if (HasAccompanyingObjects)
                AccompanyingObjects[index] = medianNodes[0];
            // Split the remaining points that have the same value as the median into left and right arrays.
            for (int i = 1; i < medianPoints.Count; i++)
            {
                if (leftIndex < leftSideLength)
                {
                    leftPoints[leftIndex] = medianPoints[i];
                    if (HasAccompanyingObjects)
                        leftNodes[leftIndex] = medianNodes[i];
                    leftIndex++;
                }
                else
                {
                    rightPoints[rightIndex] = medianPoints[i];
                    if (HasAccompanyingObjects)
                        rightNodes[rightIndex] = medianNodes[i];
                    rightIndex++;
                }
            }
            // Recursion incoming! passing the left and right arrays for arguments.
            // The current node's left and right values become the "roots" for
            // each recursion call. We also forward cycle to the next dimension.
            var nextDim = (dim + 1) % this.Dimensions; // select next dimension

            // We only need to recurse if the target array contains more than one target
            // If the array has no points then the node stay a null value
            if (leftSideLength == 1)
            {
                this.Points[BinaryTreeNavigator<TPoint, TAccObject>.LeftChildIndex(index)] = leftPoints[0];
                if (HasAccompanyingObjects)
                    this.AccompanyingObjects[BinaryTreeNavigator<TPoint, TAccObject>.LeftChildIndex(index)] = leftNodes[0];
            }
            else if (leftSideLength > 1)
                this.GenerateTree(BinaryTreeNavigator<TPoint, TAccObject>.LeftChildIndex(index), nextDim, leftPoints, leftNodes);

            // Do the same for the right points
            if (rightSideLength == 1)
            {
                this.Points[BinaryTreeNavigator<TPoint, TAccObject>.RightChildIndex(index)] = rightPoints[0];
                if (HasAccompanyingObjects)
                    this.AccompanyingObjects[BinaryTreeNavigator<TPoint, TAccObject>.RightChildIndex(index)] = rightNodes[0];
            }
            else if (rightSideLength > 1)
                this.GenerateTree(BinaryTreeNavigator<TPoint, TAccObject>.RightChildIndex(index), nextDim, rightPoints, rightNodes);
        }

        /// <summary>
        /// A top-down recursive method to find the nearest numberToFind of a given target.
        /// </summary>
        /// <param name="nodeIndex">The index of the node for the current recursion branch.</param>
        /// <param name="target">The target whose numberToFind we are trying to find.</param>
        /// <param name="rect">The <see cref="HyperRect{T}"/> containing the possible nearest numberToFind.</param>
        /// <param name="dimension">The current splitting dimension for this recursion branch.</param>
        /// <param name="nearestNeighbors">The <see cref="BoundedPriorityList{TElement,TPriority}"/> containing the nearest numberToFind already discovered.</param>
        /// <param name="maxSearchRadiusSquared">The squared radius of the current largest distance to search from the <paramref name="target"/></param>
        private void SearchForNearestNeighbors(int nodeIndex, TPoint target, HyperRect rect, int dimension,
            BoundedPriorityList<int, double> nearestNeighbors, double maxSearchRadiusSquared)
        {
            if (this.Points.Length <= nodeIndex || nodeIndex < 0
                || this.Points[nodeIndex].IsNull())
                return;

            // Work out the current dimension
            var dim = dimension % this.Dimensions;

            // Split our hyper-rectangle into 2 sub rectangles along the current
            // node's target on the current dimension
            var leftRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
            leftRect.MaxPoint[dim] = this.Points[nodeIndex][dim];

            var rightRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
            rightRect.MinPoint[dim] = this.Points[nodeIndex][dim];

            // Determine which side the target resides in
            HyperRect nearerRect, furtherRect;
            int nearerNode, furtherNode;
            if (target[dim] <= Points[nodeIndex][dim])
            {
                nearerRect = leftRect;
                furtherRect = rightRect;
                nearerNode = BinaryTreeNavigator<TPoint, TAccObject>.LeftChildIndex(nodeIndex);
                furtherNode = BinaryTreeNavigator<TPoint, TAccObject>.RightChildIndex(nodeIndex);
            }
            else
            {
                nearerRect = rightRect;
                furtherRect = leftRect;
                nearerNode = BinaryTreeNavigator<TPoint, TAccObject>.RightChildIndex(nodeIndex);
                furtherNode = BinaryTreeNavigator<TPoint, TAccObject>.LeftChildIndex(nodeIndex);
            }
            // Move down into the nearer branch
            this.SearchForNearestNeighbors(nearerNode, target, nearerRect, dimension + 1,
                nearestNeighbors, maxSearchRadiusSquared);

            // Walk down into the further branch but only if our capacity hasn't been reached
            // OR if there's a region in the further rectangle that's closer to the target than our
            // current furtherest nearest neighbor
            var distanceSquaredToTarget = furtherRect.GetClosestPoint(target);

            if (distanceSquaredToTarget <= maxSearchRadiusSquared)
            {
                if (nearestNeighbors.IsFull)
                {
                    if (distanceSquaredToTarget < nearestNeighbors.MaxPriority)
                        SearchForNearestNeighbors(furtherNode, target, furtherRect, dimension + 1,
                             nearestNeighbors, maxSearchRadiusSquared);
                }
                else SearchForNearestNeighbors(furtherNode, target, furtherRect, dimension + 1,
                        nearestNeighbors, maxSearchRadiusSquared);
            }

            // Try to add the current node to our nearest numberToFind list
            distanceSquaredToTarget = this.Metric(this.Points[nodeIndex], target);
            if (distanceSquaredToTarget <= maxSearchRadiusSquared)
                nearestNeighbors.Add(nodeIndex, distanceSquaredToTarget);
        }

        private double Metric(TPoint rectPoint, TPoint target)
        {
            var sum = 0.0;
            for (int i = 0; i < Dimensions; i++)
            {
                var difference = rectPoint[i] - target[i];
                sum += difference * difference;
            }
            return sum;
        }
    }
}
