using UnityEngine;
using System.Collections;

public class Conquest : VictoryCondition
{

    public override string GetDescription()
    {
        return "Conquest";
    }

    public override bool GameFinished()
    {
        if (commanders == null)
        {
            return true;
        }
        int playersLeft = commanders.Length;
        foreach (AgentCommander commander in commanders)
        {
            if (!CommanderMeetsConditions(commander))
            {
                playersLeft--;
            }
        }
        return playersLeft == 1;
    }

    public override bool CommanderMeetsConditions(AgentCommander commander)
    {
        return commander && !commander.IsDead();
    }

}