using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentControllerSystem;

namespace RTSLockstep.Abilities.Essential
{
    public class AttackerInfo
    {
        public AttackerInfo(LSAgent attacker, AgentController controller)
        {
            Attacker = attacker;
            Controller = controller;
        }
        public LSAgent Attacker;
        public AgentController Controller;
    }
}