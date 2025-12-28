# How-To: Working with Tessellated Solids

The `TessellatedSolid` is the cornerstone of the Tessellation Voxelization Geometry Library (TVGL). It represents a 3D object as a "boundary representation" (B-rep), which means it's defined by the collection of surfaces that form its boundary. In TVGL, these surfaces are always triangles, forming a mesh.

This guide will cover the basics of creating and manipulating `TessellatedSolid` objects.

## What is a Tessellated Solid?

A `TessellatedSolid` is made up of three fundamental elements:

-   **Vertices:** These are the 3D points that define the corners of the triangles.
-   **Edges:** These are the lines that connect pairs of vertices.
-   **TriangleFaces:** These are the triangular surfaces defined by three vertices.

Together, these elements form a "mesh" that encloses a volume of space. For a solid to be considered "water-tight" or "manifold," every edge must be shared by exactly two faces. TVGL includes many tools to check for and repair solids that don't meet this condition.

## Creating a Tessellated Solid

There are several ways to create a `TessellatedSolid`, but the most common is from a list of vertices and a list of face indices. This is how most 3D file formats (like OBJ, STL, and 3MF) are structured.

### Example: Creating a Simple Pyramid

Let's create a pyramid with a square base. It will have 5 vertices and 4 triangular faces.

```csharp
// Define the 5 vertices of the pyramid
var vertices = new List<Vector3>
{
    new Vector3(0, 0, 0),    // 0: Base corner
    new Vector3(10, 0, 0),   // 1: Base corner
    new Vector3(10, 10, 0),  // 2: Base corner
    new Vector3(0, 10, 0),   // 3: Base corner
    new Vector3(5, 5, 10)    // 4: Apex
};

// Define the 4 triangular faces using the indices of the vertices
var faceIndices = new List<(int, int, int)>
{
    (0, 1, 4), // Front face
    (1, 2, 4), // Right face
    (2, 3, 4), // Back face
    (3, 0, 4)  // Left face
};
// Note: We've omitted the base faces for simplicity here.

// Create the TessellatedSolid
var pyramid = new TessellatedSolid(vertices, faceIndices);

// You can now access its properties
Console.WriteLine($"Created a solid with {pyramid.NumberOfVertices} vertices and {pyramid.NumberOfFaces} faces.");
```

## Manipulating Solids

Once you have a `TessellatedSolid`, you'll often want to move, rotate, or duplicate it.

### Copying a Solid

It's good practice to create a copy of a solid before performing an operation that modifies it. The `Copy()` method creates a complete, deep copy of the solid.

```csharp
// Assume 'pyramid' is the solid we created earlier
var pyramidCopy = pyramid.Copy();
```

### Transforming a Solid

Transformations like translation (moving), rotation, and scaling are handled by applying a `Matrix4x4` transformation matrix.

```csharp
// 1. Translation: Move the pyramid 5 units up the Z-axis
var translationMatrix = Matrix4x4.CreateTranslation(0, 0, 5);
pyramid.Transform(translationMatrix);

// 2. Rotation: Rotate the pyramid 45 degrees around the Y-axis
var rotationMatrix = Matrix4x4.CreateRotationY((float)(Math.PI / 4.0));
pyramid.Transform(rotationMatrix);

// 3. Scaling: Make the pyramid twice as large
var scalingMatrix = Matrix4x4.CreateScale(2.0);
pyramid.Transform(scalingMatrix);
```

You can also create a new, transformed solid without modifying the original using `TransformToNewSolid`.

```csharp
var translatedPyramid = pyramid.TransformToNewSolid(translationMatrix);
```

This guide provides a basic introduction to `TessellatedSolid`. With these fundamentals, you can begin to explore more advanced topics like boolean operations, slicing, and geometric analysis.
