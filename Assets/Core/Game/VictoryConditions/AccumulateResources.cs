using RTSLockstep.Player;
using RTSLockstep.LSResources;

namespace RTSLockstep.VictoryConditions
{
    public class AccumulateResources : VictoryCondition
    {
        public int amount = 1050;

        private RawMaterialType type = RawMaterialType.Gold;

        public override string GetDescription()
        {
            return "Accumulating Gold";
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            return player && !player.IsDead() && player.PlayerResourceManager.GetRawMaterialAmount(type) >= amount;
        }
    }
}