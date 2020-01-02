using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using RotaryHeart.Lib.SerializableDictionary;

using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Agents.Teams;
using RTSLockstep.Buffs;
using RTSLockstep.Data;
using RTSLockstep.Determinism;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Managers;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.Influence;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;
using RTSLockstep.Integration;

using Coroutine = RTSLockstep.Utility.Coroutine;

namespace RTSLockstep.Agents
{
    [Serializable]
    public class ResourceCost : SerializableDictionaryBase<ResourceType, int> { };

    [RequireComponent(typeof(UnityLSBody))]
    /// <summary>
    /// LSAgents manage abilities and interpret commands.
    /// </summary>
    public class LSAgent : MonoBehaviour
    {
        #region Properties
        [SerializeField, FrameCount]
        private int _deathTime = LockstepManager.FrameRate * 2;
        [SerializeField]
        private AgentType _agentType;
        [SerializeField]
        private int _globalID;
        [SerializeField]
        private int _boxPriority = 0;
        [SerializeField]
        private int _selectionPriority = 0;
        [SerializeField]
        private bool _selectable = true;
        public AgentTag Tag;
        [SerializeField]
        private float _selectionRadius = -1f;
        public float SelectionRadius { get { return _selectionRadius <= 0 ? Body.Radius.ToFloat() + 1f : _selectionRadius; } }
        [SerializeField]
        private Transform _visualCenter;
        public Transform VisualCenter { get { return _visualCenter; } }
        public string objectName;
        [HideInInspector]
        public string AgentDescription;
        public Texture2D destroyImage;
        [SerializeField]
        public ResourceCost resourceCost = new ResourceCost
        {
            {ResourceType.Gold, 0 },
            {ResourceType.Ore, 0 },
            {ResourceType.Stone, 0 },
            {ResourceType.Wood, 0 },
            {ResourceType.Food, 0 },
            {ResourceType.Crystal, 0 },
            {ResourceType.Provision, 0 }
        };

        public string MyAgentCode { get; private set; }
        public AgentType MyAgentType { get { return _agentType; } private set { _agentType = value; } }
        public ushort GlobalID { get { return (ushort)_globalID; } protected set { _globalID = value; } }
        public ushort LocalID { get; protected set; }
        public uint BoxVersion { get; set; }
        public int BoxPriority { get { return _boxPriority; } }
        public int SelectionPriority { get { return _selectionPriority; } }
        public bool Selectable { get { return _selectable; } }
        public bool CanSelect { get { return Selectable && IsVisible; } }

        public int ReferenceIndex { get; set; }
        public Vector2 Position2 { get { return new Vector2(CachedTransform.position.x, CachedTransform.position.z); } }
        public FastList<AbilityDataItem> Interfacers { get { return abilityManager.Interfacers; } }

        private static FastList<Ability> _setupAbilitys = new FastList<Ability>();
        //private LSBusStop _busStop;
        //public LSBusStop BusStop { get { return _busStop ?? (_busStop = new LSBusStop()); }}
        /// <summary>
        /// The index of this agent in the pool.
        /// </summary>
        /// <value>The index of the type.</value>
        private ushort _typeIndex;
        public ushort TypeIndex { get { return _typeIndex; } set { _typeIndex = value; _typeIndex = LocalAgentController.UnregisterdTypeIndex; } }

        #region Pre-runtime generated (maybe not)
        public Ability[] AttachedAbilities { get; private set; }
        public UnityLSBody UnityBody { get; private set; }
        public LSBody Body { get; set; }
        public LSAnimatorBase Animator { get; private set; }
        public Transform CachedTransform { get; private set; }
        public GameObject CachedGameObject { get; private set; }
        public AgentStats MyStats { get; private set; }
        #endregion

        //TODO: Put all this stuff in an extendible class
        public bool IsActive { get; private set; }
        public bool IsLive { get; private set; }
        public event Action<LSAgent> OnDeactivate;

        public bool CheckCasting { get; set; }
        public bool IsCasting
        {
            get
            {
                if (Stunned)
                {
                    return true;
                }

                return abilityManager.CheckCasting();
            }
        }

        public bool CheckFocus { get; set; }
        public bool IsFocused
        {
            get
            {
                if (Stunned)
                {
                    return false;
                }

                return abilityManager.CheckFocus();
            }
        }

        private bool _stunned;
        public bool Stunned
        {
            get
            {
                return _stunned;
            }
            set
            {
                if (value != _stunned)
                {
                    _stunned = value;
                    if (_stunned)
                    {
                        StopCast();
                    }
                }
            }
        }

        public event Action OnSelectedChange;
        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    if (OnSelectedChange != null && IsActive)
                        OnSelectedChange();
                }
            }
        }

        public event Action OnHighlightedChange;
        private bool isHighlighted;
        public bool IsHighlighted
        {
            get { return isHighlighted; }
            set
            {
                if (IsHighlighted != value)
                {
                    isHighlighted = value;
                    if (OnHighlightedChange != null && IsActive)
                    {
                        OnHighlightedChange();
                    }
                }
            }
        }

        public bool IsVisible
        {
            //get { return cachedRenderer == null || (cachedRenderer.enabled && cachedRenderer.isVisible); }
            //  get { return true; } //TODO: Return true only if viable GladFox: seen for what kind of camera? :)
            get
            {
                Vector3 screenPoint = PlayerInputHelper.GUIManager.MainCam.WorldToViewportPoint(Body.VisualPosition);
                if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public LSInfluencer Influencer { get; private set; }
        public uint SpawnVersion { get; private set; }
        public LocalAgentController Controller { get; private set; }
        public bool Controllable { get { return GlobalAgentController.ContainsController(Controller); } }
        public AllegianceType GetAllegiance(LSAgent other) { return Controller.GetAllegiance(other.Controller); }
        public readonly AbilityManager abilityManager = new AbilityManager();
        public FastBucket<Buff> Buffs = new FastBucket<Buff>();
        public float SelectionRadiusSquared { get; private set; }
        public IAgentData Data { get; private set; }

        private readonly FastList<int> TrackedLockstepTickets = new FastList<int>();

        public bool VisualPositionChanged { get; private set; }
        private Vector3 lastVisualPosition;

        int deathingIndex;
        public Coroutine poolCoroutine;

        public bool _provisioned { get; private set; }
        private Rect _selectionArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
        private bool loadedSavedValues = false;
        private LSPlayer _cachedPlayer;
        #endregion

        #region CallEvents
        public virtual void Setup(IAgentData interfacer)
        {
            gameObject.SetActive(true);
            LoadComponents();

            DontDestroyOnLoad(gameObject);

            _setupAbilitys.FastClear();

            MyAgentCode = interfacer.Name;
            AgentDescription = interfacer.GetAgentDescription();
            Data = interfacer;
            SpawnVersion = 1;
            CheckCasting = true;

            if (_visualCenter == null)
            {
                _visualCenter = CachedTransform;
            }

            if (Animator.IsNotNull())
            {
                Animator.Setup();
            }

            Body = UnityBody.InternalBody;
            Body.Setup(this);
            abilityManager.Setup(this);

            Influencer = new LSInfluencer();
            Influencer.Setup(this);

            SelectionRadiusSquared = SelectionRadius * SelectionRadius;

            RegisterLockstep();
        }

        public virtual void Initialize(Vector2d position = default, Vector2d rotation = default)
        {
            IsActive = true;
            IsLive = true;
            CheckCasting = true;

            // place game object under it's agent commander
            CachedGameObject.transform.parent = Controller.ControllingPlayer.AgentContainer;

            CachedGameObject.SetActive(true);

            if (Body.IsNotNull())
            {
                Body.Initialize(position.ToVector3d(), rotation);
            }

            if (Influencer.IsNotNull())
            {
                Influencer.Initialize();
            }

            abilityManager.Initialize();

            if (Animator.IsNotNull())
            {
                Animator.Initialize();
            }

            SetControllingPlayer(Controller.ControllingPlayer);

            if (_cachedPlayer)
            {
                if (!loadedSavedValues)
                {
                    SetTeamColor();
                }
            }
        }

        public virtual void Simulate()
        {
            if (!_provisioned)
            {
                _provisioned = true;
                _cachedPlayer.PlayerResourceManager.AddResource(ResourceType.Provision, resourceCost[ResourceType.Provision]);
            }

            if (Influencer.IsNotNull())
            {
                Influencer.Simulate();
            }

            abilityManager.Simulate();

            if (Animator.IsNotNull() && IsCasting == false)
            {
                Animator.SetIdleState();
            }
        }

        public void LateSimulate()
        {
            abilityManager.LateSimulate();
            for (int i = 0; i < Buffs.PeakCount; i++)
            {
                if (Buffs.arrayAllocation[i])
                {
                    Buffs[i].Simulate();
                }
            }
        }

        public virtual void Visualize()
        {
            VisualPositionChanged = CachedTransform.hasChanged && lastVisualPosition != (lastVisualPosition = CachedTransform.position);
            if (VisualPositionChanged)
            {
                lastVisualPosition = CachedTransform.position;
            }

            abilityManager.Visualize();
        }

        public void LateVisualize()
        {
            abilityManager.LateVisualize();
            if (Animator.IsNotNull())
            {
                Animator.Visualize();
            }
        }
        #endregion

        #region Public
        public IEnumerable<LSVariable> GetDesyncs(int[] compare)
        {
            int position = 0;
            foreach (int ticket in TrackedLockstepTickets)
            {
                LSVariableContainer container = LSVariableManager.GetContainer(ticket);
                int[] hashes = container.GetCompareHashes();
                for (int i = 0; i < hashes.Length; i++)
                {
                    if (compare[i] != hashes[position])
                    {
                        yield return container.Variables[i];
                    }
                    position++;
                }
            }
        }

        public void SessionReset()
        {
            BoxVersion = 0;
            SpawnVersion = 1;
        }

        public void InitializeController(LocalAgentController controller, ushort localID, ushort globalID)
        {
            Controller = controller;
            LocalID = localID;
            GlobalID = globalID;
        }

        //Initialize this agent with basic functions and Ability system
        public void InitializeBare()
        {
            IsActive = true;
            IsLive = true;
            abilityManager.Initialize();
        }

        public void Execute(Command com)
        {
            abilityManager.Execute(com);
        }

        public void StopCast(int exceptionID = -1)
        {
            abilityManager.StopCast(exceptionID);
        }

        public void Die(bool immediate = false)
        {
            GlobalAgentController.DestroyAgent(this, immediate);
            if (Animator.IsNotNull())
            {
                Animator.SetDyingState();
                Animator.Visualize(); // TODO: Now call in LockstepManager.LateVisualize ()
            }
        }

        /// <summary>
        /// Do not call this to destroy the agent. Use AgentController.DestroyAgent().
        /// </summary>
        /// <param name="Immediate"></param>
        internal void _Deactivate(bool Immediate = false)
        {
            if (IsActive == false)
            {
                Debug.Log("NOASER");
            }

            OnDeactivate?.Invoke(this);

            DeactivateCore();

            if (!Immediate)
            {
                if (Animator.IsNotNull())
                {
                    Animator.SetDyingState();
                }

                poolCoroutine = CoroutineManager.StartCoroutine(PoolDelayer());
            }
            else
            {
                GlobalAgentController.EndLife(this);
            }
        }

        private void DeactivateCore()
        {
            StopCast();

            IsSelected = false;

            abilityManager.Deactivate();

            Body.Deactivate();
            if (Influencer.IsNotNull())
            {
                Influencer.Deactivate();
            }

            SpawnVersion++;
            IsActive = false;
        }

        private IEnumerator<int> PoolDelayer()
        {
            deathingIndex = GlobalAgentController.DeathingAgents.Add(this);


            yield return _deathTime;
            GlobalAgentController.DeathingAgents.RemoveAt(deathingIndex);

            GlobalAgentController.EndLife(this);
        }

        internal void _EndLife()
        {
            IsLive = false;
            for (var k = 0; k < abilityManager.Abilities.Length; k++)
            {
                abilityManager.Abilities[k].EndLife();
            }
        }

        public void ApplyImpulse(AnimImpulse animImpulse, int rate = 0)
        {
            if (Animator.IsNotNull())
            {
                Animator.ApplyImpulse(animImpulse, rate);
            }
        }

        public T GetAbility<T>() where T : Ability
        {
            return abilityManager.GetAbility<T>();
        }

        public Ability GetAbility(string name)
        {
            return abilityManager.GetAbility(name);
        }

        public long GetStateHash()
        {
            long hash = 3;
            hash ^= GlobalID;
            hash ^= LocalID;
            hash ^= Body._position.GetStateHash();
            hash ^= Body._rotation.GetHashCode();
            hash ^= Body.Velocity.GetStateHash();
            return hash;
        }

        public void AddBuff(Buff buff)
        {
            buff.ID = Buffs.Add(buff);
        }

        public void RemoveBuff(Buff buff)
        {
            Buffs.RemoveAt(buff.ID);
        }

        public void DeactivateBuff(Buff buff)
        {
            buff.Deactivate();
        }

        public bool ContainsBuff<T>()
        {
            for (int i = 0; i < Buffs.PeakCount; i++)
            {
                if (Buffs.arrayAllocation[i])
                {
                    if (Buffs[i] is T)
                        return true;
                }
            }
            return false;
        }

        public bool IsOwnedBy(LocalAgentController owner)
        {
            if (Controller.IsNotNull() && Controller.Equals(owner))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetControllingPlayer(LSPlayer player)
        {
            _cachedPlayer = player;
        }

        public LSPlayer GetControllingPlayer()
        {
            return _cachedPlayer;
        }

        public void SetTeamColor()
        {
            TeamColor[] teamColors = GetComponentsInChildren<TeamColor>();
            foreach (TeamColor teamColor in teamColors)
            {
                teamColor.GetComponent<Renderer>().material.color = _cachedPlayer.TeamColor;
            }
        }

        public void SetPlayingArea(Rect value)
        {
            _selectionArea = value;
        }

        public Rect GetPlayerArea()
        {
            return _selectionArea;
        }

        public void SetProvision(bool value)
        {
            _provisioned = value;
        }

        public void SaveDetails(JsonWriter writer)
        {
            SaveManager.WriteString(writer, "Type", objectName);
            SaveManager.WriteInt(writer, "GlobalID", GlobalID);
            SaveManager.WriteInt(writer, "LocalID", LocalID);
            SaveManager.WriteVector2d(writer, "Position", Body.Position);
            SaveManager.WriteVector2d(writer, "Rotation", Body.Rotation);
            SaveManager.WriteVector(writer, "Scale", transform.localScale);
        }

        public void LoadDetails(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string propertyName = (string)reader.Value;
                        reader.Read();
                        HandleLoadedProperty(reader, propertyName, reader.Value);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    loadedSavedValues = true;
                    return;
                }
            }
            loadedSavedValues = true;
        }
        #endregion

        #region Private
        private void RegisterLockstep()
        {
            TrackedLockstepTickets.Add(LSVariableManager.Register(Body));
            foreach (Ability abil in abilityManager.Abilities)
            {
                TrackedLockstepTickets.Add(LSVariableManager.Register(abil));
            }
        }

        private void LoadComponents()
        {
            CachedTransform = base.transform;
            CachedGameObject = base.gameObject;
            UnityBody = GetComponent<UnityLSBody>();
            Animator = GetComponent<LSAnimatorBase>();
            AttachedAbilities = GetComponents<Ability>();
            MyStats = GetComponent<AgentStats>();
        }

        private void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            switch (propertyName)
            {
                case "Type":
                    objectName = (string)readValue;
                    break;
                case "GlobalID":
                    GlobalID = (ushort)readValue;
                    break;
                case "LocalID":
                    LocalID = (ushort)readValue;
                    break;
                case "Position":
                    Body.Position = LoadManager.LoadVector2d(reader);
                    break;
                case "Rotation":
                    Body.Rotation = LoadManager.LoadVector2d(reader);
                    break;
                case "Scale":
                    transform.localScale = LoadManager.LoadVector(reader);
                    break;
                default:
                    break;
            }
        }
        #endregion

#if UNITY_EDITOR
        void Reset()
        {
            _selectionRadius = -1f;
            _visualCenter = transform;
        }
#endif
    }
}