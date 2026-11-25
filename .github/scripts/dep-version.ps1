# Computes the version of a dependency build: <official version>.<build>[-<pre-release>][+<source>].
# - Official: the version of the dependency, which the calling workflow reads from the sources it builds.
# - Build: the highest build of that official version, plus 1. The builds are the deps/<name>/<version> tags of the
#   symbols pre-releases on stride3d/stride (dep-symbols.yml), and the versions of the package on nuget.org (unlisted
#   ones included). A dependency that moves from committed binaries to a package keeps its numbering.
# - Source: build metadata, which NuGet ignores for ordering. The commit, after a g as in git describe (g4a21a48a38);
#   from a fork, its owner first (stride3d.gd18cf4701a). -Marker adds identifiers, such as patches-1.
# Writes to GITHUB_OUTPUT: version (full), number (without the metadata: the version to reference and to tag),
# and source (a line for VERSION.txt about a fork, or empty).
param(
    # The name of the dependency in the deps/<name>/<version> tags: the <name> of its dep-<name>.yml workflow.
    [Parameter(Mandatory)][string]$Name,
    # The Stride.Dependencies.* package, for a dependency published on nuget.org.
    [string]$PackageId,
    [Parameter(Mandatory)][string]$Official,
    # Git directory of the dependency, its remote origin the repository it was fetched from, shallow or not, without
    # blobs. Empty when not built from git.
    [string]$Source,
    [string]$Commit,
    [string]$Upstream,
    [string]$UpstreamBranch = 'main',
    # Prefix of the release tags, as v in v2026.4, mesa- in mesa-26.2.4 or VER- in VER-2-13-3. Without it, the release
    # tags of the commit aren't checked: some dependencies tag releases by date (may2026).
    [string]$TagPrefix,
    [string[]]$Marker = @(),
    # The version input of the workflow: used as it is.
    [string]$Override
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if ($Official -notmatch '^(?<numeric>\d+(\.\d+){1,2})(-(?<pre>[0-9A-Za-z]+))?$') {
    throw "Official version '$Official' isn't N.N[.N][-label]: did the dependency change where or how it stores it?"
}
$numeric = $Matches.numeric
$pre = $Matches.pre

# Until 2026-11, the packages were versioned by date, year.month.day. SPIRV-Tools versions are year-based too, so these
# would count as builds of v2026.4 to v2026.6.
$dateVersions = @{
    'spirv-tools' = '2026.4.21', '2026.4.22', '2026.4.23', '2026.5.26', '2026.6.2', '2026.6.3'
}

# Tags write a version with dots or dashes, with or without the trailing .0: v1.12 and VER-2-13-3 name 1.12.0 and 2.13.3
$normalize = { param($version) $version -replace '-', '.' -replace '(\.0)+$', '' }

$metadata = @()
$sourceLine = ''
if ($Source) {
    $Commit = git -C $Source rev-parse "$Commit^{commit}"
    $sha = $Commit.Substring(0, 10)
    $repository = git -C $Source remote get-url origin
    $normalizeUrl = { param($url) $url.ToLowerInvariant().TrimEnd('/') -replace '\.git$', '' }
    if ((& $normalizeUrl $repository) -eq (& $normalizeUrl $Upstream)) {
        # A release tag on the commit must name the official version, or the version file is stale or has moved.
        # ls-remote lists an annotated tag twice, the second time as <tag>^{} with the commit it points to.
        if ($PSBoundParameters.ContainsKey('TagPrefix')) {
            $releaseTags = git -C $Source ls-remote --tags origin "$TagPrefix[0-9]*" | ForEach-Object {
                $sha1, $ref = $_ -split '\t'
                if ($sha1 -eq $Commit -and $ref -notmatch 'rc\d') { $ref -replace '^refs/tags/', '' -replace '\^\{\}$', '' }
            } | Select-Object -Unique
            foreach ($tag in $releaseTags) {
                if ((& $normalize $tag.Substring($TagPrefix.Length)) -ne (& $normalize $Official)) {
                    throw "Commit $sha has tag $tag, but the official version read from its sources is $Official"
                }
            }
        }
        $metadata += "g$sha"
    } else {
        # The commits on top of upstream need the history of both
        if ((git -C $Source rev-parse --is-shallow-repository) -eq 'true') { git -C $Source fetch -q --filter=tree:0 --no-tags --unshallow origin $Commit }
        git -C $Source fetch -q --filter=tree:0 --no-tags $Upstream $UpstreamBranch
        $base = git -C $Source merge-base $Commit FETCH_HEAD
        $ahead = git -C $Source rev-list --count "FETCH_HEAD..$Commit"
        $owner = ([uri]$repository).Segments[-2].TrimEnd('/')
        $metadata += $owner, "g$sha"
        $sourceLine = "Fork of $Upstream`: $UpstreamBranch at $($base.Substring(0, 10)) plus $ahead commit$(if ($ahead -ne '1') { 's' })"
    }
}
$metadata += $Marker
# Identifiers of build metadata are [0-9A-Za-z-], separated by dots
$metadata = @(($metadata -join '.') -split '\.' | ForEach-Object { ($_ -replace '[^0-9A-Za-z-]+', '-').Trim('-') } | Where-Object { $_ })

# The tags of stride3d/stride, also from a fork: the builds that Stride uses come from there
$buildTags = "refs/tags/deps/$Name/"
$published = @(git ls-remote --tags https://github.com/stride3d/stride.git "$buildTags*" | ForEach-Object {
    ($_ -split '\t')[1].Substring($buildTags.Length) -replace '\^\{\}$', ''
})
if ($PackageId) {
    $response = Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json" -SkipHttpErrorCheck
    $published += switch ($response.StatusCode) {
        200 { @(($response.Content | ConvertFrom-Json).versions) }
        404 { @() }
        default { throw "nuget.org answered $($response.StatusCode) for the versions of $PackageId" }
    }
}
$published = @($published | ForEach-Object { $_.ToLowerInvariant() } | Select-Object -Unique)

if ($Override) {
    $version = $Override
    $number = ($Override -split '\+')[0]
} else {
    $pattern = '^' + [regex]::Escape($numeric) + '\.(\d+)(-.*)?$'
    $builds = @($published | Where-Object { $_ -notin $dateVersions[$Name] } | ForEach-Object { if ($_ -match $pattern) { [int]$Matches[1] } })
    $build = 1 + ($builds | Measure-Object -Maximum).Maximum
    # A date version can still hold the number
    do { $number = "$numeric.$build" + $(if ($pre) { "-$pre" }); $build++ } while ($published -contains $number.ToLowerInvariant())
    $version = $number + $(if ($metadata) { '+' + ($metadata -join '.') })
}
if ($published -contains $number.ToLowerInvariant()) { throw "$Name $number already exists, as a deps/$Name tag on stride3d/stride or on nuget.org" }

"$Name $version (official version $Official)"
if ($sourceLine) { $sourceLine }
if ($env:GITHUB_OUTPUT) { "version=$version", "number=$number", "source=$sourceLine" | Add-Content $env:GITHUB_OUTPUT }
if ($env:GITHUB_STEP_SUMMARY) { "Version: ``$version``" | Add-Content $env:GITHUB_STEP_SUMMARY }
