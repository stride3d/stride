# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# Helpers for the jobs that 'sandbox.ps1 run' runs in the sandbox; every job has them loaded. See README.md.
#
# C:\sbx is the work folder of the host: jobs\, shots\, logs\, projects\ survive the sandbox. Everything else in the
# sandbox is thrown away with it.

$Sbx = 'C:\sbx'
$Uia = "$Sbx\tools\uia.exe"
$Sandbox = Get-Content "$Sbx\sandbox.json" -Raw | ConvertFrom-Json
$Repo = $Sandbox.repo

function Find-GameStudio {
    if ($Sandbox.gameStudio) { return $Sandbox.gameStudio }
    $package = "$env:NUGET_PACKAGES\stride.gamestudio\$($Sandbox.gameStudioVersion)"
    Get-ChildItem $package -Recurse -Filter Stride.GameStudio.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}
$Gs = Find-GameStudio

function Stamp { Get-Date -Format HH:mm:ss }

# Takes a picture of the whole screen: C:\sbx\shots\<name>.png
function Shot([string]$Name) {
    & $Uia shot "$Sbx\shots\$Name.png" | Out-Null
    "$(Stamp) shot $Name"
}

# Polls a condition every 2 s; throws with -What when it is still false after -TimeoutSec.
function Wait-Until([scriptblock]$Condition, [int]$TimeoutSec = 120, [string]$What = 'condition') {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (-not (& $Condition)) {
        if ($watch.Elapsed.TotalSeconds -ge $TimeoutSec) { throw "timeout after $TimeoutSec s: $What" }
        Start-Sleep 2
    }
    "$(Stamp) $What after $([int]$watch.Elapsed.TotalSeconds) s"
}

# The visible windows of Game Studio, as 'uia windows' lines: hwnd:0x... Stride.GameStudio [x,y,w,h] "title"
function Get-GsWindows { @(& $Uia windows | Where-Object { $_ -match ' Stride\.GameStudio \[' }) }

# The windows of Game Studio but its main window (the one whose title has -Main) and the debug window.
function Get-Dialogs([string]$Main) {
    @(Get-GsWindows | Where-Object { $_ -notmatch 'DebugWindow' -and (-not $Main -or $_ -notmatch [regex]::Escape($Main)) })
}

function Get-Hwnd([string]$windowLine) { ([regex]::Match($windowLine, '^hwnd:0x[0-9a-f]+')).Value }

# Starts Game Studio on a solution. -FirstChanceLog logs every first-chance exception to logs\fchook-<pid>.log.
function Start-Gs([string]$Solution, [string]$Arguments = '', [switch]$FirstChanceLog, [string]$Exe = $Gs) {
    if (-not $Exe -or -not (Test-Path $Exe)) { throw "no Game Studio at '$Exe'" }
    if ($FirstChanceLog) {
        $env:DOTNET_STARTUP_HOOKS = "$Sbx\tools\fchook.dll"
        $env:FCHOOK_DIR = "$Sbx\logs"
    } else {
        Remove-Item Env:DOTNET_STARTUP_HOOKS -ErrorAction SilentlyContinue
    }
    $argumentLine = ((@($(if ($Solution) { "`"$Solution`"" }), $Arguments) | Where-Object { $_ }) -join ' ')
    $start = @{ FilePath = $Exe; WorkingDirectory = (Split-Path $Exe); PassThru = $true }
    if ($argumentLine) { $start.ArgumentList = $argumentLine }
    $p = Start-Process @start
    "$(Stamp) Game Studio started, pid $($p.Id)"
}

# Waits until the main window of Game Studio shows -Title and the "Opening session" window is gone. Answers an Upgrade
# question with Upgrade (for every package); throws at any other dialog, with its tree. A session that opens with errors
# keeps the "Opening session" window, which says "Operation failed.": throws with the errors of its log, or with
# -AllowErrors closes that window and returns them.
function Wait-Session([string]$Title, [int]$TimeoutSec = 600, [switch]$AllowErrors) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $last = ''
    while ($watch.Elapsed.TotalSeconds -lt $TimeoutSec) {
        $windows = Get-GsWindows
        $joined = $windows -join "`n"
        if ($joined -ne $last) { "[$([int]$watch.Elapsed.TotalSeconds) s] $joined"; $last = $joined }
        $crash = Test-Crash
        if ($crash -ne 'no crash window') { throw "$crash; 'Shot' and the crash window's View Report tell more" }
        $main = $windows | Where-Object { $_ -match 'Stride Game Studio' -and $_ -match [regex]::Escape($Title) }
        $opening = $windows | Where-Object { $_ -match 'Opening session' }
        if ($main -and -not $opening) { return "$(Stamp) session $Title open after $([int]$watch.Elapsed.TotalSeconds) s" }
        if ($main -and $opening) {
            $hwnd = Get-Hwnd @($opening)[0]
            if ((& $Uia find $hwnd '=Operation failed.' Text) -match '^Text') {
                $errors = @(& $Uia text $hwnd PART_LogTextBox Document | Where-Object { $_ -match '^\s*(\[[^\]]*\]: )?(Error|Fatal)' })
                if (-not $AllowErrors) { throw "session $Title opened with $($errors.Count) error(s):`n$($errors -join "`n")" }
                & $Uia invoke $hwnd '=Close' Button | Out-Null
                $errors
                return "$(Stamp) session $Title open with $($errors.Count) error(s) after $([int]$watch.Elapsed.TotalSeconds) s"
            }
        }
        # Message boxes are titled "Stride Game Studio <version> (.NET <version>)"
        foreach ($dialog in ($windows | Where-Object { $_ -match '"Stride Game Studio [^"]*\(\.NET [^"]*"$' -and $_ -notmatch [regex]::Escape($Title) })) {
            $hwnd = Get-Hwnd $dialog
            if ((& $Uia find $hwnd '=Upgrade' Button) -match '^Button') {
                & $Uia invoke $hwnd 'Do this for every' CheckBox | Out-Null
                & $Uia invoke $hwnd '=Upgrade' Button | Out-Null
                "[$([int]$watch.Elapsed.TotalSeconds) s] answered Upgrade"
            } else {
                & $Uia tree $hwnd 3
                throw "unexpected dialog: $dialog"
            }
        }
        Start-Sleep 3
    }
    throw "session $Title not open after $TimeoutSec s"
}

# Answers the dialogs that come after a save or an undo that touches code (up to 20 s later): "reload the assemblies"
# gets Don't reload, any other OK. Call it before each step.
function Clear-Dialogs([string]$Main) {
    foreach ($dialog in (Get-Dialogs $Main)) {
        $hwnd = Get-Hwnd $dialog
        $first = @(& $Uia tree $hwnd 1)[0]
        "$(Stamp) dialog: $($first.Substring(0, [Math]::Min(150, $first.Length)))"
        if ($first -match 'reload the assemblies') { & $Uia invoke $hwnd "=Don't reload" Button | Out-Null }
        else { & $Uia invoke $hwnd '=OK' Button | Out-Null }
        Start-Sleep 4
    }
}

# The exception of the newest crash report (written by the crash window of Game Studio or of the asset compiler).
function Get-CrashReport([string]$Application = 'gamestudio', [int]$Lines = 30) {
    $file = Get-ChildItem "$env:LOCALAPPDATA\stride\crash-reports\$Application" -Recurse -Filter *.json -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $file) { return "no crash report of $Application" }
    "$($file.FullName)"
    $exception = ((Get-Content $file.FullName -Raw | ConvertFrom-Json).Report | Where-Object Key -eq 'Exception').Value
    $exception -split "`n" | Select-Object -First $Lines
}

function Test-Crash {
    if (Get-Process Stride.CrashReporter -ErrorAction SilentlyContinue) { 'CRASH REPORT WINDOW OPEN' }
    elseif (-not (Get-Process Stride.GameStudio -ErrorAction SilentlyContinue)) { 'GAME STUDIO NOT RUNNING' }
    else { 'no crash window' }
}

# Copies the Editor tab of the Output panel to logs\<Name>.txt. The Build and Project tabs read back empty: take a picture
# of them. Invoking a tab of that panel keeps the focus there until Game Studio restarts, so call this last.
function Copy-EditorLog([string]$Title, [string]$Name) {
    & $Uia invoke $Title '=Editor' TabItem | Out-Null
    Start-Sleep 2
    $file = "$Sbx\logs\$Name.txt"
    & $Uia text $Title PART_LogTextBox Document | Out-File $file -Encoding utf8 -Width 800
    "$(Stamp) editor log: $file, $(@(Get-Content $file).Count) lines"
    Get-Content $file | Select-String '^\s*(\[[^\]]*\]: )?(Error|Fatal)' | Select-Object -First 20 | ForEach-Object { $_.Line }
}

# Makes a game in C:\sbx\projects\<Name> from the game templates of the Game Studio under test: the -devN package of the
# repo, or the released one. Returns the path of its solution.
function New-Game([string]$Name, [string]$Template = 'stride-game', [string[]]$Options = @()) {
    # In Windows PowerShell, 'Stop' turns each stderr line of a program into an error: exit codes are checked instead
    $ErrorActionPreference = 'Continue'
    if ($Repo) {
        $package = "$env:LOCALAPPDATA\stride\nugetdev\Stride.Templates.Games.$($Sandbox.version).nupkg"
        if (-not (Test-Path $package)) { throw "no ${package}: build sources\templates\Stride.Templates.Games on the host" }
        # A rebuilt package keeps its version: its time tells it apart
        $hive = "$Sbx\templates-hive\$($Sandbox.version)-$((Get-Item $package).LastWriteTimeUtc.Ticks)"
    } else {
        $package = "Stride.Templates.Games::$($Sandbox.gameStudioVersion)"
        $hive = "$Sbx\templates-hive\$($Sandbox.gameStudioVersion)"
    }
    $out = "$Sbx\projects\$Name"
    if (Test-Path $out) { throw "$out exists: pick another name, or remove it" }
    # One hive per package, installed once: installing again over it (--force) breaks it
    if (-not (Test-Path "$hive\installed")) {
        dotnet new install $package --debug:custom-hive $hive 2>&1 | Select-Object -Last 3 | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) { throw "dotnet new install $package failed" }
        Set-Content "$hive\installed" $package
    }
    dotnet new $Template -n $Name -o $out @Options --debug:custom-hive $hive 2>&1 | Select-Object -Last 3 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "dotnet new $Template failed" }
    Get-ChildItem $out -File | Where-Object { $_.Extension -in '.sln', '.slnx' } | Select-Object -First 1 -ExpandProperty FullName
}

# Runs a built game for -WaitSec, takes two pictures (shots\<Name>-a.png, -b.png), prints the end of its output, and always
# closes it: a game left running locks its files for the next build.
function Run-Game([string]$Exe, [string]$Name, [int]$WaitSec = 25) {
    $out = "$Sbx\logs\$Name.out.txt"
    $err = "$Sbx\logs\$Name.err.txt"
    $p = Start-Process $Exe -WorkingDirectory (Split-Path $Exe) -PassThru -RedirectStandardOutput $out -RedirectStandardError $err
    try {
        Start-Sleep $WaitSec
        if ($p.HasExited) { "GAME EXITED, code $($p.ExitCode)" }
        else { "$(Stamp) game running, pid $($p.Id), title '$((Get-Process -Id $p.Id).MainWindowTitle)'" }
        Shot "$Name-a"
        Start-Sleep 3
        Shot "$Name-b"
    } finally {
        if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force; $p.WaitForExit(10000) | Out-Null }
    }
    '--- stdout'; Get-Content $out -ErrorAction SilentlyContinue | Select-Object -Last 15
    '--- stderr'; Get-Content $err -ErrorAction SilentlyContinue | Select-Object -Last 15
}
