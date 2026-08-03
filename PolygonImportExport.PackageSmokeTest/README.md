# TVGL.PolygonImportExport package smoke test

This standalone console project verifies the packed `TVGL.PolygonImportExport` artifact rather than referencing its source project. It restores the package from an isolated local feed, imports a simple SVG polygon, exports it, and imports the result again.

From the repository root, create the package:

```powershell
dotnet pack .\PolygonImportExport\PolygonImportExport.csproj `
  --configuration Release `
  --output .\artifacts\nuget\PolygonImportExport\2.0.0
```

Then run the smoke test:

```powershell
.\PolygonImportExport.PackageSmokeTest\Run-PackageSmokeTest.ps1
```
