using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarMathLib;

namespace TVGL
{
    internal class MarchingCubesCrossSectionSolid : MarchingCubes<CrossSectionSolid, double>
    {
        internal const double NumTrianglesOnSideFactor = 0.5;
        internal const double ToleranceForSnappingToLayers = 0.517;
        private readonly double[][,] gridLayers;
        private readonly bool onLayers;
        private readonly int deltaLayer;
        private readonly double discretization;
        private readonly int start;
        private readonly int numGridLayersToStore;
        private readonly double startDistance;
        private readonly int last;
        private readonly int numLayers;

        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid)
            : this(solid, (solid.StepDistances[solid.NumLayers - 1] - solid.StepDistances[0]) / (solid.NumLayers - 1))
        { }

        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid, double discretization)
            : base(solid, discretization)
        {
            this.discretization = discretization;
            start = 0;
            while (solid.Layer2D[start] == null || !solid.Layer2D[start].Any()) start++;
            startDistance = solid.StepDistances[start];
            last = solid.NumLayers - 1;
            while (solid.Layer2D[last] == null || !solid.Layer2D[last].Any()) last--;
            var length = solid.StepDistances[last] - solid.StepDistances[start];
            numLayers = last - start;
            var distanceBetweenLayers = length / numLayers;
            var remainderError = (discretization/distanceBetweenLayers) % 1.0;
            onLayers = (remainderError < ToleranceForSnappingToLayers || remainderError > (1 - ToleranceForSnappingToLayers));
            // if the distanceBetweenLayers is some integer multiple of the discretization (well, within the tolerance)
            // then simply use the layers - it's quicker and more accurate.  
            numGridLayersToStore = onLayers ? 2 : (distanceBetweenLayers > discretization) ? 3 : 4;
            // well, it's possible that the number to store be only 3 but no point in 
            gridLayers = new double[numGridLayersToStore][,];
            if (onLayers)
            {
                deltaLayer = (int)Math.Round(discretization/distanceBetweenLayers);
                var offsetInt = (numLayers % deltaLayer) / 2;
                start += offsetInt;
                numLayers /= deltaLayer;
            }
            else
            {
                numLayers = (int)(length / discretization);
                var sampledLength = numLayers * discretization;
                var offset = 0.5 * (length - sampledLength);
                startDistance += offset;
                deltaLayer = (int)(distanceBetweenLayers / discretization);
            }
        }

        /// <summary>
        /// Generates the marching cubes solid
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        internal TessellatedSolid Generate()
        {
            if (onLayers) return GenerateOnLayers();
            else return GenerateBetweenLayers();
        }

        private TessellatedSolid GenerateOnLayers()
        {
            //for (int k = 1; k < numGridLayersToStore; k++)
            gridLayers[0] = CreateDistanceGrid(solid.Layer2D[start]);
            for (var k = 1; k <= numLayers; k++)
            {
                gridLayers[k % numGridLayersToStore] = CreateDistanceGrid(solid.Layer2D[start+k*deltaLayer]);
                for (var i = 0; i < numGridX - 1; i++)
                    for (var j = 0; j < numGridY - 1; j++)
                        MakeFacesInCube(i, j, k);
            }
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            // for (var k = 0; k < numGridZ - 1; k++)
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the cross-section solid, " + solid.Name);
            return new TessellatedSolid(faces);
        }

        private TessellatedSolid GenerateBetweenLayers()
        {
            throw new NotImplementedException();
            var nextXSection = startDistance + discretization;

            for (int k = 1; k < numGridLayersToStore; k++)
                gridLayers[k] = CreateDistanceGrid(solid.Layer2D[k + start]);
            for (var k = 0; k <= last; k++)
            {
                gridLayers[k % numGridLayersToStore] = CreateDistanceGrid(solid.Layer2D[k + start]);
                for (var i = 0; i < numGridX - 1; i++)
                    for (var j = 0; j < numGridY - 1; j++)
                        MakeFacesInCube(i, j, k);
            }
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            // for (var k = 0; k < numGridZ - 1; k++)
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the cross-section solid, " + solid.Name);
            return new TessellatedSolid(faces);
        }

        private double[,] CreateDistanceGridBruteForce(List<Polygon> layer)
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
                var lastSegment = fromPoint - lastPoint;
                var iMin = Math.Max((int)((polygon.MinX - _xMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var iMax = Math.Min((int)((polygon.MaxX - _xMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor, numGridX - 1);
                var jMin = Math.Max((int)((polygon.MinY - _yMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var jMax = Math.Min((int)((polygon.MaxY - _yMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor, numGridY - 1);
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    Parallel.For(iMin, iMax, i =>
                    //for (int i = iMin; i <= iMax; i++)
                    {
                        for (int j = jMin; j <= jMax; j++)
                        {
                            var xp = _xMin + i * discretization;
                            var yp = _yMin + j * discretization;
                            //  var p = new PointLight(, );
                            //  var vTo = p - toPoint;
                            var vTo = new[] { xp - toPoint.X, yp - toPoint.Y };
                            var dot_to = segment.dotProduct(vTo, 2);
                            if (dot_to > 0) continue;
                            //var vFrom = p - fromPoint;
                            var vFrom = new[] { xp - fromPoint.X, yp - fromPoint.Y };
                            var dot_from = segment.dotProduct(vFrom, 2);
                            if (dot_from >= 0)
                            {
                                var d = StarMath.crossProduct2(vFrom, segment.normalize());
                                if (Math.Abs(d) < Math.Abs(grid[i, j]))
                                    grid[i, j] = d;
                            }
                            else if (lastSegment.dotProduct(vFrom) > 0)
                            {
                                var sign = Math.Sign(StarMath.crossProduct2(lastSegment, segment));
                                var d = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                if (d < Math.Abs(grid[i, j]))
                                    grid[i, j] = sign * d;
                            }
                        }
                    });
                    lastSegment = segment;
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
            //Console.WriteLine("");
            //Console.WriteLine(StarMathLib.StarMath.MakePrintString(grid));
            return grid;
        }
        private double[,] CreateDistanceGrid(List<PolygonLight> layer)
        {
            var allIntersections = PolygonOperations.AllPolygonIntersectionPointsAlongY(layer, _yMin, numGridY, discretization, out var firstIntersectingIndex);
            var allIntersectionsEnumerator = allIntersections.GetEnumerator();
            var grid = new double[numGridX, numGridY];
            for (int j = 0; j < numGridY; j++)
            {
                double[] intersections = null;
                if (j >= firstIntersectingIndex)
                {
                    allIntersectionsEnumerator.MoveNext();
                    intersections = allIntersectionsEnumerator.Current;
                }
                if (intersections == null)
                    for (int i = 0; i < numGridX; i++)
                        grid[i, j] = double.PositiveInfinity;
                else
                {
                    var xIndex = 0;
                    var x = _xMin;
                    for (int i = 0; i < numGridX; i++)
                    {
                        while (xIndex < intersections.Length && x > intersections[xIndex]) xIndex++;
                        if (xIndex % 2 == 0)
                            grid[i, j] = double.PositiveInfinity;
                        else grid[i, j] = double.NegativeInfinity;
                        x += discretization;
                    }
                }
            }
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
                    if (Math.Abs(toPoint.Y - fromPoint.Y) > Math.Abs(toPoint.X - fromPoint.X))
                        ExpandHorizontally(lastPoint, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    else
                        ExpandVertically(lastPoint, fromPoint, toPoint, grid, iMin, iMax, jMin, jMax);
                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
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
                                if (Math.Sign(t) * t < Math.Sign(t) * grid[i, j])
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (lastSegment.dotProduct(vFrom, 2) >= 0)
                            {
                                var distance = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                if (distance < convexSign * grid[i, j])
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
            if (lastSegment[1] * segment[1] < 0) // then it is possible there are additional points around the corner that need 
                                                 //to be evaluated
            {
                if (Math.Abs(lastSegment[0]) < Math.Abs(lastSegment[1]))
                    ExpandLastCornerHorizontally(fromPoint, lastSegment, grid, iMin, iMax, jMin, jMax, convexSign);
                else ExpandLastCornerVertically(fromPoint, lastSegment, grid, iMin, iMax, jMin, jMax, convexSign);
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
                                if (Math.Sign(t) * t < Math.Sign(t) * grid[i, j])
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (lastSegment.dotProduct(vFrom, 2) >= 0)
                            {
                                var distance = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                                if (distance < convexSign * grid[i, j])
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


        private void ExpandLastCornerHorizontally(PointLight fromPoint, double[] lastSegment, double[,] grid, int iMin, int iMax, int jMin, int jMax, int convexSign)
        {
            var magnitude = Math.Sqrt(lastSegment[0] * lastSegment[0] + lastSegment[1] * lastSegment[1]);
            var d = new[] { convexSign * lastSegment[1] / magnitude, -convexSign * lastSegment[0] / magnitude }; //unit vector along the band
            var xDelta = Math.Sign(lastSegment[0]);
            var yDelta = Math.Sign(lastSegment[1]);
            var i = (int)((fromPoint.X - _xMin) * coordToGridFactor);
            var numSteps = 0;
            bool atLeastOneSuccessfulChange;
            do
            {  // outer x loop
                atLeastOneSuccessfulChange = false;
                numSteps++;
                if (i < iMin || i >= iMax) break;
                var x = i * gridToCoordinateFactor + _xMin;
                var y = fromPoint.Y + d[1] * (x - fromPoint.X) / d[0];
                var j = (int)((y - _yMin) * coordToGridFactor);
                while (true)
                { //inner y loop
                    if ((yDelta > 0 && j >= jMax) || (yDelta < 0 && j < jMin))
                        break;
                    if ((yDelta <= 0 || j >= jMin) && (yDelta >= 0 || j < jMax))
                    {
                        var xterm = x - fromPoint.X;
                        var yterm = j * gridToCoordinateFactor + _yMin - fromPoint.Y;
                        var distance = Math.Sqrt(xterm * xterm + yterm * yterm);
                        if (distance < convexSign * grid[i, j])
                        {
                            grid[i, j] = convexSign * distance;
                            atLeastOneSuccessfulChange = true;
                        }
                        else break;
                    }
                    j += yDelta;
                }
                i += xDelta;
            } while (atLeastOneSuccessfulChange || numSteps <= Constants.MarchingCubesMissedFactor);
        }



        private void ExpandLastCornerVertically(PointLight fromPoint, double[] lastSegment, double[,] grid, int iMin, int iMax, int jMin, int jMax, int convexSign)
        {
            var magnitude = Math.Sqrt(lastSegment[0] * lastSegment[0] + lastSegment[1] * lastSegment[1]);
            var d = new[] { convexSign * lastSegment[1] / magnitude, -convexSign * lastSegment[0] / magnitude }; //unit vector along the band
            var xDelta = Math.Sign(lastSegment[0]);
            var yDelta = Math.Sign(lastSegment[1]);
            var j = (int)((fromPoint.Y - _yMin) * coordToGridFactor);
            var numSteps = 0;
            bool atLeastOneSuccessfulChange;
            do
            {  // outer x loop
                atLeastOneSuccessfulChange = false;
                numSteps++;
                if (j < jMin || j >= jMax) break;
                var y = j * gridToCoordinateFactor + _yMin;
                var x = fromPoint.X + d[0] * (y - fromPoint.Y) / d[1];
                var i = (int)((x - _xMin) * coordToGridFactor);
                while (true)
                { //inner y loop
                    if ((xDelta > 0 && i >= iMax) || (xDelta < 0 && i < iMin))
                        break;
                    if ((xDelta <= 0 || i >= iMin) && (xDelta >= 0 || i < iMax))
                    {
                        var xterm = i * gridToCoordinateFactor + _xMin - fromPoint.X;
                        var yterm = y - fromPoint.Y;
                        var distance = Math.Sqrt(xterm * xterm + yterm * yterm);
                        if (distance < convexSign * grid[i, j])
                        {
                            grid[i, j] = convexSign * distance;
                            atLeastOneSuccessfulChange = true;
                        }
                        else break;
                    }
                    i += xDelta;
                }
                j += yDelta;
            } while (atLeastOneSuccessfulChange || numSteps <= Constants.MarchingCubesMissedFactor);
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
