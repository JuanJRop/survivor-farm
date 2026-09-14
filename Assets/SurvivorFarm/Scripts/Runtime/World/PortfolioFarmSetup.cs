using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Additive configuration of Main's authored farm; never rebuilds the scene.</summary>
    public static class PortfolioFarmSetup
    {
        public static FarmDefense Configure(PortfolioSession session)
        {
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();
            var world=campaign.World;
            foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsInactive.Include,FindObjectsSortMode.None))pool.enabled=false;
            foreach(var enemy in Object.FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None))enemy.ReturnToPool();
            foreach(var tutorial in Object.FindObjectsByType<TutorialQuestSystem>(FindObjectsSortMode.None))tutorial.enabled=false;
            player.GetComponent<PlayerRespawnController>().enabled=false;
            foreach(var item in Object.FindObjectsByType<ValleyInteraction>(FindObjectsSortMode.None))item.enabled=false;
            foreach(var item in Object.FindObjectsByType<VillageBuildingService>(FindObjectsSortMode.None))item.enabled=false;
            foreach(var item in Object.FindObjectsByType<TutorialSupply>(FindObjectsSortMode.None))item.gameObject.SetActive(false);
            foreach(var item in Object.FindObjectsByType<TutorialPracticeTarget>(FindObjectsSortMode.None))item.gameObject.SetActive(false);
            foreach(var item in Object.FindObjectsByType<BaseHouse>(FindObjectsSortMode.None))item.enabled=false;
            foreach(var item in Object.FindObjectsByType<ShopEntrance>(FindObjectsSortMode.None))item.enabled=false;
            foreach(var item in Object.FindObjectsByType<DungeonEntrance>(FindObjectsSortMode.None))item.gameObject.SetActive(false);
            foreach(var item in Object.FindObjectsByType<LandUnlockZone>(FindObjectsSortMode.None))item.enabled=false;
            foreach(var item in Object.FindObjectsByType<RepairableBridge>(FindObjectsSortMode.None))item.enabled=false;
            campaign.enabled=false;
            // The old journey remains in code/assets; its social menus do not drive this session.
            foreach(var ui in Object.FindObjectsByType<AdventureWindow>(FindObjectsSortMode.None))ui.enabled=false;
            foreach(var ui in Object.FindObjectsByType<MobileOptionsMenu>(FindObjectsSortMode.None))ui.enabled=false;
            foreach(var ui in Object.FindObjectsByType<PlayerEquipmentWindow>(FindObjectsSortMode.None))ui.enabled=false;

            var well=world.GetComponentsInChildren<ValleyInteraction>().First(v=>v.Id=="village:well");
            var core=well.gameObject.AddComponent<FarmDefense>();core.ConfigureCore(player,session.Settings.coreHealth);
            // Reuse six dormant scene plots and keep their stable save identities.
            var plots=Object.FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .OrderBy(p=>(p.transform.position-new Vector3(9,-4)).sqrMagnitude).Take(6).ToArray();
            for(int i=0;i<plots.Length;i++)
            {
                var position=new Vector3(8.5f+(i%3)*1.2f,-3.6f-(i/3)*1.25f);
                ClearResourceAt(position,1.0f);
                plots[i].transform.position=position;
                plots[i].EnableForSlice(world.Art("Soil"),session.Settings.cropStages!=null&&session.Settings.cropStages.Length>0?session.Settings.cropStages:new[]{world.Art("Crop")});
            }
            var construction=player.GetComponent<ConstructionSystem>();
            construction.AddAuthoredDefense("Fence",new Vector2(-1,-6),5);
            construction.AddAuthoredDefense("Fence",new Vector2(1,-6));
            // Existing furniture logic supplies cooking; no parallel crafting system.
            construction.AddAuthoredDefense("Campfire",new Vector2(-3.8f,-2.3f));
            player.GetComponent<PlayerCraftingController>().RegisterPlacedFire();
            ClearResourceAt(new Vector3(1,0),1.5f);
            campaign.Teleport(new Vector3(1,-1,0));
            if(Camera.main!=null)Camera.main.orthographicSize=6.8f;
            world.Label("HUERTO",new Vector3(9.6f,-5.5f),.075f);
            world.Label("POZO · PROTÉGELO",VillageLayout.Well+new Vector3(0,1.3f),.065f);
            world.Label("TALLER · F",new Vector3(-5.6f,-4.4f),.07f);
            // Natural perimeter retains the existing buildings, paths, river and vegetation.
            FarmExploration.Configure(world,player);
            world.gameObject.AddComponent<FarmAtmosphere>();
            return core;
        }
        private static void ClearResourceAt(Vector3 point,float distance)
        {
            foreach(var spawn in Object.FindObjectsByType<ResourceSpawnPoint>(FindObjectsSortMode.None))
            {
                if(spawn.Instance==null||Vector2.Distance(spawn.Instance.transform.position,point)>distance)continue;
                Vector3 destination=new Vector3(spawn.Instance is TreeResource?-13.5f:13.5f,Mathf.Clamp(point.y,-8,6),0);
                spawn.transform.position=destination;spawn.Instance.transform.position=destination;
            }
        }
    }
}
