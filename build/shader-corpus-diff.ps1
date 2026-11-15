# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# Compiles the shader corpus with a base commit and with the working tree, and writes what changed, like the Shader corpus
# CI jobs (sources/shaders/Stride.Shaders.Tests/Corpus/README.md). The base is built in a worktree of its own, in
# -WorkDirectory, and its result is kept for the next run on the same base and corpus. Exit code 1 when the output changed.
param(
    # Default: where the current branch left master
    [string]$Base,
    # Comma-separated, e.g. Vulkan; default: every platform
    [string]$Platforms = '',
    [string]$WorkDirectory
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path $PSScriptRoot -Parent
if (-not $WorkDirectory) { $WorkDirectory = "$repository.corpus" }
$project = 'sources/shaders/Stride.Shaders.Tests'
$corpus = 'sources/shaders/Stride.Shaders.Tests/Corpus/corpus.json'

function Invoke-Git { $result = git -C $repository @args; if ($LASTEXITCODE -ne 0) { throw "git $args failed" }; $result }

if (-not $Base) {
    $master = if (git -C $repository rev-parse --verify --quiet origin/master) { 'origin/master' } else { 'master' }
    $Base = Invoke-Git merge-base HEAD $master
}
$baseCommit = Invoke-Git rev-parse "$Base^{commit}"
$key = "$baseCommit $(Invoke-Git hash-object (Join-Path $repository $corpus)) $Platforms"
$tree = Join-Path $WorkDirectory 'tree'
$baseline = Join-Path $WorkDirectory 'base'
$output = Join-Path $WorkDirectory 'head'
$keyFile = Join-Path $baseline 'key.txt'

$savedEnvironment = @{}
function Set-Environment($name, $value) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $value)
}

try {
    Set-Environment STRIDE_SHADER_CORPUS_PLATFORMS $Platforms
    Set-Environment STRIDE_SHADER_CORPUS_BASELINE $baseline
    Set-Environment STRIDE_SHADER_CORPUS_OUTPUT $output

    if ((Test-Path $keyFile) -and (Get-Content $keyFile -Raw).Trim() -eq $key) {
        Write-Host "Baseline of $($baseCommit.Substring(0, 10)) already compiled: $baseline"
    } else {
        Write-Host "Compiling the baseline with $($baseCommit.Substring(0, 10)) in $tree"
        if (Test-Path (Join-Path $tree '.git')) {
            git -C $tree checkout --force --detach $baseCommit
        } else {
            New-Item -ItemType Directory -Force $WorkDirectory | Out-Null
            Invoke-Git worktree add --detach $tree $baseCommit
        }
        if ($LASTEXITCODE -ne 0) { throw "Checkout of $baseCommit failed" }
        # The same permutations on both sides: only the compiler and the shaders differ
        Copy-Item (Join-Path $repository $corpus) (Join-Path $tree $corpus) -Force
        Push-Location $tree
        try {
            dotnet test $project --filter 'FullyQualifiedName~ShaderCorpusTests.SaveBaseline' -- xUnit.Explicit=on
            if ($LASTEXITCODE -ne 0) { throw "The baseline run failed (does $Base have the shader corpus check?)" }
        } finally {
            Pop-Location
        }
        Set-Content $keyFile $key
    }

    if (Test-Path $output) { Remove-Item -Recurse -Force $output }
    Push-Location $repository
    try {
        dotnet test $project --filter 'FullyQualifiedName~ShaderCorpusTests.MatchesBaseline' -- xUnit.Explicit=on | Out-Null
        $status = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    $report = Join-Path $output 'report.txt'
    if (-not (Test-Path $report)) { throw "The comparison did not run, see: dotnet test $project --filter FullyQualifiedName~ShaderCorpusTests.MatchesBaseline -- xUnit.Explicit=on" }
    Write-Host (Get-Content $report -TotalCount 1)
    Write-Host "Report: $report"
    if (Test-Path (Join-Path $output 'diff.txt')) { Write-Host "Diff: $(Join-Path $output 'diff.txt')" }
} finally {
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name]) }
}
exit ([int]($status -ne 0))
