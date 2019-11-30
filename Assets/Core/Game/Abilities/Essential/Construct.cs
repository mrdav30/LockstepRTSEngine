using Newtonsoft.Json;
using RTSLockstep.Data;
using RTSLockstep.Grid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Construct : ActiveAbility
    {
        #region Properties
        public ConstructGroup MyConstructGroup;
        [HideInInspector]
        public int MyConstructGroupID;

        public Move CachedMove { get; private set; }

        public bool IsBuildMoving { get; private set; }

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

        public string[] BuildActions
        {
            get { return _buildActions; }
        }

        //Called whenever construction is stopped... i.e. to attack
        public event Action OnStopConstruct;

        private const int searchRate = LockstepManager.FrameRate / 2;
        private long currentAmountBuilt = 0;

        private bool canMove;
        private Turn cachedTurn;
        private bool canTurn;
        private Attack cachedAttack;
        private LSBody _cachedBody { get { return Agent.Body; } }

        //Stuff for the logic
        private bool inRange;
        private Vector2d targetDirection;
        private long fastMag;
        private long fastRangeToTarget;

        private int basePriority;
        private uint targetVersion;
        private long constructCount;

        private int loadedProjectId = -1;

        [Lockstep(true)]
        private bool IsWindingUp { get; set; }
        private long windupCount;

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

        #region Serialized Values (Further description in properties)
        [SerializeField, FixedNumber]
        private long constructAmount = FixedMath.One;
        [SerializeField, FixedNumber, Tooltip("Used to determine how fast agent can build.")]
        private long _constructInterval = 1 * FixedMath.One;
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
            CachedMove = Agent.GetAbility<Move>();
            cachedTurn = Agent.GetAbility<Turn>();

            basePriority = _cachedBody.Priority;
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

        protected virtual void OnStartConstructMove()
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

        protected virtual void OnStartWindup()
        {

        }

        protected virtual void OnConstruct(Structure target)
        {
            if (target.NeedsConstruction)
            {
                target.BuildUp(constructAmount);

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

        protected override void OnDeactivate()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
        }

        protected sealed override void OnStopCast()
        {
            if (Agent.Tag == AgentTag.Builder)
            {
                StopConstruction(true);
            }
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

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
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

        private void RegisterConstructGroup()
        {
            if (ConstructionGroupHelper.CheckValidAndAlert())
            {
                ConstructionGroupHelper.LastCreatedGroup.Add(this);
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

        private void BehaveWithTarget()
        {
            // only stop construct when groups queue is empty
            if (CurrentProject.IsActive == false
                || CurrentProject.SpawnVersion != targetVersion
                || !_projectStructure.NeedsConstruction && MyConstructGroup.ConstructionQueue.Count == 0)
            {
                // Target's lifecycle has ended
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

                            if (CurrentProject.Animator.IsNotNull())
                            {
                                CurrentProject.Animator.SetState(AnimState.Building);
                            }

                            // Restore material
                            ConstructionHandler.RestoreMaterial(CurrentProject.gameObject);

                            // restore bounds so structure is included in path & build grid
                            if (CurrentProject.GetAbility<DynamicBlocker>())
                            {
                                CurrentProject.GetAbility<DynamicBlocker>().SetTransparent(false);
                            }
                        }

                        targetDirection.Normalize(out long mag);
                        bool withinTurn = cachedAttack.TrackAttackAngle == false ||
                                          (fastMag != 0 &&
                                          _cachedBody.Forward.Dot(targetDirection.x, targetDirection.y) > 0
                                          && _cachedBody.Forward.Cross(targetDirection.x, targetDirection.y).Abs() <= cachedAttack.AttackAngle);

                        bool needTurn = mag != 0 && !withinTurn;
                        if (needTurn && canTurn)
                        {
                            cachedTurn.StartTurnDirection(targetDirection);
                        }
                        else if (constructCount >= _constructInterval)
                        {
                            StartWindup();
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
                                OnStartConstructMove();
                                _cachedBody.Priority = basePriority;
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
                                            OnStartConstructMove();
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
                        Vector2d targetVector = CurrentProject.Body.Position - _cachedBody.Position;
                        cachedTurn.StartTurnVector(targetVector);
                    }

                    if (windupCount >= _windup)
                    {
                        windupCount = 0;
                        StartConstruction();
                        while (constructCount >= _constructInterval)
                        {
                            //resetting back down after attack is fired
                            constructCount -= (_constructInterval);
                        }
                        constructCount += _windup;
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

        private bool CheckRange()
        {
            targetDirection = CurrentProject.Body.Position - _cachedBody.Position;
            fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        private void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
            OnStartWindup();
        }

        private void StartConstruction()
        {
            if (canMove)
            {
                // we don't want to be able to construct and move!
                CachedMove.StopMove();
            }
            _cachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            OnConstruct(_projectStructure);
        }

        private void StopConstruction(bool complete = false)
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
            else if (CurrentProject.IsNotNull())
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

            _cachedBody.Priority = basePriority;

            IsCasting = false;

            OnStopConstruct?.Invoke();
        }
    }
}