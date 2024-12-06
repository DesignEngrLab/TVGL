using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using TVGL;
using TVGL.amfclasses;

namespace TVGL {
    public class Delaunay2D
    {
        /// <summary>
        /// Gets the vertices of the Delaunay Tetrahedral Mesh
        /// </summary>
        public Vertex[] Vertices { get; private set; }
        /// <summary>
        /// Gets the tetrahedra of the Delaunay Tetrahedral Mesh
        /// </summary>
        public Polygon[] Triangles { get; private set; }

        /// <summary>       
        /// Gets the faces of the Delaunay Tetrahedral Mesh
        /// </summary>
        public TriangleFace[] Faces { get; private set; }
        /// <summary>        
        /// Gets the edges of the Delaunay Tetrahedral Mesh
        /// </summary>
        public Edge[] Edges { get; private set; }

        /// <summary>
        /// Create the Delaunay 3D mesh of tetrahedra from the points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="delaunay3D"></param>
        /// <returns></returns>
        public static bool Create(List<Vertex> points, out Delaunay2D delaunay2D)
        {
            Console.Write("starting convex hull/Delaunay...");
        var TmpPoints = new List<Vertex>();
        double[] z_values = new double[points.Count()];

        for (int i = 0; i < points.Count(); i++)
        {
            //Saves the vectors as vertices
            z_values[i] = points[i].Z;
            TmpPoints.Add(new Vertex(points[i].X, points[i].Y, points[i].X * points[i].X + points[i].Y * points[i].Y, i));
        }

            if (ConvexHull3D.Create(TmpPoints, out var convexHull, true))
            {
                Console.WriteLine("...finished");

                var facesToRemove = convexHull.Faces.Where(f => f.Normal.Z > 0).Cast<TriangleFace>().ToArray();

                var solid = new TessellatedSolid(convexHull.Faces.Cast<TriangleFace>().ToArray(),
                    convexHull.Vertices);
                solid.RemoveFaces(facesToRemove);

                //Replaces the z values with the actual values
                for (var i = 0; i < points.Count; i++)
                {
                    TmpPoints[i].Coordinates = new Vector3(points[i].X, points[i].Y, z_values[i]);
                }
                foreach (var face in solid.Faces)
                    face.Update();

                delaunay2D = new Delaunay2D() {
                    Vertices = TmpPoints.ToArray(),
                    Faces = solid.Faces.ToArray(),
                };

                //colorFaces(solid);
                Console.WriteLine("After delaunay");
                delaunay2D.colorFaces(solid);
                Presenter.ShowAndHang(solid);
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
        void colorFaces(TessellatedSolid solid)
        {
            solid.HasUniformColor = false;
            var minZ = solid.Vertices.Min(v => v.Z);
            var maxZ = solid.Vertices.Max(v => v.Z);
            foreach (var face in solid.Faces)
            {
                var avgZ = face.Vertices.Sum(v => v.Z) / 3;
                var color = Color.HSVtoRGB((avgZ - minZ) / (maxZ - minZ));  //Displays the mesh as a heatmap. May need to adjust
                face.Color = color;
            }
        }
    }
    }

