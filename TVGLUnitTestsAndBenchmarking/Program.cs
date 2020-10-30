using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Windows.Documents;
using TVGL;
using TVGL.Numerics;
using TVGL.IOFunctions;
using TVGL.TwoDimensional;
using Priority_Queue;


namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        static Random r = new Random();
        const int NumLayers = 20;
        const int NumPointInSignature = 5000;

        [STAThread]
        private static void Main(string[] args)
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

                var machineLearningSignature = ProcessAsCrossSections(solid, NumLayers, NumPointInSignature);
                Console.WriteLine("machine learning data = " + String.Join(',', machineLearningSignature));

                Console.Write("\n\n\n");
            }
        }

        private static double[] ProcessAsCrossSections(TessellatedSolid solid, int numLayers,
            int numPointInSignature)
        {
            // first square the part to the origin. This is a simple and quick way to avoid issues 
            // in models that are simple translations/rotations of others
            solid.SetToOriginAndSquare(out _);
            // create 3 CrossSectionSolids from the input solid. These are about x, y, and z
            var orthogonalCrossSectionsSolids = new[]
            {
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.XPositive, numLayers),
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.YPositive, numLayers),
            CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.ZPositive, numLayers)
            };
            // allLayers simply collects the 3D loops from the 3 cross-section solids
            var allLayers = orthogonalCrossSectionsSolids.SelectMany(css => css.GetCrossSectionsAs3DLoops());
            Presenter.ShowVertexPaths(allLayers);
            // now all these layers which can have multiple loops. Flatten them to a one long path
            var oneLongPath = allLayers.SelectMany(layer => layer).SelectMany(loop => loop).ToList();
            Presenter.ShowVertexPaths(oneLongPath);
            //upon viewing this "oneLongPath" I wonder if it would be good to reset to {0,0} after each loop
            //to provide some info to the machine learning about the start of a new loop. If so, would need
            //to change that SelectMany, SelectMany Linq kung-fu into some foreach loops

            // now simplify the long path to have the same number of points as all the rest. This is to provide
            // a signature that is all of one length. This is much easier input for machine learning.
            var numToRemove = oneLongPath.Count - numPointInSignature;
            if (numToRemove < 0)
            {
                for (int i = 0; i < -numToRemove; i++)
                    oneLongPath.Add(new Vector3(0, 0, 0));
            }
            if (numToRemove > 0)
                oneLongPath = Simplify3DPath(oneLongPath, numPointInSignature);
            Presenter.ShowVertexPaths(oneLongPath);

            // finally flatten the coordinates to a long string of numbers and return as array
            return oneLongPath.ConvertTo1DDoublesCollection().ToArray();
        }

        /// <summary>
        /// Simplifies the 3d path. This is a modification of TVGL's Polygon Simplify functions
        /// "Polygon Operations\PolygonOperations.Simplify.cs"
        /// </summary>
        /// <param name="connectedPath">The connected path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;Vector3&gt;.</returns>
        private static List<Vector3> Simplify3DPath(IEnumerable<Vector3> connectedPath, int numToRemove)
        {
            // this is based on the Polygon Simplify function
            var path = connectedPath.ToArray();

            #region build initial list of cross products
            var cornerQueue = new SimplePriorityQueue<int, double>();
            var crossProductsArray = new double[path.Length];
            var prevPoint = path[^2];
            for (int i = 0, j = path.Length - 1; i < path.Length; j = i++) // this is clever but cryptic, basically j
            {                                                            // is always one behind i and starts at the last point
                var currentPoint = path[j];
                var nextPoint = path[i];
                var cross = (currentPoint - prevPoint).Cross(nextPoint - currentPoint).LengthSquared();
                crossProductsArray[j] = cross;
                cornerQueue.Enqueue(j, cross);
                prevPoint = currentPoint;
            }

            #endregion
            #region main loop
            while (numToRemove-- > 0)
            {
                var index = cornerQueue.Dequeue();
                path[index] = Vector3.Null;
                // find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                // (nextnextIndex and prevprevIndex)
                int nextIndex = FindValidNeighborIndex(index, true, path);
                int nextnextIndex = FindValidNeighborIndex(nextIndex, true, path);
                int prevIndex = FindValidNeighborIndex(index, false, path);
                int prevprevIndex = FindValidNeighborIndex(prevIndex, false, path);

                // like the AddCrossProductToQueue function used above, we need a global index from stringing together all the polygons.
                // So, polygonStartIndex is used to find the start of this particular polygon's index and then add prevIndex and nextIndex to it.
                var newCross = ((path[prevIndex] - path[prevprevIndex]).Cross(path[nextIndex] - path[prevIndex])).LengthSquared();
                crossProductsArray[prevIndex] = newCross;
                cornerQueue.UpdatePriority(prevIndex, newCross);

                newCross = ((path[nextIndex] - path[prevIndex]).Cross(path[nextnextIndex] - path[nextIndex])).LengthSquared();
                crossProductsArray[nextIndex] = newCross;
                cornerQueue.UpdatePriority(nextIndex, newCross);
            }
            #endregion
            // now return the coordinates that have not been "turned off" set to Null
            var result = new List<Vector3>();
            foreach (var corner in path)
                if (!corner.IsNull()) result.Add(corner);
            return result;
        }

        private static int FindValidNeighborIndex(int index, bool forward, Vector3[] path)
        {
            int increment = forward ? 1 : -1;
            var hitLimit = false;
            do
            {
                index += increment;
                if (index < 0)
                {
                    index = path.Length - 1;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
                else if (index == path.Length)
                {
                    index = 0;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
            }
            while (path[index].IsNull());
            return index;
        }
    }
}