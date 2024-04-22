namespace PointCloudNet;

public interface IConvexVertex : IEquatable<IConvexVertex>
{
    int IndexInList { get; }

}
public interface IConnectToFaces<TFace>
where TFace : IConvexFace3D
{
    List<TFace> Faces { get; set; }
}


public interface IConnectToEdges<TEdge>
    where TEdge : IConvexEdge3D
{
    List<TEdge> Edges { get; set; }
}


public interface IBelongBoolean
{
    bool PartOfConvexHull { get; set; }
}

