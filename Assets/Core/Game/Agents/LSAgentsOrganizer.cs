using System;
using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.LSResources;
using RTSLockstep.Utility;

namespace RTSLockstep.Agents
{
    public class LSAgentsOrganizer: MonoBehaviour
    {
        public Dictionary<AgentType, Transform> AgentContainers;
        public void Setup()
        {
            AgentContainers = new Dictionary<AgentType, Transform>();

            foreach (var type in (AgentType[])Enum.GetValues(typeof(AgentType)))
            {
                Transform container = LSUtility.CreateEmpty().transform;
                container.parent = transform;
                container.gameObject.name = (type.ToString() + "Container");

                AgentContainers.Add(type, container);
            }
        }
    }
}
