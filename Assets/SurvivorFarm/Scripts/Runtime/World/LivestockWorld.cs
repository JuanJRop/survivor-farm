using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Original-pack cows and rideable horses in the meadow south of the village.</summary>
    public static class LivestockWorld
    {
        public static void Configure(ValleyWorld world, PlayerInventory player)
        {
            if (world == null || player == null) return;
            if (player.GetComponent<PlayerMountController>() == null) player.gameObject.AddComponent<PlayerMountController>();
            if (world.transform.Find("Ganado y caballos") != null) return;
            var root = new GameObject("Ganado y caballos").transform; root.SetParent(world.transform, false);
            Physics2D.SyncTransforms();
            var locations = new[] { new Vector2(4,-12), new Vector2(7,-13), new Vector2(10,-12), new Vector2(12,-15) };
            for (int i = 0; i < locations.Length; i++)
            {
                if (!FindMeadowPosition(locations[i], out var position)) continue;
                var go = new GameObject("Vaca " + (i + 1)); go.transform.SetParent(root, false); go.transform.position = position;
                var visual = Visual(go.transform, "PackCowBrown", 32, 32, .82f);
                var body = go.AddComponent<CircleCollider2D>();
                var cow = go.AddComponent<AnimalResource>(); cow.Configure(visual, null, 3, 0); cow.ConfigureHealth(6); cow.ConfigureCow();
                body.isTrigger = false; body.radius = .38f; body.offset = Vector2.up * .35f;
                var roam = go.AddComponent<AnimalRoamingVisual>(); roam.Visual = visual;
                roam.Idle = Rows("PackCowBrown", 32, 32, new[] {0,1,2}, 4); roam.IdleFrames = 4;
                roam.Walk = Rows("PackCowBrown", 32, 32, new[] {3,4,5}, 4); roam.WalkFrames = 4;
                roam.CanEat = true; roam.EatChance = .24f; roam.EatDuration = 1.6f;
                roam.Radius = 1.3f; roam.Speed = .55f; roam.FootRadius = .4f;
                ResourceSpawnPoint.Attach(cow).Configure(null, cow, "livestock:cow:" + i);
                Physics2D.SyncTransforms();
            }
            for (int i = 0; i < 2; i++)
            {
                if (!FindMeadowPosition(new Vector2(-4-i*3,-11-i*2), out var position)) continue;
                var go = new GameObject("Caballo " + (i + 1)); go.transform.SetParent(root, false); go.transform.position = position;
                var visual = Visual(go.transform, "PackHorseIdle", 32, 32, .85f);
                var body = go.AddComponent<CircleCollider2D>(); body.radius = .42f; body.offset = Vector2.up * .3f;
                var roam = go.AddComponent<AnimalRoamingVisual>(); roam.Visual = visual; roam.SideFacesRight = true;
                var idle = new List<Sprite>();
                foreach (int direction in new[] {2,0,1}) for (int frame=0; frame<2; frame++) idle.Add(HouseSprites.Slice("PackHorseIdle", (direction*2+frame)*32, 0, 32, 32));
                roam.Idle = idle.ToArray(); roam.IdleFrames = 2;
                roam.Walk = Rows("PackHorseRun", 32, 32, new[] {2,0,1}, 6); roam.WalkFrames = 6;
                roam.Radius = 1.4f; roam.Speed = .7f; roam.FootRadius = .43f;
                go.AddComponent<HorseMount>().Configure("livestock:horse:" + i, visual);
                Physics2D.SyncTransforms();
            }
            foreach (var animal in world.GetComponentsInChildren<AnimalResource>())
            {
                if (!animal.name.StartsWith("Liebre") || animal.GetComponent<AnimalRoamingVisual>() != null) continue;
                var roam = animal.gameObject.AddComponent<AnimalRoamingVisual>(); roam.Visual = animal.GetComponentInChildren<SpriteRenderer>();
                roam.Idle = Rows("PackRabbit", 16, 16, new[] {0,1,2}, 4); roam.IdleFrames = 4;
                roam.Walk = Rows("PackRabbit", 16, 16, new[] {3,4,5}, 4); roam.WalkFrames = 4;
                roam.Speed = .7f; roam.Radius = 1.1f;
            }
        }

        private static SpriteRenderer Visual(Transform root, string atlas, int width, int height, float scale)
        {
            var art = new GameObject("Animal original"); art.transform.SetParent(root, false); art.transform.localScale = Vector3.one * scale;
            var visual = art.AddComponent<SpriteRenderer>(); visual.sprite = HouseSprites.Slice(atlas, 0, 0, width, height);
            var depth = root.gameObject.AddComponent<WorldSpriteDepth>(); depth.Visual = visual;
            return visual;
        }
        private static Sprite[] Rows(string atlas, int width, int height, int[] rows, int frames)
        {
            var result = new List<Sprite>();
            foreach (int row in rows) for (int frame=0; frame<frames; frame++) result.Add(HouseSprites.Slice(atlas, frame*width, row*height, width, height));
            return result.ToArray();
        }
        private static bool FindMeadowPosition(Vector2 preferred, out Vector3 position)
        {
            for (int i=0; i<49; i++)
            {
                Vector2 p = preferred + new Vector2(i%7-3, i/7-3)*.55f;
                if (!FarmExploration.Contains(p,2) || FarmExploration.IsRiver(p,1) || VillageLayout.IsRoad(p)) continue;
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(p + Vector2.up*.35f, .9f))
                    if (!hit.isTrigger || hit.GetComponentInParent<WorldInteractable>() != null) { blocked = true; break; }
                if (blocked) continue;
                position = p; return true;
            }
            position = default; return false;
        }
    }
}
