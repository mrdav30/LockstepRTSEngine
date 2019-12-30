using UnityEngine;
using RTSLockstep.Agents;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Player;
using RTSLockstep.Simulation.LSMath;

// If critical that the unit survives, then you could always override GameFinished() 
// to make sure that the game ends if the truck is destroyed

namespace RTSLockstep.VictoryConditions
{
    public class EscortConvoy : VictoryCondition
    {

        public Vector3 destination = new Vector3(0.0f, 0.0f, 0.0f);
        public Texture2D highlight;

        private void Start()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Ground";
            cube.transform.localScale = new Vector3(3, 0.01f, 3);
            cube.transform.position = new Vector3(destination.x, 0.005f, destination.z);
            if (highlight)
            {
                cube.GetComponent<Renderer>().material.mainTexture = highlight;
            }
            cube.transform.parent = transform;
        }

        public override string GetDescription()
        {
            return "Escort Convoy";
        }

        public override bool PlayerMeetsConditions(LSPlayer player)
        {
            LSAgent[] agents = player.GetComponentInChildren<LSAgents>().GetComponentsInChildren<LSAgent>();
            foreach (LSAgent agent in agents)
            {
                if (agent.GetAbility<Convoy>())
                {
                    return player && !player.IsDead() && ConvoyInPosition(agent);
                }
            }
            return false;
        }

        private bool ConvoyInPosition(LSAgent agent)
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
}