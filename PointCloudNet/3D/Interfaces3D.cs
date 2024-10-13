namespace PointCloud;
public interface IConvexVertex3D : IConvexVertex
{
    Vector3 Coordinates { get; init; }
}

public interface IConvexEdge3D : IEquatable<IConvexEdge3D>
{
    IConvexVertex3D From { get; set; }
    IConvexVertex3D To { get; set; }
    IConvexFace3D OwnedFace { get; set; }
    IConvexFace3D OtherFace { get; set; }
}


public interface IConvexFace3D : IEquatable<IConvexFace3D>
{
    IConvexVertex3D peakVertex { get; set; }
    double peakDistance { get; set; }
    bool Visited { get; set; }

    /// <summary>
    /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
    /// of the convex hull
    /// </summary>
    List<IConvexVertex3D> InteriorVertices { get; set; }

    Vector3 Normal { get; init; }

    IConvexVertex3D A { get; set; }
    IConvexVertex3D B { get; set; }
    IConvexVertex3D C { get; set; }
    IConvexEdge3D AB { get; set; }
    IConvexEdge3D BC { get; set; }
    IConvexEdge3D CA { get; set; }
}