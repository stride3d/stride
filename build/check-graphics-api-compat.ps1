# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# Checks that the Direct3D 11, Direct3D 12 and Vulkan builds of Stride.Graphics have the same public API: a game compiles
# against one and runs on any, so a member missing on one is a MissingMethodException at run time. Run it after building
# the three (-p:StrideGraphicsApiDependentBuildAll=true). The members that differ on purpose are in the suppression file
# next to Stride.Graphics.csproj; -GenerateSuppressionFile writes all the differences found to a new file to compare.
param(
    [string]$Configuration = 'Debug',
    [switch]$GenerateSuppressionFile
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repository 'sources/engine/Stride.Graphics'
$bin = Join-Path $project "bin/$Configuration/net10.0"
$suppressionFile = Join-Path $project 'GraphicsApiCompat.BackendOnly.xml'

# The assemblies the API refers to: the .NET reference pack, the Stride assemblies next to each build, and the compile
# assets of the NuGet packages (the native bindings are not copied to the output)
$dotnetRoot = Split-Path (Get-Command dotnet).Source
$referencePack = Get-ChildItem (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Where-Object Name -like '10.*' | Sort-Object { [version]($_.Name -replace '-.*') } | Select-Object -Last 1
$references = [System.Collections.Generic.List[string]]::new()
$references.Add((Join-Path $referencePack.FullName 'ref/net10.0'))
$assets = Get-Content (Join-Path $project 'obj/project.assets.json') -Raw | ConvertFrom-Json
$packageFolders = @($assets.packageFolders.PSObject.Properties.Name)
foreach ($library in $assets.targets.'net10.0'.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { continue }
    $libraryPath = $assets.libraries.($library.Name).path
    foreach ($group in 'compile', 'runtime') {
        $files = @($library.Value.$group.PSObject.Properties.Name | Where-Object { $_ -and $_ -notlike '*/_._' })
        if ($files.Count -eq 0) { continue }
        foreach ($file in $files) {
            $path = $packageFolders | ForEach-Object { Join-Path (Join-Path $_ $libraryPath) $file } | Where-Object { Test-Path $_ } | Select-Object -First 1
            if ($path) { $references.Add($path) }
        }
        break
    }
}

# Direct3D 11 against each of the others, both ways: together, the four find a member missing on any one of the three
$pairs = @(('Direct3D11', 'Direct3D12'), ('Direct3D12', 'Direct3D11'), ('Direct3D11', 'Vulkan'), ('Vulkan', 'Direct3D11'))
$arguments = [System.Collections.Generic.List[string]]::new()
foreach ($pair in $pairs) {
    foreach ($side in @(@{ Name = $pair[0]; Assembly = '-l'; References = '--lref' }, @{ Name = $pair[1]; Assembly = '-r'; References = '--rref' })) {
        $assembly = Join-Path $bin "$($side.Name)/Stride.Graphics.dll"
        if (-not (Test-Path $assembly)) { throw "$assembly is missing: build Stride.Graphics with -p:StrideGraphicsApiDependentBuildAll=true first." }
        $arguments.Add($side.Assembly); $arguments.Add($assembly)
        $arguments.Add($side.References); $arguments.Add((@(Join-Path $bin $side.Name) + $references) -join ',')
    }
}
# The suppressions name the graphics API rather than the path of its build
foreach ($option in '--left-assemblies-transformation-pattern', '--right-assemblies-transformation-pattern') {
    $arguments.Add($option); $arguments.Add('.*(Direct3D11|Direct3D12|Vulkan).Stride.Graphics.dll;$1')
}
$arguments.Add('--create-work-item-per-assembly')
$arguments.Add('--suppression-file'); $arguments.Add($suppressionFile)
if ($GenerateSuppressionFile) {
    $arguments.Add('--generate-suppression-file')
    $arguments.Add('--suppression-output-file'); $arguments.Add((Join-Path $project 'GraphicsApiCompat.Generated.xml'))
}

# The tool writes its errors to stderr: keep them as output lines
$ErrorActionPreference = 'Continue'
$output = dotnet dnx --yes Microsoft.DotNet.ApiCompat.Tool@10.0.403 -- @arguments 2>&1 | ForEach-Object { $line = "$_"; Write-Host $line; $line }
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Host "The graphics APIs differ in the public API of Stride.Graphics. Add the member to the backends that lack it; if it differs on purpose, add it to $suppressionFile."
    exit $exitCode
}
# The tool reports a suppression of a difference that is gone without failing: keep the file exact
if ($output -match '^Unnecessary suppressions found') {
    Write-Host "Remove the suppressions above from $suppressionFile."
    exit 1
}
Write-Host 'Direct3D 11, Direct3D 12 and Vulkan have the same public API in Stride.Graphics.'
