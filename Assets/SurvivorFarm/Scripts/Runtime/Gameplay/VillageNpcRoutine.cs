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

        public void Configure(ValleyCampaign owner, string residentId, SpriteRenderer renderer, VillageNpcArtCatalog art)
        {
            campaign = owner; id = residentId; visual = renderer; catalog = art;
            interaction = GetComponent<ValleyInteraction>();
            clock = FindFirstObjectByType<DayNightCycle>();
            origin = visual.transform.localPosition;
            Tick(0);
        }

        void Update() => Tick(Time.deltaTime);

        void Tick(float delta)
        {
            if (campaign == null || visual == null) return;
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
    }
}
