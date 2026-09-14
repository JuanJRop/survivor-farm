using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
namespace SurvivorFarm.Runtime.Player
{
    public static class BackpackActions
    {
        public static bool IsBuilding(string id)=>FortressPieces.IsWall(id)||new[]{"Campfire","Trap","Turret","Chest","Workbench","Beacon","Bed","Cabinet","Furnace"}.Contains(id);
        public static int Price(string id)=>SurvivalItemCatalog.Find(id)?.SellPrice ?? id switch{"CommonSeeds"=>1,"MineralSeeds"=>5,"MagicSeeds"=>12,"Wood"=>3,"Stone"=>4,"Fruit"=>6,"Food"=>10,"Iron"=>8,"Campfire"=>10,"Fence"=>2,"Chest"=>12,"Workbench"=>20,"Beacon"=>40,"Bed"=>12,"Cabinet"=>25,"Furnace"=>40,_=>0};
        public static int Count(PlayerInventory inv,string id)=>inv==null||string.IsNullOrEmpty(id)?0:SurvivalItemCatalog.IsKnown(id)?inv.GetAvailableItemCount(id):id switch{"CommonSeeds"=>inv.GetAvailableSeedCount(SeedRarity.Common),"MineralSeeds"=>inv.GetAvailableSeedCount(SeedRarity.Mineral),"MagicSeeds"=>inv.GetAvailableSeedCount(SeedRarity.Magic),"Wood"=>inv.Wood,"Stone"=>inv.Stone,"Fruit"=>inv.Fruit,"Food"=>inv.Food,"Iron"=>inv.GetComponent<AdventureProgress>()?.Data.iron??0,_=>inv.PackedCount(id)};
        public static bool Remove(PlayerInventory inv,string id,int amount)
        {
            if(inv==null||amount<=0||Count(inv,id)<amount)return false;
            if(SurvivalItemCatalog.IsKnown(id))return inv.TryRemoveItem(id,amount);
            switch(id){case "CommonSeeds":return inv.TryRemoveSeeds(SeedRarity.Common,amount);case "MineralSeeds":return inv.TryRemoveSeeds(SeedRarity.Mineral,amount);case "MagicSeeds":return inv.TryRemoveSeeds(SeedRarity.Magic,amount);case "Wood":return inv.TryRemoveWood(amount);case "Stone":return inv.TryRemoveStone(amount);case "Fruit":return inv.TryRemoveFruit(amount);case "Food":return inv.TryRemoveFood(amount);case "Iron":return inv.GetComponent<AdventureProgress>().SpendIron(amount);default:return IsBuilding(id)&&inv.RemovePacked(id,amount);}
        }
        public static bool Sell(PlayerInventory inv,string id,int amount)
        {
            if (inv == null || !EconomyTradeRules.TryGetTotalPrice(Price(id), amount, out int total) ||
                (long)inv.Coins + total > int.MaxValue || !Remove(inv, id, amount)) return false;
            inv.AddCoins(total);
            FarmNotificationCenter.Show("Venta: +" + total + " oro.");
            return true;
        }
        public static bool Use(PlayerInventory inv,string id)
        {
            if(inv==null||string.IsNullOrEmpty(id))return false;
            if(Count(inv,id)<1)return false;
            if(IsBuilding(id))return inv.GetComponent<ConstructionSystem>().BeginPacked(id);
            var stats=inv.GetComponent<PlayerSurvivalStats>();
            var catalogItem=SurvivalItemCatalog.Find(id);
            if(id=="Food" || id=="Fruit" || catalogItem?.IsFood==true)
            {
                if(stats==null||stats.CurrentHealth<=0||stats.CurrentHealth>=stats.MaxHealth)return false;
                if(!Remove(inv,id,1))return false;
                int heal=Mathf.Min(FoodHealing(inv,id),stats.MaxHealth-stats.CurrentHealth);
                stats.Heal(heal);
                FarmGameEvents.RaiseFoodEaten();
                FarmNotificationCenter.Show($"Comiste {catalogItem?.Name ?? (id=="Food"?"una ración":"una fruta")}: +{heal} vida.");
                return true;
            }
            return false;
        }
        public static int FoodHealing(PlayerInventory inventory,string id)
        {
            int healing=id=="Food"?2:id=="Fruit"?1:SurvivalItemCatalog.Find(id)?.HealRestore??0;
            return healing>0 ? healing+(inventory?.FoodHealingBonus??0) : 0;
        }
    }
}
