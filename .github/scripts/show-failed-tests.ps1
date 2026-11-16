# Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
# Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

# Lists the failed tests of the .trx files in -Path, in red, for the "Failed tests" step that follows the test report in CI.
# Each test gets its full name, the first line of its message, and where it failed: the first stack frame in Stride code,
# and the frame in the test itself when that is another one. A test run that stopped (crash, hang) without a failed test
# shows its error instead. Exit code 1 when there is something to show, so that the step is red and its short log is the
# one to open. No ::error:: lines: the test report step already makes the annotations.
param(
    [string]$Path = 'TestResults'
)

$esc = [char]27
$red = "$esc[31m"
$boldRed = "$esc[1;31m"
$yellow = "$esc[33m"
$reset = "$esc[0m"
# PowerShell 7 can remove the colours when its output goes to a file or a pipe, as on CI.
if ($PSStyle) { $PSStyle.OutputRendering = 'Ansi' }

$failedOutcomes = @('Failed', 'Error', 'Timeout', 'Aborted')
$workspace = $env:GITHUB_WORKSPACE

function Get-ShortPath([string]$file) {
    if ($workspace -and $file.StartsWith($workspace, [StringComparison]::OrdinalIgnoreCase)) {
        $file = $file.Substring($workspace.Length).TrimStart('\', '/')
    } elseif ($file.StartsWith('/_/')) {
        # ContinuousIntegrationBuild writes the source root as /_/ in the pdbs
        $file = $file.Substring(3)
    }
    $file.Replace('\', '/')
}

# A .NET stack frame reads "at Namespace.Type.Method(args) in path:line 42"; frames without symbols have no " in " part.
function Get-StackFrames([string]$stackTrace) {
    foreach ($line in $stackTrace -split "`r?`n") {
        $line = $line.Trim()
        if (-not $line.StartsWith('at ')) { continue }
        $method = $line.Substring(3)
        $location = $null
        $in = $line.IndexOf(') in ')
        if ($in -ge 0) {
            $method = $line.Substring(3, $in - 2)
            $where = $line.Substring($in + 5)
            $lineMark = $where.LastIndexOf(':line ')
            $location = if ($lineMark -ge 0) { "$(Get-ShortPath $where.Substring(0, $lineMark)):$($where.Substring($lineMark + 6))" } else { Get-ShortPath $where }
        }
        [pscustomobject]@{ Method = $method; Location = $location }
    }
}

function Get-FirstLine([string]$text) {
    foreach ($line in $text -split "`r?`n") {
        if ($line.Trim()) { return $line.Trim() }
    }
    ''
}

$trxFiles = @(Get-ChildItem -Path $Path -Filter '*.trx' -File -ErrorAction SilentlyContinue | Sort-Object Name)
if ($trxFiles.Count -eq 0) {
    Write-Host "${yellow}No .trx file in $Path`: no test wrote its results. The log of the step that runs the tests says why.$reset"
    exit 0
}

$failures = [System.Collections.Generic.List[object]]::new()
$runErrors = [System.Collections.Generic.List[object]]::new()
foreach ($trx in $trxFiles) {
    $doc = New-Object System.Xml.XmlDocument
    try {
        $doc.Load($trx.FullName)
    } catch {
        Write-Host "${yellow}$($trx.Name) could not be read: $($_.Exception.Message)$reset"
        continue
    }
    $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')

    $classNames = @{}
    foreach ($test in $doc.SelectNodes('/t:TestRun/t:TestDefinitions/t:UnitTest', $ns)) {
        $method = $test.SelectSingleNode('t:TestMethod', $ns)
        # Some writers give the class with its assembly: "Namespace.Class, Assembly".
        if ($method) { $classNames[$test.GetAttribute('id')] = ($method.GetAttribute('className') -split ',')[0].Trim() }
    }

    $failedInFile = 0
    foreach ($result in $doc.SelectNodes('/t:TestRun/t:Results/t:UnitTestResult', $ns)) {
        if ($failedOutcomes -notcontains $result.GetAttribute('outcome')) { continue }
        $failedInFile++
        $className = $classNames[$result.GetAttribute('testId')]
        $name = $result.GetAttribute('testName')
        if ($className -and -not $name.StartsWith("$className.")) { $name = "$className.$name" }

        $message = $result.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $stackTrace = $result.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
        $frames = if ($stackTrace) { @(Get-StackFrames $stackTrace.InnerText) } else { @() }
        $located = @($frames | Where-Object { $_.Location })
        $strideFrame = $located | Where-Object { $_.Method.StartsWith('Stride.') } | Select-Object -First 1
        if (-not $strideFrame) { $strideFrame = $located | Select-Object -First 1 }
        $testFrame = if ($className) { $located | Where-Object { $_.Method.StartsWith("$className.") } | Select-Object -First 1 }

        $failures.Add([pscustomobject]@{
            Name = $name
            Outcome = $result.GetAttribute('outcome')
            Message = if ($message) { Get-FirstLine $message.InnerText } else { '' }
            StrideFrame = $strideFrame
            TestFrame = if ($testFrame -and $testFrame -ne $strideFrame) { $testFrame }
        })
    }

    # With a failed test, the errors of the run only repeat it. Without one, they say why the run stopped.
    if ($failedInFile -eq 0) {
        foreach ($info in $doc.SelectNodes("/t:TestRun/t:ResultSummary/t:RunInfos/t:RunInfo[@outcome='Error']", $ns)) {
            $text = $info.SelectSingleNode('t:Text', $ns)
            if ($text -and $text.InnerText.Trim()) { $runErrors.Add([pscustomobject]@{ File = $trx.Name; Text = $text.InnerText.Trim() }) }
        }
    }
}

if ($failures.Count -eq 0 -and $runErrors.Count -eq 0) {
    Write-Host "${yellow}No failed test in the $($trxFiles.Count) .trx file(s) of $Path. The log of the step that runs the tests says what failed.$reset"
    exit 0
}

if ($failures.Count -gt 0) {
    Write-Host "$boldRed$($failures.Count) failed test(s):$reset"
    foreach ($failure in $failures) {
        Write-Host ''
        $outcome = if ($failure.Outcome -ne 'Failed') { " ($($failure.Outcome))" } else { '' }
        Write-Host "$boldRed$($failure.Name)$outcome$reset"
        if ($failure.Message) { Write-Host "$red    $($failure.Message)$reset" }
        if ($failure.StrideFrame) { Write-Host "$red    at $($failure.StrideFrame.Location)$reset" }
        if ($failure.TestFrame) { Write-Host "$red    in the test at $($failure.TestFrame.Location)$reset" }
    }
}

if ($runErrors.Count -gt 0) {
    if ($failures.Count -gt 0) { Write-Host '' }
    Write-Host "${boldRed}Test runs that stopped with an error:$reset"
    foreach ($runError in $runErrors) {
        Write-Host ''
        Write-Host "$boldRed$($runError.File)$reset"
        # A crash or hang message names the tests that were running; past a few lines it is the whole log again.
        foreach ($line in @($runError.Text -split "`r?`n" | Select-Object -First 10)) { Write-Host "$red    $line$reset" }
    }
}

exit 1
