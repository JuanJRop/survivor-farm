using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class IronVein : WorldInteractable
    {
        public int Index;
        public bool Precious;
        public bool IsMining=>mining;
        private AdventureProgress dailyProgress;
        private DayNightCycle clock;
        private SpriteRenderer[] art;
        private Color[] colors;
        private bool depletedVisual;
        private PlayerCharacterAnimator activeAnimator;
        private int animationVersion;
        private World.WorldActionClock workClock;
        public bool IsDepleted=>dailyProgress!=null&&Index>=0&&Index<dailyProgress.Data.minedDays.Length&&dailyProgress.Data.minedDays[Index]>=(clock!=null?clock.Day:1);
        void Start()
        {
            dailyProgress=FindFirstObjectByType<AdventureProgress>();clock=FindFirstObjectByType<DayNightCycle>();
            art=GetComponentsInChildren<SpriteRenderer>();colors=new Color[art.Length];for(int i=0;i<art.Length;i++)colors[i]=art[i].color;
        }
        bool mining;PlayerInventory activeInventory;AdventureProgress activeProgress;int activeDay,activePickaxeLevel,completedHits;float mineStartedAt,mineEndsAt,nextHitAt;
        const float MineDuration=2.8f;const int MineHits=5;
        public override string GetInteractionLabel(FarmTool tool)=>IsDepleted?"Veta agotada · vuelve mañana":mining?"Picando veta":PortfolioSession.Active?(Precious?"Veta Nv.3 · E extraer":"Veta Nv.2 · E extraer"):"Veta rica: hierro, oro y gemas (pico Nv.2, se renueva al día siguiente)";
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(mining)return;
            if(!CanMine(tool,inventory,out var progress,out var day,out var pickaxeLevel))return;
            PlayerCharacterAnimator animator=inventory.GetComponent<PlayerCharacterAnimator>();
            if(animator!=null&&Application.isPlaying){BeginMining(inventory,progress,day,pickaxeLevel,animator);return;}
            animator?.PlayAction("Pickaxe",0,transform.position);
            CompleteMine(inventory,progress,day,pickaxeLevel);
        }
        bool CanMine(FarmTool tool,PlayerInventory inventory,out AdventureProgress progress,out int day,out int pickaxeLevel)
        {
            progress=inventory!=null?inventory.GetComponent<AdventureProgress>():null;var clock=FindFirstObjectByType<DayNightCycle>();day=clock!=null?clock.Day:1;
            pickaxeLevel=inventory!=null&&inventory.GetComponent<PlayerToolUpgradeController>()!=null?inventory.GetComponent<PlayerToolUpgradeController>().PickaxeLevel:0;
            if(progress==null)return false;
            int required=Precious?3:2;
            if(PortfolioSession.Active&&pickaxeLevel<required){FarmNotificationCenter.Show($"Veta Nv.{required} · desbloquea y compra el pico en Maestrías [K].");return false;}
            if(tool!=FarmTool.Pickaxe||!PortfolioSession.Active&&pickaxeLevel<2){FarmNotificationCenter.Show(PortfolioSession.Active?"Equipa el pico para extraer el mineral.":"Mejora el pico a nivel 2 en el taller F.");return false;}
            if(Vector2.Distance(inventory.transform.position,transform.position)>1.6f)return false;
            if(Index<0||Index>=progress.Data.minedDays.Length){FarmNotificationCenter.Show("Esta veta todavía no está registrada.");return false;}
            if(progress.Data.minedDays[Index]>=day){FarmNotificationCenter.Show("Esta veta se recupera mañana.");return false;}
            return true;
        }
        void BeginMining(PlayerInventory inventory,AdventureProgress progress,int day,int pickaxeLevel,PlayerCharacterAnimator animator)
        {
            activeInventory=inventory;activeProgress=progress;activeDay=day;activePickaxeLevel=pickaxeLevel;completedHits=0;mineStartedAt=Time.time;mineEndsAt=Time.time+MineDuration;nextHitAt=mineStartedAt+MineDuration/(MineHits+1f);mining=true;
            animator.PlayAction("Pickaxe",MineDuration,transform.position);
            activeAnimator=animator;animationVersion=animator.ActionVersion;
            workClock=World.WorldActionClock.For(inventory.gameObject);workClock.Show(this,0);
        }
        void Update()
        {
            if(art!=null&&depletedVisual!=IsDepleted)
            {
                depletedVisual=IsDepleted;
                for(int i=0;i<art.Length;i++)if(art[i]!=null)art[i].color=depletedVisual?colors[i]*new Color(.5f,.55f,.6f,1):colors[i];
            }
            if(!mining)return;
            var stats=activeInventory!=null?activeInventory.GetComponent<PlayerSurvivalStats>():null;
            if(activeInventory==null||activeProgress==null||stats!=null&&stats.CurrentHealth<=0||Vector2.Distance(activeInventory.transform.position,transform.position)>1.8f||activeAnimator!=null&&activeAnimator.ActionVersion!=animationVersion){ClearMining();return;}
            while(Time.time>=nextHitAt&&completedHits<MineHits)
            {
                completedHits++;VisibleHitFeedback.Play(gameObject,.08f,false);
                activeInventory.GetComponent<AudioFeedback>()?.Play(CombatSound.Mine,transform.position);
                CombatHitParticles.Spawn(transform.position,transform.parent,ImpactSurface.Stone,false,false);
                nextHitAt=mineStartedAt+MineDuration*(completedHits+1f)/(MineHits+1f);
            }
            workClock?.Show(this,Progress);
            if(Time.time<mineEndsAt)return;
            var inventory=activeInventory;var progress=activeProgress;int day=activeDay,pickaxeLevel=activePickaxeLevel;ClearMining();
            CompleteMine(inventory,progress,day,pickaxeLevel);
        }
        void CompleteMine(PlayerInventory inventory,AdventureProgress progress,int day,int pickaxeLevel)
        {
            if(Index<0||Index>=progress.Data.minedDays.Length||progress.Data.minedDays[Index]>=day)return;
            progress.Data.minedDays[Index]=day;progress.AddIron(3);
            inventory.GetComponent<ToolMastery>()?.Earn(FarmTool.Pickaxe,Precious?3:2);
            string rareDrop;
            if(PortfolioSession.Active){if(Precious)inventory.AddItem("GoldOre",2);rareDrop=Precious?"+2 oro mineral":"";}
            else rareDrop=GrantRareDrop(inventory,pickaxeLevel);
            inventory.GetComponent<GameFeelFeedback>()?.Pulse(string.IsNullOrEmpty(rareDrop)?"+3 hierro":"+3 hierro "+rareDrop,transform.position);
            FarmNotificationCenter.Show(PortfolioSession.Active?(Precious?"+3 hierro · +2 oro mineral. Listo para muros reforzados.":"+3 hierro. Hay oro en los salientes más alejados. Las vetas se recuperan mañana."):string.IsNullOrEmpty(rareDrop)?"+3 hierro. Úsalo para armaduras, armas y construcciones.":"+3 hierro y "+rareDrop+". Material para equipo avanzado.");
        }
        void ClearMining(){workClock?.Hide(this);mining=false;activeInventory=null;activeProgress=null;activeAnimator=null;activeDay=0;activePickaxeLevel=0;completedHits=0;mineStartedAt=0;mineEndsAt=0;nextHitAt=0;}
        protected override void OnDisable(){ClearMining();base.OnDisable();}
        float Progress=>mining?Mathf.Clamp01((Time.time-mineStartedAt)/MineDuration):1f;
        static string GrantRareDrop(PlayerInventory inventory,int pickaxeLevel)
        {
            float roll=UnityEngine.Random.value;
            string id=null;
            if(pickaxeLevel>=3)
            {
                id=roll<.06f?"Diamond":roll<.16f?"Emerald":roll<.28f?"Ruby":roll<.58f?"GoldOre":null;
            }
            else
            {
                id=roll<.22f?"GoldOre":roll<.30f?"RubyShard":roll<.38f?"EmeraldShard":null;
            }
            if(string.IsNullOrEmpty(id))return string.Empty;
            inventory.AddItem(id,1);
            var item=SurvivalItemCatalog.Find(id);
            return "+1 "+(item!=null?item.Name:id);
        }
    }
}
