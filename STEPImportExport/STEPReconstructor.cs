using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.STEPImportExport
{
    internal static class STEPReconstructor
    {
        public static TessellatedSolid[] ReconstructSolids(
            IntPtr handle,
            string fileName,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var result = new List<TessellatedSolid>();
            if (handle == IntPtr.Zero)
                return Array.Empty<TessellatedSolid>();

            int bodyCount = NativeMethods.StepModel_GetBodyCount(handle);
            for (int b = 0; b < bodyCount; b++)
            {
                if (!NativeMethods.StepModel_GetBodyMetadata(handle, b, out var meta))
                    continue;

                if (meta.VertexCount < 3 || meta.TriangleCount < 1)
                    continue;

                var verticesRaw = new double[meta.VertexCount * 3];
                var trianglesRaw = new int[meta.TriangleCount * 3];
                var facesRaw = new FacePrimitiveData[meta.FaceCount];

                if (!NativeMethods.StepModel_CopyBodyData(handle, b, verticesRaw, trianglesRaw, facesRaw))
                    continue;

                var vertices = new Vector3[meta.VertexCount];
                for (int i = 0; i < meta.VertexCount; i++)
                {
                    int idx = i * 3;
                    vertices[i] = new Vector3(verticesRaw[idx], verticesRaw[idx + 1], verticesRaw[idx + 2]);
                }

                var faceVertexIndices = new (int, int, int)[meta.TriangleCount];
                for (int i = 0; i < meta.TriangleCount; i++)
                {
                    int idx = i * 3;
                    faceVertexIndices[i] = (trianglesRaw[idx], trianglesRaw[idx + 1], trianglesRaw[idx + 2]);
                }

                var defaultColor = new Color(KnownColors.LightGray);
                var faceColors = new Color[meta.TriangleCount];
                Array.Fill(faceColors, defaultColor);

                for (int f = 0; f < meta.FaceCount; f++)
                {
                    ref var faceData = ref facesRaw[f];
                    var col = new Color(faceData.ColorR, faceData.ColorG, faceData.ColorB, faceData.ColorA);
                    int end = Math.Min(faceData.TriangleStartIndex + faceData.TriangleCount, meta.TriangleCount);
                    for (int t = faceData.TriangleStartIndex; t < end; t++)
                    {
                        faceColors[t] = col;
                    }
                }

                string solidName = string.IsNullOrEmpty(meta.Name) ? $"Body_{b + 1}" : meta.Name;
                var solid = new TessellatedSolid(
                    vertices,
                    meta.VertexCount,
                    faceVertexIndices,
                    meta.TriangleCount,
                    faceColors,
                    buildOptions,
                    name: solidName,
                    filename: fileName
                )
                {
                    SourceIsSheetBody = meta.IsSheetBody != 0
                };

                ReconstructPrimitives(solid, facesRaw, meta.FaceCount);
                result.Add(solid);
            }

            return result.ToArray();
        }

        private static void ReconstructPrimitives(
            TessellatedSolid solid,
            FacePrimitiveData[] facesRaw,
            int faceCount)
        {
            if (solid.Faces == null || solid.NumberOfFaces == 0)
                return;

            for (int f = 0; f < faceCount; f++)
            {
                ref var faceData = ref facesRaw[f];
                int start = faceData.TriangleStartIndex;
                int count = faceData.TriangleCount;
                if (start < 0 || start >= solid.NumberOfFaces || count <= 0)
                    continue;

                int end = Math.Min(start + count, solid.NumberOfFaces);
                var faceSlice = new List<TriangleFace>(end - start);
                for (int t = start; t < end; t++)
                {
                    faceSlice.Add(solid.Faces[t]);
                }

                if (faceSlice.Count == 0)
                    continue;

                PrimitiveSurface? primitive = null;
                var primType = (StepPrimitiveType)faceData.Type;

                switch (primType)
                {
                    case StepPrimitiveType.Plane:
                    {
                        var pointOnPlane = new Vector3(faceData.OriginX, faceData.OriginY, faceData.OriginZ);
                        var normal = new Vector3(faceData.AxisX, faceData.AxisY, faceData.AxisZ);
                        if (!normal.IsNegligible())
                            normal = normal.Normalize();
                        else
                            normal = Vector3.UnitZ;

                        var plane = new Plane(pointOnPlane, normal);
                        plane.SetFacesAndVertices(faceSlice);
                        primitive = plane;
                        break;
                    }

                    case StepPrimitiveType.Cylinder:
                    {
                        var axis = new Vector3(faceData.AxisX, faceData.AxisY, faceData.AxisZ);
                        if (!axis.IsNegligible())
                            axis = axis.Normalize();
                        else
                            axis = Vector3.UnitZ;

                        var anchor = new Vector3(faceData.OriginX, faceData.OriginY, faceData.OriginZ);
                        var cylinder = new Cylinder(axis, anchor, faceData.Radius, faceSlice)
                        {
                            IsPositive = faceData.IsPositive != 0
                        };
                        primitive = cylinder;
                        break;
                    }

                    case StepPrimitiveType.Cone:
                    {
                        var apex = new Vector3(faceData.OriginX, faceData.OriginY, faceData.OriginZ);
                        var axis = new Vector3(faceData.AxisX, faceData.AxisY, faceData.AxisZ);
                        if (!axis.IsNegligible())
                            axis = axis.Normalize();
                        else
                            axis = Vector3.UnitZ;

                        var cone = new Cone(apex, axis, faceData.Aperture, faceSlice)
                        {
                            IsPositive = faceData.IsPositive != 0
                        };
                        primitive = cone;
                        break;
                    }

                    case StepPrimitiveType.Sphere:
                    {
                        var center = new Vector3(faceData.OriginX, faceData.OriginY, faceData.OriginZ);
                        var sphere = new Sphere(center, faceData.Radius, faceData.IsPositive != 0, faceSlice);
                        primitive = sphere;
                        break;
                    }

                    case StepPrimitiveType.Torus:
                    {
                        var center = new Vector3(faceData.OriginX, faceData.OriginY, faceData.OriginZ);
                        var axis = new Vector3(faceData.AxisX, faceData.AxisY, faceData.AxisZ);
                        if (!axis.IsNegligible())
                            axis = axis.Normalize();
                        else
                            axis = Vector3.UnitZ;

                        var torus = new Torus(center, axis, faceData.Radius, faceData.MinorRadius, faceSlice)
                        {
                            IsPositive = faceData.IsPositive != 0
                        };
                        primitive = torus;
                        break;
                    }

                    case StepPrimitiveType.Unknown:
                    default:
                    {
                        var unknown = new UnknownRegion(faceSlice)
                        {
                            ImportedSurfaceType = string.IsNullOrEmpty(faceData.SurfaceTypeName)
                                ? "Unknown"
                                : faceData.SurfaceTypeName
                        };
                        primitive = unknown;
                        break;
                    }
                }

                if (primitive != null)
                {
                    primitive.OriginalColor = new Color(
                        faceData.ColorR,
                        faceData.ColorG,
                        faceData.ColorB,
                        faceData.ColorA
                    );
                    solid.AddPrimitive(primitive);
                }
            }

            if (solid.Primitives != null)
            {
                solid.NumberOfPrimitives = solid.Primitives.Count;
            }
        }
    }
}

