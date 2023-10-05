// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MarchingCubesCrossSectionSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace TVGL
{
    /// <summary>
    /// Class MarchingCubesCrossSectionSolid.
    /// Implements the <see cref="TVGL.MarchingCubes{TVGL.CrossSectionSolid, System.Double}" />
    /// </summary>
    /// <seealso cref="TVGL.MarchingCubes{TVGL.CrossSectionSolid, System.Double}" />
    internal class MarchingCubesCrossSectionSolid : MarchingCubes<CrossSectionSolid, double>
    {
        /// <summary>
        /// The number triangles on side factor
        /// </summary>
        internal const double NumTrianglesOnSideFactor = 0.5;
        /// <summary>
        /// The tolerance for snapping to layers
        /// </summary>
        internal const double ToleranceForSnappingToLayers = 0.517;
        /// <summary>
        /// The grid layers
        /// </summary>
        private readonly double[][,] gridLayers;
        /// <summary>
        /// The on layers
        /// </summary>
        private readonly bool onLayers;
        /// <summary>
        /// The delta layer
        /// </summary>
        private readonly int deltaLayer;
        /// <summary>
        /// The discretization
        /// </summary>
        private readonly double discretization;
        /// <summary>
        /// The start
        /// </summary>
        private readonly int start;
        /// <summary>
        /// The number grid layers to store
        /// </summary>
        private readonly int numGridLayersToStore;
        /// <summary>
        /// The start distance
        /// </summary>
        private readonly double startDistance;
        /// <summary>
        /// The last
        /// </summary>
        private readonly int last;
        /// <summary>
        /// The number layers
        /// </summary>
        private readonly int numLayers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarchingCubesCrossSectionSolid"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid)
            : this(solid, (solid.StepDistances[solid.NumLayers - 1] - solid.StepDistances[0]) / (solid.NumLayers - 1))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarchingCubesCrossSectionSolid"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="discretization">The discretization.</param>
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
            var remainderError = (discretization / distanceBetweenLayers) % 1.0;
            onLayers = (remainderError < ToleranceForSnappingToLayers || remainderError > (1 - ToleranceForSnappingToLayers));
            // if the distanceBetweenLayers is some integer multiple of the discretization (well, within the tolerance)
            // then simply use the layers - it's quicker and more accurate.  
            numGridLayersToStore = onLayers ? 2 : (distanceBetweenLayers > discretization) ? 3 : 4;
            // well, it's possible that the number to store be only 3 but no point in 
            gridLayers = new double[numGridLayersToStore][,];
            if (onLayers)
            {
                deltaLayer = (int)Math.Round(discretization / distanceBetweenLayers);
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
        internal override TessellatedSolid Generate()
        {
            if (onLayers) return GenerateOnLayers();
            else return GenerateBetweenLayers();
        }

        /// <summary>
        /// Generates the on layers.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        private TessellatedSolid GenerateOnLayers()
        {
            //for (int k = 1; k < numGridLayersToStore; k++)
            gridLayers[0] = CreateDistanceGrid(solid.Layer2D[start]);
            for (var k = 1; k <= numLayers; k++)
            {
                gridLayers[k % numGridLayersToStore] = CreateDistanceGrid(solid.Layer2D[start + k * deltaLayer]);
                for (var i = 0; i < numGridX - 1; i++)
                    for (var j = 0; j < numGridY - 1; j++)
                        MakeFacesInCube(i, j, k);
            }
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            // for (var k = 0; k < numGridZ - 1; k++)
            var comments = new List<string>(solid.Comments)
            {
                "tessellation (via marching cubes) of the cross-section solid, " + solid.Name
            };
            return new TessellatedSolid(faces);
        }

        /// <summary>
        /// Generates the between layers.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
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
            var comments = new List<string>(solid.Comments)
            {
                "tessellation (via marching cubes) of the cross-section solid, " + solid.Name
            };
            return new TessellatedSolid(faces);
        }

        /// <summary>
        /// Creates the distance grid brute force.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <returns>System.Double[].</returns>
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
                            //  var p = new Vector2(, );
                            //  var vTo = p - toPoint;
                            var vTo = new Vector2(xp - toPoint.X, yp - toPoint.Y);
                            var dot_to = segment.Dot(vTo);
                            if (dot_to > 0) continue;
                            //var vFrom = p - fromPoint;
                            var vFrom = new Vector2(xp - fromPoint.X, yp - fromPoint.Y);
                            var dot_from = segment.Dot(vFrom);
                            if (dot_from >= 0)
                            {
                                var d = vFrom.Cross(segment.Normalize());
                                if (Math.Abs(d) < Math.Abs(grid[i, j]))
                                    grid[i, j] = d;
                            }
                            else if (lastSegment.Dot(vFrom) > 0)
                            {
                                var sign = Math.Sign(lastSegment.Cross(segment));
                                var d = vFrom.Length();
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
            return grid;
        }
        /// <summary>
        /// Creates the distance grid.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <returns>System.Double[].</returns>
        private double[,] CreateDistanceGrid(IList<Polygon> layer)
        {
            var allIntersections = PolygonOperations.AllPolygonIntersectionPointsAlongHorizontalLines(layer, _yMin, discretization, out var firstIntersectingIndex);
            using var allIntersectionsEnumerator = allIntersections.GetEnumerator();
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
                var polygonMinX = polygon.MinX;
                var polygonMinY = polygon.MinY;
                var polygonMaxX = polygon.MaxX;
                var polygonMaxY = polygon.MaxY;
                var iMin = Math.Max((int)((polygonMinX - _xMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var iMax = Math.Min((int)((polygonMaxX - _xMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor + 1, numGridX);
                var jMin = Math.Max((int)((polygonMinY - _yMin) * coordToGridFactor) - Constants.MarchingCubesBufferFactor, 0);
                var jMax = Math.Min((int)((polygonMaxY - _yMin) * coordToGridFactor) + Constants.MarchingCubesBufferFactor + 1, numGridY);
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

        /// <summary>
        /// Expands the horizontally.
        /// </summary>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="fromPoint">From point.</param>
        /// <param name="toPoint">To point.</param>
        /// <param name="grid">The grid.</param>
        /// <param name="iMin">The i minimum.</param>
        /// <param name="iMax">The i maximum.</param>
        /// <param name="jMin">The j minimum.</param>
        /// <param name="jMax">The j maximum.</param>
        private void ExpandHorizontally(Vector2 lastPoint, Vector2 fromPoint, Vector2 toPoint, double[,] grid, int iMin, int iMax, int jMin, int jMax)
        {
            var segment = toPoint - fromPoint;
            if (segment.Y.IsNegligible(1e-9)) return;
            var segmentHalfWidth = 0.5 * segment.X;
            var magnitude = segment.Length();
            var lastSegment = fromPoint - lastPoint;
            var convexSign = Math.Sign(lastSegment.Cross(segment));
            var xStart = fromPoint.X + segmentHalfWidth;
            var iStart = (int)((xStart - _xMin) * coordToGridFactor);
            var numStepsInHalfWidth = (int)(segmentHalfWidth * coordToGridFactor) + 1;
            var d = new Vector2(segment.Y / magnitude, -segment.X / magnitude); //unit vector along the band
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
                    var y = toPoint.Y + d.Y * (x - toPoint.X) / d.X;
                    var j = (int)((y - _yMin) * coordToGridFactor);
                    while (true)
                    { //inner y loop
                        if ((yDelta > 0 && j >= jMax) || (yDelta < 0 && j < jMin))
                            break;
                        if ((yDelta <= 0 || j >= jMin) && (yDelta >= 0 || j < jMax))
                        {
                            var p = new Vector2(x, j * gridToCoordinateFactor + _yMin);
                            var vFrom = p - fromPoint;
                            var t = d.Dot(vFrom);
                            if (segment.Dot(vFrom) >= 0) //then in the band of the extruded edge
                            {
                                if (Math.Sign(t) * t < Math.Sign(t) * grid[i, j])
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (convexSign != 0 && lastSegment.Dot(vFrom) >= 0)
                            {   // then in the wedge between this segment and lastSegment
                                var distance = vFrom.Length();
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
            if (convexSign != 0 && lastSegment.Y * segment.X < 0) // then it is possible there are additional points around the corner that need 
                                                                  //to be evaluated
            {
                if (Math.Abs(lastSegment.X) < Math.Abs(lastSegment.Y))
                    ExpandLastCornerHorizontally(fromPoint, lastSegment, grid, iMin, iMax, jMin, jMax, convexSign);
                else ExpandLastCornerVertically(fromPoint, lastSegment, grid, iMin, iMax, jMin, jMax, convexSign);
            }
        }

        /// <summary>
        /// Expands the vertically.
        /// </summary>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="fromPoint">From point.</param>
        /// <param name="toPoint">To point.</param>
        /// <param name="grid">The grid.</param>
        /// <param name="iMin">The i minimum.</param>
        /// <param name="iMax">The i maximum.</param>
        /// <param name="jMin">The j minimum.</param>
        /// <param name="jMax">The j maximum.</param>
        private void ExpandVertically(Vector2 lastPoint, Vector2 fromPoint, Vector2 toPoint, double[,] grid, int iMin, int iMax, int jMin, int jMax)
        {
            var segment = toPoint - fromPoint;
            if (segment.X.IsNegligible(1e-9)) return;
            var segmentHalfHeight = 0.5 * segment.Y;
            var magnitude = Math.Sqrt(segment.X * segment.X + segment.Y * segment.Y);
            var lastSegment = fromPoint - lastPoint;
            var convexSign = Math.Sign(lastSegment.Cross(segment));
            var yStart = fromPoint.Y + segmentHalfHeight;
            var jStart = (int)((yStart - _yMin) * coordToGridFactor);
            var numStepsInHalfHeight = (int)(segmentHalfHeight * coordToGridFactor) + 1;
            var d = new Vector2(segment.Y / magnitude, -segment.X / magnitude); //unit vector along the band
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
                    var x = toPoint.X + d.X * (y - toPoint.Y) / d.Y;
                    var i = (int)((x - _xMin) * coordToGridFactor);
                    while (true)
                    { //inner y loop
                        if ((xDelta > 0 && i >= iMax) || (xDelta < 0 && i < iMin))
                            break;
                        if ((xDelta <= 0 || i >= iMin) && (xDelta >= 0 || i < iMax))
                        {
                            var p = new Vector2(i * gridToCoordinateFactor + _xMin, y);
                            var vFrom = p - fromPoint;
                            var t = d.Dot(vFrom);
                            if (segment.Dot(vFrom) >= 0) //then in the band of the extruded edge
                            {
                                if (Math.Sign(t) * t < Math.Sign(t) * grid[i, j])
                                {
                                    grid[i, j] = t;
                                    atLeastOneSuccessfulChange = true;
                                }
                            }
                            else if (convexSign != 0 && lastSegment.Dot(vFrom) >= 0)
                            {
                                var distance = vFrom.Length();
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


        /// <summary>
        /// Expands the last corner horizontally.
        /// </summary>
        /// <param name="fromPoint">From point.</param>
        /// <param name="lastSegment">The last segment.</param>
        /// <param name="grid">The grid.</param>
        /// <param name="iMin">The i minimum.</param>
        /// <param name="iMax">The i maximum.</param>
        /// <param name="jMin">The j minimum.</param>
        /// <param name="jMax">The j maximum.</param>
        /// <param name="convexSign">The convex sign.</param>
        private void ExpandLastCornerHorizontally(Vector2 fromPoint, Vector2 lastSegment, double[,] grid, int iMin, int iMax, int jMin, int jMax, int convexSign)
        {
            var magnitude = lastSegment.Length();
            var d = new[] { convexSign * lastSegment.Y / magnitude, -convexSign * lastSegment.X / magnitude }; //unit vector along the band
            var xDelta = Math.Sign(lastSegment.X);
            var yDelta = Math.Sign(lastSegment.Y);
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



        /// <summary>
        /// Expands the last corner vertically.
        /// </summary>
        /// <param name="fromPoint">From point.</param>
        /// <param name="lastSegment">The last segment.</param>
        /// <param name="grid">The grid.</param>
        /// <param name="iMin">The i minimum.</param>
        /// <param name="iMax">The i maximum.</param>
        /// <param name="jMin">The j minimum.</param>
        /// <param name="jMax">The j maximum.</param>
        /// <param name="convexSign">The convex sign.</param>
        private void ExpandLastCornerVertically(Vector2 fromPoint, Vector2 lastSegment, double[,] grid, int iMin, int iMax, int jMin, int jMax, int convexSign)
        {
            var magnitude = lastSegment.Length();
            var d = new[] { convexSign * lastSegment.Y / magnitude, -convexSign * lastSegment.X / magnitude }; //unit vector along the band
            var xDelta = Math.Sign(lastSegment.X);
            var yDelta = Math.Sign(lastSegment.Y);
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

        /// <summary>
        /// Gets the value from solid.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>ValueT.</returns>
        protected override double GetValueFromSolid(int x, int y, int z)
        {
            if (onLayers)
                return gridLayers[z % numGridLayersToStore][x, y];
            else return 0;
        }

        /// <summary>
        /// Determines whether the specified v is inside.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if the specified v is inside; otherwise, <c>false</c>.</returns>
        protected override bool IsInside(double v)
        {
            return v <= 0.0;
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sign">The sign.</param>
        /// <returns>System.Double.</returns>
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
