<#
.SYNOPSIS
    Builds Stride Game Studio and prepares the environment for MCP integration tests.

.DESCRIPTION
    This script automates the complex build pipeline required before running MCP
    integration tests. It must be run once (or after code changes) before tests.

    Steps performed:
    1. Locate Visual Studio MSBuild (required - dotnet build cannot handle native C++ deps)
    2. Build Game Studio with StrideSkipAutoPack=true
    3. Copy missing pruned framework DLLs to build output (workaround for .NET 10 issue)
    4. Pack the Stride.GameStudio NuGet package to the local dev feed
    5. Build the integration test project

.EXAMPLE
    .\bootstrap.ps1
    .\bootstrap.ps1 -Configuration Release
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

Write-Host "=== MCP Integration Test Bootstrap ===" -ForegroundColor Cyan
Write-Host "Repository root: $RepoRoot"
Write-Host "Configuration:   $Configuration"
Write-Host ""

# --- Step 1: Locate MSBuild ---
Write-Host "[1/5] Locating Visual Studio MSBuild..." -ForegroundColor Yellow

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath 2>$null
    if ($vsPath) {
        $MSBuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
    }
}

if (-not $MSBuild -or -not (Test-Path $MSBuild)) {
    # Fallback to known path
    $MSBuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $MSBuild)) {
    Write-Error @"
Could not locate Visual Studio MSBuild.
Game Studio requires VS MSBuild (not 'dotnet build') because it transitively
depends on native C++ vcxproj files.

Please install Visual Studio with the 'Desktop development with C++' workload.
"@
    exit 1
}

Write-Host "  Found: $MSBuild" -ForegroundColor Green

# --- Step 2: Build Game Studio ---
Write-Host ""
Write-Host "[2/5] Building Game Studio ($Configuration)..." -ForegroundColor Yellow

$GameStudioCsproj = Join-Path $RepoRoot "sources\editor\Stride.GameStudio\Stride.GameStudio.csproj"

& $MSBuild $GameStudioCsproj `
    "-p:Configuration=$Configuration" `
    "-p:StrideSkipAutoPack=true" `
    "-verbosity:quiet" `
    "-m"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Game Studio build failed with exit code $LASTEXITCODE"
    exit 1
}

$BuildOutput = Join-Path $RepoRoot "sources\editor\Stride.GameStudio\bin\$Configuration\net10.0-windows"
$GameStudioExe = Join-Path $BuildOutput "Stride.GameStudio.exe"

if (-not (Test-Path $GameStudioExe)) {
    Write-Error "Build succeeded but Stride.GameStudio.exe not found at: $GameStudioExe"
    exit 1
}

Write-Host "  Build succeeded: $GameStudioExe" -ForegroundColor Green

# --- Step 3: Copy pruned framework DLLs ---
Write-Host ""
Write-Host "[3/5] Copying pruned framework DLLs to build output..." -ForegroundColor Yellow

# These DLLs are explicitly referenced by Stride.NuGetResolver.Targets.projitems
# (lines 38-40) for inclusion in the NuGet package. On .NET 10, they may be pruned
# from the build output because they're part of the shared framework.
$PrunedDlls = @(
    "Microsoft.Extensions.FileProviders.Abstractions.dll",
    "Microsoft.Extensions.FileSystemGlobbing.dll",
    "Microsoft.Extensions.Primitives.dll"
)

# Find the ASP.NET Core shared framework directory
$AspNetFrameworkDir = Get-ChildItem "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App" -Directory |
    Sort-Object { [Version]$_.Name } -ErrorAction SilentlyContinue |
    Select-Object -Last 1

if (-not $AspNetFrameworkDir) {
    Write-Warning "Could not locate ASP.NET Core shared framework. NuGet pack may fail."
} else {
    $CopiedCount = 0
    foreach ($dll in $PrunedDlls) {
        $target = Join-Path $BuildOutput $dll
        if (-not (Test-Path $target)) {
            $source = Join-Path $AspNetFrameworkDir.FullName $dll
            if (Test-Path $source) {
                Copy-Item $source $target
                Write-Host "  Copied: $dll" -ForegroundColor Green
                $CopiedCount++
            } else {
                Write-Warning "  Source not found: $source"
            }
        } else {
            Write-Host "  Already exists: $dll" -ForegroundColor DarkGray
        }
    }
    if ($CopiedCount -eq 0) {
        Write-Host "  All DLLs already present." -ForegroundColor Green
    }
}

# --- Step 4: Pack NuGet package ---
Write-Host ""
Write-Host "[4/5] Packing Stride.GameStudio NuGet package..." -ForegroundColor Yellow

& $MSBuild $GameStudioCsproj `
    "-t:Pack" `
    "-p:Configuration=$Configuration" `
    "-p:StrideSkipAutoPack=true" `
    "-verbosity:quiet" `
    "-m"

if ($LASTEXITCODE -ne 0) {
    Write-Error @"
NuGet pack failed with exit code $LASTEXITCODE.
This is a known issue on .NET 10 when pruned framework DLLs are missing.
Check that Step 3 copied the required DLLs successfully.
"@
    exit 1
}

# Verify the package was deployed to the dev feed
$DevFeed = Join-Path $env:LOCALAPPDATA "Stride\NugetDev"
$PackageExists = Get-ChildItem "$DevFeed\Stride.GameStudio.*.nupkg" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($PackageExists) {
    Write-Host "  Package deployed to: $($PackageExists.FullName)" -ForegroundColor Green
} else {
    Write-Warning "  Package not found in dev feed at: $DevFeed"
    Write-Warning "  Game Studio may fail to open projects."
}

# --- Step 5: Build test project ---
Write-Host ""
Write-Host "[5/5] Building integration test project..." -ForegroundColor Yellow

$TestCsproj = Join-Path $RepoRoot "sources\editor\Stride.GameStudio.Mcp.Tests\Stride.GameStudio.Mcp.Tests.csproj"
dotnet build $TestCsproj --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "Test project build failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host "  Build succeeded." -ForegroundColor Green

# --- Done ---
Write-Host ""
Write-Host "=== Bootstrap complete! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the integration tests:" -ForegroundColor White
Write-Host '  $env:STRIDE_MCP_INTEGRATION_TESTS = "true"' -ForegroundColor White
Write-Host "  dotnet test $TestCsproj" -ForegroundColor White
Write-Host ""
