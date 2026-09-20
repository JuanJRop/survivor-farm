param([string]$ProjectPath = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')

$runtimePath = Join-Path $ProjectPath 'Assets/SurvivorFarm/Scripts/Runtime'
$ownedFiles = @(
    'Player/PlayerCombatController.cs', 'Player/EquipmentItems.cs',
    'Gameplay/ValleyEnemy.cs', 'Gameplay/HarvestableResource.cs',
    'Gameplay/ArrowProjectile.cs', 'Gameplay/TreeResource.cs', 'Gameplay/RockResource.cs'
) | ForEach-Object { Join-Path $runtimePath $_ }
$ownedFiles += Get-ChildItem -LiteralPath (Join-Path $runtimePath 'Gameplay') -Filter 'CombatFeel*.cs' | Select-Object -ExpandProperty FullName
$ownedFiles += Get-ChildItem -LiteralPath (Join-Path $ProjectPath 'Assets/SurvivorFarm/Tests/PlayMode') -Filter 'CombatFeel*.cs' | Select-Object -ExpandProperty FullName
foreach ($sourcePath in $ownedFiles) {
    $sourceText = Get-Content -LiteralPath $sourcePath -Raw
    $syntaxTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($sourceText)
    $syntaxErrors = @($syntaxTree.GetDiagnostics() | Where-Object Severity -eq Error)
    if ($syntaxErrors.Count) { throw "$sourcePath`n$($syntaxErrors -join [Environment]::NewLine)" }
}
Write-Output "PASS: C# syntax parsed for $($ownedFiles.Count) area-D source/test files. No Unity compilation performed."

$assetPath = Join-Path $ProjectPath 'Assets/SurvivorFarm/Resources/CombatFeelVisuals.asset'
$assetText = Get-Content -LiteralPath $assetPath -Raw
$arrowMetaPath = Join-Path $ProjectPath 'Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Others/Arrow/Arrow.png.meta'
$arrowMeta = Get-Content -LiteralPath $arrowMetaPath -Raw
$spriteId = '545799971500593739'
$textureGuid = '34c7b3d6ae5ddcf4c95d3167735dd5f0'
if (!$assetText.Contains("fileID: $spriteId, guid: $textureGuid") -or
    !$arrowMeta.Contains("guid: $textureGuid") -or
    !$arrowMeta.Contains("213: $spriteId") -or
    $arrowMeta -notmatch '(?s)name: Arrow_17\r?\n.*?internalID: 545799971500593739') {
    throw 'Existing Arrow_17 sprite reference does not match its importer metadata.'
}
$scriptMeta = Get-Content -LiteralPath (Join-Path $runtimePath 'Gameplay/CombatFeelVisuals.cs.meta') -Raw
if ($scriptMeta -notmatch 'guid: (\w+)') { throw 'Missing visual-script GUID.' }
if (!$assetText.Contains("guid: $($Matches[1])")) { throw 'Visual asset does not reference CombatFeelVisuals.cs.' }
foreach ($sourcePath in $ownedFiles) {
    if (!(Test-Path -LiteralPath "$sourcePath.meta")) { throw "Missing meta: $sourcePath" }
}
$combatSource = Get-Content -LiteralPath (Join-Path $runtimePath 'Player/PlayerCombatController.cs') -Raw
if ($combatSource -match 'new Texture2D|Sprite\.Create|CreateArrowSprite') {
    throw 'Per-shot generated arrow graphics still present.'
}
Write-Output 'PASS: existing Arrow_17 asset linkage, script GUID, source metadata and no per-shot sprite generation.'

# Execute the real equipment rules with a minimal inventory adapter; no Unity engine is loaded.
$equipmentSource = Get-Content -LiteralPath (Join-Path $runtimePath 'Player/EquipmentItems.cs') -Raw
$toolSource = Get-Content -LiteralPath (Join-Path $runtimePath 'Gameplay/FarmTool.cs') -Raw
$inventoryAdapter = @'
namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerInventory
    {
        public string[] EquippedEquipment;
        public int EquipmentDamage
        {
            get
            {
                int sum = 0;
                foreach (string id in EquippedEquipment ?? System.Array.Empty<string>())
                {
                    var item = EquipmentItems.Find(id);
                    if (item != null) sum += item.DamageBonus;
                }
                return sum;
            }
        }
    }
}
'@
Add-Type -TypeDefinition ($equipmentSource + [Environment]::NewLine + $toolSource + [Environment]::NewLine + $inventoryAdapter)
$sword = [SurvivorFarm.Runtime.Gameplay.FarmTool]::Sword
$bow = [SurvivorFarm.Runtime.Gameplay.FarmTool]::Bow
$cases = @(
    @{ Items = @('IronSword', 'Gem', 'DiamondAmulet', 'FireElement'); Sword = 5; Bow = 3 },
    @{ Items = @('RubySword', 'Gem', 'DiamondAmulet', 'FireElement'); Sword = 7; Bow = 3 },
    @{ Items = @('HunterBow', 'Gem', 'DiamondAmulet', 'FireElement'); Sword = 3; Bow = 5 },
    @{ Items = @('DiamondBow', 'Gem', 'DiamondAmulet', 'FireElement'); Sword = 3; Bow = 7 },
    @{ Items = @('Sword', 'Bow'); Sword = 0; Bow = 0 },
    @{ Items = @($null, 'missing', 'Gem'); Sword = 1; Bow = 1 },
    @{ Items = @('RubySword', 'DiamondBow', 'Gem'); Sword = 5; Bow = 5 },
    @{ Items = $null; Sword = 0; Bow = 0 }
)
foreach ($case in $cases) {
    $inventoryAdapterInstance = [SurvivorFarm.Runtime.Player.PlayerInventory]::new()
    $inventoryAdapterInstance.EquippedEquipment = $case.Items
    $actualSword = [SurvivorFarm.Runtime.Player.EquipmentItems]::DamageBonusFor($inventoryAdapterInstance, $sword)
    $actualBow = [SurvivorFarm.Runtime.Player.EquipmentItems]::DamageBonusFor($inventoryAdapterInstance, $bow)
    if ($actualSword -ne $case.Sword -or $actualBow -ne $case.Bow) { throw "Equipment mismatch: $($case.Items -join ', ')" }
}
if ([SurvivorFarm.Runtime.Player.EquipmentItems]::DamageBonusFor($null, $sword) -ne 0) { throw 'Null inventory damage must be zero.' }
Write-Output "PASS: $($cases.Count) equipment combinations for both weapons plus null inventory, using production EquipmentItems.cs."
