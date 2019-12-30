using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Agents.Teams;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using System;
using System.Collections.Generic;
using UnityEngine;
using RTSLockstep.Simulation.Influence;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Agents.AgentControllerSystem
{
    public sealed class AgentController
    {
        #region Properties
        public string ControllerName { get; private set; }

        public static Dictionary<string, FastStack<LSAgent>> CachedAgents;

        public static readonly bool[] GlobalAgentActive = new bool[MaxAgents * 4];
        public static readonly LSAgent[] GlobalAgents = new LSAgent[MaxAgents * 4];

        private static readonly FastStack<ushort> OpenGlobalIDs = new FastStack<ushort>();

        public static ushort PeakGlobalID { get; private set; }

        public const int MaxAgents = 16384;

        public static Dictionary<ushort, FastList<bool>> TypeAgentsActive = new Dictionary<ushort, FastList<bool>>();
        public static Dictionary<ushort, FastList<LSAgent>> TypeAgents = new Dictionary<ushort, FastList<LSAgent>>();
        internal static FastBucket<LSAgent> DeathingAgents = new FastBucket<LSAgent>();

        public struct DeactivationData
        {
            public LSAgent Agent;
            public bool Immediate;

            public DeactivationData(LSAgent agent, bool immediate)
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
        public readonly FastBucket<LSAgent> SelectedAgents = new FastBucket<LSAgent>();

        public bool SelectionChanged { get; set; }

        public readonly LSAgent[] LocalAgents = new LSAgent[MaxAgents];
        public readonly bool[] LocalAgentActive = new bool[MaxAgents];

        public byte ControllerID { get; private set; }

        public ushort PeakLocalID { get; private set; }

        public int PlayerIndex { get; set; }

        public bool HasTeam { get; private set; }

        public Team MyTeam { get; private set; }

        public AllegianceType DefaultAllegiance { get; private set; }

        private readonly FastList<AllegianceType> DiplomacyFlags = new FastList<AllegianceType>();
        private readonly FastStack<ushort> OpenLocalIDs = new FastStack<ushort>();

        private LSPlayer _commander;
        public LSPlayer Player
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

        public Selection previousSelection = new Selection();

        public event Action<LSAgent> OnCreateAgent;
        #endregion

        #region Event Behavior
        public static void Initialize()
        {
            InstanceManagers.Clear();
            GlobalAgentActive.Clear();
            OpenGlobalIDs.FastClear();
            PeakGlobalID = 0;
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
                LSAgent agent = GlobalAgents[iterator];
                if (agent.IsLive)
                {
                    GlobalAgents[iterator].Visualize();
                }
            }
        }

        public static void LateVisualize()
        {
            for (int iterator = 0; iterator < PeakGlobalID; iterator++)
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

        public static void ChangeController(LSAgent agent, AgentController newCont)
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

        public static void DestroyAgent(LSAgent agent, bool immediate = false)
        {
            DeactivationBuffer.Add(new DeactivationData(agent, immediate));
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

            if (agent.TypeIndex != UNREGISTERED_TYPE_INDEX)
            {
                CacheAgent(agent);
            }
            else
            {
                //This agent was not registered for pooling. Let's destroy it
                UnityEngine.Object.Destroy(agent.gameObject);
            }
        }

        public static void CacheAgent(LSAgent agent)
        {
            if (LockstepManager.PoolingEnabled)
                CachedAgents[agent.MyAgentCode].Add(agent);
            else
                GameObject.Destroy(agent.gameObject);
        }

        public static int GetStateHash()
        {
            int operationToggle = 0;
            int hash = 33; // LSUtility.PeekRandom(int.MaxValue);
            for (int i = 0; i < PeakGlobalID; i++)
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

        public HUD GetCommanderHUD()
        {
            return _commander.transform.GetComponentInChildren<HUD>();
        }

        public static AgentController GetInstanceManager(int index)
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

        public static AgentController Create(AllegianceType defaultAllegiance = AllegianceType.Neutral, string controllerName = "")
        {
            return new AgentController(defaultAllegiance, controllerName);
        }

        public void CreatePlayer()
        {
            if (Player.IsNotNull())
            {
                Debug.LogError("A player called '" + Player.gameObject.name + "' already exists for '" + ToString() + "'.");
            }

            if (!UnityEngine.Object.FindObjectOfType<RTSGameManager>())
            {
                Debug.LogError("A game manager has not been initialized!");
            }

            //load from ls db
            GameObject playerObject = UnityEngine.Object.Instantiate(GameResourceManager.GetPlayerObject(), UnityEngine.Object.FindObjectOfType<RTSGameManager>().GetComponentInChildren<LSPlayers>().transform);

            playerObject.name = ControllerName;

            LSPlayer playerClone = playerObject.GetComponent<LSPlayer>();
            //change to user's selected username
            playerClone.username = ControllerName;
            playerClone.SetController(this);

            if (PlayerManager.ContainsController(this))
            {
                playerClone.human = true;
            }

            //come up with better way to set selected commander to the current commander
            if (this == PlayerManager.MainController)
            {
                PlayerManager.SelectPlayer(playerClone.username, 0, ControllerID, PlayerIndex);
            }

            _commander = playerClone;
            BehaviourHelperManager.InitializeOnDemand(_commander);
        }

        public void AddToSelection(LSAgent agent)
        {
            if (!agent.IsSelected)
            {
                SelectedAgents.Add(agent);
                SelectionChanged = true;
            }
        }

        public void RemoveFromSelection(LSAgent agent)
        {
            SelectedAgents.Remove(agent);
            SelectionChanged = true;
        }

        public Selection GetSelection(Command com)
        {
            if (!com.ContainsData<Selection>())
            {
                return previousSelection;
            }

            return com.GetData<Selection>();
        }

        public Influence GetInfluencedAgent(Command com)
        {
            if (!com.ContainsData<Influence>())
            {
                return null;
            }

            return com.GetData<Influence>();
        }

        public void Execute(Command com)
        {
            Selection selection = GetSelection(com);
            //check to see if selection has changed since the last command
            ushort[] arr1 = selection.selectedAgentLocalIDs.innerArray;
            ushort[] arr2 = previousSelection.selectedAgentLocalIDs.innerArray;

            if (!arr1.StructuralEquals(arr2))
            {
                previousSelection = selection;
                SelectionChanged = true;
            }
            else
            {
                SelectionChanged = false;
            }

            Influence influence = GetInfluencedAgent(com);

            BehaviourHelperManager.Execute(com);

            // check if command is an influence from AI
            if (influence.IsNotNull())
            {
                ushort influencedAgentID = influence.InfluencedAgentLocalID;
                if (LocalAgentActive[influencedAgentID])
                {
                    var agent = LocalAgents[influencedAgentID];
                    agent.Execute(com);
                }
            }
            // otherwise it's an input command
            else if (selection != null && selection.selectedAgentLocalIDs.Count > 0)
            {
                for (int i = 0; i < selection.selectedAgentLocalIDs.Count; i++)
                {
                    ushort selectedAgentID = selection.selectedAgentLocalIDs[i];
                    if (LocalAgentActive[selectedAgentID])
                    {
                        var agent = LocalAgents[selectedAgentID];
                        agent.Execute(com);
                    }
                }
            }
        }

        public void AddAgent(LSAgent agent)
        {
            ushort localID = GenerateLocalID();
            LocalAgents[localID] = agent;
            LocalAgentActive[localID] = true;

            ushort globalID = GenerateGlobalID();
            GlobalAgentActive[globalID] = true;
            GlobalAgents[globalID] = agent;

            agent.InitializeController(this, localID, globalID);
        }

        public LSAgent CreateAgent(string agentCode, Vector2d position)
        {
            LSAgent agent = CreateAgent(agentCode, position, Vector2d.right);
            return agent;
        }

        public LSAgent CreateAgent(string agentCode, Vector2d position, Vector2d rotation)
        {
            var agent = CreateRawAgent(agentCode, position, rotation);

            InitializeAgent(agent, position, rotation);
            OnCreateAgent?.Invoke(agent);

            return agent;
        }

        /// <summary>
        /// Create an uninitialized RTSAgent
        /// </summary>
        /// <returns>The raw agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        /// <param name="isBare">If set to <c>true</c> is bare.</param>
        public static LSAgent CreateRawAgent(string agentCode, Vector2d startPosition = default(Vector2d), Vector2d startRotation = default(Vector2d))
        {
            if (!GameResourceManager.IsValidAgentCode(agentCode))
            {
                throw new ArgumentException(string.Format("Agent code '{0}' not found.", agentCode));
            }
            FastStack<LSAgent> cache = CachedAgents[agentCode];
            LSAgent curAgent;

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

                Vector3 pos = startPosition.ToVector3();
                Quaternion rot = new Quaternion(0, startRotation.y, 0, startRotation.x);

                curAgent = UnityEngine.Object.Instantiate(GameResourceManager.GetAgentTemplate(agentCode).gameObject, pos, rot).GetComponent<LSAgent>();
                curAgent.Setup(interfacer);

                RegisterRawAgent(curAgent);

            }
            return curAgent;
        }

        public static void RegisterRawAgent(LSAgent agent)
        {
            var agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
            FastList<bool> typeActive;
            if (!TypeAgentsActive.TryGetValue(agentCodeID, out typeActive))
            {
                typeActive = new FastList<bool>();
                TypeAgentsActive.Add(agentCodeID, typeActive);
            }
            FastList<LSAgent> typeAgents;
            if (!TypeAgents.TryGetValue(agentCodeID, out typeAgents))
            {
                typeAgents = new FastList<LSAgent>();
                TypeAgents.Add(agentCodeID, typeAgents);
            }

            //TypeIndex of ushort.MaxValue means that this agent isn't registered for the pool
            agent.TypeIndex = (ushort)(typeAgents.Count);
            typeAgents.Add(agent);
            typeActive.Add(true);
        }

        /// <summary>
        /// Creates and initializes an agent without activating LSBody, LSInfluencer, etc..
        /// </summary>
        /// <returns>The bare agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        public LSAgent CreateBareAgent(string agentCode, Vector2d startPosition = default(Vector2d), Vector2d startRotation = default(Vector2d))
        {
            var agent = CreateRawAgent(agentCode, startPosition, startRotation);
            return agent;
        }

        public void InitializeAgent(LSAgent agent,
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
                if (other.DefaultAllegiance == AllegianceType.Neutral)
                {
                    newCont.SetAllegiance(other, AllegianceType.Neutral);
                }
                else
                {
                    newCont.SetAllegiance(other, newCont.DefaultAllegiance);
                }
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
                SetAllegiance(InstanceManagers[i], AllegianceType.Neutral);
            }

            InstanceManagers.Add(this);
            UpdateDiplomacy(this);
            SetAllegiance(this, AllegianceType.Friendly);
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
}