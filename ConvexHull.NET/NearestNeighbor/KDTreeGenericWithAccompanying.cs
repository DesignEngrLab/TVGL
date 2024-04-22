/// modified from original source:  
namespace TVGL.PointCloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class KDTree<TPoint, TAccObject> : KDTree<TPoint>
        where TPoint : IVector
    {
        /// <summary>
        /// An array of accompanying objects that match one-to-one with the points.
        /// </summary>
        public TAccObject[] AccompanyingObjects { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="points">The points.</param>
        /// <param name="accompanyingObjects">The accompanying objects.</param>
        internal KDTree(int dimensions, IEnumerable<TPoint> points, IList<TAccObject> accompanyingObjects, Func<IVector, IVector, int, double> distanceMetric = null)
            : base(points, distanceMetric)
        {
            this.Dimensions = dimensions;
            if (Count != accompanyingObjects.Count)
                throw new ArgumentException("The number of points and accompanying objects must be the same.");
            AccompanyingObjects = new TAccObject[TreeSize];
            GenerateTree(0, 0, OriginalPoints, accompanyingObjects);
        }

        /// <summary>
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="numberToFind">The number to find.</param>
        public new IEnumerable<(TPoint, TAccObject)> FindNearest(TPoint target, int numberToFind = -1)
        { return FindNearest(target, double.MaxValue, numberToFind); }

        /// <summary>       
        /// Finds the nearest set of points to the target.
        /// </summary>
        /// <param name="target">The target point.</param>
        /// <param name="radius">The maximum radius to search.</param>
        /// <param name="numberToFind">The number to find.</param>
        /// <returns>A list of (TPoint, TAccObject).</returns>
        public new IEnumerable<(TPoint, TAccObject)> FindNearest(IVector target, double radius, int numberToFind = -1)
        {
            var nearestNeighbors = numberToFind == -1
                ? new BoundedPriorityList<int, double>(this.Count)
                : new BoundedPriorityList<int, double>(numberToFind, true);
            SearchForNearestNeighbors(0, target, new HyperRect(this.Dimensions), 0,
                nearestNeighbors, radius * radius);
            foreach (var item in nearestNeighbors)
                yield return (TreePoints[item], AccompanyingObjects[item]);
        }

        /// <summary>
        /// Grows a KD tree recursively via median splitting. We find the median by doing a full sort.
        /// </summary>
        /// <param name="index">The array index for the current node.</param>
        /// <param name="dim">The current splitting dimension.</param>
        /// <param name="points">The set of points remaining to be added to the kd-tree</param>
        /// <param name="nodes">The set of nodes RE</param>
        private void GenerateTree(int index, int dim, TPoint[] points, IList<TAccObject> nodes)
        {
            // note that the real median is sometimes the average if the number of points is even.
            // but here, we just take the lower of the two middle points
            var count = points.Length;
            var leftSideLength = count / 2;
            var medianPointValue = points.Select(p => p[dim]).NthOrderStatistic(leftSideLength);
            //is this a plus one or not?

            var leftPoints = new TPoint[leftSideLength];
            var leftNodes = new TAccObject[leftSideLength];
            var leftIndex = 0;
            var rightSideLength = count - leftSideLength - 1; // the minus one since the median is not included in either side
            var rightPoints = new TPoint[rightSideLength];
            var rightNodes = new TAccObject[rightSideLength];
            var rightIndex = 0;
            var medianPoints = new List<TPoint>();
            var medianNodes = new List<TAccObject>();
            for (int i = 0; i < points.Length; i++)
            {
                TPoint pt = points[i];
                TAccObject node = nodes[i];
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
            // The target with the median value all the current dimension now becomes the value of the current tree node
            // The previous node becomes the parents of the current node.
            TreePoints[index] = medianPoints[0];
            AccompanyingObjects[index] = medianNodes[0];
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
            var nextDim = (dim + 1) % Dimensions; // select next dimension

            // We only need to recurse if the target array contains more than one target
            // If the array has no points then the node stay a null value
            if (leftSideLength == 1)
            {
                TreePoints[LeftChildIndex(index)] = leftPoints[0];
                AccompanyingObjects[LeftChildIndex(index)] = leftNodes[0];
            }
            else if (leftSideLength > 1)
                GenerateTree(LeftChildIndex(index), nextDim, leftPoints, leftNodes);

            // Do the same for the right points
            if (rightSideLength == 1)
            {
                TreePoints[RightChildIndex(index)] = rightPoints[0];
                AccompanyingObjects[RightChildIndex(index)] = rightNodes[0];
            }
            else if (rightSideLength > 1)
                GenerateTree(RightChildIndex(index), nextDim, rightPoints, rightNodes);
        }
    }
}
