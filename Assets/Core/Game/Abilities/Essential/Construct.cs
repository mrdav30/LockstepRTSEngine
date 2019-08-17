using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Construct : ActiveAbility
    {
        #region Properties
        private const int searchRate = LockstepManager.FrameRate / 2;
        private long currentAmountBuilt = 0;
        private long fastRangeToTarget;
        private Move cachedMove;
        private Turn cachedTurn;
        private Attack cachedAttack;
        protected LSBody CachedBody { get { return Agent.Body; } }
        private RTSAgent CurrentProject;
        public bool IsBuildMoving { get; private set; }
        public bool IsBuilding { get; private set; }

        //Stuff for the logic
        private bool inRange;
        private int basePriority;
        private long constructCount;
        private int loadedProjectId = -1;

        #region Serialized Values (Further description in properties)
        [SerializeField, FixedNumber]
        private long constructAmount = FixedMath.One;
        [SerializeField, FixedNumber, Tooltip("Used to determine how fast agent can build.")]
        private long _constructInterval = 1 * FixedMath.One;
        [SerializeField, Tooltip("Enter object names for prefabs this agent can build.")]
        private String[] _buildActions;
        [SerializeField, FixedNumber]
        private long _windup = 0;
        [SerializeField]
        private bool _increasePriority = true;
        #endregion

        public long Windup { get { return _windup; } }
        [Lockstep(true)]
        public bool IsWindingUp { get; set; }

        private long windupCount;

        private Queue<QStructure> ConstructQueue = new Queue<QStructure>();

        #region variables for quick fix for repathing to target's new position
        private const long repathDistance = FixedMath.One * 2;
        private FrameTimer repathTimer = new FrameTimer();
        private const int repathInterval = LockstepManager.FrameRate * 2;
        private int repathRandom = 0;
        #endregion
        #endregion Properties

        protected override void OnSetup()
        {
            cachedTurn = Agent.GetAbility<Turn>();
            cachedMove = Agent.GetAbility<Move>();
            cachedAttack = Agent.GetAbility<Attack>();
            cachedMove.onStartMove += HandleStartMove;

            basePriority = CachedBody.Priority;
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            constructCount = 0;
            CurrentProject = null;
            IsBuilding = false;
            IsBuildMoving = false;
            inRange = false;
            IsFocused = false;

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;

            if (Agent.GetCommander() && loadedSavedValues && loadedProjectId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedProjectId);
                if (obj.MyAgentType == AgentType.Building)
                {
                    CurrentProject = obj;
                }
            }
        }

        protected override void OnSimulate()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                if (constructCount > _constructInterval)
                {
                    //reset attackCount overcharge if left idle
                    constructCount = _constructInterval;
                }
                else if (constructCount < _constructInterval)
                {
                    //charge up attack
                    constructCount += LockstepManager.DeltaTime;
                }

                // If construction queue not empty and agent not busy, get building
                if (ConstructQueue.Count > 0 && !IsBuilding)
                {
                    SetConstructQueue();
                }

                if (IsBuilding)
                {
                    BehaveWithTarget();
                }

                if (IsBuildMoving)
                {
                    cachedMove.StartLookingForStopPause();
                }
            }
        }

        private void HandleStartMove()
        {
            currentAmountBuilt = 0;

            if (!IsBuildMoving && IsBuilding)
            {
                StopConstruction();
            }
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        private void BehaveWithTarget()
        {
            if (CurrentProject && (CurrentProject.IsActive == false || !CurrentProject.GetAbility<Structure>().NeedsConstruction))
            {
                //Target's lifecycle has ended
                StopConstruction();
            }
            else
            {
                Vector2d targetDirection = CurrentProject.Body._position - CachedBody._position;
                long fastMag = targetDirection.FastMagnitude();

                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        IsBuildMoving = false;
                        if (!inRange)
                        {
                            cachedMove.StopMove();
                            inRange = true;
                        }
                        Agent.Animator.SetState(ConstructingAnimState);

                        if (!CurrentProject.GetAbility<Structure>().ConstructionStarted)
                        {
                            CurrentProject.GetAbility<Structure>().ConstructionStarted = true;
                            // Restore material
                            ConstructionHandler.RestoreMaterial(CurrentProject.gameObject);
                        }

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
                            if (constructCount >= _constructInterval)
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
                            cachedMove.StartMove(CurrentProject.Body._position);
                            CachedBody.Priority = basePriority;
                        }
                        else
                        {
                            if (inRange)
                            {
                                cachedMove.Destination = CurrentProject.Body.Position;
                            }
                            else
                            {
                                if (repathTimer.AdvanceFrame())
                                {
                                    if (CurrentProject.Body.PositionChangedBuffer &&
                                        CurrentProject.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                    {
                                        cachedMove.StartMove(CurrentProject.Body._position);
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
                        Build();
                        while (this.constructCount >= _constructInterval)
                        {
                            //resetting back down after attack is fired
                            this.constructCount -= (this._constructInterval);
                        }
                        this.constructCount += Windup;
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

        private void Build()
        {
            cachedMove.StopMove();
            CachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            CurrentProject.GetAbility<Structure>().Construct(constructAmount);

            if (!CurrentProject.GetAbility<Structure>().NeedsConstruction)
            {
                //if (audioElement != null)
                //{
                //    audioElement.Play(finishedJobSound);
                //}
                StopConstruction();
            }
        }

        private bool CheckRange()
        {
            Vector2d targetDirection = CurrentProject.Body._position - CachedBody._position;
            long fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        protected virtual AnimState ConstructingAnimState
        {
            get { return AnimState.Constructing; }
        }

        public void SetConstructQueue()
        {
            while (ConstructQueue.Count > 0)
            {
                QStructure qStructure = ConstructQueue.Dequeue();
                if (qStructure.IsNotNull())
                {
                    RTSAgent newRTSAgent = Agent.Controller.CreateAgent(qStructure.StructureName, qStructure.BuildPoint, qStructure.RotationPoint) as RTSAgent;
                    Structure newStructure = newRTSAgent.GetAbility<Structure>();

                    if (newStructure.StructureType == StructureType.Wall)
                    {
                        newRTSAgent.transform.localScale = qStructure.LocalScale.ToVector3();
                        newStructure.IsOverlay = true;
                    }

                    newRTSAgent.Body.HalfWidth = qStructure.HalfWidth;
                    newRTSAgent.Body.HalfLength = qStructure.HalfLength;

                    newStructure.BuildSizeLow = (newRTSAgent.Body.HalfWidth.CeilToInt() * 2);
                    newStructure.BuildSizeHigh = (newRTSAgent.Body.HalfLength.CeilToInt() * 2);

                    if (GridBuilder.Place(newRTSAgent.GetAbility<Structure>(), newRTSAgent.Body._position))
                    {
                        Agent.GetCommander().CachedResourceManager.RemoveResources(newRTSAgent);

                        newRTSAgent.SetPlayingArea(Agent.GetPlayerArea());
                        newRTSAgent.SetCommander(Agent.GetCommander());

                        newRTSAgent.gameObject.name = newRTSAgent.objectName;
                        newRTSAgent.transform.parent = newStructure.StructureType == StructureType.Wall ? WallPositioningHelper.OrganizerWalls.transform
                            : ConstructionHandler.OrganizerStructures.transform;

                        newStructure.AwaitConstruction();
                        // Set to transparent material until constructor is in range to start
                        ConstructionHandler.SetTransparentMaterial(newStructure.gameObject, GameResourceManager.AllowedMaterial, true);

                        if (CurrentProject.IsNull())
                        {
                            CurrentProject = newRTSAgent;
                            StartConstructMove();
                        }
                    }
                    else
                    {
                        Debug.Log("Couldn't place building!");
                        newRTSAgent.Die();
                    }
                }
            }
        }

        public void StopConstruction(bool complete = false)
        {
            inRange = false;
            IsFocused = false;
            IsBuilding = false;
            IsCasting = false;

            CurrentProject = null;
            CachedBody.Priority = basePriority;

            if (complete)
            {
                IsBuildMoving = false;
                Agent.Tag = AgentTag.None;
                ConstructQueue.Clear();
            }
            else
            {
                if (IsBuildMoving && this.CurrentProject)
                {
                    cachedMove.StartMove(this.CurrentProject.Body.Position);
                }
                else if (!inRange)
                {
                    cachedMove.StopMove();
                }
            }
        }

        protected override void OnDeactivate()
        {
            StopConstruction(true);
        }

        public virtual void StartConstructMove()
        {
            IsBuilding = true;
            IsCasting = true;
            fastRangeToTarget = cachedAttack.Range + (CurrentProject.Body.IsNotNull() ? CurrentProject.Body.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;

            if (!CheckRange())
            {
                Agent.StopCast(this.ID);

                IsBuildMoving = true;
                // send move command
                cachedMove.StartMove(CurrentProject.Body._position);
            }
        }

        protected override void OnExecute(Command com)
        {
            //first check if queue command
            QueueStructure qStructure;
            if (com.TryGetData(out qStructure))
            {
                ConstructQueue.Enqueue(qStructure.Value);
            }
            else
            {
                DefaultData target;
                if (com.TryGetData(out target))
                {
                    IsFocused = true;
                    IsBuildMoving = false;
                    Agent.Tag = AgentTag.Builder;

                    // construction hasn't started yet, only a bool given 
                    if (target.Is(DataType.Bool) && ConstructQueue.Count > 0)
                    {
                        if ((bool)target.Value)
                        {
                            SetConstructQueue();
                        }
                        else
                        {
                            ConstructQueue.Clear();
                        }
                    }
                    // otherwise this is another agent coming to help
                    // should have been sent local id of target
                    else if (target.Is(DataType.UShort))
                    {
                        RTSAgent tempTarget;
                        ushort targetValue = (ushort)target.Value;
                        if (AgentController.TryGetAgentInstance(targetValue, out tempTarget))
                        {
                            RTSAgent building = tempTarget;
                            if (building && building.GetAbility<Structure>().NeedsConstruction)
                            {
                                CurrentProject = building;
                                StartConstructMove();
                            }
                        }
                    }
                }
            }
        }

        protected sealed override void OnStopCast()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
        }

        public String[] GetBuildActions()
        {
            return this._buildActions;
        }

        public bool HasStructuresQueued()
        {
            return ConstructQueue.Count > 0;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "Building", IsBuilding);
            SaveManager.WriteFloat(writer, "AmountBuilt", currentAmountBuilt);
            SaveManager.WriteBoolean(writer, "BuildMoving", IsBuildMoving);
            if (CurrentProject)
            {
                SaveManager.WriteInt(writer, "CurrentProjectId", CurrentProject.GlobalID);
            }
            SaveManager.WriteBoolean(writer, "Focused", IsFocused);
            SaveManager.WriteBoolean(writer, "InRange", inRange);
            SaveManager.WriteLong(writer, "ConstructCount", constructCount);
            SaveManager.WriteLong(writer, "FastRangeToTarget", fastRangeToTarget);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "Building":
                    IsBuilding = (bool)readValue;
                    break;
                case "BuildMoving":
                    IsBuildMoving = (bool)readValue;
                    break;
                case "AmountBuilt":
                    currentAmountBuilt = (long)readValue;
                    break;
                case "CurrentProjectId":
                    loadedProjectId = (int)(System.Int64)readValue;
                    break;
                case "Focused":
                    IsFocused = (bool)readValue;
                    break;
                case "InRange":
                    inRange = (bool)readValue;
                    break;
                case "ConstructCount":
                    constructCount = (long)readValue;
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