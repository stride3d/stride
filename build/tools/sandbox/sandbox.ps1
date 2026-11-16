# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

<#
.SYNOPSIS
Runs Game Studio in Windows Sandbox, a clean Windows that is thrown away after use: start it, run jobs in it, take pictures,
stop it. See README.md next to this script.

.DESCRIPTION
  start   Starts a sandbox with Game Studio built from this repo (or -GameStudioVersion from nuget.org) and sets it up.
          The owner's second start reuses its running sandbox; -Fresh starts a new one.
  run     Runs a PowerShell job (-File or -Script) in the sandbox's desktop session and prints its output as it comes.
  shot    Takes a picture of the sandbox screen: <work folder>\shots\<Name>.png.
  crop    Crops a picture of shots\ to shots\<Name>-crop.png (-Rect x,y,w,h).
  stop    Stops the sandbox and releases the lock. -Force also stops and releases a sandbox of another owner.
  status  Says who holds the sandbox, since when, and how long it has been idle.

One sandbox runs at a time on a machine. The lock (%LOCALAPPDATA%\Stride\Sandbox\lock.json) belongs to the owner that
started it until stop: the CodeAlta session (CODEALTA_SESSION_ID), -Owner, or else the Windows user.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'run', 'shot', 'crop', 'stop', 'status')]
    [string]$Command,

    # start: the checkout whose Game Studio runs (default: the one of this script)
    [string]$Repo,
    [string]$Configuration = 'Debug',
    # start: a released Game Studio instead, restored in the sandbox
    [string]$GameStudioVersion,
    # start: a folder of .nupkg files that Stride packages come from first (release candidates)
    [string]$Feed,
    [int]$MemoryMB = 16384,
    [switch]$NoGpu,
    [switch]$NoHostCache,
    # start: the sandbox window on the user's desktop, instead of a hidden desktop
    [switch]$ShowWindow,
    [switch]$Fresh,
    [int]$WaitMinutes = 0,

    # run
    [string]$File,
    [string]$Script,
    [int]$TimeoutSec = 600,
    [switch]$NoWait,

    # shot, crop
    [string]$Name,
    [int[]]$Rect,

    # stop
    [switch]$Force,

    # every command
    [string]$WorkDir,
    [string]$Owner
)

$ErrorActionPreference = 'Stop'
$KitDir = $PSScriptRoot
if (-not $Repo) { $Repo = (Resolve-Path (Join-Path $KitDir '..\..\..')).Path }
$Repo = $Repo.TrimEnd('\')
$Root = Join-Path $env:LOCALAPPDATA 'Stride\Sandbox'
if (-not $WorkDir) { $WorkDir = Join-Path $Root (Split-Path $Repo -Leaf) }
$WorkDir = $WorkDir.TrimEnd('\')
$LockPath = Join-Path $Root 'lock.json'
$StatePath = Join-Path $WorkDir 'sandbox.json'
if (-not $Owner) { $Owner = if ($env:CODEALTA_SESSION_ID) { "codealta:$env:CODEALTA_SESSION_ID" } else { "user:$env:USERNAME" } }
$GuestUser = 'C:\Users\WDAGUtilityAccount'
$NugetDev = Join-Path $env:LOCALAPPDATA 'stride\nugetdev'
$HostCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES.TrimEnd('\') } else { Join-Path $env:USERPROFILE '.nuget\packages' }

function Fail([string]$message, [int]$code = 1) {
    Write-Host "sandbox: $message"
    exit $code
}

function Now { (Get-Date).ToString('s') }

function Get-RunningSandboxes {
    $list = (wsb list --raw | Out-String | ConvertFrom-Json).WindowsSandboxEnvironments
    @($list | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.Id } })
}

function Read-Json([string]$path) {
    if (Test-Path $path) { Get-Content $path -Raw | ConvertFrom-Json }
}

function Write-Json([string]$path, $value) {
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    $value | ConvertTo-Json -Depth 5 | Set-Content $path -Encoding UTF8
}

# A lock is alive while its sandbox runs, or while the start that wrote it still runs.
function Test-LockAlive($lock, [string[]]$running) {
    if (-not $lock) { return $false }
    if ($lock.state -eq 'starting') { return [bool](Get-Process -Id $lock.pid -ErrorAction SilentlyContinue) }
    return $running -contains $lock.sandboxId
}

function Format-Lock($lock) {
    $idle = [int]((Get-Date) - [datetime]$lock.lastUsed).TotalMinutes
    "owner $($lock.owner), work folder $($lock.workDir), sandbox $($lock.sandboxId), started $($lock.started), idle $idle min"
}

# The lock must be held by this owner for this work folder, and its sandbox must run.
function Assert-Mine {
    $lock = Read-Json $LockPath
    $running = Get-RunningSandboxes
    if (-not (Test-LockAlive $lock $running) -or $lock.state -ne 'running') {
        Fail "no sandbox runs for $WorkDir. Run 'start': a lost sandbox is set up again, the work folder stays." 3
    }
    if ($lock.owner -ne $Owner -or $lock.workDir -ne $WorkDir) {
        Fail "the sandbox belongs to another owner: $(Format-Lock $lock). Ask them, or 'stop -Force' to take it." 3
    }
    $lock.lastUsed = Now
    Write-Json $LockPath $lock
    $lock
}

function Invoke-Wsb([string]$arguments) {
    # wsb is a console program: hidden, so that no console window opens on the user's desktop
    $p = Start-Process wsb -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    $p.ExitCode
}

$HiddenDesktopCode = @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class SandboxHiddenDesktop {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO { public int cb; public string lpReserved; public string lpDesktop; public string lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr devmode, int flags, uint access, IntPtr sa);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcess(string app, StringBuilder cmd, IntPtr pa, IntPtr ta, bool inherit, int flags,
        IntPtr env, string dir, ref STARTUPINFO si, out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    // Starts a program on a desktop no one looks at. The desktop handle stays open in this process on purpose:
    // the desktop lives as long as a handle or a thread of it does.
    public static int Start(string desktop, string commandLine) {
        IntPtr desk = CreateDesktop(desktop, IntPtr.Zero, IntPtr.Zero, 0, 0x10000000u /* GENERIC_ALL */, IntPtr.Zero);
        if (desk == IntPtr.Zero) throw new Exception("CreateDesktop failed: " + Marshal.GetLastWin32Error());
        var si = new STARTUPINFO { cb = Marshal.SizeOf(typeof(STARTUPINFO)), lpDesktop = @"WinSta0\" + desktop };
        PROCESS_INFORMATION pi;
        if (!CreateProcess(null, new StringBuilder(commandLine), IntPtr.Zero, IntPtr.Zero, false, 0x08000000 /* CREATE_NO_WINDOW */,
            IntPtr.Zero, null, ref si, out pi))
            throw new Exception("CreateProcess failed: " + Marshal.GetLastWin32Error());
        CloseHandle(pi.hThread); CloseHandle(pi.hProcess);
        return pi.dwProcessId;
    }
}
'@

function Connect-Sandbox([string]$id) {
    $wsb = (Get-Command wsb).Source
    if ($ShowWindow) {
        Start-Process wsb -ArgumentList "connect --id $id" -WindowStyle Hidden | Out-Null
        return
    }
    if (-not ('SandboxHiddenDesktop' -as [type])) { Add-Type -TypeDefinition $HiddenDesktopCode }
    [SandboxHiddenDesktop]::Start('stride-sandbox', "`"$wsb`" connect --id $id") | Out-Null
}

# Starts a job in the desktop session of the sandbox; returns its id. Output goes to jobs\<id>.log, the exit code to jobs\<id>.done.
function Start-SandboxJob([string]$sandboxId, [string]$body) {
    $jobs = Join-Path $WorkDir 'jobs'
    New-Item -ItemType Directory -Force $jobs | Out-Null
    $id = (Get-Date -Format 'MMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
    Set-Content (Join-Path $jobs "$id.body.ps1") $body -Encoding UTF8
    $state = Read-Json $StatePath
    $wrapper = @"
`$env:PATH = '$($state.dotnetRoot);' + `$env:PATH
`$env:DOTNET_ROOT = '$($state.dotnetRoot)'
`$env:NUGET_PACKAGES = 'C:\sbx\nuget-packages'
if (Test-Path C:\sbx\repo-packages.props) { `$env:CustomBeforeDirectoryBuildProps = 'C:\sbx\repo-packages.props' }
# C:\sbx\global.json is the repo's: dotnet picks the same SDK as the host's builds
Set-Location C:\sbx
`$code = 0
try {
    . C:\sbx\tools\GameStudio.ps1
    # One write per line, so that the host sees the output as it comes
    & C:\sbx\jobs\$id.body.ps1 *>&1 | ForEach-Object { [IO.File]::AppendAllText('C:\sbx\jobs\$id.log', (`$_ | Out-String -Width 400).TrimEnd() + [Environment]::NewLine) }
} catch {
    Add-Content C:\sbx\jobs\$id.log "JOB ERROR: `$_" -Encoding UTF8
    `$code = 1
}
Set-Content C:\sbx\jobs\$id.done `$code
"@
    Set-Content (Join-Path $jobs "$id.ps1") $wrapper -Encoding UTF8
    $command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File C:\sbx\jobs\$id.ps1"
    Start-Process wsb -ArgumentList "exec --id $sandboxId -c `"$command`" -r ExistingLogin" -WindowStyle Hidden | Out-Null
    $id
}

# Waits for a job and prints its output as it comes. Returns its exit code, or 124 when it is still running at the timeout.
function Wait-SandboxJob([string]$id, [int]$timeoutSec) {
    $log = Join-Path $WorkDir "jobs\$id.log"
    $done = Join-Path $WorkDir "jobs\$id.done"
    $printed = 0
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        $finished = Test-Path $done
        if (Test-Path $log) {
            $lines = @(Get-Content $log -ErrorAction SilentlyContinue)
            if ($lines.Count -gt $printed) { $lines[$printed..($lines.Count - 1)] | Where-Object { $_.Trim() } | ForEach-Object { Write-Host $_ }; $printed = $lines.Count }
        }
        if ($finished) {
            $code = [int](Get-Content $done).Trim()
            Write-Host "job $id done, code $code, $([int]$watch.Elapsed.TotalSeconds) s"
            return $code
        }
        if ($watch.Elapsed.TotalSeconds -ge $timeoutSec) {
            Write-Host "job $id still running after $timeoutSec s; its output goes on in $log"
            return 124
        }
        Start-Sleep 2
    }
}

function Get-DevVersion([string]$repo) {
    # Written by the build: the version of the packages this checkout makes (X.Y.Z-devN)
    $generated = Join-Path $repo 'sources\shared\SharedAssemblyInfo.Generated.cs'
    if (-not (Test-Path $generated)) { return $null }
    $text = Get-Content $generated -Raw
    $public = [regex]::Match($text, 'PublicVersion = "([^"]+)"').Groups[1].Value
    $suffix = [regex]::Match($text, 'NuGetVersionSuffix = "([^"]*)"').Groups[1].Value
    "$public$suffix"
}


# The package folders the restore of Game Studio used (NuGetPackageFolders of its nuget.g.props).
function Get-RestoreFolders([string]$repo) {
    $props = Join-Path $repo 'sources\editor\Stride.GameStudio\obj\Stride.GameStudio.csproj.nuget.g.props'
    if (-not (Test-Path $props)) { return @() }
    $node = ([xml](Get-Content $props -Raw)).SelectSingleNode("//*[local-name()='NuGetPackageFolders']")
    if (-not $node) { return @() }
    @($node.InnerText.Split(';') | ForEach-Object { $_.Trim().TrimEnd('\') } | Where-Object { $_ })
}

function New-NugetConfig([bool]$fromRepo) {
    $sources = @('<add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />')
    $mapping = @('<packageSource key="nuget.org"><package pattern="*" /><package pattern="Stride" /><package pattern="Stride.*" /></packageSource>')
    if ($fromRepo) {
        $sources += "<add key=`"Stride Dev`" value=`"$GuestUser\AppData\Local\stride\nugetdev`" />"
        $mapping += '<packageSource key="Stride Dev"><package pattern="Stride" /><package pattern="Stride.*" /></packageSource>'
    }
    if ($Feed) {
        $sources += '<add key="feed" value="C:\sbx-feed" />'
        $mapping += '<packageSource key="feed"><package pattern="Stride" /><package pattern="Stride.*" /></packageSource>'
    }
    if (-not $NoHostCache) {
        # The host's packages folder as a read-only source, for everything but Stride: a Stride package must come
        # from the sources above, never from what the host happened to restore.
        $sources += "<add key=`"hostcache`" value=`"$HostCache`" />"
        $mapping += '<packageSource key="hostcache"><package pattern="*" /></packageSource>'
    }
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    $($sources -join "`n    ")
  </packageSources>
  <packageSourceMapping>
    $($mapping -join "`n    ")
  </packageSourceMapping>
</configuration>
"@
}

function Invoke-Setup($state) {
    Write-Host 'sandbox: setting up'
    $setup = Get-Content (Join-Path $KitDir 'in-sandbox\Setup.ps1') -Raw
    $code = Wait-SandboxJob (Start-SandboxJob $state.sandboxId $setup) 1200
    if ($code -ne 0) { throw "setup failed (code $code). The sandbox runs: 'start' runs the setup again, 'stop' ends it." }
    if ($state -is [Collections.IDictionary]) { $state.ready = $true } else { $state | Add-Member ready $true -Force }
    Write-Json $StatePath $state
    Write-Host "sandbox: ready. Work folder $WorkDir (C:\sbx in the sandbox)"
}

function Invoke-Start {
    $edition = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').EditionID
    if ($edition -match 'Core') { Fail "Windows Sandbox needs Windows Pro, Enterprise or Education; this is $edition." }
    if (-not (Get-Command wsb -ErrorAction SilentlyContinue)) {
        Fail "no 'wsb' command: Windows Sandbox 0.8 or later is needed (Windows 11 24H2). Turn it on as administrator, then restart:`n  Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM"
    }
    $fromRepo = -not $GameStudioVersion

    $state = [ordered]@{
        repo = $null; configuration = $Configuration; version = $null; gameStudio = $null
        gameStudioVersion = $GameStudioVersion; feed = $Feed; dotnetRoot = $null; sandboxId = $null; ready = $false
    }
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) { Fail 'no dotnet on PATH: install the .NET SDK of global.json.' }
    $state.dotnetRoot = Split-Path $dotnet.Source
    if ($fromRepo) {
        $state.repo = $Repo
        $state.version = Get-DevVersion $Repo
        $state.gameStudio = Join-Path $Repo "sources\editor\Stride.GameStudio\bin\$Configuration\net10.0-windows\Stride.GameStudio.exe"
        if (-not (Test-Path $state.gameStudio)) {
            Fail "no Game Studio build at $($state.gameStudio). Build it first:`n  dotnet build sources\editor\Stride.GameStudio\Stride.GameStudio.csproj -c $Configuration"
        }
    }

    # Lock: taken under a mutex, so that two starts can't both find it free
    $mutex = New-Object Threading.Mutex($false, 'Local\StrideSandboxKitLock')
    $reuseSetup = $false
    $deadline = (Get-Date).AddMinutes($WaitMinutes)
    while ($true) {
        # Abandoned when a start was killed: the wait still takes it
        try { [void]$mutex.WaitOne() } catch [Threading.AbandonedMutexException] { }
        $wait = $null
        try {
            $running = Get-RunningSandboxes
            $lock = Read-Json $LockPath
            if (Test-LockAlive $lock $running) {
                if ($lock.owner -ne $Owner -or $lock.workDir -ne $WorkDir) {
                    $wait = "the sandbox is in use: $(Format-Lock $lock)"
                } elseif ($lock.state -eq 'starting') {
                    $wait = "another start of this owner is still running (pid $($lock.pid))"
                } elseif (-not $Fresh) {
                    $previous = Read-Json $StatePath
                    if ($previous.repo -ne $state.repo -or $previous.configuration -ne $state.configuration -or $previous.gameStudioVersion -ne $state.gameStudioVersion) {
                        Fail "the running sandbox has other settings (repo $($previous.repo), $($previous.configuration), version '$($previous.gameStudioVersion)'): start -Fresh replaces it."
                    }
                    $lock.lastUsed = Now
                    Write-Json $LockPath $lock
                    Write-Host "sandbox: reusing $($lock.sandboxId), started $($lock.started) (start -Fresh for a new one)"
                    Copy-Item (Join-Path $KitDir 'in-sandbox\GameStudio.ps1') (Join-Path $WorkDir 'tools') -Force
                    if (-not $previous.ready) { $reuseSetup = $true; break }
                    return
                } else {
                    Write-Host "sandbox: -Fresh, stopping $($lock.sandboxId)"
                    Invoke-Wsb "stop --id $($lock.sandboxId)" | Out-Null
                    $running = Get-RunningSandboxes
                }
            } elseif ($lock -and $lock.state -eq 'running') {
                if ($lock.owner -eq $Owner -and $lock.workDir -eq $WorkDir) {
                    Write-Host "sandbox: the sandbox $($lock.sandboxId) is gone (host sleep or restart?); setting up a new one on the same work folder"
                } else {
                    Write-Host "sandbox: the sandbox of $($lock.owner) is gone; taking the lock over"
                }
            }
            if (-not $wait) {
                $foreign = @($running | Where-Object { $_ -ne $lock.sandboxId -or -not (Test-LockAlive $lock $running) })
                if ($foreign.Count -gt 0) {
                    $wait = "a sandbox this kit didn't start is running ($($foreign -join ', ')); close it, or 'wsb stop --id <id>'"
                }
            }
            if (-not $wait) {
                Write-Json $LockPath ([ordered]@{ owner = $Owner; workDir = $WorkDir; sandboxId = $null; state = 'starting'; pid = $PID; started = (Now); lastUsed = (Now) })
                break
            }
        } finally {
            $mutex.ReleaseMutex()
        }
        if ((Get-Date) -ge $deadline) { Fail "$wait. 'status' tells more; 'stop -Force' takes it." 3 }
        Write-Host "sandbox: $wait; waiting"
        Start-Sleep 30
    }

    try {
        foreach ($folder in 'jobs', 'shots', 'logs', 'projects', 'tools', 'system32') { New-Item -ItemType Directory -Force (Join-Path $WorkDir $folder) | Out-Null }

        # Tools: built on the host, from the host's dotnet, which the sandbox maps
        Write-Host 'sandbox: building uia and fchook'
        foreach ($tool in 'uia.cs', 'fchook.cs') {
            $out = & dotnet build (Join-Path $KitDir $tool) -o (Join-Path $WorkDir 'tools') 2>&1
            if ($LASTEXITCODE -ne 0) { $out | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }; throw "building $tool failed" }
        }
        Copy-Item (Join-Path $KitDir 'in-sandbox\GameStudio.ps1') (Join-Path $WorkDir 'tools') -Force
        Copy-Item (Join-Path $Repo 'global.json') (Join-Path $WorkDir 'global.json') -Force
        # The -devN number of a checkout comes from this file
        $ledger = Join-Path $env:LOCALAPPDATA 'stride\worktree-ids.txt'
        Remove-Item (Join-Path $WorkDir 'worktree-ids.txt'), (Join-Path $WorkDir 'repo-packages.props') -ErrorAction SilentlyContinue
        if ($fromRepo -and (Test-Path $ledger)) { Copy-Item $ledger (Join-Path $WorkDir 'worktree-ids.txt') -Force }
        Set-Content (Join-Path $WorkDir 'NuGet.Config.sandbox') (New-NugetConfig $fromRepo) -Encoding UTF8
        if ($fromRepo) {
            # Game Studio builds projects of the read-only repo when it starts: their inputs must be the host's, so that
            # the build finds them up to date. For the repo's projects only, before their nuget.g.props. SourceLink
            # writes the current commit, which changes with each commit on the host.
            Set-Content (Join-Path $WorkDir 'repo-packages.props') @"
<Project>
  <PropertyGroup Condition="`$([System.String]::Copy('`$(MSBuildProjectFullPath)').ToUpperInvariant().StartsWith('$($Repo.ToUpperInvariant())\'))">
    <NuGetPackageRoot>$HostCache\</NuGetPackageRoot>
    <UserProfileRuntimeStorePath>$env:USERPROFILE\.dotnet\store</UserProfileRuntimeStorePath>
    <StrideDevPackagesGenerating>true</StrideDevPackagesGenerating>
    <EnableSourceLink>false</EnableSourceLink>
  </PropertyGroup>
</Project>
"@ -Encoding UTF8
            # An imported file newer than a project's outputs makes MSBuild build it again
            (Get-Item (Join-Path $WorkDir 'repo-packages.props')).LastWriteTime = [datetime]'2000-01-01'
        }

        # Copied to the sandbox's System32 by its setup. The Visual C++ runtime: the engine's native parts need it, and
        # its installer takes 4 minutes in a sandbox, where MSI waits 2 minutes on the signature of each of its packages.
        # The D3D debug layers: a Debug build of a game asks for them.
        $system32 = Join-Path $WorkDir 'system32'
        Remove-Item "$system32\*" -ErrorAction SilentlyContinue
        $vcRuntime = 'vcruntime140.dll', 'vcruntime140_1.dll', 'vcruntime140_threads.dll', 'msvcp140.dll', 'msvcp140_1.dll', 'msvcp140_2.dll',
                     'msvcp140_atomic_wait.dll', 'msvcp140_codecvt_ids.dll', 'concrt140.dll', 'vccorlib140.dll', 'vcomp140.dll', 'vcamp140.dll'
        $layers = 'd3d11_3SDKLayers.dll', 'DXGIDebug.dll', 'd3d12SDKLayers.dll'
        foreach ($dll in $vcRuntime + $layers) {
            if (Test-Path "$env:windir\System32\$dll") { Copy-Item "$env:windir\System32\$dll" $system32 -Force }
        }
        if (-not (Test-Path "$system32\d3d11_3SDKLayers.dll")) {
            Write-Host 'sandbox: warning: the host has no D3D11 debug layer (Settings > Optional features > Graphics Tools): a Debug-built game fails with 0x887A002D'
        }

        $maps = New-Object Collections.ArrayList
        function Map([string]$hostPath, [string]$guestPath, [bool]$readOnly) {
            [void]$maps.Add("<MappedFolder><HostFolder>$hostPath</HostFolder><SandboxFolder>$guestPath</SandboxFolder><ReadOnly>$($readOnly.ToString().ToLower())</ReadOnly></MappedFolder>")
        }
        Map $WorkDir 'C:\sbx' $false
        Map $state.dotnetRoot $state.dotnetRoot $true
        if ($fromRepo) {
            Map $Repo $Repo $true
            if (Test-Path $NugetDev) {
                # The dev packages hold paths of the host: same place, and the sandbox user's place NuGet.Config names
                Map $NugetDev $NugetDev $true
                Map $NugetDev "$GuestUser\AppData\Local\stride\nugetdev" $true
            }
        }
        if ($Feed) { Map (Resolve-Path $Feed).Path 'C:\sbx-feed' $true }
        # The host's packages, read-only, at the same place: the repo's builds name them there. What the sandbox
        # restores goes to C:\sbx\nuget-packages (NUGET_PACKAGES).
        if (($fromRepo -or -not $NoHostCache) -and (Test-Path $HostCache)) { Map $HostCache $HostCache $true }
        if ($fromRepo) {
            # The restore of the repo also names fallback folders (Visual Studio's): the build fails without them
            foreach ($folder in (Get-RestoreFolders $Repo)) {
                if ($folder -ne $HostCache -and (Test-Path $folder)) { Map $folder $folder $true }
            }
        }
        $vgpu = if ($NoGpu) { 'Disable' } else { 'Enable' }
        $config = "<Configuration><vGPU>$vgpu</vGPU><Networking>Enable</Networking><MemoryInMB>$MemoryMB</MemoryInMB><ClipboardRedirection>Disable</ClipboardRedirection><MappedFolders>$($maps -join '')</MappedFolders></Configuration>"

        if ($reuseSetup) {
            Write-Host 'sandbox: its setup did not finish: running it again'
            Invoke-Setup (Read-Json $StatePath)
            return
        }

        Write-Host 'sandbox: starting Windows Sandbox'
        $started = wsb start -c $config --raw | Out-String
        if ($LASTEXITCODE -ne 0) { throw "wsb start failed: $started" }
        $id = ($started | ConvertFrom-Json).Id
        $state.sandboxId = $id
        Write-Json $StatePath $state
        Write-Json $LockPath ([ordered]@{ owner = $Owner; workDir = $WorkDir; sandboxId = $id; state = 'running'; pid = $PID; started = (Now); lastUsed = (Now) })
        Write-Host "sandbox: $id started (vGPU $vgpu, $MemoryMB MB)"

        if ($fromRepo) {
            # Game Studio and the asset compiler write local, roaming and cache next to their exe, in the read-only repo:
            # each gets a writable folder of the work folder in its place
            $bins = @("sources\editor\Stride.GameStudio\bin\$Configuration\net10.0-windows",
                      "sources\assets\Stride.AssetCompiler\bin\$Configuration\net10.0",
                      "sources\assets\Stride.AssetCompiler\bin\$Configuration\net10.0-windows")
            $index = 0
            foreach ($bin in $bins) {
                $index++
                if (-not (Test-Path (Join-Path $Repo $bin))) { continue }
                foreach ($folder in 'local', 'roaming', 'cache') {
                    $guestPath = Join-Path (Join-Path $Repo $bin) $folder
                    $hostPath = Join-Path $WorkDir "overlay\$index\$folder"
                    New-Item -ItemType Directory -Force $hostPath, $guestPath | Out-Null
                    $code = Invoke-Wsb "share --id $id -f `"$hostPath`" -s `"$guestPath`" -w"
                    if ($code -ne 0) { throw "wsb share of $guestPath failed ($code)" }
                }
            }
        }

        Connect-Sandbox $id
        Write-Host 'sandbox: waiting for the desktop session'
        $ready = Join-Path $WorkDir 'jobs\ready.txt'
        Remove-Item $ready -ErrorAction SilentlyContinue
        $watch = [Diagnostics.Stopwatch]::StartNew()
        while (-not (Test-Path $ready)) {
            if ($watch.Elapsed.TotalSeconds -gt 240) { throw 'no desktop session in the sandbox after 240 s' }
            Invoke-Wsb "exec --id $id -c `"cmd.exe /c echo ok> C:\sbx\jobs\ready.txt`" -r ExistingLogin" | Out-Null
            if (-not (Test-Path $ready)) { Start-Sleep 5 }
        }
        Write-Host "sandbox: desktop session ready after $([int]$watch.Elapsed.TotalSeconds) s"
        Invoke-Setup $state
    } catch {
        $lock = Read-Json $LockPath
        if ($lock -and $lock.state -eq 'starting' -and $lock.pid -eq $PID) { Remove-Item $LockPath -Force }
        throw
    }
}

function Invoke-Stop {
    $lock = Read-Json $LockPath
    $running = Get-RunningSandboxes
    if (-not $lock) {
        if ($running.Count -gt 0) { Fail "no lock, but sandboxes this kit didn't start run: $($running -join ', '). 'wsb stop --id <id>' stops one." }
        Write-Host 'sandbox: nothing to stop'
        return
    }
    if (($lock.owner -ne $Owner -or $lock.workDir -ne $WorkDir) -and (Test-LockAlive $lock $running) -and -not $Force) {
        Fail "the sandbox belongs to another owner: $(Format-Lock $lock). 'stop -Force' stops it." 3
    }
    if ($lock.sandboxId -and $running -contains $lock.sandboxId) {
        $code = Invoke-Wsb "stop --id $($lock.sandboxId)"
        Write-Host "sandbox: stopped $($lock.sandboxId) (exit $code)"
    }
    Remove-Item $LockPath -Force
    if ($lock.owner -ne $Owner) { Write-Host "sandbox: released the lock of $($lock.owner); its work folder $($lock.workDir) stays" }
    else { Write-Host "sandbox: lock released; the work folder $($lock.workDir) stays" }
}

function Invoke-Status {
    $lock = Read-Json $LockPath
    $running = Get-RunningSandboxes
    if ($lock) {
        $alive = Test-LockAlive $lock $running
        Write-Host "lock: $(Format-Lock $lock), $(if ($alive) { 'running' } else { 'sandbox gone (stale lock: the next start takes it over)' })"
        Write-Host "this caller: owner $Owner, work folder $WorkDir$(if ($lock.owner -eq $Owner -and $lock.workDir -eq $WorkDir) { ' (holds the lock)' })"
    } else {
        Write-Host 'lock: free'
    }
    $foreign = @($running | Where-Object { -not $lock -or $_ -ne $lock.sandboxId })
    if ($foreign.Count -gt 0) { Write-Host "sandboxes this kit didn't start: $($foreign -join ', ')" }
}

function Get-ShotPath([string]$name) {
    if (-not $name) { Fail '-Name is needed' }
    Join-Path $WorkDir "shots\$name.png"
}

switch ($Command) {
    'start' { Invoke-Start }
    'stop' { Invoke-Stop }
    'status' { Invoke-Status }
    'run' {
        $lock = Assert-Mine
        if ($File) { $Script = Get-Content $File -Raw }
        if (-not $Script) { Fail 'run needs -File or -Script' }
        $id = Start-SandboxJob $lock.sandboxId $Script
        Write-Host "job $id"
        if ($NoWait) { return }
        exit (Wait-SandboxJob $id $TimeoutSec)
    }
    'shot' {
        $lock = Assert-Mine
        $path = Get-ShotPath $Name
        $code = Wait-SandboxJob (Start-SandboxJob $lock.sandboxId "& `$Uia shot C:\sbx\shots\$Name.png") 60
        if ($code -eq 0) { Write-Host $path }
        exit $code
    }
    'crop' {
        $path = Get-ShotPath $Name
        if ($Rect.Count -ne 4) { Fail '-Rect x,y,w,h is needed' }
        Add-Type -AssemblyName System.Drawing
        $image = [Drawing.Image]::FromFile($path)
        try {
            $bitmap = New-Object Drawing.Bitmap $Rect[2], $Rect[3]
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            $graphics.DrawImage($image, (New-Object Drawing.Rectangle 0, 0, $Rect[2], $Rect[3]), (New-Object Drawing.Rectangle $Rect[0], $Rect[1], $Rect[2], $Rect[3]), [Drawing.GraphicsUnit]::Pixel)
            $out = Join-Path $WorkDir "shots\$Name-crop.png"
            $bitmap.Save($out)
            $graphics.Dispose(); $bitmap.Dispose()
            Write-Host $out
        } finally { $image.Dispose() }
    }
}
