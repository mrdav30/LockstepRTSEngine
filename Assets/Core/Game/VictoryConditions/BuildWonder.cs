using RTSLockstep;

public class BuildWonder : VictoryCondition
{
    public override string GetDescription()
    {
        return "Building Wonder";
    }

    public override bool CommanderMeetsConditions(AgentCommander commander)
    {
        Wonder wonder = commander.GetComponentInChildren<Wonder>();

        RTSAgent[] agents = commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>();
        foreach (RTSAgent agent in agents)
        {
            if (agent.GetAbility<Wonder>())
            {
                return commander && !commander.IsDead() && agent && !agent.GetAbility<Structure>().UnderConstruction();
            }
        }
        return false;
    }
}