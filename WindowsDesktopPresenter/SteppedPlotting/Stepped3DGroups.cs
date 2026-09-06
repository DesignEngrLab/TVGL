using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Media3D = System.Windows.Media.Media3D;

namespace WindowsDesktopPresenter
{
    internal sealed class SteppedPathGroup : IDisposable
    {
        private readonly IReadOnlyList<IEnumerable<TVGL.Vector3>> paths;
        private readonly IReadOnlyList<TVGL.Matrix4x4> transforms;
        private readonly IReadOnlyList<bool> closePaths;
        private readonly IReadOnlyList<double> lineThicknesses;
        private readonly IReadOnlyList<TVGL.Color> colors;
        private readonly Func<IEnumerable<TVGL.Vector3>, double, TVGL.Color, bool, GeometryModel3D> createGeometry;
        private readonly Func<TVGL.Matrix4x4, Media3D.Transform3D> createTransform;
        private readonly Dictionary<int, GeometryModel3D> geometryCache = [];
        private readonly int? historyStepLimit;
        private readonly TVGL.Color defaultColor;
        private readonly List<int> nullTransformIndices = [];
        private int transformsInspectedThrough = -1;

        internal SteppedPathGroup(IReadOnlyList<IEnumerable<TVGL.Vector3>> paths,
            IReadOnlyList<TVGL.Matrix4x4> transforms, IReadOnlyList<bool> closePaths,
            IReadOnlyList<double> lineThicknesses, IReadOnlyList<TVGL.Color> colors,
            int? historyStepLimit, TVGL.Color defaultColor,
            Func<IEnumerable<TVGL.Vector3>, double, TVGL.Color, bool, GeometryModel3D> createGeometry,
            Func<TVGL.Matrix4x4, Media3D.Transform3D> createTransform)
        {
            this.paths = paths ?? [];
            this.transforms = transforms;
            this.closePaths = closePaths ?? [];
            this.lineThicknesses = lineThicknesses ?? [];
            this.colors = colors ?? [];
            this.historyStepLimit = historyStepLimit;
            this.defaultColor = defaultColor;
            this.createGeometry = createGeometry;
            this.createTransform = createTransform;
        }

        internal int Count => Math.Max(paths.Count, transforms?.Count ?? 0);

        internal IEnumerable<GeometryModel3D> Resolve(int stepIndex)
        {
            var visibleIndices = ResolveIndices(stepIndex).ToList();
            if (historyStepLimit.HasValue)
            {
                var visibleSet = visibleIndices.ToHashSet();
                foreach (var staleIndex in geometryCache.Keys.Where(index => !visibleSet.Contains(index)).ToList())
                {
                    geometryCache[staleIndex].Dispose();
                    geometryCache.Remove(staleIndex);
                }
            }

            Media3D.Transform3D transform = null;
            if (transforms is not null && stepIndex < transforms.Count && !transforms[stepIndex].IsNull())
                transform = createTransform(transforms[stepIndex]);

            foreach (var index in visibleIndices)
            {
                if (index < 0 || index >= paths.Count)
                    continue;
                var path = paths[index];
                if (path is null)
                    continue;
                if (!geometryCache.TryGetValue(index, out var geometry))
                {
                    var pathPoints = path as IReadOnlyList<TVGL.Vector3> ?? path.ToList();
                    if (pathPoints.Count < 2 || pathPoints.Any(point => point.IsNull()))
                        continue;
                    geometry = createGeometry(pathPoints, ValueAt(lineThicknesses, index, 1.0),
                        ValueAt(colors, index, defaultColor), ValueAt(closePaths, index, false));
                    geometryCache.Add(index, geometry);
                }
                geometry.Transform = transform;
                yield return geometry;
            }
        }

        internal IReadOnlyList<int> ResolveIndicesForTesting(int stepIndex) => ResolveIndices(stepIndex).ToList();

        private IEnumerable<int> ResolveIndices(int stepIndex)
        {
            if (historyStepLimit == 0 || paths.Count == 0 || stepIndex < 0)
                yield break;

            if (transforms is null)
            {
                var endIndex = historyStepLimit.HasValue ? Math.Min(stepIndex, paths.Count - 1) : paths.Count - 1;
                var startIndex = historyStepLimit.HasValue
                    ? Math.Max(0, endIndex - historyStepLimit.Value + 1)
                    : 0;
                for (var index = endIndex; index >= startIndex; index--)
                    yield return index;
                yield break;
            }

            if (stepIndex >= transforms.Count || transforms[stepIndex].IsNull())
            {
                if (stepIndex < paths.Count)
                    yield return stepIndex;
                yield break;
            }

            var lowerBound = historyStepLimit.HasValue
                ? Math.Max(0, stepIndex - historyStepLimit.Value + 1)
                : LastNullTransformAtOrBefore(stepIndex) + 1;
            if (historyStepLimit.HasValue)
            {
                for (var index = stepIndex; index >= lowerBound; index--)
                    if (transforms[index].IsNull())
                    {
                        lowerBound = index + 1;
                        break;
                    }
            }
            for (var index = Math.Min(stepIndex, paths.Count - 1); index >= lowerBound; index--)
                yield return index;
        }

        private int LastNullTransformAtOrBefore(int stepIndex)
        {
            var inspectionEnd = Math.Min(stepIndex, transforms.Count - 1);
            for (var index = transformsInspectedThrough + 1; index <= inspectionEnd; index++)
                if (transforms[index].IsNull())
                    nullTransformIndices.Add(index);
            transformsInspectedThrough = Math.Max(transformsInspectedThrough, inspectionEnd);
            var location = nullTransformIndices.BinarySearch(stepIndex);
            if (location >= 0)
                return nullTransformIndices[location];
            location = ~location;
            return location == 0 ? -1 : nullTransformIndices[location - 1];
        }

        private static T ValueAt<T>(IReadOnlyList<T> values, int index, T defaultValue)
            => values.Count == 0 ? defaultValue : values[Math.Min(index, values.Count - 1)];

        public void Dispose()
        {
            foreach (var geometry in geometryCache.Values)
                geometry.Dispose();
            geometryCache.Clear();
        }
    }

    internal sealed class SteppedGeometryGroup
    {
        private readonly IReadOnlyList<GeometryModel3D> geometrySteps;
        private readonly IReadOnlyList<TVGL.Matrix4x4> transforms;
        private readonly Func<TVGL.Matrix4x4, Media3D.Transform3D> createTransform;
        private readonly List<int> nullTransformIndices = [];
        private int transformsInspectedThrough = -1;

        internal SteppedGeometryGroup(IReadOnlyList<GeometryModel3D> geometrySteps,
            IReadOnlyList<TVGL.Matrix4x4> transforms,
            Func<TVGL.Matrix4x4, Media3D.Transform3D> createTransform)
        {
            this.geometrySteps = geometrySteps ?? [];
            this.transforms = transforms;
            this.createTransform = createTransform;
        }

        internal int Count => Math.Max(geometrySteps.Count, transforms?.Count ?? 0);

        internal IEnumerable<GeometryModel3D> Resolve(int stepIndex)
        {
            if (transforms is null)
            {
                foreach (var geometry in geometrySteps)
                    if (geometry is not null)
                        yield return geometry;
                yield break;
            }

            if (stepIndex < 0)
                yield break;
            if (stepIndex >= transforms.Count || transforms[stepIndex].IsNull())
            {
                if (stepIndex < geometrySteps.Count && geometrySteps[stepIndex] is { } selectedGeometry)
                {
                    selectedGeometry.Transform = null;
                    yield return selectedGeometry;
                }
                yield break;
            }

            var lowerBound = LastNullTransformAtOrBefore(stepIndex) + 1;
            var transform = createTransform(transforms[stepIndex]);
            for (var index = Math.Min(stepIndex, geometrySteps.Count - 1); index >= lowerBound; index--)
            {
                if (geometrySteps[index] is not { } geometry)
                    continue;
                geometry.Transform = transform;
                yield return geometry;
            }
        }

        private int LastNullTransformAtOrBefore(int stepIndex)
        {
            var inspectionEnd = Math.Min(stepIndex, transforms.Count - 1);
            for (var index = transformsInspectedThrough + 1; index <= inspectionEnd; index++)
                if (transforms[index].IsNull())
                    nullTransformIndices.Add(index);
            transformsInspectedThrough = Math.Max(transformsInspectedThrough, inspectionEnd);
            var location = nullTransformIndices.BinarySearch(stepIndex);
            if (location >= 0)
                return nullTransformIndices[location];
            location = ~location;
            return location == 0 ? -1 : nullTransformIndices[location - 1];
        }
    }
}
