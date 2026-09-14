using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable] public sealed class DungeonExpeditionState
    {
        public int defeatedMask;
        public bool bossDefeated;
        public int[] destructibleHealth;
    }

    public sealed class DungeonExpedition : MonoBehaviour
    {
        private DungeonEntrance entrance;
        private Transform player;
        private DungeonEnemyPool pool;
        private DungeonArtCatalog art;
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<DungeonDestructible> destructibles = new List<DungeonDestructible>();
        private readonly List<Vector3> guardPositions = new List<Vector3>();
        private readonly List<int> guardRooms = new List<int>();
        private readonly List<DungeonChest> chests = new List<DungeonChest>();
        private DungeonBoss boss;
        private DungeonBossGate gate;
        private GameObject returnExit;
        private Color previousBackground;
        private bool present;
        public DungeonExpeditionState State { get; private set; } = new DungeonExpeditionState();
        public IReadOnlyList<DungeonDestructible> Destructibles => destructibles;
        public IReadOnlyList<DungeonChest> Chests => chests;
        public DungeonEnemyPool Pool => pool;
        public DungeonBoss Boss => boss;
        public bool BossFightActive { get; private set; }
        public bool IsPresent => present;
        public int GuardCount => guardPositions.Count;
        public string Objective => State.bossDefeated ? "Recoge el relicario y vuelve a Raizclara" : BossFightActive ? "Vence al Custodio" : BossDoorReady ? "Abre la camara del Custodio" : "Explora las ruinas y despeja la antesala";
        public bool BossDoorReady => Enumerable.Range(0, guardRooms.Count).Where(i => guardRooms[i] == 4).All(IsDefeated);

        public void Initialize(DungeonEntrance entry, Transform target, DungeonEnemyPool enemies, Transform spawn)
        {
            if (entrance != null) return;
            art = Resources.Load<DungeonArtCatalog>("DungeonArt");
            if (art == null || art.Tiles == null) throw new InvalidOperationException("Missing original dungeon art catalog.");
            entrance = entry; player = target; pool = enemies;
            // Replace only the legacy interior. Village objects remain outside, at their authored coordinates.
            foreach (Transform child in transform)
                if (child != pool.transform && child != spawn && child.GetComponent<DungeonChest>() == null) child.gameObject.SetActive(false);
            transform.position = DungeonLayout.Origin;
            if (spawn != null) spawn.position = DungeonLayout.Entry;
            BuildTerrain(); BuildChests(); BuildDestructibles(); BuildEncounters(); BuildExits();
            gameObject.AddComponent<DungeonExpeditionHud>().Configure(this, player);
        }

        private void BuildTerrain()
        {
            var grid = new GameObject("Explorable dungeon", typeof(Grid)); grid.transform.SetParent(transform, false);
            var floor = NewMap(grid.transform, "Stone floor", -100);
            var walls = NewMap(grid.transform, "Solid masonry", -90);
            var a = NewTile(art.Slice(art.Tiles, 144, 128, 16, 16), false);
            var b = NewTile(art.Slice(art.Tiles, 144, 112, 16, 16), false);
            a.color = b.color = new Color(.64f, .76f, .82f);
            var wall = NewTile(art.Slice(art.Tiles, 16, 64, 16, 16), true);
            var border = new HashSet<Vector2Int>();
            foreach (var p in DungeonLayout.Floor)
            {
                floor.SetTile(new Vector3Int(p.x, p.y, 0), Mathf.Abs(p.x * 13 + p.y * 17) % 19 == 0 ? b : a);
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                    if (!DungeonLayout.Floor.Contains(p + new Vector2Int(x, y))) border.Add(p + new Vector2Int(x, y));
            }
            foreach (var p in border) walls.SetTile(new Vector3Int(p.x, p.y, 0), wall);
            walls.gameObject.AddComponent<TilemapCollider2D>();
            // Pillars provide readable cover without narrowing the connecting corridors.
            foreach (var p in new[] { new Vector2(-4, 14), new Vector2(4, 20), new Vector2(15, 19), new Vector2(-21, 19), new Vector2(-4, 35), new Vector2(4, 35) })
            {
                var statue = Prop("Ancient stone shrine", DungeonLayout.At(p.x, p.y), art.Slice(art.Statue, 7, 0, 48, 48), .6f, true);
                var body = statue.GetComponent<BoxCollider2D>(); body.size = new Vector2(1.6f, 1); body.offset = Vector2.down * .5f;
            }
        }

        private Tilemap NewMap(Transform parent, string title, int order)
        {
            var go = new GameObject(title, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }
        private Tile NewTile(Sprite sprite, bool solid)
        {
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            tiles.Add(tile); return tile;
        }
        public SpriteRenderer Prop(string title, Vector3 position, Sprite sprite, float size, bool solid)
        {
            var go = new GameObject(title); go.transform.SetParent(transform, false); go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<WorldSpriteDepth>().Visual = sr;
            if (solid) go.AddComponent<BoxCollider2D>().size = new Vector2(.75f, .75f);
            return sr;
        }
        private void BuildChests()
        {
            var legacy = GetComponentsInChildren<DungeonChest>(true).OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
            Vector3[] positions = { DungeonLayout.At(-4, 2), DungeonLayout.At(-21, 15), DungeonLayout.At(21, 19), DungeonLayout.At(0, 58) };
            string[] names = { "Entry Chest", "Magic Chest", "Mineral Chest", "Z Custodian Reliquary" };
            for (int i = 0; i < 4; i++)
            {
                DungeonChest chest;
                if (i < legacy.Length) chest = legacy[i];
                else { var go = new GameObject(names[i]); go.transform.SetParent(transform, false); chest = go.AddComponent<DungeonChest>(); }
                chest.gameObject.SetActive(true); chest.transform.position = positions[i]; chest.transform.localScale = Vector3.one;
                foreach (var renderer in chest.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
                var visualGo = new GameObject("Original chest art"); visualGo.transform.SetParent(chest.transform, false);
                var sr = visualGo.AddComponent<SpriteRenderer>(); sr.sprite = art.Story("Chest");
                if (sr.sprite != null) visualGo.transform.localScale = Vector3.one * (1.15f / sr.sprite.bounds.size.x);
                visualGo.AddComponent<WorldSpriteDepth>().Visual = sr;
                foreach (var collider in chest.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                var body = chest.gameObject.AddComponent<BoxCollider2D>(); body.size = new Vector2(.85f, .7f);
                chest.ConfigureExpedition(sr, i == 3 ? 80 : 12 + i * 10, i == 3 ? 8 : i * 2, i == 3 ? 4 : i == 2 ? 2 : 0, i == 3 ? 1 : 0, i == 0 ? 2 : 1);
                if (i == 3) chest.Unlocked = () => State.bossDefeated;
                chests.Add(chest);
            }
        }
        private void BuildDestructibles()
        {
            var positions = new[] { new Vector2(4, 2), new Vector2(4, 3), new Vector2(-5, 20), new Vector2(-5, 21),
                new Vector2(5, 12), new Vector2(-22, 13), new Vector2(-21, 13), new Vector2(-14, 20),
                new Vector2(14, 13), new Vector2(15, 13), new Vector2(22, 20), new Vector2(-18, 28),
                new Vector2(-13, 33), new Vector2(5, 37), new Vector2(-5, 30), new Vector2(-6, 56), new Vector2(6, 56) };
            for (int i = 0; i < positions.Length; i++)
            {
                bool rock = i % 3 == 2;
                var sr = Prop(rock ? "Breakable rubble" : "Breakable supplies", DungeonLayout.At(positions[i].x, positions[i].y),
                    rock ? art.Story("Rock") : art.Slice(art.Boxes, 0, 0, 16, 16), 1, true);
                if (rock && sr.sprite != null) sr.transform.localScale = Vector3.one * (1f / sr.sprite.bounds.size.x);
                var destructible = sr.gameObject.AddComponent<DungeonDestructible>();
                destructible.Configure(rock); destructibles.Add(destructible);
            }
        }
        private void BuildEncounters()
        {
            AddGuards(1, new Vector2(-3, 17), new Vector2(3, 18), new Vector2(1, 21));
            AddGuards(2, new Vector2(-19, 16), new Vector2(-15, 19), new Vector2(-20, 20));
            AddGuards(3, new Vector2(16, 16), new Vector2(20, 17), new Vector2(18, 20));
            AddGuards(4, new Vector2(-3, 33), new Vector2(3, 33), new Vector2(0, 36));
            var points = guardPositions.Select((p, i) => { var go = new GameObject("Expedition guard " + i); go.transform.SetParent(transform, false); go.transform.position = p; return go.transform; }).ToArray();
            pool.ConfigureExpedition(this, points);
            var goBoss = new GameObject("El Custodio - dungeon boss"); goBoss.transform.SetParent(transform, false);
            goBoss.SetActive(false);
            var collider = goBoss.AddComponent<CircleCollider2D>(); collider.radius = .55f; collider.isTrigger = true;
            var sr = goBoss.AddComponent<SpriteRenderer>();
            goBoss.transform.localScale = Vector3.one * 1.65f;
            goBoss.AddComponent<WorldSpriteDepth>().Visual = sr;
            boss = goBoss.AddComponent<DungeonBoss>(); boss.ConfigureBoss(this, player, sr);
        }
        private void AddGuards(int room, params Vector2[] positions)
        {
            foreach (var p in positions) { guardRooms.Add(room); guardPositions.Add(DungeonLayout.At(p.x, p.y)); }
        }
        private void BuildExits()
        {
            var exit = Prop("Stairs to Raizclara", DungeonLayout.At(0, -4), art.Slice(art.Door, 96, 0, 32, 32), 1, false);
            exit.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            exit.gameObject.AddComponent<DungeonExit>().Configure(entrance);
            var gateArt = Prop("Custodian gate", DungeonLayout.At(0, 43), art.Slice(art.Door, 0, 0, 32, 32), 1, false);
            var gateBody = gateArt.gameObject.AddComponent<BoxCollider2D>(); gateBody.size = new Vector2(4, .6f);
            gate = gateArt.gameObject.AddComponent<DungeonBossGate>(); gate.Configure(this);
            var portal = Prop("Return stairs", DungeonLayout.At(6, 58), art.Story("Portal"), 1, false);
            if (portal.sprite != null) portal.transform.localScale = Vector3.one * (1.5f / portal.sprite.bounds.size.x);
            portal.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            portal.gameObject.AddComponent<DungeonExit>().Configure(entrance);
            returnExit = portal.gameObject; returnExit.SetActive(false);
        }

        public bool IsDefeated(int index) => index >= 0 && index < GuardCount && (State.defeatedMask & (1 << index)) != 0;
        public void Defeated(int index) { if (index >= 0 && index < GuardCount) State.defeatedMask |= 1 << index; }
        public bool CanEngage(EnemyAIBase enemy)
        {
            int index = IndexOf(enemy);
            return present && index >= 0 && !IsDefeated(index) && Vector2.Distance(player.position, guardPositions[index]) < 9 &&
                Vector2.Distance(player.position, enemy.transform.position) < 8;
        }
        public bool CanOccupy(EnemyAIBase enemy, Vector2 p)
        {
            int index = IndexOf(enemy);
            return index >= 0 && DungeonLayout.Walkable(p) && Vector2.Distance(p, guardPositions[index]) < 8;
        }
        private int IndexOf(EnemyAIBase enemy) { for (int i = 0; i < pool.Enemies.Count; i++) if (pool.Enemies[i] == enemy) return i; return -1; }
        public void SetPresent(bool value)
        {
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            if (Camera.main != null) { if (value && !present) previousBackground = Camera.main.backgroundColor; if (value || present) Camera.main.backgroundColor = value ? new Color32(18, 22, 26, 255) : previousBackground; }
            present = value; BossFightActive = false; boss.ReturnToPool();
            gate.gameObject.SetActive(!State.bossDefeated); returnExit.SetActive(State.bossDefeated);
        }
        public bool BeginBoss()
        {
            if (!present || !BossDoorReady || State.bossDefeated || BossFightActive || player.GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) return false;
            BossFightActive = true;
            player.position = DungeonLayout.At(0, 47); player.GetComponent<PlayerMovementController>()?.StopMovement();
            var body = player.GetComponent<Rigidbody2D>(); if (body != null) { body.position = player.position; body.linearVelocity = Vector2.zero; }
            boss.ActivateFromPool(DungeonLayout.BossCenter);
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(boss.transform);
            FarmNotificationCenter.Show("El Custodio despierta."); return true;
        }
        public void CompleteBoss()
        {
            if (State.bossDefeated) return;
            State.bossDefeated = true; BossFightActive = false;
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            gate.gameObject.SetActive(false); returnExit.SetActive(true);
            player.GetComponent<AdventureProgress>()?.DefeatGolem();
            FarmNotificationCenter.Show("Custodio derrotado. El relicario y la salida estan abiertos.");
        }
        private void Update()
        {
            if (!present || !BossFightActive) return;
            if (player.GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0)
            { BossFightActive = false; boss.ReturnToPool(); Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null); }
        }
        public DungeonExpeditionState Capture() => new DungeonExpeditionState { defeatedMask = State.defeatedMask,
            bossDefeated = State.bossDefeated, destructibleHealth = destructibles.Select(d => d.Health).ToArray() };
        public void Restore(DungeonExpeditionState data)
        {
            State = data == null ? new DungeonExpeditionState() : new DungeonExpeditionState { defeatedMask = data.defeatedMask & ((1 << GuardCount) - 1), bossDefeated = data.bossDefeated };
            for (int i = 0; i < destructibles.Count; i++) destructibles[i].Restore(data?.destructibleHealth != null && i < data.destructibleHealth.Length ? data.destructibleHealth[i] : destructibles[i].MaximumHealth);
            BossFightActive = false; boss.ReturnToPool();
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            gate.gameObject.SetActive(!State.bossDefeated); returnExit.SetActive(State.bossDefeated);
            if (present) pool.SpawnEncounter();
        }
        public Vector3 RecoverPosition(Vector3 saved)
        {
            if (!DungeonLayout.Walkable(saved)) return DungeonLayout.Entry;
            if (!State.bossDefeated && DungeonLayout.RoomAt(saved) == 5) return DungeonLayout.At(0, 41);
            foreach (var hit in Physics2D.OverlapCircleAll(saved, .35f))
                if (!hit.isTrigger && !hit.transform.IsChildOf(player)) return DungeonLayout.Entry;
            return saved;
        }
        private void OnDestroy() { foreach (var tile in tiles) if (tile != null) Destroy(tile); }
    }
}
