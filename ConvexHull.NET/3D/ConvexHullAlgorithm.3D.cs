namespace ConvexHull.NET
{
    public partial class ConvexHull3D
    {
        /// <summary>
        /// Creates the convex hull for a set of Coordinates. By the way, this is not 
        /// faster than using Vertices and - in fact - this method will wrap each coordinate
        /// within a vertex.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="convexHull"></param>
        /// <param name="vertexIndices"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool Create(IList<Vector3> points, out ConvexHull3D convexHull,
            out List<int> vertexIndices, double tolerance = double.NaN)
        {
            bool success = false;
            var n = points.Count;
            var vertices = new ConvexHullVertex[n];
            for (int i = 0; i < n; i++)
                vertices[i] = new ConvexHullVertex { Coordinates = points[i], IndexInList = i };

            success = Create<ConvexHullVertex, ConvexHullEdge, ConvexHullFace>(vertices, out convexHull, true, tolerance);
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
        public static bool Create<TVertex, TEdge, TFace>(IList<TVertex> vertices, out ConvexHull3D convexHull,
            bool connectVerticesToCvxHullFaces, double tolerance = double.NaN)
            where TVertex : IConvexVertex3D, new()
            where TEdge : IConvexEdge3D, new()
            where TFace : IConvexFace3D, new()
        {
            var n = vertices.Count;
            if (double.IsNaN(tolerance) || tolerance < Constants.BaseTolerance)
                tolerance = Constants.BaseTolerance;

            // if the vertices are all on a plane, then we can solve this as a 2D problem
            if (SolveAs2D(vertices, out convexHull, tolerance)) return true;

            /* The first step is to quickly identify the two to six vertices based on the
             * Akl-Toussaint heuristic, which is simply the points on the AABB */
            var extremePoints = GetExtremaOnAABB(n, vertices, out var numExtrema);
            if (numExtrema == 1)
            { // only one extreme point, so the convex hull is a single point
                convexHull = new ConvexHull3D();
                convexHull.Vertices.Add(extremePoints[0]);
                return true;
            }
            if (numExtrema == 2)
            {   // this is not a degenerate case! It's possible that the shape is long and skinny
                // in order to move forward, we find the third point that is farthest from the line
                // between the two extreme points. If that third point is one of the extreme points
                // or is too close to the line, then we have a degenerate case and we return a single
                var thirdPoint = Find3rdStartingPoint(extremePoints, vertices, out var radialDistance);
                if (thirdPoint.Equals(extremePoints[0]) || thirdPoint.Equals(extremePoints[1]) || radialDistance < tolerance)
                {
                    convexHull = new ConvexHull3D();
                    convexHull.Vertices.Add(extremePoints[0]);
                    convexHull.Vertices.Add(extremePoints[1]);
                    return true;
                }
                extremePoints = new List<TVertex> { extremePoints[0], extremePoints[1], thirdPoint };
            }
            else if (numExtrema > 4)
                // if more than 4 extreme points, then we need to reduce to 4 by finding the max volume tetrahedron
                FindBestExtremaSubset(extremePoints);
            var simplexFaces = MakeSimplexFaces<TVertex, TEdge,TFace>(extremePoints);

            // now add all the other vertices to the simplex faces. AddVertexToProperFace will add the vertex to the face that it is "farthest" from
            var extremePointsHash = extremePoints.ToHashSet();
            foreach (var v in vertices)
            {
                if (extremePointsHash.Contains(v)) continue;
                AddVertexToProperFace(simplexFaces, v, tolerance);
            }

            // here comes the main loop. We start with the simplex faces and then keep adding faces until we're done
            var faceQueue = new UpdatablePriorityQueue<TFace, double>(simplexFaces.Select(f => (f, f.peakDistance)), new NoEqualSort(false));
            var newFaces = new List<TFace>();
            var oldFaces = new List<TFace>();
            var verticesToReassign = new List<TVertex>();
            while (faceQueue.Count > 0)
            {
                var face = faceQueue.Dequeue();
                // solve the face that has the farthest vertex from it.
                if (face.peakVertex == null)
                {   // given the the priority queue is sorted in descending order, if the peak vertex is null then all the other faces are also null
                    // and we're done, so break the loop. Oh! but before you go, better re-add the face that you just dequeued.
                    faceQueue.Enqueue(face, face.peakDistance);
                    break;
                }
                // this function, CreateNewFaceCone, is the hardest part of the algorithm. But, from its arguments, you can see that it finds
                // faces to remove (oldFaces), faces to add (newFaces), and vertices to reassign (verticesToReassign) to the new faces.
                // These three lists are then processed in the 3 foreach loops below.
                CreateNewFaceCone<TVertex, TEdge, TFace>(face, newFaces, oldFaces, verticesToReassign);
                foreach (var iv in verticesToReassign)
                    AddVertexToProperFace(newFaces, iv, tolerance);
                foreach (var f in oldFaces)
                    faceQueue.Remove(f);
                foreach (var newFace in newFaces)
                    faceQueue.Enqueue(newFace, newFace.peakDistance);
            }
            // now we have the convex hull faces are used to build the convex hull object
            convexHull = MakeConvexHullWithFaces(tolerance, connectVerticesToCvxHullFaces,
                faceQueue.UnorderedItems.Select(fq => fq.Element));
            return true;
        }

        /// <summary>
        /// When only two extrema are found, this method finds a third point that is farthest from the line between the two extrema.
        /// </summary>
        /// <param name="extremePoints"></param>
        /// <param name="vertices"></param>
        /// <param name="radialDistance"></param>
        /// <returns></returns>
        private static TVertex Find3rdStartingPoint<TVertex>(List<TVertex> extremePoints, IList<TVertex> vertices, out double radialDistance)
            where TVertex : IConvexVertex3D
        {
            var axis = extremePoints[1].Coordinates - extremePoints[0].Coordinates;
            radialDistance = double.NegativeInfinity;
            TVertex? thirdPoint = default;
            foreach (var v in vertices)
            {
                var distance = Vector3.Cross(v.Coordinates - extremePoints[0].Coordinates, axis).LengthSquared();
                if (distance > radialDistance)
                {
                    radialDistance = distance;
                    thirdPoint = v;
                }
            }
            return thirdPoint;
        }

        /// <summary>
        /// This is based on the method described in https://algolist.ru/maths/geom/convhull/qhull3d.php as CALCULATE_HORIZON
        /// It's subtle and complicated. One difference is that our borders are populated in the clockwise direction since the
        /// children on the stack are added in the CCW direction.
        /// </summary>
        /// <param name="startingFace"></param>
        /// <param name="newFaces"></param>
        /// <param name="oldFaces"></param>
        /// <param name="verticesToReassign"></param>
        /// <returns></returns>
        private static void CreateNewFaceCone<TVertex, TEdge, TFace>(TFace startingFace, List<TFace> newFaces,
            List<TFace> oldFaces, List<TVertex> verticesToReassign)
            where TVertex : IConvexVertex3D
            where TEdge : IConvexEdge3D
            where TFace : IConvexFace3D
        {
            newFaces.Clear();
            oldFaces.Clear();
            verticesToReassign.Clear();
            TEdge? inConeEdge = default;
            TEdge? firstInConeEdge = default;
            var peakVertex = startingFace.peakVertex;
            var peakCoord = peakVertex.Coordinates;
            var stack = new Stack<(TFace, TEdge)>();
            stack.Push((startingFace, default));

            // that's initialization. now for the main loop. Using a Depth-First Search, we find all the faces that are
            // within (and beyond the horizon) from the peakVertex. Imagine this peak vertex as a point sitting in the middle
            // above the triangle. All the triangles that are visible from this point are the ones that are within the cone.
            // All these triangles need to be removed (stored in oldFaces) and replaced with new triangles (newFaces).
            // The new triangles are formed by connecting the edges on the horizon with the peakVertex.
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
                    ConvexHullFace newFace;
                    if (connectingEdge.OwnedFace.Equals(current))
                    {   // if the current owns the face then the new face will follow the edge backwards
                        newFace = new ConvexHullFace(connectingEdge.To, connectingEdge.From, peakVertex);
                        connectingEdge.OtherFace = newFace;
                        newFace.AddEdge(connectingEdge);
                        if (firstInConeEdge == null) // this is the first time through so we get to own the two in-cone edges as well
                        {
                            // the new face will have three edges. we re-use the connectingEdge as the first edge, but the other two
                            // will be in the cone of new faces. From the above link, we learn that if Depth-First Search is used to
                            // traverse the faces, then the edges on the horizon will be populated exactly in the clockwise direction.
                            // So, inConeEdge will be the edge between this and the next cone face, and firstInConeEdge will be the
                            // the last face that the cone wraps around to.
                            inConeEdge = new Edge(peakVertex, connectingEdge.To, newFace, null, false);
                            inConeEdge.OwnedFace = newFace;
                            newFace.AddEdge(inConeEdge);
                            firstInConeEdge = new Edge(connectingEdge.From, peakVertex, newFace, null, false);
                            newFace.AddEdge(firstInConeEdge);
                            firstInConeEdge.OwnedFace = newFace;
                        }
                        else // then it's not the first time we've been here, so firstEdge AND prevEdge should already be set
                        {
                            newFace.AddEdge(inConeEdge);
                            inConeEdge.OtherFace = newFace;
                            if (firstInConeEdge.To == connectingEdge.To || firstInConeEdge.From == connectingEdge.To)
                            {
                                newFace.AddEdge(firstInConeEdge);
                                firstInConeEdge.OtherFace = newFace;
                            }
                            else
                            {
                                inConeEdge = new Edge(peakVertex, connectingEdge.To, newFace, null, false);
                                newFace.AddEdge(inConeEdge);
                                inConeEdge.OwnedFace = newFace;
                            }
                        }
                    }
                    else // the newFace will own the edge
                    {
                        newFace = new ConvexHullFace(connectingEdge.From, connectingEdge.To, peakVertex);
                        connectingEdge.OwnedFace = newFace;
                        newFace.AddEdge(connectingEdge);
                        if (firstInConeEdge == null) // this is the first time through so we get to own the two in-cone edges as well
                        {
                            inConeEdge = new Edge(peakVertex, connectingEdge.From, newFace, null, false);
                            inConeEdge.OwnedFace = newFace;
                            newFace.AddEdge(inConeEdge);
                            firstInConeEdge = new Edge(connectingEdge.To, peakVertex, newFace, null, false);
                            newFace.AddEdge(firstInConeEdge);
                            firstInConeEdge.OwnedFace = newFace;
                        }
                        else // then it's not the first time we've been here, so firstEdge AND prevEdge should already be set
                        {
                            newFace.AddEdge(inConeEdge);
                            inConeEdge.OtherFace = newFace;
                            if (firstInConeEdge.To == connectingEdge.From || firstInConeEdge.From == connectingEdge.From)
                            {
                                newFace.AddEdge(firstInConeEdge);
                                firstInConeEdge.OtherFace = newFace;
                            }
                            else
                            {
                                inConeEdge = new Edge(peakVertex, connectingEdge.From, newFace, null, false);
                                newFace.AddEdge(inConeEdge);
                                inConeEdge.OwnedFace = newFace;
                            }
                        }
                    }
                    newFaces.Add(newFace);
                }
                else // the face is within the cone. it'll be deleted in the above method, so we better get its interior vertices
                     // and the peak and add them to the verticesToReassign list for later reassignment to the new faces
                {
                    current.Visited = true;
                    oldFaces.Add(current);
                    if (current.peakVertex != null && current.peakVertex != peakVertex)
                        verticesToReassign.Add(current.peakVertex);
                    verticesToReassign.AddRange(current.InteriorVertices);

                    // this is a little complicated, but we need to find the neigbors of the current face that are not the connecting edge
                    // furthermore, for the trick with the inConeEdge to work, we need to generate children in the CCW direction starting 
                    // with the one after the connecting edge. So, we need to find the connecting edge in the list of edges and then start
                    // the loop after that.
                    var connectingIndex = current.AB == connectingEdge ? 0 : current.BC == connectingEdge ? 1 : current.CA == connectingEdge ? 2 : -1;
                    // at the very start, there are no connecting edges, so -1
                    var i = 0;
                    var neighborCount = connectingIndex >= 0 ? 2 : 3;
                    foreach (var edge in current.Edges.Concat(current.Edges)) // the concat has us going through the edges twice!
                    {   // keep going until you pass the connecting edge and then add the rest to the stack
                        if (i++ > connectingIndex)
                        {
                            //if (edge == connectingEdge) continue; //this can  be removed once the code is debugged
                            stack.Push(((ConvexHullFace)edge.GetMatingFace(current), edge));
                            // we can break one we have the two connecting edges (or three when connectingIndex is -1 at the very start).
                            if (--neighborCount == 0) break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method is called at the end of the algorithm to make the convex hull object.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <param name="connectVerticesToCvxHullFaces"></param>
        /// <param name="cvxFaces"></param>
        /// <returns></returns>
        private static ConvexHull3D MakeConvexHullWithFaces<TVertex, TEdge, TFace>(double tolerance, IEnumerable<TFace> cvxFaces)
            where TVertex : IConvexVertex3D
            where TEdge : IConvexEdge3D
            where TFace : IConvexFace3D
        {
            var cvxHull = new ConvexHull3D();
            cvxHull.Faces.AddRange(cvxFaces.Cast<IConvexFace3D>());

            var cvxVertexHash = new HashSet<TVertex>();
            var cvxEdgeHash = new HashSet<TEdge>();
            foreach (var f in cvxFaces)
            {
                foreach (var v in f.Vertices)
                {
                    v.PartOfConvexHull = true;
                    cvxVertexHash.Add(v);
                    if (connectVerticesToCvxHullFaces)
                        v.Faces.Add(f);
                }
                // vertices that are stored in the interior vertices do not define the edges and faces
                // of the convex hull but it is useful to know that they are on the boundary of the convex hull
                foreach (var v in f.InteriorVertices)
                    v.PartOfConvexHull = true;
                foreach (var e in f.Edges)
                {
                    e.PartOfConvexHull = true;
                    cvxEdgeHash.Add(e);
                    if (connectVerticesToCvxHullFaces)
                    {
                        e.From.Edges.Add(e);
                        e.To.Edges.Add(e);
                    }
                }
            }
            cvxHull.Vertices.AddRange(cvxVertexHash);
            cvxHull.Edges.AddRange(cvxEdgeHash);
            return cvxHull;
        }

        /// <summary>
        /// For each vertex, add it to the interior points of the convex hull face that it is farthest from.
        /// Actually, the peakVertex and peakDistance properties of ConvexHullFace store the vertex that is farthest
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="v"></param>
        /// <param name="tolerance"></param>
        private static void AddVertexToProperFace<TFace, TVertex>(IList<TFace> faces, TVertex v, double tolerance)
            where TFace : IConvexFace3D
            where TVertex : IConvexVertex3D
        {
            var maxDot = double.NegativeInfinity;
            TFace maxFace = default;
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
        private static bool SolveAs2D<TVertex>(IList<TVertex> vertices, out ConvexHull3D convexHull, double tolerance = double.NaN)
            where TVertex : IConvexVertex3D
        {
            Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
            var plane = new Plane(distance, planeNormal);
            if (plane.Normal.IsNull() || plane.CalculateMaxError(vertices.Select(v => v.Coordinates)) > tolerance)
            {
                convexHull = null;
                return false;
            }
            var coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out var backTransform).ToList();
            if (coords2D.Area() < 0)
            {
                planeNormal *= -1;
                distance *= -1;
                coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out backTransform).ToList();
            }
            var cvxHull2D = ConvexHull2D.Create(coords2D, out var vertexIndices);
            var indexHash = vertexIndices.ToHashSet();
            convexHull = new ConvexHull3D();
            var interiorVertices = new List<TVertex>();
            for (var i = 0; i < vertices.Count; i++)
            {
                if (indexHash.Contains(i))
                {
                    convexHull.Vertices.Add(vertices[i]);
                    if (convexHull.Vertices.Count < 3) continue;
                    convexHull.Faces.Add(new ConvexHullFace(convexHull.Vertices[0], convexHull.Vertices[convexHull.Vertices.Count - 2],
                        convexHull.Vertices[convexHull.Vertices.Count - 1], planeNormal));
                    convexHull.Faces.Add(new ConvexHullFace(convexHull.Vertices[convexHull.Vertices.Count - 1],
                        convexHull.Vertices[convexHull.Vertices.Count - 2], convexHull.Vertices[0], -planeNormal));
                }
                else interiorVertices.Add(vertices[i]);
            }
            foreach (var v in interiorVertices)
            {
                for (var i = 0; i < convexHull.Faces.Count; i += 2)
                {
                    var face = convexHull.Faces[i];
                    if (MiscFunctions.IsVertexInsideTriangle(new[] { face.A, face.B, face.C }, v.Coordinates))
                    {
                        face.InteriorVertices.Add(v);
                        convexHull.Faces[i + 1].InteriorVertices.Add(v);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// If there are 5 or 6 points from the AABB check, then we run through all possibilities of 4 points 
        /// to find the one tetrahedron with the largest volume. The one or two points that are not part of the
        /// tetrahedron are removed from extremePoints
        /// </summary>
        /// <param name="extremePoints"></param>
        private static void FindBestExtremaSubset<TVertex>(List<TVertex> extremePoints) where TVertex : IConvexVertex3D
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
                        var baseTriangleArea =Vector3.Cross(extremePoints[i2].Coordinates - basePoint.Coordinates,
                            extremePoints[i3].Coordinates - basePoint.Coordinates);
                        for (int i4 = i3 + 1; i4 < numExtrema; i4++)
                        {
                            var projectedHeight = basePoint.Coordinates - extremePoints[i4].Coordinates;
                            var volume = Math.Abs(Vector3.Dot(projectedHeight, baseTriangleArea));
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
        private static List<TFace> MakeSimplexFaces<TVertex, TEdge, TFace>(List<TVertex> vertices) where TVertex : IConvexVertex3D
            where TEdge : IConvexEdge3D
            where TFace : IConvexFace3D
        {
            if (vertices.Count == 3)
            {
                var face012 = new ConvexHullFace(vertices[0], vertices[1], vertices[2]);
                var face210 = new ConvexHullFace(vertices[2], vertices[1], vertices[0]);
                var edge01 = new TEdge(vertices[0], vertices[1], face012, face210, false);
                edge01.OwnedFace = face012;
                edge01.OtherFace = face210;
                var edge12 = new TEdge(vertices[1], vertices[2], face012, face210, false);
                edge12.OwnedFace = face012;
                edge12.OtherFace = face210;
                var edge20 = new TEdge(vertices[2], vertices[0], face012, face210, false);
                edge20.OwnedFace = face012;
                edge20.OtherFace = face210;
                return [face012, face210];
            }
            else
            {
                // in order to get the order correct, we find the volume from the scalar triple product formula
                var basePoint = vertices[0].Coordinates;
                var volume = Vector3.Dot(Vector3.Cross(vertices[1].Coordinates - basePoint, vertices[2].Coordinates - basePoint),
                    basePoint - vertices[3].Coordinates);
                // if the volume is negative then swap the two middle points to get them triangles in the prpoer orientation
                if (volume < 0) Constants.SwapItemsInList(1, 2, vertices);

                var face012 = new TFace(vertices[0], vertices[1], vertices[2]);
                var face031 = new TFace(vertices[0], vertices[3], vertices[1]);
                var face132 = new TFace(vertices[1], vertices[3], vertices[2]);
                var face230 = new TlFace(vertices[2], vertices[3], vertices[0]);
                var edge01 = new TEdge(vertices[0], vertices[1], face012, face031, false);
                edge01.OwnedFace = face012;
                edge01.OtherFace = face031;
                var edge12 = new TEdge(vertices[1], vertices[2], face012, face132, false);
                edge12.OwnedFace = face012;
                edge12.OtherFace = face132;
                var edge20 = new TEdge(vertices[2], vertices[0], face012, face230, false);
                edge20.OwnedFace = face012;
                edge20.OtherFace = face230;
                var edge03 = new TEdge(vertices[0], vertices[3], face031, face230, false);
                edge03.OwnedFace = face031;
                edge03.OtherFace = face230;
                var edge13 = new TEdge(vertices[1], vertices[3], face132, face031, false);
                edge13.OwnedFace = face132;
                edge13.OtherFace = face031;
                var edge23 = new TEdge(vertices[2], vertices[3], face230, face132, false);
                edge23.OwnedFace = face230;
                edge23.OtherFace = face132;
                return [face012, face031, face132, face230];
            }
        }

        /// <summary>
        /// Finds the extreme points on the bounding box
        /// </summary>
        /// <param name="n"></param>
        /// <param name="points"></param>
        /// <param name="numExtrema"></param>
        /// <returns></returns>
        private static List<TVertex> GetExtremaOnAABB<TVertex>(int n, IList<TVertex> points, out int numExtrema)
            where TVertex : IConvexVertex3D
        {
            var extremePoints = Enumerable.Repeat(points[0], 6).ToList();
            for (int i = 1; i < n; i += 2)
            {
                if (points[i].Coordinates.X < extremePoints[0].Coordinates.X ||
                    points[i].Coordinates.X == extremePoints[0].Coordinates.X && points[i].Coordinates.Y < extremePoints[0].Coordinates.Y)
                    extremePoints[0] = points[i];
                if (points[i].Coordinates.X > extremePoints[1].Coordinates.X ||
                    points[i].Coordinates.X == extremePoints[1].Coordinates.X && points[i].Coordinates.Z > extremePoints[1].Coordinates.Z)
                    extremePoints[1] = points[i];
                if (points[i].Coordinates.Y < extremePoints[2].Coordinates.Y ||
                    points[i].Coordinates.Y == extremePoints[2].Coordinates.Y && points[i].Coordinates.Z < extremePoints[2].Coordinates.Z)
                    extremePoints[2] = points[i];
                if (points[i].Coordinates.Y > extremePoints[3].Coordinates.Y ||
                    points[i].Coordinates.Y == extremePoints[3].Coordinates.Y && points[i].Coordinates.X > extremePoints[3].Coordinates.X)
                    extremePoints[3] = points[i];
                if (points[i].Coordinates.Z < extremePoints[4].Coordinates.Z ||
                    points[i].Coordinates.Z == extremePoints[4].Coordinates.Z && points[i].Coordinates.X < extremePoints[4].Coordinates.X)
                    extremePoints[4] = points[i];
                if (points[i].Coordinates.Z > extremePoints[5].Coordinates.Z ||
                    points[i].Coordinates.Z == extremePoints[5].Coordinates.Z && points[i].Coordinates.Y > extremePoints[5].Coordinates.Y)
                    extremePoints[5] = points[i];
            }
            numExtrema = 6;
            for (int i = numExtrema - 1; i > 0; i--)
            {
                var extremeI = extremePoints[i];
                for (int j = 0; j < i; j++)
                {
                    var extremeJ = extremePoints[j];
                    if (extremeI.Equals(extremeJ))
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