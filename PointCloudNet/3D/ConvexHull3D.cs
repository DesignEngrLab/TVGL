namespace PointCloud;
/// <summary>
/// The Convex Hull of a Tesselated Solid
/// </summary>
public partial class ConvexHull3D<TVertex, TEdge, TFace>
            where TVertex : IConvexVertex3D
            where TEdge : IConvexEdge3D
            where TFace : IConvexFace3D
{
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
}

