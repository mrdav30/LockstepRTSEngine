using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Player;

namespace RTSLockstep.VictoryConditions
{
    public class BuildWonder : VictoryCondition
    {
        public override string GetDescription()
        {
            return "Building Wonder";
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            // Wonder wonder = commander.GetComponentInChildren<Wonder>();

            LSAgent[] agents = player.GetComponentInChildren<LSAgentsOrganizer>().GetComponentsInChildren<LSAgent>();
            foreach (LSAgent agent in agents)
            {
                if (agent.GetAbility<Wonder>())
                {
                    return player && !player.IsDead() && agent && !agent.GetAbility<Structure>().NeedsConstruction;
                }
            }
            return false;
        }
    }
}