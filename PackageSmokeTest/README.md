# TVGL package smoke test

This standalone console project verifies the packaged TVGL artifact rather than referencing the TVGL source project. It restores `TVGL` only from the local artifact directory while restoring transitive dependencies from NuGet.org into an isolated temporary package cache.

First create the package from the repository root:

```powershell
dotnet pack .\TessellationAndVoxelizationGeometryLibrary\TessellationAndVoxelizationGeometryLibrary.csproj `
  --configuration Release `
  --output .\artifacts\nuget\2.0.0
```

Then run the smoke test:

```powershell
.\PackageSmokeTest\Run-PackageSmokeTest.ps1
```

To test another version or artifact directory:

```powershell
.\PackageSmokeTest\Run-PackageSmokeTest.ps1 `
  -PackageVersion "2.0.0" `
  -ArtifactDirectory "C:\packages\TVGL"
```

The test fails if the package cannot be restored, the consumer cannot compile, convex-hull construction fails, or the expected tetrahedral hull topology is not produced.
