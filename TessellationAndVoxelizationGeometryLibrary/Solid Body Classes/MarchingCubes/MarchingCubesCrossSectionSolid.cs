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
                var iMin = Math.Max((int)((polygon.MinX - _xMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var iMax = Math.Min((int)((polygon.MaxX - _xMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor + 1, numGridX);
                var jMin = Math.Max((int)((polygon.MinY - _yMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var jMax = Math.Min((int)((polygon.MaxY - _yMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor + 1, numGridY);
                foreach (var toPoint in polygon.Path)
                {
                    var lineIsMoreVertical = Math.Abs(toPoint.Y - fromPoint.Y) > Math.Abs(toPoint.X - fromPoint.X);
                    if (lineIsMoreVertical || ((toPoint.X - fromPoint.X) * (fromPoint.X - lastPoint.X) < 0))
                        ExpandHorizontally(lastPoint, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    if (!lineIsMoreVertical || ((toPoint.Y - fromPoint.Y) * (fromPoint.Y - lastPoint.Y) < 0))
                        ExpandVertically(lastPoint, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    //Console.WriteLine("");
                    //Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
            //if (double.IsInfinity(grid.Max()))
            //{
            //    Console.WriteLine("infinity still in matrix");
            //Console.WriteLine();
            //Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
            //}
            return grid;
        }

        private void ExpandHorizontally(PointLight lastPoint, PointLight fromPoint, PointLight toPoint, double[,] grid, int iMin, int iMax, int jMin, int jMax)
        {
            var segment = toPoint - fromPoint;
            if (segment[1].IsNegligible(1e-9)) return;
            var segmentHalfWidth = 0.5 * segment[0];
            var magnitude = Math.Sqrt(segment[0] * segment[0] + segment[1] * segment[1]);
            var lastSegment = fromPoint - lastPoint;
            var convexSign = Math.Sign(StarMath.crossProduct2(lastSegment, segment));
            var xStart = fromPoint.X + segmentHalfWidth;
            var iStart = (int)((xStart - _xMin) * coordToGridFactor);
            var numStepsInHalfWidth = (int)(segmentHalfWidth * coordToGridFactor) + 1;
            var d = new[] { segment[1] / magnitude, -segment[0] / magnitude }; //unit vector along the band
            var yDelta = (toPoint.Y > fromPoint.Y) ? -1 : +1;
            for (int xDelta = -1; xDelta <= 1; xDelta += 2)
            { //first backward, then forward
                var i = iStart;
                if (xDelta > 0) i++;
                var numSteps = 0;
                bool atLeastOneSuccessfulChange;
                do
                {  // outer x loop
                    atLeastOneSuccessfulChange = false;
                    numSteps++;
                    if (i < iMin || i >= iMax) break;
                    var x = i * gridToCoordinateFactor + _xMin;
                    var y = toPoint.Y + d[1] * (x - toPoint.X) / d[0];
                    var j = (int)((y - _yMin) * coordToGridFactor);
                    while (true)
                    { //inner y loop
                        if ((yDelta > 0 && j >= jMax) || (yDelta < 0 && j < jMin))
                            break;
                        if ((yDelta <= 0 || j >= jMin) && (yDelta >= 0 || j < jMax))
                        {
                            var p = new PointLight(x, j * gridToCoordinateFactor + _yMin);
                            var vFrom = p - fromPoint;
                            var t = d.dotProduct(vFrom, 2);
                            if (segment.dotProduct(vFrom, 2) >= 0) //then in the band of the extruded edge
                            {
                                if (Math.Abs(t) < Math.Abs(grid[i, j]))
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (lastSegment.dotProduct(vFrom, 2) >= 0)
                            {
                                var distance = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                if (distance < Math.Abs(grid[i, j]))
                                {
                                    grid[i, j] = convexSign * distance;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else break;
                        }
                        j += yDelta;
                    }
                    i += xDelta;
                } while (atLeastOneSuccessfulChange || numSteps <= numStepsInHalfWidth);
            }
        }

        private void ExpandVertically(PointLight lastPoint, PointLight fromPoint, PointLight toPoint, double[,] grid, int iMin, int iMax, int jMin, int jMax)
        {
            var segment = toPoint - fromPoint;
            if (segment[0].IsNegligible(1e-9)) return;
            var segmentHalfHeight = 0.5 * segment[1];
            var magnitude = Math.Sqrt(segment[0] * segment[0] + segment[1] * segment[1]);
            var lastSegment = fromPoint - lastPoint;
            var convexSign = Math.Sign(StarMath.crossProduct2(lastSegment, segment));
            var yStart = fromPoint.Y + segmentHalfHeight;
            var jStart = (int)((yStart - _yMin) * coordToGridFactor);
            var numStepsInHalfHeight = (int)(segmentHalfHeight * coordToGridFactor) + 1;
            var d = new[] { segment[1] / magnitude, -segment[0] / magnitude }; //unit vector along the band
            var xDelta = (toPoint.X > fromPoint.X) ? -1 : +1;
            for (int yDelta = -1; yDelta <= 1; yDelta += 2)
            { //first backward, then forward
                var j = jStart;
                if (yDelta > 0) j++;
                var numSteps = 0;
                bool atLeastOneSuccessfulChange;
                do
                {  // outer x loop
                    atLeastOneSuccessfulChange = false;
                    numSteps++;
                    if (j < jMin || j >= jMax) break;
                    var y = j * gridToCoordinateFactor + _yMin;
                    var x = toPoint.X + d[0] * (y - toPoint.Y) / d[1];
                    var i = (int)((x - _xMin) * coordToGridFactor);
                    while (true)
                    { //inner y loop
                        if ((xDelta > 0 && i >= iMax) || (xDelta < 0 && i < iMin))
                            break;
                        if ((xDelta <= 0 || i >= iMin) && (xDelta >= 0 || i < iMax))
                        {
                            var p = new PointLight(i * gridToCoordinateFactor + _xMin, y);
                            var vFrom = p - fromPoint;
                            var t = d.dotProduct(vFrom, 2);
                            if (segment.dotProduct(vFrom, 2) >= 0) //then in the band of the extruded edge
                            {
                                if (Math.Abs(t) < Math.Abs(grid[i, j]))
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (lastSegment.dotProduct(vFrom, 2) >= 0)
                            {
                                var distance = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                if (distance < Math.Abs(grid[i, j]))
                                {
                                    grid[i, j] = convexSign * distance;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else break;
                        }
                        i += xDelta;
                    }
                    j += yDelta;
                } while (atLeastOneSuccessfulChange || numSteps <= numStepsInHalfHeight);
            }
        }


        protected override double GetValueFromSolid(int x, int y, int z)
        {
            if (onLayers)
                return gridLayers[z % numGridLayersToStore][x, y];
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
