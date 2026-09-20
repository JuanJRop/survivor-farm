using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Shared dimensions and costs: preview, collision and payment use the same module.</summary>
    public static class FortressPieces
    {
        public const float Length = 2f;
        public const float VerticalLength = 4f;
        public static readonly string[] Palette = { "Fence", "StoneWall", "ReinforcedWall", "Trap", "Turret" };
        public static bool IsWall(string kind) => kind == "Fence" || kind == "StoneWall" || kind == "ReinforcedWall";
        public static int GoldCost(string kind) => kind == "ReinforcedWall" ? 1 : 0;
        public static int Health(string kind) => kind == "ReinforcedWall" ? 64 : kind == "StoneWall" ? 36 : 20;
        public static int Tier(string kind)=>kind=="ReinforcedWall"?3:kind=="StoneWall"?2:kind=="Fence"?1:0;
        public static Vector2 Snap(string kind, Vector2 point, int rotation)
        {
            if (!IsWall(kind)) return new Vector2(Mathf.Round(point.x * 2) * .5f, Mathf.Round(point.y * 2) * .5f);
            // Centres lie on grid edges; endpoints share even coordinates at every corner.
            var offset = rotation % 2 == 0 ? Vector2.right : Vector2.up * 2;
            return new Vector2(Mathf.Round((point.x-offset.x)/Length)*Length,
                Mathf.Round((point.y-offset.y)/VerticalLength)*VerticalLength) + offset;
        }
        public static Vector2 Footprint(string kind, int rotation, bool modular = true)
        {
            if (!IsWall(kind)) return new Vector2(.7f, .55f);
            var size = modular ? new Vector2(1.94f, .4f) : new Vector2(.95f, .25f);
            return rotation % 2 == 0 ? size : new Vector2(size.y, modular ? VerticalLength-.06f : size.x);
        }
        public static bool Overlaps(string a, Vector2 p, int ar, bool am, string b, Vector2 q, int br, bool bm)
        {
            var half = (Footprint(a,ar,am)+Footprint(b,br,bm))*.5f;
            // Joining endpoints are deliberately shared by perpendicular modules.
            if (am && bm && IsWall(a) && IsWall(b) && ar%2 != br%2 &&
                Mathf.Abs(Mathf.Abs(p.x-q.x)-1)<.01f && Mathf.Abs(Mathf.Abs(p.y-q.y)-2)<.01f) return false;
            return Mathf.Abs(p.x-q.x)<half.x && Mathf.Abs(p.y-q.y)<half.y;
        }
        public static Sprite Art(string kind, int rotation = 0)
        {
            string atlas = kind == "Fence" ? "FortressWood" : "FortressStone";
            if(kind=="Fence")return rotation%2==0?HouseSprites.Slice(atlas,48,0,48,16):HouseSprites.Slice(atlas,5,0,6,48);
            var sprite=rotation % 2 == 0 ? HouseSprites.Slice(atlas,kind=="Fence"?64:16,0,16,16) : HouseSprites.Slice(atlas,0,16,16,16);
            if(sprite!=null)sprite.texture.filterMode=FilterMode.Point;
            return sprite;
        }
        public static void Style(SpriteRenderer renderer, string kind, int rotation)
        {
            renderer.sprite = Art(kind, rotation);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localPosition = VisualOffset(kind,rotation);
            renderer.transform.localScale = kind=="Fence"?(rotation%2==0?new Vector3(2f/3,.95f,1):Vector3.one*(4f/3)):
                new Vector3(2,rotation%2==0?2:4,1);
            renderer.color = kind == "ReinforcedWall" ? new Color(1,.86f,.58f) : Color.white;
        }
        // The opaque lower edge sits on the collision line, not above the player's feet.
        public static Vector3 VisualOffset(string kind,int rotation)=>rotation%2==0?new Vector3(0,kind=="Fence"?-.05f:-.575f):new Vector3(kind=="Fence"?0:.6875f,-2);
    }
}
