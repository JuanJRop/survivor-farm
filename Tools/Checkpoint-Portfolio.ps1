param()
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$snapshot = Join-Path $taskRoot 'Design/Validation/pre-vertical-slice-2026-09-14.zip'
if (!(Test-Path -LiteralPath $snapshot)) { throw 'The pre-change snapshot is required.' }
Set-Location -LiteralPath $taskRoot

function Invoke-PortfolioGit {
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$GitArgs)
    & git -c "safe.directory=$taskRoot" -c core.safecrlf=false @GitArgs
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $($GitArgs -join ' ')" }
}

# Preserve the working files throughout. The first tree is assembled in Git's
# index using the audited snapshot, then the current implementation is committed.
# This separates the user's substantial pre-existing work from the new slice.
& git -c "safe.directory=$taskRoot" diff --cached --quiet
if ($LASTEXITCODE -ne 0) { throw 'The existing index is not empty; preserve it and stop.' }
Invoke-PortfolioGit @('switch', '-c', 'codex/portfolio-three-nights')
Invoke-PortfolioGit @('add', '--', 'Assets', 'Packages', 'ProjectSettings', '.gitignore', '.gitattributes')

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($snapshot)
$originalPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$mappings = @{
    'Scripts/' = 'Assets/SurvivorFarm/Scripts/'
    'Scenes/' = 'Assets/SurvivorFarm/Scenes/'
    'Tests/' = 'Assets/SurvivorFarm/Tests/'
    'ProjectSettings/' = 'ProjectSettings/'
}
try {
    foreach ($entry in $archive.Entries) {
        if ([string]::IsNullOrEmpty($entry.Name)) { continue }
        $name = $entry.FullName.Replace('\', '/')
        $target = $null
        foreach ($prefix in $mappings.Keys) {
            if ($name.StartsWith($prefix)) { $target = $mappings[$prefix] + $name.Substring($prefix.Length); break }
        }
        if ($null -eq $target) { continue }
        [void]$originalPaths.Add($target)
        $startInfo = [Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = 'git'
        $startInfo.WorkingDirectory = $taskRoot
        $startInfo.Arguments = '-c safe.directory="' + $taskRoot + '" -c core.safecrlf=false hash-object -w --path "' + $target + '" --stdin'
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardInput = $true
        $startInfo.RedirectStandardOutput = $true
        $process = [Diagnostics.Process]::Start($startInfo)
        $stream = $entry.Open()
        try { $stream.CopyTo($process.StandardInput.BaseStream) } finally { $stream.Dispose(); $process.StandardInput.Close() }
        $hash = $process.StandardOutput.ReadToEnd().Trim()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0 -or $hash -notmatch '^[a-f0-9]{40}$') { throw "Could not record $target" }
        Invoke-PortfolioGit @('update-index', '--add', '--cacheinfo', "100644,$hash,$target")
        $process.Dispose()
    }
    foreach ($folder in $mappings.Values) {
        $files = & git -c "safe.directory=$taskRoot" ls-files -- $folder
        foreach ($file in $files) {
            if (!$originalPaths.Contains($file)) { Invoke-PortfolioGit @('update-index', '--force-remove', '--', $file) }
        }
    }
    Invoke-PortfolioGit @('update-index', '--force-remove', '--', 'Assets/SurvivorFarm/Data/ScriptableObjects/PortfolioSettings.asset', 'Assets/SurvivorFarm/Data/ScriptableObjects/PortfolioSettings.asset.meta')
    Invoke-PortfolioGit @('commit', '-q', '-m', 'Checkpoint existing farm systems before portfolio integration')
} finally { $archive.Dispose() }

Invoke-PortfolioGit @('add', '--', 'Assets', 'Packages', 'ProjectSettings', 'README.md',
    'Design/Development/VERTICAL-SLICE-AUDIT-2026-09-14.md',
    'Design/Development/PORTFOLIO-HANDOFF.md', 'Tools/Checkpoint-Portfolio.ps1')
Invoke-PortfolioGit @('commit', '-q', '-m', 'Connect three-night portfolio slice on the existing farm')
Invoke-PortfolioGit @('log', '-2', '--format=%h %s')
