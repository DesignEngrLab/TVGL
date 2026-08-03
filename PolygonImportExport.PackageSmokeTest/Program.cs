using PolygonImportExport;

var testDirectory = Path.Combine(
    Path.GetTempPath(),
    "PolygonImportExport-PackageSmokeTest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testDirectory);

try
{
    var inputPath = Path.Combine(testDirectory, "input.svg");
    var outputPath = Path.Combine(testDirectory, "output.svg");
    await File.WriteAllTextAsync(
        inputPath,
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 10">
          <polygon points="0,0 20,0 20,10 0,10" />
        </svg>
        """);

    var polygons = SVG.Open(inputPath, positiveYIsUp: false);
    if (polygons.Count != 1 || !polygons[0].IsClosed || polygons[0].Path.Count != 4)
    {
        throw new InvalidOperationException(
            $"Unexpected SVG import result: {polygons.Count} polygons; " +
            $"closed={polygons.FirstOrDefault()?.IsClosed}; " +
            $"vertices={polygons.FirstOrDefault()?.Path.Count}.");
    }

    if (!SVG.Save(outputPath, polygons, positiveYIsUp: false))
        throw new InvalidOperationException("SVG export failed.");

    var reopened = SVG.Open(outputPath, positiveYIsUp: false);
    if (reopened.Count != 1 || reopened[0].Path.Count != 4)
        throw new InvalidOperationException("The exported SVG did not round-trip correctly.");

    Console.WriteLine("TVGL.PolygonImportExport package smoke test passed.");
}
finally
{
    Directory.Delete(testDirectory, recursive: true);
}
