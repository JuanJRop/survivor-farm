using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class EnemyRosterChecks
    {
        public static IEnumerator Run(PlayerInventory player, Action<string, int, int> capture)
        {
            ValidateFrames("SpearGoblinAnimations");
            ValidateFrames("ArcherGoblinAnimations");
            ValidateFrames("EnemyProjectileArt");
            var outside = Object.FindFirstObjectByType<OutdoorEnemyPool>();
            if (outside == null) throw new Exception("Missing outdoor pool in Main");
            outside.Initialize();
            var enemies = outside.GetComponentsInChildren<OutdoorEnemyAI>(true);
            var spear = enemies.FirstOrDefault(e => e.CombatStyle == EnemyCombatStyle.SpearGoblin);
            var archer = enemies.FirstOrDefault(e => e.CombatStyle == EnemyCombatStyle.ArcherGoblin);
            if (spear == null || archer == null || !enemies.Any(e => e.CombatStyle == EnemyCombatStyle.Legacy))
                throw new Exception("Main must contain both new goblins and its legacy enemies");
            int count = outside.PoolCount;
            var dungeon = Object.FindFirstObjectByType<DungeonEnemyPool>(FindObjectsInactive.Include);
            if (dungeon == null) throw new Exception("Missing dungeon pool in Main");
            dungeon.Initialize();
            var dungeonEnemies = dungeon.GetComponentsInChildren<EnemyAIBase>(true);
            if (!dungeonEnemies.Any(e => e.CombatStyle == EnemyCombatStyle.SpearGoblin) || !dungeonEnemies.Any(e => e.CombatStyle == EnemyCombatStyle.ArcherGoblin))
                throw new Exception("Dungeon roster did not initialize");
            if (!dungeonEnemies.Any(e => e.name.Contains("Golem"))) throw new Exception("Dungeon lost its existing golems");
            if (dungeon.PoolCount != dungeonEnemies.Length) throw new Exception("Dungeon orphaned scene-authored instances");

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var cells = (List<Vector3>)typeof(OutdoorEnemyPool).GetField("spawnPositions", flags).GetValue(outside);
            var candidates = cells.OrderBy(p => Mathf.Abs(p.y + 4) + Mathf.Abs(p.x - 12));
            Vector3 center = Vector3.zero;
            bool found = false;
            foreach (var cell in candidates)
                if (outside.CanOccupy(cell, null) && outside.CanOccupy(cell + Vector3.left * .8f, spear) && outside.CanOccupy(cell + Vector3.right * 3, archer))
                { center = cell; found = true; break; }
            if (!found) throw new Exception("No unobstructed encounter area available for QA");
            var camera = Camera.main;
            var oldPosition = player.transform.position;
            var oldCamera = camera.transform.position;
            float oldSize = camera.orthographicSize;
            var stats = player.GetComponent<PlayerSurvivalStats>();
            int maxHealth = stats.MaxHealth, health = stats.CurrentHealth;
            var campaign = player.GetComponent<ValleyCampaign>();
            bool camp = campaign.Data.camp;
            var pets = Object.FindObjectsByType<PetCompanion>(FindObjectsSortMode.None);
            var enabledPets = pets.Where(p => p.enabled).ToArray();
            foreach (var pet in enabledPets) pet.enabled = false;
            try
            {
                campaign.Data.camp = true;
                stats.Restore(maxHealth, maxHealth, 1);
                outside.enabled = true;
                outside.RefreshPopulation();
                foreach (var enemy in enemies) enemy.ReturnToPool();
                typeof(OutdoorEnemyPool).GetField("nextSpawn", flags).SetValue(outside, Time.time + 999);
                campaign.Teleport(center);
                camera.transform.position = center + new Vector3(.8f, .5f, -10);
                camera.orthographicSize = 3.5f;
                spear.ActivateFromPool(center + Vector3.left * .8f);
                archer.ActivateFromPool(center + Vector3.right * 3);
                Physics2D.SyncTransforms();
                yield return new WaitForSeconds(.15f);
                if (!spear.IsPreparingAttack || !archer.IsPreparingAttack) throw new Exception("Live goblins failed to telegraph attacks");
                var initial = archer.SpriteAnimation.Visual.sprite;
                capture("enemies-telegraph-1280.png", 1280, 720);
                yield return new WaitForSeconds(.2f);
                if (initial == archer.SpriteAnimation.Visual.sprite) throw new Exception("Archer attack animation is frozen");
                yield return new WaitForSeconds(.45f);
                var arrows = outside.GetComponent<EnemyProjectilePool>();
                if (arrows.ActiveCount == 0) throw new Exception("Live archer did not release a pooled arrow");
                capture("enemies-combat-1920.png", 1920, 1080);
                var flying = arrows.GetComponentInChildren<EnemyArrowProjectile>();
                Vector3 arrowPosition = flying.transform.position; Vector2 heading = flying.Direction;
                int healthBeforeDodge = stats.CurrentHealth;
                campaign.Teleport(center + Vector3.up * 1.5f);
                yield return new WaitForSeconds(.2f);
                if (!flying.gameObject.activeSelf || Vector2.Distance(arrowPosition, flying.transform.position) < .7f || flying.Direction != heading)
                    throw new Exception("Arrow failed to fly independently of its animation and moving target.");
                capture("enemies-arrow-flight-1920.png", 1920, 1080);
                yield return new WaitForSeconds(.35f);
                if (stats.CurrentHealth != healthBeforeDodge) throw new Exception("Sidestepping the committed arrow did not dodge it.");
                archer.TakeDamage(100, player);
                yield return new WaitForSeconds(.55f);
                if (!archer.IsDying || archer.SpriteAnimation.StateName != "Dead") throw new Exception("Death clip missing in real scene");
                capture("enemies-death-1280.png", 1280, 720);
                yield return new WaitForSeconds(.35f);
                if (archer.gameObject.activeSelf) throw new Exception("Dead goblin did not return to pool");
                int generation = archer.SpawnGeneration;
                archer.ActivateFromPool(center + Vector3.right * 3);
                if (!archer.IsAlive || archer.IsDying || archer.SpawnGeneration != generation + 1) throw new Exception("Goblin reuse retained dead state");
                int swordDamage = player.GetComponent<PlayerCombatController>().GetAttackDamage(FarmTool.Sword);
                archer.TakeDamage(swordDamage, player);
                if (!archer.IsAlive || archer.CurrentHealth != archer.MaximumHealth - swordDamage) throw new Exception("Main archer cannot survive the equipped sword's first hit.");
                outside.Initialize();
                if (outside.PoolCount != count) throw new Exception("Outdoor pool grew on reuse");
            }
            finally
            {
                outside.enabled = false;
                foreach (var enemy in enemies) enemy.ReturnToPool();
                dungeon.DespawnAll();
                campaign.Data.camp = camp;
                campaign.Teleport(oldPosition);
                stats.Restore(maxHealth, health, 1);
                camera.transform.position = oldCamera;
                camera.orthographicSize = oldSize;
                foreach (var pet in enabledPets) if (pet != null) pet.enabled = true;
            }
            Debug.Log("PASS: Main outdoor/dungeon goblin rosters, nonblank original animation frames, live attacks/arrows/death, bounded pool reuse.");
        }

        private static void ValidateFrames(string resource)
        {
            var library = Resources.Load<PlayerAnimationLibrary>(resource);
            if (library == null) throw new Exception("Missing enemy art: " + resource);
            foreach (var clip in library.Clips)
            {
                var target = RenderTexture.GetTemporary(clip.Atlas.width, clip.Atlas.height, 0, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active;
                Graphics.Blit(clip.Atlas, target);
                RenderTexture.active = target;
                var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32();
                for (int row = 0; row < clip.Directions; row++)
                    for (int frame = 0; frame < clip.Frames; frame++)
                    {
                        var rect = library.Frame(clip, row, frame).rect;
                        int opaque = 0;
                        for (int y = (int)rect.yMin; y < rect.yMax; y++)
                            for (int x = (int)rect.xMin; x < rect.xMax; x++) if (pixels[y * target.width + x].a > 100) opaque++;
                        if (opaque < 4) throw new Exception("Blank original sprite frame: " + resource + "/" + clip.Name + "/" + row + "/" + frame);
                    }
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(texture);
            }
        }
    }
}
