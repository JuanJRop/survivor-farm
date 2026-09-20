param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)
$ErrorActionPreference = 'Stop'
$uiRoot = Join-Path $ProjectRoot 'Assets/SurvivorFarm/Scripts/Runtime/UI'
$files = @('AdventureWindow.cs', 'CraftingWindow.cs', 'FarmUiStyle.cs', 'HudActionTooltip.cs',
    'MaterialCostBadge.cs', 'OriginalSpriteHud.cs', 'PlayerEquipmentWindow.cs',
    'TutorialQuestSystem.cs', 'UiEquipmentComparison.cs', 'VisualBackpack.cs')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$options = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion(
    [Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp9)
$paths = @($files | ForEach-Object { Join-Path $uiRoot $_ })
$paths += Join-Path $ProjectRoot 'Assets/SurvivorFarm/Tests/PlayMode/UiConsistencyTests.cs'
foreach ($path in $paths) {
    $source = Get-Content -LiteralPath $path -Raw
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source, $options, $path,
        [System.Text.Encoding]::UTF8, [System.Threading.CancellationToken]::None)
    $errors = @($tree.GetDiagnostics([System.Threading.CancellationToken]::None) |
        Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })
    if ($errors.Count) { throw ($errors -join [Environment]::NewLine) }
}
Write-Output "PASS: C# syntax for $($paths.Count) UI/test files (Roslyn, no Unity)."
$iconRoot = Join-Path $ProjectRoot 'Assets/SurvivorFarm/Resources/BackpackIcons'
$icons = @('Panel', 'Slot', 'Backpack', 'Helmet', 'Quest', 'Wood', 'Stone', 'Iron', 'Fruit', 'Food', 'Coin', 'Sword', 'Bow', 'Axe', 'Pickaxe', 'Hoe')
foreach ($icon in $icons) {
    if (-not (Test-Path -LiteralPath (Join-Path $iconRoot "$icon.png"))) { throw "Missing existing sprite: $icon" }
}
Write-Output "PASS: $($icons.Count) existing UI sprites are present."
$spriteMeta = Get-Content -LiteralPath (Join-Path $iconRoot 'Panel.png.meta') -Raw
if ($spriteMeta -notmatch 'spriteBorder: \{x: 5, y: 5, z: 5, w: 5\}') { throw 'Panel sprite has no expected sliced border.' }
$crafting = Get-Content -LiteralPath (Join-Path $uiRoot 'CraftingWindow.cs') -Raw
foreach ($api in @('GetRecipeDescriptors', 'TryGetRecipeDescriptor', 'descriptor.IsAvailable', 'descriptor.CanCraft', 'descriptor.UnavailableReason')) {
    if (-not $crafting.Contains($api)) { throw "Crafting UI is not using $api" }
}
if ($crafting -match 'GetCosts\(|ConstructionSystem\.Cost\(|weaponLevel\s*\*|fruit\s*=\s*2|WorkshopRestored') { throw 'Crafting UI duplicates a recipe or workshop rule.' }
$journal = Get-Content -LiteralPath (Join-Path $uiRoot 'AdventureWindow.cs') -Raw
foreach ($api in @('valley.VillageBoard', 'valley.NextRankRequirement', 'valley.VillageServicesSummary', 'SelectJournalTab')) {
    if (-not $journal.Contains($api)) { throw "Journal is not using $api" }
}
$hud = Get-Content -LiteralPath (Join-Path $uiRoot 'OriginalSpriteHud.cs') -Raw
if ($hud -notmatch 'FindRect\("Context"\)[\s\S]*?context\.gameObject\.SetActive\(false\)') { throw 'HUD Context regression.' }
Write-Output 'PASS: recipe/campaign contracts and inactive Context source checks.'
Write-Output 'NOT RUN: Unity compilation, PlayMode tests, rendering and input QA.'
