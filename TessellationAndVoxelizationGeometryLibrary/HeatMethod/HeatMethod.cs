// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="HeatMethod.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>Computes geodesic distances on triangulated surface meshes using the Heat Method.</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Computes estimated geodesic distances on a surface mesh (<see cref="TessellatedSolid"/>)
    /// from one or more source vertices using the Heat Method.
    /// Performs an initial preprocessing step so that repeated distance queries with varying
    /// source vertices take minimal time.
    /// </summary>
    public class HeatMethod
    {
        private readonly TessellatedSolid _solid;
        private readonly GeodesicDistanceMode _mode;
        private readonly int _dimension;

        private double _timeStep;
        private SparseMatrix _massMatrix = null!;
        private SparseMatrix _cotanMatrix = null!;
        private SparsePcgSolver _heatSolver = null!;
        private SparsePcgSolver _poissonSolver = null!;

        private readonly HashSet<int> _sources = new();
        private bool _sourceChangeFlag = true;
        private double[] _solvedPhi = Array.Empty<double>();

        // Geometry data for either Direct or Intrinsic Delaunay mode
        private int[][] _faceVertexIndices = null!;
        private Vector3[][] _facePoints = null!;

        /// <summary>
        /// Gets the triangulated solid mesh.
        /// </summary>
        public TessellatedSolid Solid => _solid;

        /// <summary>
        /// Gets the geodesic distance computation mode.
        /// </summary>
        public GeodesicDistanceMode Mode => _mode;

        /// <summary>
        /// Gets the heat diffusion time step t = h^2.
        /// </summary>
        public double TimeStep => _timeStep;

        /// <summary>
        /// Gets the active source vertex indices.
        /// </summary>
        public IReadOnlySet<int> Sources => _sources;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatMethod"/> class.
        /// </summary>
        /// <param name="solid">The triangulated solid mesh.</param>
        /// <param name="mode">The computation mode (default is <see cref="GeodesicDistanceMode.IntrinsicDelaunay"/>).</param>
        public HeatMethod(TessellatedSolid solid, GeodesicDistanceMode mode = GeodesicDistanceMode.IntrinsicDelaunay)
        {
            _solid = solid ?? throw new ArgumentNullException(nameof(solid));
            _mode = mode;
            _dimension = solid.NumberOfVertices;

            Build();
        }

        private void Build()
        {
            if (_dimension == 0) return;

            int numFaces = _solid.NumberOfFaces;
            double edgeLengthSum = 0.0;
            int numEdges = 0;

            if (_mode == GeodesicDistanceMode.IntrinsicDelaunay)
            {
                var idt = new IntrinsicDelaunayTriangulation(_solid);
                _faceVertexIndices = idt.FaceVertexIndices;
                _facePoints = idt.FaceCoordinates;
                numEdges = idt.NumberOfEdges;
                for (int e = 0; e < idt.NumberOfEdges; e++)
                    edgeLengthSum += idt.EdgeLengths[e];
            }
            else
            {
                _faceVertexIndices = new int[numFaces][];
                _facePoints = new Vector3[numFaces][];
                for (int f = 0; f < numFaces; f++)
                {
                    var face = _solid.Faces[f];
                    _faceVertexIndices[f] = [face.A.IndexInList, face.B.IndexInList, face.C.IndexInList];
                    _facePoints[f] = [face.A.Coordinates, face.B.Coordinates, face.C.Coordinates];
                }

                numEdges = _solid.NumberOfEdges;
                foreach (var edge in _solid.Edges)
                    edgeLengthSum += edge.Length;
            }

            _massMatrix = new SparseMatrix(_dimension, _dimension);
            _cotanMatrix = new SparseMatrix(_dimension, _dimension);

            for (int f = 0; f < numFaces; f++)
            {
                int i = _faceVertexIndices[f][0];
                int j = _faceVertexIndices[f][1];
                int k = _faceVertexIndices[f][2];

                var pi = _facePoints[f][0];
                var pj = _facePoints[f][1];
                var pk = _facePoints[f][2];

                var vij = pj - pi;
                var vik = pk - pi;
                var vji = pi - pj;
                var vjk = pk - pj;
                var vki = pi - pk;
                var vkj = pj - pk;

                var cross = vij.Cross(vik);
                double normCross = cross.Length();
                double twoArea = Math.Max(normCross, 1e-30);

                // Cotangent weights for the 3 angles: cot(alpha) = (u . v) / ||u x v||
                double cotanI = vij.Dot(vik) / twoArea;
                double cotanJ = vji.Dot(vjk) / twoArea;
                double cotanK = vki.Dot(vkj) / twoArea;

                // Edge (j, k) opposite to vertex i
                _cotanMatrix.Add(j, k, -0.5 * cotanI);
                _cotanMatrix.Add(k, j, -0.5 * cotanI);
                _cotanMatrix.Add(j, j, 0.5 * cotanI);
                _cotanMatrix.Add(k, k, 0.5 * cotanI);

                // Edge (i, k) opposite to vertex j
                _cotanMatrix.Add(i, k, -0.5 * cotanJ);
                _cotanMatrix.Add(k, i, -0.5 * cotanJ);
                _cotanMatrix.Add(i, i, 0.5 * cotanJ);
                _cotanMatrix.Add(k, k, 0.5 * cotanJ);

                // Edge (i, j) opposite to vertex k
                _cotanMatrix.Add(i, j, -0.5 * cotanK);
                _cotanMatrix.Add(j, i, -0.5 * cotanK);
                _cotanMatrix.Add(i, i, 0.5 * cotanK);
                _cotanMatrix.Add(j, j, 0.5 * cotanK);

                // Lumped mass matrix: 1/3 of face area = 1/6 of normCross
                _massMatrix.Add(i, i, (1.0 / 6.0) * normCross);
                _massMatrix.Add(j, j, (1.0 / 6.0) * normCross);
                _massMatrix.Add(k, k, (1.0 / 6.0) * normCross);

                // Regularizer for Laplacian positive-definiteness (matching CGAL)
                _cotanMatrix.Add(i, i, 1e-8);
                _cotanMatrix.Add(j, j, 1e-8);
                _cotanMatrix.Add(k, k, 1e-8);
            }

            _cotanMatrix.Compress();
            _massMatrix.Compress();

            // Time step t = h^2 where h is mean edge length
            double meanEdgeLength = numEdges > 0 ? edgeLengthSum / numEdges : 1.0;
            _timeStep = meanEdgeLength * meanEdgeLength;

            // Heat operator A = M + t * Lc
            var heatOperator = SparseMatrix.Combine(_massMatrix, _cotanMatrix, 1.0, _timeStep);

            // Linear solvers
            _heatSolver = new SparsePcgSolver(heatOperator);
            _poissonSolver = new SparsePcgSolver(_cotanMatrix);

            _sourceChangeFlag = true;
        }

        /// <summary>
        /// Adds a vertex to the source set.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns><c>true</c> if the vertex was added; <c>false</c> if it was already in the set.</returns>
        public bool AddSource(Vertex vertex)
        {
            return AddSource(vertex.IndexInList);
        }

        /// <summary>
        /// Adds a vertex index to the source set.
        /// </summary>
        /// <param name="vertexIndex">The vertex index.</param>
        /// <returns><c>true</c> if the vertex was added; <c>false</c> if it was already in the set.</returns>
        public bool AddSource(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= _dimension)
                throw new ArgumentOutOfRangeException(nameof(vertexIndex));

            if (_sources.Add(vertexIndex))
            {
                _sourceChangeFlag = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a range of vertices to the source set.
        /// </summary>
        public void AddSources(IEnumerable<Vertex> vertices)
        {
            foreach (var v in vertices)
                AddSource(v.IndexInList);
        }

        /// <summary>
        /// Adds a range of vertex indices to the source set.
        /// </summary>
        public void AddSources(IEnumerable<int> vertexIndices)
        {
            foreach (var idx in vertexIndices)
                AddSource(idx);
        }

        /// <summary>
        /// Removes a vertex from the source set.
        /// </summary>
        public bool RemoveSource(Vertex vertex)
        {
            return RemoveSource(vertex.IndexInList);
        }

        /// <summary>
        /// Removes a vertex index from the source set.
        /// </summary>
        public bool RemoveSource(int vertexIndex)
        {
            if (_sources.Remove(vertexIndex))
            {
                _sourceChangeFlag = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all vertices from the source set.
        /// </summary>
        public void ClearSources()
        {
            if (_sources.Count > 0)
            {
                _sources.Clear();
                _sourceChangeFlag = true;
            }
        }

        /// <summary>
        /// Estimates the geodesic distances from the current source set to all vertices in the solid mesh.
        /// </summary>
        /// <returns>An array of geodesic distance values corresponding to each vertex in the mesh.</returns>
        public double[] EstimateGeodesicDistances()
        {
            if (_dimension == 0) return Array.Empty<double>();

            if (_sourceChangeFlag)
            {
                // Step 1: Heat diffusion (M + t * Lc) * u = delta_S
                var kronecker = new double[_dimension];
                if (_sources.Count == 0)
                {
                    kronecker[0] = 1.0;
                }
                else
                {
                    foreach (var s in _sources)
                        kronecker[s] = 1.0;
                }

                var u = _heatSolver.Solve(kronecker);

                // Step 2: Compute normalized vector field X = -grad(u) / ||grad(u)||
                int numFaces = _faceVertexIndices.Length;
                var X = new Vector3[numFaces];

                for (int f = 0; f < numFaces; f++)
                {
                    int i = _faceVertexIndices[f][0];
                    int j = _faceVertexIndices[f][1];
                    int k = _faceVertexIndices[f][2];

                    var pi = _facePoints[f][0];
                    var pj = _facePoints[f][1];
                    var pk = _facePoints[f][2];

                    var vij = pj - pi;
                    var vik = pk - pi;
                    var vjk = pk - pj;
                    var vki = pi - pk;

                    var cross = vij.Cross(vik);
                    double normCross = cross.Length();
                    double areaFace = 0.5 * Math.Max(normCross, 1e-30);
                    var unitNormal = normCross > 1e-30 ? cross / normCross : Vector3.UnitZ;

                    double ui = Math.Abs(u[i]);
                    double uj = Math.Abs(u[j]);
                    double uk = Math.Abs(u[k]);

                    double maxU = Math.Max(ui, Math.Max(uj, uk));
                    if (maxU > 1e-30 && !double.IsInfinity(maxU))
                    {
                        ui /= maxU;
                        uj /= maxU;
                        uk /= maxU;
                    }

                    // Vector field proportional to -grad(u)
                    var edgeSums = (unitNormal.Cross(vij) * uk)
                                 + (unitNormal.Cross(vjk) * ui)
                                 + (unitNormal.Cross(vki) * uj);

                    edgeSums /= areaFace;
                    double eMag = edgeSums.Length();
                    X[f] = eMag > 1e-30 ? edgeSums / eMag : Vector3.Zero;
                }

                // Step 3: Compute integrated divergence of X at vertices
                var divX = new double[_dimension];

                for (int f = 0; f < numFaces; f++)
                {
                    int i = _faceVertexIndices[f][0];
                    int j = _faceVertexIndices[f][1];
                    int k = _faceVertexIndices[f][2];

                    var pi = _facePoints[f][0];
                    var pj = _facePoints[f][1];
                    var pk = _facePoints[f][2];

                    var vij = pj - pi;
                    var vik = pk - pi;
                    var vji = pi - pj;
                    var vjk = pk - pj;
                    var vki = pi - pk;
                    var vkj = pj - pk;

                    double twoArea = Math.Max(vij.Cross(vik).Length(), 1e-30);

                    double cotanI = vij.Dot(vik) / twoArea;
                    double cotanJ = vji.Dot(vjk) / twoArea;
                    double cotanK = vki.Dot(vkj) / twoArea;

                    var a = X[f];
                    double iEntry = a.Dot(vij) * cotanK + a.Dot(vik) * cotanJ;
                    double jEntry = a.Dot(vjk) * cotanI + a.Dot(vji) * cotanK;
                    double kEntry = a.Dot(vki) * cotanJ + a.Dot(vkj) * cotanI;

                    divX[i] += 0.5 * iEntry;
                    divX[j] += 0.5 * jEntry;
                    divX[k] += 0.5 * kEntry;
                }

                // Step 4: Solve Poisson equation Lc * phi = divX
                var phi = _poissonSolver.Solve(divX);

                // Step 5: Shift by source set
                var result = new double[_dimension];
                if (_sources.Count == 0)
                {
                    double phi0 = phi[0];
                    for (int i = 0; i < _dimension; i++)
                        result[i] = Math.Abs(phi[i] - phi0);
                }
                else
                {
                    for (int i = 0; i < _dimension; i++)
                    {
                        double minVal = double.MaxValue;
                        foreach (int s in _sources)
                        {
                            double dist = Math.Abs(phi[i] - phi[s]);
                            if (dist < minVal)
                                minVal = dist;
                        }
                        result[i] = _sources.Contains(i) ? 0.0 : minVal;
                    }
                }

                _solvedPhi = result;
                _sourceChangeFlag = false;
            }

            var output = new double[_dimension];
            Array.Copy(_solvedPhi, output, _dimension);
            return output;
        }

        /// <summary>
        /// Estimates the geodesic distances and populates the provided dictionary.
        /// </summary>
        /// <param name="distances">The dictionary mapping vertices to geodesic distances.</param>
        public void EstimateGeodesicDistances(IDictionary<Vertex, double> distances)
        {
            var d = EstimateGeodesicDistances();
            for (int i = 0; i < _solid.NumberOfVertices; i++)
            {
                distances[_solid.Vertices[i]] = d[i];
            }
        }
    }
}

