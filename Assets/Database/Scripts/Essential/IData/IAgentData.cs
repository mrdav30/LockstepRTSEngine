using RTSLockstep.Agents;
using UnityEngine;

namespace RTSLockstep.Data
{
    public interface IAgentData : INamedData
    {
        LSAgent GetAgent();
        string GetAgentDescription();
        Texture2D GetAgentIcon();
    }
}