// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="CUDA.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL.CUDA
{
    /// <summary>
    /// Class VoxelizedSolidCUDA.
    /// </summary>
    public partial class VoxelizedSolidCUDA
    {
        #region VoxelizedSolid Properties
        public byte[,,] Voxels;
        public readonly int Discretization;
        public readonly int[] VoxelsPerSide;
        public double VoxelSideLength { get; private set; }
        private readonly double[] Dimensions;
        public double[][] Bounds { get; protected set; }
        public double[] Offset => Bounds[0];
        #endregion

        public VoxelizedSolidCUDA(byte[,,] voxels, int discretization, double voxelSideLength, IReadOnlyList<double[]> bounds)
        {
            Voxels = voxels.Clone();
            Discretization = discretization;
            VoxelsPerSide = Voxels.Rank().ToArray();
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
        }

        public VoxelizedSolidCUDA(VoxelizedSolidCUDA vs)
        {
            Voxels = vs.Voxels.Clone();
            Discretization = vs.Discretization;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
        }

        public VoxelizedSolidCUDA(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
            int longestDimensionIndex;
            Discretization = discretization;
            var voxelsOnLongSide = Math.Pow(2, Discretization);

            double longestSide;
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[]) bounds[0].Clone();
                Bounds[1] = (double[]) bounds[1].Clone();
                Dimensions = new double[3];
                for (var i = 0; i < 3; i++)
                    Dimensions[i] = Bounds[1][i] - Bounds[0][i];
                longestSide = Dimensions.Max();
                longestDimensionIndex = Dimensions.FindIndex(d => d == longestSide);
            }
            else
            {
                // add a small buffer only if no bounds are provided.
                Dimensions = new double[3];
                for (var i = 0; i < 3; i++)
                    Dimensions[i] = ts.Bounds[1][i] - ts.Bounds[0][i];
                longestSide = Dimensions.Max();
                longestDimensionIndex = Dimensions.FindIndex(d => d == longestSide);

                const double Delta = Voxelization.Constants.fractionOfWhiteSpaceAroundFinestVoxel;
                var delta = new double[3];
                for (var i = 0; i < 3; i++)
                    delta[i] = Dimensions[i] * ((voxelsOnLongSide / (voxelsOnLongSide - (2 * Delta))) - 1) / 2;
                //var delta = longestSide * ((voxelsOnLongSide / (voxelsOnLongSide - 2 * Constants.fractionOfWhiteSpaceAroundFinestVoxel)) - 1) / 2;

                Bounds[0] = ts.Bounds[0].subtract(delta);
                Bounds[1] = ts.Bounds[1].add(delta);
                Dimensions = Dimensions.add(delta.multiply(2));
            }

            longestSide = Dimensions[longestDimensionIndex];
            VoxelSideLength = longestSide / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int) Math.Ceiling(d / VoxelSideLength)).ToArray();
            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];

            var transformedCoordinates = MakeVertexSimulatedCoordinates(ts.Vertices, ts.NumberOfVertices);
            MakeVertexVoxels(ts.Vertices, transformedCoordinates);
            MakeVoxelsForFacesAndEdges(ts.Faces, transformedCoordinates);
            UpdateVertexSimulatedCoordinates(transformedCoordinates, ts.Vertices);
            MakeVoxelsInInterior();
            //UpdateProperties();
        }

        private double[][] MakeVertexSimulatedCoordinates(IList<Vertex> vertices, int numberOfVertices)
        {
            var transformedCoords = new double[numberOfVertices][];
            var s = VoxelSideLength;
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var p = vertices[i].Position;
                transformedCoords[i] = new[]
                    {(p[0] - Offset[0]) / s, (p[1] - Offset[1]) / s, (p[2] - Offset[2]) / s};
            });
            return transformedCoords;
        }

        // this is effectively the same function as above but the 2nd, 3rd, 4th, etc. times we do it, we can just
        // overwrite the previous values.
        private void UpdateVertexSimulatedCoordinates(double[][] transformedCoordinates, IList<Vertex> vertices)
        {
            Parallel.For(0, vertices.Count, i =>
            //for (int i = 0; i < vertices.Count; i++)
            {
                var p = vertices[i].Position;
                var t = transformedCoordinates[i];
                t[0] = (p[0] - Offset[0]) / VoxelSideLength;
                t[1] = (p[1] - Offset[1]) / VoxelSideLength;
                t[2] = (p[2] - Offset[2]) / VoxelSideLength;
            });
        }

        private void MakeVertexVoxels(IEnumerable<Vertex> vertices, double[][] transformedCoordinates)
        {
            Parallel.ForEach(vertices, vertex =>
            //foreach (var vertex in vertices)
            {
                var intCoords = IntCoordsForVertex(vertex, transformedCoordinates);
                Voxels.SetValue((byte)1, intCoords);
            });
        }

        private static int[] IntCoordsForVertex(Vertex v, IReadOnlyList<double[]> transformedCoordinates)
        {
            var coordinates = transformedCoordinates[v.IndexInList];
            int[] intCoords;
            if (coordinates.Any(Voxelization.Constants.atIntegerValue))
            {
                intCoords = new int[3];
                var edgeVectors = v.Edges.Select(e => e.To == v ? e.Vector : e.Vector.multiply(-1))
                    .ToList();
                if (Voxelization.Constants.atIntegerValue(coordinates[0]) && edgeVectors.All(ev => ev[0] >= 0))
                    intCoords[0] = (int) (coordinates[0] - 1);
                else intCoords[0] = (int) coordinates[0];
                if (Voxelization.Constants.atIntegerValue(coordinates[1]) && edgeVectors.All(ev => ev[1] >= 0))
                    intCoords[1] = (int) (coordinates[1] - 1);
                else intCoords[1] = (int) coordinates[1];
                if (Voxelization.Constants.atIntegerValue(coordinates[2]) && edgeVectors.All(ev => ev[2] >= 0))
                    intCoords[2] = (int) (coordinates[2] - 1);
                else intCoords[2] = (int) coordinates[2];
            }
            else intCoords = new[] {(int) coordinates[0], (int) coordinates[1], (int) coordinates[2]};

            return intCoords;
        }

        private void MakeVoxelsForFacesAndEdges(IEnumerable<PolygonalFace> faces, IReadOnlyList<double[]> transformedCoordinates)
        {
            Parallel.ForEach(faces, face =>
            //foreach (var face in faces) //loop over the faces
            {
                if (SimpleCase(face, transformedCoordinates)) return; //continue;

                SetUpFaceSweepDetails(face, transformedCoordinates, out var startVertex, out var sweepDim,
                    out var maxSweepValue);
                var leftStartPoint = (double[]) transformedCoordinates[startVertex.IndexInList].Clone();
                var sweepValue = (int) (Voxelization.Constants.atIntegerValue(leftStartPoint[sweepDim])
                    ? leftStartPoint[sweepDim] + 1 : Math.Ceiling(leftStartPoint[sweepDim]));

                var sweepIntersections = new Dictionary<int, List<double[]>>();
                foreach (var edge in face.Edges)
                {
                    var toIntCoords = IntCoordsForVertex(edge.To, transformedCoordinates);
                    var fromIntCoords = IntCoordsForVertex(edge.From, transformedCoordinates);
                    if (toIntCoords[0] == fromIntCoords[0] && toIntCoords[1] == fromIntCoords[1] &&
                        toIntCoords[2] == fromIntCoords[2]) continue;
                    MakeVoxelsForLine(transformedCoordinates[edge.From.IndexInList],
                        transformedCoordinates[edge.To.IndexInList], sweepDim, sweepIntersections);
                }

                while (sweepValue <= maxSweepValue) // this is the sweep along the face
                {
                    if (sweepIntersections.ContainsKey(sweepValue))
                    {
                        var intersections = sweepIntersections[sweepValue];
                        if (intersections.Count == 2)
                            MakeVoxelsForLineOnFace(intersections[0], intersections[1], sweepDim);
                    }
                    sweepValue++; //increment sweepValue and repeat!
                }
            });
        }

        private bool SimpleCase(PolygonalFace face, IReadOnlyList<double[]> transformedCoordinates)
        {
            var aCoords = IntCoordsForVertex(face.A, transformedCoordinates);
            var bCoords = IntCoordsForVertex(face.B, transformedCoordinates);
            var cCoords = IntCoordsForVertex(face.C, transformedCoordinates);
            // The first simple case is that all vertices are within the same voxel. 
            if (aCoords[0] == bCoords[0] && bCoords[0] == cCoords[0] &&
                aCoords[1] == bCoords[1] && bCoords[1] == cCoords[1] &&
                aCoords[2] == bCoords[2] && bCoords[2] == cCoords[2])
                return true;
            // the second, third, and fourth simple cases are if the triangle
            // fits within a line of voxels.
            // this condition checks that all voxels have same x & y values (hence aligned in z-direction)
            if (aCoords[0] == bCoords[0] && aCoords[0] == cCoords[0] &&
                aCoords[1] == bCoords[1] && aCoords[1] == cCoords[1])
            {
                MakeVoxelsForFaceInCardinalLine(2, aCoords, bCoords[2], cCoords[2]);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (aCoords[0] == bCoords[0] && aCoords[0] == cCoords[0] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                MakeVoxelsForFaceInCardinalLine(1, aCoords, bCoords[1], cCoords[1]);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (aCoords[1] == bCoords[1] && aCoords[1] == cCoords[1] &&
                aCoords[2] == bCoords[2] && aCoords[2] == cCoords[2])
            {
                MakeVoxelsForFaceInCardinalLine(0, aCoords, bCoords[0], cCoords[0]);
                return true;
            }

            return false;
        }

        private void MakeVoxelsForFaceInCardinalLine(int dim, int[] aCoords, int bValue, int cValue)
        {
            var aValue = aCoords[dim];
            var minCoord = aValue;
            var maxCoord = aValue;
            if (bValue < minCoord) minCoord = bValue;
            else if (bValue > maxCoord)
                maxCoord = bValue;
            if (cValue < minCoord) minCoord = cValue;
            else if (cValue > maxCoord)
                maxCoord = cValue;
            var coordinates = (int[]) aCoords.Clone();
            for (var i = minCoord; i <= maxCoord; i++)
            {
                coordinates[dim] = i;
                Voxels.SetValue((byte)1, coordinates);
            }
        }

        private static void SetUpFaceSweepDetails(PolygonalFace face, IReadOnlyList<double[]> transformedCoordinates,
            out Vertex startVertex, out int sweepDim, out double maxSweepValue)
        {
            var xLength = Math.Max(Math.Max(Math.Abs(face.A.X - face.B.X), Math.Abs(face.B.X - face.C.X)),
                Math.Abs(face.C.X - face.A.X));
            var yLength = Math.Max(Math.Max(Math.Abs(face.A.Y - face.B.Y), Math.Abs(face.B.Y - face.C.Y)),
                Math.Abs(face.C.Y - face.A.Y));
            var zLength = Math.Max(Math.Max(Math.Abs(face.A.Z - face.B.Z), Math.Abs(face.B.Z - face.C.Z)),
                Math.Abs(face.C.Z - face.A.Z));
            sweepDim = 0;
            if (yLength > xLength) sweepDim = 1;
            if (zLength > yLength && zLength > xLength) sweepDim = 2;
            startVertex = face.A;
            maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.B.IndexInList][sweepDim],
                transformedCoordinates[face.C.IndexInList][sweepDim]));
            // in the following conditions the actual vertex positions are used instead of this.transformedCoordinates,
            // but it is okay since these are simply scaled and compared to one another
            if (face.B.Position[sweepDim] < face.A.Position[sweepDim])
            {
                startVertex = face.B;
                maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.C.IndexInList][sweepDim],
                    transformedCoordinates[face.A.IndexInList][sweepDim]));
            }
            if (!(face.C.Position[sweepDim] < face.B.Position[sweepDim]) ||
                !(face.C.Position[sweepDim] < face.A.Position[sweepDim])) return;
            startVertex = face.C;
            maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.A.IndexInList][sweepDim],
                transformedCoordinates[face.B.IndexInList][sweepDim]));
        }

        private void MakeVoxelsForLine(IList<double> startPoint, IList<double> endPoint, int sweepDim,
            Dictionary<int, List<double[]>> sweepIntersections)
        {
            //Get every X,Y, and Z integer value intersection
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new[] { new List<double[]>(), new List<double[]>(), new List<double[]>() };
            for (var i = 0; i < 3; i++)
                GetIntegerIntersectionsAlongLine(startPoint, endPoint, i, intersections, vectorNorm);

            //Store the sweep dimension intersections.
            foreach (var intersection in intersections[sweepDim])
            {
                var sweepValue = (int)intersection[sweepDim];
                if (sweepIntersections.ContainsKey(sweepValue))
                    sweepIntersections[sweepValue].Add(intersection);
                else sweepIntersections.Add(sweepValue, new List<double[]> { intersection });
            }
            AddVoxelsAtIntersections(intersections);
        }

        private static void GetIntegerIntersectionsAlongLine(IList<double> startPoint, IList<double> endPoint,
            int dim, List<double[]>[] intersections, double[] vectorNorm = null)
        {
            if (vectorNorm == null) vectorNorm = endPoint.subtract(startPoint).normalize();
            var start = (int)Math.Floor(startPoint[dim]);
            var end = (int)Math.Floor(endPoint[dim]);
            var forwardX = end > start;
            var uDim = (dim + 1) % 3;
            var vDim = (dim + 2) % 3;
            var t = start;
            while (t != end)
            {
                if (forwardX) t++;
                var d = (t - startPoint[dim]) / vectorNorm[dim];
                var intersection = new double[3];
                intersection[dim] = t;
                intersection[uDim] = startPoint[uDim] + d * vectorNorm[uDim];
                intersection[vDim] = startPoint[vDim] + d * vectorNorm[vDim];
                intersections[dim].Add(intersection);
                //If going reverse, do not decrement until after using this voxel index.
                if (!forwardX) t--;
            }
        }

        private void AddVoxelsAtIntersections(IEnumerable<List<double[]>> intersections)
        {
            var ijk = new int[3];
            var dimensionsAsIntegers = new bool[3];
            foreach (var intersectionSet in intersections)
            {
                foreach (var intersection in intersectionSet)
                {
                    var numAsInt = 0;
                    //Convert the intersection values to integers. 
                    for (var i = 0; i < 3; i++)
                    {
                        if (Voxelization.Constants.atIntegerValue(intersection[i]))
                        {
                            dimensionsAsIntegers[i] = true;
                            numAsInt++;
                        }
                        else dimensionsAsIntegers[i] = false;
                        ijk[i] = (int)intersection[i];
                    }

                    var numVoxels = 0;
                    foreach (var combination in Constants.TessellationToVoxelizationIntersectionCombinations)
                    {
                        var valid = true;
                        for (var j = 0; j < 3; j++)
                        {
                            if (dimensionsAsIntegers[j]) continue;
                            if (combination[j] == 0) continue;
                            //If not an integer and not 0, then do not add it to the list
                            valid = false;
                            break;
                        }
                        if (!valid) continue;
                        //This is a valid combination, so make it a voxel
                        var newIjk = new[] { ijk[0] + combination[0], ijk[1] + combination[1], ijk[2] + combination[2] };
                        Voxels.SetValue((byte)1, newIjk);
                        numVoxels++;
                    }
                    if (numVoxels != (int)Math.Pow(2, numAsInt)) throw new Exception("Error in implementation");
                }
            }
        }

        private void MakeVoxelsForLineOnFace(IList<double> startPoint, IList<double> endPoint, int sweepDim)
        {
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new[] { new List<double[]>(), new List<double[]>(), new List<double[]>() };

            for (var i = 0; i < 3; i++)
            {
                if (i == sweepDim) continue;
                GetIntegerIntersectionsAlongLine(startPoint, endPoint, i, intersections, vectorNorm);
            }
            AddVoxelsAtIntersections(intersections);
        }

        private void MakeVoxelsInInterior()
        {
            Parallel.For(0, VoxelsPerSide[0], i =>
            //for (var i = 0; i < VoxelsPerSide[0]; i++)
                {
                    for (var j = 0; j < VoxelsPerSide[1]; j++)
                    {
                        var inside = false;
                        for (var k = 0; k < VoxelsPerSide[2]; k++)
                        {
                            if (Voxels[i, j, k] == (byte) 1)
                            {
                                inside = !inside;
                                continue;
                            }

                            if (!inside) continue;
                            Voxels.SetValue((byte) 1, i, j, k);
                        }
                    }
                });
        }
    }
}
