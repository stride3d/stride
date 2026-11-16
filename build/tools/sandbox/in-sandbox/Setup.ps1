# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# First job of a new sandbox, run by 'sandbox.ps1 start' (GameStudio.ps1 is loaded): installs what a clean Windows lacks.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# The host's Visual C++ runtime and D3D debug layers, which 'start' copied
$dlls = @(Get-ChildItem "$Sbx\system32\*.dll" -ErrorAction SilentlyContinue)
foreach ($dll in $dlls) { Copy-Item $dll.FullName "$env:windir\System32" -Force }
"copied to System32: $(if ($dlls) { ($dlls | ForEach-Object Name) -join ', ' } else { 'none' })"

# The native parts of the engine need the Visual C++ runtime: installed when the host had none to copy
if (-not (Test-Path "$env:windir\System32\vcruntime140.dll")) {
    $installer = Join-Path $env:TEMP 'vc_redist.x64.exe'
    Invoke-WebRequest https://aka.ms/vs/17/release/vc_redist.x64.exe -OutFile $installer -UseBasicParsing
    $signature = Get-AuthenticodeSignature $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw "vc_redist.x64.exe is not signed by Microsoft: $($signature.Status) $($signature.SignerCertificate.Subject)"
    }
    $p = Start-Process $installer -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
    "vc_redist: exit $($p.ExitCode)"
}

if (Test-Path "$Sbx\worktree-ids.txt") {
    New-Item -ItemType Directory -Force "$env:LOCALAPPDATA\stride" | Out-Null
    Copy-Item "$Sbx\worktree-ids.txt" "$env:LOCALAPPDATA\stride\worktree-ids.txt" -Force
}

New-Item -ItemType Directory -Force "$env:APPDATA\NuGet" | Out-Null
Copy-Item "$Sbx\NuGet.Config.sandbox" "$env:APPDATA\NuGet\NuGet.Config" -Force

# For what starts outside the jobs too: Game Studio starts MSBuild, the asset compiler and the game
[Environment]::SetEnvironmentVariable('DOTNET_ROOT', $Sandbox.dotnetRoot, 'User')
$userPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
if (($userPath -split ';') -notcontains $Sandbox.dotnetRoot) { [Environment]::SetEnvironmentVariable('PATH', "$($Sandbox.dotnetRoot);$userPath", 'User') }
[Environment]::SetEnvironmentVariable('NUGET_PACKAGES', "$Sbx\nuget-packages", 'User')
if (Test-Path "$Sbx\repo-packages.props") { [Environment]::SetEnvironmentVariable('CustomBeforeDirectoryBuildProps', "$Sbx\repo-packages.props", 'User') }
[Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', '1', 'User')
[Environment]::SetEnvironmentVariable('DOTNET_NOLOGO', '1', 'User')
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

if ($Sandbox.gameStudioVersion) {
    # A released Game Studio, restored as a user gets it (C:\sbx\global.json picks the SDK)
    $restore = "$Sbx\gamestudio-restore"
    New-Item -ItemType Directory -Force $restore | Out-Null
    Set-Content "$restore\restore.csproj" "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>net10.0-windows</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><PackageReference Include=`"Stride.GameStudio`" Version=`"$($Sandbox.gameStudioVersion)`" /></ItemGroup></Project>"
    Push-Location $restore
    try { dotnet restore restore.csproj -nodeReuse:false 2>&1 | Select-Object -Last 5 } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "restore of Stride.GameStudio $($Sandbox.gameStudioVersion) failed" }
    $Gs = Find-GameStudio
    if (-not $Gs) { throw "Stride.GameStudio $($Sandbox.gameStudioVersion) restored, but no Stride.GameStudio.exe in it" }
}

"dotnet $(dotnet --version)"
"game studio: $Gs"
Get-CimInstance Win32_VideoController | ForEach-Object { "gpu: $($_.Name)" }
