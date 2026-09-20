// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="IntrinsicDelaunayTriangulation.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>Intrinsic Delaunay Triangulation (iDT) implementation for triangle meshes.</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Represents an Intrinsic Delaunay Triangulation (iDT) of a triangulated surface mesh.
    /// It maintains intrinsic edge lengths and connectivity while flipping edges until the Delaunay
    /// condition holds for all interior edges, without altering the 3D embedding.
    /// </summary>
    public class IntrinsicDelaunayTriangulation
    {
        private struct Halfedge
        {
            public int Source;
            public int Target;
            public int Face;
            public int Next;
            public int Prev;
            public int Opposite;
            public int Edge;
        }

        private readonly int _numVertices;
        private readonly int _numFaces;
        private int _numEdges;
        private Halfedge[] _halfedges;
        private int[] _faceHalfedges;
        private int[] _edgeHalfedges;
        private double[] _edgeLengths;

        /// <summary>
        /// Per-face vertex indices (3 per face).
        /// </summary>
        public int[][] FaceVertexIndices { get; private set; }

        /// <summary>
        /// Per-face local 2D coordinates for the 3 vertices, stored as Vector3 with z = 0.
        /// </summary>
        public Vector3[][] FaceCoordinates { get; private set; }

        /// <summary>
        /// Gets the number of vertices in the mesh.
        /// </summary>
        public int NumberOfVertices => _numVertices;

        /// <summary>
        /// Gets the number of faces in the mesh.
        /// </summary>
        public int NumberOfFaces => _numFaces;

        /// <summary>
        /// Gets the number of edges in the mesh.
        /// </summary>
        public int NumberOfEdges => _numEdges;

        /// <summary>
        /// Gets the intrinsic edge lengths.
        /// </summary>
        public IReadOnlyList<double> EdgeLengths => _edgeLengths;

        /// <summary>
        /// Constructs an Intrinsic Delaunay Triangulation from a <see cref="TessellatedSolid"/>.
        /// </summary>
        public IntrinsicDelaunayTriangulation(TessellatedSolid solid)
        {
            _numVertices = solid.NumberOfVertices;
            _numFaces = solid.NumberOfFaces;

            BuildFromSolid(solid);
            MollifyIfDegenerate(solid);
            FlipToDelaunay();
            ComputeLocalFaceCoordinates();
        }

        private void BuildFromSolid(TessellatedSolid solid)
        {
            int numHalfedges = _numFaces * 3;
            _halfedges = new Halfedge[numHalfedges];
            _faceHalfedges = new int[_numFaces];

            // Map undirected edge (minVertex, maxVertex) to edge index
            var edgeMap = new Dictionary<long, int>();
            var edgeHalfedgeList = new List<int>();
            var edgeLengthList = new List<double>();

            for (int f = 0; f < _numFaces; f++)
            {
                var face = solid.Faces[f];
                int v0 = face.A.IndexInList;
                int v1 = face.B.IndexInList;
                int v2 = face.C.IndexInList;

                int h0 = 3 * f;
                int h1 = 3 * f + 1;
                int h2 = 3 * f + 2;

                _faceHalfedges[f] = h0;

                _halfedges[h0] = new Halfedge { Source = v0, Target = v1, Face = f, Next = h1, Prev = h2, Opposite = -1, Edge = -1 };
                _halfedges[h1] = new Halfedge { Source = v1, Target = v2, Face = f, Next = h2, Prev = h0, Opposite = -1, Edge = -1 };
                _halfedges[h2] = new Halfedge { Source = v2, Target = v0, Face = f, Next = h0, Prev = h1, Opposite = -1, Edge = -1 };

                ConnectEdge(h0, v0, v1, solid, edgeMap, edgeHalfedgeList, edgeLengthList);
                ConnectEdge(h1, v1, v2, solid, edgeMap, edgeHalfedgeList, edgeLengthList);
                ConnectEdge(h2, v2, v0, solid, edgeMap, edgeHalfedgeList, edgeLengthList);
            }

            _numEdges = edgeHalfedgeList.Count;
            _edgeHalfedges = edgeHalfedgeList.ToArray();
            _edgeLengths = edgeLengthList.ToArray();
        }

        private void ConnectEdge(int h, int vA, int vB, TessellatedSolid solid,
            Dictionary<long, int> edgeMap, List<int> edgeHalfedgeList, List<double> edgeLengthList)
        {
            long key = ((long)Math.Min(vA, vB) << 32) | (uint)Math.Max(vA, vB);
            if (edgeMap.TryGetValue(key, out int edgeIdx))
            {
                int hOpp = edgeHalfedgeList[edgeIdx];
                _halfedges[h].Opposite = hOpp;
                _halfedges[hOpp].Opposite = h;
                _halfedges[h].Edge = edgeIdx;
            }
            else
            {
                edgeIdx = edgeHalfedgeList.Count;
                edgeMap[key] = edgeIdx;
                edgeHalfedgeList.Add(h);
                double length = (solid.Vertices[vB].Coordinates - solid.Vertices[vA].Coordinates).Length();
                edgeLengthList.Add(length);
                _halfedges[h].Edge = edgeIdx;
            }
        }

        private void MollifyIfDegenerate(TessellatedSolid solid)
        {
            // Check if any face is degenerate
            bool hasDegenerate = false;
            double minLength = double.MaxValue;

            for (int e = 0; e < _numEdges; e++)
            {
                if (_edgeLengths[e] > 0 && _edgeLengths[e] < minLength)
                    minLength = _edgeLengths[e];
            }

            for (int f = 0; f < _numFaces; f++)
            {
                int h0 = _faceHalfedges[f];
                int h1 = _halfedges[h0].Next;
                int h2 = _halfedges[h1].Next;

                var p0 = solid.Vertices[_halfedges[h0].Source].Coordinates;
                var p1 = solid.Vertices[_halfedges[h1].Source].Coordinates;
                var p2 = solid.Vertices[_halfedges[h2].Source].Coordinates;

                var cross = (p1 - p0).Cross(p2 - p0);
                if (cross.LengthSquared().IsNegligible())
                {
                    hasDegenerate = true;
                    break;
                }
            }

            if (hasDegenerate && minLength < double.MaxValue)
            {
                double delta = minLength * 1e-4;
                double epsilon = 0.0;

                for (int h = 0; h < _halfedges.Length; h++)
                {
                    int h2 = _halfedges[h].Next;
                    int h3 = _halfedges[h2].Next;

                    double a = _edgeLengths[_halfedges[h].Edge];
                    double b = _edgeLengths[_halfedges[h2].Edge];
                    double c = _edgeLengths[_halfedges[h3].Edge];

                    double ineq = b + c - a;
                    epsilon = Math.Max(epsilon, Math.Max(0.0, delta - ineq));
                }

                for (int e = 0; e < _numEdges; e++)
                {
                    _edgeLengths[e] += epsilon;
                }
            }
        }

        private double GetCotanWeight(int edgeIdx)
        {
            int h = _edgeHalfedges[edgeIdx];
            int hOpp = _halfedges[h].Opposite;
            if (hOpp < 0) return double.PositiveInfinity; // Boundary edge is considered Delaunay

            int h2 = _halfedges[h].Next;
            int h3 = _halfedges[h2].Next;

            double a = _edgeLengths[edgeIdx];
            double b = _edgeLengths[_halfedges[h2].Edge];
            double c = _edgeLengths[_halfedges[h3].Edge];

            double s = (a + b + c) * 0.5;
            double denom1 = s * (s - a);
            double tan2Alpha = denom1 > 1e-30 ? Math.Sqrt(Math.Abs(((s - b) * (s - c)) / denom1)) : 0.0;
            double cotanWeight = Math.Abs(tan2Alpha) > 1e-30 ? (1.0 - tan2Alpha * tan2Alpha) / (2.0 * tan2Alpha) : 0.0;

            int o2 = _halfedges[hOpp].Next;
            int o3 = _halfedges[o2].Next;

            b = _edgeLengths[_halfedges[o2].Edge];
            c = _edgeLengths[_halfedges[o3].Edge];

            s = (a + b + c) * 0.5;
            denom1 = s * (s - a);
            double tan2Beta = denom1 > 1e-30 ? Math.Sqrt(Math.Abs(((s - b) * (s - c)) / denom1)) : 0.0;
            cotanWeight += Math.Abs(tan2Beta) > 1e-30 ? (1.0 - tan2Beta * tan2Beta) / (2.0 * tan2Beta) : 0.0;

            return cotanWeight;
        }

        private bool IsEdgeLocallyDelaunay(int edgeIdx)
        {
            return GetCotanWeight(edgeIdx) >= -1e-12;
        }

        private double ComputeFlippedEdgeLength(int edgeIdx)
        {
            int h = _edgeHalfedges[edgeIdx];
            int hOpp = _halfedges[h].Opposite;

            int h2 = _halfedges[h].Next;
            int h3 = _halfedges[h2].Next;
            int o2 = _halfedges[hOpp].Next;
            int o3 = _halfedges[o2].Next;

            double a = _edgeLengths[edgeIdx];
            double b1 = _edgeLengths[_halfedges[h2].Edge];
            double c1 = _edgeLengths[_halfedges[h3].Edge];
            double b2 = _edgeLengths[_halfedges[o2].Edge];
            double c2 = _edgeLengths[_halfedges[o3].Edge];

            // Angle alpha1 at Target(h) between side a and side b1: opposite to c1
            double cosAlpha1 = (a * a + b1 * b1 - c1 * c1) / Math.Max(1e-30, 2.0 * a * b1);
            cosAlpha1 = Math.Clamp(cosAlpha1, -1.0, 1.0);
            double sinAlpha1 = Math.Sqrt(Math.Max(0.0, 1.0 - cosAlpha1 * cosAlpha1));

            // Angle alpha2 at Target(h) = Source(hOpp) between side a and side c2: opposite to b2
            double cosAlpha2 = (a * a + c2 * c2 - b2 * b2) / Math.Max(1e-30, 2.0 * a * c2);
            cosAlpha2 = Math.Clamp(cosAlpha2, -1.0, 1.0);
            double sinAlpha2 = Math.Sqrt(Math.Max(0.0, 1.0 - cosAlpha2 * cosAlpha2));

            // Total angle theta = alpha1 + alpha2
            double cosTheta = cosAlpha1 * cosAlpha2 - sinAlpha1 * sinAlpha2;
            double newLength2 = Math.Max(0.0, b1 * b1 + c2 * c2 - 2.0 * b1 * c2 * cosTheta);

            return Math.Sqrt(newLength2);
        }

        private void FlipToDelaunay()
        {
            var stack = new Stack<int>(_numEdges);
            var inStack = new bool[_numEdges];

            for (int e = 0; e < _numEdges; e++)
            {
                stack.Push(e);
                inStack[e] = true;
            }

            int maxFlips = _numEdges * 10;
            int flipCount = 0;

            while (stack.Count > 0 && flipCount < maxFlips)
            {
                int e = stack.Pop();
                inStack[e] = false;

                int h = _edgeHalfedges[e];
                if (_halfedges[h].Opposite < 0) continue; // Boundary edge

                if (!IsEdgeLocallyDelaunay(e))
                {
                    double newLength = ComputeFlippedEdgeLength(e);
                    _edgeLengths[e] = newLength;

                    FlipEdgeTopology(e);
                    flipCount++;

                    // Surrounding 4 edges to re-check
                    int hAfter = _edgeHalfedges[e];
                    int hOppAfter = _halfedges[hAfter].Opposite;

                    int[] neighborEdges =
                    [
                        _halfedges[_halfedges[hAfter].Next].Edge,
                        _halfedges[_halfedges[hAfter].Prev].Edge,
                        _halfedges[_halfedges[hOppAfter].Next].Edge,
                        _halfedges[_halfedges[hOppAfter].Prev].Edge
                    ];

                    foreach (var ne in neighborEdges)
                    {
                        if (ne >= 0 && !inStack[ne])
                        {
                            stack.Push(ne);
                            inStack[ne] = true;
                        }
                    }
                }
            }
        }

        private void FlipEdgeTopology(int edgeIdx)
        {
            int h0 = _edgeHalfedges[edgeIdx];
            int o0 = _halfedges[h0].Opposite;

            int h1 = _halfedges[h0].Next;
            int h2 = _halfedges[h1].Next;

            int o1 = _halfedges[o0].Next;
            int o2 = _halfedges[o1].Next;

            int v0 = _halfedges[h0].Source;
            int v1 = _halfedges[h0].Target;
            int vOpp0 = _halfedges[h1].Target;
            int vOpp1 = _halfedges[o1].Target;

            int f0 = _halfedges[h0].Face;
            int f1 = _halfedges[o0].Face;

            // Flip: h0 goes vOpp1 -> vOpp0, o0 goes vOpp0 -> vOpp1
            _halfedges[h0].Source = vOpp1;
            _halfedges[h0].Target = vOpp0;
            _halfedges[h0].Face = f0;
            _halfedges[h0].Next = h2;
            _halfedges[h0].Prev = o1;

            _halfedges[h2].Face = f0;
            _halfedges[h2].Next = o1;
            _halfedges[h2].Prev = h0;

            _halfedges[o1].Face = f0;
            _halfedges[o1].Next = h0;
            _halfedges[o1].Prev = h2;

            _halfedges[o0].Source = vOpp0;
            _halfedges[o0].Target = vOpp1;
            _halfedges[o0].Face = f1;
            _halfedges[o0].Next = o2;
            _halfedges[o0].Prev = h1;

            _halfedges[o2].Face = f1;
            _halfedges[o2].Next = h1;
            _halfedges[o2].Prev = o0;

            _halfedges[h1].Face = f1;
            _halfedges[h1].Next = o0;
            _halfedges[h1].Prev = o2;

            _faceHalfedges[f0] = h0;
            _faceHalfedges[f1] = o0;
        }

        private void ComputeLocalFaceCoordinates()
        {
            FaceVertexIndices = new int[_numFaces][];
            FaceCoordinates = new Vector3[_numFaces][];

            for (int f = 0; f < _numFaces; f++)
            {
                int h0 = _faceHalfedges[f];
                int h1 = _halfedges[h0].Next;
                int h2 = _halfedges[h1].Next;

                int v0 = _halfedges[h0].Source;
                int v1 = _halfedges[h0].Target;
                int v2 = _halfedges[h1].Target;

                FaceVertexIndices[f] = [v0, v1, v2];

                double e0 = _edgeLengths[_halfedges[h0].Edge]; // v0 -> v1
                double e1 = _edgeLengths[_halfedges[h1].Edge]; // v1 -> v2
                double e2 = _edgeLengths[_halfedges[h2].Edge]; // v2 -> v0

                var p0 = Vector3.Zero;
                var p1 = new Vector3(e0, 0, 0);

                // Angle at v0 between e0 and e2: opposite to e1
                double cosAngle0 = (e0 * e0 + e2 * e2 - e1 * e1) / Math.Max(1e-30, 2.0 * e0 * e2);
                cosAngle0 = Math.Clamp(cosAngle0, -1.0, 1.0);
                double sinAngle0 = Math.Sqrt(Math.Max(0.0, 1.0 - cosAngle0 * cosAngle0));

                var p2 = new Vector3(e2 * cosAngle0, e2 * sinAngle0, 0);

                FaceCoordinates[f] = [p0, p1, p2];
            }
        }
    }
}

