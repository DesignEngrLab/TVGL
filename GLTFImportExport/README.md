# TVGL.GLTFImportExport

`TVGL.GLTFImportExport` imports and exports three-dimensional CAD and mesh models in glTF (`.gltf`) and binary glTF (`.glb`) formats using TVGL `TessellatedSolid` and `SolidAssembly` types.

## Features

- **glTF and GLB Support**: Reads and writes both JSON-based `.gltf` and self-contained binary `.glb` files, as well as binary streams.
- **Color & PBR Material Mapping**: Preserves uniform solid colors, multi-colored face groupings, and vertex colors using standard glTF PBR Metallic Roughness materials (`BaseColor`).
- **Assembly & Hierarchy Support**: Reconstructs multi-node scenes into TVGL `SolidAssembly` hierarchies, maintaining node transformations and mesh instancing.
- **Normal Generation**: Exports smooth vertex normals for high-fidelity rendering in glTF viewers and WebGPU pipelines.
- **Robust Winding Handling**: Automatically accounts for negative transformation determinants (reflections) to preserve correct face winding.

## Example

```csharp
using TVGL.GLTFImportExport;

// Open a model (first solid)
var solid = GLTF.Open("model.glb");

// Open all solids in the scene in global coordinates
var solids = GLTF.OpenSolids("assembly.gltf");

// Open as a TVGL SolidAssembly preserving node transforms
var assembly = GLTF.OpenSolidAssembly("assembly.glb");

// Save a solid as binary GLB
GLTF.Save("output.glb", solid);

// Save as text glTF
GLTF.Save("output.gltf", solid);

// Save an entire assembly
GLTF.Save("output_assembly.glb", assembly);
```

## Dependencies

- **TVGL** (Tessellation and Voxelization Geometry Library)
- **SharpGLTF.Core** for glTF 2.0 serialization and decoding

## License

Distributed under the MIT License.

