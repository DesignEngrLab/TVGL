using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    internal class MarchingCubesCrossSectionSolid : MarchingCubes<CrossSectionSolid, double>
    {
        private readonly double[][,] gridLayers;
        private readonly bool onLayers;
        private readonly double discretization;
        private readonly int numGridLayersToStore;

        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid)
            : this(solid, (solid.StepDistances[solid.NumLayers - 1] - solid.StepDistances[0]) / (solid.NumLayers - 1))
        { }
        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid, double discretization)
            : base(solid, discretization)
        {
            this.discretization = discretization;
            var distanceBetweenLayers = (solid.StepDistances[solid.NumLayers - 1] - solid.StepDistances[0]) / (solid.NumLayers - 1);
            onLayers = distanceBetweenLayers.IsPracticallySame(discretization);
            numGridLayersToStore =
            (distanceBetweenLayers <= discretization) ? 2
            : (2 * distanceBetweenLayers <= discretization) ? 3 : 4;
            gridLayers = new double[numGridLayersToStore][,];
        }

        internal override TessellatedSolid Generate()
        {
            var distanceForGridLayers = new double[gridLayers.Length];
            var start = 0;
            // while (solid.Layer2D[start] == null || !solid.Layer2D[start].Any()) start++;
            var startingDistance = distanceForGridLayers[0] = solid.StepDistances[start];
            distanceForGridLayers[1] = solid.StepDistances[start + 1];
            if (!onLayers)
            {
                var nextXSection = startingDistance + discretization;
                //okay, need to work on this logic for 2, 3, and 4 layers
                while (solid.StepDistances[start] < nextXSection) start++;
                distanceForGridLayers[onLayers ? 1 : 2] = solid.StepDistances[start];
                if (!onLayers) distanceForGridLayers[3] = solid.StepDistances[start + 1];
            }
            var last = solid.NumLayers - 1;
            //while (solid.Layer2D[last] == null || !solid.Layer2D[last].Any()) last--;
            for (int k = 1; k < numGridLayersToStore; k++)
                gridLayers[k] = CreateDistanceGridBruteForce(solid.Layer2D[k]);
            for (var k = start; k <= last; k++)
            {
                gridLayers[k % numGridLayersToStore] = CreateDistanceGridBruteForce(solid.Layer2D[k]);
                for (var i = 0; i < numGridX - 1; i++)
                    for (var j = 0; j < numGridY - 1; j++)
                        MakeFacesInCube(i, j, k);
            }
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            // for (var k = 0; k < numGridZ - 1; k++)
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the voxelized solid, " + solid.Name);
            return new TessellatedSolid(faces);
        }

        internal TessellatedSolid Generate2()
        {
            var distanceForGridLayers = new double[gridLayers.Length];
            var start = 0;
            // while (solid.Layer2D[start] == null || !solid.Layer2D[start].Any()) start++;
            var startingDistance = distanceForGridLayers[0] = solid.StepDistances[start];
            distanceForGridLayers[1] = solid.StepDistances[start + 1];
            if (!onLayers)
            {
                var nextXSection = startingDistance + discretization;
                //okay, need to work on this logic for 2, 3, and 4 layers
                while (solid.StepDistances[start] < nextXSection) start++;
                distanceForGridLayers[onLayers ? 1 : 2] = solid.StepDistances[start];
                if (!onLayers) distanceForGridLayers[3] = solid.StepDistances[start + 1];
            }
            var last = solid.NumLayers - 1;
            //while (solid.Layer2D[last] == null || !solid.Layer2D[last].Any()) last--;
            for (int k = 1; k < numGridLayersToStore; k++)
                gridLayers[k] = CreateDistanceGrid(solid.Layer2D[k]);
            for (var k = start; k <= last; k++)
            {
                gridLayers[k % numGridLayersToStore] = CreateDistanceGrid(solid.Layer2D[k]);
                for (var i = 0; i < numGridX - 1; i++)
                    for (var j = 0; j < numGridY - 1; j++)
                        MakeFacesInCube(i, j, k);
            }
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            // for (var k = 0; k < numGridZ - 1; k++)
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the voxelized solid, " + solid.Name);
            return new TessellatedSolid(faces);
        }

        private double[,] CreateDistanceGridBruteForce(List<PolygonLight> layer)
        {
            var grid = new double[numGridX, numGridY];
            for (int i = 0; i < numGridX; i++)
                for (int j = 0; j < numGridY; j++)
                    grid[i, j] = double.PositiveInfinity;
            foreach (var polygon in layer)
            {
                var numSegments = polygon.Path.Count;
                var fromPoint = polygon.Path[numSegments - 1];
                var lastPoint = polygon.Path[numSegments - 2];
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    for (int i = 0; i < numGridX; i++)
                        for (int j = 0; j < numGridY; j++)
                        {
                            var p = new PointLight(_xMin + i * discretization, _yMin + j * discretization);
                            var vFrom = p - fromPoint;
                            var vTo = p - toPoint;
                            var dot_from = segment.dotProduct(vFrom, 2);
                            var dot_to = segment.dotProduct(vTo, 2);
                            if (dot_from >= 0 && dot_to <= 0)
                            {
                                var d = StarMath.crossProduct2(vFrom, segment.normalize());
                                if (Math.Abs(d) < Math.Abs(grid[i, j]))
                                    grid[i, j] = d;
                            }
                            else if (dot_from < 0)
                            {
                                var lastSegment = fromPoint - lastPoint;
                                if (lastSegment.dotProduct(vFrom) > 0)
                                {
                                    var sign = Math.Sign(StarMath.crossProduct2(segment, lastSegment));
                                    var d = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                    if (d < Math.Abs(grid[i, j]))
                                        grid[i, j] = sign * d;
                                }
                            }
                        }
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
            return grid;
        }
        private double[,] CreateDistanceGrid(List<PolygonLight> layer)
        {
            var grid = new double[numGridX, numGridY];
            for (int i = 0; i < numGridX; i++)
                for (int j = 0; j < numGridY; j++)
                    grid[i, j] = double.PositiveInfinity;
            //grid[i, j] = Constants.MarchingCubesCrossSectionExpandFactor * discretization;
            foreach (var polygon in layer)
            {
                var numSegments = polygon.Path.Count;
                var fromPoint = polygon.Path[numSegments - 1];
                var lastPoint = polygon.Path[numSegments - 2];
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    //var magnitude = Math.Sqrt(segment[0] * segment[0] + segment[1] * segment[1]);
                    //if (Math.Abs(segment[0]) > Math.Abs(segment[1]))
                    //    ExpandHorizontally(segment[0], segment[1], magnitude, fromPoint, grid);
                    //else ExpandVertically(segment[0], segment[1], magnitude, fromPoint, grid);
                    // first check points on inside side of line segment
                    var xDelta = (fromPoint.Y < toPoint.Y) ? -1 : (fromPoint.Y > toPoint.Y) ? +1 : 0;
                    var yDelta = (fromPoint.X > toPoint.X) ? -1 : (fromPoint.X < toPoint.X) ? +1 : 0;
                    var visited = new HashSet<int>(GetGridPointsUnderSegment(fromPoint, toPoint, xDelta, yDelta));
                    var queuePoints = new Queue<int>(visited);
                    MarchOutQueue(queuePoints, visited, xDelta, yDelta, fromPoint, toPoint, segment,
                        grid);
                    // now check points on negative (or inside) of line segment. This is done by just 
                    // reversing to and from and the directions of x and y Deltas
                    xDelta = -xDelta;
                    yDelta = -yDelta;
                    queuePoints = new Queue<int>(GetGridPointsUnderSegment(fromPoint, toPoint, xDelta, yDelta)
                        .Where(id => !visited.Contains(id)));
                    MarchOutQueue(queuePoints, visited, xDelta, yDelta, fromPoint, toPoint, segment,
                        grid);
                    // finally, check the corner, which could be on the inside or outside, this is dictated by 
                    // the 'sign' variable. There is no quick and robust way to do this, as the wedge may be
                    // really narrow and now capture a grid point for many units distance from the current point
                    // or it may be quite wide and require a queue to march out in a semi-circle. To avoid
                    // complex math and doing something thorough like https://en.wikipedia.org/wiki/Midpoint_circle_algorithm
                    // We're just going to search the square around the point that is within plus/minus 
                    // of the MarchingCubesCrossSectionExpandFactor and do a brute force check in there
                    Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
                    Console.WriteLine();
                    var lastSegment = fromPoint - lastPoint;
                    var sign = Math.Sign(StarMath.crossProduct2(lastSegment, segment));
                    var iMid = (int)((fromPoint.X - _xMin) * coordToGridFactor);
                    var jMid = (int)((fromPoint.Y - _yMin) * coordToGridFactor);
                    var iMin = Math.Max(iMid - Constants.MarchingCubesCrossSectionExpandFactor, 0);
                    var jMin = Math.Max(jMid - Constants.MarchingCubesCrossSectionExpandFactor, 0);
                    var iMax = Math.Min(iMid + 1 + Constants.MarchingCubesCrossSectionExpandFactor, numGridX - 1);
                    var jMax = Math.Min(jMid + 1 + Constants.MarchingCubesCrossSectionExpandFactor, numGridY - 1);
                    for (int j = jMin; j <= jMax; j++)
                    {
                        var id = iMin + j * yMultiplier;
                        for (int i = iMin; i <= iMax; i++)
                        {
                            if (visited.Contains(id++)) continue;
                            var p = new PointLight(_xMin + i * discretization, _yMin + j * discretization);
                            var vFrom = p - fromPoint;
                            if (segment.dotProduct(vFrom, 2) >= 0 || lastSegment.dotProduct(vFrom, 2) <= 0) continue;
                            var d = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                            if (d < Math.Abs(grid[i, j]))
                                grid[i, j] = sign * d;
                        }
                    }
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                    Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
                    Console.WriteLine();
                }
            }
            return grid;
        }


        private void MarchOutQueue(Queue<int> queuePoints, HashSet<int> visited, int xDelta, int yDelta,
            PointLight fromPoint, PointLight toPoint, double[] segment,
            double[,] grid)
        {
            while (queuePoints.Any())
            {
                var id = queuePoints.Dequeue();
                var i = id % yMultiplier;
                var j = id / yMultiplier;
                var p = new PointLight(_xMin + i * discretization,
                    _yMin + j * discretization);
                var vFrom = p - fromPoint;
                var vTo = p - toPoint;
                var dot_from = segment.dotProduct(vFrom, 2);
                var dot_to = segment.dotProduct(vTo, 2);
                if (dot_from >= 0 && dot_to <= 0)
                {
                    var d = StarMath.crossProduct2(vFrom, segment.normalize());
                    if (Math.Abs(d) < Math.Abs(grid[i, j]))
                    {
                        Console.WriteLine("grid[i,j] was {0} will now be {1}", grid[i, j], d);
                        grid[i, j] = d;
                        if (xDelta != 0 && i + xDelta >= 0 && i + xDelta < numGridX)
                        {
                            var child = (i + xDelta) + j * yMultiplier;
                            if (!visited.Contains(child))
                            {
                                queuePoints.Enqueue(child);
                                visited.Add(child);
                            }
                        }
                        if (yDelta != 0 && j + yDelta >= 0 && j + yDelta < numGridY)
                        {
                            var child = i + (j + yDelta) * yMultiplier;
                            if (!visited.Contains(child))
                            {
                                queuePoints.Enqueue(child);
                                visited.Add(child);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<int> GetGridPointsUnderSegment(PointLight a, PointLight b, int xDelta, int yDelta)
        {
            var xmin = Math.Min(a.X, b.X);
            var ymin = Math.Min(a.Y, b.Y);
            var xmax = Math.Max(a.X, b.X);
            var ymax = Math.Max(a.Y, b.Y);
            var iMin = (int)((xmin - _xMin) * coordToGridFactor);
            var jMin = (int)((ymin - _yMin) * coordToGridFactor);
            var iMax = Math.Min((int)((xmax - _xMin) * coordToGridFactor) + 1, numGridX - 1);
            var jMax = Math.Min((int)((ymax - _yMin) * coordToGridFactor) + 1, numGridY - 1);
            var result = new List<int>();
            for (int i = iMin; i <= iMax; i++)
            {
                var x = i * gridToCoordinateFactor + _xMin;
                var t = (x - a.X) / (b.X - a.X);
                if (t > 1 || t < 0) continue;
                var y = (1 - t) * a.Y + t * b.Y;
                var j = (y - _yMin) * coordToGridFactor;
                if (yDelta > 0) j += 1;
                if (j < 0 || j >= numGridY) continue;
                var id = i + yMultiplier * (int)j;
                if (!result.Contains(id)) result.Add(id);
                if ((int)j > 0 && j.IsPracticallySame((int)j))
                {
                    id += yDelta * yMultiplier;
                    if (!result.Contains(id)) result.Add(id);
                }
            }
            for (int j = jMin; j <= jMax; j++)
            {
                var y = j * gridToCoordinateFactor + _yMin;
                var t = (y - a.Y) / (b.Y - a.Y);
                var x = (1 - t) * a.X + t * b.X;
                var i = (x - _xMin) * coordToGridFactor;
                if (xDelta > 0) i += 1;
                if (i < 0 || i >= numGridY) continue;
                var id = (int)i + yMultiplier * j;
                if (!result.Contains(id)) result.Add(id);
                if ((int)i > 0 && i.IsPracticallySame((int)i))
                {
                    id += xDelta;
                    if (!result.Contains(id)) result.Add(id);
                }
            }
            return result;
        }

        protected override double GetValueFromSolid(double x, double y, double z)
        {
            var i = (int)((x - _xMin) * coordToGridFactor);
            var j = (int)((y - _yMin) * coordToGridFactor);
            var k = (int)((z - _zMin) * coordToGridFactor);
            if (onLayers)
                return gridLayers[k % numGridLayersToStore][i, j];
            else return 0;
        }

        protected override bool IsInside(double v)
        {
            return v <= 0.0;
        }

        protected override double GetOffset(StoredValue<double> from, StoredValue<double> to,
            int direction, int sign)
        {
            if (from.Value.IsNegligible()) return 0.0;
            if (to.Value.IsNegligible()) return gridToCoordinateFactor;
            return -gridToCoordinateFactor * from.Value / (to.Value - from.Value);
        }
    }
}
