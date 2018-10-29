using FastCollections;
using Newtonsoft.Json;
using RTSLockstep;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Construct : ActiveAbility
    {
        private const int searchRate = LockstepManager.FrameRate / 2;
        //need a better way to determine time spent...
        private long currentAmountBuilt = 0;

        //Stuff for the logic
        private bool inRange;
        private int basePriority;
        private int searchCount;
        private long fastRangeToTarget;
        private Move cachedMove;
        private Turn cachedTurn;
        private Attack cachedAttack;
        protected LSBody CachedBody { get { return Agent.Body; } }

        private RTSAgent CurrentProject;
        private bool IsBuildMoving;
        public bool IsBuilding { get; private set; }
        public bool IsFocused { get; private set; }
        private int loadedProjectId = -1;

        #region Serialized Values (Further description in properties)
        [FixedNumber, SerializeField, Tooltip("Used to determine how fast agent can build.")]
        private long _buildSpeed = FixedMath.One;
        [SerializeField, Tooltip("Enter object names for prefabs this agent can build.")]
        private String[] _buildActions;
        #endregion

        #region variables for quick fix for repathing to target's new position
        private const long repathDistance = FixedMath.One * 2;
        private FrameTimer repathTimer = new FrameTimer();
        private const int repathInterval = LockstepManager.FrameRate * 2;
        private readonly int repathRandom = 0;
        #endregion

        protected override void OnSetup()
        {
            cachedTurn = Agent.GetAbility<Turn>();
            cachedMove = Agent.GetAbility<Move>();
            cachedAttack = Agent.GetAbility<Attack>();

            cachedMove.onStartMove += HandleStartMove;

            basePriority = CachedBody.Priority;

            // if agent doesn't have tag, set as builder by default
            if(Agent.Tag == AgentTag.None)
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
                BehaveWithNoTarget();
            }
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            searchCount = LSUtility.GetRandom(searchRate) + 1;
            CurrentProject = null;
            IsBuilding = false;
            IsBuildMoving = false;
            inRange = false;
            IsFocused = false;
            //    this.Destination = new Vector2d(0, 0);

            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;

            if ((Agent as RTSAgent).GetCommander() && loadedSavedValues && loadedProjectId >= 0)
            {
                RTSAgent obj = (Agent as RTSAgent).GetCommander().GetObjectForId(loadedProjectId);
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
                if (IsBuilding)
                {
                    BehaveWithTarget();
                }
                else
                {
                    BehaveWithNoTarget();
                }

                if (IsBuildMoving)
                {
                    cachedMove.StartLookingForStopPause();
                }
            }
        }

        void BehaveWithTarget()
        {
            if (CurrentProject && (CurrentProject.IsActive == false || !CurrentProject.GetAbility<Structure>().UnderConstruction()))
            {
                //Target's lifecycle has ended
                StopBuilding();
                BehaveWithNoTarget();
                return;
            }

            Vector2d targetDirection = CurrentProject.Body._position - CachedBody._position;
            long fastMag = targetDirection.FastMagnitude();

            if (CheckRange())
            {
                IsBuildMoving = false;
                if (!inRange)
                {
                    cachedMove.StopMove();
                    inRange = true;
                }
                Agent.SetState(ConstructingAnimState);

                //Use fixed math?
                currentAmountBuilt += Mathf.RoundToInt((_buildSpeed * Time.deltaTime) + 1);
                long amountBuilt = currentAmountBuilt;
                if (amountBuilt > 0)
                {
                    currentAmountBuilt -= amountBuilt;
                    CurrentProject.GetAbility<Structure>().Construct(amountBuilt);
                }
                if (!CurrentProject.GetAbility<Structure>().UnderConstruction())
                {
                    //if (audioElement != null)
                    //{
                    //    audioElement.Play(finishedJobSound);
                    //}
                    StopBuilding();
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

                if (IsBuildMoving || IsFocused == false)
                {
                    searchCount -= 1;
                    if (searchCount <= 0)
                    {
                        searchCount = searchRate;
                        //if (ScanAndEngage())
                        //{
                        //}
                        //else
                        //{
                        //}
                    }
                }
                if (inRange == true)
                {
                    inRange = false;
                }
            }
            if (inRange)
            {
                cachedMove.PauseAutoStop();
                cachedMove.PauseCollisionStop();
            }
        }

        void BehaveWithNoTarget()
        {
            if (IsBuildMoving)
            {
                if (IsBuildMoving)
                {
                    searchCount -= 8;
                }
                else
                {
                    searchCount -= 2;
                }

                if (searchCount <= 0)
                {
                    searchCount = searchRate;
                    //if (ScanAndEngage())
                    //{
                    //}
                }
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

        public void SetBuilding(RTSAgent project)
        {
            if (project != Agent && project != null)
            {
                Agent.Tag = AgentTag.Builder;

                CurrentProject = project;
                IsBuilding = true;
                IsCasting = true;
                fastRangeToTarget = cachedAttack.Range + (CurrentProject.Body.IsNotNull() ? CurrentProject.Body.Radius : 0) + Agent.Body.Radius;
                fastRangeToTarget *= fastRangeToTarget;

                if (!CheckRange())
                {
                    StartBuildMove(CurrentProject.Body.Position);
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
            if (com.TryGetData<DefaultData>(out target) && target.Is(DataType.UShort))
            {
                IsFocused = true;
                IsBuildMoving = false;
                LSAgent tempTarget;
                ushort targetValue = (ushort)target.Value;
                if (AgentController.TryGetAgentInstance(targetValue, out tempTarget))
                {
                    RTSAgent building = (RTSAgent)tempTarget;
                    if (building && building.GetAbility<Structure>().UnderConstruction())
                    {
                        SetBuilding(building);
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
            SaveManager.WriteInt(writer, "SearchCount", searchCount);
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
                case "SearchCount":
                    searchCount = (int)readValue;
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