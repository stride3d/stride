# Does what a user does with freshly built packages, from the repo root: a new blank game, a sample from this engine's
# packs, and the same sample from the oldest packs we support upgrading from. Each is instantiated, upgraded if it's on
# an older version, restored and built. It also restores Stride.GameStudio, what a user installs. See docs/build/versioning.md.
#   -Version           the engine version the packages carry.
#   -PackagesDir        the folder holding them; -ExtraSources for more local feeds.
#   -Configuration      the build of the asset compiler that runs the upgrades.
#   -KeepWorkspace      keep the projects and logs, to look at a failure.
#   -MinSamplesVersion  the published packs of the oldest version we test. Templates exist from 4.4 on, so older versions
#                       can't be tested this way, though the upgraders start at 4.0.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$PackagesDir = 'bin/packages',
    [string[]]$ExtraSources = @(),
    [string]$Configuration = 'Release',
    [string]$MinSamplesVersion = '4.4.3',
    [string]$SampleTemplate = 'stride-touchinputs',
    [switch]$KeepWorkspace
)

$PackagesDir = (Resolve-Path $PackagesDir).Path
$assetCompiler = Get-Item "sources/assets/Stride.AssetCompiler/bin/$Configuration/net*.0/Stride.AssetCompiler.dll" | Where-Object { $_.FullName -notmatch '-windows' } | Select-Object -First 1
if (-not $assetCompiler) {
    echo "::error::The $Configuration asset compiler is not built."
    exit 1
}

$workspace = Join-Path ([System.IO.Path]::GetTempPath()) "stride-smoke-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory $workspace | Out-Null

# Like samples/nuget.config: Stride.* can come from either, so an older sample restores its released engine.
$sources = @($PackagesDir) + $ExtraSources
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$writer = [System.Xml.XmlWriter]::Create((Join-Path $workspace 'nuget.config'), $settings)
$writer.WriteStartElement('configuration')
$writer.WriteStartElement('packageSources')
$writer.WriteStartElement('clear'); $writer.WriteEndElement()
$writer.WriteStartElement('add'); $writer.WriteAttributeString('key', 'nuget.org'); $writer.WriteAttributeString('value', 'https://api.nuget.org/v3/index.json'); $writer.WriteEndElement()
for ($i = 0; $i -lt $sources.Count; $i++) {
    $writer.WriteStartElement('add'); $writer.WriteAttributeString('key', "local$i"); $writer.WriteAttributeString('value', $sources[$i]); $writer.WriteEndElement()
}
$writer.WriteEndElement()
$writer.WriteStartElement('packageSourceMapping')
$writer.WriteStartElement('clear'); $writer.WriteEndElement()
foreach ($key in @('nuget.org') + (0..($sources.Count - 1) | ForEach-Object { "local$_" })) {
    $writer.WriteStartElement('packageSource'); $writer.WriteAttributeString('key', $key)
    $patterns = if ($key -eq 'nuget.org') { '*', 'Stride', 'Stride.*' } else { 'Stride', 'Stride.*' }
    foreach ($pattern in $patterns) { $writer.WriteStartElement('package'); $writer.WriteAttributeString('pattern', $pattern); $writer.WriteEndElement() }
    $writer.WriteEndElement()
}
$writer.WriteEndElement()
$writer.WriteEndElement()
$writer.Dispose()

function Invoke-Logged([string]$log, [string[]]$arguments) {
    "> dotnet $($arguments -join ' ')" | Out-File $log -Append -Encoding utf8
    & dotnet @arguments *>&1 | ForEach-Object { "$_" } | Out-File $log -Append -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($arguments[0..1] -join ' ') exited with $LASTEXITCODE" }
}

function Get-EngineVersions([string]$directory) {
    foreach ($csproj in Get-ChildItem $directory -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch '\.stride-backup-' }) {
        ([xml](Get-Content $csproj.FullName -Raw)).SelectNodes("//*[local-name()='PackageReference'][@Include='Stride.Engine']") | ForEach-Object { $_.GetAttribute('Version') }
    }
}

# $package: a .nupkg path, or id::version from the feed. Each case has its own template hive: both samples packs have the same id.
function Test-Template([string]$name, [string]$package, [string]$template) {
    $hive = Join-Path $workspace "hive-$name"
    $log = Join-Path $workspace "$name.log"
    $directory = Join-Path $workspace $name
    $start = Get-Date
    Push-Location $workspace
    try {
        Invoke-Logged $log @('new', 'install', $package, '--debug:custom-hive', $hive)
        Invoke-Logged $log @('new', $template, '-n', $name, '-o', $directory, '--debug:custom-hive', $hive)
        $before = @(Get-EngineVersions $directory | Select-Object -Unique)
        if (($before.Count -ne 1) -or ($before[0] -ne $Version)) {
            $target = Get-ChildItem $directory -Recurse -Include *.slnx, *.sdpkg | Sort-Object { $_.Extension -ne '.slnx' }, { $_.FullName.Length } | Select-Object -First 1
            Invoke-Logged $log @($assetCompiler.FullName, 'upgrade', $target.FullName, '--no-backup')
        }
        $after = @(Get-EngineVersions $directory | Select-Object -Unique)
        if (($after.Count -ne 1) -or ($after[0] -ne $Version)) { throw "the project is on '$($after -join ', ')' after the upgrade, not $Version" }
        $solution = Get-ChildItem $directory -Recurse -Filter *.slnx | Sort-Object { $_.FullName.Length } | Select-Object -First 1
        $project = if ($solution) { $solution } else { Get-ChildItem $directory -Recurse -Filter *.Windows.csproj | Select-Object -First 1 }
        Invoke-Logged $log @('build', $project.FullName, '-nr:false', '-v:m', '-p:WarningLevel=0')
        Write-Host "PASS ${name}: from $($before -join ', ') to $Version ($([int]((Get-Date) - $start).TotalSeconds) s)"
        return $true
    }
    catch {
        Write-Host "::error::Smoke test $name failed: $_"
        if (Test-Path $log) { Get-Content $log -Tail 40 | ForEach-Object { Write-Host $_ } }
        return $false
    }
    finally {
        Pop-Location
    }
}

# Catches version conflicts in the whole graph, which reading the dependencies of each package can't.
function Test-GameStudioRestore {
    $name = 'SmokeGameStudio'
    $log = Join-Path $workspace "$name.log"
    $directory = Join-Path $workspace $name
    $start = Get-Date
    New-Item -ItemType Directory $directory | Out-Null
    Set-Content (Join-Path $directory "$name.csproj") "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>net10.0-windows</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><PackageReference Include=`"Stride.GameStudio`" Version=`"$Version`" /></ItemGroup></Project>"
    Push-Location $workspace
    try {
        Invoke-Logged $log @('restore', (Join-Path $directory "$name.csproj"), '-nodeReuse:false')
        Write-Host "PASS ${name}: Stride.GameStudio $Version restores ($([int]((Get-Date) - $start).TotalSeconds) s)"
        return $true
    }
    catch {
        Write-Host "::error::Smoke test $name failed: $_"
        if (Test-Path $log) { Get-Content $log -Tail 40 | ForEach-Object { Write-Host $_ } }
        return $false
    }
    finally {
        Pop-Location
    }
}

function Get-Package([string]$id, [string]$packageVersion) {
    $file = Join-Path $PackagesDir "$id.$packageVersion.nupkg"
    if (Test-Path $file) { return $file }
    foreach ($source in $ExtraSources) {
        $file = Join-Path $source "$id.$packageVersion.nupkg"
        if (Test-Path $file) { return $file }
    }
    "$id::$packageVersion"
}

$samplesVersion = ([xml](Get-Content sources/templates/StrideSamplesVersion.props -Raw)).SelectSingleNode("//*[local-name()='_StrideCommittedSamplesVersion']").InnerText

$results = @(
    Test-Template 'SmokeGame' (Get-Package 'Stride.Templates.Games' $Version) 'stride-game'
    Test-Template 'SmokeSample' (Get-Package 'Stride.Templates.Samples' $samplesVersion) $SampleTemplate
    Test-Template 'SmokeOldSample' "Stride.Templates.Samples::$MinSamplesVersion" $SampleTemplate
    Test-GameStudioRestore
)

if ($KeepWorkspace) { echo "Kept $workspace" }
else { try { Remove-Item $workspace -Recurse -Force -ErrorAction Stop } catch { echo "Could not remove $workspace" } }
if ($results -contains $false) { exit 1 }
