
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public partial class ConvexHull4D
    {
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
        public static bool Create(IList<Vertex4D> vertices, out ConvexHull4D convexHull, double tolerance = double.NaN)
        {
            var n = vertices.Count;
            var nSqd = n * n;
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
            if (numExtrema == 2)
            {   // this is not a degenerate case! It's possible that the shape is long and skinny
                // in order to move forward, we find the third point that is farthest from the line
                // between the two extreme points. If that third point is one of the extreme points
                // or is too close to the line, then we have a degenerate case and we return a single
                var thirdPoint = Find3rdStartingPoint(extremePoints, vertices, out var radialDistance);
                if (thirdPoint == extremePoints[0] || thirdPoint == extremePoints[1] || radialDistance < tolerance)
                {
                    convexHull = new ConvexHull4D { tolerance = tolerance };
                    convexHull.Vertices.Add(extremePoints[0]);
                    convexHull.Vertices.Add(extremePoints[1]);
                    return true;
                }
                extremePoints = new List<Vertex4D> { extremePoints[0], extremePoints[1], thirdPoint };
            }
            else if (numExtrema > 4)
                // if more than 4 extreme points, then we need to reduce to 4 by finding the max volume tetrahedron
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
                CreateNewFaceCone(face, newFaces, oldFaces, verticesToReassign, n, nSqd);
                foreach (var iv in verticesToReassign)
                    AddVertexToProperFace(newFaces, iv, tolerance);
                foreach (var f in oldFaces)
                    faceQueue.Remove(f);
                foreach (var newFace in newFaces)
                    faceQueue.Enqueue(newFace, newFace.peakDistance);
            }
            // now we have the convex hull faces are used to build the convex hull object
            convexHull = MakeConvexHullWithFaces(tolerance, faceQueue.UnorderedItems.Select(fq => fq.Element));
            return true;
        }

        /// <summary>
        /// When only two extrema are found, this method finds a third point that is farthest from the line between the two extrema.
        /// </summary>
        /// <param name="extremePoints"></param>
        /// <param name="vertices"></param>
        /// <param name="radialDistance"></param>
        /// <returns></returns>
        private static Vertex4D Find3rdStartingPoint(List<Vertex4D> extremePoints, IList<Vertex4D> vertices, out double radialDistance)
        {
            var axis = extremePoints[1].Coordinates - extremePoints[0].Coordinates;
            radialDistance = double.NegativeInfinity;
            Vertex4D thirdPoint = null;
            foreach (var v in vertices)
            {
                var distance = (v.Coordinates - extremePoints[0].Coordinates).Cross(axis).LengthSquared();
                if (distance > radialDistance)
                {
                    radialDistance = distance;
                    thirdPoint = v;
                }
            }
            return thirdPoint;
        }

        private static void CreateNewFaceCone(ConvexHullFace4D startingFace, List<ConvexHullFace4D> newFaces,
            List<ConvexHullFace4D> oldFaces, List<Vertex4D> verticesToReassign, int factor1, int factor2)
        {
            newFaces.Clear();
            oldFaces.Clear();
            verticesToReassign.Clear();
            var peakVertex = startingFace.peakVertex;
            var peakCoord = peakVertex.Coordinates;
            var stack = new Stack<(ConvexHullFace4D, Edge4D)>();
            stack.Push((startingFace, null));
            var newConeEdges = new Dictionary<int, Edge4D>();
            // that's initialization. now for the main loop. Using a Depth-First Search, we find all the faces that are
            // within (and beyond the horizon) from the v3. Imagine this peak Vertex4D as a point sitting in the middle
            // above the triangle. All the triangles that are visible from this point are the ones that are within the cone.
            // All these triangles need to be removed (stored in oldFaces) and replaced with new triangles (newFaces).
            // The new triangles are formed by connecting the edges on the horizon with the v3.
            while (stack.Count > 0)
            {
                var (current, connectingEdge) = stack.Pop();
                if (current.Visited) continue; // here is the only place where "Visited" is checked. It is only set for
                // triangles within the cone to avoid cycling or redundant search.
                if ((peakCoord - current.A.Coordinates).Dot(current.Normal) < 0)
                {   // the vector from this current face to the peakCoord is below the current normal. Therefore
                    // current is beyond the horizon and is not to be replaced.
                    // so we stop here but before we move down the stack we need to create a new face
                    // this border face is stored in the borderFaces list so that at the end we can clear the Visited flags
                    var underVertex = current.VertexOppositeEdge(connectingEdge).Coordinates;
                    var newFace = new ConvexHullFace4D(connectingEdge.A, connectingEdge.B, connectingEdge.C, peakVertex, underVertex);
                    if (connectingEdge.OwnedFace == current)
                        connectingEdge.OtherFace = newFace;
                    else connectingEdge.OwnedFace = newFace;
                    newFace.AddEdge(connectingEdge);
                    newFaces.Add(newFace);
                    // now the other edges of this new face may have already been created. If so, we need to connect them
                    MakeNewInConeEdge(factor1, factor2, connectingEdge.A, connectingEdge.B, peakVertex, newConeEdges, newFace);
                    MakeNewInConeEdge(factor1, factor2, connectingEdge.A, connectingEdge.C, peakVertex, newConeEdges, newFace);
                    MakeNewInConeEdge(factor1, factor2, connectingEdge.B, connectingEdge.C, peakVertex, newConeEdges, newFace);
                }
                else // the face is within the cone. it'll be deleted in the above method, so we better get its interior vertices
                {    // and the peak and add them to the verticesToReassign list for later reassignment to the new faces
                    current.Visited = true;
                    oldFaces.Add(current);
                    if (current.peakVertex != null && current.peakVertex != peakVertex)
                        verticesToReassign.Add(current.peakVertex);
                    verticesToReassign.AddRange(current.InteriorVertices);

                    // find the neigbors of the current face that are not the connecting edge (the edge we came from)
                    if (current.ABC != connectingEdge) stack.Push((current.ABC.AdjacentFace(current), current.ABC));
                    if (current.ABD != connectingEdge) stack.Push((current.ABD.AdjacentFace(current), current.ABD));
                    if (current.ACD != connectingEdge) stack.Push((current.ACD.AdjacentFace(current), current.ACD));
                    if (current.BCD != connectingEdge) stack.Push((current.BCD.AdjacentFace(current), current.BCD));
                }
            }
        }

        private static void MakeNewInConeEdge(int factor1, int factor2, Vertex4D v1, Vertex4D v2, Vertex4D v3,
            Dictionary<int, Edge4D> newConeEdges, ConvexHullFace4D newFace)
        {
            int id;
            if (v1.IndexInList > v2.IndexInList && v1.IndexInList > v3.IndexInList)
            {
                if (v2.IndexInList > v3.IndexInList) id = factor2 * v1.IndexInList + factor1 * v2.IndexInList + v3.IndexInList;
                else id = factor2 * v1.IndexInList + factor1 * v3.IndexInList + v2.IndexInList;
            }
            else if (v2.IndexInList > v1.IndexInList && v2.IndexInList > v3.IndexInList)
            {
                if (v1.IndexInList > v3.IndexInList) id = factor2 * v2.IndexInList + factor1 * v1.IndexInList + v3.IndexInList;
                else id = factor2 * v2.IndexInList + factor1 * v3.IndexInList + v1.IndexInList;
            }
            else // then v3 is the largest
            {
                if (v2.IndexInList > v1.IndexInList) id = factor2 * v3.IndexInList + factor1 * v2.IndexInList + v1.IndexInList;
                else id = factor2 * v3.IndexInList + factor1 * v1.IndexInList + v2.IndexInList;
            }
            if (newConeEdges.TryGetValue(id, out var existingConeEdge))
            {
                newFace.AddEdge(existingConeEdge);
                existingConeEdge.OtherFace = newFace;
            }
            else
            {
                var coneEdge = new Edge4D(v1, v2, v3, newFace, null);
                newFace.AddEdge(coneEdge);
                newConeEdges.Add(id, coneEdge);
            }
        }
        /// <summary>
        /// This method is called at the end of the algorithm to make the convex hull object.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <param name="connectVerticesToCvxHullFaces"></param>
        /// <param name="cvxFaces"></param>
        /// <returns></returns>
        private static ConvexHull4D MakeConvexHullWithFaces(double tolerance, IEnumerable<ConvexHullFace4D> cvxFaces)
        {
            var cvxHull = new ConvexHull4D { tolerance = tolerance };
            cvxHull.Faces.AddRange(cvxFaces);

            var cvxVertexHash = new HashSet<Vertex4D>();
            var cvxEdgeHash = new HashSet<Edge4D>();
            foreach (var f in cvxFaces)
            {

            }
            cvxHull.Vertices.AddRange(cvxVertexHash);
            cvxHull.Edges.AddRange(cvxEdgeHash);
            return cvxHull;
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
                var dot = (v.Coordinates - face.A.Coordinates).Dot(face.Normal);
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
        /// If there are 5 or 6 points from the AABB check, then we run through all possibilities of 4 points 
        /// to find the one tetrahedron with the largest volume. The one or two points that are not part of the
        /// tetrahedron are removed from extremePoints
        /// </summary>
        /// <param name="extremePoints"></param>
        private static void FindBestExtremaSubset(List<Vertex4D> extremePoints)
        {
            var maxVol = 0.0;
            var numExtrema = extremePoints.Count;
            int maxI1 = -1, maxI2 = -1, maxI3 = -1, maxI4 = -1;
            for (int i1 = 0; i1 < numExtrema - 3; i1++)
            {
                var basePoint = extremePoints[i1];
                for (int i2 = i1 + 1; i2 < numExtrema - 2; i2++)
                {
                    for (int i3 = i2 + 1; i3 < numExtrema - 1; i3++)
                    {
                        var baseTriangleArea = (extremePoints[i2].Coordinates - basePoint.Coordinates).Cross(extremePoints[i3].Coordinates - basePoint.Coordinates);
                        for (int i4 = i3 + 1; i4 < numExtrema; i4++)
                        {
                            var projectedHeight = basePoint.Coordinates - extremePoints[i4].Coordinates;
                            var volume = Math.Abs(projectedHeight.Dot(baseTriangleArea));
                            if (volume > maxVol)
                            {
                                maxVol = volume;
                                maxI1 = i1; maxI2 = i2; maxI3 = i3; maxI4 = i4;
                            }
                        }
                    }
                }
            }
            for (int i = numExtrema - 1; i >= 0; i--)
            {
                if (i == maxI1 || i == maxI2 || i == maxI3 || i == maxI4) continue;
                extremePoints.RemoveAt(i);
            }
        }
        /// <summary>
        /// This method makes the faces for the simplex. It is called when there are 3 or 4 extreme points.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private static List<ConvexHullFace4D> MakeSimplexFaces(List<Vertex4D> vertices)
        {
            if (vertices.Count <= 3) throw new ArgumentException("There must be at least 4 vertices to make a simplex");
            if (vertices.Count == 4)
            {   // if there are only 4 vertices, then we make two faces back-to-back
                var faceOwned = new ConvexHullFace4D(vertices[0], vertices[1], vertices[2], vertices[3], Vector4.Zero);
                // here we use the origin to orient one of the faces
                var faceOther = new ConvexHullFace4D(vertices[3], vertices[2], vertices[1], vertices[0], faceOwned.Normal + vertices[3].Coordinates);
                // to make this face the opposite orientation, we create an "under point" by adding the normal of the previous face to one of the points of the face
                var edge012 = new Edge4D(vertices[0], vertices[1], vertices[2], faceOwned, faceOther);
                edge012.OwnedFace = faceOwned;
                edge012.OtherFace = faceOther;
                var edge123 = new Edge4D(vertices[1], vertices[2], vertices[3], faceOwned, faceOther);
                edge123.OwnedFace = faceOwned;
                edge123.OtherFace = faceOther;
                var edge230 = new Edge4D(vertices[2], vertices[3], vertices[0], faceOwned, faceOther);
                edge230.OwnedFace = faceOwned;
                edge230.OtherFace = faceOther;
                var edge301 = new Edge4D(vertices[3], vertices[0], vertices[1], faceOwned, faceOther);
                edge301.OwnedFace = faceOwned;
                edge301.OtherFace = faceOther;
                return [faceOwned, faceOther];
            }
            else // there are 5 or 6 vertices, so we make a proper tesseract
            {
                var face4 = new ConvexHullFace4D(vertices[0], vertices[1], vertices[2], vertices[3], vertices[4].Coordinates);
                var face3 = new ConvexHullFace4D(vertices[4], vertices[0], vertices[1], vertices[2], vertices[3].Coordinates);
                var face2 = new ConvexHullFace4D(vertices[3], vertices[4], vertices[0], vertices[1], vertices[2].Coordinates);
                var face1 = new ConvexHullFace4D(vertices[2], vertices[3], vertices[4], vertices[0], vertices[1].Coordinates);
                var face0 = new ConvexHullFace4D(vertices[1], vertices[2], vertices[3], vertices[4], vertices[0].Coordinates);

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
            }
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
            var extremePoints = Enumerable.Repeat(points[0], 6).ToList();
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
                if (points[i].W < extremePoints[4].W ||
                    points[i].W == extremePoints[4].W && points[i].X < extremePoints[4].X)
                    extremePoints[4] = points[i];
                if (points[i].W > extremePoints[5].W ||
                    points[i].W == extremePoints[5].W && points[i].Y > extremePoints[5].Y)
                    extremePoints[5] = points[i];
            }
            numExtrema = 6;
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