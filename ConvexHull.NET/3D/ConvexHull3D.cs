namespace PointCloud.NET
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public partial class ConvexHull3D<TVertex, TEdge, TFace>
            where TVertex : IConvexVertex3D
            where TEdge : IConvexEdge3D
            where TFace : IConvexFace3D
    {
        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        //public double tolerance { get; internal init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<TVertex> Vertices = new List<TVertex>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<TFace> Faces = new List<TFace>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public List<TEdge> Edges = new List<TEdge>();



        /// <summary>
        /// Calculates the center.
        /// </summary>
        public Vector3 CalculateCenter()
        { return Vector3.Zero; }

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        public double CalculateVolume() 
        {
            return double.NaN;
        }

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        public double CalculateSurfaceArea()
        {
            return double.NaN;
            //return Faces.Sum(f => f.Area);
        }
    }

}
