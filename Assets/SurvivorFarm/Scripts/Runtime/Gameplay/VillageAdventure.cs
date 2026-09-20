using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable]
    public sealed class VillageAdventureSnapshot
    {
        public EnemyCampState[] encounters;
        public DungeonSupplySnapshot[] supplies;
    }

    [Serializable]
    public sealed class DungeonSupplySnapshot
    {
        public string id;
        public int[] health;
    }

    /// <summary>Five finite excursions supply the village without respawning claimed rewards.</summary>
    public sealed class VillageAdventure : MonoBehaviour
    {
        public static VillageAdventure Instance { get; private set; }
        public const int TotalCount = 5;
        public static readonly Vector3[] PortalPositions = { new Vector3(-35, 18), new Vector3(0, 21) };
        public static readonly EnemyCampDefinition[] CampDefinitions = {
            new EnemyCampDefinition("demo-west", "Campamento del bosque", new Vector3(-35, -15), 4, 24, 3, 2,
                EnemyCombatStyle.Soldier, EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.Soldier),
            new EnemyCampDefinition("demo-east", "Saqueadores de la cantera", new Vector3(35, -15), 5, 35, 5, 3,
                EnemyCombatStyle.Orc, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Soldier),
            new EnemyCampDefinition("demo-north", "Refugio de los demonios", new Vector3(34, 18), 6, 48, 6, 3,
                EnemyCombatStyle.Demon, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Orc, EnemyCombatStyle.Demon)
        };
        public static readonly EnemyCampDefinition[] DungeonDefinitions = {
            new EnemyCampDefinition("demo-crypt", "Cripta del bosque", new Vector3(0, -220), 0, 70, 8, 4,
                EnemyCombatStyle.Soldier, EnemyCombatStyle.Soldier, EnemyCombatStyle.Legacy, EnemyCombatStyle.ArcherGoblin),
            new EnemyCampDefinition("demo-rift", "Santuario de sangre", new Vector3(40, -220), 0, 95, 12, 5,
                EnemyCombatStyle.Demon, EnemyCombatStyle.BloodMonster, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Orc, EnemyCombatStyle.Demon)
        };
        private readonly List<EnemyCamp> camps = new List<EnemyCamp>();
        private readonly List<EnemyCamp> dungeons = new List<EnemyCamp>();
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<TextMesh> signs = new List<TextMesh>();
        private readonly List<GameObject> ownedRoots = new List<GameObject>();
        private PortfolioSession session;
        private ValleyWorld world;
        private int activeDungeon = -1;
        private float outsideCameraSize, nextSignRefresh;
        private Color outsideBackground;
        public event Action ProgressChanged;
        public IReadOnlyList<EnemyCamp> Camps => camps;
        public IReadOnlyList<EnemyCamp> Dungeons => dungeons;
        public bool IsInsideDungeon => activeDungeon >= 0;
        public int ActiveDungeonIndex => activeDungeon;
        public int CompletedCount => camps.Count(c => c.Claimed) + dungeons.Count(c => c.Claimed);
        public bool CanEnterDungeon => session != null && session.HasBegun && !session.IsPaused &&
            session.Security?.IsOccupied != true &&
            !IsInsideDungeon && (session.Phase == SlicePhase.Day || session.Phase == SlicePhase.Dawn) &&
            session.Player.GetComponent<PlayerSurvivalStats>().CurrentHealth > 0;
        public string Objective => IsInsideDungeon
            ? (dungeons[activeDungeon].Claimed ? "Recoge el botín y regresa por la salida" : dungeons[activeDungeon].IsCleared ? "Abre el cofre y regresa por la salida" : dungeons[activeDungeon].MiniBoss?.IsAlive == true ? "Derrota al guardián de las ruinas" : DungeonDefinitions[activeDungeon].Title + " · quedan " + dungeons[activeDungeon].Remaining)
            : "Explora: " + CompletedCount + "/" + TotalCount + " cofres · campamentos oeste, este y noreste · ruinas al norte";

        public void Configure(PortfolioSession owner, ValleyWorld valley)
        {
            if (session != null || owner == null || valley == null || owner.IsPractice) return;
            session = owner; world = valley; Instance = this;
            var ground = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.name == "Spring Grass");
            foreach (var definition in CampDefinitions)
            {
                var camp = NewCamp(definition, ground, true);
                camps.Add(camp);
                var label = world.Label(definition.Title + "\nCofre custodiado", definition.PreferredCenter + new Vector3(0, 4.7f), .07f);
                ownedRoots.Add(label.gameObject); signs.Add(label);
            }
            for (int i = 0; i < DungeonDefinitions.Length; i++) BuildDungeon(i);
            foreach (var camp in camps.Concat(dungeons)) camp.ProgressChanged = () => ProgressChanged?.Invoke();
            Physics2D.SyncTransforms();
        }

        private EnemyCamp NewCamp(EnemyCampDefinition definition, Tilemap terrain, bool scenery)
        {
            var go = new GameObject(definition.Title); go.transform.SetParent(world.transform, false);
            ownedRoots.Add(go);
            var camp = go.AddComponent<EnemyCamp>();
            camp.Configure(definition, new EnemyCampState { id = definition.Id, center = definition.PreferredCenter },
                session.Player, null, terrain, null, null, true, scenery);
            return camp;
        }

        private void BuildDungeon(int index)
        {
            var definition = DungeonDefinitions[index];
            var art = Resources.Load<DungeonArtCatalog>("DungeonArt");
            var camp = NewCamp(definition, null, false); dungeons.Add(camp);
            camp.ConfigureDungeonChallenge(index == 0 ? 2 : 1, index == 0 ? EnemyCombatStyle.Orc : EnemyCombatStyle.Demon);
            var grid = new GameObject("Cámara de piedra", typeof(Grid)); grid.transform.SetParent(camp.transform, false);
            var floor = NewMap(grid.transform, "Suelo de la mazmorra", -100);
            var walls = NewMap(grid.transform, "Muros de las ruinas", -90);
            var floorTile = NewTile(art.Slice(art.Tiles, 144, 128, 16, 16), false);
            var accentTile = NewTile(art.Slice(art.Tiles, 144, 112, 16, 16), false);
            floorTile.color = accentTile.color = index == 0 ? new Color(.64f, .76f, .82f) : new Color(.79f, .58f, .65f);
            var wallTile = NewTile(art.Slice(art.Tiles, 16, 64, 16, 16), true);
            for (int x = -9; x <= 9; x++) for (int y = -7; y <= 7; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (x == -9 || x == 9 || y == -7 || y == 7) walls.SetTile(cell, wallTile);
                else floor.SetTile(cell, Mathf.Abs(x * 13 + y * 17) % 11 == 0 ? accentTile : floorTile);
            }
            walls.gameObject.AddComponent<TilemapCollider2D>();
            var chestArt = Resources.Load<PlayerAnimationLibrary>("EnemyCampArt");
            var chest = Prop(camp.transform, "Cofre de la mazmorra", new Vector3(0, 4), chestArt.Frame(chestArt.Find("Chest"), 2, 0), 1.3f);
            camp.BindChest(chest);
            foreach (int side in new[] { -1, 1 })
            {
                var statue = Prop(camp.transform, "Estatua antigua", new Vector3(side * 6, 3.5f), art.Slice(art.Statue, 7, 0, 48, 48), 2.2f);
                DungeonShrinePresentation.Configure(statue, art);
                var boxes = Prop(camp.transform, "Provisiones antiguas", new Vector3(side * 7, -3), art.Slice(art.Boxes, 0, 0, 16, 16), .8f);
                boxes.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(.8f, .8f);
                boxes.gameObject.AddComponent<DungeonDestructible>().Configure(false);
            }
            var exit = Prop(camp.transform, "Salida de las ruinas", new Vector3(-5, -4.5f), art.Slice(art.Door, 96, 0, 32, 32), 1.65f);
            var exitBody = exit.gameObject.AddComponent<CircleCollider2D>(); exitBody.radius = .65f; exitBody.isTrigger = true;
            exit.gameObject.AddComponent<VillageAdventurePortal>().Configure(this, index, true);
            camp.gameObject.SetActive(false);

            var portal = Prop(world.transform, "Entrada · " + definition.Title, Vector3.zero,
                art.Slice(art.Door, 96, 0, 32, 32), 2.0f).gameObject;
            portal.transform.position = PortalPositions[index]; ownedRoots.Add(portal);
            var body = portal.AddComponent<CircleCollider2D>(); body.radius = .75f; body.isTrigger = true;
            portal.AddComponent<VillageAdventurePortal>().Configure(this, index, false);
            var portalLabel = world.Label(definition.Title + "\nE · entrar de día", PortalPositions[index] + new Vector3(0, 3.1f), .07f);
            signs.Add(portalLabel); ownedRoots.Add(portalLabel.gameObject);
            foreach (int side in new[] { -1, 1 })
            {
                var rock = world.Prop("Rock", PortalPositions[index] + new Vector3(side * 2, .4f), 1.25f, true);
                ownedRoots.Add(rock);
            }
        }

        private SpriteRenderer Prop(Transform parent, string title, Vector3 local, Sprite sprite, float width)
        {
            var go = new GameObject(title); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = sprite;
            if (sprite != null) go.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual = visual; return visual;
        }
        private Tilemap NewMap(Transform parent, string title, int order)
        {
            var go = new GameObject(title, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order; return go.GetComponent<Tilemap>();
        }
        private Tile NewTile(Sprite sprite, bool solid)
        {
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            tiles.Add(tile); return tile;
        }

        public bool EnterDungeon(int index)
        {
            if (!CanEnterDungeon || index < 0 || index >= dungeons.Count ||
                Vector2.Distance(session.Player.transform.position, PortalPositions[index]) > 2.5f) return false;
            var camera = Camera.main;
            if (camera != null) { outsideCameraSize = camera.orthographicSize; outsideBackground = camera.backgroundColor; }
            activeDungeon = index;
            dungeons[index].gameObject.SetActive(true);
            if (camera != null) { camera.orthographicSize = 5.8f; camera.backgroundColor = new Color32(18, 19, 27, 255); }
            Teleport(DungeonDefinitions[index].PreferredCenter + new Vector3(-5, -3.2f));
            FarmNotificationCenter.Show(DungeonDefinitions[index].Title + " · El tiempo sigue corriendo en el valle.");
            return true;
        }

        public void ExitDungeon() => LeaveDungeon(true, true);

        private void LeaveDungeon(bool returnToPortal, bool notify)
        {
            if (!IsInsideDungeon) return;
            int index = activeDungeon;
            dungeons[index].Projectiles.ReturnAll(); dungeons[index].gameObject.SetActive(false);
            activeDungeon = -1;
            var camera = Camera.main;
            if (camera != null) { camera.orthographicSize = outsideCameraSize; camera.backgroundColor = outsideBackground; }
            // GameSaveSystem restores the saved player position before restoring this snapshot.
            // Closing a loaded expedition must preserve that position and must not autosave midway through load.
            var position = session.Player.transform.position;
            if (returnToPortal || !FarmExploration.Contains(position, .5f)) position = PortalPositions[index] + Vector3.down * 1.7f;
            Teleport(position);
            if (notify)
            {
                FarmNotificationCenter.Show("De vuelta al valle. " + CompletedCount + "/" + TotalCount + " cofres recuperados.");
                ProgressChanged?.Invoke();
            }
        }

        private void Teleport(Vector3 position)
        {
            session.Player.GetComponent<PlayerMountController>()?.ForceDismount();
            var player = session.Player;
            player.GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            player.GetComponent<PlayerMovementController>()?.StopMovement();
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
            var camera = Camera.main;
            if (camera != null)
            {
                var follow = camera.GetComponent<CameraFollowTarget>(); follow?.SetCombatFocus(null); follow?.SetTarget(player.transform);
                camera.transform.position = FarmExploration.FrameCamera(position + new Vector3(0, 0, -10), camera);
            }
            Physics2D.SyncTransforms();
        }

        public Vector3 FrameDungeonCamera(Vector3 position, Camera camera)
        {
            if (!IsInsideDungeon || camera == null) return position;
            var center = DungeonDefinitions[activeDungeon].PreferredCenter;
            float x = Mathf.Max(0, 9.5f - camera.orthographicSize * camera.aspect);
            float y = Mathf.Max(0, 7.5f - camera.orthographicSize);
            return new Vector3(Mathf.Clamp(position.x, center.x - x, center.x + x), Mathf.Clamp(position.y, center.y - y, center.y + y), position.z);
        }

        public VillageAdventureSnapshot Capture() => new VillageAdventureSnapshot
        {
            encounters = camps.Concat(dungeons).Select(c => c.Capture()).ToArray(),
            supplies = dungeons.Select(d => new DungeonSupplySnapshot { id = d.Definition.Id,
                health = d.GetComponentsInChildren<DungeonDestructible>(true).Select(b => b.Health).ToArray() }).ToArray()
        };

        public void Restore(VillageAdventureSnapshot saved)
        {
            if (IsInsideDungeon) LeaveDungeon(false, false);
            foreach (var camp in camps.Concat(dungeons))
            {
                var state = saved?.encounters?.FirstOrDefault(s => s != null && s.id == camp.Definition.Id);
                // Never trust a saved transform: progress belongs to the authored encounter ID.
                camp.Restore(new EnemyCampState { id = camp.Definition.Id, center = camp.Definition.PreferredCenter,
                    defeatedMask = state != null ? state.defeatedMask : 0, claimed = state != null && state.claimed });
            }
            foreach (var dungeon in dungeons)
            {
                var health = saved?.supplies?.FirstOrDefault(s => s != null && s.id == dungeon.Definition.Id)?.health;
                var boxes = dungeon.GetComponentsInChildren<DungeonDestructible>(true);
                for (int i = 0; i < boxes.Length; i++) boxes[i].Restore(health != null && i < health.Length ? health[i] : boxes[i].MaximumHealth);
                dungeon.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (session == null || Time.unscaledTime < nextSignRefresh) return;
            nextSignRefresh = Time.unscaledTime + .5f;
            for (int i = 0; i < camps.Count; i++)
                signs[i].text = camps[i].Definition.Title + "\n" + (camps[i].Claimed ? "Suministros recuperados" : camps[i].IsCleared ? "Cofre listo · E" : camps[i].Remaining + " guardianes · cofre custodiado");
            for (int i = 0; i < dungeons.Count; i++)
                signs[camps.Count + i].text = dungeons[i].Definition.Title + "\n" + (dungeons[i].Claimed ? "Mazmorra completada" : CanEnterDungeon ? "E · entrar de día" : "Se abre durante el día");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var tile in tiles) if (tile != null) Destroy(tile);
            foreach (var root in ownedRoots) if (root != null) Destroy(root);
        }
    }
}
