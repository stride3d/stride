#!/usr/bin/env pwsh
# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#
# Drives a deployed Stride test APK on a connected Android device/emulator:
#   1. (optional) cold-boot an emulator with -gpu swiftshader_indirect
#   2. (optional) adb install -r the APK
#   3. push gold images into the app's external files dir
#   4. launch MainActivity with --es xunit_command run (App.HeadlessMode path)
#   5. wait for process exit
#   6. pull tests/local/ (TRX + generated images) back to host
#   7. exit non-zero if any test failed (parsed from the TRX)
#
# Works on Windows, Linux, and macOS via PowerShell Core (pwsh).

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Package,                # Android package id (e.g. stride.engine.tests)
    [Parameter(Mandatory)][string]$Suite,                  # Test suite / assembly name (e.g. Stride.UI.Tests).
                                                           # Required because casing/acronyms (UI, 10_0) can't be auto-recovered from $Package.
    [string]$Apk,                                          # APK to install; skip if already deployed
    [string]$RepoRoot,                                     # Stride repo root; auto-discovered from script dir
    [string]$GoldDir,                                      # Override gold push source; defaults to <RepoRoot>/tests/<Suite>
    [string]$ResultsDir,                                   # Override result pull dest; defaults to <RepoRoot>/tests/local/<Suite>
    [string]$Avd,                                          # AVD name to boot if no device is connected
    [int]$TimeoutSeconds = 1800,                           # max wait for tests to finish
    [switch]$KeepEmulator,                                 # don't kill the emulator we started
    [switch]$StreamLogcat                                  # also tee Stride-tag logcat to console (local interactive use)
)

# 'Continue' (not Stop): Windows PowerShell 5.1 wraps each native-command stderr line as a
# NativeCommandError ErrorRecord, which under 'Stop' terminates the script even when the
# command exit code is 0 (e.g. adb pull writes its "1 file pulled" progress to stderr). The
# script relies on explicit `throw` for genuine failures; native exit codes are checked at
# install + pull sites below.
$ErrorActionPreference = 'Continue'

# $IsWindows/$IsMacOS/$IsLinux are PS 6+ only; backfill for Windows PowerShell 5.1.
if ($null -eq $IsWindows) {
    $script:IsWindows = $env:OS -eq 'Windows_NT'
    $script:IsMacOS = $false
    $script:IsLinux = -not $script:IsWindows
}

# Repo root discovery: walk up from $PSScriptRoot looking for build/Stride.sln (same convention
# GameTestBase.FindStrideSolutionRootDirectory uses on desktop). Only required when -ResultsDir
# (or -GoldDir, if pushing gold) isn't provided explicitly; CI artifact-extraction flows pass
# both paths and skip the walk-up entirely.
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
if (-not $PSBoundParameters.ContainsKey('GoldDir') -and $RepoRoot) {
    # Auto-default gold source only when caller didn't specify; pass -GoldDir '' to opt out.
    $candidate = Join-Path $RepoRoot "tests/$Suite"
    if (Test-Path $candidate) { $GoldDir = $candidate }
}
Write-Host "Suite:    $Suite"
Write-Host "Repo:     $(if ($RepoRoot) { $RepoRoot } else { '<not in repo>' })"
Write-Host "Gold:     $(if ($GoldDir) { $GoldDir } else { '<none>' })"
Write-Host "Results:  $ResultsDir"

function Find-FirstExisting {
    param([string[]]$Paths)
    foreach ($p in $Paths) { if ($p -and (Test-Path $p)) { return $p } }
    return $null
}

function Get-SdkRoot {
    foreach ($name in @('ANDROID_SDK_ROOT', 'ANDROID_HOME')) {
        $v = [Environment]::GetEnvironmentVariable($name)
        if ($v) { return $v }
    }
    return $null
}

function Resolve-Adb {
    $exe = if ($IsWindows) { 'adb.exe' } else { 'adb' }
    $sdkRoot = Get-SdkRoot
    $candidates = @()
    if ($sdkRoot) { $candidates += (Join-Path $sdkRoot "platform-tools/$exe") }
    if ($IsWindows) {
        $candidates += @(
            (Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\$exe"),
            (Join-Path ([Environment]::GetEnvironmentVariable('ProgramFiles(x86)')) "Android\android-sdk\platform-tools\$exe"),
            (Join-Path $env:ProgramFiles "Android\android-sdk\platform-tools\$exe"))
    } elseif ($IsMacOS) {
        $candidates += "$HOME/Library/Android/sdk/platform-tools/adb"
    } else {
        $candidates += "$HOME/Android/Sdk/platform-tools/adb"
    }
    $hit = Find-FirstExisting $candidates
    if ($hit) { return $hit }
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    throw "adb not found. Set ANDROID_SDK_ROOT or install Android platform-tools."
}

$adb = Resolve-Adb
Write-Host "adb: $adb"

# Git Bash's tar (Cygwin/MSYS) on PATH treats `C:\...` as a network host and fails; prefer
# Windows' native System32\tar.exe (Win10 1803+), which handles Windows paths.
$tar = if ($IsWindows -and (Test-Path "$env:SystemRoot\System32\tar.exe")) { "$env:SystemRoot\System32\tar.exe" } else { 'tar' }

# 1. Ensure a device is connected; cold-boot an emulator if not.
$devicesRaw = & $adb devices
$hasDevice = $null -ne ($devicesRaw | Select-String -Pattern 'device$' -SimpleMatch:$false)
$startedEmulator = $false
if (-not $hasDevice) {
    if (-not $Avd) { throw "No device connected and -Avd not provided. Pass -Avd <name> to cold-boot one." }
    # Boot the same Lavapipe emulator CI uses via start-emulator.ps1 (sets VK_DRIVER_FILES +
    # the helper layer and launches with -gpu host), so a local cold-boot matches CI's renderer
    # instead of falling back to a different GPU. It runs the emulator in the foreground, so
    # launch it detached and let the boot-wait below pick the device up via adb.
    $startEmu = Join-Path $PSScriptRoot 'start-emulator.ps1'
    Write-Host "No device — launching Lavapipe emulator via start-emulator.ps1 (AVD '$Avd')..."
    $proc = Start-Process pwsh -ArgumentList @('-NoProfile', '-File', $startEmu, '-Avd', $Avd) -PassThru
    $startedEmulator = $true
    Write-Host "start-emulator PID: $($proc.Id)"
}

# Wait for boot
Write-Host "Waiting for boot..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    $boot = (& $adb shell getprop sys.boot_completed 2>$null) -replace '\s', ''
    if ($boot -eq '1') { break }
    Start-Sleep -Seconds 3
}
$finalBoot = (& $adb shell getprop sys.boot_completed 2>$null) -replace '\s', ''
if ($finalBoot -ne '1') {
    throw "Device did not finish booting within $TimeoutSeconds seconds."
}
Write-Host "Boot complete."

# 2. Optional APK install
if ($Apk) {
    if (-not (Test-Path $Apk)) { throw "APK not found: $Apk" }
    Write-Host "Installing $Apk..."
    # adb install writes progress + the "Failure [...]" line to stdout; capture and surface it.
    $installOut = & $adb install -r $Apk 2>&1
    $installOut | Where-Object { $_ } | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0 -or ($installOut -match 'Failure \[')) {
        throw "adb install failed: $installOut"
    }
}

# 3. Targets write to the app's internal FilesDir (/data/user/0/<pkg>/files/) because
# targetSdk 30+ scoped storage blocks app writes through the FUSE-bound external path.
# Push/pull go through tar streams + run-as to bridge adb-shell uid <-> app uid.
# rm -rf first so stale gold from a prior run can't linger — tar x merges onto existing files.
& $adb shell "run-as $Package sh -c 'rm -rf files/tests/$Suite && mkdir -p files/tests/$Suite'" 2>$null | Out-Null

# 4. Push gold images (-GoldDir contents -> device's files/tests/<Suite>/)
if ($GoldDir) {
    if (-not (Test-Path $GoldDir)) { throw "GoldDir not found: $GoldDir" }
    Write-Host "Pushing gold $GoldDir/* -> files/tests/$Suite/ on device"
    $localTar = [IO.Path]::GetTempFileName()
    try {
        & $tar c -C $GoldDir -f $localTar . 2>$null
        & $adb push $localTar "/data/local/tmp/$Package-gold.tar" | Out-Null
        & $adb shell "cat /data/local/tmp/$Package-gold.tar | run-as $Package tar x -C files/tests/$Suite" 2>$null | Out-Null
        & $adb shell "rm /data/local/tmp/$Package-gold.tar" 2>$null | Out-Null
    } finally {
        Remove-Item $localTar -ErrorAction SilentlyContinue
    }
}

# 5. Resolve the launch activity
$resolved = (& $adb shell cmd package resolve-activity --brief $Package) -split "`n"
$activity = $resolved | Where-Object { $_ -match "^$Package/" } | Select-Object -First 1
if (-not $activity) { throw "Could not resolve launch activity for $Package" }
$activity = $activity.Trim()
Write-Host "Activity: $activity"

# Clear log + previous TRX (in internal storage via run-as) so a stale file doesn't fool detection
& $adb shell "run-as $Package rm -rf files/tests/local" 2>$null | Out-Null
& $adb logcat -c 2>$null | Out-Null

# Force-stop the package so a fresh OnCreate runs with our Intent extras (otherwise
# `am start` may resume an existing instance and skip Intent processing).
& $adb shell am force-stop $Package 2>$null | Out-Null

# 6. Start logcat capture (full ring buffer; threadtime format aids triage)
if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null }
$logcatPath = Join-Path $ResultsDir "$Package.logcat.txt"
Write-Host "Logcat -> $logcatPath"
# -WindowStyle is Windows-only on pwsh 7.6+; errors on non-Windows. Splat conditionally so
# the same call works on every host pwsh supports.
$logcatArgs = @{
    FilePath               = $adb
    ArgumentList           = @('logcat', '-v', 'threadtime')
    RedirectStandardOutput = $logcatPath
    PassThru               = $true
}
if ($IsWindows) { $logcatArgs.WindowStyle = 'Hidden' }
$logcatProc = Start-Process @logcatArgs

# Optional: tee a filtered (Stride tag only) live stream to the host console so the user
# sees test-by-test progress without scrolling Android noise. The captured file above
# still contains the full ring buffer for post-mortem triage.
$liveLogcatProc = $null
if ($StreamLogcat) {
    $liveLogcatArgs = @{
        FilePath               = $adb
        ArgumentList           = @('logcat', '-v', 'brief', 'Stride:V', '*:S')
        NoNewWindow            = $true
        PassThru               = $true
    }
    if ($IsWindows) { $liveLogcatArgs.WindowStyle = 'Hidden' }
    $liveLogcatProc = Start-Process @liveLogcatArgs
}

# 7. Launch with intent extras
Write-Host "Launching with xunit_command=run..."
& $adb shell am start -W -n $activity --es xunit_command run --ez xunit_exit_on_complete true | Out-Null

# 8. Wait for process to exit (heuristic: pidof goes empty)
Write-Host "Waiting for process to exit (timeout: ${TimeoutSeconds}s)..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$lastPid = $null
while ((Get-Date) -lt $deadline) {
    $procPid = (& $adb shell pidof $Package 2>$null) -replace '\s', ''
    if (-not $procPid) {
        if ($lastPid) { break }   # we saw it running, now it's gone
        # not yet started -- give it a moment
    } else {
        $lastPid = $procPid
    }
    Start-Sleep -Seconds 3
}
if ((Get-Date) -ge $deadline) {
    Write-Warning "Timed out waiting for $Package to exit; pulling whatever is available."
}

# Stop logcat capture
if ($logcatProc -and -not $logcatProc.HasExited) {
    Stop-Process -Id $logcatProc.Id -Force -ErrorAction SilentlyContinue
}
if ($liveLogcatProc -and -not $liveLogcatProc.HasExited) {
    Stop-Process -Id $liveLogcatProc.Id -Force -ErrorAction SilentlyContinue
}

# 9. Pull TRX + generated images from device's files/tests/local/<Suite>/ into $ResultsDir.
# tar device-side via run-as, write to a shell-readable /data/local/tmp file, adb pull, untar.
Write-Host "Pulling results -> $ResultsDir"
if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null }
$localTar = [IO.Path]::GetTempFileName()
try {
    # The outer shell (uid 2000 = shell) writes to /data/local/tmp; the inner run-as streams
    # the tarball through stdout. If the suite dir doesn't exist yet, fallback creates an
    # empty tarball so we don't break the pipe.
    & $adb shell "run-as $Package sh -c 'cd files/tests/local/$Suite 2>/dev/null && tar c . || tar c -T /dev/null' > /data/local/tmp/$Package-results.tar" 2>$null | Out-Null
    & $adb pull "/data/local/tmp/$Package-results.tar" $localTar 2>$null | Out-Null
    & $adb shell "rm /data/local/tmp/$Package-results.tar" 2>$null | Out-Null
    & $tar x -C $ResultsDir -f $localTar 2>$null
} finally {
    Remove-Item $localTar -ErrorAction SilentlyContinue
}

# 9. Parse TRX for pass/fail
$trxFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "*.trx" -ErrorAction SilentlyContinue
if (-not $trxFiles) {
    Write-Warning "No TRX files in $ResultsDir -- tests may not have run."
    if ($startedEmulator -and -not $KeepEmulator) { & $adb emu kill 2>$null | Out-Null }
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

if ($startedEmulator -and -not $KeepEmulator) {
    Write-Host "Killing emulator we started..."
    & $adb emu kill 2>$null | Out-Null
}

if ($totalFailed -gt 0) {
    Write-Host "FAILED: $totalFailed test(s)." -ForegroundColor Red
    exit 1
}

Write-Host "OK" -ForegroundColor Green
exit 0
