using UnityEngine;
using Newtonsoft.Json;
using System.IO;

using RTSLockstep.Agents.AgentController;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Utility;
using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.BehaviourHelpers;

namespace RTSLockstep.Player
{
    public static class PlayerManager
    {
        #region Properties
        private static FastBucket<PlayerDetails> _players = new FastBucket<PlayerDetails>();
        private static PlayerDetails _currentPlayer;
        private static Texture2D[] _avatars;

        public static readonly FastBucket<LocalAgentController> AgentControllers = new FastBucket<LocalAgentController>();

        public static LocalAgentController MainController { get; private set; }
        #endregion

        #region Public
        public static void Initialize()
        {
            MainController = null;
            _players.FastClear();
            AgentControllers.FastClear();
        }

        public static void Simulate()
        {
        }

        public static void Visualize()
        {
        }

        public static int AgentControllerCount
        {
            get { return AgentControllers.Count; }
        }

        public static LocalAgentController GetAgentController(int index)
        {
            return AgentControllers[index];
        }

        public static void AddController(LocalAgentController agentController)
        {
            if (ContainsController(agentController))
            {
                Debug.Log("BOOM");
                return;
            }

            agentController.PlayerIndex = AgentControllers.Add(agentController);
            if (MainController.IsNull())
            {
                MainController = agentController;
            }
        }

        public static void RemoveController(LocalAgentController agentController)
        {
            Selector.Clear();
            AgentControllers.RemoveAt(agentController.PlayerIndex);
            if (MainController == agentController)
            {
                if (AgentControllers.Count == 0)
                {
                    MainController = null;
                }
                else
                {
                    for (int i = 0; i < AgentControllers.PeakCount; i++)
                    {
                        if (AgentControllers.arrayAllocation[i])
                        {
                            MainController = AgentControllers[i];
                            break;
                        }
                    }
                }
            }
        }

        public static void ClearControllers()
        {
            Selector.Clear();
            while (MainController.IsNotNull())
            {
                RemoveController(MainController);
            }

            MainController = null;
            AgentControllers.FastClear();
        }

        public static bool ContainsController(LocalAgentController controller)
        {
            if (AgentControllers.IsNull())
            {
                Debug.Log(controller);
            }
            return controller.PlayerIndex < AgentControllers.PeakCount && AgentControllers.ContainsAt(controller.PlayerIndex, controller);
        }

        /// <summary>
        /// Sends the command for all AgentControllers under the control of this PlayerManager...
        /// Mainly for shared control capabilities
        /// </summary>
        /// <param name="com">COM.</param>
        public static void SendCommand(Command com)
        {
            com.Add(new Selection());
            for (int i = 0; i < AgentControllers.PeakCount; i++)
            {
                if (AgentControllers.arrayAllocation[i])
                {
                    LocalAgentController cont = AgentControllers[i];

                    if (cont.SelectedAgents.Count > 0)
                    {
                        com.ControllerID = cont.ControllerID;

                        //we always sending selection data
                        com.SetData(new Selection(cont.SelectedAgents));

                        CommandManager.SendCommand(com);
                    }
                }
            }
        }

        public static void SelectPlayer(string name, int avatar, int controllerId, int playerIndex)
        {
            //check player doesnt already exist
            bool playerExists = false;
            for (int i = 0; i < _players.PeakCount; i++)
            {
                if (_players.arrayAllocation[i])
                {
                    PlayerDetails commander = _players[i];

                    if (commander.Name == name)
                    {
                        _currentPlayer = commander;
                        playerExists = true;
                    }
                }

            }
            if (!playerExists)
            {
                PlayerDetails newPlayer = new PlayerDetails(name, avatar, controllerId, playerIndex);
                _players.Add(newPlayer);
                _currentPlayer = newPlayer;
                Directory.CreateDirectory("SavedGames" + Path.DirectorySeparatorChar + name);
            }

            Save();
        }

        // change to accept player param
        // search through players list
        public static string GetPlayerName()
        {
            return _currentPlayer.Name == "" ? "Unknown" : _currentPlayer.Name;
        }

        public static void SetAvatarTextures(Texture2D[] avatarTextures)
        {
            _avatars = avatarTextures;
        }

        //change to accept commander param
        //search through players list
        public static Texture2D GetPlayerAvatar()
        {
            if (_avatars.IsNull())
            {
                return null;
            }

            if (_currentPlayer.Avatar >= 0 && _currentPlayer.Avatar < _avatars.Length)
            {
                return _avatars[_currentPlayer.Avatar];
            }

            return null;
        }

        public static void Save()
        {
            _ = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            using (StreamWriter sw = new StreamWriter("SavedGames" + Path.DirectorySeparatorChar + "Players.json"))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Players");
                    writer.WriteStartArray();
                    for (int i = 0; i < _players.PeakCount; i++)
                    {
                        if (_players.arrayAllocation[i])
                        {
                            SavePlayer(writer, _players[i]);
                        }
                    }
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
            }
        }

        public static void Load()
        {
            _players.Clear();

            string filename = "SavedGames" + Path.DirectorySeparatorChar + "Players.json";
            if (File.Exists(filename))
            {
                //read contents of file
                string input;
                using (StreamReader sr = new StreamReader(filename))
                {
                    input = sr.ReadToEnd();
                }
                if (input != null)
                {
                    //parse contents of file
                    using (JsonTextReader reader = new JsonTextReader(new StringReader(input)))
                    {
                        while (reader.Read())
                        {
                            if (reader.Value.IsNotNull())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    if ((string)reader.Value == "Players")
                                    {
                                        LoadPlayers(reader);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string[] GetPlayerNames()
        {
            string[] playerNames = new string[_players.Count];
            for (int i = 0; i < playerNames.Length; i++)
            {
                playerNames[i] = _players[i].Name;
            }
            return playerNames;
        }

        public static int GetAvatar(string playerName)
        {
            for (int i = 0; i < _players.PeakCount; i++)
            {
                if (_players.arrayAllocation[i])
                {
                    if (_players[i].Name == playerName)
                    {
                        return _players[i].Avatar;
                    }
                }
            }

            return 0;
        }

        public static string[] GetSavedGames()
        {
            DirectoryInfo directory = new DirectoryInfo("SavedGames" + Path.DirectorySeparatorChar + _currentPlayer.Name);
            FileInfo[] files = directory.GetFiles();
            string[] savedGames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].Name;
                savedGames[i] = filename.Substring(0, filename.IndexOf("."));
            }

            return savedGames;
        }

        public static void CreatePlayer(LocalAgentController controller)
        {
            if (controller.ControllingPlayer.IsNotNull())
            {
                Debug.LogError("A player called '" + controller.ControllingPlayer.gameObject.name + "' already exists for '" + controller.ToString() + "'.");
            }

            if (!Object.FindObjectOfType<RTSGameManager>())
            {
                Debug.LogError("A game manager has not been initialized!");
            }

            //load from ls db
            GameObject playerObject = Object.Instantiate(GameResourceManager.GetPlayerObject(), Object.FindObjectOfType<RTSGameManager>().GetComponentInChildren<LSPlayers>().transform);

            playerObject.name = controller.ControllerName;

            LSPlayer playerClone = playerObject.GetComponent<LSPlayer>();
            //change to user's selected username
            playerClone.Username = controller.ControllerName;
            playerClone.SetController(controller);

            if (ContainsController(controller))
            {
                playerClone.IsHuman = true;
            }

            //come up with better way to set selected player to the current player
            if (controller == MainController)
            {
                SelectPlayer(playerClone.Username, 0, controller.ControllerID, controller.PlayerIndex);
            }

            controller.ControllingPlayer = playerClone;
            BehaviourHelperManager.InitializeOnDemand(controller.ControllingPlayer);
        }
        #endregion

        #region Private
        private static void SavePlayer(JsonWriter writer, PlayerDetails commander)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(commander.Name);
            writer.WritePropertyName("Avatar");
            writer.WriteValue(commander.Avatar);

            writer.WriteEndObject();
        }

        private static void LoadPlayers(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    LoadPlayer(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }

        private static void LoadPlayer(JsonTextReader reader)
        {
            string currValue = "", name = "";
            int avatar = 0;
            int controllerId = 0;
            int playerIndex = 0;
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
                            case "Name":
                                name = (string)reader.Value;
                                break;
                            case "Avatar":
                                avatar = (int)(long)reader.Value;
                                break;
                            case "ControllerId":
                                controllerId = (int)(long)reader.Value;
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    if (reader.TokenType == JsonToken.EndObject)
                    {
                        _players.Add(new PlayerDetails(name, avatar, controllerId, playerIndex));
                        return;
                    }
                }
            }
        }
        #endregion
    }
}