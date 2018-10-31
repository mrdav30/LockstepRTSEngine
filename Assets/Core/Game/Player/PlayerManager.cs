using System;
using UnityEngine;
using FastCollections;
using Newtonsoft.Json;
using System.IO;
using RTSLockstep;

namespace RTSLockstep
{
    public static class PlayerManager
    {
        #region Properties
        private struct PlayerDetails
        {
            private readonly string name;
            private readonly int avatar;
            private readonly int controllerId;
            private readonly int playerIndex;
            public PlayerDetails(string name, int avatar, int controllerId, int playerIndex)
            {
                this.name = name;
                this.avatar = avatar;
                this.controllerId = controllerId;
                this.playerIndex = playerIndex;
            }
            // using the name of the Player as a unique identifier
            // to support multiplayer at some point this may need to be modified
            public string Name { get { return name; } }
            public int Avatar { get { return avatar; } }
            public int ControllerId { get { return ControllerId; } }
            public int PlayerIndex { get { return PlayerIndex; } }
        }
        private static FastBucket<PlayerDetails> Players = new FastBucket<PlayerDetails>();
        private static PlayerDetails currentPlayer;
        private static Texture2D[] avatars;

        public static readonly FastBucket<AgentController> AgentControllers = new FastBucket<AgentController>();

        public static AgentController MainController { get; private set; }
     //   public static Player MainController.Commander { get; private set; }
      //  public static AgentController _environmentController { get; set; }
        #endregion

        #region Public
        public static void Initialize()
        {
            MainController = null;
            Players.FastClear();
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

        public static AgentController GetAgentController(int index)
        {
            return AgentControllers [index];
        }

        //instantiate commander object
        public static void AddController(AgentController agentController) //, string defaultController
        {
            if (PlayerManager.ContainsController(agentController))
            {
                Debug.Log("BOOM");
                return;
            }

            agentController.PlayerIndex = AgentControllers.Add(agentController);
			if (MainController == null)
			{
				MainController = agentController;
			}
          //  CreatePlayerObject(agentController, defaultController);
        }

        public static void RemoveController(AgentController agentController)
        { 
            Selector.Clear();
            AgentControllers.RemoveAt(agentController.PlayerIndex);
            if (MainController == agentController)
            {
                if (AgentControllers.Count == 0)
                    MainController = null;
                else
                {
                    for (int i = 0; i < AgentControllers.PeakCount; i++)
                    {
                        if (AgentControllers.arrayAllocation [i])
                        {
                            MainController = AgentControllers [i];
                            break;
                        }
                    }
                }
            }
        }

        public static void ClearControllers()
        {
            Selector.Clear();
            while (MainController != null)
            {
                RemoveController(MainController);
            }
            //return;
            //MainController = null;
            //AgentControllers.FastClear();
        }

        public static bool ContainsController(AgentController controller)
        {
			if (AgentControllers == null) Debug.Log(controller);
            return controller.PlayerIndex < AgentControllers.PeakCount && AgentControllers.ContainsAt(controller.PlayerIndex, controller);
        }

        public static AllegianceType GetAllegiance(AgentController otherController)
        {
            if (Selector.MainSelectedAgent != null)
                return Selector.MainSelectedAgent.Controller.GetAllegiance(otherController);
            if (MainController == null)
                return AllegianceType.Neutral;
            return MainController.GetAllegiance(otherController);
        }

        public static AllegianceType GetAllegiance(RTSAgent agent)
        {
            return PlayerManager.GetAllegiance(agent.Controller);
        }

        /// <summary>
        /// Sends the command for all AgentControllers under the control of this PlayerManager...
        /// Mainly for shared control capabilities
        /// </summary>
        /// <param name="com">COM.</param>
        public static void SendCommand(Command com)
        {
            com.Add<Selection>(new Selection());
            for (int i = 0; i < AgentControllers.PeakCount; i++)
            {
                if (AgentControllers.arrayAllocation [i])
                {
                    AgentController cont = AgentControllers [i];

					if (cont.SelectedAgents.Count > 0)
                    {
                        com.ControllerID = cont.ControllerID;

						#if false
                        if (cont.SelectionChanged)
                        {
                            com.SetData<Selection>(new Selection(cont.SelectedAgents));
                            cont.SelectionChanged = false;

						} else
                        {
                            com.ClearData<Selection>();
                        }
						#else
						//we always sending selection data
						com.SetData<Selection>(new Selection(cont.SelectedAgents));
						cont.SelectionChanged = false;
						#endif
                        CommandManager.SendCommand(com);
                    }
                }
            }
        }

        public static void SelectPlayer(string name, int avatar, int controllerId, int playerIndex)
        {
            //check commander doesnt already exist
            bool playerExists = false;
            for (int i = 0; i < Players.PeakCount; i++)
            {
                if (Players.arrayAllocation[i])
                {
                    PlayerDetails commander = Players[i];

                    if (commander.Name == name)
                    {
                        currentPlayer = commander;
                        playerExists = true;
                    }
                }

            }
            if (!playerExists)
            {
                PlayerDetails newPlayer = new PlayerDetails(name, avatar, controllerId, playerIndex);
                Players.Add(newPlayer);
                currentPlayer = newPlayer;
                Directory.CreateDirectory("SavedGames" + Path.DirectorySeparatorChar + name);
            }
            Save();
        }

        //change to accept commander param
        //search through players list
        public static string GetPlayerName()
        {
            return currentPlayer.Name == "" ? "Unknown" : currentPlayer.Name;
        }

        public static void SetAvatarTextures(Texture2D[] avatarTextures)
        {
            avatars = avatarTextures;
        }

        //change to accept commander param
        //search through players list
        public static Texture2D GetPlayerAvatar()
        {
            if (avatars == null)
            {
                return null;
            }
            if (currentPlayer.Avatar >= 0 && currentPlayer.Avatar < avatars.Length)
            {
                return avatars[currentPlayer.Avatar];
            }
            return null;
        }

        public static void Save()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw = new StreamWriter("SavedGames" + Path.DirectorySeparatorChar + "Players.json"))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Players");
                    writer.WriteStartArray();
                    for (int i = 0; i < Players.PeakCount; i++)
                    {
                        if (Players.arrayAllocation[i])
                        {
                            SavePlayer(writer, Players[i]);
                        }
                    }
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
            }
        }

        public static void Load()
        {
            Players.Clear();

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
                            if (reader.Value != null)
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
            string[] playerNames = new string[Players.Count];
            for (int i = 0; i < playerNames.Length; i++)
            {
                playerNames[i] = Players[i].Name;
            }
            return playerNames;
        }

        public static int GetAvatar(string playerName)
        {
            for (int i = 0; i < Players.PeakCount; i++)
            {
                if (Players.arrayAllocation[i])
                {
                    if (Players[i].Name == playerName)
                    {
                        return Players[i].Avatar;
                    }
                }
            }
            return 0;
        }

        public static string[] GetSavedGames()
        {
            DirectoryInfo directory = new DirectoryInfo("SavedGames" + Path.DirectorySeparatorChar + currentPlayer.Name);
            FileInfo[] files = directory.GetFiles();
            string[] savedGames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].Name;
                savedGames[i] = filename.Substring(0, filename.IndexOf("."));
            }
            return savedGames;
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
                                avatar = (int)(System.Int64)reader.Value;
                                break;
                            case "ControllerId":
                                controllerId = (int)(System.Int64)reader.Value;
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
                        Players.Add(new PlayerDetails(name, avatar, controllerId, playerIndex));
                        return;
                    }
                }
            }
        }
        #endregion
    }
}