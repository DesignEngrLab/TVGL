namespace ConvexHull.NET
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public partial class ConvexHull3D
    {
        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        //public double tolerance { get; internal init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<IConvexVertex3D> Vertices = new List<IConvexVertex3D>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<IConvexFace3D> Faces = new List<IConvexFace3D>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public List<IConvexEdge3D> Edges = new List<IConvexEdge3D>();



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
        }
    }

}
