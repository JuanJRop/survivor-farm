using System;
using System.Collections;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>The only owner of the three-night session state. World, waves and HUD consume it.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PortfolioSession : MonoBehaviour
    {
        public static PortfolioSession Instance { get; private set; }
        public static bool Active => Instance != null && Instance.isActiveAndEnabled;
        [SerializeField] private SliceSettings settings;
        public SliceSettings Settings => settings;
        public PlayerInventory Player { get; private set; }
        public FarmDefense Core { get; private set; }
        public FarmRaidDirector Raids { get; private set; }
        public FarmRaidNavigation Navigation { get; } = new FarmRaidNavigation();
        public SlicePhase Phase { get; private set; } = SlicePhase.Introduction;
        public int Day { get; private set; } = 1;
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public bool HasBegun { get; private set; }
        public bool InCombat => Phase == SlicePhase.Night || Phase == SlicePhase.Boss;
        public bool CanSave => HasBegun && (Phase == SlicePhase.Day || Phase == SlicePhase.Preparation || Phase == SlicePhase.Dawn || Phase == SlicePhase.Victory);
        public bool IsPaused { get; private set; }
        public bool IsReady { get; private set; }
        public string QaSlot { get; private set; }
        public int Planted { get; private set; }
        public int Harvested { get; private set; }
        public int Trees { get; private set; }
        public int Rocks { get; private set; }
        public int Defenses { get; private set; }
        public string EndingReason { get; private set; }
        public event Action<SlicePhase> PhaseChanged;
        private DayNightCycle clock;
        private PlayerSurvivalStats stats;
        private GameSaveSystem saves;
        private SliceHud hud;
        private float phaseDuration;
        private bool repaired;

        public string Objective => !HasBegun ? "Tres noches para salvar la granja" :
            Phase == SlicePhase.Victory ? "La granja vuelve a respirar" :
            Phase == SlicePhase.Defeat ? EndingReason :
            Phase == SlicePhase.BossIntro || Phase == SlicePhase.Boss ? "Vence al Custodio · sal de las marcas, ataca al recuperarse" :
            Phase == SlicePhase.Night ? $"Defiende el pozo · enemigos {Raids.Defeated}/{Raids.Total}" :
            Phase == SlicePhase.Dawn ? "La noche ha terminado · nuevas defensas disponibles" :
            Day == 1 && Trees == 0 ? "Reúne madera al oeste · acércate al árbol y pulsa E" :
            Day == 1 && Rocks == 0 ? "Recoge piedra al este · E usa el pico automáticamente" :
            Day == 1 && Planted < 3 ? $"Planta y riega 3 cultivos al sureste · {Planted}/3" :
            Day == 1 && !repaired ? "Repara la barricada dañada al sur · E, 2 madera" :
            Day == 1 && Player.GetComponent<PlayerCraftingController>().MealsCooked == 0 ? "Cosecha fruta y cocina una ración · F" :
            Day == 2 ? "Combina trampas y barricadas · los demoledores buscan el pozo" :
            Day == 3 ? "El Custodio viene por el Corazón bajo el pozo · prepara tu defensa final" :
            "Construye barricadas y guarda raciones · Q cura, Espacio esquiva";

        private void Awake()
        {
            Instance=this;
            QaSlot="qa_"+Guid.NewGuid().ToString("N");
            if(settings==null)settings=ScriptableObject.CreateInstance<SliceSettings>();
        }
        private IEnumerator Start()
        {
            // Let the existing scene and ValleyCampaign finish their ordinary initialization.
            yield return null;
            Player=FindFirstObjectByType<PlayerInventory>();
            if(Player==null){Debug.LogError("Portfolio mode requires the authored Main player.");yield break;}
            stats=Player.GetComponent<PlayerSurvivalStats>();
            clock=FindFirstObjectByType<DayNightCycle>();
            saves=FindFirstObjectByType<GameSaveSystem>();
            Core=PortfolioFarmSetup.Configure(this);
            Raids=gameObject.AddComponent<FarmRaidDirector>();Raids.Configure(this);
            hud=gameObject.AddComponent<SliceHud>();hud.Configure(this);
            gameObject.AddComponent<FarmSoundscape>().Configure(this);
            IsReady=true;
            SetPhase(SlicePhase.Introduction,0);
            Time.timeScale=0;
            FarmGameEvents.TreeHarvested+=OnTree;FarmGameEvents.RockHarvested+=OnRock;
            FarmGameEvents.SeedPlanted+=OnPlant;FarmGameEvents.CropHarvested+=OnHarvest;
        }
        public bool CanBuild(string kind) => kind != "Trap" && kind != "Turret" || Day >= 2;
        public static bool IsDemoRecipe(string id) => id=="Food"||id=="Sword"||id=="Fence"||id=="Trap"||id=="Turret";
        public void BeginNewGame()
        {
            if(!IsReady||HasBegun)return;
            if(!GameSaveSystem.IsQa)
            {
                PlayerPrefs.SetString("SurvivorFarmPortfolioSlot",DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                PlayerPrefs.Save();
            }
            Player.Restore(6,0,0,8,6,0,0,99,2);
            Player.RestorePacked(null);Player.RestoreItemStacks(null);
            Player.RestoreEquipment(new[]{"Sword","Bow"},new[]{"","","","Sword","","","",""});
            stats.Restore(5,5,1);
            HasBegun=true;Day=1;Elapsed=0;Time.timeScale=1;
            SetPhase(SlicePhase.Day,settings.days[0].preparationSeconds);
            saves?.SaveGame(false);
        }
        public bool ContinueGame()
        {
            if(!IsReady||HasBegun||saves==null)return false;
            if(!saves.TryLoadGame())return false;
            return HasBegun;
        }
        public void Pause(bool pause)
        {
            if(!HasBegun||Phase==SlicePhase.Victory||Phase==SlicePhase.Defeat)return;
            IsPaused=pause;Time.timeScale=pause?0:1;
            if(pause)Player.GetComponent<PlayerMovementController>()?.StopMovement();
            hud.RefreshOverlay();
        }
        public void PrepareNow()
        {
            if(Phase!=SlicePhase.Day||!HasBegun)return;
            SetPhase(SlicePhase.Preparation,settings.duskSeconds);
        }
        public void RegisterDefense(){Defenses++;Navigation.Invalidate();}
        public void RegisterRepair()=>repaired=true;
        private void OnTree()=>Trees++;
        private void OnRock()=>Rocks++;
        private void OnPlant()=>Planted++;
        private void OnHarvest()=>Harvested++;

        private void Update()
        {
            if(!IsReady||!HasBegun)return;
            if(Input.GetKeyDown(KeyCode.Escape)&&!InventoryPanelSystem.IsOpen)Pause(!IsPaused);
            if(IsPaused||Phase==SlicePhase.Victory||Phase==SlicePhase.Defeat)return;
            if(stats.CurrentHealth<=0){Lose("Has caído defendiendo la granja.");return;}
            if(!InventoryPanelSystem.IsOpen)
            {
                if(Input.GetKeyDown(KeyCode.Q))BackpackActions.Use(Player,"Food");
                if(Input.GetKeyDown(KeyCode.Z))Player.GetComponent<ConstructionSystem>().Begin("Fence");
                if(Input.GetKeyDown(KeyCode.X))Player.GetComponent<ConstructionSystem>().Begin("Trap");
                if(Input.GetKeyDown(KeyCode.C))Player.GetComponent<ConstructionSystem>().Begin("Turret");
            }
            Advance(Time.deltaTime);
        }
        public void Advance(float seconds)
        {
            if(!HasBegun||IsPaused||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds)||Phase==SlicePhase.Victory||Phase==SlicePhase.Defeat)return;
            Elapsed+=seconds;Remaining=Mathf.Max(0,Remaining-seconds);
            float progress=phaseDuration>0?1-Remaining/phaseDuration:1;
            if(Phase==SlicePhase.Day)clock?.Restore(Day,Mathf.Lerp(8,17,progress));
            if(Phase==SlicePhase.Preparation)clock?.Restore(Day,Mathf.Lerp(17,21,progress));
            if(Phase==SlicePhase.Night)
            {
                clock?.Restore(Day,22);
                Raids.Tick(seconds);
                if(Remaining<=0&&Raids.Complete)
                {
                    if(Day==3)SetPhase(SlicePhase.BossIntro,5);
                    else SetPhase(SlicePhase.Dawn,8);
                }
                return;
            }
            if(Phase==SlicePhase.Boss)return;
            if(Remaining>0)return;
            switch(Phase)
            {
                case SlicePhase.Day: SetPhase(SlicePhase.Preparation,settings.duskSeconds);break;
                case SlicePhase.Preparation: SetPhase(SlicePhase.Night,settings.days[Day-1].nightSeconds);break;
                case SlicePhase.Dawn:
                    Day++;stats.Heal(2);Player.AddSeeds(Mathf.Max(0,6-Player.CommonSeeds));
                    Player.GetComponent<AdventureProgress>().AddIron(4);
                    SetPhase(SlicePhase.Day,settings.days[Day-1].preparationSeconds);
                    saves?.SaveGame(false);break;
                case SlicePhase.BossIntro: SetPhase(SlicePhase.Boss,0);break;
            }
        }
        private void SetPhase(SlicePhase phase,float duration)
        {
            Phase=phase;Remaining=phaseDuration=duration;
            if(phase==SlicePhase.Night)Raids.Begin(Day);
            if(phase==SlicePhase.Dawn){Raids.Stop();clock?.Restore(Day+1,6);}
            if(phase==SlicePhase.BossIntro){Raids.Stop();Raids.IntroduceBoss();}
            if(phase==SlicePhase.Boss)Raids.ActivateBoss();
            if(phase==SlicePhase.Day)clock?.Restore(Day,8);
            PhaseChanged?.Invoke(phase);
            hud?.RefreshOverlay();
        }
        public void Win()
        {
            if(Phase!=SlicePhase.Boss)return;
            SetPhase(SlicePhase.Victory,0);Raids.Stop();clock?.Restore(4,8);
            Player.GetComponent<PlayerMovementController>()?.StopMovement();
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            saves?.SaveGame(false);Time.timeScale=0;
        }
        public void Lose(string reason)
        {
            if(!HasBegun||Phase==SlicePhase.Defeat||Phase==SlicePhase.Victory)return;
            EndingReason=reason;SetPhase(SlicePhase.Defeat,0);Raids.Stop();
            Player.GetComponent<ConstructionSystem>()?.Cancel();
            Player.GetComponent<PlayerMovementController>()?.StopMovement();Time.timeScale=0;
        }
        public SliceSnapshot Capture() => new SliceSnapshot {day=Day,coreHealth=Core.Health,elapsed=Elapsed,
            planted=Planted,harvested=Harvested,trees=Trees,rocks=Rocks,defenses=Defenses,repaired=repaired,completed=Phase==SlicePhase.Victory};
        public void Restore(SliceSnapshot snapshot)
        {
            if(snapshot==null||!IsReady)return;
            Day=Mathf.Clamp(snapshot.day,1,3);Elapsed=snapshot.elapsed;
            Planted=snapshot.planted;Harvested=snapshot.harvested;Trees=snapshot.trees;Rocks=snapshot.rocks;Defenses=snapshot.defenses;
            repaired=snapshot.repaired||Day>1||Defenses>0;Core.RestoreHealth(snapshot.coreHealth);
            HasBegun=true;IsPaused=false;Time.timeScale=1;Raids.Stop();
            SetPhase(snapshot.completed?SlicePhase.Victory:SlicePhase.Day,settings.days[Day-1].preparationSeconds);
            if(snapshot.completed)Time.timeScale=0;
        }
        public void ReturnToTitle(){Time.timeScale=1;SceneManager.LoadScene("Main");}
        public void Quit(){if(CanSave)saves?.SaveGame(false);Application.Quit();}
        private void OnDestroy()
        {
            FarmGameEvents.TreeHarvested-=OnTree;FarmGameEvents.RockHarvested-=OnRock;
            FarmGameEvents.SeedPlanted-=OnPlant;FarmGameEvents.CropHarvested-=OnHarvest;
            if(Instance==this){Instance=null;Time.timeScale=1;}
        }
    }
}
