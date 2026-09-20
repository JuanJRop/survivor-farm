using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class VillageAdventurePortal : WorldInteractable
    {
        private VillageAdventure adventure;
        private int index;
        private bool isExit;
        private SpriteRenderer visual;
        private Sprite openArt,closedArt;
        public void Configure(VillageAdventure owner, int dungeonIndex, bool exit)
        {
            adventure = owner; index = dungeonIndex; isExit = exit;
            visual=GetComponentInChildren<SpriteRenderer>();
            var atlas=Resources.Load<SurvivorFarm.Runtime.World.DungeonArtCatalog>("DungeonArt")?.Door;
            if(visual==null||visual.sprite==null||atlas==null)return;
            var pivot=new Vector2(visual.sprite.pivot.x/visual.sprite.rect.width,visual.sprite.pivot.y/visual.sprite.rect.height);
            closedArt=Sprite.Create(atlas,new Rect(0,atlas.height-32,32,32),pivot,16);
            openArt=Sprite.Create(atlas,new Rect(96,atlas.height-32,32,32),pivot,16);
            RefreshArt();
        }
        private void Update()=>RefreshArt();
        private void RefreshArt()
        {
            if(visual==null||openArt==null)return;
            bool open=isExit||adventure!=null&&adventure.CanEnterDungeon;
            visual.sprite=open?openArt:closedArt;
            visual.color=open?Color.white:new Color(.6f,.6f,.72f);
        }
        public override bool IsAvailable => adventure != null;
        public override string GetInteractionLabel(FarmTool selectedTool) => isExit ? "Regresar al valle" :
            adventure != null && adventure.CanEnterDungeon ? "Entrar: " + VillageAdventure.DungeonDefinitions[index].Title : "Entrada cerrada: defiende el pueblo al anochecer";
        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (adventure == null || inventory == null || Vector2.Distance(inventory.transform.position, transform.position) > 2.5f) return;
            if (isExit) adventure.ExitDungeon();
            else if (!adventure.EnterDungeon(index)) FarmNotificationCenter.Show("Explora las mazmorras durante el día. Al anochecer, Raízclara necesita tu ayuda.");
        }
        private void OnDestroy(){if(openArt!=null)Destroy(openArt);if(closedArt!=null)Destroy(closedArt);}
    }
}
