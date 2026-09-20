param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor',
    [string]$Output = 'Design/Validation/Team/Compile'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $dotnet = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
    $compiler = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
    if (!(Test-Path -LiteralPath $compiler)) { throw "Compiler not found: $compiler" }
    $runtimeTemplate = Get-ChildItem 'Library/Bee/artifacts' -Recurse -Filter 'SurvivorFarm.Runtime.rsp' |
        Where-Object { $_.Directory.Name -like '*E.dag' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (!$runtimeTemplate) { throw 'Open the project in Unity once to generate compiler references.' }
    $templates = $runtimeTemplate.Directory.FullName
    $outDir = [System.IO.Path]::GetFullPath((Join-Path $root $Output))
    if (!$outDir.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Output must stay inside the project.'
    }
    [System.IO.Directory]::CreateDirectory($outDir) | Out-Null
    $assemblies = @(
        @{ Name = 'SurvivorFarm.Runtime'; Source = 'Assets/SurvivorFarm/Scripts/Runtime' },
        @{ Name = 'Assembly-CSharp-Editor'; Source = 'Assets/SurvivorFarm/Scripts/Editor' },
        @{ Name = 'SurvivorFarm.PlayModeTests'; Source = 'Assets/SurvivorFarm/Tests/PlayMode' }
    )
    foreach ($assembly in $assemblies) {
        $template = Join-Path $templates ($assembly.Name + '.rsp')
        $options = Get-Content -LiteralPath $template | Where-Object {
            $_ -notmatch '^".*\.cs"$' -and $_ -notmatch '^[-/]out:' -and
            $_ -notmatch '^[-/](analyzer|additionalfile|refout):'
        }
        $runtimeDll = (Join-Path $outDir 'SurvivorFarm.Runtime.dll').Replace('\', '/')
        $options = @($options | ForEach-Object {
            if ($_ -match '^[-/]r:.*[/\\]SurvivorFarm\.Runtime(?:\.ref)?\.dll"$') { '-r:"' + $runtimeDll + '"' } else { $_ }
        })
        $target = (Join-Path $outDir ($assembly.Name + '.dll')).Replace('\', '/')
        $options += '-out:"' + $target + '"'
        $options += Get-ChildItem -LiteralPath $assembly.Source -Recurse -Filter '*.cs' | ForEach-Object {
            '"' + $_.FullName.Replace('\', '/') + '"'
        }
        $response = Join-Path $outDir ($assembly.Name + '.rsp')
        [System.IO.File]::WriteAllLines($response, $options, [System.Text.UTF8Encoding]::new($false))
        $result = & $dotnet $compiler ('@' + $response) 2>&1
        $exitCode = $LASTEXITCODE
        $log = Join-Path $outDir ($assembly.Name + '.log')
        $result | Out-File -LiteralPath $log -Encoding utf8
        $result | Write-Output
        if ($exitCode -ne 0) { throw "Compilation failed: $($assembly.Name). See $log" }
        Write-Output "PASS compile: $($assembly.Name)"
    }
}
finally { Pop-Location }
