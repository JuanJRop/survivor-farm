using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class DungeonExpeditionChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool success, string why) { if (!success) throw new Exception(why); }
        static object Snapshot(GameSaveSystem save)
        {
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", Flags).Invoke(save, null);
            return JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
        }
        static void Restore(GameSaveSystem save, object data) => typeof(GameSaveSystem).GetMethod("RestoreSaveData", Flags).Invoke(save, new[] { data });

        public static IEnumerator Run(PlayerInventory player, Action<string, int, int> capture)
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Dungeon QA requires --qa.");
            var entrance = Object.FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include); entrance.EnsureExpedition();
            var dungeon = entrance.Expedition; Check(dungeon != null, "Explorable dungeon missing in Main.");
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var playerField = typeof(GameSaveSystem).GetField("player", Flags); var originalPlayer = playerField.GetValue(save);
            var locationField = typeof(GameSaveSystem).GetField("restorePlayerLocation", Flags); var originalLocation = locationField.GetValue(save);
            playerField.SetValue(save, player.transform); locationField.SetValue(save, true);
            var originalSave = Snapshot(save);
            var camera = Camera.main; var follow = camera.GetComponent<CameraFollowTarget>(); bool following = follow.enabled;
            float size = camera.orthographicSize; Vector3 cameraPosition = camera.transform.position;
            var pets = Object.FindObjectsByType<PetCompanion>(FindObjectsSortMode.None).Where(p => p.enabled).ToArray();
            foreach (var pet in pets) pet.enabled = false;
            var campaign = player.GetComponent<ValleyCampaign>(); var stats = player.GetComponent<PlayerSurvivalStats>();
            var report = new List<string>();
            try
            {
                dungeon.Restore(null); foreach (var chest in dungeon.Chests) chest.Restore(false);
                entrance.EnterDungeon(); follow.enabled = true; stats.Restore(5, 5, 1);
                yield return null;
                Check(Vector2.Distance(player.transform.position, DungeonLayout.Entry) < .1f, "Entry overlaps town or lost its spawn.");
                Check(!dungeon.GetComponentsInChildren<VillageNpcRoutine>(true).Any(), "Dungeon contains villagers.");
                Check(dungeon.Pool.PoolCount == 12 && dungeon.Chests.Count == 4 && dungeon.Destructibles.Count == 17, "Dungeon content count mismatch.");
                Check(dungeon.Chests.All(c => c.GetComponentsInChildren<SpriteRenderer>().Any(s => s.enabled && s.sprite != null)), "A chest has no visible original art.");
                Check(dungeon.Destructibles.All(d => d.GetComponent<SpriteRenderer>().sprite != null), "A destructible has no visible original art.");
                Check(dungeon.Pool.Enemies.All(e => e.SpriteAnimation != null), "A dungeon guard renders an atlas instead of animated frames.");
                var reachable = Reachable(player.transform);
                foreach (int room in new[] { 0, 1, 2, 3, 4 })
                    Check(reachable.Any(p => DungeonLayout.Rooms[room].Contains(p)), "Physical colliders block room " + room);
                report.Add("PASS: connected physical paths to all five exploration rooms; separate sealed boss arena; no NPCs or village overlap.");
                yield return new WaitForSeconds(.4f); capture("dungeon-entry-1920.png", 1920, 1080);

                // Exercise a real Rigidbody2D passage, rather than validating only map rectangles.
                campaign.Teleport(DungeonLayout.At(0, 3)); var body = player.GetComponent<Rigidbody2D>();
                for (int i = 0; i < 55; i++) { body.MovePosition(body.position + Vector2.up * .18f); yield return new WaitForFixedUpdate(); }
                Check(player.transform.position.y > DungeonLayout.Origin.y + 12, "Player cannot walk through the first corridor.");
                capture("dungeon-corridor-1280.png", 1280, 720);

                var box = dungeon.Destructibles[0]; int wood = player.Wood;
                campaign.Teleport(box.transform.position + Vector3.down * .8f);
                player.GetComponent<PlayerToolbelt>().Select(FarmTool.Sword);
                player.GetComponent<PlayerCombatController>().Attack();
                yield return new WaitForSeconds(.6f);
                Check(box.Health < box.MaximumHealth && box.Health > 0, "Sword does not damage original-art destructibles over multiple hits.");
                box.TakeDamage(10, player); Check(player.Wood == wood && !box.IsAlive && dungeon.GetComponentsInChildren<EnemyLootPickup>().Any(d=>d.Item.Kind==ItemKind.Experience), "Destructible did not scatter its one-time experience pickup.");
                dungeon.Pool.Enemies[0].TakeDamage(100, player);
                var chest0 = dungeon.Chests[0]; chest0.Interact(FarmTool.Sword, player);
                campaign.Teleport(DungeonLayout.At(0, 4));
                var partial = Snapshot(save); Restore(save, partial);
                Check(entrance.IsInsideDungeon && chest0.IsOpened && !box.IsAlive && dungeon.IsDefeated(0) && !dungeon.Pool.Enemies[0].IsAlive, "Dungeon save roundtrip lost chest, breakable or guard state.");
                var legacy = Snapshot(save); var legacyType = legacy.GetType();
                legacyType.GetField("dungeonExpedition").SetValue(legacy, null);
                var legacyPosition = legacyType.GetField("playerPosition").GetValue(legacy);
                legacyPosition.GetType().GetField("x").SetValue(legacyPosition, 0f);
                legacyPosition.GetType().GetField("y").SetValue(legacyPosition, -3f);
                legacyType.GetField("playerPosition").SetValue(legacy, legacyPosition);
                foreach (var c in (IEnumerable)legacyType.GetField("dungeonChests").GetValue(legacy)) c.GetType().GetField("id").SetValue(c, null);
                Restore(save, legacy);
                Check(player.transform.position == DungeonLayout.Entry && chest0.IsOpened, "Legacy dungeon position or indexed chest migration failed.");
                Restore(save, partial);

                for (int room = 1; room <= 4; room++)
                {
                    campaign.Teleport(DungeonLayout.At(DungeonLayout.Rooms[room].center.x, DungeonLayout.Rooms[room].center.y));
                    stats.Restore(5, 5, 1); yield return new WaitForSeconds(.25f);
                    capture("dungeon-room-" + room + "-1920.png", 1920, 1080);
                }
                foreach (var enemy in dungeon.Pool.Enemies) if (enemy.IsAlive) enemy.TakeDamage(100, player);
                Check(dungeon.BossDoorReady, "Clearing the antechamber did not unlock the boss gate.");
                campaign.Teleport(DungeonLayout.At(0, 41)); stats.Restore(5, 5, 1);
                Check(dungeon.BeginBoss(), "Boss room could not start.");
                yield return new WaitForSeconds(1.3f);
                Check(dungeon.Boss.IsTelegraphing && dungeon.Boss.SpriteAnimation.Visual.sprite != null, "Boss has no telegraph or original animation.");
                var bossBounds = dungeon.Boss.SpriteAnimation.Visual.bounds;
                Check(camera.WorldToViewportPoint(bossBounds.min).y > .08f && camera.WorldToViewportPoint(bossBounds.max).y < .9f,
                    "Boss is clipped or obscured by the top HUD during its opening telegraph.");
                capture("dungeon-boss-telegraph-1920.png", 1920, 1080);
                Vector3 lockedImpact = dungeon.Boss.ImpactPosition; int hp = stats.CurrentHealth;
                campaign.Teleport(DungeonLayout.At(7, 48)); yield return new WaitForSeconds(1.5f);
                Check(stats.CurrentHealth == hp && dungeon.Boss.ImpactPosition == lockedImpact, "Boss lunge follows target or cannot be dodged.");
                var duringBoss = Snapshot(save); Restore(save, duringBoss);
                Check(!dungeon.BossFightActive && player.transform.position == DungeonLayout.At(0, 41), "Reload trapped the player behind the sealed boss gate.");
                Check(dungeon.BeginBoss(), "Boss retry failed after loading.");
                dungeon.Boss.TakeDamage(100, player); yield return new WaitForSeconds(.9f);
                Check(dungeon.State.bossDefeated && !dungeon.Boss.IsAlive, "Boss completion or death animation failed.");
                var relic = dungeon.Chests[3]; int coins = player.Coins;
                relic.Interact(FarmTool.Sword, player); Check(player.Coins == coins, "Opening the chest granted invisible rewards.");
                var rewards = dungeon.GetComponentsInChildren<EnemyLootPickup>().Where(d => Vector2.Distance(d.transform.position, relic.transform.position)<.1f).ToArray();
                yield return new WaitForSeconds(.7f);
                foreach (var drop in rewards) drop.TryCollect(player);
                Check(player.Coins == coins + 80, "Boss relic pickup reward missing.");
                var victory = Snapshot(save); Restore(save, victory); relic.Interact(FarmTool.Sword, player);
                Check(player.Coins == coins + 80 && !dungeon.BeginBoss(), "Victory/reload duplicates unique dungeon rewards.");
                campaign.Teleport(DungeonLayout.At(0, 55)); yield return new WaitForSeconds(.2f);
                capture("dungeon-boss-cleared-1280.png", 1280, 720);
                entrance.ExitDungeon(); Check(!entrance.IsInsideDungeon && !dungeon.IsPresent, "Dungeon exit failed.");
                report.Add("PASS: real sword destruction, real corridor walking, room camera captures, pooled deaths, full GameSaveSystem roundtrip, boss telegraph/dodge/retry, unique relic reward and exit.");
                Directory.CreateDirectory("Design/Validation/DungeonExpedition"); File.WriteAllLines("Design/Validation/DungeonExpedition/result.txt", report);
            }
            finally
            {
                Restore(save, originalSave);
                playerField.SetValue(save, originalPlayer); locationField.SetValue(save, originalLocation);
                follow.enabled = following; camera.orthographicSize = size; camera.transform.position = cameraPosition;
                foreach (var pet in pets) if (pet != null) pet.enabled = true;
            }
        }

        private static HashSet<Vector2Int> Reachable(Transform player)
        {
            Physics2D.SyncTransforms();
            var seen = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); var start = new Vector2Int(0, -2); seen.Add(start); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = p + direction;
                    if (seen.Contains(next) || !DungeonLayout.Floor.Contains(next)) continue;
                    var position = DungeonLayout.At(next.x + .5f, next.y + .5f);
                    if (Physics2D.OverlapCircleAll(position, .3f).Any(c => !c.isTrigger && !c.transform.IsChildOf(player) && c.GetComponentInParent<EnemyAIBase>() == null)) continue;
                    seen.Add(next); queue.Enqueue(next);
                }
            }
            return seen;
        }
    }
}
