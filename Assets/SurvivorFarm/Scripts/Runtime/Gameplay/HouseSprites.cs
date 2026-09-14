using System.Collections.Generic;
using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    public static class HouseSprites
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        public static Sprite Slice(string atlas,int x,int y,int w,int h)
        {string id=atlas+":"+x+":"+y+":"+w+":"+h;if(cache.TryGetValue(id,out var found)&&found!=null)return found;var t=Resources.Load<Texture2D>("StoryArt/"+atlas);if(t==null)return null;var s=Sprite.Create(t,new Rect(x,t.height-y-h,w,h),new Vector2(.5f,0),16);cache[id]=s;return s;}
        public static Sprite Furniture(string id)=>id=="Bed"?Slice("HouseBeds",0,0,32,48):id=="Cabinet"?Slice("HouseClosets",64,0,48,48):id=="Furnace"?Slice("HouseFurnace",32,0,32,32):null;
        public static Sprite Facade(int tier)=>tier==3?Slice("HouseMansion",208,0,176,208):tier==2?Slice("HouseStages",176,368,128,112):Slice("HouseStages",tier==0?0:80,368,80,112);
    }
}
