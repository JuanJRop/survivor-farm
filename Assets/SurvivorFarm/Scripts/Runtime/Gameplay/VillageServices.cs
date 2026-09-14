using System;

namespace SurvivorFarm.Runtime.Gameplay
{
    public readonly struct VillageServiceStatus
    {
        public string Id { get; }
        public string Title { get; }
        public bool Unlocked { get; }
        public bool UsedToday { get; }
        public int WoodCost { get; }
        public int StoneCost { get; }
        public int IronReward { get; }
        public string UnavailableReason { get; }
        public bool CanUse => Unlocked && !UsedToday && string.IsNullOrEmpty(UnavailableReason);
        public string Cost => WoodCost + " madera y " + StoneCost + " piedra";
        public string Effect => IronReward + " hierro, una vez al dia";
        public string Summary => Title + ": " + Cost + " por " + Effect + ". " + (CanUse ? "Disponible en el banco de Nico." : UnavailableReason);

        public VillageServiceStatus(bool unlocked, bool usedToday, int ironReward, string reason)
        {
            Id = "village:workshop";
            Title = "Nico - encargo de herrajes";
            Unlocked = unlocked;
            UsedToday = usedToday;
            WoodCost = VillageServices.WorkshopWoodCost;
            StoneCost = VillageServices.WorkshopStoneCost;
            IronReward = ironReward;
            UnavailableReason = reason;
        }
    }

    public static class VillageServices
    {
        public const int WorkshopWoodCost = 4;
        public const int WorkshopStoneCost = 6;

        public static int RepairCount(ValleyData data) =>
            (data.maraPantryStocked ? 1 : 0) + (data.nicoWorkshopRepaired ? 1 : 0) +
            (data.daliaGardenRestored ? 1 : 0) + (data.roloMarketOpened ? 1 : 0) + (data.guardPostBuilt ? 1 : 0);

        public static int RankFor(ValleyData data)
        {
            int repairs = RepairCount(data);
            if (data.influence >= 28 && repairs == 5) return 4;
            if (data.influence >= 16 && data.nicoWorkshopRepaired && data.maraPantryStocked && repairs >= 3) return 3;
            if (data.influence >= 8 && repairs >= 1) return 2;
            return data.influence >= 3 ? 1 : 0;
        }

        public static string NextRankRequirement(ValleyData data)
        {
            switch (RankFor(data))
            {
                case 0: return "Ayudante: influencia " + data.influence + "/3.";
                case 1: return "Vecino de confianza: influencia " + data.influence + "/8 y al menos 1 proyecto vecinal (" + RepairCount(data) + "/5).";
                case 2: return "Protector: influencia " + data.influence + "/16, proyectos " + RepairCount(data) + "/3, despensa de Mara " + State(data.maraPantryStocked) + " y taller de Nico " + State(data.nicoWorkshopRepaired) + ". Herrajes: 3 hierro por encargo.";
                case 3: return "Lider de Raizclara: influencia " + data.influence + "/28 y los 5 proyectos vecinales (" + RepairCount(data) + "/5).";
                default: return "Liderazgo alcanzado: los cinco servicios vecinales recuperados.";
            }
        }

        public static VillageServiceStatus WorkshopStatus(ValleyData data, int day, int wood, int stone)
        {
            bool used = data.nicoSupplyDay >= Math.Max(1, day);
            string reason = !data.nicoWorkshopRepaired ? "Repara el taller: 12 madera y 8 piedra tras la primera noche." :
                used ? "Encargo entregado hoy. Vuelve manana." :
                wood < WorkshopWoodCost || stone < WorkshopStoneCost ? "Faltan materiales: " + wood + "/4 madera, " + stone + "/6 piedra." : "";
            return new VillageServiceStatus(data.nicoWorkshopRepaired, used, RankFor(data) >= 3 ? 3 : 2, reason);
        }

        static string State(bool done) => done ? "[OK]" : "[pendiente]";
    }
}
