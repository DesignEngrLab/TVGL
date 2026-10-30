using BugViewer;
using System.Collections;
using TVGL;

namespace WebGPUPresenter;

/// <summary>Converts TVGL data lazily; large stepped timelines remain indexed instead of eagerly copied.</summary>
internal static class SceneConversions
{
    internal static ScenePoint Point(Vector3 point) => new(point.X, point.Y, point.Z);
    internal static ColorRgba Color(Color color) => new(color.R, color.G, color.B, color.A);
    internal static SceneTransform? Transform(Matrix4x4 matrix) => matrix.IsNull() ? null : new(
        matrix.M11,matrix.M12,matrix.M13,matrix.M14, matrix.M21,matrix.M22,matrix.M23,matrix.M24,
        matrix.M31,matrix.M32,matrix.M33,matrix.M34, matrix.M41,matrix.M42,matrix.M43,matrix.M44);

    internal static IReadOnlyList<IEnumerable<ScenePoint>> Paths(IReadOnlyList<IEnumerable<Vector3>> paths) =>
        new MappedList<IEnumerable<Vector3>, IEnumerable<ScenePoint>>(paths,
            path => path?.Where(point => !point.IsNull()).Select(Point) ?? []);

    internal static IReadOnlyList<SceneTransform?>? Transforms(IReadOnlyList<Matrix4x4>? transforms) =>
        transforms is null ? null : new MappedList<Matrix4x4, SceneTransform?>(transforms, Transform);

    internal static IReadOnlyList<ColorRgba> Colors(IReadOnlyList<Color> colors) =>
        new MappedList<Color, ColorRgba>(colors, Color);

    private sealed class MappedList<TSource, TResult>(IReadOnlyList<TSource> source, Func<TSource, TResult> convert)
        : IReadOnlyList<TResult>
    {
        public int Count => source.Count;
        public TResult this[int index] => convert(source[index]);
        public IEnumerator<TResult> GetEnumerator()
        {
            for (var index = 0; index < Count; index++) yield return this[index];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
