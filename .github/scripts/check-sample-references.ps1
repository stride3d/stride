# Checks the Stride.* references of the sample csprojs, from the repo root. See docs/build/versioning.md.
#   -RequireStable    a stable engine or pack can't have samples on a prerelease: error.
param(
    [switch]$RequireStable
)

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

if ($RequireStable) {
    $labelled = @($references.Keys | Where-Object { $_ -match '-' })
    if ($labelled.Count -gt 0) {
        foreach ($version in $labelled) {
            echo "::error::$($references[$version].Count) sample csproj(s) reference prerelease $version, for example $($references[$version] | Select-Object -First 1). Remove the suffix from SharedAssemblyInfo.cs, run the Upgrade Samples workflow and merge its PR first."
        }
        exit 1
    }
}
