using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;
namespace RTSLockstep.Data
{
    public interface IAgentData : INamedData
    {
        RTSAgent GetAgent();
        string GetAgentDescription();
        Texture2D GetAgentIcon();
    }
}