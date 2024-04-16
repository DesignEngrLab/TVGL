namespace PointCloud.NET
{
    public interface IConvexEdge3D : IEquatable<IConvexEdge3D>
    {
        IConvexFace3D OwnedFace { get; set; }
        IConvexFace3D OtherFace { get; set; }
        IConvexVertex3D From { get; set; }
        IConvexVertex3D To { get; set; }
    }
}
