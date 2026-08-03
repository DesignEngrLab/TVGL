# TVGL.PolygonImportExport

`TVGL.PolygonImportExport` reads and writes two-dimensional polygon geometry using TVGL types.

## Installation

```powershell
dotnet add package TVGL.PolygonImportExport --version 2.0.0
```

## Supported formats

- SVG paths and common SVG shape elements
- DXF two-dimensional entities
- DWG two-dimensional entities

Curves, arcs, circles, ellipses, splines, and supported block entities are tessellated into TVGL polygons. Format support differs between import and export, and complex source files should be validated after conversion.

## Example

```csharp
using PolygonImportExport;

var polygons = SVG.Open("part.svg");

if (!DXF.Save("part.dxf", polygons))
    throw new InvalidOperationException("The DXF file could not be written.");
```

SVG coordinates use a downward-positive Y axis. `SVG.Open` converts them to upward-positive coordinates by default; pass `positiveYIsUp: false` to preserve the SVG coordinate orientation.

## Dependencies

This package depends on `TVGL` for polygon and vector types and on `ACadSharp` for DXF and DWG support.

## License

TVGL.PolygonImportExport is distributed under the MIT License.
