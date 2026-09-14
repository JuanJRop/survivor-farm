using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SurvivorFarm.Editor
{
    [InitializeOnLoad] public static class UsabilityAudit
    {
        static UsabilityAudit(){EditorApplication.update+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists("Library/InspectUsability.request"))return;
            File.Delete("Library/InspectUsability.request");Directory.CreateDirectory("Design/Validation/Usability");
            File.WriteAllLines("Design/Validation/Usability/colliders.txt",Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>!c.isTrigger&&Mathf.Abs(c.bounds.center.x)<6&&Mathf.Abs(c.bounds.center.y)<6).Select(c=>c.name+" | "+c.GetType().Name+" | "+c.transform.position+" bounds "+c.bounds+" scale "+c.transform.lossyScale));
        };}
    }
}
