namespace PointCloud.NET
{
    public interface IConvexVertex3D : IEquatable<IConvexVertex3D>
    {
        Vector3 Coordinates { get; }
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
}
