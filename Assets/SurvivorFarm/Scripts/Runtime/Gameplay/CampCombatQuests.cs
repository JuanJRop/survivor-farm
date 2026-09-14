using System;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class CampCombatQuests
    {
        public sealed class Definition
        {
            public readonly string CampId, Title, Location, Request;
            public readonly int Bit, Coins, Influence, Rubies;
            public EnemyCampDefinition Camp => Array.Find(EnemyCampDefinition.All, c => c.Id == CampId);
            public string Reward => Coins + " oro, " + Influence + " influencia" + (Rubies > 0 ? ", 1 rubi" : "");
            public Definition(string id, int bit, string title, string location, string request, int coins, int influence, int rubies = 0)
            { CampId = id; Bit = bit; Title = title; Location = location; Request = request; Coins = coins; Influence = influence; Rubies = rubies; }
        }

        public static readonly Definition[] All = {
            new Definition("south", 1, "Una salida segura", "Sur del pueblo, al oeste del camino principal",
                "Los limos y su lancero acechan la salida sur. Elimina a todos los guardias del Escondite del sur y vuelve conmigo. Necesitamos ese camino libre.", 20, 2),
            new Definition("west", 2, "Romper el cerco", "Oeste del pueblo, hacia el borde del bosque",
                "Los Saqueadores del oeste interceptan nuestros suministros. Acaba con sus lanceros, su arquero y su limo; despues informame de la victoria.", 30, 2),
            new Definition("east", 4, "Silenciar las flechas", "Este del pueblo, al sur del rio",
                "Los Vigias del este controlan el paso con sus arcos. Esquiva sus disparos, elimina a todo el grupo y regresa para recibir tu recompensa.", 35, 3),
            new Definition("north", 8, "El ultimo bastion", "Noreste, al otro lado del rio; puente reparado",
                "El Bastion del norte reune al grupo mas numeroso. Repara el puente para cruzar el rio, derrota a sus cinco guardias y vuelve. El pueblo recordara tu ayuda.", 50, 4, 1)
        };

        public static Definition Find(string id) => Array.Find(All, q => q.CampId == id);
        public static bool IsAccepted(ValleyData data, string id) => data != null && Find(id) is Definition q && (data.acceptedCampQuests & q.Bit) != 0;
        public static bool IsClaimed(ValleyData data, string id) => data != null && Find(id) is Definition q && (data.claimedCampQuests & q.Bit) != 0;
        public static int DefeatedCount(ValleyData data, string id)
        {
            var quest = Find(id);
            var state = data?.enemyCamps?.Find(s => s != null && s.id == id);
            if (quest == null || state == null) return 0;
            if (state.claimed) return quest.Camp.Roster.Length;
            int count = 0;
            for (int i = 0; i < quest.Camp.Roster.Length; i++) if ((state.defeatedMask & (1 << i)) != 0) count++;
            return count;
        }
        public static bool IsCleared(ValleyData data, string id) => Find(id) is Definition q && DefeatedCount(data, id) == q.Camp.Roster.Length;
        public static bool CanClaim(ValleyData data, string id) => IsAccepted(data, id) && !IsClaimed(data, id) && IsCleared(data, id);
        public static string Status(ValleyData data, string id) => IsClaimed(data, id) ? "Completada" : !IsAccepted(data, id) ? "Por aceptar con Iria" :
            IsCleared(data, id) ? "Victoria confirmada - vuelve con Iria" : "En curso";
        public static string Progress(ValleyData data, string id) => Find(id) is Definition q ? DefeatedCount(data, id) + "/" + q.Camp.Roster.Length + " enemigos" : "";
        public static Definition Tracked(ValleyData data) => data != null && IsAccepted(data, data.trackedCampQuest) && !IsClaimed(data, data.trackedCampQuest) ? Find(data.trackedCampQuest) : null;
        public static void Normalize(ValleyData data)
        {
            data.acceptedCampQuests &= 15;
            data.claimedCampQuests &= 15;
            data.acceptedCampQuests |= data.claimedCampQuests;
            if (Tracked(data) == null) data.trackedCampQuest = null;
        }
    }
}
