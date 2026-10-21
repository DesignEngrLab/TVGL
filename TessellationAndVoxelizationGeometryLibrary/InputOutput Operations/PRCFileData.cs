// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 10-21-2026
//
// Reader for PRC (ISO 14739-1), the 3D format embedded in 3D PDFs. The bit-level encodings,
// compressed doubles, schema evaluator, limits and entity layouts are ported from prc-rs,
// https://github.com/ralovich/prc-rs (commit 023fc07c823a85f1ef2ebc077c3b327a97b09701), which is
// MIT licensed: Copyright (c) Kristof Ralovich 2025-2026. The entity readers themselves are
// generated from its schema into PRCEntityReader.cs, and compressed meshes are decoded in
// PRCCompressedTessellation.cs. No native code is used.
// ***********************************************************************
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Reads the display tessellation and product structure of a PRC file into a
    /// <see cref="SolidAssembly"/>. Each distinct closed part mesh becomes one
    /// <see cref="TessellatedSolid"/> (in millimeters), placed once per product occurrence. Both
    /// plain and highly compressed tessellations are read; exact B-rep geometry is not decoded.
    /// </summary>
    internal sealed class PRCFileData : IO
    {
        // Limits from prc-rs limits.rs.
        internal const uint MaxStringLength = 4096;
        internal const uint MaxUserDataBits = 8 * 1024 * 256;
        internal const uint MaxArrayElements = 1024 * 1024 * 8;
        internal const int MaxSectionBytes = 1024 * 1024 * 128;
        /// <summary> Cap on the total decompressed size of all sections read from one file. </summary>
        private const long MaxTotalDecompressedBytes = 1024L * 1024 * 512;
        /// <summary> The PRC version the entity layouts implement. </summary>
        internal const uint SupportedVersion = 8137;
        /// <summary> Deepest product-occurrence nesting or prototype chain followed. </summary>
        private const int MaxAssemblyDepth = 64;
        /// <summary> Cap on assembly nodes, so cyclic or self-repeating occurrence trees fail cleanly. </summary>
        private const int MaxAssemblyNodes = 1_000_000;
        /// <summary> PRC_FACETESSDATA_Triangle: each triangle is 3 x (normal index, vertex index). </summary>
        private const uint FaceTessDataTriangle = 0x0002;

        /// <summary>
        /// Opens the PRC stream as an assembly. Meshes that are not closed (construction and
        /// reference surfaces in CATIA exports) are left out unless <paramref name="includeOpenSurfaces"/>.
        /// </summary>
        internal static SolidAssembly OpenSolidAssembly(Stream stream, string filename,
            TessellatedSolidBuildOptions buildOptions, bool includeOpenSurfaces = false)
        {
            var assembly = OpenSolidAssembly(stream, filename, buildOptions, includeOpenSurfaces,
                out var warnings, out var skipped);
            foreach (var warning in warnings)
                Log.Warning("PRC import: {Message}", warning);
            if (skipped.Count > 0)
                Log.Information("PRC import skipped {Count} open surface mesh(es): {Names}", skipped.Count,
                    string.Join(", ", skipped));
            return assembly;
        }

        /// <summary>
        /// Opens the PRC stream as an assembly, returning the non-fatal problems found and the names
        /// of the meshes left out because they are not closed.
        /// </summary>
        internal static SolidAssembly OpenSolidAssembly(Stream stream, string filename,
            TessellatedSolidBuildOptions buildOptions, bool includeOpenSurfaces,
            out List<string> warnings, out List<string> skipped)
        {
            var file = PrcFile.Read(ReadAllBytes(stream));
            var tree = new PrcAssemblyTree(file);
            warnings = tree.Warnings;
            var builder = new PrcAssemblyBuilder(Path.GetFileNameWithoutExtension(filename ?? ""), filename ?? "",
                tree.Meshes, tree.UnitsInMillimeters, buildOptions ?? TessellatedSolidBuildOptions.Default,
                includeOpenSurfaces);
            var assembly = builder.Build(tree.Root);
            skipped = builder.Skipped;
            return assembly;
        }

        /// <summary> Opens the PRC stream and returns every placed solid in global coordinates. </summary>
        internal static TessellatedSolid[] OpenSolids(Stream stream, string filename,
            TessellatedSolidBuildOptions buildOptions)
            => OpenSolidAssembly(stream, filename, buildOptions).RootAssembly.AllTessellatedSolidsInGlobalCoordinateSystem();

        private static byte[] ReadAllBytes(Stream stream)
        {
            if (stream == null || !stream.CanRead)
                throw new ArgumentException("PRC import requires a readable stream.", nameof(stream));
            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (memory.Length + read > MaxTotalDecompressedBytes)
                    throw new InvalidDataException("The PRC file exceeds the input size limit.");
                memory.Write(buffer, 0, read);
            }
            return memory.ToArray();
        }

        #region File structure and sections
        /// <summary> A file structure: its id and its parsed tree and tessellation sections. </summary>
        internal sealed class PrcFileStructure
        {
            internal (uint, uint, uint, uint) UniqueId { get; init; }
            internal PrcNode Tree { get; set; }
            internal PrcNode Globals { get; set; }
            /// <summary> The tessellations, or null when the section could not be read. </summary>
            internal PrcNode[] Tessellations { get; set; }
            internal string TessellationProblem { get; set; }
        }

        /// <summary> The parsed sections of a PRC file. </summary>
        internal sealed class PrcFile
        {
            internal uint AuthoringVersion { get; private init; }
            internal List<PrcFileStructure> FileStructures { get; } = [];
            internal PrcNode ModelFile { get; private set; }

            internal static PrcFile Read(byte[] bytes)
            {
                var header = new ByteReader(bytes, 0, bytes.Length);
                if (bytes.Length < 3 || bytes[0] != 'P' || bytes[1] != 'R' || bytes[2] != 'C')
                    throw new InvalidDataException("The stream does not begin with a PRC file header.");
                header.Skip(3);
                var minimalVersionForRead = header.ReadUInt32();
                if (minimalVersionForRead > SupportedVersion)
                    throw new NotSupportedException($"This PRC file needs a reader for version {minimalVersionForRead}; " +
                                                    $"version {SupportedVersion} is supported.");
                var file = new PrcFile { AuthoringVersion = header.ReadUInt32() };
                header.Skip(32); // file and application unique ids
                var structureCount = header.ReadUInt32();
                // Each description is 12 integers, so the count is bounded by the file size.
                if (structureCount > (bytes.Length - header.Position) / 48)
                    throw new InvalidDataException("The PRC header lists more file structures than the file can hold.");
                var descriptions = new List<((uint, uint, uint, uint) Id, uint[] Offsets)>();
                for (var i = 0; i < structureCount; i++)
                {
                    var id = (header.ReadUInt32(), header.ReadUInt32(), header.ReadUInt32(), header.ReadUInt32());
                    header.ReadUInt32(); // reserved
                    if (header.ReadUInt32() != 6)
                        throw new InvalidDataException("Expected 6 sections in each PRC file structure.");
                    var offsets = new uint[6];
                    for (var k = 0; k < 6; k++) offsets[k] = header.ReadUInt32();
                    descriptions.Add((id, offsets));
                }
                var modelFileStart = header.ReadUInt32();
                var modelFileEnd = header.ReadUInt32();
                var uncompressedFileCount = header.ReadUInt32();
                for (var i = 0U; i < uncompressedFileCount; i++)
                {
                    var size = header.ReadUInt32();
                    if (size > MaxSectionBytes)
                        throw new InvalidDataException("A PRC uncompressed file exceeds the size limit.");
                    header.Skip((int)size);
                }

                var context = new PrcContext
                {
                    AuthoringVersion = file.AuthoringVersion,
                    NumberOfFileStructures = descriptions.Count
                };
                long decompressed = 0;
                byte[] Inflate(uint start, uint end, string name)
                {
                    var data = Decompress(bytes, start, end, name);
                    decompressed += data.Length;
                    if (decompressed > MaxTotalDecompressedBytes)
                        throw new InvalidDataException("The PRC sections exceed the decompressed size limit.");
                    return data;
                }

                for (var i = 0; i < descriptions.Count; i++)
                {
                    var (id, offsets) = descriptions[i];
                    // header, globals, tree, tessellation, geometry, extra geometry
                    for (var k = 0; k < 4; k++)
                        if (offsets[k + 1] <= offsets[k])
                            throw new InvalidDataException($"Invalid section offsets in PRC file structure {i}.");
                    ReadFileStructureHeader(new ByteReader(bytes, offsets[0], offsets[1]));
                    var globals = new PrcBitReader(Inflate(offsets[1], offsets[2], "globals"));
                    var tree = new PrcBitReader(Inflate(offsets[2], offsets[3], "tree"));
                    var tessellation = new PrcBitReader(Inflate(offsets[3], offsets[4], "tessellation"));

                    context.Schema = new PrcSchemaEvaluator(PrcEntityReader.Read_Schema(globals, context));
                    var parsedGlobals = PrcEntityReader.Read_PRC_TYPE_ASM_FileStructureGlobals(globals, context);
                    var structure = new PrcFileStructure
                    {
                        UniqueId = id,
                        Globals = parsedGlobals,
                        Tree = PrcEntityReader.Read_PRC_TYPE_ASM_FileStructureTree(tree, context)
                    };
                    try
                    {
                        var section = PrcEntityReader.Read_PRC_TYPE_ASM_FileStructureTessellation(tessellation, context);
                        structure.Tessellations = section.Nodes("tess");
                    }
                    catch (NotSupportedException exception)
                    {
                        // The tessellations of one file structure are a single stream, so the rest of
                        // this structure's meshes cannot be reached; other structures are unaffected.
                        context.ResetTessellationState();
                        structure.TessellationProblem = exception.Message;
                    }
                    file.FileStructures.Add(structure);
                }

                if (modelFileEnd <= modelFileStart)
                    throw new InvalidDataException("Invalid PRC model file offsets.");
                var modelFile = new PrcBitReader(Inflate(modelFileStart, modelFileEnd, "model file"));
                context.Schema = new PrcSchemaEvaluator(PrcEntityReader.Read_Schema(modelFile, context));
                file.ModelFile = PrcEntityReader.Read_PRC_TYPE_ASM_ModelFile(modelFile, context);
                return file;
            }

            /// <summary> Checks the uncompressed header that starts each file structure. </summary>
            private static void ReadFileStructureHeader(ByteReader reader)
            {
                if (reader.ReadByte() != 'P' || reader.ReadByte() != 'R' || reader.ReadByte() != 'C')
                    throw new InvalidDataException("A PRC file structure header is missing its signature.");
                if (reader.ReadUInt32() > SupportedVersion)
                    throw new NotSupportedException("A PRC file structure needs a newer reader version.");
            }

            private static byte[] Decompress(byte[] bytes, uint start, uint end, string name)
            {
                if (end > bytes.Length || end <= start)
                    throw new InvalidDataException($"The PRC {name} section lies outside the file.");
                if (end - start > MaxSectionBytes)
                    throw new InvalidDataException($"The PRC {name} section exceeds the size limit.");
                using var compressed = new MemoryStream(bytes, (int)start, (int)(end - start), false);
                using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
                using var output = new MemoryStream();
                var buffer = new byte[81920];
                while (true)
                {
                    int read;
                    try
                    {
                        read = zlib.Read(buffer, 0, buffer.Length);
                    }
                    catch (InvalidDataException exception)
                    {
                        throw new InvalidDataException($"The PRC {name} section is not valid zlib data.", exception);
                    }
                    if (read == 0) return output.ToArray();
                    if (output.Length + read > MaxSectionBytes)
                        throw new InvalidDataException($"The PRC {name} section exceeds the decompressed size limit.");
                    output.Write(buffer, 0, read);
                }
            }
        }

        /// <summary> Little-endian reader for the byte-aligned file headers. </summary>
        private sealed class ByteReader(byte[] bytes, long start, long end)
        {
            internal long Position { get; private set; } = start;

            internal void Skip(int count)
            {
                if (count < 0 || Position + count > end || Position + count > bytes.Length)
                    throw new InvalidDataException("The PRC header ends unexpectedly.");
                Position += count;
            }

            internal byte ReadByte()
            {
                Skip(1);
                return bytes[Position - 1];
            }

            internal uint ReadUInt32()
            {
                Skip(4);
                return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan((int)Position - 4, 4));
            }
        }
        #endregion

        #region Assembly tree
        /// <summary> One mesh read from a part definition's representation item. </summary>
        internal sealed class PrcMesh
        {
            internal string Name { get; init; }
            internal double[] Coordinates { get; init; }
            internal int[] Triangles { get; init; }
            internal double[] Transform { get; set; }
        }

        /// <summary> A product occurrence with its prototype inheritance resolved. </summary>
        internal sealed class PrcAssemblyNode
        {
            internal string Name { get; init; }
            /// <summary> The 16 PRC values in stored (column-major) order. </summary>
            internal double[] Transform { get; init; }
            internal List<int> Meshes { get; } = [];
            internal List<PrcAssemblyNode> Children { get; } = [];
        }

        /// <summary>
        /// Resolves the product occurrences of the parsed file into a tree of placed meshes. An
        /// occurrence inherits its part, children and location from its prototype unless it overrides
        /// them; prototypes in other file structures are found by their unique id.
        /// </summary>
        internal sealed class PrcAssemblyTree
        {
            private static readonly double[] Identity = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1];
            private readonly List<PrcFileStructure> structures;
            private readonly Dictionary<(uint, uint, uint, uint), int> structureById = [];
            private readonly Dictionary<(int, int), List<int>> partMeshes = [];
            private int nodeCount;

            internal List<PrcMesh> Meshes { get; } = [];
            internal List<string> Warnings { get; } = [];
            internal PrcAssemblyNode Root { get; }
            internal double UnitsInMillimeters { get; }

            internal PrcAssemblyTree(PrcFile file)
            {
                structures = file.FileStructures;
                for (var i = 0; i < structures.Count; i++)
                    structureById[structures[i].UniqueId] = i;
                foreach (var structure in structures.Where(s => s.TessellationProblem != null))
                    Warnings.Add($"file structure {structures.IndexOf(structure)}: {structure.TessellationProblem} " +
                                 "Its meshes were skipped.");

                var roots = new List<PrcAssemblyNode>();
                foreach (var reference in file.ModelFile.Nodes("product_occurrences"))
                {
                    var fs = FindStructure(reference.Node("unique_id"))
                             ?? throw new InvalidDataException("The PRC root file structure was not found.");
                    var rootIndex = reference.UInt("root_index");
                    if (rootIndex == 0)
                        throw new InvalidDataException("Invalid PRC root product occurrence index.");
                    var node = BuildNode(fs, (int)Math.Min(rootIndex - 1, int.MaxValue), 0);
                    if (node != null) roots.Add(node);
                }
                if (roots.Count == 1)
                    Root = roots[0];
                else
                {
                    Root = new PrcAssemblyNode { Name = "root", Transform = Identity };
                    Root.Children.AddRange(roots);
                }
                var units = file.ModelFile.Double("units_in_mm");
                UnitsInMillimeters = units > 0 ? units : 1.0;
            }

            private int? FindStructure(PrcNode uniqueId)
            {
                var key = (uniqueId.UInt("unique_id0"), uniqueId.UInt("unique_id1"), uniqueId.UInt("unique_id2"),
                    uniqueId.UInt("unique_id3"));
                return structureById.TryGetValue(key, out var index) ? index : null;
            }

            /// <summary> The file structure a FileIdentifier refers to. </summary>
            private int? ReferencedStructure(int fs, PrcNode fileIdentifier)
                => fileIdentifier == null || fileIdentifier.Bool("flag") ? fs : FindStructure(fileIdentifier.Node("unique_id"));

            private PrcNode Product(int fs, int index)
            {
                var products = structures[fs].Tree.Nodes("products");
                return index >= 0 && index < products.Length ? products[index] : null;
            }

            private sealed class Resolved
            {
                internal (int Fs, int Index)? Part;
                internal List<(int Fs, int Index)> Children = [];
                internal double[] Location;
                internal string Name;
            }

            private Resolved Resolve(int fs, int index, int depth)
            {
                if (depth > MaxAssemblyDepth)
                {
                    Warnings.Add($"prototype chain deeper than {MaxAssemblyDepth} at fs{fs}.po{index}");
                    return null;
                }
                var product = Product(fs, index);
                if (product == null) return null;
                var references = product.Node("references_product_occurrence");
                var prototypeIndex = references.UInt("biased_index_prototype");
                var inherited = new Resolved();
                if (prototypeIndex > 0)
                {
                    var prototypeFs = ReferencedStructure(fs, references.Node("prototype_in_same_file_structure"));
                    if (prototypeFs.HasValue)
                        inherited = Resolve(prototypeFs.Value, (int)Math.Min(prototypeIndex - 1, int.MaxValue), depth + 1)
                                    ?? inherited;
                    else
                        Warnings.Add($"unresolved prototype file structure at fs{fs}.po{index}");
                }

                var partIndex = references.UInt("biased_index_part");
                var children = references.UInts("index_child_occurrence")
                    .Select(child => (fs, (int)Math.Min(child, int.MaxValue))).ToList();
                var location = product.Node("location") switch
                {
                    { Type: "PRC_TYPE_MISC_GeneralTransformation" } general => general.Doubles("general_transform"),
                    { Type: "PRC_TYPE_MISC_CartesianTransformation" } cartesian
                        => CartesianMatrix(cartesian.Node("transform"), $"fs{fs}.po{index}"),
                    _ => null
                };
                if (product.Bool("has_transformation") && location?.Length != 16)
                {
                    Warnings.Add($"unsupported transformation type at fs{fs}.po{index}: treated as identity");
                    location = null;
                }
                return new Resolved
                {
                    Part = partIndex > 0 ? (fs, (int)Math.Min(partIndex - 1, int.MaxValue)) : inherited.Part,
                    Children = children.Count > 0 ? children : inherited.Children,
                    Location = location ?? inherited.Location,
                    Name = EntityName(product) ?? inherited.Name
                };
            }

            /// <summary>
            /// The 4x4 matrix of a Transformation3D (ISO 14739-1 §7.4.11), as the 16 values of
            /// PRC_TYPE_MISC_GeneralTransformation: column-major, translation in elements 12..14. The
            /// rotation's X and Y axes are the first two columns and their cross product the third
            /// (Y x X when mirrored), scaling applies to the 3x3 block (pdf-issues #716), and a
            /// non-orthogonal or homogeneous part, which a 3D cartesian transformation should not have,
            /// is used as stored but reported.
            /// </summary>
            private double[] CartesianMatrix(PrcNode transform, string where)
            {
                if (transform == null) return null;
                const int translate = 0x01, rotate = 0x02, mirror = 0x04, scale = 0x08, nonUniformScale = 0x10,
                    nonOrtho = 0x20, homogeneous = 0x40;
                var behavior = transform.Get<sbyte>("behavior") & 0x7F;
                double[] Vector(PrcNode v) => v == null ? null : [v.Double("x"), v.Double("y"), v.Double("z")];
                static double[] Cross(double[] a, double[] b)
                    => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];

                double[] xAxis = [1, 0, 0], yAxis = [0, 1, 0], zAxis;
                if ((behavior & nonOrtho) != 0)
                {
                    var axes = transform.Nodes("non_ortho_matrix");
                    if (axes.Length != 3) return null;
                    (xAxis, yAxis, zAxis) = (Vector(axes[0]), Vector(axes[1]), Vector(axes[2]));
                    Warnings.Add($"non-orthogonal cartesian transformation at {where} (used as stored)");
                }
                else
                {
                    if ((behavior & rotate) != 0)
                    {
                        var axes = transform.Nodes("rotation");
                        if (axes.Length != 2) return null;
                        (xAxis, yAxis) = (Vector(axes[0]), Vector(axes[1]));
                    }
                    zAxis = (behavior & mirror) != 0 ? Cross(yAxis, xAxis) : Cross(xAxis, yAxis);
                }

                double[] factors = [1, 1, 1];
                if ((behavior & nonUniformScale) != 0)
                    factors = Vector(transform.Node("non_uniform_scale")) ?? factors;
                else if ((behavior & scale) != 0)
                {
                    var s = transform.Double("scale");
                    factors = [s, s, s];
                }
                var offset = (behavior & translate) != 0 ? Vector(transform.Node("translation")) ?? [0, 0, 0] : [0, 0, 0];
                double[] lastRow = [0, 0, 0, 1];
                if ((behavior & homogeneous) != 0)
                {
                    var h = transform.Doubles("homogeneous");
                    if (h.Length != 4) return null;
                    lastRow = h;
                    Warnings.Add($"homogeneous cartesian transformation at {where} (used as stored)");
                }

                // Column j of the matrix is element 4j..4j+3.
                double[] m =
                [
                    xAxis[0] * factors[0], xAxis[1] * factors[0], xAxis[2] * factors[0], lastRow[0],
                    yAxis[0] * factors[1], yAxis[1] * factors[1], yAxis[2] * factors[1], lastRow[1],
                    zAxis[0] * factors[2], zAxis[1] * factors[2], zAxis[2] * factors[2], lastRow[2],
                    offset[0], offset[1], offset[2], lastRow[3]
                ];
                return m.All(double.IsFinite) ? m : null;
            }

            /// <summary> The occurrence's own name, or null when it reuses the previous name. </summary>
            private static string EntityName(PrcNode product)
            {
                var name = product.Node("base")?.Node("base")?.Node("base")?.Node("entity_name");
                return name == null || name.Bool("same_name") ? null : name.String("name");
            }

            private PrcAssemblyNode BuildNode(int fs, int index, int depth)
            {
                var resolved = Resolve(fs, index, 0);
                if (resolved == null) return null;
                if (depth > MaxAssemblyDepth)
                {
                    Warnings.Add($"assembly deeper than {MaxAssemblyDepth} at fs{fs}.po{index}");
                    return null;
                }
                if (++nodeCount > MaxAssemblyNodes)
                    throw new InvalidDataException($"The PRC assembly has more than {MaxAssemblyNodes} occurrences.");
                var node = new PrcAssemblyNode
                {
                    Name = resolved.Name ?? $"fs{fs}.po{index}",
                    Transform = resolved.Location ?? Identity
                };
                if (resolved.Part is { } part)
                    node.Meshes.AddRange(PartMeshes(part.Fs, part.Index, node.Name));
                foreach (var (childFs, childIndex) in resolved.Children)
                {
                    var child = BuildNode(childFs, childIndex, depth + 1);
                    if (child != null) node.Children.Add(child);
                }
                return node;
            }

            /// <summary> Mesh ids of a part definition, reading each representation item's mesh once. </summary>
            private List<int> PartMeshes(int fs, int part, string name)
            {
                if (partMeshes.TryGetValue((fs, part), out var ids)) return ids;
                ids = [];
                partMeshes[(fs, part)] = ids;
                var parts = structures[fs].Tree.Nodes("parts");
                if (part < 0 || part >= parts.Length)
                {
                    Warnings.Add($"{name}: part definition {part} does not exist (skipped)");
                    return ids;
                }
                var tessellations = structures[fs].Tessellations;
                var items = parts[part].Nodes("representation_items");
                for (var ri = 0; ri < items.Length; ri++)
                {
                    var content = items[ri].Node("data")?.Node("item_content");
                    if (content == null) continue;
                    var localTransform = LocalCoordinateSystem(
                        fs, content.UInt("biased_index_local_coordinate_system"), $"{name}.ri{ri}");
                    var tessellationIndex = content.UInt("biased_index_tessellation");
                    if (tessellationIndex == 0) continue;
                    if (tessellations == null)
                    {
                        Warnings.Add($"{name}: representation item {ri} tessellation could not be read (skipped)");
                        continue;
                    }
                    var tessellation = tessellationIndex <= tessellations.Length
                        ? tessellations[tessellationIndex - 1].Node("id")
                        : null;
                    var mesh = tessellation?.Type switch
                    {
                        "PRC_TYPE_TESS_3D" => ReadMesh(tessellation, name),
                        "PRC_TYPE_TESS_3D_Compressed" => ReadCompressedMesh(tessellation, name, ri),
                        _ => null
                    };
                    if (tessellation?.Type is not ("PRC_TYPE_TESS_3D" or "PRC_TYPE_TESS_3D_Compressed"))
                        Warnings.Add($"{name}: representation item {ri} tessellation is not a 3D mesh (skipped)");
                    if (mesh == null) continue;
                    mesh.Transform = localTransform;
                    ids.Add(Meshes.Count);
                    Meshes.Add(mesh);
                }
                return ids;
            }

            private double[] LocalCoordinateSystem(
                int fs,
                uint biasedIndex,
                string where,
                HashSet<int> path = null)
            {
                if (biasedIndex == 0) return null;
                var coordinateSystems = structures[fs].Globals?.Node("global_data")?.Nodes("ref_coords") ?? [];
                var index = (long)biasedIndex - 1;
                if (index < 0 || index >= coordinateSystems.Length)
                {
                    Warnings.Add($"{where}: local coordinate system {biasedIndex} does not exist (treated as identity)");
                    return null;
                }
                path ??= [];
                if (!path.Add((int)index))
                {
                    Warnings.Add($"{where}: local coordinate-system references are cyclic (treated as identity)");
                    return null;
                }
                var coordinateSystem = coordinateSystems[(int)index];
                var transform = coordinateSystem.Node("transform");
                var local = transform?.Type switch
                {
                    "PRC_TYPE_MISC_GeneralTransformation" => transform.Doubles("general_transform"),
                    "PRC_TYPE_MISC_CartesianTransformation" =>
                        CartesianMatrix(transform.Node("transform"), where),
                    _ => null
                };
                if (local?.Length != 16 || !local.All(double.IsFinite))
                {
                    Warnings.Add($"{where}: local coordinate system is unsupported (treated as identity)");
                    path.Remove((int)index);
                    return null;
                }
                var parentIndex = coordinateSystem.Node("item_content")
                    ?.UInt("biased_index_local_coordinate_system") ?? 0;
                var parent = LocalCoordinateSystem(fs, parentIndex, where, path);
                path.Remove((int)index);
                return parent == null ? local : Multiply(local, parent);
            }

            private static double[] Multiply(double[] first, double[] second)
            {
                var result = new double[16];
                for (var row = 0; row < 4; row++)
                for (var column = 0; column < 4; column++)
                    for (var k = 0; k < 4; k++)
                        result[4 * row + column] += first[4 * row + k] * second[4 * k + column];
                return result;
            }

            /// <summary> A highly compressed mesh, or null (with a warning) when it cannot be decoded. </summary>
            private PrcMesh ReadCompressedMesh(PrcNode tessellation, string name, int item)
            {
                try
                {
                    var (coordinates, triangles) = new PrcCompressedTessellation(tessellation).Decode();
                    return new PrcMesh { Name = name, Coordinates = coordinates, Triangles = triangles };
                }
                catch (InvalidDataException exception)
                {
                    Warnings.Add($"{name}: representation item {item} compressed tessellation skipped: {exception.Message}");
                    return null;
                }
            }

            private PrcMesh ReadMesh(PrcNode tessellation, string name)
            {
                var coordinates = tessellation.Node("tessellation_coordinates").Doubles("coordinates");
                var indices = tessellation.UInts("triangulated_index_array");
                var triangles = new List<int>();
                var unsupported = 0;
                foreach (var face in tessellation.Nodes("face_tessellation_data"))
                {
                    if (face.UInt("used_entities_flag") != FaceTessDataTriangle)
                    {
                        unsupported++;
                        continue;
                    }
                    long start = face.UInt("start_triangulated");
                    var sizes = face.UInts("triangulateddata");
                    long count = sizes.Length > 0 ? sizes[0] : 0;
                    // Each triangle is 3 x (normal index, vertex index); vertex indices address coordinate triples.
                    for (long t = 0; t < count; t++)
                    {
                        var b = start + t * 6;
                        if (b + 5 >= indices.Length)
                        {
                            Warnings.Add($"{name}: triangle index out of range (face truncated)");
                            break;
                        }
                        triangles.Add((int)(indices[b + 1] / 3));
                        triangles.Add((int)(indices[b + 3] / 3));
                        triangles.Add((int)(indices[b + 5] / 3));
                    }
                }
                if (unsupported > 0)
                    Warnings.Add($"{name}: {unsupported} face(s) use an unsupported tessellation layout (skipped)");
                var vertexCount = coordinates.Length / 3;
                if (triangles.Any(i => i >= vertexCount))
                {
                    Warnings.Add($"{name}: vertex index out of range (mesh skipped)");
                    return null;
                }
                return new PrcMesh { Name = name, Coordinates = coordinates, Triangles = triangles.ToArray() };
            }
        }

        /// <summary> Builds the TVGL assembly from the resolved occurrence tree. </summary>
        private sealed class PrcAssemblyBuilder(string name, string fileName, List<PrcMesh> meshes, double scale,
            TessellatedSolidBuildOptions buildOptions, bool includeOpenSurfaces)
        {
            private readonly SolidAssembly assembly = new(name);
            private readonly Dictionary<int, TessellatedSolid> solids = [];
            private int nextReferenceId = 1;

            internal List<string> Skipped { get; } = [];

            internal SolidAssembly Build(PrcAssemblyNode root)
            {
                var rootAssembly = BuildNode(root, null);
                if (rootAssembly != null)
                    assembly.RootAssembly.Add(rootAssembly, ToMatrix(root.Transform));
                assembly.CompleteInitialization();
                return assembly;
            }

            /// <summary> The sub-assembly of a node, or null when it places no solids. </summary>
            private SubAssembly BuildNode(PrcAssemblyNode node, string parentName)
            {
                var sub = new SubAssembly(assembly, BaseName(node.Name), "", nextReferenceId++);
                foreach (var mesh in node.Meshes)
                {
                    var solid = GetSolid(mesh, node.Name, parentName);
                    if (solid != null) sub.Add(solid, Matrix4x4.Identity);
                }
                foreach (var child in node.Children)
                {
                    var childAssembly = BuildNode(child, node.Name);
                    if (childAssembly != null) sub.Add(childAssembly, ToMatrix(child.Transform));
                }
                return sub.Solids.Count + sub.SubAssemblies.Count > 0 ? sub : null;
            }

            /// <summary>
            /// PRC stores 16 values column-major with the translation in elements 12..14, which is the
            /// row-vector layout of Matrix4x4 (M11..M44, translation in M41..M43), so they map in order.
            /// </summary>
            private Matrix4x4 ToMatrix(double[] m)
                => m == null || m.Length != 16
                    ? Matrix4x4.Identity
                    : new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11],
                        m[12] * scale, m[13] * scale, m[14] * scale, m[15]);

            private TessellatedSolid GetSolid(int id, string nodeName, string parentName)
            {
                if (solids.TryGetValue(id, out var existing)) return existing;
                var mesh = meshes[id];
                var solidName = parentName == null ? BaseName(nodeName) : $"{BaseName(parentName)} - {BaseName(nodeName)}";
                var vertices = new Vector3[mesh.Coordinates.Length / 3];
                var localTransform = ToMatrix(mesh.Transform);
                for (var i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(mesh.Coordinates[3 * i] * scale, mesh.Coordinates[3 * i + 1] * scale,
                        mesh.Coordinates[3 * i + 2] * scale).Transform(localTransform);
                var faces = new (int, int, int)[mesh.Triangles.Length / 3];
                var mirrored = localTransform.GetDeterminant() < 0;
                for (var i = 0; i < faces.Length; i++)
                    faces[i] = mirrored
                        ? (mesh.Triangles[3 * i], mesh.Triangles[3 * i + 2], mesh.Triangles[3 * i + 1])
                        : (mesh.Triangles[3 * i], mesh.Triangles[3 * i + 1], mesh.Triangles[3 * i + 2]);

                TessellatedSolid solid = null;
                if (faces.Length == 0 || (!includeOpenSurfaces && !IsClosed(faces)))
                    Skipped.Add(solidName);
                else
                {
                    solid = new TessellatedSolid(vertices, faces, null, buildOptions, UnitType.millimeter, solidName,
                        fileName);
                    // Primitive surfaces are classified downstream, but the TVGL writer fails on a null
                    // primitive list and its reader on an empty one, so mark the faces as an UnknownRegion.
                    // NumberOfPrimitives is what the writer records, and AddPrimitive does not maintain it.
                    if (solid.Primitives == null || solid.Primitives.Count == 0)
                    {
                        solid.AddPrimitive(new UnknownRegion(solid.Faces));
                        solid.NumberOfPrimitives = solid.Primitives.Count;
                    }
                }
                solids[id] = solid;
                return solid;
            }

            /// <summary> A mesh is closed when every edge is shared by exactly two triangles. </summary>
            private static bool IsClosed((int a, int b, int c)[] faces)
            {
                var edgeUse = new Dictionary<(int, int), int>();
                foreach (var (a, b, c) in faces)
                    foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
                    {
                        var key = p < q ? (p, q) : (q, p);
                        edgeUse[key] = edgeUse.GetValueOrDefault(key) + 1;
                    }
                return edgeUse.Values.All(n => n == 2);
            }

            /// <summary> CATIA names read "PartNumber(Instance name)"; keep the part number. </summary>
            private static string BaseName(string name)
            {
                var paren = name.IndexOf('(');
                return (paren > 0 ? name[..paren] : name).Trim();
            }
        }
        #endregion
    }

    /// <summary> A parsed PRC entity: its schema type name and its fields by schema name. </summary>
    internal sealed class PrcNode(string type)
    {
        private readonly Dictionary<string, object> fields = [];

        internal string Type { get; } = type;

        internal T Set<T>(string name, T value)
        {
            fields[name] = value;
            return value;
        }

        internal T Get<T>(string name) => fields.TryGetValue(name, out var value) && value is T typed ? typed : default;
        internal PrcNode Node(string name) => Get<PrcNode>(name);
        internal PrcNode[] Nodes(string name) => Get<PrcNode[]>(name) ?? [];
        internal uint UInt(string name) => Get<uint>(name);
        internal uint[] UInts(string name) => Get<uint[]>(name) ?? [];
        internal double Double(string name) => Get<double>(name);
        internal double[] Doubles(string name) => Get<double[]>(name) ?? [];
        internal bool Bool(string name) => Get<bool>(name);
        internal string String(string name) => Get<string>(name);
        public override string ToString() => Type;
    }

    /// <summary> State shared by the entity readers while one PRC file is parsed. </summary>
    internal sealed class PrcContext
    {
        private const int MaxEntityDepth = 512;
        private int depth;
        private uint faceVertexColorCount;

        internal uint AuthoringVersion { get; init; }
        internal int NumberOfFileStructures { get; init; }
        internal PrcSchemaEvaluator Schema { get; set; }

        internal void Enter()
        {
            if (++depth > MaxEntityDepth)
                throw Invalid($"entities are nested more than {MaxEntityDepth} deep");
        }

        internal void Leave() => depth--;

        /// <summary> Element count of an array, checked against the limits and the remaining data. </summary>
        internal int Count(long count, PrcBitReader reader)
        {
            if (count < 0 || count > PRCFileData.MaxArrayElements)
                throw Invalid($"array of {count} elements exceeds the limit of {PRCFileData.MaxArrayElements}");
            // Every element reads at least one bit.
            if (count > reader.RemainingBits)
                throw Invalid($"array of {count} elements is longer than the remaining data");
            return (int)count;
        }

        internal void Expect(uint value, uint expected, string field)
        {
            if (value != expected)
                throw Invalid($"{field} is {value}, expected type {expected}");
        }

        internal Exception UnknownType(string field, uint type) => Invalid($"{field}: unrecognized entity type {type}");

        /// <summary> Faces of a compressed tessellation: one more than the largest face index. </summary>
        internal static int FaceCount(int[] triangleFaces) => triangleFaces.Length == 0 ? 0 : triangleFaces.Max() + 1;

        internal static int CountTrue(bool[] values) => values.Count(v => v);

        internal Exception Invalid(string message) => new InvalidDataException("Invalid PRC data: " + message + ".");

        internal void EvaluateSchema(PrcBitReader reader, uint type) => Schema?.Evaluate(reader, type);

        #region Vertex colors
        // A VertexColors entity carries no count: it is deduced from the enclosing face or wire.
        internal bool InsideWire { get; private set; }
        internal uint[] WireIndexes { get; set; } = [];
        internal bool WireIsSegmentColor { get; set; }

        internal void EnterWire()
        {
            InsideWire = true;
            faceVertexColorCount = 0;
            WireIsSegmentColor = false;
        }

        internal void LeaveWire() => InsideWire = false;

        internal void ResetTessellationState()
        {
            InsideWire = false;
            depth = 0;
        }

        /// <summary> A face's color count: 3 per triangle of its triangulated data. </summary>
        internal void SetVertexColorCountFromFace(uint usedEntitiesFlag, uint[] triangulatedData)
        {
            faceVertexColorCount = 0;
            foreach (var count in triangulatedData)
                faceVertexColorCount = unchecked(faceVertexColorCount + 3 * count);
        }

        internal uint VertexColorCount => InsideWire ? WireVertexColorCount() : faceVertexColorCount;

        /// <summary> A wire's color count: one per point of each wire edge, or per segment. </summary>
        private uint WireVertexColorCount()
        {
            var count = 0U;
            var i = 0L;
            while (i < WireIndexes.Length)
            {
                var pointCount = WireIndexes[i] & 0x7FFFFFFF;
                if (i + 1 + pointCount > WireIndexes.Length)
                    throw Invalid("wire edge extends beyond its indices");
                count = unchecked(count + (WireIsSegmentColor ? pointCount - 1 : pointCount));
                i += 1 + pointCount;
            }
            return count;
        }
        #endregion
    }

    /// <summary> Reads the PRC bit stream: most significant bit first. </summary>
    internal sealed class PrcBitReader(byte[] data)
    {
        private readonly long lengthInBits = data.LongLength * 8;
        private long position;

        internal long RemainingBits => lengthInBits - position;

        internal bool ReadBit()
        {
            if (position >= lengthInBits)
                throw new EndOfStreamException("The PRC section ended unexpectedly.");
            var bit = (data[position >> 3] >> (7 - (int)(position & 7))) & 1;
            position++;
            return bit != 0;
        }

        /// <summary> Reads up to 32 bits as an unsigned value, most significant first. </summary>
        internal uint ReadBits(int count)
        {
            var value = 0U;
            for (var i = 0; i < count; i++)
                value = (value << 1) | (ReadBit() ? 1U : 0U);
            return value;
        }

        internal bool ReadBoolean() => ReadBit();
        internal sbyte ReadCharacter() => (sbyte)ReadBits(8);
        internal byte ReadUnsignedCharacter() => (byte)ReadBits(8);

        internal ushort ReadUnsignedShort()
        {
            var low = ReadBits(8);
            return (ushort)((ReadBits(8) << 8) | low);
        }

        /// <summary> Up to four bytes, least significant first, each preceded by a continuation bit. </summary>
        internal uint ReadUnsignedInteger()
        {
            var value = 0U;
            var i = 0;
            while (ReadBit() && i < 4)
            {
                value |= ReadBits(8) << (8 * i);
                i++;
            }
            return value;
        }

        internal uint PeekUnsignedInteger()
        {
            var start = position;
            var value = ReadUnsignedInteger();
            position = start;
            return value;
        }

        /// <summary> Like <see cref="ReadUnsignedInteger"/>, sign-extended from the bytes read. </summary>
        internal int ReadInteger()
        {
            var value = 0;
            var i = 0;
            while (ReadBit() && i < 4)
            {
                value |= (int)ReadBits(8) << (8 * i);
                i++;
            }
            if (i > 0)
            {
                value <<= (4 - i) * 8;
                value >>= (4 - i) * 8;
            }
            return value;
        }

        internal string ReadString()
        {
            if (!ReadBit()) return "";
            var length = ReadUnsignedInteger();
            if (length > PRCFileData.MaxStringLength)
                throw new InvalidDataException($"Invalid PRC data: string of {length} characters exceeds the limit.");
            var chars = new char[length];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)ReadBits(8);
            return new string(chars);
        }

        /// <summary> Skips user data, returning its size in bits. </summary>
        internal uint ReadUserData()
        {
            var bits = ReadUnsignedInteger();
            if (bits > PRCFileData.MaxUserDataBits)
                throw new InvalidDataException($"Invalid PRC data: {bits} bits of user data exceed the limit.");
            if (bits > RemainingBits)
                throw new EndOfStreamException("The PRC section ended unexpectedly.");
            position += bits;
            return bits;
        }

        internal float ReadFloatAsBytes()
        {
            Span<byte> bytes = stackalloc byte[4];
            for (var i = 0; i < 4; i++) bytes[i] = ReadUnsignedCharacter();
            return BinaryPrimitives.ReadSingleLittleEndian(bytes);
        }

        internal uint ReadNumberOfBitsThenUnsignedInteger() => ReadBits((int)ReadBits(5));

        /// <summary> A sign bit, then the magnitude in <paramref name="width"/> - 1 bits. </summary>
        internal int ReadIntegerWithVariableBitNumber(int width)
        {
            if (width < 1 || width > 32)
                throw new InvalidDataException($"Invalid PRC data: integer width of {width} bits.");
            var negative = ReadBit();
            var magnitude = width == 1 ? 0 : (int)ReadBits(width - 1);
            return negative ? -magnitude : magnitude;
        }

        internal bool[] ReadUncompressedBoolArray(long count)
        {
            if (count < 0 || count > RemainingBits || count > PRCFileData.MaxArrayElements)
                throw new InvalidDataException($"Invalid PRC data: Boolean array of {count} elements.");
            var values = new bool[count];
            for (var i = 0; i < values.Length; i++) values[i] = ReadBit();
            return values;
        }

        /// <summary>
        /// An array of characters of <paramref name="bitsPerElement"/> bits, stored directly or
        /// Huffman coded (ISO 14739-1 WriteCharacterArray). Whether it is coded is a stored bit unless
        /// <paramref name="hasCompressedBit"/> is false, when <paramref name="compressed"/> says.
        /// </summary>
        internal sbyte[] ReadCharacterArray(int bitsPerElement, bool hasCompressedBit = true, bool compressed = true,
            bool signExtend = false)
        {
            if (bitsPerElement < 1 || bitsPerElement > 8)
                throw new InvalidDataException($"Invalid PRC data: {bitsPerElement} bits per character.");
            if (hasCompressedBit) compressed = ReadBit();
            if (!compressed)
            {
                var direct = new sbyte[CheckedCount(ReadUnsignedInteger(), 8)];
                for (var i = 0; i < direct.Length; i++) direct[i] = (sbyte)ReadUnsignedCharacter();
                return direct;
            }
            return Array.ConvertAll(ReadHuffmanArray(bitsPerElement, signExtend), v => (sbyte)v);
        }

        /// <summary> Like <see cref="ReadCharacterArray"/> for elements of up to 16 bits (WriteShortArray). </summary>
        internal short[] ReadShortArray(int bitsPerElement)
        {
            if (bitsPerElement < 1 || bitsPerElement > 16)
                throw new InvalidDataException($"Invalid PRC data: {bitsPerElement} bits per short.");
            if (!ReadBit())
            {
                var direct = new short[CheckedCount(ReadUnsignedInteger(), 16)];
                for (var i = 0; i < direct.Length; i++) direct[i] = (short)ReadUnsignedShort();
                return direct;
            }
            return Array.ConvertAll(ReadHuffmanArray(bitsPerElement, true), v => (short)v);
        }

        /// <summary> Integers stored with their own bit widths, the widths in a 6-bit character array. </summary>
        internal int[] ReadCompressedIntegerArray()
        {
            var widths = ReadCharacterArray(6, signExtend: true);
            var values = new int[widths.Length];
            for (var i = 0; i < values.Length; i++) values[i] = ReadIntegerWithVariableBitNumber(widths[i]);
            return values;
        }

        /// <summary>
        /// Non-negative indices stored as differences from the previous one, each difference's bit width
        /// stored as a change from the previous width (ISO 14739-1 WriteCompressedIndiceArray).
        /// </summary>
        internal int[] ReadCompressedIndiceArray(bool hasCompressedBit = true, bool compressed = true)
        {
            var widthChanges = ReadCharacterArray(6, hasCompressedBit, compressed, true);
            var indices = new int[widthChanges.Length];
            var width = 0;
            for (var i = 0; i < indices.Length; i++)
            {
                width += widthChanges[i];
                var value = ReadIntegerWithVariableBitNumber(width);
                var index = i == 0 ? value : (long)indices[i - 1] + value;
                if (index < 0 || index > int.MaxValue)
                    throw new InvalidDataException("Invalid PRC data: compressed index out of range.");
                indices[i] = (int)index;
            }
            return indices;
        }

        private long CheckedCount(uint count, int bitsPerElement)
        {
            if (count > PRCFileData.MaxArrayElements || (long)count * bitsPerElement > RemainingBits)
                throw new InvalidDataException($"Invalid PRC data: array of {count} elements is too long.");
            return count;
        }

        /// <summary>
        /// A Huffman-coded array: the coded words, then the bits used in the last word. The words are a
        /// separate bit stream, read least significant bit first: the number of leaves, the longest code
        /// length, each leaf's symbol, code length and code (most significant bit nearest the root), the
        /// element count and then the codes (ISO 14739-1 §12.2; read side per pdf-issues #776).
        /// </summary>
        private int[] ReadHuffmanArray(int bitsPerElement, bool signExtend)
        {
            var wordCount = ReadUnsignedInteger();
            if (wordCount == 0) return [];
            if ((long)wordCount * 32 > RemainingBits)
                throw new InvalidDataException("Invalid PRC data: Huffman array is longer than the remaining data.");
            var bytes = new byte[wordCount * 4];
            for (var i = 0; i < bytes.Length; i++) bytes[i] = ReadUnsignedCharacter();
            if (ReadUnsignedInteger() > 32)
                throw new InvalidDataException("Invalid PRC data: more than 32 bits used in the last Huffman word.");
            return HuffmanDecode(bytes, bitsPerElement, signExtend);
        }

        private static int[] HuffmanDecode(byte[] bytes, int bitsPerElement, bool signExtend)
        {
            var stream = new LsbFirstReader(bytes);
            var leafCount = stream.Read(bitsPerElement + 1);
            if (leafCount == 0) return [];
            var maxCodeLength = (int)stream.Read(8);
            if (maxCodeLength < 1 || maxCodeLength > 32)
                throw new InvalidDataException($"Invalid PRC data: Huffman code length limit of {maxCodeLength}.");
            var leaves = new (int Symbol, int Length, uint Code)[leafCount];
            for (var i = 0; i < leaves.Length; i++)
            {
                var raw = stream.Read(bitsPerElement);
                var symbol = signExtend
                    ? (int)(raw << (32 - bitsPerElement)) >> (32 - bitsPerElement)
                    : (int)raw;
                var length = (int)(byte)stream.Read(maxCodeLength);
                if (length > 32)
                    throw new InvalidDataException($"Invalid PRC data: Huffman code of {length} bits.");
                leaves[i] = (symbol, length, stream.Read(length));
            }

            // Trie as child arrays; node 0 is the root. A leaf has no children.
            var children = new List<int[]> { new[] { -1, -1 } };
            var symbols = new List<int> { 0 };
            foreach (var (symbol, length, code) in leaves.OrderBy(l => l.Code))
            {
                if (length == 0)
                {
                    symbols[0] = symbol;
                    continue;
                }
                var node = 0;
                for (var bit = length - 1; bit >= 0; bit--)
                {
                    var branch = (int)((code >> bit) & 1);
                    if (children[node][branch] < 0)
                    {
                        children[node][branch] = children.Count;
                        children.Add(new[] { -1, -1 });
                        symbols.Add(0);
                    }
                    else if (bit == 0 || IsLeaf(children[children[node][branch]]))
                        throw new InvalidDataException("Invalid PRC data: Huffman codes are not prefix-free.");
                    node = children[node][branch];
                }
                symbols[node] = symbol;
            }

            var count = stream.Read(32);
            var rootIsLeaf = IsLeaf(children[0]);
            if (count > PRCFileData.MaxArrayElements || (!rootIsLeaf && count > stream.RemainingBits))
                throw new InvalidDataException($"Invalid PRC data: Huffman array of {count} elements.");
            var values = new int[count];
            for (var i = 0; i < values.Length; i++)
            {
                var node = 0;
                while (!IsLeaf(children[node]))
                {
                    node = children[node][stream.ReadBit() ? 1 : 0];
                    if (node < 0)
                        throw new InvalidDataException("Invalid PRC data: Huffman code not in the table.");
                }
                values[i] = symbols[node];
            }
            return values;

            static bool IsLeaf(int[] node) => node[0] < 0 && node[1] < 0;
        }

        /// <summary> Reads bits least significant first within each byte, values least significant bit first. </summary>
        private sealed class LsbFirstReader(byte[] bytes)
        {
            private long position;
            internal long RemainingBits => bytes.LongLength * 8 - position;

            internal bool ReadBit()
            {
                if (position >= bytes.LongLength * 8)
                    throw new EndOfStreamException("A PRC Huffman array ended unexpectedly.");
                var bit = (bytes[position >> 3] >> (int)(position & 7)) & 1;
                position++;
                return bit != 0;
            }

            internal uint Read(int count)
            {
                var value = 0U;
                for (var i = 0; i < count; i++)
                    if (ReadBit()) value |= 1U << i;
                return value;
            }
        }

        /// <summary>
        /// A compressed double: a prefix code for a frequent value or an exponent, then the sign and,
        /// for an exponent, the mantissa bytes, each stored or copied from a byte already read.
        /// </summary>
        internal double ReadDouble()
        {
            var code = 0U;
            PrcDoubleCodes.Code? match = null;
            for (var length = 1; length <= PrcDoubleCodes.MaxCodeLength; length++)
            {
                code = (code << 1) | (ReadBit() ? 1U : 0U);
                if (PrcDoubleCodes.TryGet(length, code, out var found))
                {
                    match = found;
                    break;
                }
            }
            if (match is not { } coding)
                throw new InvalidDataException("Invalid PRC data: unknown compressed double code.");
            if (coding.IsZero) return 0.0;

            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, coding.Bits);
            if (ReadBit()) bytes[7] |= 0x80;
            else bytes[7] &= 0x7F;
            if (!coding.IsExponent || !ReadBit())
                return BinaryPrimitives.ReadDoubleLittleEndian(bytes);

            bytes[6] |= (byte)(ReadBits(4) & 0x0F);
            var last = 0;
            var current = 5;
            while (current >= last)
            {
                if (ReadBit())
                    bytes[current] = (byte)ReadBits(8);
                else
                {
                    var offset = (int)ReadBits(3);
                    if (offset == 0)
                    {
                        var repeated = bytes[current + 1];
                        while (current >= last) bytes[current--] = repeated;
                        break;
                    }
                    if (offset == 6)
                    {
                        var repeated = bytes[current + 1];
                        last++;
                        while (current >= last) bytes[current--] = repeated;
                        bytes[current] = (byte)ReadBits(8);
                        break;
                    }
                    if (current + offset >= 8)
                        throw new InvalidDataException("Invalid PRC data: compressed double copies outside its bytes.");
                    bytes[current] = bytes[current + offset];
                }
                current--;
            }
            return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
        }
    }

    /// <summary>
    /// The prefix codes of PRC compressed doubles (prc-rs double.rs, from the PRC specification):
    /// 29 frequent values and one code per IEEE exponent.
    /// </summary>
    internal static class PrcDoubleCodes
    {
        internal const int MaxCodeLength = 22;

        internal readonly record struct Code(bool IsExponent, bool IsZero, ulong Bits);

        // Exponent codes as runs of consecutive codes for consecutive exponents:
        // (code length, first code, run length, high word of the first value).
        private static readonly uint[] ExponentRuns =
        [
            22, 0xd1d32, 2, 0x0, 22, 0xf78d8, 40, 0x200000, 22, 0x3a8300, 810, 0x2a00000,
            18, 0xf787, 1, 0x35400000, 18, 0xd1d4, 1, 0x35500000, 19, 0x77f9f, 1, 0x35600000,
            18, 0x3b8fb, 1, 0x35700000, 19, 0x1ef1a, 1, 0x35800000, 21, 0x1d4315, 1, 0x35900000,
            16, 0x3de2, 1, 0x35a00000, 22, 0x3a862c, 13, 0x35b00000, 21, 0x1d431d, 1, 0x36800000,
            22, 0x3a8639, 1, 0x36900000, 22, 0x3a863c, 1, 0x36a00000, 16, 0x3de0, 1, 0x36b00000,
            18, 0x3a95e, 1, 0x36c00000, 21, 0x1d431f, 1, 0x36d00000, 22, 0x3a863d, 1, 0x36e00000,
            22, 0x3a8640, 1, 0x36f00000, 20, 0x34749, 1, 0x37000000, 20, 0x3474d, 1, 0x37100000,
            22, 0x3a8641, 29, 0x37200000, 21, 0x1d432f, 1, 0x38f00000, 22, 0x3a8660, 3, 0x39000000,
            21, 0x1d4332, 1, 0x39300000, 18, 0xd1d5, 1, 0x39400000, 18, 0x3bfda, 1, 0x39500000,
            15, 0x7528, 2, 0x39600000, 18, 0x3a95f, 1, 0x39800000, 17, 0x1d434, 1, 0x39900000,
            19, 0x750cd, 1, 0x39a00000, 18, 0x3a867, 1, 0x39b00000, 19, 0x771fd, 1, 0x39c00000,
            15, 0x7764, 1, 0x39d00000, 20, 0xee3f9, 1, 0x39e00000, 18, 0x3bfdb, 1, 0x39f00000,
            16, 0xeff7, 1, 0x3a000000, 18, 0xd1d6, 1, 0x3a100000, 22, 0x3a8663, 1, 0x3a200000,
            22, 0x3a8666, 1, 0x3a300000, 18, 0x3b8fa, 1, 0x3a400000, 17, 0x1d435, 1, 0x3a500000,
            17, 0x1dfe4, 1, 0x3a600000, 19, 0x750d8, 1, 0x3a700000, 18, 0x3a95b, 1, 0x3a800000,
            19, 0x77f9e, 1, 0x3a900000, 19, 0x750d9, 1, 0x3aa00000, 18, 0xd1d7, 1, 0x3ab00000,
            18, 0x3b8ff, 1, 0x3ac00000, 17, 0x1dc7c, 1, 0x3ad00000, 19, 0x750da, 1, 0x3ae00000,
            17, 0x7bc2, 1, 0x3af00000, 18, 0x3bfca, 1, 0x3b000000, 19, 0x1a3a5, 1, 0x3b100000,
            17, 0x1d4ac, 1, 0x3b200000, 18, 0x3a86e, 1, 0x3b300000, 17, 0x1d438, 1, 0x3b400000,
            18, 0x3bfcb, 1, 0x3b500000, 19, 0x1a3a7, 1, 0x3b600000, 17, 0x1d4ae, 1, 0x3b700000,
            18, 0x3bfce, 1, 0x3b800000, 18, 0xf78c, 1, 0x3b900000, 17, 0x1dfec, 1, 0x3ba00000,
            17, 0x1d439, 1, 0x3bb00000, 17, 0x68e8, 1, 0x3bc00000, 18, 0xf786, 1, 0x3bd00000,
            15, 0x771e, 1, 0x3be00000, 17, 0x1dfe6, 1, 0x3bf00000, 15, 0x77f8, 1, 0x3c000000,
            14, 0x3bb3, 1, 0x3c100000, 14, 0x3b8e, 1, 0x3c200000, 14, 0x3a82, 1, 0x3c300000,
            14, 0x3b96, 1, 0x3c400000, 13, 0x68f, 1, 0x3c500000, 12, 0x3d4, 1, 0x3c600000,
            13, 0x1dca, 1, 0x3c700000, 12, 0x346, 1, 0x3c800000, 12, 0xee7, 1, 0x3c900000,
            12, 0xeea, 1, 0x3ca00000, 11, 0x1ed, 1, 0x3cb00000, 12, 0x3df, 1, 0x3cc00000,
            11, 0x1a2, 1, 0x3cd00000, 11, 0x56f, 1, 0x3ce00000, 11, 0xb9, 1, 0x3cf00000,
            13, 0x7b2, 1, 0x3d000000, 13, 0x1dd8, 1, 0x3d100000, 13, 0x15ba, 1, 0x3d200000,
            12, 0xee6, 1, 0x3d300000, 13, 0x15b8, 1, 0x3d400000, 14, 0xf79, 1, 0x3d500000,
            14, 0x3a81, 1, 0x3d600000, 14, 0xd1c, 1, 0x3d700000, 15, 0x7765, 1, 0x3d800000,
            14, 0xf54, 1, 0x3d900000, 13, 0x15b9, 1, 0x3da00000, 13, 0x7ab, 1, 0x3db00000,
            15, 0x7500, 1, 0x3dc00000, 15, 0x1eaa, 1, 0x3dd00000, 15, 0x7501, 1, 0x3de00000,
            15, 0x1eab, 1, 0x3df00000, 14, 0x3b97, 1, 0x3e000000, 15, 0x752a, 1, 0x3e100000,
            15, 0x77fa, 1, 0x3e200000, 14, 0x3a93, 1, 0x3e300000, 13, 0x1dc6, 1, 0x3e400000,
            13, 0x7bd, 1, 0x3e500000, 13, 0x1dff, 1, 0x3e600000, 12, 0xefe, 1, 0x3e700000,
            12, 0xeed, 1, 0x3e800000, 11, 0xb8, 1, 0x3e900000, 12, 0x3d8, 1, 0x3ea00000,
            11, 0x1eb, 1, 0x3eb00000, 13, 0x1d4b, 1, 0x3ec00000, 13, 0x7b3, 1, 0x3ed00000,
            10, 0x5d, 1, 0x3ee00000, 12, 0xeeb, 1, 0x3ef00000, 11, 0x1ee, 1, 0x3f000000,
            10, 0x5f, 1, 0x3f100000, 10, 0x2b6, 1, 0x3f200000, 9, 0x1de, 1, 0x3f300000,
            8, 0xe8, 1, 0x3f400000, 8, 0xae, 1, 0x3f500000, 7, 0x1b, 1, 0x3f600000, 7, 0x76, 1, 0x3f700000,
            7, 0xa, 1, 0x3f800000, 6, 0x8, 1, 0x3f900000, 6, 0xe, 1, 0x3fa00000, 6, 0x4, 1, 0x3fb00000,
            6, 0xc, 1, 0x3fc00000, 5, 0x3, 1, 0x3fd00000, 4, 0x8, 1, 0x3fe00000, 5, 0x13, 1, 0x3ff00000,
            5, 0x1b, 1, 0x40000000, 5, 0x17, 1, 0x40100000, 5, 0x12, 1, 0x40200000, 5, 0x19, 2, 0x40300000,
            5, 0x18, 1, 0x40500000, 5, 0x1c, 1, 0x40600000, 5, 0x14, 1, 0x40700000, 5, 0x5, 1, 0x40800000,
            8, 0x16, 1, 0x40900000, 12, 0xee2, 1, 0x40a00000, 12, 0xee4, 1, 0x40b00000,
            8, 0xac, 1, 0x40c00000, 13, 0x15bb, 1, 0x40d00000, 22, 0x3a8667, 1, 0x40e00000,
            22, 0x3a86d8, 3, 0x40f00000, 17, 0x1dc7e, 1, 0x41200000, 22, 0x3a86db, 5, 0x41300000,
            22, 0x3a86f0, 16, 0x41800000, 22, 0x3a8740, 25, 0x42800000, 15, 0x1a3b, 1, 0x44100000,
            22, 0x3a8759, 167, 0x44200000, 22, 0x3a9000, 768, 0x4e900000, 22, 0x3a95a0, 16, 0x7e900000,
            22, 0x3b8fe0, 4, 0x7f900000, 21, 0x68e90, 2, 0x7fd00000, 21, 0x68e98, 1, 0x7ff80000
        ];

        // Frequent values: (code length, code, IEEE bits).
        private static readonly (int Length, uint Code, ulong Bits)[] FrequentValues =
        [
            (2, 0x1, 0x0000000000000000UL), (10, 0xf4, 0x3e35798ee2308c3aUL), (11, 0x77c, 0x3e45798ee2308c3aUL),
            (8, 0xaf, 0x3e8ad7f29abcaf4aUL), (9, 0x1d2, 0x3ec0c6f7a0b5ed8eUL), (10, 0xd0, 0x3f454c985f06f694UL),
            (6, 0x9, 0x3f4a36e2eb1c432dUL), (4, 0xf, 0x3f50624dd2f1a9fcUL), (5, 0x16, 0x3f60624dd2f1a9fcUL),
            (11, 0x751, 0x3fbe69ad42c3c9eeUL), (11, 0x777, 0x3fe0000000000000UL), (9, 0x1d6, 0x3feffffff8000002UL),
            (4, 0x0, 0x3ff0000000000000UL), (9, 0x15a, 0x401921fb54442d18UL), (11, 0x774, 0x4035ee1480000000UL),
            (9, 0x1d3, 0x404ca5dc1a63c1f8UL), (11, 0x77e, 0x405bb32fe0000000UL), (10, 0x5e, 0x405c332fe0000000UL),
            (9, 0x1d7, 0x4066800000000000UL), (9, 0x1d5, 0x4076800000000000UL), (11, 0x77d, 0x408f400000000000UL),
            (10, 0xd2, 0x409233ffffffffffUL), (8, 0x3c, 0x4092340000000000UL), (11, 0x753, 0x4092340000000001UL),
            (10, 0xd3, 0x4092abffffffffffUL), (8, 0x35, 0x4092ac0000000000UL), (11, 0x770, 0x4092ac0000000001UL),
            (7, 0x1f, 0x40c81c8000000000UL), (6, 0x2a, 0x41cdcd64ff800000UL)
        ];

        private static readonly Dictionary<ulong, Code> codes = BuildCodes();

        private static ulong Key(int length, uint code) => ((ulong)length << 32) | code;

        private static Dictionary<ulong, Code> BuildCodes()
        {
            var table = new Dictionary<ulong, Code>();
            foreach (var (length, code, bits) in FrequentValues)
                table.Add(Key(length, code), new Code(false, length == 2 && code == 1, bits));
            for (var i = 0; i < ExponentRuns.Length; i += 4)
                for (var k = 0U; k < ExponentRuns[i + 2]; k++)
                    table.Add(Key((int)ExponentRuns[i], ExponentRuns[i + 1] + k),
                        new Code(true, false, (ulong)(ExponentRuns[i + 3] + 0x100000U * k) << 32));
            return table;
        }

        internal static bool TryGet(int length, uint code, out Code coding) => codes.TryGetValue(Key(length, code), out coding);
    }

    /// <summary>
    /// Evaluates the schemas a PRC file carries for entities written by newer versions, reading the
    /// extra data they describe so the stream stays aligned (prc-rs schema.rs).
    /// </summary>
    internal sealed class PrcSchemaEvaluator
    {
        private const uint StoredVersion = PRCFileData.SupportedVersion;
        private const int MaxDepth = 256;
        private const long MaxInstructions = 50_000_000;

        // Schema tokens
        private const uint DataBoolean = 0, DataDouble = 1, DataCharacter = 2, DataUnsignedInteger = 3,
            DataInteger = 4, DataString = 5, FatherType = 6, Vector2D = 7, Vector3D = 8, Extent1D = 9,
            Extent2D = 10, Extent3D = 11, PtrSurface = 13, PtrCurve = 14, For = 15, SimpleFor = 16, If = 17,
            Else = 18, BlockStart = 19, BlockVersion = 20, BlockEnd = 21, ValueDeclareAndSet = 24, Value = 25,
            ValueConstant = 26, OperatorEqual = 37, ObsoleteToken39 = 39, ObsoleteToken40 = 40;

        private readonly Dictionary<uint, uint[]> operationsPerType = [];
        private long instructions;

        internal PrcSchemaEvaluator(PrcNode schema)
        {
            foreach (var definition in schema.Nodes("schemas"))
                operationsPerType[definition.UInt("entity_type")] = definition.UInts("schema_tokens");
        }

        private enum ValueKind { Scalar, Text, Vector2, Vector3 }

        /// <summary> A value on the data stack: a scalar, a string or a 2D/3D vector. </summary>
        private readonly record struct SchemaValue(ValueKind Kind, double X, double Y = 0, double Z = 0, string Text = null)
        {
            internal static SchemaValue Of(double x) => new(ValueKind.Scalar, x);

            /// <summary> Scalars compare by value whatever their type, as in prc-rs. </summary>
            internal bool Matches(SchemaValue other)
                => Kind == other.Kind && Kind switch
                {
                    ValueKind.Scalar => X == other.X,
                    ValueKind.Text => Text == other.Text,
                    ValueKind.Vector2 => X == other.X && Y == other.Y,
                    _ => X == other.X && Y == other.Y && Z == other.Z
                };
        }

        private sealed class State
        {
            internal List<uint> Operations = [];
            internal List<SchemaValue> Data = [];
            internal Dictionary<uint, SchemaValue> Variables = [];
            internal int Depth;

            internal uint Pop()
            {
                if (Operations.Count == 0)
                    throw new InvalidDataException("Invalid PRC data: schema ends unexpectedly.");
                var op = Operations[^1];
                Operations.RemoveAt(Operations.Count - 1);
                return op;
            }

            internal bool NextIs(uint token) => Operations.Count > 0 && Operations[^1] == token;

            internal SchemaValue PopData()
            {
                if (Data.Count == 0)
                    throw new InvalidDataException("Invalid PRC data: schema data stack is empty.");
                var value = Data[^1];
                Data.RemoveAt(Data.Count - 1);
                return value;
            }
        }

        internal void Evaluate(PrcBitReader reader, uint type) => Evaluate(reader, type, false, 0);

        private void Evaluate(PrcBitReader reader, uint type, bool skip, int depth)
        {
            if (!operationsPerType.TryGetValue(type, out var operations)) return;
            var state = new State { Depth = depth };
            for (var i = operations.Length - 1; i >= 0; i--) state.Operations.Add(operations[i]);
            while (state.Operations.Count > 0)
                Step(reader, state, skip);
        }

        private void Step(PrcBitReader reader, State s, bool skip)
        {
            if (s.Operations.Count == 0) return;
            if (++instructions > MaxInstructions)
                throw new InvalidDataException("Invalid PRC data: schema evaluation exceeds its instruction budget.");
            if (++s.Depth > MaxDepth)
                throw new InvalidDataException("Invalid PRC data: schema is nested too deeply.");
            var instruction = s.Pop();
            switch (instruction)
            {
                case DataBoolean:
                    if (!skip) s.Data.Add(SchemaValue.Of(reader.ReadBoolean() ? 1 : 0));
                    break;
                case DataDouble:
                    if (!skip) s.Data.Add(SchemaValue.Of(reader.ReadDouble()));
                    break;
                case DataCharacter:
                    if (!skip) s.Data.Add(SchemaValue.Of(reader.ReadUnsignedCharacter()));
                    break;
                case DataUnsignedInteger:
                    if (!skip) s.Data.Add(SchemaValue.Of(reader.ReadUnsignedInteger()));
                    break;
                case DataInteger:
                    if (!skip) s.Data.Add(SchemaValue.Of(reader.ReadInteger()));
                    break;
                case DataString:
                    if (!skip) s.Data.Add(new SchemaValue(ValueKind.Text, 0, Text: reader.ReadString()));
                    break;
                case FatherType:
                    var fatherType = s.Pop();
                    if (!skip) Evaluate(reader, fatherType, false, s.Depth);
                    break;
                case Vector2D:
                case Extent1D:
                    if (!skip) s.Data.Add(new SchemaValue(ValueKind.Vector2, reader.ReadDouble(), reader.ReadDouble()));
                    break;
                case Vector3D:
                    if (!skip)
                        s.Data.Add(new SchemaValue(ValueKind.Vector3, reader.ReadDouble(), reader.ReadDouble(),
                            reader.ReadDouble()));
                    break;
                case Extent2D:
                    if (!skip)
                    {
                        s.Data.Add(new SchemaValue(ValueKind.Vector2, reader.ReadDouble(), reader.ReadDouble()));
                        s.Data.Add(new SchemaValue(ValueKind.Vector2, reader.ReadDouble(), reader.ReadDouble()));
                    }
                    break;
                case Extent3D:
                    if (!skip)
                    {
                        s.Data.Add(new SchemaValue(ValueKind.Vector3, reader.ReadDouble(), reader.ReadDouble(),
                            reader.ReadDouble()));
                        s.Data.Add(new SchemaValue(ValueKind.Vector3, reader.ReadDouble(), reader.ReadDouble(),
                            reader.ReadDouble()));
                    }
                    break;
                case PtrSurface:
                case PtrCurve:
                    if (!skip)
                        throw new NotSupportedException("PRC schemas that embed exact curves or surfaces are not supported.");
                    break;
                case For:
                    if (skip)
                    {
                        Step(reader, s, true); // iteration count
                        Step(reader, s, true); // body
                    }
                    else
                    {
                        Step(reader, s, false);
                        Loop(reader, s, (int)s.PopData().X);
                    }
                    break;
                case SimpleFor:
                    if (skip) Step(reader, s, true);
                    else Loop(reader, s, reader.ReadInteger());
                    break;
                case If:
                    if (skip)
                    {
                        Step(reader, s, true); // condition
                        Step(reader, s, true); // then
                        if (s.NextIs(Else)) Step(reader, s, true);
                    }
                    else
                    {
                        Step(reader, s, false);
                        var condition = s.PopData();
                        var isTrue = condition.Kind == ValueKind.Scalar && condition.X != 0;
                        Step(reader, s, !isTrue); // then
                        if (s.NextIs(Else)) Step(reader, s, isTrue);
                    }
                    break;
                case Else:
                    Step(reader, s, skip);
                    break;
                case ValueDeclareAndSet:
                    var declared = s.Pop();
                    Step(reader, s, skip);
                    if (!skip) s.Variables[declared] = s.PopData();
                    break;
                case Value:
                    var variable = s.Pop();
                    if (!skip)
                    {
                        if (!s.Variables.TryGetValue(variable, out var value))
                            throw new InvalidDataException($"Invalid PRC data: schema reads undeclared variable {variable}.");
                        s.Data.Add(value);
                    }
                    break;
                case ValueConstant:
                    var constant = s.Pop();
                    if (!skip) s.Data.Add(SchemaValue.Of(constant));
                    break;
                case OperatorEqual:
                    Step(reader, s, skip); // left
                    Step(reader, s, skip); // right
                    if (!skip)
                    {
                        var a = s.PopData();
                        var b = s.PopData();
                        s.Data.Add(SchemaValue.Of(a.Matches(b) ? 1 : 0));
                    }
                    break;
                case BlockStart:
                    while (!s.NextIs(BlockEnd))
                    {
                        if (s.Operations.Count == 0)
                            throw new InvalidDataException("Invalid PRC data: schema block is not closed.");
                        Step(reader, s, skip);
                    }
                    Step(reader, s, skip);
                    break;
                case BlockEnd:
                    break;
                case BlockVersion:
                    // Data written by versions newer than this reader is read; older data is already known.
                    var version = s.Pop();
                    var read = !skip && StoredVersion < version;
                    while (!s.NextIs(BlockEnd))
                    {
                        if (s.Operations.Count == 0)
                            throw new InvalidDataException("Invalid PRC data: schema block is not closed.");
                        Step(reader, s, !read);
                    }
                    Step(reader, s, skip);
                    break;
                case ObsoleteToken39:
                case ObsoleteToken40:
                    s.Pop(); // the obsolete token's argument is ignored
                    break;
                default:
                    // Declarations, assignments and operators other than equality are not implemented
                    // by prc-rs either, which ignores them the same way.
                    break;
            }
            s.Depth--;
        }

        /// <summary> Evaluates the next instruction or block <paramref name="count"/> times. </summary>
        private void Loop(PrcBitReader reader, State s, int count)
        {
            for (var i = 0; i < count - 1; i++)
            {
                var iteration = new State { Operations = [.. s.Operations], Depth = s.Depth };
                Step(reader, iteration, false);
                for (var k = iteration.Data.Count - 1; k >= 0; k--) s.Data.Add(iteration.Data[k]);
            }
            Step(reader, s, count <= 0);
        }
    }
}
