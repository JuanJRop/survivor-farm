using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable] public sealed class ValleyData
    {
        public bool note, harvest, camp, cargo, bench, chicken, sealStone, guardian, portal, boss, seed, restored, ate, sleptFirstNight;
        public bool maraPantryStocked, nicoWorkshopRepaired, daliaGardenRestored, roloMarketOpened, guardPostBuilt;
        public int startDay, zone, influence, villageStage, rankRewardMask;
        public int nicoSupplyDay;
        public int acceptedVillageQuests;
        public int acceptedCampQuests, claimedCampQuests;
        public string trackedCampQuest;
        public List<string> discoveries = new List<string>();
        public List<EnemyCampState> enemyCamps = new List<EnemyCampState>();
    }

    public sealed class ValleyCampaign : MonoBehaviour
    {
        public ValleyData Data = new ValleyData();
        public PlayerInventory Inventory { get; private set; }
        public ValleyWorld World { get; private set; }
        public Vector3 Home { get; private set; }
        public int Chapter => Data.restored ? 6 : Data.portal ? 5 : Data.guardian ? 4 : Data.sealStone ? 3 : Data.bench ? 2 : Data.camp ? 1 : 0;
        public static readonly string[] Titles = { "La aldea apagada", "El campamento sin suministros", "Bajo la piedra", "El guardián herido", "La puerta entre raíces", "Recuperar la reliquia", "El valle vuelve a florecer" };
        public static readonly string[] RankNames = { "Forastero", "Ayudante", "Vecino de confianza", "Protector", "Líder de Raízclara" };
        public bool WorkshopRestored => Data.nicoWorkshopRepaired;
        public int InfluenceRank => VillageServices.RankFor(Data);
        public string NextRankRequirement => VillageServices.NextRankRequirement(Data);
        public VillageServiceStatus WorkshopService => VillageServices.WorkshopStatus(Data, CurrentDay, Inventory != null ? Inventory.Wood : 0, Inventory != null ? Inventory.Stone : 0);
        public string VillageServicesSummary => WorkshopService.Summary + "\nPozo: punto de encuentro del pueblo.";
        int CurrentDay => FindFirstObjectByType<DayNightCycle>()?.Day ?? 1;
        public string InfluenceTitle => RankNames[InfluenceRank];
        public string InfluenceLine => "Rango: " + InfluenceTitle + " · Influencia " + Data.influence;
        public int VillageRepairCount => VillageServices.RepairCount(Data);
        public string VillageStatus => "Estado: " + (Data.restored ? "la reliquia vuelve a Raízclara. " :
            VillageRepairCount==0 ? "el pueblo espera sus primeras reparaciones. " : "los vecinos recuperan sus lugares de trabajo. ") +
            "Proyectos completados: "+VillageRepairCount+"/5. "+(WorkshopRestored ? "El taller de Nico está abierto." : "El taller de Nico sigue cerrado.");
        public string VillagerGuide => GetVillagerDialogue("village:elder") + "\n" + GetVillagerDialogue("village:blacksmith") + "\n" +
            GetVillagerDialogue("village:farmer") + "\n" + GetVillagerDialogue("village:merchant") + "\n" + GetVillagerDialogue("village:guard");
        public string GetVillagerDialogue(string id) => VillageResidents.Name(id) + ": " + VillageResidents.Dialogue(Data, id);
        public string GetVillagerActivity(string id) => VillageResidents.Activity(Data, id, FindFirstObjectByType<DayNightCycle>()?.Hour ?? 8f);
        public string NextVillageAction => !Data.note ? "Lee la nota junto a casa." : !Data.camp ? "Completa el primer día." :
            !Data.maraPantryStocked ? "Mara: entrega 2 raciones." :
            !Data.nicoWorkshopRepaired ? "Nico: entrega 12 madera y 8 piedra." :
            !Data.daliaGardenRestored ? "Dalia: entrega 2 frutas y 4 madera." :
            !Data.roloMarketOpened ? "Rolo: entrega 20 oro y 8 madera." :
            !Data.guardPostBuilt ? "Iria: entrega 6 hierro y 2 raciones." : "Misiones de aldea completas por ahora.";
        public string VillageBoard => "TABLÓN DE RAÍZCLARA · Reparaciones " + VillageRepairCount + "/5\n" +
            QuestLine("Mara",Data.maraPantryStocked,Data.camp,"Despensa comunal: 2 raciones → madera, oro e influencia.") + "\n" +
            QuestLine("Nico",Data.nicoWorkshopRepaired,Data.camp,"Taller: 12 madera, 8 piedra → fachada reparada, hierro, escudo y encargos diarios.") + "\n" +
            QuestLine("Dalia",Data.daliaGardenRestored,Data.note,"Cocina comunal: 2 frutas, 4 madera → ingredientes y raciones.") + "\n" +
            QuestLine("Rolo",Data.roloMarketOpened,Data.nicoWorkshopRepaired&&Data.daliaGardenRestored,"Mercado: 20 oro, 8 madera → trueques mejores.") + "\n" +
            QuestLine("Iria",Data.guardPostBuilt,Data.sealStone||Data.guardian,"Guardia: 6 hierro, 2 raciones → defensa de aldea.") + "\n" +
            NextRankRequirement + "\n" + VillageServicesSummary;
        public string Objective => !Data.note ? "Lee la nota del cofre junto a tu casa, en la plaza de Aldea Raízclara." : !Data.camp ? FirstDayObjective :
            !Data.cargo ? "Sigue el cartel junto a casa. Recupera el cargamento: rodea el claro o enfréntate a los limos." :
            !Data.bench ? "Repara el banco del campamento: 8 madera y 4 piedra." :
            !Data.sealStone ? "En la cantera, mejora el pico a nivel 2 en el taller y rompe el derrumbe. Recupera el primer sello." :
            !Data.guardian ? "Refuerza tu espada [F] y vence al guardián del bosque para obtener el segundo sello." :
            !Data.portal ? "Lleva los dos sellos al santuario y activa el portal." :
            !Data.boss ? "Cruza el portal. Evita la marca de salto, destruye los brotes y golpea al Rey Limo agotado." :
            !Data.seed ? "Abre el cofre de la arena para recuperar el Corazón del Valle." :
            !Data.restored ? "Regresa por el portal y devuelve la reliquia al altar junto a casa." : "Valle restaurado. Explora las vetas, comercia y reconstruye el pueblo.";
        public string HudObjective
        {
            get
            {
                var combat = CampCombatQuests.Tracked(Data);
                if (combat != null) return CampCombatQuests.IsCleared(Data, combat.CampId) ? combat.Title + ": vuelve con Iria" :
                    combat.Camp.Title + " - " + CampCombatQuests.Progress(Data, combat.CampId);
                if(!Data.note)return "Lee la nota junto al refugio";
                if(Data.camp)return !Data.cargo?"Recupera el cargamento":!Data.bench?"Repara el banco del campamento":
                    !Data.sealStone?"Abre el derrumbe de la cantera":!Data.guardian?"Vence al guardi\u00e1n del bosque":
                    !Data.portal?"Activa el portal del santuario":!Data.boss?"Vence al Rey Limo":
                    !Data.seed?"Recupera el Corazón del Valle":!Data.restored?"Devuelve la reliquia al altar":"Reconstruye y explora el valle";
                var tutorial=FindFirstObjectByType<TutorialQuestSystem>();int mask=tutorial!=null?tutorial.CompletedSteps:0;
                string[] steps={"Re\u00fane madera en el bosquecillo","Recoge piedra al este"};
                for(int i=0;i<steps.Length;i++)if((mask&(1<<i))==0)return steps[i];
                var craft=GetComponent<PlayerCraftingController>();
                if(craft==null||!craft.CampfireBuilt)return Inventory.PackedCount("Campfire")>0?"Coloca tu fogata":"Fabrica una fogata";
                if(craft.MealsCooked==0)return "Cocina tu primera raci\u00f3n";
                if(!craft.BedBuilt)return "Coloca una cama junto al refugio";
                return "Usa tu cama al caer la noche";
            }
        }
        public string FirstDayObjective
        {
            get
            {
                var tutorial=FindFirstObjectByType<TutorialQuestSystem>();int mask=tutorial!=null?tutorial.CompletedSteps:0;
                string[] steps={
                    "Acércate a un árbol y pulsa Interactuar hasta talarlo. El hacha se usa sola.",
                    "Acércate a una roca y pulsa Interactuar hasta romperla. El pico se activa solo."
                };
                for(int i=0;i<steps.Length;i++)if((mask&(1<<i))==0)return steps[i];
                var craft=GetComponent<PlayerCraftingController>();
                var stats=GetComponent<PlayerSurvivalStats>();
                var clock=FindFirstObjectByType<DayNightCycle>();
                if(Inventory.PackedCount("Campfire")>0 && (craft==null||!craft.CampfireBuilt))return "Abre la mochila [I], señala la fogata y pulsa Colocar. Elige un punto verde y confirma con clic izquierdo.";
                if(craft==null||!craft.CampfireBuilt)return "Abre las recetas [F] y fabrica una fogata. Reúne los materiales que indica la receta.";
                if(craft.MealsCooked==0)return "Abre las recetas [F] y cocina una ración. Hay ingredientes en los suministros del bosquecillo y en la tienda de Dalia.";
                if(!Data.ate&&Inventory.Food>0&&stats!=null&&stats.CurrentHealth<stats.MaxHealth)return "Puedes comer una ración desde la mochila [I] para recuperar vida. Luego prepara tu cama.";
                if(Inventory.PackedCount("Bed")>0&&!craft.BedBuilt)return "Coloca la cama en un terreno libre junto al refugio. Mantén despejados los caminos.";
                if(!craft.BedBuilt)return "Fabrica una cama o cómprala al interactuar con la fachada del refugio.";
                if(!Data.sleptFirstNight)return clock!=null&&clock.IsNight?
                    "Es de noche. Usa tu cama con Interactuar para descansar hasta el amanecer.":
                    "Día 1 preparado. A partir de las 21:00 puedes descansar en tu cama.";
                return "Día 1 completado. El campamento está listo para abrirse.";
            }
        }
        public string Journal => "CAPÍTULO " + Mathf.Min(6,Chapter+1) + " · " + Titles[Chapter] +
            "\n" + InfluenceLine +
            "\n" + VillageStatus +
            "\n\n" + (Data.note ? "El Corazón del Valle ha desaparecido. Completa el objetivo actual para descubrir la siguiente parte de la historia." :
            "Llegas a Aldea Raízclara después de la caída del valle. Las casas siguen en pie, pero el pozo, el taller y el tablón muestran un pueblo que necesita comida, materiales y protección. El primer misterio está en el cofre junto a tu casa.") +
            "\n\n" + VillageBoard +
            "\n\nGUÍA DE ALDEANOS\n" + VillagerGuide +
            "\n\nCÓMO JUGAR\nWASD: caminar · 1 espada · 2 arco\nClic izquierdo: atacar con el arma equipada\nE / clic derecho: usar el objeto señalado\nÁrboles y rocas: acércate e interactúa; hacha y pico se activan solos.\nLa comida recupera vida. Consigue ingredientes en suministros y tiendas.\n\nEl próximo objetivo aparecerá al completar este paso.";

        string QuestLine(string npc,bool done,bool available,string text)
        {
            var quest=Array.Find(VillageQuests.All,q=>VillageResidents.Name(q.ResidentId)==npc);
            string status=quest!=null?VillageQuests.Status(Data,quest.ResidentId):done?"Completada":available?"Por aceptar":"No disponible aun";
            return npc+" · "+status+": "+text;
        }

        public bool TalkToVillager(string id, Transform speaker = null)
        {
            if(!VillageQuests.IsVillager(id))return false;
            var window=FindFirstObjectByType<VillageDialogueWindow>();
            if(window==null)return Hint(GetVillagerDialogue(id));
            if(speaker==null&&World!=null)
                foreach(var npc in World.GetComponentsInChildren<ValleyInteraction>())if(npc.Id==id){speaker=npc.transform;break;}
            return window.Open(this,id,speaker);
        }

        public bool AcceptVillageQuest(string id)
        {
            var quest=VillageQuests.Find(id);
            if(quest==null||VillageQuests.IsComplete(Data,id)||VillageQuests.IsAccepted(Data,id)||VillageQuests.Requirement(Data,id)!=""||GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0)return false;
            Data.acceptedVillageQuests|=quest.Bit;
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
            return true;
        }

        public bool AcceptCampQuest(string id)
        {
            var quest = CampCombatQuests.Find(id);
            if (quest == null || CampCombatQuests.IsAccepted(Data, id) || CampCombatQuests.IsClaimed(Data, id) || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) return false;
            Data.acceptedCampQuests |= quest.Bit;
            Data.trackedCampQuest = id;
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
            return true;
        }

        public bool TrackCampQuest(string id)
        {
            if (id != null && (!CampCombatQuests.IsAccepted(Data, id) || CampCombatQuests.IsClaimed(Data, id))) return false;
            Data.trackedCampQuest = id;
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
            return true;
        }

        public bool CompleteCampQuest(string id)
        {
            if (!CampCombatQuests.CanClaim(Data, id) || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) return false;
            var quest = CampCombatQuests.Find(id);
            // Mark delivery before inventory callbacks can attempt another claim.
            Data.claimedCampQuests |= quest.Bit;
            if (Data.trackedCampQuest == id) Data.trackedCampQuest = null;
            Inventory.AddCoins(quest.Coins);
            if (quest.Rubies > 0) Inventory.AddItem("Ruby", quest.Rubies);
            Changed("Iria: " + quest.Title + " completada. +" + quest.Coins + " oro." +
                (quest.Rubies > 0 ? " +1 rubi." : "") + AddInfluence(quest.Influence));
            return true;
        }

        public bool CompleteVillageQuest(string id)
        {
            if(!VillageQuests.IsAccepted(Data,id)||VillageQuests.IsComplete(Data,id)||VillageQuests.Requirement(Data,id)!=""||
                !VillageQuests.HasMaterials(Inventory,VillageQuests.Find(id))||GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0)return false;
            return id switch {"village:elder"=>CompleteMaraProject(),"village:blacksmith"=>CompleteNicoProject(),"village:farmer"=>CompleteDaliaProject(),"village:merchant"=>CompleteRoloProject(),"village:guard"=>CompleteGuardProject(),_=>false};
        }

        public bool TradeWithRolo()
        {
            if(!Data.roloMarketOpened||GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0)return false;
            return CompleteRoloProject();
        }

        public string AddInfluence(int amount)
        {
            if(amount<=0)return "";
            int before=InfluenceRank;
            Data.influence=Mathf.Clamp(Data.influence+amount,0,99);
            SyncVillageProgress();
            string reward=GrantRankRewards(InfluenceRank);
            return InfluenceRank>before ? " Influencia +" + amount + ". Nuevo rango: " + InfluenceTitle + "." + reward : " Influencia +" + amount + "." + reward;
        }

        string GrantRankRewards(int upToRank)
        {
            string text="";
            for(int rank=1;rank<=upToRank;rank++)
            {
                int bit=1<<rank;
                if((Data.rankRewardMask&bit)!=0)continue;
                Data.rankRewardMask|=bit;
                switch(rank)
                {
                    case 1:
                        Inventory.AddCoins(10);Inventory.AddFood(2);
                        text+=" Recompensa de Ayudante: +10 oro, +2 raciones.";
                        break;
                    case 2:
                        Inventory.AddCoins(25);Inventory.AddPacked("Fence",3);Inventory.AddEquipment("WoodenShield");
                        text+=" Recompensa de Vecino: +25 oro, +3 cercas, escudo de madera.";
                        break;
                    case 3:
                        (GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>()).AddIron(4);Inventory.AddItem("GoldOre",1);Inventory.AddEquipment("IronHelmet");
                        text+=" Recompensa de Protector: +4 hierro, +1 pepita de oro, casco de hierro.";
                        break;
                    case 4:
                        Inventory.AddFood(3);Inventory.AddItem("Ruby",1);Inventory.AddEquipment("HunterBow");
                        text+=" Recompensa de Líder: arco de cazador, +1 rubí, +3 raciones.";
                        break;
                }
            }
            return text;
        }

        public void SyncVillageProgress()
        {
            int stage=0;
            if(Data.note)stage=1;
            if(Data.camp)stage=2;
            if(Data.maraPantryStocked)stage=Mathf.Max(stage,2);
            if(Data.nicoWorkshopRepaired||Data.daliaGardenRestored||Data.roloMarketOpened||Data.bench||Data.cargo)stage=3;
            if(Data.guardPostBuilt||Data.sealStone||Data.guardian)stage=4;
            if(Data.restored)stage=5;
            Data.villageStage=Mathf.Max(Data.villageStage,stage);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() { SceneManager.sceneLoaded -= Attach; SceneManager.sceneLoaded += Attach; }
        static void Attach(Scene scene, LoadSceneMode mode)
        {
            var player=FindFirstObjectByType<PlayerInventory>();
            if(player!=null&&player.GetComponent<ValleyCampaign>()==null)player.gameObject.AddComponent<ValleyCampaign>();
        }
        void Awake()
        {
            Inventory=GetComponent<PlayerInventory>(); Home=transform.position;
            EnsurePlayerDepth();
            World=new GameObject("Historia del Valle").AddComponent<ValleyWorld>(); World.Build(this); World.gameObject.AddComponent<TutorialHomestead>().Build(this); gameObject.AddComponent<HouseSystem>();
            if (!PortfolioSession.Active) gameObject.AddComponent<EnemyCampWorld>().Configure(this);
            gameObject.AddComponent<WildResourceRegrowth>();
        }
        void EnsurePlayerDepth()
        {
            var renderer=GetComponent<SpriteRenderer>();if(renderer==null)return;
            var depth=GetComponent<WorldSpriteDepth>()??gameObject.AddComponent<WorldSpriteDepth>();
            depth.Visual=renderer;if(Mathf.Approximately(depth.GroundOffset,0f))depth.GroundOffset=-.4f;
        }
        void OnEnable(){FarmGameEvents.FoodEaten+=Ate;FarmGameEvents.SleptUntilMorning+=Slept;}
        void OnDisable(){FarmGameEvents.FoodEaten-=Ate;FarmGameEvents.SleptUntilMorning-=Slept;}
        void Ate(){if(!Data.camp)Data.ate=true;}
        void Slept(){if(!Data.camp)Data.sleptFirstNight=true;}
        void Update(){Evaluate();}
        public void Evaluate()
        {
            if (PortfolioSession.Active) return;
            var clock=FindFirstObjectByType<DayNightCycle>();
            if(Data.startDay==0&&clock!=null)Data.startDay=clock.Day;
            SyncVillageProgress();
            var craft=GetComponent<PlayerCraftingController>();
            var tutorial=FindFirstObjectByType<TutorialQuestSystem>();
            bool sleptByLegacyClock=clock!=null&&Data.startDay>0&&clock.Day>Data.startDay&&craft!=null&&craft.BedBuilt;
            if(!Data.camp&&Data.note&&craft!=null&&craft.MealsCooked>0&&craft.CampfireBuilt&&craft.BedBuilt&&(Data.sleptFirstNight||sleptByLegacyClock)&&GetComponent<PlayerSurvivalStats>().CurrentHealth>0)
            {Data.camp=true;Changed("Día 1 completado. Una exploradora vio tu hoguera y abrió el camino al campamento."+AddInfluence(4));}
        }
        public bool CanTravel(int zone)=>zone==0||zone==1&&Data.camp||zone==2&&Data.bench||zone==3&&Data.sealStone||zone==4&&Data.guardian||zone==5&&Data.portal;
        public bool Travel(int zone)
        {
            if(!CanTravel(zone)){FarmNotificationCenter.Show(Objective);return false;}
            Data.zone=zone;World.ResetEncounter();Teleport(zone==0?Home:ValleyWorld.Center(zone)+new Vector3(-9,-4));
            if(zone==4)GetComponent<PlayerRespawnController>()?.SetCheckpoint(transform.position);
            Changed("Capítulo "+(zone+1)+": "+Titles[zone]+". "+Objective);return true;
        }
        public void SyncLocation()
        {
            for(int i=1;i<=5;i++)if(Mathf.Abs(transform.position.x-ValleyWorld.Center(i).x)<15&&Mathf.Abs(transform.position.y)<10){Data.zone=i;return;}
            Data.zone=0;
        }
        public void Teleport(Vector3 p)
        {
            GetComponent<PlayerMovementController>()?.StopMovement();GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            transform.position=p;var rb=GetComponent<Rigidbody2D>();if(rb!=null){rb.position=p;rb.linearVelocity=Vector2.zero;}
            if(Camera.main!=null)Camera.main.transform.position=p+new Vector3(0,0,-10);Physics2D.SyncTransforms();
        }
        public void Restore(ValleyData data)
        {
            Data=data??new ValleyData();Data.discoveries??=new List<string>();Data.zone=Mathf.Clamp(Data.zone,0,5);
            SyncVillageProgress();
            Data.nicoSupplyDay=Mathf.Max(0,Data.nicoSupplyDay);
            Data.acceptedVillageQuests=Mathf.Max(0,Data.acceptedVillageQuests)&31;
            CampCombatQuests.Normalize(Data);
            World?.Refresh();
            GetComponent<EnemyCampWorld>()?.Restore();
            // Recreate an interrupted encounter from its entrance, never inside a telegraphed attack.
            if(transform.position.x>70){if(!CanTravel(Data.zone))Data.zone=0;Teleport(Data.zone==0?Home:ValleyWorld.Center(Data.zone)+new Vector3(-9,-4));}
        }
        public void Changed(string message)
        {SyncVillageProgress();message+=GrantRankRewards(InfluenceRank);World?.Refresh();FarmNotificationCenter.Show(message);FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);}
        public bool Use(string id, FarmTool tool, bool playAction = true)
        {
            var d=Data;
            if(id.StartsWith("go:"))return Travel(int.Parse(id.Substring(3)));
            if(id=="note") {if(d.note)return Hint(Objective);d.note=true;Changed("«El valle se apaga desde que robaron el Corazón del Valle». La aldea necesita comida antes del anochecer: empieza talando un árbol cercano."+AddInfluence(1));return true;}
            if(VillageQuests.IsVillager(id))return TalkToVillager(id);
            if(id=="village:board")
            {
                var journal=FindFirstObjectByType<AdventureWindow>();
                if(journal!=null){journal.Open("Journal");return true;}
                return Hint(NextVillageAction);
            }
            if(id=="village:well")return Hint("El pozo es el punto de encuentro del pueblo. Dalia vende provisiones en su puesto, al este de la plaza.");
            if(id=="village:workshop")return WorkshopRestored ? TryUseWorkshopService() : TalkToVillager("village:blacksmith");
            if(id=="cargo"&&!d.cargo){d.cargo=true;Inventory.AddWood(8);Inventory.AddStone(4);Changed("Cargamento recuperado. La exploradora: «Esas raíces también rodean la cantera». +8 madera, +4 piedra."+AddInfluence(2));return true;}
            if(id=="bench"&&!d.bench){if(!d.cargo||Inventory.Wood<8||Inventory.Stone<4)return Hint("Recupera el cargamento y trae 8 madera y 4 piedra.");Inventory.TryRemoveWood(8);Inventory.TryRemoveStone(4);d.bench=true;Inventory.AddCoins(30);Changed("Banco reparado. +30 oro para mejorar el pico [F]. Camino a la cantera abierto."+AddInfluence(3));return true;}
            if(id=="chicken"&&!d.chicken){d.chicken=true;Inventory.AddFood(2);Changed("Gallina rescatada: vuelve a tu granja. +2 raciones."+AddInfluence(2));return true;}
            if(id=="seal"&&!d.sealStone){if(tool!=FarmTool.Pickaxe||GetComponent<PlayerToolUpgradeController>().PickaxeLevel<2)return Hint("Mejora el pico a nivel 2 en el taller antes de romper este derrumbe.");d.sealStone=true;Inventory.AddStone(12);Changed("Primer sello recuperado: «Encerramos la reliquia para protegerla». Se abre el bosque."+AddInfluence(4));return true;}
            if(id=="portal"){if(d.portal)return Travel(5);if(!d.guardian||!d.sealStone)return Hint("Necesitas los sellos de la cantera y del guardián.");d.portal=true;GetComponent<PlayerRespawnController>()?.SetCheckpoint(ValleyWorld.Center(4)+new Vector3(-9,-4));Changed("El portal despierta. La corrupción viene de dentro. Prepara comida y vuelve a interactuar para entrar."+AddInfluence(5));return true;}
            if(id=="seed"&&d.boss&&!d.seed){d.seed=true;Changed("Has recuperado el Corazón del Valle. Vuelve a casa y devuelve la reliquia al altar."+AddInfluence(3));return true;}
            if(id=="plant"&&d.seed&&!d.restored){d.restored=true;Inventory.AddCoins(100);Inventory.AddFood(5);GetComponent<PlayerSurvivalStats>().IncreaseMaxHealth(2);Changed("¡El valle florece! +100 oro, +5 raciones y +2 vida máxima. Raízclara vuelve a vivir."+AddInfluence(10));return true;}
            if(id=="trade"&&d.bench){if(!Inventory.TryRemoveWood(5))return Hint("Trueque: 5 madera por 1 comida.");Inventory.AddFood(1);return Hint("Trueque completado: +1 comida.");}
            if(id.StartsWith("wood:")||id.StartsWith("stone:")||id=="secret")
            {
                bool wood=id.StartsWith("wood:");if(tool!=(wood?FarmTool.Axe:FarmTool.Pickaxe))return Hint("Acércate y pulsa Interactuar; la herramienta correcta se activa sola.");
                int day=FindFirstObjectByType<DayNightCycle>()?.Day??1;string key=id+":"+day;
                if(d.discoveries.Contains(key))return Hint("Vuelve mañana para recolectar de nuevo.");
                d.discoveries.Add(key);if(wood){Inventory.AddWood(4);FarmGameEvents.RaiseTreeHarvested();}else{Inventory.AddStone(4);FarmGameEvents.RaiseRockHarvested();}
                if(id=="secret"){(GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>()).AddIron(3);Inventory.AddCoins(8);Inventory.AddItem("GoldOre",1);if(UnityEngine.Random.value<.35f)Inventory.AddItem("Ruby",1);}
                if(playAction)GetComponent<PlayerCharacterAnimator>()?.PlayAction(PlayerCharacterAnimator.ToolClip(tool),0);
                Changed(id=="secret"?"Veta secreta: +3 hierro, +8 oro, +1 pepita de oro y posible rubi.":"Recurso recogido. Se renueva al día siguiente.");return true;
            }
            return Hint(Objective);
        }
        bool CompleteMaraProject()
        {
            if(!Data.note||Data.maraPantryStocked||!Data.camp)return Hint(GetVillagerDialogue("village:elder"));
            if(Inventory.Food<2)return Hint("Mara pide 2 raciones para la despensa comunal. Tienes "+Inventory.Food+"/2.");
            Data.maraPantryStocked=true;
            Inventory.TryRemoveFood(2);Inventory.AddCoins(15);Inventory.AddWood(5);
            Changed("Despensa comunal abierta. Mara reparte comida y materiales para los nuevos vecinos. +15 oro, +5 madera."+AddInfluence(3));
            return true;
        }
        bool CompleteNicoProject()
        {
            if(WorkshopRestored)return Hint(GetVillagerDialogue("village:blacksmith")+" "+WorkshopService.Summary);
            if(!Data.camp)return Hint(GetVillagerDialogue("village:blacksmith"));
            if(Inventory.Wood<12||Inventory.Stone<8)return Hint("Nico pide 12 madera y 8 piedra para levantar el taller. Tienes "+Inventory.Wood+"/12 madera y "+Inventory.Stone+"/8 piedra.");
            Data.nicoWorkshopRepaired=true;
            Inventory.TryRemoveWood(12);Inventory.TryRemoveStone(8);(GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>()).AddIron(2);Inventory.AddEquipment("WoodenShield");
            string influence=AddInfluence(4);
            Changed("Nico retira las tablas y abre el taller. +2 hierro y escudo de madera. En su banco: "+WorkshopService.Cost+" por "+WorkshopService.Effect+". «Esta vez saldrás con equipo preparado aquí»."+influence);
            return true;
        }

        public bool TryUseWorkshopService()
        {
            var service=WorkshopService;
            if(!service.CanUse)return Hint(service.Summary);
            var stats=GetComponent<PlayerSurvivalStats>();
            if(Inventory==null||stats!=null&&stats.CurrentHealth<=0)return false;
            int previousDay=Data.nicoSupplyDay;
            // Reserve before inventory callbacks so a second interaction cannot pay twice.
            Data.nicoSupplyDay=CurrentDay;
            if(!Inventory.TryRemoveWood(service.WoodCost)){Data.nicoSupplyDay=previousDay;return Hint(WorkshopService.Summary);}
            if(!Inventory.TryRemoveStone(service.StoneCost))
            {
                Inventory.AddWood(service.WoodCost);Data.nicoSupplyDay=previousDay;
                return Hint(WorkshopService.Summary);
            }
            (GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>()).AddIron(service.IronReward);
            Changed("Nico entrega los herrajes: -"+service.WoodCost+" madera, -"+service.StoneCost+" piedra, +"+service.IronReward+" hierro. «Para el equipo de tu próxima salida. Mañana tendré otro encargo»." );
            return true;
        }
        bool CompleteDaliaProject()
        {
            if(Data.daliaGardenRestored||!Data.note)return Hint(GetVillagerDialogue("village:farmer"));
            if(Inventory.Fruit<2||Inventory.Wood<4)return Hint("Dalia pide 2 frutas y 4 madera para abrir la cocina comunal. Tienes "+Inventory.Fruit+"/2 frutas y "+Inventory.Wood+"/4 madera.");
            Data.daliaGardenRestored=true;
            Inventory.TryRemoveFruit(2);Inventory.TryRemoveWood(4);Inventory.AddItem("Carrot",2);Inventory.AddItem("Tomato",1);Inventory.AddFood(2);
            Changed("Cocina de Dalia restaurada. +2 zanahorias, +1 tomate y +2 raciones para tus expediciones."+AddInfluence(4));
            return true;
        }
        bool CompleteRoloProject()
        {
            if(Data.roloMarketOpened)
            {
                if(Inventory.Wood<5)return Hint(GetVillagerDialogue("village:merchant")+" Trueque: 5 madera por 1 ración.");
                Inventory.TryRemoveWood(5);Inventory.AddFood(1);Changed(GetVillagerDialogue("village:merchant")+" Trueque completado: -5 madera, +1 ración.");return true;
            }
            if(!WorkshopRestored||!Data.daliaGardenRestored)return Hint(GetVillagerDialogue("village:merchant"));
            if(Inventory.Coins<20||Inventory.Wood<8)return Hint("Rolo pide 20 oro y 8 madera para levantar el puesto. Tienes "+Inventory.Coins+"/20 oro y "+Inventory.Wood+"/8 madera.");
            Data.roloMarketOpened=true;
            Inventory.TrySpendCoins(20);Inventory.TryRemoveWood(8);Inventory.AddFood(3);Inventory.AddItem("GoldOre",1);Inventory.AddEquipment("Ring");
            Changed("Mercado de Rolo abierto. +3 raciones, +1 pepita de oro y anillo de viajero."+AddInfluence(4));
            return true;
        }
        bool CompleteGuardProject()
        {
            if(Data.guardPostBuilt||!Data.sealStone&&!Data.guardian)return Hint(GetVillagerDialogue("village:guard"));
            var progress=GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>();
            if(progress.Data.iron<6||Inventory.Food<2)return Hint("Iria pide 6 hierro y 2 raciones para organizar guardia. Tienes "+progress.Data.iron+"/6 hierro y "+Inventory.Food+"/2 raciones.");
            Data.guardPostBuilt=true;
            progress.SpendIron(6);Inventory.TryRemoveFood(2);Inventory.AddEquipment("IronShield");Inventory.AddItem("EarthEssence",1);
            Changed("Puesto de guardia levantado. Iria protege la entrada: escudo de hierro y esencia de tierra."+AddInfluence(5));
            return true;
        }
        bool Hint(string s){FarmNotificationCenter.Show(s);return false;}
        public void Defeated(bool boss)
        {
            if(boss){if(Data.boss)return;Data.boss=true;Changed("¡Rey Limo derrotado! Abre el cofre para recuperar la reliquia."+AddInfluence(5));}
            else {if(Data.guardian)return;Data.guardian=true;Inventory.AddFood(3);Changed("El guardián queda libre. Segundo sello recuperado; los animales regresan al bosque. +3 raciones."+AddInfluence(4));}
        }
        void OnDestroy(){if(World!=null)Destroy(World.gameObject);}
    }
    public sealed class ValleyInteraction : WorldInteractable
    {
        public ValleyCampaign Campaign;public string Id, Label;
        const float GatherReach=1.35f;
        bool gathering;PlayerInventory activeInventory;FarmTool activeTool;float gatherStartedAt,gatherEndsAt,nextHitAt;int hitCount,completedHits;
        PlayerCharacterAnimator activeAnimator;
        PlayerSurvivalStats activeStats;
        ValleyData activeData;
        string activeClip;
        public bool IsGathering => gathering;
        public override bool IsAvailable => gathering || Campaign!=null && Id!=null &&
            (Id=="note"?!Campaign.Data.note:Id=="plant"?Campaign.Data.seed&&!Campaign.Data.restored:
             Id=="cargo"?!Campaign.Data.cargo:Id=="bench"?!Campaign.Data.bench:
             Id=="seed"?Campaign.Data.boss&&!Campaign.Data.seed:true);
        public bool SupportsTool(FarmTool tool) => Id.StartsWith("wood:") ? tool == FarmTool.Axe :
            Id.StartsWith("stone:") || Id == "secret" || Id == "seal" ? tool == FarmTool.Pickaxe : true;
        public override string GetInteractionLabel(FarmTool tool)=>VillageQuests.IsVillager(Id)?"Hablar con "+VillageResidents.Name(Id):gathering?WorkText+" "+ProgressText():AlreadyGatheredToday()?"Recurso agotado hoy":"Interactuar: "+Label;
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            var stats=inventory!=null?inventory.GetComponent<PlayerSurvivalStats>():null;
            if(!isActiveAndEnabled||Campaign==null||inventory==null||!inventory.isActiveAndEnabled||Id==null||!SupportsTool(tool)||inventory!=Campaign.Inventory||Vector2.Distance(transform.position,inventory.transform.position)>(IsTimedResource?GatherReach:1.5f)||stats!=null&&stats.CurrentHealth<=0)return;
            if(gathering)return;
            if(VillageQuests.IsVillager(Id)){Campaign.TalkToVillager(Id,transform);return;}
            if(Id=="village:workshop"&&!Campaign.WorkshopRestored){Campaign.TalkToVillager("village:blacksmith",transform);return;}
            if(IsTimedResource&&AlreadyGatheredToday()){Campaign.Use(Id,tool);return;}
            var animator=inventory.GetComponent<PlayerCharacterAnimator>();
            if(IsTimedResource&&animator!=null&&Application.isPlaying)
            {
                if(!animator.isActiveAndEnabled||animator.MovementLocked)return;
                BeginGathering(tool,inventory);return;
            }
            Campaign.Use(Id,tool);
        }
        bool IsTimedResource=>Id!=null&&(Id.StartsWith("wood:")||Id.StartsWith("stone:")||Id=="secret");
        string WorkText=>Id!=null&&Id.StartsWith("wood:")?"Talando árbol":Id=="secret"?"Picando veta":"Picando roca";
        float WorkDuration=>Id!=null&&Id.StartsWith("wood:")?2.5f:Id=="secret"?2.8f:2.2f;
        int WorkHits=>Id=="secret"?5:4;
        void BeginGathering(FarmTool tool,PlayerInventory inventory)
        {
            activeInventory=inventory;activeTool=tool;hitCount=Mathf.Max(3,WorkHits);completedHits=0;gatherStartedAt=Time.time;gatherEndsAt=Time.time+WorkDuration;nextHitAt=gatherStartedAt+WorkDuration/(hitCount+1f);gathering=true;
            activeAnimator=inventory.GetComponent<PlayerCharacterAnimator>();activeStats=inventory.GetComponent<PlayerSurvivalStats>();
            activeClip=PlayerCharacterAnimator.ToolClip(tool);activeData=Campaign.Data;
            if(activeStats!=null)activeStats.StatsChanged+=CheckGathererHealth;
            activeAnimator.PlayAction(activeClip,WorkDuration,transform.position);
            FarmNotificationCenter.SetPrompt(WorkText+" "+ProgressText());
        }
        void Update()
        {
            if(!gathering)return;
            if(Campaign==null||Campaign.Data!=activeData||activeInventory==null||!activeInventory.isActiveAndEnabled||
                Vector2.Distance(transform.position,activeInventory.transform.position)>GatherReach||
                activeStats!=null&&activeStats.CurrentHealth<=0||activeAnimator==null||!activeAnimator.isActiveAndEnabled)
            {CancelGathering();return;}
            bool finished=Time.time>=gatherEndsAt;
            bool naturalEnd=finished&&activeAnimator.CurrentClip=="Idle"&&!activeAnimator.MovementLocked;
            if(activeAnimator.CurrentClip!=activeClip&&!naturalEnd){CancelGathering();return;}
            bool hit=false;
            while(Time.time>=nextHitAt&&completedHits<hitCount)
            {
                completedHits++;hit=true;
                nextHitAt=gatherStartedAt+WorkDuration*(completedHits+1f)/(hitCount+1f);
            }
            // Catch up after a slow frame with a single visual response and no per-hit text.
            if(hit)VisibleHitFeedback.Play(gameObject);
            FarmNotificationCenter.SetPrompt(WorkText+" "+ProgressText());
            if(!finished)return;
            var tool=activeTool;CancelGathering();Campaign.Use(Id,tool,false);
        }
        void CheckGathererHealth()
        {
            if(activeStats!=null&&activeStats.CurrentHealth<=0)CancelGathering();
        }
        public void CancelGathering()
        {
            if(!gathering)return;
            var animator=activeAnimator;string clip=activeClip;
            ClearGathering();
            if(animator!=null&&animator.CurrentClip==clip)animator.CancelAction();
        }
        void OnDisable()=>CancelGathering();
        void ClearGathering()
        {
            if(activeStats!=null)activeStats.StatsChanged-=CheckGathererHealth;
            gathering=false;activeInventory=null;activeAnimator=null;activeStats=null;activeData=null;activeClip=null;
            activeTool=FarmTool.Sword;hitCount=0;completedHits=0;gatherStartedAt=0;gatherEndsAt=0;nextHitAt=0;
        }
        bool AlreadyGatheredToday()
        {
            if(!IsTimedResource||Campaign==null)return false;
            int day=FindFirstObjectByType<DayNightCycle>()?.Day??1;
            return Campaign.Data.discoveries.Contains(Id+":"+day);
        }
        float Progress=>gathering?Mathf.Clamp01((Time.time-gatherStartedAt)/Mathf.Max(.01f,WorkDuration)):1f;
        string ProgressText(){int percent=Mathf.RoundToInt(Progress*100f);return "["+BuildProgressBar(Progress)+"] "+percent+"%";}
        static string BuildProgressBar(float progress){const int segmentCount=10;int filled=Mathf.RoundToInt(Mathf.Clamp01(progress)*segmentCount);return new string('#',filled)+new string('-',segmentCount-filled);}
    }
}
