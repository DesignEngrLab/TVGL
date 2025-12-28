# How-To: Using Primitive Surfaces

In TVGL, a `PrimitiveSurface` is a mathematical representation of a simple, unbounded geometric shape, such as a plane, a cylinder, or a sphere. These primitives are incredibly useful for a few key reasons:

1.  **Recognition:** TVGL can analyze a `TessellatedSolid` and identify regions of its surface that correspond to these primitive shapes. This allows you to understand the underlying geometry of a mesh.
2.  **Accuracy:** For operations like slicing or calculating intersections, using the precise mathematical formula of a primitive is often faster and more accurate than working with a dense collection of triangles.
3.  **Geometric Queries:** Primitives make it easy to perform geometric queries like finding the shortest distance from a point to the surface or determining if a point is inside the shape.

This guide will focus on the most fundamental primitive: the `Plane`.

## What is a Plane Primitive?

A `Plane` in TVGL is defined by two properties:

-   `Normal`: A `Vector3` that is perpendicular to the plane's surface. This vector defines the plane's orientation and is always normalized (it has a length of 1).
-   `DistanceToOrigin`: A `double` that represents the shortest distance from the world origin (0,0,0) to the plane.

By convention, the `Normal` vector points "out" of the solid.

## Creating a Plane

There are several ways to define a `Plane`.

### From a Point and a Normal

This is the most direct way to define a plane's orientation and position.

```csharp
// Define a normal vector (it will be normalized automatically)
var normal = new Vector3(0, 0, 1);

// Define a point that the plane passes through
var pointOnPlane = new Vector3(10, 5, 3);

// Create the plane
var myPlane = new Plane(pointOnPlane, normal);

// The DistanceToOrigin will be calculated as 3.0
```

### From Three Points

Any three non-collinear points are guaranteed to define a single unique plane.

```csharp
var pointA = new Vector3(0, 0, 5);
var pointB = new Vector3(10, 0, 5);
var pointC = new Vector3(0, 10, 5);

// Create a plane that passes through all three points
var myPlane = Plane.CreateFromVertices(pointA, pointB, pointC);

// The plane's normal will be (0, 0, 1) and its DistanceToOrigin will be 5.0
```

## Using Planes for Geometric Queries

Once you have a `Plane` object, you can use it to perform geometric tests.

### Distance from a Point to the Plane

You can find the shortest distance from any point in space to the plane. The distance is *signed*: a positive value means the point is on the same side as the normal vector (the "outside"), while a negative value means it's on the opposite side (the "inside").

```csharp
// Assume 'myPlane' has a normal of (0,0,1) and a DistanceToOrigin of 5.0
var testPoint = new Vector3(2, 2, 7);

// Calculate the distance
var distance = myPlane.DistanceToPoint(testPoint); // Result will be 2.0

var anotherPoint = new Vector3(2, 2, 3);
var anotherDistance = myPlane.DistanceToPoint(anotherPoint); // Result will be -2.0
```

### Line-Plane Intersection

You can find the exact point where a line (or ray) intersects the plane.

```csharp
// Define a ray starting at (0,0,10) and pointing downwards
var rayOrigin = new Vector3(0, 0, 10);
var rayDirection = new Vector3(0, 0, -1);

// Find the intersection point
var (intersectionPoint, distanceAlongRay) = myPlane.LineIntersection(rayOrigin, rayDirection).First();

if (!intersectionPoint.IsNull())
{
    // intersectionPoint will be (0, 0, 5)
    // distanceAlongRay will be 5.0
    Console.WriteLine($"The ray intersects the plane at {intersectionPoint}.");
}
```

Understanding how to create and use `Plane` primitives is the first step to leveraging TVGL's powerful primitive recognition and analysis capabilities. Other primitives like `Cylinder` and `Sphere` follow similar principles.
