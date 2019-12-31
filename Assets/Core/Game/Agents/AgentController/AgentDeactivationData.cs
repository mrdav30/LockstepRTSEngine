namespace RTSLockstep.Agents.AgentControllerSystem
{
    public struct AgentDeactivationData
    {
        public LSAgent Agent { get; private set; }
        public bool Immediate { get; private set; }

        public AgentDeactivationData(LSAgent agent, bool immediate)
        {
            Agent = agent;
            Immediate = immediate;
        }
    }
}
