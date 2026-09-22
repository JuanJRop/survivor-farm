using System;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [Serializable] public sealed class MasteryRecord
    {
        public int tool;
        public int level=1;
        public int[] uses=new int[3];
    }

    /// <summary>Usage unlocks a purchase. Existing axe/pickaxe/sword controllers still own their levels.</summary>
    public sealed class ToolMastery : MonoBehaviour
    {
        public static readonly FarmTool[] Branches={FarmTool.Sword,FarmTool.Bow,FarmTool.Axe,FarmTool.Pickaxe,FarmTool.Hoe,FarmTool.WateringCan};
        private MasteryRecord[] records;
        public event Action Changed;
        void Awake()=>Restore(null);
        MasteryRecord Record(FarmTool tool)=>Array.Find(records,r=>r.tool==(int)tool);
        public int Level(FarmTool tool)=>tool==FarmTool.Sword?GetComponent<PlayerCraftingController>()?.WeaponLevel??1:
            tool==FarmTool.Axe||tool==FarmTool.Pickaxe?GetComponent<PlayerToolUpgradeController>()?.GetToolLevel(tool)??1:Record(tool)?.level??1;
        public static int Required(FarmTool tool,int level)=>level>=3?0:
            tool==FarmTool.Sword?(level==1?18:28):tool==FarmTool.Bow?(level==1?12:20):(level==1?6:10);
        public int Uses(FarmTool tool,int level)=>Record(tool)?.uses[Mathf.Clamp(level-1,0,2)]??0;
        public bool HasBranch(FarmTool tool) => tool != FarmTool.Bow || GetComponent<PlayerInventory>() != null && GetComponent<PlayerInventory>().OwnsEquipment("Bow");
        public bool CanUnlock(FarmTool tool)=>HasBranch(tool)&&Record(tool)!=null&&Level(tool)<3&&Uses(tool,Level(tool))>=Required(tool,Level(tool));
        public string Requirement(FarmTool tool)=>Level(tool)>=3?"Maestría completa":$"Usos de nivel {Level(tool)}: {Uses(tool,Level(tool))}/{Required(tool,Level(tool))}";
        public void Earn(FarmTool tool,int resourceTier=0)
        {
            var record=Record(tool);if(record==null)return;
            int level=Level(tool);if(level>=3||resourceTier>0&&resourceTier<level)return;
            bool unlocked=CanUnlock(tool);
            record.uses[level-1]=Mathf.Min(Required(tool,level),record.uses[level-1]+1);
            if(!unlocked&&CanUnlock(tool))FarmNotificationCenter.Show(PlayerToolbelt.GetDisplayName(tool)+": nivel "+(level+1)+" desbloqueado. Cómpralo en Esc → Mejoras.");
            Changed?.Invoke();
        }
        public void Cost(FarmTool tool,out int wood,out int stone,out int coins)
        {
            CostForTier(tool, Level(tool) + 1, out wood, out stone, out coins);
        }
        public static void CostForTier(FarmTool tool,int tier,out int wood,out int stone,out int coins)
        {
            int level=Mathf.Clamp(tier-1,1,2);wood=4*level;stone=4*level;coins=level==1?15:35;
            if(tool==FarmTool.Sword){wood=6*level;stone=8*level;coins=0;}
        }
        public bool CanAfford(FarmTool tool)
        {
            var inventory = GetComponent<PlayerInventory>(); Cost(tool, out int wood, out int stone, out int coins);
            return inventory != null && inventory.Wood >= wood && inventory.Stone >= stone && inventory.Coins >= coins;
        }
        public static string TierTitle(FarmTool tool, int tier)
        {
            if(tool==FarmTool.Sword)return tier==1?"Espada de madera":tier==2?"Hoja de acero":"Espada real";
            if(tool==FarmTool.Bow)return tier==1?"Tirador":tier==2?"Cazador":"Maestro arquero";
            return tier==1?"Aprendiz":tier==2?"Especialista":"Maestro";
        }
        public static string TierDescription(FarmTool tool,int tier)
        {
            if(tool==FarmTool.Sword)return tier==1?"Combo de tres cortes.\nSin carga mágica.":tier==2?"Mantén clic para cargar.\nDescarga frontal con empuje.":"Carga mejorada.\nOnda de daño a tu alrededor.";
            if(tool==FarmTool.Bow)return (8+(tier-1)*4)+" de daño base por flecha.\nApunta con el cursor.\nConsume 1 flecha por disparo.";
            if(tool==FarmTool.Axe)return tier==1?"Tala árboles comunes.":"+"+(tier-1)+" madera por recolección.\nTala árboles de mayor nivel.";
            if(tool==FarmTool.Pickaxe)return tier==1?"Extrae piedra común.":tier==2?"+1 mineral por recolección.\nAcceso a vetas de hierro.":"+2 minerales por recolección.\nAcceso a las vetas más ricas.";
            if(tool==FarmTool.Hoe)return tier==1?"Cultivo y cosecha básicos.":"+"+(tier-1)+" fruto por cosecha.";
            return tier==1?"Riego básico.":"El riego acelera el crecimiento\nun "+((tier-1)*15)+"%.";
        }
        public bool Purchase(FarmTool tool)
        {
            var inventory=GetComponent<PlayerInventory>();
            if(!CanUnlock(tool)||GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0)return false;
            bool bought;
            if(tool==FarmTool.Axe||tool==FarmTool.Pickaxe)bought=GetComponent<PlayerToolUpgradeController>().TryUpgrade(tool);
            else if(tool==FarmTool.Sword)bought=GetComponent<PlayerCraftingController>().Craft("Sword");
            else
            {
                Cost(tool,out int wood,out int stone,out int coins);
                if(inventory.Wood<wood||inventory.Stone<stone||inventory.Coins<coins)return false;
                inventory.TryRemoveWood(wood);inventory.TryRemoveStone(stone);inventory.TrySpendCoins(coins);
                Record(tool).level++;bought=true;
            }
            if(bought){Changed?.Invoke();FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);}
            return bought;
        }
        public MasteryRecord[] Capture()=>records.Select(r=>new MasteryRecord{tool=r.tool,level=Level((FarmTool)r.tool),uses=(int[])r.uses.Clone()}).ToArray();
        public void Restore(MasteryRecord[] saved)
        {
            records=Branches.Select(t=>
            {
                var old=saved?.FirstOrDefault(r=>r!=null&&r.tool==(int)t);
                var record=new MasteryRecord{tool=(int)t,level=Mathf.Clamp(old?.level??1,1,3)};
                for(int i=0;i<3;i++)record.uses[i]=Mathf.Clamp(old?.uses!=null&&i<old.uses.Length?old.uses[i]:0,0,Required(t,i+1));
                return record;
            }).ToArray();Changed?.Invoke();
        }
    }
}
