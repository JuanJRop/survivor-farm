using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class EnemyCampChecks
    {
        public static IEnumerator Run(PlayerInventory player, Action<string, int, int> capture)
        {
            var campaign = player.GetComponent<ValleyCampaign>();
            var world = player.GetComponent<EnemyCampWorld>();
            world.Initialize();
            if (world.Camps.Count != 4) throw new Exception("Expected four enemy camps; found " + world.Camps.Count);
            string savedData = JsonUtility.ToJson(campaign.Data);
            Vector3 playerPosition = player.transform.position;
            var camera = Camera.main;
            Vector3 cameraPosition = camera.transform.position;
            float size = camera.orthographicSize;
            var stats = player.GetComponent<PlayerSurvivalStats>();
            int maxHealth = stats.MaxHealth, health = stats.CurrentHealth;
            var pets = Object.FindObjectsByType<PetCompanion>(FindObjectsSortMode.None).Where(p => p.enabled).ToArray();
            foreach (var pet in pets) pet.enabled = false;
            var bridge = Object.FindFirstObjectByType<RepairableBridge>();
            bool repaired = bridge.IsRepaired;
            var report = new List<string>();
            try
            {
                stats.Restore(50, 50, 1);
                bridge.Restore(true);
                Physics2D.SyncTransforms();
                var reachable = ReachableFromTown();
                foreach (var camp in world.Camps)
                {
                    if (!EnemyCampWorld.ValidRegion(camp.transform.position, camp.Definition)) throw new Exception("Unsafe camp location: " + camp.Definition.Id);
                    if (camp.Members.Select(e => e.CombatStyle).Distinct().Count() < 2) throw new Exception("Camp has no enemy variety");
                    if (camp.PoolCount != camp.Definition.Roster.Length) throw new Exception("Wrong guard pool size");
                    Vector2 target = camp.Chest.transform.position + Vector3.down * .6f;
                    if (!reachable.Any(p => Vector2.Distance(p, target) < .8f)) throw new Exception("Unreachable supply chest: " + camp.Definition.Id);
                    foreach (var member in camp.Members)
                    {
                        if (!camp.CanOccupy(member.GuardPosition)) throw new Exception("Invalid guard post: " + member.name);
                        if (Physics2D.OverlapCircleAll(member.GuardPosition, .28f).Any(c => !c.isTrigger && c.GetComponentInParent<EnemyAIBase>() == null))
                            throw new Exception("Guard spawns in scenery: " + camp.Definition.Id);
                        var visual = member.GetComponentInChildren<SpriteRenderer>(true);
                        if (visual == null || visual.sprite == null) throw new Exception("Missing camp enemy art");
                    }
                    // Observe patrols just outside aggro range before taking a clean camp view.
                    campaign.Teleport(camp.transform.position + Vector3.down * 8);
                    camera.transform.position = camp.transform.position + new Vector3(0, -.8f, -10);
                    camera.orthographicSize = 6;
                    stats.Restore(maxHealth, maxHealth, 1);
                    yield return new WaitForSeconds(.25f);
                    capture("camp-" + camp.Definition.Id + "-1920.png", 1920, 1080);
                    report.Add(camp.Definition.Id + ": " + camp.transform.position + ", " + camp.PoolCount + " guards, reachable supplies, no town/river/road obstruction.");
                }
                var west = world.Camps.First(c => c.Definition.Id == "west");
                var instances = west.Members.ToArray();
                int coins = player.Coins, food = player.Food, iron = player.GetComponent<AdventureProgress>().Data.iron;
                foreach (var guard in instances) if (guard.IsAlive) guard.TakeDamage(100, player);
                yield return new WaitForSeconds(.9f);
                if (!west.IsCleared) throw new Exception("Camp cannot be cleared");
                campaign.Teleport(west.Chest.transform.position + Vector3.down * .7f);
                camera.transform.position = west.transform.position + new Vector3(0, -.8f, -10);
                if (!west.Claimed || west.TryClaim(player)) throw new Exception("Automatic camp reward is unavailable or duplicated");
                if (player.Coins < coins + west.Definition.Coins || player.Food != food + west.Definition.Food ||
                    player.GetComponent<AdventureProgress>().Data.iron != iron + west.Definition.Iron) throw new Exception("Camp reward did not reach the actual inventory");
                campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
                if (!west.IsCleared || !west.Claimed || west.Members.Any(e => e.IsAlive)) throw new Exception("Camp clear did not survive a save roundtrip");
                if (!instances.SequenceEqual(west.Members)) throw new Exception("Camp pool replaced its guard instances");
                capture("camp-cleared-1280.png", 1280, 720);
                report.Add("PASS: partial/full clear persistence, automatic unique victory reward in real inventory, pooled guard identity, four original-art camps.");
                Directory.CreateDirectory("Design/Validation/EnemyCamps");
                File.WriteAllLines("Design/Validation/EnemyCamps/result.txt", report);
            }
            finally
            {
                bridge.Restore(repaired);
                campaign.Restore(JsonUtility.FromJson<ValleyData>(savedData));
                campaign.Teleport(playerPosition);
                stats.Restore(maxHealth, health, 1);
                camera.transform.position = cameraPosition; camera.orthographicSize = size;
                foreach (var pet in pets) if (pet != null) pet.enabled = true;
            }
        }

        private static HashSet<Vector2> ReachableFromTown()
        {
            var result = new HashSet<Vector2>();
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(0, -2));
            Vector2Int[] steps = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (!visited.Add(cell) || Mathf.Abs(cell.x) > 70 || Mathf.Abs(cell.y) > 40) continue;
                Vector2 p = (Vector2)cell * .5f;
                if (Physics2D.OverlapCircleAll(p, .3f).Any(c => !c.isTrigger && c.GetComponentInParent<EnemyAIBase>() == null && c.GetComponentInParent<PlayerInventory>() == null)) continue;
                result.Add(p);
                foreach (var step in steps) queue.Enqueue(cell + step);
            }
            return result;
        }
    }
}
