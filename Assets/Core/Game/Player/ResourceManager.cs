using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using RTSLockstep.Agents;
using RTSLockstep.LSResources;
using RTSLockstep.Utility;

namespace RTSLockstep.Player
{
    public class ResourceManager : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private BaseResources _startingResources = new BaseResources
    {
        {ResourceType.Gold, null},
        {ResourceType.Ore, null},
        {ResourceType.Stone, null},
        {ResourceType.Wood, null},
        {ResourceType.Food, null},
        {ResourceType.Crystal, null},
        {ResourceType.Provision, null}
    };

        private LSPlayer _cachedPlayer;
        private Dictionary<ResourceType, long> _resources;
        private Dictionary<ResourceType, long> _resourceLimits;
        #endregion

        #region Behavior
        // Use this for initialization
        public void Setup()
        {
            _cachedPlayer = GetComponentInParent<LSPlayer>();
            _resources = InitResourceList();
            _resourceLimits = InitResourceList();
        }

        public void Initialize()
        {
            AddStartResourceLimits();
            AddStartResources();
        }

        // Update is called once per frame
        public void Visualize()
        {
            _cachedPlayer.PlayerHUD.SetResourceValues(_resources, _resourceLimits);
        }
        #endregion

        #region Public
        public void AddResource(ResourceType type, long amount)
        {
            _resources[type] += amount;
        }

        public void IncrementResourceLimit(ResourceType type, long amount)
        {
            _resourceLimits[type] += amount;
        }

        public Dictionary<ResourceType, long> GetResources()
        {
            return _resources;
        }

        public long GetResourceAmount(ResourceType type)
        {
            return _resources[type];
        }

        public Dictionary<ResourceType, long> GetResourceLimits()
        {
            return _resourceLimits;
        }

        public long GetResourceLimit(ResourceType type)
        {
            return _resourceLimits[type];
        }

        public void RemoveResource(ResourceType type, int amount)
        {
            _resources[type] -= amount;
        }

        public void DecrementResourceLimit(ResourceType type, int amount)
        {
            _resourceLimits[type] -= amount;
        }

        public bool CheckResources(LSAgent agent)
        {
            bool validResources = true;
            foreach (KeyValuePair<ResourceType, int> entry in agent.resourceCost)
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
                                Debug.Log("not enough _resources!");
                            }
                            break;
                    };
                };
            }
            return validResources;
        }

        public void RemoveResources(LSAgent agent)
        {
            foreach (KeyValuePair<ResourceType, int> entry in agent.resourceCost)
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
        private Dictionary<ResourceType, long> InitResourceList()
        {
            Dictionary<ResourceType, long> list = new Dictionary<ResourceType, long>
            {
                { ResourceType.Gold, 0 },
                { ResourceType.Ore, 0 },
                { ResourceType.Stone, 0 },
                { ResourceType.Wood, 0 },
                { ResourceType.Crystal, 0 },
                { ResourceType.Food, 0 },
                { ResourceType.Provision, 0 }
            };
            return list;
        }

        private void AddStartResourceLimits()
        {
            IncrementResourceLimit(ResourceType.Gold, _startingResources[ResourceType.Gold].startLimit);
            IncrementResourceLimit(ResourceType.Ore, _startingResources[ResourceType.Ore].startLimit);
            IncrementResourceLimit(ResourceType.Stone, _startingResources[ResourceType.Stone].startLimit);
            IncrementResourceLimit(ResourceType.Wood, _startingResources[ResourceType.Wood].startLimit);
            IncrementResourceLimit(ResourceType.Crystal, _startingResources[ResourceType.Crystal].startLimit);
            IncrementResourceLimit(ResourceType.Food, _startingResources[ResourceType.Food].startLimit);
            IncrementResourceLimit(ResourceType.Provision, _startingResources[ResourceType.Provision].startLimit);
        }

        private void AddStartResources()
        {
            AddResource(ResourceType.Gold, _startingResources[ResourceType.Gold].startValue);
            AddResource(ResourceType.Ore, _startingResources[ResourceType.Ore].startValue);
            AddResource(ResourceType.Stone, _startingResources[ResourceType.Stone].startValue);
            AddResource(ResourceType.Wood, _startingResources[ResourceType.Wood].startValue);
            AddResource(ResourceType.Crystal, _startingResources[ResourceType.Crystal].startValue);
            AddResource(ResourceType.Food, _startingResources[ResourceType.Food].startValue);
            AddResource(ResourceType.Provision, _startingResources[ResourceType.Provision].startValue);
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
                                _startingResources[ResourceType.Gold].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Gold_Limit":
                                _startingResources[ResourceType.Gold].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Army":
                                _startingResources[ResourceType.Provision].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Army_Limit":
                                _startingResources[ResourceType.Provision].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Ore":
                                _startingResources[ResourceType.Ore].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Ore_Limit":
                                _startingResources[ResourceType.Ore].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Crystal":
                                _startingResources[ResourceType.Crystal].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Crystal_Limit":
                                _startingResources[ResourceType.Crystal].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Wood":
                                _startingResources[ResourceType.Wood].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Wood_Limit":
                                _startingResources[ResourceType.Wood].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Stone":
                                _startingResources[ResourceType.Stone].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Stone_Limit":
                                _startingResources[ResourceType.Stone].startLimit = (int)(System.Int64)reader.Value;
                                break;
                            case "Food":
                                _startingResources[ResourceType.Food].startValue = (int)(System.Int64)reader.Value;
                                break;
                            case "Food_Limit":
                                _startingResources[ResourceType.Food].startLimit = (int)(System.Int64)reader.Value;
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