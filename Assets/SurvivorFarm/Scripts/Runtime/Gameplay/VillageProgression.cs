using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable] public sealed class VillageHouseLevel {public string id;public int level=1;}
    [Serializable] public sealed class VillageGuardState
    {public int slot,strength=1,toughness=1,health=14;public float x,y;}
    [Serializable] public sealed class VillageProgressionState
    {public int wellLevel=1;public List<VillageHouseLevel> houses=new List<VillageHouseLevel>();public List<VillageGuardState> guards=new List<VillageGuardState>();}

    /// <summary>Settlement investments use actual inventory and persist independently of the clock.</summary>
    public sealed class VillageProgression : MonoBehaviour
    {
        public static VillageProgression Instance {get;private set;}
        private PortfolioSession session;
        private VillageUpgradeWindow window;
        private Transform guardRoot,well;
        private SpriteRenderer wellArt;
        private Vector3 wellScale;
        private GameObject wellTrim;
        private readonly List<VillageGuard> guards=new List<VillageGuard>();
        private VillageHouseHealth[] houses=Array.Empty<VillageHouseHealth>();
        private float nextHealing;
        public event Action Changed;
        public int WellLevel {get;private set;}=1;
        public int GuardCapacity=>WellLevel+1;
        public IReadOnlyList<VillageGuard> Guards=>guards;
        public IReadOnlyList<VillageHouseHealth> Houses=>houses;
        public PlayerInventory Player=>session!=null?session.Player:null;
        public PortfolioSession Session=>session;
        public string LastMessage {get;private set;}="";
        public Vector3 WellPosition=>well!=null?well.position:VillageLayout.Well;
        public bool CanManage=>session!=null&&session.HasBegun&&!session.IsPaused&&session.Adventure?.IsInsideDungeon!=true&&
            Player!=null&&Player.GetComponent<PlayerSurvivalStats>()?.CurrentHealth>0&&
            (Vector2.Distance(Player.transform.position,WellPosition)<2.7f||houses.Any(h=>h!=null&&Vector2.Distance(Player.transform.position,h.Transform.position)<2.3f));

        public void Configure(PortfolioSession owner)
        {
            if(session!=null)return;
            Instance=this;session=owner;
            houses=FindObjectsByType<VillageHouseHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(h=>h.Id).ToArray();
            well=session.Core!=null?session.Core.transform:null;
            if(well!=null)
            {
                wellArt=well.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(s=>s.enabled&&s.sprite!=null);
                if(wellArt!=null)wellScale=wellArt.transform.localScale;
                var post=well.gameObject.GetComponent<VillageUpgradePost>();
                if(post==null)post=well.gameObject.AddComponent<VillageUpgradePost>();
                post.Configure(this);
                // The old well hint must not win pointer selection over the village service.
                var oldHint=well.GetComponent<ValleyInteraction>();if(oldHint!=null)oldHint.enabled=false;
            }
            guardRoot=new GameObject("Guardia contratada de Raízclara").transform;guardRoot.SetParent(transform,false);
            window=Player.GetComponent<VillageUpgradeWindow>();
            if(window==null)window=Player.gameObject.AddComponent<VillageUpgradeWindow>();
            window.Configure(this);
            ApplyWellArt();
        }

        public void OpenVillage(){if(CanManage)window?.Open(null);}
        public void OpenHouse(VillageHouseHealth house){if(CanManage&&houses.Contains(house))window?.Open(house);}
        public static Vector2 GuardPost(int slot)=>slot switch{0=>new Vector2(-3.2f,1.4f),1=>new Vector2(3.2f,1.4f),2=>new Vector2(-2.4f,-3.1f),_=>new Vector2(2.4f,-3.1f)};
        public static string GuardName(int slot)=>slot switch{0=>"Ada",1=>"Bruno",2=>"Cora",_=>"Teo"};
        public static string HouseTitle(VillageHouseHealth house)=>house==null?"Casa":house.Id=="player-home"?"Tu casa":house.gameObject.name;
        public string HouseCost(VillageHouseHealth house)=>house==null||house.Level>=3?"Nivel máximo":$"{20*house.Level} madera · {14*house.Level} piedra · {20*house.Level} oro";
        public string WellCost=>WellLevel>=3?"Nivel máximo":$"{18*WellLevel} madera · {22*WellLevel} piedra · {30*WellLevel} oro";
        public int RecruitCost=>30+guards.Count*20;
        public string StrengthCost(VillageGuard guard)=>guard.Strength>=3?"Máxima":$"{20*guard.Strength} oro · {5*guard.Strength} madera";
        public string ToughnessCost(VillageGuard guard)=>guard.Toughness>=3?"Máxima":$"{18*guard.Toughness} oro · {7*guard.Toughness} piedra";

        private bool Reject(string message){LastMessage=message;Changed?.Invoke();return false;}
        private bool Spend(int wood,int stone,int coins,int food=0)
        {
            if(!CanManage)return Reject("Acércate al pozo o a una casa para mejorar el pueblo.");
            if(Player.Wood<wood||Player.Stone<stone||Player.Coins<coins||Player.Food<food)return Reject("Faltan materiales para esta mejora.");
            if(wood+stone>0&&!Player.TrySpendMaterials(wood,stone))return false;
            if(coins>0)Player.TrySpendCoins(coins);
            if(food>0)Player.TryRemoveFood(food);
            return true;
        }
        private bool Complete(string message)
        {
            LastMessage=message;session.Security?.RefreshState();Changed?.Invoke();
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
        }
        public bool UpgradeHouse(VillageHouseHealth house)
        {
            if(house==null||!houses.Contains(house)||house.Level>=3)return Reject("Esta casa ya está al máximo.");
            if(house.Health<house.Maximum)return Reject("Repara la casa antes de ampliar sus cimientos.");
            int tier=house.Level;if(!Spend(20*tier,14*tier,20*tier))return false;
            house.SetLevel(tier+1,true);return Complete(HouseTitle(house)+" · nivel "+house.Level);
        }
        public bool RepairHouse(VillageHouseHealth house)
        {
            if(!CanManage||house==null||!houses.Contains(house)||!house.Repair(Player))return Reject("Acércate y reúne 2 de madera; espera un instante entre reparaciones.");
            return Complete("Casa reparada: "+house.Health+" / "+house.Maximum);
        }
        public bool UpgradeWell()
        {
            if(WellLevel>=3)return Reject("El pozo ya está al máximo.");
            int tier=WellLevel;if(!Spend(18*tier,22*tier,30*tier))return false;
            WellLevel++;ApplyWellArt();return Complete("Pozo nivel "+WellLevel+" · capacidad de "+GuardCapacity+" guardias.");
        }
        public bool Recruit()
        {
            if(guards.Count>=GuardCapacity)return Reject("Mejora el pozo para alojar más guardias.");
            if(!Spend(0,0,RecruitCost,2))return false;
            int slot=Enumerable.Range(0,4).First(s=>guards.All(g=>g.Slot!=s));
            var point=GuardPost(slot);SpawnGuard(new VillageGuardState{slot=slot,x=point.x,y=point.y});
            return Complete(GuardName(slot)+" se une a la defensa del pueblo.");
        }
        public bool TrainStrength(VillageGuard guard)
        {
            if(guard==null||!guards.Contains(guard)||guard.Strength>=3)return Reject("La fuerza ya está al máximo.");
            if(!guard.Body.IsAlive)return Reject("Recupera primero al guardia caído.");
            if(!Spend(5*guard.Strength,0,20*guard.Strength))return false;
            guard.Train(guard.Strength+1,guard.Toughness);return Complete(guard.DisplayName+" mejora su ataque.");
        }
        public bool TrainToughness(VillageGuard guard)
        {
            if(guard==null||!guards.Contains(guard)||guard.Toughness>=3)return Reject("La resistencia ya está al máximo.");
            if(!guard.Body.IsAlive)return Reject("Recupera primero al guardia caído.");
            if(!Spend(0,7*guard.Toughness,18*guard.Toughness))return false;
            guard.Train(guard.Strength,guard.Toughness+1);return Complete(guard.DisplayName+" mejora su resistencia.");
        }
        public bool Recover(VillageGuard guard)
        {
            if(guard==null||!guards.Contains(guard)||guard.Body.IsAlive)return Reject("Este guardia está en pie.");
            if(!Spend(0,0,12,2))return false;
            guard.Recover();return Complete(guard.DisplayName+" vuelve a su puesto.");
        }
        private void SpawnGuard(VillageGuardState saved)
        {
            var root=new GameObject(GuardName(saved.slot)+" · guardia");root.transform.SetParent(guardRoot,false);root.transform.position=GuardPost(saved.slot);
            var guard=root.AddComponent<VillageGuard>();guards.Add(guard);guard.Configure(this,saved);
        }
        private void ApplyWellArt()
        {
            if(wellArt!=null){wellArt.transform.localScale=wellScale*(1+(WellLevel-1)*.1f);wellArt.color=WellLevel==3?new Color(1,.9f,.66f):WellLevel==2?new Color(.82f,.94f,1):Color.white;}
            if(wellTrim!=null){wellTrim.SetActive(false);Destroy(wellTrim);}
            if(well==null||WellLevel==1)return;
            wellTrim=new GameObject("Cimientos mejorados del pozo");wellTrim.transform.SetParent(well,false);
            for(int side=-1;side<=1;side+=2)
            {
                var stone=PackEnvironment.Sprite(wellTrim.transform,"Refuerzo de piedra","MineralRocks",0,144,32,16,(Vector2)well.position+new Vector2(side*.57f,-.12f),WellLevel==3?.45f:.35f);
                stone.color=WellLevel==3?new Color(1,.87f,.56f):Color.white;
            }
        }
        private void Update()
        {
            if(session==null||!session.HasBegun||session.IsPaused||Time.time<nextHealing)return;
            nextHealing=Time.time+8;
            if(Physics2D.OverlapCircleAll(WellPosition,6).Any(c=>c.GetComponentInParent<EnemyAIBase>()?.IsAlive==true))return;
            foreach(var resident in VillageResidentHealth.All)
                if(resident!=null&&resident.IsAlive&&Vector2.Distance(resident.transform.position,WellPosition)<5.5f)resident.Heal(WellLevel);
            session.Security?.RefreshState();
        }
        public VillageProgressionState Capture()=>new VillageProgressionState
        {wellLevel=WellLevel,houses=houses.Where(h=>h!=null).Select(h=>new VillageHouseLevel{id=h.Id,level=h.Level}).ToList(),guards=guards.Where(g=>g!=null).Select(g=>g.Capture()).ToList()};
        public void Restore(VillageProgressionState saved)
        {
            window?.Close();WellLevel=Mathf.Clamp(saved?.wellLevel??1,1,3);
            foreach(var house in houses)house.SetLevel(saved?.houses?.FirstOrDefault(h=>h!=null&&h.id==house.Id)?.level??1);
            foreach(var guard in guards)if(guard!=null){guard.gameObject.SetActive(false);Destroy(guard.gameObject);}guards.Clear();
            foreach(var guard in (saved?.guards??new List<VillageGuardState>()).Where(g=>g!=null&&g.slot>=0&&g.slot<4).GroupBy(g=>g.slot).Select(g=>g.First()).Take(GuardCapacity))SpawnGuard(guard);
            ApplyWellArt();LastMessage="";nextHealing=Time.time+8;Changed?.Invoke();
        }
        private void OnDestroy(){if(Instance==this)Instance=null;if(window!=null)window.Close();}
    }

    public sealed class VillageUpgradePost : WorldInteractable
    {
        private VillageProgression owner;private Transform point;
        public void Configure(VillageProgression value){owner=value;if(point==null){point=new GameObject("Mejoras del pozo").transform;point.SetParent(transform,false);point.localPosition=Vector3.down*.7f;}}
        public override Transform Transform=>point!=null?point:transform;
        public override bool IsAvailable=>owner!=null&&owner.Session.Security?.IsOccupied!=true;
        protected override float HighlightScale=>1;
        public override string GetInteractionLabel(FarmTool tool)=>"Mejorar pueblo";
        public override void Interact(FarmTool tool,PlayerInventory inventory)=>owner?.OpenVillage();
    }
}
