using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using RTSLockstep.Agents;
using RTSLockstep.LSResources;
using RTSLockstep.Utility;

namespace RTSLockstep.Player
{
    public class PlayerResourceManager : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        public BaseResources CurrentResources;

        private LSPlayer _cachedPlayer;
        private Dictionary<EnvironmentResourceType, long> _resources;
        private Dictionary<EnvironmentResourceType, long> _resourceLimits;
        #endregion

        #region Behavior
        // Use this for initialization
        public void OnSetup()
        {
            _cachedPlayer = GetComponentInParent<LSPlayer>();
            _resources = InitResourceList();
            _resourceLimits = InitResourceList();
        }

        public void OnInitialize()
        {
            AddStartResourceLimits();
            AddStartResources();
        }

        // Update is called once per frame
        public void OnVisualize()
        {
            _cachedPlayer.PlayerHUD.SetResourceValues(_resources, _resourceLimits);
        }
        #endregion

        #region Public
        public void AddResource(EnvironmentResourceType type, long amount)
        {
            _resources[type] += amount;
        }

        public void IncrementResourceLimit(EnvironmentResourceType type, long amount)
        {
            _resourceLimits[type] += amount;
        }

        public Dictionary<EnvironmentResourceType, long> GetResources()
        {
            return _resources;
        }

        public long GetResourceAmount(EnvironmentResourceType type)
        {
            return _resources[type];
        }

        public Dictionary<EnvironmentResourceType, long> GetResourceLimits()
        {
            return _resourceLimits;
        }

        public long GetResourceLimit(EnvironmentResourceType type)
        {
            return _resourceLimits[type];
        }

        public void RemoveResource(EnvironmentResourceType type, int amount)
        {
            _resources[type] -= amount;
        }

        public void DecrementResourceLimit(EnvironmentResourceType type, int amount)
        {
            _resourceLimits[type] -= amount;
        }

        public bool CheckPlayerResources(LSAgent agent)
        {
            bool validResources = true;
            foreach (KeyValuePair<EnvironmentResourceType, int> entry in agent.resourceCost)
            {
                if (entry.Value > 0)
                {
                    switch (entry.Key.ToString())
                    {
                        case "Provision":
                            if ((entry.Value + GetResourceAmount(entry.Key)) >= GetResourceLimit(entry.Key))
                            {
                                validResources = false;
                                Debug.Log("not enough supplies!");
                            }
                            break;
                        default:
                            if (entry.Value > GetResourceAmount(entry.Key))
                            {
                                validResources = false;
                                Debug.Log("not enough resources!");
                            }
                            break;
                    };
                };
            }

            return validResources;
        }

        public void RemoveResources(LSAgent agent)
        {
            foreach (KeyValuePair<EnvironmentResourceType, int> entry in agent.resourceCost)
            {
                if (entry.Value > 0)
                {
                    switch (entry.Key.ToString())
                    {
                        case "Provision":
                            AddResource(entry.Key, entry.Value);
                            break;
                        default:
                            RemoveResource(entry.Key, entry.Value);
                            break;
                    };
                };
            }
        }
        #endregion

        #region Private
        private Dictionary<EnvironmentResourceType, long> InitResourceList()
        {
            Dictionary<EnvironmentResourceType, long> list = new Dictionary<EnvironmentResourceType, long>
            {
                { EnvironmentResourceType.Gold, 0 },
                { EnvironmentResourceType.Ore, 0 },
                { EnvironmentResourceType.Stone, 0 },
                { EnvironmentResourceType.Wood, 0 },
                { EnvironmentResourceType.Crystal, 0 },
                { EnvironmentResourceType.Food, 0 },
                { EnvironmentResourceType.Provision, 0 }
            };
            return list;
        }

        private void AddStartResourceLimits()
        {
            IncrementResourceLimit(EnvironmentResourceType.Gold, CurrentResources[EnvironmentResourceType.Gold].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Ore, CurrentResources[EnvironmentResourceType.Ore].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Stone, CurrentResources[EnvironmentResourceType.Stone].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Wood, CurrentResources[EnvironmentResourceType.Wood].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Crystal, CurrentResources[EnvironmentResourceType.Crystal].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Food, CurrentResources[EnvironmentResourceType.Food].startLimit);
            IncrementResourceLimit(EnvironmentResourceType.Provision, CurrentResources[EnvironmentResourceType.Provision].startLimit);
        }

        private void AddStartResources()
        {
            AddResource(EnvironmentResourceType.Gold, CurrentResources[EnvironmentResourceType.Gold].startValue);
            AddResource(EnvironmentResourceType.Ore, CurrentResources[EnvironmentResourceType.Ore].startValue);
            AddResource(EnvironmentResourceType.Stone, CurrentResources[EnvironmentResourceType.Stone].startValue);
            AddResource(EnvironmentResourceType.Wood, CurrentResources[EnvironmentResourceType.Wood].startValue);
            AddResource(EnvironmentResourceType.Crystal, CurrentResources[EnvironmentResourceType.Crystal].startValue);
            AddResource(EnvironmentResourceType.Food, CurrentResources[EnvironmentResourceType.Food].startValue);
            AddResource(EnvironmentResourceType.Provision, CurrentResources[EnvironmentResourceType.Provision].startValue);
        }

        public void LoadResources(JsonTextReader reader)
        {
            if (reader.IsNull())
            {
                return;
            }

            string currValue = "";
            while (reader.Read())
            {
                if (reader.Value.IsNotNull())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        currValue = (string)reader.Value;
                    }
                    else
                    {
                        switch (currValue)
                        {
                            case "Gold":
                                CurrentResources[EnvironmentResourceType.Gold].startValue = (int)(long)reader.Value;
                                break;
                            case "Gold_Limit":
                                CurrentResources[EnvironmentResourceType.Gold].startLimit = (int)(long)reader.Value;
                                break;
                            case "Army":
                                CurrentResources[EnvironmentResourceType.Provision].startValue = (int)(long)reader.Value;
                                break;
                            case "Army_Limit":
                                CurrentResources[EnvironmentResourceType.Provision].startLimit = (int)(long)reader.Value;
                                break;
                            case "Ore":
                                CurrentResources[EnvironmentResourceType.Ore].startValue = (int)(long)reader.Value;
                                break;
                            case "Ore_Limit":
                                CurrentResources[EnvironmentResourceType.Ore].startLimit = (int)(long)reader.Value;
                                break;
                            case "Crystal":
                                CurrentResources[EnvironmentResourceType.Crystal].startValue = (int)(long)reader.Value;
                                break;
                            case "Crystal_Limit":
                                CurrentResources[EnvironmentResourceType.Crystal].startLimit = (int)(long)reader.Value;
                                break;
                            case "Wood":
                                CurrentResources[EnvironmentResourceType.Wood].startValue = (int)(long)reader.Value;
                                break;
                            case "Wood_Limit":
                                CurrentResources[EnvironmentResourceType.Wood].startLimit = (int)(long)reader.Value;
                                break;
                            case "Stone":
                                CurrentResources[EnvironmentResourceType.Stone].startValue = (int)(long)reader.Value;
                                break;
                            case "Stone_Limit":
                                CurrentResources[EnvironmentResourceType.Stone].startLimit = (int)(long)reader.Value;
                                break;
                            case "Food":
                                CurrentResources[EnvironmentResourceType.Food].startValue = (int)(long)reader.Value;
                                break;
                            case "Food_Limit":
                                CurrentResources[EnvironmentResourceType.Food].startLimit = (int)(long)reader.Value;
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }
        #endregion
    }
}