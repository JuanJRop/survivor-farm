$ErrorActionPreference = 'Stop'
$economyRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$economyRuntime = Join-Path $economyRoot 'Assets/SurvivorFarm/Scripts/Runtime'
$economyPureFiles = @(
    (Join-Path $economyRuntime 'Gameplay/SeedRarity.cs'),
    (Join-Path $economyRuntime 'Player/SurvivalItemCatalog.cs'),
    (Join-Path $economyRuntime 'Player/EconomyRecipes.cs'),
    (Join-Path $economyRuntime 'Player/EconomyTradeRules.cs')
)
Add-Type -Path $economyPureFiles
$script:economyChecks = 0
function Assert-Economy([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
    $script:economyChecks++
}

$economyItems = [SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::All
$economySeeds = @([SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::Seeds)
Assert-Economy ($economyItems.Count -eq 91) 'The expanded catalog must keep its 91 entries.'
Assert-Economy ($economySeeds.Count -eq 37) 'All 37 seeds and saplings must remain.'
Assert-Economy (@($economyItems | Where-Object IsFood).Count -eq 37) 'All 37 foods must remain.'
Assert-Economy (@($economyItems | Group-Object Id | Where-Object Count -gt 1).Count -eq 0) 'Item IDs must be unique.'
Assert-Economy (@($economyItems | Where-Object { $_.BuyPrice -le $_.SellPrice -or $_.SellPrice -le 0 }).Count -eq 0) 'Buy/sell spreads must be positive.'
Assert-Economy (@($economySeeds | Where-Object { $_.GrowthSeconds -le 0 -or $_.HarvestYield -lt 1 -or -not $_.CropRole }).Count -eq 0) 'Every crop needs a duration, yield and role.'

$economyStarter = @([SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::AvailableSeeds(0))
Assert-Economy (($economyStarter.CropItemId | Sort-Object) -join ',' -eq 'Carrot,Potato,Strawberry,Wheat') 'Starter crops must remain a small, deterministic set.'
Assert-Economy (@($economyStarter.GrowthSeconds | Select-Object -Unique).Count -eq 4) 'Starter crops must have distinct durations.'
Assert-Economy (@([SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::AvailableSeeds(1)).Count -gt 4) 'The first meal must expand the crop pool.'
Assert-Economy (@([SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::AvailableSeeds(6)).Count -eq 37) 'The full crop catalog must remain reachable.'
foreach ($economySeed in $economySeeds) {
    $economyCrop = [SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::Find($economySeed.CropItemId)
    Assert-Economy ($null -ne $economyCrop -and $economyCrop.IsFood) ('Missing crop: ' + $economySeed.Id)
    Assert-Economy ($economyCrop.GrowthSeconds -eq $economySeed.GrowthSeconds -and $economyCrop.HarvestYield -eq $economySeed.HarvestYield) ('Mismatched crop profile: ' + $economySeed.Id)
}

$economyRecipes = [SurvivorFarm.Runtime.Player.EconomyCookingRecipes]::All
Assert-Economy ($economyRecipes.Count -eq 4) 'Expected legacy cooking plus three concrete recipes.'
Assert-Economy (@($economyRecipes | Where-Object RequiresWorkshop).Count -eq 1) 'Only the optional batch recipe should require the workshop.'
foreach ($economyRecipe in $economyRecipes) {
    $economyBasketPrice = 0
    foreach ($economyCost in $economyRecipe.Ingredients) {
        Assert-Economy ($economyCost.Amount -gt 0) ('Invalid ingredient cost: ' + $economyRecipe.Id)
        $economyItem = [SurvivorFarm.Runtime.Player.SurvivalItemCatalog]::Find($economyCost.ItemId)
        if ($economyItem) { $economyUnitPrice = $economyItem.BuyPrice }
        elseif ($economyCost.ItemId -eq 'Fruit') { $economyUnitPrice = 8 }
        elseif ($economyCost.ItemId -eq 'Wood') { $economyUnitPrice = 4 }
        else { throw ('Unknown ingredient: ' + $economyCost.ItemId) }
        $economyBasketPrice += $economyUnitPrice * $economyCost.Amount
    }
    Assert-Economy ($economyBasketPrice -gt 10 * $economyRecipe.OutputAmount) ('Buying ingredients then cooking for resale creates free coins: ' + $economyRecipe.Id)
}

$economyTotal = 0
Assert-Economy (-not [SurvivorFarm.Runtime.Player.EconomyTradeRules]::TryGetTotalPrice(8, [int]::MaxValue, [ref]$economyTotal)) 'Overflowing purchases must fail.'
Assert-Economy (-not [SurvivorFarm.Runtime.Player.EconomyTradeRules]::TryGetTotalPrice(8, -1, [ref]$economyTotal)) 'Negative quantities must fail.'
Assert-Economy (-not [SurvivorFarm.Runtime.Player.EconomyTradeRules]::TryGetTotalPrice(0, 1, [ref]$economyTotal)) 'Zero-price transactions must fail.'
Assert-Economy ([SurvivorFarm.Runtime.Player.EconomyTradeRules]::TryGetTotalPrice(8, 3, [ref]$economyTotal) -and $economyTotal -eq 24) 'Normal totals must stay exact.'

$economyOwnedFiles = @('Gameplay/FarmingPlot.cs', 'Gameplay/CultivationDefinition.cs', 'Player/PlayerInventory.cs', 'Player/PlayerCraftingController.cs', 'Player/BackpackActions.cs', 'UI/SimpleShopSystem.cs')
$economyParseFiles = @($economyPureFiles) + @($economyOwnedFiles | ForEach-Object { Join-Path $economyRuntime $_ }) + @(Get-ChildItem -LiteralPath (Join-Path $economyRuntime 'Player') -Filter 'Economy*.cs' | Select-Object -ExpandProperty FullName) + @(Get-ChildItem -LiteralPath (Join-Path $economyRoot 'Assets/SurvivorFarm/Tests/PlayMode') -Filter 'Economy*.cs' | Select-Object -ExpandProperty FullName)
foreach ($economyFile in ($economyParseFiles | Select-Object -Unique)) {
    $economyTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([System.IO.File]::ReadAllText($economyFile))
    $economyErrors = @($economyTree.GetDiagnostics() | Where-Object Severity -eq Error)
    Assert-Economy ($economyErrors.Count -eq 0) ($economyFile + ': ' + ($economyErrors -join '; '))
}
Write-Output ('PASS: ' + $script:economyChecks + ' isolated catalog/recipe/price and C# syntax checks. Unity was not started; PlayMode behavior was not executed.')
