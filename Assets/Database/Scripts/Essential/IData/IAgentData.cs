using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;
namespace RTSLockstep.Data
{
    public interface IAgentData : INamedData
    {
        LSAgent GetAgent();
        Texture2D GetAgentIcon();
    }
}