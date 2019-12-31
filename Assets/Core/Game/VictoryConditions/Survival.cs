using RTSLockstep.Player;
using UnityEngine;

// long term it would also be good to save the amount of time that has gone by, 
// so that when a Player loads a game the timer will not be reset
namespace RTSLockstep.VictoryConditions
{
    public class Survival : VictoryCondition
    {

        public int minutes = 1;

        private float timeLeft = 0.0f;

        void Awake()
        {
            timeLeft = minutes * 60;
        }

        void Update()
        {
            timeLeft -= Time.deltaTime;
        }

        public override string GetDescription()
        {
            return "Survival";
        }

        public override bool GameFinished()
        {
            foreach (LSPlayer players in Players)
            {
                if (players && players.IsHuman && players.IsDead())
                {
                    return true;
                }
            }
            return timeLeft < 0;
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            return player && player.IsHuman && !player.IsDead();
        }
    }
}