# TVGL.STEPImportExport

TVGL.STEPImportExport provides STEP (`.step`, `.stp`) file import capabilities for TVGL (Tessellation and Voxelization Geometry Library).

## Features
- **Accurate B-Rep Meshing**: Discretizes Boundary Representation solids into high-quality watertight triangle meshes powered by Open CASCADE Technology (OCCT).
- **Analytic Primitive Recovery**: Automatically extracts and maps analytic geometry surfaces (`Plane`, `Cylinder`, `Cone`, `Sphere`, `Torus`) with their exact mathematical parameters.
- **Full Doubly Linked Primitives**: Constituent `TriangleFace` objects and `PrimitiveSurface` objects are fully doubly linked (`face.BelongsToPrimitive` and `primitive.Faces`).
- **Freeform Surface Support**: Freeform surfaces (NURBS, B-Spline, Bezier, Offset) are represented as `UnknownRegion` primitives with their descriptive surface type preserved.
- **Multi-Body & Assembly Support**: Reconstructs multi-body STEP files into individual `TessellatedSolid`s and `SolidAssembly` hierarchies.

