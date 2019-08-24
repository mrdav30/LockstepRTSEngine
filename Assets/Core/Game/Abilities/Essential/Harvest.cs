using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Harvest : ActiveAbility
    {
        private const int searchRate = LockstepManager.FrameRate / 2;
        public ResourceType HarvestType { get; private set; }
        private RTSAgent resourceTarget;
        private RTSAgent resourceStorage;
        private long currentLoadAmount = 0;
        private long currentDepositAmount = 0;

        private long fastRangeToTarget;
        private Move cachedMove;
        private Turn cachedTurn;
        private Attack cachedAttack;
        private LSBody CachedBody { get { return Agent.Body; } }

        //Stuff for the logic
        private bool inRange;
        private int basePriority;
        private long harvestCount;
        private int loadedDepositId = -1;

        public bool IsHarvestMoving { get; private set; }
        public bool IsHarvesting { get; private set; }
        public bool IsEmptying { get; private set; }

        #region Serialized Values (Further description in properties)
        public long CollectionAmount = FixedMath.One;
        public long DepositAmount = FixedMath.One;
        public long Capacity = FixedMath.One;
        [SerializeField, FixedNumber]
        private long _harvestInterval = 1 * FixedMath.One;
        [SerializeField, FixedNumber]
        private long _windup;
        [SerializeField]
        private bool _increasePriority = true;
        #endregion

        public long Windup { get { return _windup; } }
        [Lockstep(true)]
        public bool IsWindingUp { get; set; }

        private long windupCount;

        [HideInInspector]
        public AnimState HarvestingAnimState, MovingAnimState, IdlingAnimState;

        #region variables for quick fix for repathing to target's new position
        private const long repathDistance = FixedMath.One * 2;
        private FrameTimer repathTimer = new FrameTimer();
        private const int repathInterval = LockstepManager.FrameRate * 2;
        private int repathRandom = 0;
        #endregion

        protected override void OnSetup()
        {
            cachedTurn = Agent.GetAbility<Turn>();
            cachedMove = Agent.GetAbility<Move>();
            cachedAttack = Agent.GetAbility<Attack>();

            cachedMove.onStartMove += HandleStartMove;

            basePriority = CachedBody.Priority;
        }

        private void HandleStartMove()
        {
            if (currentLoadAmount > 0)
            {
                Agent.Animator.SetState(MovingAnimState);
            }
            else
            {
                Agent.Animator.SetState(AnimState.Moving);
            }
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            harvestCount = 0;
            IsHarvesting = false;
            IsHarvestMoving = false;
            inRange = false;
            IsFocused = false;

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;

            if (Agent.GetCommander() && loadedSavedValues && loadedDepositId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedDepositId);
                if (obj.MyAgentType == AgentType.Resource)
                {
                    resourceTarget = obj;
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
                if (harvestCount > _harvestInterval)
                {
                    //reset attackCount overcharge if left idle
                    harvestCount = _harvestInterval;
                }
                else if (harvestCount < _harvestInterval)
                {
                    //charge up attack
                    harvestCount += LockstepManager.DeltaTime;
                }

                if (IsFocused)
                {
                    if (IsHarvesting)
                    {
                        BehaveWithResource();
                    }
                    else if (IsEmptying)
                    {
                        BehaveWithStorage();
                    }

                    if (IsHarvestMoving)
                    {
                        cachedMove.StartLookingForStopPause();
                    }
                }

                if (!cachedMove.IsMoving && !IsHarvesting && !IsEmptying)
                {
                    if (currentLoadAmount > 0)
                    {
                        Agent.Animator.SetIdleState(IdlingAnimState);
                    }
                    else if (!cachedAttack.Target)
                    {
                        Agent.Animator.SetIdleState(AnimState.Idling);
                    }
                }
            }
        }

        void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        void BehaveWithResource()
        {
            if (!resourceTarget
                || resourceTarget.IsActive == false
                || resourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
            {
                //Target's lifecycle has ended
                StopHarvesting();
            }
            else
            {
                SetAnimState();

                if (!IsWindingUp)
                {
                    Vector2d targetDirection = resourceTarget.Body.Position - CachedBody.Position;
                    long fastMag = targetDirection.FastMagnitude();

                    if (CheckRange(resourceTarget.Body))
                    {
                        IsHarvestMoving = false;
                        if (!inRange)
                        {
                            cachedMove.StopMove();
                            inRange = true;
                        }
                        Agent.Animator.SetState(HarvestingAnimState);

                        long mag;
                        targetDirection.Normalize(out mag);
                        bool withinTurn = cachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          CachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && CachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= cachedAttack.AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn)
                        {
                            cachedTurn.StartTurnDirection(targetDirection);
                        }
                        else
                        {
                            if (harvestCount >= _harvestInterval)
                            {
                                StartWindup();
                            }
                        }
                    }
                    else
                    {
                        cachedMove.PauseAutoStop();
                        cachedMove.PauseCollisionStop();
                        if (cachedMove.IsMoving == false)
                        {
                            cachedMove.StartMove(resourceTarget.Body.Position);
                            CachedBody.Priority = basePriority;
                        }
                        else
                        {
                            if (inRange)
                            {
                                cachedMove.Destination = resourceTarget.Body.Position;
                            }
                            else
                            {
                                if (repathTimer.AdvanceFrame())
                                {
                                    if (resourceTarget.Body.PositionChangedBuffer &&
                                        resourceTarget.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                    {
                                        cachedMove.StartMove(resourceTarget.Body.Position);
                                        //So units don't sync up and path on the same frame
                                        repathTimer.AdvanceFrames(repathRandom);
                                    }
                                }
                            }
                        }

                        if (inRange == true)
                        {
                            inRange = false;
                        }
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (windupCount >= Windup)
                    {
                        windupCount = 0;
                        // begin collecting resources
                        Collect();

                        while (this.harvestCount >= _harvestInterval)
                        {
                            //resetting back down after attack is fired
                            this.harvestCount -= (this._harvestInterval);
                        }
                        this.harvestCount += Windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (inRange)
                {
                    cachedMove.PauseAutoStop();
                    cachedMove.PauseCollisionStop();
                }
            }
        }

        void BehaveWithStorage()
        {
            resourceStorage = ClosestResourceStore();
            if (!resourceStorage)
            {
                // can't find clostest resource store
                // send command to stop harvesting...
                StopHarvesting(true);
            }
            else
            {
                if (!IsWindingUp)
                {
                    Vector2d targetDirection = resourceStorage.Body.Position - CachedBody.Position;
                    long fastMag = targetDirection.FastMagnitude();

                    if (CheckRange(resourceStorage.Body))
                    {
                        if (!inRange)
                        {
                            cachedMove.StopMove();
                            inRange = true;
                        }
                        Agent.Animator.SetIdleState(IdlingAnimState);
                        //if (audioElement != null && Time.timeScale > 0)
                        //{
                        //    audioElement.Play(emptyHarvestSound);
                        //}

                        long mag;
                        targetDirection.Normalize(out mag);
                        bool withinTurn = cachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          CachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && CachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= cachedAttack.AttackAngle);
                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn)
                        {
                            cachedTurn.StartTurnDirection(targetDirection);
                        }
                        else
                        {
                            if (harvestCount >= _harvestInterval)
                            {
                                StartWindup();
                            }
                        }
                    }
                    else
                    {
                        cachedMove.PauseAutoStop();
                        cachedMove.PauseCollisionStop();
                        if (cachedMove.IsMoving == false)
                        {
                            cachedMove.StartMove(resourceStorage.Body.Position);
                            CachedBody.Priority = basePriority;
                        }
                        else
                        {
                            if (inRange)
                            {
                                cachedMove.Destination = resourceStorage.Body.Position;
                            }
                            else
                            {
                                if (repathTimer.AdvanceFrame())
                                {
                                    if (resourceStorage.Body.PositionChangedBuffer &&
                                        resourceStorage.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                    {
                                        cachedMove.StartMove(resourceStorage.Body.Position);
                                        //So units don't sync up and path on the same frame
                                        repathTimer.AdvanceFrames(repathRandom);
                                    }
                                }
                            }
                        }

                        if (inRange == true)
                        {
                            inRange = false;
                        }
                    }
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (windupCount >= Windup)
                    {
                        windupCount = 0;
                        Deposit();
                        while (this.harvestCount >= _harvestInterval)
                        {
                            //resetting back down after attack is fired
                            this.harvestCount -= (this._harvestInterval);
                        }
                        this.harvestCount += Windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (inRange)
                {
                    cachedMove.PauseAutoStop();
                    cachedMove.PauseCollisionStop();
                }
            }
        }

        private void Collect()
        {
            cachedMove.StopMove();
            CachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            long collect = CollectionAmount;
            // make sure that the harvester cannot collect more than it can carry
            if (currentLoadAmount + collect > Capacity)
            {
                collect = Capacity - currentLoadAmount;
            }

            if (!resourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
            {
                resourceTarget.GetAbility<ResourceDeposit>().Remove(collect);
            }

            currentLoadAmount += collect;

            if (LoadAtCapacity())
            {
                IsHarvesting = false;
                IsEmptying = true;
            }
        }

        private void Deposit()
        {
            cachedMove.StopMove();
            CachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            currentDepositAmount += DepositAmount;
            long deposit = DepositAmount;

            if (deposit > currentLoadAmount)
            {
                deposit = currentLoadAmount;
            }
            currentDepositAmount -= deposit;
            currentLoadAmount -= deposit;

            ResourceType depositType = HarvestType;
            Agent.Controller.Commander.CachedResourceManager.AddResource(depositType, deposit);

            if (currentLoadAmount <= 0)
            {
                IsEmptying = false;
                if (!resourceTarget
                    || resourceTarget.IsActive == false
                    || resourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
                {
                    //Target's lifecycle has ended
                    StopHarvesting();
                }
                else
                {
                    IsHarvesting = true;
                    IsHarvestMoving = true;
                }
            }
        }

        bool CheckRange(LSBody targetBody)
        {
            fastRangeToTarget = cachedAttack.Range + (targetBody.IsNotNull() ? targetBody.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;

            Vector2d targetDirection = targetBody._position - CachedBody._position;
            long fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        protected void SetAnimState()
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
        public void StopHarvesting(bool complete = false)
        {
            inRange = false;
            IsFocused = false;
            IsHarvesting = false;
            IsEmptying = false;
            IsCasting = false;

            CachedBody.Priority = basePriority;

            if (complete)
            {
                IsHarvestMoving = false;
                //    resourceTarget = null;
                Agent.Tag = AgentTag.None;
            }
            else
            {
                if (IsHarvestMoving)
                {
                    cachedMove.StartMove(this.resourceTarget.Body.Position);
                }
                else
                {
                    if (resourceTarget && inRange == false)
                    {
                        cachedMove.StopMove();
                    }
                }
            }
        }

        protected override void OnDeactivate()
        {
            StopHarvesting(true);
        }

        public void StartHarvest(RTSAgent resource)
        {
            resourceTarget = resource;
            ResourceType resourceType = resourceTarget.GetAbility<ResourceDeposit>().ResourceType;

            // we can only collect one resource at a time, other resources are lost
            if (resourceType == ResourceType.Unknown || resourceType != HarvestType)
            {
                HarvestType = resourceType;
                currentLoadAmount = 0;
            }

            IsHarvesting = true;
            IsCasting = true;
            IsEmptying = false;

            if (!CheckRange(resourceTarget.Body))
            {
                MoveToDestination(resourceTarget.Body.Position);
            }
        }

        public void TargetStorage(RTSAgent storage)
        {
            resourceStorage = storage;

            IsHarvesting = false;
            IsCasting = true;
            IsEmptying = true;

            if (!CheckRange(resourceStorage.Body))
            {
                MoveToDestination(resourceStorage.Body.Position);
            }
        }

        public virtual void MoveToDestination(Vector2d destination)
        {
            Agent.StopCast(this.ID);

            IsHarvestMoving = true;
            //send move command
            cachedMove.StartMove(destination);
        }

        protected override void OnExecute(Command com)
        {
            DefaultData target;
            if (com.TryGetData<DefaultData>(out target) && target.Is(DataType.UShort))
            {
                IsFocused = true;
                IsHarvestMoving = false;
                Agent.Tag = AgentTag.Harvester;

                RTSAgent tempTarget;
                ushort targetValue = (ushort)target.Value;

                if (AgentController.TryGetAgentInstance(targetValue, out tempTarget))
                {
                    if (tempTarget != Agent)
                    {

                        if (tempTarget.MyAgentType == AgentType.Resource && !tempTarget.GetAbility<ResourceDeposit>().IsEmpty())
                        {
                            StartHarvest(tempTarget);
                        }
                        else if (tempTarget.MyAgentType == AgentType.Building && currentLoadAmount > 0)
                        {
                            TargetStorage(tempTarget);
                        }
                    }
                    else
                    {
                        StopHarvesting(true);
                    }
                }
                else
                {
                    Debug.Log("nope");
                }
            }
        }

        protected sealed override void OnStopCast()
        {
            if (Agent.Tag == AgentTag.Harvester)
            {
                StopHarvesting(true);
            }
        }

        private RTSAgent ClosestResourceStore()
        {
            //change list to fastarray
            List<RTSAgent> playerBuildings = new List<RTSAgent>();
            // use RTS influencer?
            foreach (RTSAgent child in Agent.Controller.Commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>())
            {
                if (child.GetAbility<Structure>()
                    && child.GetAbility<Structure>().CanStoreResources(HarvestType)
                    && !child.GetAbility<Structure>().NeedsConstruction)
                {
                    playerBuildings.Add(child);
                }
            }
            if (playerBuildings.Count > 0)
            {
                RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(playerBuildings, transform.position) as RTSAgent;
                return nearestObject;
            }
            else
            {
                return null;
            }
        }

        public void SetResourceTarget(RTSAgent value)
        {
            resourceTarget = value;
        }

        public long GetCurrentLoad()
        {
            return this.currentLoadAmount;
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

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "Harvesting", IsHarvesting);
            SaveManager.WriteBoolean(writer, "Emptying", IsEmptying);
            SaveManager.WriteFloat(writer, "CurrentLoad", currentLoadAmount);
            SaveManager.WriteFloat(writer, "CurrentDeposit", currentDepositAmount);
            SaveManager.WriteBoolean(writer, "HarvestMoving", IsHarvestMoving);
            SaveManager.WriteString(writer, "HarvestType", HarvestType.ToString());
            if (resourceTarget)
            {
                SaveManager.WriteInt(writer, "ResourceDepositId", resourceTarget.GlobalID);
            }
            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "HarvestCount", harvestCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
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
                case "CurrentDeposit":
                    currentDepositAmount = (long)readValue;
                    break;
                case "HarvestType":
                    HarvestType = WorkManager.GetResourceType((string)readValue);
                    break;
                case "ResourceDepositId":
                    loadedDepositId = (int)(System.Int64)readValue;
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
    }
}