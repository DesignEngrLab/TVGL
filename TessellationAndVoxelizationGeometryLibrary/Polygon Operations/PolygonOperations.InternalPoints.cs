// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 06-07-2026
//
// ***********************************************************************
// <copyright file="PolygonOperations.InternalPoints.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class PolygonOperations
    {
        public static IEnumerable<Vector2> CreateInternalPointsOffset(this Polygon polygon, double targetRadius)
        {
            var prevPolygons = new List<Polygon> { polygon };
            while (prevPolygons.Count > 0)
            {
                prevPolygons = prevPolygons.OffsetSquare(-targetRadius);
                prevPolygons.Complexify(targetRadius);
                prevPolygons.SimplifyMinLength(targetRadius);
                foreach (var v in prevPolygons.SelectMany(poly => poly.AllPolygons).SelectMany(p => p.Vertices))
                    yield return v.Coordinates;
            }
        }
        private static int numAngleForInternalPtCreation = 30;

        public static IEnumerable<Vector2> CreateInternalPointsPoissonDisk(this Polygon polygon, double targetRadius, int maxPointsToReturn = -1)
        {
            var random = new Random(0);
            var rSqd = targetRadius * targetRadius;
            //Bridson’s Algorithm runs in linear time.
            // 1. Initialize a background grid where each cell size is sqrt(r) / 2 (guaranteeing that each cell can hold
            // at most one point).
            var gridLength = Math.Sqrt(0.5 * rSqd);
            var grid = new Grid<(bool, Vector2)>();
            grid.Initialize(polygon.MinX, polygon.MaxX, polygon.MinY, polygon.MaxY, gridLength);
            var gridValues = grid.Values;
            var gridYCount = grid.YCount;

            foreach (var v in polygon.AllPaths.SelectMany(x => x))
                gridValues[grid.GetIndex(v.X, v.Y)] = (true, v);
            var queue = new Queue<Vector2>();
            // 2. Select some initial random seed point inside the polygon, place it in queue and the background
            //    grid.
            foreach (var seedPt in CreateInternalPointsRadial(polygon, 10))
            {
                var seedIndex = grid.GetIndex(seedPt.X, seedPt.Y);
                if (!gridValues[seedIndex].Item1)
                {
                    gridValues[seedIndex] = (true, seedPt);
                    queue.Enqueue(seedPt);
                    if (maxPointsToReturn > 0 && queue.Count == maxPointsToReturn)
                        break;
                }
            }
            var deltaAngle = 2 * Math.PI / numAngleForInternalPtCreation;
            var sinAngles = new double[numAngleForInternalPtCreation];
            var cosAngles = new double[numAngleForInternalPtCreation];
            for (var i = 0; i < numAngleForInternalPtCreation; i++)
                (sinAngles[i], cosAngles[i]) = Math.SinCos(i * deltaAngle);
            // 3. While the queue isn't empty, pick a point P from it. Generate up to k (usually 30) candidate points
            //    randomly in a spherical ring between distance r and 2r around P. For each candidate, check if it is
            //    inside the polygon and use the background grid to quickly verify it isn't too close to any existing
            //    points.
            var numberOfPointsReturned = 0;
            while (queue.TryDequeue(out var parentPt))
            {
                yield return parentPt;
                numberOfPointsReturned++;
                //Console.WriteLine(queue.Count + ", " + gridValues.Count(c => c.Item1));
                if (numberOfPointsReturned == maxPointsToReturn)
                    yield break;

                // Every queued point is already valid and will eventually be returned. Once the queue contains
                // enough points to satisfy the requested limit, generating more children is wasted work.
                var remainingCapacity = maxPointsToReturn > 0
                    ? maxPointsToReturn - numberOfPointsReturned - queue.Count
                    : int.MaxValue;
                if (remainingCapacity <= 0)
                    continue;

                // 4. If a candidate is valid, add it to the queue and output. If all k attempts fail -> oh well. go to next in queue
                var startAngleIndex = random.Next(numAngleForInternalPtCreation);
                for (var attempt = 0; attempt < numAngleForInternalPtCreation; attempt++)
                {
                    var ind = startAngleIndex + attempt;
                    if (ind >= numAngleForInternalPtCreation)
                        ind -= numAngleForInternalPtCreation;
                    var radius = targetRadius + random.NextDouble() * targetRadius;
                    var childPt = parentPt + new Vector2(radius * cosAngles[ind], radius * sinAngles[ind]);
                    if (childPt.X < polygon.MinX || childPt.X >= polygon.MaxX ||
                        childPt.Y < polygon.MinY || childPt.Y >= polygon.MaxY)
                        continue;
                    var xIndex = grid.GetXIndex(childPt.X);
                    var yIndex = grid.GetYIndex(childPt.Y);
                    var gridIndex = grid.GetIndex(xIndex, yIndex);
                    if (gridValues[gridIndex].Item1)
                        continue;
                    var startX = Math.Max(0, xIndex - 2);
                    var endX = Math.Min(grid.XCount - 1, xIndex + 2);
                    var startY = Math.Max(0, yIndex - 2);
                    var endY = Math.Min(grid.YCount - 1, yIndex + 2);
                    var neighborIsTooClose = false;
                    for (var i = startX; i <= endX; i++)
                    {
                        for (int j = startY; j <= endY; j++)
                        {
                            if (i == xIndex && j == yIndex)
                                continue;
                            var neighbor = gridValues[gridYCount * i + j];
                            if (neighbor.Item1 && neighbor.Item2.DistanceSquared(childPt) < rSqd)
                            {
                                neighborIsTooClose = true;
                                break;
                            }
                        }
                        if (neighborIsTooClose) break;
                    }
                    if (neighborIsTooClose) continue;
                    if (!polygon.IsPointInsidePolygon(false, childPt))
                        continue;

                    gridValues[gridIndex] = (true, childPt);
                    queue.Enqueue(childPt);
                    remainingCapacity--;
                    if (remainingCapacity == 0)
                        break;
                }
            }
        }
        public static Vector2[] CreateInternalPointsVoronoi(this Polygon polygon, int numberPoints)
        {
            var points = CreateInternalPointsRadial(polygon, numberPoints).ToArray();

            throw new NotImplementedException();
            return points;
        }

        public static IEnumerable<Vector2> CreateInternalPointsRadial(this Polygon polygon, int numberPoints)
        {
            var random = new Random();
            var stepSize = polygon.Edges.Sum(e => e.Length) / numberPoints;
            var edgeIndex = 0;
            var delta = 0.0;
            var center = polygon.Centroid;
            var maxNumberOfLoops = 10;
            while (numberPoints > 0 && maxNumberOfLoops > 0)
            {
                var edge = polygon.Edges[edgeIndex];
                var point = edge.FromPoint.Coordinates + delta * edge.Vector.Normalize();
                //Presenter.ShowAndHang([polygon.Path,new[] { point }]);
                if (PointOnRadius(polygon, point, out var internalPoint))
                {
                    yield return internalPoint;
                    numberPoints--;
                }
                delta += stepSize;
                while (delta >= edge.Length)
                {
                    edgeIndex++;
                    if (edgeIndex == polygon.Edges.Count)
                    {
                        edgeIndex = 0;
                        maxNumberOfLoops--;
                        delta = random.NextDouble() * stepSize;
                    }
                    delta -= edge.Length;
                    edge = polygon.Edges[edgeIndex];
                }
            }
        }

        private static bool PointOnRadius(Polygon polygon, Vector2 perimeterPt, out Vector2 result)
        {
            result = polygon.Centroid;
            for (var i = 0; i < 15; i++)
            {
                result = (result + perimeterPt) / 2;
                if (polygon.IsPointInsidePolygon(false, result))
                    return true;
            }
            return false;
        }
    }
}
