using System;
using UnityEngine;

using RTSLockstep.Agents.Teams;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.Influence;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using RTSLockstep.Utility.FastCollections;

namespace RTSLockstep.Agents.AgentController
{
    public class LocalAgentController
    {
        #region Constructor
        public LocalAgentController(AllegianceType defaultAllegiance, string controllerName)
        {
            if (GlobalAgentController.InstanceManagers.Count > byte.MaxValue)
            {
                throw new Exception("Cannot have more than 256 AgentControllers");
            }
            OpenLocalIDs.FastClear();
            PeakLocalID = 0;
            ControllerID = (byte)GlobalAgentController.InstanceManagers.Count;
            ControllerName = controllerName;
            DefaultAllegiance = defaultAllegiance;

            for (int i = 0; i < GlobalAgentController.InstanceManagers.Count; i++)
            {
                SetAllegiance(GlobalAgentController.InstanceManagers[i], AllegianceType.Neutral);
            }

            GlobalAgentController.InstanceManagers.Add(this);
            GlobalAgentController.UpdateDiplomacy(this);
            SetAllegiance(this, AllegianceType.Friendly);
        }
        #endregion

        #region Properties
        public string ControllerName { get; private set; }

        public readonly FastStack<ushort> OpenLocalIDs = new FastStack<ushort>();

        public const int MaxAgents = 16384;
        public const ushort UnregisterdTypeIndex = ushort.MaxValue;

        public readonly FastBucket<LSAgent> SelectedAgents = new FastBucket<LSAgent>();

        public readonly LSAgent[] LocalAgents = new LSAgent[MaxAgents];
        public readonly bool[] LocalAgentActive = new bool[MaxAgents];

        public byte ControllerID { get; private set; }

        public ushort PeakLocalID { get; private set; }

        public int PlayerIndex { get; set; }

        public bool HasTeam { get; private set; }

        public Team MyTeam { get; private set; }

        public AllegianceType DefaultAllegiance { get; private set; }

        private readonly FastList<AllegianceType> _diplomacyFlags = new FastList<AllegianceType>();

        public LSPlayer ControllingPlayer;

        private Selection _previousSelection = new Selection();

        public event Action<LSAgent> OnCreateAgent;
        #endregion

        #region Event Behavior
        #endregion
        
        #region Public
        public void AddToSelection(LSAgent agent)
        {
            if (!agent.IsSelected)
            {
                SelectedAgents.Add(agent);
            }
        }

        public void RemoveFromSelection(LSAgent agent)
        {
            SelectedAgents.Remove(agent);
        }

        public Selection GetSelection(Command com)
        {
            if (!com.ContainsData<Selection>())
            {
                return _previousSelection;
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
            ushort[] arr2 = _previousSelection.selectedAgentLocalIDs.innerArray;

            if (!arr1.StructuralEquals(arr2))
            {
                _previousSelection = selection;
            }

            Influence influence = GetInfluencedAgent(com);

            BehaviourHelperManager.Execute(com);

            // check if command is an influence from AI
            if (influence.IsNotNull())
            {
                ushort influencedAgentID = influence.InfluencedAgentLocalID;
                if (LocalAgentActive[influencedAgentID])
                {
                    LSAgent agent = LocalAgents[influencedAgentID];
                    agent.Execute(com);
                }
            }
            else if (selection != null && selection.selectedAgentLocalIDs.Count > 0)
            {
                // otherwise it's an input command
                for (int i = 0; i < selection.selectedAgentLocalIDs.Count; i++)
                {
                    ushort selectedAgentID = selection.selectedAgentLocalIDs[i];
                    if (LocalAgentActive[selectedAgentID])
                    {
                        LSAgent agent = LocalAgents[selectedAgentID];
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

            ushort globalID = GlobalAgentController.GenerateGlobalID();
            GlobalAgentController.GlobalAgentActive[globalID] = true;
            GlobalAgentController.GlobalAgents[globalID] = agent;

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

            // remove the clone tag and replace with global ID
            agent.gameObject.name = agent.gameObject.name.Replace("(Clone)", "") + "_" + agent.GlobalID;

            agent.transform.parent = ControllingPlayer.LocalAgentContainer.AgentContainers.ContainsKey(agent.MyAgentType) ? ControllingPlayer.LocalAgentContainer.AgentContainers[agent.MyAgentType]
                : ControllingPlayer.LocalAgentContainer.transform;

            return agent;
        }

        /// <summary>
        /// Create an uninitialized RTSAgent
        /// </summary>
        /// <returns>The raw agent.</returns>
        /// <param name="agentCode">Agent code.</param>
        /// <param name="isBare">If set to <c>true</c> is bare.</param>
        public static LSAgent CreateRawAgent(string agentCode, Vector2d startPosition = default, Vector2d startRotation = default)
        {
            if (!GameResourceManager.IsValidAgentCode(agentCode))
            {
                throw new ArgumentException(string.Format("Agent code '{0}' not found.", agentCode));
            }
            FastStack<LSAgent> cache = GlobalAgentController.CachedAgents[agentCode];
            LSAgent curAgent;

            if (cache.IsNotNull() && cache.Count > 0)
            {
                curAgent = cache.Pop();
                ushort agentCodeID = GameResourceManager.GetAgentCodeIndex(agentCode);
                Debug.Log(curAgent.TypeIndex);
                GlobalAgentController.TypeAgentsActive[agentCodeID][curAgent.TypeIndex] = true;
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
            ushort agentCodeID = GameResourceManager.GetAgentCodeIndex(agent.MyAgentCode);
            if (!GlobalAgentController.TypeAgentsActive.TryGetValue(agentCodeID, out FastList<bool> typeActive))
            {
                typeActive = new FastList<bool>();
                GlobalAgentController.TypeAgentsActive.Add(agentCodeID, typeActive);
            }

            if (!GlobalAgentController.TypeAgents.TryGetValue(agentCodeID, out FastList<LSAgent> typeAgents))
            {
                typeAgents = new FastList<LSAgent>();
                GlobalAgentController.TypeAgents.Add(agentCodeID, typeAgents);
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
        public LSAgent CreateBareAgent(string agentCode, Vector2d startPosition = default, Vector2d startRotation = default)
        {
            var agent = CreateRawAgent(agentCode, startPosition, startRotation);
            return agent;
        }

        public void InitializeAgent(LSAgent agent, Vector2d position, Vector2d rotation)
        {
            AddAgent(agent);
            agent.Initialize(position, rotation);
        }

        public void SetAllegiance(LocalAgentController otherController, AllegianceType allegianceType)
        {
            while (_diplomacyFlags.Count <= otherController.ControllerID)
            {
                _diplomacyFlags.Add(AllegianceType.Neutral);
            }

            _diplomacyFlags[otherController.ControllerID] = allegianceType;
        }

        public AllegianceType GetAllegiance(LocalAgentController otherController)
        {
            return HasTeam && otherController.HasTeam ? MyTeam.GetAllegiance(otherController) : _diplomacyFlags[otherController.ControllerID];
        }

        public AllegianceType GetAllegiance(byte controllerID)
        {
            if (HasTeam)
            {
                //TODO: Team allegiance
            }

            return _diplomacyFlags[controllerID];
        }

        public static void SetFullHostile(LocalAgentController con)
        {
            //TODO: Make this hostile to new controllers
            for (int j = 0; j < GlobalAgentController.InstanceManagers.Count; j++)
            {
                if (j == con.ControllerID)
                {
                    continue;
                }

                LocalAgentController ac = GlobalAgentController.InstanceManagers[j];
                if (ac != con)
                {
                    con.SetAllegiance(ac, AllegianceType.Enemy);
                    ac.SetAllegiance(con, AllegianceType.Enemy);
                }
            }
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