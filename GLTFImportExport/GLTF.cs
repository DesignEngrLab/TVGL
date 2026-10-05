using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVGL.GLTFImportExport
{
    /// <summary>
    /// Provides methods for importing and exporting 3D CAD and mesh models in glTF (.gltf)
    /// and binary glTF (.glb) formats into TVGL TessellatedSolids and SolidAssemblies.
    /// </summary>
    public static class GLTF
    {
        #region Open Methods

        /// <summary>
        /// Reads a glTF (.gltf) or binary glTF (.glb) file and returns the first TessellatedSolid found.
        /// </summary>
        /// <param name="filePath">Path to the .gltf or .glb file.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>The first imported TessellatedSolid, or null if no valid solids exist.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static TessellatedSolid? Open(
            string filePath,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var solids = OpenSolids(filePath, buildOptions);
            return solids.FirstOrDefault();
        }

        /// <summary>
        /// Reads a glTF or binary glTF stream and returns the first TessellatedSolid found.
        /// </summary>
        /// <param name="stream">The input stream containing glTF or GLB data.</param>
        /// <param name="buildOptions">Optional TVGL build options.</param>
        /// <param name="name">Optional name to assign to the imported solid.</param>
        /// <returns>The first imported TessellatedSolid, or null if no valid solids exist.</returns>
        public static TessellatedSolid? Open(
            Stream stream,
            TessellatedSolidBuildOptions? buildOptions = null,
            string name = "")
        {
            var solids = OpenSolids(stream, buildOptions, name);
            return solids.FirstOrDefault();
        }

        /// <summary>
        /// Reads a glTF (.gltf) or binary glTF (.glb) file and returns all distinct TessellatedSolids
        /// positioned in their global scene coordinates.
        /// </summary>
        /// <param name="filePath">Path to the .gltf or .glb file.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>A list of imported TessellatedSolids.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static List<TessellatedSolid> OpenSolids(
            string filePath,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var model = GLTFReconstructor.LoadModel(filePath);
            var fileName = Path.GetFileName(filePath);
            return GLTFReconstructor.ReconstructSolids(model, fileName, buildOptions);
        }

        /// <summary>
        /// Reads a glTF or binary glTF stream and returns all distinct TessellatedSolids
        /// positioned in their global scene coordinates.
        /// </summary>
        /// <param name="stream">The input stream containing glTF or GLB data.</param>
        /// <param name="buildOptions">Optional TVGL build options.</param>
        /// <param name="name">Optional name associated with the stream source.</param>
        /// <returns>A list of imported TessellatedSolids.</returns>
        public static List<TessellatedSolid> OpenSolids(
            Stream stream,
            TessellatedSolidBuildOptions? buildOptions = null,
            string name = "")
        {
            var model = GLTFReconstructor.LoadModel(stream);
            return GLTFReconstructor.ReconstructSolids(model, name, buildOptions);
        }

        /// <summary>
        /// Reads a glTF (.gltf) or binary glTF (.glb) file and packages all contained solids
        /// into a TVGL SolidAssembly preserving instancing and node transforms.
        /// </summary>
        /// <param name="filePath">Path to the .gltf or .glb file.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>A SolidAssembly containing all imported solids.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static SolidAssembly OpenSolidAssembly(
            string filePath,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var model = GLTFReconstructor.LoadModel(filePath);
            var fileName = Path.GetFileName(filePath);
            return GLTFReconstructor.ReconstructSolidAssembly(model, fileName, buildOptions);
        }

        /// <summary>
        /// Reads a glTF or binary glTF stream and packages all contained solids
        /// into a TVGL SolidAssembly preserving instancing and node transforms.
        /// </summary>
        /// <param name="stream">The input stream containing glTF or GLB data.</param>
        /// <param name="buildOptions">Optional TVGL build options.</param>
        /// <param name="name">Optional name associated with the stream source.</param>
        /// <returns>A SolidAssembly containing all imported solids.</returns>
        public static SolidAssembly OpenSolidAssembly(
            Stream stream,
            TessellatedSolidBuildOptions? buildOptions = null,
            string name = "")
        {
            var model = GLTFReconstructor.LoadModel(stream);
            return GLTFReconstructor.ReconstructSolidAssembly(model, name, buildOptions);
        }

        #endregion

        #region Save Methods

        /// <summary>
        /// Writes a single TVGL Solid to a glTF (.gltf) or binary glTF (.glb) file.
        /// Format is chosen automatically based on the file extension.
        /// </summary>
        /// <param name="filePath">The target file path (.glb or .gltf).</param>
        /// <param name="solid">The solid to write.</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(string filePath, Solid solid)
        {
            return GLTFWriter.Save(filePath, solid);
        }

        /// <summary>
        /// Writes a single TVGL Solid to a stream in binary glTF (.glb) format.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="solid">The solid to write.</param>
        /// <param name="asBinary">Whether to write as binary GLB (default is true).</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(Stream stream, Solid solid, bool asBinary = true)
        {
            return GLTFWriter.Save(stream, solid, asBinary);
        }

        /// <summary>
        /// Writes multiple TVGL Solids to a glTF (.gltf) or binary glTF (.glb) file.
        /// Format is chosen automatically based on the file extension.
        /// </summary>
        /// <param name="filePath">The target file path (.glb or .gltf).</param>
        /// <param name="solids">The solids to write.</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(string filePath, IEnumerable<Solid> solids)
        {
            return GLTFWriter.Save(filePath, solids);
        }

        /// <summary>
        /// Writes multiple TVGL Solids to a stream in binary glTF (.glb) format.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="solids">The solids to write.</param>
        /// <param name="asBinary">Whether to write as binary GLB (default is true).</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(Stream stream, IEnumerable<Solid> solids, bool asBinary = true)
        {
            return GLTFWriter.Save(stream, solids, asBinary);
        }

        /// <summary>
        /// Writes a TVGL SolidAssembly to a glTF (.gltf) or binary glTF (.glb) file,
        /// preserving subassembly hierarchies, node transforms, and part instancing.
        /// </summary>
        /// <param name="filePath">The target file path (.glb or .gltf).</param>
        /// <param name="solidAssembly">The assembly to write.</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(string filePath, SolidAssembly solidAssembly)
        {
            return GLTFWriter.Save(filePath, solidAssembly);
        }

        /// <summary>
        /// Writes a TVGL SolidAssembly to a stream in binary glTF (.glb) format.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="solidAssembly">The assembly to write.</param>
        /// <param name="asBinary">Whether to write as binary GLB (default is true).</param>
        /// <returns>True when saved successfully; otherwise, false.</returns>
        public static bool Save(Stream stream, SolidAssembly solidAssembly, bool asBinary = true)
        {
            return GLTFWriter.Save(stream, solidAssembly, asBinary);
        }

        #endregion
    }
}

