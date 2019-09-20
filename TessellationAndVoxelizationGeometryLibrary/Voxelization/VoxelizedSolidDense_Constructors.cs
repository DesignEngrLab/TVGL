// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="VoxelizedSolidDense_Constructors.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using Newtonsoft.Json.Linq;
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TVGL.IOFunctions;

namespace TVGL.Voxelization
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolid : Solid
    {
        #region Properties
        public byte this[int x, int y, int z]
        {
            get
            {
                return Voxels[x, y, z];
            }
            set { Voxels[x, y, z] = value; }
        }
        public byte this[int[] index]
        {
            get
            {
                return Voxels[index[0], index[1], index[2]];
            }
            set { Voxels[index[0], index[1], index[2]] = value; }
        }


        byte[,,] Voxels;
     
        public readonly int Discretization;
        public int[] VoxelsPerSide;
        public int[][] VoxelBounds { get; set; }
        public double VoxelSideLength { get; internal set; }
        public double[] TessToVoxSpace { get; }
        private readonly double[] Dimensions;
        public double[] Offset => Bounds[0];
        public int Count { get; internal set; }
        public TessellatedSolid TS { get; set; }

        #endregion

        public VoxelizedSolid(int[] voxelsPerSide, int discretization, double voxelSideLength,
            IEnumerable<double[]> bounds, byte value = 0)
        {
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];
            Count = 0;
            SurfaceArea = 0;
            Volume = 0;
            if (value != 0)
            {
                Parallel.For(0, xLim, m =>
                {
                    for (var n = 0; n < yLim; n++)
                        for (var o = 0; o < zLim; o++)
                            Voxels[m, n, o] = value;
                });
                UpdateBoundingProperties();

            }
            Discretization = discretization;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
        }

        public VoxelizedSolid(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds)
        {
            Voxels = (byte[,,])voxels.Clone();
            Discretization = discretization;
            VoxelsPerSide = voxelsPerSide;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolid(VoxelizedSolid vs)
        {
            Voxels = (byte[,,])vs.Voxels.Clone();
            Discretization = vs.Discretization;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolid(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
            TS = ts;
            Discretization = discretization;
            SolidColor = new Color(Constants.DefaultColor);
            var voxelsOnLongSide = Math.Pow(2, Discretization);

            Bounds = new double[2][];
            Dimensions = new double[3];

            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
            }
            else
            {
                Bounds[0] = ts.Bounds[0];
                Bounds[1] = ts.Bounds[1];
            }
            for (var i = 0; i < 3; i++)
                Dimensions[i] = Bounds[1][i] - Bounds[0][i];

            //var longestSide = Dimensions.Max();
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int)Math.Round(d / VoxelSideLength)).ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);

            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];

            VoxelizeSolid(ts);
            UpdateProperties();
        }



        private void VoxelizeSolid(TessellatedSolid ts, bool possibleNullSlices = false)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = AllSlicesAlongZ(ts, zBegin, zLim, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time

            Parallel.For(0, zLim, k =>
            //for (var k = 0; k < zLim; k++)
            {
                var loops = decomp[k];
                if (loops.Any())
                {
                    var intersections = AllPolygonIntersectionPointsAlongY(loops, yBegin, yLim, VoxelSideLength, out var yStartIndex);
                    var numYlines = intersections.Count;
                    for (int j = 0; j < numYlines; j++)
                    {
                        var intersectionPoints = intersections[j];
                        var numXRangesOnThisLine = intersectionPoints.Length;
                        for (var m = 0; m < numXRangesOnThisLine; m += 2)
                        {
                            //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                            //Although other dimensions do not also do this. Everything operates with Round (effectively).
                            //Could reverse this to add more voxels
                            var sp = (int)((intersectionPoints[m] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (sp < 0) sp = 0;
                            var ep = (int)((intersectionPoints[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (ep >= xLim) ep = xLim-1;
                            for (var i = sp; i <= ep; i++)  // the range is inclusive
                                Voxels[i, yStartIndex + j, k] = 1;
                        }
                    }
                }
            }   );
        }
        private static List<List<PointLight>> GetZLoops(HashSet<Edge> penetratingEdges, double ZOfPlane)
        {
            var loops = new List<List<PointLight>>();

            var unusedEdges = new HashSet<Edge>(penetratingEdges);
            while (unusedEdges.Any())
            {
                var loop = new List<PointLight>();
                var firstEdgeInLoop = unusedEdges.First();
                var finishedLoop = false;
                var currentEdge = firstEdgeInLoop;
                do
                {
                    unusedEdges.Remove(currentEdge);
                    var intersectVertex = MiscFunctions.PointLightOnZPlaneFromIntersectingLine(ZOfPlane, currentEdge.From, currentEdge.To);
                    loop.Add(intersectVertex);
                    var nextFace = (currentEdge.From.Z < ZOfPlane) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                    Edge nextEdge = null;
                    foreach (var whichEdge in nextFace.Edges)
                    {
                        if (currentEdge == whichEdge) continue;
                        if (whichEdge == firstEdgeInLoop)
                        {
                            finishedLoop = true;
                            loops.Add(loop);
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
                        loops.Add(loop);
                    }
                    else currentEdge = nextEdge;
                } while (!finishedLoop);
            }
            return loops;
        }

        static List<List<PointLight>>[] AllSlicesAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            List<List<PointLight>>[] loopsAlongZ = new List<List<PointLight>>[numSteps];
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
                    if (thisVertex.Z == z) needToOffset = true;
                    foreach (var edge in thisVertex.Edges)
                    {
                        if (currentEdges.Contains(edge)) currentEdges.Remove(edge);
                        else currentEdges.Add(edge);
                    }
                    vIndex++;
                    thisVertex = sortedVertices[vIndex];
                }
                if (needToOffset)
                    z += (z + Math.Min(stepSize / 2, sortedVertices[vIndex + 1].Z)) / 2;
                if (currentEdges.Any()) loopsAlongZ[step] = GetZLoops(currentEdges, z);
                else loopsAlongZ[step] = new List<List<PointLight>>();
            }
            return loopsAlongZ;
        }

        internal string[] GetVoxelsAsStringArrays()
        {
            var vSparse = new VoxelizedSparse(Voxels,this.Discretization,this.VoxelsPerSide,this.VoxelSideLength,
                this.Bounds);
            return vSparse.GetVoxelsAsStringArrays();
        }

        internal static List<double[]> AllPolygonIntersectionPointsAlongY(List<List<PointLight>> loops, double start, int numSteps, double stepSize,
            out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongY(loops.Select(p => new Polygon(p.Select(point => new Point(point)), true)), start, numSteps, stepSize,
                out firstIntersectingIndex);
        }
        internal static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<Polygon> polygons, double start, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Path).OrderBy(p => p.Y).ToList();
            var currentLines = new HashSet<Line>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - start) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = start + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.Y <= y)
                {
                    if (thisPoint.Y == y) needToOffset = true;
                    foreach (var line in thisPoint.Lines)
                    {
                        if (currentLines.Contains(line)) currentLines.Remove(line);
                        else currentLines.Add(line);
                    }
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    y += (y + Math.Min(stepSize / 2, sortedPoints[pIndex + 1].Y)) / 2;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.XGivenY(y);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }

       public TessellatedSolid ConvertToTessellatedSolid(Color color)
        {
            throw new NotImplementedException();
        }


        [OnSerializing()]
        protected  void OnSerializingMethod(StreamingContext context)
        {
            throw new NotImplementedException();
        }


        [OnDeserialized()]
        protected  void OnDeserializedMethod(StreamingContext context)
        {
            throw new NotImplementedException();


        }

    }
}
