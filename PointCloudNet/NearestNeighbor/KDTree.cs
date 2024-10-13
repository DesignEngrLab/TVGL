
using PointCloud.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace PointCloud;

public enum DistanceMetric
{
    StraightLine,
    Spherical
}
public class KDTree
{
    /// <summary>
    /// Creates the KDTree for the list of points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>A KDTree.</returns>
    public static KDTree<Vector2> Create(IEnumerable<Vector2> points, DistanceMetric distanceMetric = DistanceMetric.StraightLine)
    => new KDTree<Vector2>(points as IList<Vector2> ?? points.ToList(), distanceMetric, 2);

    /// <summary>
    /// Creates the KDTree for the list of points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>A KDTree.</returns>
    public static KDTree<Vector3> Create(IEnumerable<Vector3> points, DistanceMetric distanceMetric = DistanceMetric.StraightLine)
    => new KDTree<Vector3>(points as IList<Vector3> ?? points.ToList(), distanceMetric, 3);


    /// <summary>
    /// Creates the KDTree for the list of points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>A KDTree.</returns>
    public static KDTree<Vector4> Create(IEnumerable<Vector4> points, DistanceMetric distanceMetric = DistanceMetric.StraightLine)
    => new KDTree<Vector4>(points as IList<Vector4> ?? points.ToList(), distanceMetric, 4);


    /// <summary>
    /// Creates the KDTree for the list of points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>A KDTree.</returns>
    public static KDTree<TPoint> Create<TPoint>(IEnumerable<TPoint> points, DistanceMetric distanceMetric = DistanceMetric.StraightLine)
        where TPoint : IConvexVertex, new()
    {
        var typeOfTPoint = typeof(TPoint);
        var dimensions = typeOfTPoint is IConvexVertex2D ? 2 : typeOfTPoint is IConvexVertex3D ? 3 : 4;
        return new KDTree<TPoint>(points as IList<TPoint> ?? points.ToList(), distanceMetric, dimensions);
    }

    /// <summary>
    /// Creates the KDTree for the list of points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>A KDTree.</returns>
    public static KDTree<TPoint, TAccObject> Create<TPoint, TAccObject>(IEnumerable<TPoint> points, IList<TAccObject> accObjects)
        where TPoint : IConvexVertex, new()
    {
        var typeOfTPoint = typeof(TPoint);
        var dimensions = typeOfTPoint is IConvexVertex2D ? 2 : typeOfTPoint is IConvexVertex3D ? 3 : 4;
        return new KDTree<TPoint, TAccObject>(dimensions, points as IList<TPoint> ?? points.ToList(), accObjects);
    }
}
public class KDTree<TPoint> //where TPoint : IConvexVertex
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

    private Func<TPoint, TPoint, double>? DistanceMetricFunc { get; }
    private protected Func<TPoint, int, double>? GetCoordFunc { get; }

    private protected readonly int Dimensions;
    readonly Type typeOfTPoint;
    readonly object defaultPoint;
    internal KDTree(IEnumerable<TPoint> points, DistanceMetric distanceMetric, int dimensions)
    {
        Dimensions = dimensions;
        if (points == null) throw new ArgumentNullException(nameof(points), "The list of points cannot be null.");
        typeOfTPoint = typeof(TPoint);
        defaultPoint = GetDefault(typeOfTPoint);

        DistanceMetricFunc = DetermineDistanceMetric(typeOfTPoint, distanceMetric);
        GetCoordFunc = DetermineGetCoord(typeOfTPoint);

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
        // Calculate the number of nodes needed to contain the binary tree.
        // This is equivalent to finding the power of 2 greater than the number of points
        TreeSize = (int)Math.Pow(2, (int)(Math.Log(Count) / Math.Log(2)) + 1);
        TreePoints = Enumerable.Repeat(defaultPoint, TreeSize).Cast<TPoint>().ToArray();
        GenerateTree(0, 0, OriginalPoints);
    }

    private Func<TPoint, TPoint, double>? DetermineDistanceMetric(Type typeOfTPoint, DistanceMetric distanceMetric)
    {
        if (distanceMetric == DistanceMetric.StraightLine)
        {
            if (typeOfTPoint is IConvexVertex2D)
                return (a, b) => StraightLineDistanceSquared((IConvexVertex2D)a, (IConvexVertex2D)b);
            if (typeOfTPoint is IConvexVertex3D)
                return (a, b) => StraightLineDistanceSquared((IConvexVertex3D)a, (IConvexVertex3D)b);
            if (typeOfTPoint is IConvexVertex3D)
                return (a, b) => StraightLineDistanceSquared((IConvexVertex4D)a, (IConvexVertex4D)b);
        }
        else
        {
            if (typeOfTPoint is IConvexVertex2D)
                return (a, b) => SphericalDistance((IConvexVertex2D)a, (IConvexVertex2D)b);
            if (typeOfTPoint is IConvexVertex3D)
                return (a, b) => SphericalDistance((IConvexVertex3D)a, (IConvexVertex3D)b);
            if (typeOfTPoint is IConvexVertex4D)
                return (a, b) => SphericalDistance((IConvexVertex4D)a, (IConvexVertex4D)b);
        }
        throw new ArgumentException("The type of TPoint is not supported for the given distance metric.");
    }

    private Func<TPoint, int, double>? DetermineGetCoord(Type typeOfTPoint)
    {
        //if (typeOfTPoint is Vector2)
        //    return new Func<Vector2, int, double>(GetCoord);
        //    //return (Vector2 a, int b) => GetCoord((Vector2)a, b);
        //if (typeOfTPoint is Vector3)
        //    return (a, b) => GetCoord((Vector3)a, b);
        //if (typeOfTPoint is Vector4)
        //    return (a, b) => GetCoord((Vector4)a, b);
        if (typeOfTPoint is IConvexVertex2D)
            return (a, b) => GetCoord((IConvexVertex2D)a, b);
        if (typeOfTPoint is IConvexVertex3D)
            return (a, b) => GetCoord((IConvexVertex3D)a, b);
        if (typeOfTPoint is IConvexVertex4D)
            return (a, b) => GetCoord((IConvexVertex4D)a, b);
        throw new ArgumentException("The type of TPoint is not supported for the given distance metric.");
    }

    internal static double GetCoord(IConvexVertex2D vector, int index)
        => GetCoord(vector.Coordinates, index);
    internal static double GetCoord(IConvexVertex3D vector, int index)
        => GetCoord(vector.Coordinates, index);
    internal static double GetCoord(IConvexVertex4D vector, int index)
        => GetCoord(vector.Coordinates, index);

    internal static double GetCoord(Vector2 vector, int index)
    {
        if (index == 0)
            return vector.X;
        return vector.Y;
    }

    internal static double GetCoord(Vector3 vector, int index)
    {
        if (index == 0)
            return vector.X;
        if (index == 1)
            return vector.Y;
        return vector.Z;
    }

    internal static double GetCoord(Vector4 vector, int index)
    {
        if (index == 0)
            return vector.X;
        if (index == 1)
            return vector.Y;
        if (index == 2)
            return vector.Z;
        return vector.W;
    }


    public static object GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
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
        var medianPointValue = points.Select(p => GetCoordFunc(p, dim)).NthOrderStatistic(leftSideLength);
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
            if (GetCoordFunc(pt, dim) > medianPointValue)
            {
                rightPoints[rightIndex] = pt;
                rightIndex++;
            }
            else if (GetCoordFunc(pt, dim) < medianPointValue)
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
            || TreePoints[nodeIndex].Equals(defaultPoint))
            return;

        // Work out the current dimension
        var dim = dimension % Dimensions;

        // Split our hyper-rectangle into 2 sub rectangles along the current
        // node's target on the current dimension
        var leftRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
        leftRect.MaxPoint[dim] = GetCoordFunc(TreePoints[nodeIndex], dim);

        var rightRect = new HyperRect(rect.MinPoint, rect.MaxPoint);
        rightRect.MinPoint[dim] = GetCoordFunc(TreePoints[nodeIndex], dim);

        // Determine which side the target resides in
        HyperRect nearerRect, furtherRect;
        int nearerNode, furtherNode;
        if (GetCoordFunc(target, dim) <= GetCoordFunc(TreePoints[nodeIndex], dim))
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
        var distanceSquaredToTarget = GetClosestPoint(furtherRect, target);

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
        distanceSquaredToTarget = DistanceMetricFunc(TreePoints[nodeIndex], target);
        if (distanceSquaredToTarget <= maxSearchRadiusSquared)
            nearestNeighbors.Add(nodeIndex, distanceSquaredToTarget);
    }

    internal static double StraightLineDistanceSquared(IConvexVertex2D a, IConvexVertex2D b)
    => StraightLineDistanceSquared(a.Coordinates, b.Coordinates);
    public static double StraightLineDistanceSquared(Vector2 a, Vector2 b)
    {
        var d = a - b;
        return d.Dot(d);
    }
    internal static double StraightLineDistanceSquared(IConvexVertex3D a, IConvexVertex3D b)
    => StraightLineDistanceSquared(a.Coordinates, b.Coordinates);
    public static double StraightLineDistanceSquared(Vector3 a, Vector3 b)
    {
        var d = a - b;
        return d.Dot(d);
    }
    internal static double StraightLineDistanceSquared(IConvexVertex4D a, IConvexVertex4D b)
    => StraightLineDistanceSquared(a.Coordinates, b.Coordinates);
    public static double StraightLineDistanceSquared(Vector4 a, Vector4 b)
    {
        var d = a - b;
        return d.Dot(d);
    }

    internal static double SphericalDistance(IConvexVertex2D a, IConvexVertex2D b)
    => SphericalDistance(a.Coordinates, b.Coordinates);
    public static double SphericalDistance(Vector2 a, Vector2 b)
    {
        var dot = a.Dot(b); //dot product between the two vectors: a and b
        var r1 = a.Dot(a); //squared magnitude of a
        var r2 = b.Dot(b); //squared magnitude of b
        var rAvg = 0.5 * (r1 + r2); //average of the squared magnitudes
        return rAvg * Math.Acos(dot / rAvg);
    }

    internal static double SphericalDistance(IConvexVertex3D a, IConvexVertex3D b)
    => SphericalDistance(a.Coordinates, b.Coordinates);
    public static double SphericalDistance(Vector3 a, Vector3 b)
    {
        var dot = a.Dot(b); //dot product between the two vectors: a and b
        var r1 = a.Dot(a); //squared magnitude of a
        var r2 = b.Dot(b); //squared magnitude of b
        var rAvg = 0.5 * (r1 + r2); //average of the squared magnitudes
        return rAvg * Math.Acos(dot / rAvg);
    }

    internal static double SphericalDistance(IConvexVertex4D a, IConvexVertex4D b)
    => SphericalDistance(a.Coordinates, b.Coordinates);
    public static double SphericalDistance(Vector4 a, Vector4 b)
    {
        var dot = a.Dot(b); //dot product between the two vectors: a and b
        var r1 = a.Dot(a); //squared magnitude of a
        var r2 = b.Dot(b); //squared magnitude of b
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



    /// <summary>
    /// Gets the point on the rectangle that is closest to the given point.
    /// If the point is within the rectangle, then the input point is the same as the
    /// output point.f the point is outside the rectangle then the point on the rectangle
    /// that is closest to the given point is returned.
    /// </summary>
    /// <param name="targetPoint">We try to find a point in or on the rectangle closest to this point.</param>
    /// <returns>The point on or in the rectangle that is closest to the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double GetClosestPoint(HyperRect hyperRect, TPoint targetPoint)
    {
        var distSqd = 0.0;
        for (var i = 0; i < Dimensions; i++)
        {
            var targetPointCoord = GetCoordFunc(targetPoint, i);
            if (hyperRect.MinPoint[i] > targetPointCoord)
                distSqd += (hyperRect.MinPoint[i] - targetPointCoord) * (hyperRect.MinPoint[i] - targetPointCoord);
            else if (hyperRect.MaxPoint[i] < targetPointCoord)
                distSqd += (hyperRect.MaxPoint[i] - targetPointCoord) * (hyperRect.MaxPoint[i] - targetPointCoord);
            // else Point is within rectangle, and the distance should not increase
        }
        return distSqd;
    }
}
