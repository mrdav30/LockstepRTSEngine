using FastCollections;
using RTSLockstep.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public sealed class AgentController
    {
        #region Properties
        public string ControllerName { get; private set; }

        public static Dictionary<string, FastStack<RTSAgent>> CachedAgents;

        public static readonly bool[] GlobalAgentActive = new bool[MaxAgents * 4];
        public static readonly RTSAgent[] GlobalAgents = new RTSAgent[MaxAgents * 4];

        private static readonly FastStack<ushort> OpenGlobalIDs = new FastStack<ushort>();

        public static ushort PeakGlobalID { get; private set; }

        public const int MaxAgents = 16384;

        public static Dictionary<ushort, FastList<bool>> TypeAgentsActive = new Dictionary<ushort, FastList<bool>>();
        public static Dictionary<ushort, FastList<RTSAgent>> TypeAgents = new Dictionary<ushort, FastList<RTSAgent>>();
        internal static FastBucket<RTSAgent> DeathingAgents = new FastBucket<RTSAgent>();

        public struct DeactivationData
        {
            public RTSAgent Agent;
            public bool Immediate;

            public DeactivationData(RTSAgent agent, bool immediate)
            {
                Agent = agent;
                Immediate = immediate;
            }
        }

        static FastList<DeactivationData> DeactivationBuffer = new FastList<DeactivationData>();

        public const ushort UNREGISTERED_TYPE_INDEX = ushort.MaxValue;
        public static AgentController DefaultController { get { return InstanceManagers[0]; } }
        //TODO: Hide this list and use methods to access it
        //Also, move static AC stuff into its own class
        public static FastList<AgentController> InstanceManagers = new FastList<AgentController>();
        public readonly FastBucket<RTSAgent> SelectedAgents = new FastBucket<RTSAgent>();

        public bool SelectionChanged { get; set; }

        public readonly RTSAgent[] LocalAgents = new RTSAgent[MaxAgents];
        public readonly bool[] LocalAgentActive = new bool[MaxAgents];

        public byte ControllerID { get; private set; }

        public ushort PeakLocalID { get; private set; }

        public int PlayerIndex { get; set; }

        public bool HasTeam { get; private set; }

        public Team MyTeam { get; private set; }

        public AllegianceType DefaultAllegiance { get; private set; }

        private readonly FastList<AllegianceType> DiplomacyFlags = new FastList<AllegianceType>();
        private readonly FastStack<ushort> OpenLocalIDs = new FastStack<ushort>();

        private AgentCommander _commander;
        public AgentCommander Commander
        {
            get
            {
                if (_commander.IsNotNull())
                    return _commander;
                else
                {
                    return null;
                }
            }
        }

        private Selection previousSelection = new Selection();

        public event Action<RTSAgent> onCreateAgent;
        #endregion

        #region Event Behavior
        public static void Initialize()
        {
            InstanceManagers.Clear();
            GlobalAgentActive.Clear();
            OpenGlobalIDs.FastClear();
            PeakGlobalID = 0;
            foreach (FastStack<RTSAgent> cache in CachedAgents.Values)
            {
                for (int j = 0; j < cache.Count; j++)
                {
                    cache.innerArray[j].SessionReset();
                }
            }
        }

        public static void Simulate()
        {
            for (int iterator = 0; iterator < PeakGlobalID; iterator++)
            {
                if (GlobalAgentActive[iterator] && GlobalAgentActive[iterator])
                {
                    GlobalAgents[iterator].Simulate();
                }
            }

        }

        public static void LateSimulate()
        {
            for (int i = 0; i < PeakGlobalID; i++)
            {
                if (GlobalAgentActive[i])
                    GlobalAgents[i].LateSimulate();
            }
            CheckDestroyAgent();
        }

        public static void Visualize()
        {
            for (int iterator = 0; iterator < PeakGlobalID; iterator++)
            {
                if (GlobalAgentActive[iterator])
                {
                    GlobalAgents[iterator].Visualize();
                }
            }
        }

        public static void LateVisualize()
        {
            for (int iterator = 0; iterator < PeakGlobalID; iterator++)
            {
                if (GlobalAgentActive[iterator])
                {
                    GlobalAgents[iterator].LateVisualize();
                }
            }
        }

        public static void Deactivate()
        {
            for (int i = 0; i < PeakGlobalID; i++)
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
                    RTSAgent agent = DeathingAgents[i];
                    AgentController.CompleteLife(agent);
                }
            }
            DeathingAgents.FastClear();
        }
        #endregion

        #region Public
        public static bool TryGetAgentInstance(int globalID, out RTSAgent returnAgent)
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
                    RTSAgent agent = GlobalAgents[i];
                    AgentController.DestroyAgent(agent);
                }
            }
        }

        public static void ChangeController(RTSAgent agent, AgentController newCont)
        {

            AgentController leController = agent.Controller;
            if (leController != null)
            {
                leController.LocalAgentActive[agent.LocalID] = false;
                GlobalAgentActive[agent.GlobalID] = false;
                leController.OpenLocalIDs.Add(agent.LocalID);
                OpenGlobalIDs.Add(agent.GlobalID);

                if (newCont == null)
                {
                    agent.InitializeController(null, 0, 0);
                }
                else
                {
                    agent.Influencer.Deactivate();

                    newCont.AddAgent(agent);
                    agent.Influencer.Initialize();

                }
            }
        }

        public static void DestroyAgent(RTSAgent agent, bool immediate = false)
        {
            DeactivationBuffer.Add(new DeactivationData(agent, immediate));
        }

        /// <summary>
        /// Completes the life of the agent and pools or destroys it.
        /// </summary>
        /// <param name="agent">Agent.</param>
        public static void CompleteLife(RTSAgent agent)
        {
            if (agent.CachedGameObject != null)
                agent.CachedGameObject.SetActive(false);
            if (agent.TypeIndex != UNREGISTERED_TYPE_INDEX)
            {
                AgentController.CacheAgent(agent);

            }
            else
            {
                //This agent was not registered for pooling. Let's destroy it
                GameObject.Destroy(agent.gameObject);
            }
        }

        public static void CacheAgent(RTSAgent agent)
        {
            if (LockstepManager.PoolingEnabled)
                CachedAgents[agent.MyAgentCode].Add(agent);
            else
                GameObject.Destroy(agent.gameObject);
        }

        public static int GetStateHash()
        {
            int operationToggle = 0;
            int hash = LSUtility.PeekRandom(int.MaxValue);
            for (int i = 0; i < PeakGlobalID; i++)
            {
                if (GlobalAgentActive[i])
                {
                    RTSAgent agent = GlobalAgents[i];
                    int n1 = agent.Body._position.GetHashCode() + agent.Body._rotation.GetStateHash();
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

        public HUD GetCommanderHUD()
        {
            return _commander.transform.GetComponentInChildren<HUD>();
        }

        public BuildManager GetCommanderBuilderManager()
        {
            return _commander.transform.GetComponentInChildren<BuildManager>();
        }

        public static AgentController GetInstanceManager(int index)
        {
            if (index >= AgentController.InstanceManagers.Count)
            {
                Debug.LogError("Controller with index " + index + " not created. You can automatically create controllers by configuring AgentControllerCreator.");
                return null;
            }
            else if (index < 0)
            {
                Debug.LogError("Controller cannot have negative index.");
            }
            return AgentController.InstanceManagers[index];
        }

        public static AgentController Create(AllegianceType defaultAllegiance = AllegianceType.Neutral, string controllerName = "")
        {
            return new AgentController(defaultAllegiance, controllerName);
        }


        public void CreateCommander()
        {
            if (Commander != null)
                Debug.LogError("A commander called '" + Commander.gameObject.name + "' already exists for '" + this.ToString() + "'.");
            if (!UnityEngine.Object.FindObjectOfType<RTSGameManager>())
                Debug.LogError("A game manager has not been initialized!");

            //load from ls db
            GameObject commanderObject = GameObject.Instantiate(GameResourceManager.GetCommanderObject(), UnityEngine.Object.FindObjectOfType<RTSGameManager>().GetComponentInChildren<AgentCommanders>().transform);

            commanderObject.name = this.ControllerName;

            AgentCommander commanderClone = commanderObject.GetComponent<AgentCommander>();
            //change to user's selected username
            commanderClone.username = this.ControllerName;
            commanderClone.SetController(this);

            if (PlayerManager.ContainsController(this))
            {
                commanderClone.human = true;
            }

            //come up with better way to set selected commander to the current commander
            if (this == PlayerManager.MainController)
            {
                PlayerManager.SelectPlayer(commanderClone.username, 0, this.ControllerID, this.PlayerIndex);
            }

            _commander = commanderClone;
            BehaviourHelperManager.InitializeOnDemand(_commander);
        }

        public void AddToSelection(RTSAgent agent)
        {
            if (agent.IsSelected == false)
            {
                SelectedAgents.Add(agent);
                SelectionChanged = true;
            }
        }

        public void RemoveFromSelection(RTSAgent agent)
        {
            SelectedAgents.Remove(agent);
            SelectionChanged = true;
        }



        public Selection GetSelection(Command com)
        {
            if (com.ContainsData<Selection>() == false)
            {
                return previousSelection;
            }
            return com.GetData<Selection>();
        }

        public void Execute(Command com)
        {
            if (com.ContainsData<Selection>())
            {
                previousSelection = com.GetData<Selection>();
            }

            BehaviourHelperManager.Execute(com);
            Selection selection = GetSelection(com);
            for (int i = 0; i < selection.selectedAgentLocalIDs.Count; i++)
            {
                ushort selectedAgentID = selection.selectedAgentLocalIDs[i];
                if (LocalAgentActive[selectedAgentID])
                {
                    var agent = LocalAgents[selectedAgentID];
                    ////Prevent executing twice on commander
                    //if (Commander.IsNull() || agent != Commander.Agent) {
                    agent.Execute(com);
                    //}
                }
            }
            //if (Commander.IsNotNull())
            //Commander.Agent.Execute (com);
        }

        public void AddAgent(RTSAgent agent)
        {
            ushort localID = GenerateLocalID();
            LocalAgents[localID] = agent;
            LocalAgentActive[localID] = true;

            ushort globalID = GenerateGlobalID();
            GlobalAgentActive[globalID] = true;
            GlobalAgents[globalID] = agent;

            agent.InitializeController(this, localID, globalID);
        }

        public RTSAgent CreateAgent(string agentCode, Vector2d position)
        {
            RTSAgent agent = CreateAgent(agentCode, position, Vector2d.right);
            if (onCreateAgent != null)
                onCreateAgent(agent);
            return agent;
        }

        public static void RegisterRawAgent(RTSAgent agent)
        {
            var agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
            FastList<bool> typeActive;
            if (!AgentController.TypeAgentsActive.TryGetValue(agentCodeID, out typeActive))
            {
                typeActive = new FastList<bool>();
                TypeAgentsActive.Add(agentCodeID, typeActive);
            }
            FastList<RTSAgent> typeAgents;
            if (!TypeAgents.TryGetValue(agentCodeID, out typeAgents))
            {
                typeAgents = new FastList<RTSAgent>();
                TypeAgents.Add(agentCodeID, typeAgents);
            }

            //TypeIndex of ushort.MaxValue means that this agent isn't registered for the pool
            agent.TypeIndex = (ushort)(typeAgents.Count);
            typeAgents.Add(agent);
            typeActive.Add(true);
        }

        /// <summary>
        /// Create an uninitialized RTSAgent
        /// </summary>
        /// <returns>The raw agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        /// <param name="isBare">If set to <c>true</c> is bare.</param>
        public static RTSAgent CreateRawAgent(string agentCode)
        {
            if (!GameResourceManager.IsValidAgentCode(agentCode))
            {
                throw new System.ArgumentException(string.Format("Agent code '{0}' not found.", agentCode));
            }
            FastStack<RTSAgent> cache = CachedAgents[agentCode];
            RTSAgent curAgent = null;

            if (cache.IsNotNull() && cache.Count > 0)
            {
                curAgent = cache.Pop();
                ushort agentCodeID = GameResourceManager.GetAgentCodeIndex(agentCode);
                Debug.Log(curAgent.TypeIndex);
                TypeAgentsActive[agentCodeID][curAgent.TypeIndex] = true;
            }
            else
            {
                IAgentData interfacer = GameResourceManager.AgentCodeInterfacerMap[agentCode];

                curAgent = GameObject.Instantiate(GameResourceManager.GetAgentTemplate(agentCode).gameObject).GetComponent<RTSAgent>();
                curAgent.Setup(interfacer);

                RegisterRawAgent(curAgent);

            }
            return curAgent;
        }
        public RTSAgent CreateAgent(string agentCode, Vector2d position, Vector2d rotation)
        {
            var agent = CreateRawAgent(agentCode);
            InitializeAgent(agent, position, rotation);

            return agent;
        }

        /// <summary>
        /// Creates and initializes an agent without activating LSBody, LSInfluencer, etc..
        /// </summary>
        /// <returns>The bare agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        public RTSAgent CreateBareAgent(string agentCode)
        {
            var agent = CreateRawAgent(agentCode);
            AddAgent(agent);
            agent.InitializeBare();
            return agent;
        }

        /// <summary>
        /// Creates an agent without initialization
        /// </summary>
        /// <returns>The dumb agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        public RTSAgent CreateDumbAgent(string agentCode)
        {
            var agent = CreateRawAgent(agentCode);
            AddAgent(agent);
            return agent;
        }

        public void InitializeAgent(RTSAgent agent,
                                      Vector2d position,
                                      Vector2d rotation)
        {
            AddAgent(agent);
            agent.Initialize(position, rotation);
        }



        public void SetAllegiance(AgentController otherController, AllegianceType allegianceType)
        {
            while (DiplomacyFlags.Count <= otherController.ControllerID)
            {
                DiplomacyFlags.Add(AllegianceType.Neutral);
            }
            DiplomacyFlags[otherController.ControllerID] = allegianceType;
        }

        public AllegianceType GetAllegiance(AgentController otherController)
        {
            return HasTeam && otherController.HasTeam ? MyTeam.GetAllegiance(otherController) : DiplomacyFlags[otherController.ControllerID];
        }

        public AllegianceType GetAllegiance(byte controllerID)
        {
            if (HasTeam)
            {
                //TODO: Team allegiance
            }

            return DiplomacyFlags[controllerID];
        }

        public void JoinTeam(Team team)
        {
            MyTeam = team;
            HasTeam = true;
        }

        public void LeaveTeam()
        {
            HasTeam = false;
        }
        #endregion

        #region Private
        private static ushort GenerateGlobalID()
        {
            if (OpenGlobalIDs.Count > 0)
            {
                return OpenGlobalIDs.Pop();
            }
            return PeakGlobalID++;
        }

        private static void CheckDestroyAgent()
        {
            for (int i = 0; i < DeactivationBuffer.Count; i++)
            {
                DestroyAgentBuffer(DeactivationBuffer[i]);
            }
            DeactivationBuffer.FastClear();
        }

        private static void DestroyAgentBuffer(DeactivationData data)
        {
            RTSAgent agent = data.Agent;
            if (agent.IsActive == false)
                return;
            bool immediate = data.Immediate;

            agent.Deactivate(immediate);
            ChangeController(agent, null);

            //Pool if the agent is registered
            ushort agentCodeID;
            if (agent.TypeIndex != UNREGISTERED_TYPE_INDEX)
            {
                // if (CodeIndexMap.TryGetValue(agent.MyAgentCode, out agentCodeID))
                // {
                agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
                if (agentCodeID.IsNotNull())
                {
                    TypeAgentsActive[agentCodeID][agent.TypeIndex] = false;
                }
            }
        }

        private static void UpdateDiplomacy(AgentController newCont)
        {
            for (int i = 0; i < InstanceManagers.Count; i++)
            {
                var other = InstanceManagers[i];
                other.SetAllegiance(newCont, other.DefaultAllegiance);
                newCont.SetAllegiance(other, newCont.DefaultAllegiance);
            }
        }

        private AgentController(AllegianceType defaultAllegiance, string controllerName)
        {
            if (InstanceManagers.Count > byte.MaxValue)
            {
                throw new System.Exception("Cannot have more than 256 AgentControllers");
            }
            OpenLocalIDs.FastClear();
            PeakLocalID = 0;
            ControllerID = (byte)InstanceManagers.Count;
            ControllerName = controllerName;
            DefaultAllegiance = defaultAllegiance;

            for (int i = 0; i < InstanceManagers.Count; i++)
            {
                this.SetAllegiance(InstanceManagers[i], AllegianceType.Neutral);
            }

            InstanceManagers.Add(this);
            UpdateDiplomacy(this);
            this.SetAllegiance(this, AllegianceType.Friendly);
        }

        private ushort GenerateLocalID()
        {
            if (OpenLocalIDs.Count > 0)
            {
                return OpenLocalIDs.Pop();
            }
            else
            {
                return PeakLocalID++;
            }
        }
        #endregion
    }

    //Implemented as flags for selecting multiple types.
    [System.Flags]
    public enum AllegianceType : byte
    {
        Neutral = 1 << 0,
        Friendly = 1 << 1,
        Enemy = 1 << 2,
        All = 0xff
    }

}