using System;
using UnityEngine;
namespace RTSLockstep.Data
{
    [Serializable]
    public class AgentDataItem : ObjectDataItem, IAgentData
    {
        public AgentDataItem(string name, string description) : this()
        {
            base._name = name;
            base._description = description;
        }

        public AgentDataItem()
        {

        }

        public RTSAgent GetAgent()
        {
            if (this.Prefab != null)
            {
                RTSAgent agent = this.Prefab.GetComponent<RTSAgent>();
                if (agent)
                {
                    return agent;
                }
            }
            return null;
        }

        public String GetAgentDescription()
        {
            return _description;
        }

        public Texture2D GetAgentIcon()
        {
            if (this.Icon != null)
            {
                return this.Icon.texture;
            }
            return null;
        }

        public int SortDegreeFromAgentType(AgentType agentType)
        {
            RTSAgent agent = GetAgent();
            if (agent == null) return -1;
            if (agentType == agent.MyAgentType) return 1;
            return 0;
        }

#if UNITY_EDITOR

        GameObject lastPrefab;
        protected override void OnManage()
        {

            if (lastPrefab != Prefab)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    this._name = Prefab.name;
                }

                lastPrefab = Prefab;
            }
        }

#endif

    }
}
