
namespace PointCloudNet;

public class ConvexHullVertex : IConvexVertex3D
{
    public ConvexHullVertex() { }
    public  Vector3 Coordinates { get; init; }
    public  int IndexInList { get; init; }

    public bool Equals(IConvexVertex? other) => this == other;
}
