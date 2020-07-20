using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGLPresenterDebugger
{
    internal static class Program
    {


        static readonly Stopwatch stopwatch = new Stopwatch();
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        [STAThread]
        private static void Main(string[] args)
        {

            DirectoryInfo dir;
            if (Directory.Exists("../../../../TestFiles"))
            {
                //x64
                dir = new DirectoryInfo("../../../../TestFiles");
            }
            else
            {
                //x86
                dir = new DirectoryInfo("../../../TestFiles");
            }
            var fileNames = dir.GetFiles("Pump10*").OrderBy(x => r.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                //Presenter.ShowAndHang(solid);
                //var vs = new VoxelizedSolid(solid, 100);
                //Presenter.ShowAndHang(vs);

                //vs.Draft(CartesianDirections.XNegative);
                //Presenter.ShowAndHang(vs);
                //solid.SliceOnInfiniteFlat(new Flat(solid.Center,
                //    new Vector3(random.NextDouble(), random.NextDouble(), random.NextDouble()).Normalize()), out var solids, out _);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                solid.SolidColor = new Color(100, 200, 100, 50);
                //Presenter.ShowAndHang(solid);
                //var cs = CrossSectionSolid.CreateFromTessellatedSolid(solid, CartesianDirections.ZPositive, 100);
                //Presenter.ShowAndHang(cs);
                //var ts = cs.ConvertToTessellatedExtrusions(true, false);
                //Presenter.ShowAndHang(ts);




                var silhouette = solid.CreateSilhouette(new Vector3(1, 1, 1));
                Presenter.ShowAndHang(silhouette);
            }
        }

    }
}
