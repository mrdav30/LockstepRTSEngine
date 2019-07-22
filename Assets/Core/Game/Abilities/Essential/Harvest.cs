using Newtonsoft.Json;
using System;
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
        private RTSAgent closestResourceStore;
        private long currentLoadAmount = 0;
        private long currentDepositAmount = 0;

        private long fastRangeToTarget;
        private Move cachedMove;
        private Turn cachedTurn;
        private Attack cachedAttack;
        private WorkerAI cachedAI;
        private LSBody CachedBody { get { return Agent.Body; } }

        //Stuff for the logic
        private bool inRange;
        private int basePriority;
        private long harvestCount;
        private int loadedDepositId = -1;

        public bool IsHarvestMoving { get; private set; }
        public bool IsHarvesting { get; private set; }
        public bool IsEmptying { get; private set; }
        public bool IsFocused { get; private set; }

        #region Serialized Values (Further description in properties)
        public string ResourceStoreName = String.Empty;
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

        long windupCount;

        #region variables for quick fix for repathing to target's new position
        const long repathDistance = FixedMath.One * 2;
        FrameTimer repathTimer = new FrameTimer();
        const int repathInterval = LockstepManager.FrameRate * 2;
        private int repathRandom = 0;
        #endregion

        protected override void OnSetup()
        {
            cachedTurn = Agent.GetAbility<Turn>();
            cachedMove = Agent.GetAbility<Move>();
            cachedAttack = Agent.GetAbility<Attack>();
            cachedAI = Agent.GetAbility<WorkerAI>();

            cachedMove.onStartMove += HandleStartMove;

            basePriority = CachedBody.Priority;
        }

        private void HandleStartMove()
        {
            if (currentLoadAmount > 0)
            {
                Agent.SetState(MovingAnimState);
            }
            else
            {
                Agent.SetState(AnimState.Moving);
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
                        Agent.SetState(IdlingAnimState);
                    }
                    else if (!cachedAttack.Target)
                    {
                        Agent.SetState(AnimState.Idling);
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
            if (!resourceTarget || resourceTarget.IsActive == false)
            {
                //Target's lifecycle has ended
                StopHarvesting();
                return;
            }

            SetAnimState();

            if (!IsWindingUp)
            {
                Vector2d targetDirection = resourceTarget.Body._position - CachedBody._position;
                long fastMag = targetDirection.FastMagnitude();

                if (CheckRange(resourceTarget.Body))
                {
                    IsHarvestMoving = false;
                    if (!inRange)
                    {
                        cachedMove.StopMove();
                        inRange = true;
                    }
                    Agent.SetState(HarvestingAnimState);

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
                        cachedMove.StartMove(resourceTarget.Body._position);
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
                                    cachedMove.StartMove(resourceTarget.Body._position);
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

        void BehaveWithStorage()
        {
            closestResourceStore = ClosestResourceStore(ResourceStoreName);
            if (!closestResourceStore || closestResourceStore.IsActive == false)
            {
                //Target's lifecycle has ended
                return;
            }

            if (!IsWindingUp)
            {
                Vector2d targetDirection = closestResourceStore.Body._position - CachedBody._position;
                long fastMag = targetDirection.FastMagnitude();

                if (CheckRange(closestResourceStore.Body))
                {
                    if (!inRange)
                    {
                        cachedMove.StopMove();
                        inRange = true;
                    }
                    Agent.SetState(IdlingAnimState);
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
                        cachedMove.StartMove(closestResourceStore.Body._position);
                        CachedBody.Priority = basePriority;
                    }
                    else
                    {
                        if (inRange)
                        {
                            cachedMove.Destination = closestResourceStore.Body.Position;
                        }
                        else
                        {
                            if (repathTimer.AdvanceFrame())
                            {
                                if (closestResourceStore.Body.PositionChangedBuffer &&
                                    closestResourceStore.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                {
                                    cachedMove.StartMove(closestResourceStore.Body._position);
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

            if (resourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
            {
                cachedAI.DecideWhatToDo();
            }
            else
            {
                resourceTarget.GetAbility<ResourceDeposit>().Remove(collect);
            }
            currentLoadAmount += collect;

            if (currentLoadAmount >= Capacity)
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
                if (resourceTarget && !resourceTarget.GetAbility<ResourceDeposit>().IsEmpty())
                {
                    IsHarvesting = true;
                    IsHarvestMoving = true;
                }
                else
                {
                    cachedAI.DecideWhatToDo();
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

        [HideInInspector]
        public AnimState HarvestingAnimState, MovingAnimState, IdlingAnimState;

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

        public void StopHarvesting(bool complete = false)
        {
            inRange = false;
            IsFocused = false;
            if (complete)
            {
                IsHarvestMoving = false;
            }
            else
            {
                if (IsHarvestMoving)
                {
                    cachedMove.StartMove(this.resourceTarget.Body.Position);
                }
                else
                {
                    if (resourceTarget != null && inRange == false)
                    {
                        cachedMove.StopMove();
                    }
                }
            }

            //  ResourceDeposit = null;
            CachedBody.Priority = basePriority;

            //  IsCasting = false;
            IsHarvesting = false;
            IsEmptying = false;

            if (currentLoadAmount <= 0)
            {
                IsCasting = false;
            }
        }

        protected override void OnDeactivate()
        {
            StopHarvesting(true);
        }

        public void StartHarvest(RTSAgent resource)
        {
            if (resource != Agent && resource != null)
            {
                Agent.Tag = AgentTag.Harvester;
                //if (audioElement != null)
                //{
                //    audioElement.Play(startHarvestSound);
                //}
                resourceTarget = resource;
                ResourceType resourceType = resource.GetAbility<ResourceDeposit>().ResourceType;
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
                    StartHarvestMove(resourceTarget.Body._position);
                }
            }
        }

        public virtual void StartHarvestMove(Vector2d destination)
        {
            Agent.StopCast(this.ID);

            IsHarvestMoving = true;
            //send move command
            cachedMove.StartMove(destination);
        }

        public void StartDeposit(RTSAgent resourceStore)
        {
            if (resourceStore != Agent && resourceStore != null)
            {
                Agent.Tag = AgentTag.Harvester;
                //if (audioElement != null)
                //{
                //    audioElement.Play(startHarvestSound);
                //}

                IsHarvesting = false;
                IsCasting = true;
                IsEmptying = true;

                if (!CheckRange(resourceStore.Body))
                {
                    StartDepositMove(resourceStore.Body._position);
                }
            }
        }

        public virtual void StartDepositMove(Vector2d destination)
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
                RTSAgent tempTarget;
                ushort targetValue = (ushort)target.Value;
                if (AgentController.TryGetAgentInstance(targetValue, out tempTarget))
                {
                    if (tempTarget)
                    {
                        if (tempTarget.MyAgentType == AgentType.Resource && !tempTarget.GetAbility<ResourceDeposit>().IsEmpty())
                        {
                            StartHarvest(tempTarget as RTSAgent);
                        }
                        else if (tempTarget.MyAgentType == AgentType.Building && (tempTarget as RTSAgent).objectName == ResourceStoreName && currentLoadAmount > 0)
                        {
                            StartDeposit(tempTarget as RTSAgent);
                        }
                    }
                    else
                    {
                        StopHarvesting();
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
            StopHarvesting(true);
        }

        private RTSAgent ClosestResourceStore(string resourceStoreName)
        {
            //change list to fastarray
            List<RTSAgent> playerBuildings = new List<RTSAgent>();
            // use RTS influencer?
            foreach (RTSAgent child in Agent.Controller.Commander.GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>())
            {
                if (child.objectName == resourceStoreName)
                {
                    playerBuildings.Add(child);
                }
            }
            RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(playerBuildings, transform.position) as RTSAgent;

            return nearestObject;
        }

        public void SetResourceTarget(RTSAgent value)
        {
            resourceTarget = value;
        }

        public long GetCurrentLoad()
        {
            return this.currentLoadAmount;
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