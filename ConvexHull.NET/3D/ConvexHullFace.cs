namespace ConvexHull.NET
{
    public class ConvexHullFace : IConvexFace3D
    {
        public ConvexHullFace()
        {
        }
        internal ConvexHullFace(IConvexVertex3D vertex1, IConvexVertex3D vertex2, IConvexVertex3D vertex3, Vector3 planeNormal)
            : this(vertex1, vertex2, vertex3)
        {
            Normal = planeNormal;
        }
        internal ConvexHullFace(IConvexVertex3D vertex1, IConvexVertex3D vertex2, IConvexVertex3D vertex3)
        {
            peakVertex = null;
            InteriorVertices = new List<IConvexVertex3D>();
        }
        public Vector3 Normal { get; init; }

        public IConvexVertex3D peakVertex { get;  set; }
        public double peakDistance { get;  set; }

        /// <summary>
        /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
        /// of the convex hull
        /// </summary>
        public List<IConvexVertex3D> InteriorVertices { get;  set; }
        public bool Visited { get;  set; }
        public IConvexVertex3D A { get; set; }
        public IConvexVertex3D B { get; set; }
        public IConvexVertex3D C { get; set; }
        public IConvexEdge3D AB { get; set; }
        public IConvexEdge3D BC { get; set; }
        public IConvexEdge3D CA { get; set; }
    }

}
