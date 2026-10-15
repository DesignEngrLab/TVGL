using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector4 = System.Numerics.Vector4;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace TVGL.GLTFImportExport
{
    internal static class GLTFWriter
    {
        public static bool Save(string filePath, Solid solid)
        {
            try
            {
                var model = CreateModel(solid);
                return SaveToFile(model, filePath);
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(Stream stream, Solid solid, bool asBinary = true)
        {
            try
            {
                var model = CreateModel(solid);
                return SaveToStream(model, stream, asBinary);
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(string filePath, IEnumerable<Solid> solids)
        {
            try
            {
                var model = CreateModel(solids);
                return SaveToFile(model, filePath);
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(Stream stream, IEnumerable<Solid> solids, bool asBinary = true)
        {
            try
            {
                var model = CreateModel(solids);
                return SaveToStream(model, stream, asBinary);
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(string filePath, SolidAssembly solidAssembly)
        {
            try
            {
                var model = CreateModel(solidAssembly);
                return SaveToFile(model, filePath);
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(Stream stream, SolidAssembly solidAssembly, bool asBinary = true)
        {
            try
            {
                var model = CreateModel(solidAssembly);
                return SaveToStream(model, stream, asBinary);
            }
            catch
            {
                return false;
            }
        }

        private static ModelRoot CreateModel(Solid solid)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene(0);

            var ts = EnsureTessellatedSolid(solid);
            var mesh = AddSolidMesh(model, ts);
            var node = scene.CreateNode(string.IsNullOrWhiteSpace(mesh.Name) ? "Solid" : mesh.Name);
            node.Mesh = mesh;

            return model;
        }

        private static ModelRoot CreateModel(IEnumerable<Solid> solids)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene(0);

            int index = 0;
            foreach (var solid in solids)
            {
                var ts = EnsureTessellatedSolid(solid);
                var mesh = AddSolidMesh(model, ts);
                var node = scene.CreateNode(string.IsNullOrWhiteSpace(mesh.Name) ? $"Solid_{++index}" : mesh.Name);
                node.Mesh = mesh;
            }

            return model;
        }

        private static ModelRoot CreateModel(SolidAssembly solidAssembly)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene(0);

            if (solidAssembly.Solids != null && solidAssembly.Solids.Length > 0)
            {
                var meshMap = new Dictionary<Solid, Mesh>();
                foreach (var solid in solidAssembly.Solids)
                {
                    var ts = EnsureTessellatedSolid(solid);
                    var mesh = AddSolidMesh(model, ts);
                    meshMap[solid] = mesh;
                }

                if (solidAssembly.RootAssembly != null)
                {
                    ExportSubAssembly(scene, null, solidAssembly.RootAssembly, solidAssembly.Solids, meshMap);
                }
            }
            else if (solidAssembly.RootAssembly != null)
            {
                int partIdx = 0;
                foreach (var (part, backTransform) in solidAssembly.RootAssembly.AllPartsInGlobalCoordinateSystem)
                {
                    var ts = EnsureTessellatedSolid(part);
                    var mesh = AddSolidMesh(model, ts);
                    var node = scene.CreateNode(string.IsNullOrWhiteSpace(mesh.Name) ? $"Part_{++partIdx}" : mesh.Name);
                    node.Mesh = mesh;
                    node.LocalMatrix = backTransform.ToNumerics();
                }
            }

            return model;
        }

        private static void ExportSubAssembly(
            Scene scene,
            Node? parentNode,
            SubAssembly subAssembly,
            Solid[] distinctSolids,
            Dictionary<Solid, Mesh> meshMap)
        {
            foreach (var (partIndex, backTransform) in subAssembly.Solids)
            {
                if (partIndex >= 0 && partIndex < distinctSolids.Length)
                {
                    var solid = distinctSolids[partIndex];
                    if (meshMap.TryGetValue(solid, out var mesh))
                    {
                        var partNode = parentNode != null
                            ? parentNode.CreateNode(string.IsNullOrWhiteSpace(solid.Name) ? "Part" : solid.Name)
                            : scene.CreateNode(string.IsNullOrWhiteSpace(solid.Name) ? "Part" : solid.Name);

                        partNode.Mesh = mesh;
                        partNode.LocalMatrix = backTransform.ToNumerics();
                    }
                }
            }

            foreach (var (childAssembly, backTransform) in subAssembly.SubAssemblies)
            {
                var subNode = parentNode != null
                    ? parentNode.CreateNode(string.IsNullOrWhiteSpace(childAssembly.Name) ? "SubAssembly" : childAssembly.Name)
                    : scene.CreateNode(string.IsNullOrWhiteSpace(childAssembly.Name) ? "SubAssembly" : childAssembly.Name);

                subNode.LocalMatrix = backTransform.ToNumerics();
                ExportSubAssembly(scene, subNode, childAssembly, distinctSolids, meshMap);
            }
        }

        private static Mesh AddSolidMesh(ModelRoot model, TessellatedSolid solid)
        {
            var mesh = model.CreateMesh(string.IsNullOrWhiteSpace(solid.Name) ? "Solid" : solid.Name);

            var positions = new NumericsVector3[solid.NumberOfVertices];
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                positions[i] = solid.Vertices[i].Coordinates.ToNumerics();
            }

            var posView = model.CreateBufferView(12 * positions.Length, 0, BufferMode.ARRAY_BUFFER);
            new Vector3Array(posView.Content).Fill(positions);
            var posAccessor = model.CreateAccessor();
            posAccessor.SetVertexData(posView, 0, positions.Length, new AttributeFormat(DimensionType.VEC3, EncodingType.FLOAT, false));
            posAccessor.UpdateBounds();

            var normals = new NumericsVector3[solid.NumberOfVertices];
            bool hasValidNormals = false;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var n = solid.Vertices[i].Normal;
                if (!n.IsNull() && n.LengthSquared() > 1e-12)
                {
                    var norm = n.Normalize();
                    normals[i] = new NumericsVector3((float)norm.X, (float)norm.Y, (float)norm.Z);
                    hasValidNormals = true;
                }
                else
                {
                    normals[i] = NumericsVector3.UnitZ;
                }
            }

            Accessor? normAccessor = null;
            if (hasValidNormals)
            {
                var normView = model.CreateBufferView(12 * normals.Length, 0, BufferMode.ARRAY_BUFFER);
                new Vector3Array(normView.Content).Fill(normals);
                normAccessor = model.CreateAccessor();
                normAccessor.SetVertexData(normView, 0, normals.Length, new AttributeFormat(DimensionType.VEC3, EncodingType.FLOAT, false));
                normAccessor.UpdateBounds();
            }

            bool isUniform = solid.HasUniformColor ||
                solid.Faces.All(f => f.Color == null || f.Color.Equals(solid.SolidColor));

            if (isUniform)
            {
                var mat = model.CreateMaterial($"{mesh.Name}_Material");
                mat.InitializePBRMetallicRoughness();
                var col = solid.SolidColor ?? new TVGL.Color(KnownColors.LightGray);
                var baseColorChannel = mat.FindChannel("BaseColor");
                if (baseColorChannel.HasValue)
                {
                    var ch = baseColorChannel.Value;
                    ch.Color = col.ToNumericsVector4();
                }

                var prim = mesh.CreatePrimitive();
                prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;
                prim.Material = mat;
                prim.SetVertexAccessor("POSITION", posAccessor);
                if (normAccessor != null)
                    prim.SetVertexAccessor("NORMAL", normAccessor);

                var indices = new int[solid.NumberOfFaces * 3];
                for (int i = 0; i < solid.NumberOfFaces; i++)
                {
                    var f = solid.Faces[i];
                    indices[i * 3] = f.A.IndexInList;
                    indices[i * 3 + 1] = f.B.IndexInList;
                    indices[i * 3 + 2] = f.C.IndexInList;
                }
                SetIndexAccessor(model, prim, indices, solid.NumberOfVertices);
            }
            else
            {
                var faceGroups = solid.Faces
                    .GroupBy(f => f.Color ?? solid.SolidColor ?? new TVGL.Color(KnownColors.LightGray))
                    .ToList();

                int groupIdx = 0;
                foreach (var group in faceGroups)
                {
                    var color = group.Key;
                    var mat = model.CreateMaterial($"{mesh.Name}_Material_{groupIdx++}");
                    mat.InitializePBRMetallicRoughness();
                    var baseColorChannel = mat.FindChannel("BaseColor");
                    if (baseColorChannel.HasValue)
                    {
                        var ch = baseColorChannel.Value;
                        ch.Color = color.ToNumericsVector4();
                    }

                    var prim = mesh.CreatePrimitive();
                    prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;
                    prim.Material = mat;
                    prim.SetVertexAccessor("POSITION", posAccessor);
                    if (normAccessor != null)
                        prim.SetVertexAccessor("NORMAL", normAccessor);

                    var groupFaces = group.ToList();
                    var indices = new int[groupFaces.Count * 3];
                    for (int i = 0; i < groupFaces.Count; i++)
                    {
                        var f = groupFaces[i];
                        indices[i * 3] = f.A.IndexInList;
                        indices[i * 3 + 1] = f.B.IndexInList;
                        indices[i * 3 + 2] = f.C.IndexInList;
                    }
                    SetIndexAccessor(model, prim, indices, solid.NumberOfVertices);
                }
            }

            return mesh;
        }

        private static void SetIndexAccessor(ModelRoot model, MeshPrimitive prim, int[] indices, int maxVertexCount)
        {
            if (indices.Length == 0) return;

            bool useShort = maxVertexCount <= ushort.MaxValue;
            var encoding = useShort ? IndexEncodingType.UNSIGNED_SHORT : IndexEncodingType.UNSIGNED_INT;
            int bytesPerIndex = useShort ? 2 : 4;

            var idxView = model.CreateBufferView(bytesPerIndex * indices.Length, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var idxArray = new IntegerArray(idxView.Content, encoding);
            idxArray.Fill(indices);

            var idxAccessor = model.CreateAccessor();
            idxAccessor.SetIndexData(idxView, 0, indices.Length, encoding);
            prim.SetIndexAccessor(idxAccessor);
        }

        private static TessellatedSolid EnsureTessellatedSolid(Solid solid)
        {
            if (solid is TessellatedSolid ts)
                return ts;
            if (solid is CrossSectionSolid css)
                return css.ConvertToTessellatedSolidMarchingCubes();
            if (solid is VoxelizedSolid vs)
                return vs.ConvertToTessellatedSolidRectilinear();

            throw new ArgumentException($"Solid of type '{solid.GetType().Name}' cannot be converted to a TessellatedSolid.", nameof(solid));
        }

        private static bool SaveToFile(ModelRoot model, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            model.Save(filePath);
            return true;
        }

        private static bool SaveToStream(ModelRoot model, Stream stream, bool asBinary)
        {
            model.WriteGLB(stream);
            return true;
        }
    }
}

