# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# Example job: makes a game, opens it in Game Studio, colors its directional light red, saves, closes Game Studio, then
# builds and runs the game. Pictures: shots\example-*.png. Run it with:
#   .\build\tools\sandbox\sandbox.ps1 run -File build\tools\sandbox\examples\open-change-save-run.ps1 -TimeoutSec 1800
# The helpers throw when a step fails, which ends the job with code 1.
$name = 'ExampleGame'
$title = "$name.slnx"

$solution = "$Sbx\projects\$name\$title"
if (-not (Test-Path $solution)) { $solution = New-Game $name }

Start-Gs $solution
# The first session of a sandbox compiles the editor's shaders: several minutes
Wait-Session $name 900
Start-Sleep 20
Clear-Dialogs $title
Shot 'example-1-open'

# Entities are EntityViewModel rows of the scene tree, in its order: Camera, Directional light, Skybox, Ground, Sphere
& $Uia invoke $title 'EntityViewModel' DataItem 1
Start-Sleep 4

# The hex box of the color editor is not in the UI Automation tree: click it inside the Color row of the property grid
$row = & $Uia find $title 'Color: [' DataItem | Select-Object -First 1
if ($row -notmatch 'rect=(\d+),(\d+),(\d+),(\d+)') { throw "no Color row in the property grid: $row" }
$x = [int]$Matches[1] + [int]([int]$Matches[3] * 0.7)
$y = [int]$Matches[2] + [int]([int]$Matches[4] / 2)
& $Uia activate $title
& $Uia click $x $y
& $Uia key ctrl+a
& $Uia keys '#FF0000'
& $Uia key enter
Start-Sleep 4
Shot 'example-2-red'

& $Uia key ctrl+s
Wait-Until { (Get-GsWindows) -match "$([regex]::Escape($title)) - Stride" } 120 'saved (no * in the title)'
Clear-Dialogs $title
Shot 'example-3-saved'
Test-Crash

Get-Process Stride.GameStudio | Stop-Process
Wait-Until { -not (Get-Process Stride.GameStudio -ErrorAction SilentlyContinue) } 60 'Game Studio closed'

Push-Location "$Sbx\projects\$name"
try { dotnet build "$name.Windows\$name.Windows.csproj" -nodeReuse:false -v:m 2>&1 | Select-Object -Last 3 } finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw "game build failed ($LASTEXITCODE)" }
# The light is red in these pictures
Run-Game "$Sbx\projects\$name\Bin\Windows\Debug\$name.Windows.exe" 'example-4-game'
