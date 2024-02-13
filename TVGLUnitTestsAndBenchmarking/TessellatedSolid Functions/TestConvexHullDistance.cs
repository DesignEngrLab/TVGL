using System;
using TVGL;
using System.Linq;
using System.Diagnostics;


namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class ConvexHull
    {
        public static void Test1()
        {

            var sphere1 = new Sphere(new Vector3(0, 0, 0), 6, true);
            sphere1.Tessellate(6);
            foreach (var f in sphere1.Faces)
                f.Color = new Color(KnownColors.MediumTurquoise);
            var testSphere = new TessellatedSolid(sphere1.Faces, buildOptions: new TessellatedSolidBuildOptions
            {
                CopyElementsPassedToConstructor = true,
                DefineConvexHull = false
            });
            //Presenter.ShowAndHang(sphere1.Faces);
            ConvexHull3D.Create(testSphere);
            var cvxHull1 = testSphere.ConvexHull;
            foreach (var f in cvxHull1.Faces)
                f.Color = new Color(100, 100, 0, 0);

            Presenter.ShowAndHang(cvxHull1.Faces.Concat(sphere1.Faces));
            var linesegment = new[] { new Vertex(10, 0, 0), new Vertex(10, 10, 0) };
            var d = cvxHull1.Vertices.DistanceBetween(linesegment, out var v);
            Console.WriteLine("Distance apart = " + d + "and the vector is " + v);
        }
        public static void Test2(TessellatedSolid ts)
        {
            var sw = Stopwatch.StartNew();
            ConvexHull3D.Create(ts);
            sw.Stop();
            ts.ResetDefaultColor();
            foreach (var f in ts.ConvexHull.Faces)
                f.Color = new Color(100, 0, 100, 100);
            Console.WriteLine("num vertices in solid = " + ts.Vertices.Length.ToString() + ", Convex Hull Time, " + sw.ElapsedTicks.ToString() + ", ");
            Presenter.ShowAndHang(ts.Faces.Concat(ts.ConvexHull.Faces));
        }
        public static void Test3(TessellatedSolid ts)
        {
            var sw = Stopwatch.StartNew();
            var rand = new Random();
            var slicePlane = new Plane(ts.Center + new Vector3(0, 0, 0),SphericalAnglePair.ConvertSphericalToCartesian(1, rand.NextDouble()*Math.PI, 
                2*Math.PI*rand.NextDouble()-Math.PI));
            var xSections = ts.GetCrossSection(slicePlane, out _);
            if (xSections.Count == 0) return;
            //Presenter.ShowAndHang(xSections.Select(p => p.Vertices.Select(v => v.Coordinates)));

            var cvxHull =   xSections.CreateConvexHull(out _);
            //Presenter.ShowAndHang((new[] { cvxHull.Select(v=>v.Coordinates)}).Concat(xSections.Select(p=>p.Vertices.Select(v=>v.Coordinates))));

        }
    }

}
