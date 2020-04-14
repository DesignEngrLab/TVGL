using System;
using System.Collections.Generic;
using System.Windows.Media;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{

    internal static class SolidsMethods
    {
        private static void Run()
        {
            #region TessellatedSolid
            var ts = new TessellatedSolid();
            ts = new TessellatedSolid(new[] { new List<Vector3>() }, new TVGL.Color[0]);
            ts = new TessellatedSolid(new Vector3[0], new[] { new[] { 1, 2, 3 } }, new TVGL.Color[0]);
            ts = new TessellatedSolid(new[] { new PolygonalFace() });
            ts.AddPrimitive(new Flat());
            ts.CheckModelIntegrity();
            ts.ClassifyPrimitiveSurfaces();
            ts.Complexify();
            ts.Copy();
            ts.CreateSilhouette(Vector3.UnitX);
            ts.Repair();
            ts.SetToOriginAndSquare(out var backTransform);
            ts.SetToOriginAndSquareToNewSolid(out backTransform);
            ts.Simplify();
            ts.SimplifyFlatPatches();
            ts.Transform(new Matrix4x4());
            ts.TransformToNewSolid(new Matrix4x4());


            #endregion

            #region CrossSectionSolid
            var cs = new CrossSectionSolid(new Dictionary<int, double>());
            //cs.Add(new List<Vertex>)
            #endregion

            #region VoxelizedSolid
            var vs = new VoxelizedSolid(ts, 0.1);

            #endregion
        }
    }
}