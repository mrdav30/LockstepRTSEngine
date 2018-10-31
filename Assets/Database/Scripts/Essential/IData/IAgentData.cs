using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;
namespace RTSLockstep.Data
{
    public interface IAgentData : INamedData
    {
        RTSAgent GetAgent();
        Texture2D GetAgentIcon();
    }
}