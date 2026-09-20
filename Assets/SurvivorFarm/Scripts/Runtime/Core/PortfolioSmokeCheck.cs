using System;
using System.Collections;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Opt-in executable verification; never runs during a normal player's session.</summary>
    public sealed class PortfolioSmokeCheck : MonoBehaviour
    {
        private string output;
        private bool failed;
        private float started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--portfolio-smoke")>=0)
                new GameObject("Portfolio executable QA").AddComponent<PortfolioSmokeCheck>();
        }
        private void Awake()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Portfolio"));Directory.CreateDirectory(output);
            Application.runInBackground=true;started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;
            failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");
        }
        private void Update(){if(Time.realtimeSinceStartup-started>180)Finish(false,"Verification timed out.");}
        private IEnumerator Start()
        {
            var run=Run();
            while(true)
            {
                bool next;
                try{next=run.MoveNext();}catch(Exception error){Finish(false,error.ToString());yield break;}
                if(!next)break;yield return run.Current;
            }
            Finish(!failed,"Windows executable: guided movement/combo/charge tutorial, authored charged sword effect and impact, real animated 1-2-3 combo, elite finisher, retired defensive construction blocked without spending, visual backpack, all four outer ore nodes, world work clock, crops, recipes, save/load, bounded raids, boss patterns, phase II, ending; staged screenshots captured. This is automated verification, not a human balance playtest.");
        }
        private IEnumerator Run()
        {
            PortfolioSession session=null;
            for(int i=0;i<200;i++){session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;yield return null;}
            Require(session!=null&&session.IsReady,"Main did not initialize.");
            yield return new WaitForSecondsRealtime(.5f);Capture("01-title");yield return new WaitForSecondsRealtime(.3f);
            var menu=session.GetComponent<UI.DemoFrontEnd>();
            menu.Options();yield return new WaitForSecondsRealtime(.3f);Capture("33-options");
            menu.LoadScreen();yield return new WaitForSecondsRealtime(.3f);Capture("34-load-slots");menu.Home();
            session.BeginNewGame();
            yield return ExerciseTutorial(session);
            yield return new WaitForSecondsRealtime(.7f);Capture("02-day");yield return new WaitForSecondsRealtime(.3f);
            yield return ExerciseWorldRenewal(session);
            var mastery=session.Player.GetComponent<ToolMastery>();session.Player.AddCoins(200);session.Player.AddWood(50);session.Player.AddStone(50);
            for(int tier=1;tier<=2;tier++)
            {
                for(int i=0;i<ToolMastery.Required(FarmTool.Pickaxe,tier);i++)mastery.Earn(FarmTool.Pickaxe,tier);
                Require(mastery.Purchase(FarmTool.Pickaxe),"Usage unlock + purchase failed.");
            }
            session.Player.GetComponent<UI.MasteryWindow>().Open();
            yield return new WaitForSecondsRealtime(.4f);Capture("14-mastery-tree");session.Player.GetComponent<UI.MasteryWindow>().Close();
            yield return ExerciseCombo(session);
            yield return ExerciseSuppliesAndExploration(session);
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();
            var plots=FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None);
            foreach(var plot in plots)
            {
                campaign.Teleport(plot.transform.position+Vector3.down*.8f);
                plot.Interact(FarmTool.Hoe,player);plot.Interact(FarmTool.WateringCan,player);plot.AdvanceGrowth(36);
            }
            player.GetComponent<PlayerCharacterAnimator>().CancelAction();
            campaign.Teleport(new Vector3(8.5f,-5.8f));
            yield return new WaitForSecondsRealtime(.6f);Capture("03-farming");yield return new WaitForSecondsRealtime(.3f);
            foreach(var plot in plots){campaign.Teleport(plot.transform.position+Vector3.down*.8f);plot.Interact(FarmTool.Hoe,player);}
            Require(player.Fruit>=12,"Crop rewards missing.");
            Require(player.GetComponent<PlayerCraftingController>().Craft("Food"),"Cooking failed.");
            campaign.Teleport(new Vector3(1,-2));
            var save=FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            int wood=player.Wood;player.AddWood(30);Require(save.TryLoadGame()&&player.Wood==wood,"Checkpoint failed.");
            for(int day=1;day<=3;day++)
            {
                session.Advance(session.Remaining+.1f);session.Advance(session.Settings.duskSeconds+1);
                // Allow real AI movement and committed attacks before accelerating the wave schedule.
                for(int i=0;i<6;i++){session.Advance(5);yield return new WaitForSecondsRealtime(.15f);}
                if(day==1){yield return new WaitForSecondsRealtime(.6f);Capture("04-night");yield return new WaitForSecondsRealtime(.3f);}
                for(int i=0;i<180&&session.Phase==SlicePhase.Night;i++)
                {
                    session.Advance(3);
                    foreach(var enemy in session.Raids.Enemies)if(enemy.IsAlive){enemy.TakeDamage(100,player);enemy.ReturnToPool();}
                }
                Require(session.Phase==(day==3?SlicePhase.BossIntro:SlicePhase.Dawn),"Night did not finish.");
                if(day<3)session.Advance(9);
            }
            session.Advance(6);
            var boss=session.Raids.Boss;
            Require(boss!=null,"Boss missing.");
            player.GetComponent<PlayerSurvivalStats>().GrantInvulnerability(120);
            campaign.Teleport(new Vector3(2,-5));
            yield return new WaitForSecondsRealtime(1.25f);Capture("05-boss");yield return new WaitForSecondsRealtime(.3f);
            // Execute the actual attack state machine and verify that all four patterns occur.
            var seen=new System.Collections.Generic.HashSet<int>();
            Time.timeScale=4;
            float end=Time.realtimeSinceStartup+10;
            while(Time.realtimeSinceStartup<end&&session.Phase==SlicePhase.Boss)
            {seen.Add(session.Raids.BossPattern.Pattern);yield return null;}
            Require(seen.Count==4,"Not all four boss attacks executed.");
            Time.timeScale=1;boss.TakeDamage(boss.MaximumHealth/2/(session.Raids.BossPattern.IsExposed?2:1),player);
            Require(boss.IsAlive&&session.Raids.BossPattern.PhaseTwo,"Second phase missing.");
            yield return new WaitForSecondsRealtime(1.1f);Capture("06-phase-two");yield return new WaitForSecondsRealtime(.3f);
            boss.TakeDamage(1000,player);Require(session.Phase==SlicePhase.Victory,"Victory missing.");
            yield return new WaitForSecondsRealtime(1.2f);Capture("07-ending");yield return new WaitForSecondsRealtime(.5f);
        }
        private IEnumerator ExerciseWorldRenewal(PortfolioSession session)
        {
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();
            Require(!player.GetComponent<PlayerPetController>().Equipped,"The cat must be a purchase, not a free starting companion.");
            campaign.Teleport(new Vector3(0,5));yield return new WaitForSecondsRealtime(.7f);Capture("35-native-bridge-water");
            var tree=FindObjectsByType<TreeResource>(FindObjectsSortMode.None).First(t=>Mathf.Abs(t.transform.position.x+8)<.1f);
            Require(tree.GetComponent<CircleCollider2D>().enabled&&!tree.GetComponent<CircleCollider2D>().isTrigger,"Village trunk is not solid.");
            campaign.Teleport(tree.transform.position+Vector3.up*.9f);yield return new WaitForSecondsRealtime(.5f);Capture("36-behind-village-tree");
            campaign.Teleport(tree.transform.position+Vector3.down);yield return new WaitForSecondsRealtime(.5f);Capture("37-front-of-tree");
            var flower=FindObjectsByType<ForagePlant>(FindObjectsSortMode.None).First(p=>!p.Fruit);
            campaign.Teleport(flower.transform.position+Vector3.down*.7f);int seeds=player.CommonSeeds;
            flower.Interact(FarmTool.Axe,player);Require(player.CommonSeeds==seeds+1,"Foraging did not pay seeds.");
            var house=VillageHouseHealth.All.First();campaign.Teleport(house.ContactPoint(house.transform.position+Vector3.down*3)+Vector2.down);
            house.TakeDamage(8,null);yield return new WaitForSecondsRealtime(.5f);Capture("38-house-health");
            house.TakeDamage(100,null);yield return new WaitForSecondsRealtime(.5f);Capture("39-house-ruins");
            Require(!house.IsAlive,"House destruction failed.");house.Restore(null);
            campaign.Teleport(new Vector3(1,-2));
        }
        private IEnumerator ExerciseCombo(PortfolioSession session)
        {
            // An arranged receiver fixture uses actual arrows, melee windups and damage.
            // It is intentionally separate from the accelerated wave/state-machine check.
            var player = session.Player;
            var combat = player.GetComponent<PlayerCombatController>();
            var belt = player.GetComponent<PlayerToolbelt>();
            var campaign = player.GetComponent<ValleyCampaign>();
            var enemy = session.Raids.Enemies[0];
            enemy.ConfigureRaid(session, RaidRole.Brute, null);
            enemy.ActivateFromPool(new Vector3(4, -7));
            campaign.Teleport(new Vector3(2, -7));
            int normal = combat.GetAttackDamage(FarmTool.Sword);
            var final = combat.Combo.Definition.attacks[2];
            int chainDamage = normal * 2 + Mathf.RoundToInt(normal * final.damageMultiplier) + final.bonusDamage;
            belt.Select(FarmTool.Bow);
            int shots = 0;
            while (enemy.CurrentHealth > chainDamage && shots++ < 15)
            {
                combat.AttackTarget(enemy);
                yield return new WaitForSeconds(.8f);
            }
            Require(enemy.CurrentHealth > normal * 2, "Receiver must survive the two opening cuts.");
            belt.Select(FarmTool.Sword);
            int beforeFinishers = player.GetComponent<HitFeedback>().FinisherCount;
            for (int step = 0; step < 3; step++)
            {
                campaign.Teleport(enemy.transform.position + Vector3.left * .8f);
                Physics2D.SyncTransforms();
                int health = enemy.CurrentHealth;
                combat.AttackTarget(enemy);
                Require(enemy.CurrentHealth == health, "Melee damage occurred before the animated contact.");
                float deadline = Time.realtimeSinceStartup + 3;
                while (enemy.CurrentHealth == health && Time.realtimeSinceStartup < deadline) yield return null;
                Require(enemy.CurrentHealth < health, "Animated melee did not connect.");
                yield return null;
                Capture("08-combo-" + (step + 1));
                yield return new WaitForSeconds(combat.Combo.Definition.attacks[step].duration * .6f + .1f);
            }
            Require(!enemy.IsAlive, "Combo failed to finish the elite.");
            Require(player.GetComponent<HitFeedback>().FinisherCount == beforeFinishers + 1, "Elite finisher missing.");
            yield return new WaitForSecondsRealtime(.4f);
            Require(Mathf.Approximately(Time.timeScale, 1), "Impact timing did not restore normal speed.");
            enemy.ReturnToPool();
            combat.CancelMelee();
        }

        private IEnumerator ExerciseSuppliesAndExploration(PortfolioSession session)
        {
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();var build=player.GetComponent<ConstructionSystem>();
            // Arranged materials only in --qa; retired defenses stay unavailable
            // even with enough resources or a direct request through their old APIs.
            player.AddWood(100);player.AddStone(100);player.GetComponent<AdventureProgress>().AddIron(15);player.AddItem("GoldOre",4);
            int wood=player.Wood,stone=player.Stone;
            foreach(string kind in FortressPieces.Palette)
            {
                Require(!session.CanBuild(kind)&&!PortfolioSession.IsDemoRecipe(kind),"A retired defense remains available: "+kind);
                build.Begin(kind);Require(!ConstructionSystem.IsPlacing,"A retired defense opened placement: "+kind);
                Require(!build.Pack(kind)&&!build.Place(kind,new Vector2(1,-12)),"A retired defense was created: "+kind);
            }
            Require(player.Wood==wood&&player.Stone==stone,"Rejected defenses consumed resources.");
            Require(!build.Buildings.Any(b=>BackpackActions.IsRetiredDefense(b.kind)),"Authored defenses remain in the village.");
            campaign.Teleport(new Vector3(1,-2));yield return new WaitForSecondsRealtime(.4f);Capture("09-village-security");
            FindFirstObjectByType<UI.InventoryPanelSystem>().Toggle();
            yield return new WaitForSecondsRealtime(.4f);Capture("10-backpack");yield return new WaitForSecondsRealtime(.25f);
            FindFirstObjectByType<UI.InventoryPanelSystem>().Close();
            foreach(var vein in FindObjectsByType<IronVein>(FindObjectsSortMode.None).OrderBy(v=>v.Index))
            {
                campaign.Teleport(vein.transform.position+Vector3.down);player.GetComponent<PlayerToolbelt>().Select(FarmTool.Pickaxe);
                yield return new WaitForSecondsRealtime(.4f);
                var oreArt=vein.transform.Find(vein.Precious?"Gem":"Iron").GetComponentInChildren<SpriteRenderer>();
                Require(oreArt.sprite!=null&&oreArt.enabled,"Ore marking has no visible sprite.");
                Require(oreArt.sortingOrder>vein.transform.Find("Sprite").GetComponent<SpriteRenderer>().sortingOrder,"Ore marking is hidden behind its rock.");
                Capture("12-ore-ready-"+vein.Index);yield return new WaitForSecondsRealtime(.2f);vein.Interact(FarmTool.Pickaxe,player);
                yield return new WaitForSecondsRealtime(.8f);Capture("21-work-clock-"+vein.Index);
                yield return new WaitForSecondsRealtime(2.35f);
                Require(!vein.IsMining,"Mining did not complete.");
                Capture("11-exploration-"+vein.Index);yield return new WaitForSecondsRealtime(.25f);
            }
            player.GetComponent<PlayerToolbelt>().Select(FarmTool.Sword);
            Require(player.GetAvailableItemCount("GoldOre")>=6,"Outer ore reward missing.");
            campaign.Teleport(new Vector3(1,-2));
            campaign.Teleport(new Vector3(-5,9.2f));yield return new WaitForSecondsRealtime(.6f);Capture("16-river-bank");
            var chicken=FindObjectsByType<AnimalResource>(FindObjectsSortMode.None).First(a=>a.name.StartsWith("Chicken "));
            int food=player.Food;chicken.TakeDamage(100,player);Require(player.Food==food+2&&!chicken.IsAlive,"Chicken did not pay meat once.");
            campaign.Teleport(new Vector3(1,-2));
        }

        private IEnumerator ExerciseTutorial(PortfolioSession session)
        {
            var player=session.Player;var combat=player.GetComponent<PlayerCombatController>();
            var intro=session.gameObject.AddComponent<UI.FarmIntroduction>();intro.Open();float clock=session.Remaining;
            yield return new WaitForSecondsRealtime(3);Capture("13-introduction");
            Require(intro.TryBeginMovement(Vector2.right),"Movement lesson did not accept its control.");
            player.GetComponent<ValleyCampaign>().Teleport(player.transform.position+Vector3.right*1.1f);
            yield return new WaitForSecondsRealtime(2);Capture("17-practice-enemy");intro.Next();intro.Next();
            Require(intro.CurrentLesson==UI.FarmIntroduction.Lesson.Combo,"Combo practice did not begin.");
            for(int step=0;step<3;step++)
            {
                intro.PracticeEnemy.transform.position=player.transform.position+Vector3.right*.8f;Physics2D.SyncTransforms();
                combat.AttackTarget(intro.PracticeEnemy);yield return new WaitForSeconds(step==2?.4f:.46f);
            }
            yield return new WaitForSecondsRealtime(1.6f);
            Require(intro.CurrentLesson==UI.FarmIntroduction.Lesson.ChargeHint,"Third contact did not advance the lesson.");
            intro.Next();intro.Next();Require(combat.BeginCharge(),"Sword charge was unavailable.");
            yield return new WaitForSecondsRealtime(1.12f);Capture("18-sword-charged");
            var enemy=intro.PracticeEnemy;int health=enemy.CurrentHealth;
            enemy.transform.position=player.transform.position+Vector3.right;Physics2D.SyncTransforms();combat.ReleaseCharge(enemy);
            float deadline=Time.realtimeSinceStartup+2;
            while(enemy.CurrentHealth==health&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(enemy.CurrentHealth<health&&combat.LastAttackWasCharged,"Charged sword failed to resolve contact.");
            Capture("19-charged-impact");yield return new WaitForSecondsRealtime(1.4f);
            Require(intro.CurrentLesson==UI.FarmIntroduction.Lesson.Mission,"Charge did not finish the practice.");
            Require(Mathf.Approximately(session.Remaining,clock),"Night countdown ran during the tutorial.");
            intro.Finish();yield return new WaitForSecondsRealtime(.3f);
        }

        private void Capture(string name)
        {
            // Explicit offscreen rendering works even when the Windows QA helper is hidden.
            // Temporarily route overlay canvases through the same camera, then restore them.
            var camera=Camera.main;
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var orders=canvases.Select(c=>c.sortingOrder).ToArray();
            var target=new RenderTexture(1280,720,24);
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                for(int i=0;i<canvases.Length;i++)
                {var canvas=canvases[i];canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=30000+orders[i];}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                var pixels=texture.GetPixels32();int visible=0;
                for(int i=0;i<pixels.Length;i+=101)if(pixels[i].r>12||pixels[i].g>12||pixels[i].b>12)visible++;
                Require(visible>100,"Screenshot contains no rendered scene.");
                File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                for(int i=0;i<canvases.Length;i++)
                {var canvas=canvases[i];canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.sortingOrder=orders[i];}
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;target.Release();Destroy(target);Destroy(texture);
            }
        }
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        private void Finish(bool pass,string message){File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS\n":"FAIL\n")+message);Application.Quit(pass?0:1);}
        private void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
