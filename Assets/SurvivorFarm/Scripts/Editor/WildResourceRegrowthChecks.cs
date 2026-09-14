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
    public static class WildResourceRegrowthChecks
    {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Roundtrip(PlayerInventory player)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var field = typeof(GameSaveSystem).GetField("player", flags); var original = field.GetValue(save);
            field.SetValue(save, player.transform);
            try
            {
                var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
                data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
                typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            }
            finally { field.SetValue(save, original); }
        }

        public static IEnumerator Run(PlayerInventory player, Action<string, int, int> capture)
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Regrowth checks require --qa.");
            var world = player.GetComponent<WildResourceRegrowth>(); world.Initialize();
            Check(world.PoolCount > 10, "Main resource pool was not populated.");
            int poolCount = world.PoolCount;
            var campaign = player.GetComponent<ValleyCampaign>();
            var camera = Camera.main;
            Vector3 playerPosition = player.transform.position, cameraPosition = camera.transform.position;
            float size = camera.orthographicSize; bool enabled = world.enabled;
            world.enabled = false;
            var report = new List<string>();
            try
            {
                foreach (bool tree in new[] { true, false })
                {
                    var point = world.Points.Where(p => tree ? p.Instance is TreeResource : p.Instance is RockResource)
                        .OrderBy(p => Vector2.Distance(p.transform.position, tree ? new Vector2(-11, 2.6f) : new Vector2(9.7f, 3))).First();
                    var resource = point.Instance; var definition = resource.Definition;
                    var saved = point.CaptureRegrowth(); bool harvested = resource.IsHarvested; int health = resource.CurrentHealth;
                    string id = point.PersistentId; string name = tree ? "trees" : "rocks";
                    try
                    {
                        point.Respawn(); point.EnableRegrowth(8, 8);
                        Vector3 old = resource.transform.position;
                        campaign.Teleport(old + Vector3.down * .75f);
                        camera.transform.position = point.transform.position + new Vector3(0, 0, -10); camera.orthographicSize = 8;
                        capture("regrowth-" + name + "-before-1920.png", 1920, 1080);
                        resource.Interact(tree ? FarmTool.Axe : FarmTool.Pickaxe, player);
                        yield return new WaitForSeconds(3.7f);
                        Check(resource.IsHarvested, "Actual timed gathering did not deplete " + name);
                        campaign.Teleport(campaign.Home);
                        camera.transform.position = point.transform.position + new Vector3(0, 0, -10);
                        world.Tick(3);
                        float pending = point.RegrowthRemaining;
                        Roundtrip(player);
                        Check(resource.IsHarvested && Mathf.Abs(point.RegrowthRemaining - pending) < .01f, "Save/load lost regrowth timer.");
                        capture("regrowth-" + name + "-cleared-1920.png", 1920, 1080);
                        for (int attempt = 0; attempt < 12 && resource.IsHarvested; attempt++) world.Tick(5.1f);
                        Check(!resource.IsHarvested, "Main has no valid random regrowth site for " + name);
                        Vector3 regrown = resource.transform.position;
                        Check(Vector2.Distance(old, regrown) >= 1 && Vector2.Distance(player.transform.position, regrown) >= 4, "Regrowth did not relocate safely.");
                        Check(player.GetComponent<EnemyCampWorld>().ContainsCamp(regrown, 2) == false, "Resource grew inside a camp.");
                        Check(ReferenceEquals(resource, point.Instance) && ReferenceEquals(definition, resource.Definition) && point.PersistentId == id,
                            "Regrowth replaced its pool instance, flyweight or stable ID.");
                        Roundtrip(player);
                        Check(Vector2.Distance(regrown, resource.transform.position) < .01f && !resource.IsHarvested, "Save/load lost live regrown position.");
                        camera.transform.position = point.transform.position + new Vector3(0, 0, -10);
                        capture("regrowth-" + name + "-after-1920.png", 1920, 1080);
                        report.Add(name + ": " + old + " -> " + regrown + ", timed gathering, delayed random regrowth, full save roundtrip, same shared instance.");
                    }
                    finally
                    {
                        point.EnableRegrowth(tree ? 45 : 60, tree ? 90 : 110);
                        point.Restore(harvested, health); point.RestoreRegrowth(saved);
                    }
                }
                Check(world.PoolCount == poolCount, "Resource pool grew during regeneration.");
                report.Add("PASS: " + poolCount + " reusable tree/rock instances; no per-regrowth Instantiate or unique flyweight data.");
                Directory.CreateDirectory("Design/Validation/WildResourceRegrowth");
                File.WriteAllLines("Design/Validation/WildResourceRegrowth/result.txt", report);
            }
            finally
            {
                world.enabled = enabled;
                campaign.Teleport(playerPosition); camera.transform.position = cameraPosition; camera.orthographicSize = size;
            }
        }
    }
}
