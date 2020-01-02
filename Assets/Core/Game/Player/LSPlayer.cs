using Newtonsoft.Json;
using UnityEngine;

using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Utility;

namespace RTSLockstep.Player
{
    public class LSPlayer : BehaviourHelper
    {
        #region Properties
        public string Username;
        public bool IsCurrentPlayer;
        public HUD PlayerHUD;
        public ResourceManager PlayerResourceManager;
        public Color TeamColor;

        public Transform AgentContainer; 

        private LocalAgentController _cachedController;
        private bool IsSetup = false;
        #endregion

        #region MonoBehavior
        protected void Setup()
        {
            PlayerResourceManager.Setup();
            PlayerHUD.Setup();

            if (!GameResourceManager.AssignedTeamColors.Contains(TeamColor))
            {
                GameResourceManager.AssignedTeamColors.Add(TeamColor);
            }
            else
            {
                TeamColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                GameResourceManager.AssignedTeamColors.Add(TeamColor);
            }

            AgentContainer = GetComponentInChildren<LSAgents>().transform;

            IsSetup = true;
        }

        // Use this for initialization
        protected override void OnInitialize()
        {
            if (!IsSetup)
            {
                Setup();
            }

            PlayerResourceManager.Initialize();
        }

        // Update is called once per frame
        protected override void OnVisualize()
        {
            if (IsCurrentPlayer)
            {
                PlayerResourceManager.Visualize();
                PlayerHUD.Visualize();
            }
        }

        protected override void OnUpdateGUI()
        {
            PlayerHUD.DoGUI();
        }
        #endregion

        #region Public
        public virtual void SaveDetails(JsonWriter writer)
        {
            SaveManager.WriteString(writer, "Username", Username);
            SaveManager.WriteBoolean(writer, "Human", IsCurrentPlayer);
            SaveManager.WriteColor(writer, "TeamColor", TeamColor);
            SaveManager.SavePlayerResources(writer, PlayerResourceManager.GetResources(), PlayerResourceManager.GetResourceLimits());
            SaveManager.SavePlayerRTSAgents(writer, GetComponent<LSAgents>().GetComponentsInChildren<LSAgent>());
        }

        public LSAgent GetObjectForId(int id)
        {
            LSAgent[] objects = FindObjectsOfType(typeof(LSAgent)) as LSAgent[];
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
            if (reader.IsNull())
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
                                Username = (string)reader.Value;
                                break;
                            case "Human":
                                IsCurrentPlayer = (bool)reader.Value;
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
                            PlayerResourceManager.LoadResources(reader);
                            break;
                        case "Units":
                            LoadLSAgents(reader);
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
            if (agents.IsNotNull() && agents.Length > 0)
            {
                return false;
            }

            return true;
        }

        public void SetController(LocalAgentController controller)
        {
            _cachedController = controller;
        }

        public LocalAgentController GetController()
        {
            return _cachedController;
        }
        #endregion

        #region Private
        //this should be in the controller...
        private void LoadLSAgents(JsonTextReader reader)
        {
            if (reader.IsNull())
            {
                return;
            }

            LSAgents agents = GetComponentInChildren<LSAgents>();
            string currValue = "";
            while (reader.Read())
            {
                if (reader.Value.IsNotNull())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        currValue = (string)reader.Value;
                    }
                    else if (currValue == "Type")
                    {
                        string type = (string)reader.Value;
                        // need to create unit via player controller...
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