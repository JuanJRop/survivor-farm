# Enemy camps around Raizclara

## Encounters

Four optional outdoor encounters use the project's existing sprites. Counts and rewards are defined in `EnemyCampDefinition`.

| Camp | Guards | Automatic victory reward | Center in Main QA |
| --- | --- | --- | --- |
| Escondite del sur | 2 sprout slimes, 1 spear goblin | 15 coins, 2 iron, 2 food | (-8, -16) |
| Saqueadores del oeste | 1 slime, 2 spear goblins, 1 archer | 25 coins, 4 iron, 2 food | (-26, -8) |
| Vigias del este | 1 slime, 1 spear goblin, 2 archers | 30 coins, 4 iron, 3 food | (23, -8) |
| Bastion del norte | 1 slime, 2 spear goblins, 2 archers | 45 coins, 6 iron, 3 food | (18, 15) |

Defeating the last living guard now delivers these supplies automatically, plus 2 village influence. There is no need to walk to the chest. Normal enemy drops are separate. Iron enters `AdventureProgress.Data.iron`, the resource used by the existing crafting system. Food still heals; no hunger or farming is introduced.

The northern camp is beyond the existing river crossing. Camps do not replace the village, main quests, wandering enemies or dungeon encounters. New saves choose a clear site near each preferred center without moving existing resources, solid objects or roads. The chosen center is saved.

## Behavior and pooling

- Guards patrol near their posts, engage intruders within 7 world units of the camp center and remain within a 6-unit leash. They cancel attacks and return when the player leaves or enters protected ground.
- Spear goblins use existing melee windups. Archers reuse the dodgeable, collision-tested projectile system. Slimes use a correctly sliced original animation library, including damage and death.
- Each camp owns a prewarmed, bounded guard pool and projectile pool. All camps together add 16 guard instances and 32 reusable arrow instances. Restore reuses the same guards rather than instantiating replacements.
- Camps share the outdoor world's visibility. Disabling a camp or leaving its engagement area cancels its projectiles. Pets do not target disengaged camp guards.
- Movement uses the existing collision sweeps and axis sliding, not global pathfinding. More complex camp layouts should receive dedicated navigation testing.
- Shelters, fences, crates, fire, chests and animations come from `FarmRPGTinyAssetPack`. `EnemyRosterAssetBuilder.Build` regenerates the two new resource libraries without rebuilding Main.

## Persistence

`ValleyData.enemyCamps` stores the camp ID, center, defeated-guard bitmask and supply claim flag (the existing `claimed` field). Missing legacy data starts uncleared camps. Partial clears survive reload; defeated guards do not respawn. Supplies cannot be granted twice. Older cleared but unclaimed chests deliver their pending supplies on the next camp update after save restoration. Delivery waits if the player is dead. Surviving guards return at full health when restored. There is no timed camp respawn in this version.

## Combat missions

Iria's conversation has a Campamentos branch with four independent, optional missions. Each offer has explicit acceptance and decline choices; accepting only tracks the mission, never delivers a reward or consumes resources. Eliminate every guard in the matching camp and report the victory to Iria for a separate bonus.

| Mission | Target | Iria's bonus |
| --- | --- | --- |
| Una salida segura | South, 3 guards | 20 coins, 2 influence |
| Romper el cerco | West, 4 guards | 30 coins, 2 influence |
| Silenciar las flechas | East, 4 guards | 35 coins, 3 influence |
| El ultimo bastion | North, 5 guards | 50 coins, 4 influence, 1 ruby |

Definitions and progress queries live in `CampCombatQuests`. Defeated masks are the source of truth: kills from other camps, roaming enemies and repeated death callbacks cannot inflate progress. Earlier camp victories count even when the mission is accepted later. No camp needs to respawn to make a mission completable.

`ValleyData.acceptedCampQuests`, `claimedCampQuests` and `trackedCampQuest` persist acceptance, unique mission payments and the selected HUD objective. The journal's Combate tab lists all four missions, locations, progress and bonuses. Accepted missions can be tracked/untracked there; the original story HUD returns after reporting the tracked victory. Existing reconstruction quests and Iria's services remain available.

Camps are created when Play starts through `ValleyCampaign` and `EnemyCampWorld`. Stop and restart Play after Unity imports the changes; no scene regeneration is required. Personal saves and the user's open editor were not changed by QA.

## Verification

- `Tools/Verify-TeamChanges.ps1`: runtime, editor and test compilation.
- `Tools/Run-TeamQA.ps1 -UnitTests`: includes camp, victory payout and combat mission regression tests; see the current XML report for totals.
- `Tools/Run-TeamQA.ps1 -Method SurvivorFarm.Editor.RaizclaraTeamVerification.Run`: passed in isolated Unity.
- Main checks cover four valid camp sites, mixed rosters, clear guard posts, reachable chests, partial/full clear serialization, actual inventory rewards and pool identity reuse.
- Existing village layout, exterior shops, villager dialogues, campaign progression and save-roundtrip checks also passed.
- `CampCombatQuestChecks` exercises actual Iria dialogue buttons, partial kills and tracking through full `GameSaveSystem` roundtrips, automatic camp supplies, separate mission payment and duplicate-claim rejection. Dialogue and journal screenshots cover 1280x720 and 1920x1080, including the journal's lower rows.
- Final screenshots inspected at 1920x1080 for all four camps and 1280x720 for a cleared camp. Corrected double-frame slimes and clipped chest slicing found during the first visual pass.

Reports: `Design/Validation/Team/playmode-results.xml`, `Design/Validation/Team/Integrated/EnemyCamps/result.txt` and `Design/Validation/Team/Integrated/CampCombatQuests/result.txt`.
Screenshots: `Design/Validation/Team/Integrated/TeamUi/camp-*.png`.
