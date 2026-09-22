using System;
using System.Collections;
using System.Linq;
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
        public VillageSecurity Security { get; private set; }
        public VillageAdventure Adventure { get; private set; }
        public VillageProgression Progression { get; private set; }
        public FarmRaidNavigation Navigation { get; } = new FarmRaidNavigation();
        public SlicePhase Phase { get; private set; } = SlicePhase.Introduction;
        public int Day { get; private set; } = 1;
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public bool HasBegun { get; private set; }
        public bool InCombat => Security?.IsOccupied == true || Phase == SlicePhase.Night || Phase == SlicePhase.Boss;
        public bool CanSave => !IsPractice && HasBegun && Adventure?.IsInsideDungeon != true && Security?.IsOccupied != true && (Phase == SlicePhase.Day || Phase == SlicePhase.Preparation || Phase == SlicePhase.Dawn || Phase == SlicePhase.Victory);
        public bool IsPaused { get; private set; }
        public bool IsReady { get; private set; }
        public PracticeSession Practice => GetComponent<PracticeSession>();
        public bool IsPractice => Practice != null;
        public string QaSlot { get; private set; }
        public void SelectQaSave(string slot){if(GameSaveSystem.IsQa&&!HasBegun)QaSlot=slot;}
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

        public string Objective => !HasBegun ? "Explora el valle y protege a sus habitantes" :
            Phase == SlicePhase.Victory ? "El pueblo vuelve a respirar" :
            Phase == SlicePhase.Defeat ? EndingReason :
            Security?.IsOccupied == true ? (Security.GarrisonRemaining > 0 ? $"PUEBLO OCUPADO · vence a {Security.GarrisonRemaining} invasores" : "Repara una casa y libera el pueblo en el pozo · E") :
            Adventure?.IsInsideDungeon == true ? Adventure.Objective :
            Phase == SlicePhase.BossIntro || Phase == SlicePhase.Boss ? "Vence al Custodio · sal de las marcas, ataca al recuperarse" :
            Phase == SlicePhase.Night ? $"Protege casas y vecinos · invasores {Raids.Defeated}/{Raids.Total}" :
            Phase == SlicePhase.Preparation ? "¡Se acercan monstruos! Regresa al pueblo antes del anochecer" :
            Phase == SlicePhase.Dawn ? "Pueblo a salvo · auxilia a los vecinos y repara las casas con E" :
            Security != null && Security.Percent < 65 ? "El pueblo necesita ayuda · E repara casas y atiende a los vecinos" :
            Adventure != null ? Adventure.Objective : "Explora, consigue raciones y vuelve para proteger el pueblo";

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
            if(!IsPractice)
            {
                Security=gameObject.AddComponent<VillageSecurity>();Security.Configure(this);
                Security.OccupationStarted+=OnOccupation;Security.VillageLiberated+=OnLiberated;
                Adventure=gameObject.AddComponent<VillageAdventure>();Adventure.Configure(this,Player.GetComponent<ValleyCampaign>().World);
                Adventure.ProgressChanged+=SaveExplorationProgress;
                Progression=gameObject.AddComponent<VillageProgression>();Progression.Configure(this);
            }
            if(!IsPractice){hud=gameObject.AddComponent<SliceHud>();hud.Configure(this);}
            gameObject.AddComponent<FarmSoundscape>().Configure(this);
            IsReady=true;
            SetPhase(SlicePhase.Introduction,0);
            Time.timeScale=0;
            FarmGameEvents.TreeHarvested+=OnTree;FarmGameEvents.RockHarvested+=OnRock;
            FarmGameEvents.SeedPlanted+=OnPlant;FarmGameEvents.CropHarvested+=OnHarvest;
            if(IsPractice)
            {
                HasBegun=true;Day=3;Time.timeScale=1;
                SetPracticePhase(false);
                Practice.Configure(this);
            }
        }
        public void SetPracticePhase(bool combat,bool boss=false)
        {
            if(!IsPractice)return;
            Phase=combat?(boss?SlicePhase.Boss:SlicePhase.Night):SlicePhase.Day;
            Remaining=0;clock?.Restore(3,combat?20:10);PhaseChanged?.Invoke(Phase);
        }
        public bool CanBuild(string kind) => !FortressPieces.IsWall(kind) && kind != "Trap" && kind != "Turret";
        public static bool IsDemoRecipe(string id) => id=="Food"||id=="Sword"||id=="Arrow"||id=="Saddle";
        public void BeginNewGame()
        {
            if(!IsReady||HasBegun||IsPractice)return;
            if(!GameSaveSystem.IsQa)
            {
                PlayerPrefs.SetString("SurvivorFarmPortfolioSlot",DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                PlayerPrefs.Save();
            }
            Player.Restore(6,0,0,8,6,0,0,99,2);
            Player.RestorePacked(null);Player.RestoreItemStacks(null);
            Player.RestoreEquipment(new[]{"Sword"},new[]{"","","","Sword","","","",""});
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
            Player.GetComponent<CombatTimeFeedback>()?.Cancel();
            if(pause)Player.GetComponent<PlayerCombatController>()?.CancelMelee();
            IsPaused=pause;Time.timeScale=pause?0:1;
            if(pause)Player.GetComponent<PlayerMovementController>()?.StopMovement();
            hud?.RefreshOverlay();
            Practice?.RefreshPause();
        }
        public void PrepareNow()
        {
            if(IsPractice||Phase!=SlicePhase.Day||!HasBegun||Adventure?.IsInsideDungeon==true||Security?.IsOccupied==true)return;
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
            // GameMenuWindow owns Escape and pause across all progression pages.
            if(IsPaused||Phase==SlicePhase.Victory||Phase==SlicePhase.Defeat)return;
            if(stats.CurrentHealth<=0){Lose("Has caído. El pueblo necesita a su defensor.");return;}
            if(!InventoryPanelSystem.IsOpen&&!FarmIntroduction.IsOpen)
            {
                if(Input.GetKeyDown(KeyCode.Q))BackpackActions.Use(Player,"Food");
            }
            if(IsPractice)Practice.Tick(Time.deltaTime);else Advance(Time.deltaTime);
        }
        public void Advance(float seconds)
        {
            if(IsPractice||!HasBegun||IsPaused||FarmIntroduction.IsOpen||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds)||Phase==SlicePhase.Victory||Phase==SlicePhase.Defeat)return;
            if(Security?.IsOccupied==true)return;
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
            if(phase==SlicePhase.Preparation)FarmNotificationCenter.Show("¡Regresa al pueblo! Los monstruos atacarán las casas y a los vecinos.");
            PhaseChanged?.Invoke(phase);
            hud?.RefreshOverlay();
        }
        public void Win()
        {
            if(IsPractice){Practice.BossDefeated();return;}
            if(Phase!=SlicePhase.Boss||Security?.IsOccupied==true)return;
            SetPhase(SlicePhase.Victory,0);Raids.Stop();clock?.Restore(4,8);
            Player.GetComponent<PlayerMovementController>()?.StopMovement();
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            saves?.SaveGame(false);Time.timeScale=0;
        }
        public void Lose(string reason)
        {
            if(IsPractice){Practice.PlayerDefeated();return;}
            if(!HasBegun||Phase==SlicePhase.Defeat||Phase==SlicePhase.Victory)return;
            EndingReason=reason;SetPhase(SlicePhase.Defeat,0);Raids.Stop();
            Player.GetComponent<ConstructionSystem>()?.Cancel();
            Player.GetComponent<PlayerMovementController>()?.StopMovement();Time.timeScale=0;
        }
        public SliceSnapshot Capture() => new SliceSnapshot {day=Day,coreHealth=Core.Health,elapsed=Elapsed,
            security=Security?.Capture(),adventure=Adventure?.Capture(),villageProgression=Progression?.Capture(),
            mastery=Player.GetComponent<ToolMastery>()?.Capture(),
            petPurchased=Player.GetComponent<PetAdoption>()?.Owned==true,
            houses=FindObjectsByType<VillageHouseHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(h=>h.Capture()).ToArray(),
            foragedPlants=FindObjectsByType<ForagePlant>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.Collected).Select(p=>p.Id).ToArray(),
            residents=FindObjectsByType<VillageResidentHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.GetComponent<VillageGuard>()==null).Select(r=>r.Capture()).ToArray(),
            planted=Planted,harvested=Harvested,trees=Trees,rocks=Rocks,defenses=Defenses,repaired=repaired,completed=Phase==SlicePhase.Victory};
        public void Restore(SliceSnapshot snapshot)
        {
            if(snapshot==null||!IsReady)return;
            Day=Mathf.Clamp(snapshot.day,1,3);Elapsed=snapshot.elapsed;
            Planted=snapshot.planted;Harvested=snapshot.harvested;Trees=snapshot.trees;Rocks=snapshot.rocks;Defenses=snapshot.defenses;
            repaired=snapshot.repaired||Day>1||Defenses>0;
            Player.GetComponent<ToolMastery>()?.Restore(snapshot.mastery);
            Player.GetComponent<PetAdoption>()?.Restore(snapshot.petPurchased);
            Player.GetComponent<PlayerMountController>()?.ForceDismount();
            Progression?.Restore(snapshot.villageProgression);
            foreach(var house in FindObjectsByType<VillageHouseHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))house.Restore(snapshot.houses?.FirstOrDefault(h=>h.id==house.Id));
            foreach(var plant in FindObjectsByType<ForagePlant>(FindObjectsInactive.Include,FindObjectsSortMode.None))plant.Restore(snapshot.foragedPlants?.Contains(plant.Id)==true);
            Core.ConfigureCore(Player,settings.coreHealth);
            foreach(var resident in FindObjectsByType<VillageResidentHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(resident.GetComponent<VillageGuard>()!=null)continue;
                var state=snapshot.residents?.FirstOrDefault(r=>r!=null&&r.id==resident.Id);
                resident.Restore(state);
            }
            Core.RestoreHealth(snapshot.coreHealth);
            HasBegun=true;IsPaused=false;Time.timeScale=1;Raids.Stop();
            SetPhase(snapshot.completed?SlicePhase.Victory:SlicePhase.Day,settings.days[Day-1].preparationSeconds);
            Adventure?.Restore(snapshot.adventure);
            Security?.Restore(snapshot.security);
            if(snapshot.completed)Time.timeScale=0;
        }
        public void ReturnToTitle(){Time.timeScale=1;SceneManager.LoadScene("Main");}
        private void SaveExplorationProgress(){if(CanSave)saves?.SaveGame(false);}
        private void OnOccupation()
        {
            Adventure?.ExitDungeon();
            Raids.Stop();
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(null);
            hud?.RefreshOverlay();
        }
        private void OnLiberated()
        {
            Raids.Stop();stats.Heal(2);
            SetPhase(SlicePhase.Day,settings.days[Day-1].preparationSeconds);
            saves?.SaveGame(false);
        }
        public void Quit(){if(CanSave)saves?.SaveGame(false);Application.Quit();}
        private void OnDestroy()
        {
            FarmGameEvents.TreeHarvested-=OnTree;FarmGameEvents.RockHarvested-=OnRock;
            FarmGameEvents.SeedPlanted-=OnPlant;FarmGameEvents.CropHarvested-=OnHarvest;
            if(Instance==this){Instance=null;Time.timeScale=1;}
        }
    }
}
