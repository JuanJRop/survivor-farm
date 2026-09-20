using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Refunds only paid construction material. Round down each indivisible resource.</summary>
    public readonly struct DemolitionRefund
    {
        public readonly int Wood, Stone, Iron, Gold;
        public DemolitionRefund(BuildingData data)
        {
            ConstructionSystem.Cost(data.kind,out var wood,out var stone,out var iron);
            bool paid=data.costRecorded;
            Wood=Part(data.authoredFree?0:paid?data.paidWood:wood);
            Stone=Part(data.authoredFree?0:paid?data.paidStone:stone);
            Iron=Part(data.authoredFree?0:paid?data.paidIron:iron);
            Gold=Part(data.authoredFree?0:paid?data.paidGold:FortressPieces.GoldCost(data.kind));
        }
        public static int Part(int value)=>(int)(System.Math.Max(0L,value)*69/100);
        public static void RecordPayment(BuildingData data,string kind)
        {
            if(!data.costRecorded&&!data.authoredFree)
            {
                ConstructionSystem.Cost(data.kind,out data.paidWood,out data.paidStone,out data.paidIron);
                data.paidGold=FortressPieces.GoldCost(data.kind);
            }
            ConstructionSystem.Cost(kind,out int wood,out int stone,out int iron);
            data.paidWood+=wood;data.paidStone+=stone;data.paidIron+=iron;data.paidGold+=FortressPieces.GoldCost(kind);
            data.costRecorded=true;data.authoredFree=false;
        }
    }
}
