# Checks the packages of a release before they are published, from the repo root. See docs/build/versioning.md.
# Every package carries the release's version, every Stride.* dependency can be restored from these packages or nuget.org,
# and the samples this engine names exist. A package nobody can restore passes every build, and can't be taken back.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$PackagesDir = 'bin/packages'
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Released on its own version: docs/build/versioning.md, "Crash reporter package version".
$ownVersion = @('Stride.CrashReporter')
$publishedVersions = @{}

function Get-Published([string]$id) {
    $key = $id.ToLowerInvariant()
    if (-not $publishedVersions.ContainsKey($key)) {
        try { $publishedVersions[$key] = @((Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$key/index.json").versions) }
        catch {
            # Only an unknown package means not published: anything else must not pass as a missing dependency.
            if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
            $publishedVersions[$key] = @()
        }
    }
    $publishedVersions[$key]
}

# The lowest version of a NuGet range ([4.5.0-beta], 4.5.0-beta, [1.0,2.0)), in the form the feed lists: three parts at least.
function Get-Floor([string]$range) {
    $floor = (($range -replace '^[\[\(]', '' -replace '[\]\)]$', '') -split ',')[0].Trim() -replace '\+.*$', ''
    if ($floor -eq '') { return '' }
    $parts = $floor -split '-', 2
    $numbers = @($parts[0] -split '\.')
    while ($numbers.Count -lt 3) { $numbers += '0' }
    if ($numbers.Count -eq 4 -and $numbers[3] -eq '0') { $numbers = $numbers[0..2] }
    (($numbers -join '.') + $(if ($parts.Count -gt 1) { "-$($parts[1])" })).ToLowerInvariant()
}

$packages = foreach ($file in Get-ChildItem $PackagesDir -Filter *.nupkg) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
        $reader = New-Object System.IO.StreamReader($entry.Open())
        $nuspec = [xml]$reader.ReadToEnd()
        $reader.Dispose()
        [pscustomobject]@{
            Id = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='id']").InnerText
            # NuGet ignores the build metadata (+g<sha>) when it compares versions.
            PackageVersion = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='version']").InnerText -replace '\+.*$', ''
            Dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']") | ForEach-Object { [pscustomobject]@{ Id = $_.GetAttribute('id'); Floor = Get-Floor $_.GetAttribute('version') } })
        }
    }
    finally { $zip.Dispose() }
}

$errors = [System.Collections.Generic.List[string]]::new()
$local = @{}
foreach ($package in $packages) { $local["$($package.Id.ToLowerInvariant())/$($package.PackageVersion.ToLowerInvariant())"] = $true }

foreach ($package in $packages) {
    if ($ownVersion -notcontains $package.Id -and $package.PackageVersion -ne $Version) {
        $errors.Add("$($package.Id) is on $($package.PackageVersion), not $Version.")
    }
}

function Test-Available([string]$id, [string]$floor) {
    $local["$($id.ToLowerInvariant())/$floor"] -or ((Get-Published $id) -contains $floor)
}

$checked = @{}
foreach ($package in $packages) {
    foreach ($dependency in $package.Dependencies) {
        if (-not $dependency.Id.StartsWith('Stride', [StringComparison]::OrdinalIgnoreCase) -or $dependency.Floor -eq '') { continue }
        $key = "$($dependency.Id.ToLowerInvariant())/$($dependency.Floor)"
        if (-not $checked.ContainsKey($key)) { $checked[$key] = Test-Available $dependency.Id $dependency.Floor }
        if (-not $checked[$key]) { $errors.Add("$($package.Id) depends on $($dependency.Id) $($dependency.Floor), which is neither in these packages nor on nuget.org.") }
    }
}

# AssetPacks has no dependency to find it by: Game Studio fetches it on demand, on the number the samples share.
$gameStudio = $packages | Where-Object { $_.Id -eq 'Stride.GameStudio' }
if (-not $gameStudio) { $errors.Add('Stride.GameStudio is not in these packages.') }
else {
    $pin = ($gameStudio.Dependencies | Where-Object { $_.Id -eq 'Stride.Templates.Samples' } | Select-Object -First 1).Floor
    if (-not $pin) { $errors.Add('Stride.GameStudio has no dependency on Stride.Templates.Samples.') }
    elseif (-not (Test-Available 'Stride.Templates.AssetPacks' $pin)) { $errors.Add("Stride.Templates.AssetPacks $pin is neither in these packages nor on nuget.org, and this engine names it.") }
    else { echo "Samples named by this engine: $pin" }
}

foreach ($message in $errors | Select-Object -Unique) { echo "::error::$message" }
if ($errors.Count -gt 0) { exit 1 }
echo "$($packages.Count) packages on $Version, $($checked.Count) Stride.* dependencies available."
