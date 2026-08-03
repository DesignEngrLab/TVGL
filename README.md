# TVGL

TVGL is a computational geometry library for .NET. It provides data structures and algorithms for triangle meshes, polygons, voxels, analytic surfaces, convex hulls, and solid-model analysis.

The repository also contains testing, benchmarking, file-conversion, and Windows desktop presentation projects. Some of these projects have platform-specific requirements beyond those of the core library.
## Installation 
Clone the repository and build the core library in Release configuration:
```powershell
git clone https://github.com/DesignEngrLab/TVGL.git
cd TVGL
dotnet build TessellationAndVoxelizationGeometryLibrary/TessellationAndVoxelizationGeometryLibrary.csproj --configuration Release
```
Reference `TessellationAndVoxelizationGeometryLibrary.csproj` directly from your project.
## Installation from NuGet

Install it from NuGet with the .NET CLI:

```powershell
dotnet add package TVGL --version 2.0.0
```

Or add the package reference directly to your project file:

```xml
<PackageReference Include="TVGL" Version="2.0.0" />
```

## Quick start

The following example constructs the three-dimensional convex hull of five points:

```csharp
using System;
using System.Collections.Generic;
using TVGL;

var points = new List<Vector3>
{
    new Vector3(0, 0, 0),
    new Vector3(1, 0, 0),
    new Vector3(0, 1, 0),
    new Vector3(0, 0, 1),
    new Vector3(0.2, 0.2, 0.2)
};

if (ConvexHull3D.Create(points, out var hull, out var hullVertexIndices))
{
    Console.WriteLine($"Hull vertices: {hull.Vertices.Count}");
    Console.WriteLine($"Hull edges: {hull.Edges.Count}");
    Console.WriteLine($"Hull faces: {hull.Faces.Count}");
    Console.WriteLine($"Input vertices on hull: {string.Join(", ", hullVertexIndices)}");
}
```

## Capabilities

TVGL includes tools for:

- triangle meshes and boundary-representation solids;
- two-dimensional polygons, offsets, intersections, and triangulation;
- voxelization and voxel-based solid operations;
- convex hulls and Delaunay triangulation in two, three, and four dimensions;
- planes, cylinders, cones, spheres, tori, and general quadrics;
- axis-aligned and oriented bounding volumes;
- KD-trees and nearest-neighbor searches;
- geometric transformations, projections, intersections, and distances;
- mesh inspection, modification, and repair; and
- importing and exporting common mesh file formats.

Computational geometry is sensitive to degeneracies, floating-point precision, topology, and model scale. Callers should choose tolerances appropriate to their data and validate results when working with malformed, non-manifold, or nearly degenerate geometry.

## Core types

### Vectors and coordinates

`Vector2`, `Vector3`, and `Vector4` represent coordinates and directions. Many TVGL algorithms are implemented as extension methods on vectors, vertices, faces, polygons, and solids.

### Polygons

`Polygon` represents a bounded two-dimensional contour. Polygon operations include containment queries, intersections, offsets, simplification, triangulation, and related planar geometry functions.

### Tessellated solids

`TessellatedSolid` represents a three-dimensional boundary using vertices, edges, and triangular faces. TVGL includes operations for constructing, transforming, querying, modifying, and analyzing these meshes.

### Voxelized solids

`VoxelizedSolid` represents geometry on a regular grid of cubic cells. Voxel models are useful when a discrete representation is preferable to exact boundary geometry, including some boolean, slicing, and analysis workflows.

### Analytic primitives

TVGL models planes, cylinders, cones, spheres, tori, and more general conic and quadric surfaces. These types support geometric queries and the interpretation of analytic geometry found in tessellated models.

## Loading and saving geometry

The core library contains readers and writers for several three-dimensional mesh formats, including:

- STL, both ASCII and binary;
- 3MF;
- AMF;
- OBJ;
- OFF;
- PLY, both ASCII and binary; and
- TVGL's native serialized formats.

Open a supported model using its file extension:

```csharp
using TVGL;

Solid solid = IO.Open("part.stl");
```

For two-dimensional SVG, DXF, and DWG workflows, install the companion package:

```powershell
dotnet add package TVGL.PolygonImportExport --version 2.0.0
```

The package exposes `PolygonImportExport.SVG`, `PolygonImportExport.DXF`, and `PolygonImportExport.DWG`. Format support varies between reading and writing; consult the relevant APIs before depending on round-trip conversion.

## Documentation

The repository contains introductory guides for several major areas of TVGL:

- [Basic vector and geometric operations](https://github.com/DesignEngrLab/TVGL/blob/master/Documentation/articles/basic-vector-operations.md)
- [Working with tessellated solids](https://github.com/DesignEngrLab/TVGL/blob/master/Documentation/articles/working-with-tessellated-solids.md)
- [Understanding voxelized solids](https://github.com/DesignEngrLab/TVGL/blob/master/Documentation/articles/understanding-voxelized-solids.md)
- [Using primitive surfaces](https://github.com/DesignEngrLab/TVGL/blob/master/Documentation/articles/using-primitive-surfaces.md)

The public API also includes XML documentation for use in IntelliSense and generated reference documentation.

## Contributing

Bug reports and pull requests are welcome. Geometry bug reports are most useful when they include:

- a minimal reproducible example;
- the input geometry or a small equivalent model;
- the expected and actual results;
- the units, scale, and tolerance involved; and
- relevant topology information, such as whether the mesh is closed and manifold.

Please keep behavioral changes separate from formatting or documentation-only changes where practical, and include focused tests for geometry fixes.

## License

TVGL is distributed under the [MIT License](https://github.com/DesignEngrLab/TVGL/blob/master/LICENSE).
