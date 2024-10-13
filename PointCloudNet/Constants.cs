namespace PointCloud;
public static class Constants
{
    // for single precision
    public const double BaseTolerance = 1e-8;
    // for double precision
    //public const double BaseTolerance = 1e-12;
    public const double DefaultEqualityTolerance = 1e-12;



    public static void SwapItemsInList<T>(int i, int j, IList<T> points)
    {
        var temp = points[i];
        points[i] = points[j];
        points[j] = temp;
    }
}