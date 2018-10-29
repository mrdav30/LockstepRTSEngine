using Newtonsoft.Json;
using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

public class AgentCommander : BehaviourHelper
{
    #region Properties
    public string username;
    public bool human;
    public HUD CachedHud { get; private set; }
    public BuildManager CachedBuilderManager { get; private set; }
    public AgentController CachedController { get; private set; }
    public int startGold, startGoldLimit, startArmy, startArmyLimit, startOre, startOreLimit, startCrystal, startCrystalLimit,
        startWood, startWoodLimit, startStone, startStoneLimit, startFood, startFoodLimit;
    public Color teamColor;

    private Dictionary<ResourceType, long> resources, resourceLimits;
    private bool Setted = false;
    #endregion

    #region MonoBehavior
    protected void Setup()
    {
        resources = InitResourceList();
        resourceLimits = InitResourceList();
        Setted = true;
    }

    // Use this for initialization
    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
        CachedHud = GetComponentInChildren<HUD>();
        CachedBuilderManager = GetComponentInChildren<BuildManager>();
        AddStartResourceLimits();
        AddStartResources();
    }

    // Update is called once per frame
    protected override void OnVisualize()
    {
        if (human)
        {
            CachedHud.SetResourceValues(resources, resourceLimits);
        }
    }
    #endregion

    #region Public
    public void AddResource(ResourceType type, long amount)
    {
        resources[type] += amount;
    }

    public void IncrementResourceLimit(ResourceType type, int amount)
    {
        resourceLimits[type] += amount;
    }

    public virtual void SaveDetails(JsonWriter writer)
    {
        SaveManager.WriteString(writer, "Username", username);
        SaveManager.WriteBoolean(writer, "Human", human);
        SaveManager.WriteColor(writer, "TeamColor", teamColor);
        SaveManager.SavePlayerResources(writer, resources, resourceLimits);
        SaveManager.SavePlayerRTSAgents(writer, GetComponent<RTSAgents>().GetComponentsInChildren<RTSAgent>());
    }

    public RTSAgent GetObjectForId(int id)
    {
        RTSAgent[] objects = GameObject.FindObjectsOfType(typeof(RTSAgent)) as RTSAgent[];
        foreach (RTSAgent obj in objects)
        {
            if (obj.GlobalID == id)
            {
                return obj;
            }
        }
        return null;
    }

    public void LoadDetails(JsonTextReader reader)
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
                        case "Username":
                            username = (string)reader.Value;
                            break;
                        case "Human":
                            human = (bool)reader.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray)
            {
                switch (currValue)
                {
                    case "TeamColor":
                        teamColor = LoadManager.LoadColor(reader);
                        break;
                    case "Resources":
                        LoadResources(reader);
                        break;
                    case "Units":
                        LoadRTSAgents(reader);
                        break;
                    default:
                        break;
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                return;
            }
        }
    }

    public bool IsDead()
    {
        RTSAgent[] agents = GetComponentInChildren<RTSAgents>().GetComponentsInChildren<RTSAgent>();
        if (agents != null && agents.Length > 0)
        {
            return false;
        }
        return true;
    }

    public long GetResourceAmount(ResourceType type)
    {
        return resources[type];
    }

    public void RemoveResource(ResourceType type, int amount)
    {
        resources[type] -= amount;
    }
    
    public void SetController(AgentController controller)
    {
        CachedController = controller;
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
        list.Add(ResourceType.Army, 0);
        return list;
    }

    private void AddStartResourceLimits()
    {
        IncrementResourceLimit(ResourceType.Gold, startGoldLimit);
        IncrementResourceLimit(ResourceType.Ore, startOreLimit);
        IncrementResourceLimit(ResourceType.Stone, startStoneLimit);
        IncrementResourceLimit(ResourceType.Wood, startWoodLimit);
        IncrementResourceLimit(ResourceType.Crystal, startCrystalLimit);
        IncrementResourceLimit(ResourceType.Food, startFoodLimit);
        IncrementResourceLimit(ResourceType.Army, startArmyLimit);
    }

    private void AddStartResources()
    {
        AddResource(ResourceType.Gold, startGold);
        AddResource(ResourceType.Ore, startOre);
        AddResource(ResourceType.Stone, startStone);
        AddResource(ResourceType.Wood, startWood);
        AddResource(ResourceType.Crystal, startCrystal);
        AddResource(ResourceType.Food, startFood);
        AddResource(ResourceType.Army, startArmy);
    }

    private void LoadResources(JsonTextReader reader)
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
                            startGold = (int)(System.Int64)reader.Value;
                            break;
                        case "Gold_Limit":
                            startGoldLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Army":
                            startArmy = (int)(System.Int64)reader.Value;
                            break;
                        case "Army_Limit":
                            startArmyLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Ore":
                            startOre = (int)(System.Int64)reader.Value;
                            break;
                        case "Ore_Limit":
                            startOreLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Crystal":
                            startCrystal = (int)(System.Int64)reader.Value;
                            break;
                        case "Crystal_Limit":
                            startCrystalLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Wood":
                            startWood = (int)(System.Int64)reader.Value;
                            break;
                        case "Wood_Limit":
                            startWoodLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Stone":
                            startStone = (int)(System.Int64)reader.Value;
                            break;
                        case "Stone_Limit":
                            startStoneLimit = (int)(System.Int64)reader.Value;
                            break;
                        case "Food":
                            startFood = (int)(System.Int64)reader.Value;
                            break;
                        case "Food_Limit":
                            startFoodLimit = (int)(System.Int64)reader.Value;
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

    private void LoadRTSAgents(JsonTextReader reader)
    {
        if (reader == null)
        {
            return;
        }
        RTSAgents agents = GetComponentInChildren<RTSAgents>();
        string currValue = "", type = "";
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currValue = (string)reader.Value;
                }
                else if (currValue == "Type")
                {
                    type = (string)reader.Value;
                    // need to create unit via commander controller...
                    GameObject newObject = Instantiate(ResourceManager.GetAgentTemplate(type).gameObject);
                    RTSAgent agent = newObject.GetComponent<RTSAgent>();
                    agent.name = agent.name.Replace("(Clone)", "").Trim();
                    agent.LoadDetails(reader);
                    agent.transform.parent = agents.transform;
                    agent.SetCommander();
                    agent.SetTeamColor();

                    if (agent.GetAbility<Structure>().UnderConstruction())
                    {
                        agent.SetTransparentMaterial(CachedBuilderManager.allowedMaterial, true);
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
