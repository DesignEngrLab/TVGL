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
                Presenter.ShowAndHang(solid);
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

                var direction = new Vector3(0.4878742024072471, -0.5642634200314975, -0.6660221883990427);
                var silhouette = solid.CreateSilhouette(direction);
                Presenter.ShowAndHang(silhouette);
                direction = new Vector3(-56.18410927997162, -22.861805242887613, 14.32601181526016);
                silhouette = solid.CreateSilhouette(direction);
                Presenter.ShowAndHang(silhouette);
                direction = new Vector3(84.27030862507888, 72.49305023462188, -74.07118919029422);
                silhouette = solid.CreateSilhouette(direction);
                Presenter.ShowAndHang(silhouette);
                direction = new Vector3(74.38205758779407, -61.35750848863158, 94.06754653624608);
                silhouette = solid.CreateSilhouette(direction);
                Presenter.ShowAndHang(silhouette);
                direction = new Vector3(10.153487841670156, -2.140804241476957, 65.03817390885118);
                silhouette = solid.CreateSilhouette(direction);
                Presenter.ShowAndHang(silhouette);

                for (int j = 0; j < 33; j++)
                {
                    direction = new Vector3(r100, r100, r100);
                    silhouette = solid.CreateSilhouette(direction);
                    Presenter.ShowAndHang(silhouette);
                }
            }
        }

    }
}
