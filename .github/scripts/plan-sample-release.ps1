# Decides whether the release of -Version includes the content template packs, from the repo root. It does when a packed
# sample is on that version and its packs aren't published yet; the other samples keep their number in the pack. Otherwise
# the engine keeps naming the last published packs. See docs/build/versioning.md.
# Writes samples-version=<version, or empty> to GITHUB_OUTPUT. Fails on what would break the packs or the engine.
param(
    [Parameter(Mandatory)][string]$Version
)

# Same list as TemplatePreprocessor.NonEnginePackagePrefixes: these Stride.* packages don't follow the engine.
$nonEnginePrefixes = 'Stride.Awesome.Shaders', 'Stride.Community', 'Stride.Dependencies.', 'Stride.GNU.', 'Stride.Mono.', 'Stride.OpenTK'

function Get-Number([string]$version) {
    [version](($version -split '-')[0])
}

function Get-PublishedVersions([string]$id) {
    $index = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$id/index.json" -ErrorAction SilentlyContinue
    if ($index) { @($index.versions) } else { @() }
}

# The sample folders of the content packs, and through their project references the asset packs they use.
$pending = [System.Collections.Generic.Stack[string]]::new()
foreach ($template in Get-ChildItem sources/templates/*/*.csproj) {
    $xml = [xml](Get-Content $template.FullName -Raw)
    if ($xml.SelectSingleNode("//*[local-name()='StrideTemplateContentVersioned']").InnerText -ne 'true') { continue }
    foreach ($path in $xml.SelectNodes("//*[local-name()='StrideSampleTemplate']/*[local-name()='SamplePath']")) {
        foreach ($csproj in Get-ChildItem ($path.InnerText -replace '^\$\(StrideRoot\)', '') -Recurse -Filter *.csproj) {
            $pending.Push($csproj.FullName)
        }
    }
}

# version -> the csprojs on it. A version not starting with a digit is a placeholder of the template sources.
$references = @{}
$seen = [System.Collections.Generic.HashSet[string]]::new()
while ($pending.Count -gt 0) {
    $csproj = $pending.Pop()
    if (-not $seen.Add($csproj)) { continue }
    $xml = [xml](Get-Content $csproj -Raw)
    foreach ($reference in $xml.SelectNodes("//*[local-name()='ProjectReference']")) {
        $pending.Push([System.IO.Path]::GetFullPath((Join-Path (Split-Path $csproj) ($reference.GetAttribute('Include') -replace '\\', '/'))))
    }
    foreach ($reference in $xml.SelectNodes("//*[local-name()='PackageReference'][starts-with(@Include,'Stride.')]")) {
        $include = $reference.GetAttribute('Include')
        $used = $reference.GetAttribute('Version')
        if ($used -notmatch '^\d' -or ($nonEnginePrefixes | Where-Object { $include.StartsWith($_) })) { continue }
        if (-not $references.ContainsKey($used)) { $references[$used] = [System.Collections.Generic.List[string]]::new() }
        $references[$used].Add($csproj)
    }
}

$committed = ([xml](Get-Content sources/templates/StrideSamplesVersion.props -Raw)).SelectSingleNode("//*[local-name()='_StrideCommittedSamplesVersion']").InnerText
$stable = $Version -notmatch '-'
# A re-run on the tagged commit completes a partial deploy.
$published = (Get-PublishedVersions stride.templates.samples) -contains $Version.ToLowerInvariant() -and -not (git tag --points-at HEAD --list "samples/$Version")
$samplesVersion = ''

if ($references.ContainsKey($Version) -and -not $published) {
    $samplesVersion = $Version
    # A plain 'dotnet new' restores what the csproj says, so a number the engine never shipped under can't go in a pack.
    $engineVersions = Get-PublishedVersions stride.engine
    $missing = @($references.Keys | Where-Object { $_ -ne $Version -and $engineVersions -notcontains $_.ToLowerInvariant() })
    if ($missing.Count -gt 0) {
        foreach ($used in $missing) {
            echo "::error::$($references[$used].Count) sample csproj(s) reference $used, which was never released, for example $($references[$used][0]). Run the Upgrade Samples workflow and merge its PR first."
        }
        exit 1
    }
    echo "::notice title=Samples::This release includes the samples as $Version ($($references[$Version].Count) reference(s) on it)."
}
else {
    if ($stable -and $committed -match '-') {
        echo "::error::Stable engine $Version names prerelease content $committed. Run the Upgrade Samples workflow and merge its PR first, so this release includes stable samples."
        exit 1
    }
    # Every New Project would break on an engine naming packs that aren't on the feed.
    foreach ($id in 'stride.templates.samples', 'stride.templates.games.starters', 'stride.templates.assetpacks') {
        if ((Get-PublishedVersions $id) -notcontains $committed.ToLowerInvariant()) {
            echo "::error::$id $committed is not on nuget.org, and this release doesn't include the samples."
            exit 1
        }
    }
    # A sample moved after the published packs, but the number moved on before this release.
    $missed = @($references.Keys | Where-Object { (Get-Number $_) -gt (Get-Number $committed) })
    foreach ($used in $missed) {
        echo "::warning title=Samples not included::$($references[$used].Count) reference(s) on $used are newer than the published samples $committed but not on $Version, so this release reuses $committed. Run the Upgrade Samples workflow and merge its PR to include them."
    }
    # Samples changed since the commit the published packs come from (some older packs have no tag) don't ship: fine for
    # work not released yet, and the release manager decides whether a sample should move first.
    if (git tag --list "samples/$committed") {
        $changed = @(git diff --name-only "samples/$committed" HEAD -- samples ':(exclude)samples/NewGame' ':(exclude)samples/Library' ':(exclude)*.md')
        if ($changed.Count -gt 0) {
            echo "::warning title=Samples changed::$($changed.Count) sample file(s) changed since the published samples $committed (tag samples/$committed). This engine ships with $committed. To ship those changes, move a sample to the version being released (see the run summary)."
            if ($env:GITHUB_STEP_SUMMARY) {
                @(
                    "## Sample changes not in the published content ($committed)"
                    ''
                    "This engine ships with the published $committed content. Fine for work not released yet; to ship these changes, move a sample to the version being released (the Upgrade Samples workflow, or by hand for one sample) before the release."
                    ''
                    '```'
                    $changed | Select-Object -First 50
                    if ($changed.Count -gt 50) { "... and $($changed.Count - 50) more" }
                    '```'
                ) | Add-Content $env:GITHUB_STEP_SUMMARY
            }
        }
    }
    else {
        echo "::warning::No samples/$committed tag; skipped the check for sample changes since the published samples."
    }
    echo "::notice title=Samples::This release reuses the published samples $committed."
}

if ($env:GITHUB_OUTPUT) { "samples-version=$samplesVersion" >> $env:GITHUB_OUTPUT }
