using Newtonsoft.Json;
using RTSLockstep.Grid;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [DisallowMultipleComponent]
    public class Construct : ActiveAbility
    {
        #region Properties
        public ConstructGroup MyConstructGroup;
        [HideInInspector]
        public int MyConstructGroupID;

        public bool IsBuildMoving { get; private set; }

        public string[] BuildActions
        {
            get { return _buildActions; }
        }

        //Called whenever construction is stopped... i.e. to attack
        public event Action OnStopConstruct;

        private RTSAgent _currentProject;
        private Structure _projectStructure
        {
            get
            {
                return _currentProject.IsNotNull() ? _currentProject.GetAbility<Structure>() : null;
            }
        }

        private const int _searchRate = LockstepManager.FrameRate / 2;

        //Stuff for the logic
        private bool _inRange;
        private Vector2d _targetDirection;
        private long _fastMag;
        private long _fastRangeToTarget;

        private int _basePriority;
        private uint _targetVersion;
        private long _constructCount;

        private int _loadedProjectId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long _windupCount;

        protected virtual AnimState ConstructingAnimState
        {
            get { return AnimState.Constructing; }
        }

        #region variables for quick fix for repathing to target's new position
        private const long _repathDistance = FixedMath.One * 2;
        private FrameTimer _repathTimer = new FrameTimer();
        private const int _repathInterval = LockstepManager.FrameRate * 2;
        private int _repathRandom = 0;
        #endregion

        #region Serialized Values (Further description in properties)
        [SerializeField, FixedNumber]
        private long _constructAmount = FixedMath.One;
        [SerializeField, FixedNumber, Tooltip("Used to determine how fast agent can build.")]
        private long _constructionSpeed = 1 * FixedMath.One;
        [SerializeField, Tooltip("Enter object names for prefabs this agent can build.")]
        private string[] _buildActions;
        [SerializeField, FixedNumber]
        private long _windup = 0;
        [SerializeField]
        private bool _increasePriority = true;
        #endregion
        #endregion Properties

        protected override void OnSetup()
        {
            _basePriority = Agent.Body.Priority;
        }

        protected override void OnInitialize()
        {
            _constructCount = 0;

            IsBuildMoving = false;

            MyConstructGroup = null;
            MyConstructGroupID = -1;

            _currentProject = null;

            _inRange = false;
            IsFocused = false;

            if (Agent.MyStats.CanMove)
            {
                Agent.MyStats.CachedMove.OnArrive += HandleOnArrive;
            }

            _repathTimer.Reset(_repathInterval);
            _repathRandom = LSUtility.GetRandom(_repathInterval);

            // need to move this to a construct group
            if (Agent.GetCommander() && loadedSavedValues && _loadedProjectId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(_loadedProjectId);
                if (obj.MyAgentType == AgentType.Structure)
                {
                    _currentProject = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                if (_constructCount > _constructionSpeed)
                {
                    //reset constructCount overcharge if left idle
                    _constructCount = _constructionSpeed;
                }
                else if (_constructCount < _constructionSpeed)
                {
                    //charge up constructCount
                    _constructCount += LockstepManager.DeltaTime;
                }

                if (Agent && Agent.IsActive)
                {
                    if ((IsFocused || IsBuildMoving))
                    {
                        BehaveWithTarget();
                    }
                }

                if (Agent.MyStats.CanMove && IsBuildMoving)
                {
                    Agent.MyStats.CachedMove.StartLookingForStopPause();
                }
            }
        }

        protected override void OnExecute(Command com)
        {
            Agent.StopCast(ID);
            IsCasting = true;
            RegisterConstructGroup();
        }

        protected virtual void OnStartConstructMove()
        {
            if (Agent.MyStats.CanMove
                && _projectStructure.IsNotNull()
                && !CheckRange())
            {
                IsBuildMoving = true;
                IsFocused = false;

                Agent.MyStats.CachedMove.StartMove(Agent.MyStats.CachedMove.Destination, true);
            }
        }

        protected virtual void OnConstruct(Structure target)
        {
            if (target.NeedsConstruction)
            {
                target.BuildUp(_constructAmount);

                //if (audioElement != null)
                //{
                //    audioElement.Play(finishedJobSound);
                //}
            }
            else
            {
                // what are we building for then?
                StopConstruct();
            }
        }

        protected override void OnDeactivate()
        {
            StopConstruct(true);
        }

        protected sealed override void OnStopCast()
        {
            StopConstruct(true);
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "BuildMoving", IsBuildMoving);
            if (_projectStructure)
            {
                SaveManager.WriteInt(writer, "currentProjectId", _currentProject.GlobalID);
            }
            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", _inRange);
            SaveManager.WriteLong(writer, "ConstructCount", _constructCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", _fastRangeToTarget);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "BuildMoving":
                    IsBuildMoving = (bool)readValue;
                    break;
                case "currentProjectId":
                    _loadedProjectId = (int)(long)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    _inRange = (bool)readValue;
                    break;
                case "ConstructCount":
                    _constructCount = (long)readValue;
                    break;
                case "FastRangeToTarget":
                    _fastRangeToTarget = (long)readValue;
                    break;
                default:
                    break;
            }
        }

        public void OnConstructGroupProcessed(RTSAgent currentTarget)
        {
            Agent.Tag = AgentTag.Builder;

            if (currentTarget.IsNotNull())
            {
                _currentProject = currentTarget;

                IsFocused = true;
                IsBuildMoving = false;

                _targetVersion = _currentProject.SpawnVersion;

                _fastRangeToTarget = Agent.MyStats.ActionRange + (_currentProject.Body.IsNotNull() ? _currentProject.Body.Radius : 0) + Agent.Body.Radius;
                _fastRangeToTarget *= _fastRangeToTarget;

                OnStartConstructMove();
            }
            else
            {
                StopConstruct();
            }
        }

        private void RegisterConstructGroup()
        {
            if (ConstructionGroupHelper.CheckValidAndAlert())
            {
                ConstructionGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        private void HandleOnArrive()
        {
            if (IsBuildMoving)
            {
                IsFocused = true;
                IsBuildMoving = false;
            }
        }

        private void BehaveWithTarget()
        {
            // only stop construct when groups queue is empty
            if (!_currentProject.IsActive
                || _currentProject.SpawnVersion != _targetVersion
                || !_projectStructure.NeedsConstruction)
            {
                // Target's lifecycle has ended
                StopConstruct();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        if (!_inRange)
                        {
                            if (Agent.MyStats.CanMove)
                            {
                                Agent.MyStats.CachedMove.Arrive();
                            }

                            _inRange = true;
                        }
                        Agent.Animator.SetState(ConstructingAnimState);

                        if (!_projectStructure.ConstructionStarted)
                        {
                            _projectStructure.ConstructionStarted = true;

                            if (_currentProject.Animator.IsNotNull())
                            {
                                _currentProject.Animator.SetState(AnimState.Building);
                            }

                            // Restore material
                            ConstructionHandler.RestoreMaterial(_currentProject.gameObject);

                            // restore bounds so structure is included in path & build grid
                            if (_currentProject.GetAbility<DynamicBlocker>())
                            {
                                _currentProject.GetAbility<DynamicBlocker>().SetTransparent(false);
                            }
                        }

                        _targetDirection.Normalize(out long mag);
                        bool withinTurn = Agent.MyStats.CachedAttack.TrackAttackAngle == false ||
                                          (_fastMag != 0 &&
                                          Agent.Body.Forward.Dot(_targetDirection.x, _targetDirection.y) > 0
                                          && Agent.Body.Forward.Cross(_targetDirection.x, _targetDirection.y).Abs() <= Agent.MyStats.CachedAttack.AttackAngle);

                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && Agent.MyStats.CanTurn)
                        {
                            Agent.MyStats.CachedTurn.StartTurnDirection(_targetDirection);
                        }
                        else if (_constructCount >= _constructionSpeed)
                        {
                            StartWindup();
                        }
                    }
                    else if (Agent.MyStats.CanMove)
                    {
                        bool needsRepath = false;
                        if (!Agent.MyStats.CachedMove.IsMoving
                            && !Agent.MyStats.CachedMove.MoveOnGroupProcessed)
                        {
                            if (Agent.MyStats.CachedMove.IsStuck)
                            {
                                StopConstruct();
                            }
                            else
                            {
                                needsRepath = true;
                            }

                            Agent.Body.Priority = _basePriority;
                        }
                        else if (!_inRange && _repathTimer.AdvanceFrame())
                        {
                            if (_currentProject.Body.PositionChangedBuffer &&
                                _currentProject.Body.Position.FastDistance(Agent.MyStats.CachedMove.Destination.x, Agent.MyStats.CachedMove.Destination.y) >= (_repathDistance * _repathDistance))
                            {
                                needsRepath = true;
                                //So units don't sync up and path on the same frame
                                _repathTimer.AdvanceFrames(_repathRandom);
                            }
                        }

                        if (needsRepath)
                        {
                            Agent.MyStats.CachedMove.Destination = _currentProject.Body.Position;
                            Agent.MyStats.CachedMove.PauseAutoStop();
                            Agent.MyStats.CachedMove.PauseCollisionStop();
                            OnStartConstructMove();
                        }
                    }

                    if (_inRange)
                    {
                        _inRange = false;
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    _windupCount += LockstepManager.DeltaTime;
                    if (Agent.MyStats.CanTurn)
                    {
                        Vector2d targetVector = _currentProject.Body.Position - Agent.Body.Position;
                        Agent.MyStats.CachedTurn.StartTurnVector(targetVector);
                    }

                    if (_windupCount >= _windup)
                    {
                        _windupCount = 0;
                        StartConstruction();
                        while (_constructCount >= _constructionSpeed)
                        {
                            //resetting back down after attack is fired
                            _constructCount -= _constructionSpeed;
                        }
                        _constructCount += _windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    _windupCount = 0;
                }

                if (Agent.MyStats.CanMove && _inRange)
                {
                    Agent.MyStats.CachedMove.PauseAutoStop();
                    Agent.MyStats.CachedMove.PauseCollisionStop();
                }
            }
        }

        private bool CheckRange()
        {
            _targetDirection = _currentProject.Body.Position - Agent.Body.Position;
            _fastMag = _targetDirection.FastMagnitude();

            return _fastMag <= _fastRangeToTarget;
        }

        private void StartWindup()
        {
            _windupCount = 0;
            IsWindingUp = true;
        }

        private void StartConstruction()
        {
            if (Agent.MyStats.CanMove)
            {
                // we don't want to be able to construct and move!
                IsBuildMoving = false;
                Agent.MyStats.CachedMove.StopMove();
            }
            Agent.Body.Priority = _increasePriority ? _basePriority + 1 : _basePriority;

            OnConstruct(_projectStructure);
        }

        private void StopConstruct(bool complete = false)
        {
            _inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyConstructGroup.IsNotNull()
                && MyConstructGroup.GroupConstructionQueue.Count == 0)
            {
                // Only remove from construction group if their queue is empty
                MyConstructGroup.Remove(this);
            }

            IsBuildMoving = false;

            if (complete)
            {
                Agent.Tag = AgentTag.None;
            }
            else if (_currentProject.IsNotNull())
            {
                if (Agent.MyStats.CanMove && !_inRange)
                {
                    Agent.MyStats.CachedMove.StopMove();
                }
            }

            _currentProject = null;

            IsCasting = false;

            Agent.Body.Priority = _basePriority;

            OnStopConstruct?.Invoke();
        }
    }
}