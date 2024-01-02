using System.Collections.Generic;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{

    internal static class AllSolidsMethods
    {
        private static void Run()
        {
            #region TessellatedSolid
            var ts = new TessellatedSolid();
            ts = new TessellatedSolid(new Vector3[0], new[] { (1, 2, 3) }, new TVGL.Color[0], TessellatedSolidBuildOptions.Default);
            ts.AddPrimitive(new Plane());
            
            ts.Complexify();
            ts.Copy();
            ts.OrientedBoundingBox();
            ts.CreateSilhouette(Vector3.UnitX);
            ts.SetToOriginAndSquare(out var backTransform);
            ts.SetToOriginAndSquareToNewSolid(out backTransform);
            ts.Simplify();
            ts.SimplifyFlatPatches();
            ts.Transform(new Matrix4x4());
            ts.TransformToNewSolid(new Matrix4x4());
            ts.SliceOnInfiniteFlat(new Plane(), out List<TessellatedSolid> solids, out ContactData contactData);
            ts.SliceOnFlatAsSingleSolids(new Plane(), out TessellatedSolid positiveSideSolids, out TessellatedSolid negativeSideSolid);
            ts.GetSliceContactData(new Plane(), out contactData, false);
            ts.ConvexHull.Vertices.MinimumBoundingCylinder();
            ts.ConvexHull.Vertices.OrientedBoundingBox();
            var length = ts.ConvexHull.Vertices.GetLengthAndExtremeVertices(Vector3.UnitX, out List<IPoint3D> bottomVertices,
                  out List<IPoint3D> topVertices);
            length = ts.ConvexHull.Vertices.GetLengthAndExtremeVertex(Vector3.UnitX, out Vertex bottomVertex,
                  out Vertex topVertex);

            #endregion

            #region CrossSectionSolid
            var cs = new CrossSectionSolid(new Dictionary<int, double>());
            //cs.Add(new List<Vertex>)
            #endregion

            #region VoxelizedSolid
            var vs1 = VoxelizedSolid.CreateFrom(ts, 0.1);
            vs1.ConvertToTessellatedSolidMarchingCubes(5);
            vs1.ConvertToTessellatedSolidRectilinear();
            vs1.Draft(CartesianDirections.XNegative);
            var vs3 = vs1.DraftToNewSolid(CartesianDirections.XNegative);

            #endregion
        }
    }
}