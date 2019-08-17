using Newtonsoft.Json;
using RTSLockstep;
using UnityEngine;

public class AgentCommander : BehaviourHelper
{
    #region Properties
    public string username;
    public bool human;
    public HUD CachedHud { get; private set; }
    public ResourceManager CachedResourceManager { get; private set; }
    private AgentController _cachedController;

    public Color teamColor;
    private bool Setted = false;
    #endregion

    #region MonoBehavior
    protected void Setup()
    {
        CachedResourceManager = GetComponentInParent<ResourceManager>();
        CachedHud = GetComponentInParent<HUD>();

        CachedResourceManager.Setup();
        CachedHud.Setup();

        Setted = true;
    }

    // Use this for initialization
    protected override void OnInitialize()
    {
        if (!Setted)
            Setup();
        CachedResourceManager.Initialize();
     //   CachedBuilderManager.Initialize();
    }

    // Update is called once per frame
    protected override void OnVisualize()
    {
        if (human)
        {
            CachedResourceManager.Visualize();
            CachedHud.Visualize();
          //  CachedBuilderManager.Visualize();
        }
    }

    protected override void doGUI()
    {
        CachedHud.doGUI();
    }
    #endregion

    #region Public
    public virtual void SaveDetails(JsonWriter writer)
    {
        SaveManager.WriteString(writer, "Username", username);
        SaveManager.WriteBoolean(writer, "Human", human);
        SaveManager.WriteColor(writer, "TeamColor", teamColor);
        SaveManager.SavePlayerResources(writer, CachedResourceManager.GetResources(), CachedResourceManager.GetResourceLimits());
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
                        CachedResourceManager.LoadResources(reader);
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

    public void SetController(AgentController controller)
    {
        _cachedController = controller;
    }

    public AgentController GetController()
    {
        return _cachedController;
    }
    #endregion

    #region Private
    //this should be in the controller...
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
                    GameObject newObject = Instantiate(GameResourceManager.GetAgentTemplate(type).gameObject);
                    RTSAgent agent = newObject.GetComponent<RTSAgent>();
                    agent.name = agent.name.Replace("(Clone)", "").Trim();
                    agent.LoadDetails(reader);
                    agent.transform.parent = agents.transform;
                    agent.SetCommander(this);
                    agent.SetTeamColor();
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
