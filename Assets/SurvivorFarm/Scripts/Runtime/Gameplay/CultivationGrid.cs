using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CultivationGrid : MonoBehaviour
    {
        public const float Size = 1.4f;
        static Tilemap paths;
        public static FarmingPlot At(Vector3 pointer) => null;
        public static bool CanWork(FarmingPlot plot,Vector3 player,float reach,FarmTool tool) => false;
        public static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x/Size)*Size,Mathf.Round(p.y/Size)*Size,0);
        public static bool IsGreen(Vector3 p)
        {
            if(paths==null) paths=FindObjectsByType<Tilemap>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Farm Paths");
            if(paths==null)return false;
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
                if(paths.HasTile(paths.WorldToCell(p+new Vector3(x*.6f,y*.6f))))return false;
            return Mathf.Abs(p.x)<36 && Mathf.Abs(p.y)<21;
        }
        public static bool Clear(Vector3 p) => !Physics2D.OverlapBoxAll(p,new Vector2(1.2f,1.2f),0).Any(c=>(!c.isTrigger || c.GetComponentInParent<WorldInteractable>()!=null) && c.GetComponentInParent<FarmingPlot>()==null && c.GetComponentInParent<SurvivorFarm.Runtime.Player.PlayerInventory>()==null);
        // Ground queries remain available for construction; no cultivation grid is installed.
        public void ArrangePlots() { }
    }
}
