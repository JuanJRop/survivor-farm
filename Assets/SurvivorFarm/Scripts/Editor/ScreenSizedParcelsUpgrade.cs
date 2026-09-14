using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class ScreenSizedParcelsUpgrade
    {
        static ScreenSizedParcelsUpgrade()=>EditorApplication.update+=Tick;
        static void Tick()
        {
            const string request="Library/ExpandParcels.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Library/ExpandParcels-result.txt","PASS: 24 x 14 parcels saved, terrain expanded and unlock gates aligned.");}
            catch(Exception e){File.WriteAllText("Library/ExpandParcels-result.txt",e.ToString());Debug.LogException(e);}
        }
        static Vector3 Move(Vector3 p)=>p+new Vector3(Mathf.Clamp(Mathf.RoundToInt(p.x/8),-1,1)*16,Mathf.Clamp(Mathf.RoundToInt(p.y/8),-1,1)*6,0);
        [MenuItem("Survivor Farm/Expand Parcels to Screen Size")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!="Assets/SurvivorFarm/Scenes/Main.unity")throw new Exception("Open Main outside Play.");
            var outside=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Outdoor World");
            if(outside.Find("Screen Sized Parcels")!=null)return;
            Directory.CreateDirectory("Design/Validation/Parcels");
            if(!File.Exists("Design/Validation/Parcels/Main-before-expansion.unity.backup"))File.Copy(scene.path,"Design/Validation/Parcels/Main-before-expansion.unity.backup");
            var originalPlots=outside.GetComponentsInChildren<FarmingPlot>(true);
            var all=outside.GetComponentsInChildren<Transform>(true);
            var targets=new HashSet<Transform>();
            foreach(var t in all)
                if(t.GetComponent<WorldInteractable>()!=null||t.GetComponent<ResourceSpawnPoint>()!=null||t.GetComponent<EnemyAIBase>()!=null||t.GetComponent<AnimalRoamingVisual>()!=null)targets.Add(t);
            foreach(var t in targets.Where(t=>!targets.Any(a=>a!=t&&t.IsChildOf(a))).ToArray())t.position=Move(t.position);
            // Outdoor return points live alongside interiors, outside the outdoor hierarchy.
            foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Shop Outside Spawn"||t.name=="Dungeon Outside Spawn"))t.position=Move(t.position);
            foreach(var zone in outside.GetComponentsInChildren<LandUnlockZone>(true))
            {
                zone.transform.localScale=new Vector3(23.85f,13.85f,1);
                var data=new SerializedObject(zone);
                var previous=data.FindProperty("prerequisiteZone").objectReferenceValue as LandUnlockZone;
                var point=data.FindProperty("interactionPoint").objectReferenceValue as Transform;
                Vector3 from=previous!=null?previous.transform.position:Vector3.zero;
                point.position=(from+zone.transform.position)*.5f+(from-zone.transform.position).normalized*.25f;
            }
            var start=outside.Find("Starting Unlocked Square");if(start!=null)start.localScale=new Vector3(23.85f,13.85f,1);
            var maps=outside.GetComponentsInChildren<Tilemap>(true);
            var terrain=maps.First(t=>t.name=="Spring Grass");var paths=maps.First(t=>t.name=="Farm Paths");
            var grass=terrain.GetTile(Vector3Int.zero);var path=paths.GetTile(Vector3Int.zero);
            for(int y=-44;y<44;y++)for(int x=-74;x<74;x++)terrain.SetTile(new Vector3Int(x,y,0),grass);
            for(int y=-42;y<42;y++)for(int x=-1;x<=1;x++)paths.SetTile(new Vector3Int(x,y,0),path);
            for(int x=-72;x<72;x++)for(int y=0;y<=2;y++)paths.SetTile(new Vector3Int(x,y,0),path);
            var marker=new GameObject("Screen Sized Parcels");marker.transform.SetParent(outside,false);
            // Appending names after the old Grass Cell names preserves positional save ordering.
            var template=originalPlots.First();int added=0;
            Physics2D.SyncTransforms();
            for(int row=-1;row<=1;row++)for(int col=-1;col<=1;col++)
                for(float y=-5.5f;y<=5.5f;y+=1.42f)for(float x=-10.5f;x<=10.5f;x+=1.42f)
                {
                    Vector3 p=new Vector3(col*24+x,row*14+y,0);
                    if(Mathf.Abs(p.x)<1.3f||p.y>-.8f&&p.y<1.8f||originalPlots.Any(plot=>Vector2.Distance(plot.transform.position,p)<1.15f))continue;
                    if(Physics2D.OverlapCircleAll(p,.6f).Any(c=>!c.isTrigger&&c.GetComponent<LandUnlockZone>()==null))continue;
                    var cell=Object.Instantiate(template,marker.transform);cell.name="ZZ Expanded Soil "+added.ToString("D4");cell.transform.position=p;
                    cell.ConfigureArt(OriginalWorldArtUpgrade.S("Soil"),OriginalWorldArtUpgrade.S("WetSoil"),Enumerable.Range(0,5).Select(i=>OriginalWorldArtUpgrade.S("Crop"+i)).ToArray());
                    added++;
                }
            foreach(var component in outside.GetComponentsInChildren<Component>(true))
            {
                if(component==null)continue;EditorUtility.SetDirty(component);
                if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Design/Validation/Parcels/layout.txt",$"Each parcel: 24 x 14 world units. Camera at 16:9 and size 6 sees 21.33 x 12. Existing farming cells: {originalPlots.Length}. Additional cells: {added}. Original sprites and actor scales retained. Old plot names and resource IDs retained.");
        }
    }
}
