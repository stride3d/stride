# Builds the prerequisites installer again with Advanced Installer's own signing and puts it in a GameStudio package, in place
# of the unsigned one the release build made. See docs/build/versioning.md. Advanced Installer also signs the MSI packed inside
# the exe, which the sign tool can't reach. Needs Advanced Installer and the StrideSign* variables.
#   -Package  the GameStudio .nupkg, before it is signed
#   -Commit   the commit the package was built from: the installer is built from it, in a worktree of its own
param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Commit
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$entryName = 'tools/install-prerequisites.exe'
$packageName = Split-Path $Package -Leaf

$zip = [IO.Compression.ZipFile]::OpenRead($Package)
try { $present = $null -ne $zip.GetEntry($entryName) } finally { $zip.Dispose() }
if (-not $present) {
    Write-Host "::notice::$packageName has no $entryName (built without the prerequisites installer): nothing to replace."
    exit 0
}

$temp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$worktree = Join-Path $temp 'stride-prerequisites'
git worktree add --detach $worktree $Commit
if ($LASTEXITCODE -ne 0) { throw "Could not check out $Commit" }
try {
    dotnet build "$worktree/build/Stride.build" -t:PrerequisitesInstaller --no-restore -p:StrideSign=true
    if ($LASTEXITCODE -ne 0) { throw 'Building the prerequisites installer failed' }
    $exe = Join-Path $worktree 'sources/prerequisites/install-prerequisites.exe'
    $signature = Get-AuthenticodeSignature $exe
    if ($signature.Status -ne 'Valid') { throw "The prerequisites installer built is not signed: $($signature.Status)" }

    $zip = [IO.Compression.ZipFile]::Open($Package, 'Update')
    try {
        $zip.GetEntry($entryName).Delete()
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $exe, $entryName)
    } finally { $zip.Dispose() }
    Write-Host "Replaced $entryName in $packageName, signed by $($signature.SignerCertificate.Subject)"
} finally {
    git worktree remove --force $worktree
}
