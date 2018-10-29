using UnityEngine;
using System.Collections;

public abstract class VictoryCondition : MonoBehaviour
{

    protected AgentCommander[] commanders;

    public void SetCommanders(AgentCommander[] commanders)
    {
        this.commanders = commanders;
    }

    public AgentCommander[] GetCommanders()
    {
        return commanders;
    }

    public virtual bool GameFinished()
    {
        if (commanders == null)
        {
            return true;
        }
        foreach (AgentCommander commander in commanders)
        {
            if (CommanderMeetsConditions(commander))
            {
                return true;
            }
        }
        return false;
    }

    public AgentCommander GetWinner()
    {
        if (commanders == null)
        {
            return null;
        }
        foreach (AgentCommander commander in commanders)
        {
            if (CommanderMeetsConditions(commander))
            {
                return commander;
            }
        }
        return null;
    }

    public abstract string GetDescription();

    // Any child class that extends must provide an implementation for this method. 
    public abstract bool CommanderMeetsConditions(AgentCommander commander);
}