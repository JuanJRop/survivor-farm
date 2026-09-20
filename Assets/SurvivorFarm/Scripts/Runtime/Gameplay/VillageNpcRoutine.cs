using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class VillageNpcRoutine : MonoBehaviour
    {
        ValleyCampaign campaign;
        VillageNpcArtCatalog catalog;
        SpriteRenderer visual;
        ValleyInteraction interaction;
        DayNightCycle clock;
        Vector3 origin;
        string id;
        VillageResidentHealth resident;
        Vector2 destination;
        float nextDecision;
        float nextThreatScan, frightenedUntil;
        Vector2 lastThreatPosition, escapeDirection, steering;
        float nextSteering;
        readonly Collider2D[] hits=new Collider2D[48];
        public bool IsFleeing => resident != null && resident.IsAlive && Time.time < frightenedUntil;

        public void Configure(ValleyCampaign owner, string residentId, SpriteRenderer renderer, VillageNpcArtCatalog art)
        {
            campaign = owner; id = residentId; visual = renderer; catalog = art;
            interaction = GetComponent<ValleyInteraction>();
            clock = FindFirstObjectByType<DayNightCycle>();
            origin = visual.transform.localPosition;
            if(PortfolioSession.Active)
            {
                resident=GetComponent<VillageResidentHealth>();
                if(resident==null)resident=gameObject.AddComponent<VillageResidentHealth>();
                resident.Configure(id);
                destination=transform.position;
                nextThreatScan=Time.time+(Mathf.Abs(GetInstanceID())%12)*.01f;
            }
            Tick(0);
        }

        void Update() => Tick(Time.deltaTime);

        void Tick(float delta)
        {
            if (campaign == null || visual == null) return;
            if(resident!=null){TickResident(delta);return;}
            float hour = clock != null ? clock.Hour : 8f;
            bool restored = VillageResidents.ProjectComplete(campaign.Data, id);
            bool talking = Vector2.Distance(campaign.transform.position, transform.position) < 1.7f;
            Vector3 previous = visual.transform.localPosition;
            Vector3 target = origin + VillageResidents.Offset(id, hour, restored);
            if (!talking) visual.transform.localPosition = Vector3.MoveTowards(previous, target, delta * .28f);
            Vector3 movement = visual.transform.localPosition - previous;
            bool walking = movement.sqrMagnitude > .0000001f;
            bool working = !talking && !walking && restored && VillageResidents.Period(hour) == 1;
            Vector3 facing = talking ? campaign.transform.position - visual.transform.position : walking ? movement : working && id == "village:blacksmith" ? Vector3.up : Vector3.down;
            int direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? 2 : facing.y > 0 ? 1 : 0;
            if (catalog != null)
            {
                var sprite = catalog.Frame(id, walking, working, direction, (int)(Time.time * (walking || working ? 6f : 3f)));
                if (sprite != null) { visual.sprite = sprite; visual.color = Color.white; }
                visual.flipX = direction == 2 && facing.x < 0;
            }
            if (interaction != null) interaction.Label = VillageResidents.Name(id) + " - " + VillageResidents.Activity(campaign.Data, id, hour);
        }
        void TickResident(float delta)
        {
            if(!resident.IsAlive)return;
            if(Time.time>=nextThreatScan)
            {
                nextThreatScan=Time.time+.12f;
                if(TryFindThreat(out var threat))
                {
                    lastThreatPosition=threat;
                    frightenedUntil=Time.time+2.5f;
                    nextDecision=0;
                    nextSteering=0;
                }
            }
            bool fleeing=IsFleeing;
            if(Time.time>=nextDecision)
            {
                nextDecision=Time.time+(fleeing?.35f:Random.Range(3f,6f));
                destination=fleeing?(Vector2)transform.position+((Vector2)transform.position-lastThreatPosition).normalized*3f:
                    resident.Home+Random.insideUnitCircle*2.6f;
            }
            Vector2 previous=transform.position;
            Vector2 desired=fleeing?previous-lastThreatPosition:destination-previous;
            if(desired.sqrMagnitude<.0001f&&fleeing)desired=Vector2.down;
            // Perception and route selection have a budget; physical movement still checks every frame.
            if(Time.time>=nextSteering)
            {
                nextSteering=Time.time+.1f;
                steering=Steer(desired.normalized,fleeing);
            }
            Vector2 moveDirection=steering;
            float travel=Mathf.Min(delta*(fleeing?2.25f:.55f),fleeing?float.PositiveInfinity:desired.magnitude);
            Vector2 next=previous+moveDirection*travel;
            if(travel>.00001f)
            {
                if(CanStand(next))transform.position=next;
                else {nextDecision=Mathf.Min(nextDecision,Time.time+.3f);nextSteering=0;}
            }
            Vector2 movement=(Vector2)transform.position-previous;
            bool walking=movement.sqrMagnitude>.0000001f;
            if(walking)escapeDirection=movement.normalized;
            int direction=Mathf.Abs(movement.x)>Mathf.Abs(movement.y)?2:movement.y>0?1:0;
            if(catalog!=null)
            {
                visual.sprite=catalog.Frame(id,walking,false,direction,(int)(Time.time*(walking?(fleeing?10:6):3)))??visual.sprite;
                visual.flipX=direction==2&&movement.x<0;visual.color=Color.white;
            }
        }

        bool TryFindThreat(out Vector2 position)
        {
            position=Vector2.zero;
            int count=Physics2D.OverlapCircle(transform.position,5.5f,new ContactFilter2D{useTriggers=true},hits);
            float closest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var enemy=hits[i].GetComponentInParent<EnemyAIBase>();
                if(enemy==null||!enemy.IsAlive)continue;
                float distance=((Vector2)enemy.transform.position-(Vector2)transform.position).sqrMagnitude;
                if(distance>=closest)continue;
                closest=distance;position=enemy.transform.position;
            }
            return !float.IsPositiveInfinity(closest);
        }

        Vector2 Steer(Vector2 desired,bool fleeing)
        {
            if(desired.sqrMagnitude<.001f)return Vector2.zero;
            Vector2 best=Vector2.zero;
            float bestScore=float.NegativeInfinity;
            // Try escape lanes around houses, fences and other residents before stopping.
            for(int i=0;i<9;i++)
            {
                float angle=i==0?0:((i+1)/2)*35f*(i%2==0?-1:1);
                Vector2 candidate=Quaternion.Euler(0,0,angle)*desired;
                if(!CanStand((Vector2)transform.position+candidate*.38f))continue;
                float score=Vector2.Dot(candidate,desired)*2+Vector2.Dot(candidate,escapeDirection)*.35f;
                if(CanStand((Vector2)transform.position+candidate*.85f))score+=1;
                if(fleeing)score+=Vector2.Distance((Vector2)transform.position+candidate,lastThreatPosition)*.15f;
                if(score>bestScore){bestScore=score;best=candidate;}
            }
            return best;
        }

        bool CanStand(Vector2 point)
        {
            if(!World.FarmExploration.Contains(point,1)||World.FarmExploration.IsRiver(point,.25f))return false;
            int count=Physics2D.OverlapCircle(point+Vector2.up*.26f,.27f,new ContactFilter2D{useTriggers=false},hits);
            if(count==hits.Length)return false;
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform))return false;
            return true;
        }
    }
}
