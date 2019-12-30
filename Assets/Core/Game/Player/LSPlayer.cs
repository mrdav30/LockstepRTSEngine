using Newtonsoft.Json;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentControllerSystem;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Managers.GameState;
using UnityEngine;

namespace RTSLockstep.Player
{
    public class LSPlayer : BehaviourHelper
    {
        #region Properties
        public string username;
        public bool human;
        public HUD CachedHud { get; private set; }
        public ResourceManager CachedResourceManager { get; private set; }
        private AgentController _cachedController;

        public Color TeamColor;
        private bool Setted = false;
        #endregion

        #region MonoBehavior
        protected void Setup()
        {
            CachedResourceManager = GetComponentInParent<ResourceManager>();
            CachedHud = GetComponentInParent<HUD>();

            CachedResourceManager.Setup();
            CachedHud.Setup();

            if (!GameResourceManager.AssignedTeamColors.Contains(TeamColor))
            {
                GameResourceManager.AssignedTeamColors.Add(TeamColor);
            }
            else
            {
                TeamColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                GameResourceManager.AssignedTeamColors.Add(TeamColor);
            }

            Setted = true;
        }

        // Use this for initialization
        protected override void OnInitialize()
        {
            if (!Setted)
            {
                Setup();
            }

            CachedResourceManager.Initialize();
        }

        // Update is called once per frame
        protected override void OnVisualize()
        {
            if (human)
            {
                CachedResourceManager.Visualize();
                CachedHud.Visualize();
            }
        }

        protected override void DoGUI()
        {
            CachedHud.DoGUI();
        }
        #endregion

        #region Public
        public virtual void SaveDetails(JsonWriter writer)
        {
            SaveManager.WriteString(writer, "Username", username);
            SaveManager.WriteBoolean(writer, "Human", human);
            SaveManager.WriteColor(writer, "TeamColor", TeamColor);
            SaveManager.SavePlayerResources(writer, CachedResourceManager.GetResources(), CachedResourceManager.GetResourceLimits());
            SaveManager.SavePlayerRTSAgents(writer, GetComponent<LSAgents>().GetComponentsInChildren<LSAgent>());
        }

        public LSAgent GetObjectForId(int id)
        {
            LSAgent[] objects = GameObject.FindObjectsOfType(typeof(LSAgent)) as LSAgent[];
            foreach (LSAgent obj in objects)
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
                            TeamColor = LoadManager.LoadColor(reader);
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
            LSAgent[] agents = GetComponentInChildren<LSAgents>().GetComponentsInChildren<LSAgent>();
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
            LSAgents agents = GetComponentInChildren<LSAgents>();
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
                        LSAgent agent = newObject.GetComponent<LSAgent>();
                        agent.name = agent.name.Replace("(Clone)", "").Trim();
                        agent.LoadDetails(reader);
                        agent.transform.parent = agents.transform;
                        agent.SetControllingPlayer(this);
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
}