param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor',
    [string]$Method = 'SurvivorFarm.Editor.VillageLayoutVerification.Run',
    [switch]$PrepareOnly,
    [switch]$SkipCopy,
    [switch]$UnitTests
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$qa = [System.IO.Path]::GetFullPath((Join-Path $root 'Temp/TeamQA'))
$output = Join-Path $root 'Design/Validation/Team'
if (!$qa.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'QA project must remain inside the workspace.'
}
[System.IO.Directory]::CreateDirectory($qa) | Out-Null
[System.IO.Directory]::CreateDirectory($output) | Out-Null

if (!$SkipCopy) {
    $folders = @('Assets', 'Packages', 'ProjectSettings')
    if (!(Test-Path -LiteralPath (Join-Path $qa 'Library'))) { $folders += 'Library' }
    foreach ($folder in $folders) {
        $source = Join-Path $root $folder
        $target = Join-Path $qa $folder
        & robocopy $source $target /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD 'ShaderCache' 'PackageCache/.tmp' /XF '*.lock' | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "QA copy failed: $folder" }
    }
}
if ($PrepareOnly) { Write-Output "Prepared isolated QA project: $qa"; exit 0 }

$log = Join-Path $output $(if ($UnitTests) { 'unity-tests.log' } else { 'unity-integration.log' })
$args = @('-batchmode', '-projectPath', ('"' + $qa + '"'), '--qa', '-logFile', ('"' + $log + '"'))
if ($UnitTests) {
    $args += @('-runTests', '-testPlatform', 'PlayMode', '-testResults', ('"' + (Join-Path $output 'playmode-results.xml') + '"'))
} else {
    $args += @('-executeMethod', $Method, '-screen-width', '1920', '-screen-height', '1080')
}
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $args -WindowStyle Hidden -PassThru
Write-Output "Started isolated Unity QA, process $($process.Id). Log: $log"
if (!$process.WaitForExit(900000)) {
    $process.Kill()
    throw 'Only the isolated QA process was stopped after its 15-minute timeout.'
}

$validation = Join-Path $qa 'Design/Validation'
if (Test-Path -LiteralPath $validation) {
    & robocopy $validation (Join-Path $output 'Integrated') /E /COPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Failed to collect validation artifacts.' }
}
if ($process.ExitCode -ne 0) { throw "Unity QA exited with code $($process.ExitCode). See $log" }
Write-Output 'PASS isolated Unity execution. Inspect the test report for individual results.'
