namespace PointCloud;

public readonly struct Sphere
{
    public readonly Vector3 Center;
    public readonly double Radius;

    public Sphere(Vector3 center, double radius)
    {
        Center = center;
        Radius = radius;
    }
}
