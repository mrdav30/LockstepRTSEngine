using Newtonsoft.Json;
using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Spawner : ActiveAbility
    {
        private Vector3d spawnPoint;
        private Vector3d rallyPoint;
        private FlagState _flagState;
        private Queue<string> buildQueue;
        private long currentSpawnProgress = 0;
        private LSBody CachedBody { get { return Agent.Body; } }

        #region Serialized Values (Further description in properties)
        [SerializeField, Tooltip("Used to determine how fast agent can spawn.")]
        private int _maxBuildProgress;         // By adjusting, make some buildings take longer to complete their work than others 
        public Texture2D rallyPointImage;
        [SerializeField, Tooltip("Enter object names for prefabs this agent can spawn.")]
        private String[] _spawnActions;
        #endregion

        protected override void OnSetup()
        {
            Agent.onSelectedChange += HandleSelectedChange;
            buildQueue = new Queue<string>();
        }

        protected override void OnInitialize()
        {
            //caching parameters
            var spawnVersion = Agent.SpawnVersion;
            var controller = Agent.Controller;
        }

        protected override void OnSimulate()
        {
            ProcessBuildQueue();
        }

        public string[] getBuildQueueValues()
        {
            string[] values = new string[buildQueue.Count];
            int pos = 0;
            foreach (string unit in buildQueue)
            {
                values[pos++] = unit;
            }
            return values;
        }

        public void HandleSelectedChange()
        {
            if (Agent.Controller.Commander == PlayerManager.MainController.Commander)
            {
                RallyPoint flag = PlayerManager.MainController.Commander.GetComponentInChildren<RallyPoint>();
                if (Agent.IsSelected)
                {
                    if (flag && spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition)
                    {
                        if (_flagState == FlagState.FlagSet)
                        {
                            flag.transform.localPosition = rallyPoint.ToVector3();
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
            return (float)currentSpawnProgress / (float)_maxBuildProgress;
        }

        public void CreateUnit(string unitName)
        {
            GameObject unit = ResourceManager.GetAgentTemplate(unitName).gameObject;
            RTSAgent unitObject = unit.GetComponent<RTSAgent>();
            // check that the Player has the resources available before allowing them to create a new Unit / Building
            if (PlayerManager.MainController.Commander && unitObject)
            {
                PlayerManager.MainController.Commander.RemoveResource(ResourceType.Gold, unitObject.cost);
            }
            buildQueue.Enqueue(unitName);
        }

        protected void ProcessBuildQueue()
        {
            if (buildQueue.Count > 0)
            {
                currentSpawnProgress += Mathf.RoundToInt(ResourceManager.BuildSpeed + Time.deltaTime);
                if (currentSpawnProgress > _maxBuildProgress)
                {
                    if (PlayerManager.MainController.Commander)
                    {
                        //if (audioElement != null)
                        //{
                        //    audioElement.Play(finishedJobSound);
                        //}
                        Vector2d spawnOutside = new Vector2d(this.transform.position);
                        LSAgent agent = PlayerManager.MainController.CreateAgent(buildQueue.Dequeue(), spawnOutside);
                        RTSAgent newUnit = agent.GetComponent<RTSAgent>();
                        if (newUnit && spawnPoint != rallyPoint)
                        {
                            newUnit.GetAbility<Move>().StartMove(rallyPoint.ToVector2d());
                        }
                    }
                    currentSpawnProgress = 0;
                }
            }
        }

        protected virtual AnimState SpawningAnimState
        {
            get { return AnimState.Spawning; }
        }

        public void SetRallyPoint(Vector3d position)
        {
            rallyPoint = position;
            if (PlayerManager.MainController.Commander && Agent.IsSelected)
            {
                RallyPoint flag = PlayerManager.MainController.Commander.GetComponentInChildren<RallyPoint>();
                if (flag)
                {
                    if (!flag.ActiveStatus)
                    {
                        flag.Enable();
                    }
                    flag.transform.localPosition = rallyPoint.ToVector3();
                    _flagState = FlagState.FlagSet;
                }
            }
        }

        public void SetSpawnPoint()
        {
            long spawnX = (long)(Agent.Body.GetSelectionBounds().center.x + transform.forward.x * Agent.Body.GetSelectionBounds().extents.x + transform.forward.x * 10);
            long spawnZ = (long)(Agent.Body.GetSelectionBounds().center.z + transform.forward.z * Agent.Body.GetSelectionBounds().extents.z + transform.forward.z * 10);
            spawnPoint = new Vector3d(spawnX, 0, spawnZ);
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
                    SetRallyPoint(pos.ToVector3d());
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
            return spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition;
        }

        public FlagState GetFlagState()
        {
            return this._flagState;
        }

        public void SetFlagState(FlagState value)
        {
            this._flagState = value;
        }

        public String[] GetSpawnActions()
        {
            return this._spawnActions;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteVector3d(writer, "SpawnPoint", spawnPoint);
            SaveManager.WriteVector3d(writer, "RallyPoint", rallyPoint);
            SaveManager.WriteString(writer, "FlagState", _flagState.ToString());
            SaveManager.WriteFloat(writer, "BuildProgress", currentSpawnProgress);
            SaveManager.WriteStringArray(writer, "BuildQueue", buildQueue.ToArray());
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "SpawnPoint":
                    spawnPoint = LoadManager.LoadVector3d(reader);
                    break;
                case "RallyPoint":
                    rallyPoint = LoadManager.LoadVector3d(reader);
                    break;
                case "FlagState":
                    _flagState = WorkManager.GetFlagState((string)readValue);
                    break;
                case "BuildProgress":
                    currentSpawnProgress = (long)readValue;
                    break;
                case "BuildQueue":
                    buildQueue = new Queue<string>(LoadManager.LoadStringArray(reader));
                    break;
                default: break;
            }
        }
    }
}