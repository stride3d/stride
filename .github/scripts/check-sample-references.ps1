# Checks the Stride.* references of the sample csprojs, from the repo root. See docs/build/versioning.md.
#   -RequireStable    a stable engine or pack can't have samples on a prerelease: error.
#   -WarnMissedGates  a sample below the newest upgrader gate would ship without that migration, because the pack stamps
#                     the engine version on every sample, whatever its csproj says: warning.
param(
    [switch]$RequireStable,
    [switch]$WarnMissedGates
)

function Get-Number([string]$version) {
    $parts = @(($version -split '-')[0] -split '\.')
    while ($parts.Count -lt 4) { $parts += '0' }
    [version]($parts -join '.')
}

# version -> the csprojs on it. $EngineVersion$ marks the template sources (NewGame, Library), which have no version yet.
$references = @{}
foreach ($csproj in Get-ChildItem samples -Recurse -Filter *.csproj) {
    foreach ($reference in ([xml](Get-Content $csproj.FullName -Raw)).SelectNodes("//*[local-name()='PackageReference'][starts-with(@Include,'Stride.')]")) {
        $version = $reference.GetAttribute('Version')
        if ($version -notmatch '^\d') { continue }
        if (-not $references.ContainsKey($version)) { $references[$version] = [System.Collections.Generic.HashSet[string]]::new() }
        [void]$references[$version].Add((Resolve-Path -Relative $csproj.FullName))
    }
}

if ($WarnMissedGates) {
    # Gates are source code: the same pattern as the "Check that upgraders move the version" step of repo-file-guard.yml.
    $gates = Select-String -Path sources/engine/Stride.Assets/StridePackageUpgrader*.cs -Pattern '^\s*registry\.[A-Za-z]+\("(\d+(\.\d+){1,3})"' |
        ForEach-Object { $_.Matches[0].Groups[1].Value }
    $newestGate = $gates | Sort-Object { Get-Number $_ } | Select-Object -Last 1
    foreach ($version in $references.Keys) {
        if ((Get-Number $version) -lt (Get-Number $newestGate)) {
            echo "::warning title=Samples missing an upgrade::$($references[$version].Count) sample csproj(s) on $version are below upgrader gate $newestGate, and the pack would hide it: they ship as the engine's version without that migration. Run PrepareSamplesForRelease first."
        }
    }
}

if ($RequireStable) {
    $labelled = @($references.Keys | Where-Object { $_ -match '-' })
    if ($labelled.Count -gt 0) {
        foreach ($version in $labelled) {
            echo "::error::$($references[$version].Count) sample csproj(s) reference prerelease $version, for example $($references[$version] | Select-Object -First 1). Remove the suffix from SharedAssemblyInfo.cs, run PrepareSamplesForRelease and commit the samples first."
        }
        exit 1
    }
}
