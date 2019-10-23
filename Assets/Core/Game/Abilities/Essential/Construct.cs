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

        public ConstructGroup MyConstructGroup;
        [HideInInspector]
        public int MyConstructGroupID;

        //Stuff for the logic
        private bool inRange;
        private Vector2d targetDirection;
        private long fastMag;
        private long fastRangeToTarget;

        public Move CachedMove { get; private set; }
        protected virtual bool canMove { get; private set; }
        private Turn cachedTurn;
        protected bool canTurn { get; private set; }
        private Attack cachedAttack;
        protected LSBody cachedBody { get { return Agent.Body; } }

        private RTSAgent _currentProject;
        public RTSAgent CurrentProject
        {
            get
            {
                if (MyConstructGroup.IsNotNull())
                {
                    return MyConstructGroup.CurrentProject;
                }
                else
                {
                    return _currentProject;
                }
            }
            set
            {
                if (MyConstructGroup.IsNotNull())
                {
                    MyConstructGroup.CurrentProject = value;
                }
                else
                {
                    _currentProject = value;
                }
            }
        }
        private Structure _projectStructure
        {
            get
            {
                return CurrentProject.IsNotNull() ? CurrentProject.GetAbility<Structure>() : null;
            }
        }
        public bool IsBuildMoving { get; private set; }

        private int basePriority;
        private uint targetVersion;
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

        //Called whenever construction is stopped... i.e. to attack
        public event Action OnStopConstruct;

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
            CachedMove = Agent.GetAbility<Move>();

            basePriority = cachedBody.Priority;
            canMove = CachedMove.IsNotNull();

            if (canMove)
            {
                CachedMove.onStartMove += HandleStartMove;
                CachedMove.onArrive += HandleOnArrive;
            }

            canTurn = cachedTurn.IsNotNull();

            cachedAttack = Agent.GetAbility<Attack>();
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            constructCount = 0;
            CurrentProject = null;

            IsBuildMoving = false;

            MyConstructGroupID = -1;

            inRange = false;
            IsFocused = false;

            repathTimer.Reset(repathInterval);
            repathRandom = LSUtility.GetRandom(repathInterval);

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

                if (Agent && Agent.IsActive)
                {
                    if (CurrentProject.IsNotNull() && (IsFocused || IsBuildMoving))
                    {
                        BehaveWithTarget();
                    }
                    else if (IsBuildMoving)
                    {
                        if (canMove && CachedMove.IsMoving)
                        {
                            // we shouldn't be moving then!
                            CachedMove.StopMove();
                            IsBuildMoving = false;
                        }
                    }
                }

                if (canMove && IsBuildMoving)
                {
                    CachedMove.StartLookingForStopPause();
                }
            }
        }

        private void HandleStartMove()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                currentAmountBuilt = 0;
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

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        private void BehaveWithTarget()
        {
            // only stop construct when groups queue is empty
            if (CurrentProject.IsActive == false
                || CurrentProject.SpawnVersion != targetVersion
                || !_projectStructure.NeedsConstruction && MyConstructGroup.ConstructionQueue.Count == 0)
            {
                //Target's lifecycle has ended
                StopConstruction();
            }
            else
            {
                if (!IsWindingUp)
                {
                    if (CheckRange())
                    {
                        if (!inRange)
                        {
                            if (canMove)
                            {
                                CachedMove.Arrive();
                            }

                            inRange = true;
                        }
                        Agent.Animator.SetState(ConstructingAnimState);

                        if (!_projectStructure.ConstructionStarted)
                        {
                            _projectStructure.ConstructionStarted = true;
                            // Restore material
                            ConstructionHandler.RestoreMaterial(CurrentProject.gameObject);
                        }

                        targetDirection.Normalize(out long mag);
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
                            CachedMove.PauseAutoStop();
                            CachedMove.PauseCollisionStop();
                            if (!CachedMove.IsMoving
                                && !CachedMove.MoveOnGroupProcessed)
                            {
                                StartConstructMove();
                                cachedBody.Priority = basePriority;
                            }
                            else
                            {
                                if (inRange)
                                {
                                    CachedMove.Destination = CurrentProject.Body.Position;
                                }
                                else
                                {
                                    if (repathTimer.AdvanceFrame())
                                    {
                                        if (CurrentProject.Body.PositionChangedBuffer &&
                                            CurrentProject.Body.Position.FastDistance(CachedMove.Destination.x, CachedMove.Destination.y) >= (repathDistance * repathDistance))
                                        {
                                            StartConstructMove();
                                            //So units don't sync up and path on the same frame
                                            repathTimer.AdvanceFrames(repathRandom);
                                        }
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
                    if (canTurn)
                    {
                        Vector2d targetVector = CurrentProject.Body.Position - cachedBody.Position;
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
                    CachedMove.PauseAutoStop();
                    CachedMove.PauseCollisionStop();
                }
            }
        }

        private void Build()
        {
            if (canMove)
            {
                // we don't want to be able to fire and move!
                CachedMove.StopMove();
            }
            cachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            if (_projectStructure.NeedsConstruction)
            {
                _projectStructure.Construct(constructAmount);

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
            targetDirection = CurrentProject.Body.Position - cachedBody.Position;
            fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        public void OnConstructGroupProcessed()
        {
            Agent.Tag = AgentTag.Builder;

            IsFocused = true;
            IsBuildMoving = false;

            targetVersion = CurrentProject.SpawnVersion;
            IsCasting = true;

            fastRangeToTarget = cachedAttack.Range + (CurrentProject.Body.IsNotNull() ? CurrentProject.Body.Radius : 0) + Agent.Body.Radius;
            fastRangeToTarget *= fastRangeToTarget;
        }

        public virtual void StartConstructMove()
        {
            if (canMove && !CheckRange())
            {
                if (CurrentProject.IsNotNull())
                {
                    // position is set by the movement group tied to construct group
                    CachedMove.StartMove(CachedMove.Destination);
                }

                IsBuildMoving = true;
                IsFocused = false;
            }
        }

        protected override void OnExecute(Command com)
        {
            // flag sent to signal agent to register with group
            if (com.TryGetData(out DefaultData target, 0)
                && target.Is(DataType.Bool)
                && (bool)target.Value)
            {
                Agent.StopCast(ID);
                RegisterConstructGroup();
            }
        }

        public void RegisterConstructGroup()
        {
            if (ConstructionGroupHelper.CheckValidAndAlert())
            {
                ConstructionGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        protected override void OnDeactivate()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
        }

        public void StopConstruction(bool complete = false)
        {
            inRange = false;
            IsWindingUp = false;
            IsFocused = false;

            if (MyConstructGroup.IsNotNull())
            {
                if (MyConstructGroup.ConstructionQueue.Count == 0 || complete)
                {
                    MyConstructGroup.Remove(this);
                }
            }

            if (complete)
            {
                IsBuildMoving = false;
                Agent.Tag = AgentTag.None;
            }
            else
            {
                if (CurrentProject.IsNotNull())
                {
                    if (IsBuildMoving)
                    {
                        CachedMove.StartMove(CurrentProject.Body.Position);
                    }
                    else if (canMove && !inRange)
                    {
                        CachedMove.StopMove();
                    }
                }
            }

            cachedBody.Priority = basePriority;

            IsCasting = false;

            OnStopConstruct?.Invoke();
        }

        protected sealed override void OnStopCast()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
        }

        public string[] GetBuildActions()
        {
            return this._buildActions;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            SaveDetails(writer);
            SaveManager.WriteFloat(writer, "AmountBuilt", currentAmountBuilt);
            SaveManager.WriteBoolean(writer, "BuildMoving", IsBuildMoving);
            if (CurrentProject)
            {
                SaveManager.WriteInt(writer, "currentProjectId", CurrentProject.GlobalID);
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