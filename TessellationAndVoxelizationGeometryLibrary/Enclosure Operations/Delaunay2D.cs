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
        public static bool Create<T>(List<T> points, out Delaunay2D delaunay2D) where T : IVector2D
        {
            var random = new Random();
            var vertices = new List<Vertex>();
            var avgX = points.Average(p => p.X);
            avgX += 0.001 * avgX * (random.NextDouble() - 0.5);
            var avgY = points.Average(p => p.Y);
            avgY += 0.001 * avgY * (random.NextDouble() - 0.5);
            var xFarthest = points.Max(p => Math.Abs(p.X - avgX));
            var yFarthest = points.Max(p => Math.Abs(p.Y - avgY));

            var reductionFactor = 1 / Math.Sqrt((xFarthest-avgX) * (xFarthest-avgX)
                + (yFarthest-avgY) * (yFarthest-avgY));
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
                if (convexHull.Vertices.Any(v => !v.PartOfConvexHull))
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
    }
}

