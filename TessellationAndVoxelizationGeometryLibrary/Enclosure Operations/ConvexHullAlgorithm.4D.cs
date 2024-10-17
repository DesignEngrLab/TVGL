using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public partial class ConvexHull4D
    {
        const double jiggleToleranceFactor = 100;
        const int defaultNumAttempts = 10;


        /// <summary>
        /// Creates the convex hull for a set of Coordinates. By the way, this is not 
        /// faster than using Vertices and - in fact - this method will wrap each coordinate
        /// within a Vertex.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="convexHull"></param>
        /// <param name="vertexIndices"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool Create(IList<Vector4> points, out ConvexHull4D convexHull,
            out List<int> vertexIndices, double tolerance = double.NaN)
        {
            bool success = false;
            var n = points.Count;
            var vertices = new Vertex4D[n];
            for (int i = 0; i < n; i++)
                vertices[i] = new Vertex4D(points[i], i);

            success = Create(vertices, out convexHull, tolerance);
            if (success)
            {
                vertexIndices = vertices.Select(v => v.IndexInList).ToList();
                return true;
            }
            else
            {
                vertexIndices = null;
                return false;
            }
        }

        /// <summary>
        /// Creates the convex hull for a set of vertices. This method is used by the TessellatedSolid,
        /// but it can be used within any set of vertices.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="convexHull"></param>
        /// <param name="connectVerticesToCvxHullFaces"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool Create(IList<Vertex4D> vertices, out ConvexHull4D convexHull, double tolerance = double.NaN,
            int numAttempts = defaultNumAttempts)
        {
            for (int i = 1; i < vertices.Count; i++)
                if (vertices[i].IndexInList <= vertices[i - 1].IndexInList)
                    throw new Exception("The vertices must be in order of their index in the list");
            var n = vertices.Count;
            var nSqd = (long)(n * n);
            if (double.IsNaN(tolerance) || tolerance < Constants.BaseTolerance)
                tolerance = Constants.BaseTolerance;

            // if the vertices are all on a plane, then we can solve this as a 2D problem
            if (SolveAs3D(vertices, out convexHull, tolerance)) return true;

            /* The first step is to quickly identify the two to six vertices based on the
             * Akl-Toussaint heuristic, which is simply the points on the AABB */
            var extremePoints = GetExtremaOnAABB(n, vertices, out var numExtrema);
            if (numExtrema == 1)
            { // only one extreme point, so the convex hull is a single point
                convexHull = new ConvexHull4D { tolerance = tolerance };
                convexHull.Vertices.Add(extremePoints[0]);
                return true;
            }
            if (numExtrema < 4)
                throw new NotImplementedException();
            else if (numExtrema > 4)
                // if more than 5 extreme points, then we need to reduce to 5 by finding the max volume tesseract
                FindBestExtremaSubset(extremePoints);
            var simplexFaces = MakeSimplexFaces(extremePoints);

            // now add all the other vertices to the simplex faces. AddVertexToProperFace will add the Vertex4D to the face that it is "farthest" from
            var extremePointsHash = extremePoints.ToHashSet();
            foreach (var v in vertices)
            {
                if (extremePointsHash.Contains(v)) continue;
                AddVertexToProperFace(simplexFaces, v, tolerance);
            }
            // here comes the main loop. We start with the simplex faces and then keep adding faces until we're done
            var faceQueue = new UpdatablePriorityQueue<ConvexHullFace4D, double>(simplexFaces.Select(f => (f, f.peakDistance)),
                new NoEqualSort(false));
            var newFaces = new List<ConvexHullFace4D>();
            var oldFaces = new List<ConvexHullFace4D>();
            var verticesToReassign = new List<Vertex4D>();
            while (faceQueue.Count > 0)
            {
                var face = faceQueue.Dequeue();
                // solve the face that has the farthest Vertex4D from it.
                if (face.peakVertex == null)
                {   // given the the priority queue is sorted in descending order, if the peak Vertex4D is null then all the other faces are also null
                    // and we're done, so break the loop. Oh! but before you go, better re-add the face that you just dequeued.
                    faceQueue.Enqueue(face, face.peakDistance);
                    break;
                }
                // this function, CreateNewFaceCone, is the hardest part of the algorithm. But, from its arguments, you can see that it finds
                // faces to remove (oldFaces), faces to add (newFaces), and vertices to reassign (verticesToReassign) to the new faces.
                // These three lists are then processed in the 3 foreach loops below.
                if (!CreateNewFaceCone(face, newFaces, oldFaces, verticesToReassign, n, nSqd))
                    return JigglePointsAndTryAgain(vertices, out convexHull, tolerance, numAttempts - 1);
                foreach (var iv in verticesToReassign)
                    AddVertexToProperFace(newFaces, iv, tolerance);
                foreach (var f in oldFaces)
                    faceQueue.Remove(f);
                foreach (var newFace in newFaces)
                    faceQueue.Enqueue(newFace, newFace.peakDistance);
            }
            // now we have the convex hull faces are used to build the convex hull object
            convexHull = MakeConvexHullWithFaces(tolerance, faceQueue.UnorderedItems.Select(fq => fq.Element), n);
            return true;
        }
        private static bool JigglePointsAndTryAgain(IList<Vertex4D> vertices, out ConvexHull4D convexHull, double tolerance,
            int numAttempts)
        {
            Console.WriteLine("jiggling " + (defaultNumAttempts - numAttempts).ToString());
            convexHull = null;
            if (numAttempts < 0)
                return false;

            tolerance *= jiggleToleranceFactor;
            var random = new Random();
            var jiggledPoints = new Vertex4D[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                var p = vertices[i].Coordinates;
                jiggledPoints[i] = new Vertex4D(new Vector4(p.X + 2 * tolerance * (random.NextDouble() - 1), p.Y + 2 * tolerance * (random.NextDouble() - 1),
                    p.Z + 2 * tolerance * (random.NextDouble() - 1), p.W + 2 * tolerance * (random.NextDouble() - 1)), i);
            }
            if (!Create(jiggledPoints, out var jiggledConvexHull, tolerance, numAttempts))
                return false;

            convexHull = new ConvexHull4D
            { tolerance = tolerance };
            var vertsArray = new Vertex4D[jiggledConvexHull.Vertices.Count];
            for (int i = 0; i < jiggledConvexHull.Vertices.Count; i++)
                vertsArray[i] = vertices[jiggledConvexHull.Vertices[i].IndexInList];
            convexHull.Vertices.AddRange(vertsArray);

            var vertPairArray = new VertexPair[jiggledConvexHull.VertexPairs.Count];
            for (int i = 0; i < jiggledConvexHull.Vertices.Count; i++)
                vertPairArray[i] = new VertexPair
                {
                    Vertex1 = vertices[jiggledConvexHull.VertexPairs[i].Vertex1.IndexInList],
                    Vertex2 = vertices[jiggledConvexHull.VertexPairs[i].Vertex2.IndexInList]
                };
            convexHull.VertexPairs.AddRange(vertPairArray);
            var newConeFaces = new Dictionary<long, Edge4D>();
            var n = vertices.Count;
            var nSqd = (long)(n * n);

            //cvxHull.Tetrahedra.AddRange(tetras);
            foreach (var tetra in jiggledConvexHull.Tetrahedra)
            {
                var newTetra = new ConvexHullFace4D
                {
                    A = vertices[tetra.A.IndexInList],
                    B = vertices[tetra.B.IndexInList],
                    C = vertices[tetra.C.IndexInList],
                    D = vertices[tetra.D.IndexInList],
                    Normal = tetra.Normal,
                };
                convexHull.Tetrahedra.Add(newTetra);
                MakeNewInConeFace(n, nSqd, newTetra.A, newTetra.B, newTetra.C, newConeFaces, newTetra);
                MakeNewInConeFace(n, nSqd, newTetra.A, newTetra.B, newTetra.D, newConeFaces, newTetra);
                MakeNewInConeFace(n, nSqd, newTetra.A, newTetra.C, newTetra.D, newConeFaces, newTetra);
                MakeNewInConeFace(n, nSqd, newTetra.B, newTetra.C, newTetra.D, newConeFaces, newTetra);
            }
            convexHull.Faces.AddRange(newConeFaces.Values);
            return true;
        }

        private static bool CreateNewFaceCone(ConvexHullFace4D startingTetra, List<ConvexHullFace4D> newTetras,
            List<ConvexHullFace4D> oldTetras, List<Vertex4D> verticesToReassign, int base1Factor, long base2Factor)
        {
            newTetras.Clear();
            oldTetras.Clear();
            verticesToReassign.Clear();
            var peakVertex = startingTetra.peakVertex;
            var peakCoord = peakVertex.Coordinates;
            var stack = new Queue<(ConvexHullFace4D, Edge4D)>();
            stack.Enqueue((startingTetra, null));
            var newConeFaces = new Dictionary<long, Edge4D>();
            // that's initialization. now for the main loop. Using a Depth-First Search, we find all the tetras that are
            // within (and beyond the horizon) from the peak vertex. Imagine this peak Vertex4D as a point sitting in the middle
            // above the triangle. All the triangles that are visible from this point are the ones that are within the cone.
            // All these triangles need to be removed (stored in oldTetras) and replaced with new ones (newTetras).
            // The new triangles are formed by connecting the edges on the horizon with the peak.
            while (stack.Count > 0)
            {
                var (current, connectingFace) = stack.Dequeue();
                if (current == null || current.Visited) continue; // here is the only place where "Visited" is checked. It is only set for
                // triangles within the cone to avoid cycling or redundant search.
                var dot = (peakCoord - current.A.Coordinates).Dot(current.GetNormal(true));
                if (dot > 0) // && CheckAndRetrieveConeEdge(base1Factor, base2Factor, peakVertex, newConeFaces, current))
                {    // and the peak and add them to the verticesToReassign list for later reassignment to the new faces
                    current.Visited = true;
                    oldTetras.Add(current);
                    if (current.peakVertex != null && current.peakVertex != peakVertex)
                        verticesToReassign.Add(current.peakVertex);
                    verticesToReassign.AddRange(current.InteriorVertices);

                    // find the neigbors of the current face that are not the connecting edge (the edge we came from)
                    if (current.ABC != connectingFace) stack.Enqueue((current.ABC.AdjacentTetra(current), current.ABC));
                    if (current.ABD != connectingFace) stack.Enqueue((current.ABD.AdjacentTetra(current), current.ABD));
                    if (current.ACD != connectingFace) stack.Enqueue((current.ACD.AdjacentTetra(current), current.ACD));
                    if (current.BCD != connectingFace) stack.Enqueue((current.BCD.AdjacentTetra(current), current.BCD));
                }
                else
                {
                    var underVertex = current.VertexOppositeFace(connectingFace).Coordinates;
                    var normal = DetermineNormal(connectingFace.A.Coordinates, connectingFace.B.Coordinates,
                        connectingFace.C.Coordinates, peakVertex.Coordinates, underVertex); //, current.Normal);
                    var newTetra = new ConvexHullFace4D
                    {
                        A = connectingFace.A,
                        B = connectingFace.B,
                        C = connectingFace.C,
                        D = peakVertex,
                        Normal = normal
                    };
                    if (connectingFace.OwnedTetra == current)
                        connectingFace.OtherTetra = newTetra;
                    else connectingFace.OwnedTetra = newTetra;
                    newTetra.AddEdge(connectingFace);
                    newTetras.Add(newTetra);
                    // now the other faces of this new tetra may have already been created. If so, we need to connect them
                    if (!MakeNewInConeFace(base1Factor, base2Factor, connectingFace.A, connectingFace.B, peakVertex, newConeFaces, newTetra))
                        return false;
                    if (!MakeNewInConeFace(base1Factor, base2Factor, connectingFace.A, connectingFace.C, peakVertex, newConeFaces, newTetra))
                        return false;
                    if (!MakeNewInConeFace(base1Factor, base2Factor, connectingFace.B, connectingFace.C, peakVertex, newConeFaces, newTetra))
                        return false;
                }
            }
            return true;
        }


        internal static Vector4 DetermineNormal(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, Vector4 knownUnderPoint)
        {
            // following the approach at https://www.mathwizurd.com/linalg/2018/11/15/find-a-normal-vector-to-a-hyperplane
            // f is the vector that should have a positive dot product with the normal. It's used to flip the normal if necessary.
            var f = p0 - knownUnderPoint;
            Vector4 normal;
            var successfulNormal = GetNormalComponents(p0, p1, p2, p3, out var nx, out var ny, out var nz, out var nw);
            if (!successfulNormal)
                return Vector4.Null;
            else
                normal = new Vector4(nx, ny, nz, nw).Normalize();

            var flipForUnderVertex = f.Dot(normal) < 0;
            var a = new Vector3(p0.X, p0.Y, p0.Z);
            var b = new Vector3(p1.X, p1.Y, p1.Z);
            var c = new Vector3(p2.X, p2.Y, p2.Z);
            var d = new Vector3(p3.X, p3.Y, p3.Z);
            var vol3D = Math.Abs((a - d).Dot((b - d).Cross(c - d))); // / 6; for speed sake just avoid div by 6

            if (vol3D.IsNegligible(Constants.BaseTolerance))
                return Vector4.Null;

            if (flipForUnderVertex) normal = -normal;
            return normal;
        }

        private static bool GetNormalComponents(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, out double nx, out double ny, out double nz, out double nw)
        {
            var d1 = p1 - p0;
            var d2 = p2 - p0;
            var d3 = p3 - p0;

            // these look like terms from the determinant...but the why is where I'm confused.
            // is this GrahamSchmidt or just solving for the null vector?
            nx = d1.W * (d2.Z * d3.Y - d2.Y * d3.Z)
                + d1.Z * (d2.Y * d3.W - d2.W * d3.Y)
                + d1.Y * (d2.W * d3.Z - d2.Z * d3.W);
            ny = d1.W * (d2.X * d3.Z - d2.Z * d3.X)
                    + d1.Z * (d2.W * d3.X - d2.X * d3.W)
                    + d1.X * (d2.Z * d3.W - d2.W * d3.Z);
            nz = d1.W * (d2.Y * d3.X - d2.X * d3.Y)
                    + d1.Y * (d2.X * d3.W - d2.W * d3.X)
                    + d1.X * (d2.W * d3.Y - d2.Y * d3.W);
            nw = d1.Z * (d2.X * d3.Y - d2.Y * d3.X)
                    + d1.Y * (d2.Z * d3.X - d2.X * d3.Z)
                    + d1.X * (d2.Y * d3.Z - d2.Z * d3.Y);
            return Math.Min(d1.LengthSquared(), Math.Min(d2.LengthSquared(), d3.LengthSquared())) <= 10000 * Math.Max(Math.Max(Math.Abs(nx), Math.Abs(ny)),
                    Math.Max(Math.Abs(nz), Math.Abs(nw)));
        }

        private static bool RetrieveNewEdge4D(int base1Factor, long base2Factor, Vertex4D v1, Vertex4D v2, Vertex4D v3,
            Dictionary<long, Edge4D> newConeEdges, out Edge4D existingConeEdge, out long id)
        {
            if (v1.IndexInList > v2.IndexInList && v1.IndexInList > v3.IndexInList)
            {
                id = base2Factor * v1.IndexInList;
                if (v2.IndexInList > v3.IndexInList) id += base1Factor * v2.IndexInList + v3.IndexInList;
                else id += base1Factor * v3.IndexInList + v2.IndexInList;
            }
            else if (v2.IndexInList > v1.IndexInList && v2.IndexInList > v3.IndexInList)
            {
                id = base2Factor * v2.IndexInList;
                if (v1.IndexInList > v3.IndexInList) id += base1Factor * v1.IndexInList + v3.IndexInList;
                else id += base1Factor * v3.IndexInList + v1.IndexInList;
            }
            else // then v3 is the largest
            {
                id = base2Factor * v3.IndexInList;
                if (v2.IndexInList > v1.IndexInList) id += base1Factor * v2.IndexInList + v1.IndexInList;
                else id += base1Factor * v1.IndexInList + v2.IndexInList;
            }
            existingConeEdge = null;
            return newConeEdges.TryGetValue(id, out existingConeEdge);
        }

        private static bool MakeNewInConeFace(int base1Factor, long base2Factor, Vertex4D v1, Vertex4D v2, Vertex4D v3,
            Dictionary<long, Edge4D> newConeEdges, ConvexHullFace4D newTetra)
        {
            if (RetrieveNewEdge4D(base1Factor, base2Factor, v1, v2, v3, newConeEdges, out var existingConeEdge, out long id))
            {
                if (existingConeEdge.OtherTetra != null) return false;
                newTetra.AddEdge(existingConeEdge);
                existingConeEdge.OtherTetra = newTetra;
            }
            else
            {
                var coneEdge = new Edge4D(v1, v2, v3, newTetra, null);
                newTetra.AddEdge(coneEdge);
                newConeEdges.Add(id, coneEdge);
            }
            return true;
        }

        /// <summary>
        /// This method is called at the end of the algorithm to make the convex hull object.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <param name="connectVerticesToCvxHullFaces"></param>
        /// <param name="tetras"></param>
        /// <returns></returns>
        private static ConvexHull4D MakeConvexHullWithFaces(double tolerance, IEnumerable<ConvexHullFace4D> tetras, int baseFactor)
        {
            var cvxHull = new ConvexHull4D { tolerance = tolerance };
            cvxHull.Tetrahedra.AddRange(tetras);

            var cvxVertexHash = new HashSet<Vertex4D>();
            var cvxFaceHash = new HashSet<Edge4D>();
            var vertexPairs = new Dictionary<long, VertexPair>();
            foreach (var tetra in tetras)
            {
                cvxFaceHash.Add(tetra.ABC);
                cvxFaceHash.Add(tetra.ABD);
                cvxFaceHash.Add(tetra.ACD);
                cvxFaceHash.Add(tetra.BCD);
                cvxVertexHash.Add(tetra.A);
                cvxVertexHash.Add(tetra.B);
                cvxVertexHash.Add(tetra.C);
                cvxVertexHash.Add(tetra.D);
                AddVertexPair(vertexPairs, tetra.A, tetra.B, baseFactor, tetra);
                AddVertexPair(vertexPairs, tetra.A, tetra.C, baseFactor, tetra);
                AddVertexPair(vertexPairs, tetra.A, tetra.D, baseFactor, tetra);
                AddVertexPair(vertexPairs, tetra.B, tetra.C, baseFactor, tetra);
                AddVertexPair(vertexPairs, tetra.B, tetra.D, baseFactor, tetra);
                AddVertexPair(vertexPairs, tetra.C, tetra.D, baseFactor, tetra);
            }
            cvxHull.Vertices.AddRange(cvxVertexHash);
            cvxHull.Faces.AddRange(cvxFaceHash);
            cvxHull.VertexPairs.AddRange(vertexPairs.Values);
            return cvxHull;
        }

        private static void AddVertexPair(Dictionary<long, VertexPair> vertexPairs, Vertex4D a, Vertex4D b, int baseFactor, ConvexHullFace4D tetra)
        {
            var id = a.IndexInList > b.IndexInList ? baseFactor * a.IndexInList + b.IndexInList : baseFactor * b.IndexInList + a.IndexInList;
            if (vertexPairs.TryGetValue(id, out var existingPair))
                existingPair.Tetrahedra.Add(tetra);
            else
            {
                VertexPair newPair = null;
                if (a.IndexInList > b.IndexInList)
                    newPair = new VertexPair { Vertex1 = b, Vertex2 = a };
                else
                    newPair = new VertexPair { Vertex1 = a, Vertex2 = b };
                newPair.Tetrahedra.Add(tetra);
                vertexPairs.Add(id, newPair);
            }
        }

        /// <summary>
        /// For each Vertex4D, add it to the interior points of the convex hull face that it is farthest from.
        /// Actually, the v3 and peakDistance properties of ConvexHullFace4D store the Vertex4D that is farthest
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="v"></param>
        /// <param name="tolerance"></param>
        private static void AddVertexToProperFace(IList<ConvexHullFace4D> faces, Vertex4D v, double tolerance)
        {
            var maxDot = double.NegativeInfinity;
            ConvexHullFace4D maxFace = null;
            foreach (var face in faces)
            {
                var dot = (v.Coordinates - face.A.Coordinates).Dot(face.GetNormal(false));
                if (dot > maxDot)
                {
                    maxDot = dot;
                    maxFace = face;
                    //if (maxDot >= tolerance) break;
                }
            }
            if (maxDot >= -tolerance)
            {
                if ((maxFace.peakVertex == null || maxDot > maxFace.peakDistance) && maxDot >= tolerance)
                {
                    if (maxFace.peakVertex != null)
                        maxFace.InteriorVertices.Add(maxFace.peakVertex);
                    maxFace.peakVertex = v;
                    maxFace.peakDistance = maxDot;
                }
                else maxFace.InteriorVertices.Add(v);
            }
        }

        /// <summary>
        /// Before the 3D convex hull is run, we find if the points are all on a plane.
        /// If so, then we can solve this as a 2D problem.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="convexHull"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static bool SolveAs3D(IList<Vertex4D> vertices, out ConvexHull4D convexHull, double tolerance = double.NaN)
        {
            convexHull = null;
            // todo: add code that mimics Plane.DefineNormalAndDistanceFromVertices(...);
            return false;
        }

        /// <summary>
        /// If there are 6,7 or8 points from the AABB check, then we run through all possibilities of 5 points 
        /// to find the one tesseract with the largest volume. The one or two points that are not part of the
        /// tetrahedron are removed from extremePoints
        /// </summary>
        /// <param name="extremePoints"></param>
        private static void FindBestExtremaSubsetOLD(List<Vertex4D> extremePoints)
        {
            var n = extremePoints.Count;
            var sums = Enumerable.Repeat(0.0, n).ToList();
            for (int i = 0; i < n - 1; i++)
                for (int j = i + 1; j < n; j++)
                {
                    var distSqd = (extremePoints[i].Coordinates - extremePoints[j].Coordinates).LengthSquared();
                    sums[i] += distSqd;
                    sums[j] += distSqd;
                }
            while (n > 4)
            {
                var min = double.PositiveInfinity;
                var minIndex = -1;
                for (int i = 0; i < n; i++)
                {
                    if (min > sums[i])
                    {
                        min = sums[i];
                        minIndex = i;
                    }
                }
                sums.RemoveAt(minIndex);
                extremePoints.RemoveAt(minIndex);
                n--;
            }
        }
        private static void FindBestExtremaSubset(List<Vertex4D> extremePoints)
        {
            var n = extremePoints.Count;
            var maxSum = 0.0;
            var iMax = -1;
            var jMax = -1;
            var kMax = -1;
            var mMax = -1;
            for (int i = 0; i < n - 3; i++)
                for (int j = i + 1; j < n - 2; j++)
                    for (int k = j + 1; k < n - 1; k++)
                        for (int m = k + 1; m < n; m++)
                        {
                            GetNormalComponents(extremePoints[i].Coordinates, extremePoints[j].Coordinates, extremePoints[k].Coordinates,
                                extremePoints[m].Coordinates, out var nx, out var ny, out var nz, out var nw);
                            var sum = Math.Abs(nx) + Math.Abs(ny) + Math.Abs(nz) + Math.Abs(nw);
                            if (maxSum < sum)
                            {
                                maxSum = sum; iMax = i; jMax = j; kMax = k; mMax = m;
                            }
                        }
            for (int i = n - 1; i >= 0; i--)
                if (i != iMax && i != jMax && i != kMax && i != mMax)
                    extremePoints.RemoveAt(i);
        }



        /// <summary>
        /// This method makes the faces for the simplex. It is called when there are 3 or 4 extreme points.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private static List<ConvexHullFace4D> MakeSimplexFaces(List<Vertex4D> vertices)
        {
            if (vertices.Count <= 3) throw new ArgumentException("There must be at least 4 vertices to make a simplex");
            //if (vertices.Count == 4)
            {
                //throw new NotImplementedException();
                var normal = DetermineNormal(vertices[0].Coordinates, vertices[1].Coordinates, vertices[2].Coordinates, vertices[3].Coordinates, Vector4.Null);

                // make two faces back-to-back
                var faceOwned = new ConvexHullFace4D
                {
                    A = vertices[0],
                    B = vertices[1],
                    C = vertices[2],
                    D = vertices[3],
                    Normal = normal
                };
                // here we use the origin to orient one of the faces
                var faceOther = new ConvexHullFace4D
                {
                    A = vertices[3],
                    B = vertices[2],
                    C = vertices[1],
                    D = vertices[0],
                    Normal = -normal
                };
                //(vertices[3], vertices[2], vertices[1], vertices[0], faceOwned.Normal + vertices[3].Coordinates);
                // to make this face the opposite orientation, we create an "under point" by adding the normal of the previous face to one of the points of the face
                var edge = new Edge4D(vertices[0], vertices[1], vertices[2], faceOwned, faceOther);
                faceOwned.AddEdge(edge);
                faceOther.AddEdge(edge);
                edge = new Edge4D(vertices[1], vertices[2], vertices[3], faceOwned, faceOther);
                faceOwned.AddEdge(edge);
                faceOther.AddEdge(edge);
                edge = new Edge4D(vertices[2], vertices[3], vertices[0], faceOwned, faceOther);
                faceOwned.AddEdge(edge);
                faceOther.AddEdge(edge);
                edge = new Edge4D(vertices[3], vertices[0], vertices[1], faceOwned, faceOther);
                faceOwned.AddEdge(edge);
                faceOther.AddEdge(edge);
                return [faceOwned, faceOther];

            }
            /*
            else // there are 5 or 6 vertices, so we make a proper tesseract
            {
                var face4 = new ConvexHullFace4D(vertices[0], vertices[1], vertices[2], vertices[3], vertices[4].Coordinates, Vector4.Null);
                var face3 = new ConvexHullFace4D(vertices[4], vertices[0], vertices[1], vertices[2], vertices[3].Coordinates, Vector4.Null);
                var face2 = new ConvexHullFace4D(vertices[3], vertices[4], vertices[0], vertices[1], vertices[2].Coordinates, Vector4.Null);
                var face1 = new ConvexHullFace4D(vertices[2], vertices[3], vertices[4], vertices[0], vertices[1].Coordinates, Vector4.Null);
                var face0 = new ConvexHullFace4D(vertices[1], vertices[2], vertices[3], vertices[4], vertices[0].Coordinates, Vector4.Null);

                var edge = new Edge4D(vertices[2], vertices[3], vertices[4], face0, face1);
                face0.AddEdge(edge);
                face1.AddEdge(edge);
                edge = new Edge4D(vertices[3], vertices[4], vertices[1], face0, face2);
                face0.AddEdge(edge);
                face2.AddEdge(edge);
                edge = new Edge4D(vertices[4], vertices[1], vertices[2], face0, face3);
                face0.AddEdge(edge);
                face3.AddEdge(edge);
                edge = new Edge4D(vertices[1], vertices[2], vertices[3], face0, face4);
                face0.AddEdge(edge);
                face4.AddEdge(edge);
                edge = new Edge4D(vertices[3], vertices[4], vertices[0], face1, face2);
                face1.AddEdge(edge);
                face2.AddEdge(edge);
                edge = new Edge4D(vertices[4], vertices[0], vertices[2], face1, face3);
                face1.AddEdge(edge);
                face3.AddEdge(edge);
                edge = new Edge4D(vertices[0], vertices[2], vertices[3], face1, face4);
                face1.AddEdge(edge);
                face4.AddEdge(edge);
                edge = new Edge4D(vertices[4], vertices[0], vertices[1], face2, face3);
                face2.AddEdge(edge);
                face3.AddEdge(edge);
                edge = new Edge4D(vertices[0], vertices[1], vertices[3], face2, face4);
                face2.AddEdge(edge);
                face4.AddEdge(edge);
                edge = new Edge4D(vertices[0], vertices[1], vertices[2], face3, face4);
                face3.AddEdge(edge);
                face4.AddEdge(edge);

                return [face4, face3, face2, face1, face0];
            }*/
        }

        /// <summary>
        /// Finds the extreme points on the bounding box
        /// </summary>
        /// <param name="n"></param>
        /// <param name="points"></param>
        /// <param name="numExtrema"></param>
        /// <returns></returns>
        private static List<Vertex4D> GetExtremaOnAABB(int n, IList<Vertex4D> points, out int numExtrema)
        {
            numExtrema = 8;
            var extremePoints = Enumerable.Repeat(points[0], numExtrema).ToList();
            for (int i = 1; i < n; i += 2)
            {
                if (points[i].X < extremePoints[0].X ||
                    points[i].X == extremePoints[0].X && points[i].Y < extremePoints[0].Y)
                    extremePoints[0] = points[i];
                if (points[i].X > extremePoints[1].X ||
                    points[i].X == extremePoints[1].X && points[i].Z > extremePoints[1].Z)
                    extremePoints[1] = points[i];
                if (points[i].Y < extremePoints[2].Y ||
                    points[i].Y == extremePoints[2].Y && points[i].Z < extremePoints[2].Z)
                    extremePoints[2] = points[i];
                if (points[i].Y > extremePoints[3].Y ||
                    points[i].Y == extremePoints[3].Y && points[i].W > extremePoints[3].W)
                    extremePoints[3] = points[i];
                if (points[i].Z < extremePoints[4].Z ||
                    points[i].Z == extremePoints[4].Z && points[i].W < extremePoints[4].W)
                    extremePoints[4] = points[i];
                if (points[i].Z > extremePoints[5].Z ||
                    points[i].Z == extremePoints[5].Z && points[i].X > extremePoints[5].X)
                    extremePoints[5] = points[i];
                if (points[i].W < extremePoints[6].W ||
                    points[i].W == extremePoints[6].W && points[i].X < extremePoints[6].X)
                    extremePoints[6] = points[i];
                if (points[i].W > extremePoints[7].W ||
                    points[i].W == extremePoints[7].W && points[i].Y > extremePoints[7].Y)
                    extremePoints[7] = points[i];
            }
            for (int i = numExtrema - 1; i > 0; i--)
            {
                var extremeI = extremePoints[i];
                for (int j = 0; j < i; j++)
                {
                    var extremeJ = extremePoints[j];
                    if (extremeI == extremeJ)
                    {
                        numExtrema--;
                        extremePoints.RemoveAt(i);
                        break;
                    }
                }
            }
            return extremePoints;
        }
    }
}