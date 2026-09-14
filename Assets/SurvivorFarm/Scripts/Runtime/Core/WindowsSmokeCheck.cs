using System;
using System.Collections;
using System.IO;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
namespace SurvivorFarm.Runtime.Core
{
    public sealed class WindowsSmokeCheck : MonoBehaviour
    {
        string output;bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--qa")>=0&&!PortfolioSession.Active)new GameObject("Windows QA").AddComponent<WindowsSmokeCheck>();}
        void Awake(){output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;}
        void Log(string message,string trace,LogType kind){if(kind==LogType.Exception||kind==LogType.Error){failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n");}}
        IEnumerator Start()
        {
            yield return new WaitForSeconds(2);
            var inventory=FindFirstObjectByType<PlayerInventory>();var save=FindFirstObjectByType<GameSaveSystem>();
            if(inventory==null||save==null||FindFirstObjectByType<AdventureWindow>()==null){Finish(false,"Missing main scene systems");yield break;}
            inventory.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);
            var valley=inventory.GetComponent<SurvivorFarm.Runtime.Gameplay.ValleyCampaign>();
            if(valley==null||valley.World.Art("Portal")==null||valley.World.BossFrame()==null){Finish(false,"Missing valley story or original portal/boss art");yield break;}
            inventory.GetComponent<AdventureProgress>().AddIron(7);int iron=inventory.GetComponent<AdventureProgress>().Data.iron;save.SaveGame(false);inventory.GetComponent<AdventureProgress>().Data.iron=0;
            bool restored=save.TryLoadGame()&&inventory.GetComponent<AdventureProgress>().Data.iron==iron;
            var window=FindFirstObjectByType<AdventureWindow>();window.Open("Journal");yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"windows.png"));yield return new WaitForSeconds(.5f);window.Close();
            Finish(restored&&!failed,"Windows x64 boot, six-zone valley campaign, portal/boss art, journal UI and save/load. QA slot isolated.");
        }
        void Finish(bool pass,string detail){File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS: ":"FAIL: ")+detail);Application.Quit(pass?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
