namespace PointCloud;

public class ConvexHullFace : IConvexFace3D
{
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

    public bool Equals(IConvexFace3D? other)
    {
        throw new NotImplementedException();
    }
}
