param(
    [string]$PackageVersion = "2.0.0",
    [string]$ArtifactDirectory
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    $ArtifactDirectory = Join-Path $PSScriptRoot "..\artifacts\nuget\PolygonImportExport\$PackageVersion"
}

$artifactPath = [System.IO.Path]::GetFullPath($ArtifactDirectory)
$packagePath = Join-Path $artifactPath "TVGL.PolygonImportExport.$PackageVersion.nupkg"
$projectPath = Join-Path $PSScriptRoot "PolygonImportExport.PackageSmokeTest.csproj"

if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Package not found: $packagePath. Run dotnet pack before running this smoke test."
}

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    "PolygonImportExport-PackageSmokeTest-" + (Get-Date -Format "yyyyMMdd-HHmmssfff"))
$packagesPath = Join-Path $testRoot "packages"
$configPath = Join-Path $testRoot "NuGet.Config"
New-Item -ItemType Directory -Path $testRoot | Out-Null

$escapedArtifactPath = [System.Security.SecurityElement]::Escape($artifactPath)
$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="PolygonImportExport-local" value="$escapedArtifactPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="PolygonImportExport-local">
      <package pattern="TVGL.PolygonImportExport" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@
[System.IO.File]::WriteAllText($configPath, $config)

$previousPackagesPath = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = $packagesPath

    Write-Host "Restoring TVGL.PolygonImportExport $PackageVersion from $artifactPath"
    dotnet restore $projectPath `
        --configfile $configPath `
        --force-evaluate `
        -p:PolygonImportExportPackageVersion=$PackageVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed with exit code $LASTEXITCODE."
    }

    dotnet run `
        --project $projectPath `
        --configuration Release `
        --no-restore `
        -p:PolygonImportExportPackageVersion=$PackageVersion
    if ($LASTEXITCODE -ne 0) {
        throw "Package smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:NUGET_PACKAGES = $previousPackagesPath
}
