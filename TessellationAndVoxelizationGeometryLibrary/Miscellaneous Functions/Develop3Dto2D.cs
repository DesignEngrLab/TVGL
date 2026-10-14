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
            foreach (var pointList in pointSets)
            {
                var isClosed = isClosedEnumerator.MoveNext() ? isClosedEnumerator.Current : false;
                var pointList2D = cyl.TransformFrom3DTo2D(pointList).ToList();
                // when the points are a closed path and they encircle the axis
                var windingAngle = MiscFunctions.FindWindingAroundAxis(pointList, cyl.Axis,
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


        public static IEnumerable<(Polygon polygon, bool properEnclosure, int numRepeats, int indexMatchedToStart)>
            GetUnrolledPolygons(this Cone cone, IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet, bool bluntTheTip = true)
        {
            var repeatAngle = Math.Abs(cone.sinAperture) * Math.Tau;
            var halfRepeatAngle = 0.5 * repeatAngle;
            var polyToIndices = new Dictionary<Polygon, (int numRepeats, int indexMatchedToStart)>();
            var encirclingLoops = new SortedList<double, (List<Vector2>, bool)>();
            var isClosedEnumerator = IsClosedSet.GetEnumerator();
            foreach (var pointList in pointSets)
            {
                var isClosed = isClosedEnumerator.MoveNext() ? isClosedEnumerator.Current : false;
                // when the points are a closed path and they encircle the axis
                var windingAngle = MiscFunctions.FindWindingAroundAxis(pointList, cone.Axis, cone.Apex, out _, out _, isClosed);
                var encirclesAxis = isClosed && Math.Abs(windingAngle) > 1.67 * Math.PI;
                // there are 3 types of results:
                // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                // 2. a closed path that does encircle the axis - this will be an open polygon where the
                //    ends are offset by 2π*Radius in the x-direction
                // 3. an open path - this will be an open polygon
                var points2D = cone.TransformFrom3DTo2D(pointList);
                if (encirclesAxis)
                {
                    var pointList2D = points2D.ToList();
                    // store for later. use distance from the apex (length in 2D) to sort loops along the cone
                    encirclingLoops.Add(Math.Min(pointList2D[0].LengthSquared(), pointList2D[^1].LengthSquared()),
                        (pointList2D, windingAngle > 0));
                }
                else if (isClosed) // simple closed shape is also stored for later polygon tree creation (not sorted)
                    polyToIndices.Add(new Polygon(points2D, isClosed: true), (0, -1));
                else // hopeless open path, just return it as is. It will be open in the 2D plane too.
                    yield return ((new Polygon(points2D, isClosed: false), false, 0, -1));
            }

            // now to handle the encircling loops. They are sorted by distance from the apex, so that the innermost loop is first.
            if (encirclingLoops.Count > 0 && encirclingLoops.Values[0].Item2)
            {   // then loop is enclosing the apex of the cone 
                Polygon p = MakeConePolygon(Array.Empty<Vector2>(), encirclingLoops.Values[0].Item1, repeatAngle,
                    halfRepeatAngle, bluntTheTip, out var numRepeats, out var indexMatchedToStart);
                polyToIndices.Add(p, (numRepeats, indexMatchedToStart));
                encirclingLoops.RemoveAt(0);
            }
            // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
            for (int i = 1; i < encirclingLoops.Count; i += 2)
            {
                Polygon p = MakeConePolygon(encirclingLoops.Values[i - 1].Item1, encirclingLoops.Values[i].Item1,
                    repeatAngle, halfRepeatAngle, bluntTheTip, out var numRepeats, out var indexMatchedToStart);
                polyToIndices.Add(p, (numRepeats, indexMatchedToStart));
            }
            // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
            if (int.IsOddInteger(encirclingLoops.Count))
                yield return (new Polygon(encirclingLoops.Values[^1].Item1, isClosed: true), false, 0, -1);

            foreach (var poly in polyToIndices.Keys.CreateShallowPolygonTrees(false))
                yield return (poly, true, polyToIndices[poly].numRepeats, polyToIndices[poly].indexMatchedToStart);
        }

        private static Polygon MakeConePolygon(IList<Vector2> innerLoop, IList<Vector2> outerLoop, double repeatAngle,
            double halfRepeatAngle, bool bluntTheTip, out int numRepeats, out int indexMatchedToStart)
        {
            var cosGap = Math.Cos(halfRepeatAngle);
            var sinGap = Math.Sin(halfRepeatAngle);
            var rmsLength = GetRootMeanSquareEdgeLength(innerLoop, outerLoop);
            double angle;
            if (innerLoop.Count == 0)
            {
                if (bluntTheTip)
                    innerLoop = [new Vector2(rmsLength*cosGap, rmsLength * sinGap),
                   new Vector2(rmsLength*cosGap, -rmsLength * sinGap) ];
                innerLoop = [Vector2.Zero];
            }
            else
            {
                // btm loop goes CW, top loop goes CCW.
                angle = Math.Atan2(innerLoop[^1].Y, innerLoop[^1].X);
                while (angle < -halfRepeatAngle)
                {   // if the last point is less than repeatAngle, then we will remove it and
                    var radius = innerLoop[^1].Length();
                    angle += repeatAngle;
                    innerLoop.Insert(0, new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                    innerLoop.RemoveAt(innerLoop.Count - 1);
                    angle = Math.Atan2(innerLoop[^1].Y, innerLoop[^1].X);
                }
            }
            angle = Math.Atan2(outerLoop[^1].Y, outerLoop[^1].X);
            while (outerLoop[0].X > halfRepeatAngle)
            {
                var radius = outerLoop[^1].Length();
                angle -= repeatAngle;
                outerLoop.Insert(0, new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                outerLoop.RemoveAt(outerLoop.Count - 1);
                angle = Math.Atan2(outerLoop[^1].Y, outerLoop[^1].X);
            }

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
                outerLoop.Add(outerBtm);
            }

            var polygonPoints = new List<Vector2> { outerTop }; 
            var topDistance = outerTop.Distance(innerTop);
            var btmDistance = outerBtm.Distance(innerBtm);
            var numPointsToAdd = (int)Math.Ceiling(Math.Max(topDistance, btmDistance) / rmsLength);
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