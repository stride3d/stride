# Refuses a release below one already made on its line, from the repo root. See docs/build/versioning.md.
# A version on nuget.org can't be taken back, and each release bumps Patch afterwards, so this only happens when that bump
# failed or after a wrong edit: 4.5.8-beta after 4.5.8. Lines are compared on their own, as 4.4.x still ships after 4.5.0.
param(
    [Parameter(Mandatory)][string]$Version
)

# A re-run on its own tag completes a partial deploy.
if (git tag --points-at HEAD --list "releases/$Version") { exit 0 }

$number = [version](($Version -split '-')[0])
$blocking = @(foreach ($tag in git tag --list "releases/$($number.Major).$($number.Minor).*") {
    $released = $tag -replace '^releases/', ''
    $releasedNumber = [version](($released -split '-')[0])
    # The labels of one number aren't compared: only stable sorts above them for sure.
    if ($releasedNumber -gt $number -or ($releasedNumber -eq $number -and $released -notmatch '-' -and $Version -match '-')) { $released }
})

if ($blocking.Count -gt 0) {
    echo "::error::Version $Version sorts below the released $($blocking -join ', '). Bump Patch in SharedAssemblyInfo.cs: the bump after that release may have failed."
    exit 1
}
