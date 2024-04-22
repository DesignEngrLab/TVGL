namespace PointCloudNet;


/// <summary>
/// Represents a hyper-rectangle. An N-Dimensional rectangle.
/// </summary>
/// <typeparam name="T">The type of "i" in the metric space in which the hyper-rectangle lives.</typeparam>
internal readonly struct HyperRect
{
    internal double[] MinPoint { get; private init; }
    internal double[] MaxPoint { get; private init; }


    /// <summary>
    /// Get a hyper rectangle which spans the entire implicit metric space.
    /// </summary>
    internal HyperRect(int dimensions)
    {
        MinPoint = Enumerable.Repeat(double.MinValue, dimensions).ToArray();
        MaxPoint = Enumerable.Repeat(double.MaxValue, dimensions).ToArray();
    }

    internal HyperRect(double[] minValues, double[] maxValues)
    {
        MinPoint = minValues.Clone() as double[];
        MaxPoint = maxValues.Clone() as double[];
    }

}
