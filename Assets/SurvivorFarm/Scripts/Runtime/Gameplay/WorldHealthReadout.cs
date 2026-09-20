using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Small damage-only health bar. No labels or full-time clutter over every actor.</summary>
    public sealed class WorldHealthReadout : MonoBehaviour
    {
        private EnemyAIBase enemy;private VillageHouseHealth house;
        private SpriteRenderer back,fill;private float previous=1,until;
        private float top=1.45f;
        private static Sprite pixel;
        public bool Visible=>back!=null&&back.enabled;
        public void Configure(VillageHouseHealth value){house=value;Initialize();}
        private void Start(){enemy=GetComponent<EnemyAIBase>();Initialize();}
        private void Initialize()
        {
            if(back!=null)return;
            // Tiny RPG cells include large transparent margins; use the authored head
            // height instead of the full 100px rectangle. Other art retains its bounds.
            if(enemy==null||!EnemyRoster.IsTinyRpg(enemy.CombatStyle))
                foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())if(renderer.sprite!=null)top=Mathf.Max(top,renderer.bounds.max.y-transform.position.y+.15f);
            if(pixel==null)pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.zero,1);
            back=Make("Fondo de vida",new Color(.1f,.13f,.12f),30001);fill=Make("Vida",new Color(.8f,.3f,.27f),30002);
            back.enabled=fill.enabled=false;
        }
        private SpriteRenderer Make(string label,Color color,int order)
        {var go=new GameObject(label);go.transform.SetParent(transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=pixel;sr.color=color;sr.sortingOrder=order;return sr;}
        private void LateUpdate()
        {
            if(back==null)return;
            float ratio=enemy!=null?enemy.CurrentHealth/(float)enemy.MaximumHealth:house!=null?house.Health/(float)house.Maximum:1;
            if(ratio<previous)until=Time.time+5;previous=ratio;
            bool show=ratio>0&&ratio<1&&Time.time<until;back.enabled=fill.enabled=show;
            if(!show)return;
            float width=house!=null?1.3f:.75f;
            float height=enemy!=null&&EnemyRoster.IsTinyRpg(enemy.CombatStyle)?1.1f:top;
            back.transform.position=transform.position+new Vector3(-width*.5f,height);
            fill.transform.position=back.transform.position+new Vector3(.025f,.025f);
            back.transform.localScale=new Vector3(width,.1f,1);fill.transform.localScale=new Vector3((width-.05f)*Mathf.Clamp01(ratio),.05f,1);
        }
    }
}
