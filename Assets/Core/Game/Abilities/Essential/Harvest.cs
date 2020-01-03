using Newtonsoft.Json;
using RTSLockstep.Agents;
using RTSLockstep.Determinism;
using RTSLockstep.Grouping;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Player.Commands;
using RTSLockstep.LSResources;
using UnityEngine;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;
using RTSLockstep.Integration;

namespace RTSLockstep.Abilities.Essential
{
    [DisallowMultipleComponent]
    public class Harvest : ActiveAbility
    {
        #region Properties
        public HarvestGroup MyHarvestGroup;
        [HideInInspector]
        public int MyHarvestGroupID;

        public bool IsHarvestMoving { get; private set; }
        [HideInInspector]
        public bool IsHarvesting;
        [HideInInspector]
        public bool IsEmptying;

        public RawMaterialType HarvestType { get; private set; }
        public LSAgent CurrentTarget { get; private set; }
        public LSAgent LastResourceTarget { get; private set; }
        public LSAgent LastStorageTarget { get; private set; }

        private const int _searchRate = LockstepManager.FrameRate / 2;
        private long _currentLoadAmount = 0;

        //Stuff for the logic
        private bool _inRange;
        private Vector2d _targetDirection;
        private long _fastMag;
        private long _fastRangeToTarget;

        private int _basePriority;
        private uint _targetVersion;
        private long _harvestCount;

        private int _loadedDepositId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long _windupCount;

        [HideInInspector]
        public AnimState HarvestingAnimState, MovingAnimState, IdlingAnimState;

        #region variables for quick fix for repathing to target's new position
        private const long _repathDistance = FixedMath.One * 2;
        private FrameTimer _repathTimer = new FrameTimer();
        private const int _repathInterval = LockstepManager.FrameRate * 2;
        private int _repathRandom = 0;
        #endregion

        #region Serialized Values (Further description in properties)
        public long CollectionAmount = FixedMath.One;
        public long DepositAmount = FixedMath.One;
        public long Capacity = FixedMath.One;
        [SerializeField, FixedNumber, Tooltip("Used to determine how fast agent can harvest.")]
        private long _harvestSpeed = 1 * FixedMath.One;
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
            _harvestCount = 0;

            IsHarvesting = false;
            IsEmptying = false;
            IsHarvestMoving = false;

            CurrentTarget = null;

            MyHarvestGroup = null;
            MyHarvestGroupID = -1;

            _inRange = false;
            IsFocused = false;

            if (Agent.MyStats.CanMove)
            {
                Agent.MyStats.CachedMove.OnArrive += HandleOnArrive;
            }

            _repathTimer.Reset(_repathInterval);
            _repathRandom = LSUtility.GetRandom(_repathInterval);

            if (Agent.GetControllingPlayer() && loadedSavedValues && _loadedDepositId >= 0)
            {
                LSAgent obj = Agent.GetControllingPlayer().GetObjectForId(_loadedDepositId);
                if (obj.MyAgentType == AgentType.Resource)
                {
                    CurrentTarget = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Harvester)
            {
                if (_harvestCount > _harvestSpeed)
                {
                    //reset attackCount overcharge if left idle
                    _harvestCount = _harvestSpeed;
                }
                else if (_harvestCount < _harvestSpeed)
                {
                    //charge up attack
                    _harvestCount += LockstepManager.DeltaTime;
                }

                if (Agent && Agent.IsActive)
                {
                    if ((IsFocused || IsHarvestMoving))
                    {
                        BehaveWithTarget();
                    }
                }

                if (Agent.MyStats.CanMove && IsHarvestMoving)
                {
                    Agent.MyStats.CachedMove.StartLookingForStopPause();
                }
            }
        }

        protected override void OnExecute(Command com)
        {
            Agent.StopCast(ID);
            IsCasting = true;
            RegisterHarvestGroup();
        }

        protected virtual void OnStartHarvestMove()
        {
            if (Agent.MyStats.CanMove
                && CurrentTarget.IsNotNull()
                && !CheckRange(CurrentTarget.Body))
            {
                IsHarvestMoving = true;
                IsFocused = false;

                Agent.MyStats.CachedMove.StartMove(Agent.MyStats.CachedMove.Destination, true);
            }
        }

        protected virtual void OnCollect()
        {
            long collect = CollectionAmount;
            // make sure that the harvester cannot collect more than it can carry
            if (_currentLoadAmount + collect > Capacity)
            {
                collect = Capacity - _currentLoadAmount;
            }

            if (!CurrentTarget.GetAbility<ResourceDeposit>().IsEmpty())
            {
                CurrentTarget.GetAbility<ResourceDeposit>().Remove(collect);
            }

            _currentLoadAmount += collect;

            if (LoadAtCapacity())
            {
                IsHarvesting = false;
                IsEmptying = true;

                // The harvest AI will determine what to do next
                LastResourceTarget = CurrentTarget;
                StopHarvest();
            }
        }

        protected virtual void OnDeposit()
        {
            long deposit = DepositAmount;

            if (deposit > _currentLoadAmount)
            {
                deposit = _currentLoadAmount;
            }

            _currentLoadAmount -= deposit;

            RawMaterialType depositType = HarvestType;
            Agent.Controller.ControllingPlayer.PlayerRawMaterialManager.AddRawMaterial(depositType, deposit);

            if (_currentLoadAmount <= 0)
            {
                IsHarvesting = true;
                IsEmptying = false;

                // The harvest AI will determine what to do next
                LastStorageTarget = CurrentTarget;
                StopHarvest();
            }
        }

        protected override void OnDeactivate()
        {
            StopHarvest(true);
        }

        protected sealed override void OnStopCast()
        {
            if (IsHarvesting || IsEmptying)
            {
                StopHarvest(true);
            }
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "Harvesting", IsHarvesting);
            SaveManager.WriteBoolean(writer, "Emptying", IsEmptying);
            SaveManager.WriteFloat(writer, "CurrentLoad", _currentLoadAmount);
            SaveManager.WriteBoolean(writer, "HarvestMoving", IsHarvestMoving);
            SaveManager.WriteString(writer, "HarvestType", HarvestType.ToString());
            if (CurrentTarget)
            {
                SaveManager.WriteInt(writer, "ResourceDepositId", CurrentTarget.GlobalID);
            }
            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", _inRange);
            SaveManager.WriteLong(writer, "HarvestCount", _harvestCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", _fastRangeToTarget);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "Harvesting":
                    IsHarvesting = (bool)readValue;
                    break;
                case "HarvestMoving":
                    IsHarvestMoving = (bool)readValue;
                    break;
                case "Emptying":
                    IsEmptying = (bool)readValue;
                    break;
                case "CurrentLoad":
                    _currentLoadAmount = (long)readValue;
                    break;
                case "HarvestType":
                    HarvestType = WorkManager.GetResourceType((string)readValue);
                    break;
                case "ResourceDepositId":
                    _loadedDepositId = (int)(long)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    _inRange = (bool)readValue;
                    break;
                case "HarvestCount":
                    _harvestCount = (long)readValue;
                    break;
                case "FastRangeToTarget":
                    _fastRangeToTarget = (long)readValue;
                    break;
                default:
                    break;
            }
        }

        public void OnHarvestGroupProcessed(LSAgent currentTarget)
        {
            Agent.Tag = AgentTag.Harvester;

            if (currentTarget.IsNotNull())
            {
                CurrentTarget = currentTarget;

                IsFocused = true;
                IsHarvestMoving = false;

                if (IsHarvesting)
                {
                    RawMaterialType resourceType = CurrentTarget.GetAbility<ResourceDeposit>().ResourceType;

                    // we can only collect one resource at a time, other resources are lost
                    if (resourceType != HarvestType)
                    {
                        HarvestType = resourceType;
                        _currentLoadAmount = 0;
                    }

                    SetHarvestAnimState();
                }

                _targetVersion = currentTarget.SpawnVersion;

                OnStartHarvestMove();
            }
            else
            {
                StopHarvest();
            }
        }

        public long GetCurrentLoad()
        {
            return _currentLoadAmount;
        }

        public bool LoadAtCapacity()
        {
            if (_currentLoadAmount >= Capacity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RegisterHarvestGroup()
        {
            if (HarvestGroupHelper.CheckValidAndAlert())
            {
                HarvestGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        private void HandleOnArrive()
        {
            if (IsHarvestMoving)
            {
                IsFocused = true;
                IsHarvestMoving = false;
            }
        }

        private void BehaveWithTarget()
        {
            if (!CurrentTarget.IsActive
                || CurrentTarget.SpawnVersion != _targetVersion
                || IsHarvesting
                && CurrentTarget.GetAbility<ResourceDeposit>()
                && CurrentTarget.GetAbility<ResourceDeposit>().IsEmpty())
            {
                //  Target's lifecycle has ended
                StopHarvest();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange(CurrentTarget.Body))
                    {
                        if (!_inRange)
                        {
                            if (Agent.MyStats.CanMove)
                            {
                                Agent.MyStats.CachedMove.Arrive();
                            }

                            _inRange = true;
                        }
                        Agent.Animator.SetState(IsHarvesting ? HarvestingAnimState : IdlingAnimState);
                        //if (audioElement != null && Time.timeScale > 0)
                        //{
                        //    audioElement.Play(emptyHarvestSound);
                        //}

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
                        else if (_harvestCount >= _harvestSpeed)
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
                                StopHarvest();
                            }
                            else
                            {
                                needsRepath = true;
                            }

                            Agent.Body.Priority = _basePriority;
                        }
                        else if (!_inRange && _repathTimer.AdvanceFrame())
                        {
                            if (CurrentTarget.Body.PositionChangedBuffer &&
                                CurrentTarget.Body.Position.FastDistance(Agent.MyStats.CachedMove.Destination.x, Agent.MyStats.CachedMove.Destination.y) >= (_repathDistance * _repathDistance))
                            {
                                needsRepath = true;
                                //So units don't sync up and path on the same frame
                                _repathTimer.AdvanceFrames(_repathRandom);
                            }
                        }

                        if (needsRepath)
                        {
                            Agent.MyStats.CachedMove.Destination = CurrentTarget.Body.Position;
                            Agent.MyStats.CachedMove.PauseAutoStop();
                            Agent.MyStats.CachedMove.PauseCollisionStop();
                            OnStartHarvestMove();
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
                        Vector2d targetVector = CurrentTarget.Body.Position - Agent.Body.Position;
                        Agent.MyStats.CachedTurn.StartTurnVector(targetVector);
                    }

                    if (_windupCount >= _windup)
                    {
                        _windupCount = 0;
                        // start action
                        StartHarvest();

                        while (_harvestCount >= _harvestSpeed)
                        {
                            //resetting back down after attack is fired
                            _harvestCount -= _harvestSpeed;
                        }
                        _harvestCount += _windup;
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

        private bool CheckRange(LSBody targetBody)
        {
            _fastRangeToTarget = Agent.MyStats.ActionRange + (targetBody.IsNotNull() ? targetBody.Radius : 0) + Agent.Body.Radius;
            _fastRangeToTarget *= _fastRangeToTarget;

            _targetDirection = targetBody.Position - Agent.Body.Position;
            _fastMag = _targetDirection.FastMagnitude();

            return _fastMag <= _fastRangeToTarget;
        }

        private void StartWindup()
        {
            _windupCount = 0;
            IsWindingUp = true;
        }

        private void StartHarvest()
        {
            if (Agent.MyStats.CanMove)
            {
                // we don't want to be able to construct and move!
                IsHarvestMoving = false;
                Agent.MyStats.CachedMove.StopMove();
            }
            Agent.Body.Priority = _increasePriority ? _basePriority + 1 : _basePriority;

            if (IsHarvesting)
            {
                OnCollect();
            }
            else if (IsEmptying)
            {
                OnDeposit();
            }
        }

        private void SetHarvestAnimState()
        {
            switch (HarvestType)
            {
                case RawMaterialType.Wood:
                    HarvestingAnimState = AnimState.EngagingWood;
                    MovingAnimState = AnimState.MovingWood;
                    IdlingAnimState = AnimState.IdlingWood;
                    break;
                default:
                    HarvestingAnimState = AnimState.EngagingOre;
                    MovingAnimState = AnimState.MovingOre;
                    IdlingAnimState = AnimState.IdlingOre;
                    break;
            }
        }

        // send complete command to stop harvesting cycle, i.e. a move command issued by player
        private void StopHarvest(bool complete = false)
        {
            _inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyHarvestGroup.IsNotNull())
            {
                MyHarvestGroup.Remove(this);
            }

            IsHarvestMoving = false;

            if (complete)
            {
                IsHarvesting = false;
                IsEmptying = false;
                Agent.Tag = AgentTag.None;
            }
            else if (CurrentTarget.IsNotNull())
            {
                if (Agent.MyStats.CanMove && !_inRange)
                {
                    Agent.MyStats.CachedMove.StopMove();
                }
            }

            CurrentTarget = null;

            IsCasting = false;

            Agent.Body.Priority = _basePriority;
        }
    }
}