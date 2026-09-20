$ErrorActionPreference = 'Stop'
$persistenceRoot = Split-Path -Parent $PSScriptRoot
$persistenceSources = @(
    (Join-Path $persistenceRoot 'Assets/SurvivorFarm/Scripts/Runtime/Core/SaveFileStore.cs'),
    (Join-Path $persistenceRoot 'Assets/SurvivorFarm/Scripts/Runtime/Core/SaveJsonValidation.cs'),
    (Join-Path $PSScriptRoot 'PersistenceFileChecks.cs')
)
Add-Type -Path $persistenceSources
$persistenceScratch = Join-Path ([IO.Path]::GetTempPath()) ('SurvivorFarm-Persistence-' + [Guid]::NewGuid().ToString('N'))
$persistenceResults = [PersistenceFileChecks]::Run($persistenceScratch)
$persistenceResults | ForEach-Object { 'PASS: ' + $_ }
'Isolated files retained at: ' + $persistenceScratch

$persistenceSyntaxFiles = @(
    'Assets/SurvivorFarm/Scripts/Runtime/Core/GameSaveSystem.cs',
    'Assets/SurvivorFarm/Scripts/Runtime/Core/SaveFileStore.cs',
    'Assets/SurvivorFarm/Scripts/Runtime/Core/SaveJsonValidation.cs',
    'Assets/SurvivorFarm/Scripts/Runtime/Core/StableSaveId.cs',
    'Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ResourceSpawnPoint.cs',
    'Assets/SurvivorFarm/Scripts/Editor/StableSaveIdAssignment.cs',
    'Assets/SurvivorFarm/Tests/PlayMode/PersistenceSaveTests.cs'
)
$persistenceParseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp9).WithPreprocessorSymbols([string[]]@('UNITY_EDITOR'))
foreach ($persistenceRelativePath in $persistenceSyntaxFiles) {
    $persistenceSourcePath = Join-Path $persistenceRoot $persistenceRelativePath
    $persistenceTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($persistenceSourcePath), $persistenceParseOptions, $persistenceSourcePath, [Text.Encoding]::UTF8, [Threading.CancellationToken]::None)
    $persistenceDiagnostics = @($persistenceTree.GetDiagnostics([Threading.CancellationToken]::None) | Where-Object { $_.Severity -eq 'Error' })
    if ($persistenceDiagnostics.Count -gt 0) { throw ($persistenceDiagnostics -join [Environment]::NewLine) }
}
'PASS: C# 9 syntax in seven owned Unity files (not an integrated compilation)'
