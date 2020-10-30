using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Xml;
using System.Windows.Documents;
using TVGL;
using TVGL.IOFunctions;
using TVGL.TwoDimensional;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class SimplifyComplexify
    {
        const int NumLayers = 10;
        const int NumPointsPerLayer = 100;
        internal static void Run(string[] args)
        {
            // 1. bubble up from the bin directories to find the TestFiles directory
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "TestFiles"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "TestFiles");

            // 2. get the file path
            var fileNames = dir.GetFiles("*");
            foreach (var fileName in fileNames)
            {
                Console.WriteLine("Attempting: " + fileName);

                // 3. open the file into TessellatedSolid, named "solid"
                IO.Open(fileName.FullName, out TessellatedSolid solid);

                // 4. show the loaded solid
                Presenter.ShowAndHang(solid);
                solid.SolidColor = new Color(100, 200, 200, 200);

                var machineLearningSignature = GetCrossSections(solid, NumLayers, NumPointsPerLayer);
                Console.WriteLine("machine learning data =");
                foreach (var num in machineLearningSignature)
                    Console.WriteLine(num);
                Console.Write("\n\n\n");
            }
        }

        private static List<double> GetCrossSections(TessellatedSolid solid, int numLayers,
            int numPointsPerLayer)
        {
            solid.SetToOriginAndSquare(out _);
            var orthogonalCrossSectionsSolids = new[]
            {
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.XPositive, numLayers),
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.YPositive, numLayers),
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.ZPositive, numLayers)
            };

            var allLayers = orthogonalCrossSectionsSolids.SelectMany(css => css.GetCrossSectionsAs3DLoops());
            Presenter.ShowVertexPaths(allLayers);
            var signature = new List<double>();
            foreach (var crossSectionSolid in orthogonalCrossSectionsSolids)
            {
                foreach (var layer in crossSectionSolid.Layer2D.Values)
                {
                    var newPolygons =
                    (layer.Sum(poly => poly.Vertices.Count) > numPointsPerLayer)
                      ? layer.Simplify(numPointsPerLayer)
                       : layer.ComplexifyToNewPolygons(numPointsPerLayer);
                    signature.AddRange(newPolygons.SelectMany(p => p.Path.ConvertTo1DDoublesCollection()));
                }
            }
            return signature;
        }
    }
}