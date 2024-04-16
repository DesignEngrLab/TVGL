global using System.Runtime.CompilerServices;

global using Vector2 = System.Numerics.Vector2;
global using Vector3 = System.Numerics.Vector3;
global using Vector4 = System.Numerics.Vector4;
using PointCloud.NET;

internal static class Constants
{
    // for single precision
    internal const double BaseTolerance = 1e-8;
    // for double precision
    //internal const double BaseTolerance = 1e-12;

    internal static double Dot(this Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }
    internal static double Dot(this Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }
    internal static double Dot(this Vector4 a, Vector4 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }
    internal static double Cross(this Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
    internal static Vector3 Cross(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    }


    internal static void SwapItemsInList<T>(int i, int j, IList<T> points)
    {
        var temp = points[i];
        points[i] = points[j];
        points[j] = temp;
    }
}