using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class ExteriorServicesChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

        public static void Run(PlayerInventory inventory)
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Exterior services verification requires --qa.");
            var campaign = inventory.GetComponent<ValleyCampaign>();
            var home = inventory.GetComponent<HouseSystem>();
            var entrance = Object.FindFirstObjectByType<ShopEntrance>(FindObjectsInactive.Include);
            var shop = Object.FindFirstObjectByType<SimpleShopSystem>();
            var original = inventory.transform.position;
            var camera = Camera.main; float size = camera.orthographicSize;
            entrance.Interact(FarmTool.Sword, inventory);
            Check(SimpleShopSystem.IsOpen && !entrance.IsInsideShop, "Shop must open only a service window.");
            Check(inventory.transform.position == original && camera.orthographicSize == size, "Shop interaction moved the player or camera.");
            shop.ExitShop(); Check(!SimpleShopSystem.IsOpen, "Shop close failed.");
            Check(!Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name == "Shop Interior"), "Legacy shop scenery remains visible.");

            var doors = campaign.World.GetComponentsInChildren<VillageBuildingService>();
            Check(doors.Length == 4, "Village facades need four exterior service points.");
            campaign.Data.nicoWorkshopRepaired = true;
            foreach (var door in doors)
            {
                campaign.Teleport(door.transform.position + Vector3.down);
                var before = inventory.transform.position;
                door.Interact(FarmTool.Sword, inventory);
                Check(inventory.transform.position == before && !home.IsInside && !entrance.IsInsideShop, "A village service entered an interior.");
                if (door.LotId == "nico") Check(CraftingWindow.IsOpen, "Armory did not open equipment recipes.");
                if (door.LotId == "rolo" || door.LotId == "dalia") Check(SimpleShopSystem.IsOpen, "Trade service did not open.");
                Object.FindFirstObjectByType<CraftingWindow>()?.Close(); shop.ExitShop();
            }

            campaign.Teleport(home.ReturnPosition);
            original = inventory.transform.position; home.Enter(false);
            Check(AdventureWindow.IsOpen && !home.IsInside && !home.RoomRoot.gameObject.activeSelf && inventory.transform.position == original, "Refuge service still enters a room.");
            Object.FindFirstObjectByType<AdventureWindow>().Close();

            var build = inventory.GetComponent<ConstructionSystem>();
            campaign.Teleport(new Vector3(3, -3)); inventory.AddPacked("Bed");
            bool placed = false;
            for (float x = 2; x < 9 && !placed; x += .5f)
                for (float y = -6; y < -2 && !placed; y += .5f)
                    if (build.CanPlace("Bed", new Vector2(x, y), out _)) placed = build.PlacePacked("Bed", new Vector2(x, y));
            Check(placed, "A bed can no longer be placed without entering a house.");
            var bed = build.Buildings.First(item => item.kind == "Bed");
            campaign.Teleport(new Vector3(bed.x, bed.y - 1));
            var clock = Object.FindFirstObjectByType<DayNightCycle>(); clock.Restore(3, 22); home.Sleep(bed);
            Check(clock.Day == 4, "The exterior bed did not advance to morning.");

            var chest = new BuildingData { kind = "Chest", indoors = true, x = -2, y = 101, wood = 17, food = 3 };
            build.Buildings.Add(chest);
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var playerField = typeof(GameSaveSystem).GetField("player", Flags);
            var restoreLocation = typeof(GameSaveSystem).GetField("restorePlayerLocation", Flags);
            bool previousRestoreLocation = (bool)restoreLocation.GetValue(save);
            playerField.SetValue(save, inventory.transform); restoreLocation.SetValue(save, true);
            try
            {
                var snapshot = typeof(GameSaveSystem).GetMethod("BuildSaveData", Flags).Invoke(save, null);
                snapshot = JsonUtility.FromJson(JsonUtility.ToJson(snapshot), snapshot.GetType());
                var type = snapshot.GetType();
                var restore = typeof(GameSaveSystem).GetMethod("RestoreSaveData", Flags);
                int coins = inventory.Coins;
                type.GetField("insideShop").SetValue(snapshot, true);
                restore.Invoke(save, new[] { snapshot });
                Check(!SimpleShopSystem.IsOpen && !entrance.IsInsideShop && inventory.transform.position == entrance.OutsidePosition, "Saved shop presence was restored inside.");
                type.GetField("insideShop").SetValue(snapshot, false);
                ((HouseData)type.GetField("house").GetValue(snapshot)).inside = true;
                restore.Invoke(save, new[] { snapshot });
                Check(inventory.transform.position == home.ReturnPosition && !home.IsInside, "Saved house presence was restored inside.");
                Check(inventory.Coins == coins && build.Buildings.Any(b => b.indoors && b.kind == "Chest" && b.wood == 17 && b.food == 3), "Legacy inventory or furniture contents were lost.");
            }
            finally { playerField.SetValue(save, null); restoreLocation.SetValue(save, previousRestoreLocation); }
            Directory.CreateDirectory("Design/Validation/ExteriorServices");
            File.WriteAllText("Design/Validation/ExteriorServices/result.txt", "PASS: exterior storefronts, armory, food and material services, direct close, unchanged player/camera, outside bed placement/sleep, legacy shop/house save recovery with furniture and resources preserved.\n");
        }
    }
}
