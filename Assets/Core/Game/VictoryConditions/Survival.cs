using UnityEngine;

// long term it would also be good to save the amount of time that has gone by, 
// so that when a Player loads a game the timer will not be reset

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
        foreach (AgentCommander commander in commanders)
        {
            if (commander && commander.human && commander.IsDead())
            {
                return true;
            }
        }
        return timeLeft < 0;
    }

    public override bool CommanderMeetsConditions(AgentCommander commander)
    {
        return commander && commander.human && !commander.IsDead();
    }
}