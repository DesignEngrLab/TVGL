// <copyright file="KDTree.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace TVGL.KDTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using TVGL.ConvexHull;

    public class KDTree<TPoint, TNode> where TPoint : IPoint
    {
        /// <summary>
        /// The number of points in the KDTree
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        public TPoint[] InternalPointArray { get; }

        /// <summary>
        /// The array in which the node objects are stored. There is a one-to-one correspondence with this array and the <see cref="InternalPointArray"/>.
        /// </summary>
        public TNode[] InternalNodeArray { get; }

        /// <summary>
        /// Gets a <see cref="BinaryTreeNavigator{TPoint,TNode}"/> that allows for manual tree navigation,
        /// </summary>
        private BinaryTreeNavigator<TPoint, TNode> Navigator
            => new BinaryTreeNavigator<TPoint, TNode>(this.InternalPointArray, this.InternalNodeArray);

        public int Dimensions { get; }

        public KDTree(int dimensions, IList<TPoint> points, IList<TNode> nodes)
        {
            this.Dimensions = dimensions;
            this.Count = points.Count;

            // Calculate the number of nodes needed to contain the binary tree.
            // This is equivalent to finding the power of 2 greater than the number of points
            var elementCount = (int)Math.Pow(2, (int)(Math.Log(Count) / Math.Log(2)) + 1);
            this.InternalPointArray = new TPoint[elementCount];
            this.InternalNodeArray = new TNode[elementCount];
            this.GenerateTree(0, 0, points, nodes);
        }

        /// <summary>
        /// Finds the nearest neighbors in the <see cref="KDTree{TPoint,TNode}"/> of the given <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point whose neighbors we search for.</param>
        /// <param name="neighbors">The number of neighbors to look for.</param>
        /// <returns>The</returns>
        public (TPoint, TNode)[] NearestNeighbors(TPoint point, int neighbors)
        {
            var nearestNeighborList = new BoundedPriorityList<int, double>(neighbors, true);
            var rect = HyperRect.Infinite(this.Dimensions, double.MaxValue, double.MinValue);
            this.SearchForNearestNeighbors(0, point, rect, 0, nearestNeighborList, double.MaxValue);

            return ToResultSet(nearestNeighborList);
        }

        /// <summary>
        /// Searches for the closest points in a hyper-sphere around the given center.
        /// </summary>
        /// <param name="center">The center of the hyper-sphere</param>
        /// <param name="radius">The radius of the hyper-sphere</param>
        /// <param name="neighbors">The number of neighbors to return.</param>
        /// <returns>The specified number of closest points in the hyper-sphere</returns>
        public (TPoint, TNode)[] RadialSearch(TPoint center, double radius, int neighbors = -1)
        {
            var nearestNeighbors = new BoundedPriorityList<int, double>(this.Count);
            if (neighbors == -1)
            {
                this.SearchForNearestNeighbors(
                    0,
                    center,
                    HyperRect.Infinite(this.Dimensions, double.MaxValue, double.MinValue),
                    0,
                    nearestNeighbors,
                    radius);
            }
            else
            {
                this.SearchForNearestNeighbors(
                    0,
                    center,
                    HyperRect.Infinite(this.Dimensions, double.MaxValue, double.MinValue),
                    0,
                    nearestNeighbors,
                    radius);
            }

            return ToResultSet(nearestNeighbors);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (TPoint, TNode)[] ToResultSet(BoundedPriorityList<int, double> list)
        {
            var array = new (TPoint, TNode)[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                array[i] = (InternalPointArray[list[i]], InternalNodeArray[list[i]]);
            }
            return array;
        }

        /// <summary>
        /// Grows a KD tree recursively via median splitting. We find the median by doing a full sort.
        /// </summary>
        /// <param name="index">The array index for the current node.</param>
        /// <param name="dim">The current splitting dimension.</param>
        /// <param name="points">The set of points remaining to be added to the kd-tree</param>
        /// <param name="nodes">The set of nodes RE</param>
        private void GenerateTree(int index, int dim, IList<TPoint> points, IList<TNode> nodes)
        {
            // note that the real median is sometimes the average if the number of points is even.
            // but here, we just take the lower of the two middle points
            var count = points.Count;
            var leftSideLength = count / 2;
            var medianPointValue = points.Select(p => p[dim]).NthOrderStatistic(leftSideLength + 1);
            //is this a plus one or not?

            var leftPoints = new TPoint[leftSideLength];
            var leftNodes = new TNode[leftSideLength];
            var leftIndex = 0;
            var rightSideLength = count - leftSideLength - 1; // the minus one since the median is not included in either side
            var rightPoints = new TPoint[rightSideLength];
            var rightNodes = new TNode[rightSideLength];
            var rightIndex = 0;
            var medianPoints = new List<TPoint>();
            var medianNodes = new List<TNode>();
            for (int i = 0; i < points.Count; i++)
            {
                TPoint pt = points[i];
                var node = nodes[i];
                if (pt[dim] > medianPointValue)
                {
                    rightPoints[rightIndex] = pt;
                    rightNodes[rightIndex] = node;
                    rightIndex++;
                }
                else if (pt[dim] < medianPointValue)
                {
                    leftPoints[leftIndex] = pt;
                    leftNodes[leftIndex] = node;
                    leftIndex++;
                }
                else
                {
                    medianPoints.Add(pt);
                    medianNodes.Add(node);
                }
            }
            // The point with the median value all the current dimension now becomes the value of the current tree node
            // The previous node becomes the parents of the current node.
            this.InternalPointArray[index] = medianPoints[0];
            this.InternalNodeArray[index] = medianNodes[0];
            // Split the remaining points that have the same value as the median into left and right arrays.
            for (int i = 1; i < medianPoints.Count; i++)
            {
                if (leftIndex < leftSideLength)
                {
                    leftPoints[leftIndex] = medianPoints[i];
                    leftNodes[leftIndex] = medianNodes[i];
                    leftIndex++;
                }
                else
                {
                    rightPoints[rightIndex] = medianPoints[i];
                    rightNodes[rightIndex] = medianNodes[i];
                    rightIndex++;
                }
            }
            // Recursion incoming! passing the left and right arrays for arguments.
            // The current node's left and right values become the "roots" for
            // each recursion call. We also forward cycle to the next dimension.
            var nextDim = (dim + 1) % this.Dimensions; // select next dimension

            // We only need to recurse if the point array contains more than one point
            // If the array has no points then the node stay a null value
            if (leftSideLength == 1)
            {
                this.InternalPointArray[BinaryTreeNavigator<TPoint, TNode>.LeftChildIndex(index)] = leftPoints[0];
                this.InternalNodeArray[BinaryTreeNavigator<TPoint, TNode>.LeftChildIndex(index)] = leftNodes[0];
            }
            else if (leftSideLength > 1)
                this.GenerateTree(BinaryTreeNavigator<TPoint, TNode>.LeftChildIndex(index), nextDim, leftPoints, leftNodes);

            // Do the same for the right points
            if (rightSideLength == 1)
            {
                this.InternalPointArray[BinaryTreeNavigator<TPoint, TNode>.RightChildIndex(index)] = rightPoints[0];
                this.InternalNodeArray[BinaryTreeNavigator<TPoint, TNode>.RightChildIndex(index)] = rightNodes[0];
            }
            else if (rightSideLength > 1)
                this.GenerateTree(BinaryTreeNavigator<TPoint, TNode>.RightChildIndex(index), nextDim, rightPoints, rightNodes);
        }

        /// <summary>
        /// A top-down recursive method to find the nearest neighbors of a given point.
        /// </summary>
        /// <param name="nodeIndex">The index of the node for the current recursion branch.</param>
        /// <param name="target">The point whose neighbors we are trying to find.</param>
        /// <param name="rect">The <see cref="HyperRect{T}"/> containing the possible nearest neighbors.</param>
        /// <param name="dimension">The current splitting dimension for this recursion branch.</param>
        /// <param name="nearestNeighbors">The <see cref="BoundedPriorityList{TElement,TPriority}"/> containing the nearest neighbors already discovered.</param>
        /// <param name="maxSearchRadiusSquared">The squared radius of the current largest distance to search from the <paramref name="target"/></param>
        private void SearchForNearestNeighbors(int nodeIndex, TPoint target, HyperRect rect, int dimension,
            BoundedPriorityList<int, double> nearestNeighbors, double maxSearchRadiusSquared)
        {
            if (this.InternalPointArray.Length <= nodeIndex || nodeIndex < 0
                || this.InternalPointArray[nodeIndex] == null)
                return;

            // Work out the current dimension
            var dim = dimension % this.Dimensions;

            // Split our hyper-rectangle into 2 sub rectangles along the current
            // node's point on the current dimension
            var leftRect = rect.Clone();
            leftRect.MaxPoint[dim] = this.InternalPointArray[nodeIndex][dim];

            var rightRect = rect.Clone();
            rightRect.MinPoint[dim] = this.InternalPointArray[nodeIndex][dim];

            // Determine which side the target resides in
            var compare = target[dim].CompareTo(this.InternalPointArray[nodeIndex][dim]);
            HyperRect nearerRect, furtherRect;
            int nearerNode, furtherNode;
            if (compare <= 0)
            {
                nearerRect = leftRect;
                furtherRect = rightRect;
                nearerNode = BinaryTreeNavigator<TPoint, TNode>.LeftChildIndex(nodeIndex);
                furtherNode = BinaryTreeNavigator<TPoint, TNode>.RightChildIndex(nodeIndex);
            }
            else
            {
                nearerRect = rightRect;
                furtherRect = leftRect;
                nearerNode = BinaryTreeNavigator<TPoint, TNode>.RightChildIndex(nodeIndex);
                furtherNode = BinaryTreeNavigator<TPoint, TNode>.LeftChildIndex(nodeIndex);
            }
            // Move down into the nearer branch
            this.SearchForNearestNeighbors(
                nearerNode,
                target,
                nearerRect,
                dimension + 1,
                nearestNeighbors,
                maxSearchRadiusSquared);

            // Walk down into the further branch but only if our capacity hasn't been reached
            // OR if there's a region in the further rectangle that's closer to the target than our
            // current furtherest nearest neighbor
            var closestPointInFurtherRect = furtherRect.GetClosestPoint(target, Dimensions);
            var distanceSquaredToTarget = this.Metric(closestPointInFurtherRect, target);

            if (distanceSquaredToTarget.CompareTo(maxSearchRadiusSquared) <= 0)
            {
                if (nearestNeighbors.IsFull)
                {
                    if (distanceSquaredToTarget.CompareTo(nearestNeighbors.MaxPriority) < 0)
                        SearchForNearestNeighbors(furtherNode, target, furtherRect, dimension + 1,
                             nearestNeighbors, maxSearchRadiusSquared);
                }
                else SearchForNearestNeighbors(furtherNode, target, furtherRect, dimension + 1,
                        nearestNeighbors, maxSearchRadiusSquared);
            }

            // Try to add the current node to our nearest neighbors list
            distanceSquaredToTarget = this.Metric(this.InternalPointArray[nodeIndex], target);
            if (distanceSquaredToTarget.CompareTo(maxSearchRadiusSquared) <= 0)
                nearestNeighbors.Add(nodeIndex, distanceSquaredToTarget);
        }

        private double Metric(double[] rectPoint, TPoint target)
        {
            var sum = 0.0;
            for (int i = 0; i < Dimensions; i++)
            {
                var difference = rectPoint[i] - target[i];
                sum += difference * difference;
            }
            return sum;
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
