// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Proximity.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Options shared by plane, cylinder, and cone development.
    /// </summary>
    public sealed class DevelopmentOptions
    {
        public double NewVertexSpacing { get; init; } = -1.0;
        public bool SelectBestSeam { get; init; }
    }

    /// <summary>
    /// A developed polygon together with the information needed to restore it to 3D.
    /// </summary>
    public sealed class DevelopedPolygon
    {
        internal DevelopedPolygon(Polygon polygon, bool isProperEnclosure, int numDuplicates,
            int indexWhereMatchingStarts, IReadOnlyDictionary<Vector3, Vertex2D> pointToVertexMap)
        {
            Polygon = polygon;
            IsProperEnclosure = isProperEnclosure;
            NumDuplicates = numDuplicates;
            IndexWhereMatchingStarts = indexWhereMatchingStarts;
            PointToVertexMap = pointToVertexMap;
        }

        public Polygon Polygon { get; }
        public bool IsProperEnclosure { get; }
        public int NumDuplicates { get; }
        public int IndexWhereMatchingStarts { get; }
        public IReadOnlyDictionary<Vector3, Vertex2D> PointToVertexMap { get; }
    }

    // Here are the set of misellaneous functions that optimize! find the closest point, the most
    // orthogonal direction, etc.
    /// <summary>
    /// Class MiscFunctions.
    /// </summary>
    public static class Develop3Dto2D
    {
        /// <summary>
        /// Develops paths on a plane, cylinder, or cone using one common result contract.
        /// </summary>
        public static IReadOnlyList<DevelopedPolygon> DevelopPolygons(this PrimitiveSurface surface,
            IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> isClosedSet,
            DevelopmentOptions options = null)
        {
            options ??= new DevelopmentOptions();
            var paths = pointSets.ToList();
            var closed = isClosedSet.ToList();
            while (closed.Count < paths.Count)
                closed.Add(false);

            List<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)> results;
            Dictionary<Vector3, Vertex2D> pointToVertexMap;
            switch (surface)
            {
                case Plane plane:
                    var loops = paths.Select(path => (IList<Vector2>)plane.TransformFrom3DTo2D(path).ToList())
                        .ToList();
                    if (loops.Count == 0)
                        return Array.Empty<DevelopedPolygon>();
                    var planePolygon = new Polygon(loops[0], isClosed: closed[0]);
                    if (closed[0])
                        foreach (var loop in loops.Skip(1))
                            planePolygon.AddInnerPolygon(new Polygon(loop, isClosed: true));
                    results = [(planePolygon, planePolygon.IsClosed, 0, -1)];
                    pointToVertexMap = CreatePointMappingsByInverse(plane, paths, results.Select(r => r.polygon));
                    break;

                case Cylinder cylinder:
                    if (options.SelectBestSeam)
                        cylinder.PerpXDir = FindBestRotationalSeamDirection(cylinder.Axis, cylinder.Anchor,
                            paths, closed);
                    results = cylinder.GetUnrolledPolygons(paths, closed, options.NewVertexSpacing).ToList();
                    pointToVertexMap = CreatePointMappingsByInverse(cylinder, paths,
                        results.Select(r => r.polygon));
                    break;

                case Cone cone:
                    results = cone.GetUnrolledPolygons(paths, closed, out pointToVertexMap, out _,
                        options.NewVertexSpacing, options.SelectBestSeam).ToList();
                    break;

                default:
                    throw new NotSupportedException($"{surface.GetType().Name} cannot be developed to polygons.");
            }

            var developed = new List<DevelopedPolygon>(results.Count);
            foreach (var result in results)
            {
                var ownedVertices = result.polygon.AllPolygons.SelectMany(polygon => polygon.Vertices).ToHashSet();
                var resultMap = pointToVertexMap
                    .Where(pair => ownedVertices.Contains(pair.Value))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                developed.Add(new DevelopedPolygon(result.polygon, result.properEnclosure, result.numRepeats,
                    result.indexMatchedToStart, resultMap));
            }
            return developed;
        }

        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cylinder cyl, IEnumerable<IList<Vector3>> pointSets,
            IEnumerable<bool> IsClosedSet, double newVertexSpacing = -1.0)
        {
            var polyToIndices = new Dictionary<Polygon, (int numRepeats, int indexMatchedToStart)>();
            var encirclingLoops = new SortedList<double, List<Vector2>>();
            var isClosedEnumerator = IsClosedSet.GetEnumerator();
            var circumference = Math.Tau * cyl.Radius;
            var transform2D = cyl.Axis.TransformToXYPlane(out _);
            foreach (var pointList in pointSets)
            {
                var isClosed = isClosedEnumerator.MoveNext() ? isClosedEnumerator.Current : false;
                var pointList2D = cyl.TransformFrom3DTo2D(pointList).ToList();
                // when the points are a closed path and they encircle the axis
                var windingAngle = MiscFunctions.FindWindingAroundAxis(pointList, transform2D,
                    cyl.Anchor, out _, out _, isClosed);
                var encirclesAxis = isClosed && Math.Abs(windingAngle) > 1.67 * Math.PI;
                // there are 3 types of results:
                // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                // 2. a closed path that does encircle the axis - this will be an open polygon where the
                //    ends are offset by 2π*Radius in the x-direction
                // 3. an open path - this will be an open polygon
                if (encirclesAxis)
                    // store for later in a sorted list by the lower of the endpoint y-values
                    encirclingLoops.Add(Math.Min(pointList2D[0].Y, pointList2D[^1].Y), pointList2D);
                else if (isClosed)
                    // closed but not encircling. We will hold off returning in case this is a hole
                    // in another
                    polyToIndices.Add(new Polygon(pointList2D, isClosed: true), (0, -1));
                else // hopeless open path, so we will return it now
                    yield return (new Polygon(pointList2D, isClosed: false), false, 0, -1);
            }
            // now to handle the encircling loops.
            // if the first one is going the wrong way, then it is not a proper enclosure of the cylinder, and we will consider it an open polygon
            if (encirclingLoops.Count > 0 && encirclingLoops.Values[0][0].X > encirclingLoops.Values[0][^1].X)
            {
                yield return (new Polygon(encirclingLoops.Values[0], isClosed: true), false, 1, encirclingLoops.Values[0].Count - 1);
                encirclingLoops.RemoveAt(0);
            }
            // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
            for (int i = 1; i < encirclingLoops.Count; i += 2)
            {
                // ensure that both loops are between 0 and 2π*Radius
                var btmLoop = encirclingLoops.Values[i - 1];
                while (btmLoop[^1].X > circumference)
                {   // if the last point is greater than 2π*Radius, then we will remove it and
                    // insert a new point at the beginning that is 2π*Radius less than the last point
                    btmLoop.Insert(0, new Vector2(btmLoop[^1].X - circumference, btmLoop[^1].Y));
                    btmLoop.RemoveAt(btmLoop.Count - 1);
                }
                var topLoop = encirclingLoops.Values[i];
                while (topLoop[^1].X < 0)
                {   // if the last point is less than 0, then we will remove it and
                    // add a new point at the end that is 2π*Radius more than the last point
                    topLoop.Insert(0, new Vector2(topLoop[^1].X + circumference, topLoop[^1].Y));
                    topLoop.RemoveAt(topLoop.Count - 1);
                }

                // Intersect each loop's closing edge with the seam once, then use the same height on both
                // developed copies. The paired seam vertices must roll back to one physical 3D point.
                var lowerSeamHeight = GetCylinderSeamHeight(btmLoop, circumference, isIncreasing: true);
                var lowerLeft = new Vector2(0, lowerSeamHeight);
                var lowerRight = new Vector2(circumference, lowerSeamHeight);
                if (btmLoop[0].X.IsNegligible())
                    btmLoop[0] = lowerLeft;
                else
                    btmLoop.Insert(0, lowerLeft);
                if ((btmLoop[^1].X - circumference).IsNegligible())
                    btmLoop[^1] = lowerRight;
                else
                    btmLoop.Add(lowerRight);

                var upperSeamHeight = GetCylinderSeamHeight(topLoop, circumference, isIncreasing: false);
                var upperLeft = new Vector2(0, upperSeamHeight);
                var upperRight = new Vector2(circumference, upperSeamHeight);
                if ((topLoop[0].X - circumference).IsNegligible())
                    topLoop[0] = upperRight;
                else
                    topLoop.Insert(0, upperRight);
                if (topLoop[^1].X.IsNegligible())
                    topLoop.RemoveAt(topLoop.Count - 1);
                // I'm going to assume that the caller is okay with me adding extra points on either end.
                // The current use case is triangulation, so I want the edges to not have huge jumps in size
                var totalLengthSqdSum = 0.0;
                for (int j = 1; j < btmLoop.Count - 1; j++)
                    totalLengthSqdSum += (btmLoop[j - 1] - btmLoop[j]).LengthSquared();
                for (int j = 1; j < topLoop.Count - 1; j++)
                    totalLengthSqdSum += (topLoop[j - 1] - topLoop[j]).LengthSquared();
                var rmsLength = Math.Sqrt(totalLengthSqdSum / (btmLoop.Count + topLoop.Count - 2));
                var seamVertexSpacing = double.IsNaN(newVertexSpacing)
                    ? double.PositiveInfinity
                    : newVertexSpacing > 0
                        ? newVertexSpacing
                        : rmsLength;
                var numPointsToAdd = (int)(Math.Max(upperRight.Y - lowerRight.Y,
                    upperLeft.Y - lowerLeft.Y) / seamVertexSpacing);
                var polygonPoints = new List<Vector2> { upperLeft };
                var stepSize = (upperLeft.Y - lowerLeft.Y) / (1 + numPointsToAdd);
                for (int j = 1; j <= numPointsToAdd; j++)
                {
                    var y = upperLeft.Y - j * stepSize;
                    polygonPoints.Add(new Vector2(0, y));
                }
                var numRepeats = polygonPoints.Count + 1;
                polygonPoints.AddRange(btmLoop);
                var indexMatchedToStart = polygonPoints.Count;
                stepSize = (upperRight.Y - lowerRight.Y) / (1 + numPointsToAdd);
                for (int j = 1; j <= numPointsToAdd; j++)
                {
                    var y = lowerRight.Y + j * stepSize;
                    polygonPoints.Add(new Vector2(circumference, y));
                }
                polygonPoints.AddRange(topLoop);
                // Now we can create the polygon
                polyToIndices.Add(new Polygon(polygonPoints, isClosed: true), (numRepeats, indexMatchedToStart));
            }
            // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
            if (int.IsOddInteger(encirclingLoops.Count))
                yield return (new Polygon(encirclingLoops.Values[^1], isClosed: true), false, 0, -1);

            foreach (var poly in polyToIndices.Keys.CreateShallowPolygonTrees(false))
                yield return (poly, true, polyToIndices[poly].numRepeats, polyToIndices[poly].indexMatchedToStart);
        }

        private static double GetCylinderSeamHeight(IReadOnlyList<Vector2> loop, double circumference,
            bool isIncreasing)
        {
            var first = loop[0];
            var last = loop[^1];
            var equivalentFirst = new Vector2(
                first.X + (isIncreasing ? circumference : -circumference), first.Y);
            var seamX = isIncreasing ? circumference : 0.0;
            var deltaX = equivalentFirst.X - last.X;
            if (deltaX.IsNegligible())
                return 0.5 * (first.Y + last.Y);
            var fraction = (seamX - last.X) / deltaX;
            return last.Y + fraction * (equivalentFirst.Y - last.Y);
        }

        /// <summary>
        /// Create polygons that are unrolled version of the 3D polygons (pointSets) on the surface.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="pointSets"></param>
        /// <param name="IsClosedSet"></param>
        /// <param name="newVertexSpacing">New vertices are added at the seams. If newVertex is left unspecified,
        /// it's default will put vertices at the seams at the root-mean-square spacing of the current point Sets.
        /// If newVertexSpacing is a positive number then that will be used for the seam vertex space. If Nan is
        /// specified, the only the corners of seam are added.</param>
        /// <returns></returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            double newVertexSpacing = -1.0)
            => GetUnrolledPolygons(cone, pointSets, IsClosedSet, out _, newVertexSpacing);

        /// <summary>
        /// Create polygons that are unrolled versions of the 3D polygons on the cone and link each source 3D point
        /// to the actual <see cref="Vertex2D"/> retained by the resulting polygon.
        /// </summary>
        /// <param name="cone">The cone on which the point sets lie.</param>
        /// <param name="pointSets">The ordered 3D paths to develop.</param>
        /// <param name="IsClosedSet">Whether each corresponding point set is closed.</param>
        /// <param name="pointToVertexMap">
        /// Maps each source 3D point to its resulting polygon vertex. If polygon tolerance combines source points,
        /// each combined source point maps to the same retained vertex.
        /// </param>
        /// <param name="newVertexSpacing">Spacing for vertices added along the cone seam.</param>
        /// <returns>The developed polygons and their enclosure/seam metadata.</returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            out Dictionary<Vector3, Vertex2D> pointToVertexMap, double newVertexSpacing = -1.0)
            => GetUnrolledPolygons(cone, pointSets, IsClosedSet, out pointToVertexMap, out _, newVertexSpacing);

        /// <summary>
        /// Create polygons that are unrolled versions of the 3D polygons on the cone and return both the source
        /// point mapping and the seam direction needed to roll newly-created 2D vertices back onto the cone.
        /// </summary>
        /// <param name="cone">The cone on which the point sets lie.</param>
        /// <param name="pointSets">The ordered 3D paths to develop.</param>
        /// <param name="IsClosedSet">Whether each corresponding point set is closed.</param>
        /// <param name="pointToVertexMap">Maps each source 3D point to its retained polygon vertex.</param>
        /// <param name="seamDirection">The radial 3D direction at which the cone was cut.</param>
        /// <param name="newVertexSpacing">Spacing for vertices added along the cone seam.</param>
        /// <returns>The developed polygons and their enclosure/seam metadata.</returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            out Dictionary<Vector3, Vertex2D> pointToVertexMap, out Vector3 seamDirection,
            double newVertexSpacing = -1.0, bool selectBestSeam = true)
        {
            var repeatAngle = Math.Abs(cone.sinAperture) * Math.Tau;
            var halfRepeatAngle = 0.5 * repeatAngle;
            var polyToIndices = new Dictionary<Polygon, (int numRepeats, int indexMatchedToStart)>();
            var encirclingLoops = new SortedList<double, ConeLoopData>();
            var results = new List<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>();
            pointToVertexMap = new Dictionary<Vector3, Vertex2D>();
            var isClosedEnumerator = IsClosedSet.GetEnumerator();
            var transform2D = cone.Axis.TransformToXYPlane(out var backTransform);
            var pathData = new List<ConePathData>();

            foreach (var pointList in pointSets)
            {
                var isClosed = isClosedEnumerator.MoveNext() ? isClosedEnumerator.Current : false;
                var windingAngle = MiscFunctions.FindWindingAroundAxis(pointList, transform2D, cone.Apex, out var minAngle,
                    out var maxAngle, isClosed);
                pathData.Add(new ConePathData(pointList, isClosed, windingAngle, minAngle, maxAngle));
            }

            // Choose one seam for the complete set of paths. This is important for outer loops and their holes:
            // optimizing each loop independently would put them into incompatible developed coordinate systems.
            seamDirection = selectBestSeam
                ? FindBestRotationalSeamDirection(cone.Axis, cone.Apex, pathData, transform2D, backTransform)
                : cone.PerpXDir;
            cone.PerpXDir = seamDirection;

            foreach (var path in pathData)
            {
                var pointList = path.Points;
                var isClosed = path.IsClosed;
                var windingAngle = path.WindingAngle;
                // when the points are a closed path and they encircle the axis
                var encirclesAxis = isClosed && windingAngle > 1.67 * Math.PI;
                // there are 3 types of results:
                // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                // 2. a closed path that does encircle the axis - this will be an open polygon where the
                //    ends are offset by 2π*Radius in the x-direction
                // 3. an open path - this will be an open polygon
                var transformedPoints = cone.TransformFrom3DTo2D(pointList, seamDirection).ToList();
                if (encirclesAxis)
                    transformedPoints = MoveIntoConeSector(transformedPoints, repeatAngle, halfRepeatAngle);
                var sourcePointMappings = pointList.Zip(transformedPoints,
                    (point3D, point2D) => (point3D, point2D)).ToList();
                IEnumerable<Vector2> points2D;
                if (cone.IsPositive.GetValueOrDefault(true))
                    points2D = transformedPoints.AsEnumerable().Reverse();
                // note that points2D will need to be reversed when positive. The problem is that this transform
                // converts the axis down the cone to the positive, thus reversing the direction
                else points2D = transformedPoints;
                if (encirclesAxis)
                {
                    var pointList2D = points2D.ToList();
                    // store for later. use distance from the apex (length in 2D) to sort loops along the cone
                    encirclingLoops.Add(Math.Min(pointList2D[0].LengthSquared(), pointList2D[^1].LengthSquared()),
                        new ConeLoopData(pointList2D, windingAngle > 0, sourcePointMappings));
                }
                else if (isClosed) // simple closed shape is also stored for later polygon tree creation (not sorted)
                {
                    var polygon = new Polygon(points2D, isClosed: true);
                    AddPointMappings(pointToVertexMap, polygon, sourcePointMappings);
                    polyToIndices.Add(polygon, (0, -1));
                }
                else // hopeless open path, just return it as is. It will be open in the 2D plane too.
                {
                    var polygon = new Polygon(points2D, isClosed: false);
                    AddPointMappings(pointToVertexMap, polygon, sourcePointMappings);
                    results.Add((polygon, false, 0, -1));
                }
            }

            // now to handle the encircling loops. They are sorted by distance from the apex, so that the innermost loop is first.
            if (encirclingLoops.Count > 0 && encirclingLoops.Values[0].WindsPositive)
            {   // then loop is enclosing the apex of the cone 
                var loop = encirclingLoops.Values[0];
                Polygon p = MakeConePolygon(new List<Vector2>(), loop.Points2D, repeatAngle,
                    halfRepeatAngle, newVertexSpacing,
                    out var numRepeats, out var indexMatchedToStart);
                AddPointMappings(pointToVertexMap, p, loop.SourcePointMappings);
                polyToIndices.Add(p, (numRepeats, indexMatchedToStart));
                encirclingLoops.RemoveAt(0);
            }
            // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
            for (int i = 1; i < encirclingLoops.Count; i += 2)
            {
                var innerLoop = encirclingLoops.Values[i - 1];
                var outerLoop = encirclingLoops.Values[i];
                Polygon p = MakeConePolygon(innerLoop.Points2D, outerLoop.Points2D,
                    repeatAngle, halfRepeatAngle, newVertexSpacing,
                    out var numRepeats, out var indexMatchedToStart);
                AddPointMappings(pointToVertexMap, p,
                    innerLoop.SourcePointMappings.Concat(outerLoop.SourcePointMappings));
                polyToIndices.Add(p, (numRepeats, indexMatchedToStart));
            }
            // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
            if (int.IsOddInteger(encirclingLoops.Count))
            {
                var loop = encirclingLoops.Values[^1];
                var polygon = new Polygon(loop.Points2D, isClosed: true);
                AddPointMappings(pointToVertexMap, polygon, loop.SourcePointMappings);
                results.Add((polygon, false, 0, -1));
            }

            foreach (var poly in polyToIndices.Keys.CreateShallowPolygonTrees(false))
                results.Add((poly, true, polyToIndices[poly].numRepeats, polyToIndices[poly].indexMatchedToStart));
            //Presenter.ShowAndHang(results.Select(p=>p.polygon));
            return results;
        }

        private sealed class ConeLoopData
        {
            internal ConeLoopData(List<Vector2> points2D, bool windsPositive,
                List<(Vector3 point3D, Vector2 point2D)> sourcePointMappings)
            {
                Points2D = points2D;
                WindsPositive = windsPositive;
                SourcePointMappings = sourcePointMappings;
            }

            internal List<Vector2> Points2D { get; }
            internal bool WindsPositive { get; }
            internal List<(Vector3 point3D, Vector2 point2D)> SourcePointMappings { get; }
        }

        private readonly struct ConePathData
        {
            internal ConePathData(IList<Vector3> points, bool isClosed, double windingAngle, double minAngle,
                double maxAngle)
            {
                Points = points;
                IsClosed = isClosed;
                WindingAngle = windingAngle;
                MinAngle = minAngle;
                MaxAngle = maxAngle;
            }

            internal IList<Vector3> Points { get; }
            internal bool IsClosed { get; }
            internal double WindingAngle { get; }
            internal double MinAngle { get; }
            internal double MaxAngle { get; }
        }

        private static void AddPointMappings(Dictionary<Vector3, Vertex2D> pointToVertexMap, Polygon polygon,
            IEnumerable<(Vector3 point3D, Vector2 point2D)> sourcePointMappings)
        {
            if (polygon.Vertices.Count == 0)
                return;

            // Polygon rounds its input coordinates and may collapse adjacent points at that precision. Build the
            // lookup from the actual retained vertices so callers receive references owned by the returned polygon.
            var verticesByCoordinate = new Dictionary<Vector2, Vertex2D>();
            foreach (var vertex in polygon.Vertices)
                verticesByCoordinate.TryAdd(vertex.Coordinates, vertex);

            foreach (var (point3D, point2D) in sourcePointMappings)
            {
                var roundedPoint = new Vector2(Math.Round(point2D.X, polygon.NumSigDigits),
                    Math.Round(point2D.Y, polygon.NumSigDigits));
                if (!verticesByCoordinate.TryGetValue(roundedPoint, out var vertex))
                {
                    // This fallback covers a point removed as a near-duplicate at a polygon boundary. The closest
                    // retained vertex is the representative that Polygon kept for that source coordinate.
                    vertex = polygon.Vertices.MinBy(candidate =>
                        Vector2.DistanceSquared(candidate.Coordinates, roundedPoint));
                }

                // A Dictionary cannot represent multiple polygon occurrences of an identical 3D coordinate. Keep
                // the first occurrence, which is deterministic in the caller's point-set order.
                pointToVertexMap.TryAdd(point3D, vertex);
            }
        }

        private static Vector3 FindBestRotationalSeamDirection(Vector3 axis, Vector3 centerPoint,
            IReadOnlyList<IList<Vector3>> paths, IReadOnlyList<bool> isClosedSet)
        {
            var transform2D = axis.TransformToXYPlane(out var backTransform);
            var pathData = new List<ConePathData>(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var isClosed = i < isClosedSet.Count && isClosedSet[i];
                var windingAngle = MiscFunctions.FindWindingAroundAxis(paths[i], transform2D, centerPoint,
                    out var minAngle, out var maxAngle, isClosed);
                pathData.Add(new ConePathData(paths[i], isClosed, windingAngle, minAngle, maxAngle));
            }
            return FindBestRotationalSeamDirection(axis, centerPoint, pathData, transform2D, backTransform);
        }

        private static Vector3 FindBestRotationalSeamDirection(Vector3 axis, Vector3 centerPoint,
            IReadOnlyList<ConePathData> paths,
            Matrix4x4 transform2D, Matrix4x4 backTransform)
        {
            const double angularTolerance = 1e-10;
            var center = centerPoint.ConvertTo2DCoordinates(transform2D);
            var vertexAngles = new List<double>();

            foreach (var path in paths)
            {
                if (path.Points.Count == 0)
                    continue;

                var coordinates = path.Points.Select(point => point.ConvertTo2DCoordinates(transform2D)).ToList();
                var radiiSquared = coordinates.Select(point => Vector2.DistanceSquared(center, point)).ToList();
                var averageRadiusSquared = radiiSquared.Average();
                if (averageRadiusSquared <= 0 || double.IsNaN(averageRadiusSquared))
                    continue;

                // Match FindWindingAroundAxis's apex filter. Near the apex, an arbitrarily small positional change
                // can produce a very large azimuth change, so those points should not control the seam location.
                var minimumRadiusSquared = 0.005 * averageRadiusSquared;
                for (var i = 0; i < coordinates.Count; i++)
                {
                    if (radiiSquared[i] < minimumRadiusSquared)
                        continue;

                    var radialVector = coordinates[i] - center;
                    vertexAngles.Add(NormalizeAngle(Math.Atan2(radialVector.Y, radialVector.X)));
                }
            }

            // With no reliable radial points there is nothing to optimize. This is the same seam used by the
            // parameterless rotational transform.
            if (vertexAngles.Count == 0)
                return axis.GetPerpendicularDirection();

            vertexAngles.Sort();
            var bestNumberOfCutPaths = int.MaxValue;
            var bestGapWidth = double.NegativeInfinity;
            var bestSeamAngle = double.NaN;

            // The cut count is constant between consecutive vertex azimuths because each continuous path covers
            // the complete angular interval between its minimum and maximum unwrapped angles. Testing the midpoint
            // of every cyclic gap therefore finds the exact minimum while also keeping the seam away from vertices.
            for (var i = 0; i < vertexAngles.Count; i++)
            {
                var gapStart = vertexAngles[i];
                var gapEnd = i + 1 < vertexAngles.Count ? vertexAngles[i + 1] : vertexAngles[0] + Math.Tau;
                var gapWidth = gapEnd - gapStart;
                if (gapWidth <= angularTolerance)
                    continue;

                var candidateSeamAngle = NormalizeAngle(gapStart + 0.5 * gapWidth);
                var numberOfCutPaths = 0;
                foreach (var path in paths)
                    if (SeamCutsAngularPath(candidateSeamAngle, path.MinAngle, path.MaxAngle, angularTolerance))
                        numberOfCutPaths++;

                if (numberOfCutPaths < bestNumberOfCutPaths ||
                    (numberOfCutPaths == bestNumberOfCutPaths && gapWidth > bestGapWidth + angularTolerance))
                {
                    bestNumberOfCutPaths = numberOfCutPaths;
                    bestGapWidth = gapWidth;
                    bestSeamAngle = candidateSeamAngle;
                }
            }

            if (double.IsNaN(bestSeamAngle))
                return axis.GetPerpendicularDirection();

            var seamDirection = new Vector3(Math.Cos(bestSeamAngle), Math.Sin(bestSeamAngle), 0)
                .TransformNoTranslate(backTransform);
            var axisDirection = axis.Normalize();
            seamDirection -= seamDirection.Dot(axisDirection) * axisDirection;
            return seamDirection.Normalize();
        }

        private static Dictionary<Vector3, Vertex2D> CreatePointMappingsByInverse(PrimitiveSurface surface,
            IEnumerable<IList<Vector3>> pointSets, IEnumerable<Polygon> polygons)
        {
            var vertices = polygons.SelectMany(polygon => polygon.AllPolygons)
                .SelectMany(polygon => polygon.Vertices).Distinct().ToList();
            var pointToVertexMap = new Dictionary<Vector3, Vertex2D>();
            if (vertices.Count == 0)
                return pointToVertexMap;

            var restoredPoints = vertices.ToDictionary(vertex => vertex,
                vertex => surface.TransformFrom2DTo3D(vertex.Coordinates));
            foreach (var point in pointSets.SelectMany(path => path))
            {
                if (pointToVertexMap.ContainsKey(point))
                    continue;
                var vertex = vertices.MinBy(candidate =>
                    Vector3.DistanceSquared(restoredPoints[candidate], point));
                pointToVertexMap.Add(point, vertex);
            }
            return pointToVertexMap;
        }

        private static bool SeamCutsAngularPath(double seamAngle, double minAngle, double maxAngle,
            double angularTolerance)
        {
            // The physical seam has equivalent copies every 2*pi in the unwrapped angular coordinate. The path is
            // cut when at least one of those copies lies in its continuous [minAngle, maxAngle] angular range.
            var firstPossibleCopy = Math.Ceiling((minAngle - seamAngle - angularTolerance) / Math.Tau);
            var lastPossibleCopy = Math.Floor((maxAngle - seamAngle + angularTolerance) / Math.Tau);
            return firstPossibleCopy <= lastPossibleCopy;
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= Math.Tau;
            return angle < 0 ? angle + Math.Tau : angle;
        }

        private static List<Vector2> MoveIntoConeSector(IReadOnlyList<Vector2> loop, double repeatAngle,
            double halfRepeatAngle)
        {
            if (loop.Count == 0)
                return [];

            // TransformFrom3DTo2D keeps a path continuous by allowing its developed polar angle to move onto
            // adjacent copies of the cone sector. Cartesian coordinates hide whole 2*pi turns, so recover that
            // continuous angle before reducing it modulo repeatAngle. Reducing each raw Atan2 value independently
            // would choose the wrong sector copy after the path has crossed +/-pi.
            var result = new List<Vector2>(loop.Count);
            var previousAngle = Math.Atan2(loop[0].Y, loop[0].X);
            foreach (var point in loop)
            {
                var angle = Math.Atan2(point.Y, point.X);
                while (angle - previousAngle > Math.PI)
                    angle -= Math.Tau;
                while (previousAngle - angle > Math.PI)
                    angle += Math.Tau;

                previousAngle = angle;
                angle -= repeatAngle * Math.Floor((angle + halfRepeatAngle) / repeatAngle);
                result.Add(new Vector2(point.Length() * Math.Cos(angle), point.Length() * Math.Sin(angle)));
            }

            return result;
        }

        private static Polygon MakeConePolygon(List<Vector2> innerLoop, List<Vector2> outerLoop, double repeatAngle,
            double halfRepeatAngle, double newVertexSpacing,
            out int numRepeats, out int indexMatchedToStart)
        {
            var cosGap = Math.Cos(halfRepeatAngle);
            var sinGap = Math.Sin(halfRepeatAngle);
            double rmsLength = (newVertexSpacing > 0) ? newVertexSpacing :
                GetRootMeanSquareEdgeLength(innerLoop, outerLoop);
            if (double.IsNaN(newVertexSpacing))
                newVertexSpacing = double.MaxValue;
            else if (newVertexSpacing <= 0)
                newVertexSpacing = rmsLength;
            var innerLoopWasEmpty = innerLoop.Count == 0;
            if (innerLoopWasEmpty)
                innerLoop = [Vector2.Zero];
            else
            {
                ArrangeConeLoopAtSeam(innerLoop, shouldIncrease: false);
            }
            ArrangeConeLoopAtSeam(outerLoop, shouldIncrease: true);

            // The closing edge of each arranged loop is the edge cut by the seam. Intersect that edge with one
            // sector border and use the same radius for both copies. This creates true duplicate 3D points instead
            // of projecting the two neighboring samples independently onto the two borders.
            var outerSeamRadius = GetConeSeamRadius(outerLoop, repeatAngle, halfRepeatAngle, isIncreasing: true);
            var outerTop = new Vector2(outerSeamRadius * cosGap, outerSeamRadius * sinGap);
            var outerBtm = new Vector2(outerSeamRadius * cosGap, -outerSeamRadius * sinGap);
            outerLoop.Insert(0, outerBtm);

            Vector2 innerTop;
            Vector2 innerBtm;
            if (innerLoopWasEmpty)
            {
                innerTop = innerLoop[0];
                innerBtm = innerLoop[^1];
            }
            else
            {
                var innerSeamRadius = GetConeSeamRadius(innerLoop, repeatAngle, halfRepeatAngle,
                    isIncreasing: false);
                innerTop = new Vector2(innerSeamRadius * cosGap, innerSeamRadius * sinGap);
                innerBtm = new Vector2(innerSeamRadius * cosGap, -innerSeamRadius * sinGap);
                innerLoop.Insert(0, innerTop);
                innerLoop.Add(innerBtm);
            }

            var polygonPoints = new List<Vector2> { outerTop };
            var topDistance = outerTop.Distance(innerTop);
            var btmDistance = outerBtm.Distance(innerBtm);
            var numPointsToAdd = (int)Math.Ceiling(Math.Max(topDistance, btmDistance) / newVertexSpacing);
            var stepSize = topDistance / numPointsToAdd;
            for (int j = 1; j < numPointsToAdd; j++)
            {
                // make this a weighted average of the two points, so that the points are evenly spaced along the line between them
                polygonPoints.Add(new Vector2(outerTop.X + j * stepSize * (innerTop.X - outerTop.X) / topDistance,
                    outerTop.Y + j * stepSize * (innerTop.Y - outerTop.Y) / topDistance));
            }
            numRepeats = polygonPoints.Count + 1;
            polygonPoints.AddRange(innerLoop);
            indexMatchedToStart = polygonPoints.Count;
            stepSize = btmDistance / numPointsToAdd;
            for (int j = 1; j < numPointsToAdd; j++)
            {
                polygonPoints.Add(new Vector2(innerBtm.X + j * stepSize * (outerBtm.X - innerBtm.X) / btmDistance,
                    innerBtm.Y + j * stepSize * (outerBtm.Y - innerBtm.Y) / btmDistance));
            }
            polygonPoints.AddRange(outerLoop);
            //Presenter.ShowAndHang(new Polygon(polygonPoints, isClosed: true));
            return new Polygon(polygonPoints, isClosed: true);
        }

        private static void ArrangeConeLoopAtSeam(List<Vector2> loop, bool shouldIncrease)
        {
            if (loop.Count < 2)
                return;

            var crossingIndex = FindConeSectorWrap(loop, out var wrapDelta);
            var isIncreasing = wrapDelta < 0;
            if (isIncreasing != shouldIncrease)
            {
                loop.Reverse();
                crossingIndex = FindConeSectorWrap(loop, out _);
            }

            var firstIndex = (crossingIndex + 1) % loop.Count;
            if (firstIndex == 0)
                return;

            loop.AddRange(loop.Take(firstIndex));
            loop.RemoveRange(0, firstIndex);
        }

        private static int FindConeSectorWrap(IReadOnlyList<Vector2> loop, out double wrapDelta)
        {
            var crossingIndex = 0;
            wrapDelta = 0;
            var largestJump = double.NegativeInfinity;
            for (var i = 0; i < loop.Count; i++)
            {
                var next = (i + 1) % loop.Count;
                var currentAngle = Math.Atan2(loop[i].Y, loop[i].X);
                var nextAngle = Math.Atan2(loop[next].Y, loop[next].X);
                var delta = nextAngle - currentAngle;
                if (Math.Abs(delta) <= largestJump)
                    continue;

                largestJump = Math.Abs(delta);
                crossingIndex = i;
                wrapDelta = delta;
            }

            return crossingIndex;
        }

        private static double GetConeSeamRadius(IReadOnlyList<Vector2> loop, double repeatAngle,
            double halfRepeatAngle, bool isIncreasing)
        {
            var first = loop[0];
            var last = loop[^1];
            var firstAngle = Math.Atan2(first.Y, first.X) + (isIncreasing ? repeatAngle : -repeatAngle);
            var equivalentFirst = new Vector2(first.Length() * Math.Cos(firstAngle),
                first.Length() * Math.Sin(firstAngle));
            var seamAngle = isIncreasing ? halfRepeatAngle : -halfRepeatAngle;
            var seamDirection = new Vector2(Math.Cos(seamAngle), Math.Sin(seamAngle));
            var edge = equivalentFirst - last;
            var denominator = edge.Cross(seamDirection);
            if (denominator.IsNegligible())
                return 0.5 * (first.Length() + last.Length());

            var fraction = -last.Cross(seamDirection) / denominator;
            return (last + fraction * edge).Dot(seamDirection);
        }

        private static double GetRootMeanSquareEdgeLength(params IList<Vector2>[] loops)
        {
            var totalLengthSqdSum = 0.0;
            foreach (var loop in loops)
            {
                for (int j = 1; j < loop.Count - 1; j++)
                    totalLengthSqdSum += (loop[j - 1] - loop[j]).LengthSquared();
            }
            var rmsLength = Math.Sqrt(totalLengthSqdSum / (loops.Sum(l => l.Count) - loops.Length));
            return rmsLength;
        }

        /*
        // if the shape encircles the axis, then the first and last points should be the same in 3D, so we
        // add the first point again to close the path, BUT rotated by ±repeatAngle around the apex (the origin)
        // so that the shape can be unrolled into a flat sector.
        var totalAngle = 0.0;
        var prevAngle = Math.Atan2(pointList2D[0].Y, pointList2D[0].X);
            for (int i = 1; i<pointList2D.Count; i++)
            {
                var currentAngle = Math.Atan2(pointList2D[i].Y, pointList2D[i].X);
        var dTheta = currentAngle - prevAngle;
                while (dTheta > Math.PI) dTheta -= Math.Tau;
                while (dTheta< -Math.PI) dTheta += Math.Tau;
                totalAngle += dTheta;
                prevAngle = currentAngle;
            }
            var isPositive = totalAngle >= 0;
        var deltaAngle = isPositive ? repeatAngle : -repeatAngle;
        var cos = Math.Cos(deltaAngle);
        var sin = Math.Sin(deltaAngle);
        var p0 = pointList2D[0];
            pointList2D.Add(new Vector2(p0.X* cos - p0.Y* sin, p0.X* sin + p0.Y* cos));


            // now to handle the encircling loops
            var sortedLoops = encirclingLoops.OrderBy(l => l.distance).ToList();
            if (sortedLoops.Count > 0 && !sortedLoops[0].isPositive) // then loop is enclosing the apex of the cone and we consider it open
            {
                result.Add(new Polygon(sortedLoops[0].points, isClosed: true));
                // unlike cylinder, we do not consider a loop to be open if it
                // encloses the tip of the cone. Positive polygons would be okay, so set to false
                open.Add(!result[^1].IsPositive);
                sortedLoops.RemoveAt(0);
            }
for (int i = 1; i < sortedLoops.Count; i += 2)
{
    var loopA = sortedLoops[i - 1].points;
    var loopB = sortedLoops[i].points;
    if (sortedLoops[i].isPositive == sortedLoops[i - 1].isPositive)
        loopB = loopB.AsEnumerable().Reverse().ToList();

    var angleAEnd = Math.Atan2(loopA[^1].Y, loopA[^1].X);
    var angleBStart = Math.Atan2(loopB[0].Y, loopB[0].X);
    var angleDiff = angleAEnd - angleBStart;
    var k = (int)Math.Round(angleDiff / repeatAngle);
    if (k != 0)
    {
        var rot = k * repeatAngle;
        var c = Math.Cos(rot);
        var s = Math.Sin(rot);
        loopB = loopB.Select(p => new Vector2(p.X * c - p.Y * s, p.X * s + p.Y * c)).ToList();
    }
    polygons.Add(new Polygon(loopA.Concat(loopB), isClosed: true));
}
if (int.IsOddInteger(sortedLoops.Count))
{
    result.Add(new Polygon(sortedLoops[^1].points, isClosed: true));
    // open only is negative (see earlier comment at the beginning of the process
    open.Add(!result[^1].IsPositive);
}
foreach (var poly in polygons.CreateShallowPolygonTrees(false))
{
    result.Add(poly);
    open.Add(false);
}
//Presenter.ShowAndHang(result);
return result;
        }

*/
    }
}
