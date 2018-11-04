using Newtonsoft.Json;
using RotaryHeart.Lib.SerializableDictionary;
using RTSLockstep;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StartingResourceInfo
{
    [Range(0, 5000)]
    public long startValue;
    [Range(0, 5000)]
    public long startLimit;
}

[Serializable]
public class StartingResources : SerializableDictionaryBase<ResourceType, StartingResourceInfo> { };

public class ResourceManager : MonoBehaviour
{
    #region Properties
    [SerializeField]
    private StartingResources startingResources = new StartingResources
    {
        {ResourceType.Gold, null},
        {ResourceType.Ore, null},
        {ResourceType.Stone, null},
        {ResourceType.Wood, null},
        {ResourceType.Food, null},
        {ResourceType.Crystal, null},
        {ResourceType.Provision, null}
    };

    private AgentCommander _cachedCommander;
    private Dictionary<ResourceType, long> _resources;
    private Dictionary<ResourceType, long> _resourceLimits;
    #endregion

    #region Behavior
    // Use this for initialization
    public void Setup()
    {
        _cachedCommander = GetComponentInParent<AgentCommander>();
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
        _cachedCommander.CachedHud.SetResourceValues(_resources, _resourceLimits);
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

    public bool CheckResources(RTSAgent agent)
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

    public void RemoveResources(RTSAgent agent)
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
        Dictionary<ResourceType, long> list = new Dictionary<ResourceType, long>();
        list.Add(ResourceType.Gold, 0);
        list.Add(ResourceType.Ore, 0);
        list.Add(ResourceType.Stone, 0);
        list.Add(ResourceType.Wood, 0);
        list.Add(ResourceType.Crystal, 0);
        list.Add(ResourceType.Food, 0);
        list.Add(ResourceType.Provision, 0);
        return list;
    }

    private void AddStartResourceLimits()
    {
        IncrementResourceLimit(ResourceType.Gold, startingResources[ResourceType.Gold].startLimit);
        IncrementResourceLimit(ResourceType.Ore, startingResources[ResourceType.Ore].startLimit);
        IncrementResourceLimit(ResourceType.Stone, startingResources[ResourceType.Stone].startLimit);
        IncrementResourceLimit(ResourceType.Wood, startingResources[ResourceType.Wood].startLimit);
        IncrementResourceLimit(ResourceType.Crystal, startingResources[ResourceType.Crystal].startLimit);
        IncrementResourceLimit(ResourceType.Food, startingResources[ResourceType.Food].startLimit);
        IncrementResourceLimit(ResourceType.Provision, startingResources[ResourceType.Provision].startLimit);
    }

    private void AddStartResources()
    {
        AddResource(ResourceType.Gold, startingResources[ResourceType.Gold].startValue);
        AddResource(ResourceType.Ore, startingResources[ResourceType.Ore].startValue);
        AddResource(ResourceType.Stone, startingResources[ResourceType.Stone].startValue);
        AddResource(ResourceType.Wood, startingResources[ResourceType.Wood].startValue);
        AddResource(ResourceType.Crystal, startingResources[ResourceType.Crystal].startValue);
        AddResource(ResourceType.Food, startingResources[ResourceType.Food].startValue);
        AddResource(ResourceType.Provision, startingResources[ResourceType.Provision].startValue);
    }

    public void LoadResources(JsonTextReader reader)
    {
        if (reader == null)
        {
            return;
        }
        string currValue = "";
        while (reader.Read())
        {
            if (reader.Value != null)
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
                            startingResources[ResourceType.Gold].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Gold_Limit":
                            startingResources[ResourceType.Gold].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Army":
                            startingResources[ResourceType.Provision].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Army_Limit":
                            startingResources[ResourceType.Provision].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Ore":
                            startingResources[ResourceType.Ore].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Ore_Limit":
                            startingResources[ResourceType.Ore].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Crystal":
                            startingResources[ResourceType.Crystal].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Crystal_Limit":
                            startingResources[ResourceType.Crystal].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Wood":
                            startingResources[ResourceType.Wood].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Wood_Limit":
                            startingResources[ResourceType.Wood].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Stone":
                            startingResources[ResourceType.Stone].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Stone_Limit":
                            startingResources[ResourceType.Stone].startLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Food":
                            startingResources[ResourceType.Food].startValue = (int)(System.Int64)reader.Value;
                            break;
                        case "Food_Limit":
                            startingResources[ResourceType.Food].startLimit = (int)(System.Int64)reader.Value;
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
