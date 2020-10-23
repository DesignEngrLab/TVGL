using System;
using System.IO;
using TVGL;
using TVGL.IOFunctions;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // 1. bubble up from the bin directories to find the TestFiles directory
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "TestFiles"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "TestFiles");

            // 2. get the file path
            var fileName = dir.FullName + Path.DirectorySeparatorChar + "TieFighter.STL";
            Console.WriteLine("Attempting: " + fileName);

            // 3. open the file into TessellatedSolid, named "solid"
            IO.Open(fileName, out TessellatedSolid solid);

            // 4. show the loaded solid
            Presenter.ShowAndHang(solid);

            // 5. perform some analysis on the loaded solid
            var firstFace = solid.Faces[0];
            var furthestFace = FindFurthestFace(solid, firstFace);
            
            // 6. show the result
            // change the solid to a transparent color and indicate the two faces by green and red
            solid.SolidColor = new Color(100, 200, 200, 200);
            firstFace.Color = new Color(TVGL.KnownColors.Green);
            furthestFace.Color = new Color(TVGL.KnownColors.Red);
            solid.HasUniformColor = false; //need to set this to false now that two faces are different
            Presenter.ShowAndHang(solid);
        }

        private static PolygonalFace FindFurthestFace(TessellatedSolid solid, PolygonalFace firstFace)
        {
            /*********YOUR CODE HERE! ***********/
            /*********YOUR CODE HERE! ***********/
            /*********YOUR CODE HERE! ***********/
            /*********YOUR CODE HERE! ***********/

            return solid.Faces[10]; // must return a face. The 100th face is a stand-in for the real answer
        }
    }
}
