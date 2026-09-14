using System;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class VillageQuests
    {
        public sealed class Cost
        {
            public readonly string ItemId;
            public readonly int Amount;
            public Cost(string itemId, int amount) { ItemId = itemId; Amount = amount; }
        }

        public sealed class Definition
        {
            public readonly string ResidentId, Title, Request, Reward;
            public readonly int Bit;
            public readonly Cost[] Costs;
            public Definition(string id, int bit, string title, string request, string reward, params Cost[] costs)
            { ResidentId = id; Bit = bit; Title = title; Request = request; Reward = reward; Costs = costs; }
        }

        public static readonly Definition[] All =
        {
            new Definition("village:elder", 1, "Una mesa para todos", "La despensa esta vacia. Con dos raciones podre preparar la primera comida comunal. ¿Me ayudas?", "+15 oro, +5 madera, +3 influencia", new Cost("Food", 2)),
            new Definition("village:blacksmith", 2, "Volver a encender la forja", "Las vigas del taller no aguantan otra noche. Si traes madera y piedra, podre volver a trabajar. ¿Te encargas?", "+2 hierro, escudo de madera, +4 influencia", new Cost("Wood", 12), new Cost("Stone", 8)),
            new Definition("village:farmer", 4, "Una cocina encendida", "Necesito dos frutas y cuatro piezas de madera para recuperar la cocina. Quiero preparar provisiones para quienes exploran el valle. ¿Cuento contigo?", "+2 zanahorias, +1 tomate, +2 raciones, +4 influencia", new Cost("Fruit", 2), new Cost("Wood", 4)),
            new Definition("village:merchant", 8, "Un puesto abierto", "Con el taller y la cocina funcionando, el mercado tiene futuro. Me falta madera y algo de oro para montarlo. ¿Me echas una mano?", "+3 raciones, +1 pepita de oro, anillo de viajero, +4 influencia", new Cost("Coins", 20), new Cost("Wood", 8)),
            new Definition("village:guard", 16, "Una guardia para Raizclara", "Necesito hierro para reforzar el puesto y comida para los turnos de guardia. Asi podremos proteger el camino. ¿Lo haras?", "Escudo de hierro, +1 esencia de tierra, +5 influencia", new Cost("Iron", 6), new Cost("Food", 2))
        };

        public static Definition Find(string id) => Array.Find(All, quest => quest.ResidentId == id);
        public static bool IsVillager(string id) => Find(id) != null;
        public static bool IsAccepted(ValleyData data, string id) => Find(id) is Definition quest && (data.acceptedVillageQuests & quest.Bit) != 0;
        public static bool IsComplete(ValleyData data, string id) => VillageResidents.ProjectComplete(data, id);
        public static string Requirement(ValleyData data, string id) => id switch
        {
            "village:elder" => !data.note ? "Lee primero la nota junto al refugio." : !data.camp ? "Completa tu primer dia en el pueblo." : "",
            "village:blacksmith" => !data.camp ? "Completa tu primer dia en el pueblo." : "",
            "village:farmer" => !data.note ? "Lee primero la nota junto al refugio." : "",
            "village:merchant" => !data.nicoWorkshopRepaired || !data.daliaGardenRestored ? "Recupera el taller de Nico y la cocina de Dalia." : "",
            "village:guard" => !data.sealStone && !data.guardian ? "Recupera un sello o libera el bosque." : "",
            _ => "Encargo desconocido."
        };
        public static int Owned(PlayerInventory inventory, string itemId) => itemId == "Coins" ? inventory?.Coins ?? 0 : BackpackActions.Count(inventory, itemId);
        public static bool HasMaterials(PlayerInventory inventory, Definition quest)
        {
            if (inventory == null || quest == null) return false;
            foreach (var cost in quest.Costs) if (Owned(inventory, cost.ItemId) < cost.Amount) return false;
            return true;
        }
        public static string Status(ValleyData data, string id) => IsComplete(data, id) ? "Completada" :
            IsAccepted(data, id) ? "En curso" : Requirement(data, id).Length == 0 ? "Por aceptar" : "No disponible aun";
        public static string SmallTalk(ValleyData data, string id) => id switch
        {
            "village:elder" => data.restored ? "Hacia tiempo que no se oian tantas voces en la plaza. Ya no esperamos que alguien nos salve: nos tenemos unos a otros." : "Antes la plaza olia a pan por las mananas. Quiero recuperar eso: un lugar al que apetezca volver, aunque no tengas nada que pedir.",
            "village:blacksmith" => data.nicoWorkshopRepaired ? "El primer martillazo despues de reparar el taller me hizo temblar las manos. Pensaba que habia olvidado el oficio. No se olvida." : "Guardo las herramientas envueltas para que no se oxiden. Mientras sigan aqui, esto es un taller y no una ruina.",
            "village:farmer" => data.daliaGardenRestored ? "He guardado provisiones para la proxima expedicion. Siempre conviene tener algo caliente al volver." : "Aun quedan ollas entre las tablas rotas. Con lena e ingredientes podemos volver a encender esta cocina.",
            "village:merchant" => data.roloMarketOpened ? "Hoy alguien me pregunto si abriria manana. Hacia mucho que nadie daba por hecho que habria un manana aqui." : "Podria irme a vender al otro lado del valle. Pero alli no sabria a quien le estoy fiando el desayuno.",
            "village:guard" => data.guardian ? "Desde que el bosque se calmo, las noches suenan distintas. Sigo de guardia, pero ya puedo escuchar algo mas que amenazas." : "Vigilo la entrada, aunque lo importante queda a mi espalda. Cada luz del pueblo es una razon para seguir aqui.",
            _ => "Nos vemos en la plaza."
        };
    }
}
