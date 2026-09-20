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
        public const float LeashRadius = 18f;
        public const float AlertRadius = 8f;
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
        private bool manualRewards;
        private int bossSlot = -1, splitParentSlot = -1, childStartSlot = -1;
        public bool HasDungeonChallenge => bossSlot >= 0;
        public CampEnemyAI MiniBoss => bossSlot >= 0 ? members[bossSlot] : null;
        public System.Action ProgressChanged;
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
        public bool CanSimulate => !manualRewards || !Core.PortfolioSession.Active ||
            (Core.PortfolioSession.Instance.HasBegun && !Core.PortfolioSession.Instance.IsPaused &&
             Core.PortfolioSession.Instance.Phase != Core.SlicePhase.Introduction &&
             Core.PortfolioSession.Instance.Phase != Core.SlicePhase.Victory && Core.PortfolioSession.Instance.Phase != Core.SlicePhase.Defeat);
        public bool CanEngage => CanSimulate && isActiveAndEnabled && player != null && player.gameObject.activeInHierarchy && stats.CurrentHealth > 0 &&
            Vector2.Distance(player.transform.position, transform.position) <= AlertRadius &&
            !VillageLayout.IsVillage(player.transform.position) && (Protection == null || !Protection.Contains(player.transform.position));

        public bool CanPursue(CampEnemyAI enemy) => CanSimulate && isActiveAndEnabled && player != null && player.gameObject.activeInHierarchy &&
            stats.CurrentHealth > 0 && (!VillageLayout.IsVillage(player.transform.position) || manualRewards && (enemy.IsProvoked || enemy.HasEngaged)) &&
            (Protection == null || !Protection.Contains(player.transform.position)) &&
            (CanEngage || enemy.IsProvoked && Vector2.Distance(player.transform.position, enemy.transform.position) <= 18f ||
             enemy.HasEngaged && Vector2.Distance(player.transform.position, enemy.transform.position) <= 12f);

        public void Configure(EnemyCampDefinition definition, EnemyCampState saved, PlayerInventory target,
            HomeSafeZone protection, Tilemap terrain, OutdoorEnemyAI template, ValleyCampaign owner = null,
            bool requireChestInteraction = false, bool buildScenery = true)
        {
            if (members.Count != 0) return;
            Definition = definition; state = saved; player = target; campaign = owner;
            manualRewards = requireChestInteraction;
            stats = target.GetComponent<PlayerSurvivalStats>(); Protection = protection; ground = terrain;
            transform.position = state.center;
            art = Resources.Load<PlayerAnimationLibrary>("EnemyCampArt");
            Projectiles = EnemyProjectilePool.Ensure(gameObject, definition.Roster.Length * 2);
            if (buildScenery) BuildScenery();
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
            enemy.ConfigureStats("Limo del campamento", 8, 1, 1.6f, 1.1f, 1.35f, 2);
            enemy.ConfigureAnimation(library);
        }

        public void ConfigureDungeonChallenge(int splitIndex, EnemyCombatStyle bossStyle)
        {
            if (HasDungeonChallenge || player == null) return;
            splitParentSlot = Mathf.Clamp(splitIndex, 0, Definition.Roster.Length - 1);
            childStartSlot = members.Count;
            for (int i = 0; i < 2; i++)
            {
                var child = AddChallengeEnemy("Fragmento viviente", Definition.Roster[splitParentSlot], new Vector3(i == 0 ? -.7f : .7f, 0), false);
                child.transform.localScale = Vector3.one * .62f;
                child.ConfigureStats("Fragmento viviente", 4, 1, 2.2f, .85f, 1.35f, 1);
            }
            bossSlot = members.Count;
            var boss = AddChallengeEnemy("Guardián de las ruinas", bossStyle, new Vector3(0, 2.3f), true);
            boss.transform.localScale = Vector3.one * 1.45f;
            boss.ConfigureStats("Guardián de las ruinas", 48, 3, 1.8f, 1.6f, 1.35f, 18);
            Restore(state);
        }

        private CampEnemyAI AddChallengeEnemy(string title, EnemyCombatStyle style, Vector3 offset, bool elite)
        {
            var go = new GameObject(title); go.transform.SetParent(transform, false); go.SetActive(false);
            go.AddComponent<CircleCollider2D>().radius = .275f;
            var enemy = go.AddComponent<CampEnemyAI>();
            enemy.ConfigureCamp(this, player.transform, members.Count, offset); enemy.DungeonElite = elite;
            if (style == EnemyCombatStyle.Legacy) ConfigureSlime(enemy); else EnemyRoster.Configure(enemy, style, Projectiles);
            members.Add(enemy); return enemy;
        }

        private bool DefeatedSlot(int slot) => slot >= 0 && (state.defeatedMask & (1 << slot)) != 0;
        private bool GuardsAndFragmentsDefeated
        {
            get { for (int i = 0; i < members.Count; i++) if (i != bossSlot && !DefeatedSlot(i)) return false; return true; }
        }
        private bool ShouldBeActive(int slot) => !DefeatedSlot(slot) &&
            (slot == bossSlot ? GuardsAndFragmentsDefeated : slot >= childStartSlot && childStartSlot >= 0 ? DefeatedSlot(splitParentSlot) : true);

        private void AdvanceDungeonChallenge(int defeatedSlot)
        {
            if (!HasDungeonChallenge) return;
            if (defeatedSlot == splitParentSlot)
                for (int i = childStartSlot; i < childStartSlot + 2; i++)
                    if (!DefeatedSlot(i))
                    {
                        Vector3 origin=members[splitParentSlot].transform.position;
                        members[i].ActivateFromPool(ChallengeSpawn(members[i],origin,Vector3.right*(i==childStartSlot?-.45f:.45f)));
                    }
            if (!DefeatedSlot(bossSlot) && GuardsAndFragmentsDefeated && !members[bossSlot].IsAlive)
            {
                members[bossSlot].ActivateFromPool(ChallengeSpawn(members[bossSlot],members[bossSlot].GuardPosition,Vector3.zero));
                FarmNotificationCenter.Show("El guardián despierta · derrota al miniboss para abrir el cofre.");
            }
            if (defeatedSlot == bossSlot) EnemyLootPickup.Scatter(members[bossSlot].transform.position, transform, ItemKind.Diamond, 1);
        }

        private Vector3 ChallengeSpawn(CampEnemyAI enemy,Vector3 origin,Vector3 preferredOffset)
        {
            Physics2D.SyncTransforms();
            float radius=Mathf.Max(.26f,.28f*enemy.transform.lossyScale.x);
            for(int attempt=0;attempt<33;attempt++)
            {
                float angle=attempt*2.399963f;
                Vector3 candidate=origin+(attempt==0?preferredOffset:new Vector3(Mathf.Cos(angle),Mathf.Sin(angle))*(.55f+(attempt%4)*.28f));
                if(!CanOccupy(candidate))continue;
                bool blocked=false;
                foreach(var body in Physics2D.OverlapCircleAll(candidate,radius))
                    if(!body.isTrigger&&!body.transform.IsChildOf(enemy.transform)){blocked=true;break;}
                if(blocked)continue;
                foreach(var hit in Physics2D.LinecastAll(origin,candidate))
                    if(!hit.collider.isTrigger&&hit.collider.GetComponentInParent<EnemyAIBase>()==null&&
                        hit.collider.GetComponentInParent<PlayerInventory>()==null){blocked=true;break;}
                if(!blocked)return candidate;
            }
            // Authored guard posts are inside the room even if the killing blow took
            // place in a narrow corner. Never leave a fragment embedded in a wall.
            return enemy.GuardPosition;
        }

        public bool CanOccupy(Vector2 position) => (manualRewards || !VillageLayout.IsVillage(position)) &&
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
                if (ShouldBeActive(i)) members[i].ActivateFromPool(members[i].GuardPosition);
            }
            RefreshScenery();
        }

        public EnemyCampState Capture() => new EnemyCampState { id = Definition.Id, center = transform.position,
            defeatedMask = state.defeatedMask, claimed = state.claimed };

        public void BindChest(SpriteRenderer visual)
        {
            chest = visual;
            var body = visual.gameObject.GetComponent<CircleCollider2D>();
            if (body == null) body = visual.gameObject.AddComponent<CircleCollider2D>();
            body.isTrigger = false; body.radius = .42f / Mathf.Max(.01f, visual.transform.lossyScale.x);
            Chest = visual.gameObject.GetComponent<EnemyCampChest>();
            if (Chest == null) Chest = visual.gameObject.AddComponent<EnemyCampChest>();
            Chest.Camp = this;
            RefreshScenery();
        }

        public void Defeated(int slot)
        {
            if (slot < 0 || slot >= members.Count || members[slot].IsAlive || (state.defeatedMask & (1 << slot)) != 0) return;
            state.defeatedMask |= 1 << slot;
            AdvanceDungeonChallenge(slot);
            RefreshScenery();
            ProgressChanged?.Invoke();
            string message = IsCleared ? Definition.Title + " despejado. Abre el cofre con E." :
                HasDungeonChallenge && MiniBoss.IsAlive && Remaining == 1 ? "Guardián de las ruinas · derrota al miniboss" :
                Definition.Title + (Remaining == 1 ? ": queda 1 enemigo." : ": quedan " + Remaining + " enemigos.");
            if (campaign != null) campaign.Changed(message);
            else FarmNotificationCenter.Show(message);
        }

        public bool TryClaim(PlayerInventory inventory)
        {
            if (inventory == null || inventory != player || Chest == null || stats.CurrentHealth <= 0 || !IsCleared || Claimed ||
                Vector2.Distance(inventory.transform.position, Chest.transform.position) > 1.75f) return false;
            return GrantVictoryReward();
        }

        private bool GrantVictoryReward()
        {
            if (!IsCleared || Claimed || player == null || stats.CurrentHealth <= 0) return false;
            state.claimed = true;
            var origin = Chest != null ? Chest.transform.position : transform.position;
            EnemyLootPickup.Scatter(origin, transform, ItemKind.Coins, Definition.Coins, angle: 0);
            EnemyLootPickup.Scatter(origin, transform, ItemKind.Iron, Definition.Iron, angle: 2.399963f);
            EnemyLootPickup.Scatter(origin, transform, ItemKind.Food, Definition.Food, angle: 4.799926f);
            if (manualRewards)
            {
                EnemyLootPickup.Scatter(origin, transform, ItemKind.Wood, Definition.Iron * 2, angle: 1.2f);
                EnemyLootPickup.Scatter(origin, transform, ItemKind.Stone, Definition.Iron, angle: 3.6f);
            }
            if (HasDungeonChallenge)
            {
                if (!player.OwnsEquipment("Bow")) EnemyLootPickup.Scatter(origin, transform, ItemKind.Bow, 1, angle: 4.2f);
                EnemyLootPickup.Scatter(origin, transform, ItemKind.Arrow, 16, angle: 5.6f);
            }
            RefreshScenery();
            ProgressChanged?.Invoke();
            if (campaign != null) { campaign.AddInfluence(2); campaign.Changed(""); }
            return true;
        }

        private void Update()
        {
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
            BindChest(chest);
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
            if (chest != null)
            {
                chest.color = IsCleared ? Color.white : new Color(.7f, .7f, .7f);
                if (art != null) chest.sprite = art.Frame(art.Find("Chest"), Claimed ? 0 : 2, 0);
            }
        }

        private void OnDisable() { if (Projectiles != null) Projectiles.ReturnAll(); }
    }
}
