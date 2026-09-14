using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class FlyweightCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];
        [SerializeField] private ResourceDefinition[] resources = new ResourceDefinition[0];
        [SerializeField] private ResourceAnimation[] animations = new ResourceAnimation[0];
        [SerializeField] private CultivationDefinition[] cultivations = new CultivationDefinition[0];

        internal void Register()
        {
            foreach (ItemDefinition item in items) ResourceFlyweights.Register(item);
            foreach (ResourceAnimation clip in animations) ResourceFlyweights.Register(clip);
            foreach (ResourceDefinition resource in resources) ResourceFlyweights.Register(resource);
            foreach (CultivationDefinition cultivation in cultivations) ResourceFlyweights.Register(cultivation);
        }
    }
}
