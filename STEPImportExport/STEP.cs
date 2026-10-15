using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVGL.STEPImportExport
{
    /// <summary>
    /// Provides methods for importing 3D CAD models from STEP (ISO 10303) files (.step, .stp)
    /// into TVGL TessellatedSolids and SolidAssemblies, preserving analytic primitives.
    /// </summary>
    public static class STEP
    {
        /// <summary>
        /// Default linear chordal deflection tolerance for tessellation (in model units, typically mm).
        /// </summary>
        public const double DefaultLinearDeflection = 0.1;

        /// <summary>
        /// Default angular deflection tolerance in radians (approx. 28.6 degrees).
        /// </summary>
        public const double DefaultAngularDeflection = 0.5;

        /// <summary>
        /// Reads a STEP file and returns the first TessellatedSolid found.
        /// </summary>
        /// <param name="filePath">Path to the .step or .stp file.</param>
        /// <param name="linearDeflection">Linear deflection (chordal error) tolerance for meshing.</param>
        /// <param name="angularDeflection">Angular deflection tolerance for meshing in radians.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>The first imported TessellatedSolid, or null if no valid solids exist.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static TessellatedSolid? Open(
            string filePath,
            double linearDeflection = DefaultLinearDeflection,
            double angularDeflection = DefaultAngularDeflection,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var solids = OpenSolids(filePath, linearDeflection, angularDeflection, buildOptions);
            return solids.FirstOrDefault();
        }

        /// <summary>
        /// Reads a STEP file and returns all distinct TessellatedSolids contained within it.
        /// </summary>
        /// <param name="filePath">Path to the .step or .stp file.</param>
        /// <param name="linearDeflection">Linear deflection (chordal error) tolerance for meshing.</param>
        /// <param name="angularDeflection">Angular deflection tolerance for meshing in radians.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>An array of imported TessellatedSolids.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static TessellatedSolid[] OpenSolids(
            string filePath,
            double linearDeflection = DefaultLinearDeflection,
            double angularDeflection = DefaultAngularDeflection,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("STEP file not found.", filePath);

            var handle = NativeMethods.StepModel_Load(filePath, linearDeflection, angularDeflection);
            if (handle == IntPtr.Zero)
                return Array.Empty<TessellatedSolid>();

            try
            {
                var fileName = Path.GetFileName(filePath);
                return STEPReconstructor.ReconstructSolids(handle, fileName, buildOptions);
            }
            finally
            {
                NativeMethods.StepModel_Free(handle);
            }
        }

        /// <summary>
        /// Reads a STEP stream and returns all distinct TessellatedSolids contained within it.
        /// </summary>
        /// <param name="s">The input stream containing STEP data.</param>
        /// <param name="filePath">The source path or filename used to name the imported solids.</param>
        /// <param name="linearDeflection">Linear deflection (chordal error) tolerance for meshing.</param>
        /// <param name="angularDeflection">Angular deflection tolerance for meshing in radians.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>An array of imported TessellatedSolids.</returns>
        public static TessellatedSolid[] OpenSolids(Stream s, string filePath,
            double linearDeflection = DefaultLinearDeflection,
            double angularDeflection = DefaultAngularDeflection,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            ArgumentNullException.ThrowIfNull(s);

            var temporaryFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.step");
            try
            {
                using (var temporaryFile = new FileStream(
                    temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    s.CopyTo(temporaryFile);
                }

                var handle = NativeMethods.StepModel_Load(
                    temporaryFilePath, linearDeflection, angularDeflection);
                if (handle == IntPtr.Zero)
                    return Array.Empty<TessellatedSolid>();

                try
                {
                    var fileName = Path.GetFileName(filePath);
                    return STEPReconstructor.ReconstructSolids(handle, fileName, buildOptions);
                }
                finally
                {
                    NativeMethods.StepModel_Free(handle);
                }
            }
            finally
            {
                File.Delete(temporaryFilePath);
            }
        }

        /// <summary>
        /// Reads a STEP file and packages all contained solids into a TVGL SolidAssembly.
        /// </summary>
        /// <param name="filePath">Path to the .step or .stp file.</param>
        /// <param name="linearDeflection">Linear deflection (chordal error) tolerance for meshing.</param>
        /// <param name="angularDeflection">Angular deflection tolerance for meshing in radians.</param>
        /// <param name="buildOptions">Optional TVGL build options (e.g. repairs, duplicate checks).</param>
        /// <returns>A SolidAssembly containing all imported solids.</returns>
        /// <exception cref="FileNotFoundException">Thrown when filePath does not exist.</exception>
        public static SolidAssembly OpenSolidAssembly(
            string filePath,
            double linearDeflection = DefaultLinearDeflection,
            double angularDeflection = DefaultAngularDeflection,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var solids = OpenSolids(filePath, linearDeflection, angularDeflection, buildOptions);
            var fileName = Path.GetFileName(filePath);
            return new SolidAssembly(solids, fileName);
        }

        /// <summary>
        /// Creates a valid test STEP file (a box with a through-hole cylinder) for verification and testing.
        /// </summary>
        /// <param name="filePath">Target path to write the test .step file.</param>
        /// <returns>True on success, false on failure.</returns>
        public static bool CreateTestStepFile(string filePath)
        {
            return NativeMethods.StepModel_CreateTestStepFile(filePath);
        }
    }
}
