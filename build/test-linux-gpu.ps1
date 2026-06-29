<#
.SYNOPSIS
    Reproduce CI Linux GPU tests locally using WSL2.
    Matches the test-linux-game.yml workflow exactly.

.DESCRIPTION
    1. Builds GPU test projects on Windows targeting Linux/Vulkan
    2. Copies SwiftShader Linux native to test outputs
    3. Runs tests via WSL2 with SwiftShader ICD

.PARAMETER Filter
    Optional test filter (e.g. "TestDrawQuad" or "Stride.Graphics.Tests")

.PARAMETER SkipBuild
    Skip the build step (use previously built binaries).

.PARAMETER VsTestArgs
    Additional arguments forwarded to dotnet vstest.

.EXAMPLE
    .\build\test-linux-gpu.ps1
    .\build\test-linux-gpu.ps1 -Filter "TestDynamicSpriteFont"
    .\build\test-linux-gpu.ps1 -SkipBuild
    .\build\test-linux-gpu.ps1 -- --Logger:console
#>
param(
    [string]$Filter,
    [switch]$SkipBuild,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$VsTestArgs
)

$ErrorActionPreference = "Stop"
$env:STRIDE_GRAPHICS_SOFTWARE_RENDERING = "1"

if (-not $SkipBuild) {
Write-Host "=== Step 1: Clean ===" -ForegroundColor Cyan
$testProjects = @(
    "Stride.Graphics.Tests",
    "Stride.UI.Tests"
)
foreach ($proj in $testProjects) {
    Remove-Item -Recurse -Force "bin\Tests\$proj\Linux" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "sources\engine\$proj\obj\stride" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "sources\engine\$proj\obj\Linux-Vulkan" -ErrorAction SilentlyContinue
}

Write-Host "=== Step 2: Build (targeting Linux/Vulkan) ===" -ForegroundColor Cyan
dotnet build build\Stride.Tests.Game.GPU.slnf `
    -nr:false `
    -v:m -p:WarningLevel=0 `
    -p:Configuration=Debug `
    -p:StridePlatform=Linux `
    -p:StrideGraphicsApis=Vulkan `
    -p:StrideGraphicsApi=Vulkan
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "=== Step 3: Copy SwiftShader Linux native ===" -ForegroundColor Cyan
$swiftShaderDir = Get-ChildItem -Recurse -Path "$env:USERPROFILE\.nuget\packages\stride.dependencies.swiftshader" -Directory -Filter linux-x64 -ErrorAction SilentlyContinue | Select-Object -First 1
if ($swiftShaderDir) {
    Get-ChildItem bin\Tests -Directory | ForEach-Object {
        $dest = Join-Path $_.FullName "Linux\Vulkan\runtimes\linux-x64\native"
        if (Test-Path (Join-Path $_.FullName "Linux\Vulkan")) {
            New-Item -Path $dest -ItemType Directory -Force | Out-Null
            Copy-Item "$($swiftShaderDir.Parent.FullName)\linux-x64\native\*" $dest -Force
            Write-Host "  Copied SwiftShader to $dest"
        }
    }
} else {
    Write-Warning "SwiftShader linux-x64 not found in NuGet cache"
}

} else {
    Write-Host "=== Skipping build (using existing binaries) ===" -ForegroundColor Yellow
}

Write-Host "=== Step 4: Setup WSL2 ===" -ForegroundColor Cyan
# Check WSL is available
$wslStatus = wsl --status 2>&1
if ($LASTEXITCODE -ne 0) { throw "WSL2 is not available. Install it with: wsl --install" }

# Ensure required packages are installed
$setupScript = "$env:TEMP\stride_wsl_setup.sh"
@"
#!/bin/bash
dpkg -s libvulkan1 > /dev/null 2>&1 || (echo "Installing dependencies..." && sudo apt-get update -qq && sudo apt-get install -y -qq libvulkan1 libopenal-dev)
"@ | ForEach-Object { [System.IO.File]::WriteAllText($setupScript, $_, [System.Text.UTF8Encoding]::new($false)) }
wsl -- /bin/bash (wsl wslpath -u ($setupScript -replace '\\', '/'))

# Ensure dotnet is available in WSL
$dotnetCheck = wsl -- /bin/bash -c "ls `$HOME/.dotnet/dotnet 2>/dev/null || which dotnet 2>/dev/null"
if (-not $dotnetCheck) { throw "dotnet not found in WSL. Install .NET SDK: https://learn.microsoft.com/dotnet/core/install/linux" }

Write-Host "=== Step 5: Run tests via WSL2 ===" -ForegroundColor Cyan
$wslScript = @'
#!/bin/bash
# Find dotnet - check common locations
for d in "$HOME/.dotnet" "/usr/share/dotnet" "/usr/lib/dotnet"; do
  if [ -x "$d/dotnet" ]; then
    export PATH="$d:$PATH"
    export DOTNET_ROOT="$d"
    break
  fi
done
export STRIDE_GRAPHICS_SOFTWARE_RENDERING=1
export STRIDE_MAX_PARALLELISM=8
cd /mnt/c/dev/stride2

# Register SwiftShader ICD
LIB=$(find bin/Tests -name libvk_swiftshader.so -path "*/linux-x64/*" | head -1)
if [ -n "$LIB" ]; then
  LIB_ABS=$(readlink -f "$LIB")
  ICD_JSON="$PWD/vk_swiftshader_icd.json"
  echo "{\"file_format_version\":\"1.0.0\",\"ICD\":{\"library_path\":\"$LIB_ABS\",\"api_version\":\"1.1.0\"}}" > "$ICD_JSON"
  export VK_DRIVER_FILES="$ICD_JSON"
  echo "SwiftShader ICD: $LIB_ABS"
fi

FILTER_ARG=""
if [ -n "$1" ]; then
  FILTER_ARG="--TestCaseFilter:DisplayName~$1"
fi
shift 2>/dev/null  # remaining args are extra vstest args
EXTRA_ARGS="$@"

echo ""
echo "=== Graphics Tests ==="
dotnet vstest bin/Tests/Stride.Graphics.Tests/Linux/Vulkan/Stride.Graphics.Tests.dll \
  $FILTER_ARG $EXTRA_ARGS -- RunConfiguration.MaxCpuCount=1

echo ""
echo "=== UI Tests ==="
dotnet vstest bin/Tests/Stride.UI.Tests/Linux/Vulkan/Stride.UI.Tests.dll \
  $FILTER_ARG $EXTRA_ARGS -- RunConfiguration.MaxCpuCount=1
'@

$wslScriptPath = "$env:TEMP\stride_linux_gpu_test.sh"
# Write without BOM for bash compatibility
[System.IO.File]::WriteAllText($wslScriptPath, $wslScript, [System.Text.UTF8Encoding]::new($false))

$wslScriptLinux = wsl wslpath -u ($wslScriptPath -replace '\\', '/')
$extraArgs = if ($VsTestArgs) { $VsTestArgs -join ' ' } else { '' }
wsl -- /bin/bash $wslScriptLinux $Filter $extraArgs

Write-Host ""
Write-Host "Done. Use 'tests\compare-gold.cmd' to review results visually." -ForegroundColor Green
