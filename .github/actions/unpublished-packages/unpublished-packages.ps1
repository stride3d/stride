# Makes every restore of the job take the packages of the dep-* runs listed in deps/unpublished-packages.txt
# (one run id or run URL per line), instead of nuget.org. Those runs wait for the approval of their publish.
param(
    [string]$List = 'deps/unpublished-packages.txt',
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$Token = $env:GH_TOKEN
)

$ErrorActionPreference = 'Stop'

$runs = foreach ($line in Get-Content $List) {
    $line = ($line -replace '#.*', '').Trim()
    if (-not $line) { continue }
    if ($line -notmatch '^(?:.*/actions/runs/)?(\d+)(?:/.*)?$') { throw "${List}: '$line' is neither a run id nor a run URL" }
    $Matches[1]
}
if (-not $runs) { throw "$List lists no run: remove it" }

$pins = @{}
foreach ($node in ([xml](Get-Content -Raw sources/Directory.Packages.props)).GetElementsByTagName('PackageVersion')) {
    $pins[$node.Include] = $node.Version
}

$headers = @{ Authorization = "Bearer $Token"; Accept = 'application/vnd.github+json' }
$download = Join-Path $env:RUNNER_TEMP unpublished-packages-download
$feed = Join-Path $env:RUNNER_TEMP unpublished-packages
New-Item -ItemType Directory -Force $download, $feed, bin/packages | Out-Null

$packages = [ordered]@{}
foreach ($run in $runs) {
    $artifacts = @((Invoke-RestMethod "$env:GITHUB_API_URL/repos/$Repository/actions/runs/$run/artifacts?per_page=100" -Headers $headers).artifacts |
        Where-Object name -like '*.nupkg')
    if (-not $artifacts) { throw "Run $run has no .nupkg artifact" }
    foreach ($artifact in $artifacts) {
        if ($artifact.expired) { throw "The $($artifact.name) artifact of run $run expired: run its workflow again" }
        $zip = Join-Path $download "$($artifact.id).zip"
        # The download redirects to a storage URL, which gets no Authorization header
        Invoke-WebRequest $artifact.archive_download_url -Headers $headers -OutFile $zip
        Expand-Archive $zip (Join-Path $download $artifact.id)
        foreach ($nupkg in Get-ChildItem (Join-Path $download $artifact.id) -Filter *.nupkg -Recurse) {
            $archive = [IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
            try {
                $reader = [IO.StreamReader]::new(($archive.Entries | Where-Object { $_.FullName -eq $_.Name -and $_.Name -like '*.nuspec' }).Open())
                try { $metadata = ([xml]$reader.ReadToEnd()).package.metadata } finally { $reader.Dispose() }
            } finally { $archive.Dispose() }
            $id = $metadata.id
            if ($packages.Contains($id)) { throw "Runs $($packages[$id].Run) and $run both build $id" }
            # Otherwise CI would pass on the package of nuget.org without testing this one.
            # Without the build metadata (+...), which NuGet ignores and the nuspec keeps.
            if (("$($pins[$id])" -replace '\+.*') -ne ($metadata.version -replace '\+.*')) {
                throw "Run $run built $id $($metadata.version), but sources/Directory.Packages.props pins $($pins[$id] ?? 'no version of it')"
            }
            Copy-Item $nupkg.FullName bin/packages
            Copy-Item $nupkg.FullName $feed
            $packages[$id] = [pscustomobject]@{ Version = $metadata.version; Run = $run }
        }
    }
}

# Restores in the repository: the ids map to bin/packages, a source of the root nuget.config that samples/nuget.config also keeps.
# An exact id is more specific than the Stride.Dependencies.* pattern that maps them to nuget.org.
$path = (Resolve-Path nuget.config).Path
$config = [xml]::new()
$config.PreserveWhitespace = $true
$config.Load($path)
$local = $config.SelectSingleNode("/configuration/packageSourceMapping/packageSource[@key='stride-local']")
foreach ($id in $packages.Keys) {
    $package = $config.CreateElement('package')
    $package.SetAttribute('pattern', $id)
    [void]$local.AppendChild($package)
}
$config.Save($path)

# Restores outside the repository (the games that tests create, the Lavapipe of the Vulkan setup) read the user config.
# A folder of its own: the lanes that add bin/packages to that config would fail on a second source with the same path.
$path = if ($IsWindows) { Join-Path $env:APPDATA NuGet/NuGet.Config } else { Join-Path $HOME .nuget/NuGet/NuGet.Config }
$config = [xml]::new()
if (Test-Path $path) {
    $config.Load($path)
} else {
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    $config.LoadXml('<configuration><packageSources><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" /></packageSources></configuration>')
}
$sources = $config.SelectSingleNode('/configuration/packageSources') ?? $config.DocumentElement.AppendChild($config.CreateElement('packageSources'))
$source = $config.CreateElement('add')
$source.SetAttribute('key', 'unpublished-packages')
$source.SetAttribute('value', $feed)
[void]$sources.AppendChild($source)
if ($mapping = $config.SelectSingleNode('/configuration/packageSourceMapping')) {
    $mapped = $config.CreateElement('packageSource')
    $mapped.SetAttribute('key', 'unpublished-packages')
    foreach ($id in $packages.Keys) {
        $package = $config.CreateElement('package')
        $package.SetAttribute('pattern', $id)
        [void]$mapped.AppendChild($package)
    }
    [void]$mapping.AppendChild($mapped)
}
$config.Save($path)

$rows = foreach ($entry in $packages.GetEnumerator()) {
    "| $($entry.Key) | $($entry.Value.Version) | [$($entry.Value.Run)]($env:GITHUB_SERVER_URL/$Repository/actions/runs/$($entry.Value.Run)) |"
}
$summary = @('### Unpublished packages', '', 'This job restores these packages from the runs that built them, not from nuget.org.', '', '| Package | Version | Run |', '|---|---|---|') + $rows
$summary | Write-Host
if ($env:GITHUB_STEP_SUMMARY) { $summary | Add-Content $env:GITHUB_STEP_SUMMARY }
