# shipped-picks.ps1 <from> <to> <outdir>
#
# When <from> is a release on its release branch and <to> is on the mainline, finds the mainline
# work of <from>..<to> that already shipped in <from> as cherry-picks. A mainline commit is picked
# when a commit of merge-base..<from> names it in a `(cherry picked from commit <sha>)` line, or
# has the same patch (git's cherry-mark). Its picks carry the share of its changed lines they also
# change, paths ignored, so moved files and small conflict fixes still count. At 95% or more the
# commit shipped; below that it shipped in part.
#
# A first-parent entry of <to> (a squash or direct commit, or a merge commit and the commits it
# brings in) shipped when all its commits shipped. Writes, in <outdir>:
#   shipped-units.txt   the entries that shipped, one sha per line;
#   shipped-commits.txt all the commits of those entries;
#   partly-shipped.md   the entries that shipped in part, as a Markdown list for the prompt;
#   picked-commits.tsv  each picked commit: sha, share carried (%), picks.
# Prints a summary line.
param(
    [Parameter(Mandatory)][string]$From,
    [Parameter(Mandatory)][string]$To,
    [Parameter(Mandatory)][string]$OutDir
)
$ErrorActionPreference = 'Stop'
$threshold = 95
$utf8 = [Text.UTF8Encoding]::new($false)

# The output of git as one string, split by the callers on "\n" only: a line of a diff may hold a
# lone "\r", which PowerShell's own line splitting would cut. $InputLines go through stdin, as a
# list of commits can exceed a command line.
function Invoke-Git([string[]]$GitArgs, [string[]]$InputLines) {
    $psi = [Diagnostics.ProcessStartInfo]::new('git')
    foreach ($a in $GitArgs) { $psi.ArgumentList.Add($a) }
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.StandardOutputEncoding = $utf8
    $psi.RedirectStandardInput = $null -ne $InputLines
    $p = [Diagnostics.Process]::Start($psi)
    $output = $p.StandardOutput.ReadToEndAsync()
    if ($null -ne $InputLines) {
        foreach ($line in $InputLines) { $p.StandardInput.Write("$line`n") }
        $p.StandardInput.Close()
    }
    $p.WaitForExit()
    if ($p.ExitCode) { throw "git $GitArgs failed" }
    $output.Result
}
function Split-Lines([string]$Text) { $Text.Split("`n", [StringSplitOptions]::RemoveEmptyEntries) }
# Dictionaries, not @{}: hashtable keys ignore case, and changed lines must not.
function New-Map {
    [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
}
function Get-Count($Map, [string]$Key) { $Map.ContainsKey($Key) ? $Map[$Key] : 0 }

# The commits of the range, their parents and subjects.
$parents = New-Map
$subject = New-Map
foreach ($line in Split-Lines (Invoke-Git log, '--format=%H %P%x09%s', "$From..$To")) {
    $ids, $s = $line.Split("`t", 2)
    $h, $p = $ids.Split(' ')
    $parents[$h] = @($p | Where-Object { $_ })
    $subject[$h] = "$s"
}

# Each first-parent entry and its commits, oldest first: a merge brings in what its other parents
# reach and no earlier entry already has.
$units = @(Split-Lines (Invoke-Git log, '--first-parent', '--format=%H', "$From..$To"))
[array]::Reverse($units)
$unitOf = New-Map
$commitsOf = New-Map
foreach ($u in $units) {
    $unitOf[$u] = $u
    $p = $parents[$u]
    if ($p.Count -lt 2) { $commitsOf[$u] = @($u); continue }
    $stack = [Collections.Generic.List[string]]::new([string[]]($p[1..($p.Count - 1)]))
    $c = [Collections.Generic.List[string]]::new()
    while ($stack.Count) {
        $x = $stack[$stack.Count - 1]
        $stack.RemoveAt($stack.Count - 1)
        if (!$parents.ContainsKey($x) -or $unitOf.ContainsKey($x)) { continue }
        $unitOf[$x] = $u
        if ($parents[$x].Count -lt 2) { $c.Add($x) }
        $stack.AddRange([string[]]$parents[$x])
    }
    $c.Reverse()
    $commitsOf[$u] = $c.ToArray()
}

# Picks named by `-x` lines on the release branch.
$picks = New-Map    # mainline commit -> its picks
$range = @($parents.Keys)
function Resolve-Commit([string]$Sha) {
    if ($Sha.Length -eq 40) { return $parents.ContainsKey($Sha) ? $Sha : $null }
    $m = @($range | Where-Object { $_.StartsWith($Sha, [StringComparison]::Ordinal) })
    $m.Count -eq 1 ? $m[0] : $null
}
function Add-Pick([string]$Orig, [string]$Pick) {
    if (!$picks.ContainsKey($Orig)) { $picks[$Orig] = [Collections.Generic.List[string]]::new() }
    if (!$picks[$Orig].Contains($Pick)) { $picks[$Orig].Add($Pick) }
}
$log = Invoke-Git log, '--format=%H%x00%B%x00', "$To..$From"
foreach ($record in [regex]::Matches($log, '([0-9a-f]{40})\x00(.*?)\x00', 'Singleline')) {
    $pick = $record.Groups[1].Value
    foreach ($o in [regex]::Matches($record.Groups[2].Value, '\(cherry picked from commit ([0-9a-f]{7,40})\)')) {
        $orig = Resolve-Commit $o.Groups[1].Value
        if ($orig) { Add-Pick $orig $pick }
    }
}

# Commits with the same patch on both sides, picked without `-x`.
$sameLeft = [Collections.Generic.List[string]]::new()
$sameRight = [Collections.Generic.List[string]]::new()
foreach ($line in Split-Lines (Invoke-Git log, '--cherry-mark', '--left-right', '--no-merges', '--format=%m%x09%H', "$From...$To")) {
    $m, $h = $line.Split("`t")
    if ($m -ne '=') { continue }
    if ($parents.ContainsKey($h)) { if (!$picks.ContainsKey($h)) { $sameRight.Add($h) } } else { $sameLeft.Add($h) }
}

# The changed lines of each commit, per file: sign and text, spacing ignored, blank lines left out.
$files = New-Map    # sha -> list of { Path; Lines: line -> count }
$shas = @($picks.Keys) + @($picks.Values | ForEach-Object { $_ }) + $sameLeft + $sameRight
if ($shas.Count) {
    $diffs = Invoke-Git -GitArgs log, '--no-walk=unsorted', '--stdin', '--format=commit %H', '-p', '--diff-merges=first-parent',
        '--no-color', '--no-ext-diff', '--no-textconv' -InputLines $shas
    $cur = $null
    $inHeader = $false
    foreach ($line in Split-Lines $diffs) {
        if ($line -cmatch '^commit ([0-9a-f]{40})$') {
            $cur = [Collections.Generic.List[object]]::new()
            $files[$Matches[1]] = $cur
            $inHeader = $false
            continue
        }
        if ($line -cmatch '^diff --git a/.* b/(.*)$') {
            $cur.Add([pscustomobject]@{
                Path = $Matches[1]
                Lines = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
            })
            $inHeader = $true
            continue
        }
        if ($line.StartsWith('@@')) { $inHeader = $false; continue }
        if ($inHeader -or !$cur -or !$cur.Count -or $line -notmatch '^[+-]') { continue }
        # Only ASCII spacing, so a line that differs by a no-break space still differs.
        $text = ($line.Substring(1) -replace '^[\t\n\v\f\r ]+|[\t\n\v\f\r ]+$', '') -replace '[\t\n\v\f\r ]+', ' '
        if ($text -eq '') { continue }
        $lines = $cur[$cur.Count - 1].Lines
        $key = $line.Substring(0, 1) + $text
        $lines[$key] = (Get-Count $lines $key) + 1
    }
}
function Get-Files([string]$Sha) { $files.ContainsKey($Sha) ? $files[$Sha] : @() }

# Pair the cherry-marked commits by their changed lines.
function Get-LinesKey([string]$Sha) {
    $all = [Collections.Generic.List[string]]::new()
    foreach ($f in Get-Files $Sha) { foreach ($kv in $f.Lines.GetEnumerator()) { for ($i = 0; $i -lt $kv.Value; $i++) { $all.Add($kv.Key) } } }
    $all.Sort([StringComparer]::Ordinal)
    $all -join "`n"
}
$byLines = New-Map
foreach ($l in $sameLeft) {
    $key = Get-LinesKey $l
    if (!$byLines.ContainsKey($key)) { $byLines[$key] = [Collections.Generic.Queue[string]]::new() }
    $byLines[$key].Enqueue($l)
}
foreach ($o in $sameRight) {
    $queue = $byLines[(Get-LinesKey $o)]
    $picks[$o] = [Collections.Generic.List[string]]::new()
    if ($queue -and $queue.Count) { $picks[$o].Add($queue.Dequeue()) }
}

# The share of a commit's changed lines its picks carry, and the files they carry less of.
$share = New-Map
$leftOut = New-Map
foreach ($o in $picks.Keys) {
    $pool = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
    foreach ($p in $picks[$o]) {
        foreach ($f in Get-Files $p) { foreach ($kv in $f.Lines.GetEnumerator()) { $pool[$kv.Key] = (Get-Count $pool $kv.Key) + $kv.Value } }
    }
    # A commit paired by cherry-mark alone has the same patch.
    $same = $picks[$o].Count -eq 0
    $total = 0; $carried = 0
    $left = [Collections.Generic.List[string]]::new()
    foreach ($f in Get-Files $o) {
        $ft = 0; $fc = 0
        foreach ($kv in $f.Lines.GetEnumerator()) {
            $n = $kv.Value
            $m = $same ? $n : [Math]::Min((Get-Count $pool $kv.Key), $n)
            if (!$same) { $pool[$kv.Key] = (Get-Count $pool $kv.Key) - $m }
            $ft += $n
            $fc += $m
        }
        $total += $ft
        $carried += $fc
        if ($fc * 100 -lt $threshold * $ft) {
            $left.Add($fc ? ('`{0}` ({1}% picked)' -f $f.Path, [Math]::Floor(100 * $fc / $ft)) : "``$($f.Path)``")
        }
    }
    $share[$o] = $total ? [Math]::Floor(100 * $carried / $total) : 100
    $leftOut[$o] = $left
}
function Test-Shipped([string]$Sha) { $share.ContainsKey($Sha) -and $share[$Sha] -ge $threshold }

$tsv = $share.Keys | Sort-Object { $share[$_] }, { $_ } | ForEach-Object { "$_`t$($share[$_])`t$($picks[$_] -join ',')`n" }
[IO.File]::WriteAllText((Join-Path $OutDir 'picked-commits.tsv'), (-join $tsv), $utf8)

function Get-Picked([string]$Sha) {
    $as = $picks[$Sha].Count ? (($picks[$Sha] | ForEach-Object { '`' + $_.Substring(0, 10) + '`' }) -join ', ') : 'an identical commit'
    if (Test-Shipped $Sha) { return "shipped as $as." }
    $left = $leftOut[$Sha]
    $more = $left.Count -gt 15 ? " and $($left.Count - 15) more files" : ''
    "picked as $as, with $($share[$Sha])% of its changed lines. Left out: $(($left | Select-Object -First 15) -join ', ')$more."
}

# Each entry: shipped, shipped in part, or new.
$shippedUnits = [Collections.Generic.List[string]]::new()
$partialMd = [Collections.Generic.List[string]]::new()
for ($i = $units.Count - 1; $i -ge 0; $i--) {
    $u = $units[$i]
    $c = @($commitsOf[$u])
    if ((Test-Shipped $u) -or ($c.Count -and !($c | Where-Object { !(Test-Shipped $_) }))) { $shippedUnits.Add($u); continue }
    if ($parents[$u].Count -lt 2) {
        if ($share.ContainsKey($u)) { $partialMd.Add("- ``$u`` $($subject[$u]): $(Get-Picked $u)") }
        continue
    }
    $picked = @(@($u) + $c | Where-Object { $share.ContainsKey($_) })
    if (!$picked.Count) { continue }
    $lines = @("- ``$u`` $($subject[$u])")
    foreach ($x in $picked) { $lines += '  - ' + ($x -eq $u ? 'The whole merge' : "``$x`` $($subject[$x])") + ": $(Get-Picked $x)" }
    $new = @($c | Where-Object { !$share.ContainsKey($_) }).Count
    if ($new) { $lines += "  - Its $new other commit(s) were not picked." }
    $partialMd.Add($lines -join "`n")
}

[IO.File]::WriteAllText((Join-Path $OutDir 'shipped-units.txt'), (-join ($shippedUnits | ForEach-Object { "$_`n" })), $utf8)
$isShipped = [Collections.Generic.HashSet[string]]::new([string[]]$shippedUnits, [StringComparer]::Ordinal)
$shippedCommits = [string[]]@($unitOf.Keys | Where-Object { $isShipped.Contains($unitOf[$_]) })
[array]::Sort($shippedCommits, [StringComparer]::Ordinal)
[IO.File]::WriteAllText((Join-Path $OutDir 'shipped-commits.txt'), (-join ($shippedCommits | ForEach-Object { "$_`n" })), $utf8)
[IO.File]::WriteAllText((Join-Path $OutDir 'partly-shipped.md'), ($partialMd.Count ? ($partialMd -join "`n") + "`n" : ''), $utf8)

$full = @($share.Keys | Where-Object { Test-Shipped $_ }).Count
"$($share.Count) commits of $From..$To were picked into ${From}: $full shipped ($threshold% of their changed lines or more), $($share.Count - $full) in part. First-parent entries: $($shippedUnits.Count) shipped, $($partialMd.Count) in part, of $($units.Count)."
