/// modified from original source:  
namespace TVGL.PointCloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public class KDTree
    {
        #region Straight Line Distance
        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector3> Create(IEnumerable<Vector3> points)
        { return new KDTree<Vector3>(3, points as IList<Vector3> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector2> Create(IEnumerable<Vector2> points)
        { return new KDTree<Vector2>(3, points as IList<Vector2> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex> Create(IEnumerable<Vertex> points)
        { return new KDTree<Vertex>(3, points as IList<Vertex> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex2D> Create(IEnumerable<Vertex2D> points)
        { return new KDTree<Vertex2D>(3, points as IList<Vertex2D> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<IPoint> Create(IEnumerable<IPoint> points)
        { return new KDTree<IPoint>(3, points as IList<IPoint> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<TPoint> Create<TPoint>(IEnumerable<TPoint> points) where TPoint : IPoint
        { return new KDTree<TPoint>(3, points as IList<TPoint> ?? points.ToList()); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector3, TAccObject> Create<TAccObject>(IEnumerable<Vector3> points, IList<TAccObject> accObjects)
        { return new KDTree<Vector3, TAccObject>(3, points as IList<Vector3> ?? points.ToList(), accObjects); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector2, TAccObject> Create<TAccObject>(IEnumerable<Vector2> points, IList<TAccObject> accObjects)
        { return new KDTree<Vector2, TAccObject>(3, points as IList<Vector2> ?? points.ToList(), accObjects); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex, TAccObject> Create<TAccObject>(IEnumerable<Vertex> points, IList<TAccObject> accObjects)
        { return new KDTree<Vertex, TAccObject>(3, points as IList<Vertex> ?? points.ToList(), accObjects); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex2D, TAccObject> Create<TAccObject>(IEnumerable<Vertex2D> points, IList<TAccObject> accObjects)
        { return new KDTree<Vertex2D, TAccObject>(3, points as IList<Vertex2D> ?? points.ToList(), accObjects); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<IPoint, TAccObject> Create<TAccObject>(IEnumerable<IPoint> points, IList<TAccObject> accObjects)
        { return new KDTree<IPoint, TAccObject>(3, points as IList<IPoint> ?? points.ToList(), accObjects); }

        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<TPoint, TAccObject> Create<TPoint, TAccObject>(IEnumerable<TPoint> points, IList<TAccObject> accObjects) where TPoint : IPoint
        { return new KDTree<TPoint, TAccObject>(3, points as IList<TPoint> ?? points.ToList(), accObjects); }
        #endregion

        #region Over Spherical Surface
        /// <summary>
        /// Creates the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector3> CreateSpherical(IEnumerable<Vector3> points)
        { return new KDTree<Vector3>(3, points as IList<Vector3> ?? points.ToList(), KDTree<Vector3>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector2> CreateSpherical(IEnumerable<Vector2> points)
        { return new KDTree<Vector2>(3, points as IList<Vector2> ?? points.ToList(), KDTree<Vector2>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex> CreateSpherical(IEnumerable<Vertex> points)
        { return new KDTree<Vertex>(3, points as IList<Vertex> ?? points.ToList(), KDTree<Vertex>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex2D> CreateSpherical(IEnumerable<Vertex2D> points)
        { return new KDTree<Vertex2D>(3, points as IList<Vertex2D> ?? points.ToList(), KDTree<Vertex2D>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<IPoint> CreateSpherical(IEnumerable<IPoint> points)
        { return new KDTree<IPoint>(3, points as IList<IPoint> ?? points.ToList(), KDTree<IPoint>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<TPoint> CreateSpherical<TPoint>(IEnumerable<TPoint> points) where TPoint : IPoint
        { return new KDTree<TPoint>(3, points as IList<TPoint> ?? points.ToList(), KDTree<TPoint>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector3, TAccObject> CreateSpherical<TAccObject>(IEnumerable<Vector3> points, IList<TAccObject> accObjects)
        { return new KDTree<Vector3, TAccObject>(3, points as IList<Vector3> ?? points.ToList(), accObjects, 
            KDTree<Vector3,TAccObject>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vector2, TAccObject> CreateSpherical<TAccObject>(IEnumerable<Vector2> points, IList<TAccObject> accObjects)
        { return new KDTree<Vector2, TAccObject>(3, points as IList<Vector2> ?? points.ToList(), accObjects,
            KDTree<Vector2, TAccObject>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex, TAccObject> CreateSpherical<TAccObject>(IEnumerable<Vertex> points, IList<TAccObject> accObjects)
        { return new KDTree<Vertex, TAccObject>(3, points as IList<Vertex> ?? points.ToList(), accObjects, KDTree<Vertex, TAccObject>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<Vertex2D, TAccObject> CreateSpherical<TAccObject>(IEnumerable<Vertex2D> points, IList<TAccObject> accObjects)
        { return new KDTree<Vertex2D, TAccObject>(3, points as IList<Vertex2D> ?? points.ToList(), accObjects, KDTree<Vertex2D, TAccObject>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<IPoint, TAccObject> CreateSpherical<TAccObject>(IEnumerable<IPoint> points, IList<TAccObject> accObjects)
        { return new KDTree<IPoint, TAccObject>(3, points as IList<IPoint> ?? points.ToList(), accObjects, KDTree<IPoint, TAccObject>.SphericalDistance); }

        /// <summary>
        /// CreateSphericals the KDTree for the list of points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A KDTree.</returns>
        public static KDTree<TPoint, TAccObject> CreateSpherical<TPoint, TAccObject>(IEnumerable<TPoint> points, IList<TAccObject> accObjects) where TPoint : IPoint
        { return new KDTree<TPoint, TAccObject>(3, points as IList<TPoint> ?? points.ToList(), accObjects, KDTree<TPoint, TAccObject>.SphericalDistance); }
        #endregion
    }
    public class KDTree<TPoint> where TPoint : IPoint
    {
        /// <summary>
        /// The number of points in the KDTree
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        private protected TPoint[] TreePoints { get; }

        public TPoint[] OriginalPoints { get; }

        /// <summary>
        /// Gets the tree size which is the next power of 2 above the number of points.
        /// </summary>
        private protected int TreeSize { get; init; }
        /// <summary>
        /// Gets the dimensions of the points, typically 2 or 3.
        /// </summary>
        private protected int Dimensions { get; init; }
        private Func<TPoint, TPoint, int, double> DistanceMetric { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="points">The points.</param>
        internal KDTree(int dimensions, IEnumerable<TPoint> points, Func<TPoint, TPoint, int, double> distanceMetric = null) : this(points, distanceMetric)
        {
            Dimensions = dimensions;
            GenerateTree(0, 0, OriginalPoints);
        }

        private protected KDTree(IEnumerable<TPoint> points, Func<TPoint, TPoint, int, double> distanceMetric)
        {
            if (distanceMetric == null)
                distanceMetric = (p1, p2, dim) => StraightLineDistanceSquared(p1, p2, dim);
            this.DistanceMetric = distanceMetric;
            if (points is ICollection<TPoint> pointCollection)
            {
                Count = pointCollection.Count;
                OriginalPoints = new TPoint[Count];
                var i = 0;
                foreach (var p in pointCollection)
                    OriginalPoints[i++] = p;
            }
            else
            {
                var i = 0;
                var listOfPoints = new List<TPoint>();
                foreach (var p in points)
                {
                    listOfPoints.Add(p);
                    i++;
                }
                OriginalPoints = listOfPoints.ToArray();
                Count = i;
            }
            var nullPoint = (TPoint)OriginalPoints[0].GetType().GetField("Null").GetValue(null);
            // Calculate the number of nodes needed to contain the binary tree.
            // This is equivalent to finding the power of 2 greater than the number of points
            TreeSize = (int)Math.Pow(2, (int)(Math.Log(Count) / Math.Log(2)) + 1);
            TreePoints = Enumerable.Repeat(nullPoint, TreeSize).ToArray();
        }

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
                nearestNeighbors, radius * radius);
            foreach (var item in nearestNeighbors)
                yield return TreePoints[item];
        }


        /// <summary>
        /// Grows a KD tree recursively via median splitting. We find the median by doing a full sort.
        /// </summary>
        /// <param name="index">The array index for the current node.</param>
        /// <param name="dim">The current splitting dimension.</param>
        /// <param name="points">The set of points remaining to be added to the kd-tree</param>
        /// <param name="nodes">The set of nodes RE</param>
        private void GenerateTree(int index, int dim, TPoint[] points)
        {
            // note that the real median is sometimes the average if the number of points is even.
            // but here, we just take the lower of the two middle points
            var count = points.Length;
            var leftSideLength = count / 2;
            var medianPointValue = points.Select(p => p[dim]).NthOrderStatistic(leftSideLength);
            //is this a plus one or not?

            var leftPoints = new TPoint[leftSideLength];
            var leftIndex = 0;
            var rightSideLength = count - leftSideLength - 1; // the minus one since the median is not included in either side
            var rightPoints = new TPoint[rightSideLength];
            var rightIndex = 0;
            var medianPoints = new List<TPoint>();
            for (int i = 0; i < points.Length; i++)
            {
                TPoint pt = points[i];
                if (pt[dim] > medianPointValue)
                {
                    rightPoints[rightIndex] = pt;
                    rightIndex++;
                }
                else if (pt[dim] < medianPointValue)
                {
                    leftPoints[leftIndex] = pt;
                    leftIndex++;
                }
                else
                {
                    medianPoints.Add(pt);
                }
            }
            // The target with the median value all the current dimension now becomes the value of the current tree node
            // The previous node becomes the parents of the current node.
            TreePoints[index] = medianPoints[0];
            // Split the remaining points that have the same value as the median into left and right arrays.
            for (int i = 1; i < medianPoints.Count; i++)
            {
                if (leftIndex < leftSideLength)
                {
                    leftPoints[leftIndex] = medianPoints[i];
                    leftIndex++;
                }
                else
                {
                    rightPoints[rightIndex] = medianPoints[i];
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
            }
            else if (leftSideLength > 1)
                GenerateTree(LeftChildIndex(index), nextDim, leftPoints);

            // Do the same for the right points
            if (rightSideLength == 1)
            {
                TreePoints[RightChildIndex(index)] = rightPoints[0];
            }
            else if (rightSideLength > 1)
                GenerateTree(RightChildIndex(index), nextDim, rightPoints);
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
        private protected void SearchForNearestNeighbors(int nodeIndex, TPoint target, HyperRect rect, int dimension,
              BoundedPriorityList<int, double> nearestNeighbors, double maxSearchRadiusSquared)
        {
            if (TreePoints.Length <= nodeIndex || nodeIndex < 0
                || TreePoints[nodeIndex].IsNull())
                return;

            // Work out the current dimension
            var dim = dimension % Dimensions;

            // Split our hyper-rectangle into 2 sub rectangles along the current
            // node's target on the current dimension
            var leftRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
            leftRect.MaxPoint[dim] = TreePoints[nodeIndex][dim];

            var rightRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
            rightRect.MinPoint[dim] = TreePoints[nodeIndex][dim];

            // Determine which side the target resides in
            HyperRect nearerRect, furtherRect;
            int nearerNode, furtherNode;
            if (target[dim] <= TreePoints[nodeIndex][dim])
            {
                nearerRect = leftRect;
                furtherRect = rightRect;
                nearerNode = LeftChildIndex(nodeIndex);
                furtherNode = RightChildIndex(nodeIndex);
            }
            else
            {
                nearerRect = rightRect;
                furtherRect = leftRect;
                nearerNode = RightChildIndex(nodeIndex);
                furtherNode = LeftChildIndex(nodeIndex);
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
            distanceSquaredToTarget = DistanceMetric(TreePoints[nodeIndex], target, Dimensions);
            if (distanceSquaredToTarget <= maxSearchRadiusSquared)
                nearestNeighbors.Add(nodeIndex, distanceSquaredToTarget);
        }

        public static double StraightLineDistanceSquared(TPoint p1, TPoint p2, int dim)
        {
            var sum = 0.0;
            for (int i = 0; i < dim; i++)
            {
                var difference = p1[i] - p2[i];
                sum += difference * difference;
            }
            return sum;
        }
        public static double SphericalDistance(TPoint p1, TPoint p2, int dim)
        {
            var dot = 0.0; //dot product between the two vectors: p1 and p2
            var r1 = 0.0; //squared magnitude of p1
            var r2 = 0.0; //squared magnitude of p2
            for (int i = 0; i < dim; i++)
            {
                r1 += p1[i] * p1[i];
                dot += p1[i] * p2[i];
                r2 += p2[i] * p2[i];
            }
            var rAvg = 0.5 * (r1 + r2); //average of the squared magnitudes
            return rAvg * Math.Acos(dot / rAvg);
        }

        /// <summary>
        /// Computes the index of the right child of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the right child.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static int RightChildIndex(int index)
        {
            return (2 * index) + 2;
        }

        /// <summary>
        /// Computes the index of the left child of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the left child.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static int LeftChildIndex(int index)
        {
            return (2 * index) + 1;
        }

        /// <summary>
        /// Computes the index of the parent of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the parent node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static int ParentIndex(int index)
        {
            return (index - 1) / 2;
        }
    }
}
