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
        public static bool Create<T>(List<T> points, out Delaunay2D delaunay2D) where T: IVector2D
        {
            Console.Write("starting convex hull/Delaunay...");
            var TmpPoints = new List<Vertex>();
            //double[] z_values = new double[points.Count()];

            for (int i = 0; i < points.Count(); i++)
            {
                //Saves the vectors as vertices
                //z_values[i] = points[i].Z;
                TmpPoints.Add(new Vertex(points[i].X, points[i].Y, points[i].X * points[i].X + points[i].Y * points[i].Y, i));
            }

            if (ConvexHull3D.Create(TmpPoints, out var convexHull, true, false))
            {
                Console.WriteLine("...finished");

                var facesToRemove = convexHull.Faces.Where(f => f.Normal.Z > 0).Cast<TriangleFace>().ToArray();

                var solid = new TessellatedSolid(convexHull.Faces.Cast<TriangleFace>().ToArray(),
                    convexHull.Vertices);
                solid.RemoveFaces(facesToRemove);
                solid.MakeEdgesIfNonExistent();
                //Replaces the z values with the actual values
                for (var i = 0; i < points.Count; i++)
                {
                    if (points[i] is IVector3D vector3D)
                        TmpPoints[i].Coordinates = new Vector3(points[i].X, points[i].Y, vector3D.Z);
                    else
                        TmpPoints[i].Coordinates = new Vector3(points[i].X, points[i].Y, 0);
                }
                foreach (var face in solid.Faces)
                    face.Update();

                delaunay2D = new Delaunay2D()
                {
                    Vertices = TmpPoints.ToArray(),
                    Faces = solid.Faces,
                    Edges = solid.Edges
                };

                //colorFaces(solid);
                //Console.WriteLine("After delaunay");
                //delaunay2D.colorFaces(solid);
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
        /* for debugging 
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
        */
    }
}

