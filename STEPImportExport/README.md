# TVGL.STEPImportExport

TVGL.STEPImportExport provides STEP (`.step`, `.stp`) import and export capabilities for TVGL (Tessellation and Voxelization Geometry Library).

## Features
- **Accurate B-Rep Meshing**: Discretizes Boundary Representation solids into high-quality watertight triangle meshes powered by Open CASCADE Technology (OCCT).
- **Analytic Primitive Recovery**: Automatically extracts and maps analytic geometry surfaces (`Plane`, `Cylinder`, `Cone`, `Sphere`, `Torus`) with their exact mathematical parameters.
- **Full Doubly Linked Primitives**: Constituent `TriangleFace` objects and `PrimitiveSurface` objects are fully doubly linked (`face.BelongsToPrimitive` and `primitive.Faces`).
- **Freeform Surface Support**: Freeform surfaces (NURBS, B-Spline, Bezier, Offset) are represented as `UnknownRegion` primitives with their descriptive surface type preserved.
- **Multi-Body & Assembly Support**: Reconstructs multi-body STEP files into individual `TessellatedSolid`s and `SolidAssembly` hierarchies.
- **Hybrid Export**: `STEP.Save(solid, filePath)` and `STEP.SaveHybrid(solid, filePath)` preserve reliable TVGL planes, circularly bounded cylinders and cones, closed spheres, and closed ring tori as analytic STEP surfaces. Other regions use triangle faces.
- **Faceted Export**: `STEP.SaveFaceted(solid, filePath)` writes every triangle as a bounded planar STEP face. Watertight meshes become faceted B-reps; open meshes become surface models.
- **Assembly Export**: `STEP.Save(assembly, filePath)` writes transformed assembly instances as distinct STEP bodies.
- **Stream Symmetry**: STEP solids and assemblies can be opened from or saved to caller-owned streams as well as files.
- **Unit Preservation**: Export converts supported TVGL units to the millimeter representation declared in the STEP context, including analytic dimensions.

## Export fidelity

Faceted export preserves the TVGL triangle geometry and winding. It does not
recreate analytic surfaces from primitive metadata. Hybrid export reconstructs
the validated analytic cases above. Trimmed spheres and tori, cylinders or cones
with non-circular trim curves, spindle tori, and NURBS regions remain faceted.
Closed spheres are written as eight exact spherical octants and closed tori as
sixteen exact analytic patches so their periodic topology is explicit. The
writer uses a temporary file and replaces the requested destination only after
the STEP write succeeds.

