using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.Managers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.LSResources;
using RTSLockstep.Utility;
using RTSLockstep.Utility.FastCollections;

namespace RTSLockstep.Agents.AgentController
{
    public static class GlobalAgentController
    {
        #region Properties
        public static Dictionary<string, FastStack<LSAgent>> CachedAgents;

        public static readonly bool[] GlobalAgentActive = new bool[MaxAgents * 4];
        public static readonly LSAgent[] GlobalAgents = new LSAgent[MaxAgents * 4];

        public const int MaxAgents = 16384;
        public const ushort UnregisterdTypeIndex = ushort.MaxValue;

        public static Dictionary<ushort, FastList<bool>> TypeAgentsActive = new Dictionary<ushort, FastList<bool>>();
        public static Dictionary<ushort, FastList<LSAgent>> TypeAgents = new Dictionary<ushort, FastList<LSAgent>>();

        // TODO: Hide this list and use methods to access it
        public static FastList<LocalAgentController> InstanceManagers = new FastList<LocalAgentController>();

        private static readonly FastStack<ushort> _openGlobalIDs = new FastStack<ushort>();

        private static ushort _peakGlobalID;

        internal static FastBucket<LSAgent> DeathingAgents = new FastBucket<LSAgent>();

        private static FastList<AgentDeactivationData> DeactivationBuffer = new FastList<AgentDeactivationData>();
        #endregion

        #region Event Behavior
        public static void Initialize()
        {
            InstanceManagers.Clear();
            GlobalAgentActive.Clear();
            _openGlobalIDs.FastClear();
            _peakGlobalID = 0;
            foreach (FastStack<LSAgent> cache in CachedAgents.Values)
            {
                for (int j = 0; j < cache.Count; j++)
                {
                    cache.innerArray[j].SessionReset();
                }
            }
        }

        public static void Simulate()
        {
            for (int iterator = 0; iterator < _peakGlobalID; iterator++)
            {
                if (GlobalAgentActive[iterator] && GlobalAgentActive[iterator])
                {
                    GlobalAgents[iterator].Simulate();
                }
            }
        }

        public static void LateSimulate()
        {
            for (int i = 0; i < _peakGlobalID; i++)
            {
                if (GlobalAgentActive[i])
                {
                    GlobalAgents[i].LateSimulate();
                }
            }

            CheckDestroyAgent();
        }

        public static void Visualize()
        {
            for (int iterator = 0; iterator < _peakGlobalID; iterator++)
            {
                LSAgent agent = GlobalAgents[iterator];
                if (agent.IsLive)
                {
                    GlobalAgents[iterator].Visualize();
                }
            }
        }

        public static void LateVisualize()
        {
            for (int iterator = 0; iterator < _peakGlobalID; iterator++)
            {
                LSAgent agent = GlobalAgents[iterator];
                if (agent.IsLive)
                {
                    GlobalAgents[iterator].LateVisualize();
                }
            }
        }

        public static void Deactivate()
        {
            for (int i = 0; i < _peakGlobalID; i++)
            {
                if (GlobalAgentActive[i])
                {
                    DestroyAgent(GlobalAgents[i], true);
                }
            }

            CheckDestroyAgent();

            for (int i = 0; i < DeathingAgents.PeakCount; i++)
            {
                if (DeathingAgents.arrayAllocation[i])
                {
                    LSAgent agent = DeathingAgents[i];
                    EndLife(agent);
                }
            }

            DeathingAgents.FastClear();
        }
        #endregion

        #region Public
        public static bool TryGetAgentInstance(int globalID, out LSAgent returnAgent)
        {
            if (GlobalAgentActive[globalID])
            {
                returnAgent = GlobalAgents[globalID];
                return true;
            }

            returnAgent = null;
            return false;
        }

        public static void ClearAgents()
        {
            for (int i = GlobalAgents.Length - 1; i >= 0; i--)
            {
                if (GlobalAgentActive[i])
                {
                    LSAgent agent = GlobalAgents[i];
                    DestroyAgent(agent);
                }
            }
        }

        public static void DestroyAgent(LSAgent agent, bool immediate = false)
        {
            DeactivationBuffer.Add(new AgentDeactivationData(agent, immediate));
        }

        /// <summary>
        /// Completes the life of the agent and pools or destroys it.
        /// </summary>
        /// <param name="agent">Agent.</param>
        public static void EndLife(LSAgent agent)
        {
            if (agent.CachedGameObject.IsNotNull())
            {
                agent.CachedGameObject.SetActive(false);
            }

            agent._EndLife();

            if (agent.TypeIndex != UnregisterdTypeIndex)
            {
                CacheAgent(agent);
            }
            else
            {
                //This agent was not registered for pooling. Let's destroy it
                Object.Destroy(agent.gameObject);
            }
        }

        public static void CacheAgent(LSAgent agent)
        {
            if (LockstepManager.PoolingEnabled)
            {
                CachedAgents[agent.MyAgentCode].Add(agent);
            }
            else
            {
                Object.Destroy(agent.gameObject);
            }
        }

        public static int GetStateHash()
        {
            int operationToggle = 0;
            int hash = 33; // LSUtility.PeekRandom(int.MaxValue);
            for (int i = 0; i < _peakGlobalID; i++)
            {
                if (GlobalAgentActive[i])
                {
                    LSAgent agent = GlobalAgents[i];
                    int n1 = agent.Body._position.GetHashCode() + agent.Body._rotation.GetHashCode();
                    switch (operationToggle)
                    {
                        case 0:
                            hash ^= n1;
                            break;
                        case 1:
                            hash += n1;
                            break;
                        default:
                            hash ^= n1 * 3;
                            break;
                    }

                    operationToggle++;
                    if (operationToggle >= 2)
                    {
                        operationToggle = 0;
                    }
                }
            }

            return hash;
        }

        public static LocalAgentController GetInstanceManager(int index)
        {
            if (index >= InstanceManagers.Count)
            {
                Debug.LogError("Controller with index " + index + " not created. You can automatically create controllers by configuring AgentControllerCreator.");
                return null;
            }
            else if (index < 0)
            {
                Debug.LogError("Controller cannot have negative index.");
            }

            return InstanceManagers[index];
        }

        public static LocalAgentController Create(AllegianceType defaultAllegiance = AllegianceType.Neutral, string controllerName = "")
        {
            return new LocalAgentController(defaultAllegiance, controllerName);
        }
    
        public static void RegisterRawAgent(LSAgent agent)
        {
            ushort agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
            if (!TypeAgentsActive.TryGetValue(agentCodeID, out FastList<bool> typeActive))
            {
                typeActive = new FastList<bool>();
                TypeAgentsActive.Add(agentCodeID, typeActive);
            }

            if (!TypeAgents.TryGetValue(agentCodeID, out FastList<LSAgent> typeAgents))
            {
                typeAgents = new FastList<LSAgent>();
                TypeAgents.Add(agentCodeID, typeAgents);
            }

            //TypeIndex of ushort.MaxValue means that this agent isn't registered for the pool
            agent.TypeIndex = (ushort)(typeAgents.Count);
            typeAgents.Add(agent);
            typeActive.Add(true);
        }

        public static void SetFullHostile(LocalAgentController con)
        {
            //TODO: Make this hostile to new controllers
            for (int j = 0; j < InstanceManagers.Count; j++)
            {
                if (j == con.ControllerID) continue;
                LocalAgentController ac = InstanceManagers[j];
                if (ac != con)
                {
                    con.SetAllegiance(ac, AllegianceType.Enemy);
                    ac.SetAllegiance(con, AllegianceType.Enemy);
                }
            }
        }

        public static void ChangeController(LSAgent agent, LocalAgentController newController)
        {
            LocalAgentController currentController = agent.Controller;
            if (currentController.IsNotNull())
            {
                currentController.LocalAgentActive[agent.LocalID] = false;
                currentController.OpenLocalIDs.Add(agent.LocalID);

                GlobalAgentActive[agent.GlobalID] = false;
                _openGlobalIDs.Add(agent.GlobalID);

                if (newController.IsNull())
                {
                    // initialize with no controller
                    agent.InitializeController(null, 0, 0);
                }
                else
                {
                    agent.Influencer.Deactivate();

                    newController.AddAgent(agent);
                    agent.Influencer.Initialize();
                }
            }
        }

        public static ushort GenerateGlobalID()
        {
            if (_openGlobalIDs.Count > 0)
            {
                return _openGlobalIDs.Pop();
            }
            return _peakGlobalID++;
        }

        public static void UpdateDiplomacy(LocalAgentController newController)
        {
            for (int i = 0; i < InstanceManagers.Count; i++)
            {
                LocalAgentController other = InstanceManagers[i];
                other.SetAllegiance(newController, other.DefaultAllegiance);
                if (other.DefaultAllegiance == AllegianceType.Neutral)
                {
                    newController.SetAllegiance(other, AllegianceType.Neutral);
                }
                else
                {
                    newController.SetAllegiance(other, newController.DefaultAllegiance);
                }
            }
        }
        #endregion

        #region Private
        private static void CheckDestroyAgent()
        {
            for (int i = 0; i < DeactivationBuffer.Count; i++)
            {
                DestroyAgentBuffer(DeactivationBuffer[i]);
            }
            DeactivationBuffer.FastClear();
        }

        private static void DestroyAgentBuffer(AgentDeactivationData data)
        {
            LSAgent agent = data.Agent;
            if (!agent.IsActive)
            {
                return;
            }

            bool immediate = data.Immediate;

            agent._Deactivate(immediate);
            ChangeController(agent, null);

            //Pool if the agent is registered
            ushort agentCodeID;
            if (agent.TypeIndex != UnregisterdTypeIndex)
            {
                agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
                if (agentCodeID.IsNotNull())
                {
                    TypeAgentsActive[agentCodeID][agent.TypeIndex] = false;
                }
            }
        }
        #endregion
    }
}