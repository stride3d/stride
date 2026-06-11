#!/usr/bin/env pwsh
# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#
# Drives a Stride test .app bundle on the iOS Simulator:
#   1. (optional) boot a simulator device
#   2. (optional) xcrun simctl install the .app
#   3. push gold images into the app's Documents dir (simulator data is a host-visible path)
#   4. launch the app with --xunit-command run (App.HeadlessMode path)
#   5. wait for process exit
#   6. copy tests/local/ (TRX + generated images) back to host
#   7. exit non-zero if any test failed (parsed from the TRX)
#
# macOS-only (iOS Simulator). Requires Xcode + pwsh.

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Package,                # Bundle id (e.g. com.stride.engine.tests)
    [Parameter(Mandatory)][string]$Suite,                  # Test suite / assembly name (e.g. Stride.UI.Tests).
                                                           # Required because casing/acronyms (UI, 10_0) can't be auto-recovered from $Package.
    [string]$App,                                          # .app bundle path; skip install if already deployed
    [string]$RepoRoot,                                     # Stride repo root; auto-discovered from script dir
    [string]$GoldDir,                                      # Override gold push source; defaults to <RepoRoot>/tests/<Suite>
    [string]$ResultsDir,                                   # Override result pull dest; defaults to <RepoRoot>/tests/local/<Suite>
    [string]$Simulator = 'booted',                         # 'booted' (use whatever is booted), a UDID, or a device-type+OS pair (e.g. 'iPhone 15')
    [int]$TimeoutSeconds = 1800,                           # max wait for tests to finish
    [string]$Filter,                                       # optional vstest --filter expr passed on to the on-device runner
    [switch]$KeepSimulator,                                # don't shutdown a simulator we booted
    [switch]$StreamLog                                     # also tee live log to console (CI live status / local interactive)
)

# 'Continue': pwsh on macOS surfaces native-command stderr lines as ErrorRecord under 'Stop'
# even when exit code is 0 (xcrun writes progress to stderr). Use explicit exit-code checks.
$ErrorActionPreference = 'Continue'

if ($null -eq $IsMacOS -or -not $IsMacOS) {
    throw "iOS Simulator only runs on macOS hosts. Use pwsh on macOS."
}

# Repo root discovery: walk up from $PSScriptRoot looking for build/Stride.sln (same convention
# the Android driver and GameTestBase use). Only required when -ResultsDir (or -GoldDir, if
# pushing gold) isn't provided explicitly; CI artifact-extraction flows pass both paths.
function Find-RepoRoot {
    $dir = $PSScriptRoot
    while ($dir) {
        if (Test-Path (Join-Path $dir 'build/Stride.sln')) { return $dir }
        $parent = Split-Path -Parent $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    return $null
}
if (-not $RepoRoot) { $RepoRoot = Find-RepoRoot }
if (-not $ResultsDir) {
    if (-not $RepoRoot) { throw "ResultsDir not provided and RepoRoot couldn't be auto-discovered (no build/Stride.sln found walking up from $PSScriptRoot). Pass -ResultsDir or -RepoRoot." }
    $ResultsDir = Join-Path $RepoRoot "tests/local/$Suite"
}
# Inner-suite discovery for multi-suite hosts (Stride.Tests.Combined): scan the .app for
# *.Tests.dll names. Each one keys gold lookup by Assembly.GetName().Name at runtime, so we
# push <RepoRoot>/tests/<DllName>/ for each. Single-suite apps surface their own *.Tests.dll
# alongside, so the discovery list collapses to one and behavior matches the pre-Combined flow.
# `*.Tests*` (not `*.Tests.dll`) so suffixed variants like Stride.Graphics.Tests.10_0.dll match.
$innerSuites = @()
if ($App -and (Test-Path $App)) {
    $innerSuites = Get-ChildItem -Path $App -Filter '*.dll' -ErrorAction SilentlyContinue |
        Where-Object { $_.BaseName -like '*.Tests*' } |
        ForEach-Object { $_.BaseName } |
        Sort-Object -Unique
}
if ($innerSuites.Count -eq 0) { $innerSuites = @($Suite) }
# $GoldDir override applies only to $Suite (back-compat with single-suite callers).
if (-not $PSBoundParameters.ContainsKey('GoldDir') -and $RepoRoot) {
    $candidate = Join-Path $RepoRoot "tests/$Suite"
    if (Test-Path $candidate) { $GoldDir = $candidate }
}
Write-Host "Suite:    $Suite"
Write-Host "Inner:    $($innerSuites -join ', ')"
Write-Host "Repo:     $(if ($RepoRoot) { $RepoRoot } else { '<not in repo>' })"
Write-Host "Gold:     $(if ($GoldDir) { $GoldDir } else { '<none>' })"
Write-Host "Results:  $ResultsDir"

function Invoke-Simctl {
    # xcrun simctl trampoline; throws on non-zero exit so the script fails fast at the call site.
    param([Parameter(Mandatory)][string[]]$Args, [switch]$AllowFailure)
    $out = & xcrun simctl @Args 2>&1
    if ($LASTEXITCODE -ne 0 -and -not $AllowFailure) {
        throw "xcrun simctl $($Args -join ' ') failed: $out"
    }
    return $out
}

function Resolve-SimulatorUdid {
    param([string]$Hint)
    if ($Hint -eq 'booted') {
        # First booted device wins. simctl accepts 'booted' literally for most subcommands, but
        # we resolve to a UDID up-front to allow the data-container path lookup below.
        $json = (& xcrun simctl list devices booted --json) | ConvertFrom-Json
        foreach ($rt in $json.devices.PSObject.Properties) {
            foreach ($d in $rt.Value) { if ($d.state -eq 'Booted') { return $d.udid } }
        }
        return $null
    }
    # UDID is a 36-char hyphenated GUID; anything else we treat as a name match.
    if ($Hint -match '^[0-9A-Fa-f-]{36}$') { return $Hint }
    $json = (& xcrun simctl list devices --json) | ConvertFrom-Json
    foreach ($rt in $json.devices.PSObject.Properties) {
        foreach ($d in $rt.Value) { if ($d.name -eq $Hint) { return $d.udid } }
    }
    return $null
}

# 1. Resolve / boot a simulator.
$udid = Resolve-SimulatorUdid $Simulator
$startedSimulator = $false
if (-not $udid) {
    if ($Simulator -eq 'booted') {
        throw "No simulator is booted and -Simulator wasn't given. Pass a UDID or device name."
    }
    throw "No simulator matched -Simulator '$Simulator'. List with: xcrun simctl list devices"
}
$json = (& xcrun simctl list devices --json) | ConvertFrom-Json
$device = $null
foreach ($rt in $json.devices.PSObject.Properties) {
    foreach ($d in $rt.Value) { if ($d.udid -eq $udid) { $device = $d; break } }
    if ($device) { break }
}
if ($device.state -ne 'Booted') {
    Write-Host "Booting simulator $($device.name) ($udid)..."
    Invoke-Simctl boot,$udid | Out-Null
    $startedSimulator = $true
    # Wait for system to be ready (springboard up).
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $state = (& xcrun simctl bootstatus $udid -b 2>&1 | Select-Object -Last 1)
        if ($state -match 'Device booted|already booted') { break }
        Start-Sleep -Seconds 2
    }
}
Write-Host "Simulator: $udid"

# 2. Optional install
if ($App) {
    if (-not (Test-Path $App)) { throw "App bundle not found: $App" }
    Write-Host "Installing $App..."
    Invoke-Simctl install,$udid,$App | Out-Null
}

# 3. Push gold images. Simulator data is host-visible — copy directly into the app's Documents.
# Wipe each device gold dir before pushing so it mirrors the host: a gold file deleted on the
# host (e.g. a stale iOS primary that's been promoted away) must also disappear from the device,
# otherwise the on-device framework still finds it as "primary" and ignores fallback golds.
# Multi-suite hosts (Combined) push one dir per inner suite, keyed by AssemblyName.
$dataContainer = (Invoke-Simctl get_app_container,$udid,$Package,data).Trim()
if (-not $dataContainer -or -not (Test-Path $dataContainer)) {
    throw "Could not resolve data container for $Package on $udid (app not installed?)"
}
foreach ($s in $innerSuites) {
    $src = if ($s -eq $Suite -and $GoldDir) { $GoldDir }
           elseif ($RepoRoot) { Join-Path $RepoRoot "tests/$s" }
           else { $null }
    if (-not $src -or -not (Test-Path $src)) { continue }
    $target = Join-Path $dataContainer "Documents/tests/$s"
    Write-Host "Pushing gold $src/* -> $target (cleared first)"
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item -Path (Join-Path $src '*') -Destination $target -Recurse -Force
}

# Clear previous results so a stale TRX doesn't fool detection.
$resultsOnDevice = Join-Path $dataContainer "Documents/tests/local"
if (Test-Path $resultsOnDevice) { Remove-Item -Recurse -Force $resultsOnDevice }

# 4. Launch with args. Send the same xunit_command/xunit_exit_on_complete pair the Android
# launcher reads — App.HeadlessMode parses NSProcessInfo.Arguments on iOS for the same keys.
Write-Host "Launching $Package with xunit_command=run..."
# Force-stop in case it's already running with stale state.
Invoke-Simctl terminate,$udid,$Package -AllowFailure | Out-Null
# Capture stdout for diagnosis; --console-pty would block. Background and pull when done.
$logPath = Join-Path $ResultsDir "$Package.console.txt"
if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null }
# `simctl launch` returns immediately with the PID; we poll for exit below.
$launchArgs = @('launch', $udid, $Package, '--xunit-command', 'run', '--xunit-exit-on-complete', 'true')
if ($Filter) { $launchArgs += @('--xunit-filter', $Filter) }
$launchOut = & xcrun simctl @launchArgs 2>&1
if ($LASTEXITCODE -ne 0) { throw "simctl launch failed: $launchOut" }
# Output is like "com.stride.engine.tests: 12345"
$procPid = ($launchOut | Select-String -Pattern ':\s*(\d+)$').Matches.Groups[1].Value
Write-Host "PID: $procPid"

# Tail the simulator system log into the console capture in the background. Use --process
# (takes a PID directly) rather than --predicate "processID == N", since the latter requires
# quoting that doesn't survive PowerShell → simctl spawn → log stream argument forwarding.
# Redirect BOTH stdout and stderr — `simctl spawn` runs `log stream` inside the simulator under
# launchd_sim, and that in-simulator child inherits its stderr from this Start-Process. If we
# leave it pointing at our own stderr (which is the pipe to `tail -25` when invoked from a
# `pwsh ... | tail` shell), the bash wrapper will never see EOF on that pipe after pwsh exits,
# because the in-sim log streamer keeps holding the write end open.
$logPathErr = "$logPath.err"
$logArgs = @{
    FilePath               = 'xcrun'
    ArgumentList           = @('simctl', 'spawn', $udid, 'log', 'stream', '--process', $procPid)
    RedirectStandardOutput = $logPath
    RedirectStandardError  = $logPathErr
    PassThru               = $true
}
$logProc = Start-Process @logArgs

# Optional: tee the simulator system log to the host console so CI shows test progress in real
# time (and local interactive runs can watch without tail-ing a file). The captured file above
# still holds the full stream for post-mortem triage. Second spawn since `log stream` can't fan
# out to both file and console from one invocation; pkill below cleans up both at once.
$liveLogProc = $null
if ($StreamLog) {
    $liveLogArgs = @{
        FilePath     = 'xcrun'
        ArgumentList = @('simctl', 'spawn', $udid, 'log', 'stream', '--process', $procPid, '--style', 'compact')
        NoNewWindow  = $true
        PassThru     = $true
    }
    $liveLogProc = Start-Process @liveLogArgs
}

# 5. Wait for the test process to exit. simctl has no built-in wait; poll the PID directly.
# Simulator apps run as ordinary macOS processes, so the PID `simctl launch` returns is usable
# with the host's `kill -0`. (TRX-write-detect is unreliable for multi-suite hosts — the first
# TRX appears mid-run, not at exit.)
$resultsOnDevice = Join-Path $dataContainer "Documents/tests/local"
Write-Host "Waiting for PID $procPid to exit (timeout: ${TimeoutSeconds}s)..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    & kill -0 $procPid 2>$null
    if ($LASTEXITCODE -ne 0) {
        # PID gone — but the final TRX writeback can take a couple of seconds to surface on the
        # host-visible simulator filesystem. Give a brief grace window before proceeding to pull.
        Start-Sleep -Seconds 2
        Write-Host "PID $procPid gone — proceeding."
        break
    }
    Start-Sleep -Seconds 3
}
if ((Get-Date) -ge $deadline) {
    Write-Warning "Timed out waiting for $Package to exit; pulling whatever is available."
    Invoke-Simctl terminate,$udid,$Package -AllowFailure | Out-Null
}

# Stop log capture. -Force sends SIGKILL on the local `xcrun simctl spawn` wrapper, but the
# actual `log stream` process running inside the simulator (parented by launchd_sim) survives
# and would linger forever — and it inherited stdout/stderr from us, so it'll also keep our
# parent shell's pipes open. Explicitly kill its in-sim instance too. pkill matches on the
# command line we passed; multiple --process N invocations don't collide.
if ($logProc -and -not $logProc.HasExited) {
    Stop-Process -Id $logProc.Id -Force -ErrorAction SilentlyContinue
}
if ($liveLogProc -and -not $liveLogProc.HasExited) {
    Stop-Process -Id $liveLogProc.Id -Force -ErrorAction SilentlyContinue
}
# pkill matches BOTH log stream children at once (file capture + live tee) since they share
# the same --process arg in their command lines.
& xcrun simctl spawn $udid pkill -f "log stream --process $procPid" 2>$null | Out-Null

# If the test process exited without writing any TRX (early crash, abort in startup), the live
# `log stream` started AFTER launch and missed the early output. Query the simulator's unified
# log STORE retroactively to recover startup logs / abort messages.
$anyTrx = Test-Path $resultsOnDevice -PathType Container
if ($anyTrx) { $anyTrx = (Get-ChildItem -Path $resultsOnDevice -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue).Count -gt 0 }
if (-not $anyTrx) {
    $crashPath = Join-Path $ResultsDir "$Package.crashlog.txt"
    if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null }
    Write-Host "No TRX — extracting unified log for $Package from simulator -> $crashPath"
    & xcrun simctl spawn $udid log show --predicate "process == '$Package'" --info --debug --last 5m 2>&1 | Set-Content $crashPath
}

# 6. Pull TRX + images from the simulator's Documents/tests/local/ to $ResultsDir.
# Each inner suite writes Documents/tests/local/<SuiteName>/<SuiteName>.trx (+ generated images).
# Wipe $ResultsDir first so the host mirrors the device — otherwise a previously-failing
# test that now passes (no fresh image written) would leave its stale failure PNG behind
# and mislead CompareGold. Trade-off: no incremental, every run is a clean pull.
if (Test-Path $resultsOnDevice) {
    if (Test-Path $ResultsDir) { Remove-Item -Recurse -Force (Join-Path $ResultsDir '*') }
    else { New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null }
    Write-Host "Pulling results $resultsOnDevice -> $ResultsDir (cleared first)"
    Copy-Item -Path (Join-Path $resultsOnDevice '*') -Destination $ResultsDir -Recurse -Force
} else {
    Write-Warning "No results dir on device — tests may not have written any TRX."
}

# 7. Parse TRX for pass/fail
$trxFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "*.trx" -ErrorAction SilentlyContinue
if (-not $trxFiles) {
    Write-Warning "No TRX files in $ResultsDir -- tests may not have run."
    if ($startedSimulator -and -not $KeepSimulator) { Invoke-Simctl shutdown,$udid -AllowFailure | Out-Null }
    exit 2
}

$totalFailed = 0
foreach ($trx in $trxFiles) {
    [xml]$doc = Get-Content $trx.FullName
    $counters = $doc.TestRun.ResultSummary.Counters
    function Get-CounterInt($node, $name) {
        $v = $node.GetAttribute($name); if ($v) { return [int]$v } else { return 0 }
    }
    $failed = Get-CounterInt $counters 'failed'
    $total  = Get-CounterInt $counters 'total'
    $passed = Get-CounterInt $counters 'passed'
    Write-Host "$($trx.Name): total=$total passed=$passed failed=$failed"
    $totalFailed += $failed
}

if ($startedSimulator -and -not $KeepSimulator) {
    Write-Host "Shutting down simulator we booted..."
    Invoke-Simctl shutdown,$udid -AllowFailure | Out-Null
}

if ($totalFailed -gt 0) {
    Write-Host "FAILED: $totalFailed test(s)." -ForegroundColor Red
    exit 1
}

Write-Host "OK" -ForegroundColor Green
exit 0
