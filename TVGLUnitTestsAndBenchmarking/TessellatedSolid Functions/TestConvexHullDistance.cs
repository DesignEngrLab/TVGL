using System;
#if !PRESENT
#endif
using TVGL;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using MIConvexHull;
using HelixToolkit.SharpDX.Core;


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
            var testSphere = new TessellatedSolid(sphere1.Faces,buildOptions: new TessellatedSolidBuildOptions
            {
                CopyElementsPassedToConstructor=true,
                DefineConvexHull=false
            });
            //Presenter.ShowAndHang(sphere1.Faces);
            ConvexHull3D.Create(testSphere, out var cvxHull1);
            foreach (var f in cvxHull1.Faces)
                f.Color = new Color(100,100,0,0);

            Presenter.ShowAndHang(cvxHull1.Faces.Concat(sphere1.Faces));
            var sphere2 = new Sphere(new Vector3(10, 0, 0), 6, true);
            sphere2.Tessellate();
            ConvexHull3D.Create(sphere2.Vertices.ToList(), out var cvxHull2, false);
            var linesegment = new[] { new Vertex(10, 0, 0), new Vertex(10, 10, 0) };
            var d = cvxHull1.Vertices.DistanceBetween(linesegment, out var v);
        }
        public static void Test2(TessellatedSolid ts)
        {
            var sw = Stopwatch.StartNew();
            ConvexHull3D.Create(ts, out var convexHull1);

            Console.Write(ts.Vertices.Length.ToString()+", NEW Convex Hull Time, " + sw.ElapsedTicks.ToString()+", ");
            sw = Stopwatch.StartNew();
            var convexHull2 = MakeMIConvexHull(ts.Vertices, ts.SameTolerance);
            Console.Write(sw.ElapsedTicks.ToString() + ",");

            Console.Write("New, " + convexHull1.Faces.Count + ", " + convexHull1.Vertices.Count + ", " + convexHull1.Volume + ",");
            Console.WriteLine("MIC, " + convexHull2.Faces.Count + ", " + convexHull2.Vertices.Count + ", " + convexHull2.Volume);
        }

        public static ConvexHull3D MakeMIConvexHull(IList<Vertex> vertices, double tolerance)
        {

            var convexHull =MIConvexHull.ConvexHull.Create<Vertex>(vertices, tolerance);
            var Vertices = convexHull.Result.Points;
            var faces = convexHull.Result.Faces;
            var cvxHull = new ConvexHull3D { tolerance = tolerance };
            cvxHull.Vertices.AddRange(Vertices);
            cvxHull.Faces.AddRange(faces.Select(f=>new ConvexHullFace(f.Vertices[0], f.Vertices[1], f.Vertices[2])));

            return cvxHull;
        }

    }
}