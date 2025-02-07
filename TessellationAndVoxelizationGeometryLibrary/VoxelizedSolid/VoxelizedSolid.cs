// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    /// Class VoxelizedSparseDense.
    /// </summary>
    public partial class VoxelizedSolid : Solid, IEnumerable<(int xIndex, int yIndex, int zIndex)>, IEquatable<VoxelizedSolid>
    {
        #region Properties

        /// <summary>
        /// Gets the voxels.
        /// </summary>
        /// <value>The voxels.</value>
        internal VoxelRowBase[] voxels { get; private set; }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public long Count { get; private set; }

        /// <summary>
        /// Gets the voxels per side.
        /// </summary>
        /// <value>The voxels per side.</value>
        public int[] VoxelsPerSide => new int[] { numVoxelsX, numVoxelsY, numVoxelsZ };
        /// <summary>
        /// Gets the voxel bounds.
        /// </summary>
        /// <value>The voxel bounds.</value>
        public int[][] VoxelBounds { get; }
        /// <summary>
        /// Gets the length of the voxel side.
        /// </summary>
        /// <value>The length of the voxel side.</value>
        public double VoxelSideLength { get; private set; }

        private double inverseVoxelSideLength;
        /// <summary>
        /// Gets the dimensions.
        /// </summary>
        /// <value>The dimensions.</value>
        public Vector3 Dimensions { get; private set; }
        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public Vector3 Offset => Bounds[0];
        /// <summary>
        /// Gets the number voxels x.
        /// </summary>
        /// <value>The number voxels x.</value>
        public ushort numVoxelsX { get; private set; }
        /// <summary>
        /// Gets the number voxels y.
        /// </summary>
        /// <value>The number voxels y.</value>
        public ushort numVoxelsY { get; private set; }
        /// <summary>
        /// Gets the number voxels z.
        /// </summary>
        /// <value>The number voxels z.</value>
        public ushort numVoxelsZ { get; private set; }
        /// <summary>
        /// Gets the z multiplier.
        /// </summary>
        /// <value>The z multiplier.</value>
        private int zMultiplier => numVoxelsY;
        /// <summary>
        /// Gets the fraction dense.
        /// </summary>
        /// <value>The fraction dense.</value>
        public double FractionDense { get; private set; }


        #endregion Properties

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="VoxelizedSolid"/> class from being created.
        /// </summary>
        private VoxelizedSolid() { }


        /// <summary>
        /// Copies this instance. Note, that this overrides the base class, Solid. You may need to
        /// cast it to VoxelizedSolid in your code. E.g., var copyOfVS = (VoxelizedSolid)vs.copy;
        /// </summary>
        /// <returns>Solid.</returns>
        public VoxelizedSolid Copy()
        {
            var copy = new VoxelizedSolid
            {
                Bounds = new[] { this.Bounds[0], this.Bounds[1] },
                Dimensions = Bounds[1].Subtract(Bounds[0]),
                SolidColor = new Color(this.SolidColor.A, this.SolidColor.R, this.SolidColor.G, this.SolidColor.B),
                VoxelSideLength = this.VoxelSideLength,
                numVoxelsX = this.numVoxelsX,
                numVoxelsY = this.numVoxelsY,
                numVoxelsZ = this.numVoxelsZ,
                voxels = new VoxelRowBase[numVoxelsY * numVoxelsZ]
            };
            copy.inverseVoxelSideLength = 1 / copy.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                copy.voxels[i] = CopyToSparse(this.voxels[i]);
            copy.FractionDense = 0;
            copy.UpdateProperties();
            return copy;
        }

        /// <summary>
        /// Create a voxelized solid from a tessellated solid.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="voxelsOnLongSide"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static VoxelizedSolid CreateFrom(TessellatedSolid ts, int voxelsOnLongSide, IReadOnlyList<Vector3> bounds = null)
        {
            var copy = new VoxelizedSolid();
            if (bounds != null)
                copy.Bounds = new[] { bounds[0], bounds[1] };
            else
                copy.Bounds = new[] { ts.Bounds[0], ts.Bounds[1] };
            copy.Dimensions = copy.Bounds[1].Subtract(copy.Bounds[0]);
            copy.SolidColor = new Color(ts.SolidColor.A, ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B);
            copy.VoxelSideLength = Math.Max(copy.Dimensions.X, Math.Max(copy.Dimensions.Y, copy.Dimensions.Z)) / voxelsOnLongSide;
            copy.inverseVoxelSideLength = 1 / copy.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            copy.numVoxelsX = GetMaxNumberOfVoxels(copy.Dimensions.X, copy.VoxelSideLength, "X");
            copy.numVoxelsY = GetMaxNumberOfVoxels(copy.Dimensions.Y, copy.VoxelSideLength, "Y");
            copy.numVoxelsZ = GetMaxNumberOfVoxels(copy.Dimensions.Z, copy.VoxelSideLength, "Z");
            copy.voxels = new VoxelRowBase[copy.numVoxelsY * copy.numVoxelsZ];
            for (int i = 0; i < copy.numVoxelsY * copy.numVoxelsZ; i++)
                copy.voxels[i] = new VoxelRowSparse();
            copy.FillInFromTessellation(ts);
            copy.FractionDense = 0;
            copy.UpdateProperties();
            return copy;
        }

        /// <summary>
        /// Create a voxelized solid from a tessellated solid.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="voxelSideLength"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static VoxelizedSolid CreateFrom(TessellatedSolid ts, double voxelSideLength, IReadOnlyList<Vector3> bounds = null)
        {
            var copy = new VoxelizedSolid();
            if (bounds != null)
                copy.Bounds = new[] { bounds[0], bounds[1] };
            else
                copy.Bounds = new[] { ts.Bounds[0], ts.Bounds[1] };
            copy.Dimensions = copy.Bounds[1].Subtract(copy.Bounds[0]);
            copy.SolidColor = new Color(Constants.DefaultColor);
            copy.VoxelSideLength = voxelSideLength;
            copy.inverseVoxelSideLength = 1 / copy.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            copy.numVoxelsX = GetMaxNumberOfVoxels(copy.Dimensions.X, copy.VoxelSideLength, "X");
            copy.numVoxelsY = GetMaxNumberOfVoxels(copy.Dimensions.Y, copy.VoxelSideLength, "Y");
            copy.numVoxelsZ = GetMaxNumberOfVoxels(copy.Dimensions.Z, copy.VoxelSideLength, "Z");
            copy.voxels = new VoxelRowBase[copy.numVoxelsY * copy.numVoxelsZ];
            for (int i = 0; i < copy.numVoxelsY * copy.numVoxelsZ; i++)
                copy.voxels[i] = new VoxelRowSparse();
            copy.FillInFromTessellation(ts);
            copy.FractionDense = 0;
            copy.UpdateProperties();
            return copy;
        }

        /// <summary>
        /// Create a one-layer voxelized solid from a set of polygons
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="voxelsOnLongSide"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static VoxelizedSolid CreateFrom(IEnumerable<Polygon> loops, int voxelsOnLongSide, IReadOnlyList<Vector2> bounds)
        {
            var copy = new VoxelizedSolid();
            copy.Bounds = new[] { new Vector3(bounds[0], 0), new Vector3(bounds[1], 1) };
            copy.Dimensions = copy.Bounds[1].Subtract(copy.Bounds[0]);
            copy.VoxelSideLength = Math.Max(copy.Dimensions.X, Math.Max(copy.Dimensions.Y, copy.Dimensions.Z)) / voxelsOnLongSide;
            copy.inverseVoxelSideLength = 1 / copy.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            copy.numVoxelsX = GetMaxNumberOfVoxels(copy.Dimensions.X, copy.VoxelSideLength, "X");
            copy.numVoxelsY = GetMaxNumberOfVoxels(copy.Dimensions.Y, copy.VoxelSideLength, "Y");
            copy.numVoxelsZ = 1;
            copy.voxels = new VoxelRowBase[copy.numVoxelsY * copy.numVoxelsZ];
            for (int i = 0; i < copy.numVoxelsY * copy.numVoxelsZ; i++)
                copy.voxels[i] = new VoxelRowSparse();

            var yBegin = copy.Bounds[0][1] + copy.VoxelSideLength / 2;
            var intersections = loops.AllPolygonIntersectionPointsAlongHorizontalLines(yBegin, copy.VoxelSideLength, out var yStartIndex);
            var numYlines = intersections.Count;
            for (int j = -Math.Min(0, yStartIndex); j < numYlines; j++)
            {
                var intersectionPoints = intersections[j];
                var numXRangesOnThisLine = intersectionPoints.Length;
                for (var m = 0; m < numXRangesOnThisLine; m += 2)
                {
                    var sp = copy.ConvertXCoordToIndex(intersectionPoints[m]);
                    var ep = Math.Min((ushort)(copy.numVoxelsX - 1), copy.ConvertXCoordToIndex(intersectionPoints[m + 1]));
                    ((VoxelRowSparse)copy.voxels[yStartIndex + j]).indices.Add(sp);
                    ((VoxelRowSparse)copy.voxels[yStartIndex + j]).indices.Add(ep);
                }
            }
            //}
            copy.FractionDense = 0;
            copy.UpdateProperties();
            return copy;
        }

        #region Fill In From Tessellation Functions

        /// <summary>
        /// Fills the in from tessellation.
        /// </summary>
        /// <param name="ts">The ts.</param>
        private void FillInFromTessellation(TessellatedSolid ts)
        {
            var yBegin = YMin + VoxelSideLength / 2;
            var zBegin = ZMin + VoxelSideLength / 2;
            var decomp = ts.GetUniformlySpacedCrossSections(CartesianDirections.ZPositive, out _, out _, out _, zBegin, numVoxelsZ, VoxelSideLength);

            //Parallel.For(0, numVoxelsZ, k =>
            for (var k = 0; k < numVoxelsZ; k++)
            {
                var loops = decomp[k];
                if (loops != null && loops.Count > 0)
                {
                    var intersections = PolygonOperations.AllPolygonIntersectionPointsAlongHorizontalLines(loops, yBegin, VoxelSideLength, out var yStartIndex);
                    var numYlines = intersections.Count;
                    for (int j = yStartIndex; j < numYlines; j++)
                    {
                        var intersectionPoints = intersections[j];
                        var numXRangesOnThisLine = intersectionPoints.Length;
                        if (numXRangesOnThisLine > 0)
                        {
                            var voxelRow = (VoxelRowSparse)voxels[j + zMultiplier * k];
                            for (var m = 0; m < numXRangesOnThisLine; m += 2)
                            {
                                var sp = ConvertXCoordToIndex(intersectionPoints[m]);
                                var ep = Math.Min((ushort)(numVoxelsX - 1), ConvertXCoordToIndex(intersectionPoints[m + 1]));
                                if (sp == ep) continue;
                                var numIndices = voxelRow.indices.Count;
                                if (numIndices > 0 && voxelRow.indices[numIndices - 1] == sp)
                                    voxelRow.indices.RemoveAt(numIndices - 1);
                                else voxelRow.indices.Add(sp);
                                voxelRow.indices.Add(ep);
                            }
                        }
                    }
                }
            }
            //);
        }

        #endregion Fill In From Tessellation Functions

        /// <summary>
        /// Creates the full block of voxels using the bounds and dimensions of an existing voxelized solid.
        /// </summary>
        /// <param name="vs">The vs.</param>
        /// <returns>VoxelizedSolid.</returns>
        public static VoxelizedSolid CreateFullBlock(VoxelizedSolid vs)
        {
            return CreateFullBlock(vs.VoxelSideLength, vs.Bounds);
        }

        /// <summary>
        /// Creates the full block given the dimensions and the size.
        /// </summary>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>VoxelizedSolid.</returns>
        public static VoxelizedSolid CreateFullBlock(double voxelSideLength, IReadOnlyList<Vector3> bounds)
        {
            var fullBlock = new VoxelizedSolid();
            fullBlock.Bounds = new[] { bounds[0], bounds[1] };
            fullBlock.Dimensions = fullBlock.Bounds[1].Subtract(fullBlock.Bounds[0]);
            fullBlock.SolidColor = new Color(Constants.DefaultColor);
            fullBlock.VoxelSideLength = voxelSideLength;
            fullBlock.inverseVoxelSideLength = 1 / fullBlock.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            fullBlock.numVoxelsX = GetMaxNumberOfVoxels(fullBlock.Dimensions.X, fullBlock.VoxelSideLength, "X");
            fullBlock.numVoxelsY = GetMaxNumberOfVoxels(fullBlock.Dimensions.Y, fullBlock.VoxelSideLength, "Y");
            fullBlock.numVoxelsZ = GetMaxNumberOfVoxels(fullBlock.Dimensions.Z, fullBlock.VoxelSideLength, "Z");
            fullBlock.voxels = new VoxelRowBase[fullBlock.numVoxelsY * fullBlock.numVoxelsZ];
            for (int i = 0; i < fullBlock.numVoxelsY * fullBlock.numVoxelsZ; i++)
            {
                var fullRow = new VoxelRowSparse();
                fullRow.indices.Add(0);
                fullRow.indices.Add((ushort)fullBlock.numVoxelsX);
                fullBlock.voxels[i] = fullRow;
            }
            fullBlock.UpdateProperties();
            return fullBlock;
        }

        /// <summary>
        /// Creates the full block of voxels using the bounds and dimensions of an existing voxelized solid.
        /// </summary>
        /// <param name="vs">The vs.</param>
        /// <returns>VoxelizedSolid.</returns>
        public static VoxelizedSolid CreateEmpty(VoxelizedSolid vs)
        {
            var result = new VoxelizedSolid();
            result.Bounds = new[] { vs.Bounds[0], vs.Bounds[1] };
            result.Dimensions = result.Bounds[1].Subtract(result.Bounds[0]);
            result.SolidColor = vs.SolidColor;
            result.VoxelSideLength = vs.VoxelSideLength;
            result.inverseVoxelSideLength = 1 / result.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            result.numVoxelsX = vs.numVoxelsX;
            result.numVoxelsY = vs.numVoxelsY;
            result.numVoxelsZ = vs.numVoxelsZ;
            result.voxels = new VoxelRowBase[result.numVoxelsY * result.numVoxelsZ];
            for (int i = 0; i < result.numVoxelsY * result.numVoxelsZ; i++)
                result.voxels[i] = new VoxelRowSparse();
            result.UpdateProperties();
            return result;
        }

        /// <summary>
        /// Creates the full block given the dimensions and the size.
        /// </summary>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>VoxelizedSolid.</returns>
        public static VoxelizedSolid CreateEmpty(double voxelSideLength, IReadOnlyList<Vector3> bounds)
        {
            var result = new VoxelizedSolid();
            result.Bounds = new[] { bounds[0], bounds[1] };
            result.Dimensions = result.Bounds[1].Subtract(result.Bounds[0]);
            result.SolidColor = new Color(Constants.DefaultColor);
            result.VoxelSideLength = voxelSideLength;
            result.inverseVoxelSideLength = 1 / result.VoxelSideLength; // since its quicker to multiply then to divide, maybe doing this once at the top will save some time
            result.numVoxelsX = GetMaxNumberOfVoxels(result.Dimensions.X, result.VoxelSideLength, "X");
            result.numVoxelsY = GetMaxNumberOfVoxels(result.Dimensions.Y, result.VoxelSideLength, "Y");
            result.numVoxelsZ = GetMaxNumberOfVoxels(result.Dimensions.Z, result.VoxelSideLength, "Z");
            result.voxels = new VoxelRowBase[result.numVoxelsY * result.numVoxelsZ];
            for (int i = 0; i < result.numVoxelsY * result.numVoxelsZ; i++)
                result.voxels[i] = new VoxelRowSparse();
            result.UpdateProperties();
            return result;
        }

        private static ushort GetMaxNumberOfVoxels(double length, double voxelSideLength, string dimensionStr)
        {
            var num = Math.Ceiling(length / voxelSideLength);
            if (num > ushort.MaxValue)
                throw new Exception("Exceeds maximum voxel limit in " + dimensionStr + "-dir (" + num + " > " + ushort.MaxValue);
            return (ushort)num;
        }

        #endregion Constructors

        #region Conversion Methods

        /// <summary>
        /// Updates to all dense.
        /// </summary>
        public void UpdateToAllDense()
        {
            var numBytesInX = 1 + (numVoxelsX >> 3);
            if (FractionDense == 1) return;
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
            {
                if (voxels[i] is VoxelRowDense) continue;
                voxels[i] = CopyToDense(voxels[i], numBytesInX);
            }
            FractionDense = 1;
        }

        /// <summary>
        /// Updates to all sparse.
        /// </summary>
        public void UpdateToAllSparse()
        {
            if (FractionDense == 0) return;
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
            {
                if (voxels[i] is VoxelRowSparse) continue;
                voxels[i] = CopyToSparse(voxels[i]);
            }
            FractionDense = 0;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelRowSparse" /> struct.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="length">The length.</param>
        internal static VoxelRowSparse CopyToSparse(VoxelRowBase row)
        {
            var copy = new VoxelRowSparse();
            if (row is VoxelRowSparse sparse)
                copy.indices.AddRange(sparse.indices);
            else
                copy.indices.AddRange(GetDenseRowAsSparseIndices((VoxelRowDense)row));
            return copy;
        }

        internal static IEnumerable<ushort> GetDenseRowAsSparseIndices(VoxelRowDense denseRow)
        {
            var lastVal = false;
            ushort i = 0;
            foreach (var thisByte in denseRow.values)
            {
                var currentByte = thisByte;
                for (int j = 0; j < 8; j++)
                {
                    var currentVal = (currentByte & 0b00000001) != 0;
                    if (currentVal != lastVal)
                    {
                        lastVal = currentVal;
                        yield return i;
                    }
                    currentByte >>= 1;
                    i++;
                }
            }
            if (lastVal) yield return i;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelRowDense" /> struct.
        /// This is typically used to copy an existing dense row, or convert from
        /// a sparse row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="length">The length.</param>
        internal static VoxelRowDense CopyToDense(VoxelRowBase row, int numBytesInX)
        {
            var copy = new VoxelRowDense(numBytesInX);
            if (row is VoxelRowSparse sparse)
            {
                if (sparse.indices.Any())
                    for (int i = 0; i < sparse.indices.Count; i += 2)
                        copy.TurnOnRange(sparse.indices[i], sparse.indices[i + 1]);
            }
            else
            {
                var denseRow = (VoxelRowDense)row;
                denseRow.values.CopyTo(copy.values, 0);
            }
            return copy;
        }

        /// <summary>
        /// Gets the voxels as string arrays.
        /// </summary>
        /// <returns>System.String[].</returns>
        internal string[] GetVoxelsAsStringArrays()
        {
            UpdateToAllSparse();
            var allRows = new List<string>();
            for (int i = 0; i < numVoxelsY; i++)
            {
                for (int j = 0; j < numVoxelsZ; j++)
                {
                    var sparseRow = (VoxelRowSparse)voxels[i + j * zMultiplier];
                    if (sparseRow.indices.Any())
                    {
                        var rowDetails = new List<ushort>(sparseRow.indices);
                        rowDetails.Insert(0, (ushort)j);
                        rowDetails.Insert(0, (ushort)i);
                        allRows.Add(BitConverter.ToString(rowDetails.SelectMany(BitConverter.GetBytes).ToArray()));
                    }
                }
            }
            return allRows.ToArray();
        }

        /// <summary>
        /// Converts to tessellated solid rectilinear.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public TessellatedSolid ConvertToTessellatedSolidRectilinear()
        {
            Message.output("Converting voxelized solid to tessellated solid rectilinear", 2);
            var vertexDictionary = new Dictionary<long, Vertex>();
            var faces = new List<TriangleFace>();
            var s = VoxelSideLength;
            foreach (var expVox in GetExposedVoxelsWithSides())
            {
                var iBase = expVox.xIndex;
                var jBase = expVox.yIndex;
                var kBase = expVox.zIndex;
                var neighbors = new[] { expVox.xNeg, expVox.xPos, expVox.yNeg, expVox.yPos, expVox.zNeg, expVox.zPos };
                for (var m = 0; m < 12; m++)
                {
                    if (neighbors[m / 2])
                    {
                        var faceVertices = new Vertex[3];
                        for (var n = 0; n < 3; n++)
                        {
                            var i = iBase + coordOffsets[m][n][0];
                            var j = jBase + coordOffsets[m][n][1];
                            var k = kBase + coordOffsets[m][n][2];
                            if (vertexDictionary.TryGetValue(GetLongID(i, j, k), out var vertex))
                                faceVertices[n] = vertex;
                            else
                            {
                                vertex = new Vertex(i * s + XMin, j * s + YMin, k * s + ZMin);
                                vertexDictionary.Add(GetLongID(i, j, k), vertex);
                                faceVertices[n] = vertex;
                            }
                        }
                        faces.Add(new TriangleFace(faceVertices[0], faceVertices[1], faceVertices[2], true));
                    }
                }
            }
            var result = new TessellatedSolid(faces, vertexDictionary.Values);
            result.Comments = new List<string>(Comments);
            result.HasUniformColor = HasUniformColor;
            result.Language = Language;
            result.Name = Name;
            result.ReferenceIndex = ReferenceIndex;
            result.SameTolerance = SameTolerance;
            result.SolidColor = SolidColor;
            result.Units = Units;
            return result;
        }
        public long GetLongID(int x, int y, int z) => (long)x + ((long)y << 21) + ((long)z << 42);
        public (int x, int y, int z) GetIndicesFromID(long id)
        {
            var x = (int)(id & 0x1FFFFF);
            var y = (int)((id >> 21) & 0x1FFFFF);
            var z = (int)((id >> 42) & 0x1FFFFF);
            return (x, y, z);
        }

        static readonly int[][][] coordOffsets =
        {
            new[]{ new int[] {0, 0, 0}, new int[] { 0, 0, 1}, new int[] {0, 1, 0}},
            new[]{ new int[] {0, 1, 0}, new int[] {0, 0, 1}, new int[] {0, 1, 1}}, //x-neg
            new[]{ new int[] {1, 0, 0}, new int[] {1, 1, 0}, new int[] {1, 0, 1}},
            new[]{ new int[] {1, 1, 0}, new int[] {1, 1, 1}, new int[] {1, 0, 1}}, //x-pos
            new[]{ new int[] {0, 0, 0}, new int[] { 1, 0, 0}, new int[] {0, 0, 1}},
            new[]{ new int[] {1, 0, 0}, new int[] {1, 0, 1}, new int[] {0, 0, 1}}, //y-neg
            new[]{ new int[] {0, 1, 0}, new int[] {0, 1, 1}, new int[] {1, 1, 0}},
            new[]{ new int[] {1, 1, 0}, new int[] {0, 1, 1}, new int[] {1, 1, 1}}, //y-pos
            new[]{ new int[] {0, 0, 0}, new int[] {0, 1, 0}, new int[] {1, 0, 0}},
            new[]{new int[] {1, 0, 0}, new int[] {0, 1, 0}, new int[] {1, 1, 0}}, //z-neg
            new[]{ new int[] {0, 0, 1}, new int[] {1, 0, 1}, new int[] {0, 1, 1}},
            new[]{ new int[] {1, 0, 1}, new int[] {1, 1, 1}, new int[] {0, 1, 1}}, //z-pos
        };

        /// <summary>
        /// Converts to tessellated solid marching cubes.
        /// </summary>
        /// <param name="voxelsPerTriangleSpacing">The voxels per triangle spacing.</param>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(int voxelsPerTriangleSpacing)
        {
            var marchingCubes = new MarchingCubesDenseVoxels(this, voxelsPerTriangleSpacing);
            var ts = marchingCubes.Generate();
            ts.Comments = new List<string>(Comments);
            ts.HasUniformColor = HasUniformColor;
            ts.Language = Language;
            ts.Name = Name;
            ts.ReferenceIndex = ReferenceIndex;
            ts.SameTolerance = SameTolerance;
            ts.SolidColor = SolidColor;
            ts.Units = Units;
            return ts;
        }

        #endregion Conversion Methods

        #region Overrides of Solid abstract members
        /// <summary>
        /// Transforms the solid with the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new version of the solid transformed by the specified transform matrix.
        /// </summary>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>Solid.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="TVertex:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VoxelEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of voxels, which is simply
        /// a boolean.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<(int xIndex, int yIndex, int zIndex)> GetEnumerator()
        {
            return new VoxelEnumerator(this);
        }

        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected override void CalculateCenter()
        {
            Count = 0;
            var xTotal = 0;
            var yTotal = 0;
            var zTotal = 0;
            for (int j = 0; j < numVoxelsY; j++)
                for (int k = 0; k < numVoxelsZ; k++)
                {
                    var voxelRow = voxels[j + zMultiplier * k];
                    if (voxelRow != null)
                    {
                        var rowCount = voxelRow.Count;
                        xTotal += rowCount * voxelRow.AverageXPosition();
                        yTotal += rowCount * j;
                        zTotal += rowCount * k;
                        Count += rowCount;
                    }
                }
            _center = new Vector3  //is this right?
            (
                VoxelSideLength * xTotal / Count,
                VoxelSideLength * yTotal / Count,
                VoxelSideLength * zTotal / Count
            );
        }

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected override void CalculateVolume()
        {
            _volume = Count * VoxelSideLength * VoxelSideLength * VoxelSideLength;
        }

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateSurfaceArea()
        {
            var num = 0;
            foreach ((int xIndex, int yIndex, int zIndex, bool xNeg, bool xPos, bool yNeg, bool yPos, bool zNeg, bool zPos) in GetExposedVoxelsWithSides())
            {
                if (xNeg) num++;
                if (xPos) num++;
                if (yNeg) num++;
                if (yPos) num++;
                if (zNeg) num++;
                if (zPos) num++;
            }
            _surfaceArea = num * VoxelSideLength * VoxelSideLength;
        }

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }

        #endregion Overrides of Solid abstract members

        #region Getting Voxels and Neighbors

        public IEnumerable<(int xIndex, int yIndex, int zIndex)> GetExposedVoxels()
        {
            for (int zCoord = 0; zCoord < numVoxelsZ; zCoord++)
                foreach (var vox in GetExposedVoxels(zCoord))
                    yield return vox;
        }
        public IEnumerable<(int xIndex, int yIndex, int zIndex)> GetExposedVoxels(int zCoord)
        {
            for (int yCoord = 0; yCoord < numVoxelsY; yCoord++)
            {
                var xCoord = -1;
                var nextX = -1;
                foreach (var xValue in voxels[yCoord + zMultiplier * zCoord].XIndices())
                {
                    var lastX = xCoord;
                    xCoord = nextX;
                    nextX = xValue;
                    if (xCoord < 0) continue;
                    if (xCoord - lastX > 1 || nextX - xCoord > 1
                    || yCoord == 0 || yCoord + 1 >= numVoxelsY || zCoord == 0 || zCoord + 1 >= numVoxelsZ
                     || !voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]
                     || !voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]
                     || !voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]
                     || !voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord])
                        yield return (xCoord, yCoord, zCoord);
                    if (nextX >= numVoxelsX) break;
                }
                if (nextX >= 0 && nextX < numVoxelsX) yield return (nextX, yCoord, zCoord);
            }
        }
        public IEnumerable<(int xIndex, int yIndex, int zIndex, bool xNeg, bool xPos, bool yNeg, bool yPos, bool zNeg, bool zPos)> GetExposedVoxelsWithSides()
        {
            //var collection = new BlockingCollection<(int xIndex, int yIndex, int zIndex, bool xNeg, bool xPos, bool yNeg, bool yPos, bool zNeg, bool zPos)>();

            //Task.Run(() =>
            //{
            //    Parallel.For(0, numVoxelsZ, zCoord =>
            //    {
            //        foreach (var vox in GetExposedVoxelsWithSides(zCoord))
            //            collection.Add(vox);
            //    });

            //    collection.CompleteAdding();
            //});

            //foreach (var result in collection.GetConsumingEnumerable())
            //    yield return result;
            for (int zCoord = 0; zCoord < numVoxelsZ; zCoord++)
            {
                foreach (var vox in GetExposedVoxelsWithSides(zCoord))
                    yield return vox;
            }
        }

        public IEnumerable<(int xIndex, int yIndex, int zIndex, bool xNeg, bool xPos, bool yNeg, bool yPos, bool zNeg, bool zPos)> GetExposedVoxelsWithSides(int zCoord)
        {
            for (int yCoord = 0; yCoord < numVoxelsY; yCoord++)
            {
                using (var enumerator = voxels[yCoord + zMultiplier * zCoord].XIndices().GetEnumerator())
                {
                    if (!enumerator.MoveNext()) continue;
                    var xCoord = enumerator.Current;
                    var xNeg = true;
                    var xPos = !enumerator.MoveNext();
                    ushort nextX;
                    if (xPos) nextX = numVoxelsX;
                    else
                    {
                        nextX = enumerator.Current;
                        xPos = nextX - xCoord > 1;
                    }
                    var yNeg = yCoord == 0 || !voxels[yCoord - 1 + zMultiplier * zCoord][xCoord];
                    var yPos = yCoord + 1 >= numVoxelsY || !voxels[yCoord + 1 + zMultiplier * zCoord][xCoord];
                    var zNeg = zCoord == 0 || !voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord];
                    var zPos = zCoord + 1 >= numVoxelsZ || !voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord];
                    if (xNeg || xPos || yNeg || yPos || zNeg || zPos)
                        yield return (xCoord, yCoord, zCoord, xNeg, xPos, yNeg, yPos, zNeg, zPos);
                    if (nextX >= numVoxelsX) continue;

                    while (true)
                    {
                        var lastX = xCoord;
                        xCoord = nextX;
                        xNeg = xCoord - lastX > 1;
                        xPos = !enumerator.MoveNext();
                        if (xPos) nextX = numVoxelsX;
                        else
                        {
                            nextX = enumerator.Current;
                            xPos = nextX - xCoord > 1;
                        }
                        yNeg = yCoord == 0 || !voxels[yCoord - 1 + zMultiplier * zCoord][xCoord];
                        yPos = yCoord + 1 >= numVoxelsY || !voxels[yCoord + 1 + zMultiplier * zCoord][xCoord];
                        zNeg = zCoord == 0 || !voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord];
                        zPos = zCoord + 1 >= numVoxelsZ || !voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord];
                        if (xNeg || xPos || yNeg || yPos || zNeg || zPos)
                            yield return (xCoord, yCoord, zCoord, xNeg, xPos, yNeg, yPos, zNeg, zPos);
                        if (nextX >= numVoxelsX) break;
                    }
                }
            }
        }

        #region Public Methods that Branch
        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[ushort xCoord, ushort yCoord, ushort zCoord]
        {
            get
            {
                if (xCoord >= numVoxelsX || yCoord >= numVoxelsY || zCoord >= numVoxelsZ)
                    // this is needed because the end voxel index in sparse is sometimes
                    // set to ushort.MaxValue
                    return false;
                return voxels[yCoord + zMultiplier * zCoord][xCoord];
            }
            set
            {
                if (xCoord >= numVoxelsX || yCoord >= numVoxelsY || zCoord >= numVoxelsZ)
                    throw new ArgumentOutOfRangeException("Voxel index out of range");
                voxels[yCoord + zMultiplier * zCoord][xCoord] = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int xCoord, int yCoord, int zCoord]
        {
            get
            {
                if (xCoord < 0 || yCoord < 0 || zCoord < 0
                    || xCoord >= numVoxelsX || yCoord >= numVoxelsY || zCoord >= numVoxelsZ)
                    return false;
                return voxels[yCoord + zMultiplier * zCoord][xCoord];
            }
            set
            {
                if (xCoord < 0 || yCoord < 0 || zCoord < 0
                    || xCoord >= numVoxelsX || yCoord >= numVoxelsY || zCoord >= numVoxelsZ)
                    throw new ArgumentOutOfRangeException("Voxel index out of range");
                voxels[yCoord + zMultiplier * zCoord][xCoord] = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int[] coordinates]
        {
            get => this[coordinates[0], coordinates[1], coordinates[2]];
            set => this[coordinates[0], coordinates[1], coordinates[2]] = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[ushort[] coordinates]
        {
            get => this[coordinates[0], coordinates[1], coordinates[2]];
            set => this[coordinates[0], coordinates[1], coordinates[2]] = value;
        }

        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is true if there are neighbors and false if there are none.
        /// the neighbors array is the coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="neighbors">The neighbors.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool GetNeighbors(int xCoord, int yCoord, int zCoord, out int[][] neighbors)
        {
            neighbors = GetNeighbors(xCoord, yCoord, zCoord);
            return neighbors.Any(n => n != null);
        }

        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is an array of coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns>System.Int32[][].</returns>
        public int[][] GetNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (xNeighbors.Item1)
                neighbors[0] = new[] { xCoord - 1, yCoord, zCoord };
            if (xNeighbors.Item2)
                neighbors[1] = new[] { xCoord + 1, yCoord, zCoord };

            if (yCoord > 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord])
                neighbors[2] = new[] { xCoord, yCoord - 1, zCoord };
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord])
                neighbors[3] = new[] { xCoord, yCoord + 1, zCoord };

            if (zCoord > 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord])
                neighbors[4] = new[] { xCoord, yCoord, zCoord - 1 };
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord])
                neighbors[5] = new[] { xCoord, yCoord, zCoord + 1 };

            return neighbors;
        }


        /// <summary>
        /// Returns the number of adjacent voxels (0 to 6)
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns>System.Int32.</returns>
        public int NumNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = 0;

            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (xNeighbors.Item1) neighbors++;
            if (xNeighbors.Item2) neighbors++;

            if (yCoord != 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]) neighbors++;
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]) neighbors++;

            if (zCoord != 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]) neighbors++;
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord]) neighbors++;

            return neighbors;
        }

        /// <summary>
        /// Reports whether the specified voxel is exposed. An exposed voxel lacks a direct neighbor on one or
        /// more of its 6 sides.
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="zCoord"></param>
        /// <returns></returns>
        public bool IsExposed(int xCoord, int yCoord, int zCoord)
        {
            if (!this[xCoord, yCoord, zCoord]) return false;
            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (!xNeighbors.Item1) return true;
            if (!xNeighbors.Item2) return true;
            if (yCoord == 0 || yCoord + 1 >= numVoxelsY || zCoord == 0 || zCoord + 1 >= numVoxelsZ)
                return true;
            if (!voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]) return true;
            if (!voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]) return true;
            if (!voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]) return true;
            if (!voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord]) return true;
            return false;
        }

        /// <summary>
        /// Converts a 3D coordinate in the the voxels indices that occupy the point.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public ushort[] ConvertCoordinatesToIndices(Vector3 coordinates)
        {
            return new[]
            {
                ConvertXCoordToIndex(coordinates.X),
                ConvertYCoordToIndex(coordinates.Y),
                ConvertZCoordToIndex(coordinates.Z)
            };
        }

        /// <summary>
        /// Converst the x-coordinate to the index of the voxel that occupies the point.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public ushort ConvertXCoordToIndex(double x)
        {
            if (x < Offset.X) return 0;
            return (ushort)(inverseVoxelSideLength * (x - Offset.X));
        }
        /// <summary>
        /// Converts the y-coordinate to the index of the voxel that occupies the point.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public ushort ConvertYCoordToIndex(double y)
        {
            if (y < Offset.Y) return 0;
            return (ushort)(inverseVoxelSideLength * (y - Offset.Y));
        }
        /// <summary>
        /// Converts the z-coordinate to the index of the voxel that occupies the point.
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public ushort ConvertZCoordToIndex(double z)
        {
            if (z < Offset.Z) return 0;
            return (ushort)(inverseVoxelSideLength * (z - Offset.Z));
        }
        /// <summary>
        /// Converts the x-index of a voxel to the x-coordinate of the lower bound of the voxel.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double ConvertXIndexToCoord(int i) => Offset.X + (i + 0.5) * VoxelSideLength;

        /// <summary>
        /// Converts the y-index of a voxel to the y-coordinate of the lower bound of the voxel.
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        public double ConvertYIndexToCoord(int j) => Offset.Y + (j + 0.5) * VoxelSideLength;

        /// <summary>
        /// Converts the z-index of a voxel to the z-coordinate of the lower bound of the voxel.
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public double ConvertZIndexToCoord(int k) => Offset.Z + (k + 0.5) * VoxelSideLength;

        /// <summary>
        /// Converts the indices of a voxel to the 3D-coordinates of the lower bound of the voxel.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        public Vector3 ConvertIndicesToCoordinates(int[] indices) => new Vector3(ConvertXIndexToCoord(indices[0]),
            ConvertYIndexToCoord(indices[1]), ConvertZIndexToCoord(indices[2]));

        /// <summary>
        /// Converts the indices of a voxel to the 3D-coordinates of the lower bound of the voxel.
        /// </summary>
        /// <param name="xIndex"></param>
        /// <param name="yIndex"></param>
        /// <param name="zIndex"></param>
        /// <returns></returns>
        public Vector3 ConvertIndicesToCoordinates(int xIndex, int yIndex, int zIndex) => new Vector3(ConvertXIndexToCoord(xIndex),
            ConvertYIndexToCoord(yIndex), ConvertZIndexToCoord(zIndex));

        #endregion

        /// <summary>
        /// Updates the properties.
        /// </summary>
        /// <font color="red">Badly formed XML comment.</font>
        public void UpdateProperties()
        {
            CalculateCenter();
            CalculateVolume();
        }

        /// <summary>
        /// Determines if two voxelized solids are identical. This is a deep comparison and is not possible
        /// for most solid types
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Equals(VoxelizedSolid other)
        {
            if (other.numVoxelsX != numVoxelsX || other.numVoxelsY != numVoxelsY || other.numVoxelsZ != numVoxelsZ) return false;
            for (int i = 0; i < voxels.Length; i++)
            {
                var thisVoxelRowSparse = voxels[i] as VoxelRowSparse ?? CopyToSparse(voxels[i]);
                var otherVoxelRowSparse = other.voxels[i] as VoxelRowSparse ?? CopyToSparse(other.voxels[i]);
                var numIndices = thisVoxelRowSparse.indices.Count;
                if (numIndices != otherVoxelRowSparse.indices.Count) return false;
                for (int j = 0; j < numIndices; j++)
                {
                    if (thisVoxelRowSparse.indices[j] != otherVoxelRowSparse.indices[j]) return false;
                }
            }
            return true;
        }


        #endregion
    }
}