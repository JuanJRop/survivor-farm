using System;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
namespace SurvivorFarm.Runtime.Player
{
    [Serializable] public sealed class AdventureData
    {
        public int iron; public bool enteredRuins, defeatedGolem, returnedHome, beacon, temperedBlade;
        public int[] minedDays=new int[4];
    }
    public sealed class AdventureProgress : MonoBehaviour
    {
        public AdventureData Data=new AdventureData();
        public string Objective
        {
            get
            {
                var inventory=GetComponent<PlayerInventory>();var tools=GetComponent<PlayerToolUpgradeController>();
                if(!Data.enteredRuins)return "Expedición: repara el puente del norte con 20 madera y 10 piedra; prepara comida y espada para explorar las ruinas.";
                if(!Data.defeatedGolem)return "Ruinas: derrota a un gólem y consigue su gema. Apártate cuando prepare el golpe naranja.";
                if(!Data.returnedHome)return "Vuelve a la protección de tu casa con la gema del gólem.";
                if(tools!=null&&tools.PickaxeLevel<2)return "Mejora el pico a nivel 2 en F para extraer hierro en la cantera del este.";
                if(Data.iron<10&&!Data.beacon)return "Reúne 10 de hierro en las vetas ricas del este. También pueden soltar oro, rubí, esmeralda y diamante.";
                if(!Data.beacon)return "Construye la baliza [G] con 10 hierro, 12 madera y 8 piedra.";
                return "¡Baliza encendida! Expedición completada. Canjea hierro, oro y gemas en el taller para crear armas y armaduras mejores.";
            }
        }
        public void AddIron(int amount){if(amount<=0)return;Data.iron=(int)Math.Min(int.MaxValue,(long)Data.iron+amount);GetComponent<PlayerInventory>().NotifyInventoryChanged();}
        public bool SpendIron(int amount){if(amount<0||Data.iron<amount)return false;Data.iron-=amount;GetComponent<PlayerInventory>().NotifyInventoryChanged();return true;}
        public void Restore(AdventureData data){Data=data??new AdventureData();if(Data.minedDays==null||Data.minedDays.Length!=4)Data.minedDays=new int[4];Data.iron=Mathf.Max(0,Data.iron);}
        public void EnterRuins(){Data.enteredRuins=true;FarmNotificationCenter.Show("Ruinas de Raizclara: explora las galerias y encuentra la camara del Custodio.");}
        public void DefeatGolem(){Data.defeatedGolem=true;FarmNotificationCenter.Show("Gema recuperada. Regresa a la casa para completar la expedición.");}
        void Update()
        {
            if(!Data.defeatedGolem||Data.returnedHome)return;
            var home=FindFirstObjectByType<HomeSafeZone>();
            if(home!=null&&home.Contains(transform.position)&&GetComponent<PlayerInventory>().OwnsEquipment("Gem")&&GetComponent<PlayerSurvivalStats>().CurrentHealth>0)
            {Data.returnedHome=true;GetComponent<PlayerInventory>().AddCoins(25);FarmNotificationCenter.Show("Expedición completada: +25 oro. Ya puedes construir la baliza.");}
        }
    }
}
