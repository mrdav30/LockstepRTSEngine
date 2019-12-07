using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
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

        public ResourceType HarvestType { get; private set; }
        public RTSAgent CurrentResourceTarget { get; private set; }
        public ResourceDeposit TargetResource
        {
            get
            {
                return CurrentResourceTarget.IsNotNull() ? CurrentResourceTarget.GetAbility<ResourceDeposit>() : null;
            }
            private set { }
        }
        public RTSAgent CurrentStorageTarget { get; private set; }

        private const int searchRate = LockstepManager.FrameRate / 2;
        private long currentLoadAmount = 0;

        //Stuff for the logic
        private bool inRange;
        private Vector2d targetDirection;
        private long fastMag;
        private long fastRangeToTarget;

        private int basePriority;
        private uint targetVersion;
        private long harvestCount;

        private int loadedDepositId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long windupCount;

        [HideInInspector]
        public AnimState HarvestingAnimState, MovingAnimState, IdlingAnimState;

        #region variables for quick fix for repathing to target's new position
        private const long repathDistance = FixedMath.One * 2;
        private FrameTimer repathTimer = new FrameTimer();
        private const int repathInterval = LockstepManager.FrameRate * 2;
        private int repathRandom = 0;
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
            basePriority = Agent.Body.Priority;
        }

        protected override void OnInitialize()
        {
            harvestCount = 0;

            IsHarvesting = false;
            IsEmptying = false;
            IsHarvestMoving = false;

            CurrentResourceTarget = null;
            CurrentStorageTarget = null;

            MyHarvestGroup = null;
            MyHarvestGroupID = -1;

            inRange = false;
            IsFocused = false;

            if (Agent.MyStats.CanMove)
            {
                Agent.MyStats.CachedMove.OnArrive += HandleOnArrive;
            }

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            if (Agent.GetCommander() && loadedSavedValues && loadedDepositId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedDepositId);
                if (obj.MyAgentType == AgentType.Resource)
                {
                    CurrentResourceTarget = obj;
                }
            }
            else
            {
                HarvestType = ResourceType.Unknown;
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Harvester)
            {
                if (harvestCount > _harvestSpeed)
                {
                    //reset attackCount overcharge if left idle
                    harvestCount = _harvestSpeed;
                }
                else if (harvestCount < _harvestSpeed)
                {
                    //charge up attack
                    harvestCount += LockstepManager.DeltaTime;
                }

                if (Agent && Agent.IsActive)
                {
                    if (IsFocused || IsHarvestMoving)
                    {
                        if (CurrentResourceTarget.IsNotNull() && IsHarvesting)
                        {
                            BehaveWithResource();
                        }
                        else if (CurrentStorageTarget.IsNotNull() && IsEmptying)
                        {
                            BehaveWithStorage();
                        }
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
            RegisterHarvestGroup();
        }

        protected virtual void OnStartHarvestMove()
        {
            RTSAgent currentTarget = IsHarvesting ? CurrentResourceTarget : IsEmptying ? CurrentStorageTarget : null;

            if (Agent.MyStats.CanMove
                && currentTarget.IsNotNull()
                && !CheckRange(currentTarget.Body)
                )
            {
                Agent.MyStats.CachedMove.StartMove(Agent.MyStats.CachedMove.Destination, false);

                IsHarvestMoving = true;
                IsFocused = false;
            }
        }

        protected virtual void OnCollect()
        {
            long collect = CollectionAmount;
            // make sure that the harvester cannot collect more than it can carry
            if (currentLoadAmount + collect > Capacity)
            {
                collect = Capacity - currentLoadAmount;
            }

            if (!TargetResource.IsEmpty())
            {
                TargetResource.Remove(collect);
            }

            currentLoadAmount += collect;

            if (LoadAtCapacity())
            {
                Agent.Animator.SetIdleState(IdlingAnimState);
                IsHarvesting = false;
                IsEmptying = true;

                // The harvest AI will determine what to do next
                StopHarvesting();
            }
        }

        protected virtual void OnDeposit()
        {
            long deposit = DepositAmount;

            if (deposit > currentLoadAmount)
            {
                deposit = currentLoadAmount;
            }

            currentLoadAmount -= deposit;

            ResourceType depositType = HarvestType;
            Agent.Controller.Commander.CachedResourceManager.AddResource(depositType, deposit);

            if (currentLoadAmount <= 0)
            {
                IsHarvesting = true;
                IsEmptying = false;
                // The harvest AI will determine what to do next
                StopHarvesting();
            }
        }

        protected override void OnDeactivate()
        {
            StopHarvesting(true);
        }

        protected sealed override void OnStopCast()
        {
            StopHarvesting(true);
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "Harvesting", IsHarvesting);
            SaveManager.WriteBoolean(writer, "Emptying", IsEmptying);
            SaveManager.WriteFloat(writer, "CurrentLoad", currentLoadAmount);
            SaveManager.WriteBoolean(writer, "HarvestMoving", IsHarvestMoving);
            SaveManager.WriteString(writer, "HarvestType", HarvestType.ToString());
            if (CurrentResourceTarget)
            {
                SaveManager.WriteInt(writer, "ResourceDepositId", CurrentResourceTarget.GlobalID);
            }
            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "HarvestCount", harvestCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
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
                    currentLoadAmount = (long)readValue;
                    break;
                case "HarvestType":
                    HarvestType = WorkManager.GetResourceType((string)readValue);
                    break;
                case "ResourceDepositId":
                    loadedDepositId = (int)(long)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    inRange = (bool)readValue;
                    break;
                case "HarvestCount":
                    harvestCount = (long)readValue;
                    break;
                case "FastRangeToTarget":
                    fastRangeToTarget = (long)readValue;
                    break;
                default:
                    break;
            }
        }

        public void OnHarvestGroupProcessed(RTSAgent currentTarget)
        {
            Agent.Tag = AgentTag.Harvester;

            if (IsHarvesting)
            {
                CurrentResourceTarget = currentTarget;
                ResourceType resourceType = TargetResource.ResourceType;

                // we can only collect one resource at a time, other resources are lost
                if (resourceType == ResourceType.Unknown || resourceType != HarvestType)
                {
                    HarvestType = resourceType;
                    currentLoadAmount = 0;
                }

                SetHarvestAnimState();
            }
            else if (IsEmptying)
            {
                CurrentStorageTarget = currentTarget;
            }

            IsFocused = true;
            IsHarvestMoving = false;
            IsCasting = true;

            targetVersion = currentTarget.SpawnVersion;
        }

        public long GetCurrentLoad()
        {
            return currentLoadAmount;
        }

        public bool LoadAtCapacity()
        {
            if (currentLoadAmount >= Capacity)
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

        private void BehaveWithResource()
        {
            if (!CurrentResourceTarget.IsActive
                || CurrentResourceTarget.SpawnVersion != targetVersion
                || TargetResource.IsEmpty())
            {
                //Target's lifecycle has ended
                StopHarvesting();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange(CurrentResourceTarget.Body))
                    {
                        if (!inRange)
                        {
                            if (Agent.MyStats.CanMove)
                            {
                                Agent.MyStats.CachedMove.Arrive();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(HarvestingAnimState);

                        targetDirection.Normalize(out long mag);
                        bool withinTurn = Agent.MyStats.CachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          Agent.Body.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && Agent.Body.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= Agent.MyStats.CachedAttack.AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && Agent.MyStats.CanTurn)
                        {
                            Agent.MyStats.CachedTurn.StartTurnDirection(targetDirection);
                        }
                        else if (harvestCount >= _harvestSpeed)
                        {
                            StartWindup();
                        }
                    }
                    else
                    {
                        if (Agent.MyStats.CanMove)
                        {
                            Agent.MyStats.CachedMove.PauseAutoStop();
                            Agent.MyStats.CachedMove.PauseCollisionStop();
                            if (!Agent.MyStats.CachedMove.IsMoving
                                && !Agent.MyStats.CachedMove.MoveOnGroupProcessed)
                            {
                                OnStartHarvestMove();
                                Agent.Body.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    Agent.MyStats.CachedMove.Destination = CurrentResourceTarget.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (CurrentResourceTarget.Body.PositionChangedBuffer &&
                                            CurrentResourceTarget.Body.Position.FastDistance(Agent.MyStats.CachedMove.Destination.x, Agent.MyStats.CachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            OnStartHarvestMove();
                                            //So units don't sync up and path on the same frame
                                            repathTimer.AdvanceFrames(repathRandom);
                                        }
                                    }
                                }
                            }
                        }

                        if (inRange)
                        {
                            inRange = false;
                        }
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (Agent.MyStats.CanTurn)
                    {
                        Vector2d targetVector = CurrentResourceTarget.Body.Position - Agent.Body.Position;
                        Agent.MyStats.CachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= _windup)
                    {
                        windupCount = 0;
                        // begin collecting resources
                        StartHarvest();

                        while (harvestCount >= _harvestSpeed)
                        {
                            //resetting back down after attack is fired
                            harvestCount -= _harvestSpeed;
                        }
                        harvestCount += _windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (Agent.MyStats.CanMove && inRange)
                {
                    Agent.MyStats.CachedMove.PauseAutoStop();
                    Agent.MyStats.CachedMove.PauseCollisionStop();
                }
            }
        }

        private void BehaveWithStorage()
        {
            if (!CurrentStorageTarget.IsActive
               || CurrentStorageTarget.SpawnVersion != targetVersion)
            {
                // still can't find clostest resource store
                // send command to stop harvesting...
                StopHarvesting(true);
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange(CurrentStorageTarget.Body))
                    {
                        if (!inRange)
                        {
                            if (Agent.MyStats.CanMove)
                            {
                                Agent.MyStats.CachedMove.Arrive();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetIdleState(IdlingAnimState);
                        //if (audioElement != null && Time.timeScale > 0)
                        //{
                        //    audioElement.Play(emptyHarvestSound);
                        //}

                        targetDirection.Normalize(out long mag);
                        bool withinTurn = Agent.MyStats.CachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          Agent.Body.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && Agent.Body.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= Agent.MyStats.CachedAttack.AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && Agent.MyStats.CanTurn)
                        {
                            Agent.MyStats.CachedTurn.StartTurnDirection(targetDirection);
                        }
                        else if (harvestCount >= _harvestSpeed)
                        {
                            StartWindup();
                        }
                    }
                    else
                    {
                        if (Agent.MyStats.CanMove)
                        {
                            Agent.MyStats.CachedMove.PauseAutoStop();
                            Agent.MyStats.CachedMove.PauseCollisionStop();
                            if (!Agent.MyStats.CachedMove.IsMoving
                                && !Agent.MyStats.CachedMove.MoveOnGroupProcessed)
                            {
                                OnStartHarvestMove();
                                Agent.Body.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    Agent.MyStats.CachedMove.Destination = CurrentStorageTarget.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (CurrentStorageTarget.Body.PositionChangedBuffer &&
                                            CurrentStorageTarget.Body.Position.FastDistance(Agent.MyStats.CachedMove.Destination.x, Agent.MyStats.CachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            OnStartHarvestMove();
                                            //So units don't sync up and path on the same frame
                                            repathTimer.AdvanceFrames(repathRandom);
                                        }
                                    }
                                }
                            }
                        }

                        if (inRange)
                        {
                            inRange = false;
                        }
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (Agent.MyStats.CanTurn)
                    {
                        Vector2d targetVector = CurrentStorageTarget.Body.Position - Agent.Body.Position;
                        Agent.MyStats.CachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= _windup)
                    {
                        windupCount = 0;
                        StartHarvest();
                        while (harvestCount >= _harvestSpeed)
                        {
                            //resetting back down after attack is fired
                            harvestCount -= (_harvestSpeed);
                        }
                        harvestCount += _windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (Agent.MyStats.CanMove && inRange)
                {
                    Agent.MyStats.CachedMove.PauseAutoStop();
                    Agent.MyStats.CachedMove.PauseCollisionStop();
                }
            }
        }

        private bool CheckRange(LSBody targetBody)
        {
            fastRangeToTarget = Agent.MyStats.ActionRange + (targetBody.IsNotNull() ? targetBody.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;

            targetDirection = targetBody.Position - Agent.Body.Position;
            fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        private void StartHarvest()
        {
            if (Agent.MyStats.CanMove)
            {
                // we don't want to be able to construct and move!
                Agent.MyStats.CachedMove.StopMove();
            }
            Agent.Body.Priority = _increasePriority ? basePriority + 1 : basePriority;

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
                case ResourceType.Wood:
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
        private void StopHarvesting(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyHarvestGroup.IsNotNull() && complete)
            {
                MyHarvestGroup.Remove(this);
            }

            IsHarvestMoving = false;

            if (complete)
            {
                IsHarvesting = false;
                IsEmptying = false;
                Agent.Tag = AgentTag.None;

                CurrentResourceTarget = null;
                CurrentStorageTarget = null;
            }
            else if (CurrentResourceTarget.IsNotNull() || CurrentStorageTarget.IsNotNull())
            {
                if (Agent.MyStats.CanMove && !inRange)
                {
                    Agent.MyStats.CachedMove.StopMove();
                }
            }

            Agent.Body.Priority = basePriority;

            IsCasting = false;
        }
    }
}