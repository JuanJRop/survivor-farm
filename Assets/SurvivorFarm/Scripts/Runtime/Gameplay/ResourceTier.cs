using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Resource tier is spatial authoring, not a replacement for health or regrowth.</summary>
    public sealed class ResourceTier : MonoBehaviour
    {
        public int Tier {get;private set;}=1;
        public FarmTool Tool=>GetComponent<TreeResource>()!=null?FarmTool.Axe:FarmTool.Pickaxe;
        public static void Configure(HarvestableResource resource)
        {
            if(resource is AnimalResource)return;
            var tier=resource.GetComponent<ResourceTier>()??resource.gameObject.AddComponent<ResourceTier>();
            float distance=Mathf.Abs(resource.transform.position.x);
            tier.Tier=distance>=25?3:distance>=19?2:1;
        }
        public bool CanHarvest(PlayerInventory player)
        {
            bool ready=(player.GetComponent<PlayerToolUpgradeController>()?.GetToolLevel(Tool)??1)>=Tier;
            if(!ready)
            {
                string display=PlayerToolbelt.GetDisplayName(Tool);
                FarmNotificationCenter.Show($"Este recurso es de nivel {Tier}. Mejora tu {display} a nivel {Tier} en Maestrías [K] para poder recolectarlo.");
            }
            return ready;
        }
        public void Grant(PlayerInventory player) => Grant(player, false);

        public void Grant(PlayerInventory player, bool physicalDrop)
        {
            player.GetComponent<ToolMastery>()?.Earn(Tool,Tier);
            if(Tier<2)return;
            if(Tool==FarmTool.Pickaxe){player.GetComponent<AdventureProgress>()?.AddIron(Tier==3?3:2);if(Tier==3)player.AddItem("GoldOre",1);}
            else if (physicalDrop && PortfolioSession.Active)
            {
                float playerAngle=Mathf.Atan2(player.transform.position.y-transform.position.y,
                    player.transform.position.x-transform.position.x);
                EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Wood,
                    Tier==3?5:2, null, playerAngle);
            }
            else player.AddWood(Tier==3?5:2);
        }
    }
}
