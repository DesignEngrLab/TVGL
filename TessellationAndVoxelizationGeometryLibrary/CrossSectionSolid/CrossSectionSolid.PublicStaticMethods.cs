using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        public CrossSectionSolid CreateUniformCrossSectionSolid(IEnumerable<IEnumerable<Vector2>> bottomPolygon, Matrix4x4 transform,
            double sameToleranceTVGL, UnitType unitsTVGL)
        {
            throw new NotImplementedException();
        }

        public static CrossSectionSolid CreateUniformCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            IEnumerable<Polygon> shape, double sameTolerance, UnitType units)
        {
            var shapeList = shape as IList<Polygon> ?? shape.ToList();
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, IList<Polygon>> { { 0, shapeList }, { 1, shapeList } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }

        public static List<Polygon>[] GetUniformlySpacedSlices(TessellatedSolid ts, CartesianDirections direction, double startDistanceAlongDirection = double.NaN, int numSlices = -1,
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

        private static List<Polygon>[] AllSlicesAlongX(TessellatedSolid ts, double startDistanceAlongDirection, int numSlices, double stepSize)
        {
            throw new NotImplementedException();
        }

        private static List<Polygon>[] AllSlicesAlongY(TessellatedSolid ts, double startDistanceAlongDirection, int numSlices, double stepSize)
        {
            throw new NotImplementedException();
        }

        static List<Polygon>[] AllSlicesAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            var loopsAlongZ = new List<Polygon>[numSteps];
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
                    z += Math.Min(stepSize, sortedVertices[vIndex].Z - z) / 10.0;
                if (currentEdges.Any()) loopsAlongZ[step] = GetZLoops(currentEdges, z);
                else loopsAlongZ[step] = new List<Polygon>();
            }
            return loopsAlongZ;
        }

        private static List<Polygon> GetZLoops(HashSet<Edge> penetratingEdges, double ZOfPlane)
        {
            var loops = new List<Polygon>();

            var unusedEdges = new HashSet<Edge>(penetratingEdges);
            while (unusedEdges.Any())
            {
                var path = new List<Vector2>();
                var firstEdgeInLoop = unusedEdges.First();
                var finishedLoop = false;
                var currentEdge = firstEdgeInLoop;
                do
                {
                    unusedEdges.Remove(currentEdge);
                    var intersectVertex = MiscFunctions.PointOnZPlaneFromIntersectingLine(ZOfPlane, currentEdge.From.Coordinates,
                        currentEdge.To.Coordinates);
                    path.Add(intersectVertex);
                    var nextFace = (currentEdge.From.Z < ZOfPlane) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                    Edge nextEdge = null;
                    foreach (var whichEdge in nextFace.Edges)
                    {
                        if (currentEdge == whichEdge) continue;
                        if (whichEdge == firstEdgeInLoop)
                        {
                            finishedLoop = true;
                            loops.Add(new Polygon(path, false));
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
                        loops.Add(new Polygon(path, false));
                    }
                    else currentEdge = nextEdge;
                } while (!finishedLoop);
            }
            return loops;
        }

        public Vector3[][][] GetCrossSectionsAs3DLoops()
        {
            var result = new Vector3[Layer2D.Count][][];
            int k = 0;
            foreach (var layerKeyValuePair in Layer2D)
            {
                var index = layerKeyValuePair.Key;
                var zValue = StepDistances[index];
                var numLoops = layerKeyValuePair.Value.Count;
                var layer = new Vector3[numLoops][];
                result[k++] = layer;
                for (int j = 0; j < numLoops; j++)
                {
                    var loop = new Vector3[layerKeyValuePair.Value[j].Path.Count];
                    layer[j] = loop;
                    for (int i = 0; i < loop.Length; i++)
                        loop[i] = (new Vector3(layerKeyValuePair.Value[j].Path[i], zValue)).Transform(TransformMatrix);
                }
            }
            return result;
        }

    }
}