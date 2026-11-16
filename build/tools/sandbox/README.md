# Game Studio in Windows Sandbox

This kit runs Game Studio in [Windows Sandbox](https://learn.microsoft.com/windows/security/application-security/application-isolation/windows-sandbox/), a clean Windows that is thrown away after use. You (or your coding agent) start Game Studio there, click and type in it, take pictures, read its log, and run the game. Use it to check an editor change on a machine without your settings, caches or installs.

It is a tool for interactive checks, run by hand. The editor tests that compare fixed screenshots are in [tests/editor](../../../tests/editor) and run on CI.

## Requirements

- Windows 11 Pro, Enterprise or Education, version 24H2 or later (Windows Sandbox is not on Home; the `wsb` command came with 24H2).
- Windows Sandbox turned on. As administrator, then restart:
  ```powershell
  Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM
  ```
- The .NET SDK of [global.json](../../../global.json). The sandbox uses the host's `dotnet`.
- The Visual C++ runtime on the host, which `start` copies into the sandbox. Without it, the sandbox installs it, which takes 4 more minutes.
- For a Debug build of a game: the D3D debug layers on the host (Settings > System > Optional features > Graphics Tools). Without them, the game fails with `0x887A002D`.

## Commands

All commands are verbs of [sandbox.ps1](sandbox.ps1), run from Windows PowerShell or `pwsh` on the host.

```powershell
# 1. Build Game Studio and the game templates of this checkout (Debug)
dotnet build sources\editor\Stride.GameStudio\Stride.GameStudio.csproj -c Debug
dotnet build sources\templates\Stride.Templates.Games\Stride.Templates.Games.csproj -c Debug

# 2. Start a sandbox and set it up (about 30 seconds; a few when it is already running)
.\build\tools\sandbox\sandbox.ps1 start

# 3. Run jobs: PowerShell scripts that run inside the sandbox, with the helpers of in-sandbox\GameStudio.ps1 loaded
.\build\tools\sandbox\sandbox.ps1 run -File build\tools\sandbox\examples\open-change-save-run.ps1 -TimeoutSec 1800
.\build\tools\sandbox\sandbox.ps1 run -Script 'Shot now; Get-GsWindows'

# 4. Look at the pictures (in <work folder>\shots); crop one to read a detail
.\build\tools\sandbox\sandbox.ps1 shot -Name now
.\build\tools\sandbox\sandbox.ps1 crop -Name now -Rect 0,0,800,600

# 5. Stop: the sandbox and all it changed inside are gone; the work folder stays
.\build\tools\sandbox\sandbox.ps1 stop
```

To check a released Game Studio instead, start with its version. It is restored inside the sandbox from nuget.org, or first from a folder of packages with `-Feed`:

```powershell
.\build\tools\sandbox\sandbox.ps1 start -GameStudioVersion 4.4.2
```

Options of `start`: `-Configuration Release`, `-NoGpu` (WARP instead of the host's GPU), `-MemoryMB`, `-NoHostCache` (released Game Studio: restore everything from nuget.org), `-ShowWindow` (the sandbox window on your desktop), `-Fresh` (a new sandbox), `-WaitMinutes N` (wait for a sandbox in use).

## The work folder

Each checkout has a work folder, `%LOCALAPPDATA%\Stride\Sandbox\<checkout folder name>` (or `-WorkDir`). It is `C:\sbx` inside the sandbox, and it stays when the sandbox stops:

| Folder | What it holds |
|---|---|
| `jobs\` | Each job, its output (`.log`) and exit code (`.done`) |
| `shots\` | Pictures |
| `logs\` | Logs that jobs copy there |
| `projects\` | Games made or copied by jobs. Game Studio edits them here. |
| `nuget-packages\` | What the sandbox restored, so a new sandbox restores faster |
| `tools\` | `uia.exe`, `fchook.dll`, `GameStudio.ps1` |

The repo is mapped read-only at its own path. Game Studio and the asset compiler write next to their exe (`local`, `roaming`, `cache`): `start` puts writable folders of the work folder there.

## One sandbox at a time

Windows Sandbox runs one sandbox at a time. `start` takes a lock (`%LOCALAPPDATA%\Stride\Sandbox\lock.json`) for an owner: the CodeAlta session (`CODEALTA_SESSION_ID`), `-Owner`, or else your Windows user.

- The lock stays until `stop`. An agent that stops to ask you something keeps its sandbox as it is.
- `start` by the owner reuses its running sandbox. Another owner's `start` fails and says who holds it; `-WaitMinutes` waits instead.
- `status` shows the owner, the work folder, and how long it has been idle.
- A lock whose sandbox is gone (host restart or sleep) is free: the next `start` takes it. The work folder is on the host, so projects, jobs and pictures are still there; only what was installed inside is lost.
- `stop -Force` stops the sandbox of another owner and releases its lock, for example when the session that held it was deleted.

## Writing jobs

A job is a PowerShell script that runs in the desktop session of the sandbox. [in-sandbox/GameStudio.ps1](in-sandbox/GameStudio.ps1) gives it:

| Helper | What it does |
|---|---|
| `New-Game <name>` | Makes a game in `C:\sbx\projects\<name>` from the game templates of the Game Studio under test; returns its solution |
| `Start-Gs <solution> [-FirstChanceLog]` | Starts Game Studio. `-FirstChanceLog` logs every first-chance exception to `logs\fchook-<pid>.log`: the exceptions Game Studio catches and never logs |
| `Wait-Session <title>` | Waits for the session to open, answers the Upgrade question, fails at any other dialog or a crash |
| `Clear-Dialogs <title>` | Answers the dialogs that came up: "reload the assemblies" gets Don't reload, others OK |
| `Shot <name>` | Picture of the screen: `shots\<name>.png` |
| `Copy-EditorLog <title> <name>` | The Editor tab of the Output panel to `logs\<name>.txt` |
| `Run-Game <exe> <name>` | Runs a game, takes two pictures, prints its output, and closes it |
| `Wait-Until { condition } <seconds> <what>` | Polls until true, fails after the timeout |
| `Get-CrashReport` | The exception of the newest crash report |
| `Test-Crash`, `Get-GsWindows`, `Get-Dialogs` | State of Game Studio |

`$Uia` is the UI driver ([uia.cs](uia.cs)): run `& $Uia` for its commands. It finds windows by a part of their title and elements by a part of their name or automation id, with an optional control type:

```powershell
& $Uia tree 'MyGame.slnx' 3                         # what the window holds
& $Uia invoke 'MyGame.slnx' 'Directional light' TreeItem
& $Uia key ctrl+s
& $Uia drag 2166 545 2166 420 ctrl                  # screen pixels, Ctrl held
```

[examples/open-change-save-run.ps1](examples/open-change-save-run.ps1) is a whole check: it makes a game, changes the color of its light in Game Studio, saves, builds and runs the game.

## Things that bite

- Build Game Studio on the host after every change to the repo, before you start it in the sandbox. When it starts, Game Studio builds the template package of the repo and what it references; that build must find everything up to date, since the repo is read-only in the sandbox.
- The first session of a sandbox takes a few minutes to open: the editor compiles its shaders.
- In Windows PowerShell, `$ErrorActionPreference = 'Stop'` turns each line a program writes to stderr into an error. Check `$LASTEXITCODE` instead.
- Some controls are not in the UI Automation tree, such as the hex box of the color editor. Click them inside the rectangle of their row, which `uia find` gives (see the example).
- Entity rows of the scene tree are `EntityViewModel` DataItems without names: `invoke` them by index, in the order of the tree.
- Files written from the host into the work folder raise no file-change event in the sandbox: Game Studio doesn't see them. Change project files from a job.
- `uia text` reads the Editor tab of the Output panel; the Build and Project tabs read back empty, so take a picture. Invoking a tab of that panel keeps the focus there until Game Studio restarts: read it last.
- Asset rows are selected with `invoke` on a ListItem named `{<type>ViewModel: <url>}` (`uia tree` lists them); clicks don't select them. Save is `ctrl+s` with an asset selected.
- A save or an undo that touches a `.cs` file brings a reload question up to 20 seconds later, and it blocks the next step without an error: `Clear-Dialogs` before each step, and check that the step did what you expected.
- `ctrl+shift+d` (the debug window) does nothing while the scene view has the focus. Close that window before a save: while it is open, a save never finishes.
- Thumbnails come within about a minute. When all of them stay placeholders, the thumbnail generator failed: that is a real bug.
- A game left running locks its files for the next build: `Run-Game` always closes it.
- Only Direct3D 11 was tried in the sandbox.
