using System;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public static class PracticeSceneBuilder
    {
        public static readonly string[] ReleaseScenes={"Assets/SurvivorFarm/Scenes/Main.unity",
            "Assets/SurvivorFarm/Scenes/ArenaCombate.unity","Assets/SurvivorFarm/Scenes/TallerGranja.unity"};
        [MenuItem("Survivor Farm/Practice/Rebuild practice scenes from Main")]
        public static void BuildScenes()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            for(int i=1;i<=2;i++)
            {
                var scene=EditorSceneManager.OpenScene(ReleaseScenes[0]);
                var session=UnityEngine.Object.FindFirstObjectByType<PortfolioSession>();
                if(session==null)throw new InvalidOperationException("Main has no PortfolioSession. Main was not modified.");
                var mode=session.gameObject.AddComponent<PracticeSession>();
                var serialized=new SerializedObject(mode);serialized.FindProperty("mode").enumValueIndex=i-1;
                var array=serialized.FindProperty("legacyTemplates");array.arraySize=3;
                string[] names={"Limo","Murcielago","Golem"};
                var templates=UnityEngine.Object.FindObjectsByType<BasicEnemyAI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                for(int n=0;n<3;n++)
                {
                    var template=templates.FirstOrDefault(e=>new SerializedObject(e).FindProperty("enemyName").stringValue==names[n]);
                    if(template==null)throw new InvalidOperationException("Missing authored enemy: "+names[n]);
                    array.GetArrayElementAtIndex(n).objectReferenceValue=template;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if(!EditorSceneManager.SaveScene(scene,ReleaseScenes[i]))throw new InvalidOperationException("Could not save practice scene.");
            }
            var existing=EditorBuildSettings.scenes.ToList();
            foreach(var path in ReleaseScenes)
            {
                var scene=existing.FirstOrDefault(s=>s.path==path);
                if(scene==null)existing.Add(new EditorBuildSettingsScene(path,true));else scene.enabled=true;
            }
            EditorBuildSettings.scenes=existing.ToArray();AssetDatabase.SaveAssets();
            Debug.Log("PRACTICE_SCENES_READY: ArenaCombate + TallerGranja; Main preserved.");
        }
    }
}
