namespace ConvexHull.NET
{
    public interface IConvexVertex3D : IEquatable<IConvexVertex3D>
    {
        Vector3 Coordinates { get; }
        int IndexInList { get; }
        
    }
}
