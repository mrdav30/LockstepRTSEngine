using RTSLockstep.Agents;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Environment
{
    [System.Serializable]
    public class EnvironmentAgentInfo
    {
        public EnvironmentAgentInfo(string agentCode, LSAgent agent, Vector3d pos, Vector2d rot)
        {
            AgentCode = agentCode;
            Agent = agent;
            Position = pos;
            Rotation = rot;
        }
        public string AgentCode;
        public LSAgent Agent;
        public Vector3d Position;
        public Vector2d Rotation;
    }
}