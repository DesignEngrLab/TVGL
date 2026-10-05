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
    internal static class GLTFReconstructor
    {
        public static ModelRoot LoadModel(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("GLTF file not found.", filePath);

            return ModelRoot.Load(filePath);
        }

        public static ModelRoot LoadModel(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            return ModelRoot.ReadGLB(stream);
        }

        public static List<TessellatedSolid> ReconstructSolids(
            ModelRoot model,
            string fileName = "",
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var result = new List<TessellatedSolid>();
            if (model.LogicalMeshes.Count == 0)
                return result;

            var scene = model.DefaultScene ?? model.LogicalScenes.FirstOrDefault();
            var nodes = scene != null
                ? FlattenVisualNodes(scene.VisualChildren).Where(n => n.Mesh != null).ToList()
                : new List<Node>();

            if (nodes.Count > 0)
            {
                int nodeIndex = 0;
                foreach (var node in nodes)
                {
                    string solidName = !string.IsNullOrWhiteSpace(node.Name)
                        ? node.Name
                        : (!string.IsNullOrWhiteSpace(node.Mesh.Name) ? node.Mesh.Name : $"Solid_{nodeIndex + 1}");

                    var solid = ConvertMeshToSolid(
                        node.Mesh,
                        node.WorldMatrix,
                        applyTransform: true,
                        solidName: solidName,
                        fileName: fileName,
                        buildOptions: buildOptions);

                    if (solid != null)
                        result.Add(solid);

                    nodeIndex++;
                }
            }
            else
            {
                int meshIndex = 0;
                foreach (var mesh in model.LogicalMeshes)
                {
                    string solidName = !string.IsNullOrWhiteSpace(mesh.Name)
                        ? mesh.Name
                        : $"Solid_{meshIndex + 1}";

                    var solid = ConvertMeshToSolid(
                        mesh,
                        NumericsMatrix4x4.Identity,
                        applyTransform: false,
                        solidName: solidName,
                        fileName: fileName,
                        buildOptions: buildOptions);

                    if (solid != null)
                        result.Add(solid);

                    meshIndex++;
                }
            }

            return result;
        }

        public static SolidAssembly ReconstructSolidAssembly(
            ModelRoot model,
            string fileName = "",
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var assemblyName = !string.IsNullOrWhiteSpace(fileName)
                ? Path.GetFileName(fileName)
                : "GLTFAssembly";

            var assembly = new SolidAssembly(assemblyName);
            if (model.LogicalMeshes.Count == 0)
                return assembly;

            var meshToSolid = new Dictionary<Mesh, TessellatedSolid>();
            int meshIndex = 0;
            foreach (var mesh in model.LogicalMeshes)
            {
                string solidName = !string.IsNullOrWhiteSpace(mesh.Name)
                    ? mesh.Name
                    : $"Part_{meshIndex + 1}";

                var solid = ConvertMeshToSolid(
                    mesh,
                    NumericsMatrix4x4.Identity,
                    applyTransform: false,
                    solidName: solidName,
                    fileName: fileName,
                    buildOptions: buildOptions);

                if (solid != null)
                    meshToSolid[mesh] = solid;

                meshIndex++;
            }

            if (meshToSolid.Count == 0)
                return assembly;

            var scene = model.DefaultScene ?? model.LogicalScenes.FirstOrDefault();
            var nodes = scene != null
                ? FlattenVisualNodes(scene.VisualChildren).Where(n => n.Mesh != null).ToList()
                : new List<Node>();

            if (nodes.Count > 0)
            {
                foreach (var node in nodes)
                {
                    if (node.Mesh != null && meshToSolid.TryGetValue(node.Mesh, out var solid))
                    {
                        assembly.RootAssembly.Add(solid, node.WorldMatrix.ToTVGL());
                    }
                }
            }
            else
            {
                foreach (var solid in meshToSolid.Values)
                {
                    assembly.RootAssembly.Add(solid, TVGL.Matrix4x4.Identity);
                }
            }

            assembly.CompleteInitialization();
            return assembly;
        }

        private static TessellatedSolid? ConvertMeshToSolid(
            Mesh mesh,
            NumericsMatrix4x4 worldTransform,
            bool applyTransform,
            string solidName,
            string fileName,
            TessellatedSolidBuildOptions? buildOptions)
        {
            var allVertices = new List<TVGL.Vector3>();
            var allFaceIndices = new List<(int, int, int)>();
            var faceColors = new List<TVGL.Color>();

            bool flipWinding = applyTransform && worldTransform.GetDeterminant() < 0;

            // Check if all primitives share the same POSITION accessor
            var distinctPosAccessors = mesh.Primitives
                .Select(p => p.GetVertexAccessor("POSITION"))
                .Where(p => p != null && p.Count > 0)
                .Distinct()
                .ToList();

            if (distinctPosAccessors.Count == 1)
            {
                var posAccessor = distinctPosAccessors[0];
                var positions = posAccessor.AsVector3Array();

                for (int i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    if (applyTransform && !worldTransform.IsIdentity)
                        p = NumericsVector3.Transform(p, worldTransform);

                    allVertices.Add(new TVGL.Vector3(p.X, p.Y, p.Z));
                }

                foreach (var prim in mesh.Primitives)
                {
                    var triangles = prim.GetTriangleIndices().ToList();
                    if (triangles.Count == 0)
                        continue;

                    var primColor = GetPrimitiveColor(prim);
                    var colAccessor = prim.GetVertexAccessor("COLOR_0");
                    var vertexColors = colAccessor?.AsColorArray();

                    foreach (var (a, b, c) in triangles)
                    {
                        if (a < 0 || a >= positions.Count ||
                            b < 0 || b >= positions.Count ||
                            c < 0 || c >= positions.Count)
                            continue;

                        if (flipWinding)
                            allFaceIndices.Add((a, c, b));
                        else
                            allFaceIndices.Add((a, b, c));

                        if (vertexColors != null && vertexColors.Count > Math.Max(a, Math.Max(b, c)))
                        {
                            var ca = vertexColors[a];
                            var cb = vertexColors[b];
                            var cc = vertexColors[c];
                            faceColors.Add(new TVGL.Color(
                                (ca.X + cb.X + cc.X) / 3f,
                                (ca.Y + cb.Y + cc.Y) / 3f,
                                (ca.Z + cb.Z + cc.Z) / 3f,
                                (ca.W + cb.W + cc.W) / 3f));
                        }
                        else
                        {
                            faceColors.Add(primColor);
                        }
                    }
                }
            }
            else
            {
                // Primitives have separate position accessors
                foreach (var prim in mesh.Primitives)
                {
                    var posAccessor = prim.GetVertexAccessor("POSITION");
                    if (posAccessor == null || posAccessor.Count == 0)
                        continue;

                    var triangles = prim.GetTriangleIndices().ToList();
                    if (triangles.Count == 0)
                        continue;

                    var positions = posAccessor.AsVector3Array();
                    int vertexOffset = allVertices.Count;

                    for (int i = 0; i < positions.Count; i++)
                    {
                        var p = positions[i];
                        if (applyTransform && !worldTransform.IsIdentity)
                            p = NumericsVector3.Transform(p, worldTransform);

                        allVertices.Add(new TVGL.Vector3(p.X, p.Y, p.Z));
                    }

                    var primColor = GetPrimitiveColor(prim);
                    var colAccessor = prim.GetVertexAccessor("COLOR_0");
                    var vertexColors = colAccessor?.AsColorArray();

                    foreach (var (a, b, c) in triangles)
                    {
                        if (a < 0 || a >= positions.Count ||
                            b < 0 || b >= positions.Count ||
                            c < 0 || c >= positions.Count)
                            continue;

                        if (flipWinding)
                            allFaceIndices.Add((a + vertexOffset, c + vertexOffset, b + vertexOffset));
                        else
                            allFaceIndices.Add((a + vertexOffset, b + vertexOffset, c + vertexOffset));

                        if (vertexColors != null && vertexColors.Count > Math.Max(a, Math.Max(b, c)))
                        {
                            var ca = vertexColors[a];
                            var cb = vertexColors[b];
                            var cc = vertexColors[c];
                            faceColors.Add(new TVGL.Color(
                                (ca.X + cb.X + cc.X) / 3f,
                                (ca.Y + cb.Y + cc.Y) / 3f,
                                (ca.Z + cb.Z + cc.Z) / 3f,
                                (ca.W + cb.W + cc.W) / 3f));
                        }
                        else
                        {
                            faceColors.Add(primColor);
                        }
                    }
                }
            }

            if (allVertices.Count < 3 || allFaceIndices.Count == 0)
                return null;

            IList<TVGL.Color> colors;
            if (faceColors.Count > 0 && faceColors.All(c => c.Equals(faceColors[0])))
                colors = new[] { faceColors[0] };
            else
                colors = faceColors;

            return new TessellatedSolid(
                allVertices,
                allVertices.Count,
                allFaceIndices,
                allFaceIndices.Count,
                colors,
                buildOptions,
                name: solidName,
                filename: fileName);
        }

        private static TVGL.Color GetPrimitiveColor(MeshPrimitive primitive)
        {
            if (primitive.Material != null)
            {
                var baseColorChannel = primitive.Material.FindChannel("BaseColor")
                                    ?? primitive.Material.FindChannel("Diffuse");
                if (baseColorChannel.HasValue)
                {
                    var col = baseColorChannel.Value.Color;
                    return new TVGL.Color(col.X, col.Y, col.Z, col.W);
                }

                var emissiveChannel = primitive.Material.FindChannel("Emissive");
                if (emissiveChannel.HasValue && emissiveChannel.Value.Color.LengthSquared() > 0f)
                {
                    var col = emissiveChannel.Value.Color;
                    return new TVGL.Color(col.X, col.Y, col.Z, 1f);
                }
            }

            return new TVGL.Color(KnownColors.LightGray);
        }

        private static IEnumerable<Node> FlattenVisualNodes(IEnumerable<Node> roots)
        {
            var visited = new HashSet<Node>();
            var queue = new Queue<Node>(roots);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                    continue;

                yield return current;

                foreach (var child in current.VisualChildren)
                    queue.Enqueue(child);
            }
        }
    }
}
