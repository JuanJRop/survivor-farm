using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class PetAdoptionWindow : MonoBehaviour
    {
        public static bool IsOpen {get;private set;}
        private GameObject root;private PetAdoption adoption;private Text status;
        public void Open(PlayerInventory player)
        {
            if(root!=null)Close();adoption=player.GetComponent<PetAdoption>();if(adoption==null)return;
            IsOpen=true;root=MasteryWindow.CreateCanvas("Compañero del valle",115);
            var panel=AdventureWindow.Rect(root.transform,"Compra del gato",0,0,586,344);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.anchoredPosition=Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            MasteryWindow.Label(panel,"UN COMPAÑERO PARA TU GRANJA",24,20,540,42,23);
            MasteryWindow.Label(panel,"El gato te acompaña y ayuda en combate.\nUna compra avanzada, no parte del equipo inicial.",24,81,538,58,17);
            MasteryWindow.Label(panel,"Día 2 · 180 monedas · 12 hierro · 3 oro mineral",24,158,538,45,17).color=FarmUiStyle.Accent;
            status=MasteryWindow.Label(panel,adoption.Requirement,24,216,538,30,15);
            MasteryWindow.Button(panel,adoption.Owned?"Adquirido":"Comprar compañero",24,276,370,44,()=>{if(adoption.Buy())Close();else status.text=adoption.Requirement;});
            MasteryWindow.Button(panel,"Cerrar",414,276,148,44,Close);player.GetComponent<PlayerMovementController>()?.StopMovement();
        }
        public void Close(){IsOpen=false;if(root!=null)Destroy(root);root=null;}
        private void Update(){if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))Close();}
        private void OnDestroy()=>Close();
    }
    public sealed class PetMerchant : WorldInteractable
    {
        public override string GetInteractionLabel(FarmTool tool)=>"Rolo · comprar compañero";
        public override void Interact(FarmTool tool,PlayerInventory player)=>(player.GetComponent<PetAdoptionWindow>()??player.gameObject.AddComponent<PetAdoptionWindow>()).Open(player);
        protected override float HighlightScale=>1;
    }
}
