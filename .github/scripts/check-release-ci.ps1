# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

<#
.SYNOPSIS
    Refuses to release a commit whose CI did not pass, and waits for that CI when it is still running.
.DESCRIPTION
    The CI run that tested a commit is the main.yml push run of the newest commit of its first-parent history that has one:
    a push that only changes files main.yml ignores runs no CI. The commits after that one must change only such files, or
    the release is refused (a push with [skip ci], for example). Runs from a full clone, with GH_TOKEN set.
#>
param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$Commit
)

$ErrorActionPreference = 'Stop'
# Commits looked at, from the released one back, to find the one CI ran on
$maxCommits = 200

function Invoke-Native {
    $arguments = if ($args.Count -gt 1) { $args[1..($args.Count - 1)] } else { @() }
    $output = & $args[0] @arguments
    if ($LASTEXITCODE -ne 0) { throw "$($args[0]) failed with exit code $LASTEXITCODE" }
    $output
}

# A path filter of a workflow: * matches within a folder, ** across folders
function ConvertTo-PathRegex([string]$pattern) {
    $regex = ''
    $i = 0
    while ($i -lt $pattern.Length) {
        $rest = $pattern.Substring($i)
        if ($rest.StartsWith('**/')) { $regex += '(?:.*/)?'; $i += 3 }
        elseif ($rest.StartsWith('**')) { $regex += '.*'; $i += 2 }
        elseif ($rest[0] -eq '*') { $regex += '[^/]*'; $i++ }
        elseif ($rest[0] -eq '?') { $regex += '[^/]'; $i++ }
        else { $regex += [regex]::Escape($rest[0]); $i++ }
    }
    "^$regex$"
}

$patterns = (Invoke-Native yq -o=json '.on.push.paths' .github/workflows/main.yml) -join "`n" | ConvertFrom-Json
$filters = foreach ($pattern in $patterns) {
    @{ Negated = $pattern.StartsWith('!'); Regex = ConvertTo-PathRegex $pattern.TrimStart('!') }
}
# Without filters every change would look ignored: refuse rather than release untested code
if (@($filters).Count -eq 0) {
    Write-Output "::error::Could not read the push paths of .github/workflows/main.yml."
    exit 1
}

# Whether a changed file starts main.yml on a push: the last filter that matches it decides
function Test-TriggersCI([string]$path) {
    $result = $false
    foreach ($filter in $filters) {
        if ($path -cmatch $filter.Regex) { $result = -not $filter.Negated }
    }
    $result
}

$tested = $null
$runId = $null
foreach ($candidate in Invoke-Native git rev-list --first-parent "--max-count=$maxCommits" $Commit) {
    $found = Invoke-Native gh api "repos/$Repository/actions/workflows/main.yml/runs?head_sha=$candidate&event=push&per_page=1" --jq '.workflow_runs[0].id // empty'
    if ($found) {
        $tested = $candidate
        $runId = $found
        break
    }
}
if (-not $tested) {
    Write-Output "::error::No CI run of a push in the last $maxCommits commits of $Commit."
    exit 1
}

$url = "https://github.com/$Repository/actions/runs/$runId"
if ($tested -ne $Commit) {
    # Both paths of a rename count, as for the push filters
    $untested = @(Invoke-Native git diff --no-renames --name-only $tested $Commit | Where-Object { Test-TriggersCI $_ })
    if ($untested.Count -gt 0) {
        Write-Output "::error::CI last ran on $tested ($url), and the commits after it change files CI tests, for example: $(($untested | Select-Object -First 10) -join ', '). Push them so CI runs, or deploy with skip-ci-check."
        exit 1
    }
    Write-Output "::notice::$Commit changes only files CI ignores since $tested, whose CI run is checked."
}

if ((Invoke-Native gh run view $runId -R $Repository --json status --jq .status) -ne 'completed') {
    Write-Output "::notice::CI is still running on $tested, waiting for it: $url"
    Invoke-Native gh run watch $runId -R $Repository --interval 60 | Out-Null
}

$conclusion = Invoke-Native gh run view $runId -R $Repository --json conclusion --jq .conclusion
if ($conclusion -ne 'success') {
    Write-Output "::error::CI did not pass on $tested ($conclusion): $url. Rerun its failed jobs, or deploy with skip-ci-check."
    exit 1
}
Write-Output "::notice::CI passed on ${tested}: $url"
