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
                    var lastSegment = fromPoint - lastPoint;
                    for (int i = 0; i < numGridX; i++)
                        for (int j = 0; j < numGridY; j++)
                            UpdateGrid(i, j, lastPoint, fromPoint, toPoint,segment, lastSegment, grid);
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
                    grid[i, j] = Constants.MarchingCubesCrossSectionExpandFactor * discretization;
            foreach (var polygon in layer)
            {
                var numSegments = polygon.Path.Count;
                var fromPoint = polygon.Path[numSegments - 1];
                var lastPoint = polygon.Path[numSegments - 2];
                foreach (var toPoint in polygon.Path)
                {
                    var segment = toPoint - fromPoint;
                    var xDelta = (toPoint.Y > fromPoint.Y) ? +1 : (fromPoint.Y > toPoint.Y) ? -1 : 0;
                    var yDelta = (fromPoint.X > toPoint.X) ? +1 : (toPoint.X > fromPoint.X) ? -1 : 0;
                    var queuePoints = GetGridPointsAroundSegment(fromPoint, toPoint);
                    while (queuePoints.Any())
                    {
                        var p = queuePoints.Dequeue();
                        var vFrom = p - fromPoint;
                        var vTo = p - toPoint;
                        var dot_from = segment.dotProduct(vFrom);
                        var dot_to = segment.dotProduct(vTo);
                        if (dot_from <= 0 || dot_to >= 0) continue;
                        var d = segment.normalize().crossProduct(vFrom).norm2();
                        //    l = norm(axis)
                        //    radius = abs(dot_to) * rfrom / l + abs(dot_from) * rto / l
                        //    smoothness = abs(dot_to) * sfrom / l + abs(dot_from) * sto / l
                        //end

                        //v = radius * ((1 - smoothness) / d + smoothness / d ^ 5)
                        //if v > DefaultMaximumValue
                        //    v = DefaultMaximumValue
                        //end
                        //return v

                        //var newDist =
                    }








                    lastPoint = fromPoint;
                    fromPoint = toPoint;
                }
            }
            return grid;
        }

        bool UpdateGrid(int i, int j, PointLight lastPoint, PointLight fromPoint, PointLight toPoint,
            double[] segment, double[] lastSegment, double[,] grid)
        {
            var p = new PointLight(xMin + i * discretization, yMin + j * discretization);
            var vFrom = p - fromPoint;
            var vTo = p - toPoint;
            var dot_from = segment.dotProduct(vFrom, 2);
            var dot_to = segment.dotProduct(vTo, 2);
            if (dot_from >= 0 && dot_to <= 0)
            {
                var d = StarMath.crossProduct2(segment.normalize(), vFrom);
                if (Math.Abs(d) >= Math.Abs(grid[i, j]))
                    return false;
                grid[i, j] = d;
                return true;
            }
            if (dot_from < 0)
            {
                if (lastSegment.dotProduct(vFrom) > 0)
                {
                    var sign = Math.Sign(StarMath.crossProduct2(segment, lastSegment));
                    var d = Math.Sqrt(vFrom[0] * vFrom[0] + vFrom[1] * vFrom[1]);
                    if (d >= Math.Abs(grid[i, j]))
                        return false;
                    grid[i, j] = sign * d;
                    return true;
                }
            }
            return false;
        }

        private Queue<PointLight> GetGridPointsAroundSegment(PointLight a, PointLight b)
        {
            throw new NotImplementedException();
        }

        protected override double GetValueFromSolid(double x, double y, double z)
        {
            var i = (int)((x - xMin) * coordToGridFactor);
            var j = (int)((y - yMin) * coordToGridFactor);
            var k = (int)((z - zMin) * coordToGridFactor);
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
