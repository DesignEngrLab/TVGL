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
using System.Reflection;

namespace TVGL
{
    // Here are the set of misellaneous functions that optimize! find the closest point, the most
    // orthogonal direction, etc.
    /// <summary>
    /// Class MiscFunctions.
    /// </summary>
    public static class Develop3Dto2D
    {

        public static List<(Polygon polygon, int numRepeats, int indexMatchedToStart)> GetUnrolledPolygons
            (this Cylinder cyl, IEnumerable<Polygon3D> polyon3Ds, bool defineSeam,
            out Dictionary<Vector3, Vertex2D> vertexMapping, double newVertexSpacing = -1.0)
        {
            var listOf3DPolygons = polyon3Ds as IList<Polygon3D> ?? polyon3Ds.ToList();
            var results = new List<(Polygon polygon, int numRepeats, int indexMatchedToStart)>();
            vertexMapping = new Dictionary<Vector3, Vertex2D>();
            var polyToIndices = new Dictionary<Polygon, (int numRepeats, int indexMatchedToStart)>();
            var encirclingLoops = new SortedList<double, List<Vertex2D>>();
            var circumference = Math.Tau * cyl.Radius;
            var allPathsData = new List<PathWindingData>();
            var transform2D = cyl.Axis.TransformToXYPlane(out var backTransform);
            foreach (var poly3 in listOf3DPolygons)
            {
                bool? isClosed = null;
                foreach (var path in poly3.AllPaths())
                {
                    if (!isClosed.HasValue) isClosed = poly3.IsClosed;
                    else isClosed = true;
                    var windingAngle = MiscFunctions.FindWindingAroundAxis(path, transform2D, cyl.Anchor, out var minAngle,
                        out var maxAngle, isClosed.Value);
                    allPathsData.Add(new PathWindingData(path, isClosed.Value, windingAngle, minAngle, maxAngle));
                }
            }
            if (defineSeam)
                cyl.PerpXDir = -FindBestRotationalSeamDirection(cyl.Axis, cyl.Anchor, allPathsData, transform2D, backTransform);

            var loopID = 0;
            foreach (var pathData in allPathsData)
            {
                var pointList2D = new Vertex2D[pathData.Points.Count].ToList();
                for (int i = 0; i < pointList2D.Count; i++)
                {
                    var point3D = pathData.Points[i];
                    var vertex2D = new Vertex2D(cyl.TransformFrom3DTo2D(point3D), i, loopID);
                    pointList2D[i] = vertex2D;
                    vertexMapping.Add(point3D, vertex2D);
                }
                var encirclesAxis = pathData.IsClosed && Math.Abs(pathData.WindingAngle) > 1.67 * Math.PI;
                // there are 3 types of results:
                // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                // 2. a closed path that does encircle the axis - this will be an open polygon where the
                //    ends are offset by 2π*Radius in the x-direction
                // 3. an open path - this will be an open polygon
                if (encirclesAxis)
                    // store for later in a sorted list by the lower of the endpoint y-values
                    encirclingLoops.Add(Math.Min(pointList2D[0].Y, pointList2D[^1].Y), pointList2D);
                else if (pathData.IsClosed)
                    // closed but not encircling. We will hold off returning in case this is a hole
                    // in another
                    polyToIndices.Add(new Polygon(pointList2D, loopID, true), (0, -1));
                else // hopeless open path, so we will return it now
                    results.Add((new Polygon(pointList2D, loopID, false), 0, -1));
                loopID++;
            }

            // now to handle the encircling loops.
            // if the first one is going the wrong way, then it is not a proper enclosure of the cylinder, and we will consider it an open polygon
            if (encirclingLoops.Count > 0 && encirclingLoops.Values[0][0].X > encirclingLoops.Values[0][^1].X)
            {
                results.Add((new Polygon(encirclingLoops.Values[0], encirclingLoops.Values[0][0].LoopID, true), 1, encirclingLoops.Values[0].Count - 1));
                encirclingLoops.RemoveAt(0);
            }
            // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
            for (int i = 1; i < encirclingLoops.Count; i += 2)
            {
                // ensure that both loops are between 0 and 2π*Radius
                var btmLoop = encirclingLoops.Values[i - 1];
                loopID = btmLoop[0].LoopID;
                var vertex2 = btmLoop[^1];
                while (vertex2.X > circumference)
                {   // if the last point is greater than 2π*Radius, then we will remove it and
                    // insert a new point at the beginning that is 2π*Radius less than the last point
                    vertex2.Coordinates = new Vector2(vertex2.X - circumference, vertex2.Y);
                    btmLoop.Insert(0, vertex2);
                    btmLoop.RemoveAt(btmLoop.Count - 1);
                }
                var topLoop = encirclingLoops.Values[i];
                vertex2 = topLoop[^1];
                while (vertex2.X < 0)
                {   // if the last point is less than 0, then we will remove it and
                    // add a new point at the end that is 2π*Radius more than the last point
                    vertex2.Coordinates = new Vector2(vertex2.X + circumference, vertex2.Y);
                    topLoop.Insert(0, vertex2);
                    topLoop.RemoveAt(topLoop.Count - 1);
                }

                // Intersect each loop's closing edge with the seam once, then use the same height on both
                // developed copies. The paired seam vertices must roll back to one physical 3D point.
                var lowerSeamHeight = GetCylinderSeamHeight(btmLoop, circumference, isIncreasing: true);
                var lowerLeft = new Vertex2D(new Vector2(0, lowerSeamHeight), -1, loopID);
                var lowerRight = new Vertex2D(new Vector2(circumference, lowerSeamHeight), -1, loopID);

                btmLoop.Insert(0, lowerLeft);
                btmLoop.Add(lowerRight);

                var upperSeamHeight = GetCylinderSeamHeight(topLoop, circumference, isIncreasing: false);
                var upperLeft = new Vertex2D(new Vector2(0, upperSeamHeight), -1, loopID);
                var upperRight = new Vertex2D(new Vector2(circumference, upperSeamHeight), -1, loopID);

                topLoop.Insert(0, upperRight);
                topLoop.Add(upperLeft);

                var rmsLength = GetRootMeanSquareEdgeLength(btmLoop, topLoop);
                var seamVertexSpacing = double.IsNaN(newVertexSpacing)
                    ? double.PositiveInfinity
                    : newVertexSpacing > 0
                        ? newVertexSpacing
                        : rmsLength;
                var numPointsToAdd = (int)(Math.Max(upperRight.Y - lowerRight.Y,
                    upperLeft.Y - lowerLeft.Y) / seamVertexSpacing);
                var polygonPoints = new List<Vertex2D> { upperLeft };
                var stepSize = (upperLeft.Y - lowerLeft.Y) / (1 + numPointsToAdd);
                for (int j = 1; j <= numPointsToAdd; j++)
                {
                    var y = upperLeft.Y - j * stepSize;
                    polygonPoints.Add(new Vertex2D(new Vector2(0, y), -1, loopID));
                }
                var numRepeats = polygonPoints.Count + 1;
                polygonPoints.AddRange(btmLoop);
                var indexMatchedToStart = polygonPoints.Count;
                stepSize = (upperRight.Y - lowerRight.Y) / (1 + numPointsToAdd);
                for (int j = 1; j <= numPointsToAdd; j++)
                {
                    var y = lowerRight.Y + j * stepSize;
                    polygonPoints.Add(new Vertex2D(new Vector2(circumference, y), -1, loopID));
                }
                polygonPoints.AddRange(topLoop);
                // Now we can create the polygon
                polyToIndices.Add(new Polygon(polygonPoints, isClosed: true), (numRepeats, indexMatchedToStart));
            }
            // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
            if (int.IsOddInteger(encirclingLoops.Count))
                results.Add((new Polygon(encirclingLoops.Values[^1], isClosed: true), 0, -1));

            foreach (var poly in polyToIndices.Keys.CreateShallowPolygonTrees(false))
                results.Add((poly, polyToIndices[poly].numRepeats, polyToIndices[poly].indexMatchedToStart));
            return results;
        }

        private static double GetCylinderSeamHeight(IReadOnlyList<Vertex2D> loop, double circumference,
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
        public static List<(Polygon polygon, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<Polygon3D> polygon3Ds, bool defineSeam,
            out Dictionary<Vector3, Vertex2D> pointToVertexMap, double newVertexSpacing = -1.0)
        {
            var repeatAngle = Math.Abs(cone.sinAperture) * Math.Tau;
            var halfRepeatAngle = 0.5 * repeatAngle;
            var polyToIndices = new Dictionary<Polygon, (int numRepeats, int indexMatchedToStart)>();
            var encirclingLoops = new SortedList<double, ConeLoopData>();
            var results = new List<(Polygon polygon, int numRepeats, int indexMatchedToStart)>();
            pointToVertexMap = new Dictionary<Vector3, Vertex2D>();
            var transform2D = cone.Axis.TransformToXYPlane(out var backTransform);
            var pathData = new List<PathWindingData>();
            foreach (var poly3 in polygon3Ds)
            {
                bool? isClosed = null;
                foreach (var path in poly3.AllPaths())
                {
                    if (!isClosed.HasValue) isClosed = poly3.IsClosed;
                    else isClosed = true;
                    var windingAngle = MiscFunctions.FindWindingAroundAxis(path, transform2D, cone.Apex, out var minAngle,
                        out var maxAngle, isClosed.Value);
                    pathData.Add(new PathWindingData(path, isClosed.Value, windingAngle, minAngle, maxAngle));
                }
            }

            // Choose one seam for the complete set of paths. This is important for outer loops and their holes:
            // optimizing each loop independently would put them into incompatible developed coordinate systems.
            if (defineSeam)
                cone.PerpXDir = -FindBestRotationalSeamDirection(cone.Axis, cone.Apex, pathData, transform2D, backTransform);
            var loopID = 0;
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
                var transformedPoints = cone.TransformFrom3DTo2D(pointList).ToList();
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
                        new ConeLoopData(pointList2D, loopID, windingAngle > 0, sourcePointMappings));
                }
                else if (isClosed) // simple closed shape is also stored for later polygon tree creation (not sorted)
                {
                    var polygon = new Polygon(points2D, loopID, false, true);
                    AddPointMappings(pointToVertexMap, polygon, sourcePointMappings);
                    polyToIndices.Add(polygon, (0, -1));
                }
                else // hopeless open path, just return it as is. It will be open in the 2D plane too.
                {
                    var polygon = new Polygon(points2D, loopID, false, false);
                    AddPointMappings(pointToVertexMap, polygon, sourcePointMappings);
                    results.Add((polygon, 0, -1));
                }
                loopID++;
            }

            // now to handle the encircling loops. They are sorted by distance from the apex, so that the innermost loop is first.
            if (encirclingLoops.Count > 0 && !encirclingLoops.Values[0].WindsPositive)
            {   // then loop is enclosing the apex of the cone 
                var loop = encirclingLoops.Values[0];
                Polygon p = MakeConePolygon(new List<Vector2>(), loop.Points2D, repeatAngle,
                    halfRepeatAngle, newVertexSpacing, -1, loop.LoopID,
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
                    repeatAngle, halfRepeatAngle, newVertexSpacing, innerLoop.LoopID, outerLoop.LoopID,
                    out var numRepeats, out var indexMatchedToStart);
                Presenter.ShowAndHang(p);
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
                results.Add((polygon, 0, -1));
            }

            foreach (var poly in polyToIndices.Keys.CreateShallowPolygonTrees(false))
                results.Add((poly, polyToIndices[poly].numRepeats, polyToIndices[poly].indexMatchedToStart));
            Presenter.ShowAndHang(results.Select(p => p.polygon));
            return results;
        }

        private sealed class ConeLoopData
        {
            internal ConeLoopData(List<Vector2> points2D, int loopID, bool windsPositive,
                List<(Vector3 point3D, Vector2 point2D)> sourcePointMappings)
            {
                Points2D = points2D;
                LoopID = loopID;
                WindsPositive = windsPositive;
                SourcePointMappings = sourcePointMappings;
            }

            internal List<Vector2> Points2D { get; }
            internal int LoopID { get; }
            internal bool WindsPositive { get; }
            internal List<(Vector3 point3D, Vector2 point2D)> SourcePointMappings { get; }
        }

        private readonly struct PathWindingData
        {
            internal PathWindingData(IList<Vector3> points, bool isClosed, double windingAngle, double minAngle,
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
            IReadOnlyList<PathWindingData> paths,
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
            double halfRepeatAngle, double newVertexSpacing, int innerLoopID, int outerLoopID,
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
                ArrangeConeLoopAtSeam(innerLoop, shouldIncrease: false);

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

            var polygonPoints = new List<Vertex2D> { new Vertex2D(outerTop, -1, outerLoopID) };
            var topDistance = outerTop.Distance(innerTop);
            var btmDistance = outerBtm.Distance(innerBtm);
            var numPointsToAdd = (int)Math.Ceiling(Math.Max(topDistance, btmDistance) / newVertexSpacing);
            var stepSize = topDistance / numPointsToAdd;
            for (int j = 1; j < numPointsToAdd; j++)
            {
                // make this a weighted average of the two points, so that the points are evenly spaced along the line between them
                polygonPoints.Add(new Vertex2D(new Vector2(outerTop.X + j * stepSize * (innerTop.X - outerTop.X) / topDistance,
                    outerTop.Y + j * stepSize * (innerTop.Y - outerTop.Y) / topDistance), -1, outerLoopID));
            }
            numRepeats = polygonPoints.Count + 1;
            polygonPoints.AddRange(innerLoop.Select(p => new Vertex2D(p, -1, innerLoopID)));
            indexMatchedToStart = polygonPoints.Count;
            stepSize = btmDistance / numPointsToAdd;
            for (int j = 1; j < numPointsToAdd; j++)
            {
                polygonPoints.Add(new Vertex2D(new Vector2(innerBtm.X + j * stepSize * (outerBtm.X - innerBtm.X) / btmDistance,
                    innerBtm.Y + j * stepSize * (outerBtm.Y - innerBtm.Y) / btmDistance), -1, innerLoopID));
            }
            polygonPoints.AddRange(outerLoop.Select(p => new Vertex2D(p, -1, outerLoopID)));
            //Presenter.ShowAndHang(new Polygon(polygonPoints, isClosed: true));
            return new Polygon(polygonPoints, outerLoopID, true);
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

        private static double GetRootMeanSquareEdgeLength<T>(params IEnumerable<IList<T>> loops)
            where T : IVector2D
        {
            var totalLengthSqdSum = 0.0;
            var numLengths = 0;
            foreach (var loop in loops)
            {
                numLengths = loop.Count - 1;
                for (int j = 1; j < loop.Count - 1; j++)
                    totalLengthSqdSum += (loop[j - 1].X - loop[j].X) * (loop[j - 1].X - loop[j].X) +
                        (loop[j - 1].Y - loop[j].Y) * (loop[j - 1].Y - loop[j].Y);
            }
            var rmsLength = Math.Sqrt(totalLengthSqdSum / numLengths);
            return rmsLength;
        }

    }
}
