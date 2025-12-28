# How-To: Basic Vector and Geometric Operations

The `TessellationVoxelizationGeometryLibrary` (TVGL) provides a rich set of functions for performing common vector and geometric calculations. These functions are typically available as extension methods on `Vector3`, `Vertex`, `TriangleFace`, and other geometric objects, making them easy to discover and use. This guide will walk you through some of the most fundamental operations.

## Finding the Extremes: Min and Max Points

A common task in computational geometry is to find the points in a set that are the furthest or closest along a specific direction. This is useful for calculating bounding boxes, determining the dimensions of a shape, or as a step in more complex algorithms.

TVGL provides `GetMinVertexDistanceAlongVector` and `GetMaxVertexDistanceAlongVector` for this purpose. For greater efficiency, you can get both at the same time with `GetMinAndMaxAlongVector`.

### Example: Finding the Height of a Solid

Let's say you want to find the highest and lowest points of a `TessellatedSolid` along the Z-axis.

```csharp
// Assume 'solid' is a TVGL TessellatedSolid object
var zDirection = new Vector3(0, 0, 1);

// Find the lowest and highest points along the Z-axis
var (minPoint, minDistance, maxPoint, maxDistance) = solid.Vertices.GetMinAndMaxAlongVector(zDirection);

// The height of the solid is the difference between the max and min distances
var height = maxDistance - minDistance;

Console.WriteLine($"The solid's lowest point is at Z = {minPoint.Z}");
Console.WriteLine($"The solid's highest point is at Z = {maxPoint.Z}");
Console.WriteLine($"The total height is {height}");
```

## Projections: Moving from 3D to 2D

Many geometric problems are simpler to solve in 2D. TVGL allows you to project 3D geometry onto a 2D plane for analysis. The key method for this is `ProjectTo2DCoordinates`. You provide a direction vector, which becomes the normal of the 2D plane (it gets "flattened" out).

The method also provides a `backTransform` matrix, which is crucial for converting your 2D results back into the original 3D coordinate system.

### Example: Slicing a Solid

A common application of projection is creating a 2D "slice" of a 3D solid.

```csharp
// Assume 'solid' is a TVGL TessellatedSolid object
// We want to create a slice perpendicular to the Z-axis
var sliceNormal = new Vector3(0, 0, 1);

// Project all vertices onto the XY plane
var projectedVertices = solid.Vertices.ProjectTo2DCoordinates(sliceNormal, out var backTransform);

// Now you can work with the 'projectedVertices' (a collection of Vector2)
// to perform 2D operations like polygon clipping, offsetting, etc.

// To convert a 2D point from the slice back to 3D:
var point2D = new Vector2(10.0, 5.0);
var point3D = point2D.ConvertTo3DLocation(backTransform);
```

## Intersections and Distances

TVGL provides a comprehensive set of methods for calculating intersections and distances between geometric primitives.

### Line-Triangle Intersection

A fundamental query in raytracing and solid modeling is to find where a line or ray intersects with a triangle. You can use `PointOnTriangleFromLineSegment` or `PointOnTriangleFromRay`.

```csharp
// Assume 'face' is a TriangleFace object
var rayOrigin = new Vector3(0, 0, 10);
var rayDirection = new Vector3(0, 0, -1); // Firing a ray downwards

// Check for intersection
var intersectionPoint = face.PointOnTriangleFromRay(rayOrigin, rayDirection, out var distanceToHit);

if (!intersectionPoint.IsNull())
{
    Console.WriteLine($"Ray hit the triangle at {intersectionPoint}, at a distance of {distanceToHit}");
}
```

### Point-in-Polygon Test

To check if a point lies inside a solid, you often need to perform a point-in-polygon test on a 2D slice. The `IsVertexInsideTriangle` method is a robust way to check if a coplanar point is within a single triangle. For more complex polygons, you would typically use a ray-casting algorithm on the 2D polygon slice.

```csharp
// Assume 'face' is a TriangleFace and 'point' is a Vector3
// that is known to be on the same plane as the face.

bool isInside = MiscFunctions.IsVertexInsideTriangle(face, point);

if (isInside)
{
    Console.WriteLine("The point is inside the triangle.");
}
```

This guide has covered just a few of the core utility functions in TVGL. By understanding these building blocks, you can start to tackle more complex geometric challenges.
