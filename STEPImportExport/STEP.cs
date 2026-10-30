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
        /// Reads STEP data from a stream and packages all bodies into a TVGL assembly.
        /// </summary>
        public static SolidAssembly OpenSolidAssembly(
            Stream stream,
            string filePath,
            double linearDeflection = DefaultLinearDeflection,
            double angularDeflection = DefaultAngularDeflection,
            TessellatedSolidBuildOptions? buildOptions = null)
        {
            var solids = OpenSolids(stream, filePath, linearDeflection, angularDeflection, buildOptions);
            return new SolidAssembly(solids, Path.GetFileName(filePath));
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

        /// <summary>
        /// Writes a TVGL solid to STEP using the currently supported faceted representation.
        /// </summary>
        /// <remarks>
        /// This is the symmetric file API for <see cref="Open(string, double, double, TessellatedSolidBuildOptions?)"/>.
        /// Use <see cref="SaveFaceted"/> when the representation choice should be explicit.
        /// </remarks>
        public static bool Save(TessellatedSolid solid, string filePath) => SaveHybrid(solid, filePath);

        /// <summary>
        /// Writes every tessellated instance in an assembly, in global assembly coordinates,
        /// as distinct STEP bodies.
        /// </summary>
        public static bool Save(SolidAssembly assembly, string filePath) => SaveHybrid(assembly, filePath);

        /// <summary>
        /// Writes a solid as STEP data to a caller-owned stream.
        /// </summary>
        public static bool Save(Stream stream, TessellatedSolid solid) =>
            SaveToStream(stream, temporaryPath =>
                SaveCore(new[] { solid }, temporaryPath, preserveAnalyticPrimitives: true, "model.step"));

        /// <summary>
        /// Writes an assembly as STEP data to a caller-owned stream.
        /// </summary>
        public static bool Save(Stream stream, SolidAssembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            var solids = assembly.RootAssembly?.AllTessellatedSolidsInGlobalCoordinateSystem()
                ?? assembly.Solids?.OfType<TessellatedSolid>().ToArray()
                ?? Array.Empty<TessellatedSolid>();
            return SaveToStream(stream, temporaryPath =>
                SaveCore(solids, temporaryPath, preserveAnalyticPrimitives: true, "assembly.step"));
        }

        /// <summary>
        /// Writes a STEP model using validated analytic planes, cylinders, cones,
        /// closed spheres, and closed ring tori, with faceted fallback elsewhere.
        /// </summary>
        public static bool SaveHybrid(TessellatedSolid solid, string filePath) =>
            SaveCore(new[] { solid }, filePath, preserveAnalyticPrimitives: true);

        /// <summary>
        /// Writes an assembly using validated analytic surfaces with faceted fallback.
        /// </summary>
        public static bool SaveHybrid(SolidAssembly assembly, string filePath)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            var solids = assembly.RootAssembly?.AllTessellatedSolidsInGlobalCoordinateSystem()
                ?? assembly.Solids?.OfType<TessellatedSolid>().ToArray()
                ?? Array.Empty<TessellatedSolid>();
            return SaveCore(solids, filePath, preserveAnalyticPrimitives: true);
        }

        /// <summary>
        /// Writes a tessellated solid as a faceted STEP surface model.
        /// </summary>
        /// <remarks>
        /// Each TVGL triangle becomes a bounded planar STEP face. Watertight meshes are
        /// written as a faceted B-rep; open meshes are written as surface models. This
        /// mode does not reconstruct the original analytic or NURBS surfaces. The destination is replaced only after the
        /// writer has successfully completed a temporary file.
        /// </remarks>
        public static bool SaveFaceted(TessellatedSolid solid, string filePath)
            => SaveCore(new[] { solid }, filePath, preserveAnalyticPrimitives: false);

        /// <summary>
        /// Writes every tessellated assembly instance using triangle-only STEP faces.
        /// </summary>
        public static bool SaveFaceted(SolidAssembly assembly, string filePath)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            var solids = assembly.RootAssembly?.AllTessellatedSolidsInGlobalCoordinateSystem()
                ?? assembly.Solids?.OfType<TessellatedSolid>().ToArray()
                ?? Array.Empty<TessellatedSolid>();
            return SaveCore(solids, filePath, preserveAnalyticPrimitives: false);
        }

        private static bool SaveCore(
            IReadOnlyList<TessellatedSolid> solids,
            string filePath,
            bool preserveAnalyticPrimitives,
            string? headerFileName = null)
        {
            ArgumentNullException.ThrowIfNull(solids);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (solids.Count == 0)
                return false;

            var fullPath = Path.GetFullPath(filePath);
            var destinationDirectory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(destinationDirectory))
                return false;
            Directory.CreateDirectory(destinationDirectory);

            var vertices = new List<double>();
            var triangles = new List<int>();
            var analyticFaces = new List<FacetedStepWriter.AnalyticFace>();
            foreach (var solid in solids)
            {
                if (solid?.Vertices == null || solid.Vertices.Length < 3 ||
                    solid.Faces == null || solid.Faces.Length == 0)
                    continue;

                var millimeterScale = MillimetersPerUnit(solid.Units);
                var vertexIndices = new Dictionary<Vertex, int>(solid.Vertices.Length);
                foreach (var vertex in solid.Vertices)
                {
                    var exportIndex = vertices.Count / 3;
                    vertexIndices[vertex] = exportIndex;
                    vertices.Add(vertex.X * millimeterScale);
                    vertices.Add(vertex.Y * millimeterScale);
                    vertices.Add(vertex.Z * millimeterScale);
                }

                var exportFaceIndices = new Dictionary<TriangleFace, int>(solid.Faces.Length);
                foreach (var face in solid.Faces)
                {
                    if (face?.A == null || face.B == null || face.C == null ||
                        !vertexIndices.TryGetValue(face.A, out var a) ||
                        !vertexIndices.TryGetValue(face.B, out var b) ||
                        !vertexIndices.TryGetValue(face.C, out var c) ||
                        a == b || b == c || c == a)
                        continue;

                    exportFaceIndices[face] = triangles.Count / 3;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                }

                if (preserveAnalyticPrimitives)
                {
                    var planarFaces = BuildPlanarFaces(solid, vertexIndices, exportFaceIndices);
                    var curvedFaces = BuildCurvedFaces(solid, vertexIndices, exportFaceIndices);

                    // Circular analytic topology is safe only when both faces
                    // sharing the circle are emitted analytically. Otherwise a
                    // smooth circle on one face and polygon edges on its faceted
                    // neighbor describe different topological edges and open the shell.
                    var supportedCurvedFaces = curvedFaces.Where(curvedFace =>
                        curvedFace.CircularBoundaries == null ||
                        curvedFace.CircularBoundaries.All(curvedBoundary =>
                            planarFaces.Any(planarFace =>
                                planarFace.CircularBoundaries?.Any(planarBoundary =>
                                    planarBoundary != null && SameCircle(curvedBoundary, planarBoundary)) == true)))
                        .ToArray();
                    var supportedCircularBoundaries = supportedCurvedFaces
                        .SelectMany(curvedFace => curvedFace.CircularBoundaries ??
                                                  Array.Empty<FacetedStepWriter.CircularBoundary>())
                        .ToArray();
                    var safePlanarFaces = planarFaces.Select(planarFace => planarFace with
                    {
                        CircularBoundaries = planarFace.CircularBoundaries?.Select(boundary =>
                            boundary != null && supportedCircularBoundaries.Any(candidate =>
                                SameCircle(boundary, candidate))
                                ? boundary
                                : null).ToArray()
                    });

                    analyticFaces.AddRange(safePlanarFaces.Select(face => Scale(face, millimeterScale)));
                    analyticFaces.AddRange(supportedCurvedFaces.Select(face => Scale(face, millimeterScale)));
                }
            }

            if (triangles.Count == 0)
                return false;

            var temporaryPath = Path.Combine(
                destinationDirectory,
                $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                if (!FacetedStepWriter.Write(
                        temporaryPath,
                        vertices.ToArray(),
                        triangles.ToArray(),
                        analyticFaces,
                        headerFileName ?? Path.GetFileName(fullPath)) ||
                    !File.Exists(temporaryPath))
                    return false;

                File.Move(temporaryPath, fullPath, overwrite: true);
                return true;
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        private static double MillimetersPerUnit(UnitType units) => units switch
        {
            UnitType.micron => 0.001,
            UnitType.centimeter => 10.0,
            UnitType.meter => 1000.0,
            UnitType.inch => 25.4,
            UnitType.foot => 304.8,
            _ => 1.0
        };

        private static FacetedStepWriter.PlanarFace Scale(
            FacetedStepWriter.PlanarFace face,
            double scale) => face with
        {
            CircularBoundaries = face.CircularBoundaries?.Select(boundary =>
                boundary == null ? null : Scale(boundary, scale)).ToArray()
        };

        private static FacetedStepWriter.CurvedFace Scale(
            FacetedStepWriter.CurvedFace face,
            double scale) => face with
        {
            Origin = Scale(face.Origin, scale),
            FirstRadius = face.FirstRadius * scale,
            SecondRadius = face.SecondRadius * scale,
            CircularBoundaries = face.CircularBoundaries?.Select(boundary => Scale(boundary, scale)).ToArray()
        };

        private static FacetedStepWriter.CircularBoundary Scale(
            FacetedStepWriter.CircularBoundary boundary,
            double scale) => boundary with
        {
            Center = Scale(boundary.Center, scale),
            Radius = boundary.Radius * scale
        };

        private static (double X, double Y, double Z) Scale(
            (double X, double Y, double Z) value,
            double scale) => (value.X * scale, value.Y * scale, value.Z * scale);

        private static bool SameCircle(
            FacetedStepWriter.CircularBoundary first,
            FacetedStepWriter.CircularBoundary second)
        {
            var scale = Math.Max(1.0, Math.Max(first.Radius, second.Radius));
            var tolerance = 1.0e-6 * scale;
            var dx = first.Center.X - second.Center.X;
            var dy = first.Center.Y - second.Center.Y;
            var dz = first.Center.Z - second.Center.Z;
            return Math.Abs(first.Radius - second.Radius) <= tolerance &&
                   dx * dx + dy * dy + dz * dz <= tolerance * tolerance;
        }

        private static IReadOnlyList<FacetedStepWriter.CurvedFace> BuildCurvedFaces(
            TessellatedSolid solid,
            IReadOnlyDictionary<Vertex, int> vertexIndices,
            IReadOnlyDictionary<TriangleFace, int> faceIndices)
        {
            if (solid.Primitives == null || solid.Primitives.Count == 0)
                return Array.Empty<FacetedStepWriter.CurvedFace>();

            var result = new List<FacetedStepWriter.CurvedFace>();
            var coordinates = vertexIndices.ToDictionary(pair => pair.Value, pair => pair.Key.Coordinates);
            foreach (var primitive in solid.Primitives.Where(primitive =>
                         primitive is Cone or Cylinder or Sphere or Torus))
            {
                if (primitive is Sphere or Torus)
                {
                    // Only a complete closed sphere can use the canonical
                    // octant topology below. Trimmed spherical patches retain
                    // their tessellation until general intersection curves are supported.
                    if (!TryGetClosedPrimitiveTriangles(
                            primitive,
                            vertexIndices,
                            faceIndices,
                            out var closedSurfaceTriangleIndices,
                            out var sampleFace))
                        continue;
                    var validSurface = primitive switch
                    {
                        Sphere sphere => sphere.Radius > 0,
                        Torus torus => !torus.Axis.IsNegligible() &&
                                       torus.MajorRadius > torus.MinorRadius && torus.MinorRadius > 0,
                        _ => false
                    };
                    if (validSurface)
                    {
                        var (kind, origin, axis, firstRadius, secondRadius, closedSurfaceSense) = primitive switch
                        {
                            Sphere sphere => (
                                FacetedStepWriter.CurvedSurfaceKind.Sphere,
                                sphere.Center,
                                Vector3.UnitZ,
                                sphere.Radius,
                                0.0,
                                sphere.IsPositive.GetValueOrDefault(true)),
                            Torus torus => (
                                FacetedStepWriter.CurvedSurfaceKind.Torus,
                                torus.Center,
                                torus.Axis,
                                torus.MajorRadius,
                                torus.MinorRadius,
                                torus.IsPositive.GetValueOrDefault(true)),
                            _ => throw new InvalidOperationException()
                        };
                        result.Add(new FacetedStepWriter.CurvedFace(
                            new[]
                            {
                                vertexIndices[sampleFace!.A],
                                vertexIndices[sampleFace.B],
                                vertexIndices[sampleFace.C]
                            },
                            Array.Empty<int[]>(),
                            closedSurfaceTriangleIndices,
                            closedSurfaceSense,
                            kind,
                            (origin.X, origin.Y, origin.Z),
                            (axis.X, axis.Y, axis.Z),
                            firstRadius,
                            secondRadius));
                    }
                    continue;
                }

                if (!TryGetPrimitiveLoops(
                        primitive,
                        vertexIndices,
                        faceIndices,
                        out var loops,
                        out var primitiveTriangleIndices))
                    continue;

                var outerLoop = loops.OrderByDescending(loop => loop.Length).First();
                var innerLoops = loops.Where(loop => !ReferenceEquals(loop, outerLoop)).ToArray();
                var orderedLoops = new[] { outerLoop }.Concat(innerLoops).ToArray();
                var circularBoundaries = new List<FacetedStepWriter.CircularBoundary>();
                if (orderedLoops.Any(loop => !TryFitCircularBoundary(loop, coordinates, out _)))
                    continue;
                foreach (var loop in orderedLoops)
                {
                    TryFitCircularBoundary(loop, coordinates, out var boundary);
                    circularBoundaries.Add(boundary!);
                }
                var sameSense = primitive.IsPositive.GetValueOrDefault(true);
                FacetedStepWriter.CurvedFace? face = primitive switch
                {
                    Cone cone when !cone.Axis.IsNegligible() && cone.Aperture > 0 =>
                        CreateConeFace(cone, sameSense),
                    Cylinder cylinder when !cylinder.Axis.IsNegligible() &&
                                               cylinder.Radius > 0 && circularBoundaries.Count == 2 =>
                        CreateCurvedFace(
                            FacetedStepWriter.CurvedSurfaceKind.Cylinder,
                            cylinder.Anchor,
                            cylinder.Axis,
                            cylinder.Radius,
                            sameSense),
                    _ => null
                };
                if (face != null)
                    result.Add(face);

                FacetedStepWriter.CurvedFace CreateCurvedFace(
                    FacetedStepWriter.CurvedSurfaceKind kind,
                    Vector3 origin,
                    Vector3 axis,
                    double firstRadius,
                    bool sense,
                    double secondRadius = 0,
                    double semiAngle = 0)
                    => new(
                        outerLoop,
                        innerLoops,
                        primitiveTriangleIndices,
                        sense,
                        kind,
                        (origin.X, origin.Y, origin.Z),
                        (axis.X, axis.Y, axis.Z),
                        firstRadius,
                        secondRadius,
                        semiAngle,
                        circularBoundaries);

                FacetedStepWriter.CurvedFace? CreateConeFace(Cone cone, bool sense)
                {
                    var axis = cone.Axis.Normalize();
                    var axialDistances = cone.Faces
                        .SelectMany(triangle => triangle.Vertices)
                        .Select(vertex => (vertex.Coordinates - cone.Apex).Dot(axis))
                        .Where(distance => distance > 1.0e-9)
                        .ToArray();
                    if (axialDistances.Length == 0)
                        return null;
                    var referenceDistance = axialDistances.Min();
                    var referenceRadius = referenceDistance * cone.Aperture;
                    if (referenceRadius <= 1.0e-9)
                        return null;
                    var origin = cone.Apex + referenceDistance * axis;
                    return CreateCurvedFace(
                        FacetedStepWriter.CurvedSurfaceKind.Cone,
                        origin,
                        axis,
                        referenceRadius,
                        sense,
                        semiAngle: Math.Atan(cone.Aperture));
                }
            }
            return result;
        }

        private static bool TryGetClosedPrimitiveTriangles(
            PrimitiveSurface primitive,
            IReadOnlyDictionary<Vertex, int> vertexIndices,
            IReadOnlyDictionary<TriangleFace, int> faceIndices,
            out HashSet<int> primitiveTriangleIndices,
            out TriangleFace? sampleFace)
        {
            primitiveTriangleIndices = new HashSet<int>();
            sampleFace = null;
            if (primitive.Faces == null || primitive.Faces.Count == 0)
                return false;

            var edgeUseCounts = new Dictionary<(int Min, int Max), int>();
            foreach (var face in primitive.Faces)
            {
                if (!faceIndices.TryGetValue(face, out var triangleIndex) ||
                    !vertexIndices.TryGetValue(face.A, out var a) ||
                    !vertexIndices.TryGetValue(face.B, out var b) ||
                    !vertexIndices.TryGetValue(face.C, out var c))
                    return false;

                sampleFace ??= face;
                primitiveTriangleIndices.Add(triangleIndex);
                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }

            // A trimmed spherical or toroidal patch always has at least one
            // mesh boundary edge. Only a truly closed primitive may be
            // replaced by the canonical exact-surface topology.
            return edgeUseCounts.Count > 0 && edgeUseCounts.Values.All(count => count == 2);

            void AddEdge(int first, int second)
            {
                var key = first < second ? (first, second) : (second, first);
                edgeUseCounts[key] = edgeUseCounts.GetValueOrDefault(key) + 1;
            }
        }

        private static bool TryFitCircularBoundary(
            int[] loop,
            IReadOnlyDictionary<int, Vector3> vertices,
            out FacetedStepWriter.CircularBoundary? boundary)
        {
            boundary = null;
            // Three- and four-sided polygon loops are always mathematically
            // co-circular, but their intended STEP edges are straight. Analytic
            // circular boundaries produced by tessellation have enough samples
            // to distinguish them reliably.
            if (loop.Length < 8 || loop.Any(index => !vertices.ContainsKey(index)))
                return false;

            var a = vertices[loop[0]];
            Vector3 center = default;
            Vector3 normal = default;
            var found = false;
            for (var second = 1; second < loop.Length - 1 && !found; second++)
            {
                var u = vertices[loop[second]] - a;
                for (var third = second + 1; third < loop.Length; third++)
                {
                    var v = vertices[loop[third]] - a;
                    var cross = u.Cross(v);
                    var crossLengthSquared = cross.LengthSquared();
                    if (crossLengthSquared <= 1.0e-18)
                        continue;
                    center = a +
                             (u.LengthSquared() * v.Cross(cross) +
                              v.LengthSquared() * cross.Cross(u)) /
                             (2 * crossLengthSquared);
                    normal = cross.Normalize();
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;

            var radius = (a - center).Length();
            if (radius <= 1.0e-9)
                return false;
            var tolerance = Math.Max(1.0e-6, radius * 1.0e-5);
            foreach (var index in loop)
            {
                var offset = vertices[index] - center;
                if (Math.Abs(offset.Dot(normal)) > tolerance ||
                    Math.Abs(offset.Length() - radius) > tolerance)
                    return false;
            }

            var areaNormal = Vector3.Zero;
            for (var index = 0; index < loop.Length; index++)
                areaNormal += vertices[loop[index]].Cross(vertices[loop[(index + 1) % loop.Length]]);
            if (areaNormal.Dot(normal) < 0)
                normal = -normal;
            var canonicalVertexIndex = loop
                .OrderByDescending(index => vertices[index].X)
                .ThenByDescending(index => vertices[index].Y)
                .ThenByDescending(index => vertices[index].Z)
                .First();
            boundary = new FacetedStepWriter.CircularBoundary(
                canonicalVertexIndex,
                (center.X, center.Y, center.Z),
                (normal.X, normal.Y, normal.Z),
                radius);
            return true;
        }

        private static bool TryGetPrimitiveLoops(
            PrimitiveSurface primitive,
            IReadOnlyDictionary<Vertex, int> vertexIndices,
            IReadOnlyDictionary<TriangleFace, int> faceIndices,
            out List<int[]> loops,
            out HashSet<int> primitiveTriangleIndices)
        {
            loops = new List<int[]>();
            primitiveTriangleIndices = new HashSet<int>();
            if (primitive.Faces == null || primitive.Faces.Count == 0)
                return false;

            var boundaryEdges = new Dictionary<(int Min, int Max), (int Count, int From, int To)>();
            foreach (var face in primitive.Faces)
            {
                if (!faceIndices.TryGetValue(face, out var triangleIndex) ||
                    !vertexIndices.TryGetValue(face.A, out var a) ||
                    !vertexIndices.TryGetValue(face.B, out var b) ||
                    !vertexIndices.TryGetValue(face.C, out var c))
                    return false;
                primitiveTriangleIndices.Add(triangleIndex);
                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }

            var directedEdges = boundaryEdges.Values
                .Where(edge => edge.Count == 1)
                .Select(edge => (edge.From, edge.To))
                .ToList();
            return directedEdges.Count > 0 && TryBuildLoops(directedEdges, out loops) && loops.Count > 0;

            void AddEdge(int from, int to)
            {
                var key = from < to ? (from, to) : (to, from);
                if (boundaryEdges.TryGetValue(key, out var edge))
                    boundaryEdges[key] = (edge.Count + 1, edge.From, edge.To);
                else
                    boundaryEdges[key] = (1, from, to);
            }
        }

        private static IReadOnlyList<FacetedStepWriter.PlanarFace> BuildPlanarFaces(
            TessellatedSolid solid,
            IReadOnlyDictionary<Vertex, int> vertexIndices,
            IReadOnlyDictionary<TriangleFace, int> faceIndices)
        {
            if (solid.Primitives == null || solid.Primitives.Count == 0)
                return Array.Empty<FacetedStepWriter.PlanarFace>();

            var result = new List<FacetedStepWriter.PlanarFace>();
            var coordinates = vertexIndices.ToDictionary(pair => pair.Value, pair => pair.Key.Coordinates);
            var verticesByIndex = vertexIndices.ToDictionary(pair => pair.Value, pair => pair.Key);
            var circularAnalyticSurfaces = solid.Primitives
                .Where(primitive => primitive is Cone or Cylinder)
                .Where(primitive => primitive.Vertices != null && primitive.Vertices.Count > 0)
                .ToArray();

            foreach (var plane in solid.Primitives.OfType<Plane>())
            {
                if (plane.Faces == null || plane.Faces.Count == 0 || plane.Normal.IsNegligible())
                    continue;

                var boundaryEdges = new Dictionary<(int Min, int Max), (int Count, int From, int To)>();
                var primitiveTriangleIndices = new HashSet<int>();
                var valid = true;
                foreach (var face in plane.Faces)
                {
                    if (!faceIndices.TryGetValue(face, out var triangleIndex) ||
                        !vertexIndices.TryGetValue(face.A, out var a) ||
                        !vertexIndices.TryGetValue(face.B, out var b) ||
                        !vertexIndices.TryGetValue(face.C, out var c))
                    {
                        valid = false;
                        break;
                    }
                    primitiveTriangleIndices.Add(triangleIndex);
                    AddEdge(a, b);
                    AddEdge(b, c);
                    AddEdge(c, a);
                }
                if (!valid)
                    continue;

                var directedEdges = boundaryEdges.Values
                    .Where(edge => edge.Count == 1)
                    .Select(edge => (edge.From, edge.To))
                    .ToList();
                if (!TryBuildLoops(directedEdges, out var loops) || loops.Count == 0)
                    continue;

                var normal = plane.Normal.Normalize();
                var loopAreas = loops
                    .Select(loop => SignedArea(loop, coordinates, normal))
                    .ToArray();
                var outerIndex = Enumerable.Range(0, loops.Count)
                    .MaxBy(index => Math.Abs(loopAreas[index]));
                var outerLoop = loops[outerIndex];
                if (loopAreas[outerIndex] < 0)
                    Array.Reverse(outerLoop);

                var innerLoops = new List<int[]>();
                valid = true;
                for (var loopIndex = 0; loopIndex < loops.Count; loopIndex++)
                {
                    if (loopIndex == outerIndex)
                        continue;
                    var loop = loops[loopIndex];
                    if (!PointInsidePlanarLoop(
                            coordinates[loop[0]],
                            outerLoop.Select(index => coordinates[index]),
                            normal))
                    {
                        valid = false;
                        break;
                    }
                    if (loopAreas[loopIndex] > 0)
                        Array.Reverse(loop);
                    innerLoops.Add(loop);
                }
                if (!valid)
                    continue;

                var orderedLoops = new[] { outerLoop }.Concat(innerLoops).ToArray();
                var circularBoundaries = orderedLoops.Select(loop =>
                {
                    var isAnalyticBoundary = circularAnalyticSurfaces.Any(primitive =>
                        loop.All(index => primitive.Vertices.Contains(verticesByIndex[index])));
                    return isAnalyticBoundary && TryFitCircularBoundary(loop, coordinates, out var boundary)
                        ? boundary
                        : null;
                }).ToArray();
                result.Add(new FacetedStepWriter.PlanarFace(
                    outerLoop,
                    innerLoops,
                    primitiveTriangleIndices,
                    (normal.X, normal.Y, normal.Z),
                    circularBoundaries));

                void AddEdge(int from, int to)
                {
                    var key = from < to ? (from, to) : (to, from);
                    if (boundaryEdges.TryGetValue(key, out var edge))
                        boundaryEdges[key] = (edge.Count + 1, edge.From, edge.To);
                    else
                        boundaryEdges[key] = (1, from, to);
                }
            }
            return result;
        }

        private static bool TryBuildLoops(
            IReadOnlyCollection<(int From, int To)> edges,
            out List<int[]> loops)
        {
            loops = new List<int[]>();
            var outgoing = edges.GroupBy(edge => edge.From)
                .ToDictionary(group => group.Key, group => group.Select(edge => edge.To).ToList());
            if (outgoing.Values.Any(destinations => destinations.Count != 1))
                return false;

            var remaining = edges.ToHashSet();
            while (remaining.Count > 0)
            {
                var first = remaining.First();
                var loop = new List<int> { first.From };
                var current = first;
                while (true)
                {
                    if (!remaining.Remove(current))
                        return false;
                    if (current.To == loop[0])
                        break;
                    loop.Add(current.To);
                    if (!outgoing.TryGetValue(current.To, out var next) || next.Count != 1)
                        return false;
                    current = (current.To, next[0]);
                    if (loop.Count > edges.Count)
                        return false;
                }
                if (loop.Count < 3)
                    return false;
                loops.Add(loop.ToArray());
            }
            return true;
        }

        private static double SignedArea(
            int[] loop,
            IReadOnlyDictionary<int, Vector3> vertices,
            Vector3 normal)
        {
            var areaNormal = Vector3.Zero;
            for (var index = 0; index < loop.Length; index++)
                areaNormal += vertices[loop[index]].Cross(
                    vertices[loop[(index + 1) % loop.Length]]);
            return 0.5 * areaNormal.Dot(normal);
        }

        private static bool PointInsidePlanarLoop(
            Vector3 point,
            IEnumerable<Vector3> loopPoints,
            Vector3 normal)
        {
            var points = loopPoints.ToArray();
            var dropAxis = Math.Abs(normal.X) >= Math.Abs(normal.Y) && Math.Abs(normal.X) >= Math.Abs(normal.Z)
                ? 0
                : Math.Abs(normal.Y) >= Math.Abs(normal.Z) ? 1 : 2;
            (double X, double Y) Project(Vector3 value) => dropAxis switch
            {
                0 => (value.Y, value.Z),
                1 => (value.X, value.Z),
                _ => (value.X, value.Y)
            };
            var p = Project(point);
            var inside = false;
            for (var i = 0; i < points.Length; i++)
            {
                var a = Project(points[i]);
                var b = Project(points[(i + 1) % points.Length]);
                if ((a.Y > p.Y) != (b.Y > p.Y) &&
                    p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
            return inside;
        }

        private static bool SaveToStream(Stream stream, Func<string, bool> saveFile)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanWrite)
                throw new ArgumentException("The destination stream must be writable.", nameof(stream));

            var temporaryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.step");
            try
            {
                if (!saveFile(temporaryPath))
                    return false;
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                    stream.SetLength(0);
                }
                using var temporaryFile = new FileStream(
                    temporaryPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                temporaryFile.CopyTo(stream);
                return true;
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
