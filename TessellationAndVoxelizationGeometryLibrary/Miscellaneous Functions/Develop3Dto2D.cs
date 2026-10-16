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
    // Here are the set of misellaneous functions that optimize! find the closest point, the most
    // orthogonal direction, etc.
    /// <summary>
    /// Class MiscFunctions.
    /// </summary>
    public static class Develop3Dto2D
    {

        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cylinder cyl, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet)
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

                // now to set out the 4 corners of the polygon. The top and bottom loops may
                // not have points at the edges, so we will add them if they are missing
                Vector2 upperLeft;
                if (topLoop[^1].X.IsNegligible())
                { // unlike the ones below we keep this off the lists until the end. This 
                    // will actually be the beginning of the loop.
                    upperLeft = topLoop[^1];
                    topLoop.RemoveAt(topLoop.Count - 1);
                }
                else
                {
                    upperLeft = new Vector2(0, topLoop[^1].Y);
                }
                Vector2 upperRight;
                if (topLoop[0].X.IsPracticallySame(Math.Tau * cyl.Radius))
                    upperRight = topLoop[0];
                else
                {
                    upperRight = new Vector2(Math.Tau * cyl.Radius, topLoop[0].Y);
                    topLoop.Insert(0, upperRight);
                }
                Vector2 lowerLeft;
                if (btmLoop[0].X.IsNegligible())
                    lowerLeft = btmLoop[0];
                else
                {
                    lowerLeft = new Vector2(0, btmLoop[0].Y);
                    btmLoop.Insert(0, lowerLeft);
                }
                Vector2 lowerRight;
                if (btmLoop[^1].X.IsPracticallySame(Math.Tau * cyl.Radius))
                    lowerRight = btmLoop[^1];
                else
                {
                    lowerRight = new Vector2(Math.Tau * cyl.Radius, btmLoop[0].Y);
                    btmLoop.Add(lowerRight);
                }
                // I'm going to assume that the caller is okay with me adding extra points on either end.
                // The current use case is triangulation, so I want the edges to not have huge jumps in size
                var totalLengthSqdSum = 0.0;
                for (int j = 1; j < btmLoop.Count - 1; j++)
                    totalLengthSqdSum += (btmLoop[j - 1] - btmLoop[j]).LengthSquared();
                for (int j = 1; j < topLoop.Count - 1; j++)
                    totalLengthSqdSum += (topLoop[j - 1] - topLoop[j]).LengthSquared();
                var rmsLength = Math.Sqrt(totalLengthSqdSum / (btmLoop.Count + topLoop.Count - 2));
                var numPointsToAdd = (int)(Math.Max(upperRight.Y - lowerRight.Y,
                    upperLeft.Y - lowerLeft.Y) / rmsLength);
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
        /// <param name="bluntTheTip">If true, then two new vertices are near the tip at the previous specified spacing,
        /// which means that the resulting polygon will not reach the center.</param>
        /// <returns></returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            double newVertexSpacing = -1.0, bool bluntTheTip = true)
            => GetUnrolledPolygons(cone, pointSets, IsClosedSet, out _, newVertexSpacing, bluntTheTip);

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
        /// <param name="bluntTheTip">Whether to replace the cone apex with two nearby vertices.</param>
        /// <returns>The developed polygons and their enclosure/seam metadata.</returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            out Dictionary<Vector3, Vertex2D> pointToVertexMap, double newVertexSpacing = -1.0,
            bool bluntTheTip = true)
            => GetUnrolledPolygons(cone, pointSets, IsClosedSet, out pointToVertexMap, out _, newVertexSpacing,
                bluntTheTip);

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
        /// <param name="bluntTheTip">Whether to replace the cone apex with two nearby vertices.</param>
        /// <returns>The developed polygons and their enclosure/seam metadata.</returns>
        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            out Dictionary<Vector3, Vertex2D> pointToVertexMap, out Vector3 seamDirection,
            double newVertexSpacing = -1.0, bool bluntTheTip = true)
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
            seamDirection = FindBestConeSeamDirection(cone, pathData, transform2D, backTransform);

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
                    halfRepeatAngle, newVertexSpacing, bluntTheTip, out var numRepeats, out var indexMatchedToStart);
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
                    repeatAngle, halfRepeatAngle, newVertexSpacing, bluntTheTip, out var numRepeats, out var indexMatchedToStart);
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

        private static Vector3 FindBestConeSeamDirection(Cone cone, IReadOnlyList<ConePathData> paths,
            Matrix4x4 transform2D, Matrix4x4 backTransform)
        {
            const double angularTolerance = 1e-10;
            var center = cone.Apex.ConvertTo2DCoordinates(transform2D);
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
            // parameterless cone transform: the Atan2 branch cut lies opposite its default perpendicular X axis.
            if (vertexAngles.Count == 0)
                return -cone.Axis.GetPerpendicularDirection();

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
                return -cone.Axis.GetPerpendicularDirection();

            var seamDirection = new Vector3(Math.Cos(bestSeamAngle), Math.Sin(bestSeamAngle), 0)
                .TransformNoTranslate(backTransform);
            var axisDirection = cone.Axis.Normalize();
            seamDirection -= seamDirection.Dot(axisDirection) * axisDirection;
            return seamDirection.Normalize();
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

        private static Polygon MakeConePolygon(List<Vector2> innerLoop, List<Vector2> outerLoop, double repeatAngle,
            double halfRepeatAngle, double newVertexSpacing, bool bluntTheTip, out int numRepeats, out int indexMatchedToStart)
        {
            var cosGap = Math.Cos(halfRepeatAngle);
            var sinGap = Math.Sin(halfRepeatAngle);
            double rmsLength = (newVertexSpacing > 0) ? newVertexSpacing :
                GetRootMeanSquareEdgeLength(innerLoop, outerLoop);
            if (double.IsNaN(newVertexSpacing))
                newVertexSpacing = double.MaxValue;
            else if (newVertexSpacing <= 0)
                newVertexSpacing = rmsLength;
            double prevAngle;
            int i;
            if (innerLoop.Count == 0)
            {
                if (bluntTheTip)
                    innerLoop = [new Vector2(rmsLength*cosGap, rmsLength * sinGap),
                   new Vector2(rmsLength*cosGap, -rmsLength * sinGap) ];
                else innerLoop = [Vector2.Zero];
            }
            else
            {
                // btm loop goes CW, top loop goes CCW.
                prevAngle = Math.Atan2(innerLoop[^1].Y, innerLoop[^1].X);
                for (i = 0; i < innerLoop.Count; i++)
                {
                    var nextAngle = Math.Atan2(innerLoop[i].Y, innerLoop[i].X);
                    if (nextAngle > halfRepeatAngle && (prevAngle < halfRepeatAngle || prevAngle > nextAngle))
                        break;
                    prevAngle = nextAngle;
                }
                innerLoop.AddRange(innerLoop.Take(i));
                innerLoop.RemoveRange(0, i);
            }
            prevAngle = Math.Atan2(outerLoop[^1].Y, outerLoop[^1].X);
            for (i = 0; i < outerLoop.Count; i++)
            {
                var nextAngle = Math.Atan2(outerLoop[i].Y, outerLoop[i].X);
                if (nextAngle > -halfRepeatAngle && (prevAngle < -halfRepeatAngle || prevAngle > nextAngle))
                    break;
                prevAngle = nextAngle;
            }
            outerLoop.AddRange(outerLoop.Take(i));
            outerLoop.RemoveRange(0, i);
            // now to set out the 4 corners of the polygon. 
            Vector2 outerTop;
            if (Math.Atan2(outerLoop[^1].Y, outerLoop[^1].X).IsPracticallySame(halfRepeatAngle))
            { // unlike the ones below we keep this off the lists until the end. This 
              // will actually be the beginning of the loop.
                outerTop = outerLoop[^1];
                outerLoop.RemoveAt(outerLoop.Count - 1);
            }
            else
            {
                var radius = outerLoop[^1].Length();
                outerTop = new Vector2(radius * cosGap, radius * sinGap);
            }
            Vector2 innerTop;
            if (Math.Atan2(innerLoop[0].Y, innerLoop[0].X).IsPracticallySame(halfRepeatAngle))
            { // unlike the ones below we keep this off the lists until the end. This 
              // will actually be the beginning of the loop.
                innerTop = innerLoop[0];
            }
            else
            {
                var radius = innerLoop[0].Length();
                innerTop = new Vector2(radius * cosGap, radius * sinGap);
                innerLoop.Insert(0, innerTop);
            }
            Vector2 innerBtm;
            if (Math.Atan2(innerLoop[^1].Y, innerLoop[^1].X).IsPracticallySame(-halfRepeatAngle))
                innerBtm = innerLoop[^1];
            else
            {
                var radius = innerLoop[^1].Length();
                innerBtm = new Vector2(radius * cosGap, -radius * sinGap);
                innerLoop.Add(innerBtm);
            }
            Vector2 outerBtm;
            if (Math.Atan2(outerLoop[0].Y, outerLoop[0].X).IsPracticallySame(-halfRepeatAngle))
                outerBtm = outerLoop[0];
            else
            {
                var radius = outerLoop[0].Length();
                outerBtm = new Vector2(radius * cosGap, -radius * sinGap);
                outerLoop.Insert(0, outerBtm);
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
                polygonPoints.Add(new Vector2(outerBtm.X + j * stepSize * (innerBtm.X - outerBtm.X) / btmDistance,
                    outerBtm.Y + j * stepSize * (innerBtm.Y - outerBtm.Y) / btmDistance));
            }
            polygonPoints.AddRange(outerLoop);
            Presenter.ShowAndHang(polygonPoints);
            return new Polygon(polygonPoints, isClosed: true);
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
