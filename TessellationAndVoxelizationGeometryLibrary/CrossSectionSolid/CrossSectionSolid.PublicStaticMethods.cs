using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        public CrossSectionSolid CreateConstantCrossSectionSolid(IEnumerable<IEnumerable<Vector2>> bottomPolygon, Matrix4x4 transform, 
            double sameToleranceTVGL, UnitType unitsTVGL)
        {
            throw new NotImplementedException();
        }

        public static CrossSectionSolid CreateConstantCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            IEnumerable<IEnumerable<Vector2>> shape, double sameTolerance, UnitType units)
        {
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, List<List<Vector2>>> { { 0, shape.Select(a => new List<Vector2>(a)).ToList() }, 
                { 1, shape.Select(a => new List<Vector2>(a)).ToList() } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }

        public static List<List<Vector2>>[] GetUniformlySpacedSlices(TessellatedSolid ts, CartesianDirections direction, double startDistanceAlongDirection = double.NaN, int numSlices = -1,
            double stepSize = double.NaN)
        {
            if (double.IsNaN(stepSize) && numSlices < 1) throw new ArgumentException("Either a valid stepSize or a number of slices greater than zero must be specified.");
            var intDir = Math.Abs((int)direction) - 1;
            var lengthAlongDir = ts.Bounds[1][intDir] - ts.Bounds[0][intDir];
            stepSize = Math.Abs(stepSize);
            if (double.IsNaN(stepSize)) stepSize = lengthAlongDir / numSlices;
            if (numSlices < 1) numSlices = (int)(lengthAlongDir / stepSize);
            if (double.IsNaN(startDistanceAlongDirection))
            {
                if (direction < 0)
                    startDistanceAlongDirection = ts.Bounds[1][intDir] - 0.5 * stepSize;
                else startDistanceAlongDirection = ts.Bounds[0][intDir] + 0.5 * stepSize;
            }
            switch (direction)
            {
                case CartesianDirections.XPositive:
                    return AllSlicesAlongX(ts, startDistanceAlongDirection, numSlices, stepSize);
                case CartesianDirections.YPositive:
                    return AllSlicesAlongY(ts, startDistanceAlongDirection, numSlices, stepSize);
                case CartesianDirections.ZPositive:
                    return AllSlicesAlongZ(ts, startDistanceAlongDirection, numSlices, stepSize);
                case CartesianDirections.XNegative:
                    return AllSlicesAlongX(ts, startDistanceAlongDirection, numSlices, stepSize).Reverse().ToArray();
                case CartesianDirections.YNegative:
                    return AllSlicesAlongY(ts, startDistanceAlongDirection, numSlices, stepSize).Reverse().ToArray();
                default:
                    return AllSlicesAlongZ(ts, startDistanceAlongDirection, numSlices, stepSize).Reverse().ToArray();
            }
        }

        private static List<List<Vector2>>[] AllSlicesAlongX(TessellatedSolid ts, double startDistanceAlongDirection, int numSlices, double stepSize)
        {
            throw new NotImplementedException();
        }

        private static List<List<Vector2>>[] AllSlicesAlongY(TessellatedSolid ts, double startDistanceAlongDirection, int numSlices, double stepSize)
        {
            throw new NotImplementedException();
        }

        static List<List<Vector2>>[] AllSlicesAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            List<List<Vector2>>[] loopsAlongZ = new List<List<Vector2>>[numSteps];
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            var sortedVertices = ts.Vertices.OrderBy(v => v.Z).ToArray();
            var currentEdges = new HashSet<Edge>();
            var nextDistance = sortedVertices.First().Z;
            var vIndex = 0;
            for (int step = 0; step < numSteps; step++)
            {
                var z = startDistance + step * stepSize;
                var thisVertex = sortedVertices[vIndex];
                var needToOffset = false;
                while (thisVertex.Z <= z)
                {
                    if (z.IsPracticallySame(thisVertex.Z)) needToOffset = true;
                    foreach (var edge in thisVertex.Edges)
                    {
                        if (currentEdges.Contains(edge)) currentEdges.Remove(edge);
                        else currentEdges.Add(edge);
                    }
                    vIndex++;
                    if (vIndex == sortedVertices.Length) break;
                    thisVertex = sortedVertices[vIndex];
                }
                if (needToOffset)
                    z += Math.Min(stepSize, sortedVertices[vIndex + 1].Z - z) / 10.0;
                if (currentEdges.Any()) loopsAlongZ[step] = GetZLoops(currentEdges, z);
                else loopsAlongZ[step] = new List<List<Vector2>>();
            }
            return loopsAlongZ;
        }

        private static List<List<Vector2>> GetZLoops(HashSet<Edge> penetratingEdges, double ZOfPlane)
        {
            var loops = new List<List<Vector2>>();

            var unusedEdges = new HashSet<Edge>(penetratingEdges);
            while (unusedEdges.Any())
            {
                var loop = new List<Vector2>();
                var firstEdgeInLoop = unusedEdges.First();
                var finishedLoop = false;
                var currentEdge = firstEdgeInLoop;
                do
                {
                    unusedEdges.Remove(currentEdge);
                    var intersectVertex = MiscFunctions.Vector2OnZPlaneFromIntersectingLine(ZOfPlane, currentEdge.From, currentEdge.To);
                    loop.Add(intersectVertex);
                    var nextFace = (currentEdge.From.Z < ZOfPlane) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                    Edge nextEdge = null;
                    foreach (var whichEdge in nextFace.Edges)
                    {
                        if (currentEdge == whichEdge) continue;
                        if (whichEdge == firstEdgeInLoop)
                        {
                            finishedLoop = true;
                            loops.Add(new List<Vector2>(loop));
                            break;
                        }
                        else if (unusedEdges.Contains(whichEdge))
                        {
                            nextEdge = whichEdge;
                            break;
                        }
                    }
                    if (!finishedLoop && nextEdge == null)
                    {
                        Console.WriteLine("Incomplete loop.");
                        loops.Add(new List<Vector2>(loop));
                    }
                    else currentEdge = nextEdge;
                } while (!finishedLoop);
            }
            return loops;
        }



    }
}