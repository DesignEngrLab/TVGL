# How-To: Understanding Voxelized Solids

While `TessellatedSolid` is the most common representation in TVGL, `VoxelizedSolid` offers a powerful alternative approach to representing 3D geometry. Instead of a surface mesh, a voxelized solid represents an object as a grid of discrete, cubic elements called **voxels**.

Think of it like a 3D bitmap image or a LEGO model. The entire bounding box of the object is divided into a uniform grid, and each cell in the grid (each voxel) is either "on" (part of the solid) or "off" (empty space).

## Why Use Voxelized Solids?

Voxel representations have several key advantages:

-   **Boolean Operations are Fast and Robust:** Operations like union, intersection, and subtraction are incredibly fast and reliable with voxelized solids. They become simple logical operations on the grid, avoiding the complex and sometimes fragile geometric calculations required for meshes.
-   **Complex Geometry is Simplified:** Voxelization can simplify extremely complex or imperfect mesh geometry (e.g., meshes with self-intersections or thousands of tiny holes) into a uniform, manageable grid.
-   **Natural for Simulation and Analysis:** Voxel grids are a natural fit for many types of simulation, such as fluid dynamics, heat transfer, and stress analysis.

## Creating a Voxelized Solid from a Mesh

The most common way to create a `VoxelizedSolid` is by converting an existing `TessellatedSolid`. You have two main ways to control the resolution of the voxel grid.

### 1. By Voxels on Longest Side

This is the easiest method. You simply specify how many voxels the longest side of your object's bounding box should have. TVGL calculates the correct voxel size for you.

```csharp
// Assume 'myMesh' is a TVGL TessellatedSolid object
int resolution = 200; // I want the longest side to be 200 voxels long

// Convert the mesh to a voxelized solid
var voxelSolid = VoxelizedSolid.CreateFrom(myMesh, resolution);

Console.WriteLine($"Created a voxel solid with {voxelSolid.Count} active voxels.");
```

### 2. By Voxel Side Length

If you need voxels of a specific, absolute size (e.g., 1 millimeter), you can specify the side length directly.

```csharp
// Assume 'myMesh' is a TVGL TessellatedSolid object
double voxelSize = 1.0; // Each voxel will be 1x1x1 units

// Convert the mesh to a voxelized solid
var voxelSolid = VoxelizedSolid.CreateFrom(myMesh, voxelSize);
```

## Voxel Boolean Operations

Once you have voxelized solids, performing boolean operations is straightforward. For these operations to be correct, the solids should have the **same voxel size and occupy the same space** (i.e., have the same bounds).

```csharp
// Assume 'voxelA' and 'voxelB' are two aligned VoxelizedSolids

// Union (A or B)
var unionSolid = voxelA.UnionToNewSolid(voxelB);

// Intersection (A and B)
var intersectionSolid = voxelA.IntersectToNewSolid(voxelB);

// Subtraction (A and not B)
var subtractedSolid = voxelA.SubtractToNewSolid(voxelB);
```

You can also perform these operations "in-place" to modify the original solid (e.g., `voxelA.Subtract(voxelB)`), which can be more memory efficient.

## Slicing a Voxelized Solid

Slicing is much simpler with a `VoxelizedSolid` than a `TessellatedSolid`, especially for cuts that are aligned with the main X, Y, or Z axes.

```csharp
// Assume 'voxelSolid' is a VoxelizedSolid

// Slice the solid along a plane perpendicular to the X-axis
// 'distance' is the voxel index to cut before.
(var part1, var part2) = voxelSolid.SliceOnPlane(CartesianDirections.XPositive, distance: 100);
```

You can also slice with an arbitrary `Plane`, though this is more computationally intensive than an axis-aligned slice.

Voxelized solids are a powerful tool for robust geometric modeling and analysis. They are especially useful when you need to perform complex boolean operations or when working with difficult or imperfect mesh data.
