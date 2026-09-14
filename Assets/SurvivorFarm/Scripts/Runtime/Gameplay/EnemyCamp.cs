using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class EnemyCamp : MonoBehaviour
    {
        public const float LeashRadius = 6f;
        private static readonly Vector3[] GuardOffsets = {
            new Vector3(-2, -1.4f), new Vector3(2, -1.4f), new Vector3(0, .15f), new Vector3(-3, .1f), new Vector3(3, .1f)
        };
        private readonly List<CampEnemyAI> members = new List<CampEnemyAI>();
        private ValleyCampaign campaign;
        private PlayerInventory player;
        private PlayerSurvivalStats stats;
        private EnemyCampState state;
        private Tilemap ground;
        private GameObject fire;
        private SpriteRenderer chest;
        private PlayerAnimationLibrary art;
        private bool discovered;
        public EnemyCampDefinition Definition { get; private set; }
        public IReadOnlyList<CampEnemyAI> Members => members;
        public EnemyProjectilePool Projectiles { get; private set; }
        public HomeSafeZone Protection { get; private set; }
        public EnemyCampChest Chest { get; private set; }
        public int PoolCount => members.Count;
        public int Remaining
        {
            get { int count = 0; for (int i = 0; i < members.Count; i++) if ((state.defeatedMask & (1 << i)) == 0) count++; return count; }
        }
        public bool IsCleared => state != null && Remaining == 0;
        public bool Claimed => state != null && state.claimed;
        public bool CanEngage => isActiveAndEnabled && player != null && player.gameObject.activeInHierarchy && stats.CurrentHealth > 0 &&
            Vector2.Distance(player.transform.position, transform.position) <= LeashRadius + 1 &&
            !VillageLayout.IsVillage(player.transform.position) && (Protection == null || !Protection.Contains(player.transform.position));

        public void Configure(EnemyCampDefinition definition, EnemyCampState saved, PlayerInventory target,
            HomeSafeZone protection, Tilemap terrain, OutdoorEnemyAI template, ValleyCampaign owner = null)
        {
            if (members.Count != 0) return;
            Definition = definition; state = saved; player = target; campaign = owner;
            stats = target.GetComponent<PlayerSurvivalStats>(); Protection = protection; ground = terrain;
            transform.position = state.center;
            art = Resources.Load<PlayerAnimationLibrary>("EnemyCampArt");
            Projectiles = EnemyProjectilePool.Ensure(gameObject, definition.Roster.Length * 2);
            BuildScenery();
            for (int i = 0; i < definition.Roster.Length; i++)
            {
                var go = new GameObject("Camp guard " + (i + 1)); go.transform.SetParent(transform, false); go.SetActive(false);
                go.AddComponent<CircleCollider2D>().radius = .275f;
                var enemy = go.AddComponent<CampEnemyAI>();
                enemy.ConfigureCamp(this, target.transform, i, GuardOffsets[i]);
                enemy.ConfigureLoot(template != null ? template.LootPrefab : null);
                if (definition.Roster[i] == EnemyCombatStyle.Legacy) ConfigureSlime(enemy);
                else EnemyRoster.Configure(enemy, definition.Roster[i], Projectiles);
                members.Add(enemy);
            }
            Restore(saved);
        }

        private void ConfigureSlime(CampEnemyAI enemy)
        {
            var library = Resources.Load<PlayerAnimationLibrary>("SproutSlimeAnimations");
            var go = new GameObject("Slime Visual"); go.transform.SetParent(enemy.transform, false);
            var visual = go.AddComponent<SpriteRenderer>();
            if (library != null) visual.sprite = library.Frame(library.Find("Idle"), 0, 0);
            else if (campaign != null) visual.sprite = campaign.World.Art("Slime");
            if (visual.sprite != null) go.transform.localScale = Vector3.one * (.8f / visual.sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual = visual;
            enemy.ConfigureVisuals(visual, null, Color.white, "Limo del campamento");
            enemy.ConfigureStats("Limo del campamento", 8, 1, 1.3f, .75f, 1.7f, 2);
            enemy.ConfigureAnimation(library);
        }

        public bool CanOccupy(Vector2 position) => !VillageLayout.IsVillage(position) &&
            Vector2.Distance(position, transform.position) <= LeashRadius &&
            (Protection == null || !Protection.Contains(position, .3f)) &&
            (ground == null || ground.HasTile(ground.WorldToCell(position)));

        public void Restore(EnemyCampState saved)
        {
            state = saved;
            state.defeatedMask &= (1 << members.Count) - 1;
            if (state.claimed) state.defeatedMask = (1 << members.Count) - 1;
            transform.position = state.center;
            Projectiles.ReturnAll();
            for (int i = 0; i < members.Count; i++)
            {
                members[i].ReturnToPool();
                if ((state.defeatedMask & (1 << i)) == 0) members[i].ActivateFromPool(members[i].GuardPosition);
            }
            RefreshScenery();
        }

        public void Defeated(int slot)
        {
            if (slot < 0 || slot >= members.Count || members[slot].IsAlive || (state.defeatedMask & (1 << slot)) != 0) return;
            state.defeatedMask |= 1 << slot;
            RefreshScenery();
            if (IsCleared && GrantVictoryReward()) return;
            string message = IsCleared ? Definition.Title + " despejado. Suministros pendientes." : Definition.Title + ": quedan " + Remaining + " enemigos.";
            if (campaign != null) campaign.Changed(message);
            else FarmNotificationCenter.Show(message);
        }

        public bool TryClaim(PlayerInventory inventory)
        {
            if (inventory == null || inventory != player || stats.CurrentHealth <= 0 || !IsCleared || Claimed ||
                Vector2.Distance(inventory.transform.position, Chest.transform.position) > 1.75f) return false;
            return GrantVictoryReward();
        }

        private bool GrantVictoryReward()
        {
            if (!IsCleared || Claimed || player == null || stats.CurrentHealth <= 0) return false;
            var inventory = player;
            state.claimed = true;
            inventory.AddCoins(Definition.Coins);
            (inventory.GetComponent<AdventureProgress>() ?? inventory.gameObject.AddComponent<AdventureProgress>()).AddIron(Definition.Iron);
            inventory.AddFood(Definition.Food);
            RefreshScenery();
            string message = Definition.Title + " despejado!\n+" + Definition.Coins + " oro, +" + Definition.Iron + " hierro, +" + Definition.Food + " raciones.";
            if (campaign != null && CampCombatQuests.CanClaim(campaign.Data, Definition.Id)) message += "\nMision lista: vuelve con Iria.";
            if (campaign != null) campaign.Changed(message + campaign.AddInfluence(2));
            else FarmNotificationCenter.Show(message);
            return true;
        }

        private void Update()
        {
            // Old saves may contain cleared, unclaimed chests. Pay after all save data has restored.
            if (IsCleared && !Claimed && GrantVictoryReward()) { discovered = true; return; }
            if (discovered || player == null || Vector2.Distance(player.transform.position, transform.position) > 10) return;
            discovered = true;
            FarmNotificationCenter.Show(Definition.Title + (IsCleared ? " - despejado" : " - " + Remaining + " enemigos"));
        }

        private void BuildScenery()
        {
            if (art == null) { Debug.LogError("EnemyCampArt is missing", this); return; }
            var dirt = new HashSet<Vector2Int>();
            for (int x = -4; x < 4; x++) for (int y = -3; y < 3; y++)
                if (!(Mathf.Abs(x + .5f) > 3 && Mathf.Abs(y + .5f) > 2)) dirt.Add(new Vector2Int(x, y));
            for (int y = -5; y < -3; y++) { dirt.Add(new Vector2Int(-1, y)); dirt.Add(new Vector2Int(0, y)); }
            var terrain = new GameObject("Camp clearing"); terrain.transform.SetParent(transform, false);
            terrain.AddComponent<ValleyTerrain>().Paint(dirt, transform.position, Vector3.right, Vector3.up, -70);
            Prop("Shelter", "Shelter", 3, Definition.ShelterVariant, new Vector2(-2, 1.4f), 2.2f, new Vector2(1.6f, .45f));
            if (Definition.Roster.Length > 3) Prop("Shelter", "Shelter", 3, Definition.ShelterVariant, new Vector2(2, 1.4f), 2.2f, new Vector2(1.6f, .45f));
            for (int i = -3; i <= 3; i++)
                Prop("Barricade", "Fence", 0, 3, new Vector2(i, 3.1f), 1, new Vector2(.95f, .18f));
            Prop("Crate", "Crate", 0, 0, new Vector2(-3.25f, 1.1f), .7f, new Vector2(.6f, .5f));
            Prop("Crate", "Crate", 0, 1, new Vector2(3.25f, 1.1f), .7f, new Vector2(.6f, .5f));
            fire = Prop("Campfire", "Fire", 0, 0, new Vector2(0, -1.2f), .6f, new Vector2(.3f, .2f)).gameObject;
            var flame = fire.AddComponent<MovementSpriteAnimation>(); flame.Visual = fire.GetComponent<SpriteRenderer>();
            flame.Idle = new Sprite[6]; for (int i = 0; i < 6; i++) flame.Idle[i] = art.Frame(art.Find("Fire"), 0, i); flame.Walk = flame.Idle;
            chest = Prop("Supplies", "Chest", 2, 0, new Vector2(.2f, 1.6f), .75f, Vector2.zero);
            chest.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            chest.GetComponent<CircleCollider2D>().radius = .45f;
            Chest = chest.gameObject.AddComponent<EnemyCampChest>(); Chest.Camp = this;
        }

        private SpriteRenderer Prop(string name, string clip, int row, int frame, Vector2 offset, float width, Vector2 colliderSize)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = offset;
            var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = art.Frame(art.Find(clip), row, frame);
            go.transform.localScale = Vector3.one * (width / visual.sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual = visual;
            if (colliderSize != Vector2.zero)
            {
                var solid = go.AddComponent<BoxCollider2D>(); solid.size = colliderSize / go.transform.localScale.x;
                solid.offset = new Vector2(0, .1f) / go.transform.localScale.x;
            }
            return visual;
        }

        private void RefreshScenery()
        {
            if (fire != null) fire.SetActive(!IsCleared);
            if (chest != null) chest.color = Claimed ? new Color(.65f, .65f, .65f) : IsCleared ? Color.white : new Color(.7f, .7f, .7f);
        }

        private void OnDisable() { if (Projectiles != null) Projectiles.ReturnAll(); }
    }
}
