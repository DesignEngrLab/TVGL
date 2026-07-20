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

            foreach (var v in polygon.AllPaths.SelectMany(x => x))
                grid.Values[grid.GetIndex(v.X, v.Y)] = (true, v);
            var queue = new Queue<Vector2>();
            // 2. Select some initial random seed point inside the polygon, place it in queue and the background
            //    grid.
            foreach (var seedPt in CreateInternalPointsRadial(polygon, 10))
            {
                if (!grid.TryGet(seedPt.X, seedPt.Y, out var value) || !value.Item1)
                {
                    grid.Values[grid.GetIndex(seedPt.X, seedPt.Y)] = (true, seedPt);
                    queue.Enqueue(seedPt);
                }
            }
            var deltaAngle = 2 * Math.PI / numAngleForInternalPtCreation;
            var indices = Enumerable.Range(0, numAngleForInternalPtCreation).ToArray();
            var angles = indices.Select(i => i * deltaAngle).ToArray();
            var sinAngles = angles.Select(Math.Sin).ToArray();
            var cosAngles = angles.Select(Math.Cos).ToArray();
            // 3. While the queue isn't empty, pick a point P from it. Generate up to k (usually 30) candidate points
            //    randomly in a spherical ring between distance r and 2r around P. For each candidate, check if it is
            //    inside the polygon and use the background grid to quickly verify it isn't too close to any existing
            //    points.
            while (queue.TryDequeue(out var parentPt))
            {
                yield return parentPt;
                //Console.WriteLine(queue.Count + ", " + grid.Values.Count(c => c.Item1));
                maxPointsToReturn--;
                if (maxPointsToReturn == 0) yield break;
                // 4. If a candidate is valid, add it to the queue and output. If all k attempts fail -> oh well. go to next in queue
                foreach (var ind in indices.Shuffle())
                {
                    var radius = targetRadius + random.NextDouble() * targetRadius;
                    var childPt = parentPt + new Vector2(radius * cosAngles[ind], radius * sinAngles[ind]);
                    if (childPt.X < polygon.MinX || childPt.X >= polygon.MaxX ||
                        childPt.Y < polygon.MinY || childPt.Y >= polygon.MaxY)
                        continue;
                    if (!polygon.IsPointInsidePolygon(false, childPt))
                        continue;
                    var xIndex = grid.GetXIndex(childPt.X);
                    var yIndex = grid.GetYIndex(childPt.Y);
                    if (grid[xIndex, yIndex].Item1)
                        continue;
                    var startX = Math.Max(0, xIndex - 1);
                    var endX = Math.Min(grid.XCount - 1, xIndex + 1);
                    var startY = Math.Max(0, yIndex - 1);
                    var endY = Math.Min(grid.YCount - 1, yIndex + 1);
                    var neighborIsTooClose = false;
                    for (var i = startX; i <= endX; i++)
                    {
                        for (int j = startY; j <= endY; j++)
                        {
                            if (i == 0 && j == 0) continue; // this is checked earlie
                            var neighbor = grid[i, j];
                            if (neighbor.Item1 || neighbor.Item2.DistanceSquared(childPt) < rSqd)
                            {
                                neighborIsTooClose = true;
                                break;
                            }
                        }
                        if (neighborIsTooClose) break;
                    }
                    if (neighborIsTooClose) continue;

                    grid[xIndex, yIndex] = (true, childPt);
                    queue.Enqueue(childPt);
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
