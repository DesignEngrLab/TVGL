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

## SVG visibility during import

`SVG.Open` filters hidden geometry before polygon containment/holes are constructed. Shape and ancestor-group presentation attributes, inline styles, and embedded stylesheets control `display`, `visibility`, and `opacity`. `display:none` and zero opacity suppress a subtree. Inherited `visibility:hidden`/`collapse` can be overridden by a visible child. Visible unfilled paths remain cut geometry.

Static CSS supports type/universal, ID/class/compound and simple attribute selectors, selector lists, descendant/child/adjacent/general-sibling combinators, specificity, source order, inline priority and `!important`. Attribute selectors support existence or equality with single-token values. `inherit`, `initial`, and `unset` resolve per property. External stylesheets, at-rules, nested rules, unsupported visibility selectors and unresolved visibility expressions throw `NotSupportedException`; resolve those styles to inline declarations before import. This avoids silently importing potentially hidden geometry. CSS scripts/animations and paint effects are not rendered.

Definition containers (`defs`, `symbol`, `clipPath`, `mask`, `pattern`, `marker`) do not contribute standalone contours. `<use>` instantiation, clipping and masking are not implemented by this filter. Filtering applies to both `Open` overloads; `includeOpenPaths: false` additionally excludes open annotations before containment.
