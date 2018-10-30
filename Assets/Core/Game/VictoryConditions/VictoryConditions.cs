using RTSLockstep;
using UnityEngine;

public abstract class VictoryCondition : BehaviourHelper
{
    // private VictoryCondition[] victoryConditions;
    private HUD hud;
    private static bool Setted = false;

    protected void Setup()
    {
        LoadDetails();
        Setted = true;
    }

    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
    }

    protected override void OnSimulate()
    {
        if (GameFinished())
        {
            ResultsScreen resultsScreen = hud.GetComponent<ResultsScreen>();
            resultsScreen.SetMetVictoryCondition(this);
            resultsScreen.enabled = true;
            Time.timeScale = 0.0f;
            Cursor.visible = true;
            ResourceManager.MenuOpen = true;
            hud.enabled = false;
        }
    }

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

    private void LoadDetails()
    {
        AgentCommander[] commanders = GameObject.FindObjectsOfType(typeof(AgentCommander)) as AgentCommander[];
        hud = PlayerManager.MainController.Commander.GetComponentInChildren<HUD>();

        SetCommanders(commanders);
    }
}