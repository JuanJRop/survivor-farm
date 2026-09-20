using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Core
{
    public enum PracticeMode { Combat, Farm }

    /// <summary>Scene-local practice lifecycle. Never reads or writes campaign progress.</summary>
    public sealed class PracticeSession : MonoBehaviour
    {
        public const string ArenaScene="ArenaCombate", FarmScene="TallerGranja";
        public const int PracticeArrowSupply = 96;
        [SerializeField] private PracticeMode mode;
        [SerializeField] private BasicEnemyAI[] legacyTemplates;
        public PracticeMode Mode=>mode;
        public static bool Active=>PortfolioSession.Instance!=null&&PortfolioSession.Instance.IsPractice;
        public PortfolioSession Session {get;private set;}
        public PracticeWaveDirector Waves {get;private set;}
        public bool Ready {get;private set;}
        public string Status {get;private set;}
        private PracticeHud hud;
        public void Configure(PortfolioSession owner)
        {
            Session=owner;
            var inventory=owner.Player;
            inventory.Restore(30,0,0,180,140,40,250,99,20);
            inventory.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);
            inventory.GetComponent<AdventureProgress>().AddIron(80);inventory.AddItem("GoldOre",30);
            inventory.AddEquipment("Bow");
            inventory.AddItem("Arrow", Mathf.Max(0, PracticeArrowSupply - inventory.GetItemCount("Arrow")));
            HideCampaignReadouts();
            if(mode==PracticeMode.Combat)
            {
                PracticeWorld.BuildArena(owner);
                Waves=gameObject.AddComponent<PracticeWaveDirector>();Waves.Configure(this,legacyTemplates);
                Status="Elige un encuentro o inicia el circuito de cinco oleadas.";
            }
            else
            {
                Status="Día sin límite · materiales de prueba · progreso independiente.";
                GoToStation(0);
            }
            hud=gameObject.AddComponent<PracticeHud>();hud.Configure(this);
            Ready=true;
        }
        private void HideCampaignReadouts()
        {
            var original=FindFirstObjectByType<OriginalSpriteHud>();if(original==null)return;
            if(original.Day!=null)original.Day.transform.parent.gameObject.SetActive(false);
            foreach(var item in original.GetComponentsInChildren<RectTransform>(true))
                if(item.name=="Mission"||item.name=="Diario [J]"||item.name=="Personaje [C]"||item.name=="HUD Settings")item.gameObject.SetActive(false);
        }
        public void Tick(float seconds)
        {
            if(!Ready)return;
            Waves?.Tick(seconds);
        }
        public void SetStatus(string text)=>Status=text;
        public void RefreshPause()=>hud?.RefreshPause();
        public void Heal()
        {
            var p=Session.Player;p.GetComponent<CombatTimeFeedback>()?.Cancel();
            p.GetComponent<PlayerCombatController>().CancelMelee();
            p.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);
        }
        public void Refill()
        {
            var p=Session.Player;
            p.AddWood(Mathf.Max(0,180-p.Wood));p.AddStone(Mathf.Max(0,140-p.Stone));
            p.AddCoins(Mathf.Max(0,250-p.Coins));p.AddFruit(Mathf.Max(0,40-p.Fruit));
            p.AddSeeds(Mathf.Max(0,30-p.CommonSeeds));p.AddFood(Mathf.Max(0,20-p.Food));
            // Rare materials are deliberately bounded too; this button is not an accumulating reward.
            var progress=p.GetComponent<AdventureProgress>();
            progress.AddIron(Mathf.Max(0,80-progress.Data.iron));
            p.AddItem("GoldOre",Mathf.Max(0,30-p.GetItemCount("GoldOre")));
            p.AddEquipment("Bow");
            p.AddItem("Arrow", Mathf.Max(0, PracticeArrowSupply - p.GetItemCount("Arrow")));
            Heal();Status="Suministros repuestos. Tus cultivos, equipo y maestrías se conservan.";
        }
        public void SetToolTier(int tier)
        {
            tier=Mathf.Clamp(tier,1,3);var p=Session.Player;
            p.GetComponent<PlayerToolUpgradeController>().Restore(tier,tier,tier);
            var crafting=p.GetComponent<PlayerCraftingController>();
            crafting.Restore(crafting.StorageLevel,crafting.CampLevel,false,true,crafting.MealsCooked,tier);
            var mastery=p.GetComponent<ToolMastery>();var records=mastery.Capture();
            foreach(var record in records)record.level=tier;
            mastery.Restore(records);Status="Equipo de práctica · nivel "+tier+". También puedes progresar por uso en K.";
        }
        public void GrowCrops()
        {
            if(mode!=PracticeMode.Farm)return;
            foreach(var plot in FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None))plot.AdvanceGrowth(60);
            Status="Crecimiento adelantado 60 s. Primero planta y riega con E.";
        }
        public void GoToStation(int station)
        {
            if(mode!=PracticeMode.Farm)return;
            Vector3[] positions={new Vector3(7,-3.5f),new Vector3(0,-11),new Vector3(-14,0),new Vector3(21,1),new Vector3(27,10),new Vector3(-3.8f,-3.6f)};
            Teleport(positions[Mathf.Clamp(station,0,positions.Length-1)]);
        }
        public void Teleport(Vector3 position)
        {
            Session.Player.GetComponent<ConstructionSystem>().Cancel();
            Session.Player.GetComponent<PlayerCombatController>().CancelMelee();
            Physics2D.SyncTransforms();
            bool Clear(Vector3 p)=>FarmExploration.Contains(p,.5f)&&!FarmExploration.IsRiver(p,.5f)&&
                !Physics2D.OverlapCircleAll(p,.45f).Any(c=>!c.isTrigger&&!c.transform.IsChildOf(Session.Player.transform));
            if(!Clear(position))
            {
                bool found=false;
                for(int ring=1;ring<=6&&!found;ring++)for(int direction=0;direction<8;direction++)
                {
                    float angle=direction*Mathf.PI/4;
                    var candidate=position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle))*ring*.65f;
                    if(!Clear(candidate))continue;position=candidate;found=true;break;
                }
                if(!found){Status="No hay espacio libre junto a esta estación. Despeja el acceso.";return;}
            }
            Session.Player.GetComponent<ValleyCampaign>().Teleport(position);
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetTarget(Session.Player.transform);
        }
        public void PlayerDefeated()
        {
            Waves?.Stop();Session.Raids.ResetPracticeBoss();Session.SetPracticePhase(false);
            Heal();Teleport(mode==PracticeMode.Combat?new Vector3(0,-2):new Vector3(7,-3.5f));
            Status="Has caído. Recuperado en el área segura: puedes repetir el encuentro.";
        }
        public void BossDefeated()=>Waves?.BossDefeated();
        public void ResetScene()=>Open(mode);
        public static void Open(PracticeMode destination)
        {
            if(PortfolioSession.Instance!=null)
            {
                var player=PortfolioSession.Instance.Player;
                if(player!=null){player.GetComponent<CombatTimeFeedback>()?.Cancel();player.GetComponent<ConstructionSystem>()?.Cancel();}
            }
            Time.timeScale=1;SceneManager.LoadScene(destination==PracticeMode.Combat?ArenaScene:FarmScene);
        }
        public void ReturnToTitle()
        {
            Session.Player.GetComponent<CombatTimeFeedback>()?.Cancel();
            Session.Player.GetComponent<ConstructionSystem>()?.Cancel();Session.ReturnToTitle();
        }
    }
}
