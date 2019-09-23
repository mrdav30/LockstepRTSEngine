using Newtonsoft.Json;
using RTSLockstep.Data;
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
        private Vector2d Destination;

        private Move cachedMove;
        protected virtual bool canMove { get; private set; }
        private Turn cachedTurn;
        protected bool canTurn { get; private set; }
        private Attack cachedAttack;
        protected LSBody cachedBody { get { return Agent.Body; } }

        private RTSAgent currentProject;
        private Structure projectStructure { get { return currentProject.GetAbility<Structure>(); } }
        public bool IsBuildMoving { get; private set; }

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

        protected virtual AnimState ConstructingAnimState
        {
            get { return AnimState.Constructing; }
        }

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

            basePriority = cachedBody.Priority;
            canMove = cachedMove.IsNotNull();

            if (canMove)
            {
                cachedMove.onStartMove += HandleStartMove;
                cachedMove.onGroupProcessed += HandleMoveGroupProcessed;
                cachedMove.onArrive += HandleOnArrive;
            }

            canTurn = cachedTurn.IsNotNull();

            cachedAttack = Agent.GetAbility<Attack>();
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            constructCount = 0;
            currentProject = null;
            IsBuildMoving = false;
            inRange = false;
            IsFocused = false;

            Destination = Vector2d.zero;
            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

            if (Agent.GetCommander() && loadedSavedValues && loadedProjectId >= 0)
            {
                RTSAgent obj = Agent.GetCommander().GetObjectForId(loadedProjectId);
                if (obj.MyAgentType == AgentType.Building)
                {
                    currentProject = obj;
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
                if (currentProject.IsNull() && ConstructQueue.Count > 0 && !IsFocused)
                {
                    StartConstructQueue();
                }
                else
                {
                    BehaveWithTarget();
                }

                if (canMove)
                {
                    if (IsBuildMoving)
                    {
                        cachedMove.StartLookingForStopPause();
                    }
                }
            }
        }

        private void HandleStartMove()
        {
            currentAmountBuilt = 0;

            if (!IsBuildMoving && IsFocused)
            {
                StopConstruction();
            }
        }

        private void HandleMoveGroupProcessed()
        {
            Destination = cachedMove.Destination;
        }

        private void HandleOnArrive()
        {
            if (IsBuildMoving)
            {
                IsBuildMoving = false;
            }
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        private void BehaveWithTarget()
        {
            if (currentProject.IsNull()
                || currentProject.IsActive == false
                || !projectStructure.NeedsConstruction)
            {
                //Target's lifecycle has ended
                StopConstruction();
            }
            else
            {
                Vector2d targetDirection = currentProject.Body.Position - cachedBody.Position;
                long fastMag = targetDirection.FastMagnitude();

                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        if (!inRange)
                        {
                            if (canMove)
                            {
                                cachedMove.StopMove();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(ConstructingAnimState);

                        if (!projectStructure.ConstructionStarted)
                        {
                            projectStructure.ConstructionStarted = true;
                            // Restore material
                            ConstructionHandler.RestoreMaterial(currentProject.gameObject);
                        }

                        long mag;
                        targetDirection.Normalize(out mag);
                        bool withinTurn = cachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          cachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && cachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= cachedAttack.AttackAngle);
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
                        if (canMove)
                        {
                            cachedMove.PauseAutoStop();
                            cachedMove.PauseCollisionStop();
                            if (!cachedMove.IsMoving)
                            {
                                cachedMove.StartMove(currentProject.Body.Position);
                                cachedBody.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    cachedMove.Destination = currentProject.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (currentProject.Body.PositionChangedBuffer &&
                                            currentProject.Body.Position.FastDistance(cachedMove.Destination.x, cachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            cachedMove.StartMove(currentProject.Body.Position);
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
                }

                if (IsWindingUp)
                {
                    //TODO: Do we need AgentConditional checks here?
                    windupCount += LockstepManager.DeltaTime;
                    if (canTurn)
                    {
                        Vector2d targetVector = currentProject.Body.Position - cachedBody.Position;
                        cachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= Windup)
                    {
                        windupCount = 0;
                        Build();
                        while (constructCount >= _constructInterval)
                        {
                            //resetting back down after attack is fired
                            constructCount -= (_constructInterval);
                        }
                        constructCount += Windup;
                        IsWindingUp = false;
                    }
                }
                else
                {
                    windupCount = 0;
                }

                if (canMove && inRange)
                {
                    cachedMove.PauseAutoStop();
                    cachedMove.PauseCollisionStop();
                }
            }
        }

        private void Build()
        {
            if (canMove)
            {
                // we don't want to be able to fire and move!
                cachedMove.StopMove();
            }
            cachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            if (projectStructure.NeedsConstruction)
            {
                IsFocused = true;
                IsBuildMoving = false;
                IsCasting = true;

                if (!CheckRange())
                {
                    if (canMove)
                    {
                        cachedMove.StartMove(currentProject.Body.Position);
                    }
                }

                projectStructure.Construct(constructAmount);

                //if (audioElement != null)
                //{
                //    audioElement.Play(finishedJobSound);
                //}
            }
            else
            {
                // what are we building for then?
                StopConstruction();
            }
        }

        private bool CheckRange()
        {
            Vector2d targetDirection = currentProject.Body.Position - cachedBody.Position;
            long fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        public void StartConstructQueue()
        {
            bool projectSet = false;
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

                    if (GridBuilder.Place(newRTSAgent.GetAbility<Structure>(), newRTSAgent.Body.Position))
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

                        if (currentProject.IsNull())
                        {
                            //Set the current project is we don't have one
                            currentProject = newRTSAgent;
                            projectSet = true;
                        }
                    }
                    else
                    {
                        Debug.Log("Couldn't place building!");
                        newRTSAgent.Die();
                    }
                }
            }

            if (projectSet)
            {
                StartConstructMove();
            }
        }

        public void StopConstruction(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (complete)
            {
                IsBuildMoving = false;
                Agent.Tag = AgentTag.None;
                ConstructQueue.Clear();
            }
            else
            {
                if (IsBuildMoving)
                {
                    cachedMove.StartMove(Destination);
                }
                else if (canMove && currentProject.IsNotNull() && !inRange)
                {
                    cachedMove.StopMove();
                }
            }

            currentProject = null;
            cachedBody.Priority = basePriority;

            IsCasting = false;
        }

        protected override void OnDeactivate()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
        }

        public virtual void StartConstructMove(bool isFormal = true)
        {
            Agent.StopCast(this.ID);
            Agent.Tag = AgentTag.Builder;

            //if formal (going through normal Execute routes), do the group stuff
            //if (isFormal)
            //{
            //    if (currentProject.IsNotNull())
            //    {
            //        cachedMove.RegisterGroup(false);
            //    }
            //    else
            //    {
            //        cachedMove.RegisterGroup();
            //    }
            //}
            //else
            //{
            fastRangeToTarget = cachedAttack.Range + (currentProject.Body.IsNotNull() ? currentProject.Body.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;

            if (currentProject.IsNotNull())
            {
                if (canMove)
                {
                    cachedMove.StartMove(currentProject.Body.Position);
                }
            }
            //}

            IsBuildMoving = true;
            IsFocused = false;
        }

        protected override void OnExecute(Command com)
        {
            //first check if queue command
            if (com.TryGetData(out QueueStructure qStructure))
            {
                ConstructQueue.Enqueue(qStructure.Value);
            }
            else
            {
                if (com.TryGetData(out DefaultData target))
                {
                    // construction hasn't started yet, only a bool given 
                    if (target.Is(DataType.Bool) && ConstructQueue.Count > 0)
                    {
                        if ((bool)target.Value)
                        {
                            StartConstructQueue();
                        }
                        else
                        {
                            // An event triggered to clear agents construction queue
                            ConstructQueue.Clear();
                        }
                    }
                    // otherwise this is another agent coming to help
                    // should have been sent local id of target
                    else if (target.Is(DataType.UShort))
                    {
                        ushort targetValue = (ushort)target.Value;
                        if (AgentController.TryGetAgentInstance(targetValue, out RTSAgent tempTarget))
                        {
                            if (tempTarget && tempTarget.GetAbility<Structure>().NeedsConstruction)
                            {
                                currentProject = tempTarget;
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
            SaveManager.WriteFloat(writer, "AmountBuilt", currentAmountBuilt);
            SaveManager.WriteBoolean(writer, "BuildMoving", IsBuildMoving);
            if (currentProject)
            {
                SaveManager.WriteInt(writer, "currentProjectId", currentProject.GlobalID);
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
                case "BuildMoving":
                    IsBuildMoving = (bool)readValue;
                    break;
                case "AmountBuilt":
                    currentAmountBuilt = (long)readValue;
                    break;
                case "currentProjectId":
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