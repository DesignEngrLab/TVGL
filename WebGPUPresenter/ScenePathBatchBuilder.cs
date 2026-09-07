using BugViewer;
using NumericsVector3 = System.Numerics.Vector3;

namespace WebGPUPresenter;

/// <summary>
/// Converts independently addressable presenter paths into a bounded number of WebGPU line buffers.
/// Zero-thickness intervals keep unrelated paths disconnected inside each buffer.
/// </summary>
internal static class ScenePathBatchBuilder
{
    internal const int DefaultMaxSegmentsPerBatch = 32_768;

    internal static IReadOnlyList<LineData> Build(
        SceneRequest scene,
        int maxSegmentsPerBatch = DefaultMaxSegmentsPerBatch)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return Build(scene.RequestId, scene.Paths, maxSegmentsPerBatch);
    }

    internal static IReadOnlyList<LineData> Build(
        Guid requestId,
        IEnumerable<ScenePath> paths,
        int maxSegmentsPerBatch = DefaultMaxSegmentsPerBatch)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (maxSegmentsPerBatch <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSegmentsPerBatch));

        var result = new List<LineData>();
        var vertices = new List<NumericsVector3>();
        var thicknesses = new List<float>();
        var colors = new List<ColorRgba>();
        var fadeFactors = new List<float>();
        var renderedSegmentCount = 0;

        foreach (var path in paths)
        {
            if (path.Vertices.Count < 2)
                continue;
            if (!double.IsFinite(path.Thickness))
                throw new InvalidOperationException($"Path '{path.Id}' has a non-finite thickness.");

            var thickness = path.Thickness < 0
                ? LineData.AutomaticThickness
                : (float)path.Thickness;
            if (!float.IsFinite(thickness))
                throw new InvalidOperationException($"Path '{path.Id}' has a thickness outside the supported range.");

            var color = path.Color;
            var start = ToVector(path, 0);
            var continuingPathInBatch = false;

            for (var vertexIndex = 1; vertexIndex < path.Vertices.Count; vertexIndex++)
            {
                if (renderedSegmentCount == maxSegmentsPerBatch)
                {
                    Flush();
                    continuingPathInBatch = false;
                }

                if (vertices.Count == 0)
                    vertices.Add(start);
                else if (!continuingPathInBatch)
                {
                    vertices.Add(start);
                    thicknesses.Add(0f);
                    colors.Add(color);
                    fadeFactors.Add(0f);
                }

                var end = ToVector(path, vertexIndex);
                vertices.Add(end);
                thicknesses.Add(thickness);
                colors.Add(color);
                fadeFactors.Add(0f);
                renderedSegmentCount++;
                continuingPathInBatch = true;
                start = end;
            }
        }

        Flush();
        return result;

        void Flush()
        {
            if (renderedSegmentCount == 0)
                return;

            result.Add(new LineData
            {
                Id = $"scene-path-batch-{requestId:N}-{result.Count}",
                Vertices = vertices,
                Thicknesses = thicknesses,
                Colors = colors,
                FadeFactors = fadeFactors
            });
            vertices = [];
            thicknesses = [];
            colors = [];
            fadeFactors = [];
            renderedSegmentCount = 0;
        }
    }

    private static NumericsVector3 ToVector(ScenePath path, int vertexIndex)
    {
        var vertex = path.Vertices[vertexIndex];
        if (vertex.Length < 3)
            throw new InvalidOperationException(
                $"Path '{path.Id}' vertex {vertexIndex} does not contain three coordinates.");
        if (!float.IsFinite(vertex[0]) || !float.IsFinite(vertex[1]) || !float.IsFinite(vertex[2]))
            throw new InvalidOperationException(
                $"Path '{path.Id}' vertex {vertexIndex} contains a non-finite coordinate.");

        return new NumericsVector3(vertex[0], vertex[1], vertex[2]);
    }
}
