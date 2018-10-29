using UnityEngine;
using System.Collections;
using RTSLockstep;

public class AccumulateResources : VictoryCondition
{

    public int amount = 1050;

    private ResourceType type = ResourceType.Gold;

    public override string GetDescription()
    {
        return "Accumulating Gold";
    }

    public override bool CommanderMeetsConditions(AgentCommander commander)
    {
        return commander && !commander.IsDead() && commander.GetResourceAmount(type) >= amount;
    }
} 