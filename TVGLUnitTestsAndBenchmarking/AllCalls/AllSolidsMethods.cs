using System;
using System.Collections.Generic;
using System.Windows.Media;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{

    internal static class AllSolidsMethods
    {
        private static void Run()
        {
            #region TessellatedSolid
            var ts = new TessellatedSolid();
            ts = new TessellatedSolid(new[] { new List<Vector3>() }, true, new TVGL.Color[0]);
            ts = new TessellatedSolid(new Vector3[0], new[] { new[] { 1, 2, 3 } }, true, new TVGL.Color[0]);
            ts.AddPrimitive(new Flat());
            ts.CheckModelIntegrity();
            ts.ClassifyPrimitiveSurfaces();
            ts.Complexify();
            ts.Copy();
            ts.OrientedBoundingBox();
            ts.CreateSilhouette(Vector3.UnitX);
            ts.Repair();
            ts.SetToOriginAndSquare(out var backTransform);
            ts.SetToOriginAndSquareToNewSolid(out backTransform);
            ts.Simplify();
            ts.SimplifyFlatPatches();
            ts.Transform(new Matrix4x4());
            ts.TransformToNewSolid(new Matrix4x4());
            ts.SliceOnInfiniteFlat(new Flat(), out List<TessellatedSolid> solids, out ContactData contactData);
            ts.SliceOnFlatAsSingleSolids(new Flat(), out TessellatedSolid positiveSideSolids, out TessellatedSolid negativeSideSolid);
            ts.GetSliceContactData(new Flat(), out contactData, false);
            ts.ConvexHull.Vertices.MinimumBoundingCylinder();
            ts.ConvexHull.Vertices.OrientedBoundingBox();
            var length = ts.ConvexHull.Vertices.GetLengthAndExtremeVertices(Vector3.UnitX, out List<IVertex3D> bottomVertices,
                  out List<IVertex3D> topVertices);
            length = ts.ConvexHull.Vertices.GetLengthAndExtremeVertex(Vector3.UnitX, out Vertex bottomVertex,
                  out Vertex topVertex);

            #endregion

            #region CrossSectionSolid
            var cs = new CrossSectionSolid(new Dictionary<int, double>());
            //cs.Add(new List<Vertex>)
            #endregion

            #region VoxelizedSolid
            var vs1 = new VoxelizedSolid(ts, 0.1);
            vs1.ConvertToTessellatedSolidMarchingCubes(5);
            vs1.ConvertToTessellatedSolidRectilinear();
            var vs2 = (VoxelizedSolid)vs1.Copy();
            vs1.DirectionalErodeToConstraintToNewSolid(in vs2, CartesianDirections.XNegative);
            vs1.Draft(CartesianDirections.XNegative);
            var vs3 = vs1.DraftToNewSolid(CartesianDirections.XNegative);

            #endregion
        }
    }
}