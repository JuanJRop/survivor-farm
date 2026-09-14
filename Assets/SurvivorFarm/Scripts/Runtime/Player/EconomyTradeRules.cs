namespace SurvivorFarm.Runtime.Player
{
    public static class EconomyTradeRules
    {
        public static bool TryGetTotalPrice(int unitPrice, int amount, out int total)
        {
            total = 0;
            if (unitPrice <= 0 || amount <= 0) return false;
            long value = (long)unitPrice * amount;
            if (value > int.MaxValue) return false;
            total = (int)value;
            return true;
        }
    }
}
