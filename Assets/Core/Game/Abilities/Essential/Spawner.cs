using Newtonsoft.Json;
using RTSLockstep.Agents;
using RTSLockstep.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using RTSLockstep.Determinism;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player.Commands;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.Influence;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Integration;

namespace RTSLockstep.Abilities.Essential
{
    [UnityEngine.DisallowMultipleComponent]
    public class Spawner : ActiveAbility
    {
        private Queue<string> spawnQueue;
        private long currentSpawnProgress;

        private Rally cachedRally;

        //Stuff for the logic
        private int basePriority;
        private long spawnCount;

        #region Serialized Values (Further description in properties)
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

        [Lockstep(true)]
        public bool IsWindingUp { get; set; }

        long windupCount;

        protected override void OnSetup()
        {
            spawnQueue = new Queue<string>();

            basePriority = Agent.Body.Priority;
        }

        protected override void OnInitialize()
        {
            basePriority = Agent.Body.Priority;
            spawnCount = 0;
            IsFocused = false;

            cachedRally = Agent.GetAbility<Rally>();
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
                Agent.Animator.SetState(AnimState.Working);
                BehaveWithSpawnQueue();
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

        public float getBuildPercentage()
        {
            return currentSpawnProgress / (float)_maxSpawnProgress;
        }

        public void CreateUnit(string unitName)
        {
            GameObject unit = GameResourceManager.GetAgentTemplate(unitName).gameObject;
            LSAgent unitObject = unit.GetComponent<LSAgent>();
            // check that the Player has the resources available before allowing them to create a new Unit / Building
            if (Agent.GetControllingPlayer() && unitObject)
            {
                if (Agent.GetControllingPlayer().PlayerRawMaterialManager.CheckPlayersRawMaterials(unitObject))
                {
                    Agent.GetControllingPlayer().PlayerRawMaterialManager.RemovePlayersRawMaterials(unitObject);
                    spawnQueue.Enqueue(unitName);
                }
                else
                {
                    //    Debug.Log("not enough resources!");
                }
            }
        }

        protected void BehaveWithSpawnQueue()
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
                if (windupCount >= _windup)
                {
                    windupCount = 0;
                    ProcessSpawnQueue();
                    while (spawnCount >= _spawnInterval)
                    {
                        //resetting back down after attack is fired
                        spawnCount -= (_spawnInterval);
                    }
                    spawnCount += _windup;
                    IsWindingUp = false;
                }
            }
            else
            {
                windupCount = 0;
            }
        }

        protected void ProcessSpawnQueue()
        {
            currentSpawnProgress += spawnIncrement;
            if (currentSpawnProgress > _maxSpawnProgress)
            {
                if (Agent.GetControllingPlayer())
                {
                    //if (audioElement != null)
                    //{
                    //    audioElement.Play(finishedJobSound);
                    //}
                    Vector2d spawnOutside = new Vector2d(transform.position);
                    LSAgent agent = Agent.Controller.CreateAgent(spawnQueue.Dequeue(), spawnOutside);
                    agent.SetProvision(true);

                    if (cachedRally)
                    {
                        if (cachedRally.SpawnPoint != cachedRally.RallyPoint)
                        {
                            Command moveCom = new Command(AbilityDataItem.FindInterfacer("Move").ListenInputID);
                            moveCom.Add(new Vector2d(cachedRally.RallyPoint));
                            moveCom.ControllerID = agent.Controller.ControllerID;
                            moveCom.Add(new Influence(agent));

                            CommandManager.SendCommand(moveCom);
                        }
                    }

                }
                currentSpawnProgress = 0;
            }
        }

        protected virtual AnimState SpawningAnimState
        {
            get { return AnimState.Spawning; }
        }

        protected override void OnExecute(Command com)
        {
            DefaultData action;
            if (com.TryGetData(out action) && action.Is(DataType.String))
            {
                string unit = action.Value.ToString();
                CreateUnit(unit);
            }
        }

        public int GetSpawnQueueCount()
        {
            return spawnQueue.Count;
        }

        public string[] GetSpawnActions()
        {
            return _spawnActions;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            SaveDetails(writer);
            SaveManager.WriteFloat(writer, "SpawnProgress", currentSpawnProgress);
            SaveManager.WriteLong(writer, "SpawnCount", spawnCount);
            SaveManager.WriteStringArray(writer, "SpawnQueue", spawnQueue.ToArray());
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
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