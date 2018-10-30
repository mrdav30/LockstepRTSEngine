using UnityEngine;
using RTSLockstep;

// If critical that the unit survives, then you could always override GameFinished() 
// to make sure that the game ends if the truck is destroyed

public class EscortConvoy : VictoryCondition
{

    public Vector3 destination = new Vector3(0.0f, 0.0f, 0.0f);
    public Texture2D highlight;

    void Start()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Ground";
        cube.transform.localScale = new Vector3(3, 0.01f, 3);
        cube.transform.position = new Vector3(destination.x, 0.005f, destination.z);
        if (highlight)
        {
            cube.GetComponent<Renderer>().material.mainTexture = highlight;
        }
        cube.transform.parent = this.transform;
    }

    public override string GetDescription()
    {
        return "Escort Convoy";
    }

    public override bool CommanderMeetsConditions(AgentCommander commander)
    {
        RTSAgent[] agents = commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>();
        foreach(RTSAgent agent in agents)
        {
            if (agent.GetAbility<Convoy>())
            {
                return commander && !commander.IsDead() && ConvoyInPosition(agent);
            }
        }
        return false;
    }

    private bool ConvoyInPosition(RTSAgent agent)
    {
        if (!agent)
        {
            return false;
        }
        float closeEnough = 3.0f;
        Vector3d agentPos = agent.Body.Position3d;
        bool xInPos = agentPos.x > destination.x - closeEnough && agentPos.x < destination.x + closeEnough;
        bool zInPos = agentPos.z > destination.z - closeEnough && agentPos.z < destination.z + closeEnough;
        return xInPos && zInPos;
    }
}