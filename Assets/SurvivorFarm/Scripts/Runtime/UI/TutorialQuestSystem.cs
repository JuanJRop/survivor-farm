using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class TutorialQuestSystem : MonoBehaviour
    {
        [SerializeField] private Text questText;
        private const int RetiredSteps = 124; // Preserve bit positions in existing saves.
        private int completedSteps = RetiredSteps;
        private int rewardedSteps = RetiredSteps;
        private PlayerInventory inventory;
        public int RewardedSteps => rewardedSteps;
        private int firstNightDay;
        private float nextRefresh;
        private DayNightCycle clock;
        private PlayerCraftingController crafting;
        private const int StepCount = 12;
        private const int QuestMask = (1 << StepCount) - 1;
        public int CompletedSteps => completedSteps;
        public int FirstNightDay => firstNightDay;
        public int QuestIndex { get { for(int i=0;i<StepCount;i++) if((completedSteps&(1<<i))==0)return i; return StepCount; } }
        public int QuestProgress => 0;
        public bool Complete => QuestIndex == StepCount;
        private static readonly string[] Objectives = {
            "Interactúa con un árbol para reunir madera", "Interactúa con una roca para reunir piedra",
            "", "", "", "", "", "Fabrica y coloca una fogata", "Cocina una ración en el taller",
            "Construye y coloca una cama", "Sobrevive hasta poder dormir", "Duerme y empieza el día 2" };
        private static readonly string[] Rewards = {
            "+2 madera", "+2 piedra", "", "", "", "",
            "", "", "", "", "", "+10 oro" };
        private void OnEnable() { Unsubscribe(); Subscribe(); Refresh(); }
        private void OnDisable() => Unsubscribe();
        public void Configure(Text text) { questText=text; Refresh(); }
        public void Restore(int savedQuestIndex,int savedQuestProgress,int mask=-1,int nightDay=0,int savedRewardedSteps=-1)
        {
            completedSteps=(mask>=0?mask&QuestMask:(1<<Mathf.Clamp(savedQuestIndex,0,StepCount))-1)|RetiredSteps;
            rewardedSteps=savedRewardedSteps<0?completedSteps:(savedRewardedSteps|completedSteps)&QuestMask;
            firstNightDay=Mathf.Max(0,nightDay); Refresh();
        }
        private void Subscribe()
        {
            FarmGameEvents.TreeHarvested+=Tree; FarmGameEvents.RockHarvested+=Rock;
            FarmGameEvents.EnemyDefeated+=Enemy;
            FarmGameEvents.SleptUntilMorning+=Slept;
        }
        private void Unsubscribe()
        {
            FarmGameEvents.TreeHarvested-=Tree; FarmGameEvents.RockHarvested-=Rock;
            FarmGameEvents.EnemyDefeated-=Enemy;
            FarmGameEvents.SleptUntilMorning-=Slept;
        }
        private void Tree()=>Mark(0);private void Rock()=>Mark(1);
        private void Enemy()
        {
            if(clock==null)clock=FindFirstObjectByType<DayNightCycle>();
            if(clock!=null&&clock.IsNight){firstNightDay=clock.Day;Mark(10);}
        }
        private void Slept()
        {
            if(clock==null)clock=FindFirstObjectByType<DayNightCycle>();
            if(firstNightDay==0&&clock!=null)firstNightDay=Mathf.Max(1,clock.Day-1);
            bool changed = Mark(10, false);
            changed |= Mark(11, false);
            if(changed)FarmNotificationCenter.Show("Día 1 completado. Amaneció y el camino al campamento puede abrirse.");
        }
        private bool Mark(int step, bool notify = true)
        {
            if(step<0||step>=StepCount||(completedSteps&(1<<step))!=0)return false;
            completedSteps|=1<<step;Refresh();
            string reward = GrantReward(step);
            if(notify)FarmNotificationCenter.Show((step==11?"¡Sobreviviste a tu primera noche!":"Objetivo completado: "+Objectives[step]+".")+
                (string.IsNullOrEmpty(reward)?"":" "+reward));
            return true;
        }
        private string GrantReward(int step)
        {
            if((rewardedSteps&(1<<step))!=0)return string.Empty;
            rewardedSteps|=1<<step;
            if(inventory==null)inventory=FindFirstObjectByType<PlayerInventory>();
            if(inventory==null)return string.Empty;
            switch(step)
            {
                case 0: inventory.AddWood(2);break;
                case 1: inventory.AddStone(2);break;
                case 11: inventory.AddCoins(10);break;
            }
            return Rewards[step];
        }
        public void NotifyDeath(){if(!Complete){completedSteps &= ~(1<<10);completedSteps &= ~(1<<11);firstNightDay=0;Refresh();}}
        public void Evaluate()
        {
            if(clock==null)clock=FindFirstObjectByType<DayNightCycle>();
            if(crafting==null)crafting=FindFirstObjectByType<PlayerCraftingController>();
            if(crafting!=null)
            {
                if(crafting.CampfireBuilt)Mark(7);
                if(crafting.MealsCooked>0)Mark(8);
                if(crafting.BedBuilt)Mark(9);
                var stats=crafting.GetComponent<PlayerSurvivalStats>();
                if(crafting.BedBuilt&&clock!=null&&clock.IsNight){if(firstNightDay==0)firstNightDay=clock.Day;Mark(10);}
                if(crafting.BedBuilt&&clock!=null&&!clock.IsNight&&clock.Day>1&&firstNightDay==0){firstNightDay=clock.Day-1;Mark(10,false);}
                if((completedSteps&(1<<10))!=0&&clock!=null&&!clock.IsNight&&clock.Day>firstNightDay&&stats!=null&&stats.CurrentHealth>0)Mark(11);
            }
        }
        private void Update(){if(Time.time<nextRefresh)return;nextRefresh=Time.time+.5f;Evaluate();Refresh();}
        private void Refresh()
        {
            var valley=FindFirstObjectByType<ValleyCampaign>();
            if(valley!=null&&questText!=null)
            {
                var panel=questText.transform.parent as RectTransform;
                if(panel!=null && panel.name=="Mission")
                {
                    panel.sizeDelta=new Vector2(284,76);
                    var image=panel.GetComponent<Image>();if(image!=null)FarmUiStyle.Frame(image);
                    var rect=questText.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(228,58);rect.anchoredPosition=new Vector2(44,-10);
                    FarmUiStyle.Text(questText,14);questText.alignment=TextAnchor.UpperLeft;questText.supportRichText=true;
                    questText.horizontalOverflow=HorizontalWrapMode.Wrap;questText.verticalOverflow=VerticalWrapMode.Truncate;
                    EnsureOutline(questText);
                    var icon=panel.Find("Quest Icon")?.GetComponent<Image>();
                    if(icon!=null){var iconRect=icon.rectTransform;iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(0,1);iconRect.anchoredPosition=new Vector2(12,-12);iconRect.sizeDelta=new Vector2(24,24);icon.color=Color.white;string objective=valley.Objective.ToLowerInvariant();string key=objective.Contains("árbol")||objective.Contains("arbol")||objective.Contains("madera")?"Axe":objective.Contains("roca")||objective.Contains("piedra")||objective.Contains("derrumbe")?"Pickaxe":objective.Contains("fogata")?"Campfire":objective.Contains("ración")||objective.Contains("comida")||objective.Contains("come")?"Food":objective.Contains("cama")||objective.Contains("dorm")?"Bed":"Quest";icon.sprite=Resources.Load<Sprite>("BackpackIcons/"+key)??icon.sprite;}
                }
                bool combatTracked=CampCombatQuests.Tracked(valley.Data)!=null;
                var dungeon=FindFirstObjectByType<DungeonExpedition>();
                if(dungeon!=null&&dungeon.IsPresent)
                {
                    var icon=questText.transform.parent.Find("Quest Icon")?.GetComponent<Image>();
                    if(icon!=null)icon.sprite=Resources.Load<Sprite>("BackpackIcons/Sword")??icon.sprite;
                    questText.text="<color=#ffca6a><b>RUINAS DE RAIZCLARA</b></color>\n"+dungeon.Objective;
                    return;
                }
                if(combatTracked){var icon=questText.transform.parent.Find("Quest Icon")?.GetComponent<Image>();if(icon!=null)icon.sprite=Resources.Load<Sprite>("BackpackIcons/Sword")??icon.sprite;}
                questText.text="<color=#ffca6a><b>"+(combatTracked?"DEFENSA DE RA\u00cdZCLARA":valley.Chapter==0?"RA\u00cdZCLARA":"EXPEDICI\u00d3N")+"</b></color>\n"+valley.HudObjective;
                return;
            }
            if(questText!=null)questText.text=Complete?(FindFirstObjectByType<AdventureProgress>()?.Data.beacon==true?"Baliza encendida · Diario":"Expedición a las ruinas · Diario"):$"{(QuestIndex<2?QuestIndex+1:QuestIndex-4)}/7 · "+Objectives[QuestIndex]+
                (string.IsNullOrEmpty(Rewards[QuestIndex])?"":"\nRecompensa: "+Rewards[QuestIndex]);
        }

        private static void EnsureOutline(Text text)
        {
            if(text==null||text.GetComponent<Outline>()!=null)return;
            var outline=text.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(.04f,.025f,.015f,.92f);
            outline.effectDistance=new Vector2(.7f,-.7f);
            outline.useGraphicAlpha=true;
        }
    }
}
