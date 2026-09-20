param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$runtimeDirectory = Split-Path -Parent (Get-Command pwsh).Source
Add-Type -Path (Join-Path $runtimeDirectory 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $runtimeDirectory 'Microsoft.CodeAnalysis.CSharp.dll')
$gameplay = Join-Path $ProjectRoot 'Assets/SurvivorFarm/Scripts/Runtime/Gameplay'
$sourceFiles = @(
    'ValleyCampaign.cs', 'ValleyWorld.cs', 'VillageServices.cs',
    'VillageResidents.cs', 'VillageNpcRoutine.cs', 'VillageNpcArtCatalog.cs'
) | ForEach-Object { Join-Path $gameplay $_ }
$sourceFiles += Join-Path $ProjectRoot 'Assets/SurvivorFarm/Tests/PlayMode/VillageServicesTests.cs'

foreach ($sourceFile in $sourceFiles) {
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($sourceFile))
    $errors = @($tree.GetDiagnostics() | Where-Object Severity -eq 'Error')
    if ($errors.Count -gt 0) { throw ($sourceFile + ': ' + ($errors -join "`n")) }
}
Write-Output ('PASS: C# syntax in ' + $sourceFiles.Count + ' owned source files. Not a Unity compilation.')

# Run the actual data class and pure service rules in memory, without Unity stubs.
$campaignTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $gameplay 'ValleyCampaign.cs')))
$dataNode = $campaignTree.GetRoot().DescendantNodes() | Where-Object {
    $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax] -and $_.Identifier.ValueText -eq 'ValleyData'
} | Select-Object -First 1
if ($null -eq $dataNode) { throw 'ValleyData declaration missing.' }
$serviceSource = [IO.File]::ReadAllText((Join-Path $gameplay 'VillageServices.cs'))
$domainSource = "using System.Collections.Generic;`n" + $serviceSource + "`nnamespace SurvivorFarm.Runtime.Gameplay {`n" + $dataNode.ToFullString() + "`n}"
$residentTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $gameplay 'VillageResidents.cs')))
$residentClass = $residentTree.GetRoot().DescendantNodes() | Where-Object {
    $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax] -and $_.Identifier.ValueText -eq 'VillageResidents'
} | Select-Object -First 1
# Only Offset requires Vector3; execute the remaining actual narrative methods without Unity.
$residentMethods = $residentClass.Members | Where-Object { $_.Identifier.ValueText -ne 'Offset' } | ForEach-Object { $_.ToFullString() }
$domainSource += "`nnamespace SurvivorFarm.Runtime.Gameplay { public static class VillageResidents {`n" + ($residentMethods -join "`n") + "`n} }"
Add-Type -TypeDefinition $domainSource

$script:checks = 0
function Assert-VillageRule([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}

$data = [SurvivorFarm.Runtime.Gameplay.ValleyData]::new()
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 0) 'New arrival rank incorrect.'
$data.influence = 99
$data.boss = $data.guardian = $data.portal = $data.restored = $true
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 1) 'Campaign alone grants a high village rank.'
$data.daliaGardenRestored = $true
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 2) 'A first project does not unlock trusted neighbor.'
$data.influence = 7
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 1) 'Trusted neighbor ignores points.'
$data.influence = 16
$data.nicoWorkshopRepaired = $true
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 2) 'Protector ignores pantry and third project.'
$data.maraPantryStocked = $true
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 3) 'Protector stays locked after its milestones.'
$data.roloMarketOpened = $data.guardPostBuilt = $true
$data.influence = 27
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 3) 'Leader ignores point requirement.'
$data.influence = 28
$data.rankRewardMask = 30
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -eq 4) 'Leader stays locked with all milestones.'
foreach ($field in @('maraPantryStocked', 'nicoWorkshopRepaired', 'daliaGardenRestored', 'roloMarketOpened', 'guardPostBuilt')) {
    $data.$field = $false
    Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageServices]::RankFor($data) -lt 4) ('Leader ignores project ' + $field)
    $data.$field = $true
}
Assert-VillageRule ($data.rankRewardMask -eq 30) 'Queries changed historical reward mask.'

$data = [SurvivorFarm.Runtime.Gameplay.ValleyData]::new()
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 1, 40, 60)
Assert-VillageRule (-not $status.CanUse) 'Closed workshop offers service.'
$data.nicoWorkshopRepaired = $true
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 1, 4, 6)
Assert-VillageRule $status.CanUse 'First order unavailable with sufficient materials.'
Assert-VillageRule ($status.WoodCost -eq 4 -and $status.StoneCost -eq 6 -and $status.IronReward -eq 2) 'Basic order balance changed.'
Assert-VillageRule ($data.nicoSupplyDay -eq 0) 'Status query consumes an order.'
foreach ($materials in @(@(3, 6), @(4, 5))) {
    $status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 1, $materials[0], $materials[1])
    Assert-VillageRule (-not $status.CanUse -and $status.UnavailableReason.Length -gt 0) 'Missing cost is not explained.'
}
$data.nicoSupplyDay = 1
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 1, 4, 6)
Assert-VillageRule ($status.UsedToday -and -not $status.CanUse) 'Same-day order can repeat.'
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 2, 4, 6)
Assert-VillageRule $status.CanUse 'Order does not renew the next day.'
$data.nicoSupplyDay = 5
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 2, 4, 6)
Assert-VillageRule (-not $status.CanUse) 'A backwards clock refreshes an order.'
$data.nicoSupplyDay = 0
$data.maraPantryStocked = $data.daliaGardenRestored = $true
$data.influence = 16
$status = [SurvivorFarm.Runtime.Gameplay.VillageServices]::WorkshopStatus($data, 1, 4, 6)
Assert-VillageRule ($status.IronReward -eq 3 -and $status.CanUse) 'Protector order bonus incorrect.'

$well = $campaignTree.GetRoot().DescendantNodes() | Where-Object {
    $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax] -and $_.Condition.ToString().Contains('"village:well"')
} | Select-Object -First 1
Assert-VillageRule ($null -ne $well -and -not $well.Statement.ToFullString().Contains('RestoreHunger')) 'Well still refills hunger.'
$legacy = [SurvivorFarm.Runtime.Gameplay.ValleyData]::new()
$firstDialogue = [SurvivorFarm.Runtime.Gameplay.VillageResidents]::Dialogue($legacy, 'village:elder')
$legacy.maraPantryStocked = $true
$restoredDialogue = [SurvivorFarm.Runtime.Gameplay.VillageResidents]::Dialogue($legacy, 'village:elder')
Assert-VillageRule ($restoredDialogue -ne $firstDialogue) 'Mara ignores a restored pantry in legacy data without the note.'
Assert-VillageRule (-not $legacy.note) 'A dialogue query changes story progress.'
$legacy.restored = $true
Assert-VillageRule ([SurvivorFarm.Runtime.Gameplay.VillageResidents]::Dialogue($legacy, 'village:elder') -ne $restoredDialogue) 'Mara ignores the restored valley in legacy data.'
$interactionClass = $campaignTree.GetRoot().DescendantNodes() | Where-Object {
    $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax] -and $_.Identifier.ValueText -eq 'ValleyInteraction'
} | Select-Object -First 1
$gatherUpdate = $interactionClass.Members | Where-Object { $_.Identifier.ValueText -eq 'Update' } | Select-Object -First 1
Assert-VillageRule (-not $gatherUpdate.ToFullString().Contains('.Pulse(')) 'Campaign gathering still emits per-hit Pulse text.'
$gatherCancel = $interactionClass.Members | Where-Object { $_.Identifier.ValueText -eq 'CancelGathering' } | Select-Object -First 1
Assert-VillageRule ($null -ne $gatherCancel -and -not $gatherCancel.ToFullString().Contains('Campaign.Use(')) 'Cancellation can grant a resource.'
Write-Output ('PASS: ' + $script:checks + ' isolated rule/static assertions. Inventory transactions, JsonUtility and rendering require Unity QA.')
