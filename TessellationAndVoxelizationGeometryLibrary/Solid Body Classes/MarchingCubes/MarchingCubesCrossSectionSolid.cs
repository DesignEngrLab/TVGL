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
                var iMin = Math.Max((int)((polygon.MinX - _xMin) * coordToGridFactor) - 1, 0);
                var iMax = Math.Min((int)((polygon.MaxX - _xMin) * coordToGridFactor) + 2, numGridX);
                var jMin = Math.Max((int)((polygon.MinY - _yMin) * coordToGridFactor) - 1, 0);
                var jMax = Math.Min((int)((polygon.MaxY - _yMin) * coordToGridFactor) + 2, numGridY);
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    for (int i = iMin; i < iMax; i++)
                        for (int j = jMin; j < jMax; j++)
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
            foreach (var polygon in layer)
            {
                var numSegments = polygon.Path.Count;
                var fromPoint = polygon.Path[numSegments - 1];
                var lastPoint = polygon.Path[numSegments - 2];
                var iMin = Math.Max((int)((polygon.MinX - _xMin) * coordToGridFactor) - 1, 0);
                var iMax = Math.Min((int)((polygon.MaxX - _xMin) * coordToGridFactor) + 2, numGridX);
                var jMin = Math.Max((int)((polygon.MinY - _yMin) * coordToGridFactor) - 1, 0);
                var jMax = Math.Min((int)((polygon.MaxY - _yMin) * coordToGridFactor) + 2, numGridY);
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    var magnitude = Math.Sqrt(segment[0] * segment[0] + segment[1] * segment[1]);
                    // first check points on inside side of line segment
                    if (Math.Abs(segment[0]) <= Math.Abs(segment[1]))
                        ExpandHorizontally(segment, magnitude, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    else ExpandVertically(segment, magnitude, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    //Console.WriteLine("line");
                    //Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
                    // finally, check the corner, which could be on the inside or outside, this is dictated by 
                    // the 'sign' variable. There is no quick and robust way to do this, as the wedge may be
                    // really narrow and not capture a grid point for many units distance from the current point
                    // or it may be quite wide and require a queue to march out in a semi-circle. To avoid
                    // complex math and doing something thorough like https://en.wikipedia.org/wiki/Midpoint_circle_algorithm
                    // We're just going to search the square around the point that is within plus/minus 
                    // of the MarchingCubesCrossSectionExpandFactor and do a brute force check in there
                    var lastSegment = fromPoint - lastPoint;
                    var xDiffFactor = segment[0] * lastSegment[0];
                    var yDiffFactor = segment[1] * lastSegment[1];
                    var cross = StarMath.crossProduct2(lastSegment, segment);
                    var sign = Math.Sign(cross);
                    var iMid = (int)((fromPoint.X - _xMin) * coordToGridFactor);
                    var jMid = (int)((fromPoint.Y - _yMin) * coordToGridFactor);
                    var iStart = Math.Max(iMin, 0);
                    var jStart = Math.Max(jMin, 0);
                    var iEnd = Math.Min(iMax, numGridX);
                    var jEnd = Math.Min(jMax, numGridY);

                    //if (xDiffFactor >= 0 && yDiffFactor >= 0)
                    //{ // then two ranges can be reduced
                    //    if (segment[0] >= 0 == sign >= 0) iStart = iMid;
                    //    else iEnd = iMid + 1;
                    //    if (segment[1] >= 0 == sign <= 0) jStart = jMid;
                    //    else jEnd = jMid + 1;
                    //}
                    //else if (yDiffFactor >= 0)
                    //{ //then x is going in opposite directions but y's are the same
                    //    if ((segment[1] >= 0) == sign >= 0) iStart = iMid;
                    //    else iEnd = iMid + 1;
                    //}
                    //else if (xDiffFactor >= 0)
                    //{ //then y is going in opposite directions but x's are the same
                    //    if ((segment[0] >= 0) == sign <= 0) jStart = jMid;
                    //    else jEnd = jMid + 1;
                    //}

                    for (int i = iStart; i < iEnd; i++)
                        for (int j = jStart; j < jEnd; j++)
                        {
                            var p = new PointLight(_xMin + i * discretization, _yMin + j * discretization);
                            var vFrom = p - fromPoint;
                            if (segment.dotProduct(vFrom, 2) > 0 || lastSegment.dotProduct(vFrom, 2) < 0) continue;
                            var d = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                            if (d < Math.Abs(grid[i, j]))
                                grid[i, j] = sign * d;
                        }
                    //Console.WriteLine("angle");
                    //Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
            //if (double.IsInfinity(grid.Max()))
            //{
            //    Console.WriteLine("infinity still in matrix");
            //    Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
            //}
            return grid;
        }

        private void ExpandVertically(double[] segment, double magnitude, PointLight fromPoint, PointLight toPoint, double[,] grid,
            int iMin, int iMax, int jMin, int jMax)
        {
            var yStart = fromPoint.Y + 0.5 * segment[1];
            var jStart = (int)((yStart - _yMin) * coordToGridFactor);
            var d = new[] { segment[1] / magnitude, -segment[0] / magnitude }; //unit vector along the band
            var refPoint = (toPoint.X > fromPoint.X) ? toPoint : fromPoint;
            var bandWidth = magnitude * magnitude / Math.Abs(segment[0]);
            var numPointsInBand = (int)(bandWidth * coordToGridFactor) + 1;
            for (int yDelta = -1; yDelta <= 1; yDelta += 2)
            {
                var numOutOfBoundAttempts = Constants.MarchingCubesOOBFactor * numPointsInBand;
                //this magnitude * magnitude / yS should be the height of the band...this is weird but correctkk
                var j = jStart;
                if (yDelta > 0) j++;
                while (numOutOfBoundAttempts >= 0)
                {
                    if (j < jMin || j >= jMax)
                    {
                        numOutOfBoundAttempts = -1;
                        continue;
                    }
                    var y = j * gridToCoordinateFactor + _yMin;
                    var x = refPoint.X + d[0] * (y - refPoint.Y) / d[1];
                    var i = (int)((x - _xMin) * coordToGridFactor);
                    for (var n = 0; n < numPointsInBand; n++)
                    {
                        if (i >= iMin && i < iMax)
                        {
                            var p = new PointLight(i * gridToCoordinateFactor + _xMin, y);
                            var vFrom = p - fromPoint;
                            var vTo = p - toPoint;
                            var dot_from = segment.dotProduct(vFrom, 2);
                            var dot_to = segment.dotProduct(vTo, 2);
                            if (dot_from >= 0 && dot_to <= 0)
                            {
                                var t = d.dotProduct(vFrom, 2);
                                if (Math.Abs(t) < Math.Abs(grid[i, j]))
                                    grid[i, j] = t;
                                else numOutOfBoundAttempts--;
                            }
                        }
                        i--;
                    }
                    j += yDelta;
                }
            }
        }

        private void ExpandHorizontally(double[] segment, double magnitude, PointLight fromPoint, PointLight toPoint, double[,] grid,
            int iMin, int iMax, int jMin, int jMax)
        {
            var xStart = fromPoint.X + 0.5 * segment[0];
            var iStart = (int)((xStart - _xMin) * coordToGridFactor);
            var d = new[] { segment[1] / magnitude, -segment[0] / magnitude }; //unit vector along the band
            var refPoint = (toPoint.Y > fromPoint.Y) ? toPoint : fromPoint;
            var bandHeight = magnitude * magnitude / Math.Abs(segment[1]);
            var numPointsInBand = (int)(bandHeight * coordToGridFactor) + 1;
            for (int xDelta = -1; xDelta <= 1; xDelta += 2)
            {
                var numOutOfBoundAttempts = Constants.MarchingCubesOOBFactor * numPointsInBand;
                //this magnitude * magnitude / yS should be the height of the band...this is weird but correctkk
                var i = iStart;
                if (xDelta > 0) i++;
                while (numOutOfBoundAttempts >= 0)
                {
                    if (i < iMin || i >= iMax)
                    {
                        numOutOfBoundAttempts = -1;
                        continue;
                    }
                    var x = i * gridToCoordinateFactor + _xMin;
                    var y = refPoint.Y + d[1] * (x - refPoint.X) / d[0];
                    var j = (int)((y - _yMin) * coordToGridFactor);
                    for (var n = 0; n < numPointsInBand; n++)
                    {
                        if (j >= jMin && j < jMax)
                        {
                            var p = new PointLight(x, j * gridToCoordinateFactor + _yMin);
                            var vFrom = p - fromPoint;
                            var vTo = p - toPoint;
                            var dot_from = segment.dotProduct(vFrom, 2);
                            var dot_to = segment.dotProduct(vTo, 2);
                            if (dot_from >= 0 && dot_to <= 0)
                            {
                                var t = d.dotProduct(vFrom, 2);
                                if (Math.Abs(t) < Math.Abs(grid[i, j]))
                                    grid[i, j] = t;
                                else numOutOfBoundAttempts--;
                            }
                        }
                        j--;
                    }
                    i += xDelta;
                }
            }
        }

        protected override double GetValueFromSolid(int x, int y, int z)
        {
            if (onLayers)
                return gridLayers[z % numGridLayersToStore][x,y];
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
            if (direction == 2 && (double.IsInfinity(from.Value) || double.IsInfinity(to.Value))) return 0.5 * gridToCoordinateFactor;
            return -gridToCoordinateFactor * from.Value / (to.Value - from.Value);
        }
    }
}
