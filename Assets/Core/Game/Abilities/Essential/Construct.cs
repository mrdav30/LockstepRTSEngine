using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Construct : ActiveAbility
    {
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
        public bool IsFocused { get; private set; }

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

        long windupCount;

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

            // if agent doesn't have tag, set as builder by default
            if (Agent.Tag == AgentTag.None)
            {
                Agent.Tag = AgentTag.Builder;
            }
        }

        private void HandleStartMove()
        {
            currentAmountBuilt = 0;

            if (!IsBuildMoving && IsBuilding)
            {
                StopBuilding();
            }
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

        void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        void BehaveWithTarget()
        {
            if (CurrentProject && (CurrentProject.IsActive == false || !CurrentProject.GetAbility<Structure>().UnderConstruction()))
            {
                //Target's lifecycle has ended
                StopBuilding();
                return;
            }

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
                    Agent.SetState(ConstructingAnimState);


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

        void Build()
        {
            cachedMove.StopMove();
            CachedBody.Priority = _increasePriority ? basePriority + 1 : basePriority;

            CurrentProject.GetAbility<Structure>().Construct(constructAmount);

            if (!CurrentProject.GetAbility<Structure>().UnderConstruction())
            {
                //if (audioElement != null)
                //{
                //    audioElement.Play(finishedJobSound);
                //}
                StopBuilding();
            }
        }

        bool CheckRange()
        {
            Vector2d targetDirection = CurrentProject.Body._position - CachedBody._position;
            long fastMag = targetDirection.FastMagnitude();

            return fastMag <= fastRangeToTarget;
        }

        protected virtual AnimState ConstructingAnimState
        {
            get { return AnimState.Constructing; }
        }

        public void SetBuilding(string agentCode, Vector2d targetPOS, Vector2d targetRotation, Vector3d targetLocalScale)
        {
            if (agentCode != null)
            {
                RTSAgent newBuilding = Agent.Controller.CreateAgent(agentCode, targetPOS, targetRotation) as RTSAgent;

                if (GridBuilder.Place(newBuilding.GetAbility<Structure>(), newBuilding.Body.Position))
                {
                    Agent.GetCommander().CachedResourceManager.RemoveResources(newBuilding);
                    newBuilding.SetState(AnimState.Building);
                    newBuilding.SetPlayingArea(Agent.GetPlayerArea());
                    newBuilding.GetAbility<Health>().HealthAmount = FixedMath.Create(0);
                    newBuilding.SetCommander(Agent.GetCommander());

                    newBuilding.gameObject.name = agentCode;
                    newBuilding.transform.parent = ConstructionHandler.OrganizerStructures.transform;

                    if (newBuilding.Tag == AgentTag.Wall)
                    {
                        newBuilding.transform.localScale = targetLocalScale.ToVector3();
                        newBuilding.GetAbility<Structure>().IsOverlay = true;
                    }

                    newBuilding.Body.CalculateBounds();
                    newBuilding.GetAbility<Structure>().BuildSizeLow = (int)Math.Ceiling(newBuilding.Body.GetSelectionBounds().size.x);
                    newBuilding.GetAbility<Structure>().BuildSizeHigh = (int)Math.Ceiling(newBuilding.Body.GetSelectionBounds().size.z);

                    newBuilding.GetAbility<Structure>().StartConstruction();

                    Agent.Tag = AgentTag.Builder;

                    CurrentProject = newBuilding;
                    IsBuilding = true;
                    IsCasting = true;
                    fastRangeToTarget = cachedAttack.Range + (CurrentProject.Body.IsNotNull() ? CurrentProject.Body.Radius : 0) + Agent.Body.Radius;
                    fastRangeToTarget *= fastRangeToTarget;

                    if (!CheckRange())
                    {
                        StartBuildMove(CurrentProject.Body.Position);
                    }
                }
                else
                {
                    Debug.Log("Couldn't place building!");
                }
            }
        }

        public void StopBuilding(bool complete = false)
        {
            inRange = false;
            IsFocused = false;
            if (complete)
            {
                IsBuildMoving = false;
            }
            else
            {
                if (IsBuildMoving)
                {
                    cachedMove.StartMove(this.CurrentProject.Body.Position);
                }
                else
                {
                    if (CurrentProject != null && inRange == false)
                    {
                        cachedMove.StopMove();
                    }
                }
            }

            CurrentProject = null;
            CachedBody.Priority = basePriority;

            IsCasting = false;
            IsBuilding = false;
        }

        protected override void OnDeactivate()
        {
            StopBuilding(true);
        }

        public virtual void StartBuildMove(Vector2d destination)
        {
            Agent.StopCast(this.ID);

            IsBuildMoving = true;
            //send move command
            cachedMove.StartMove(destination);
        }

        protected override void OnExecute(Command com)
        {
            DefaultData target;
            Vector2d targetPOS;
            Vector2d targetRotation;
            Vector3d targetLocalScale;
            com.TryGetData<DefaultData>(out target);
            if (target != null)
            {
                com.TryGetData<Vector2d>(out targetPOS, 0);
                com.TryGetData<Vector2d>(out targetRotation, 1);
                com.TryGetData<Vector3d>(out targetLocalScale, 2);

                IsFocused = true;
                IsBuildMoving = false;
                SetBuilding((string)target.Value, targetPOS, targetRotation, targetLocalScale);
            }
        }

        protected sealed override void OnStopCast()
        {
            StopBuilding(true);
        }

        public String[] GetBuildActions()
        {
            return this._buildActions;
        }

        public void SetCurrentProject(RTSAgent agent)
        {
            CurrentProject = agent;
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