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
        /// Create the Delaunay 3D mesh of tetrahedra from the points.
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
        public static bool Create<T>(List<T> points, out Delaunay2D delaunay2D) where T : IVector2D
        {
            if (points == null || points.Count < 3)
            {
                delaunay2D = null;
                return false;
            }

            var tvglVertices = new List<Vertex>();
            // Create TVGL vertices and track original Z if applicable
            if (points[0] is Vertex)
                for (int i = 0; i < points.Count; i++)
                {
                    var vert3D = points[i] as Vertex;
                    tvglVertices.Add(vert3D);
                }
            else if (points[0] is Vertex2D)
                for (int i = 0; i < points.Count; i++)
                {
                    var vert2D = points[i] as Vertex2D;
                    tvglVertices.Add(new Vertex(new Vector3(points[i].X, points[i].Y, 0.0), vert2D.IndexInList));
                }
            else if (points[0] is IVector3D)
                for (int i = 0; i < points.Count; i++)
                    tvglVertices.Add(new Vertex(new Vector3(points[i].X, points[i].Y, ((IVector3D)points[i]).Z), i));
            else
                for (int i = 0; i < points.Count; i++)
                    tvglVertices.Add(new Vertex(new Vector3(points[i].X, points[i].Y, 0.0), i));

            // Find bounds for super triangle
            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxX = points.Max(p => p.X);
            double maxY = points.Max(p => p.Y);
            double dx = maxX - minX;
            double dy = maxY - minY;
            double deltaMax = Math.Max(dx, dy);
            double midX = (minX + maxX) / 2.0;
            double midY = (minY + maxY) / 2.0;

            // Super triangle vertices (large enough to encompass all points)
            var p1 = new Vertex(new Vector3(midX - 20 * deltaMax, midY - deltaMax, 0));
            var p2 = new Vertex(new Vector3(midX, midY + 20 * deltaMax, 0));
            var p3 = new Vertex(new Vector3(midX + 20 * deltaMax, midY - deltaMax, 0));

            var superTriangle = new BWTriangle(p1, p2, p3);
            var triangles = new List<BWTriangle> { superTriangle };

            foreach (var p in tvglVertices)
            {
                var badTriangles = new List<BWTriangle>();
                foreach (var t in triangles)
                    if (t.IsPointInCircumcircle(p.Coordinates.X, p.Coordinates.Y))
                        badTriangles.Add(t);

                var polygon = new List<(Vertex v1, Vertex v2)>();
                foreach (var t in badTriangles)
                {
                    foreach (var edge in t.Edges)
                    {
                        // If this edge is not shared with any other bad triangle, it goes in the polygon
                        bool isShared = false;
                        foreach (var otherT in badTriangles)
                        {
                            if (otherT != t && otherT.HasEdge(edge.v1, edge.v2))
                            {
                                isShared = true;
                                break;
                            }
                        }
                        if (!isShared)
                            polygon.Add(edge);
                    }
                }

                foreach (var badT in badTriangles)
                    triangles.Remove(badT);

                foreach (var edge in polygon)
                    triangles.Add(new BWTriangle(edge.v1, edge.v2, p));
            }

            // Remove triangles sharing vertices with super triangle
            triangles.RemoveAll(t => t.HasVertex(p1) || t.HasVertex(p2) || t.HasVertex(p3));

            // Assemble TVGL Faces and Edges
            var faces = new List<TriangleFace>();
            var edgesDict = new Dictionary<long, Edge>();

            long GetEdgeKey(int i1, int i2) => i1 < i2 ? ((long)i1 << 32) | (uint)i2 : ((long)i2 << 32) | (uint)i1;

            foreach (var t in triangles)
            {
                var face = new TriangleFace(t.V1, t.V2, t.V3);
                faces.Add(face);

                // Add faces to vertices
                t.V1.Faces.Add(face);
                t.V2.Faces.Add(face);
                t.V3.Faces.Add(face);

                // Build TVGL edges
                var vArr = new[] { t.V1, t.V2, t.V3 };
                for (int i = 0; i < 3; i++)
                {
                    var from = vArr[i];
                    var to = vArr[(i + 1) % 3];
                    var key = GetEdgeKey(from.IndexInList, to.IndexInList);

                    if (!edgesDict.TryGetValue(key, out var edge))
                    {
                        edge = new Edge(from, to, face, null, false);
                        edgesDict.Add(key, edge);
                    }
                    else
                    {
                        edge.OtherFace = face;
                    }
                    if (i == 0) face.AB = edge;
                    else if (i == 1) face.BC = edge;
                    else face.CA = edge;
                }
            }
            delaunay2D = new Delaunay2D()
            {
                Vertices = tvglVertices.ToArray(),
                Faces = faces.ToArray(),
                Edges = edgesDict.Values.ToArray()
            };
            return true;
        }

        private class BWTriangle
        {
            public Vertex V1 { get; }
            public Vertex V2 { get; }
            public Vertex V3 { get; }

            public (Vertex v1, Vertex v2)[] Edges { get; }

            private double circumcenterX;
            private double circumcenterY;
            private double circumradiusSq;

            public BWTriangle(Vertex v1, Vertex v2, Vertex v3)
            {
                // Ensure counter-clockwise for consistent outwards normals
                if (CrossProduct2D(v1, v2, v3) < 0)
                {
                    V1 = v1; V2 = v3; V3 = v2;
                }
                else
                {
                    V1 = v1; V2 = v2; V3 = v3;
                }

                Edges = new[]
                {
                    (V1, V2),
                    (V2, V3),
                    (V3, V1)
                };

                CalculateCircumcircle();
            }

            private double CrossProduct2D(Vertex a, Vertex b, Vertex c)
            {
                return (b.Coordinates.X - a.Coordinates.X) * (c.Coordinates.Y - a.Coordinates.Y) -
                       (b.Coordinates.Y - a.Coordinates.Y) * (c.Coordinates.X - a.Coordinates.X);
            }

            public bool HasVertex(Vertex v)
            {
                return V1 == v || V2 == v || V3 == v;
            }

            public bool HasEdge(Vertex a, Vertex b)
            {
                return (V1 == a && V2 == b) || (V2 == a && V3 == b) || (V3 == a && V1 == b) ||
                       (V1 == b && V2 == a) || (V2 == b && V3 == a) || (V3 == b && V1 == a);
            }

            public bool IsPointInCircumcircle(double px, double py)
            {
                double dx = px - circumcenterX;
                double dy = py - circumcenterY;
                return (dx * dx + dy * dy) <= circumradiusSq;
            }

            private void CalculateCircumcircle()
            {
                double ax = V1.Coordinates.X, ay = V1.Coordinates.Y;
                double bx = V2.Coordinates.X, by = V2.Coordinates.Y;
                double cx = V3.Coordinates.X, cy = V3.Coordinates.Y;

                double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

                if (Math.Abs(d) < 1e-12)
                {
                    circumcenterX = 0;
                    circumcenterY = 0;
                    circumradiusSq = double.PositiveInfinity;
                    return;
                }

                double aSq = ax * ax + ay * ay;
                double bSq = bx * bx + by * by;
                double cSq = cx * cx + cy * cy;

                circumcenterX = (aSq * (by - cy) + bSq * (cy - ay) + cSq * (ay - by)) / d;
                circumcenterY = (aSq * (cx - bx) + bSq * (ax - cx) + cSq * (bx - ax)) / d;

                double dx = circumcenterX - ax;
                double dy = circumcenterY - ay;
                circumradiusSq = dx * dx + dy * dy;
            }
        }
    }
}

