using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;

namespace TVGL
{
    public class Delaunay2D
    {
        /// <summary>
        /// Gets the vertices of the Delaunay Triangular Mesh
        /// </summary>
        public Vertex[] Vertices { get; internal set; }


        /// <summary>       
        /// Gets the faces of the Delaunay Triangular Mesh
        /// </summary>
        public TriangleFace[] Faces { get; internal set; }
        /// <summary>        
        /// Gets the edges of the Delaunay Triangular Mesh
        /// </summary>
        public Edge[] Edges { get; internal set; }

        /// <summary>
        /// Create the Delaunay 3D mesh of tetrahedra from the points. This may be quicker than the default below
        /// (Bowyer-Watson) for large point sets, but it tends to skip points that are too similar to others.
        /// It is left here for reference, but it is not recommended for general use.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="delaunay3D"></param>
        /// <returns></returns>
        public static bool CreateViaConvexHull<T>(List<T> points, out Delaunay2D delaunay2D) where T : IVector2D
        {
            var random = new Random();
            var vertices = new List<Vertex>();
            var avgX = points.Average(p => p.X);
            avgX += 0.001 * avgX * (random.NextDouble() - 0.5);
            var avgY = points.Average(p => p.Y);
            avgY += 0.001 * avgY * (random.NextDouble() - 0.5);
            var xFarthest = points.Max(p => Math.Abs(p.X - avgX));
            var yFarthest = points.Max(p => Math.Abs(p.Y - avgY));

            var reductionFactor = 1 / Math.Sqrt((xFarthest - avgX) * (xFarthest - avgX)
                + (yFarthest - avgY) * (yFarthest - avgY));
            // getting ready for convex hull, we need to add a z value to the points
            for (int i = 0; i < points.Count(); i++)
            {
                var vIndex = (points[i] is Vertex v3d) ? v3d.IndexInList : (points[i] is Vertex2D v2d) ? v2d.IndexInList : i;
                var xDelta = points[i].X - avgX;
                var yDelta = points[i].Y - avgY;
                var dSqd = reductionFactor * (xDelta * xDelta + yDelta * yDelta);
                var vertex = new Vertex(xDelta, yDelta, dSqd, vIndex);
                vertices.Add(vertex);
            }


            if (ConvexHull3D.Create(vertices, out var convexHull, true, true))
            {
                if (convexHull.Vertices.Count < vertices.Count)
                    throw new Exception("Problem mapping all triangles. Two points are too similar or too many collinearities.");
                //removing the faces that are on the back
                var topCoverFaces = convexHull.Faces.Where(f => f.Normal.Z >= 0).ToHashSet();
                for (int i = convexHull.Edges.Count - 1; i >= 0; i--)
                {
                    var edge = convexHull.Edges[i];
                    if (topCoverFaces.Contains(edge.OwnedFace)) edge.OwnedFace = null;
                    if (topCoverFaces.Contains(edge.OtherFace)) edge.OtherFace = null;
                    if (edge.OwnedFace == null && edge.OtherFace == null)
                        convexHull.Edges.RemoveAt(i);
                    var ownedFace = edge.OwnedFace;    // why flip these? Even though the convex hull algorithm creates the faces with
                    edge.OwnedFace = edge.OtherFace; // the normal pointing outwards - this outwards is down facing (underside of paraboloid),
                    edge.OtherFace = ownedFace;      // but for the Delaunay triangulation, triangles will be viewed in traditional x-y plane
                }
                for (int i = 0; i < convexHull.Vertices.Count; i++)
                {
                    var vert = convexHull.Vertices[i];
                    for (int j = vert.Faces.Count - 1; j >= 0; j--)
                    {
                        if (topCoverFaces.Contains(vert.Faces[j]))
                            vert.Faces.RemoveAt(j);
                    }
                }
                convexHull.Faces.RemoveAll(topCoverFaces.Contains);
                if (points[0] is IVector3D vector3D)
                {
                    for (int i = 0; i < points.Count(); i++)
                        vertices[i].Coordinates = new Vector3(points[i].X, points[i].Y, ((IVector3D)points[i]).Z);
                }
                else
                    for (int i = 0; i < points.Count(); i++)
                        vertices[i].Coordinates = new Vector3(points[i].X, points[i].Y, 0);

                foreach (var face in convexHull.Faces)
                    face.Invert();

                delaunay2D = new Delaunay2D()
                {
                    Vertices = vertices.ToArray(),
                    Faces = convexHull.Faces.ToArray(),
                    Edges = convexHull.Edges.ToArray()
                };
                return true;
            }
            else
            {
                Console.WriteLine("Convex Hull failed");
                delaunay2D = new Delaunay2D()
                {
                    Vertices = null,
                    Edges = null,
                    Faces = null
                };
                return false;
            }
        }
        /// <summary>
        /// Create the Delaunay 2D mesh of triangles from the points using the Bowyer-Watson algorithm.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="delaunay2D"></param>
        /// <returns></returns>
        public static bool Create<T>(ICollection<T> points, out Delaunay2D delaunay2D) where T : IVector2D
        {
            if (points == null || points.Count < 3)
            {
                delaunay2D = null;
                return false;
            }

            var delaunayVertices = new Vertex[points.Count];
            // Create TVGL vertices and track original Z and IndexInList if applicable
            var i = 0;
            if (points.First() is Vertex)
                foreach (var point in points)
                {
                    var vert3D = point as Vertex;
                    delaunayVertices[i++] = vert3D;
                }
            else if (points.First() is Vertex2D)
                foreach (var point in points)
                {
                    var vert2D = point as Vertex2D;
                    delaunayVertices[i++] = new Vertex(new Vector3(point.X, point.Y, 0.0), vert2D.IndexInList);
                }
            else if (points.First() is IVector3D)
                foreach (var point in points)
                    delaunayVertices[i++] = new Vertex(new Vector3(point.X, point.Y, ((IVector3D)point).Z), i);
            else
                foreach (var point in points)
                    delaunayVertices[i++] = new Vertex(new Vector3(point.X, point.Y, 0.0), i);

            var maxIndex = 1 + delaunayVertices.Max(v => v.IndexInList);
            // Find bounds for super triangle
            var centerX = delaunayVertices.Average(p => p.X);
            var centerY = delaunayVertices.Average(p => p.Y);
            var radius = Math.Sqrt(delaunayVertices.Max(p => (p.X - centerX) * (p.X - centerX) + (p.Y - centerY) * (p.Y - centerY)));
            // so all points are in the circle defined here. The encompassing triangle will have a vertices that are r/cos60
            // or double the distance away. Then, in order that the super triangle not be too tight (to prevent the edge triangles
            // from being too extreme, we add a little more
            radius *= 2.1;

            // Super triangle vertices (large enough to encompass all points)
            var topSuperV = new Vertex(new Vector3(centerX, centerY + radius, 0), maxIndex++);
            var leftSuperV = new Vertex(new Vector3(centerX - 0.866 * radius, centerY - 0.5 * radius, 0), maxIndex++);
            var rightSuperV = new Vertex(new Vector3(centerX + 0.866 * radius, centerY - 0.5 * radius, 0), maxIndex++);
            var superTriangle = new TriangleFace(topSuperV, leftSuperV, rightSuperV, false);
            // for now, the vertices won't link to the faces. We'll need to do that at the end
            var topToLeftEdge = new Edge(topSuperV, leftSuperV, superTriangle, null, false);
            var leftToRightEdge = new Edge(leftSuperV, rightSuperV, superTriangle, null, false);
            var rightToTopEdge = new Edge(rightSuperV, topSuperV, superTriangle, null, false);
            Circle.CreateFrom3Points(topSuperV, leftSuperV, rightSuperV, out var superCircle);
            var circles = new List<Circle> { superCircle };
            var triangles = new List<TriangleFace> { superTriangle };
            var outerEdges = new Dictionary<Edge, bool>();
            var newInnerEdges = new Dictionary<long, Edge>();
            // the main loop: for each point, we find the triangles whose circumcircles contain the point,
            // remove those triangles, and re-triangulate the resulting hole with the new point
            foreach (var vertex in delaunayVertices)
            {
                var badTriangleIndices = new List<int>();
                // gather all the triangles that contain the new point, p in their circumcircles.
                // At the beginning this will be the super
                // triangle, but as we go on, it will be more and more local
                for (int j = triangles.Count - 1; j >= 0; j--)
                // this is reversed so that the order in badTriangleIndices is from the end
                // of the list to the beginning, which will be important when we remove them
                // from the triangles list in this next loop
                {
                    if (PointIsInCircle(vertex, circles[j]))
                        badTriangleIndices.Add(j);
                }

                outerEdges.Clear();
                foreach (var badIndex in badTriangleIndices)
                {
                    var t = triangles[badIndex];
                    foreach (var e in t.Edges)
                    {
                        if (!outerEdges.ContainsKey(e))
                        {  //  first time seeing this edge. Store it with a flag indicating
                           //  whether it was owned by the triangle that is being removed
                            outerEdges.Add(e, e.OwnedFace == t);
                        }
                        else
                        {   //if already in dictionary then we actually remove from the dictionary
                            // because this means it's between two bad triangles
                            outerEdges.Remove(e);
                            // the inner edges will be forgotten along with the bad triangles,
                            // so we don't need to unlink them from each other
                        }
                    }
                    triangles.RemoveAt(badIndex);
                    circles.RemoveAt(badIndex);
                }
                foreach (var (edge, isOwned) in outerEdges)
                {
                    TriangleFace newTriangle;
                    if (isOwned)
                    {
                        newTriangle = new TriangleFace(edge.From, edge.To, vertex, false);
                        edge.OwnedFace = newTriangle;
                    }
                    else
                    {
                        newTriangle = new TriangleFace(edge.To, edge.From, vertex, false);
                        edge.OtherFace = newTriangle;
                    }
                    newTriangle.AddEdge(edge);
                    var fromSideRef = Edge.GetEdgeChecksum(edge.From.IndexInList, vertex.IndexInList);
                    if (newInnerEdges.TryGetValue(fromSideRef, out var fromSideEdge))
                    {
                        newTriangle.AddEdge(fromSideEdge);
                        if (isOwned == (fromSideEdge.From == vertex))
                            fromSideEdge.OwnedFace = newTriangle;
                        else fromSideEdge.OtherFace = newTriangle;
                    }
                    else
                    {
                        if (isOwned)
                            fromSideEdge = new Edge(vertex, edge.From, newTriangle, null, false);
                        else
                            fromSideEdge = new Edge(edge.From, vertex, newTriangle, null, false);
                        newInnerEdges.Add(fromSideRef, fromSideEdge);
                    }
                    var toSideRef = Edge.GetEdgeChecksum(edge.To.IndexInList, vertex.IndexInList);
                    if (newInnerEdges.TryGetValue(toSideRef, out var toSideEdge))
                    {
                        newTriangle.AddEdge(toSideEdge);
                        if (isOwned == (toSideEdge.To == vertex))
                            toSideEdge.OwnedFace = newTriangle;
                        else toSideEdge.OtherFace = newTriangle;
                    }
                    else
                    {
                        if (isOwned)
                            toSideEdge = new Edge(edge.To, vertex, newTriangle, null, false);
                        else
                            toSideEdge = new Edge(vertex, edge.To, newTriangle, null, false);
                        newInnerEdges.Add(toSideRef, toSideEdge);
                    }
                    triangles.Add(newTriangle);
                    Circle.CreateFrom3Points(newTriangle.A, newTriangle.B, newTriangle.C, out var newCircle);
                    circles.Add(newCircle);
                }
            }

            // Remove triangles sharing vertices with super triangle
            var edges = new HashSet<Edge>();
            for (i = triangles.Count - 1; i >= 0; i--)
            {
                TriangleFace t = triangles[i];
                if (t.A == topSuperV || t.B == topSuperV || t.C == topSuperV ||
                    t.A == leftSuperV || t.B == leftSuperV || t.C == leftSuperV ||
                    t.A == rightSuperV || t.B == rightSuperV || t.C == rightSuperV)
                {
                    triangles.RemoveAt(i);
                    if (t.AB.OwnedFace == t) t.AB.OwnedFace = null; else t.AB.OtherFace = null;
                    if (t.BC.OwnedFace == t) t.BC.OwnedFace = null; else t.BC.OtherFace = null;
                    if (t.CA.OwnedFace == t) t.CA.OwnedFace = null; else t.CA.OtherFace = null;
                }
                else // we are keeping the triangle, so finish linking the vertices (and the edges to the vertices)
                {    // and also add the edges to the edge list if not already there
                    foreach (var v in t.Vertices)
                        v.Faces.Add(t);
                    foreach (var e in t.Edges)
                    {
                        if (edges.Add(e)) // first time seeing the edge, so link it to the vertices and add to edge list
                        {
                            e.From.Edges.Add(e);
                            e.To.Edges.Add(e);
                        }
                    }
                }
            }
            delaunay2D = new Delaunay2D()
            {
                Vertices = delaunayVertices.ToArray(),
                Faces = triangles.ToArray(),
                Edges = edges.ToArray()
            };
            return true;
        }

        private static bool PointIsInCircle(Vertex p, Circle c)
        {
            return (p.X - c.Center.X) * (p.X - c.Center.X) + (p.Y - c.Center.Y) * (p.Y - c.Center.Y)
                < c.RadiusSquared;
        }
    }
}

