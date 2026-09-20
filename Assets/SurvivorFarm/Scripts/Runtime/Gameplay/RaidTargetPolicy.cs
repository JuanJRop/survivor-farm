using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Target priority only; navigation decides whether a wall must be breached.</summary>
    public static class RaidTargetPolicy
    {
        public const float CivilianRadius=7f;
        public static bool TargetsPlayer(int assignment)=>(assignment&1)==1;
        public static VillageResidentHealth NearestCivilian(Vector2 position,IEnumerable<VillageResidentHealth> people)
        {
            VillageResidentHealth best=null;float nearest=CivilianRadius*CivilianRadius;
            foreach(var person in people)
            {
                if(person==null||!person.IsAlive)continue;
                float d=((Vector2)person.transform.position-position).sqrMagnitude;
                if(d<nearest){nearest=d;best=person;}
            }
            return best;
        }
    }
}
