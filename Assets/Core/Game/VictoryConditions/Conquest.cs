using RTSLockstep.Player;
using RTSLockstep.Utility;

namespace RTSLockstep.VictoryConditions
{
    public class Conquest : VictoryCondition
    {
        public override string GetDescription()
        {
            return "Conquest";
        }

        public override bool GameFinished()
        {
            if (Players.IsNull())
            {
                return true;
            }
            int playersLeft = Players.Length;
            foreach (LSPlayer player in Players)
            {
                if (!PlayerMeetsConditions(player))
                {
                    playersLeft--;
                }
            }
            return playersLeft == 1;
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            return player && !player.IsDead();
        }
    }
}