using RTSLockstep.Player;
using RTSLockstep.LSResources;

namespace RTSLockstep.VictoryConditions
{
    public class AccumulateResources : VictoryCondition
    {
        public int amount = 1050;

        private ResourceType type = ResourceType.Gold;

        public override string GetDescription()
        {
            return "Accumulating Gold";
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            return player && !player.IsDead() && player.CachedResourceManager.GetResourceAmount(type) >= amount;
        }
    }
}