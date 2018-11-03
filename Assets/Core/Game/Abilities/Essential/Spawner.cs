using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Spawner : ActiveAbility
    {
        private Vector3 spawnPoint;
        private Vector3 rallyPoint;
        private FlagState _flagState;
        private Queue<string> spawnQueue;
        private long currentSpawnProgress;
        private LSBody CachedBody { get { return Agent.Body; } }

        //Stuff for the logic
        private int basePriority;
        private long spawnCount;
        public bool IsFocused { get; private set; }

        #region Serialized Values (Further description in properties)
        public Texture2D rallyPointImage;
        [SerializeField]
        private long spawnIncrement = FixedMath.One;
        [SerializeField]
        private long _maxSpawnProgress = FixedMath.One;
        [SerializeField, Tooltip("Enter object names for prefabs this agent can spawn.")]
        private String[] _spawnActions;
        [SerializeField, FixedNumber, Tooltip("Used to determine how fast agent can spawn.")]
        private long _spawnInterval = 1 * FixedMath.One;
        [SerializeField, FixedNumber]
        private long _windup;
        #endregion

        public long Windup { get { return _windup; } }
        [Lockstep(true)]
        public bool IsWindingUp { get; set; }

        long windupCount;

        protected override void OnSetup()
        {
            Agent.OnSelectedChange += HandleSelectedChange;
            spawnQueue = new Queue<string>();

            basePriority = CachedBody.Priority;
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            spawnCount = 0;
            IsFocused = false;

            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;
        }

        protected override void OnSimulate()
        {
            if (spawnCount > _spawnInterval)
            {
                //reset attackCount overcharge if left idle
                spawnCount = _spawnInterval;
            }
            else if (spawnCount < _spawnInterval)
            {
                //charge up attack
                spawnCount += LockstepManager.DeltaTime;
            }

            if (spawnQueue.Count > 0)
            {
                IsCasting = true;
                Agent.SetState(AnimState.Working);
                BehaveWithBuildQueue();
            }
            else
            {
                IsCasting = false;
            }
        }

        void StartWindup()
        {
            windupCount = 0;
            IsWindingUp = true;
        }

        public string[] getBuildQueueValues()
        {
            string[] values = new string[spawnQueue.Count];
            int pos = 0;
            foreach (string unit in spawnQueue)
            {
                values[pos++] = unit;
            }
            return values;
        }

        public void HandleSelectedChange()
        {
            if (Agent.GetCommander() == PlayerManager.MainController.Commander)
            {
                RallyPoint flag = Agent.GetCommander().GetComponentInChildren<RallyPoint>();
                if (Agent.IsSelected)
                {
                    if (flag && spawnPoint != ResourceManager.InvalidPosition.ToVector3() && rallyPoint != ResourceManager.InvalidPosition.ToVector3())
                    {
                        if (_flagState == FlagState.FlagSet)
                        {
                            flag.transform.localPosition = rallyPoint;
                            flag.transform.forward = transform.forward;
                            flag.Enable();
                        }
                        else
                        {
                            flag.transform.localPosition = Agent.Body.Position3d.ToVector3();
                            flag.Disable();
                        }
                    }
                }
                else
                {
                    if (flag)
                    {
                        flag.Disable();
                    }
                }
            }
        }

        public float getBuildPercentage()
        {
            return currentSpawnProgress / (float)_maxSpawnProgress;
        }

        public void CreateUnit(string unitName)
        {
            GameObject unit = ResourceManager.GetAgentTemplate(unitName).gameObject;
            RTSAgent unitObject = unit.GetComponent<RTSAgent>();
            // check that the Player has the resources available before allowing them to create a new Unit / Building
            if (Agent.GetCommander() && unitObject)
            {
                if (Agent.GetCommander().CheckResources(unitObject))
                {
                    Agent.GetCommander().RemoveResources(unitObject);
                    spawnQueue.Enqueue(unitName);
                }
                else
                {
                //    Debug.Log("not enough resources!");
                }
            }
        }

        protected void BehaveWithBuildQueue()
        {

            if (!IsWindingUp)
            {
                if (spawnCount >= _spawnInterval)
                {
                    StartWindup();
                }
            }

            if (IsWindingUp)
            {
                //TODO: Do we need AgentConditional checks here?
                windupCount += LockstepManager.DeltaTime;
                if (windupCount >= Windup)
                {
                    windupCount = 0;
                    ProcessBuildQueue();
                    while (this.spawnCount >= _spawnInterval)
                    {
                        //resetting back down after attack is fired
                        this.spawnCount -= (this._spawnInterval);
                    }
                    this.spawnCount += Windup;
                    IsWindingUp = false;
                }
            }
            else
            {
                windupCount = 0;
            }
        }

        protected void ProcessBuildQueue()
        {
            currentSpawnProgress += spawnIncrement;
            if (currentSpawnProgress > _maxSpawnProgress)
            {
                if (Agent.GetCommander())
                {
                    //if (audioElement != null)
                    //{
                    //    audioElement.Play(finishedJobSound);
                    //}
                    Vector2d spawnOutside = new Vector2d(this.transform.position);
                    RTSAgent agent = Agent.Controller.CreateAgent(spawnQueue.Dequeue(), spawnOutside);
                    RTSAgent newUnit = agent as RTSAgent;
                    if (newUnit && spawnPoint != rallyPoint)
                    {
                        newUnit.SetProvision(true);
                        newUnit.GetAbility<Move>().StartMove(new Vector2d(rallyPoint));
                    }
                }
                currentSpawnProgress = 0;
            }
        }

        protected virtual AnimState SpawningAnimState
        {
            get { return AnimState.Spawning; }
        }

        public void SetRallyPoint(Vector3 position)
        {
            rallyPoint = position;
            if (Agent.GetCommander() && Agent.IsSelected)
            {
                RallyPoint flag = Agent.GetCommander().GetComponentInChildren<RallyPoint>();
                if (flag)
                {
                    if (!flag.ActiveStatus)
                    {
                        flag.Enable();
                    }
                    flag.transform.localPosition = rallyPoint;
                    _flagState = FlagState.FlagSet;
                    Agent.Controller.GetCommanderHUD().SetCursorLock(false);
                    Agent.Controller.GetCommanderHUD().SetCursorState(CursorState.Select);
                }
            }
        }

        public void SetSpawnPoint()
        {
            long spawnX = (long)(Agent.Body.GetSelectionBounds().center.x + transform.forward.x * Agent.Body.GetSelectionBounds().extents.x + transform.forward.x * 10);
            long spawnZ = (long)(Agent.Body.GetSelectionBounds().center.z + transform.forward.z * Agent.Body.GetSelectionBounds().extents.z + transform.forward.z * 10);
            spawnPoint = new Vector3(spawnX, 0, spawnZ);
            rallyPoint = spawnPoint;
        }

        protected override void OnExecute(Command com)
        {
            DefaultData action;
            Vector2d pos;
            if (com.TryGetData<Vector2d>(out pos))
            {
                if (pos.ToVector3d() != ResourceManager.InvalidPosition)
                {
                    SetRallyPoint(pos.ToVector3());
                }
            }
            else if (com.TryGetData<DefaultData>(out action) && action.Is(DataType.String))
            {
                String unit = action.Value.ToString();
                CreateUnit(unit);
            }
        }

        public bool hasSpawnPoint()
        {
            return spawnPoint != ResourceManager.InvalidPosition.ToVector3() && rallyPoint != ResourceManager.InvalidPosition.ToVector3();
        }

        public FlagState GetFlagState()
        {
            return this._flagState;
        }

        public void SetFlagState(FlagState value)
        {
            this._flagState = value;
        }

        public int GetSpawnQueueCount()
        {
            return this.spawnQueue.Count;
        }

        public String[] GetSpawnActions()
        {
            return this._spawnActions;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteVector(writer, "SpawnPoint", spawnPoint);
            SaveManager.WriteVector(writer, "RallyPoint", rallyPoint);
            SaveManager.WriteString(writer, "FlagState", _flagState.ToString());
            SaveManager.WriteFloat(writer, "SpawnProgress", currentSpawnProgress);
            SaveManager.WriteLong(writer, "SpawnCount", spawnCount);
            SaveManager.WriteStringArray(writer, "SpawnQueue", spawnQueue.ToArray());
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "SpawnPoint":
                    spawnPoint = LoadManager.LoadVector(reader);
                    break;
                case "RallyPoint":
                    rallyPoint = LoadManager.LoadVector(reader);
                    break;
                case "FlagState":
                    _flagState = WorkManager.GetFlagState((string)readValue);
                    break;
                case "SpawnProgress":
                    currentSpawnProgress = (long)readValue;
                    break;
                case "SpawnCount":
                    spawnCount = (long)readValue;
                    break;
                case "SpawnQueue":
                    spawnQueue = new Queue<string>(LoadManager.LoadStringArray(reader));
                    break;
                default: break;
            }
        }
    }
}