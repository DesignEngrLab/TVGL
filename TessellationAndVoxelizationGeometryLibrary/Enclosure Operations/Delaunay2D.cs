using System;
using TVGL;

namespace TVGL {
    public class Delaunay2D
    {
        public class Delaunay2D
        {
            /// <summary>
            /// Gets the vertices of the Delaunay Tetrahedral Mesh
            /// </summary>
            public Vertex[] Vertices { get; private set; }
            /// <summary>
            /// Gets the tetrahedra of the Delaunay Tetrahedral Mesh
            /// </summary>
            public Triangle[] Triangles { get; private set; }

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
            public static bool Create(List<Vertex> points, out Delaunay3D delaunay3D)
            {
                Console.Write("starting convex hull/Delaunay...");
                if (ConvexHull3D.Create(points, out var convexHull, true))
                {
                    Console.WriteLine("...finished");

                    var facesToRemove = convexHull.Faces.Where(f => f.Normal.Z > 0).Cast<TriangleFace>().ToArray();

                    solid = new TessellatedSolid(convexHull.Faces.Cast<TriangleFace>().ToArray(),
                        convexHull.Vertices);
                    solid.RemoveFaces(facesToRemove);

                    //Replaces the z values with the actual values
                    for (var i = 0; i < TmpPoints.Count; i++)
                    {
                        TmpPoints[i].Coordinates = new Vector3(TmpPoints[i].X, TmpPoints[i].Y, z_values[i]);
                    }
                    foreach (var face in solid.Faces)
                        face.Update();
                    colorFaces(solid);
                    Console.WriteLine("After delaunay");
                    Presenter.ShowAndHang(solid);
                }
                else
                {
                    Console.WriteLine("Convex Hull failed");
                }
            }
        }
    }
}
