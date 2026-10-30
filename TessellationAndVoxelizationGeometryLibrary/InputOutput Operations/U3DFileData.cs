// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 10-21-2026
//
// Clean-room implementation based on ECMA-363 and Adobe's published
// "U3D Supported Elements" format documentation. No Adobe runtime code is
// used or required.
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TVGL
{
    /// <summary>
    /// Reads Universal 3D files. The first supported geometry representation is
    /// the Right Hemisphere Adobe compressed mesh extension used by Acrobat.
    /// </summary>
    internal sealed class U3DFileData : IO
    {
        private const uint FileHeaderBlock = 0x00443355;
        private const uint ModifierChainBlock = 0xFFFFFF14;
        private const uint NewObjectTypeBlock = 0xFFFFFF16;
        private const uint GroupNodeBlock = 0xFFFFFF21;
        private const uint ModelNodeBlock = 0xFFFFFF22;
        private const string RightHemisphereTypeName = "RHAdobeMeshResource";
        private const int MaxSceneDepth = 64;
        private const int MaxPlacements = 1_000_000;

        internal static TessellatedSolid[] OpenSolids(Stream stream, string filename,
            TessellatedSolidBuildOptions buildOptions)
        {
            buildOptions ??= TessellatedSolidBuildOptions.Default;
            var reader = new U3DReader(stream);
            var blocks = reader.ReadTopLevelBlocks();
            if (blocks.Count == 0 || blocks[0].Type != FileHeaderBlock)
                throw new InvalidDataException("The stream does not begin with a U3D file header block.");

            var (unitType, coordinateScale) = ReadUnits(reader, blocks[0]);
            var chains = blocks.Where(block => block.Type == ModifierChainBlock)
                .Select(reader.ReadModifierChain)
                .ToArray();
            uint? rhAdobeBlockType = null;

            foreach (var chain in chains)
            {
                foreach (var child in chain.Blocks)
                {
                    if (child.Type == NewObjectTypeBlock &&
                        TryReadRightHemisphereDeclaration(reader, child, out var declaredType))
                        rhAdobeBlockType = declaredType;
                }
            }

            if (!rhAdobeBlockType.HasValue)
                throw new NotSupportedException(
                    "This U3D file does not declare the RHAdobeMeshResource extension. " +
                    "Standard CLOD mesh decoding has not yet been implemented.");

            var resources = new Dictionary<string, List<RHAdobeMesh>>(StringComparer.Ordinal);
            var failures = new List<string>();
            foreach (var chain in chains)
            {
                foreach (var block in chain.Blocks.Where(block => block.Type == rhAdobeBlockType.Value))
                {
                    try
                    {
                        var mesh = reader.ReadRightHemisphereMesh(block, coordinateScale);
                        if (mesh.Positions.Count == 0 || mesh.Faces.Count == 0) continue;
                        if (!resources.TryGetValue(chain.Name, out var meshes))
                            resources[chain.Name] = meshes = [];
                        meshes.Add(mesh);
                    }
                    catch (Exception exception) when (exception is InvalidDataException ||
                                                      exception is NotSupportedException ||
                                                      exception is EndOfStreamException)
                    {
                        failures.Add($"{chain.Name}: {exception.Message}");
                    }
                }
            }

            var sceneNodes = new Dictionary<string, U3DSceneNode>(StringComparer.Ordinal);
            var sceneNodeBlocksFound = false;
            foreach (var chain in chains)
            foreach (var block in chain.Blocks.Where(block =>
                         block.Type is GroupNodeBlock or ModelNodeBlock))
            {
                sceneNodeBlocksFound = true;
                try
                {
                    var node = reader.ReadSceneNode(block, block.Type == ModelNodeBlock, coordinateScale);
                    if (!sceneNodes.TryAdd(node.Name, node))
                        failures.Add($"Duplicate U3D scene node '{node.Name}' was ignored.");
                }
                catch (Exception exception) when (exception is InvalidDataException ||
                                                  exception is EndOfStreamException ||
                                                  exception is OverflowException)
                {
                    failures.Add($"{chain.Name}: could not read scene node: {exception.Message}");
                }
            }

            var solids = new List<TessellatedSolid>();
            var placementCount = 0;
            if (sceneNodeBlocksFound)
            {
                var placementCache = new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.Ordinal);
                foreach (var node in sceneNodes.Values.Where(node => node.ResourceName != null && node.Visibility != 0))
                {
                    if (!resources.TryGetValue(node.ResourceName!, out var meshes))
                    {
                        failures.Add($"{node.Name}: model resource '{node.ResourceName}' was not found.");
                        continue;
                    }
                    var placements = ResolvePlacements(node, sceneNodes, placementCache, [], 0, failures);
                    foreach (var placement in placements)
                    foreach (var mesh in meshes)
                    {
                        if (++placementCount > MaxPlacements)
                            throw new InvalidDataException($"The U3D scene contains more than {MaxPlacements} mesh placements.");
                        AddSolid(mesh, node.Name, placement);
                    }
                }
            }
            else
            {
                foreach (var (resourceName, meshes) in resources)
                foreach (var mesh in meshes)
                    AddSolid(mesh, resourceName, Matrix4x4.Identity);
            }

            if (solids.Count == 0)
            {
                var detail = failures.Count == 0 ? "No mesh resources were found." : string.Join("; ", failures.Take(5));
                throw new InvalidDataException("Unable to decode any RHAdobe mesh resources. " + detail);
            }

            if (failures.Count > 0)
                Log.Information($"U3D import decoded {solids.Count} mesh resources and skipped " +
                                $"{failures.Count}: {string.Join("; ", failures.Take(3))}");
            return solids.ToArray();

            void AddSolid(RHAdobeMesh mesh, string instanceName, Matrix4x4 transform)
            {
                var positions = mesh.Positions.Select(position => position.Transform(transform)).ToList();
                var mirrored = transform.GetDeterminant() < 0;
                var faces = mirrored
                    ? mesh.Faces.Select(face => (face.Item1, face.Item3, face.Item2)).ToList()
                    : mesh.Faces.ToList();
                var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? instanceName : mesh.Name;
                var solidName = string.Equals(meshName, instanceName, StringComparison.Ordinal)
                    ? meshName
                    : $"{instanceName} - {meshName}";
                solids.Add(new TessellatedSolid(positions, faces, null, buildOptions,
                    unitType, solidName, filename));
            }
        }

        private static IReadOnlyList<Matrix4x4> ResolvePlacements(
            U3DSceneNode node,
            IReadOnlyDictionary<string, U3DSceneNode> nodes,
            IDictionary<string, IReadOnlyList<Matrix4x4>> cache,
            HashSet<string> path,
            int depth,
            ICollection<string> failures)
        {
            if (cache.TryGetValue(node.Name, out var cached)) return cached;
            if (depth > MaxSceneDepth || !path.Add(node.Name))
            {
                failures.Add($"{node.Name}: U3D scene hierarchy is cyclic or deeper than {MaxSceneDepth}.");
                return [];
            }

            var result = new List<Matrix4x4>();
            if (node.Parents.Count == 0)
                result.Add(Matrix4x4.Identity);
            foreach (var parent in node.Parents)
            {
                if (string.IsNullOrEmpty(parent.Name))
                {
                    result.Add(parent.Transform);
                    continue;
                }
                if (!nodes.TryGetValue(parent.Name, out var parentNode))
                {
                    failures.Add($"{node.Name}: parent node '{parent.Name}' was not found; its local transform was used.");
                    result.Add(parent.Transform);
                    continue;
                }
                foreach (var parentPlacement in ResolvePlacements(
                             parentNode, nodes, cache, path, depth + 1, failures))
                    result.Add(parent.Transform * parentPlacement);
            }
            path.Remove(node.Name);
            cache[node.Name] = result;
            return result;
        }

        private static (UnitType UnitType, double CoordinateScale) ReadUnits(U3DReader reader, U3DBlock header)
        {
            using var data = reader.OpenData(header);
            data.ReadUInt16(); // major version
            data.ReadUInt16(); // minor version
            var profile = data.ReadUInt32();
            data.ReadUInt32(); // declaration size
            data.ReadUInt64(); // file size
            data.ReadUInt32(); // character encoding

            if ((profile & 0x8) == 0 || data.Remaining < sizeof(double))
                return (UnitType.unspecified, 1.0);

            var metersPerUnit = data.ReadDouble();
            if (NearlyEqual(metersPerUnit, 0.001)) return (UnitType.millimeter, 1.0);
            if (NearlyEqual(metersPerUnit, 0.000001)) return (UnitType.micron, 1.0);
            if (NearlyEqual(metersPerUnit, 0.01)) return (UnitType.centimeter, 1.0);
            if (NearlyEqual(metersPerUnit, 0.0254)) return (UnitType.inch, 1.0);
            if (NearlyEqual(metersPerUnit, 0.3048)) return (UnitType.foot, 1.0);
            if (NearlyEqual(metersPerUnit, 1.0)) return (UnitType.meter, 1.0);

            // Preserve the physical size for unusual scale factors by converting coordinates to meters.
            return (UnitType.meter, metersPerUnit);
        }

        private static bool NearlyEqual(double first, double second)
            => Math.Abs(first - second) <= Math.Max(Math.Abs(second) * 1e-9, 1e-12);

        private static bool TryReadRightHemisphereDeclaration(U3DReader reader, U3DBlock block,
            out uint declaredType)
        {
            declaredType = 0;
            using var data = reader.OpenData(block);
            if (data.ReadString() != RightHemisphereTypeName)
                return false;
            data.ReadUInt32(); // modifier type
            var extensionId = data.ReadBytes(16);
            byte[] expectedId = { 0xA6, 0x04, 0xA8, 0x96, 0xB9, 0x3F, 0xC5, 0x43,
                                  0xB2, 0xDF, 0x2A, 0x31, 0xB5, 0x56, 0x93, 0x40 };
            if (!extensionId.SequenceEqual(expectedId))
                return false;
            declaredType = data.ReadUInt32();
            return true;
        }
    }

    internal readonly record struct U3DBlock(uint Type, long DataOffset, uint DataSize,
        long MetadataOffset, uint MetadataSize);

    internal sealed class U3DModifierChain
    {
        internal string Name { get; init; }
        internal uint Type { get; init; }
        internal IReadOnlyList<U3DBlock> Blocks { get; init; }
    }

    internal sealed class U3DSceneNode
    {
        internal string Name { get; init; }
        internal List<U3DParentNode> Parents { get; } = [];
        internal string ResourceName { get; init; }
        internal uint Visibility { get; init; } = 3;
    }

    internal readonly record struct U3DParentNode(string Name, Matrix4x4 Transform);

    internal sealed class RHAdobeMesh
    {
        internal string Name { get; init; }
        internal List<Vector3> Positions { get; init; }
        internal List<(int, int, int)> Faces { get; init; }
    }

    internal sealed class RHAdobeMaterial
    {
        internal uint TextureLayerCount { get; init; }
        internal bool HasDiffuseColors { get; init; }
        internal bool HasSpecularColors { get; init; }
    }

    internal sealed class U3DReader
    {
        private readonly Stream stream;

        internal U3DReader(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("U3D import requires a readable, seekable stream.", nameof(stream));
            this.stream = stream;
        }

        internal List<U3DBlock> ReadTopLevelBlocks()
        {
            var blocks = new List<U3DBlock>();
            stream.Position = 0;
            while (stream.Position < stream.Length)
            {
                if (stream.Length - stream.Position < 12)
                    throw new InvalidDataException("The U3D file ends inside a block header.");
                var block = ReadBlockHeader(stream.Position, stream.Length);
                blocks.Add(block);
                stream.Position = Align4(block.MetadataOffset + block.MetadataSize);
            }
            return blocks;
        }

        internal U3DModifierChain ReadModifierChain(U3DBlock block)
        {
            using var data = OpenData(block);
            var name = data.ReadString();
            var chainType = data.ReadUInt32();
            var attributes = data.ReadUInt32();
            if ((attributes & 1) != 0) data.Skip(16); // bounding sphere
            if ((attributes & 2) != 0) data.Skip(24); // axis-aligned bounding box
            data.Align4(block.DataOffset);
            var count = data.ReadUInt32();
            var children = new List<U3DBlock>(checked((int)count));
            for (var i = 0U; i < count; i++)
            {
                var child = ReadBlockHeader(data.AbsolutePosition, block.DataOffset + block.DataSize);
                children.Add(child);
                data.AbsolutePosition = Align4(child.MetadataOffset + child.MetadataSize);
            }
            return new U3DModifierChain { Name = name, Type = chainType, Blocks = children };
        }

        internal RHAdobeMesh ReadRightHemisphereMesh(U3DBlock block, double coordinateScale)
        {
            using var data = OpenData(block);
            var name = data.ReadString();
            var chunkFlags = data.ReadByte();
            if ((chunkFlags & 0x0F) != 0)
                throw new NotSupportedException($"RHAdobe mesh version {chunkFlags & 0x0F} is not supported.");

            if ((chunkFlags & 0x10) != 0)
            {
                data.ReadUInt32(); // mesh description size
                var subchunkCount = data.ReadUInt16();
                data.Skip(checked(subchunkCount * 6));
            }

            var materialCountEncoding = chunkFlags >> 6;
            var materialCount = materialCountEncoding == 0
                ? 1U
                : data.ReadVariableUInt(materialCountEncoding);
            var materials = new List<RHAdobeMaterial>(checked((int)materialCount));
            for (var i = 0U; i < materialCount; i++)
            {
                var flags = data.ReadByte();
                var textureLayers = data.ReadVariableUInt(((flags >> 4) & 3) + 1);
                data.ReadVariableUInt(((flags >> 2) & 3) + 1); // original shading ID
                var additionalLayerDimensionBytes = textureLayers <= 1 ? 0 : (textureLayers - 1 + 3) / 4;
                data.Skip(checked((int)additionalLayerDimensionBytes));
                materials.Add(new RHAdobeMaterial
                {
                    TextureLayerCount = textureLayers,
                    HasDiffuseColors = (flags & 0x40) != 0,
                    HasSpecularColors = (flags & 0x80) != 0
                });
            }

            var valueFlags = data.ReadByte();
            var valueSize = (valueFlags >> 6) + 1;
            uint ReadCount(int bit) => (valueFlags & (1 << bit)) == 0 ? 0 : data.ReadVariableUInt(valueSize);
            var faceCount = ReadCount(0);
            var positionCount = ReadCount(1);
            var normalCount = ReadCount(2);
            var diffuseColorCount = ReadCount(3);
            var specularColorCount = ReadCount(4);
            var textureCoordinateCount = ReadCount(5);

            Vector3[] positions;
            try
            {
                positions = data.ReadFloatVectors(positionCount, 3, false);
            }
            catch (Exception exception) when (exception is InvalidDataException ||
                                              exception is NotSupportedException ||
                                              exception is EndOfStreamException)
            {
                throw new InvalidDataException(
                    $"Could not decode positions near byte 0x{data.AbsolutePosition:X}: {exception.Message}",
                    exception);
            }
            if (coordinateScale != 1.0)
                for (var i = 0; i < positions.Length; i++) positions[i] *= coordinateScale;

            var normalsStart = data.AbsolutePosition;
            if (normalCount > 0)
            {
                try
                {
                    data.ReadFloatVectors(normalCount, 3, true);
                }
                catch (Exception exception) when (exception is InvalidDataException ||
                                                  exception is NotSupportedException ||
                                                  exception is EndOfStreamException)
                {
                    throw new InvalidDataException(
                        $"Could not decode normals near byte 0x{data.AbsolutePosition:X}: {exception.Message}",
                        exception);
                }
            }
            var normalsEnd = data.AbsolutePosition;
            try
            {
                if (diffuseColorCount > 0) data.ReadFloatVectors(diffuseColorCount, 4, false);
                if (specularColorCount > 0) data.ReadFloatVectors(specularColorCount, 4, false);
                if (textureCoordinateCount > 0) data.ReadFloatVectors(textureCoordinateCount, 4, false);
            }
            catch (Exception exception) when (exception is InvalidDataException ||
                                              exception is NotSupportedException ||
                                              exception is EndOfStreamException)
            {
                throw new InvalidDataException(
                    $"Could not decode vertex attributes near byte 0x{data.AbsolutePosition:X} " +
                    $"(normals 0x{normalsStart:X}-0x{normalsEnd:X}): {exception.Message}",
                    exception);
            }

            var indexDataStart = data.AbsolutePosition;
            // This exporter omits the redundant face-material stream when there
            // is only one declared material.
            var faceMaterials = materials.Count == 1
                ? new uint[faceCount]
                : data.ReadEncodedUIntArray(faceCount);
            var indices = data.ReadEncodedUIntArray(checked(faceCount * 3), 3, positionCount);
            if (normalCount > 0)
                data.ReadEncodedUIntArray(checked(faceCount * 3), 3, normalCount);

            var diffuseIndexCount = 0U;
            var specularIndexCount = 0U;
            var textureIndexCount = 0U;
            for (var i = 0; i < faceMaterials.Length; i++)
            {
                if (faceMaterials[i] >= materials.Count)
                    throw new InvalidDataException(
                        $"A face references material {faceMaterials[i]} outside the {materials.Count}-item " +
                        $"material array (index data starts at 0x{indexDataStart:X}).");
                var material = materials[(int)faceMaterials[i]];
                if (material.HasDiffuseColors) diffuseIndexCount += 3;
                if (material.HasSpecularColors) specularIndexCount += 3;
                textureIndexCount = checked(textureIndexCount + material.TextureLayerCount * 3);
            }
            if (diffuseIndexCount > 0) data.ReadEncodedUIntArray(diffuseIndexCount, 3);
            if (specularIndexCount > 0) data.ReadEncodedUIntArray(specularIndexCount, 3);
            if (textureIndexCount > 0) data.ReadEncodedUIntArray(textureIndexCount, 3);

            if ((chunkFlags & 0x20) != 0)
                throw new NotSupportedException("RHAdobe skeleton descriptions are not supported yet.");

            var faces = new List<(int, int, int)>(checked((int)faceCount));
            for (var i = 0; i < indices.Length; i += 3)
            {
                var first = indices[i];
                var second = indices[i + 1];
                var third = indices[i + 2];
                if (first >= positionCount || second >= positionCount || third >= positionCount)
                    throw new InvalidDataException(
                        $"Face {i / 3} references positions ({first}, {second}, {third}) " +
                        $"outside the {positionCount}-item position array.");
                faces.Add(((int)first, (int)second, (int)third));
            }
            return new RHAdobeMesh { Name = name, Positions = positions.ToList(), Faces = faces };
        }

        internal U3DSceneNode ReadSceneNode(U3DBlock block, bool isModelNode, double coordinateScale)
        {
            using var data = OpenData(block);
            var name = data.ReadString();
            var parentCount = data.ReadUInt32();
            if (parentCount > 1_000_000)
                throw new InvalidDataException("A U3D scene node has too many parents.");
            var node = new U3DSceneNode { Name = name };
            for (var parentIndex = 0U; parentIndex < parentCount; parentIndex++)
            {
                var parentName = data.ReadString();
                var serialized = new double[16];
                for (var i = 0; i < serialized.Length; i++) serialized[i] = data.ReadSingle();
                // ECMA-363 stores A..P while displaying the matrix as
                // [M I E A; N J F B; O K G C; P L H D]. Transpose that
                // column-vector matrix into TVGL's row-vector Matrix4x4 layout.
                var transform = new Matrix4x4(
                    serialized[12], serialized[13], serialized[14], serialized[15],
                    serialized[8], serialized[9], serialized[10], serialized[11],
                    serialized[4], serialized[5], serialized[6], serialized[7],
                    serialized[0] * coordinateScale, serialized[1] * coordinateScale,
                    serialized[2] * coordinateScale, serialized[3]);
                if (!IsFinite(transform))
                    throw new InvalidDataException($"Scene node '{name}' contains a non-finite transform.");
                node.Parents.Add(new U3DParentNode(parentName, transform));
            }
            if (!isModelNode) return node;
            var modelNode = new U3DSceneNode
            {
                Name = node.Name,
                ResourceName = data.ReadString(),
                Visibility = data.ReadUInt32()
            };
            modelNode.Parents.AddRange(node.Parents);
            return modelNode;
        }

        private static bool IsFinite(Matrix4x4 matrix) =>
            double.IsFinite(matrix.M11) && double.IsFinite(matrix.M12) &&
            double.IsFinite(matrix.M13) && double.IsFinite(matrix.M14) &&
            double.IsFinite(matrix.M21) && double.IsFinite(matrix.M22) &&
            double.IsFinite(matrix.M23) && double.IsFinite(matrix.M24) &&
            double.IsFinite(matrix.M31) && double.IsFinite(matrix.M32) &&
            double.IsFinite(matrix.M33) && double.IsFinite(matrix.M34) &&
            double.IsFinite(matrix.M41) && double.IsFinite(matrix.M42) &&
            double.IsFinite(matrix.M43) && double.IsFinite(matrix.M44);

        internal U3DDataReader OpenData(U3DBlock block)
            => new(stream, block.DataOffset, block.DataSize);

        private U3DBlock ReadBlockHeader(long offset, long limit)
        {
            stream.Position = offset;
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var type = reader.ReadUInt32();
            var dataSize = reader.ReadUInt32();
            var metadataSize = reader.ReadUInt32();
            var dataOffset = offset + 12;
            var metadataOffset = Align4(dataOffset + dataSize);
            var end = Align4(metadataOffset + metadataSize);
            if (end > limit || end < offset)
                throw new InvalidDataException($"U3D block 0x{type:X8} extends beyond its containing block.");
            return new U3DBlock(type, dataOffset, dataSize, metadataOffset, metadataSize);
        }

        private static long Align4(long value) => (value + 3) & ~3L;
    }

    internal sealed class U3DDataReader : IDisposable
    {
        private readonly Stream stream;
        private readonly BinaryReader reader;
        private readonly long start;
        private readonly long end;

        internal U3DDataReader(Stream stream, long start, uint length)
        {
            this.stream = stream;
            this.start = start;
            end = checked(start + length);
            stream.Position = start;
            reader = new BinaryReader(stream, Encoding.UTF8, true);
        }

        internal long Remaining => end - stream.Position;
        internal long AbsolutePosition
        {
            get => stream.Position;
            set
            {
                if (value < start || value > end) throw new EndOfStreamException();
                stream.Position = value;
            }
        }

        internal byte ReadByte() { Ensure(1); return reader.ReadByte(); }
        internal ushort ReadUInt16() { Ensure(2); return reader.ReadUInt16(); }
        internal uint ReadUInt32() { Ensure(4); return reader.ReadUInt32(); }
        internal ulong ReadUInt64() { Ensure(8); return reader.ReadUInt64(); }
        internal float ReadSingle() { Ensure(4); return reader.ReadSingle(); }
        internal double ReadDouble() { Ensure(8); return reader.ReadDouble(); }
        internal byte[] ReadBytes(int count) { Ensure(count); return reader.ReadBytes(count); }

        internal string ReadString()
        {
            var length = ReadUInt16();
            return Encoding.UTF8.GetString(ReadBytes(length));
        }

        internal void Skip(long count)
        {
            Ensure(count);
            stream.Position += count;
        }

        internal void Align4(long origin)
        {
            var relative = stream.Position - origin;
            Skip((4 - relative % 4) % 4);
        }

        internal uint ReadVariableUInt(int byteCount)
        {
            if (byteCount is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(byteCount));
            uint result = 0;
            for (var i = 0; i < byteCount; i++) result |= (uint)ReadByte() << (8 * i);
            return result;
        }

        internal Vector3[] ReadFloatVectors(uint count, int dimensions, bool normals)
        {
            if (count > int.MaxValue) throw new InvalidDataException("The vector count is too large.");
            var dataType = ReadByte();
            var components = new double[dimensions][];
            switch (dataType)
            {
                case 0:
                    for (var component = 0; component < dimensions; component++)
                    {
                        components[component] = new double[count];
                        for (var i = 0; i < count; i++) components[component][i] = ReadSingle();
                    }
                    break;
                case 2:
                    for (var component = 0; component < dimensions; component++)
                        components[component] = ReadQuantizedFloats(count);
                    break;
                case 3 when normals && dimensions == 3:
                    var quantization = ReadUInt32();
                    if (quantization == 0) throw new InvalidDataException("Normal quantization cannot be zero.");
                    // Files emitted by the Acrobat/Right Hemisphere writer store
                    // both component arrays in a single encoded block: all alpha
                    // values followed by all theta values.
                    var angles = ReadEncodedUIntArray(checked(count * 2), 0,
                        quantization == uint.MaxValue ? null : quantization + 1);
                    components[0] = new double[count];
                    components[1] = new double[count];
                    components[2] = new double[count];
                    for (var i = 0; i < count; i++)
                    {
                        var alpha = angles[i] * 2.0 * Math.PI / quantization;
                        var theta = angles[count + i] * 2.0 * Math.PI / quantization;
                        components[0][i] = Math.Sin(theta) * Math.Cos(alpha);
                        components[1][i] = Math.Sin(theta) * Math.Sin(alpha);
                        components[2][i] = Math.Cos(theta);
                    }
                    break;
                default:
                    throw new NotSupportedException($"RHAdobe floating-point encoding {dataType} is not supported.");
            }

            var result = new Vector3[count];
            for (var i = 0; i < count; i++)
                result[i] = new Vector3(components[0][i], dimensions > 1 ? components[1][i] : 0,
                    dimensions > 2 ? components[2][i] : 0);
            return result;
        }

        private double[] ReadQuantizedFloats(uint count)
        {
            var minimum = ReadSingle();
            var maximum = ReadSingle();
            var result = new double[count];
            if (minimum == maximum)
            {
                Array.Fill(result, minimum);
                return result;
            }
            var quantization = ReadUInt32();
            if (quantization == 0) throw new InvalidDataException("Float quantization cannot be zero.");
            var values = ReadEncodedUIntArray(count, 0,
                quantization == uint.MaxValue ? null : quantization + 1);
            for (var i = 0; i < values.Length; i++)
                result[i] = minimum + values[i] * (maximum - minimum) / quantization;
            return result;
        }

        internal uint[] ReadEncodedUIntArray(uint count, int verticalStride = 0, uint? maximumExclusive = null)
        {
            if (count > int.MaxValue) throw new InvalidDataException("The encoded array is too large.");
            var header = ReadByte();
            var encoding = header & 0x3F;
            var result = new uint[count];
            switch (encoding)
            {
                case 0:
                    return result;
                case 1:
                    Array.Fill(result, ReadUInt32());
                    return result;
                case 2:
                    if (verticalStride > 0)
                    {
                        // Face triplets are byte-aligned and emitted high nibble
                        // first. An odd-length row therefore leaves the low nibble
                        // of its final byte unused.
                        for (var rowStart = 0; rowStart < result.Length; rowStart += verticalStride)
                        {
                            var rowLength = Math.Min(verticalStride, result.Length - rowStart);
                            for (var column = 0; column < rowLength; column += 2)
                            {
                                var rowPacked = ReadByte();
                                result[rowStart + column] = (uint)(rowPacked >> 4);
                                if (column + 1 < rowLength)
                                    result[rowStart + column + 1] = (uint)(rowPacked & 0xF);
                            }
                        }
                    }
                    else
                    {
                        byte packed = 0;
                        for (var i = 0; i < result.Length; i++)
                        {
                            if ((i & 1) == 0) packed = ReadByte();
                            result[i] = (uint)(((i & 1) == 0) ? packed & 0xF : packed >> 4);
                        }
                    }
                    return result;
                case >= 3 and <= 6:
                    var bytes = encoding - 2;
                    for (var i = 0; i < result.Length; i++) result[i] = ReadVariableUInt(bytes);
                    return result;
                case 7:
                case 10:
                    var sizeByteCount = (header >> 6) + 1;
                    var encodedSize = ReadVariableUInt(sizeByteCount);
                    if (encodedSize > Remaining) throw new InvalidDataException("Encoded integer buffer is truncated.");
                    var encodedEnd = stream.Position + encodedSize;
                    if (encoding == 7) DecodeRH39(result, encodedEnd);
                    else DecodeUIC1(result, encodedEnd, verticalStride, maximumExclusive);
                    if (stream.Position > encodedEnd) throw new InvalidDataException("Encoded integer buffer was over-read.");
                    stream.Position = encodedEnd;
                    return result;
                case 8:
                case 9:
                    throw new NotSupportedException("U3D arithmetic integer compression is not supported yet.");
                default:
                    throw new InvalidDataException($"Unknown RHAdobe integer encoding {encoding}.");
            }
        }

        private void DecodeRH39(uint[] result, long encodedEnd)
        {
            uint offset = 0;
            var output = 0;
            while (output < result.Length)
            {
                if (stream.Position >= encodedEnd) throw new InvalidDataException("RH39 buffer ended early.");
                var command = ReadByte();
                var operation = (command >> 4) & 7;
                var length = command & 0xF;
                length = ((command & 0x80) != 0) ? 17 + (length << 8) + ReadByte() : length + 1;
                if (operation == 7)
                {
                    if (length is < 1 or > 4) throw new InvalidDataException("Invalid RH39 offset size.");
                    offset = ReadVariableUInt(length);
                    continue;
                }
                if (output + length > result.Length) throw new InvalidDataException("RH39 output exceeds its declared count.");
                byte packed = 0;
                for (var i = 0; i < length; i++)
                {
                    result[output++] = operation switch
                    {
                        0 => ReadUInt32(),
                        1 => offset + (uint)(((i & 1) == 0 ? packed = ReadByte() : packed >> 4) & 0xF),
                        2 => offset + ReadByte(),
                        3 => offset + ReadUInt16(),
                        4 => ReadUInt16(),
                        5 => offset,
                        6 => ReadByte(),
                        _ => throw new InvalidDataException("Invalid RH39 operation.")
                    };
                }
            }
        }

        private void DecodeUIC1(uint[] result, long encodedEnd, int verticalStride, uint? maximumExclusive)
        {
            var previousValues = new uint[32];
            var previousDeltas = new int[32];
            var valuePosition = 0;
            var deltaPosition = 0;
            var queuedCount = 0U;
            var queuedBackIndex = 0U;
            uint current = 0;

            uint Normalize(long value)
            {
                if (!maximumExclusive.HasValue) return unchecked((uint)value);
                var modulus = (long)maximumExclusive.Value;
                value %= modulus;
                if (value < 0) value += modulus;
                return (uint)value;
            }

            uint BackValue(uint which) => previousValues[(valuePosition - (int)which + 32) & 31];
            int BackDelta(uint which) => previousDeltas[(deltaPosition - (int)which + 32) & 31];
            void AddValue(uint value) { valuePosition = (valuePosition + 1) & 31; previousValues[valuePosition] = value; }
            void AddDelta(int value) { deltaPosition = (deltaPosition + 1) & 31; previousDeltas[deltaPosition] = value; }

            for (var output = 0; output < result.Length; output++)
            {
                var previous = current;
                var vertical = verticalStride > 0 && output >= verticalStride ? result[output - verticalStride] : 0U;
                if (queuedCount > 0)
                {
                    queuedCount--;
                    current = BackValue(queuedBackIndex);
                }
                else
                {
                    if (stream.Position >= encodedEnd) throw new InvalidDataException("UIC1 buffer ended early.");
                    var command = ReadByte();
                    var operand = (uint)(command >> 4);
                    switch (command & 0xF)
                    {
                        case 0: current = BackValue(operand); break;
                        case 1:
                            current = (command & 0x10) != 0
                                ? Normalize((long)current + (command >> 5) + 1)
                                : Normalize((long)current - (command >> 5) - 1);
                            break;
                        case 2: throw new InvalidDataException("UIC1 command 2 is reserved.");
                        case 3: current = Normalize((long)vertical + operand + 1); break;
                        case 4: current = Normalize((long)vertical - operand - 1); break;
                        case 5: current = BackValue(16 + operand); break;
                        case 6: current = Normalize((long)current + BackDelta(operand)); break;
                        case 7: current = Normalize((long)vertical + operand + 17); break;
                        case 8: current = Normalize((long)vertical - operand - 17); break;
                        case 9: current = Normalize((long)current + operand + ((uint)ReadByte() << 4) + 3); break;
                        case 10: current = Normalize((long)current - operand - ((uint)ReadByte() << 4) - 3); break;
                        case 11: current = operand + ((uint)ReadByte() << 4); break;
                        case 12: current = operand + ((uint)ReadUInt16() << 4); break;
                        case 13:
                            current = BackValue(operand >> 2);
                            queuedBackIndex = operand & 3;
                            queuedCount = 1;
                            break;
                        case 14:
                            current = BackValue(0);
                            queuedBackIndex = 0;
                            queuedCount = operand + 2;
                            break;
                        case 15:
                            current = operand switch
                            {
                                <= 4 => operand,
                                5 => 0x000000FF,
                                6 => 0x0000FF00,
                                7 => 0x00FF0000,
                                8 => 0xFF000000,
                                9 => 0x00FFFFFF,
                                10 => 0xFFFFFFFF,
                                11 => ReadVariableUInt(3),
                                12 => ReadUInt32(),
                                13 => current,
                                _ => throw new InvalidDataException("Invalid UIC1 extended command.")
                            };
                            if (operand == 13)
                            {
                                queuedBackIndex = 0;
                                queuedCount = (uint)ReadUInt16() + 36;
                            }
                            break;
                    }
                }

                result[output] = current;
                AddValue(current);
                if (current != previous)
                {
                    var difference = (long)current - previous;
                    if (Math.Abs(difference) < int.MaxValue) AddDelta((int)difference);
                }
            }
        }

        private void Ensure(long count)
        {
            if (count < 0 || stream.Position + count > end) throw new EndOfStreamException();
        }

        public void Dispose() => reader.Dispose();
    }
}
