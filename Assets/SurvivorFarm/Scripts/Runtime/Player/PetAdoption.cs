using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Late-demo companion purchase, kept separate from following/attacking and equipment.</summary>
    public sealed class PetAdoption : MonoBehaviour
    {
        public const int CoinCost=180, IronCost=12, GoldCost=3;
        public bool Owned {get;private set;}
        public string Requirement
        {
            get
            {
                if(Owned)return "Compañero adquirido";
                if(PortfolioSession.Instance?.Day<2)return "Disponible desde el día 2";
                var player=GetComponent<PlayerInventory>();
                return player.Coins<CoinCost||GetComponent<AdventureProgress>().Data.iron<IronCost||player.GetAvailableItemCount("GoldOre")<GoldCost?
                    "180 monedas · 12 hierro · 3 oro mineral":"Puedes comprar el compañero";
            }
        }
        public bool Buy()
        {
            var player=GetComponent<PlayerInventory>();var progress=GetComponent<AdventureProgress>();
            if(Owned||PortfolioSession.Instance==null||PortfolioSession.Instance.Day<2||player.Coins<CoinCost||progress.Data.iron<IronCost||player.GetAvailableItemCount("GoldOre")<GoldCost)return false;
            player.TrySpendCoins(CoinCost);progress.SpendIron(IronCost);player.TryRemoveItem("GoldOre",GoldCost);
            Owned=true;GetComponent<PlayerPetController>()?.SetEquipped(true);AudioFeedback.PlayAt(CombatSound.Craft,transform.position);
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
        }
        public void Restore(bool owned){Owned=owned;if(!owned)GetComponent<PlayerPetController>()?.SetEquipped(false);else GetComponent<PlayerPetController>()?.SetEquipped(true);}
    }
}
